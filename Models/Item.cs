namespace InventoryApp.Models
{
    public class Item
    {
        public int Id { get; set; }

        public int InventoryId { get; set; }
        public Inventory Inventory { get; set; } = default!;

        // All values live here:
        public ICollection<ItemField> Values { get; set; } = new List<ItemField>();
    }

    public class ItemField
    {
        public int Id { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; } = default!;

        public int FieldId { get; set; }
        public InventoryField Field { get; set; } = default!;

        // We keep values as string; parse when needed (e.g., avg)
        public string? Value { get; set; }
    }
}
