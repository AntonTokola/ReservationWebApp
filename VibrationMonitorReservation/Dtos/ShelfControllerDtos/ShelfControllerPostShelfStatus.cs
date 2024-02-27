namespace VibrationMonitorReservation.Dtos.ShelfControllerDtos
{
    public class ShelfControllerPostShelfStatus
    {
        public string ShelfId { get; set; }
        public bool Available { get; set; }
        public int ReservationId { get; set; }
    }
}
