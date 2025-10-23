using Microsoft.AspNetCore.Mvc;
using BarterSystem.Data;
using BarterSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BarterSystem.Controllers
{
    public class ItemController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ItemController> _logger;

        public ItemController(AppDbContext context, IWebHostEnvironment environment, ILogger<ItemController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // SHOW USER'S ITEMS ONLY
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["Error"] = "Please login to view your items";
                    return RedirectToAction("Login", "User");
                }

                var userItems = await _context.Items
                    .Include(i => i.Owner)
                    .Where(i => i.OwnerId == userId)
                    .OrderByDescending(i => i.Id)
                    .AsNoTracking()
                    .ToListAsync();

                ViewBag.TotalItems = userItems.Count;
                ViewBag.ImageCount = userItems.Count(i => !string.IsNullOrEmpty(i.ImageUrl) && i.ImageUrl != "/images/no-image.jpg");
                ViewBag.UploadPath = Path.Combine(_environment.WebRootPath, "images");

                return View(userItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user items");
                TempData["Error"] = "Error loading your items";
                return View(new List<Item>());
            }
        }

        // CREATE NEW ITEM - GET
        [HttpGet]
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["Error"] = "Please login to add items";
                return RedirectToAction("Login", "User");
            }
            return View(new Item());
        }

        // CREATE NEW ITEM - POST (SIMPLIFIED)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Item item, IFormFile? ImageFile)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["Error"] = "Please login to add items";
                    return RedirectToAction("Login", "User");
                }

                // BASIC VALIDATION
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    TempData["Error"] = "Item name is required";
                    return View(item);
                }

                // SET BASIC PROPERTIES
                item.OwnerId = userId.Value;
                item.CreatedAt = DateTime.Now;
                item.ImageUrl = "/images/no-image.jpg"; // Default image

                // HANDLE IMAGE UPLOAD
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images");

                    // Ensure directory exists
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    item.ImageUrl = "/images/" + uniqueFileName;
                }

                // SAVE TO DATABASE
                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Item created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item");
                TempData["Error"] = "Error creating item. Please try again.";
                return View(item);
            }
        }

        // BROWSE ALL ITEMS (EXCEPT USER'S OWN)
        [HttpGet]
        public async Task<IActionResult> All()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["Error"] = "Please login to browse items";
                    return RedirectToAction("Login", "User");
                }

                var allItems = await _context.Items
                    .Include(i => i.Owner)
                    .Where(i => i.OwnerId != userId)
                    .OrderByDescending(i => i.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();

                ViewBag.TotalItems = allItems.Count;
                return View(allItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all items");
                TempData["Error"] = "Error loading items";
                return View(new List<Item>());
            }
        }

        // ITEM DETAILS
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var item = await _context.Items
                    .Include(i => i.Owner)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (item == null)
                {
                    TempData["Error"] = "Item not found";
                    return RedirectToAction("All");
                }

                return View(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading item details for ID: {ItemId}", id);
                TempData["Error"] = "Error loading item details";
                return RedirectToAction("All");
            }
        }
    }
}