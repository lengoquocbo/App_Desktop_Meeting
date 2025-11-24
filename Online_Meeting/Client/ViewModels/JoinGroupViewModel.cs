using System;
using System.Threading.Tasks;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Services;

namespace Online_Meeting.Client.ViewModels
{
    public class JoinGroupViewModel : ViewModelBase
    {
        private readonly IGroupService _groupService;
        private readonly ITokenService _token;


        private string _groupId;
        private bool _isLoading;
        private string _errorMessage;

        public JoinGroupViewModel(IGroupService groupService)
        {
            _groupService = groupService;

        }

        public string GroupId
        {
            get => _groupId;
            set
            {
                if (SetProperty(ref _groupId, value))
                {
                    ErrorMessage = string.Empty; // Clear error khi user nhập
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public async Task<bool> JoinGroupAsync()
        {
            // Validate
            if (string.IsNullOrWhiteSpace(GroupId))
            {
                ErrorMessage = "Please enter Group ID";
                return false;
            }

            if (!Guid.TryParse(GroupId, out Guid groupGuid))
            {
                ErrorMessage = "Invalid Group ID format";
                return false;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var response = await _groupService.JoinGroupAsync(groupGuid);

                if (response != null && response.Success)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = response?.Message ?? "Failed to join group";
                    return false;
                }
            }
            catch (Refit.ApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    ErrorMessage = "Already joined this group or group not found";
                }
                else
                {
                    ErrorMessage = $"Error: {ex.Message}";
                }
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
