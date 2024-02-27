using VibrationMonitorReservation.Models;

namespace VibrationMonitorReservation.Services
{
    public class ShelfGenerator
    {
        //Räätälöity palvelu varaushyllyjä varten
        public void createHardCodedShelves(ApplicationDbContext context)
        {
            List<string> shelfIDs = new List<string> { "A1", "A2", "A3", "B4", "B5", "B6", "C7", "C8", "C9", "D10", "D11", "D12", "E13", "E14", "E15" };

            // Loops through each id in the list
            foreach (var id in shelfIDs)
            {
                // If the id does not exist in the Shelf table, add a new Shelf with the id
                if (!context.Shelf.Any(s => s.ShelfId == id))
                {
                    // Assuming you have a Shelf class that takes a ShelfId as a constructor argument
                    context.Shelf.Add(new Shelf(id, true, null));
                }
            }

            // Loop through each shelf in the table
            foreach (var shelf in context.Shelf)
            {
                // If the shelf's id is not in the list of allowed ids, remove it
                if (!shelfIDs.Contains(shelf.ShelfId))
                {
                    context.Shelf.Remove(shelf);
                }
            }

            // Save the changes to the database
            context.SaveChanges();
        }
    }
}
