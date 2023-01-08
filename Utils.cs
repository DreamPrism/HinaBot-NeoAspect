using HinaBot_NeoAspect.Config;
using HinaBot_NeoAspect.Handler;
using HinaBot_NeoAspect.Models;
using Newtonsoft.Json.Linq;
using Sora.Entities;
using Sora.Entities.Base;
using Sora.Entities.Info;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GroupMemberInfo = HinaBot_NeoAspect.Models.GroupMemberInfo;
using Image = System.Drawing.Image;
using Sora.Util;

namespace HinaBot_NeoAspect
{
    public static class Utils
    {
        public static Random rand = new();
        public static string ToHMS(this TimeSpan timeSpan)
        {
            StringBuilder sb = new();
            if (timeSpan.TotalDays >= 1) sb.Append($"{(int)timeSpan.TotalDays}d");
            if (timeSpan.TotalHours >= 1) sb.Append($"{(int)timeSpan.TotalHours % 24}h");
            if (timeSpan.TotalMinutes >= 1) sb.Append($"{(int)timeSpan.TotalMinutes % 60}m");
            if (timeSpan.TotalSeconds >= 1) sb.Append($"{(int)timeSpan.TotalSeconds % 60}s");
            return sb.ToString();
        }
        public static string FindAtMe(string origin, out bool isat, long qq)
        {
            if (qq == -1)
            {
                isat = false;
                return origin;
            }
            var at = CQCodeUtil.SerializeSegment(SoraSegment.At(qq));
            isat = origin.Contains(at);
            return origin.Replace(at, "");
        }

        public static async Task<List<GroupMemberInfo>> GetMemberList(this SoraApi session, long groupId)
        {
            return (await session.GetGroupMemberList(groupId)).groupMemberList
                .Select(info => new GroupMemberInfo
                {
                    GroupId = groupId,
                    QQId = info.UserId,
                    PermitType = info.Role switch
                    {
                        MemberRoleType.Owner => PermitType.Holder,
                        MemberRoleType.Admin => PermitType.Manage,
                        _ => PermitType.None
                    }
                }).ToList();
        }
        public static async Task<List<Models.GroupInfo>> GetGroupList0(this SoraApi session)
        {
            return (await session.GetGroupList())
                .groupList.Select(info => new Models.GroupInfo
                {
                    Id = info.GroupId,
                    Name = info.GroupName
                }).ToList();
        }

        public static int SetGroupSpecialTitle(this SoraApi session, long groupId, long qqId, string specialTitle, TimeSpan time)
        {
            throw new NotImplementedException();
        }
        public static string TryGetValueStart<T>(IEnumerable<T> dict, Func<T, string> conv, string start, out T value)
        {
            var matches = new List<Tuple<string, T>>();
            foreach (var pair in dict)
            {
                var key = conv(pair);
                if (key.StartsWith(start))
                {
                    if (key == start)
                    {
                        value = pair;
                        return null;
                    }
                    matches.Add(new Tuple<string, T>(key, pair));
                }
            }

            value = default;

            if (matches.Count == 0)
            {
                return $"No matches found for `{start}`";
            }

            if (matches.Count > 2)
            {
                return $"Multiple matches found : \n{string.Concat(matches.Select((pair) => pair.Item1 + "\n"))}";
            }

            value = matches[0].Item2;
            return null;
        }

        private static Regex codeReg = new Regex(@"^(.*?)\[(.*?)=(.*?)\](.*)$", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);

        private static string FixImage(this string imgcode)
        {
            return imgcode[1..37].Replace("-", "").ToLower() + ".image";
        }

        public static MessageBody GetMessageChain(string msg)
        {
            return CQCodeUtil.DeserializeMessage(msg.Decode()) ?? new();
            /*Match match;
            List<SoraSegment> result = new List<SoraSegment>();

            while ((match = codeReg.Match(msg)).Success)
            {
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                    result.Add(SoraSegment.Text(match.Groups[1].Value.Decode()));
                var val = match.Groups[3].Value;
                switch (match.Groups[2].Value)
                {
                    case "mirai:at": result.Add(SoraSegment.At(long.Parse(val))); break;
                    case "mirai:imageid": result.Add(SoraSegment.Image(val.Decode().FixImage(), false)); break;
                    case "mirai:imageurl": result.Add(SoraSegment.Image(val.Decode())); break;
                    case "mirai:imagepath": result.Add(SoraSegment.Image(val.Decode())); break;
                    case "mirai:imagenew": result.Add(SoraSegment.Image(val.Decode())); break;
                    case "mirai:atall": result.Add(SoraSegment.AtAll()); break;
                    case "mirai:json": result.Add(SoraSegment.Json(val.Decode())); break;
                    case "mirai:xml": result.Add(SoraSegment.Xml(val.Decode())); break;
                    case "mirai:poke": result.Add(SoraSegment.Poke(long.Parse(val))); break;
                    case "mirai:face": result.Add(SoraSegment.Face(int.Parse(val))); break;
                    case "CQ:at,qq": result.Add(SoraSegment.At(long.Parse(val))); break;
                    case "CQ:face,id": result.Add(SoraSegment.Face(int.Parse(val))); break;
                    case "CQ:image,file": result.Add(SoraSegment.Image(val.Decode())); break;
                    default: result.Add(SoraSegment.Text($"[{match.Groups[2].Value}={match.Groups[3].Value}]")); break;
                }
                msg = match.Groups[4].Value;
            }

            if (!string.IsNullOrEmpty(msg)) result.Add(SoraSegment.Text(msg.Decode()));

            return result.ToList();*/
        }
        public static string FixRegex(string origin)
        {
            return origin.Replace("[", @"\[").Replace("]", @"\]").Replace("&#91;", "[").Replace("&#93;", "]");
        }

        public static Bitmap LoadImage(this string path)
        {
            return Image.FromFile(path) as Bitmap;
        }

        public static async Task<string> GetName(this SoraApi session, long group, long qq)
        {
            try
            {
                return (await session.GetGroupMemberInfo(qq, group)).memberInfo.Card;
            }
            catch (Exception e)
            {
                Log(LoggerLevel.Error, e.ToString());
                return qq.ToString();
            }
        }

        public static async Task<string> GetName(this Source source)
            => await source.Session.GetName(source.FromGroup, source.FromQQ);

        internal static string GetCQMessage(MessageBody chain)
        {
            var msg = chain.SerializeMessage().Decode();
            return msg;
        }

        public static string Encode(this string str)
        {
            return str.Replace("&", "&amp;").Replace("[", "&#91;").Replace("]", "&#93;");
        }

        public static string Decode(this string str)
        {
            return str.Replace("&#91;", "[").Replace("&#93;", "]").Replace("&amp;", "&");
        }
        public static string GetImageCode(byte[] img)
        {
            var path = Path.Combine("imagecache", $"cache{rand.Next()}.jpg");
            File.WriteAllBytes(path, img);
            return CQCodeUtil.SerializeSegment(SoraSegment.Image(path));
        }

        public static Image Resize(this Image img, float scale)
        {
            var result = new Bitmap(img, new Size((int)(img.Width * scale), (int)(img.Height * scale)));
            img.Dispose();
            return result;
        }
        public static Image Resize(this Image img, int width, int height)
        {
            var result = new Bitmap(img, new Size(width, height));
            img.Dispose();
            return result;
        }

        public static string GetImageCode(Image img)
        {
            var path = Path.Combine("imagecache", $"cache{rand.Next()}.jpg");
            img.Save(path);
            return CQCodeUtil.SerializeSegment(SoraSegment.Image(path));
        }
        public static Image GetImageFromBase64(string base64)
        {
            using var stream = new MemoryStream(Convert.FromBase64String(base64));
            return Image.FromStream(stream);
        }
        public static string GetImageBase64Code(this Image img)
        {
            var stream = new MemoryStream();
            img.Save(stream, ImageFormat.Png);
            var bytes = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(bytes, 0, (int)stream.Length);
            return $"[CQ:image,file=base64://{Convert.ToBase64String(bytes)}]";
        }

        public static string ToCache(this byte[] b)
        {
            var path = Path.Combine("imagecache", $"cache{rand.Next()}");
            File.WriteAllBytes(path, b);
            return Path.GetFullPath(path);
        }

        private static Font font = new Font(FontFamily.GenericMonospace, 10f, FontStyle.Regular);
        private static Brush brush = Brushes.Black;
        public static string ToImageText(this string str)
        {
            using var bitmap = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bitmap);
            var lines = str.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var sizes = lines.Select(l => g.MeasureString(l, font)).ToArray();
            var img = new Bitmap((int)sizes.Max(s => s.Width) + 6, (int)sizes.Sum(s => s.Height) + 6);
            using (var g2 = Graphics.FromImage(img))
            {
                g2.Clear(Color.White);
                var h = 3f;
                for (int i = 0; i < lines.Length; ++i)
                {
                    g2.DrawString(lines[i], font, brush, 3, h);
                    h += sizes[i].Height;
                }
            }

            return GetImageCode(img);
        }

        public static void Log(this object o, LoggerLevel level, object s)
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = level switch
                {
                    LoggerLevel.Debug => ConsoleColor.White,
                    LoggerLevel.Info => ConsoleColor.Green,
                    LoggerLevel.Warn => ConsoleColor.Yellow,
                    LoggerLevel.Error => ConsoleColor.Red,
                    LoggerLevel.Fatal => ConsoleColor.Magenta,
                    _ => ConsoleColor.White
                };
                var now = DateTime.Now;
                var text = $"[{now:HH:mm:ss}] [{o.GetType().Name}/{level}] {s}";
                Console.WriteLine(text);
                Console.ResetColor();
                File.AppendAllText($"Data\\{now:yyyy-MM-dd}.log", text + "\n");
            }
        }

        public static void Log(LoggerLevel level, string s)
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = level switch
                {
                    LoggerLevel.Debug => ConsoleColor.White,
                    LoggerLevel.Info => ConsoleColor.Green,
                    LoggerLevel.Warn => ConsoleColor.Yellow,
                    LoggerLevel.Error => ConsoleColor.Red,
                    LoggerLevel.Fatal => ConsoleColor.Magenta,
                    _ => ConsoleColor.White
                };
                var now = DateTime.Now;
                var text = $"[{now:HH:mm:ss}] [{new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name}/{level}] {s}";
                Console.WriteLine(text);
                Console.ResetColor();
                File.AppendAllText($"Data\\{now:yyyy-MM-dd}.log", text + "\n");
            }
        }
        public static void ClearImageCache()
        {
            DirectoryInfo dir = new("imagecache");
            var files = dir.GetFiles("*.jpg");
            foreach (var file in files) file.Delete();
        }
        public static async Task<string> GetHttpContentAsync(string uri, int timeout = 5)
        {
            try
            {
                using HttpClient client = new();
                client.Timeout = new TimeSpan(0, 0, timeout);
                return await client.GetStringAsync(uri);
            }
            catch
            {
                return null;
            }
        }
        public static string GetHttpContent(string uri, int timeout = 5)
        {
            try
            {
                using HttpClient client = new();
                client.Timeout = new TimeSpan(0, 0, timeout);
                return client.GetStringAsync(uri).Result;
            }
            catch
            {
                return null;
            }
        }
        public static JObject GetHttp(string uri, int timeout = 5)
        {
            try
            {
                return JObject.Parse(GetHttpContent(uri, timeout));
            }
            catch
            {
                return null;
            }
        }

        public static async Task<JObject> GetHttpAsync(string uri, int timeout = 5)
        {
            try
            {
                return JObject.Parse(await GetHttpContentAsync(uri, timeout));
            }
            catch
            {
                return null;
            }
        }

        public static T ParseTo<T>(this string str)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)str;
            else
                return (T)typeof(T).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null)
                    .Invoke(null, new object[] { str });
        }

        private static DateTime dateTimeStart = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);

        // 时间戳转为C#格式时间
        public static DateTime ToDateTime(this long timeStamp)
        {
            return dateTimeStart.Add(new TimeSpan(timeStamp * 10000));
        }

        // DateTime时间格式转换为Unix时间戳格式
        public static long ToTimestamp(this DateTime time)
        {
            return (long)(time - dateTimeStart).TotalMilliseconds;
        }
        public static T Next<T>(this T[] arr)
        {
            return arr[rand.Next(arr.Length)];
        }
        public static T Next<T>(this List<T> list)
        {
            return list[rand.Next(list.Count)];
        }
        public static async Task<string> GetImageCodeFromBase64(string url)
        {
            using var client = new HttpClient();
            var base64 = await (await client.GetAsync(url)).Content.ReadAsStringAsync();
            return $"[CQ:image,file=base64://{base64}]";
        }
    }
}
