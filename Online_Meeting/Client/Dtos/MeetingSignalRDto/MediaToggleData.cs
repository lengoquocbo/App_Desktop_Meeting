using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingSignalRDto
{
    internal class MediaToggleData
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public bool IsEnabled { get; set; }
    }
}
