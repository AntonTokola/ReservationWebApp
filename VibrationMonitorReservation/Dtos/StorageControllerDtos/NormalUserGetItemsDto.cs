namespace VibrationMonitorReservation.Dtos.StorageControllerDtos
{
    public class NormalUserGetItemsDto
    {
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string? ItemSerialNumber { get; set; }
        public bool? Available { get; set; }
        public string? State { get; set; }
        public string? ProjectName { get; set; }
        public string? AdditionalInformation { get; set; }
    }
}
