using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect.Models
{
    public class LocalData
    {
        public string Name;
        public string UpdateUrl;
        public JObject Data;
        public async Task<bool> Update()
        {
            try
            {
                if (UpdateUrl.StartsWith("http"))
                    Data = await Utils.GetHttpAsync(UpdateUrl);
                else
                    Data = JObject.Parse(File.ReadAllText(UpdateUrl));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
