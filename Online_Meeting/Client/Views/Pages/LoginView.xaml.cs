using Online_Meeting.Client.ViewModels;
using Online_Meeting.Client.Views.Pages;
using System.Windows;
using System.Windows.Controls;

namespace Online_Meeting.Client.Views.Pages
{
    public partial class LoginView : Page
    {
        public LoginView()
        {
            InitializeComponent();

            // Set DataContext if not set in XAML
            if (DataContext == null)
            {
                DataContext = new LoginViewModel();
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Register page
            if (NavigationService != null)
            {
                NavigationService.Navigate(new RegisterView());
            }
        }

        // Handle PasswordBox separately since it can't be bound directly for security reasons
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = txtPassword.Password;
            }
        }
    }
}