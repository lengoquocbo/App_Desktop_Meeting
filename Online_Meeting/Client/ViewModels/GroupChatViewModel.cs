using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Models;
using Online_Meeting.Share.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Online_Meeting.Client.ViewModels
{
    public class GroupChatViewModel : ViewModelBase
    {

        private readonly IGroupService _groupService;

        // ObservableCollection để UI tự cập nhật khi thêm/bớt nhóm
        private ObservableCollection<ChatGroup> _groups = new();
        public ObservableCollection<ChatGroup> Groups
        {
            get => _groups;
            set => SetProperty(ref _groups, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // Command để load danh sách nhóm
        public ICommand LoadGroupsCommand { get; }

        public GroupChatViewModel(IGroupService groupService)
        {
            _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));

            // Khởi tạo command, dùng RelayCommand hoặc DelegateCommand tùy bạn implement
            LoadGroupsCommand = new AsyncRelayCommand(LoadGroupsAsync);
        }

        // Hàm tải nhóm
        private async Task LoadGroupsAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var response = await _groupService.GetMyGroupsAsync();

                if (response != null && response.Success)
                {
                    Groups = new ObservableCollection<ChatGroup>(response.Data ?? new List<ChatGroup>());
                }
                else
                {
                    ErrorMessage = "Không thể tải danh sách nhóm!";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải dữ liệu: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    // RelayCommand cho ICommand async
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting;

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
