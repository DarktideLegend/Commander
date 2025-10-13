using Commander.Lib.Common;
using Commander.Models;
using Decal.Adapter;
using System;

namespace Commander.Lib.Managers
{
    public interface LoginSessionManager : IDisposable
    {
        void Init();
        LoginSession Session { get; }
        void Clear();
    }

    public class LoginSessionManagerImpl : LoginSessionManager
    {
        private readonly LoginSession.Factory _loginSessionFactory;
        private Logger _logger;
        private GlobalProvider _globals;
        private bool _disposed = false;

        public LoginSession Session { get; private set; }

        public LoginSessionManagerImpl(
            Logger logger,
            GlobalProvider globals,
            LoginSession.Factory loginSessionFactory)
        {
            _logger = logger.Scope("LoginSessionManager");
            _globals = globals;
            _loginSessionFactory = loginSessionFactory;
        }

        public void Clear()
        {
            Session = null;
        }

        public void Init()
        {
            _globals.Core.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete; 
            _logger.Info("LoginSessionManager initialized.");
        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            if (_globals.IsLoggingOnDeath) // <race condition> if in the process of death, don't create a session
            {
                _logger.Info("Logging on Death before login complete, session not created");
                Clear();
                return;
            }

            int monarchId;
            string monarchName;

            int characterId = CoreManager.Current.CharacterFilter.Id;
            string characterName = CoreManager.Current.CharacterFilter.Name;

            try
            {
                monarchId = CoreManager.Current.CharacterFilter.Monarch.Id;
                monarchName = CoreManager.Current.CharacterFilter.Monarch.Name;
            } catch (Exception ex)
            {
                monarchId = characterId;
                monarchName = characterName;
            }

            Session = _loginSessionFactory(
                CoreManager.Current.CharacterFilter.Id,
                CoreManager.Current.CharacterFilter.Vitae,
                CoreManager.Current.CharacterFilter.Health,
                CoreManager.Current.CharacterFilter.LoginStatus,
                CoreManager.Current.CharacterFilter.ServerPopulation,
                monarchId,
                monarchName,
                CoreManager.Current.CharacterFilter.Server,
                CoreManager.Current.CharacterFilter.Name,
                CoreManager.Current.CharacterFilter.AccountName);

            string server = Session.Server;
            string account = Session.AccountName;
            string name = Session.Name;

            _logger.Info($@"LoginStatus: {Session.LoginStatus.ToString()}");
            _logger.Info($@"Server: {server.ToString()}");
            _logger.Info($@"AccountName: {account.ToString()}");
            _logger.Info($@"Id: {Session.Id.ToString()}");
            _logger.Info($@"Name: {name.ToString()}");
            _logger.Info($@"Monarch: {Session.Monarch.ToString()}");
            _logger.Info($@"MonarchName: {Session.MonarchName.ToString()}");
            _logger.Info($@"Vitae: {Session.Vitae.ToString()}");
            _logger.Info($@"Health: {Session.Health.ToString()}");
            _logger.Info($@"ServerPopulation: {Session.ServerPopulation.ToString()}");
            _logger.WriteToWindow($"{server}-{account}-{name}");
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
                }
                _disposed = true;
            }
        }
    }
}
