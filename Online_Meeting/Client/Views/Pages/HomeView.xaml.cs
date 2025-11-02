using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Online_Meeting.Client.Views.Pages
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : Page
    {
        public HomeView()
        {
            InitializeComponent();
        }

        private void StartMeetingCard_Click(object sender, MouseButtonEventArgs e)
        {
            // TODO: Navigate to Start Meeting
            MessageBox.Show("Start Meeting clicked!");
        }

        private void JoinMeetingCard_Click(object sender, MouseButtonEventArgs e)
        {
            // TODO: Navigate to Join Meeting
            MessageBox.Show("Join Meeting clicked!");
        }

        private void ScheduleMeetingCard_Click(object sender, MouseButtonEventArgs e)
        {
            // TODO: Navigate to Schedule Meeting
            MessageBox.Show("Schedule Meeting clicked!");
        }
    }
}