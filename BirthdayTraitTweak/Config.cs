using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BirthdayTraitTweak
{
    public static class Config
    {
        public const string MOD_NAME = "BirthdayTraitTweak";

        public enum RWMode
        {
            None,
            Single,
            Family,
        };

        static RWMode mode = RWMode.Family;
        static string modeStr = RWMode.Family.ToString();
        public static RWMode Mode => mode;
        public static string ModeStr => modeStr;
        /// <summary>
        /// Set the mode based on the string. If it's one of the RWMode values, returns true.
        /// </summary>
        public static bool SetMode(string value)
        {
            modeStr = value??string.Empty;

            if (value != null)
            {
                bool success = Enum.TryParse(value, out RWMode res);
                mode = success ? res : RWMode.None;
                return success;
            }
            mode = RWMode.None;
            return false;
        }

        public static bool SetMode(RWMode value)
        {
            modeStr = value.ToString();
            mode = value;
            return true;
        }
    }
}
