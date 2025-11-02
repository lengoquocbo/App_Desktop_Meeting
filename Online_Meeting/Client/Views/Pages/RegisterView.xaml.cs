using Online_Meeting.Client.Views.Pages;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Online_Meeting.Client.Views.Pages
{
    public partial class RegisterView : Page
    {
        public RegisterView()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Get input values
            string fullName = txtFullName.Text.Trim();
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;
            bool acceptedTerms = chkTerms.IsChecked ?? false;

            // Validate Full Name
            if (string.IsNullOrEmpty(fullName))
            {
                MessageBox.Show("Vui lòng nhập họ và tên!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFullName.Focus();
                return;
            }

            // Validate Username
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.Focus();
                return;
            }

            if (username.Length < 3)
            {
                MessageBox.Show("Tên đăng nhập phải có ít nhất 3 ký tự!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.Focus();
                return;
            }

            // Validate Email
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Vui lòng nhập email!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Email không hợp lệ!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            // Validate Password
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return;
            }

            if (password.Length < 6)
            {
                MessageBox.Show("Mật khẩu phải có ít nhất 6 ký tự!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return;
            }

            // Validate Confirm Password
            if (password != confirmPassword)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtConfirmPassword.Focus();
                return;
            }

            // Check Terms
            if (!acceptedTerms)
            {
                MessageBox.Show("Vui lòng đồng ý với điều khoản và chính sách!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: Call API to register user
            // Example:
            // var result = await _authService.RegisterAsync(fullName, username, email, password);
            // if (result.IsSuccess)
            // {
            //     MessageBox.Show("Đăng ký thành công!", "Thông báo", 
            //         MessageBoxButton.OK, MessageBoxImage.Information);
            //     NavigationService?.Navigate(new LoginView());
            // }

            MessageBox.Show($"Đăng ký thành công!\n\nHọ tên: {fullName}\nUsername: {username}\nEmail: {email}",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

            // Navigate to Login page
            if (NavigationService != null)
            {
                NavigationService.Navigate(new LoginView());
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to Login page
            if (NavigationService != null)
            {
                NavigationService.Navigate(new LoginView());
            }
        }

        private bool IsValidEmail(string email)
        {
            // Simple email validation using regex
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }
    }
}