using HinaBot_NeoAspect.Config;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect.Commands
{
    public class WhitelistCommand : HashCommand<Whitelist, long>
    {
        public override List<string> Alias => new()
        {
            "/whitelist"
        };

        protected override long GetTarget(long value) => value;
        protected override string Permission => "management.whitelist";
        public override async Task Run(CommandArgs args)
        {
            await base.Run(args);
        }
    }
}
