using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Commander.Lib.Common;
using Commander.Models;

namespace Commander.Lib.Services
{
    public interface VitaeManager : IDisposable
    {
        void Init();
    }

    public class VitaeManagerImpl : VitaeManager
    {
        private bool disposedValue = false;
        private Logger _logger;
        private GameClient _gameClient;
        private GlobalProvider _globals;
        private SettingsManager _settingsManager;
        private Timer _vitaeTimer;

        public VitaeManagerImpl(Logger logger, GameClient gameClient, GlobalProvider globals, SettingsManager settingsManager)
        {
            _logger = logger.Scope("VitaeManager");
            _globals = globals;
            _gameClient = gameClient;
            _settingsManager = settingsManager;
        }

        public void Init()
        {
            _globals.Core.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
            _globals.Core.PluginTermComplete += Core_PluginTermComplete;
            _globals.Core.CharacterFilter.Death += CharacterFilter_Death;
            _logger.Info("VitaeManager initialized.");
        }

        private void CharacterFilter_Death(object sender, Decal.Adapter.Wrappers.DeathEventArgs e)
        {
            _logger.Info("CharacterFilter_Death[EVENT]");
            Settings settings = _settingsManager.Settings;

            if (settings.LogOnVitae)
            {
                CheckVitae();
            }   
        }

        private void Core_PluginTermComplete(object sender, EventArgs e)
        {
            Dispose(true);
        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            _logger.Info("CharacterFilter_LoginComplete[EventArgs]");
            _vitaeTimer = new Timer();
            _vitaeTimer.Elapsed += new ElapsedEventHandler(_vitaeTimerChecker);
            _vitaeTimer.Interval = 10000;
            _vitaeTimer.AutoReset = false;
            _vitaeTimer.Start();
        }
        private void _vitaeTimerChecker(object sender, ElapsedEventArgs e)
        {
            _logger.Info("VitaeTimerChecker[EVENT]");
            CheckVitae();
        }

         private void CheckVitae()
        {
            _logger.Info("CheckVitae()");
            try
            {
                Settings settings = _settingsManager.Settings;
                bool logOnVit = settings.LogOnVitae;
                int limit = settings.VitaeLimit;
                int vit = _gameClient.GetVitae();

                if (logOnVit && (vit >= limit))
                {
                    string message = $"Logging off, due to vitae limit of {limit.ToString()} being reached";
                    _logger.WriteToChat(message);
                    _logger.WriteToWindow(message);
                    _gameClient.Logout();
                    _vitaeTimer?.Stop();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _vitaeTimer?.Dispose();
                    _globals.Core.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
                    _globals.Core.PluginTermComplete -= Core_PluginTermComplete;
                }

                disposedValue = true;
            }
        }


        public void Dispose()
        {
            Dispose(disposing: true);
        }
    }

}
