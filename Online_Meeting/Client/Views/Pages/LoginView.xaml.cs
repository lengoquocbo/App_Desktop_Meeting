using Online_Meeting.Client.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Online_Meeting.Client.Views.Pages
{
    public partial class LoginView : Page
    {
        private readonly LoginViewModel _viewModel;

        public LoginView()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel();
            DataContext = _viewModel;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Username = txtUsername.Text;
            _viewModel.Password = txtPassword.Password;

            var response = await _viewModel.LoginAsync();

            if (response.IsSuccess)
            {
                MessageBox.Show(
                    $"Đăng nhập thành công!\nXin chào {response.UserName}",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Gọi ShowMainContent ở MainWindow hiện tại
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.ShowMainContent();
            }
            else
            {
                MessageBox.Show(
                    response.ErrorMessage,
                    "Lỗi đăng nhập",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }


        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new RegisterView());
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng quên mật khẩu đang phát triển", "Thông báo");
        }
    }
}
