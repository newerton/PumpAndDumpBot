using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace PumpAndDumpBot.Modules
{
    public class DeveloperModule : ModuleBase<SocketCommandContext>
    {
        [Command("roles", RunMode = RunMode.Async)]
        [Summary("Returns the role ID")]
        [RequireContext(ContextType.Guild)]
        public async Task RolesAsync()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            if (Context.Message.Author.Id != application.Owner.Id)
                return;

            string msg = "**__Roles__**";
            foreach (IRole role in Context.Guild.Roles.Where(x => x.Id != Context.Guild.EveryoneRole.Id))
                msg += $"\n{role.Name} ({role.Id})";

            await ReplyAsync(msg);
        }
    }
}