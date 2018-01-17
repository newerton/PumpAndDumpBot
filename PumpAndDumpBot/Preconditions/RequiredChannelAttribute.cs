using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace PumpAndDumpBot.Attributes
{
    public class RequiredChannelAttribute : PreconditionAttribute
    {
        private readonly List<ulong> channelIds;
        public RequiredChannelAttribute(params ulong[] channelIds)
        {
            this.channelIds = channelIds.ToList();
        }

        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!channelIds.Contains(context.Channel.Id)) return PreconditionResult.FromError("");
            return PreconditionResult.FromSuccess();
        }
    }
}