using HinaBot_NeoAspect.Config;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect.Services
{
    public static class BandoriUtils
    {
        public static string GetPresentEventId()
        {
            var eventList = Configuration.GetConfig<LocalDataConfiguration>()["event_list"].Data.ToObject<Dictionary<string, JToken>>();
            var id = eventList.LastOrDefault(k => k.Value["eventName"][3].Value<string>() != null).Key;
            return id;
        }
        public static (int, JObject) GetPresentEvent()
        {
            var events = Configuration.GetConfig<LocalDataConfiguration>()["event_list"].Data;
            var curTime = DateTime.Now.ToTimestamp();
            JObject ret = null;
            foreach (var kvp in events)
            {
                var e = kvp.Value;
                if (e["startAt"][3].Value<long>() <= curTime && curTime <= e["endAt"][3].Value<long>())
                {
                    ret = e.ToObject<JObject>();
                    return (int.Parse(kvp.Key), ret);
                }
            }
            return (0, ret);
        }
        public static async Task<double> GetEventRate(string eventType, string tier)
        {
            var rates = JArray.Parse(await Utils.GetHttpContentAsync($"https://bestdori.com/api/tracker/rates.json"));
            var rate = rates.FirstOrDefault(t => t["type"].Value<string>() == eventType && t["tier"].Value<string>() == tier)["rate"].Value<double>();
            return rate;
        }
    }
}
