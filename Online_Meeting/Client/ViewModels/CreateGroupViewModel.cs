using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Online_Meeting.Client.Interfaces;
using static Online_Meeting.Client.Dtos.ChatDto.CreateGroupChatRequest;

namespace Online_Meeting.Client.ViewModels
{
    public class CreateGroupViewModel : INotifyPropertyChanged
    {
        private readonly IGroupService _groupService;
        private bool _isLoading;
        private string _errorMessage;

        public CreateGroupViewModel(IGroupService groupService)
        {
            _groupService = groupService;
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
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

        public async Task<CreateGroupResponse> CreateGroupAsync(string groupName)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var request = new CreateGroupRequest
                {
                    GroupName = groupName
                };

                // DEBUG: Log request
                System.Diagnostics.Debug.WriteLine($"Calling API with GroupName: {groupName}");

                var response = await _groupService.CreateGroup(request);

                // DEBUG: Log response
                System.Diagnostics.Debug.WriteLine($"Response Success: {response.Success}");

                if (response.Success)
                {
                    return response.Data;
                }
                else
                {
                    ErrorMessage = response.Message ?? "Failed to create group";
                    return null;
                }
            }
            catch (Refit.ApiException apiEx)
            {
                // DEBUG: Log chi tiết lỗi
                System.Diagnostics.Debug.WriteLine($"API Exception: {apiEx.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Request URL: {apiEx.RequestMessage?.RequestUri}");
                System.Diagnostics.Debug.WriteLine($"Response: {apiEx.Content}");

                ErrorMessage = $"API Error: {apiEx.StatusCode} - {apiEx.Message}";
                return null;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                ErrorMessage = $"Error: {ex.Message}";
                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}