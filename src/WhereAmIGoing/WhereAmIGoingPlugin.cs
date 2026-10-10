using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace WhereAmIGoing
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class WhereAmIGoingPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Harmony.CreateAndPatchAll(typeof(SeerPatches), MyPluginInfo.PLUGIN_GUID);
            Log.LogInfo("WhereAmIGoing loaded");
        }
    }
}
