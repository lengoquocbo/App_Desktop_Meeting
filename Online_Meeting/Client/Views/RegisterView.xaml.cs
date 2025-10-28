using System.Diagnostics;
using System.Windows;

namespace Online_Meeting.Views
{
    public partial class RegisterView : Window
    {
        public RegisterView()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Đăng ký thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chuyển sang trang đăng nhập...", "Thông báo");
        }

        private void Terms_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://example.com/terms") { UseShellExecute = true });
        }

        private void Privacy_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://example.com/privacy") { UseShellExecute = true });
        }
    }
}
