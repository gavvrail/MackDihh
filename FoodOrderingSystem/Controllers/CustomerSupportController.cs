using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using System.Security.Claims;

namespace FoodOrderingSystem.Controllers
{
    [Authorize]
    public class CustomerSupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerSupportController> _logger;

        public CustomerSupportController(ApplicationDbContext context, ILogger<CustomerSupportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Redirect to home page - customers should use the support widget instead
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    return Json(new { success = false, error = "Message cannot be empty" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return Json(new { success = false, error = "User not authenticated" });
                }
                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {UserId}", userId);
                    return Json(new { success = false, error = "User not found" });
                }

                _logger.LogInformation("Sending message from user: {UserId} ({UserName})", userId, user.Email);

                // Get or create session
                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.CustomerId == userId && s.Status == ChatSessionStatus.Active);

                if (session == null)
                {
                    session = new ChatSession
                    {
                        CustomerId = userId,
                        CustomerName = $"{user.FirstName} {user.LastName}".Trim(),
                        Status = ChatSessionStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        MessageCount = 0,
                        UnreadCount = 0
                    };
                    _context.ChatSessions.Add(session);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created new chat session: {SessionId} for user: {UserId}", session.Id, userId);
                }
                else
                {
                    _logger.LogInformation("Using existing chat session: {SessionId} for user: {UserId}", session.Id, userId);
                }

                // Create message
                var chatMessage = new ChatMessage
                {
                    SessionId = session.Id,
                    SenderId = userId,
                    SenderName = $"{user.FirstName} {user.LastName}".Trim(),
                    Message = message.Trim(),
                    Timestamp = DateTime.UtcNow,
                    IsFromCustomer = true,
                    IsRead = false
                };

                _context.ChatMessages.Add(chatMessage);
                
                // Update session
                session.MessageCount++;
                session.UnreadCount++;
                session.LastMessageAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Message saved successfully. MessageId: {MessageId}, SessionId: {SessionId}", chatMessage.Id, session.Id);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from user: {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return Json(new { success = false, error = "An error occurred while sending the message" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { messages = new List<object>() });
            }
            
            var session = await _context.ChatSessions
                .Include(s => s.Messages.OrderBy(m => m.Timestamp))
                .FirstOrDefaultAsync(s => s.CustomerId == userId && s.Status == ChatSessionStatus.Active);

            if (session == null)
            {
                return Json(new { messages = new List<object>() });
            }

            var messages = session.Messages.Select(m => new
            {
                id = m.Id,
                senderName = m.SenderName,
                message = m.Message,
                timestamp = m.Timestamp.ToString("MMM dd, yyyy HH:mm"),
                isFromCustomer = m.IsFromCustomer
            }).ToList();

            return Json(new { messages });
        }

        [HttpGet]
        public async Task<IActionResult> GetAutoResponse(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { response = (string?)null });
            }

            var lowerMessage = message.ToLower();
            
            // Get all active auto-responses
            var autoResponses = await _context.AutoResponses
                .Where(ar => ar.IsActive)
                .ToListAsync();

            // Find matching auto-response
            foreach (var autoResponse in autoResponses)
            {
                var keywords = autoResponse.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim().ToLower())
                    .Where(k => !string.IsNullOrEmpty(k));

                if (keywords.Any(keyword => lowerMessage.Contains(keyword)))
                {
                    return Json(new { response = autoResponse.Response });
                }
            }

            return Json(new { response = (string?)null });
        }

        [HttpGet]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var sessionCount = await _context.ChatSessions.CountAsync();
                var messageCount = await _context.ChatMessages.CountAsync();
                var userSessionCount = await _context.ChatSessions.CountAsync(s => s.CustomerId == userId);
                
                return Json(new
                {
                    success = true,
                    userId = userId,
                    totalSessions = sessionCount,
                    totalMessages = messageCount,
                    userSessions = userSessionCount,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
