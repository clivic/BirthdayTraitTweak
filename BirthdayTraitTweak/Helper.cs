using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SandBox.GauntletUI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Core;
using TaleWorlds.Library;
using HarmonyLib;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;

namespace BirthdayTraitTweak
{
    class Helper
    {
        public static string FilesPath => Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
                                         $"\\Mount and Blade II Bannerlord\\{Config.MOD_NAME}\\";

        public static string SavePath => FilesPath + "Birthdays\\";

        public static string LogPath => FilesPath + $"{Config.MOD_NAME}_log.txt";

        public static string ConfigPath => FilesPath + $"config.ini";

        public static void ClearLog()
        {
            string logPath = Helper.LogPath;
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }

        public static void Log(string text)
        {
            File.AppendAllText(LogPath, text + Environment.NewLine);
        }

        public static void ShowMsg(string text)
        {
            InformationManager.DisplayMessage(new InformationMessage(text));
        }

        public static void ShowMsg(string text, ref Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(text, color));
        }

        public static void ShowAndLog(string text)
        {
            ShowMsg(text);
            Log(text);
        }

        public static void ShowAndLog(string text, Color color)
        {
            ShowMsg(text, ref color);
            Log(text);
        }

        public static void ReadConfig()
        {
            // Directories
            if (!Directory.Exists(FilesPath))
            {
                Directory.CreateDirectory(FilesPath);
            }
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }

            string filename = ConfigPath;
            if (!File.Exists(filename))
            {
                WriteConfig(Config.RWMode.Family.ToString());
            }

            string mode = null;
            foreach (var keyValuePair in ReadFile(filename))
            {
                var key = keyValuePair.Key;
                var value = keyValuePair.Value;

                if (key.Equals("Mode"))
                {
                    mode = value;
                }
            }

            // Read
            bool success = Config.SetMode(mode);
            if (!success)
            {
                if(mode == null)
                    ShowAndLog($"\"Mode\" entry not found. Please check {ConfigPath}, or delete the parent folder, switch back to the game.");
                else
                    Log($"Invalid mode \"{mode}\".");
            }
        }

        public static void WriteConfig(string mode)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Lines started with // are comments.");
            sb.AppendLine(Environment.NewLine);
            string availableOpts = string.Empty;
            foreach (var val in GetEnumValues(typeof(Config.RWMode)))
                if (val != "None")  availableOpts += $"{val}, ";
            availableOpts = availableOpts.TrimEnd(' ', ',');
            sb.AppendLine($"//Mode: (Available options: {availableOpts})");
            sb.AppendLine($"Mode={mode}");
            File.WriteAllText(ConfigPath, sb.ToString());

        }

        private static IEnumerable<string> GetEnumValues(Type enumType)
        {
            if (!enumType.IsEnum) yield break;

            foreach (int result in Enum.GetValues(enumType))
            {
                var str = Enum.ToObject(enumType, result).ToString();
                yield return str;
            }
        }

        public static IEnumerable<KeyValuePair<string, string>> ReadFile(string filename)
        {
            if (!File.Exists(filename)) yield break;
            string[] lines = File.ReadAllLines(filename);
            foreach (var line in lines)
            {
                if (line.StartsWith("//")) continue;
                if (!line.Contains("=")) continue;

                string[] arr = line.Split(new char[] { '=' });
                if (arr.Length < 2) continue;
                var key = arr[0].Trim();
                var value = arr[1].Trim();
                yield return new KeyValuePair<string, string>(key, value);
            }
        }

        public static CampaignTime CreateCampaignTime(long ticks)
        {
            ConstructorInfo ctor = typeof(CampaignTime).GetConstructor
                (BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(long) }, null);

            CampaignTime time = (CampaignTime)ctor.Invoke(new object[] { ticks });
            return time;
        }

        public static EncyclopediaPageVM GetEncycData(ref GauntletEncyclopediaScreenManager manager)
        {
            var dataInfo = typeof(GauntletEncyclopediaScreenManager).GetField("_encyclopediaData",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (dataInfo == null) return null;
            var data = (EncyclopediaData)dataInfo.GetValue(manager);
            if (data == null) { return null; };
            var dataSrcInfo =
                typeof(EncyclopediaData).GetField("_activeDatasource", BindingFlags.Instance | BindingFlags.NonPublic);
            if (dataSrcInfo == null) return null;

            var dataSrc = (EncyclopediaPageVM)dataSrcInfo.GetValue(data);
            return dataSrc;
        }

        public static bool CharacterHasTraitDeveloper(Hero character)
        {
            var heroInfo = typeof(HeroTraitDeveloper).GetProperty("Hero", BindingFlags.NonPublic | BindingFlags.Instance);
            if (heroInfo == null) return false;
            Hero traitDeveloperHero = (Hero)heroInfo.GetValue(Campaign.Current.PlayerTraitDeveloper);
            return traitDeveloperHero == character;
        }
    }

    public static class CampaignTimeExtension
    {
        public static long GetTicks(this CampaignTime time)
        {
            System.Type c = typeof(CampaignTime);
            System.Reflection.PropertyInfo ticksInfo = c.GetProperty("NumTicks", BindingFlags.NonPublic | BindingFlags.Instance);
            if (ticksInfo != null)
            {
                long ticks = (long)ticksInfo.GetValue(time);
                return ticks;
            }
            return 0;
        }
    }
}
