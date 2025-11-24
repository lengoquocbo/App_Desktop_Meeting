using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingSignalRDto
{
    public class AnswerData
    {
        public string FromConnectionId { get; set; }
        public Guid FromUserId { get; set; }
        public string FromUsername { get; set; }
        public object Answer { get; set; }
    }
}
