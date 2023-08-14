namespace VibrationMonitorReservation.Dtos.StorageControllerDtos
{
    public class PostItemDto
    {
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string? ItemSerialNumber { get; set; }
        public string? AdditionalInformation { get; set; }
    }
}
