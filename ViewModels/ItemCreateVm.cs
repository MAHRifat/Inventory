using System.ComponentModel.DataAnnotations;

namespace InventoryApp.ViewModels
{
    public class ItemCreateVm
    {
        [Required]
        public int InventoryId { get; set; }

        // key: FieldId, value: user input
        public Dictionary<int, string?> Values { get; set; } = new();
    }
}
