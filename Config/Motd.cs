using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect.Config
{
    public class Motd : DictConfiguration<long, string>
    {
        public override string Name => "motds.json";
    }
}
