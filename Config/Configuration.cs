using HinaBot_NeoAspect.Models;
using HinaBot_NeoAspect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Data;

namespace HinaBot_NeoAspect.Config
{
    public abstract class Configuration
    {
        public const string ConfigPath = "configs";
        private static readonly List<Configuration> instances = new();

        public static void Register<T>() where T : Configuration, new()
        {
            configs.Add(typeof(T), new T());
        }

        public static void Register<T>(T t) where T : Configuration
        {
            configs.Add(t.GetType(), t);
        }

        private static readonly Dictionary<Type, Configuration> configs = new();

        public static T GetConfig<T>() where T : Configuration
        {
            return configs[typeof(T)] as T;
        }

        public abstract string Name { get; }
        public abstract void SaveTo(BinaryWriter bw);
        public abstract void LoadFrom(BinaryReader br);
        public abstract void LoadDefault();

        public void Save()
        {
            using (FileStream fs = new(Path.Combine(ConfigPath, Name), FileMode.Create))
            using (BinaryWriter bw = new(fs))
                SaveTo(bw);
            Utils.Log(LoggerLevel.Info, $"{GetType().Name} successfully saved");
        }
        public void Load()
        {
            try
            {
                Dispose();
            }
            catch
            {

            }

            using (FileStream fs = new(Path.Combine(ConfigPath, Name), FileMode.Open))
            using (BinaryReader br = new(fs))
                LoadFrom(br);
            Utils.Log(LoggerLevel.Info, $"{GetType().Name} successfully loaded");
        }
        public static void SaveAll()
        {
            foreach (Configuration config in instances)
            {
                config.Save();
            }
        }

        public static void Save<T>() where T : Configuration
        {
            GetConfig<T>().Save();
        }

        public static void LoadAll()
        {
            foreach (var config in configs)
            {
                try
                {
                    config.Value.Load();
                }
                catch (Exception e)
                {
                    if (!(e is FileNotFoundException))
                    {
                        Utils.Log(LoggerLevel.Error, e.ToString());
                        //backup error file
                        File.Copy(Path.Combine(ConfigPath, config.Value.Name), Path.Combine(ConfigPath, config.Value.Name + ".errbak"));
                    }
                    config.Value.LoadDefault();
                }
            }
        }

        public virtual void Dispose() { }
    }
}
