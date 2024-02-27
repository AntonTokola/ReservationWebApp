using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VibrationMonitorReservation.Dtos.StorageControllerDtos;
using VibrationMonitorReservation.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using VibrationMonitorReservation.Dtos.ReservationControllerDtos;
using VibrationMonitorReservation.Services;
using VibrationMonitorReservation.Dtos.ReservationControllerDtos.ReservationHandlerDtos;
using System.Security.Cryptography.X509Certificates;

namespace VibrationMonitorReservation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StorageController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;

        public StorageController(ApplicationDbContext dbContext, ILogger<StorageController> logger, UserManager<ApplicationUser> userManager, EmailService emailService)
        {
            _context = dbContext;
            _logger = logger;
            _userManager = userManager;
            _emailService = emailService;
        }
        /// <summary>
        /// Hakee kaikki varastossa olevat laitteet normaalitason käyttäjälle [AUTHORIZED]
        /// </summary>
        /// <remarks>
        /// Palauttaa kategorioidun luettelon kaikista laitteista "Storage"-taulusta normaali-tason käyttäjälle.
        /// Laitteet eivät sisällä käyttäjätietoja.
        /// 
        /// Vastaukset:
        /// - OK 200: Kaikki varastoon lisätyt laitteet palautettu onnistuneesti.
        /// - Exception: Jos tapahtuu odottamaton virhe.
        /// </remarks>
        [HttpGet("normalUser/getAllItemsFromStorage")]
        public async Task<ActionResult<List<NormalUserItemTypesDto>>> NormalUserGetAllItemsFromStorage()
        {
            try
            {
                var itemTypes = await _context.Storage
                    .Select(r => r.ItemType)
                    .Distinct()
                    .ToListAsync();

                var items = await _context.Storage.ToListAsync();
                List<NormalUserItemTypesDto> itemCategories = new List<NormalUserItemTypesDto>();

                foreach (var itemType in itemTypes)
                {
                    if (!itemCategories.Any(i => i.Category == itemType))
                    {
                        NormalUserItemTypesDto itemTypesDto = new NormalUserItemTypesDto
                        {
                            Category = itemType,
                            Items = new List<NormalUserGetItemsDto>()
                        };
                        itemCategories.Add(itemTypesDto);
                    }
                }

                foreach (var item in items)
                {
                    NormalUserGetItemsDto itemDto = new NormalUserGetItemsDto
                    {
                        ItemSerialNumber = item.ItemSerialNumber,
                        ItemName = item.ItemName,
                        ItemType = item.ItemType,
                        Available = item.Available,
                        State = item.State,
                        ProjectName = item.ProjectName,
                        AdditionalInformation = item.AdditionalInformation
                    };

                    var itemTypeDto = itemCategories.FirstOrDefault(i => i.Category == item.ItemType);
                    if (itemTypeDto != null)
                    {
                        itemTypeDto.Items.Add(itemDto);
                    }
                }
                return itemCategories;
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Hakee kaikki varastoon lisätyt laitteet varastokäsittelijälle [AUTHORIZED - STORAGEHANDLER]
        /// </summary>
        /// <remarks>
        /// Palauttaa kategorioidun luettelon kaikista laitteista "Storage"-taulusta varastokäsittelijälle.
        /// Laitteet sisältävät myös käyttäjätietoja.
        /// 
        /// Vastaukset:
        /// - OK 200: Kaikki varastossa olevat laitteet palautettu onnistuneesti.
        /// - StatusCode 500: Tapahtui odottamaton virhe. Yritä myöhemmin uudelleen.
        /// </remarks>
        // Return categorized list of all items from "Storage"-table.
        // GET: api/items
        [HttpGet("storageHandler/getAllItemsFromStorage")]
        [Authorize(Policy = "IsStorageHandler")]
        public async Task<ActionResult<List<StorageHandlerItemTypesDto>>> StorageHandlerGetAllItemsFromStorage()
        {
            try
            {
                var itemTypes = await _context.Storage
                .Select(r => r.ItemType)
                .Distinct()
                .ToListAsync();

                var items = await _context.Storage.ToListAsync();
                List<StorageHandlerItemTypesDto> itemCategories = new List<StorageHandlerItemTypesDto>();

                foreach (var itemType in itemTypes)
                {
                    if (!itemCategories.Any(i => i.Category == itemType))
                    {
                        StorageHandlerItemTypesDto itemTypesDto = new StorageHandlerItemTypesDto
                        {
                            Category = itemType,
                            Items = new List<StorageHandlerGetItemsDto>()
                        };
                        itemCategories.Add(itemTypesDto);
                    }
                }

                foreach (var item in items)
                {
                    StorageHandlerGetItemsDto itemDto = new StorageHandlerGetItemsDto
                    {
                        ItemSerialNumber = item.ItemSerialNumber,
                        ItemName = item.ItemName,
                        ItemType = item.ItemType,
                        Available = item.Available,
                        State = item.State,
                        ProjectName = item.ProjectName,
                        AddedToStorageDateTime = item.AddedToStorageDateTime,
                        AddedToStorageByUser = item.AddedToStorageByUser,
                        AdditionalInformation = item.AdditionalInformation
                    };

                    var itemTypeDto = itemCategories.FirstOrDefault(i => i.Category == item.ItemType);
                    if (itemTypeDto != null)
                    {
                        itemTypeDto.Items.Add(itemDto);
                    }
                }
                return itemCategories;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unexpected error occurred while getting all items from 'Storage'-table.");
                // Return status 500 and error message for the enduser
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }

        }
        /// <summary>
        /// Lisää uuden laitteen varastoon [AUTHORIZED - STORAGEHANDLER]
        /// </summary>
        /// <param name="item">Syötteenä uuden laitteen tiedot.</param>
        /// <remarks>
        /// Lisää uuden laitteen varastoon, uudelle laitteelle on määriteltävä olemassa oleva kategoria.
        /// 
        /// Vastaukset:
        /// - OK 200: Uusi laite lisätty onnistuneesti varastoon.
        /// - BadRequest 400: Tapahtui virhe laitteen lisäämisessä varastoon.
        /// </remarks>
        //POST: api/storage
        [HttpPost("storageHandler/addNewItemToStorage")]
        [Authorize(Policy = "IsStorageHandler")]
        public async Task<IActionResult> AddNewItemToStorage(PostItemDto item)
        {
            try
            {
                bool existWithSameSerialNumber = _context.Storage.Any(r => r.ItemSerialNumber == item.ItemSerialNumber && r.ItemName == item.ItemName && r.ItemType == item.ItemType);


                if (!existWithSameSerialNumber && item != null)
                {
                    Storage storageItem = new Storage();
                    storageItem.ItemType = item.ItemType;
                    storageItem.ItemName = item.ItemName;
                    storageItem.ItemSerialNumber = item.ItemSerialNumber;
                    storageItem.Available = true;
                    storageItem.State = "In the storage";
                    storageItem.ProjectName = "-";
                    storageItem.AddedToStorageDateTime = DateTime.Now;
                    storageItem.AddedToStorageByUser = User.FindFirstValue(ClaimTypes.Name);
                    storageItem.AdditionalInformation = item.AdditionalInformation;
                    storageItem.ReservationId = 0;

                    _context.Storage.Add(storageItem);
                    await _context.SaveChangesAsync();
                }

                return Ok(new  { Message = "New item '" + item.ItemName + "' added successfully to the storage" });
            }
            catch (Exception)
            {

                return BadRequest();
            }
            
        }

        /// <summary>
        /// Avoimen varauksen käsittely [AUTHORIZED - STORAGEHANDLER]
        /// </summary>
        /// <param name="reservationPostDto">Syötteenä tuleva varauskäsittelydata.</param>
        /// <remarks>
        /// Varauksen käsittely varastokäsittelijälle. Varauksen käsittely sisältää varastossa vapaana olevien sarjanumeroitujen laitteiden määrittelyn varaukseen.
        /// Sen lisäksi varaukselle määritellään hyllypaikan id, josta laitteet voidaan noutaa. Kun varaus on käsitelty, siitä lähetetään sähköpostiviesti varaajan tekijälle.
        /// 
        /// Vastaukset:
        /// - OK 200: Varauksen käsittely suoritettu onnistuneesti.
        /// - BadRequest 400: Varauksen käsittelyssä tapahtui virhe.
        /// - NotFound 404: Varausta ei löytynyt annetulla ID:llä.
        /// </remarks>
        // POST-metodi avoimien varausten käsittelyyn
        [HttpPost("storageHandler/handleReservation")]
        [Authorize(Policy = "IsStorageHandler")]
        public async Task<IActionResult> HandleReservation([FromBody] HandleReservationDto reservationPostDto)
        {
            try
            {
                // Etsii tietokannasta varauksen, jonka ID vastaa saapuvan pyynnön varauksen ID:tä
                var reservationDB = _context.Reservations
                .Where(r => r.Id == reservationPostDto.HR_Reservation.Id)
                .FirstOrDefault();

                // Varauksen tehneen henkilön alunperin varaamat laitteet (tämä lista sähköpostiviestiä varten)
                var OriginalItemOrderList = _context.ReservatedItems.Where(oi => oi.ReservationId == reservationPostDto.HR_Reservation.Id).ToList();

                var ShelvesDB = _context.Shelf.ToList();

                // Jos varausta ei löydy, palautetaan NotFound-vastaus
                if (reservationDB == null)
                {
                    return NotFound();
                }

                // Haetaan varauksen käsittelijän tiedot
                var storageHandlerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var storageHandlerUser = _context.Users
                .Where(u => u.Id == storageHandlerUserId)
                .FirstOrDefault();

                // Päivitetään varauksen tila 'valmiiksi' ja lisätään tarvittavat lisätiedot varaukseen
                reservationDB.ReservationIsReady = true;
                reservationDB.ReservationIsReadyDate = DateTime.Now;
                reservationDB.AdditionalInformationFromStorageHandler = reservationPostDto.HR_Reservation.AdditionalInformationFromStorageHandler;
                reservationDB.ReservationHandlerName = storageHandlerUser.FirstName + " " + storageHandlerUser.LastName;
                reservationDB.ReservationHandlerEmail = storageHandlerUser.Email;



                // Käydään läpi kaikki hyllyt, jotka varauksen käsittelijä on merkinnyt varaukseen.
                // Kyseisellä id:llä varustetut hyllyt merkitään käytössä oleviksi.
                // Myös varausten id:t lisätään hyllyihin
                foreach (var item in reservationPostDto.HR_Reservation.Shelves)
                {
                    foreach (var s in ShelvesDB)
                    {
                        if (item.ShelfId == s.ShelfId)
                        {
                            s.Available = false;
                            s.ReservationId = reservationPostDto.HR_Reservation.Id;
                        }
                    }
                }



                //Käsittelijän varaamat 'StorageItems'-listan laitteet päivitetään tietokantaan (ReservatedItems)

                //Varauksesta poistetaan tilaajan alunperin tilaamat laitteet
                var reservatedItemsDB = _context.ReservatedItems.Where(r => r.ReservationId == reservationDB.Id).ToList();
                if (reservatedItemsDB.Any())
                {
                    _context.ReservatedItems.RemoveRange(reservatedItemsDB);
                }

                //Varaukseen lisätään käsittelijän kuittaamat sarjanumerolliset laitteet
                List<ReservatedItem> reservatedItems = new List<ReservatedItem>();

                foreach (var item in reservationPostDto.HR_Reservation.StorageItems)
                {
                    foreach (var a in item.HR_StorageItemsDto)
                    {
                        ReservatedItem reservatedItem = new ReservatedItem();
                        reservatedItem.ReservationId = reservationDB.Id;
                        reservatedItem.ItemName = a.ItemName;
                        reservatedItem.ItemType = a.ItemType;
                        reservatedItem.ItemSerialNumber = a.ItemSerialNumber;
                        reservatedItems.Add(reservatedItem);
                    }
                }
                _context.ReservatedItems.AddRange(reservatedItems);



                // Luodaan kokoelma varatut varastotuotteet
                var reservatedStorageItems = reservationPostDto.HR_Reservation.StorageItems.ToList();

                List<Storage> reservatedStorageItemsDB = new List<Storage>();

                // Käydään läpi kaikki varatut varastotuotteet ja merkitään ne 'käytössä' tilaan
                foreach (var reservatedStorageItem in reservatedStorageItems)
                {
                    foreach (var StorageItem in reservatedStorageItem.HR_StorageItemsDto)
                    {
                        var storage = _context.Storage
                            .Where(s => s.ItemSerialNumber == StorageItem.ItemSerialNumber)
                            .FirstOrDefault();

                        storage.State = "In use";
                        storage.ReservationId = reservationPostDto.HR_Reservation.Id;
                        storage.Available = false;
                        storage.ProjectName = reservationDB.ProjectName;

                    }
                }


                // Tallennetaan muutokset tietokantaan
                _context.SaveChanges();

                // Haetaan varaajan tiedot sähköpostiviestiä varten
                var reservationUserId = reservationDB.UserId;
                var reservationUser = _context.Users.Where(te => te.Id == reservationUserId).FirstOrDefault();

                string OriginalOrderedItemsEmailList = "";
                foreach (var items in OriginalItemOrderList)
                {
                    OriginalOrderedItemsEmailList += "- " + Convert.ToString(items.ItemType) + " / " + Convert.ToString(items.ItemName) + "\n";    
                }

                // Luodaan string-muuttuja varattujen tuotteiden listalle sähköpostia varten
                string reservatedItemsEmailList = "";
                foreach (var items in reservationDB.Items)
                {
                    reservatedItemsEmailList += "- " + Convert.ToString(items.ItemType) + " / " + Convert.ToString(items.ItemName) + " / SN: " + Convert.ToString(items.ItemSerialNumber) + "\n";
                }

                //Hyllylokeroiden purku sähköpostiviestiin
                string shelves = "";
                foreach (var shelf in reservationDB.Shelves)
                {
                    shelves += shelf.ShelfId + "\n";
                }

                //Käsittelijän lisätiedot sähköpostiviestiin
                string additionalInformationFromStorageHandler = "";
                if (reservationDB.AdditionalInformationFromStorageHandler != null)
                {
                    additionalInformationFromStorageHandler = Convert.ToString(reservationPostDto.HR_Reservation.AdditionalInformationFromStorageHandler);
                }

                // Luodaan sähköpostiviesti käsitellyn varauksen tiedoilla
                string emailBody = @$"Hei, {reservationDB.FirstName} {reservationDB.LastName}

Varauksesi #{reservationDB.Id} / '{reservationDB.ProjectName}' on käsitelty.

# Varauksen tiedot: #
Varaus luotu: {(reservationDB.ReservationCreated.HasValue ? reservationDB.ReservationCreated.Value.ToString(("dddd"), new System.Globalization.CultureInfo("fi-FI")) : "")}na {(reservationDB.ReservationCreated.HasValue ? reservationDB.ReservationCreated.Value.ToString("dd.MM.yyyy HH:mm") : "Ei saatavilla")}
Varaajan tiedot: {reservationDB.FirstName} {reservationDB.LastName}
Projektin nimi: {reservationDB.ProjectName}
Toivottu noutopäivämäärä: {reservationDB.PickupDate.ToString(("dddd"), new System.Globalization.CultureInfo("fi-FI"))} {reservationDB.PickupDate.ToString("dd.MM.yyyy")} klo: {reservationDB.PickupDate.ToString("HH:mm")}
Antamasi lisätiedot varaukseen liittyen: {reservationDB.AdditionalInformation}

Tilaamasi laitteet:
{OriginalOrderedItemsEmailList}

# Varauksen noutotiedot: #
Varaus id: #{reservationDB.Id}
Käsittelypäivämäärä: {(reservationDB.ReservationIsReadyDate.HasValue ? reservationDB.ReservationIsReadyDate.Value.ToString(("dddd"), new System.Globalization.CultureInfo("fi-FI")) : "")} {(reservationDB.ReservationIsReadyDate.HasValue ? reservationDB.ReservationIsReadyDate.Value.ToString("dd.MM.yyyy HH:mm") : "Päivämäärä ei saatavilla")}
Varaus on noudettavissa hyllylokerosta: {shelves}Käsittelijän antamat lisätiedot tilausta koskien: '{additionalInformationFromStorageHandler}'
Käsittelijän yhteystiedot: {storageHandlerUser.Email}

Noudettavat laitteet:
{reservatedItemsEmailList} 

Ystävällisin terveisin - 


{storageHandlerUser.FirstName} {storageHandlerUser.LastName}
Käsittelijä


";

                List<string> toEmail = new List<string>();
                toEmail.Add(reservationUser.Email);

                try
                {
                    string emailProcessMessage = _emailService.SendEmail(toEmail, "Tilauksesi #" + reservationDB.Id + " / '" + reservationDB.ProjectName + "' on käsitelty.", emailBody);
                    // Palautetaan Ok-vastaus, jos toiminto suoritettiin onnistuneesti
                    return Ok("Reservation id: " + reservationDB.Id + " handled successfully. " + emailProcessMessage);
                }
                catch (Exception)
                {

                    return BadRequest("Reservation id: " + reservationDB.Id + " handled successfully. Unable to send email message.");
                }
                

                
                
            }
            catch (Exception)
            {
                // Palautetaan BadRequest-vastaus, jos toiminnossa tapahtuu virhe
                return BadRequest();
            }

        }
    }
}
