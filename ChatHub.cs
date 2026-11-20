using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class ChatHub : Hub
{
    private readonly ChatDbContext _db;

    public ChatHub(ChatDbContext db) => _db = db;

    public async Task JoinRoom(string room, string user)
    {
        room = Clean(room);
        await Groups.AddToGroupAsync(Context.ConnectionId, room);

        // Last 50 messages of this room, oldest first
        var history = await _db.Messages
            .Where(m => m.Room == room)
            .OrderByDescending(m => m.Id)
            .Take(50)
            .OrderBy(m => m.Id)
            .Select(m => new { m.User, m.Text, m.SentAt })
            .ToListAsync();

        // Send history only to the person who just joined
        await Clients.Caller.SendAsync("LoadHistory", history);

        await Clients.Group(room).SendAsync("ReceiveMessage", "System", $"{user} joined #{room}", DateTimeOffset.UtcNow);
    }

    public async Task LeaveRoom(string room, string user)
    {
        room = Clean(room);
        await Clients.Group(room).SendAsync("ReceiveMessage", "System", $"{user} left #{room}", DateTimeOffset.UtcNow);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
    }

    public async Task SendMessage(string room, string user, string message)
    {
        room = Clean(room);
        message = (message ?? "").Trim();
        if (message.Length == 0) return;
        if (message.Length > 500) message = message[..500];

        user = string.IsNullOrWhiteSpace(user) ? "Anonymous" : user.Trim();
        if (user.Length > 30) user = user[..30];

        var msg = new ChatMessage
        {
            Room = room,
            User = user,
            Text = message,
            SentAt = DateTimeOffset.UtcNow
        };

        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();   // save to the database first

        await Clients.Group(room).SendAsync("ReceiveMessage", msg.User, msg.Text, msg.SentAt);
    }

    private static string Clean(string room)
    {
        room = (room ?? "").Trim().ToLowerInvariant();
        return room.Length > 30 ? room[..30] : room;
    }
}