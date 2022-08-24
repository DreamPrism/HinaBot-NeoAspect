using System.Collections.Generic;

namespace HinaBot_NeoAspect.Config
{
    public class PermissionConfig : SerializableConfiguration<Dictionary<long, HashSet<string>>>
    {
        public override string Name => "perms.json";
    }
}
