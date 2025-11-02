using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Online_Meeting.Client.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        // Hardcoded credentials for testing
        private const string VALID_USERNAME = "admin";
        private const string VALID_PASSWORD = "123456";

        private string _username;
        private string _password;
        private bool _rememberMe;
        private string _errorMessage;

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            RegisterCommand = new RelayCommand(ExecuteRegister);
            ForgotPasswordCommand = new RelayCommand(ExecuteForgotPassword);
        }

        #region Properties

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                _rememberMe = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand ForgotPasswordCommand { get; }

        #endregion

        #region Command Methods

        private bool CanExecuteLogin(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password);
        }

        private void ExecuteLogin(object parameter)
        {
            ErrorMessage = string.Empty;

            // Validate with hardcoded credentials
            if (Username == VALID_USERNAME && Password == VALID_PASSWORD)
            {
                // Login successful
                MessageBox.Show($"Đăng nhập thành công!\nChào mừng {Username}!",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // TODO: Navigate to main view
                // Get MainWindow and call ShowMainContent
                var mainWindow = Application.Current.MainWindow as Views.MainWindow;
                mainWindow?.ShowMainContent();

                // TODO: Save remember me preference if needed
                if (RememberMe)
                {
                    // Save credentials to settings or secure storage
                }
            }
            else
            {
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng!";
                MessageBox.Show(ErrorMessage, "Lỗi đăng nhập",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteRegister(object parameter)
        {
            // TODO: Navigate to Register page
            // This will be handled by the View's code-behind
            MessageBox.Show("Chuyển đến trang đăng ký", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteForgotPassword(object parameter)
        {
            MessageBox.Show("Chức năng quên mật khẩu đang được phát triển.\n\n" +
                          "Liên hệ admin để được hỗ trợ.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region RelayCommand Helper Class

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion
}