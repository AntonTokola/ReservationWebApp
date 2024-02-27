namespace VibrationMonitorReservation.Dtos.ItemControllerDtos
{
    public class GetAllCategoriesAndItemsDto
    {
        public string Category { get; set; }
        public List<ItemNameDto> listOfItems { get; set; }
    }
    public class ItemNameDto
    {
        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public string ImageURL { get; set; }
        public string ManualURL { get; set; }
    }
}
