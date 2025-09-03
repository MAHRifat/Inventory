using System.ComponentModel.DataAnnotations;
using InventoryApp.Models;

namespace InventoryApp.ViewModels
{
    public class InventoryCreateVm
    {
        [Required, MaxLength(140)]
        public string Title { get; set; } = "";

        public int? CategoryId { get; set; }

        // Comma-separated tags input like: tags: "blue,office,2024"
        public string? TagsCsv { get; set; }

        // Initial fields to create
        public List<FieldInput> Fields { get; set; } = new()
        {
            new FieldInput { FieldName = "Person Age", FieldType = FieldType.Number, IsVisible = true },
            new FieldInput { FieldName = "Person Name", FieldType = FieldType.String, IsVisible = true }
        };
    }

    public class FieldInput
    {
        [Required, MaxLength(120)]
        public string FieldName { get; set; } = "";
        public FieldType FieldType { get; set; } = FieldType.String;
        public bool IsVisible { get; set; } = true;
        public int Order { get; set; } = 0;
    }
}
