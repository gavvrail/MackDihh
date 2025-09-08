using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using System.Security.Claims;

namespace FoodOrderingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminSupportController> _logger;

        public AdminSupportController(ApplicationDbContext context, ILogger<AdminSupportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading AdminSupport dashboard...");

                var sessions = await _context.ChatSessions
                    .Include(s => s.Messages.OrderByDescending(m => m.Timestamp).Take(1))
                    .Include(s => s.Customer)
                    .Include(s => s.Agent)
                    .OrderByDescending(s => s.LastMessageAt ?? s.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Found {SessionCount} chat sessions", sessions.Count);

                // Get actual message counts for each session
                foreach (var session in sessions)
                {
                    session.MessageCount = await _context.ChatMessages
                        .Where(m => m.SessionId == session.Id)
                        .CountAsync();
                        
                    session.UnreadCount = await _context.ChatMessages
                        .Where(m => m.SessionId == session.Id && !m.IsRead && m.IsFromCustomer)
                        .CountAsync();

                    _logger.LogInformation("Session {SessionId}: Customer={CustomerName}, Messages={MessageCount}, Unread={UnreadCount}", 
                        session.Id, session.CustomerName, session.MessageCount, session.UnreadCount);
                }

                return View(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading AdminSupport dashboard");
                return View(new List<ChatSession>());
            }
        }

        public async Task<IActionResult> Chat(string sessionId)
        {
            if (!int.TryParse(sessionId, out int sessionIdInt))
            {
                return RedirectToAction("Index");
            }

            var session = await _context.ChatSessions
                .Include(s => s.Messages.OrderBy(m => m.Timestamp))
                .FirstOrDefaultAsync(s => s.Id == sessionIdInt);

            if (session == null)
            {
                return RedirectToAction("Index");
            }

            // Mark customer messages as read (admin is viewing them)
            var unreadCustomerMessages = session.Messages.Where(m => !m.IsRead && m.IsFromCustomer).ToList();
            foreach (var message in unreadCustomerMessages)
            {
                message.IsRead = true;
            }
            session.UnreadCount = 0;
            await _context.SaveChangesAsync();

            ViewBag.Session = session;
            ViewBag.Messages = session.Messages.ToList();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendReply(string sessionId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, error = "Message cannot be empty" });
            }

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId))
            {
                return Json(new { success = false, error = "Admin not authenticated" });
            }
            
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId);
            if (admin == null)
            {
                return Json(new { success = false, error = "Admin not found" });
            }

            if (!int.TryParse(sessionId, out int sessionIdInt))
            {
                return Json(new { success = false, error = "Invalid session ID" });
            }

            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionIdInt);

            if (session == null)
            {
                return Json(new { success = false, error = "Session not found" });
            }

            // Assign admin to session if not already assigned
            if (string.IsNullOrEmpty(session.AgentId))
            {
                session.AgentId = adminId;
                session.AgentName = $"{admin.FirstName} {admin.LastName}".Trim();
            }

            // Create admin reply
            var chatMessage = new ChatMessage
            {
                SessionId = sessionIdInt,
                SenderId = adminId,
                SenderName = $"{admin.FirstName} {admin.LastName}".Trim(),
                Message = message.Trim(),
                Timestamp = DateTime.UtcNow,
                IsFromCustomer = false,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMessage);
            
            // Update session
            session.MessageCount++;
            session.LastMessageAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = new
                {
                    id = chatMessage.Id,
                    senderName = chatMessage.SenderName,
                    message = chatMessage.Message,
                    timestamp = chatMessage.Timestamp.ToString("MMM dd, yyyy HH:mm"),
                    isFromCustomer = chatMessage.IsFromCustomer
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> CloseSession(string sessionId)
        {
            if (!int.TryParse(sessionId, out int sessionIdInt))
            {
                return Json(new { success = false, error = "Invalid session ID" });
            }

            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionIdInt);

            if (session == null)
            {
                return Json(new { success = false, error = "Session not found" });
            }

            session.Status = ChatSessionStatus.Closed;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ResolveSession(string sessionId)
        {
            if (!int.TryParse(sessionId, out int sessionIdInt))
            {
                return Json(new { success = false, error = "Invalid session ID" });
            }

            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionIdInt);

            if (session == null)
            {
                return Json(new { success = false, error = "Session not found" });
            }

            session.Status = ChatSessionStatus.Resolved;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSession(string sessionId)
        {
            try
            {
                if (!int.TryParse(sessionId, out int sessionIdInt))
                {
                    return Json(new { success = false, error = "Invalid session ID" });
                }

                var session = await _context.ChatSessions
                    .Include(s => s.Messages)
                    .FirstOrDefaultAsync(s => s.Id == sessionIdInt);

                if (session == null)
                {
                    return Json(new { success = false, error = "Session not found" });
                }

                // Delete all messages in the session first
                _context.ChatMessages.RemoveRange(session.Messages);

                // Delete the session
                _context.ChatSessions.Remove(session);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Chat session {SessionId} deleted by admin {AdminId}", sessionIdInt, User.FindFirstValue(ClaimTypes.NameIdentifier));

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chat session {SessionId}", sessionId);
                return Json(new { success = false, error = "An error occurred while deleting the chat session" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNewMessages(string sessionId)
        {
            if (!int.TryParse(sessionId, out int sessionIdInt))
            {
                return Json(new { messages = new List<object>() });
            }

            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionIdInt && !m.IsRead && m.IsFromCustomer)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            var messageList = messages.Select(m => new
            {
                id = m.Id,
                senderName = m.SenderName,
                message = m.Message,
                timestamp = m.Timestamp.ToString("MMM dd, yyyy HH:mm"),
                isFromCustomer = m.IsFromCustomer
            }).ToList();

            return Json(new { messages = messageList });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var unreadCount = await _context.ChatMessages
                    .Where(m => !m.IsRead && m.IsFromCustomer)
                    .CountAsync();

                var totalSessions = await _context.ChatSessions
                    .Where(s => s.Status == ChatSessionStatus.Active)
                    .CountAsync();

                return Json(new { 
                    count = unreadCount,
                    totalSessions = totalSessions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread message count");
                return Json(new { count = 0, totalSessions = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                // Test database connection
                var sessionCount = await _context.ChatSessions.CountAsync();
                var messageCount = await _context.ChatMessages.CountAsync();
                
                var allSessions = await _context.ChatSessions
                    .Include(s => s.Messages)
                    .Include(s => s.Customer)
                    .ToListAsync();

                var allMessages = await _context.ChatMessages
                    .Include(m => m.Session)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    sessionCount = sessionCount,
                    messageCount = messageCount,
                    sessions = allSessions.Select(s => new
                    {
                        id = s.Id,
                        customerId = s.CustomerId,
                        customerName = s.CustomerName,
                        status = s.Status.ToString(),
                        messageCount = s.MessageCount,
                        unreadCount = s.UnreadCount,
                        createdAt = s.CreatedAt,
                        lastMessageAt = s.LastMessageAt,
                        customerEmail = s.Customer?.Email
                    }),
                    messages = allMessages.Select(m => new
                    {
                        id = m.Id,
                        sessionId = m.SessionId,
                        senderId = m.SenderId,
                        senderName = m.SenderName,
                        message = m.Message,
                        isFromCustomer = m.IsFromCustomer,
                        isRead = m.IsRead,
                        timestamp = m.Timestamp
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database");
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
