using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackHelper.Models
{
    public class PasswordEntry
    {
        public string Id { get; set; }
        public string ServiceName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Notes { get; set; }
        public string IconBase64 { get; set; }

        public PasswordEntry()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
