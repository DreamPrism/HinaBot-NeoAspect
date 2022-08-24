using HinaBot_NeoAspect.Config;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Configuration = HinaBot_NeoAspect.Config.Configuration;

namespace HinaBot_NeoAspect.Commands
{
    public class MotdCommand : ICommand
    {
        public List<string> Alias => new() { "/motd" };

        public async Task Run(CommandArgs args)
        {
            var cfg = Configuration.GetConfig<Motd>();
            if (args.Source.FromGroup == 0) return;
            var split = args.Arg.Split(' ');
            if (split.Length == 1) await args.Callback(cfg[args.Source.FromGroup] != null ? $"当前群的入群欢迎文本：\n{cfg[args.Source.FromGroup]}" : "当前群暂无入群欢迎文本");
            else
            {
                if (!await args.Source.HasPermission("motd.set")) return;
                cfg[args.Source.FromGroup] = args.Arg;
                await args.Callback($"设置成功！当前入群欢迎文本\n{args.Arg}");
                cfg.Save();
            }
        }
    }
}
