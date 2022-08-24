using HinaBot_NeoAspect.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect.Config
{
    public class LocalDataConfiguration : DictConfiguration<string, LocalData>
    {
        public override string Name => "localdatas.json";
        public override void LoadDefault()
        {
            base.LoadDefault();
            this["song_list"] = new LocalData()
            {
                Name = "song_list",
                UpdateUrl = "https://bestdori.com/api/songs/all.7.json"
            };
            this["gacha_list"] = new LocalData()
            {
                Name = "gacha_list",
                UpdateUrl = "https://bestdori.com/api/gacha/all.5.json"
            };
            this["card_list"] = new LocalData()
            {
                Name = "card_list",
                UpdateUrl = "https://bestdori.com/api/cards/all.5.json"
            };
            foreach (var item in t)
            {
                if (item.value.Data == null)
                {
                    if (item.value.Update().Result)
                        Utils.Log(LoggerLevel.Info, $"{item.value.Name}获取成功");
                    else
                        Utils.Log(LoggerLevel.Error, $"{item.value.Name}获取失败");
                }
            }
            Save();
        }
        public override void LoadFrom(BinaryReader br)
        {
            base.LoadFrom(br);
            foreach (var item in t)
            {
                if (item.value.Data == null)
                {
                    Utils.Log(LoggerLevel.Info, $"{item.value.Name}无本地缓存数据，尝试获取...");
                    if (item.value.Update().Result)
                        Utils.Log(LoggerLevel.Info, $"{item.value.Name}获取成功！");
                    else
                        Utils.Log(LoggerLevel.Error, $"{item.value.Name}获取失败");
                }
                else
                {
                    Utils.Log(LoggerLevel.Info, $"{item.value.Name}已加载本地缓存数据");
                }
            }
            Save();
        }
    }
}
