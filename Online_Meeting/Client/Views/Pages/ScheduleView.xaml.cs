using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Online_Meeting.Client.Views.Pages
{
    public partial class ScheduleView : Page, INotifyPropertyChanged
    {
        // Dữ liệu binding
        private ObservableCollection<ScheduleItem> _allMeetings;
        private ObservableCollection<ScheduleItem> _currentDayMeetings;

        public ObservableCollection<ScheduleItem> CurrentDayMeetings
        {
            get => _currentDayMeetings;
            set { _currentDayMeetings = value; OnPropertyChanged(); }
        }

        public ScheduleView()
        {
            InitializeComponent();
            this.DataContext = this;

            // 1. Tạo dữ liệu giả
            GenerateFakeData();

            // 2. Mặc định chọn ngày hôm nay
            MainCalendar.SelectedDate = DateTime.Today;
        }

        private void GenerateFakeData()
        {
            _allMeetings = new ObservableCollection<ScheduleItem>();
            var today = DateTime.Today;
            var r = new Random();

            // Tạo fake cho 5 ngày tới
            for (int i = 0; i < 5; i++)
            {
                var date = today.AddDays(i);
                // Mỗi ngày 2-4 cuộc họp
                int count = r.Next(2, 5);

                for (int j = 0; j < count; j++)
                {
                    var startHour = 8 + (j * 3); // 8h, 11h, 14h...
                    var startTime = date.AddHours(startHour);

                    _allMeetings.Add(new ScheduleItem
                    {
                        Title = GetRandomTitle(r),
                        StartTime = startTime,
                        EndTime = startTime.AddMinutes(r.Next(30, 120)),
                        HostName = "Nguyen Quang Huy",
                        RoomKey = "ROOM-" + r.Next(1000, 9999),
                        Type = r.Next(0, 2) == 0 ? "Online Meeting" : "Weekly Sync",
                        StatusColor = GetRandomColor(r)
                    });
                }
            }
        }

        private string GetRandomTitle(Random r)
        {
            string[] titles = {
                "Daily Standup Team Dev",
                "Họp triển khai dự án WebRTC",
                "Review thiết kế UI/UX",
                "Báo cáo tiến độ Sprint 15",
                "Client Meeting - Demo sản phẩm"
            };
            return titles[r.Next(titles.Length)];
        }

        private Brush GetRandomColor(Random r)
        {
            // Trả về mấy màu pastel đẹp đẹp
            string[] hexColors = { "#3B82F6", "#10B981", "#F59E0B", "#8B5CF6", "#EF4444" };
            var colorCode = hexColors[r.Next(hexColors.Length)];
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorCode));
        }

        private void MainCalendar_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainCalendar.SelectedDate.HasValue)
            {
                var date = MainCalendar.SelectedDate.Value;
                UpdateListForDate(date);
            }
        }

        private void UpdateListForDate(DateTime date)
        {
            // Cập nhật Header Text
            TxtSelectedDate.Text = date.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("vi-VN"));

            // Lọc dữ liệu từ danh sách tổng
            var filtered = _allMeetings.Where(x => x.StartTime.Date == date.Date).OrderBy(x => x.StartTime).ToList();

            CurrentDayMeetings = new ObservableCollection<ScheduleItem>(filtered);

            // Cập nhật số lượng
            TxtMeetingCount.Text = $"Bạn có {CurrentDayMeetings.Count} cuộc họp";

            // Xử lý Empty State
            if (CurrentDayMeetings.Count == 0)
            {
                ScheduleList.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
            }
            else
            {
                ScheduleList.Visibility = Visibility.Visible;
                EmptyState.Visibility = Visibility.Collapsed;
            }
        }

        // Xử lý nút Join trong List Item
        private void BtnJoin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string roomKey)
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    // Gọi hàm điều hướng và pass action Join
                    // (Hàm NavigateToMeetingPage phải được public ở MainWindow như bài trước)
                    // mainWindow.NavigateToMeetingPage(MeetingAction.Join);

                    // Hoặc copy code đơn giản:
                    Clipboard.SetText(roomKey);
                    MessageBox.Show($"Đã copy mã phòng: {roomKey}", "Sẵn sàng tham gia");
                }
            }
        }

        private void BtnCreateSchedule_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tính năng tạo lịch họp đang phát triển!", "Thông báo");
        }

        // Implementation INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

   public class ScheduleItem
    {
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string HostName { get; set; }
        public string RoomKey { get; set; }
        public string Type { get; set; }
        public Brush StatusColor { get; set; } // Màu sắc thanh trạng thái bên trái

        public string MeetingIdDisplay => RoomKey;

        // Logic: Chỉ hiện nút Join nếu cuộc họp là Hôm nay và chưa kết thúc
        public bool IsJoinable
        {
            get
            {
                var now = DateTime.Now;
                // Cho phép join trước 15 phút và chưa kết thúc quá 30 phút
                return StartTime.Date == DateTime.Today &&
                       now >= StartTime.AddMinutes(-15) &&
                       now <= EndTime.AddMinutes(30);
            }
        }
    }
}