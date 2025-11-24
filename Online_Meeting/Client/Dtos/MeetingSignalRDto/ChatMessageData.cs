using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingSignalRDto
{
    public class ChatMessageData
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
