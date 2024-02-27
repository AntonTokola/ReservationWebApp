namespace VibrationMonitorReservation.ViewModels
{
    public class UserInfoViewModel
    {
        public string Id { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool? IsStoragehandler { get; set; }
        public bool? IsAdmin { get; set; }
        public bool? EmailsActivated { get; set;}
    }
}
