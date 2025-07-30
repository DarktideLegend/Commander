using Commander.Lib.Models;
using Commander.Lib.Services;
using Commander.Models;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;

namespace Commander.Lib.Controllers
{
    public interface ServerDispatchController
    {
        void Init(object sender, NetworkMessageEventArgs e);
    }

    public class ServerDispatchControllerImpl : ServerDispatchController
    {
        private PlayerManager _playerManager;
        private Logger _logger;
        private SettingsManager _settingsManager;
        private DeathManager _deathManager;
        private DebuffInformation.Factory _debuffInformationFactory;
        private List<int> _messages = new List<int>();

        public ServerDispatchControllerImpl(
            PlayerManager playerManager,
            SettingsManager settingsManager,
            DeathManager deathManager,
            DebuffInformation.Factory debuffInformationFactory,
            Logger logger)
        {
            _logger = logger.Scope("ServerDispatchController");
            _playerManager = playerManager;
            _settingsManager = settingsManager;
            _deathManager = deathManager;
            _debuffInformationFactory = debuffInformationFactory;

        }

        public void Init(object sender, NetworkMessageEventArgs e)
        {
            try
            {
                int messageType = e.Message.Type;

                if (messageType == 63408 && e.Message.Value<int>("event") == 201)
                {
                    //_logger.Info($"MessageType: {messageType}");
                }

                if (e.Message.Type == 0xF755) // ApplyVisual, used to detect debuffs
                {
                    _processApplyVisual(e);
                }

                if (e.Message.Type == 0xF7B0) // Game Event
                {
                    _processGameEvent(e);
                }

                if (e.Message.Type == 414) // PlayerKilled
                {
                    _processPlayerKilled(e);
                }

            }
            catch (Exception ex) { _logger.Error(ex); }
        }

        private void _processPlayerKilled(NetworkMessageEventArgs e)
        {
            _logger.Info("_processPlayerKilled()");
            int killed = e.Message.Value<int>("killed");
            int killer = e.Message.Value<int>("killer");
            string deathMessage = e.Message.Value<string>("text");

            if (WorldObjectService.IsValidObject(killer) && WorldObjectService.IsPlayer(killer))
                _deathManager.ProcessPkDeath(killer, killed, deathMessage);
        }

        private void _processApplyVisual(NetworkMessageEventArgs e)
        {
            int id = e.Message.Value<int>("object");
            int effect = e.Message.Value<int>("effect");

            if (
                !WorldObjectService.IsValidObject(id) ||
                !WorldObjectService.IsPlayer(id) ||
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

        private void _processGameEvent(NetworkMessageEventArgs e)
        {
            int gameEvent = e.Message.Value<int>("event");

            if (gameEvent == 34)
            {
                _processPutItemInContainer(e);
            }

            if (gameEvent == 201) // IdentifyObject
                return;
            //_processIndentifyObject(e);

        }

        private HashSet<int> tier4Rares = new HashSet<int>() { 30352, 30353, 30354, 30355, 30356, 30357, 30358, 30359, 30360, 30361, 30362, 30363, 30364, 30365, 30366, 30367, 30368, 30369, 30370, 30371, 30372, 30373, 30510, 30511, 30512, 30513, 30514, 30515, 30516, 30517, 30518, 30519, 30520, 30521, 30522, 30523, 30524, 30525, 30526, 30527, 30528, 30529, 30530, 30531, 30532, 30533, 30534 };
        private HashSet<int> tier5Rares = new HashSet<int>() { 30074, 30075, 30076, 30077, 30078, 30079, 30080, 30081, 30082, 30083, 30084, 30085, 30086, 30087, 30088, 30089, 30090, 30091, 30092, 30093, 30094, 30095, 30096, 30097, 30098, 30099, 30100, 30101, 30102, 30103, 30104, 30105, 30106, 30110, 30111, 30112, 30113, 30114, 30115, 30116, 30117, 30118, 30119, 30120, 30121, 30122, 30123, 30124, 30125, 30126, 30127, 30128, 30130, 30131, 30132, 30133, 30134, 30135, 30136, 30137, 30138, 30139, 30140, 30141, 30142, 30143, 30144, 30145, 30146, 30147, 30148, 30149, 30150, 30151, 30152, 30153, 30154, 30155, 30157, 30158, 30159, 30160, 30161, 30162, 30163, 30164, 30165, 30166, 30167, 30168, 30169, 30171, 30173, 30174, 30175, 30176, 30179, 30180, 30247, 30248, 30249, 30253, 30254, 30936, 45361, 45362, 45363, 45364, 45365, 70001, 70002, 70003 };
        private HashSet<int> tier6Rares = new HashSet<int>() { 30302, 30303, 30304, 30305, 30306, 30307, 30308, 30309, 30345, 30346, 30347, 30348, 30349, 30350, 30351, 30374, 30375, 30376, 30377, 30378, 42662, 42663, 42664, 42665, 42666, 43848, 45436, 45437, 45438, 45439, 45440, 45441, 45442, 45443, 45444, 45445, 45446, 45447, 45448, 45449, 45450, 45451, 45452, 45453, 45454, 45455, 45456, 45457, 45458, 45459, 45460, 45461, 45462, 45463, 45464, 45465, 45466, 45467, 45468, 45469, 45470 };

        private void _processPutItemInContainer(NetworkMessageEventArgs e)
        {
            int id = e.Message.Value<int>("item");
            var obj = WorldObjectService.GetWorldObject(id);

            if (obj != null)
            {
                var wcid = obj.Values(LongValueKey.Type);
                var rareId = obj.Values(LongValueKey.RareId);

                if (tier4Rares.Contains(wcid) || tier5Rares.Contains(wcid) || tier6Rares.Contains(wcid))
                {
                    var message = $"You have found the rare item {obj.Name}!";
                    _logger.Info("_processPutItemInContainer Rare Item Found");
                    _logger.WriteToChat(message);
                    _logger.WriteToWindow(message);
                    WorldObjectService.Logout();
                    return;
                }
            }
        }

        private void _processIndentifyObject(NetworkMessageEventArgs e)
        {
            int id = e.Message.Value<int>("object");

            if (!WorldObjectService.IsValidObject(id))
                return;

            if ((e.Message.Value<int>("flags") & 256) <= 0)
                return;

            Player player = _playerManager.Get(id);

            if (player != null)
            {
                _processPlayerIdentified(e, player);
            }
        }

        private void _processPlayerIdentified(NetworkMessageEventArgs e, Player player)
        {
            int health = e.Message.Value<int>("health");
            int healthMax = e.Message.Value<int>("healthMax");

            if (health <= 0 || healthMax <= 0)
                return;

            decimal pct = ((decimal)health / (decimal)healthMax) * (decimal)100;

            if (pct < 50)
                player.LowHealth = true;
            else
                player.LowHealth = false;

            _playerManager.Update(player.Id, player);
        }
    }


}

