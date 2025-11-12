using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Models
{
    public class ChatGroup
    {
        public Guid Id { get; set; }
        public string GroupKey { get; set; }
        public string GroupName { get; set; }
        public string Status { get; set; }

    }
}
