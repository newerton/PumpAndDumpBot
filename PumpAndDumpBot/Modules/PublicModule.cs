using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using PumpAndDumpBot.Models;

namespace PumpAndDumpBot.Modules
{
    
    [Name("Public")]
    [Summary("Public module")]
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        private static readonly List<Affiliate> _ranks = new List<Affiliate>()
        {
            new Affiliate(399769377194377216, 1),
            new Affiliate(400361570959622144, 4),
            new Affiliate(400361693672112128, 10),
            new Affiliate(400362357412331523, 20),
            new Affiliate(400362301481418752, 50),
            new Affiliate(400362225317183489, 125)
        };

        [Command("invites", RunMode = RunMode.Async)]
        [Summary("Returns the total invites for this users.")]
        [RequireContext(ContextType.Guild)]
        public async Task InvitesAsync()
        {
            try
            {
                var guildUser = Context.Message.Author as SocketGuildUser;

                var invites = await Context.Guild.GetInvitesAsync();
                var totalInvites = invites.Where(x => x.Inviter.Id == guildUser.Id).Sum(x => x.Uses);

                string message = $"{guildUser.Mention}\nYou have {totalInvites} invites.";

                var currentRank = _ranks.LastOrDefault(x => x.Invites <= totalInvites);
                if (currentRank != null)
                {
                    var roles = guildUser.Roles;
                    if (!roles.Select(x => x.Id).Contains(currentRank.RoleID))
                    {
                        var newRole = Context.Guild.GetRole(currentRank.RoleID);
                        await guildUser.AddRoleAsync(newRole);
                        message += $"\nCongratulations, you have been promoted to {newRole.Name}.";
                    }
                }

                var nextRank = _ranks.FirstOrDefault(x => x.Invites > totalInvites);
                if (nextRank != null)
                {
                    message += $"\n{nextRank.Invites - totalInvites} more to become {Context.Guild.GetRole(nextRank.RoleID).Name}.";
                }

                await ReplyAsync(message);
            }
            catch (HttpException ex)
            {
                await ReplyAsync(ex.Message);
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }
    }
}