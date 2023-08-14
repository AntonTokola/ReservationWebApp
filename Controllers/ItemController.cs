using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VibrationMonitorReservation.Models;
using System.Security.Cryptography.X509Certificates;
using VibrationMonitorReservation.Dtos.ItemControllerDtos;
using VibrationMonitorReservation.Dtos.StorageControllerDtos;

namespace VibrationMonitorReservation.Controllers
{
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


        //Hakee kaikken lisättyjen laitteiden ja kategorioiden nimet 'ItemCategories' ja 'Items' - tauluista
        [HttpGet("getAllCategoriesAndItems")]
        public async Task<ActionResult<List<GetAllCategoriesAndItemsDto>>> GetAllCategoriesAndItems()
        {
            try
            {
                var itemCategories = await _context.ItemCategories
                .Select(r => r.Category)
                .Distinct()
                .ToListAsync();

                var items = await _context.Items.ToListAsync();
                List<GetAllCategoriesAndItemsDto> ItemTypes = new List<GetAllCategoriesAndItemsDto>();

                foreach (var category in itemCategories)
                {
                    GetAllCategoriesAndItemsDto getAllCategoriesAndItemsDto = new GetAllCategoriesAndItemsDto();
                    getAllCategoriesAndItemsDto.Category = category;
                    getAllCategoriesAndItemsDto.listOfItems = new List<ItemNameDto>();
                    ItemTypes.Add(getAllCategoriesAndItemsDto);
                }

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

                return ItemTypes;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unexpected error occurred while getting all items and categories from 'Items' and 'ItemCategories'-tables.");
                // Return status 500 and error message for the enduser
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }

        }
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

                bool categoryExist = _context.ItemCategories
                    .Any(i => i.Category == createNewItemCategoryDto.Category);

                if (categoryExist)
                {
                    return BadRequest("Category '" + createNewItemCategoryDto.Category + "' already exist");
                }


                _context.ItemCategories.Add(itemCategory);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "New category '" + itemCategory.Category + "' created successfully. " });
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, "A database error occurred while creating new category.");
                // Return status 500 and error message for the enduser
                return StatusCode(500, "A database error occurred. Please try again later.");
            }
            catch (Exception e) // Catch-all for other exceptions
            {
                _logger.LogError(e, "An unexpected error occurred while creating new category.");
                // Return status 500 and error message for the enduser
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        // Create new item to "Items"-table
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

        //Delete item from "Items"-table
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

        //Delete category from "ItemCategories"-table
        [HttpDelete("deletecategory/{category}")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> DeleteCategory(string category)
        {
            try
            {
                var selectedCategory = await _context.ItemCategories
                .FirstOrDefaultAsync(i => i.Category == category);

                var selectedItems = await _context.Items
                    .Where(r => r.ItemType == category)
                    .ToListAsync();

                

                if (selectedCategory == null)
                {
                    return NotFound("Category '" + category + "' not found");
                }
                else
                {
                    _context.ItemCategories.Remove(selectedCategory);

                        if (selectedItems != null)
                        {
                            foreach (var item in selectedItems)
                            {
                                _context.Items.Remove(item);
                            }
                        }
                    await _context.SaveChangesAsync();
                }

                return Ok(new { Message = "Category '" + selectedCategory.Category + "' and all its items deleted successfully" });
            }
            catch (Exception)
            {

                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }


        }

    }
}



