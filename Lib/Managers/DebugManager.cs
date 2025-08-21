using System;
using System.IO;
using System.Runtime.InteropServices;
using Commander.Lib.Common;
using Commander.Models;

namespace Commander.Lib.Services
{
    public interface DebugManager : IDisposable
    {
        void Init();
        void Start();
        void Toggle();
        bool Active { get; }
    }

    public class DebuggerImpl : DebugManager
    {
        private GlobalProvider _globals;
        private SettingsManager _settingsManager;
        private Logger _logger;
        private bool disposedValue = false;

        [DllImport("kernel32")]
        static extern bool AllocConsole();

        [DllImport("kernel32")]
        static extern bool FreeConsole();

        [DllImport("Kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("User32.dll")]
        static extern int ShowWindow(IntPtr hwnd, int nShow);

        public bool Active { get; private set; } = false;

        public DebuggerImpl(GlobalProvider globals, Logger logger, SettingsManager settingsManager)
        {
            _globals = globals;
            _settingsManager = settingsManager;
            _logger = logger;
        }

        public void Init()
        {
            _globals.Core.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
            _logger.Info("DebugManager initialized.");
        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            Settings settings = _settingsManager.Settings;

            if (settings.Debug)
                _show();
            else
                _hide();
        }

        private void _enable()
        {
            if (!AllocConsole())
            {
                _logger.Error(new Exception("Failed to allocate console"));
                return;
            }
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            Console.BufferHeight = 9000;
            Console.SetWindowPosition(0, 0);
            Console.SetWindowSize(100, 30);
            _hide();
        }

        public void Start()
        {
            if (!Active)
            {
                _enable();
            }
        }

        private void _stop()
        {
            FreeConsole();
        }

        public void Toggle()
        {
            if(Active)
            {
                _hide();
            } else
            {
                _show();
            }
        }

        private void _show()
        {
            if (Active)
            {
                return;
            }

            Active = true;
            ShowWindow(GetConsoleWindow(), 5);
        }

       private void _hide()
        {
            Active = false;
            ShowWindow(GetConsoleWindow(), 0);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _stop();
                    _globals.Core.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
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
