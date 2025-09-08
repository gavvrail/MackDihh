using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FoodOrderingSystem.Hubs
{
    [Authorize]
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
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
            
            await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", new
            {
                SenderId = userId,
                SenderName = senderName,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsFromCustomer = Context.User?.IsInRole("Customer") ?? false
            });
        }

        public async Task SendTypingIndicator(string sessionId, string senderName, bool isTyping)
        {
            await Clients.GroupExcept($"session_{sessionId}", Context.ConnectionId)
                .SendAsync("TypingIndicator", new
                {
                    SenderName = senderName,
                    IsTyping = isTyping
                });
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
