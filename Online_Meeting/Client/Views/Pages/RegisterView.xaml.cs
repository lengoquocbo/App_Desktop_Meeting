using Online_Meeting.Client.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Online_Meeting.Client.Views.Pages
{
    public partial class RegisterView : Page
    {
        private readonly RegisterViewModel _viewModel;

        public RegisterView()
        {
            InitializeComponent();
            _viewModel = new RegisterViewModel();
            DataContext = _viewModel;
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Bind UI to ViewModel
            _viewModel.Username = txtUsername.Text.Trim();
            _viewModel.Email = txtEmail.Text.Trim();
            _viewModel.Password = txtPassword.Password;
            _viewModel.ConfirmPassword = txtConfirmPassword.Password;

            bool acceptedTerms = chkTerms.IsChecked ?? false;

            if (!acceptedTerms)
            {
                MessageBox.Show("Vui lòng đồng ý với điều khoản và chính sách!",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var response = await _viewModel.RegisterAsync();

            if (response.IsSuccess)
            {
                MessageBox.Show("Đăng ký thành công!", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService?.Navigate(new LoginView());
            }
            else
            {
                MessageBox.Show(
                    response.ErrorMessage ?? "Đăng ký thất bại",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new LoginView());
        }

        private void txtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
