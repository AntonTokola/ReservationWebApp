using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VibrationMonitorReservation.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using VibrationMonitorReservation.Services;
using VibrationMonitorReservation.Dtos.ReservationControllerDtos;

namespace VibrationMonitorReservation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    
    //ReservationController-luokka varausten luomista ja käsittelyä varten
    public class ReservationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        /// <summary>
        /// Hakee käyttäjän omat varaukset. [AUTHORIZED]
        /// </summary>
        /// <remarks>
        /// Vastaukset:
        /// - OK 200: Varaukset haettu onnistuneesti. Palauttaa listan käyttäjän omista varauksista.
        /// </remarks>
        // GET: api/reservations
        //Actionmetodi normaalitason käyttäjille, hakee käyttäjän omat varaukset
        [HttpGet]
            public async Task<ActionResult<IEnumerable<ReservationGetDto>>> GetUserReservations()
            {
                // Get the current user's ID
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Retrieve the reservations for the current user
                var reservations = await _context.Reservations
                    .Where(r => r.UserId == userId)
                    .ToListAsync();

                List<ReservationGetDto> reservationGetDtos = new List<ReservationGetDto>();

                //Hakee varatut hyllyt
                var shelves = _context.Shelf.Where(s => s.Available == false).ToList();

                foreach (var item in reservations)
                {
                    List<ReservationShelfGetDto> shelfList = new List<ReservationShelfGetDto>();
                    foreach (var shelf in shelves)
                    {
                        if (shelf.ReservationId == item.Id)
                        {
                            ReservationShelfGetDto s = new ReservationShelfGetDto();
                            s.ShelfId = shelf.ShelfId;
                            s.ReservationId = shelf.ReservationId;
                            s.Available = shelf.Available;
                            shelfList.Add(s);
                        }
                    
                    }

                    ReservationGetDto c = new ReservationGetDto
                    {
                        Id = item.Id,
                        IsActive = item.IsActive,
                        AdditionalInformation = item.AdditionalInformation,
                        PickupDate = item.PickupDate,
                        ProjectName = item.ProjectName,
                        Shelves = shelfList,
                        ReservationIsReady = item.ReservationIsReady,
                        ReservationIsReadyDate = item.ReservationIsReadyDate,
                        UserId = userId,
                        UserName = item.UserName,
                        FirstName = item.FirstName,
                        LastName = item.LastName
                    };
                    reservationGetDtos.Add(c);
                }

                // Retrieve the reservated items for the reservations
                foreach (var item in reservationGetDtos)
                {
                    var items = await _context.ReservatedItems
                        .Where(r => r.ReservationId == item.Id)
                        .ToListAsync();

                    if (items.Any())
                    {
                        List<ReservationItemGetDto> itemsList = new List<ReservationItemGetDto>();
                        foreach (var a in items)
                        {
                            ReservationItemGetDto reservationItemGetDto = new ReservationItemGetDto
                            {
                                ReservationId = a.ReservationId,
                                ItemName = a.ItemName,
                                ItemType = a.ItemType,
                                ItemSerialNumber = a.ItemSerialNumber
                            };
                            itemsList.Add(reservationItemGetDto);
                        }
                        item.Items = itemsList;
                    }
                }

                return Ok(reservationGetDtos);
            }

        /// <summary>
        /// Luo uuden varauksen käyttäjälle. [AUTHORIZED]
        /// </summary>
        /// <param name="reservationDto">Syötteenä uuden varauksen tiedot.</param>
        /// <remarks>
        /// Vastaukset:
        /// - OK 200: Varaus luotu onnistuneesti.
        /// - StatusCode 500: Tapahtui tietokantavirhe tai muu odottamaton virhe.
        /// </remarks>
        // POST: api/reservations
        //Actionmetodi normaalitason käyttäjille. Luo uuden varauksen käyttäjälle.
        [HttpPost]
            public async Task<IActionResult> CreateReservation([FromBody] ReservationPostDto reservationDto)
            {
                try
                {
                    // Get the current user
                    var user = await _userManager.GetUserAsync(User);

                    // Get the current user's ID and username
                    string userId = user.Id;
                    string username = user.UserName;
                    string firstname = user.FirstName;
                    string lastname = user.LastName;

                    // Create the items without the ReservationId
                    List<ReservatedItem> itemList = reservationDto.Items.Select(i => new ReservatedItem
                    {
                        ItemType = i.ItemType,
                        ItemName = i.ItemName,
                        // ReservationId is not set here
                    }).ToList();

                    var reservation = new Reservation
                    {
                        IsActive = true,
                        AdditionalInformation = reservationDto.AdditionalInformation,
                        PickupDate = reservationDto.PickupDate,
                        ReservationCreated = DateTime.Now,
                        ProjectName = reservationDto.ProjectName,
                        Items = itemList,
                        UserId = userId,
                        UserName = username,
                        FirstName = firstname,
                        LastName = lastname
                    };

                    // Add the reservation to the database
                    _context.Reservations.Add(reservation);
                    await _context.SaveChangesAsync();

                    //Sähköpostiviestien lähetys uudesta varauksesta kaikille varastokäsittelijä-tason käyttäjille
                    var toEmail = _context.Users
                        .Where(u => u.IsStorageHandler == true)
                        .Select(u => u.Email)
                        .ToList();

                    string items = "";
                    foreach (var item in reservation.Items)
                    {
                        items = (items + "\n" + item.ItemType + ", " + item.ItemName);
                    }

                    string emailBody = @$"Hei,

    Olet saanut uuden laitevarauksen henkilöltä {user.FirstName} {user.LastName}.

    Varauksen tiedot:

    Projektin/työmaan nimi: {reservation.ProjectName}
    Toivottu noutopäivämäärä: {reservation.PickupDate.ToString(("dddd"), new System.Globalization.CultureInfo("fi-FI"))} {reservation.PickupDate.ToString("dd.MM.yyyy")} klo: {reservation.PickupDate.ToString("HH:mm")}
    Varauksen lisätiedot: {reservation.AdditionalInformation}

    Varauksen sisältö: " + items + @$"

    Varaajan tiedot:
    Nimi: {user.FirstName} {user.LastName}
    Sähköpostiosoite: {user.Email}


    Ystävällisin terveisin - 

    VibrationMonitorReservation


    (varaus luotu: {(reservation.ReservationCreated.HasValue ? reservation.ReservationCreated.Value.ToString(("dddd"), new System.Globalization.CultureInfo("fi-FI")) : "")}na {(reservation.ReservationCreated.HasValue ? reservation.ReservationCreated.Value.ToString("dd.MM.yyyy HH:mm") : "Ei saatavilla")})
    ";


                    string emailProcessMessage = _emailService.SendEmail(toEmail, "Uusi varaus henkilöltä " + user.FirstName + " " + user.LastName, emailBody);

                    return Ok(new { Message = "Reservation created successfully. " + emailProcessMessage });
                }
                catch (DbUpdateException e)
                {
                    _logger.LogError(e, "A database error occurred while creating a reservation.");
                    // Return status 500 and error message for the enduser
                    return StatusCode(500, "A database error occurred. Please try again later.");
                }
                catch (Exception e) // Catch-all for other exceptions
                {
                    _logger.LogError(e, "An unexpected error occurred while creating a reservation.");
                    // Return status 500 and error message for the enduser
                    return StatusCode(500, "An unexpected error occurred. Please try again later.");
                }

            }

        /// <summary>
        /// Poistaa käyttäjän oman varauksen annetun ID:n perusteella. [AUTHORIZED]
        /// </summary>
        /// <param name="id">Varauksen tunniste</param>
        /// <remarks>
        /// Vastaukset:
        /// - OK 200: Varaus poistettu onnistuneesti.
        /// - NotFound 404: Varausta ei löydetty annetulla ID:llä.
        /// </remarks>
        /// 
        // DELETE: api/reservations
        //Delete-metodi normaalitason käyttäjille varausten poistamista varten
        [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteOwnReservation(int id)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Retrieve the reservations for the current user
                var reservations = await _context.Reservations
                    .Where(r => r.UserId == userId)
                    .ToListAsync();
                if (reservations == null)
                {
                    return NotFound(new { Message = "Reservation cannot be found with the given id" });
                }
                foreach (var item in reservations)
                {
                    if (item.Id == id)
                    {
                        _context.Reservations.Remove(item);
                        await _context.SaveChangesAsync();
                        return Ok(new { Message = "Reservation (id: " + id + " deleted successfully" });
                    }                
                }
                return NotFound();
            }

            /// <summary>
            /// Päivittää käyttäjän oman varauksen annetulla JSON-patch dokumentilla. [AUTHORIZED]
            /// </summary>
            /// <param name="id">Varauksen tunniste</param>
            /// <param name="patchDoc">JSON-patch dokumentti päivitykselle</param>
            /// <remarks>
            /// Vastaukset:
            /// - OK 200: Varaus päivitetty onnistuneesti.
            /// - BadRequest 400: Epäkelpo pyyntö tai JSON-patch dokumentti.
            /// - NotFound 404: Varausta ei löydetty annetulla ID:llä.
            /// </remarks>
            /// 
            // PATCH: api/reservations
            // PATCH-metodi normaalikäyttäjälle varauksen päivittämistä varten
            [HttpPatch("{id}")]
            [Consumes("application/json-patch+json")]
            public async Task<IActionResult> PatchOwnReservation(int id, [FromBody] JsonPatchDocument<Reservation> patchDoc)
            {
                if (patchDoc != null)
                {
                    // Get the current user's ID
                    string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    // Retrieve the reservations for the current user
                    var reservations = await _context.Reservations
                        .Where(r => r.UserId == userId)
                        .ToListAsync();

                    var reservation = await _context.Reservations.FindAsync(id);

                    foreach (var item in reservations)
                    {
                        if (item == reservation) {
                            patchDoc.ApplyTo(reservation, (error) =>
                            {
                                ModelState.AddModelError("", error.ErrorMessage);
                            });
                        }
                        else
                        {
                            return NotFound();
                        }
                    }

                    if (reservation == null)
                    {
                        return NotFound();
                    }
           

                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }

                    await _context.SaveChangesAsync();

                    return new ObjectResult(reservation);
                }
                else
                {
                    return BadRequest(ModelState);
                }
            }

            /// <summary>
            /// Hakee kaikki varaukset varastokäsittelijälle. [AUTHORIZED - STORAGEHANDLER]
            /// </summary>
            /// <remarks>
            /// Vastaukset:
            /// - OK 200: Varaukset haettu onnistuneesti.
            /// </remarks>
            /// // GET: api/reservations/admin
            //Action metodi varastokäsittelijä-tason käyttäjille. Hakee koko Reservation tietokannan sisällön. "GET ALL"
            [HttpGet("admin")]
            [Authorize(Policy = "IsStorageHandler")]
            public async Task<ActionResult<IEnumerable<Reservation>>> GetAllReservations()
            {
                var reservations = await _context.Reservations.ToListAsync();

                List<ReservationGetDto> reservationGetDtos = new List<ReservationGetDto>();

                var shelves = _context.Shelf.Where(s => s.Available == false).ToList();

                foreach (var item in reservations)
                {
                    List<ReservationShelfGetDto> shelfList = new List<ReservationShelfGetDto>();
                    foreach (var shelf in shelves)
                    {
                        if (shelf.ReservationId == item.Id)
                        {
                            ReservationShelfGetDto s = new ReservationShelfGetDto();
                            s.ShelfId = shelf.ShelfId;
                            s.ReservationId = shelf.ReservationId;
                            s.Available = shelf.Available;
                            shelfList.Add(s);
                        }
                    }

                    ReservationGetDto c = new ReservationGetDto
                    {
                        Id = item.Id,
                        IsActive = item.IsActive,
                        AdditionalInformation = item.AdditionalInformation,
                        PickupDate = item.PickupDate,
                        ProjectName = item.ProjectName,
                        Shelves = shelfList,
                        ReservationIsReady = item.ReservationIsReady,
                        ReservationIsReadyDate = item.ReservationIsReadyDate,
                        UserId = item.UserId,
                        UserName = item.UserName,
                        FirstName = item.FirstName,
                        LastName = item.LastName
                    };
                    reservationGetDtos.Add(c);
                }

                // Hakee varatut laitetyypit varauksista
                foreach (var item in reservationGetDtos)
                {
                    var items = await _context.ReservatedItems
                        .Where(r => r.ReservationId == item.Id)
                        .ToListAsync();

                    if (items.Any())
                    {
                        List<ReservationItemGetDto> itemsList = new List<ReservationItemGetDto>();
                        foreach (var a in items)
                        {
                            ReservationItemGetDto reservationItemGetDto = new ReservationItemGetDto
                            {
                                ReservationId = a.ReservationId,
                                ItemName = a.ItemName,
                                ItemType = a.ItemType,
                                ItemSerialNumber = a.ItemSerialNumber
                            };
                            itemsList.Add(reservationItemGetDto);
                        }
                        item.Items = itemsList;
                    }
                }

                return Ok(reservationGetDtos);
            }

            /// <summary>
            /// Poistaa varauksen annetun ID:n perusteella. [AUTHORIZED - ADMIN]
            /// </summary>
            /// <param name="id">Varauksen tunniste</param>
            /// <remarks>
            /// Toiminto admineille - mahdollisuus poistaa mikä tahansa varaus.
            /// 
            /// Vastaukset:
            /// - OK 200: Varaus poistettu onnistuneesti.
            /// - NotFound 404: Varausta ei löydetty annetulla ID:llä.
            /// </remarks>
            // DELETE: api/reservations/admin
            [HttpDelete("admin/{id}")]
            [Authorize(Policy = "IsAdmin")]
            public async Task<IActionResult> DeleteReservation(int id)
            {
                // Find the reservation by ID
                var reservation = await _context.Reservations.FindAsync(id);

                if (reservation == null)
	            {
                    // If the reservation does not exist, return a 404 Not Found status
                    return NotFound();
	            }
            // If the reservation does exist, remove it from the database
            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            // Return a 200 OK status
            return Ok(new { Message = "Reservation deleted successfully" });

            }

        /// <summary>
        /// Päivittää varauksen annetulla JSON-patch dokumentilla. [AUTHORIZED - ADMIN]
        /// </summary>
        /// <param name="id">Varauksen tunniste</param>
        /// <param name="patchDoc">JSON-patch dokumentti päivitykselle</param>
        /// <remarks>
        /// Toiminto adminille - päivittää minkä tahansa varauksen tiedot.
        /// 
        /// Vastaukset:
        /// - OK 200: Varaus päivitetty onnistuneesti.
        /// - BadRequest 400: Epäkelpo pyyntö tai JSON-patch dokumentti.
        /// - NotFound 404: Varausta ei löydetty annetulla ID:llä.
        /// /// </remarks>
        // PATCH: api/reservations/admin/{id}
        [HttpPatch("admin/{id}")]
            [Authorize(Policy = "IsAdmin")]
            [Consumes("application/json-patch+json")]
            public async Task<IActionResult> PatchReservation(int id, [FromBody] JsonPatchDocument<Reservation> patchDoc)
            {
                if (patchDoc != null)
                {
                    var reservation = await _context.Reservations.FindAsync(id);

                    if (reservation == null)
                    {
                        return NotFound();
                    }

                    patchDoc.ApplyTo(reservation, (error) =>
                    {
                        ModelState.AddModelError("", error.ErrorMessage);
                    });

                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }

                    await _context.SaveChangesAsync();

                    return new ObjectResult(reservation);
                }
                else
                {
                    return BadRequest(ModelState);
                }
            }


        }
    }



