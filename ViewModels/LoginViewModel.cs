using System.ComponentModel.DataAnnotations;

namespace VibrationMonitorReservation.ViewModels
{
    //A ViewModel class to hold the user's email, password, and a 'Remember me?' flag for login purposes. 
    //The ViewModel includes validation attributes to ensure email and password fields are required.
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
