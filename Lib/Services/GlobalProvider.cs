using System;
using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace Commander.Lib.Services
{
    public class GlobalProvider
    {
        public string PluginName;
        public string PluginPath;
        public string Version;
        public bool Relogging = false;
        public NetServiceHost Host;
        public CoreManager Core;

        public GlobalProvider(NetServiceHost host, CoreManager core)
        {
            string pluginName = "Commander";
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string pluginPath = $@"{documentsPath}\Decal Plugins\{pluginName}";

            PluginName = pluginName;
            PluginPath = pluginPath;
            Version = "1.0.2";
            Host = host;
            Core = core;
        }
    }
}
