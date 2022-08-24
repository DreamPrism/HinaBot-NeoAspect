using Microsoft.CodeAnalysis.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect.Services
{
    [JsonObject]
    public class Cutoff
    {
        public long time;
        [JsonProperty("ep")]
        public int Points;
        [JsonIgnore]
        public DateTime RealTime => time.ToDateTime();
        public void Deconstruct(out long ts, out int pt)
        {
            ts = time;
            pt = Points;
        }
    }
    public class Predictor
    {
        public static async Task<string> SimplePredict(string tier)
        {
            var e = BandoriUtils.GetPresentEvent();
            var rate = await BandoriUtils.GetEventRate(e.Item2["eventType"].Value<string>(), tier);
            var cutoffs = (await GetCutoffs(e.Item1, int.Parse(tier))).OrderBy(e => e.time);
            var result = Predict(cutoffs, rate, e.Item2["startAt"][3].Value<long>(), e.Item2["endAt"][3].Value<long>());
            return $"Last Pt:{cutoffs.Last().Points}({DateTime.Now - cutoffs.Last().RealTime}前)\nLatest predict:{result.Last().reg}";
        }
        public static async Task<List<Cutoff>> GetCutoffs(int eventId, int tier)
        {
            var ret = await Utils.GetHttpAsync($"https://bestdori.com/api/tracker/data?server=3&event={eventId}&tier={tier}");
            if (ret["result"].Value<bool>())
            {
                var list = ret["cutoffs"].ToObject<List<Cutoff>>();
                return list;
            }
            else return null;
        }
        public static List<(long ts, int reg)> Predict(IEnumerable<Cutoff> cutoffs, double rate, long start_ts, long end_ts)
        {
            List<Cutoff> cutoff = cutoffs.OrderBy(c => c.time).ToList();
            List<(double percent, int pt)> data = new();
            List<(long ts, int reg)> output = new();
            foreach (var (ts, pt) in cutoff)
            {
                if (ts - start_ts < 43200) continue;
                double percent = (ts - start_ts) / (end_ts - start_ts);
                data.Add((percent, pt));
                if (data.Count < 5 || !(start_ts + 86400 < ts && ts < end_ts - 86400)) continue;
                var (a, b, r2) = Regression(data);
                var reg = a + b * (1 + rate);
                output.Add((ts, (int)reg));
            }
            return output;
        }
        public static (double x, double y, double z) Regression(List<(double percent, int pt)> data)
        {
            double avg_percentage = data.Sum(d => d.percent) / data.Count;
            double avg_pt = data.Sum(d => d.pt * 1.0) / data.Count;
            double x = 0, y = 0, z = 0, w = 0;
            foreach (var (perc, pt) in data)
            {
                z += (perc - avg_percentage) * (pt - avg_pt);
                w += (perc - avg_percentage) * (perc - avg_percentage);
                x += (perc - avg_percentage) * (perc - avg_percentage);
                y += (pt - avg_pt) * (pt - avg_pt);
            }
            x = Math.Sqrt(x / data.Count);
            y = Math.Sqrt(y / data.Count);
            double b = z / w, a = avg_pt - b * avg_percentage, c = b * x / y;
            return (a, b, c * c);
        }
    }
}
