namespace VibrationMonitorReservation.Dtos.ShelfControllerDtos
{
    public class ShelfControllerGetShelfStatus
    {
        public string ShelfId { get; set; }
        public bool Available { get; set; }
        public string? ProjectName { get; set; }
        public string? PickUpDate { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserId { get; set; }
        public int? ReservationId { get; set; }
    }
}
