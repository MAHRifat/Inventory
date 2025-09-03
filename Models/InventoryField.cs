using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Models
{
    public class InventoryField
    {
        public int Id { get; set; }

        public int InventoryId { get; set; }
        public Inventory Inventory { get; set; } = default!;

        [Required, MaxLength(120)]
        public string FieldName { get; set; } = "";

        public FieldType FieldType { get; set; } = FieldType.String;

        // Display only; author can toggle visibility
        public bool IsVisible { get; set; } = true;

        // Optional display order
        public int Order { get; set; } = 0;
    }
}
