using BepInEx;
using BepInEx.Logging;

namespace WhereAmIGoing
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class WhereAmIGoingPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("WhereAmIGoing loaded");
        }
    }
}
