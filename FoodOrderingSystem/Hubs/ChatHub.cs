using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FoodOrderingSystem.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task SendMessage(string sessionId, string message, string senderName)
        {
            try
            {
                // Check authentication for sending messages
                if (Context.User?.Identity?.IsAuthenticated != true)
                {
                    await Clients.Caller.SendAsync("Error", "Authentication required to send messages");
                    return;
                }

                if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(message))
                {
                    await Clients.Caller.SendAsync("Error", "Session ID and message are required");
                    return;
                }

                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = Context.User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
                
                // Sanitize inputs
                var sanitizedMessage = System.Web.HttpUtility.HtmlEncode(message.Trim());
                var sanitizedSenderName = System.Web.HttpUtility.HtmlEncode(senderName.Trim());
                
                if (sanitizedMessage.Length > 1000)
                {
                    sanitizedMessage = sanitizedMessage.Substring(0, 1000);
                }
                
                await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", new
                {
                    SenderId = userId,
                    SenderName = sanitizedSenderName,
                    Message = sanitizedMessage,
                    Timestamp = DateTime.UtcNow,
                    IsFromCustomer = Context.User.IsInRole("Customer")
                });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", "An error occurred while sending the message");
                // Log the exception for debugging
                Console.WriteLine($"Error in SendMessage: {ex.Message}");
            }
        }

        public async Task SendTypingIndicator(string sessionId, string senderName, bool isTyping)
        {
            try
            {
                // Check authentication for typing indicators
                if (Context.User?.Identity?.IsAuthenticated != true)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return;
                }

                var sanitizedSenderName = System.Web.HttpUtility.HtmlEncode(senderName.Trim());
                
                await Clients.GroupExcept($"session_{sessionId}", Context.ConnectionId)
                    .SendAsync("TypingIndicator", new
                    {
                        SenderName = sanitizedSenderName,
                        IsTyping = isTyping
                    });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error in SendTypingIndicator: {ex.Message}");
            }
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
            
            await Clients.All.SendAsync("UserConnected", new
            {
                UserId = userId,
                UserName = userName,
                ConnectionId = Context.ConnectionId
            });
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
            
            await Clients.All.SendAsync("UserDisconnected", new
            {
                UserId = userId,
                UserName = userName,
                ConnectionId = Context.ConnectionId
            });
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}
