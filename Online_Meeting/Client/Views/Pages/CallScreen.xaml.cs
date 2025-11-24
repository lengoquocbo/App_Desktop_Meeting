using LanguageExt;
using Microsoft.Web.WebView2.Core;
using Online_Meeting.Client.Dtos.MeetingSignalRDto;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Models;
using Online_Meeting.Client.Services;
using Online_Meeting.Share.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Xml;
using static vpxmd.VpxCodecCxPkt;

namespace Online_Meeting.Client.Views
{
    public partial class CallScreen : Window
    {
        // Virtual host for mapping local folder -> secure origin
        private const string VirtualHost = "appassets.example";

        private bool _isCleanExit = false; // Cờ đánh dấu đã dọn dẹp xong
        private bool _isEndedByHost = false; // cờ đánh dấu là được tắt bởi host
        private readonly bool _isWaiting;
        private readonly string _roomName;
        private readonly Guid _roomId;
        private readonly string _roomKey;
        private readonly string _roomUrl;
        private readonly Guid _userId;
        private readonly string _userName;
        private readonly bool _isHost;
        private readonly string _camId;
        private readonly string _micId;
        private readonly bool _audioEnabled;
        private readonly bool _videoEnabled;
        private readonly MeetingService _meetingService;
        private readonly MeetingSignalRServices _meetingSignalRServices;


        // Event: typed message received from JS (deserialized)
        public event EventHandler<WebMsg>? MessageReceived;

        public CallScreen(
            bool isWaiting,
            string roomName,
            Guid roomId,
            string roomKey,
            string roomUrl,
            Guid userId,
            string userName,
            bool isHost,
            string camId,
            string micId,
            bool audioEnabled,
            bool videoEnabled,
            MeetingService meetingService,
            MeetingSignalRServices meetingSignalRServices
        )
        {
            InitializeComponent();

            _isWaiting = isWaiting; 
            _roomName = roomName;
            _roomId = roomId;
            _roomKey = roomKey;
            _roomUrl = roomUrl;
            _userId = userId;
            _userName = userName;
            _isHost = isHost;
            _camId = camId;
            _micId = micId;
            _audioEnabled = audioEnabled;
            _videoEnabled = videoEnabled;
            _meetingService = meetingService;
            _meetingSignalRServices = meetingSignalRServices;

            Loaded += CallScreen_Loaded;

            _meetingSignalRServices.OnYouAreRejected += SignalR_OnYouAreRejected;
            _meetingSignalRServices.OnGuestRequested += SignalR_OnGuestRequested;

            // Subscribe to participants updates from SignalR
            _meetingService.OnParticipantsUpdated += MeetingService_OnParticipantsUpdated;
            _meetingService.OnUserJoined += MeetingService_OnUserJoined;
            _meetingService.OnUserLeft += MeetingService_OnUserLeft;

            // Subscribe to WebRTC signaling events
            _meetingSignalRServices.OnOfferReceived += MeetingSignalRServices_OnOfferReceived;
            _meetingSignalRServices.OnAnswerReceived += MeetingSignalRServices_OnAnswerReceived;
            _meetingSignalRServices.OnIceCandidateReceived += MeetingSignalRServices_OnIceCandidateReceived;
            _meetingSignalRServices.OnMicrophoneToggled += SignalR_OnMicrophoneToggled;
            _meetingSignalRServices.OnCameraToggled += SignalR_OnCameraToggled;
            _meetingSignalRServices.OnScreenShareToggled += SignalR_OnShareSceenToggled;
            _meetingSignalRServices.OnChatMessageReceived += SignalR_OnChatMessage;
            this.Closing += CallScreen_Closing;
            _meetingSignalRServices.OnMeetingEnded += SignalR_OnMeetingEnded;
        }

        private async void CallScreen_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {

                await Web.EnsureCoreWebView2Async();

                // Map folder -> virtual host (must be done before navigate)
                string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
                string webFolder = Path.Combine(exeFolder, "Client", "Views", "CallScreenView");

                try
                {
                    Web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        VirtualHost,
                        webFolder,
                        CoreWebView2HostResourceAccessKind.Allow);
                    System.Diagnostics.Debug.WriteLine($"Mapped {VirtualHost} -> {webFolder}");
                }
                catch (Exception mapEx)
                {
                    System.Diagnostics.Debug.WriteLine($"SetVirtualHostNameToFolderMapping failed: {mapEx.Message}");
                    // continue anyway; mapping failure will likely make media devices unavailable
                }

                // permission handler
                Web.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;

                // message from JS
                Web.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                // navigation completed
                Web.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

                // Navigate to secure virtual host URL (index.html inside mapped folder)
                var url = $"https://{VirtualHost}/index.html";
                Web.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 init error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed: {e.WebErrorStatus}");
                MessageBox.Show($"Navigation failed: {e.WebErrorStatus}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Navigation completed: {Web.Source}");

            var currentParticipants = _meetingService.CurrentParticipants.Select(p => new
            {
                userId = p.UserId.ToString(), // Map đúng tên biến cho JS
                username = p.Username,
                connectionId = p.ConnectionId,
                micEnable = p.micEnable,
                camEnable = p.camEnable
            }).ToArray();

            System.Diagnostics.Debug.WriteLine($"[Init] Found {currentParticipants.Length} participants in Service.");

            // After navigation, send initial settings from constructor to the page
            try
            {
                var initPayload = new
                {
                    type = "init-call",
                    roomId = _roomId.ToString(),
                    roomName = _roomName,
                    roomKey = _roomKey,
                    roomUrl = _roomUrl,
                    userId = _userId.ToString(), // Important: match LOCAL_ID in JavaScript
                    userName = _userName,
                    isHost = _isHost,
                    cameraId = _camId,
                    micId = _micId,
                    audioEnabled = _audioEnabled,
                    videoEnabled = _videoEnabled,
                    participants = currentParticipants,
                    isWaiting = _isWaiting
                };
                var json = JsonSerializer.Serialize(initPayload);
                Web.CoreWebView2.PostWebMessageAsJson(json);
                System.Diagnostics.Debug.WriteLine($"Sent init-call: {json}");

                // After a short delay, request current participants list from MeetingService
                // This ensures that participants who joined are properly loaded
                if (!_isWaiting)
                {
                    System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            SendCurrentParticipantsToWebView();
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to send init-settings: " + ex.Message);
            }
        }

        // Allow camera/microphone for our mapped origin
        private void CoreWebView2_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PermissionRequested: {e.PermissionKind} from {e.Uri}");

                if ((e.PermissionKind == CoreWebView2PermissionKind.Camera ||
                     e.PermissionKind == CoreWebView2PermissionKind.Microphone) &&
                    (e.Uri?.Contains(VirtualHost, StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    e.State = CoreWebView2PermissionState.Allow;
                    System.Diagnostics.Debug.WriteLine($"Permission allowed for {e.PermissionKind} at {e.Uri}");
                }
                else
                {
                    e.State = CoreWebView2PermissionState.Default;
                    System.Diagnostics.Debug.WriteLine($"Permission default for {e.PermissionKind} at {e.Uri}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PermissionRequested handler error: " + ex.Message);
            }
        }

        // Central handler for raw WebMessageReceived from WebView2
        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json;
                try
                {
                    // Prefer JSON string representation if available
                    json = e.TryGetWebMessageAsString() ?? e.WebMessageAsJson;
                }
                catch
                {
                    json = e.WebMessageAsJson;
                }

                System.Diagnostics.Debug.WriteLine("WebMessageReceived: " + json);

                WebMsg? message = null;
                try
                {
                    message = JsonSerializer.Deserialize<WebMsg>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to deserialize WebMsg: " + ex.Message);
                }

                // If we have a typed message, raise typed event for listeners
                if (message != null)
                {
                    OnMessageReceived(message);
                }
                else
                {
                    // If not able to deserialize, still notify subscribers with raw JSON if needed
                    MessageReceived?.Invoke(this, new WebMsg { type = "raw", msg = json });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CoreWebView2_WebMessageReceived error: " + ex.Message);
            }
        }


        // Raise typed event for consumers
        private void OnMessageReceived(WebMsg msg)
        {
            try
            {
                MessageReceived?.Invoke(this, msg);

                // Optionally handle some message types directly here:
                switch (msg.type)
                {
                    case "toggleCamera":
                        _ = HandleToogleCamera(msg);
                        break;

                    case "toggleMic":
                        _ = HandleToogleMicrophone(msg);
                        break;

                    case "ToggleScreenShare":
                        _ = HandleShareScreen(msg);
                        break;

                    case "send-chat":
                        _ = HandleChatMessageReceived(msg);
                        break;

                    case "admit-guest":
                        _ = HandleAdminGuest(msg);
                        break;

                    case "reject-guest":
                        _ = HandleRejectGuest(msg);
                        break;

                    case "close-window":
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.Close();
                        });
                        break;

                    case "end-call":
                        _ = HandleEndCall();
                        break;

                    case "send-offer":
                        _ = HandleSendOffer(msg);
                        break;

                    case "send-answer":
                        _ = HandleSendAnswer(msg);
                        break;

                    case "send-ice-candidate":
                        _ = HandleSendIceCandidate(msg);
                        break;

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OnMessageReceived handler error: " + ex.Message);
            }
        }

        // UI actions from WPF -> send commands to JS
        private async void SignalR_OnCameraToggled(MediaToggleData data)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Web?.CoreWebView2 != null)
                    {
                        var payload = new
                        {
                            type = "remote-cam-toggled",
                            id = data.UserId.ToString(),
                            videoEnabled = data.IsEnabled
                        };
                        var json = JsonSerializer.Serialize(payload);
                        Web.CoreWebView2.PostWebMessageAsJson(json);
                        System.Diagnostics.Debug.WriteLine($"Sent camera-toggled: {json}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending camera-toggled: {ex.Message}");
                }
            });
        }

        private async void SignalR_OnMicrophoneToggled(MediaToggleData data)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Web?.CoreWebView2 != null)
                    {
                        var payload = new
                        {
                            type = "remote-mic-toggled",
                            id = data.UserId.ToString(),
                            audioEnabled = data.IsEnabled
                        };
                        var json = JsonSerializer.Serialize(payload);
                        Web.CoreWebView2.PostWebMessageAsJson(json);
                        System.Diagnostics.Debug.WriteLine($"Sent microphone-toggled: {json}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending microphone-toggled: {ex.Message}");
                }
            });
        }

        private async void SignalR_OnShareSceenToggled(MediaToggleData data)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Web?.CoreWebView2 != null)
                    {
                        var payload = new
                        {
                            type = "remote-screen-sharing-toggled",
                            id = data.UserId.ToString(),
                            name = data.Username,
                            isScreenSharing = data.IsEnabled
                        };
                        var json = JsonSerializer.Serialize(payload);
                        Web.CoreWebView2.PostWebMessageAsJson(json);
                        System.Diagnostics.Debug.WriteLine($"Sent share screen-toggled: {json}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending share screen-toggled: {ex.Message}");
                }
            });
        }

        private async void SignalR_OnChatMessage(ChatMessageData data)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Web?.CoreWebView2 != null)
                    {
                        var payload = new
                        {
                            type = "chat-message", // Type để JS nhận biết
                            id = data.UserId.ToString(),
                            username = data.Username,
                            content = data.Content,
                            // Format giờ phút cho đẹp (VD: 14:30)
                            timestamp = data.Timestamp.ToLocalTime().ToString("HH:mm")
                        };

                        var json = JsonSerializer.Serialize(payload);
                        Web.CoreWebView2.PostWebMessageAsJson(json);
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending Chat message: {e.Message}");
                }

            });
        }

        private async void SignalR_OnYouAreRejected()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Web?.CoreWebView2 != null)
                    {
                        var payload = new
                        {
                            type = "you-are-rejected", // Type để JS nhận biết
                        };

                        var json = JsonSerializer.Serialize(payload);
                        Web.CoreWebView2.PostWebMessageAsJson(json);
                        System.Diagnostics.Debug.WriteLine($"You are rejected");


                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending Chat message: {e.Message}");
                }

            });
        }

        private async void SignalR_OnGuestRequested(UserJoinData data)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Web?.CoreWebView2 != null)
                    {
                        var payload = new
                        {
                            type = "guest-requested",
                            name = data.Username,
                            id = data.UserId,
                            connectionId = data.ConnectionId
                        };

                        var json = JsonSerializer.Serialize(payload);
                        Web.CoreWebView2.PostWebMessageAsJson(json);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Guest requested: {json}");
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending Chat message: {e.Message}");
                }

            });
        }

        private async void CallScreen_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Nếu đã dọn dẹp xong thì cho đóng luôn, không chặn nữa
            if (_isCleanExit) return;

            if (!_isEndedByHost)
            {
                // Chặn đóng để hỏi ý kiến
                e.Cancel = true;

                if (_isHost)
                {
                    var result = MessageBox.Show(
                        "Bạn là người chủ trì. Nếu bạn thoát, cuộc họp sẽ kết thúc với tất cả mọi người.\nBạn có chắc chắn muốn thoát không?",
                        "Xác nhận kết thúc",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes) return; // User hủy đóng -> Ở lại
                }
                else
                {
                    var result = MessageBox.Show(
                        "Bạn có muốn rời cuộc họp không?",
                        "Xác nhận",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return; // User hủy đóng -> Ở lại
                }
            }
            else
            {
                // Nếu _isEndedByHost == true, ta cũng phải set e.Cancel = true 
                // để code chạy xuống phần clean up (async) bên dưới, 
                // sau đó Dispatcher sẽ gọi Close() lần cuối.
                e.Cancel = true;
            }

            // --- BẮT ĐẦU QUY TRÌNH DỌN DẸP ---

            // Ẩn cửa sổ đi cho người dùng đỡ phải chờ
            this.Visibility = Visibility.Hidden;

            try
            {
                if (_meetingSignalRServices.Isconnected)
                {
                    // Chủ động gửi lệnh Leave/End để Server xử lý ngay lập tức
                    // (Nhanh hơn đợi Server tự phát hiện disconnect)
                    await _meetingSignalRServices.LeaveRoomAsync(_roomId);

                    // Ngắt kết nối SignalR an toàn
                    await _meetingSignalRServices.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during closing cleanup: {ex.Message}");
            }
            finally
            {
                // Dọn dẹp WebView
                Web?.Dispose();

                // Đánh dấu là đã xong, lần sau hàm này chạy sẽ không bị chặn bởi e.Cancel = true nữa
                _isCleanExit = true;

                // Đóng cửa sổ thật sự
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.Close();
                }));
            }
        }

        private void SignalR_OnMeetingEnded(string reason)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(reason, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                // Đóng cửa sổ, quay về màn hình chính
                _isEndedByHost = true;

                this.Close();
            });
        }

        // Handlers for toggling media from JS -> SignalR
        private async Task HandleToogleMicrophone(WebMsg msg)
        {
            var micEnable = msg.audio;
            System.Diagnostics.Debug.WriteLine($"HandleToogleMicrophone: micEnable={micEnable}");
            await _meetingSignalRServices.ToggleMicrophoneAsync(micEnable);
        }

        private async Task HandleToogleCamera(WebMsg msg)
        {
            var camEnable = msg.video;
            System.Diagnostics.Debug.WriteLine($"HandleToogleCamera: camEnable={camEnable}");
            await _meetingSignalRServices.ToggleCameraAsync(camEnable);
        }

        private async Task HandleShareScreen(WebMsg msg)
        {
            var shareScreenEnable = msg.isSharingScreen;
            System.Diagnostics.Debug.WriteLine($"Handle Toggle Share Screen: isSharing={shareScreenEnable}");
            await _meetingSignalRServices.ToggleScreenShareAsync(shareScreenEnable);
        }

        public async Task HandleChatMessageReceived(WebMsg msg)
        {
            var content = msg.content;
            System.Diagnostics.Debug.WriteLine($"HandleChatMessageReceived: content = {content}");
            await _meetingSignalRServices.SendChatMessageAsync(content);
        }

        private async Task HandleAdminGuest(WebMsg msg)
        {
            var toConnectionId = msg.toConnectionId;
            System.Diagnostics.Debug.WriteLine($"HandleAdmitGuest: to connection id: {toConnectionId}");
            await _meetingSignalRServices.AdmitUserAsync(toConnectionId);
        }

        private async Task HandleRejectGuest(WebMsg msg)
        {
            var toConnectionId = msg.toConnectionId;
            System.Diagnostics.Debug.WriteLine($"HandleRejectGuest to connection id: {toConnectionId}");
            await _meetingSignalRServices.RejectUserAsync(toConnectionId);
        }

        private async Task HandleEndCall()
        {
            // 1. Gửi lệnh rời phòng lên Server
            await _meetingSignalRServices.LeaveRoomAsync(_roomId);

            // 2. Ngắt kết nối SignalR
            await _meetingSignalRServices.DisconnectAsync();

            // 3. Đóng cửa sổ trên UI Thread
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.Close();
            });
        }

        // Handler for participants updates from SignalR
        private void MeetingService_OnParticipantsUpdated(List<ParticipantInfo> participants)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SendParticipantsToWebView(participants);
            });
        }
        // Handler for user joined event
        private void MeetingService_OnUserJoined(UserJoinData data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (Web?.CoreWebView2 != null)
                    {
                        var payload = new
                        {
                            type = "participant-joined",
                            participant = new
                            {
                                id = data.UserId.ToString(),
                                name = data.Username,
                                connectionId = data.ConnectionId,
                                camEnable = data.camEnable,
                                micEnable = data.micEnable
                            }
                        };
                        var json = JsonSerializer.Serialize(payload);
                        Web.CoreWebView2.PostWebMessageAsJson(json);
                        System.Diagnostics.Debug.WriteLine($"Sent participant-joined: {json}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending participant-joined: {ex.Message}");
                }
            });
        }

        // Handler for user left event
        private void MeetingService_OnUserLeft(UserLeftData data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (Web?.CoreWebView2 != null)
                    {
                        var payload = new
                        {
                            type = "participant-left",
                            id = data.UserId.ToString()
                        };
                        var json = JsonSerializer.Serialize(payload);
                        Web.CoreWebView2.PostWebMessageAsJson(json);
                        System.Diagnostics.Debug.WriteLine($"Sent participant-left: {json}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending participant-left: {ex.Message}");
                }
            });
        }

        // Helper method to send current participants from MeetingService
        private void SendCurrentParticipantsToWebView()
        {
            if (_isWaiting) return;
            try
            {
                // Get current participants from MeetingService
                var participants = _meetingService.CurrentParticipants.ToList();
                System.Diagnostics.Debug.WriteLine($"Sending current participants: {participants.Count}");
                SendParticipantsToWebView(participants);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error requesting participants: {ex.Message}");
            }
        }



        // Send participants list to WebView2
        private void SendParticipantsToWebView(List<ParticipantInfo> participants)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CallScreen] SendParticipantsToWebView called with {participants.Count} participants");
                if (Web?.CoreWebView2 != null)
                {
                    // Map participants to a format JavaScript expects
                    var participantsData = participants.Select(p => new
                    {
                        userId = p.UserId.ToString(),
                        username = p.Username,
                        connectionId = p.ConnectionId,
                        camEnable = p.camEnable, 
                        micEnable = p.micEnable    
                    }).ToArray();

                    // ⭐ THÊM LOG NÀY
                    System.Diagnostics.Debug.WriteLine($"[CallScreen] Mapped participants:");
                    foreach (var p in participantsData)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {p.username} (userId: {p.userId}, connId: {p.connectionId})");
                    }

                    var payload = new
                    {
                        type = "participants-update",
                        participants = participantsData
                    };

                    var json = JsonSerializer.Serialize(payload);
                    Web.CoreWebView2.PostWebMessageAsJson(json);
                    System.Diagnostics.Debug.WriteLine($"Sent participants-update: {json}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending participants update: {ex.Message}");
            }
        }

        // ===== WebRTC SIGNALING HANDLERS =====

        // Handle outgoing offer from JavaScript (send via SignalR)
        private async Task HandleSendOffer(WebMsg msg)
        {
            try
            {
                var toConnectionId = msg.toConnectionId;
                var offer = msg.offer;

                if (string.IsNullOrEmpty(toConnectionId) || offer == null)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid send-offer message: missing toConnectionId or offer");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Sending offer to {toConnectionId}");
                await _meetingSignalRServices.SendOfferAsync(toConnectionId, offer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending offer: {ex.Message}");
            }
        }

        // Handle outgoing answer from JavaScript (send via SignalR)
        private async Task HandleSendAnswer(WebMsg msg)
        {
            try
            {
                var toConnectionId = msg.toConnectionId;
                var answer = msg.answer;

                if (string.IsNullOrEmpty(toConnectionId) || answer == null)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid send-answer message: missing toConnectionId or answer");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Sending answer to {toConnectionId}");
                await _meetingSignalRServices.SendAnswerAsync(toConnectionId, answer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending answer: {ex.Message}");
            }
        }

        // Handle outgoing ICE candidate from JavaScript (send via SignalR)
        private async Task HandleSendIceCandidate(WebMsg msg)
        {
            try
            {
                var toConnectionId = msg.toConnectionId;
                var candidate = msg.candidate;

                if (string.IsNullOrEmpty(toConnectionId) || candidate == null)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid send-ice-candidate message: missing toConnectionId or candidate");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Sending ICE candidate to {toConnectionId}");
                await _meetingSignalRServices.SendIceCandidateAsync(toConnectionId, candidate);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending ICE candidate: {ex.Message}");
            }
        }

        // Handle incoming offer from SignalR (forward to JavaScript)
        private void MeetingSignalRServices_OnOfferReceived(OfferData data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (Web?.CoreWebView2 != null)
                    {
                        var payload = new
                        {
                            type = "receive-offer",
                            fromConnectionId = data.FromConnectionId,
                            fromUserId = data.FromUserId.ToString(),
                            fromUsername = data.FromUsername,
                            offer = data.Offer
                        };
                        var json = JsonSerializer.Serialize(payload);
                        Web.CoreWebView2.PostWebMessageAsJson(json);
                        System.Diagnostics.Debug.WriteLine($"Forwarded offer from {data.FromUsername}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error forwarding offer: {ex.Message}");
                }
            });
        }

        // Handle incoming answer from SignalR (forward to JavaScript)
        private void MeetingSignalRServices_OnAnswerReceived(AnswerData data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (Web?.CoreWebView2 != null)
                    {
                        var payload = new
                        {
                            type = "receive-answer",
                            fromConnectionId = data.FromConnectionId,
                            fromUserId = data.FromUserId.ToString(),
                            fromUsername = data.FromUsername,
                            answer = data.Answer
                        };
                        var json = JsonSerializer.Serialize(payload);
                        Web.CoreWebView2.PostWebMessageAsJson(json);
                        System.Diagnostics.Debug.WriteLine($"Forwarded answer from {data.FromUsername}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error forwarding answer: {ex.Message}");
                }
            });
        }

        // Handle incoming ICE candidate from SignalR (forward to JavaScript)
        private void MeetingSignalRServices_OnIceCandidateReceived(IceCandidateData data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (Web?.CoreWebView2 != null)
                    {
                        var payload = new
                        {
                            type = "receive-ice-candidate",
                            fromConnectionId = data.FromConnectionId,
                            candidate = data.Candidate
                        };
                        var json = JsonSerializer.Serialize(payload);
                        Web.CoreWebView2.PostWebMessageAsJson(json);
                        System.Diagnostics.Debug.WriteLine($"Forwarded ICE candidate from {data.FromConnectionId}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error forwarding ICE candidate: {ex.Message}");
                }
            });
        }

        // Cleanup subscriptions
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            try
            {
                // Unsubscribe from meeting service events
                if (_meetingService != null)
                {
                    _meetingService.OnParticipantsUpdated -= MeetingService_OnParticipantsUpdated;
                    _meetingService.OnUserJoined -= MeetingService_OnUserJoined;
                    _meetingService.OnUserLeft -= MeetingService_OnUserLeft;
                }

                // Unsubscribe from WebRTC signaling events
                if (_meetingSignalRServices != null)
                {
                    _meetingSignalRServices.OnOfferReceived -= MeetingSignalRServices_OnOfferReceived;
                    _meetingSignalRServices.OnAnswerReceived -= MeetingSignalRServices_OnAnswerReceived;
                    _meetingSignalRServices.OnIceCandidateReceived -= MeetingSignalRServices_OnIceCandidateReceived;
                }

                if (Web?.CoreWebView2 != null)
                {
                    try { Web.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived; } catch { }
                    try { Web.CoreWebView2.PermissionRequested -= CoreWebView2_PermissionRequested; } catch { }
                    try { Web.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted; } catch { }
                }
            }
            catch { /* swallow */ }
        }
    }
}
