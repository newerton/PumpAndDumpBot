using System.Threading.Tasks;
using Discord.WebSocket;

namespace PumpAndDumpBot.Services
{
    public class VeteranService
    {
        private readonly DiscordSocketClient _client;
        public VeteranService(DiscordSocketClient client)
        {
            _client = client;
            _client.UserJoined += UserJoinedAsync;
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            if (_client.CurrentUser.Id != 400573066134028288) return;

            if (user.Guild.Users.Count < 500)
            {
                var veteranRole = _client.GetGuild(399748192524042262).GetRole(400384561881546783);
                await user.AddRoleAsync(veteranRole);
            }
        }
    }
}