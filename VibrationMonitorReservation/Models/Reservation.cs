using Org.BouncyCastle.Asn1.Mozilla;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VibrationMonitorReservation.Dtos;

namespace VibrationMonitorReservation.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; } //Taulukon "Key" id. Rullaava numero     
        public bool IsActive { get; set; } // Onko varaus aktiivisena / true (tämä muuttuu kun admin on käsitellty varauksen, ja varauksen tekijä on merkinnyt varauksen noudetuksi)
        public string AdditionalInformation { get; set; } //Varauksen lisätiedot (varauksen tekijä täyttää)
        public DateTime PickupDate { get; set; } // Varauksen toivottu noutopäivä (varauksen tekijä täyttää)
        public DateTime? ReservationCreated { get; set; }
        public string ProjectName { get; set; } // Projektin nimi jolle laitteet varataan (varauksen tekijä täyttää)
        public ICollection<Shelf> Shelves { get; set; }
        public bool ReservationIsReady { get; set; }  // Varaus on käsitelty (admin täyttää)
        public DateTime? ReservationIsReadyDate { get; set; } // Varauksen käsittelyn ajankohta (tämä muuttuu kun admin on käsitellyt varauksen)
        public string? AdditionalInformationFromStorageHandler { get; set; }
        public string? ReservationHandlerName { get; set; }
        public string? ReservationHandlerEmail { get; set; }
        public ICollection<ReservatedItem> Items { get; set; }

        // ForeignKey to reference AspNetUsers - käyttäjätiedot haetaan aspnetusers-taulukosta
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
