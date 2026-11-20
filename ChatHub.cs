using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class ChatHub : Hub
{
    // Remembers who is connected: connectionId -> (room, user)
    private static readonly ConcurrentDictionary<string, (string Room, string User)> Connections = new();

    private readonly ChatDbContext _db;

    public ChatHub(ChatDbContext db) => _db = db;

    public async Task JoinRoom(string room, string user)
    {
        room = Clean(room);
        user = CleanUser(user);

        // If already in another room, leave it first
        if (Connections.TryGetValue(Context.ConnectionId, out var old) && old.Room != room)
            await RemoveFromRoom();

        await Groups.AddToGroupAsync(Context.ConnectionId, room);
        Connections[Context.ConnectionId] = (room, user);

        var history = await _db.Messages
            .Where(m => m.Room == room)
            .OrderByDescending(m => m.Id)
            .Take(50)
            .OrderBy(m => m.Id)
            .Select(m => new { m.User, m.Text, m.SentAt })
            .ToListAsync();

        await Clients.Caller.SendAsync("LoadHistory", history);
        await Clients.Group(room).SendAsync("ReceiveMessage", "System", $"{user} joined #{room}", DateTimeOffset.UtcNow);
        await BroadcastUsers(room);
    }

    public async Task LeaveRoom(string room, string user)
    {
        await RemoveFromRoom();
    }

    public async Task SendMessage(string room, string user, string message)
    {
        room = Clean(room);
        message = (message ?? "").Trim();
        if (message.Length == 0) return;
        if (message.Length > 500) message = message[..500];

        user = CleanUser(user);

        var msg = new ChatMessage
        {
            Room = room,
            User = user,
            Text = message,
            SentAt = DateTimeOffset.UtcNow
        };

        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        await Clients.Group(room).SendAsync("ReceiveMessage", msg.User, msg.Text, msg.SentAt);
    }

    // The browser calls this while someone is typing
    public async Task Typing()
    {
        if (Connections.TryGetValue(Context.ConnectionId, out var info))
            await Clients.OthersInGroup(info.Room).SendAsync("UserTyping", info.User);
    }

    // Runs automatically when a tab closes or the connection drops
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await RemoveFromRoom();
        await base.OnDisconnectedAsync(exception);
    }

    private async Task RemoveFromRoom()
    {
        if (!Connections.TryRemove(Context.ConnectionId, out var info)) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, info.Room);
        await Clients.Group(info.Room).SendAsync("ReceiveMessage", "System", $"{info.User} left #{info.Room}", DateTimeOffset.UtcNow);
        await BroadcastUsers(info.Room);
    }

    private async Task BroadcastUsers(string room)
    {
        var names = Connections.Values
            .Where(c => c.Room == room)
            .Select(c => c.User)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        await Clients.Group(room).SendAsync("UpdateUsers", names);
    }

    private static string Clean(string room)
    {
        room = (room ?? "").Trim().ToLowerInvariant();
        return room.Length > 30 ? room[..30] : room;
    }

    private static string CleanUser(string user)
    {
        user = string.IsNullOrWhiteSpace(user) ? "Anonymous" : user.Trim();
        return user.Length > 30 ? user[..30] : user;
    }
}