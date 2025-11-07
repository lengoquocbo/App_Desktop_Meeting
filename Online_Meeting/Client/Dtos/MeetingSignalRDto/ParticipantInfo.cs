using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingSignalRDto
{
    internal class ParticipantInfo
    {
        public string ConnectionId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }

    }
}
