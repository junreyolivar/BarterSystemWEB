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
    public class TradeRequestController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TradeRequestController> _logger;

        public TradeRequestController(AppDbContext context, ILogger<TradeRequestController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // SEARCH USERS
        [HttpGet]
        public async Task<IActionResult> Search(string? query)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to Search");
                    return RedirectToAction("Login", "User");
                }

                IQueryable<User> usersQuery = _context.Users.Where(u => u.Id != userId);

                if (!string.IsNullOrEmpty(query))
                {
                    usersQuery = usersQuery.Where(u =>
                        u.Username.Contains(query) ||
                        u.DisplayName.Contains(query));
                }

                var users = await usersQuery.ToListAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Search method");
                TempData["Error"] = "An error occurred while searching for users.";
                return View(new List<User>());
            }
        }

        // VIEW USER'S ITEMS FOR TRADING
        [HttpGet]
        public async Task<IActionResult> UserItems(int id)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (currentUserId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to UserItems");
                    return RedirectToAction("Login", "User");
                }

                var user = await _context.Users
                    .Include(u => u.Items)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Search");
                }

                // Get current user's items for trading
                var myItems = await _context.Items
                    .Where(i => i.OwnerId == currentUserId)
                    .ToListAsync();

                if (myItems.Count == 0)
                {
                    TempData["Warning"] = "You need to add items first before you can trade.";
                    return RedirectToAction("Create", "Item");
                }

                ViewBag.MyItems = myItems;
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UserItems method for user ID {UserId}", id);
                TempData["Error"] = "An error occurred while loading user items.";
                return RedirectToAction("Search");
            }
        }

        // SEND TRADE REQUEST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(int receiverId, int offeredItemId, int requestedItemId)
        {
            try
            {
                var requesterId = HttpContext.Session.GetInt32("UserId");
                if (requesterId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to SendRequest");
                    return RedirectToAction("Login", "User");
                }

                // Basic validation
                if (receiverId <= 0 || offeredItemId <= 0 || requestedItemId <= 0)
                {
                    TempData["Error"] = "Invalid trade request parameters.";
                    return RedirectToAction("Search");
                }

                // Validate items exist and belong to correct users
                var offeredItem = await _context.Items
                    .Include(i => i.Owner)
                    .FirstOrDefaultAsync(i => i.Id == offeredItemId && i.OwnerId == requesterId);

                var requestedItem = await _context.Items
                    .Include(i => i.Owner)
                    .FirstOrDefaultAsync(i => i.Id == requestedItemId && i.OwnerId == receiverId);

                if (offeredItem == null)
                {
                    TempData["Error"] = "The item you're offering was not found or doesn't belong to you.";
                    return RedirectToAction("Search");
                }

                if (requestedItem == null)
                {
                    TempData["Error"] = "The requested item was not found or is no longer available.";
                    return RedirectToAction("Search");
                }

                // Check if trade request already exists
                var existingRequest = await _context.TradeRequests
                    .FirstOrDefaultAsync(tr =>
                        tr.RequesterId == requesterId &&
                        tr.ReceiverId == receiverId &&
                        tr.OfferedItemId == offeredItemId &&
                        tr.RequestedItemId == requestedItemId &&
                        tr.Status == "Pending");

                if (existingRequest != null)
                {
                    TempData["Warning"] = "You already have a pending trade request for these items.";
                    return RedirectToAction("Pending");
                }

                // Create new trade request
                var tradeRequest = new TradeRequest
                {
                    RequesterId = requesterId.Value,
                    ReceiverId = receiverId,
                    OfferedItemId = offeredItemId,
                    RequestedItemId = requestedItemId,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.TradeRequests.Add(tradeRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Trade request created: {TradeRequestId} from {RequesterId} to {ReceiverId}",
                    tradeRequest.Id, requesterId, receiverId);

                TempData["Success"] = $"Trade request sent successfully to {requestedItem.Owner.Username}!";
                return RedirectToAction("Pending");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendRequest method");
                TempData["Error"] = "An error occurred while sending the trade request.";
                return RedirectToAction("Search");
            }
        }

        // PENDING REQUESTS
        [HttpGet]
        public async Task<IActionResult> Pending()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to Pending");
                    return RedirectToAction("Login", "User");
                }

                // Get incoming requests (requests sent to you) - BOTH TradeRequest AND Trade
                var incomingTradeRequests = await _context.TradeRequests
                    .Include(tr => tr.Requester)
                    .Include(tr => tr.OfferedItem)
                    .Include(tr => tr.RequestedItem)
                    .Where(tr => tr.ReceiverId == userId && tr.Status == "Pending")
                    .OrderByDescending(tr => tr.CreatedAt)
                    .ToListAsync();

                // Get incoming trades (trades where you are the receiver)
                var incomingTrades = await _context.Trades
                    .Include(t => t.RequestedBy)
                    .Include(t => t.TradeItems)
                        .ThenInclude(ti => ti.Item)
                    .Where(t => t.RequestedToId == userId && t.Status == "Pending")
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                // Get outgoing requests (requests you sent)
                var outgoingTradeRequests = await _context.TradeRequests
                    .Include(tr => tr.Receiver)
                    .Include(tr => tr.OfferedItem)
                    .Include(tr => tr.RequestedItem)
                    .Where(tr => tr.RequesterId == userId && tr.Status == "Pending")
                    .OrderByDescending(tr => tr.CreatedAt)
                    .ToListAsync();

                // Get outgoing trades (trades you initiated)
                var outgoingTrades = await _context.Trades
                    .Include(t => t.RequestedTo)
                    .Include(t => t.TradeItems)
                        .ThenInclude(ti => ti.Item)
                    .Where(t => t.RequestedById == userId && t.Status == "Pending")
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                ViewBag.IncomingTradeRequests = incomingTradeRequests;
                ViewBag.IncomingTrades = incomingTrades;
                ViewBag.OutgoingTradeRequests = outgoingTradeRequests;
                ViewBag.OutgoingTrades = outgoingTrades;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Pending method");
                TempData["Error"] = "An error occurred while loading trade requests.";

                // Return view with empty lists
                ViewBag.IncomingTradeRequests = new List<TradeRequest>();
                ViewBag.IncomingTrades = new List<Trade>();
                ViewBag.OutgoingTradeRequests = new List<TradeRequest>();
                ViewBag.OutgoingTrades = new List<Trade>();
                return View();
            }
        }

        // ACCEPT TRADE REQUEST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to Accept");
                    return RedirectToAction("Login", "User");
                }

                var request = await _context.TradeRequests
                    .Include(tr => tr.OfferedItem)
                    .Include(tr => tr.RequestedItem)
                    .Include(tr => tr.Requester)
                    .Include(tr => tr.Receiver)
                    .FirstOrDefaultAsync(tr => tr.Id == id && tr.ReceiverId == userId && tr.Status == "Pending");

                if (request == null)
                {
                    TempData["Error"] = "Trade request not found or already processed.";
                    return RedirectToAction("Pending");
                }

                // Verify items still belong to the correct owners
                if (request.OfferedItem.OwnerId != request.RequesterId)
                {
                    TempData["Error"] = "The offered item is no longer available from the requester.";
                    request.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Pending");
                }

                if (request.RequestedItem.OwnerId != userId)
                {
                    TempData["Error"] = "You no longer own the requested item.";
                    request.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Pending");
                }

                // SWAP ITEM OWNERSHIP
                var tempOwnerId = request.OfferedItem.OwnerId;
                request.OfferedItem.OwnerId = request.RequestedItem.OwnerId;
                request.RequestedItem.OwnerId = tempOwnerId;

                request.Status = "Accepted";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Trade request {TradeRequestId} accepted by user {UserId}", id, userId);

                TempData["Success"] = $"Trade completed! You received {request.OfferedItem.Name} from {request.Requester.Username}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Accept method for trade request {TradeRequestId}", id);
                TempData["Error"] = "An error occurred while accepting the trade.";
            }

            return RedirectToAction("Pending");
        }

        // REJECT TRADE REQUEST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to Reject");
                    return RedirectToAction("Login", "User");
                }

                var request = await _context.TradeRequests
                    .FirstOrDefaultAsync(tr => tr.Id == id && tr.ReceiverId == userId && tr.Status == "Pending");

                if (request == null)
                {
                    TempData["Error"] = "Trade request not found or already processed.";
                    return RedirectToAction("Pending");
                }

                request.Status = "Rejected";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Trade request {TradeRequestId} rejected by user {UserId}", id, userId);

                TempData["Info"] = "Trade request has been rejected.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Reject method for trade request {TradeRequestId}", id);
                TempData["Error"] = "An error occurred while rejecting the trade.";
            }

            return RedirectToAction("Pending");
        }

        // CANCEL OUTGOING REQUEST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to CancelRequest");
                    return RedirectToAction("Login", "User");
                }

                var request = await _context.TradeRequests
                    .FirstOrDefaultAsync(tr => tr.Id == id && tr.RequesterId == userId && tr.Status == "Pending");

                if (request == null)
                {
                    TempData["Error"] = "Trade request not found or already processed.";
                    return RedirectToAction("Pending");
                }

                request.Status = "Cancelled";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Trade request {TradeRequestId} cancelled by user {UserId}", id, userId);

                TempData["Info"] = "Your trade request has been cancelled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelRequest method for trade request {TradeRequestId}", id);
                TempData["Error"] = "An error occurred while cancelling the trade request.";
            }

            return RedirectToAction("Pending");
        }

        // GET PENDING REQUESTS COUNT FOR BADGE
        [HttpGet]
        public async Task<IActionResult> GetPendingCount()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Json(new { count = 0 });

                // Count BOTH TradeRequest AND Trade pending requests
                var tradeRequestCount = await _context.TradeRequests
                    .CountAsync(tr => tr.ReceiverId == userId && tr.Status == "Pending");

                var tradeCount = await _context.Trades
                    .CountAsync(t => t.RequestedToId == userId && t.Status == "Pending");

                var totalCount = tradeRequestCount + tradeCount;

                return Json(new { count = totalCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPendingCount method");
                return Json(new { count = 0 });
            }
        }
    }
}