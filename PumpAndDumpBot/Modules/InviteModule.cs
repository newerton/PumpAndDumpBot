﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Preconditions;
using Discord.Commands;
using Discord.WebSocket;
using PumpAndDumpBot.Attributes;
using PumpAndDumpBot.Data;
using PumpAndDumpBot.Models;

namespace PumpAndDumpBot.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireChannel(400560041947693064, 400552471010869248)]
    public class InviteModule : ModuleBase<SocketCommandContext>
    {
        private static readonly List<Affiliate> _ranks = new List<Affiliate>()
        {
            new Affiliate(399769377194377216, 1),
            new Affiliate(400361570959622144, 10),
            new Affiliate(400361693672112128, 35),
            new Affiliate(400362357412331523, 75),
            new Affiliate(400362301481418752, 150),
            new Affiliate(400362225317183489, 250),
            new Affiliate(405756048331374592, 500)
        };

        [Command("invites", RunMode = RunMode.Async)]
        [Ratelimit(1, 1, Measure.Minutes)]
        [Summary("Returns the total invites for this user.")]
        public async Task InvitesAsync()
        {
            var roles = Context.Guild.Roles;
            try
            {
                var author = Context.Message.Author as SocketGuildUser;

                var invites = await Database.GetInviteCountAsync(author.Id);
                string message = $"{author.Mention}\nYou have {invites} invites.";

                var currentRank = _ranks.LastOrDefault(x => x.Invites <= invites);
                if (currentRank != null)
                {
                    if (!author.Roles.Select(x => x.Id).Contains(currentRank.RoleID))
                    {
                        var newRank = Context.Guild.GetRole(currentRank.RoleID);
                        //add new role
                        await author.AddRoleAsync(newRank);

                        message += $"\nCongratulations, you have been promoted to {newRank.Name}.";
                    }
                }

                //remove any of the other roles the user might have that are below or above his rank
                HashSet<ulong> rankRoleIds = new HashSet<ulong>(_ranks.Select(r => r.RoleID));
                var rolesToRemove = author.Roles.Where(x => rankRoleIds.Contains(x.Id) && x.Id != currentRank?.RoleID).ToList();
                if (rolesToRemove.Count() > 0)
                    await author.RemoveRolesAsync(rolesToRemove);

                var nextRank = _ranks.FirstOrDefault(x => x.Invites > invites);
                if (nextRank != null)
                {
                    message += $"\n{nextRank.Invites - invites} more to become {Context.Guild.GetRole(nextRank.RoleID).Name}.";
                }

                await ReplyAsync(message);
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }
    }
}