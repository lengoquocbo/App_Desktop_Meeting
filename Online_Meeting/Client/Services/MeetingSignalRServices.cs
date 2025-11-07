using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Online_Meeting.Client.Dtos.MeetingSignalRDto;
using System.Windows;

namespace Online_Meeting.Client.Services
{
    internal class MeetingSignalRServices
    {
        private HubConnection _hubConnection;
        private readonly TokenService _tokenService;

        public event Action<ExistingParticipantData> OnExistingParticipant;
        public event Action<UserJoinData> OnUserJoined;
        public event Action<UserLeftData> OnUserLeft;
        public event Action<OfferData> OnOfferReceived;
        public event Action<AnswerData> OnAnswerReceived;
        public event Action<IceCandidateData> OnIceCandidateReceived;
        public event Action<MediaToggleData> OnCameraToggled;
        public event Action<MediaToggleData> OnMicrophoneToggled;
        public event Action<MediaToggleData> OnScreenShareToggled;
        public event Action<string> OnError;

        public bool Isconnected => _hubConnection?.State == HubConnectionState.Connected;

        public MeetingSignalRServices(TokenService tokenService)
        {
            _tokenService = tokenService;
        }

        //================CONNECTION================
        public async Task ConnectAsync()
        {
            var token = _tokenService.GetAccessToken();
            if (String.IsNullOrEmpty(token)) throw new InvalidOperationException("Token is required");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(AppConfig.MeetingBaseUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token);
                })
                .WithAutomaticReconnect()
                .Build();

            SetUpHandler();

            await _hubConnection.StartAsync();
            Console.WriteLine("Connected to MeetingHub");
        }

        private void SetUpHandler()
        {
            // Existing Participants
            _hubConnection.On<List<ParticipantInfo>>("ExistingParticipants", participants =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnExistingParticipant?.Invoke(new ExistingParticipantData { Participants = participants });
                });
            });

            // User Joined
            _hubConnection.On<UserJoinData>("UserJoined", data =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnUserJoined?.Invoke(data);
                });
            });

            // User Left
            _hubConnection.On<UserLeftData>("UserLeft", data =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnUserLeft?.Invoke(data);
                });
            });

            // Receive Offer
            _hubConnection.On<OfferData>("ReceiveOffer", data =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnOfferReceived?.Invoke(data);
                });
            });

            // Receive Answer
            _hubConnection.On<AnswerData>("ReceiveAnswer", data =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnAnswerReceived?.Invoke(data);
                });
            });

            // Receive ICE Candidate
            _hubConnection.On<IceCandidateData>("ReceiveIceCandidate", data =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnIceCandidateReceived?.Invoke(data);
                });
            });

            // Camera Toggled
            _hubConnection.On<MediaToggleData>("CameraToggled", data =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnCameraToggled?.Invoke(data);
                });
            });

            // Microphone Toggled
            _hubConnection.On<MediaToggleData>("MicrophoneToggled", data =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnMicrophoneToggled?.Invoke(data);
                });
            });

            // Screen Share Toggled
            _hubConnection.On<MediaToggleData>("ScreenShareToggled", data =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnScreenShareToggled?.Invoke(data);
                });
            });

            // Error Handling
            _hubConnection.On<string>("Error", errorMessage =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnError?.Invoke(errorMessage);
                });
            });

            // Reconnection
            _hubConnection.Reconnecting += error =>
            {
                Console.WriteLine("Reconnecting to MeetingHub...");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                Console.WriteLine("Reconnected to MeetingHub");
                return Task.CompletedTask;
            };

            _hubConnection.Closed += async error =>
            {
                Console.WriteLine("Connection to MeetingHub closed. Attempting to reconnect...");
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _hubConnection.StartAsync();
            };
        }


        //================JOIN/LEAVE================
        public async Task JoinRoomAsync(Guid roomId)
        {
            if (!Isconnected) throw new InvalidOperationException("Not connected to the hub.");

            await _hubConnection.InvokeAsync("JoinRoom", roomId);
        }

        public async Task LeaveRoomAsync(Guid roomId)
        {
            if (!Isconnected) throw new InvalidOperationException("Not connected to the hub.");
            await _hubConnection.InvokeAsync("LeaveRoom", roomId);
        }


        //================SIGNALING================
        public async Task SendOfferAsync(string toConnectId, object offer)
        {
            if (!Isconnected) throw new InvalidOperationException("Not connected to the hub.");
            await _hubConnection.InvokeAsync("SendOffer", toConnectId, offer);
        }

        public async Task SendAnswerAsync(string toConnectId, object answer)
        {
            if (!Isconnected) throw new InvalidOperationException("Not connected to the hub.");
            await _hubConnection.InvokeAsync("SendAnswer", toConnectId, answer);
        }

        public async Task SendIceCandidateAsync(string toConnectId, object iceCandidate)
        {
            if (!Isconnected) throw new InvalidOperationException("Not connected to the hub.");
            await _hubConnection.InvokeAsync("SendIceCandidate", toConnectId, iceCandidate);
        }

        //==============MEDIA CONTROLS=============
        public async Task ToggleCameraAsync(bool isEnabled)
        {
            if (!Isconnected) throw new InvalidOperationException("Not connected to the hub.");
            await _hubConnection.InvokeAsync("ToggleCamera", isEnabled);
        }

        public async Task ToggleMicrophoneAsync(bool isEnabled)
        {
            if (!Isconnected) throw new InvalidOperationException("Not connected to the hub.");
            await _hubConnection.InvokeAsync("ToggleMicrophone", isEnabled);
        }

        public async Task ToggleScreenShareAsync(bool isEnabled)
        {
            if (!Isconnected) throw new InvalidOperationException("Not connected to the hub.");
            await _hubConnection.InvokeAsync("ToggleScreenShare", isEnabled);
        }

        //================DISCONNECT================
        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                Console.WriteLine("Disconnected from MeetingHub");
            }
        }
    }
}
