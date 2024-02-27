using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VibrationMonitorReservation.Dtos.ShelfControllerDtos;
using VibrationMonitorReservation.Models;
using VibrationMonitorReservation.Services;

namespace VibrationMonitorReservation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    
    public class ShelfController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ShelfGenerator _temporaryServices;

        public ShelfController(ApplicationDbContext context, ShelfGenerator temporaryServices)
        {
            _context = context;
            _temporaryServices = temporaryServices;
        }

        /// <summary>
        /// Hakee kaikkien hyllyjen statuksen [AUTHORIZED - STORAGEHANDLER]
        /// </summary>
        /// <remarks>
        /// Vastaukset:
        /// - OK 200: Hyllyjen tiedot haettu onnistuneesti.
        /// - BadRequest 400: Virhe haettaessa hyllyjen tietoja.
        /// </remarks>
        [HttpGet]
        [Authorize(Policy = "IsStorageHandler")]
        public async Task<ActionResult<IEnumerable<ShelfControllerGetShelfStatus>>> GetShelfStatus()
        {
            try
            {
                //Tarkistaa ovatko kovakoodatut hylly-id:t olemassa (15kpl)
                //Jos ei, niin luodaan uudet. Jos hyllyjä on liikaa, ne poistetaan.
                _temporaryServices.createHardCodedShelves(_context);

                List<ShelfControllerGetShelfStatus> Shelves = new List<ShelfControllerGetShelfStatus>();
                var shelves = await _context.Shelf.ToListAsync();

                //Kaikki hyllyt käydään läpi, ja niihin sijoitetaan mahdollisten noudettavien varausten tiedot
                foreach (var shelf in shelves)
                {
                    var reservation = await _context.Reservations.Where(r => r.Id == shelf.ReservationId).FirstOrDefaultAsync();

                        ShelfControllerGetShelfStatus shelfItem = new ShelfControllerGetShelfStatus();
                        shelfItem.ShelfId = shelf.ShelfId;
                        shelfItem.ReservationId = shelf.ReservationId;
                        shelfItem.Available = shelf.Available;

                        //Jos varaus on olemassa, siitä lisätään tarvittavat tiedot hyllyä varten
                        if (reservation != null)
                        {
                            shelfItem.ProjectName = reservation.ProjectName;
                            shelfItem.PickUpDate = reservation.PickupDate.ToString("dd.MM.yyyy HH:mm");
                            shelfItem.FirstName = reservation.FirstName;
                            shelfItem.LastName = reservation.LastName;
                            shelfItem.UserId = reservation.UserId;
                        }
                        
                        Shelves.Add(shelfItem);

                }

                return Ok(Shelves);
            }
            catch (Exception)
            {

                return BadRequest("Error retrieving shelf information");
            }
            
        }

        //Uuden hylly-id:n lisääminen - kommentoituna mahdollista jatkoa varten.
        //[HttpPost]
        //public async Task<IActionResult> CreateNewShelfID([FromBody] ShelfControllerPostShelfStatus shelfControllerPostShelfStatus)
        //{
        //    if (shelfControllerPostShelfStatus != null)
        //    {
        //        using (var transaction = _context.Database.BeginTransaction())
        //        {
        //            try
        //            {
        //                var shelvesDB = await _context.Shelf.ToListAsync();

        //                Shelf shelf = new Shelf();
        //                shelf.ShelfId = shelfControllerPostShelfStatus.ShelfId;
        //                shelf.ReservationId = null;
        //                shelf.Available = true;
        //                shelvesDB.Add(shelf);
        //                await _context.SaveChangesAsync();

        //                transaction.Commit();
        //                return Ok();
        //            }
        //            catch (Exception)
        //            {
        //                transaction.Rollback();
        //                return BadRequest();
        //            }
        //        }
        //    }
        //    return BadRequest();
        //}


    }
}
