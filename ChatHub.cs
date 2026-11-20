using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    public async Task JoinRoom(string room, string user)
    {
        room = Clean(room);
        await Groups.AddToGroupAsync(Context.ConnectionId, room);
        await Clients.Group(room).SendAsync("ReceiveMessage", "System", $"{user} joined #{room}");
    }

    public async Task LeaveRoom(string room, string user)
    {
        room = Clean(room);
        await Clients.Group(room).SendAsync("ReceiveMessage", "System", $"{user} left #{room}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
    }

    public async Task SendMessage(string room, string user, string message)
    {
        room = Clean(room);
        await Clients.Group(room).SendAsync("ReceiveMessage", user, message);
    }

    // Normalize room names so "General" and "general " are the same room
    private static string Clean(string room)
    {
        room = (room ?? "").Trim().ToLowerInvariant();
        return room.Length > 30 ? room[..30] : room;
    }
}