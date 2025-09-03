using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Models
{
    public class Inventory
    {
        public int Id { get; set; }

        [Required, MaxLength(140)]
        public string Title { get; set; } = "";

        // Lookup category (fixed list, no UI)
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        // Owner (the user who created the inventory)
        [Required]
        public string OwnerId { get; set; } = "";
        public ApplicationUser? Owner { get; set; }

        // Independent inventories: no links between inventories
        public ICollection<InventoryField> Fields { get; set; } = new List<InventoryField>();
        public ICollection<Item> Items { get; set; } = new List<Item>();
        public ICollection<InventoryTag> InventoryTags { get; set; } = new List<InventoryTag>();
    }

    public class InventoryTag
    {
        public int InventoryId { get; set; }
        public Inventory Inventory { get; set; } = default!;
        public int TagId { get; set; }
        public Tag Tag { get; set; } = default!;
    }
}
