using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Models
{
    public class WebPreviewMsg
    {
        public string type { get; set; }
        public string msg { get; set; }
        public string cameraId { get; set; }
        public string micId { get; set; }
        public bool alwaysPreview { get; set; }
        public bool audio { get; set; }
        public bool video { get; set; }

        // WebRTC signaling properties
        public string toConnectionId { get; set; }
        public object offer { get; set; }
        public object answer { get; set; }
        public object candidate { get; set; }
    }
}
