using Microsoft.AspNetCore.Identity;

namespace VibrationMonitorReservation.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? IsAdmin { get; set; }
        public bool? IsStorageHandler { get; set; }
        public bool? EmailsActivated { get; set; }
    }
}

