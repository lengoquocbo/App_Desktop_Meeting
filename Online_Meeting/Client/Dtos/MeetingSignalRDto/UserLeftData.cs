using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingSignalRDto
{
    public class UserLeftData
    {
        public string ConnectionId { get; set; }
        public Guid UserId { get; set; }
    }
}
