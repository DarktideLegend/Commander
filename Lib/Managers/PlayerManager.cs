using Commander.Lib.Common;
using Commander.Lib.Models;
using Commander.Models;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Timers;

namespace Commander.Lib.Services
{
    public interface PlayerManager : IDisposable
    {
        void Remove(int id, Player player);
        void Update(int id, Player player);
        List<int> GetCache();
        Player Get(int id);
        Player GetByName(string name);
        Dictionary<int, Player> PlayersInstance();
        void Init();

        event EventHandler<Player> PlayerAdded;
        event EventHandler<Player> PlayerRemoved;
        event EventHandler<Player> PlayerUpdated;
    }

    public class PlayerManagerImpl : PlayerManager
    {
        private Logger _logger;
        private LoginSessionManager _loginSessionManager;
        private SettingsManager _settingsManager;
        private GameClient _gameClient;
        private GlobalProvider _globals;
        private Player.Factory _playerFactory;
        private List<int> _preSessionPlayerCache = new List<int>();
        private Dictionary<int, Player> _players = new Dictionary<int, Player>();
        public Dictionary<int, Player> Enemies = new Dictionary<int, Player>();
        public Dictionary<int, Player> Friends = new Dictionary<int, Player>();
        private Timer _ghostObjectTimer;
        private bool isPlayingSound = false;
        private bool _disposed;

        public event EventHandler<Player> PlayerAdded;
        public event EventHandler<Player> PlayerRemoved;
        public event EventHandler<Player> PlayerUpdated;

        public PlayerManagerImpl(
            Logger logger,
            GameClient gameClient,
            SettingsManager settingsManager,
            GlobalProvider globals,
            Player.Factory playerFactory,
            LoginSessionManager loginSessionManager)
        {
            _logger = logger.Scope("PlayerManager");
            _gameClient = gameClient;
            _loginSessionManager = loginSessionManager;
            _settingsManager = settingsManager;
            _globals = globals;
            _playerFactory = playerFactory;
            _ghostObjectTimerInit();
        }

        public void Init()
        {
            _logger.Info("PlayerManager Initialized");
            _globals.Core.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
            _globals.Core.WorldFilter.CreateObject += WorldFilter_CreateObject;
            _globals.Core.WorldFilter.MoveObject += WorldFilter_MoveObject;
            _globals.Core.WorldFilter.ReleaseObject += WorldFilter_ReleaseObject;
            _globals.Core.PluginTermComplete += Core_PluginTermComplete;
        }

        private void Core_PluginTermComplete(object sender, EventArgs e)
        {
            _logger.Info($"Core_PluginTermComplete()");
            _clear();
        }

        private void WorldFilter_ReleaseObject(object sender, ReleaseObjectEventArgs e)
        {
            try
            {
                int id = e.Released.Id;
                string name = e.Released.Name;
                Player player = Get(id);

                if (player != null) 
                {
                    _logger.Info($"Enemy Released: {name}");
                    Remove(id, player);
                }
            } catch (Exception ex) { _logger.Error(ex); }
        }

        private void WorldFilter_MoveObject(object sender, MoveObjectEventArgs e)
        {
            try
            {
                _processWorldObject(e.Moved);
            } catch (Exception ex) { _logger.Error(ex); }
        }

        private void WorldFilter_CreateObject(object sender, CreateObjectEventArgs e)
        {
            try
            {
                _processWorldObject(e.New);
            } catch (Exception ex) { _logger.Error(ex); }
        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            LoginSession session = _loginSessionManager.Session;
            foreach (int id in GetCache())
            {
                if (!_gameClient.IsValidObject(id))
                    return;

                WorldObject wo = _gameClient.GetWorldObject(id);
                int woMonarch = wo.Values(LongValueKey.Monarch);

                bool enemy = _isEnemy(id);
                bool self = session.Id == id;
                if (!self)
                {
                    _addPlayer(_playerFactory(wo, enemy));
                }
            }

            _preSessionPlayerCache.Clear();
        }

        private void _processWorldObject(WorldObject obj)
        {
            LoginSession session = _loginSessionManager.Session;
            Settings settings = _settingsManager.Settings;

            if (session == null || settings == null)
            {
                _processPreSession(obj);
            }
            else
            {
                _processPostSession(obj);
            }
        }
        private void _processPostSession(WorldObject wo)
        {
            if (_gameClient.GetSelf().Id != _loginSessionManager.Session.Id)
            {
                _loginSessionManager.Clear();
                _processPreSession(wo);
                return;
            }

            if (_gameClient.IsPlayer(wo.Id))
            {
                _processPlayerObject(wo);
            }
        }
        private void _processPlayerObject(WorldObject wo)
        {
            int currentId = _gameClient.GetSelf().Id;
            var enemy = _isEnemy(wo.Id);
            bool self = wo.Id == currentId;

            if (self)
                return;

            if (Get(wo.Id) != null)
            {
                //WorldObjectService.RequestId(woId);
                return;
            }

            double distance = _gameClient.GetDistanceFromPlayer(wo.Id, currentId);
            int playerDistance = Convert.ToInt32(distance);

            if (playerDistance > _settingsManager.Settings.RelogDistance)
                return;

            if (enemy)
            {
                _addPlayer(_playerFactory(wo, true));
                return;
            }

            _addPlayer(_playerFactory(wo, false));
        }

        private void _processPreSession(WorldObject obj)
        {
            if (_gameClient.IsPlayer(obj.Id))
            {
                _cachePlayer(obj.Id);
                return;
            }
        }

        private void _ghostObjectTimerInit()
        {
            _ghostObjectTimer = new Timer();
            _ghostObjectTimer.Interval = 1000 * 60 * 1;
            _ghostObjectTimer.AutoReset = true;
            _ghostObjectTimer.Elapsed += _ghostObjectTimer_Elapsed;
        }

        private void _cachePlayer(int id)
        {
            if (!_preSessionPlayerCache.Contains(id))
                _preSessionPlayerCache.Add(id);
        }

        public List<int> GetCache()
        {
            return _preSessionPlayerCache;
        }

        private bool _isEnemy(int otherId)
        {
            int currentId = _gameClient.GetSelf().Id;
            WorldObject wo = _gameClient.GetWorldObject(otherId);
            Settings settings = _settingsManager.Settings;
            LoginSession session = _loginSessionManager.Session;

            int woMonarch = wo.Values(LongValueKey.Monarch);
            string woName = wo.Name;
            int woId = wo.Id;
            int id = session.Id;
            int monarch = session.Monarch;

            var friendsList = _settingsManager.GlobalSettings.Friends
                .Select(f => f.ToLower())
                .ToList();

            bool enemy = true;

            if (woMonarch == monarch)
                enemy = false;

            if (friendsList.Contains(wo.Name.ToLower()))
                enemy = false;

            return enemy;
        }

        private void _processGhostObjects()
        {
            int currentId = _gameClient.GetSelf().Id;

            if (_players.Count == 0)
            {
                _ghostObjectTimer.Stop();
            }

            foreach (KeyValuePair<int, Player> player in _players)
            {
                int playerId = player.Value.Id;
                if (
                    !_gameClient.IsValidObject(playerId) ||
                    _gameClient.GetDistanceFromPlayer(currentId, playerId) > 1000)
                {
                    _logger.Info($"Player: {player.Value.Name} is not a valid object");
                    Remove(playerId, Get(playerId));
                }
            }
        }

        private void _ghostObjectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _logger.Info("Checking for GhostPlayerObjects");

            if (_players.Count > 0 || _loginSessionManager.Session != null)
                _processGhostObjects();
        }

        public Dictionary<int, Player> PlayersInstance()
        {
            return _players;
        }

        protected virtual void OnPlayerUpdated(Player player)
        {
            PlayerUpdated?.Invoke(this, player);
        }

        protected virtual void OnPlayerRemoved(Player player)
        {
            PlayerRemoved?.Invoke(this, player);
        }

        protected virtual void OnPlayerAdded(Player player)
        {
            PlayerAdded?.Invoke(this, player);
            _ghostObjectTimer.Start();
        }

        public void Update(int id, Player player)
        {
            Player currentPlayer = Get(id);

            if (currentPlayer == null)
            {
                _addPlayer(player);
                return;
            }

            _players[id] = player;
            OnPlayerUpdated(player);
        }

        public void Remove(int id, Player player)
        {
            _players.Remove(id);
            if (player.Enemy && Enemies.TryGetValue(player.Id, out var _))
                Enemies.Remove(player.Id);
            else if (!player.Enemy && Friends.TryGetValue(player.Id, out var _))
                Friends.Remove(player.Id);

            OnPlayerRemoved(player);
            _logger.WriteToChat($"Player Removed: {player.Name}");
        }

        private void _addPlayer(Player player)
        {
            LoginSession session = _loginSessionManager.Session;
            Settings settings = _settingsManager.Settings;
            var relog = settings.Relog;
            var isRelogging = _globals.Relogging;
            string soundPath;

            if (Get(player.Id) != null)
                return;

            _players.Add(player.Id, player);
            if (player.Enemy && Enemies.TryGetValue(player.Id, out var _))
                Enemies.Add(player.Id, player);
            else if (!player.Enemy && Friends.TryGetValue(player.Id, out var _))
                Friends.Add(player.Id, player);

            OnPlayerAdded(player);

            if (session == null)
                return;

            _gameClient.RequestId(player.Id);
            if (player.Enemy)
            {
                _logger.WriteToChat($"Enemy Added: {player.Name}");
                soundPath = "Commander.Assets.Audio.enemy.wav";
                if (settings.EnemySounds)
                {
                    PlaySoundFromResource(soundPath);
                }
            }
            else
            {
                _logger.WriteToChat($"Friendly Added: {player.Name}");
                soundPath = "Commander.Assets.Audio.friendly.wav";
                if (settings.FriendlySounds)
                {
                    PlaySoundFromResource(soundPath);
                }
            }

        }

        public Player Get(int id)
        {
            if (_players.TryGetValue(id, out Player player))
            {
                return player;
            }
            else
            {
                return null;
            }
        }

        public Player GetByName(string name)
        {
            foreach (KeyValuePair<int, Player> entry in _players)
            {
                if (entry.Value.Name == name)
                {
                    return entry.Value;
                }
            }

            return null;
        }

        private Stream GetResourceStream(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
            return resourceStream;
        }

        private void PlaySoundFromResource(string resourceName)
        {
            if (isPlayingSound)
                return;

            isPlayingSound = true;

            Stream resourceStream = GetResourceStream(resourceName);
            if (resourceStream != null)
            {
                using (Stream input = resourceStream)
                {
                    new SoundPlayer(input).Play();
                }
            }

            isPlayingSound = false;
        }

        public void _clear()
        {
            _logger.Info("Clearing PlayerManager");
            _players.Clear();
            Enemies.Clear();
            Friends.Clear();
            _preSessionPlayerCache.Clear();
            _ghostObjectTimer.Stop();
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
                    _globals.Core.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
                    _globals.Core.WorldFilter.CreateObject -= WorldFilter_CreateObject;
                    _globals.Core.WorldFilter.MoveObject -= WorldFilter_MoveObject;
                    _globals.Core.WorldFilter.ReleaseObject -= WorldFilter_ReleaseObject;
                    _globals.Core.PluginTermComplete -= Core_PluginTermComplete;
                    _clear();
                }
                _disposed = true;
            }
        }
    }
}

