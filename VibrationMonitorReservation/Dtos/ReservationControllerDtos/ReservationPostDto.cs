using VibrationMonitorReservation.Models;

namespace VibrationMonitorReservation.Dtos.ReservationControllerDtos
{
    //Reservation "POST"
    public class ReservationPostDto
    {
        public ReservationPostDto()
        {
            Items = new List<ReservatedItemDto>();
        }
        public string AdditionalInformation { get; set; }
        public DateTime PickupDate { get; set; }
        public string ProjectName { get; set; }
        public ICollection<ReservatedItemDto> Items { get; set; }
    }
}
