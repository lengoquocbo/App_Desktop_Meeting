using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json.Linq;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Models;
using Online_Meeting.Client.Services;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Services
{
    public class ChatSignalRService
    {
        private HubConnection _connection;
        private readonly ITokenService _tokenService;

        public event EventHandler<GroupMessageDto>? GroupMessageReceived;
        public event EventHandler<MessageUpdatedDto>? MessageUpdated;
        public event EventHandler<MessageDeletedDto>? MessageDeleted;
        public event EventHandler<TypingDto>? UserTyping;
        public event EventHandler<ConnectionState>? ConnectionStateChanged;

        public ChatSignalRService(TokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public async Task ConnectAsync()
        {
            try
            {
                Debug.WriteLine("[SignalR] ========== CONNECTING ==========");

                var token = _tokenService.GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    Debug.WriteLine("[SignalR] ❌ NO TOKEN FOUND!");
                    throw new Exception("No access token available");
                }

                Debug.WriteLine($"[SignalR] ✅ Token: {token.Substring(0, 20)}...");

                string cleanUrl = AppConfig.ChatBaseUrl?.Trim();
                if (string.IsNullOrEmpty(cleanUrl)) throw new Exception("ChatBaseUrl is empty");

                Debug.WriteLine($"[SignalR] Target URL: '{cleanUrl}'");

                _connection = new HubConnectionBuilder()
                    .WithUrl(cleanUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(token);

                        // Header Ngrok (Bạn đã làm đúng)
                        options.Headers["ngrok-skip-browser-warning"] = "true";

                        // 2. ÉP DÙNG WEBSOCKETS (Quan trọng với Ngrok)
                        // Giúp kết nối ổn định hơn, tránh bị ngrok chặn các request HTTP polling
                        options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;

                        // Bỏ qua kiểm tra chứng chỉ SSL (nếu cần thiết với server dev)
                        options.HttpMessageHandlerFactory = (handler) =>
                        {
                            if (handler is HttpClientHandler clientHandler)
                            {
                                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                            }
                            return handler;
                        };
                    })
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) })
                    .Build();

                // ✅ EVENT: ReceiveGroupMessage
                _connection.On<object>("ReceiveGroupMessage", (data) =>
                {
                    Debug.WriteLine("[SignalR] 🔔 ReceiveGroupMessage EVENT FIRED!");

                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(data);
                        Debug.WriteLine($"[SignalR] Raw Data: {json}");

                        // ✅ THÊM OPTIONS
                        var options = new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true // ← KEY POINT!
                        };

                        var dto = System.Text.Json.JsonSerializer.Deserialize<GroupMessageDto>(json, options);   


                        GroupMessageReceived?.Invoke(this, dto);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SignalR] ❌ Parse error: {ex.Message}");
                        Debug.WriteLine($"[SignalR] Stack: {ex.StackTrace}");
                    }
                });

                // ✅ EVENT: MessageUpdated
                _connection.On<object>("MessageUpdated", (data) =>
                {
                    Debug.WriteLine("[SignalR] 🔔 MessageUpdated EVENT FIRED!");
                    var json = System.Text.Json.JsonSerializer.Serialize(data);
                    var dto = System.Text.Json.JsonSerializer.Deserialize<MessageUpdatedDto>(json);
                    MessageUpdated?.Invoke(this, dto);
                });

                // ✅ EVENT: MessageDeleted
                _connection.On<object>("MessageDeleted", (data) =>
                {
                    Debug.WriteLine("[SignalR] 🔔 MessageDeleted EVENT FIRED!");
                    var json = System.Text.Json.JsonSerializer.Serialize(data);
                    var dto = System.Text.Json.JsonSerializer.Deserialize<MessageDeletedDto>(json);
                    MessageDeleted?.Invoke(this, dto);
                });

                // ✅ EVENT: UserTyping
                _connection.On<object>("UserTyping", (data) =>
                {
                    Debug.WriteLine("[SignalR] 🔔 UserTyping EVENT FIRED!");
                    var json = System.Text.Json.JsonSerializer.Serialize(data);
                    var dto = System.Text.Json.JsonSerializer.Deserialize<TypingDto>(json);
                    UserTyping?.Invoke(this, dto);
                });

                // ✅ EVENT: Error từ server
                _connection.On<string>("Error", (errorMessage) =>
                {
                    Debug.WriteLine($"[SignalR] ❌ Server Error: {errorMessage}");
                });

                // ✅ Connection lifecycle events
                _connection.Closed += async (error) =>
                {
                    Debug.WriteLine($"[SignalR] ❌ Connection CLOSED: {error?.Message ?? "No error"}");
                    ConnectionStateChanged?.Invoke(this, ConnectionState.Disconnected);
                    await Task.Delay(5000);
                };

                _connection.Reconnecting += (error) =>
                {
                    Debug.WriteLine($"[SignalR] 🔄 Reconnecting... {error?.Message ?? ""}");
                    ConnectionStateChanged?.Invoke(this, ConnectionState.Reconnecting);
                    return Task.CompletedTask;
                };

                _connection.Reconnected += (connectionId) =>
                {
                    Debug.WriteLine($"[SignalR] ✅ Reconnected! ConnectionId: {connectionId}");
                    ConnectionStateChanged?.Invoke(this, ConnectionState.Connected);
                    return Task.CompletedTask;
                };

                // Start connection
                await _connection.StartAsync();

                Debug.WriteLine($"[SignalR] ✅ CONNECTED! State: {_connection.State}");
                Debug.WriteLine($"[SignalR] ConnectionId: {_connection.ConnectionId}");

                ConnectionStateChanged?.Invoke(this, ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR] ❌ CONNECT FAILED: {ex.Message}");
                Debug.WriteLine($"[SignalR] Stack: {ex.StackTrace}");
                throw;
            }
        }

        public async Task JoinGroupAsync(Guid groupId)
        {
            try
            {
                Debug.WriteLine($"[SignalR] >>> Calling JoinGroup({groupId})");

                if (_connection?.State != HubConnectionState.Connected)
                {
                    Debug.WriteLine($"[SignalR] ❌ Cannot join group - Connection state: {_connection?.State}");
                    throw new Exception($"SignalR not connected. State: {_connection?.State}");
                }

                await _connection.InvokeAsync("JoinGroup", groupId);
                Debug.WriteLine($"[SignalR] ✅ JoinGroup SUCCESS for {groupId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR] ❌ JoinGroup FAILED: {ex.Message}");
                throw;
            }
        }

        public async Task LeaveGroupAsync(Guid groupId)
        {
            try
            {
                Debug.WriteLine($"[SignalR] >>> Calling LeaveGroup({groupId})");

                if (_connection?.State == HubConnectionState.Connected)
                {
                    await _connection.InvokeAsync("LeaveGroup", groupId);
                    Debug.WriteLine($"[SignalR] ✅ LeaveGroup SUCCESS for {groupId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR] ❌ LeaveGroup FAILED: {ex.Message}");
            }
        }

        public async Task SendMessageAsync(Guid groupId, string message, string typeMessage = "TEXT")
        {
            try
            {
                Debug.WriteLine($"[SignalR] >>> Calling SendGroupMessage");
                Debug.WriteLine($"  - GroupId: {groupId}");
                Debug.WriteLine($"  - Message: {message}");
                Debug.WriteLine($"  - Type: {typeMessage}");

                if (_connection?.State != HubConnectionState.Connected)
                {
                    Debug.WriteLine($"[SignalR] ❌ Cannot send - Connection state: {_connection?.State}");
                    throw new Exception($"SignalR not connected. State: {_connection?.State}");
                }

                await _connection.InvokeAsync("SendGroupMessage", groupId, message, typeMessage);
                Debug.WriteLine($"[SignalR] ✅ SendGroupMessage SUCCESS");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR] ❌ SendGroupMessage FAILED: {ex.Message}");
                throw;
            }
        }

        public async Task SendTypingAsync(Guid groupId)
        {
            try
            {
                if (_connection?.State == HubConnectionState.Connected)
                {
                    await _connection.InvokeAsync("UserTyping", groupId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR] ❌ UserTyping FAILED: {ex.Message}");
            }
        }

        public async Task UpdateMessageAsync(Guid messageId, Guid groupId, string newContent)
        {
            try
            {
                Debug.WriteLine($"[SignalR] >>> UpdateMessage({messageId})");
                await _connection.InvokeAsync("UpdateMessage", messageId, groupId, newContent);
                Debug.WriteLine($"[SignalR] ✅ UpdateMessage SUCCESS");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR] ❌ UpdateMessage FAILED: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteMessageAsync(Guid messageId, Guid groupId)
        {
            try
            {
                Debug.WriteLine($"[SignalR] >>> DeleteMessage({messageId})");
                await _connection.InvokeAsync("DeleteMessage", messageId, groupId);
                Debug.WriteLine($"[SignalR] ✅ DeleteMessage SUCCESS");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR] ❌ DeleteMessage FAILED: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                Debug.WriteLine("[SignalR] >>> Disconnecting...");
                if (_connection != null)
                {
                    await _connection.StopAsync();
                    await _connection.DisposeAsync();
                    Debug.WriteLine("[SignalR] ✅ Disconnected");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR] ❌ Disconnect error: {ex.Message}");
            }
        }
    }

    // ============================================
    // 2. DTOs - Đảm bảo khớp với Server
    // ============================================
    public class GroupMessageDto
    {
        public Guid MessageId { get; set; }
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public string TypeMessage { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public DateTime? SendAt { get; set; }
    }

    public class MessageUpdatedDto
    {
        public Guid MessageId { get; set; }
        public Guid GroupId { get; set; }
        public string NewContent { get; set; }
        public Guid UpdatedBy { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    public class MessageDeletedDto
    {
        public Guid MessageId { get; set; }
        public Guid GroupId { get; set; }
        public Guid DeletedBy { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    public class TypingDto
    {
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting
    }
}
