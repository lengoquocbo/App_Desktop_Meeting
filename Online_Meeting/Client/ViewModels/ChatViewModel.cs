using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Models;
using Online_Meeting.Client.Services;
using Refit;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Online_Meeting.Client.ViewModels
{
    public class ChatViewModel : ViewModelBase, IDisposable
    {
        private readonly IChatService _chatService;
        private readonly ChatSignalRService _signalR;
        private readonly IFileUploadService _upload;
        private readonly ITokenService _token;

        private Guid _currentGroupId;
        private Guid _currentUserId;
        private string _currentUserName;
        private bool _isLoading;
        private bool _isConnected;
        private string _connectionStatus = "Disconnected";
        private string _typingIndicator = "";
        private int _currentPage = 1;
        private const int PageSize = 50;
        private bool _hasMoreMessages = true;

        /// <summary>
        // lưu trữ thời gian nhận tin nhắn cuối cùng cho mỗi nhóm để quản lý thông báo
        /// </summary>
        private Dictionary<Guid, DateTime> _lastMessageTimes = new(); // luu trữ thời gian nhận tin nhắn cuối cùng cho mỗi nhóm
        private Dictionary<Guid, int> _unreadCounts = new(); // lưu trữ số tin nhắn chưa đọc cho mỗi nhóm

        public DateTime GetLastMessageTime(Guid groupId)
        {
            return _lastMessageTimes.TryGetValue(groupId, out var time) ? time : DateTime.MinValue;
        }

        public int GetUnreadCount(Guid groupId)
        {
            return _unreadCounts.TryGetValue(groupId, out var count) ? count : 0;
        }

        public Guid CurrentGroupId { get => _currentGroupId; private set => SetProperty(ref _currentGroupId, value); }
        public Guid CurrentUserId { get => _currentUserId; private set => SetProperty(ref _currentUserId, value); }
        public string CurrentUserName { get => _currentUserName; private set => SetProperty(ref _currentUserName, value); }
        public ObservableCollection<ChatMessage> Messages { get; } = new();
        public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
        public bool IsConnected { get => _isConnected; private set => SetProperty(ref _isConnected, value); }
        public string ConnectionStatus { get => _connectionStatus; private set => SetProperty(ref _connectionStatus, value); }
        public string TypingIndicator { get => _typingIndicator; private set => SetProperty(ref _typingIndicator, value); }
        public event EventHandler<string>? ErrorOccurred;

        public ChatViewModel(IChatService chatService, ChatSignalRService signalR, IFileUploadService upload, TokenService token)
        {
            _chatService = chatService;
            _signalR = signalR;
            _upload = upload;
            _token = token;

            // Subscribe SignalR events
            _signalR.GroupMessageReceived += OnGroupMessageReceived;
            _signalR.MessageUpdated += OnMessageUpdated;
            _signalR.MessageDeleted += OnMessageDeleted;
            _signalR.UserTyping += OnUserTyping;
            _signalR.ConnectionStateChanged += OnConnectionChanged;
        }

        public async Task InitializeAsync(Guid userId, string userName)
        {
            try
            {
                var token = _token.GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    ErrorOccurred?.Invoke(this, "Please login first");
                    return;
                }

                CurrentUserId = userId;
                CurrentUserName = userName;

                Debug.WriteLine($"[ViewModel] Initializing with username: {userName}");

                await _signalR.ConnectAsync();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Initialize error: {ex.Message}");
            }
        }


        // LOAD GROUP - ĐÃ SỬA: Clear rồi mới load
        public async Task LoadGroupAsync(Guid groupId)
        {
            try
            {
                Debug.WriteLine($"[ViewModel] LoadGroupAsync: {groupId}");

                //  Nếu đang ở group khác, leave trước
                if (CurrentGroupId != Guid.Empty && CurrentGroupId != groupId)
                {
                    Debug.WriteLine($"[ViewModel] Leaving current group: {CurrentGroupId}");
                    await _signalR.LeaveGroupAsync(CurrentGroupId);
                }

                //  Clear messages trước khi load group mới
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine($"[ViewModel] Clearing {Messages.Count} old messages");
                    Messages.Clear();
                });
                _unreadCounts[groupId] = 0;


                // Set group mới
                CurrentGroupId = groupId;
                _currentPage = 1;
                _hasMoreMessages = true;

                // Join vào SignalR group
                Debug.WriteLine($"[ViewModel] Joining group: {groupId}");
                await _signalR.JoinGroupAsync(groupId);

                // Load messages từ API
                await LoadMessagesAsync();
                // ✅ Thông báo UI refresh
                //GroupNeedsUpdate?.Invoke(this, groupId);
                Debug.WriteLine($"[ViewModel] LoadGroupAsync completed. Messages: {Messages.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViewModel] LoadGroupAsync ERROR: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"Load group error: {ex.Message}");
            }
        }

        //  LOAD MESSAGES - ĐÃ SỬA: OrderBy để cũ nhất trước
        public async Task LoadMessagesAsync()
        {
            if (IsLoading || !_hasMoreMessages || CurrentGroupId == Guid.Empty)
            {
                Debug.WriteLine($"[ViewModel] LoadMessagesAsync SKIPPED - IsLoading:{IsLoading}, HasMore:{_hasMoreMessages}, GroupId:{CurrentGroupId}");
                return;
            }

            try
            {
                IsLoading = true;
                Debug.WriteLine($"[ViewModel] Loading messages - Page: {_currentPage}");

                var response = await _chatService.GetGroupMessagesAsync(CurrentGroupId, _currentPage, PageSize);

                if (response?.Success == true && response.Data != null)
                {
                    Debug.WriteLine($"[ViewModel] API returned {response.Data.Count()} messages");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var myUsername = CurrentUserName?.ToLower() ?? _token.GetUsername()?.ToLower();
                        Debug.WriteLine($"[ViewModel] My username: {myUsername}");

                        // SẮP XẾP: CŨ NHẤT TRƯỚC (OrderBy - Tăng dần)
                        var sortedMessages = response.Data.OrderBy(m => m.SendAt).ToList();

                        foreach (var msg in sortedMessages)
                        {
                            // SO SÁNH USERNAME
                            msg.IsMyMessage = msg.UserName?.ToLower() == myUsername;

                            Debug.WriteLine($"[ViewModel] Add: '{msg.Content}' from '{msg.UserName}' IsMyMessage:{msg.IsMyMessage}");

                            Messages.Add(msg);
                        }
                    });

                    _hasMoreMessages = response.Pagination?.HasMore ?? false;
                    _currentPage++;

                    Debug.WriteLine($"[ViewModel] Messages loaded. Total in collection: {Messages.Count}");
                }
                else
                {
                    Debug.WriteLine($"[ViewModel] API failed or empty: Success={response?.Success}, Data={response?.Data?.Count()}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViewModel] LoadMessagesAsync ERROR: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"Load messages failed: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        //  GỬI TIN NHẮN - ĐÃ SỬA
        public async Task SendTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (CurrentGroupId == Guid.Empty)
            {
                ErrorOccurred?.Invoke(this, "Please select a group first");
                return;
            }

            Debug.WriteLine($"[ViewModel] Sending message: {text}");

            //  TẠO TIN NHẮN ẢO (Optimistic UI)
            var tempId = Guid.NewGuid();
            var displayName = CurrentUserName ?? _token.GetUsername() ?? "You";

            var tempMessage = new ChatMessage
            {
                Id = tempId,
                GroupId = CurrentGroupId,
                UserId = CurrentUserId,
                UserName = displayName,
                Content = text,
                TypeMessage = "TEXT",
                IsMyMessage = true,
                IsSending = true,
                SendAt = DateTime.Now
            };

            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(tempMessage);
                Debug.WriteLine($"[ViewModel] Temp message added. Total: {Messages.Count}");
            });

            try
            {
                var request = new SendMessageRequest
                {
                    Content = text,
                    TypeMessage = "TEXT",
                    FileName = null,
                    FileUrl = null
                };

                var response = await _chatService.SendMessageAsync(CurrentGroupId, request);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (response.Success && response.Data != null)
                    {
                        Debug.WriteLine($"[ViewModel] API success. MessageId: {response.Data.Id}");

                        var realMessage = response.Data;
                        realMessage.IsMyMessage = true;
                        realMessage.FileName = response.Data.FileName;
                        realMessage.FileUrl = response.Data.FileUrl;

                        //  Kiểm tra SignalR đã gửi tin chưa
                        var signalRMessage = Messages.FirstOrDefault(m => m.Id == realMessage.Id);

                        if (signalRMessage == null)
                        {
                            // SignalR chưa tới → Update temp
                            Debug.WriteLine($"[ViewModel] SignalR not received, updating temp");
                            var index = Messages.IndexOf(tempMessage);
                            if (index != -1) Messages[index] = realMessage;
                        }
                        else
                        {
                            // SignalR tới rồi → Xóa temp (tránh duplicate)
                            Debug.WriteLine($"[ViewModel] SignalR already received, removing temp");
                            Messages.Remove(tempMessage);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[ViewModel] API failed: {response?.Message}");
                        Messages.Remove(tempMessage);
                        var errorMsg = response?.Message ?? "Unknown Error";
                        ErrorOccurred?.Invoke(this, $"Send failed: {errorMsg}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViewModel] Send ERROR: {ex.Message}");
                Application.Current.Dispatcher.Invoke(() => Messages.Remove(tempMessage));
                ErrorOccurred?.Invoke(this, $"Send error: {ex.Message}");
            }
        }

        public async Task EditMessageAsync(Guid msgId, string newContent)
        {
            if (string.IsNullOrWhiteSpace(newContent)) return;
            try
            {
                await _signalR.UpdateMessageAsync(msgId, CurrentGroupId, newContent);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Edit failed: {ex.Message}");
            }
        }

        public async Task DeleteMessageAsync(Guid msgId)
        {
            try
            {
                await _signalR.DeleteMessageAsync(msgId, CurrentGroupId);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Delete failed: {ex.Message}");
            }
        }

        public async Task SendTypingAsync()
        {
            if (CurrentGroupId == Guid.Empty) return;
            try
            {
                await _signalR.SendTypingAsync(CurrentGroupId);
            }
            catch { }
        }

        public async Task LeaveCurrentGroupAsync()
        {
            if (CurrentGroupId != Guid.Empty)
            {
                try
                {
                    await _signalR.LeaveGroupAsync(CurrentGroupId);
                    Application.Current.Dispatcher.Invoke(() => Messages.Clear());
                    CurrentGroupId = Guid.Empty;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, $"Leave group failed: {ex.Message}");
                }
            }
        }

        //=====================================================================
        // ✅ SIGNALR EVENT HANDLERS - ĐÃ SỬA
        //=====================================================================
        private void OnGroupMessageReceived(object? sender, GroupMessageDto dto)
        {
            Debug.WriteLine("[ViewModel] ========== SIGNALR MESSAGE RECEIVED ==========");
            Debug.WriteLine($"[ViewModel] MessageId: {dto.MessageId}");
            Debug.WriteLine($"[ViewModel] GroupId: {dto.GroupId}");
            Debug.WriteLine($"[ViewModel] Username: {dto.Username}");
            Debug.WriteLine($"[ViewModel] Content: {dto.Content}");
            Debug.WriteLine($"[ViewModel] CurrentGroupId: {CurrentGroupId}");

            // ✅ BỎ QUA nếu không phải group hiện tại
            if (dto.GroupId != CurrentGroupId)
            {
                Debug.WriteLine($"[ViewModel] ⚠️ IGNORED - Wrong group");
                return;
            }

            var myUsername = CurrentUserName?.ToLower() ?? _token.GetUsername()?.ToLower();
            Debug.WriteLine($"[ViewModel] My username: {myUsername}");

            //  CONVERT DTO → ChatMessage
            var msg = new ChatMessage
            {
                Id = dto.MessageId,              
                GroupId = dto.GroupId,
                UserId = dto.UserId,
                UserName = dto.Username,        
                Content = dto.Content,
                TypeMessage = dto.TypeMessage,
                FileName = dto.FileName,
                FileUrl = dto.FileUrl,
                SendAt = dto.SendAt ?? DateTime.Now,
                IsMyMessage = dto.Username?.ToLower() == myUsername
            };

            Debug.WriteLine($"[ViewModel] IsMyMessage: {msg.IsMyMessage}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                var exists = Messages.Any(m => m.Id == msg.Id);
                Debug.WriteLine($"[ViewModel] Exists: {exists}, Count before: {Messages.Count}");

                if (!exists)
                {
                    Messages.Add(msg);
                    Debug.WriteLine($"[ViewModel]  MESSAGE ADDED! Count after: {Messages.Count}");
                }
                else
                {
                    Debug.WriteLine($"[ViewModel]  DUPLICATE - Skipped");
                }
            });

            Debug.WriteLine("[ViewModel] ==========================================");
        }

        private void OnMessageUpdated(object? sender, MessageUpdatedDto dto)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var msg = Messages.FirstOrDefault(m => m.Id == dto.MessageId);
                if (msg != null)
                {
                    msg.Content = dto.NewContent;
                    msg.IsEdited = true;
                    Debug.WriteLine($"[ViewModel] Message updated: {dto.MessageId}");
                }
            });
        }

        private void OnMessageDeleted(object? sender, MessageDeletedDto dto)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var msg = Messages.FirstOrDefault(m => m.Id == dto.MessageId);
                if (msg != null)
                {
                    Messages.Remove(msg);
                    Debug.WriteLine($"[ViewModel] Message deleted: {dto.MessageId}");
                }
            });
        }

        private void OnUserTyping(object? sender, TypingDto dto)
        {
            if (dto.UserId == CurrentUserId) return;
            Application.Current.Dispatcher.Invoke(() => TypingIndicator = $"{dto.Username} is typing...");
            Task.Delay(3000).ContinueWith(_ => Application.Current.Dispatcher.Invoke(() =>
            {
                if (TypingIndicator.StartsWith(dto.Username)) TypingIndicator = "";
            }));
        }

        private void OnConnectionChanged(object? sender, ConnectionState state)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = state == ConnectionState.Connected;
                ConnectionStatus = state.ToString();
                Debug.WriteLine($"[ViewModel] Connection state: {state}");
            });
        }

        public void Dispose()
        {
            Debug.WriteLine("[ViewModel] Disposing...");

            _signalR.GroupMessageReceived -= OnGroupMessageReceived;
            _signalR.MessageUpdated -= OnMessageUpdated;
            _signalR.MessageDeleted -= OnMessageDeleted;
            _signalR.UserTyping -= OnUserTyping;
            _signalR.ConnectionStateChanged -= OnConnectionChanged;

            if (CurrentGroupId != Guid.Empty)
                _signalR.LeaveGroupAsync(CurrentGroupId).GetAwaiter().GetResult();
        }



        // GỬI FILE - ĐÃ SỬA
        // GỬI FILE - ĐÃ SỬA HOÀN CHỈNH
        public async Task SendFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            if (CurrentGroupId == Guid.Empty)
            {
                ErrorOccurred?.Invoke(this, "Please select a group first");
                return;
            }

            Debug.WriteLine($"[ViewModel] ========== SEND FILE START ==========");
            Debug.WriteLine($"[ViewModel] File: {filePath}");

            try
            {
                // 1. Chuẩn bị file
                var fileName = Path.GetFileName(filePath);
                var fileExtension = Path.GetExtension(filePath).ToLower();

                // Validate extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".pdf", ".docx" };
                if (!allowedExtensions.Contains(fileExtension))
                {
                    Debug.WriteLine($"[ViewModel] ❌ Extension not allowed: {fileExtension}");
                    ErrorOccurred?.Invoke(this, "File type not supported!");
                    return;
                }

                var fileType = GetFileType(fileExtension);
                Debug.WriteLine($"[ViewModel] FileType: {fileType}");

                // 2. Tạo tin nhắn ảo (Optimistic UI)
                var tempId = Guid.NewGuid();
                var displayName = CurrentUserName ?? _token.GetUsername() ?? "You";

                var tempMessage = new ChatMessage
                {
                    Id = tempId,
                    GroupId = CurrentGroupId,
                    UserId = CurrentUserId,
                    UserName = displayName,
                    Content = fileName,         
                    TypeMessage = fileType,
                    FileName = fileName,
                    FileUrl = filePath,             
                    IsMyMessage = true,
                    IsSending = true,
                    SendAt = DateTime.Now
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Add(tempMessage);
                    Debug.WriteLine($"[ViewModel]  Temp message added. Total: {Messages.Count}");
                });

                // 3. Upload file lên server
                Debug.WriteLine($"[ViewModel]  Starting upload...");

                using var fileStream = File.OpenRead(filePath);
                var streamPart = new StreamPart(fileStream, fileName, GetMimeType(fileExtension));

                Debug.WriteLine($"[ViewModel]  Calling UploadFileAsync...");
                var uploadResponse = await _upload.UploadFileAsync(streamPart, fileType);

                // ✅ FIX: Dùng IsSuccessStatusCode và Content (Refit response)
                Debug.WriteLine($"[ViewModel]  Upload response - IsSuccess: {uploadResponse.Success}");

                if (!uploadResponse.Success || uploadResponse.Data == null)
                {
                    var error =  "Unknown error";
                    Debug.WriteLine($"[ViewModel]  Upload failed: {error}");

                    Application.Current.Dispatcher.Invoke(() => Messages.Remove(tempMessage));
                    ErrorOccurred?.Invoke(this, $"Upload failed: {error}");
                    return;
                }

                var fileUrl = uploadResponse.Data.FileUrl;
                Debug.WriteLine($"[ViewModel] Upload SUCCESS!");
                Debug.WriteLine($"[ViewModel] FileUrl: {fileUrl}");

                // 4. Gửi message qua API
                Debug.WriteLine($"[ViewModel]  Sending message to chat...");

                var request = new SendMessageRequest
                {
                    Content = fileName,
                    TypeMessage = fileType,
                    FileName = fileName,
                    FileUrl = fileUrl
                };

                var response = await _chatService.SendMessageAsync(CurrentGroupId, request);
                Debug.WriteLine($"[ViewModel] Message response - IsSuccess: {response.Success}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (response.Success && response.Data != null)
                    {
                        Debug.WriteLine($"[ViewModel]  Message sent! MessageId: {response.Data.Id}");

                        var realMessage = response.Data;  
                        realMessage.IsMyMessage = true;
                        realMessage.FileUrl = fileUrl;
                        realMessage.FileName = fileName;
                        realMessage.Content = fileName;

                        Debug.WriteLine($"[ViewModel] RealMessage - FileUrl: '{realMessage.FileUrl}'");
                        Debug.WriteLine($"[ViewModel] RealMessage - FileName: '{realMessage.FileName}'");

                        // Kiểm tra SignalR đã gửi tin chưa
                        var signalRMessage = Messages.FirstOrDefault(m => m.Id == realMessage.Id);

                        if (signalRMessage == null)
                        {
                            // FIX: Remove và Insert để trigger CollectionChanged 
                            Debug.WriteLine($"[ViewModel] SignalR not received yet, replacing temp");
                            var index = Messages.IndexOf(tempMessage);
                            if (index != -1)
                            {
                                Messages.RemoveAt(index);           // Trigger Remove event
                                Messages.Insert(index, realMessage); // Trigger Add event
                                Debug.WriteLine($"[ViewModel]  Temp replaced! UI will refresh now");
                            }
                        }
                        else
                        {
                            // SignalR tới rồi → Xóa temp
                            Debug.WriteLine($"[ViewModel] SignalR already received, removing temp");
                            Messages.Remove(tempMessage);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[ViewModel]  Message send failed");
                        Messages.Remove(tempMessage);
                        ErrorOccurred?.Invoke(this, "Send message failed");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViewModel]  EXCEPTION: {ex.Message}");
                Debug.WriteLine($"[ViewModel] Stack: {ex.StackTrace}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var tempMsg = Messages.FirstOrDefault(m => m.IsSending && m.FileName == Path.GetFileName(filePath));
                    if (tempMsg != null)
                    {
                        Messages.Remove(tempMsg);
                        Debug.WriteLine($"[ViewModel] Temp message removed due to exception");
                    }
                });

                ErrorOccurred?.Invoke(this, $"Error: {ex.Message}");
            }
            finally
            {
                Debug.WriteLine($"[ViewModel] ========== SEND FILE END ==========");
            }
        }

        // Helper methods (GIỮ NGUYÊN)
        private string GetFileType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" => "IMAGE",
                ".mp4" => "VIDEO",
                ".pdf" or ".docx" => "DOCUMENT",
                _ => throw new NotSupportedException($"File type {extension} is not supported")
            };
        }

        private string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".mp4" => "video/mp4",
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }
    }
}