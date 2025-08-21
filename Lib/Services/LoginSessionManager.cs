using Commander.Models;
using Decal.Adapter;
using System;

namespace Commander.Lib.Services
{
    public interface LoginSessionManager
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
            _logger.Info("Init()");
            _globals.Core.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete; 
        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
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

            _logger.Info($@"LoginStatus: {Session.LoginStatus.ToString()}");
            _logger.Info($@"Server: {Session.Server.ToString()}");
            _logger.Info($@"AccountName: {Session.AccountName.ToString()}");
            _logger.Info($@"Id: {Session.Id.ToString()}");
            _logger.Info($@"Name: {Session.Name.ToString()}");
            _logger.Info($@"Monarch: {Session.Monarch.ToString()}");
            _logger.Info($@"MonarchName: {Session.MonarchName.ToString()}");
            _logger.Info($@"Vitae: {Session.Vitae.ToString()}");
            _logger.Info($@"Health: {Session.Health.ToString()}");
            _logger.Info($@"ServerPopulation: {Session.ServerPopulation.ToString()}");
        }
    }
}
