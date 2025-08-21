using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace Commander.Lib.Common
{
    public interface GameClient
    {
        double GetDistanceFromPlayer(int src, int dest);
        bool IsValidObject(int id);
        bool IsSpellKnown(int id);
        bool IsPlayer(int id);
        WorldObject GetWorldObject(int id);
        void Logout();
        int GetVitae();
        CharacterFilter GetSelf();
        void RequestId(int id);
        void SelectItem(int id);
        int BusyState { get; }
        void CastSpell(int spell, int playerId);
        void CastHeal(int playerId);
    }

    public class ACClientImpl : GameClient
    {
        private Logger _logger;
        private GlobalProvider _globals;

        public ACClientImpl(GlobalProvider globals, Logger logger)
        {
            _globals = globals;
            _logger = logger.Scope("GameClient");
        }

        public double GetDistanceFromPlayer(int src, int dest)
        {
            return _globals.Core.WorldFilter.Distance(src, dest) * 240;
        }

        public bool IsValidObject(int id)
        {
            return _globals.Core.Actions.IsValidObject(id);
        }

        public bool IsSpellKnown(int id)
        {
            return _globals.Core.CharacterFilter.IsSpellKnown(id);
        }

        public bool IsPlayer(int id)
        {
            return _globals.Core.WorldFilter[id].ObjectClass == ObjectClass.Player;
        }

        public WorldObject GetWorldObject(int id)
        {
            return _globals.Core.WorldFilter[id];
        }

        public void Logout()
        {
            _globals.Core.Actions.Logout();
        }

        public int GetVitae()
        {
            return _globals.Core.CharacterFilter.Vitae;
        }

        public CharacterFilter GetSelf()
        {
            return _globals.Core.CharacterFilter;
        }

        public void RequestId(int id)
        {
            _globals.Core.Actions.RequestId(id);
        }

        public void SelectItem(int id)
        {
            _globals.Core.Actions.SelectItem(id);
        }
        public int BusyState
        {
            get
            {
                return _globals.Core.Actions.BusyState;
            }
        }

        public void CastSpell(int spell, int playerId)
        {
            _globals.Core.Actions.CastSpell(spell, playerId);
        }

        public void CastHeal(int playerId)
        {
            if (IsSpellKnown(4310))
            {
                CastSpell(4310, playerId);
            }
            else
            {
                CastSpell(2072, playerId);
            }
        }
    }
}

