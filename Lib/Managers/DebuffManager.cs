using Commander.Lib.Common;
using Commander.Lib.Models;
using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Timers;

namespace Commander.Lib.Services
{
    public interface DebuffManager: IDisposable
    {
        void Start();
        void Add(Player player);

        void Init();
    }

    public class DebuffManagerImpl : DebuffManager
    {
        private Logger _logger;
        private PlayerManager _playerManager;
        private List<Player> _debuffedPlayers;
        private Timer _debuffTimer;
        private TimeSpan _remaining;
        private GlobalProvider _globals;
        private GameClient _gameClient;
        private DebuffInformation.Factory _debuffInformationFactory;
        private bool disposedValue = false;

        public DebuffManagerImpl(
            PlayerManager playerManager,
            GlobalProvider globals,
            GameClient gameClient,
            DebuffInformation.Factory debuffInformationFactory,
            Logger logger)
        {
            _logger = logger.Scope("DebuffManager");
            _globals = globals;
            _gameClient = gameClient;
            _playerManager = playerManager;
            _debuffInformationFactory = debuffInformationFactory;
            _debuffedPlayers = new List<Player>();
            _debuffTimer = new Timer();
            _debuffTimer.Interval = 15000;
            _debuffTimer.AutoReset = true;
            _debuffTimer.Elapsed += _debuffTimer_Elapsed;
        }
        public void Init()
        {
            _globals.Core.PluginTermComplete += Core_PluginTermComplete;
            _playerManager.PlayerUpdated += _playerManager_PlayerUpdated;
            _globals.Core.EchoFilter.ServerDispatch += EchoFilter_ServerDispatch;
            _logger.Equals("DebuffManager initialized.");
        }

        private void EchoFilter_ServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            if (e.Message.Type == 0xF755) 
            {
                _processApplyVisual(e);
            }
        }
        private void _processApplyVisual(NetworkMessageEventArgs e)
        {
            int id = e.Message.Value<int>("object");
            int effect = e.Message.Value<int>("effect");

            if (
                !_gameClient.IsValidObject(id) ||
                !_gameClient.IsPlayer(id) ||
                !Enum.IsDefined(typeof(Debuff), effect))
            {
                return;
            }

            Player player = _playerManager.Get(id);

            if (player != null)
            {
                _processApplyVisualOnPlayer(player, effect);
            }
        }

        private void _processApplyVisualOnPlayer(Player player, int effect)
        {
            int index = player.Debuffs.FindIndex(obj => obj.Spell == effect);
            if (index != -1)
            {
                player.Debuffs[index].StartTime = DateTime.Now;
            }
            else
            {
                player.Debuffs.Add(_debuffInformationFactory(effect, DateTime.Now));
            }

            _playerManager.Update(player.Id, player);
        }


        private void Core_PluginTermComplete(object sender, EventArgs e)
        {
            _stop();
        }

        private void Update(Player player)
        {
            _logger.Info($"Update(Player {player.Name})");
            int index = _debuffedPlayers.FindIndex(_player => _player.Id == player.Id);
            _debuffedPlayers[index] = player;
        }

        private void _playerManager_PlayerUpdated(object sender, Player player)
        {
            try
            {
                if (player.Debuffs.Count == 0)
                    return;

                if (_debuffedPlayers.Contains(player))
                    Update(player);

                Start();

            } catch (Exception ex) { _logger.Error(ex); }
        }

        public void Start()
        {
            _logger.Info("Start()");
            _debuffTimer.Start();
        }

        public void Add(Player player)
        {
            _logger.Info($"Add(Player {player})");
            _debuffedPlayers.Add(player);
        }

        private void _stop()
        {
            _logger.Info("Stop()");
            _debuffTimer.Stop();
        }

        private void _processTimeLimitReached(Player player, DebuffInformation info)
        {
            player.Debuffs.Remove(info);

            if (player.Debuffs.Count == 0)
            {
                _debuffedPlayers.Remove(player);
            }

            _playerManager.Update(player.Id, player);
        }

        private void _processDebuffedPlayer(Player player)
        {
            _logger.Info($"Player in _debuffedPlayers: {player.Name}");
            foreach (DebuffInformation info in player.Debuffs)
            {
                _remaining = (TimeSpan.FromMinutes(Convert.ToDouble(5)) - (DateTime.Now - info.StartTime));
                _logger.Info(
                    $"DebuffInformation: Spell: [{info.Spell}] -- _remaining: {_remaining.Minutes}:{_remaining.Seconds}");

                if (_remaining.Minutes <= 0 && _remaining.Seconds <= 0)
                {
                    _processTimeLimitReached(player, info);
                }
            }
        }

        private void _debuffTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _logger.Info("=====_deuffTimer_Elapsed(Event)=====");
            _logger.Info($"_debuffedPlayers.count: {_debuffedPlayers.Count.ToString()}");

            if (_debuffedPlayers.Count == 0)
            {
                _stop();
            }

            foreach (Player player in _debuffedPlayers)
            {
                _processDebuffedPlayer(player);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _debuffTimer.Dispose();
                    _globals.Core.PluginTermComplete -= Core_PluginTermComplete;
                    _playerManager.PlayerUpdated -= _playerManager_PlayerUpdated;
                    _globals.Core.EchoFilter.ServerDispatch -= EchoFilter_ServerDispatch;
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
