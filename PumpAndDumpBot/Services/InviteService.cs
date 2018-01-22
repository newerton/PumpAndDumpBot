using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using PumpAndDumpBot.Data;
using PumpAndDumpBot.Data.Objects;

namespace PumpAndDumpBot.Services
{
    public class InviteService
    {
        private readonly DiscordSocketClient _client;
        private ConcurrentDictionary<ulong, int> _currentState;

        private Timer _timer;
        private ConcurrentQueue<Invite> _invites;

        public InviteService(DiscordSocketClient client)
        {
            _client = client;
            _client.Ready += StartAsync;

            _currentState = new ConcurrentDictionary<ulong, int>();

            _timer = new Timer(15000);
            _timer.AutoReset = false;
            _timer.Elapsed += WorkTimerElapsed;

            _invites = new ConcurrentQueue<Invite>();
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
                    _currentState.TryAdd(inviter.Id, inviteTotal);
                }

                _client.UserJoined += UserJoinedAsync;
                _timer.Start();
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
                if (_currentState.TryGetValue(inviter.Id, out int currentTotal)) // known user's counters
                {
                    var newTotal = usedInviteLinks.Where(x => x.Inviter.Id == inviter.Id).Sum(x => x.Uses);
                    if (currentTotal == newTotal) continue;

                    _currentState.TryUpdate(inviter.Id, currentTotal + 1, currentTotal);
                }
                else // new user's counter
                {
                    _currentState.TryAdd(inviter.Id, 1);
                }

                if (_client.CurrentUser.Id == 400573066134028288)
                {
                    //await Database.InsertInviteAsync(user.Id, inviter.Id);
                    _invites.Enqueue(new Invite()
                    {
                        UserId = user.Id,
                        ReferrerId = inviter.Id,
                        JoinDate = DateTime.Now
                    });
                }

                return;
            }
        }

        private void WorkTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    await Database.InsertInvitesAsync(_invites);
                }
                catch (Exception ex)
                {
                    await (_client.GetChannel(401021271556620288) as SocketTextChannel).SendMessageAsync(ex.Message);
                }
                finally
                {
                    _timer.Start();
                }
            }).Wait();
        }
    }
}