using Microsoft.AspNetCore.SignalR.Protocol;
using Online_Meeting.Client.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.ViewModels
{
    internal class MeetingPreviewViewModel : INotifyPropertyChanged
    {
        public string SelectedCameraId { get; set; }
        public string SelectedMicId { get; set; }
        public bool AudioEnabled { get; set; }
        public bool VideoEnabled { get; set; }
        public bool AlwaysShowPreview { get; set; }
        public bool IsPermissionGranted { get; set; }

        // Event to request meeting start
        public event EventHandler<MeetingStartEventArgs> MeetingStartRequested;

        public void HandleStartMeeting(WebPreviewMsg message)
        {
            // Cập nhật state từ JS
            SelectedCameraId = message.cameraId;
            SelectedMicId = message.micId;
            AlwaysShowPreview = message.alwaysPreview;
            AudioEnabled = message.audio;
            VideoEnabled = message.video;

            // Fire event với tất cả thông tin
            MeetingStartRequested?.Invoke(this, new MeetingStartEventArgs
            {
                CameraId = SelectedCameraId,
                MicId = SelectedMicId,
                AudioEnabled = AudioEnabled,
                VideoEnabled = VideoEnabled,
                AlwaysShowPreview = AlwaysShowPreview,
                IsPermissionGranted = IsPermissionGranted
            });
        }

        public void HandlePermissionGranted()
        {
            IsPermissionGranted = true;
        }

        public void HandlePermissionDenied()
        {
            IsPermissionGranted = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
