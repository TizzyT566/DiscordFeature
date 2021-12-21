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
            Console.WriteLine(arg.Content);
            return Task.CompletedTask;
        }
    }
}