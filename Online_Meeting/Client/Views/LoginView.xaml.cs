using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Online_Meeting.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        // Event handlers cho TextBox focus effects
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var border = FindParentBorder(textBox);
                if (border != null)
                {
                    AnimateBorderColor(border, "#3498DB");
                }
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var border = FindParentBorder(textBox);
                if (border != null)
                {
                    AnimateBorderColor(border, "#E8EAED");
                }
            }
        }

        // Event handlers cho PasswordBox focus effects
        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                var border = FindParentBorder(passwordBox);
                if (border != null)
                {
                    AnimateBorderColor(border, "#3498DB");
                }
            }
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                var border = FindParentBorder(passwordBox);
                if (border != null)
                {
                    AnimateBorderColor(border, "#E8EAED");
                }
            }
        }

        // Helper method để tìm Border cha
        private Border FindParentBorder(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is Border))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as Border;
        }

        // Animation cho border color
        private void AnimateBorderColor(Border border, string colorHex)
        {
            Color targetColor = (Color)ColorConverter.ConvertFromString(colorHex);
            ColorAnimation animation = new ColorAnimation
            {
                To = targetColor,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            SolidColorBrush brush = new SolidColorBrush(((SolidColorBrush)border.BorderBrush).Color);
            border.BorderBrush = brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        // Button Click Events
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập hoặc email!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return;
            }

            // TODO: Implement authentication logic here
            // Example:
            // var authService = new AuthenticationService();
            // var result = await authService.LoginAsync(txtUsername.Text, txtPassword.Password);

            // if (result.IsSuccess)
            // {
            //     var mainWindow = new MainWindow();
            //     mainWindow.Show();
            //     this.Close();
            // }
            // else
            // {
            //     MessageBox.Show(result.ErrorMessage, "Đăng nhập thất bại", 
            //         MessageBoxButton.OK, MessageBoxImage.Error);
            // }

            // For demo purposes:
            if (txtUsername.Text == "admin" && txtPassword.Password == "admin")
            {
                MessageBox.Show("Đăng nhập thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Navigate to main window
                // var mainWindow = new MainWindow();
                // mainWindow.Show();
                // this.Close();
            }
            else
            {
                MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng!", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to register window
            var registerWindow = new RegisterView();
            registerWindow.Show();
            this.Close();
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to forgot password window or show dialog
            MessageBox.Show("Chức năng đặt lại mật khẩu sẽ được cập nhật sớm!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // Example implementation:
            // var forgotPasswordWindow = new ForgotPasswordView();
            // forgotPasswordWindow.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Optional: Show confirmation dialog
            var result = MessageBox.Show("Bạn có chắc muốn thoát?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        // Optional: Drag window functionality
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            try
            {
                this.DragMove();
            }
            catch (Exception)
            {
                // Ignore exception when clicking on interactive elements
            }
        }

        // Optional: Handle Enter key press for quick login
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }
    }
}