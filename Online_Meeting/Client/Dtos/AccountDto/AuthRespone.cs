using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.AccountDto
{
    public class AuthResponse
    {
        public Guid userId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }


        // Optional: để thông báo UI cho dễ
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }
   
}