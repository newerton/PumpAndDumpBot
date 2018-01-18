using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using PumpAndDumpBot.Data;

namespace PumpAndDumpBot.Handlers
{
    public class InviteHandler
    {
        private readonly DiscordSocketClient _client;
        private ConcurrentDictionary<ulong, int> currentState = new ConcurrentDictionary<ulong, int>();

        public InviteHandler(DiscordSocketClient client)
        {
            _client = client;
            _client.Ready += StartAsync;
        }

        public async Task StartAsync()
        {
            try
            {
                var invites = await _client.GetGuild(399748192524042262).GetInvitesAsync();
                var usedInviteLinks = invites.Where(x => x.Uses > 0).ToList();
                var inviters = usedInviteLinks.Select(x => x.Inviter).Distinct().ToList();
                foreach (IUser inviter in inviters)
                {
                    var inviteTotal = usedInviteLinks.Where(x => x.Inviter.Id == inviter.Id).Sum(x => x.Uses);
                    currentState.TryAdd(inviter.Id, inviteTotal);
                }

                _client.UserJoined += UserJoinedAsync;
            }
            catch (Exception ex)
            {
                _client.UserJoined -= UserJoinedAsync;
                await Program.Log(new LogMessage(LogSeverity.Critical, "UserHandler.StartAsync", ex.Message, ex));
            }
            finally
            {
                _client.Ready -= StartAsync;
            }
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            var invites = await _client.GetGuild(399748192524042262).GetInvitesAsync();
            var usedInviteLinks = invites.Where(x => x.Uses > 0).ToList();
            var inviters = usedInviteLinks.Select(x => x.Inviter).Distinct().ToList();

            foreach (IUser inviter in inviters)
            {
                var newTotal = usedInviteLinks.Where(x => x.Inviter.Id == inviter.Id).Sum(x => x.Uses);
                if (currentState.TryGetValue(inviter.Id, out int currentTotal)) // known user's counters
                {
                    if (currentTotal == newTotal) continue;

                    currentState.TryUpdate(inviter.Id, currentTotal + 1, currentTotal);
                    if (_client.CurrentUser.Id == 400573066134028288)
                        await Database.InsertInviteAsync(user.Id, inviter.Id);

                    return;
                }
                else // new user's counter
                {
                    currentState.TryAdd(inviter.Id, 1);
                    if (_client.CurrentUser.Id == 400573066134028288)
                        await Database.InsertInviteAsync(user.Id, inviter.Id);

                    return;
                }
            }
        }
    }
}