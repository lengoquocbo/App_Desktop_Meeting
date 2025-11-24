using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Models
{
    public class MeetingStartEventArgs : EventArgs
    {
        public string CameraId { get; set; }
        public string MicId { get; set; }
        public bool AudioEnabled { get; set; }
        public bool VideoEnabled { get; set; }
        public bool AlwaysShowPreview { get; set; }
        public bool IsPermissionGranted { get; set; }
    }
}
