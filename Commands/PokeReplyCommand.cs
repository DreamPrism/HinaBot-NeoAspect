using HinaBot_NeoAspect.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect.Commands
{
    public class PokeReplyCommand : ICommand
    {
        public List<string> Alias => new() { "/poke" };
        public async Task Run(CommandArgs args)
        {
            if (args.Source.FromGroup == 0) return;
            var cfg = Configuration.GetConfig<PokeReply>();
            var splits = args.Arg.Split(' ');
            if (splits.Length != 1&&await args.Source.HasPermission("pokereply.admin"))
            {
                cfg.t.Add(splits[1]);
                cfg.Save();
                await args.Callback("添加成功");
            }
        }
    }
}
