using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace PumpAndDumpBot.Handlers
{
    public class UserHandler
    {
        private readonly DiscordSocketClient _client;
        private ConcurrentDictionary<ulong, int> currentState = new ConcurrentDictionary<ulong, int>();
        private bool initialized = false;
        
        public UserHandler(DiscordSocketClient client)
        {
            _client = client;
            _client.UserJoined += UserJoinedAsync;
        }

        public async Task SetupBeginStateAsync()
        {
            var invites = await _client.GetGuild(399748192524042262).GetInvitesAsync();
            foreach (ulong inviterId in invites.Select(x => x.Inviter.Id).Distinct())
            {
                var inviteTotal = invites.Where(x => x.Inviter.Id == inviterId).Sum(x => x.Uses);
                currentState.TryAdd(inviterId, inviteTotal);
            }

            initialized = true;
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            if (!initialized) await SetupBeginStateAsync();

            var invites = await user.Guild.GetInvitesAsync();
            if (invites == null) return;

            ulong channel = (ulong)(_client.CurrentUser.Id == 400573066134028288 ? 401021271556620288 : 400552471010869248); // live then test

            foreach (IUser inviter in invites.Select(x => x.Inviter).Distinct())
            {
                var newTotal = invites.Where(x => x.Inviter.Id == inviter.Id).Sum(x => x.Uses);
                if (currentState.TryGetValue(inviter.Id, out int currentTotal)) // known user's counters
                {
                    if (currentTotal == newTotal) continue;

                    await (_client.GetChannel(channel) as SocketTextChannel).SendMessageAsync($"{user.Mention} probably joined using an invite link created by {inviter.Mention}.");
                    currentState.TryUpdate(inviter.Id, currentTotal + 1, currentTotal);
                    return;
                }
                else // new user's counter
                {
                    await (_client.GetChannel(channel) as SocketTextChannel).SendMessageAsync($"{user.Mention} probably joined using an invite link created by {inviter.Mention}.");
                    currentState.TryAdd(inviter.Id, 1);
                    return;
                }
            }
        }
    }
}