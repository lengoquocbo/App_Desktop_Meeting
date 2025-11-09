using Online_Meeting.Client.Dtos;
using Online_Meeting.Client.Dtos.MeetingSignalRDto;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Models;


namespace Online_Meeting.Client.Services
{
    internal class MeetingService
    {
        private readonly IMeetingService _meetingApi;
        private readonly MeetingSignalRServices _signalR;

        private List<ParticipantInfo> _currentParticipants = new();

        public event Action<List<ParticipantInfo>> OnParticipantsUpdated;
        public event Action<UserJoinData> OnUserJoined;
        public event Action<UserLeftData> OnUserLeft;
        public event Action<string> OnError;

        public Guid? CurrentRoomId { get; private set; }
        public string CurrentRoomName { get; private set; }
        public string CurrentRole { get; private set; }

        public MeetingService(MeetingSignalRServices signalRService, IMeetingService meetingApi)
        {
            _meetingApi = meetingApi;
            _signalR = signalRService;

            // Gắn event từ SignalR ra ngoài
            _signalR.OnExistingParticipant += d =>
            {
                _currentParticipants = d.Participants ?? new List<ParticipantInfo>();
                OnParticipantsUpdated?.Invoke(_currentParticipants);
            };

            _signalR.OnUserJoined += d =>
            {
                OnUserJoined?.Invoke(d);
                _currentParticipants.Add(new ParticipantInfo
                {
                    UserId = d.UserId,
                    Username = d.Username,
                    ConnectionId = d.ConnectionId
                });
                OnParticipantsUpdated?.Invoke(_currentParticipants);
            };

            _signalR.OnUserLeft += d =>
            {
                OnUserLeft?.Invoke(d);
                _currentParticipants.RemoveAll(x => x.UserId == d.UserId);
                OnParticipantsUpdated?.Invoke(_currentParticipants);
            };

            _signalR.OnError += msg => OnError?.Invoke(msg);
        }

        //=================ROOM MANAGEMENT=================

        // Join room by id
        public async Task<JoinRoomResult> JoinRoomByIdAsync(Guid roomId)
        {
            var joiRes = await _meetingApi.JoinRoom(roomId);
            if (!joiRes.Success) throw new Exception($"Error when join room: {joiRes.Message}");

            var data = joiRes.Data;

            // Set current room info
            CurrentRoomId = data.RoomId;
            CurrentRoomName = data.Room.RoomName;
            CurrentRole = data.Role;

            if (!_signalR.Isconnected) await _signalR.ConnectAsync();

            await _signalR.JoinRoomAsync(roomId);

            _currentParticipants = new List<ParticipantInfo>();
            OnParticipantsUpdated?.Invoke(_currentParticipants);

            return new JoinRoomResult
            {
                RoomId = data.RoomId,
                RoomName = data.Room.RoomName,
                Role = data.Role,
                Participants = _currentParticipants
            };
        }

        // Join room by key
        public async Task<JoinRoomResult> JoinRoomByKeyAsync(string roomKey)
        {
            var joiRes = await _meetingApi.JoinRoomByKey(roomKey);
            if (!joiRes.Success) throw new Exception($"Error when join room: {joiRes.Message}");

            var data = joiRes.Data;

            // Set current room info
            CurrentRoomId = data.RoomId;
            CurrentRoomName = data.Room.RoomName;
            CurrentRole = data.Role;

            if (!_signalR.Isconnected) await _signalR.ConnectAsync();

            await _signalR.JoinRoomAsync(data.RoomId);

            _currentParticipants = new List<ParticipantInfo>();
            OnParticipantsUpdated?.Invoke(_currentParticipants);

            return new JoinRoomResult
            {
                RoomId = data.RoomId,
                RoomName = data.Room.RoomName,
                Role = data.Role,
                Participants = _currentParticipants
            };
        }

        // Leave room
        public async Task LeaveRoomAsync(Guid roomId)
        {
            var res = await _meetingApi.LeaveRoom(roomId);
            if (!res.Success)
                throw new Exception(res.Message);

            // Clear current room info
            CurrentRoomId = null;
            CurrentRoomName = null;
            CurrentRole = null;

            if (_signalR.Isconnected)
                await _signalR.LeaveRoomAsync(roomId);

            _currentParticipants.Clear();
            OnParticipantsUpdated?.Invoke(_currentParticipants);
        }

        //=================RESYNC AFTER RECONNECT=================
        private async Task ResyncAfterReconnectAsync()
        {
            if (CurrentRoomId == null) return;

            // 1) Lấy snapshot từ API
            var snap = await _meetingApi.GetParticipants(CurrentRoomId.Value);
            if (!snap.Success)
            {
                // Không còn trong phòng (hoặc token/room invalid) → clear UI
                _currentParticipants.Clear();
                OnParticipantsUpdated?.Invoke(new List<ParticipantInfo>(_currentParticipants));
                return;
            }

            // 2) Join lại group (phòng) trên hub
            if (!_signalR.Isconnected) return; // hiếm khi reconnected lại false, nhưng cứ phòng hờ
            await _signalR.JoinRoomAsync(CurrentRoomId.Value);

            // 3) Cập nhật danh sách hiện tại và notify UI
            _currentParticipants.Clear();
            OnParticipantsUpdated?.Invoke(new List<ParticipantInfo>(_currentParticipants));
        }
    }
}
