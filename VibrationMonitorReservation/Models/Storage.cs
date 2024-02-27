using System.ComponentModel.DataAnnotations;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace VibrationMonitorReservation.Models
{
    [Table("Storage")]
    public class Storage
    {
        [Key]
        public int Id { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string? ItemSerialNumber { get; set; }
        public bool? Available { get; set; }
        public string? State { get; set; }
        public string? ProjectName { get; set; }
        public DateTime? AddedToStorageDateTime { get; set; }
        public string? AddedToStorageByUser { get; set; }
        public string? AdditionalInformation { get; set; }
        public int ReservationId { get; set; }

    }
}
