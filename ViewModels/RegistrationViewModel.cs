using Microsoft.AspNetCore.Hosting.Server;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace VibrationMonitorReservation.ViewModels
{
    public class RegistrationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        //[Required]
        //[DataType(DataType.Password)]
        //public string Password { get; set; }

        //[DataType(DataType.Password)]
        //[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        //public string ConfirmPassword { get; set; }

        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }

        public bool IsStorageHandler { get; set; }
        public bool IsAdmin { get; set; }
    }
}
