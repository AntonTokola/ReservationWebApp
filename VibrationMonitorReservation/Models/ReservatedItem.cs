using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VibrationMonitorReservation.Models
{
    public class ReservatedItem
    {
        [Key]
        public int Id { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string? ItemSerialNumber {get; set; }
        public int ReservationId { get; set; }

        [ForeignKey("ReservationId")]
        public Reservation Reservation { get; set; }
    }
}
