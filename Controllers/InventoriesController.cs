using System.Globalization;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    [Authorize] // must be logged in to create/edit; Index/Details allow anonymous explicitly
    public class InventoriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoriesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? q, int? categoryId, string? tag)
        {
            var query = _db.Inventories
                .Include(i => i.Category)
                .Include(i => i.InventoryTags).ThenInclude(it => it.Tag)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(i => i.Title.Contains(q));

            if (categoryId.HasValue)
                query = query.Where(i => i.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(tag))
                query = query.Where(i => i.InventoryTags.Any(t => t.Tag.Name == tag));

            var list = await query
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.AllTags = await _db.Tags.OrderBy(t => t.Name).ToListAsync();
            ViewBag.Query = q;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedTag = tag;

            return View(list);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var inv = await _db.Inventories
                .Include(i => i.Category)
                .Include(i => i.Fields.OrderBy(f => f.Order))
                .Include(i => i.Items).ThenInclude(it => it.Values).ThenInclude(v => v.Field)
                .Include(i => i.InventoryTags).ThenInclude(it => it.Tag)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inv == null) return NotFound();

            // Example aggregation: average of numeric fields (by name you selected)
            // We'll compute averages for *every* numeric field by FieldId.
            var numericFields = inv.Fields.Where(f => f.FieldType == FieldType.Number).ToList();
            var averages = new Dictionary<int, double?>();
            foreach (var nf in numericFields)
            {
                var vals = inv.Items
                    .SelectMany(it => it.Values)
                    .Where(v => v.FieldId == nf.Id && !string.IsNullOrWhiteSpace(v.Value))
                    .Select(v => double.TryParse(v.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : (double?)null)
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .ToList();

                averages[nf.Id] = vals.Count > 0 ? vals.Average() : null;
            }
            ViewBag.Averages = averages;

            return View(inv);
        }

        // [Authorize(Roles = "Admin,Creator,User")]
        [Authorize]   // any logged-in user can access

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(new InventoryCreateVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        // [Authorize(Roles = "Admin,Creator,User")]
        [Authorize]   // any logged-in user can access

        public async Task<IActionResult> Create(InventoryCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
                return View(vm);
            }

            var userId = _userManager.GetUserId(User)!;

            var inv = new Inventory
            {
                Title = vm.Title,
                CategoryId = vm.CategoryId,
                OwnerId = userId
            };

            foreach (var f in vm.Fields)
            {
                inv.Fields.Add(new InventoryField
                {
                    FieldName = f.FieldName.Trim(),
                    FieldType = f.FieldType,
                    IsVisible = f.IsVisible,
                    Order = f.Order
                });
            }

            // Tags (auto-create on first use)
            if (!string.IsNullOrWhiteSpace(vm.TagsCsv))
            {
                var names = vm.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                      .Select(x => x.ToLowerInvariant())
                                      .Distinct()
                                      .ToList();

                var existing = await _db.Tags.Where(t => names.Contains(t.Name)).ToListAsync();
                var toCreate = names.Except(existing.Select(e => e.Name))
                                    .Select(n => new Tag { Name = n })
                                    .ToList();

                _db.Tags.AddRange(toCreate);
                await _db.SaveChangesAsync();

                var all = existing.Concat(toCreate).ToList();
                foreach (var t in all)
                    inv.InventoryTags.Add(new InventoryTag { TagId = t.Id, Inventory = inv });
            }

            _db.Inventories.Add(inv);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = inv.Id });
        }

        // Only owner or admin can edit fields/title
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var inv = await _db.Inventories
                .Include(i => i.Fields.OrderBy(f => f.Order))
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inv == null) return NotFound();

            if (!User.IsInRole("Admin") && inv.OwnerId != _userManager.GetUserId(User))
                return Forbid();

            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(inv);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Inventory updated)
        {
            var inv = await _db.Inventories
                .Include(i => i.Fields)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inv == null) return NotFound();

            if (!User.IsInRole("Admin") && inv.OwnerId != _userManager.GetUserId(User))
                return Forbid();

            inv.Title = updated.Title;
            inv.CategoryId = updated.CategoryId;
            // Fields edits happen via separate endpoints to keep it simple (rename/add)

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var inv = await _db.Inventories.FindAsync(id);
            if (inv == null) return NotFound();

            if (!User.IsInRole("Admin") && inv.OwnerId != _userManager.GetUserId(User))
                return Forbid();

            _db.Inventories.Remove(inv); // cascade will delete fields/items/tags links
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Rename a field (demonstrates "Finger count" rename effect)
        [HttpPost, ValidateAntiForgeryToken]
[Authorize]
public async Task<IActionResult> RenameField(int fieldId, string newName)
{
    // 1. Validate newName
    if (string.IsNullOrWhiteSpace(newName))
    {
        TempData["Error"] = "Field name cannot be empty.";
        return RedirectToAction(nameof(Index)); // redirect to index if invalid
    }

    // 2. Find the field
    var field = await _db.InventoryFields
        .Include(f => f.Inventory)
        .FirstOrDefaultAsync(f => f.Id == fieldId);

    if (field == null)
    {
        TempData["Error"] = $"Field with ID {fieldId} not found.";
        return RedirectToAction(nameof(Index));
    }

    // 3. Check if parent inventory exists
    if (field.Inventory == null)
    {
        TempData["Error"] = "Parent inventory not found for this field.";
        return RedirectToAction(nameof(Index));
    }

    // 4. Authorization check
    var userId = _userManager.GetUserId(User);
    if (!User.IsInRole("Admin") && field.Inventory.OwnerId != userId)
    {
        return Forbid();
    }

    // 5. Safe assignment
    field.FieldName = newName.Trim();
    await _db.SaveChangesAsync();

    return RedirectToAction(nameof(Details), new { id = field.InventoryId });
}


        // Add new field
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddField(int inventoryId, string name, FieldType type, bool isVisible = true, int order = 0)
        {
            var inv = await _db.Inventories.FindAsync(inventoryId);
            if (inv == null) return NotFound();

            if (!User.IsInRole("Admin") && inv.OwnerId != _userManager.GetUserId(User))
                return Forbid();

            _db.InventoryFields.Add(new InventoryField
            {
                InventoryId = inventoryId,
                FieldName = name.Trim(),
                FieldType = type,
                IsVisible = isVisible,
                Order = order
            });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = inventoryId });
        }
    }
}
