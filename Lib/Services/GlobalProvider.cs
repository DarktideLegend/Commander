using System;
using System.Reflection;
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
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

            PluginName = pluginName;
            PluginPath = pluginPath;
            Version = assemblyVersion.ToString();
            Host = host;
            Core = core;
        }
    }
}

