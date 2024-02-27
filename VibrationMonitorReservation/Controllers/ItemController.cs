using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VibrationMonitorReservation.Models;
using System.Security.Cryptography.X509Certificates;
using VibrationMonitorReservation.Dtos.ItemControllerDtos;
using VibrationMonitorReservation.Dtos.StorageControllerDtos;

namespace VibrationMonitorReservation.Controllers
{
    // Controller-luokka, joka käsittelee tietokannan tauluja 'ItemCategories' ja 'Items'.
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ItemController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;

        public ItemController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hakee kaikki kategoriat sekä niiden alla olevat laitteet - [AUTHORIZED]
        /// </summary>
        /// <remarks>
        /// Hakee kaikki kategoriat sekä niiden alaiset laitteet 'ItemCategories' ja 'Items' - tauluista 
        /// 
        /// Vastaukset:
        /// - OK 200: Kategorioiden ja laitteiden haku onnistui.
        /// - Internal Server Error 500: Operaatio epäonnistui.
        /// </remarks>
        // Hakee kaikki kategoriat sekä niiden alaiset laitteet 'ItemCategories' ja 'Items' -tauluista.
        [HttpGet("getAllCategoriesAndItems")]
        public async Task<ActionResult<List<GetAllCategoriesAndItemsDto>>> GetAllCategoriesAndItems()
        {
            try
            {
                // Hakee kaikki erilaiset kategoriat 'ItemCategories' -taulusta.
                var itemCategories = await _context.ItemCategories
                .Select(r => r.Category)
                .Distinct()
                .ToListAsync();

                // Hakee kaikki laitteet 'Items' -taulusta.
                var items = await _context.Items.ToListAsync();
                List<GetAllCategoriesAndItemsDto> ItemTypes = new List<GetAllCategoriesAndItemsDto>();

                // Luo uuden DTO-objektin jokaiselle kategorialle.
                foreach (var category in itemCategories)
                {
                    GetAllCategoriesAndItemsDto getAllCategoriesAndItemsDto = new GetAllCategoriesAndItemsDto();
                    getAllCategoriesAndItemsDto.Category = category;
                    getAllCategoriesAndItemsDto.listOfItems = new List<ItemNameDto>();
                    ItemTypes.Add(getAllCategoriesAndItemsDto);
                }

                // Lisää laitteet vastaavaan kategoriaan DTO-objektissa.
                foreach (var item in items) {
                    foreach (var category in ItemTypes)
                    {

                        if (item.ItemType == category.Category)
                        {
                            ItemNameDto itemNameDto = new ItemNameDto();
                            itemNameDto.ItemName = item.ItemName;
                            itemNameDto.ItemType = item.ItemType;
                            itemNameDto.ImageURL = item.ImageURL;
                            itemNameDto.ManualURL = item.ManualURL;
                            category.listOfItems.Add(itemNameDto);

                        }
                    }
                }

                // Palauttaa luodut DTO-objektit.
                return ItemTypes;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unexpected error occurred while getting all items and categories from 'Items' and 'ItemCategories'-tables.");
                // Return status 500 and error message for the enduser
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }

        }

        /// <summary>
        /// Luo uuden kategorian 'ItemCategories' -tauluun. [AUTHORIZED - ADMIN]
        /// </summary>
        /// <param name="createNewItemCategoryDto">Syötteenä uuden kategorian nimi.</param>
        /// <remarks>
        /// Vastaukset:
        /// - OK 200: Uuden kategorian luominen onnistui.
        /// - BadRequest 400: Kategoria on jo olemassa tai operaatio epäonnistuu muutoin.
        /// - StatusCode 500: Tapahtui tietokantavirhe tai muu odottamaton virhe.
        /// </remarks>
        // Luo uuden kategorian "ItemCategories"-tauluun
        // POST: api/items/newCategory
        [HttpPost("newCategory")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> CreateNewItemCategory([FromBody] CreateNewItemCategoryDto createNewItemCategoryDto)
        {
            try
            {
                ItemCategory itemCategory = new ItemCategory();
                itemCategory.Category = createNewItemCategoryDto.Category;

                // Tarkistaa, onko kategoria jo olemassa 'ItemCategories' -taulussa.
                bool categoryExist = _context.ItemCategories
                    .Any(i => i.Category == createNewItemCategoryDto.Category);

                if (categoryExist)
                {
                    return BadRequest("Category '" + createNewItemCategoryDto.Category + "' already exist");
                }

                // Lisää uuden kategorian tauluun.
                _context.ItemCategories.Add(itemCategory);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "New category '" + itemCategory.Category + "' created successfully. " });
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, "A database error occurred while creating new category.");
                // Palauttaa statuksen 500 loppukäyttäjälle
                return StatusCode(500, "A database error occurred. Please try again later.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unexpected error occurred while creating new category.");
                // Palauttaa statuksen 500 loppukäyttäjälle
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        /// <summary>
        /// Luo uuden laitteen 'Items' -tauluun. [AUTHORIZED - ADMIN]
        /// </summary>
        /// <param name="itemDto">Syötteenä uuden laitteen tiedot.</param>
        /// <remarks>
        /// Vastaukset:
        /// - OK 200: Uusi laite luotu onnistuneesti.
        /// - BadRequest 400: Laitenimi on jo olemassa tai kategoriaa ei löydy.
        /// - StatusCode 500: Tapahtui odottamaton virhe tai tietokantavirhe.
        /// </remarks>
        // Luo uuden laitteen 'Items' -tauluun.
        // POST: api/items/newItem
        [HttpPost("newItem")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> CreateNewItem([FromBody] CreateNewItemDto itemDto)
        {
            try
            {
                Item newItem = new Item();
                newItem.ItemName = itemDto.ItemName;
                newItem.ItemType = itemDto.ItemType;
                newItem.ImageURL = itemDto.PictureURL;
                newItem.ManualURL = itemDto.ManualURL;

                // Tarkistaa, onko kategoria tai laitteen nimi jo olemassa.
                bool categoryExist = _context.ItemCategories
                    .Any(i => i.Category == itemDto.ItemType);

                bool itemExist = _context.Items
                    .Any(j => j.ItemName == itemDto.ItemName);


                if (categoryExist && !itemExist)
                {
                    _context.Items.Add(newItem);
                    await _context.SaveChangesAsync();

                    return Ok(new { Message = "New item '" + itemDto.ItemName + "' created successfully. " });
                }

                string badRequest = "";

                if (!categoryExist) { badRequest = "Category '" + itemDto.ItemType + "' not found"; }

                if (itemExist) { badRequest = badRequest + " - Item name '" + itemDto.ItemName + "' is already in use."; }

                return BadRequest(badRequest);
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, "A database error occurred while creating new item.");
                // Return status 500 and error message for the enduser
                return StatusCode(500, "A database error occurred. Please try again later.");
            }
            catch (Exception e) // Catch-all for other exceptions
            {
                _logger.LogError(e, "An unexpected error occurred while creating new item.");
                // Return status 500 and error message for the enduser
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }

        }

        /// <summary>
        /// Poistaa laitteen 'Items' -taulusta. [AUTHORIZED - ADMIN]
        /// </summary>
        /// <param name="itemName">Laitteen nimi, joka halutaan poistaa.</param>
        /// <remarks>
        /// Vastaukset:
        /// - OK 200: Laite poistettu onnistuneesti.
        /// - NotFound 404: Laitetta ei löytynyt.
        /// - StatusCode 500: Tapahtui odottamaton virhe.
        /// </remarks>
        // Poistaa laitteen 'Items' -taulusta.
        [HttpDelete("deleteitem/{itemName}")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> DeleteItem(string itemName)
        {
            try
            {
                var item = await _context.Items
                .FirstOrDefaultAsync(i => i.ItemName == itemName);

                if (item == null) 
                {
                    return NotFound( new { Message = "Item '" + itemName + "' not found."});
                }
                else
                {
                    _context.Items.Remove(item);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { Message = "Item '" + itemName + "' deleted successfully" });
            }
            catch (Exception)
            {

                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }          

            
        }

        /// <summary>
        /// Poistaa kategorian 'ItemCategories' -taulusta ja kaikki kategorian alaisuudessa olevat itemit. [AUTHORIZED - ADMIN]
        /// </summary>
        /// <param name="category">Kategorian nimi, joka halutaan poistaa.</param>
        /// <remarks>
        /// Vastaukset:
        /// - OK 200: Kategoria ja sen alaiset itemit poistettu onnistuneesti.
        /// - NotFound 404: Kategoriaa ei löytynyt.
        /// - StatusCode 500: Tapahtui odottamaton virhe.
        /// </remarks>
        //Poistaa kategorian "ItemCategories"-taulusta. Kaikki kategorian alaisuudessa olevat itemit poistaan samalla.
        [HttpDelete("deletecategory/{category}")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> DeleteCategory(string category)
        {
            try
            {
                // Hakee valitun kategorian tietokannasta
                var selectedCategory = await _context.ItemCategories
                .FirstOrDefaultAsync(i => i.Category == category);

                // Hakee kaikki valitun kategorian itemit tietokannasta
                var selectedItems = await _context.Items
                    .Where(r => r.ItemType == category)
                    .ToListAsync();

                // Jos kategoriaa ei löydy, palauttaa NotFound-virheen
                if (selectedCategory == null)
                {
                    return NotFound("Category '" + category + "' not found");
                }
                else
                {
                    // Poistaa valitun kategorian tietokannasta
                    _context.ItemCategories.Remove(selectedCategory);

                    // Jos kategorialla on itemeja, poistetaan ne kaikki
                    if (selectedItems != null)
                        {
                            foreach (var item in selectedItems)
                            {
                                _context.Items.Remove(item);
                            }
                        }

                    // Tallentaa muutokset tietokantaan
                    await _context.SaveChangesAsync();
                }

                // Palauttaa viestin onnistuneesta poistosta
                return Ok(new { Message = "Category '" + selectedCategory.Category + "' and all its items deleted successfully" });
            }
            catch (Exception)
            {
                // Jos jokin menee pieleen, palauttaa yleisen virheviestin
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }


        }

    }
}



