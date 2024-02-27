using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Internal;

namespace VibrationMonitorReservation.Models
{

//Kontekstiluokka tietokannalle
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Storage> Storage { get; set; }

        public DbSet<ReservatedItem> ReservatedItems { get; set;}
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemCategory> ItemCategories { get; set; }
        public DbSet<Shelf> Shelf { get; set; }
    }
}
