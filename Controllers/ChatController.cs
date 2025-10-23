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
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ChatController> _logger;

        public ChatController(AppDbContext context, ILogger<ChatController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // CHAT WITH SPECIFIC USER
        [HttpGet]
        public async Task<IActionResult> Index(int? userId)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (currentUserId == null)
                {
                    TempData["Error"] = "Please login to chat";
                    return RedirectToAction("Login", "User");
                }

                // Get chat users (people you've messaged or received messages from)
                var chatUsers = await _context.Messages
                    .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                    .Select(m => m.SenderId == currentUserId ? m.Receiver : m.Sender)
                    .Distinct()
                    .ToListAsync();

                ViewBag.ChatUsers = chatUsers;

                // If specific user is selected, get conversation
                if (userId.HasValue)
                {
                    var selectedUser = await _context.Users.FindAsync(userId);
                    if (selectedUser != null)
                    {
                        ViewBag.SelectedUser = selectedUser;

                        // Get messages between current user and selected user
                        var messages = await _context.Messages
                            .Include(m => m.Sender)
                            .Include(m => m.Receiver)
                            .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                                       (m.SenderId == userId && m.ReceiverId == currentUserId))
                            .OrderBy(m => m.SentAt)
                            .ToListAsync();

                        // Mark messages as read
                        var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead);
                        foreach (var msg in unreadMessages)
                        {
                            msg.IsRead = true;
                        }
                        await _context.SaveChangesAsync();

                        ViewBag.Messages = messages;
                    }
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat");
                TempData["Error"] = "Error loading chat";
                return View();
            }
        }

        // SEND MESSAGE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int receiverId, string content)
        {
            try
            {
                var senderId = HttpContext.Session.GetInt32("UserId");
                if (senderId == null)
                {
                    return Json(new { success = false, error = "Please login to send messages" });
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, error = "Message cannot be empty" });
                }

                // Check if receiver exists
                var receiver = await _context.Users.FindAsync(receiverId);
                if (receiver == null)
                {
                    return Json(new { success = false, error = "User not found" });
                }

                var message = new Message
                {
                    SenderId = senderId.Value,
                    ReceiverId = receiverId,
                    Content = content.Trim(),
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from user {SenderId} to {ReceiverId}",
                    HttpContext.Session.GetInt32("UserId"), receiverId);
                return Json(new { success = false, error = "Error sending message. Please try again." });
            }
        }

        // GET UNREAD MESSAGES COUNT
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Json(new { count = 0 });

                var unreadCount = await _context.Messages
                    .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

                return Json(new { count = unreadCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return Json(new { count = 0 });
            }
        }

        // GET MESSAGES FOR SPECIFIC USER (for real-time updates)
        [HttpGet]
        public async Task<IActionResult> GetMessages(int userId)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (currentUserId == null)
                    return Json(new { success = false });

                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                               (m.SenderId == userId && m.ReceiverId == currentUserId))
                    .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        id = m.Id,
                        senderId = m.SenderId,
                        senderName = m.Sender.Username,
                        content = m.Content,
                        sentAt = m.SentAt.ToString("MMM dd, yyyy hh:mm tt"),
                        isOwn = m.SenderId == currentUserId
                    })
                    .ToListAsync();

                return Json(new { success = true, messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages");
                return Json(new { success = false });
            }
        }
    }
}