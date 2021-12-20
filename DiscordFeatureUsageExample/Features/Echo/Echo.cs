using Discord;
using Discord.WebSocket;

namespace DiscordFeatureUsageExample.Features.Echo
{
    internal class Echo : Feature
    {
        public override void Init(in DiscordSocketClient client)
        {
            client.MessageReceived += Client_MessageReceived;
        }

        private Task Client_MessageReceived(SocketMessage arg)
        {
            // Writes to its own dedicated console window
            LogLine(arg.CleanContent);
            return Task.CompletedTask;
        }
    }
}
