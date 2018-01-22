using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace PumpAndDumpBot.Attributes
{
    /// <summary> Sets what channel the command or any command
    /// in this module can be used in. </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RequireChannelAttribute : PreconditionAttribute
    {
        private readonly HashSet<ulong> _channelIds = new HashSet<ulong>();

        /// <summary> e.g. [RequireChannel(123456789012345678, 01234567890123456789)]</summary>
        /// <param name="channelIds">The params array of channels the command can be used in.</param>
        public RequireChannelAttribute(params ulong[] channelIds) : this(channelIds.ToList())
        { }

        /// <summary> It takes a list of parameters. e.g. [RequireChannel(123456789012345678, 01234567890123456789)]</summary>
        /// <param name="channelIds">The list of channels the command can be used in.</param>
        public RequireChannelAttribute(IEnumerable<ulong> channelIds)
        {
            foreach (ulong channelId in channelIds)
                _channelIds.Add(channelId);
        }

        /// <inheritdoc />
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!_channelIds.Contains(context.Channel.Id)) return Task.FromResult(PreconditionResult.FromError(""));
            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}