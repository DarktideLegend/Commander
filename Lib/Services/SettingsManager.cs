using System.IO;
using Newtonsoft.Json;
using Commander.Models;
using System;

namespace Commander.Lib.Services
{
    public interface SettingsManager
    {
        Settings ReadUserSettings();
        GlobalSettings ReadGlobalSettings();
        Settings WriteUserSettings();
        GlobalSettings WriteGlobalSettings();
        void SaveUISettings(int x, int y, int width, int height);
        Settings Settings { get; set; }
        GlobalSettings GlobalSettings { get; set; }

        void Init(string server, string account, string name);
    }

    public class SettingsManagerImpl : SettingsManager
    {
        public Settings Settings { get; set; }
        public GlobalSettings GlobalSettings { get; set; }
        private Logger _logger;
        private Settings.Factory _settingsFactory;
        private GlobalSettings.Factory _globalSettingsFactory;
        private string _pluginPath;
        private string _serverPath;
        private string _serverFilePath;
        private string _characterPath;
        private string _filePath;
        private string _pluginName;
        private string _server;
        private string _name;
        private string _account;

        public SettingsManagerImpl(
            Logger logger, 
            GlobalProvider globals)

        {
            _logger = logger.Scope("SettingsManager");
            _settingsFactory = Settings.CreateDefaultSettings;
            _globalSettingsFactory = GlobalSettings.CreateDefaultSettings;
            _pluginPath = globals.PluginPath;
            _pluginName = globals.PluginName;
        }

        public void Init(string server, string account, string name)
        {
            _server = server;
            _name = name;
            _account = account;
            _serverPath = $@"{_pluginPath}\{_server}";
            _characterPath = $@"{_pluginPath}\{_server}\{_account}\{_name}";
            _serverFilePath = $@"{_serverPath}\globals.json";
            _filePath = $@"{_characterPath}\{_pluginName}.json";

            Read();
        }

        private void Read()
        {
            ReadGlobalSettings();
            ReadUserSettings();
        }

        public Settings WriteUserSettings()
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                Directory.CreateDirectory(_characterPath);
                using (StreamWriter sw = new StreamWriter(_filePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, Settings);
                }

                _logger.Info($@"Writing to: {_filePath}");

            } catch(IOException ex) { _logger.Error(ex); }

            return Settings;
        }

        public GlobalSettings WriteGlobalSettings()
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                Directory.CreateDirectory(_serverPath);
                using (StreamWriter sw = new StreamWriter(_serverFilePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, GlobalSettings);
                }

                _logger.Info($@"Writing to: {_serverFilePath}");

            } catch(IOException ex) { _logger.Error(ex); }

            return GlobalSettings;
        }

        public Settings ReadUserSettings()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string jsonString = File.ReadAllText(_filePath);
                    Settings = JsonConvert.DeserializeObject<Settings>(jsonString);
                    _logger.Info("Settings successfully read");
                }
                else
                {
                    Settings = _settingsFactory();
                    WriteUserSettings();
                }

            } catch (IOException ex)
            {
                _logger.Error(ex);
                Settings = _settingsFactory();
            }

            return Settings;
        }

        public GlobalSettings ReadGlobalSettings()
        {
            try
            {
                if (File.Exists(_serverFilePath))
                {
                    string jsonString = File.ReadAllText(_serverFilePath);
                    GlobalSettings = JsonConvert.DeserializeObject<GlobalSettings>(jsonString);
                    _logger.Info("Global Settings successfully read");
                }
                else
                {
                    GlobalSettings = _globalSettingsFactory();
                    WriteGlobalSettings();
                }

            } catch (IOException ex)
            {
                _logger.Error(ex);
                GlobalSettings = _globalSettingsFactory();
            }

            return GlobalSettings;
        }

        public void SaveUISettings(int x, int y, int width, int height)
        {
                Settings.UIX = x;
                Settings.UIY = y;
                Settings.UIWidth = width;
                Settings.UIHeight = height;
                WriteUserSettings();
        }

    }
}
