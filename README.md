# Realtime Chat

A simple chat app I built to learn real-time web apps. You pick a name, join a room, and chat with other people in that room. Messages show up right away, without refreshing the page.

Live link: coming soon

## What it does

- Join any room by typing its name
- Messages show up instantly for everyone in the room
- The last 50 messages are saved, so you still see them after a refresh or a server restart
- Shows who is online in the room
- Shows "Name is typing..." when someone is typing
- Reconnects on its own if the connection drops

## Built with

- C# and ASP.NET Core (.NET 10)
- SignalR for the real-time part
- Entity Framework Core with SQLite for saving messages
- Plain HTML and JavaScript for the page

## Run it on your computer

You need the .NET 10 SDK installed.

1. Download or clone this repo
2. Open a terminal in the project folder
3. Run `dotnet run`
4. Open http://localhost:5187 in your browser
5. Open a second browser window to chat with yourself

The database file (`chat.db`) is made automatically the first time you run it.

## How it works

The browser connects to the server using SignalR. When you send a message, the server cleans it up, saves it to the database, and sends it to everyone in the same room. The server also keeps a list of who is connected, which is how the online list and typing messages work.

## What I learned

- How real-time connections work, and how SignalR groups people into rooms
- Saving data with Entity Framework Core
- Keeping track of connected users when people close the tab or lose connection
- Not trusting input from the browser, so the server limits name and message length