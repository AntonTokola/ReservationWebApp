using System.ComponentModel.DataAnnotations;

namespace VibrationMonitorReservation.Models
{
    public class Item
    {
        [Key]
        public int Id { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string? ImageURL { get; set; }
        public string? ManualURL { get; set; }
    }
}
