using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingSignalRDto
{
    public class ParticipantInfo
    {
        public string ConnectionId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public bool camEnable { get; set; }
        public bool micEnable { get; set; }
    }
}
