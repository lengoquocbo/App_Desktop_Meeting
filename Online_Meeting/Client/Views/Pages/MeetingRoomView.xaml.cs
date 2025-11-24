using Microsoft.Extensions.DependencyInjection;
using Online_Meeting.Client.Dtos.MeetingDto;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Models;
using Online_Meeting.Client.Services;
using Online_Meeting.Client.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;


namespace Online_Meeting.Client.Views.Pages
{
    public partial class MeetingRoomView : Page
    {

        private readonly ITokenService _token;
        private readonly IMeetingService _meetingApi;
        private readonly MeetingService _meetingService;
        private readonly MeetingSignalRServices _meetingSignalRServices;
        public MeetingRoomView(
            ITokenService token, 
            IMeetingService api,
            MeetingService meetingService,
            MeetingSignalRServices meetingSignalRServices
        )
        {
            InitializeComponent();
            _token = token;
            _meetingApi = api;
            _meetingService = meetingService;
            _meetingSignalRServices = meetingSignalRServices;



        }

        public void SetupForCreate()
        {
            // Reset text cũ 
            RoomNameTextBox.Text = string.Empty;
            SetFocus(RoomNameTextBox);
        }
        public void SetupForJoin()
        {
            // Reset text cũ
            MeetingCodeTextBox.Text = string.Empty;
            SetFocus(MeetingCodeTextBox);
        }
        private void SetFocus(Control control)
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
            {
                control.Focus();
                System.Windows.Input.Keyboard.Focus(control);
            }));
        }
        private void OpenPreview_Click(object sender, RoutedEventArgs e)
        {
            var token = _token.GetAccessToken();
            if (String.IsNullOrEmpty(token))
            {
                MessageBox.Show("Please login first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var previewDialog = App.Services.GetService<MeetingPreview>();

            previewDialog.PreviewCompleted += OnMeetingStart;

            if (previewDialog == null)
            {
                MessageBox.Show("Dialog service not registered in DI container.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            previewDialog.ShowDialog();
        }

        private void JoinByKeyBtn_Click(object sender, RoutedEventArgs e)
        {
            var token = _token.GetAccessToken();
            if (String.IsNullOrEmpty(token))
            {
                MessageBox.Show("Please login first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var previewDialog = App.Services.GetService<MeetingPreview>();

            previewDialog.PreviewCompleted += OnMeetingStartJoinByRoomKey;

            if (previewDialog == null)
            {
                MessageBox.Show("Dialog service not registered in DI container.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            previewDialog.ShowDialog();
        }

        private async void OnMeetingStart(object sender, MeetingStartEventArgs e)
        {
            var callName = RoomNameTextBox.Text;
            RoomNameTextBox.Text = ""; // Clear text box after getting the name

            var request = new CreateRoomRequest
            {
                RoomName = callName,
                MaxParticipants = AppConfig.VideoCall.MaxParticipants
            };

            // Gọi API để tạo cuộc họp mới
            var createRoomResult = await _meetingApi.CreateRoom(request);
            var userId = _token.GetUserId();
            var username = _token.GetUsername();


            if (!createRoomResult.Success)
            {
                MessageBox.Show($"Cannot create meeting room: {createRoomResult.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //join room vừa tạo (cho host)
            var joinResult = await _meetingService.JoinRoomByIdForHostAsync(createRoomResult.Data.Id, createRoomResult.Data.RoomName, "host", e.AudioEnabled, e.VideoEnabled);

            //Khởi tạo giao diện callscreen
            var callScreen = new CallScreen(
                false,
                callName,
                createRoomResult.Data.Id,
                createRoomResult.Data.RoomKey,
                createRoomResult.Data.JoinUrl,
                userId,
                username,
                true,
                e.CameraId,
                e.MicId,
                e.AudioEnabled,
                e.VideoEnabled,
                _meetingService,
                _meetingSignalRServices
               
            );
            callScreen.Show();
        }

        private async void OnMeetingStartJoinByRoomKey(object sender, MeetingStartEventArgs e)
        {
            var token = _token.GetAccessToken();
            if (String.IsNullOrEmpty(token))
            {
                MessageBox.Show("Please login first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var roomKey = MeetingCodeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(roomKey))
            {
                MessageBox.Show("Please enter a valid room key.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // Gọi meeting service để join room bằng room key
            var joinRoomResult = await _meetingService.JoinRoomByKeyAsync(roomKey, e.AudioEnabled, e.VideoEnabled);
            
            //Khởi tạo giao diện callscreen
            var callScreen = new CallScreen(
                joinRoomResult.IsWaiting,
                joinRoomResult.RoomName,
                joinRoomResult.RoomId,
                joinRoomResult.RoomKey,
                joinRoomResult.RoomUrl,
                _token.GetUserId(),
                _token.GetUsername(),
                joinRoomResult.Role == "host",
                e.CameraId,
                e.MicId,
                e.AudioEnabled,
                e.VideoEnabled,
                _meetingService,
                _meetingSignalRServices
            );
            callScreen.Show();
        }
    }
}
