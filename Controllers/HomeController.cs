using System.Diagnostics;
using BarterSystem.Data;
using BarterSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarterSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId != null)
            {
                try
                {
                    // Get user info
                    var user = await _context.Users
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.DisplayName, u.Username })
                        .FirstOrDefaultAsync();

                    ViewBag.UserDisplayName = user?.DisplayName ?? user?.Username ?? "Trader";

                    // Get counts for dashboard
                    ViewBag.UserItemsCount = await _context.Items.CountAsync(i => i.OwnerId == userId);
                    ViewBag.ActiveTradesCount = await _context.Trades.CountAsync(t =>
                        (t.RequestedById == userId || t.RequestedToId == userId) && t.Status == "Pending");
                    ViewBag.CompletedTradesCount = await _context.Trades.CountAsync(t =>
                        (t.RequestedById == userId || t.RequestedToId == userId) && t.Status == "Completed");
                    ViewBag.PendingRequestsCount = await _context.TradeRequests.CountAsync(tr =>
                        tr.ReceiverId == userId && tr.Status == "Pending");

                    // Get recent items (excluding user's own items)
                    ViewBag.RecentItems = await _context.Items
                        .Include(i => i.Owner)
                        .Where(i => i.OwnerId != userId)
                        .OrderByDescending(i => i.CreatedAt)
                        .Take(6)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading dashboard data for user {UserId}", userId);
                    // Set default values
                    ViewBag.UserItemsCount = 0;
                    ViewBag.ActiveTradesCount = 0;
                    ViewBag.CompletedTradesCount = 0;
                    ViewBag.PendingRequestsCount = 0;
                    ViewBag.RecentItems = new List<Item>();
                    ViewBag.UserDisplayName = "Trader";
                }
            }

            return View();
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