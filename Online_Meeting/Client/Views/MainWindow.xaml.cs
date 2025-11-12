using Microsoft.Extensions.DependencyInjection;
using Online_Meeting.Client.Views.Pages;
using System.Windows;
using System.Windows.Controls;

namespace Online_Meeting.Client.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Navigate to Login page when app starts
            ShowLoginPage();
        }

        // Method to show login page (hide sidebar)
        private void ShowLoginPage()
        {
            // Ẩn sidebar
            SidebarPanel.Visibility = Visibility.Collapsed;

            // Set MainContentPanel chiếm toàn bộ cả 2 cột
            Grid.SetColumn(MainContentPanel, 0);
            Grid.SetColumnSpan(MainContentPanel, 2);

            // Navigate to login
            MainFrame.Navigate(new LoginView());
        }

        // Method to show main content after successful login
        public void ShowMainContent()
        {
            // Hiện sidebar
            SidebarPanel.Visibility = Visibility.Visible;

            // Đặt lại MainContentPanel về cột 1
            Grid.SetColumn(MainContentPanel, 1);
            Grid.SetColumnSpan(MainContentPanel, 1);

            // Navigate to home page
            SetActiveButton(btnHome);
            MainFrame.Navigate(new HomeView());
        }


        private void SetActiveButton(Button activeButton)
        {
            // Reset tất cả buttons về style mặc định
            btnHome.Style = (Style)FindResource("SidebarButtonStyle");
            btnChat.Style = (Style)FindResource("SidebarButtonStyle");
            btnMeeting.Style = (Style)FindResource("SidebarButtonStyle");
            btnSchedule.Style = (Style)FindResource("SidebarButtonStyle");
            btnSettings.Style = (Style)FindResource("SidebarButtonStyle");
            btnProfile.Style = (Style)FindResource("SidebarButtonStyle");

            // Set active style cho button được chọn
            activeButton.Style = (Style)FindResource("ActiveButtonStyle");
        }
        public void SetUsername(string username)
        {
            txtUsername.Text = username;
        }


        private void NavigateToHome(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnHome);
            MainFrame.Navigate(new HomeView());
        }

        private void NavigateToChat(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnChat);

            // Resolve GroupChatView từ DI container
            var view = App.Services.GetService<GroupChatView>();
            if (view != null)
            {
                MainFrame.Navigate(view);
            }
            else
            {
                MessageBox.Show("Cannot navigate to GroupChatView. Service not registered.");
            }
        }
        private void NavigateToMeeting(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnMeeting);
            MainFrame.Navigate(new MeetingRoomView());
        }

        private void NavigateToSchedule(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnSchedule);
            // MainFrame.Navigate(new SchedulePage());
        }

        private void NavigateToSettings(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnSettings);
            // MainFrame.Navigate(new SettingsPage());
        }

        private void NavigateToProfile(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnProfile);
            // MainFrame.Navigate(new ProfilePage());
        }
    }
}