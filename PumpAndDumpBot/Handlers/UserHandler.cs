using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using PumpAndDumpBot.Data;

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
            if (_client.CurrentUser.Id != 400573066134028288) return; //if not the live bot exit

            if (user.Guild.Users.Count < 500)
            {
                var veteranRole =_client.GetGuild(399748192524042262).GetRole(400384561881546783);
                await user.AddRoleAsync(veteranRole);
            }

            if (!initialized) await SetupBeginStateAsync();

            var invites = await user.Guild.GetInvitesAsync();
            if (invites == null) return;

            foreach (IUser inviter in invites.Select(x => x.Inviter).Distinct())
            {
                var newTotal = invites.Where(x => x.Inviter.Id == inviter.Id).Sum(x => x.Uses);
                if (currentState.TryGetValue(inviter.Id, out int currentTotal)) // known user's counters
                {
                    if (currentTotal == newTotal) continue;

                    currentState.TryUpdate(inviter.Id, currentTotal + 1, currentTotal);
                    await Database.InsertInviteAsync(user.Id, inviter.Id);
                    
                    return;
                }
                else // new user's counter
                {
                    currentState.TryAdd(inviter.Id, 1);
                    await Database.InsertInviteAsync(user.Id, inviter.Id);
                    
                    return;
                }
            }
        }
    }
}