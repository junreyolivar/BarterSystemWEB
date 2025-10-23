using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BarterSystem.Data;
using BarterSystem.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarterSystem.Controllers
{
    public class TradeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TradeController> _logger;

        public TradeController(AppDbContext context, ILogger<TradeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // CREATE TRADE - GET
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["Error"] = "Please login to trade";
                    return RedirectToAction("Login", "User");
                }

                // Get user's items
                var myItems = await _context.Items
                    .Where(i => i.OwnerId == userId)
                    .ToListAsync();

                // Get other users' items
                var otherItems = await _context.Items
                    .Include(i => i.Owner)
                    .Where(i => i.OwnerId != userId)
                    .Take(50)
                    .ToListAsync();

                // Get other users
                var users = await _context.Users
                    .Where(u => u.Id != userId)
                    .Take(100)
                    .ToListAsync();

                if (myItems.Count == 0)
                {
                    TempData["Warning"] = "You need to add items first before you can trade.";
                    return RedirectToAction("Create", "Item");
                }

                if (otherItems.Count == 0)
                {
                    TempData["Warning"] = "No items available for trading at the moment.";
                    return RedirectToAction("All", "Item");
                }

                ViewBag.MyItems = myItems;
                ViewBag.OtherItems = otherItems;
                ViewBag.Users = users;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Trade Create GET method");
                TempData["Error"] = "An error occurred while loading trade data.";
                return RedirectToAction("Index", "Home");
            }
        }

        // CREATE TRADE - POST (SIMPLIFIED)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int requestedToId, int offeredItemId, int requestedItemId)
        {
            try
            {
                var requestedById = HttpContext.Session.GetInt32("UserId");
                if (requestedById == null)
                {
                    TempData["Error"] = "Please login to create trades";
                    return RedirectToAction("Login", "User");
                }

                // BASIC VALIDATION
                if (requestedToId <= 0 || offeredItemId <= 0 || requestedItemId <= 0)
                {
                    TempData["Error"] = "Please select all required fields";
                    return RedirectToAction("Create");
                }

                if (requestedToId == requestedById)
                {
                    TempData["Error"] = "You cannot trade with yourself";
                    return RedirectToAction("Create");
                }

                // SIMPLIFIED ITEM VALIDATION
                var offeredItem = await _context.Items.FindAsync(offeredItemId);
                var requestedItem = await _context.Items.FindAsync(requestedItemId);

                if (offeredItem == null || requestedItem == null)
                {
                    TempData["Error"] = "One or more items not found";
                    return RedirectToAction("Create");
                }

                if (offeredItem.OwnerId != requestedById)
                {
                    TempData["Error"] = "You can only offer items that you own";
                    return RedirectToAction("Create");
                }

                if (requestedItem.OwnerId == requestedById)
                {
                    TempData["Error"] = "You cannot request your own item";
                    return RedirectToAction("Create");
                }

                // CREATE TRADE REQUEST INSTEAD OF TRADE
                var tradeRequest = new TradeRequest
                {
                    RequesterId = requestedById.Value,
                    ReceiverId = requestedToId,
                    OfferedItemId = offeredItemId,
                    RequestedItemId = requestedItemId,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.TradeRequests.Add(tradeRequest);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Trade request sent successfully!";
                return RedirectToAction("Pending", "TradeRequest"); // ✅ CHANGE TO TRADEREQUEST PENDING
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trade");
                TempData["Error"] = "Error creating trade. Please try again.";
                return RedirectToAction("Create");
            }
        }

        // VIEW ALL TRADES
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["Error"] = "Please login to view trades";
                    return RedirectToAction("Login", "User");
                }

                var trades = await _context.Trades
                    .Include(t => t.RequestedBy)
                    .Include(t => t.RequestedTo)
                    .Include(t => t.TradeItems)
                        .ThenInclude(ti => ti.Item)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return View(trades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Trade Index method");
                TempData["Error"] = "An error occurred while loading trades.";
                return View(new List<Trade>());
            }
        }

        // APPROVE TRADE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["Error"] = "Please login to approve trades";
                    return RedirectToAction("Login", "User");
                }

                var trade = await _context.Trades
                    .Include(t => t.TradeItems)
                        .ThenInclude(ti => ti.Item)
                    .FirstOrDefaultAsync(t => t.Id == id && t.RequestedToId == userId && t.Status == "Pending");

                if (trade == null)
                {
                    TempData["Error"] = "Trade not found or already processed";
                    return RedirectToAction("Index");
                }

                // GET ITEMS
                var offeredItem = trade.TradeItems.FirstOrDefault(ti => ti.IsOfferedItem)?.Item;
                var requestedItem = trade.TradeItems.FirstOrDefault(ti => !ti.IsOfferedItem)?.Item;

                if (offeredItem == null || requestedItem == null)
                {
                    TempData["Error"] = "Trade items not found";
                    trade.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }

                // SWAP OWNERSHIP
                var tempOwnerId = offeredItem.OwnerId;
                offeredItem.OwnerId = requestedItem.OwnerId;
                requestedItem.OwnerId = tempOwnerId;

                trade.Status = "Completed";
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Trade completed! You received {offeredItem.Name}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Trade Approve method for trade {TradeId}", id);
                TempData["Error"] = "An error occurred while approving the trade.";
            }

            return RedirectToAction("Index");
        }

        // CANCEL TRADE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["Error"] = "Please login to cancel trades";
                    return RedirectToAction("Login", "User");
                }

                var trade = await _context.Trades
                    .FirstOrDefaultAsync(t => t.Id == id && (t.RequestedById == userId || t.RequestedToId == userId) && t.Status == "Pending");

                if (trade == null)
                {
                    TempData["Error"] = "Trade not found or already processed";
                    return RedirectToAction("Index");
                }

                trade.Status = "Cancelled";
                await _context.SaveChangesAsync();

                TempData["Info"] = "Trade has been cancelled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Trade Cancel method for trade {TradeId}", id);
                TempData["Error"] = "An error occurred while cancelling the trade.";
            }

            return RedirectToAction("Index");
        }

        // TRADE HISTORY
        public async Task<IActionResult> TradeHistory()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["Error"] = "Please login to view trade history";
                    return RedirectToAction("Login", "User");
                }

                var trades = await _context.Trades
                    .Include(t => t.RequestedBy)
                    .Include(t => t.RequestedTo)
                    .Include(t => t.TradeItems)
                        .ThenInclude(ti => ti.Item)
                    .Where(t => (t.RequestedById == userId || t.RequestedToId == userId) && t.Status == "Completed")
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return View(trades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Trade History method");
                TempData["Error"] = "An error occurred while loading trade history.";
                return View(new List<Trade>());
            }
        }
    }
}