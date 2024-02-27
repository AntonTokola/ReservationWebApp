using VibrationMonitorReservation.Models;

namespace VibrationMonitorReservation.Dtos.ReservationControllerDtos
{
    public class ReservationShelfGetDto
    {
        public string ShelfId { get; set; }
        public bool Available { get; set; }
        public int? ReservationId { get; set; }
    }
    public class ReservationItemGetDto
    {
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string? ItemSerialNumber { get; set; }
        public int ReservationId { get; set; }
    }
    public class ReservationGetDto
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } // Onko varaus aktiivisena / true (tämä muuttuu kun admin on käsitellty varauksen, ja varauksen tekijä on merkinnyt varauksen noudetuksi)
        public string AdditionalInformation { get; set; } //Varauksen lisätiedot (varauksen tekijä täyttää)
        public DateTime PickupDate { get; set; } // Varauksen toivottu noutopäivä (varauksen tekijä täyttää)
        public string ProjectName { get; set; } // Projektin nimi jolle laitteet varataan (varauksen tekijä täyttää)
        public ICollection<ReservationShelfGetDto> Shelves { get; set; } // Hylly ID (admin täyttää)
        public bool? ReservationIsReady { get; set; }  // Varaus on käsitelty (admin täyttää)
        public DateTime? ReservationIsReadyDate { get; set; } // Varauksen käsittelyn ajankohta (tämä muuttuu kun admin on käsitellyt varauksen)

        public List<ReservationItemGetDto> Items { get; set; }

        // Käyttäjätiedot haetaan aspnetusers-taulukosta
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }


}
