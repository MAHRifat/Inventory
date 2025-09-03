using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required, MaxLength(60)]
        public string Name { get; set; } = "";
        
        public ICollection<InventoryTag> InventoryTags { get; set; } = new List<InventoryTag>();
    }
}
