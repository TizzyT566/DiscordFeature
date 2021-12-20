using Discord;
using Discord.WebSocket;
using static Discord.Feature;

// Login to discord
DiscordSocketConfig config = new() { AlwaysDownloadUsers = true, MessageCacheSize = 1000 };
await LoginAsync(TokenType.Bot, File.ReadAllText("token.tok"), config);

// Prevents the application from closing until specific word is entered
await LogoutKeyword("exit");