using HinaBot_NeoAspect.Config;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect.Handler
{
    class WhitelistHandler : IMessageHandler
    {
        public bool IgnoreCommandHandled => true;

        public async Task<bool> OnMessage(HandlerArgs args)
        {
            return !Configuration.GetConfig<Whitelist>().hash.Contains(args.Sender.FromGroup) &&
                !await args.Sender.HasPermission("ignore.whitelist", -1);
        }
    }
}
