using Commander.Lib.Common;
using Commander.Lib.Models;
using Commander.Models;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace Commander.Lib.Services
{
    public interface RelogManager : IDisposable
    {
        void Init();
        void Enable(string loggedBy);
        void Disable();
        void StartTimer();
        void Stop();
    }

    public class RelogManagerImpl : RelogManager
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hhwnd, uint msg, IntPtr wparam, UIntPtr lparam);

        private Logger _logger;
        private GameClient _gameClient;
        private SettingsManager _settingsManager;
        private LoginSessionManager _loginSessionManager;
        private PlayerManager _playerManager;
        private GlobalProvider _globals;
        private Timer _relogTimer;
        private TimeSpan _remaining;
        private DateTime _startTime;
        private string _loggedBy;
        private bool _active = false;
        private bool disposedValue = false;
        private const uint WM_MOUSEMOVE = 0x0200;
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const byte VK_PAUSE = 0x13;

        public RelogManagerImpl(
            Logger logger,
            GameClient gameClient,
            GlobalProvider globals,
            LoginSessionManager loginSessionManager,
            PlayerManager playerManager,
            SettingsManager settingsMangager)
        {
            _logger = logger.Scope("RelogManager");
            _gameClient = gameClient;
            _settingsManager = settingsMangager;
            _playerManager = playerManager;
            _globals = globals;
            _loginSessionManager = loginSessionManager;
        }
        public void Init()
        {
            _globals.Core.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
            _globals.Core.CharacterFilter.Login += CharacterFilter_Login;   
            _globals.Core.CharacterFilter.Death += CharacterFilter_Death;
            _globals.Core.PluginTermComplete += Core_PluginTermComplete;
            _playerManager.PlayerAdded += PlayerManager_PlayerAdded;
            _logger.Info("RelogManager initialized");
        }

        private void Core_PluginTermComplete(object sender, EventArgs e)
        {
            _logger.Info("Core_PluginTermComplete()");
            if (_globals.Relogging)
            {
                StartTimer();
            }
        }

        private void PlayerManager_PlayerAdded(object sender, Player player)
        {
            _checkEnemy(player);
        }

        private void _checkEnemy(Player player)
        {
            if (player.Enemy)
            {
                var relog = _settingsManager.Settings.Relog;
                var isRelogging = _globals.Relogging;

                if (relog && !isRelogging)
                {
                    Enable(player.Name);
                }
            }
        }

        private void CharacterFilter_Login(object sender, LoginEventArgs e)
        {
            try
            {
                _logger.Info($"CharacterFilter_Login()");
                if (_globals.Relogging)
                {
                    _logger.Info("Premature login during relogging, disabling RelogManager...");
                    Disable();
                }
            } catch (Exception ex) { _logger.Error(ex); }
        }

        private void CharacterFilter_Death(object sender, DeathEventArgs e)
        {
            _logger.Info($"CharacterFilter_Death()");
            try { 
                Disable();
            } catch(Exception ex) { _logger.Error(ex); }

        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            Settings settings = _settingsManager.Settings;

            if (settings.Relog)
            {
                _sendPause();
            }
        }

        public void Enable(string loggedBy)
        {
            _logger.Info($"Enable() - {loggedBy}");
            _loggedBy = loggedBy;
            _relogTimer = new Timer();
            _relogTimer.Interval = 1000;
            _relogTimer.Elapsed += new ElapsedEventHandler(_relogTimerChecker);
            _relogTimer.AutoReset = true;
            _relogTimer.Stop();

            _logger.WriteToChat($"Logged by: {loggedBy}");
            _globals.Relogging = true;
            _gameClient.Logout();
        }
        
        public void Stop()
        {
            LoginSession session = _loginSessionManager.Session;
            _globals.Relogging = false;
            _relogTimer?.Stop();
            _logger.Info("Stop()");
            _logger.WriteToWindow($"{session.Server}-{session.AccountName}-{session.Name}");
        }

        public void Disable()
        {
            _logger.Info("Disable");
            Stop();
            _settingsManager.Settings.Relog = false;
            _settingsManager.WriteUserSettings();
        }

        public void StartTimer()
        {
            _logger.Info("StartTimer()");
            _startTime = DateTime.Now;
            _relogTimer.Start();
            _logger.Info("Start()");
        }

        private void _relogTimerChecker(object sender, ElapsedEventArgs e)
        {
            try
            {
                int relogDuration = _settingsManager.Settings.RelogDuration;

                _remaining = (TimeSpan.FromMinutes(Convert.ToDouble(relogDuration)) - (DateTime.Now - _startTime));
                string countdown = string.Format("{0:00}:{1:00}", (int)_remaining.Minutes, _remaining.Seconds);

                if (_remaining.Minutes <= 0 && _remaining.Seconds <= 0)
                {
                    Stop();
                    _sendMouseClick(300, 407);
                    return;
                }

                _logger.WriteToWindow("Logged by: " + _loggedBy + " relogging in " + countdown);
            } catch(Exception ex) { _logger.Error(ex); }
        }

        private void _sendMouseClick(int x, int y)
        {
            int loc = (y * 0x10000) + x;

            PostMessage(CoreManager.Current.Decal.Hwnd, WM_MOUSEMOVE, (IntPtr)0x00000000, (UIntPtr)loc);
            PostMessage(CoreManager.Current.Decal.Hwnd, WM_LBUTTONDOWN, (IntPtr)0x00000001, (UIntPtr)loc);
            PostMessage(CoreManager.Current.Decal.Hwnd, WM_LBUTTONUP, (IntPtr)0x00000000, (UIntPtr)loc);
        }
        private void _sendPause()
        {
            Console.WriteLine("Enabling Vtank");
            PostMessage(CoreManager.Current.Decal.Hwnd, WM_KEYDOWN, (IntPtr)VK_PAUSE, (UIntPtr)0x00450001);
            PostMessage(CoreManager.Current.Decal.Hwnd, WM_KEYUP, (IntPtr)VK_PAUSE, (UIntPtr)0xC0450001);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                    _relogTimer?.Dispose();
                    _globals.Core.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
                    _globals.Core.CharacterFilter.Login -= CharacterFilter_Login;
                    _globals.Core.CharacterFilter.Death -= CharacterFilter_Death;
                    _globals.Core.PluginTermComplete -= Core_PluginTermComplete;
                    _playerManager.PlayerAdded -= PlayerManager_PlayerAdded;
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
