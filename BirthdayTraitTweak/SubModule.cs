using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;
using Module = TaleWorlds.MountAndBlade.Module;

using SandBox.GauntletUI;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.Data;

namespace BirthdayTraitTweak
{
    public class SubModule : MBSubModuleBase
    {
        private Hero currChar = null;

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            // Directories and Config.ini
            Helper.ReadConfig();
            // Logs
            Helper.ClearLog();
            Helper.ShowAndLog($"Initialized {Config.MOD_NAME} mod.");
            if (!(game.GameType is Campaign)) { Helper.Log("Game mode is not campaign."); return; }
            Helper.Log("Game mode is campaign.");

            // Current hero is by default the playerChar.
            currChar = this.PlayerChar;
            Helper.Log(currChar == null
                ? $"Unable to get the player's character. Current Character is null."
                : $"Current Character: {currChar.Name}, age: {currChar.Age}");

            base.OnGameStart(game, gameStarterObject);

            // Bind events
            game.EventManager.RegisterEvent<EncyclopediaPageChangedEvent>((e) =>
           {
               EncyclopediaData.EncyclopediaPages type = e.NewPage;
               if (type == EncyclopediaData.EncyclopediaPages.Hero)
               {
                   GauntletEncyclopediaScreenManager m = MapScreen.Instance.EncyclopediaScreenManager as GauntletEncyclopediaScreenManager;
                   var data = Helper.GetEncycData(ref m);
                   if (data != null)
                   {
                       currChar = (Hero)data.Obj;
                       Helper.Log(CurrentCharacterLogString);
                   }
               }
           });

        }

        private string CurrentCharacterLogString => $"Current Character is {currChar?.Name.ToString() ?? "null"}";

        private bool TryToSelectPlayerCharIfNull()
        {
            if (currChar == null)
            {
                currChar = PlayerChar;
            }

            return currChar != null;
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);

            if (!InputKey.LeftControl.IsDown() && !InputKey.RightControl.IsDown()) return;
            if (!InputKey.B.IsDown()) return;
            if (Campaign.Current == null) return;
            if (!Campaign.Current.GameStarted) return;

            if (InputKey.S.IsReleased())
            {
                if (!TryToSelectPlayerCharIfNull()) return;

                // Refresh the config.
                Helper.ReadConfig();
                if (Config.Mode == Config.RWMode.Single)
                {
                    Export(currChar);
                }
                else if (Config.Mode == Config.RWMode.Family)
                {
                    ExportWholeFamily(currChar);
                }
                else Helper.ShowMsg($"Invalid mode \"{Config.ModeStr}\". Please check {Helper.ConfigPath}");
            }

            if (InputKey.L.IsReleased())
            {
                if (!TryToSelectPlayerCharIfNull()) return;

                // Refresh the config.
                Helper.ReadConfig();
                if (Config.Mode == Config.RWMode.Single)
                {
                    Import(currChar);
                }
                else if (Config.Mode == Config.RWMode.Family)
                {
                    ImportWholeFamily(currChar);
                }
                else Helper.ShowMsg($"Invalid mode \"{Config.ModeStr}\". Please check {Helper.ConfigPath}");
            }

            if (InputKey.T.IsReleased())
            {
                if (!TryToSelectPlayerCharIfNull()) return;
               
                // Refresh the config.
                Helper.ReadConfig();
                // Toggle the Read Write mode
                if (Config.Mode == Config.RWMode.Single)
                    Config.SetMode(Config.RWMode.Family);
                else if (Config.Mode == Config.RWMode.Family)
                    Config.SetMode(Config.RWMode.Single);

                // Overwrite the config file.
                Helper.WriteConfig(Config.ModeStr);
                string str = Config.Mode == Config.RWMode.Family ? "and family." : "only.";
                Helper.ShowAndLog($"Mode: Import/Export Current Character {currChar} {str}");
            }
        }

        public Hero PlayerChar
        {
            get
            {
                if (Game.Current == null)
                {
                    return null;
                }
                return CharacterObject.PlayerCharacter?.HeroObject;
            }
        }

        /// <summary>
        /// Includes character themselves.
        /// </summary>
        public IEnumerable<Hero> GetFamily(Hero character)
        {
            List<Hero> family = new List<Hero>();
            GetFamilyUp(character, ref family);
            GetFamilyDown(character, ref family);

            foreach (var member in family)
            {
                yield return member;
            }
        }

        /// <summary>
        /// Includes your parents, two grand parents, four grand grand parents etc.
        /// Includes your uncles/aunts, and all of their children (your cousins/nephews, for example)
        /// </summary>
        public void GetFamilyUp(Hero character, ref List<Hero> family)
        {
            if (character == null) return;
            if (!family.Contains(character)) family.Add(character);
            GetFamilyUp(character.Father, ref family);
            GetFamilyUp(character.Mother, ref family);
            foreach (var child in character.Siblings)
            {
                // Share the same ancestry. Only needs to search downwards.
                GetFamilyDown(child, ref family);
            }
        }

        /// <summary>
        /// Includes all of your children, and all the children of your children.
        /// Doesn't include the spouses of your children.
        /// </summary>
        public void GetFamilyDown(Hero character, ref List<Hero> family)
        {
            if (character == null) return;
            if (!family.Contains(character)) family.Add(character);
            foreach (var child in character.Children)
            {
                GetFamilyDown(child, ref family);
            }
        }

        public string GetSaveName(Hero character) => $"{Helper.SavePath}{character.Name}.txt";
        public void Export(Hero character)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Lines started with // are comments.");
            sb.AppendLine(string.Empty);
            sb.AppendLine("//Readme:");
            sb.AppendLine("//Season ranges from 0 to 3. Spring is 0 and Autumn is 2, for example.");
            sb.AppendLine("//Day starts with 0. So if your birthday is Summer 11, Day should be 10.");
            sb.AppendLine("//Use the field \"FormattedBirthday\" to check what your birthday is, after export.");
            sb.AppendLine("//If you want to modify RemainingTicks, remember there are 10000 ticks per game second.");
            sb.AppendLine("//Traits range from -2 to 2.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("//Main character birthday:");
            sb.AppendLine($"Year={character.BirthDay.GetYear}");
            sb.AppendLine($"Season={character.BirthDay.GetSeasonOfYear}");
            sb.AppendLine($"Day={character.BirthDay.GetDayOfSeason}");
            sb.AppendLine($"Hour={character.BirthDay.GetHourOfDay}");
            // Get remaining ticks so we don't lose any milliseconds.
            var b = CampaignTime.Years(character.BirthDay.GetYear) +
                    CampaignTime.Seasons(character.BirthDay.GetSeasonOfYear) +
                    CampaignTime.Days(character.BirthDay.GetDayOfSeason) +
                    CampaignTime.Hours(character.BirthDay.GetHourOfDay);
            sb.AppendLine($"RemainingTicks={(character.BirthDay - b).GetTicks()}");
            sb.AppendLine(Environment.NewLine);

            // Traits
            sb.AppendLine("//Main character traits:");
            var traits = character.GetHeroTraits();
            sb.AppendLine($"Mercy={traits.Mercy}");
            sb.AppendLine($"Valor={traits.Valor}");
            sb.AppendLine($"Honor={traits.Honor}");
            sb.AppendLine($"Generosity={traits.Generosity}");
            sb.AppendLine($"Calculating={traits.Calculating}");
            sb.AppendLine(Environment.NewLine);
            ;
            // Output.
            sb.AppendLine("//Don't modify this field as it will not be read.");
            sb.AppendLine($"FormattedBirthday={character.BirthDay.ToString()}");
            File.WriteAllText(this.GetSaveName(character), sb.ToString());

            Helper.ShowAndLog($"Exported {character.Name}'s birthday and traits.");
        }

        public void ExportWholeFamily(Hero character)
        {
            foreach (var familyMember in GetFamily(character))
            {
                Export(familyMember);
            }
        }

        public void Import(Hero character)
        {
            string filename = GetSaveName(character);
            int? year = null, season = null, day = null, hour = null;
            long? remaining = null;
            int? mercy = null, valor = null, honor = null, generosity = null, calculating = null;
            foreach (var keyValuePair in Helper.ReadFile(filename))
            {
                var key = keyValuePair.Key;
                var value = keyValuePair.Value;

                // Parse
                try
                {
                    // Birthday
                    if (key.Equals("Year"))
                    {
                        year = Convert.ToInt32(value);
                    }
                    else if (key.Equals("Season"))
                    {
                        season = Convert.ToInt32(value);
                    }
                    else if (key.Equals("Day"))
                    {
                        day = Convert.ToInt32(value);
                    }
                    else if (key.Equals("Hour"))
                    {
                        hour = Convert.ToInt32(value);
                    }
                    else if (key.Equals("RemainingTicks"))
                    {
                        remaining = Convert.ToInt64(value);
                    }

                    // Traits
                    else if (key.Equals("Mercy"))
                    {
                        mercy = Convert.ToInt32(value);
                    }
                    else if (key.Equals("Valor"))
                    {
                        valor = Convert.ToInt32(value);
                    }
                    else if (key.Equals("Honor"))
                    {
                        honor = Convert.ToInt32(value);
                    }
                    else if (key.Equals("Generosity"))
                    {
                        generosity = Convert.ToInt32(value);
                    }
                    else if (key.Equals("Calculating"))
                    {
                        calculating = Convert.ToInt32(value);
                    }
                }
                catch { Helper.ShowAndLog($"Failed to parse {key} for {character}"); return; }
            }

            bool readBirthdaySuccess = false;
            bool readTraitsSuccess = false;
            // Read Birthday
            if (year.HasValue &&
                season.HasValue &&
                day.HasValue &&
                hour.HasValue &&
                remaining.HasValue)
            {
                var b = CampaignTime.Years(year.Value) +
                        CampaignTime.Seasons(season.Value) +
                        CampaignTime.Days(day.Value) +
                        CampaignTime.Hours(hour.Value);
                long newTicks = b.GetTicks();
                newTicks += remaining.Value;

                var newBirthDay = Helper.CreateCampaignTime(newTicks);
                character.BirthDay = newBirthDay;
                // Update character model
                var dps = character.DynamicBodyProperties;
                dps.Age = character.Age;
                character.DynamicBodyProperties = dps;

                readBirthdaySuccess = true;
            }

            // Read Traits
            if (mercy.HasValue &&
                valor.HasValue &&
                honor.HasValue &&
                generosity.HasValue &&
                calculating.HasValue)
            {
                // It will clamp to min max value in SetTraitLevel()
                character.SetTraitLevel(DefaultTraits.Mercy, mercy.Value);
                character.SetTraitLevel(DefaultTraits.Valor, valor.Value);
                character.SetTraitLevel(DefaultTraits.Honor, honor.Value);
                character.SetTraitLevel(DefaultTraits.Generosity, generosity.Value);
                character.SetTraitLevel(DefaultTraits.Calculating, calculating.Value);

                readTraitsSuccess = false;
            }

            string msg = $"Imported {character.Name}'s ";
            if (readBirthdaySuccess && readBirthdaySuccess) msg += "birthday and traits.";
            else if (readBirthdaySuccess) msg += "birthday.";
            else if (readTraitsSuccess) msg += "traits.";
            else msg = null;
            if (msg != null) Helper.ShowAndLog(msg);
        }

        public void ImportWholeFamily(Hero character)
        {
            foreach (var familyMember in GetFamily(character))
            {
                Import(familyMember);
            }
        }
    }
}