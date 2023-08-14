using VibrationMonitorReservation.Models;

namespace VibrationMonitorReservation.Dtos.StorageControllerDtos
{
    public class StorageHandlerItemTypesDto
    {
        public string Category { get; set; }
        public List<StorageHandlerGetItemsDto> Items { get; set; }
    }
}
