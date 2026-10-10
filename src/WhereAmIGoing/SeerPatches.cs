using System;
using HarmonyLib;
using RoR2;

namespace WhereAmIGoing
{
    [HarmonyPatch(typeof(PurchaseInteraction))]
    internal static class SeerPatches
    {
        private static bool displayNameErrorLogged;
        private static bool contextErrorLogged;

        // Feeds the ping chat line: "<player> has found: Lunar Seer (Sky Meadow) (2 Lunar)."
        [HarmonyPostfix, HarmonyPatch(nameof(PurchaseInteraction.GetDisplayName))]
        private static void DisplayNamePostfix(PurchaseInteraction __instance, ref string __result)
        {
            try
            {
                if (SeerDestination.TryGetStageName(__instance, out string stageName))
                {
                    __result = SeerText.AppendStage(__result, stageName);
                }
            }
            catch (Exception e)
            {
                LogOnce(ref displayNameErrorLogged, e);
            }
        }

        // The interaction prompt: "Dream of Sky Meadow (2 Lunar)".
        [HarmonyPostfix, HarmonyPatch(nameof(PurchaseInteraction.GetContextString))]
        private static void ContextPostfix(PurchaseInteraction __instance, ref string __result)
        {
            try
            {
                if (SeerDestination.TryGetStageName(__instance, out string stageName))
                {
                    string contextLabel = Language.GetString(__instance.contextToken);
                    __result = SeerText.InsertStageIntoContext(__result, contextLabel, stageName);
                }
            }
            catch (Exception e)
            {
                LogOnce(ref contextErrorLogged, e);
            }
        }

        private static void LogOnce(ref bool logged, Exception e)
        {
            if (!logged)
            {
                logged = true;
                WhereAmIGoingPlugin.Log.LogError(e);
            }
        }
    }
}
