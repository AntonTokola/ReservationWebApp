namespace VibrationMonitorReservation.Dtos.AccountControllerDtos
{
    public class UpdateUserDto
    {
        public string Id {get; set;}
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? IsStorageHandler { get; set; }
        public bool? IsAdmin { get; set; }
        
    }
}
