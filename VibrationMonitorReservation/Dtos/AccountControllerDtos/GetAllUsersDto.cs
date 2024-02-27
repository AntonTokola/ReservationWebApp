namespace VibrationMonitorReservation.Dtos.AccountControllerDtos
{
    public class GetAllUsersDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? IsAdmin { get; set; }
        public bool? IsStorageHandler { get; set; }
    }
}
