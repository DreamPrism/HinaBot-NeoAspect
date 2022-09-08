using HinaBot_NeoAspect.Config;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.CodeAnalysis.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        public Cutoff(long time, int points)
        {
            this.time = time;
            Points = points;
        }

        public void Deconstruct(out long ts, out int pt)
        {
            ts = time;
            pt = Points;
        }
    }
    [JsonObject]
    public class Event
    {
        [JsonIgnore]
        public int Id;
        [JsonProperty]
        public string eventType;
        [JsonProperty]
        public string[] eventName;
        [JsonProperty]
        public string[] startAt;
        [JsonIgnore]
        public long start_ts;
        [JsonProperty]
        public string[] endAt;
        [JsonIgnore]
        public long end_ts;
    }
    public static class EventTracker
    {
        public static string Func(Match match)
        {
            if (int.TryParse(match.Groups[1].Value, out int id) && int.TryParse(match.Groups[2].Value, out int tier) && EventTracker.GenEventCutoffsImage(id, tier))
            {
                var path = Path.Combine("imagecache", "chart.jpg");
                return $"[mirai:imagepath={path}]";
            }
            else
            {
                return "输入有误";
            }
        }
        public static bool GenEventCutoffsImage(int eventId, int tier, bool force = true)
        {
            var data = Configuration.GetConfig<LocalDataConfiguration>()["event_list"].Data;
            var evt = JsonConvert.DeserializeObject<Event>(data[eventId.ToString()].ToString());
            evt.Id = eventId;
            var server = 3;
            if (!force)
            {
                if (evt.eventName[server] == null) return false;
            }
            var name = evt.eventName[server] ?? evt.eventName[0];
            const long timeStartZero = 1662440400000;
            const long timeEndZero = 1663081140000;
            long start_ts, end_ts;
            if (evt.startAt[server] != null) start_ts = long.Parse(evt.startAt[server]);
            else start_ts = timeStartZero + 9 * 24 * 60 * 60 * 1000 * (eventId - 170);
            if (evt.endAt[server] != null) end_ts = long.Parse(evt.endAt[server]);
            else end_ts = timeEndZero + 9 * 24 * 60 * 60 * 1000 * (eventId - 170);

            var cutoffs = Utils.GetHttp($"https://bestdori.com/api/tracker/data?server=3&event={eventId}&tier={tier}")["cutoffs"].Select(t => new Cutoff(t["time"].Value<long>(), t["ep"].Value<int>())).OrderBy(c => c.time).ToList();
            var rateobj = JArray.Parse(Utils.GetHttpContent("https://bestdori.com/api/tracker/rates.json")).FirstOrDefault(j => (int)j["server"] == server && (int)j["tier"] == tier && j["type"].ToString() == evt.eventType);
            if (rateobj == null) return false;
            double rate = (double?)rateobj["rate"] ?? 0;
            var predict = cutoffs.Count >= 5 && rate != 0;
            var line0 = new LineSeries<DateTimePoint>()
            {
                Fill = null,
                GeometrySize = 0,
                Stroke = null,
                Values = new DateTimePoint[] { new(Utils.ToDateTime(end_ts - 24 * 60 * 60 * 1000), 1000000) }
            };
            var line1 = new LineSeries<DateTimePoint>()
            {
                Values = (from c in cutoffs select new DateTimePoint(c.RealTime, c.Points)),
                Fill = null,
                GeometrySize = 6.5,
                Name = "实时线",
                LineSmoothness = 0.6,
                Stroke = new SolidColorPaint(SKColors.DarkBlue) { StrokeThickness = 3.5f },
                GeometryStroke = new SolidColorPaint(SKColors.DarkBlue)
            };
            line1.GeometryFill = line1.GeometryStroke;

            SKCartesianChart chart = new()
            {
                Width = 1000,
                Height = 800,
                Series = new ISeries[]
                {
                    line0, line1
                },
                XAxes = new[]
                {
                    new Axis()
                    {
                        Labeler = v=>new DateTime((long)v).ToString("MM-dd"),
                        //MinLimit = Utils.ToDateTime(start_ts+12*60*60*1000).Ticks,
                        //MaxLimit = Utils.ToDateTime(end_ts+60*1000).Ticks,
                        UnitWidth = TimeSpan.FromDays(1).Ticks,
                        MinStep = TimeSpan.FromDays(1).Ticks,
                        ShowSeparatorLines = true,
                        Name=$"Event ID:{eventId} TOP{tier}    Latest PT:{(cutoffs.Count>0?$"{cutoffs.Last().Points}({(DateTime.Now-cutoffs.Last().RealTime).ToHMS()} ago)":"N/A")}, Latest Prediction:Need more data / lack rate" ,
                        NameTextSize=20,
                        NamePaint=new SolidColorPaint(SKColors.DarkGray)
                    }
                },
                YAxes = new[]
                {
                    new Axis()
                    {
                        MinLimit = 0
                    }
                },
                LegendPosition = LegendPosition.Top,
                Background = SKColor.Parse("#ffffff"),
                AutoUpdateEnabled = true
            };
            if (predict)
            {
                var predictions = Predict(cutoffs, rate, start_ts, end_ts);
                var strokeThickness = 3.5f;
                var strokeDashArray = new float[] { 3 * strokeThickness, 2 * strokeThickness };
                var effect = new DashEffect(strokeDashArray);
                var line2 = new LineSeries<DateTimePoint>()
                {
                    Values = from c in predictions select new DateTimePoint(Utils.ToDateTime(c.ts), c.pt),
                    Fill = null,
                    GeometrySize = 6.5,
                    Name = "预测线",
                    LineSmoothness = 0.6,
                    Stroke = new SolidColorPaint(SKColors.Aqua) { StrokeThickness = strokeThickness, PathEffect = effect },
                    GeometryStroke = new SolidColorPaint(SKColors.Aqua)
                };
                line2.GeometryFill = line2.GeometryStroke;
                chart.Series = chart.Series.Append(line2);
                chart.XAxes.ElementAt(0).Name = $"Event ID:{eventId} TOP{tier}    Latest PT:{(cutoffs.Count > 0 ? $"{cutoffs.Last().Points}({(DateTime.Now - cutoffs.Last().RealTime).ToHMS()} ago)" : "N/A")}, Latest Prediction:{(predictions.Count > 0 ? $"{predictions.Last().pt}" : "N/A")}";
            }
            var savePath = Path.Combine("imagecache", "chart.jpg");
            chart.SaveImage(savePath, SKEncodedImageFormat.Jpeg, 100);
            return true;
        }
        private static List<(long ts, int pt)> Predict(IEnumerable<Cutoff> cutoffs, double rate, long start_ts, long end_ts)
        {
            cutoffs = cutoffs.OrderBy(c => c.time);
            var data = new List<(double percent, int pt)>();
            var output = new List<(long ts, int pt)>();
            foreach (var (ts, pt) in cutoffs)
            {
                if (ts - start_ts < 43200000 || !(ts < end_ts - 86400000)) continue;

                data.Add(((ts - start_ts) * 1.0 / (end_ts - start_ts) * 1.0, pt));
                if (data.Count < 5) continue;
                (double a, double b, double r2) = Regression(data);
                double reg = a + b * (1 + rate);
                output.Add((ts, (int)reg));
            }
            return output;
        }
        private static (double a, double b, double c) Regression(List<(double percent, int pt)> data)
        {
            var avg_percentage = data.Average(d => d.percent);
            var avg_pt = data.Average(d => d.pt);
            double x, y, z, w, a, b, c;
            x = y = z = w = 0;
            foreach (var (perc, pt) in data)
            {
                z += (perc - avg_percentage) * (pt - avg_pt);
                w += (perc - avg_percentage) * (perc - avg_percentage);
                x += (perc - avg_percentage) * (perc - avg_percentage);
                y += (pt - avg_pt) * (pt - avg_pt);
            }
            x = Math.Sqrt(x / data.Count);
            y = Math.Sqrt(y / data.Count);
            b = z / w;
            a = avg_pt - b * avg_percentage;
            c = b * x / y;
            return (a, b, c * c);
        }
    }
}
