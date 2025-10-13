using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Commander.Lib.Common;
using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace Commander.Lib.Managers
{
    public interface RareManager : IDisposable
    {
        void Init();
    }

    public class RareManagerImpl : RareManager
    {
        private Logger _logger;
        private GameClient _gameClient;
        private GlobalProvider _globals;
        private SettingsManager _settingsManager;
        private bool _disposed = false;

        public RareManagerImpl(Logger logger, GameClient gameClient, GlobalProvider globals, SettingsManager settingsManager)
        {
            _logger = logger.Scope("VitaeManager");
            _globals = globals;
            _gameClient = gameClient;
            _settingsManager = settingsManager;
        }

        public void Init()
        {
            _globals.Core.EchoFilter.ServerDispatch += EchoFilter_ServerDispatch;
            _logger.Info("RareManager initialized.");
        }

        private void EchoFilter_ServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            if (e.Message.Type == 0xF7B0) 
            {
                int gameEvent = e.Message.Value<int>("event");

                if (gameEvent == 34)
                {
                    _processPutItemInContainer(e);
                }
            }
        }

        private HashSet<int> tier4Rares = new HashSet<int>() { 30352, 30353, 30354, 30355, 30356, 30357, 30358, 30359, 30360, 30361, 30362, 30363, 30364, 30365, 30366, 30367, 30368, 30369, 30370, 30371, 30372, 30373, 30510, 30511, 30512, 30513, 30514, 30515, 30516, 30517, 30518, 30519, 30520, 30521, 30522, 30523, 30524, 30525, 30526, 30527, 30528, 30529, 30530, 30531, 30532, 30533, 30534 };
        private HashSet<int> tier5Rares = new HashSet<int>() { 30074, 30075, 30076, 30077, 30078, 30079, 30080, 30081, 30082, 30083, 30084, 30085, 30086, 30087, 30088, 30089, 30090, 30091, 30092, 30093, 30094, 30095, 30096, 30097, 30098, 30099, 30100, 30101, 30102, 30103, 30104, 30105, 30106, 30110, 30111, 30112, 30113, 30114, 30115, 30116, 30117, 30118, 30119, 30120, 30121, 30122, 30123, 30124, 30125, 30126, 30127, 30128, 30130, 30131, 30132, 30133, 30134, 30135, 30136, 30137, 30138, 30139, 30140, 30141, 30142, 30143, 30144, 30145, 30146, 30147, 30148, 30149, 30150, 30151, 30152, 30153, 30154, 30155, 30157, 30158, 30159, 30160, 30161, 30162, 30163, 30164, 30165, 30166, 30167, 30168, 30169, 30171, 30173, 30174, 30175, 30176, 30179, 30180, 30247, 30248, 30249, 30253, 30254, 30936, 45361, 45362, 45363, 45364, 45365, 70001, 70002, 70003 };
        private HashSet<int> tier6Rares = new HashSet<int>() { 30302, 30303, 30304, 30305, 30306, 30307, 30308, 30309, 30345, 30346, 30347, 30348, 30349, 30350, 30351, 30374, 30375, 30376, 30377, 30378, 42662, 42663, 42664, 42665, 42666, 43848, 45436, 45437, 45438, 45439, 45440, 45441, 45442, 45443, 45444, 45445, 45446, 45447, 45448, 45449, 45450, 45451, 45452, 45453, 45454, 45455, 45456, 45457, 45458, 45459, 45460, 45461, 45462, 45463, 45464, 45465, 45466, 45467, 45468, 45469, 45470 };

        private void _processPutItemInContainer(NetworkMessageEventArgs e)
        {
            int id = e.Message.Value<int>("item");
            var obj = _gameClient.GetWorldObject(id);

            if (obj != null)
            {
                var logOnRare = _settingsManager.Settings.LogOnRare;
                if (logOnRare)
                {
                    var wcid = obj.Values(LongValueKey.Type);
                    var rareId = obj.Values(LongValueKey.RareId);

                    if (tier4Rares.Contains(wcid) || tier5Rares.Contains(wcid) || tier6Rares.Contains(wcid))
                    {
                        var message = $"You have found the rare item {obj.Name}!";
                        _logger.Info("_processPutItemInContainer Rare Item Found");
                        _logger.WriteToChat(message);
                        _logger.WriteToWindow(message);
                        _gameClient.Logout();
                        return;
                    }
                }
            }
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
                    _globals.Core.EchoFilter.ServerDispatch -= EchoFilter_ServerDispatch;
                }
                _disposed = true;
            }
        }

    }
}
