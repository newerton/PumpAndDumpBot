using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PumpAndDumpBot.Attributes;

namespace PumpAndDumpBot.Modules
{
    [Name("Info")]
    [Summary("General information")]
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("invite", RunMode = RunMode.Async)]
        [Summary("Returns the OAuth2 Invite URL of the bot")]
        [Remarks("invite")]
        public async Task InviteAsync()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            if (Context.Message.Author.Id != application.Owner.Id)
                return;
            
            // https://discordapi.com/permissions.html
            await ReplyAsync($"<https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot&permissions=268528672>");
        }

        [Command("ping", RunMode = RunMode.Async)]
        [Summary("Ping to see the latency")]
        [Remarks("ping")]
        public async Task PingAsync()
            => await ReplyAsync($"Pong! - {Context.Client.Latency}ms");

        [Command("botinfo", RunMode = RunMode.Async)]
        [Summary("General info about the bot")]
        [Remarks("botinfo")]
        public async Task InfoAsync()
        {
            var application = await Context.Client.GetApplicationInfoAsync();

            await ReplyAsync("", embed: new EmbedBuilder()
                .WithColor(Program.EMBED_COLOR)
                .AddField("Info",
                    $"**Author:** `{application.Owner}` [ID: {application.Owner.Id}]\n" +
                    $"**Library:** Discord.Net - Version: {DiscordConfig.Version}\n" +
                    $"**Total Channels:** {Context.Client.Guilds.Sum(g => g.Channels.Count())}\n" +
                    $"**Total Users:** {Context.Client.Guilds.Sum(g => g.Users.Where(b => !b.IsBot).Count())}")
                .AddField("Process Info",
                    $"**Runtime:** {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                    $"**Heap Size:** {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString()}MB\n" +
                    $"**Uptime**: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"d\d\ h\h\ m\m\ s\s")}")
                .Build());
        }

        [Command("suggestion", RunMode = RunMode.Async)]
        [Summary("Send in suggestions for new features or improvements for the bot.")]
        [Remarks("suggestion <your suggestion>")]
        public async Task SuggestionAsync([Remainder] string suggestion)
        {
            try
            {
                var channel = Context.Client.GetChannel(400674114974646272) as IMessageChannel;
                if (channel == null)
                {
                    var application = await Context.Client.GetApplicationInfoAsync();
                    await ReplyAsync($"Can't find the suggestion channel, please contact {application.Owner}.");
                    return;
                }

                var msg = await channel.SendMessageAsync("", embed: new EmbedBuilder()
                    .WithColor(Program.EMBED_COLOR)
                    .WithTitle("Suggestion")
                    .WithDescription(suggestion.Trim())
                    .WithFooter($"Submitted by {Context.Message.Author}")
                    .Build());

                await msg.AddReactionAsync(new Emoji("✅"));
                await msg.AddReactionAsync(new Emoji("❌"));

                await ReplyAsync("Succesfully submitted the suggestion.");
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("game", RunMode = RunMode.Async)]
        [Summary("Change the bot's game text.")]
        [Remarks("game <new game text>")]
        [RequiredChannel(401021271556620288, 400552471010869248)]
        public async Task GameAsync([Remainder] string game)
        {
            try
            {
                await Context.Client.SetGameAsync(game);
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }
    }
}