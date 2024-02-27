using System.ComponentModel.DataAnnotations;

namespace VibrationMonitorReservation.Models
{
    public class ItemCategory
    {
        [Key]
        public int Id { get; set; }
        public string Category { get; set; }
    }
}
