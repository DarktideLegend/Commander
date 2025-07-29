using System.Collections.Generic;

namespace Commander.Models
{
    public class Settings
    {
        public bool Debug = false;
        public bool LogOnDeath = true;
        public bool LogOnVitae = true;
        public bool Relog = false;
        public bool EnemySounds = true;
        public bool FriendlySounds = true;
        public int RelogDuration = 5;
        public int VitaeLimit = 10;
        public int RelogDistance = 300;
        public bool EnemyIcon = true;
        public bool FriendlyIcon = true;
        public int IconSize = 2;
        public int UIHeight = 200;
        public int UIWidth = 200;
        public int UIX = 50;
        public int UIY = 50;

        public delegate Settings Factory();

        public static Settings CreateDefaultSettings()
        {
            return new Settings(); 
        }
    }

    public class GlobalSettings
    {
        public List<string> Friends = new List<string>();
        public delegate GlobalSettings Factory();
        public static GlobalSettings CreateDefaultSettings()
        {
            return new GlobalSettings(); 
        }
    }
}
