using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // <-- use ApplicationDbContext

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index(string? q, int? categoryId, string? tag)
        {
            var inventories = _context.Inventories
                .Include(i => i.Category)
                .Include(i => i.InventoryTags)
                    .ThenInclude(it => it.Tag)
                .AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                inventories = inventories.Where(i => i.Title.Contains(q));
            }

            if (categoryId.HasValue)
            {
                inventories = inventories.Where(i => i.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(tag))
            {
                inventories = inventories.Where(i => i.InventoryTags.Any(it => it.Tag.Name == tag));
            }

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.AllTags = _context.Tags.ToList();
            ViewBag.Query = q;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedTag = tag;

            return View(inventories.ToList());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
