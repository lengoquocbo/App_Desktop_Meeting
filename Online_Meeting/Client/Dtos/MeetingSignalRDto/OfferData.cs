using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingSignalRDto
{
    public class OfferData
    {
        public string FromConnectionId { get; set; }
        public Guid FromUserId { get; set; }
        public string FromUsername { get; set; }
        public object Offer { get; set; }
    }
}
