using System.ComponentModel.DataAnnotations.Schema;
using VibrationMonitorReservation.Dtos.ReservationControllerDtos;


namespace VibrationMonitorReservation.Models
{
    public class Shelf
    {
       
        public string ShelfId { get; set; }
        public bool Available { get; set; }
        public int? ReservationId { get; set; }
        [ForeignKey("ReservationId")]
        public Reservation reservation = new Reservation();

        public Shelf() { }
        public Shelf(string shelfId, bool available, int? reservationId)
        {
            ShelfId = shelfId;
            Available = available;
            ReservationId = reservationId;
        }

    }
}
