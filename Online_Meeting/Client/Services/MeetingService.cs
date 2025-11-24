using Online_Meeting.Client.Dtos;
using Online_Meeting.Client.Dtos.MeetingSignalRDto;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Models;
using static vpxmd.VpxCodecCxPkt;


namespace Online_Meeting.Client.Services
{
    public class MeetingService
    {
        private readonly IMeetingService _meetingApi;
        private readonly MeetingSignalRServices _signalR;

        private List<ParticipantInfo> _currentParticipants = new();

        public event Action<List<ParticipantInfo>> OnParticipantsUpdated;
        public event Action<UserJoinData> OnUserJoined;
        public event Action<UserLeftData> OnUserLeft;
        public event Action<string> OnError;
        public event Action OnYouAreWaiting;
        public event Action<UserJoinData> OnGuestRequested;
        public event Action OnYouAreRejected;

        public Guid? CurrentRoomId { get; private set; }
        public string CurrentRoomName { get; private set; }
        public string CurrentRole { get; private set; }

        // Expose current participants as read-only
        public IReadOnlyList<ParticipantInfo> CurrentParticipants => _currentParticipants.AsReadOnly();

        public MeetingService(MeetingSignalRServices signalRService, IMeetingService meetingApi)
        {
            _meetingApi = meetingApi;
            _signalR = signalRService;

            // Gắn event từ SignalR ra ngoài
            _signalR.OnExistingParticipant += d =>
            {
                Console.WriteLine($"[MeetingService] OnExistingParticipant triggered with {d.Participants?.Count ?? 0} participants");

                _currentParticipants = d.Participants ?? new List<ParticipantInfo>();

                // ⭐ THÊM LOG NÀY
                Console.WriteLine($"[MeetingService] _currentParticipants now has {_currentParticipants.Count} participants");

                OnParticipantsUpdated?.Invoke(_currentParticipants);
            };

            _signalR.OnUserJoined += d =>
            {
                OnUserJoined?.Invoke(d);
                _currentParticipants.Add(new ParticipantInfo
                {
                    UserId = d.UserId,
                    Username = d.Username,
                    ConnectionId = d.ConnectionId,
                    camEnable = d.camEnable,
                    micEnable = d.micEnable
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

            _signalR.OnYouAreWaiting += () => OnYouAreWaiting?.Invoke();
            _signalR.OnGuestRequested += (d) => OnGuestRequested?.Invoke(d);
            _signalR.OnYouAreRejected += () => OnYouAreRejected?.Invoke();
        }

        //=================ROOM MANAGEMENT=================

        // Join room by id
        public async Task<JoinRoomResult> JoinRoomByIdAsync(Guid roomId, bool micEnable, bool camEnable)
        {
            var joiRes = await _meetingApi.JoinRoom(roomId);
            if (!joiRes.Success) throw new Exception($"Error when join room: {joiRes.Message}");

            var data = joiRes.Data;

            // Set current room info
            CurrentRoomId = data.RoomId;
            CurrentRoomName = data.Room.RoomName;
            CurrentRole = data.Role;

            if (!_signalR.Isconnected) await _signalR.ConnectAsync();


            // 1. Tạo chốt đợi kết quả dạng String (để biết trạng thái trả về là gì)
            // Các trạng thái: "Joined", "Waiting", "Rejected"
            var tcs = new TaskCompletionSource<string>();

            // 2. Tạo các handler tạm thời

            // Trường hợp 1: Được vào thẳng (Host hoặc phòng không có waiting room)
            Action<ExistingParticipantData> onAdmitted = (d) =>
            {
                // Biến _currentParticipants đã được cập nhật ở Constructor (sự kiện chính),
                // ở đây chỉ cần mở chốt.
                tcs.TrySetResult("Joined");
            };

            // Trường hợp 2: Phải vào phòng chờ
            Action onWaiting = () =>
            {
                tcs.TrySetResult("Waiting");
            };

            // Trường hợp 3: Bị từ chối (Edge case)
            Action onRejected = () =>
            {
                tcs.TrySetResult("Rejected");
            };

            // 3. Đăng ký tất cả handler tạm thời
            _signalR.OnExistingParticipant += onAdmitted;
            _signalR.OnYouAreWaiting += onWaiting;
            _signalR.OnYouAreRejected += onRejected;

            try
            {
                // 4. Gửi lệnh Join
                await _signalR.JoinRoomAsync(roomId, micEnable, camEnable);

                // 5. Đợi chốt mở (hoặc timeout)
                var waitTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));

                if (waitTask == tcs.Task)
                {
                    // Nhận được phản hồi từ Server
                    string status = tcs.Task.Result;

                    if (status == "Waiting")
                    {
                        // Nếu đang chờ -> Danh sách người tham gia tạm thời là Rỗng
                        // (Vì chưa nhìn thấy ai)
                        _currentParticipants.Clear();

                        // Có thể log hoặc xử lý thêm nếu cần
                        Console.WriteLine("ℹ️ User is in Waiting Room");
                    }
                    else if (status == "Rejected")
                    {
                        throw new Exception("Access to the meeting was rejected.");
                    }
                    // Nếu status == "Joined" thì mọi thứ đã OK, danh sách đã có.
                }
                else
                {
                    Console.WriteLine("⚠️ Timeout waiting for server response (ExistingParticipants or Waiting)!");
                }
            }
            finally
            {
                // 6. Dọn dẹp TẤT CẢ handler tạm thời
                _signalR.OnExistingParticipant -= onAdmitted;
                _signalR.OnYouAreWaiting -= onWaiting;
                _signalR.OnYouAreRejected -= onRejected;
            }

            return new JoinRoomResult
            {
                RoomId = data.RoomId,
                RoomName = data.Room.RoomName,
                RoomKey = data.Room.RoomKey,
                RoomUrl = data.Room.RoomUrl,
                Role = data.Role,
                Participants = _currentParticipants
            };
        }

        //Join room by id for host
        public async Task<JoinRoomResult> JoinRoomByIdForHostAsync(Guid roomId, string RoomName, string role, bool micEnable, bool camEnable)
        {
            // Set current room info
            CurrentRoomId = roomId;
            CurrentRoomName = RoomName;
            CurrentRole = role;

            if (!_signalR.Isconnected) await _signalR.ConnectAsync();

            await _signalR.JoinRoomAsync(roomId, micEnable, camEnable);

            _currentParticipants = new List<ParticipantInfo>();
            OnParticipantsUpdated?.Invoke(_currentParticipants);

            return new JoinRoomResult
            {
                RoomId = roomId,
                RoomName = RoomName,
                Role = role,
                Participants = _currentParticipants
            };
        }

        public async Task<JoinRoomResult> JoinRoomByKeyAsync(string roomKey, bool micEnable, bool camEnable)
        {
            var joiRes = await _meetingApi.JoinRoomByKey(roomKey);
            if (!joiRes.Success) throw new Exception($"Error when join room: {joiRes.Message}");

            var data = joiRes.Data;

            // Set current room info
            CurrentRoomId = data.RoomId;
            CurrentRoomName = data.Room.RoomName;
            CurrentRole = data.Role;

            if (!_signalR.Isconnected) await _signalR.ConnectAsync();

            var tcs = new TaskCompletionSource<string>();

            Action<ExistingParticipantData> onAdmitted = (d) =>
            {
                // 1. Cập nhật danh sách NGAY LẬP TỨC khi nhận event
                if (d.Participants != null)
                {
                    _currentParticipants = d.Participants;
                    Console.WriteLine($"[Join] Received {d.Participants.Count} participants via JoinByKey");
                }

                // 2. Mở chốt
                tcs.TrySetResult("Joined");
            };

            Action onWaiting = () => { tcs.TrySetResult("Waiting"); };
            Action onRejected = () => { tcs.TrySetResult("Rejected"); };

            _signalR.OnExistingParticipant += onAdmitted;
            _signalR.OnYouAreWaiting += onWaiting;
            _signalR.OnYouAreRejected += onRejected;

            string status = null;

            try
            {
                await _signalR.JoinRoomAsync(data.RoomId, micEnable, camEnable);

                var waitTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));

                if (waitTask == tcs.Task)
                {
                    status = tcs.Task.Result;
                    if (status == "Waiting")
                    {
                        _currentParticipants.Clear();
                        Console.WriteLine("ℹ️ User is in Waiting Room");
                    }
                    else if (status == "Rejected")
                    {
                        throw new Exception("Access to the meeting was rejected by the host.");
                    }
                    // Nếu status == "Joined", _currentParticipants ĐÃ CÓ DỮ LIỆU nhờ handler ở trên
                }
                else
                {
                    Console.WriteLine("⚠️ Timeout waiting for server response!");
                }
            }
            finally
            {
                _signalR.OnExistingParticipant -= onAdmitted;
                _signalR.OnYouAreWaiting -= onWaiting;
                _signalR.OnYouAreRejected -= onRejected;
            }

            return new JoinRoomResult
            {
                RoomId = data.RoomId,
                RoomName = data.Room.RoomName,
                RoomKey = data.Room.RoomKey,
                RoomUrl = data.Room.RoomUrl,
                Role = data.Role,
                IsWaiting = (status == "Waiting"),
                Participants = _currentParticipants // Bây giờ chắc chắn đã có dữ liệu
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

        public async Task AdmitGuest(string connectionId) => await _signalR.AdmitUserAsync(connectionId);
        public async Task RejectGuest(string connectionId) => await _signalR.RejectUserAsync(connectionId);
        //=================RESYNC AFTER RECONNECT=================
        private async Task ResyncAfterReconnectAsync(bool micEnable, bool camEnable)
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
            await _signalR.JoinRoomAsync(CurrentRoomId.Value, micEnable, camEnable);

            // 3) Cập nhật danh sách hiện tại và notify UI
            _currentParticipants.Clear();
            OnParticipantsUpdated?.Invoke(new List<ParticipantInfo>(_currentParticipants));
        }
    }
}
