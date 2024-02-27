using VibrationMonitorReservation.Models;

namespace VibrationMonitorReservation.Dtos.ReservationControllerDtos.ReservationHandlerDtos
{
    public class HR_ReservationDto
    {
        public int Id { get; set; }
        public string? AdditionalInformationFromStorageHandler { get; set; }
        public List<HR_ShelfDto> Shelves { get; set; }

        public List<HR_StorageCategoriesDto> StorageItems { get; set; }

    }
}
