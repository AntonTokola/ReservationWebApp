using VibrationMonitorReservation.Models;

namespace VibrationMonitorReservation.Dtos.ReservationControllerDtos
{
    //Reservation "POST" form
    public class ReservationPostDto
    {
        public ReservationPostDto()
        {
            Items = new List<ReservatedItemDto>(); // Alustetaan lista konstruktorissa
        }
        public string AdditionalInformation { get; set; }
        public DateTime PickupDate { get; set; }
        public string ProjectName { get; set; }
        public ICollection<ReservatedItemDto> Items { get; set; }
    }
}
