using Microsoft.AspNetCore.Identity;

namespace VibrationMonitorReservation.Models
{
    //  A class that inherits from IdentityUser and extends it with additional properties like FirstName and LastName.
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? IsAdmin { get; set; }
        public bool? IsStorageHandler { get; set; }
    }
}

