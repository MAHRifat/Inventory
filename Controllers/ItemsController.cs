using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    [Authorize] // must be logged in to add items
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ItemsController(ApplicationDbContext db) { _db = db; }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemCreateVm vm)
        {
            var inv = await _db.Inventories
                .Include(i => i.Fields)
                .FirstOrDefaultAsync(i => i.Id == vm.InventoryId);

            if (inv == null) return NotFound();

            var item = new Item { InventoryId = inv.Id };
            _db.Items.Add(item);
            await _db.SaveChangesAsync(); // get item.Id

            // Create values for any field keys present
            foreach (var field in inv.Fields)
            {
                vm.Values.TryGetValue(field.Id, out var raw);
                _db.ItemFields.Add(new ItemField
                {
                    ItemId = item.Id,
                    FieldId = field.Id,
                    Value = raw
                });
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Details", "Inventories", new { id = inv.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Items.FindAsync(id);
            if (item == null) return NotFound();

            var invId = item.InventoryId;
            _db.Items.Remove(item); // cascade delete values
            await _db.SaveChangesAsync();
            return RedirectToAction("Details", "Inventories", new { id = invId });
        }
    }
}
