namespace VibrationMonitorReservation.Dtos.StorageControllerDtos
{
    public class StorageHandlerGetItemsDto
    {
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string? ItemSerialNumber { get; set; }
        public bool? Available { get; set; }
        public string? State { get; set; }
        public string? ProjectName { get; set; }
        public DateTime? AddedToStorageDateTime { get; set; }
        public string? AddedToStorageByUser { get; set; }
        public string? AdditionalInformation { get; set; }

    }

}
