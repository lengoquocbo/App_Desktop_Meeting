using Microsoft.Extensions.DependencyInjection;
using Online_Meeting.Client.Dtos.MeetingDto;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Online_Meeting.Client.Views.Pages
{
    public partial class HomeView : Page
    {
        private readonly IMeetingService _meetingservice;

        public HomeView()
        {
            InitializeComponent();

            _meetingservice = App.Services.GetService<IMeetingService>();
            this.Loaded += HomePage_Loaded;
        }
        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Tránh load lại nhiều lần nếu không cần thiết
            this.Loaded -= HomePage_Loaded;
            await LoadMeetingHistory();
        }

        private async Task LoadMeetingHistory()
        {
            try
            {
                // 1. Hiện loading
                LoadingBar.Visibility = Visibility.Visible;
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                RecentMeetingsList.Visibility = Visibility.Collapsed;

                // 2. Gọi API
                var response = await _meetingservice.GetMyMeetingHistory();

                if (response.Success && response.Data != null && response.Data.Any())
                {
                    // 3. Xử lý dữ liệu + Múi giờ UTC+7
                    // Chuyển đổi từ DTO sang DisplayItem để hiển thị đẹp
                    var displayList = response.Data
                        .OrderByDescending(x => x.JoinAt) // Mới nhất lên đầu
                        .Select(item => new MeetingDisplayItem(item)) // Wrapper class (xem bên dưới)
                        .ToList();

                    // 4. Gán vào ItemsControl
                    RecentMeetingsList.ItemsSource = displayList;
                    RecentMeetingsList.Visibility = Visibility.Visible;
                }
                else
                {
                    // Hiện Empty State
                    EmptyStatePanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi load history: {ex.Message}");
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingBar.Visibility = Visibility.Collapsed;
            }
        }

        private void StartMeetingCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                // Truyền action Create -> Sẽ focus ô RoomName
                mainWindow.NavigateToMeetingPage(MeetingAction.Create);
            }
        }

        private void JoinMeetingCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                // Truyền action Join -> Sẽ focus ô MeetingCode
                mainWindow.NavigateToMeetingPage(MeetingAction.Join);
            }
        }

        private void ScheduleMeetingCard_Click(object sender, MouseButtonEventArgs e)
        {
            if(Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.NavigateToSchedule();
            }
        }

        public class MeetingDisplayItem
        {
            // Giữ lại dữ liệu gốc nếu cần dùng
            private MeetingHistory _original;

            public Guid Id => _original.Id;
            public string RoomName => _original.RoomName;
            public string RoomKey => _original.RoomKey;

            // Lấy chữ cái đầu để làm Avatar
            public string AvatarChar => !string.IsNullOrEmpty(_original.RoomName)
                ? _original.RoomName.Substring(0, 1).ToUpper()
                : "?";

            // Chuỗi hiển thị dòng phụ (Time • Role • Duration)
            public string SubtitleInfo { get; private set; }

            public MeetingDisplayItem(MeetingHistory history)
            {
                _original = history;
                SubtitleInfo = FormatSubtitle();
            }

            private string FormatSubtitle()
            {
                // --- XỬ LÝ MÚI GIỜ UTC+7 ---

                // DateTime.UtcNow là giờ quốc tế
                // Giờ Việt Nam hiện tại = UtcNow + 7
                var nowVietnam = DateTime.UtcNow.AddHours(7);

                // Thời gian join của user (Giả sử DB lưu UTC) -> Chuyển sang giờ VN
                var joinAtVietnam = _original.JoinAt.ToUniversalTime().AddHours(7);

                // Tính khoảng cách thời gian
                var span = nowVietnam - joinAtVietnam;
                string timeAgoString;

                if (span.TotalMinutes < 1) timeAgoString = "Vừa xong";
                else if (span.TotalMinutes < 60) timeAgoString = $"{(int)span.TotalMinutes} phút trước";
                else if (span.TotalHours < 24) timeAgoString = $"{(int)span.TotalHours} giờ trước";
                else if (span.TotalDays < 7) timeAgoString = $"{(int)span.TotalDays} ngày trước";
                else timeAgoString = joinAtVietnam.ToString("dd/MM/yyyy"); // Quá lâu thì hiện ngày

                // Format duration (nếu null thì coi là 0)
                var duration = _original.Duration.HasValue ? Math.Round(_original.Duration.Value, 0) : 0;

                // Ghép chuỗi: "2 giờ trước • Host • 45 phút"
                return $"{timeAgoString} • {_original.Role} • {duration} phút";
            }
        }
    }
}