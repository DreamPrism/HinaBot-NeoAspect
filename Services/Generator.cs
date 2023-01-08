using HinaBot_NeoAspect.Config;
using HinaBot_NeoAspect.Handler;
using HinaBot_NeoAspect.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Entities.Segment;
using Sora.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design.Serialization;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect.Services
{
    public enum CardType
    {
        None, DreamFes, KiraFes, Limited
    }
    public enum Attr
    {
        powerful = 0,
        cool = 1,
        pure = 2,
        happy = 3,
        error = -1
    }
    public enum SkillType
    {
        score, judge, heal, shield
    }
    [JsonObject]
    public class CardInfo
    {
        public int characterId;
        public string resourceSetName;
        public string[] prefix;
        public byte rarity;
        public byte skillId;
        public string type;
        public Attr attribute;
    }
    [JsonObject]
    public class Skill
    {
        public string text;
        public string type;
        public int percent;
    }
    [JsonObject]
    public class CardFilter
    {
        public Dictionary<string, HashSet<string>> chara;
        public Dictionary<string, HashSet<string>> bands;
        public Dictionary<string, HashSet<string>> attribute;
        public Dictionary<string, HashSet<string>> type;
        public Dictionary<string, HashSet<string>> rarity;
        public Dictionary<string, HashSet<string>> skill;
    }
    public class Generator
    {
        private readonly string[] Attrs;
        private readonly string[] Bands;
        private readonly string[] Frames;
        private readonly string[] Miscs;
        private readonly CardFilter cardFilter;
        private readonly Image BackgroupImage;
        public Dictionary<string, CardInfo> CardMap { get; private set; }
        public Dictionary<string, string> Cards { get; private set; }
        public Dictionary<string, Skill> Skills { get; private set; }
        public static Generator Instance { get; internal set; }
        public string RootPath { get; }
        public string CachePath { get => Path.Combine(RootPath, "cachedCards"); }
        public Generator(string rootPath)
        {
            RootPath = rootPath;
            if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
            Skills = JsonConvert.DeserializeObject<Dictionary<string, Skill>>(File.ReadAllText(Path.Combine(RootPath, "skill_names.json")));

            Frames = new string[7];
            Frames[4] = Path.Combine(rootPath, "common", "card-2.png");
            Frames[5] = Path.Combine(rootPath, "common", "card-3.png");
            Frames[6] = Path.Combine(rootPath, "common", "card-4.png");

            Attrs = new string[4];
            Attrs[(int)Attr.powerful] = Path.Combine(rootPath, "common", "powerful.png");
            Frames[(int)Attr.powerful] = Path.Combine(rootPath, "common", "card-1-powerful.png");
            Attrs[(int)Attr.cool] = Path.Combine(rootPath, "common", "cool.png");
            Frames[(int)Attr.cool] = Path.Combine(rootPath, "common", "card-1-cool.png");
            Attrs[(int)Attr.happy] = Path.Combine(rootPath, "common", "happy.png");
            Frames[(int)Attr.happy] = Path.Combine(rootPath, "common", "card-1-happy.png");
            Attrs[(int)Attr.pure] = Path.Combine(rootPath, "common", "pure.png");
            Frames[(int)Attr.pure] = Path.Combine(rootPath, "common", "card-1-pure.png");

            Bands = new string[8];
            for (int i = 0; i < 8; ++i)
                Bands[i] = Path.Combine(rootPath, "common", $"band-{i + 1}.png");

            Miscs = new string[2];
            Miscs[0] = Path.Combine(rootPath, "common", "star.png");
            Miscs[1] = Path.Combine(rootPath, "common", "star_trained.png");
            BackgroupImage = LoadTexture(Path.Combine(RootPath, "common", "BG.png"));
            cardFilter = JsonConvert.DeserializeObject<CardFilter>(File.ReadAllText(Path.Combine(RootPath, "nickname.json")));
            LoadCardList();
            Utils.Log(LoggerLevel.Info, "Generator has been loaded successfully!");
        }
        public void LoadCardList()
        {
            var cardJson = Configuration.GetConfig<LocalDataConfiguration>()["card_list"].Data;
            CardMap = cardJson.ToObject<Dictionary<string, CardInfo>>();
            Cards = new();
            var cardpath = Path.Combine(RootPath, "card");
            foreach (var file in Directory.GetFiles(cardpath))
                Cards.Add(Path.GetFileNameWithoutExtension(file), file);
        }
        private string GetFrame(Attr attribute, byte rarity)
        {
            return rarity == 1 ? Frames[(int)attribute] : Frames[rarity + 2];
        }
        private string GetCard(string res) => Cards.ContainsKey(res) ? Cards[res] : null;
        private string GetAttribute(Attr attribute) => Attrs[(int)attribute];
        private string GetBands(byte band) => Bands[band];
        private string GetMisc(int index) => Miscs[index];
        private static Bitmap LoadTexture(string path)
        {
            var bmp = new Bitmap(path);
            bmp.SetResolution(96, 96);
            return bmp;
        }
        private Image LoadCharaImage(int charaId)
        {
            var bmp = new Bitmap(Path.Combine(RootPath, "chara", $"chara_icon_{charaId}.png"));
            bmp.SetResolution(96, 96);
            return bmp;
        }
        public async Task UpdateCardImages()
        {
            var savePath = Path.Combine(RootPath, "card");
            var json1 = await Utils.GetHttpAsync("https://bestdori.com/api/explorer/jp/assets/_info.json");
            var json2 = await Utils.GetHttpAsync("https://bestdori.com/api/explorer/cn/assets/_info.json");
            var packList1 = json1["thumb"]["chara"].Value<JObject>();
            var packList2 = json2["thumb"]["chara"].Value<JObject>();
            var downloadList = new List<(string, string)>();
            foreach (var pack in packList1)
            {
                var fileList = JArray.Parse(Utils.GetHttpContent($"https://bestdori.com/api/explorer/jp/assets/thumb/chara/{pack.Key}.json"));
                foreach (var token in fileList)
                {
                    var file = token.ToString();
                    if (file.ToString().EndsWith("png") && Cards.Keys.All(k => !file.StartsWith(k)))
                    {
                        var uri = $"https://bestdori.com/assets/jp/thumb/chara/{pack.Key}_rip/{file}";
                        downloadList.Add((file, uri));
                        Utils.Log(LoggerLevel.Info, $"File {file} has been added to download list");
                    }
                }
            }
            foreach (var pack in packList2)
            {
                var fileList = JArray.Parse(Utils.GetHttpContent($"https://bestdori.com/api/explorer/cn/assets/thumb/chara/{pack.Key}.json"));
                foreach (var token in fileList)
                {
                    var file = token.ToString();
                    if (file.ToString().EndsWith("png") && Cards.Keys.All(k => !file.StartsWith(k)))
                    {
                        var uri = $"https://bestdori.com/assets/cn/thumb/chara/{pack.Key}_rip/{file}";
                        downloadList.Add((file, uri));
                        Utils.Log(LoggerLevel.Info, $"File {file} has been added to download list");
                    }
                }
            }
            Utils.Log(LoggerLevel.Info, $"{downloadList.Count} files to download...");
            int cnt = 0;
            var tasks = new List<Task>();
            foreach (var kvp in downloadList)
            {
                tasks.Add(Task.Run(() =>
                {
#pragma warning disable SYSLIB0014 // 类型或成员已过时
                    var client = new WebClient();
#pragma warning restore SYSLIB0014 // 类型或成员已过时
                    Console.WriteLine($"Downloading: {kvp.Item2}");
                    try
                    {
                        client.DownloadFile(kvp.Item2, Path.Combine(savePath, kvp.Item1));
                        Console.WriteLine($"Download {kvp.Item2} successfully!");
                        cnt++;
                    }
                    catch
                    {
                        try
                        {
                            client.DownloadFile(kvp.Item2, Path.Combine(savePath, kvp.Item1));
                            Console.WriteLine($"Download {kvp.Item2} successfully!");
                            cnt++;
                        }
                        catch
                        {
                            try
                            {
                                client.DownloadFile(kvp.Item2, Path.Combine(savePath, kvp.Item1));
                                Console.WriteLine($"Download {kvp.Item2} successfully!");
                                cnt++;
                            }
                            catch
                            {
                                Utils.Log(LoggerLevel.Error, $"{kvp.Item2} download failed.");
                            }
                        }
                    }
                }));
            }
            Cards.Clear();
            var cardpath = Path.Combine(RootPath, "card");
            foreach (var file in Directory.GetFiles(cardpath))
                Cards.Add(Path.GetFileNameWithoutExtension(file), file);
            Task.WaitAll(tasks.ToArray());
            Console.Clear();
            Utils.Log(LoggerLevel.Info, $"All download tasks finished!({cnt}/{downloadList.Count})");
        }
        public void ClearInvalidImages()
        {
            var savePath = new DirectoryInfo(Path.Combine(RootPath, "card"));
            var files = savePath.GetFiles();
            Parallel.ForEach(files, file =>
            {
                try
                {
                    using Bitmap bitmap = new(file.FullName);
                }
                catch
                {
                    Utils.Log(LoggerLevel.Info, $"File {file.Name} has been deleted");
                    file.Delete();
                }
            });
        }
        public List<CardInfo> FilterCards(params string[] conditions)
        {
            var ret = new List<CardInfo>();
            foreach (var con in conditions)
            {
                var chara = cardFilter.chara.FirstOrDefault(k => k.Value.Contains(con));
                if (chara.Key != null) ret.AddRange(CardMap.Values.Where(c => c.characterId == int.Parse(chara.Key)));
            }
            return ret;
        }
        public Image GetCardImage(short cardId, bool transformed = false, bool drawData = true)
        {
            var cardPath = Path.Combine(RootPath, "cachedCards", $"{cardId}_{(transformed ? "trained" : "normal")}{(drawData ? "_data" : "")}.png");
            if (File.Exists(cardPath)) return Image.FromFile(cardPath);
            else
            {
                var img = GenerateCard(cardId, transformed, drawData);
                img.Save(cardPath);
                return img;
            }
        }
        public Image GetCardImage(CardInfo card, bool transformed = false, bool drawData = true)
        {
            var cardId = short.Parse(CardMap.FirstOrDefault(c => c.Value.resourceSetName == card.resourceSetName).Key);
            return GetCardImage(cardId, transformed, drawData);
        }
        public string GetCardImageCode(short cardId, bool transformed = false, bool drawData = true)
        {
            var cardPath = Path.Combine(RootPath, "cachedCards", $"{cardId}_{(transformed ? "trained" : "normal")}{(drawData ? "_data" : "")}.png");
            if (!File.Exists(cardPath))
            {
                var img = GenerateCard(cardId, transformed, drawData);
                img.Save(cardPath);
            }
            return CQCodeUtil.SerializeSegment(SoraSegment.Image(Path.GetFullPath(cardPath)));
        }
        public Image GenerateCard(CardInfo cardInfo, bool transformed = false, bool drawData = true)
        {
            var img = new Bitmap(180, 180);
            var resource = cardInfo.resourceSetName;
            var attribute = cardInfo.attribute;
            var rarity = cardInfo.rarity;
            transformed = transformed && rarity > 2;
            var band = (byte)((cardInfo.characterId - 1) / 5);

            using var tex = LoadTexture(GetCard(resource + (transformed ? "_after_training" : "_normal")) ??
                      GetCard(resource + "_normal")).Resize(180, 180);
            using var frame = LoadTexture(GetFrame(attribute, rarity));
            using var star = LoadTexture(GetMisc(transformed ? 1 : 0));
            using var bandtex = LoadTexture(GetBands(band)).Resize(41, 41);
            using var attr = LoadTexture(GetAttribute(attribute));

            using var canvas = Graphics.FromImage(img);
            canvas.Clear(Color.Transparent);
            canvas.DrawImage(tex, 0, 0);
            canvas.DrawImage(frame, 0, 0);
            canvas.DrawImage(bandtex, new RectangleF(2, 2, bandtex.Width * 1f, bandtex.Height * 1f));
            canvas.DrawImage(attr, new RectangleF(132, 2, 46, 46));

            for (int i = 0; i < rarity; ++i)
                canvas.DrawImage(star, new Rectangle(0, 170 - 28 * (i + 1), 35, 35));
            if (drawData)
            {
                Image typeimg = null;
                if (cardInfo.type == "dreamfes")
                    typeimg = LoadTexture(Path.Combine(RootPath, "common", "data", "D.png"));
                else if (cardInfo.type == "kirafes")
                    typeimg = LoadTexture(Path.Combine(RootPath, "common", "data", "K.png"));
                else if (cardInfo.type == "limited")
                    typeimg = LoadTexture(Path.Combine(RootPath, "common", "data", "L.png"));
                if (typeimg != null) canvas.DrawImage(typeimg, 137.5f, 91.5f);
                using var brush = new SolidBrush(Color.FromArgb(128, Color.Black));
                var skill = Skills[cardInfo.skillId.ToString()];
                var skillname = skill.text;
                using var font = new Font(FontFamily.GenericSansSerif, 18f, FontStyle.Regular);
                var textsize = canvas.MeasureString(skillname, font);
                SizeF size = textsize;
                Image icon = null;
                if (skill.type != "score")
                {
                    icon = LoadTexture(Path.Combine(RootPath, "common", "data", $"{skill.type}.png")).Resize(20, 20);
                    size.Width += icon.Width;
                }
                var rect = new RectangleF(172.5f - size.Width, 172.5f - size.Height, size.Width, size.Height);
                canvas.FillRectangle(brush, rect);
                canvas.DrawString(skillname, font, Brushes.White, rect.Location);
                if (icon != null) canvas.DrawImage(icon, new PointF(rect.Location.X + textsize.Width - 4f, rect.Location.Y + 4f));
            }

            img.MakeTransparent();

            tex.Dispose(); frame.Dispose(); star.Dispose(); bandtex.Dispose(); attr.Dispose();
            return img;

        }
        public Image DrawCustomCard(string origin, Attr attribute, byte rarity, byte band, bool transformed = false, bool drawData = false, string description = "", string skillType = "score", string type = "none")
        {
            var img = new Bitmap(180, 180);
            transformed = transformed && rarity > 2;
            using var tex = LoadTexture(origin).Resize(180, 180);
            using var frame = LoadTexture(GetFrame(attribute, rarity));
            using var star = LoadTexture(GetMisc(transformed ? 1 : 0));
            using var bandtex = LoadTexture(GetBands(band)).Resize(41, 41);
            using var attr = LoadTexture(GetAttribute(attribute));

            using var canvas = Graphics.FromImage(img);
            canvas.Clear(Color.Transparent);
            canvas.DrawImage(tex, 0, 0);
            canvas.DrawImage(frame, 0, 0);
            canvas.DrawImage(bandtex, new RectangleF(2, 2, bandtex.Width * 1f, bandtex.Height * 1f));
            canvas.DrawImage(attr, new RectangleF(132, 2, 46, 46));

            for (int i = 0; i < rarity; ++i)
                canvas.DrawImage(star, new Rectangle(0, 170 - 28 * (i + 1), 35, 35));
            if (drawData)
            {
                Image typeimg = null;
                if (type == "D")
                    typeimg = LoadTexture(Path.Combine(RootPath, "common", "data", "D.png"));
                else if (type == "K")
                    typeimg = LoadTexture(Path.Combine(RootPath, "common", "data", "K.png"));
                else if (type == "L")
                    typeimg = LoadTexture(Path.Combine(RootPath, "common", "data", "L.png"));
                if (typeimg != null) canvas.DrawImage(typeimg, 137.5f, 91.5f);
                if (description != "")
                {
                    using var brush = new SolidBrush(Color.FromArgb(128, Color.Black));
                    using var font = new Font(FontFamily.GenericSansSerif, 16f, FontStyle.Regular);
                    var textsize = canvas.MeasureString(description, font);

                    SizeF size = textsize;
                    Image icon = null;
                    if (skillType != "score" && File.Exists(Path.Combine(RootPath, "common", "data", $"{skillType}.png")))
                    {
                        icon = LoadTexture(Path.Combine(RootPath, "common", "data", $"{skillType}.png")).Resize(20, 20);
                        size.Width += icon.Width;
                    }

                    var rect = new RectangleF(172.5f - size.Width, 172.5f - size.Height, size.Width, size.Height);
                    canvas.FillRectangle(brush, rect);
                    canvas.DrawString(description, font, Brushes.White, rect.Location);
                    if (icon != null) canvas.DrawImage(icon, new PointF(rect.Location.X + textsize.Width - 4f, rect.Location.Y + 4f));
                }
            }

            img.MakeTransparent();

            tex.Dispose(); frame.Dispose(); star.Dispose(); bandtex.Dispose(); attr.Dispose();
            return img;
        }
        public Image GenerateCard(short cardId, bool transformed = false, bool drawData = true)
        {
            var cardInfo = CardMap[cardId.ToString()];
            return GenerateCard(cardInfo, transformed, drawData);
        }
        public Image GetComeBackMiracleExchange()
        {
            string miracleName = "カムバック★4ミラクルチケット";
            var miracleList = Configuration.GetConfig<LocalDataConfiguration>()["miracle_list"].Data.ToObject<Dictionary<string, JToken>>();
            var miracle = miracleList.Values.LastOrDefault(m => m["name"][0].ToObject<string>() != null && m["name"][0].Value<string>().Contains(miracleName) && m["name"][3].ToObject<string>() != null);
            var cards = miracle["ids"][3].Select(i => CardMap[i.ToString()]);
            return GenCardListImage(cards);
        }
        public Image GenCardListImage(IEnumerable<CardInfo> cardInfos, bool drawData = true)
        {
            var cardGroups = cardInfos.GroupBy(c => c.characterId).Select(g => (charaId: g.Key, cardGroups: g.GroupBy(c => c.attribute)));
            if (cardGroups.Count() > 5)
            {
                int[] maxs = new int[4] { 0, 0, 0, 0 };
                foreach (var g in cardGroups)
                    foreach (var c in g.cardGroups)
                        maxs[(int)c.Key] = Math.Max(maxs[(int)c.Key], c.Count());
                var width = (maxs.Sum() + 1) * (180 + 20) + 80 + 30 * 2;
                var height = cardGroups.Count() * (180 + 20) + (30 + 20) * 2;
                var img = GetBackgroud(width, height);
                using var canvas = Graphics.FromImage(img);
                PointF point = new(30, 30);
                maxs[0] = 80 + 30 + 10 + (180 + 20) * maxs[0];
                for (int i = 1; i < maxs.Length; i++) maxs[i] = maxs[i - 1] + (180 + 20) * maxs[i];
                foreach (var group in cardGroups.OrderBy(g => g.charaId))
                {
                    var icon = LoadCharaImage(group.charaId);
                    canvas.DrawImage(icon, point);
                    point.X += icon.Width + 10;
                    int curMax = -1;
                    bool[] drawed = new bool[4];
                    foreach (var cards in group.cardGroups.OrderBy(g => g.Key))
                    {
                        if ((int)cards.Key > 0 && !drawed[(int)cards.Key - 1])
                            point.X = maxs[(int)cards.Key - 1];
                        drawed[(int)cards.Key] = true;
                        int attr = (int)cards.Key;
                        foreach (var card in cards.OrderBy(c => c.rarity).ThenByDescending(c => Skills[c.skillId.ToString()].percent))
                        {
                            if (card.prefix[0] == null || card.type == "others") continue;
                            canvas.DrawImage(GetCardImage(card, true, drawData).Resize(180, 180), point);
                            point.X += 180 + 20;
                            curMax = Math.Max(curMax, (int)point.X);
                        }
                        point.X = maxs[attr];
                    }
                    point.X = 30; point.Y += 180 + 20;
                }
                return img;
            }
            else
            {
                var width = cardGroups.Max(g => g.cardGroups.Max(c => c.Count())) * (180 + 10) + 80 + 30 * 2;
                var height = cardGroups.Sum(g => g.cardGroups.Count()) * (180 + 20) + (30 + 20) * 2;
                var img = GetBackgroud(width, height);
                using var canvas = Graphics.FromImage(img);

                PointF point = new(30, 30);
                foreach (var group in cardGroups.OrderBy(g => g.charaId))
                {
                    var icon = LoadCharaImage(group.charaId);
                    canvas.DrawImage(icon, point);
                    point.X += icon.Width + 10;
                    foreach (var cards in group.cardGroups.OrderBy(g => g.Key))
                    {
                        foreach (var card in cards.OrderByDescending(c => c.rarity).ThenByDescending(c => Skills[c.skillId.ToString()].percent))
                        {
                            if (card.prefix[0] == null || card.type == "others") continue;
                            canvas.DrawImage(GetCardImage(card, true, drawData), point);
                            point.X += 180 + 20;
                        }
                        point.X = icon.Width + 30 + 10; point.Y += 180 + 20;
                    }
                    point.X = 30;
                }
                return img;
            }
        }
        public Image GetBackgroud(int width, int height)
        {
            Bitmap img = new(width, height);
            using var canvas = Graphics.FromImage(img);
            for (int i = 0; i < width; i += BackgroupImage.Width)
                for (int j = 0; j < height; j += BackgroupImage.Height)
                    canvas.DrawImage(BackgroupImage, i, j);
            return img;
        }
    }
}
