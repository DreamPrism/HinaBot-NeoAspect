using Newtonsoft.Json.Linq;

namespace HinaBot_NeoAspect.Config
{
    public class Save : DictConfiguration<long, JObject>
    {
        public override string Name => "save.json";
    }
}
