using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetkusApplication.Models
{
    public class UserSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public TimeSpan? Duration => LogoutTime.HasValue ? LogoutTime - LoginTime : (TimeSpan?)null;
        public bool IsActive { get; set; }
    }
}
