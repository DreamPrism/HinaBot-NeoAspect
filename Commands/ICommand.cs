using HinaBot_NeoAspect.Handler;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect.Commands
{
    public struct CommandArgs
    {
        public Func<string, Task> Callback;
        public string Arg;
        public Source Source;
    }

    public interface ICommand
    {
        List<string> Alias { get; }

        string Permission => null;
        Task Run(CommandArgs args);
    }
}
