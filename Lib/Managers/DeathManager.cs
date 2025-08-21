using Commander.Lib.Common;
using Commander.Models;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Commander.Lib.Services
{
    public interface DeathManager : IDisposable
    {
        void Init();
        void ProcessDeath(int killerId, int killedId, string deathMessage);
        void ProcessDeath(string deathMessage);
    }

    public class DeathManagerImpl : DeathManager
    {
        private Logger _logger;
        private SettingsManager _settingsManager;
        private GlobalProvider _globals;
        private GameClient _client;
        private RelogManager _relogManager;
        private bool _disposed = false;

        public DeathManagerImpl(
            Logger logger,
            SettingsManager settingsManager,
            VitaeManager vitaeManager,
            GlobalProvider globals,
            GameClient client,
            RelogManager relogManager)
        {
            _logger = logger.Scope("DeathManager");
            _settingsManager = settingsManager;
            _globals = globals;
            _client = client;
            _relogManager = relogManager;
        }

        public void Init()
        {
            _logger.Info("DeathManager initialized");
            _globals.Core.CharacterFilter.Death += CharacterFilter_Death;
            _globals.Core.CharacterFilter.Login += CharacterFilter_Login;
        }

        private void CharacterFilter_Login(object sender, LoginEventArgs e)
        {
            _globals.IsLoggingOnDeath = false;
        }

        private void CharacterFilter_Death(object sender, DeathEventArgs e)
        {
            try
            {
                ProcessDeath(e.Text);
            } catch (Exception ex) { _logger.Error(ex); }
        }

        public void ProcessDeath(int killerId, int killedId, string deathMessage)
        {
            _logger.Info("ProcessPkDeath()");
            int self = _client.GetSelf().Id;

            if (killerId == self)
            {
                _logger.Info($"You killed: {_client.GetWorldObject(killedId).Name}");
                _logger.Info(deathMessage);
            }

            if (killedId == self)
            {
                _processDeath(deathMessage);
            }
        }

        public void ProcessDeath(string deathMessage)
        {
            _logger.Info("ProcessDeathFromOther()");
            _processDeath(deathMessage);
        }

        private void _processDeath(string deathMessage)
        {
            _logger.Info("_processDeath");
            Settings settings = _settingsManager.Settings;

            if (settings.LogOnDeath)
            {
                _processLogOnDeath(deathMessage);
            }
        }

        private void _processLogOnDeath(string deathMessage)
        {
            _logger.Info("_processLogOnDeath()");
            _globals.IsLoggingOnDeath = true;
            _logger.WriteToChat(deathMessage);
            _logger.WriteToWindow(deathMessage);

            _client.Logout();
            return;
        }
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _globals.Core.CharacterFilter.Death -= CharacterFilter_Death;
                    _globals.Core.CharacterFilter.Login -= CharacterFilter_Login;
                }
                _disposed = true;
            }
        }
    }
}

