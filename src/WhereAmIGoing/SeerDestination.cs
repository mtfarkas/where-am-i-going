using RoR2;
using UnityEngine;

namespace WhereAmIGoing
{
    internal static class SeerDestination
    {
        // Resolves the localized name of the stage a Lunar Seer leads to.
        // Returns false for anything that isn't a Seer with a valid target.
        public static bool TryGetStageName(Component component, out string stageName)
        {
            stageName = null;
            if (!component)
            {
                return false;
            }

            var seer = component.GetComponent<SeerStationController>();
            if (!seer)
            {
                seer = component.GetComponentInParent<SeerStationController>();
            }
            if (!seer)
            {
                return false;
            }

            // Same bounds check the game uses before looking the index up; -1 means no target.
            int index = seer.NetworktargetSceneDefIndex;
            if ((uint)index >= (uint)SceneCatalog.sceneDefCount)
            {
                return false;
            }

            SceneDef sceneDef = SceneCatalog.GetSceneDef((SceneIndex)index);
            if (!sceneDef || string.IsNullOrEmpty(sceneDef.nameToken))
            {
                return false;
            }

            stageName = Language.GetString(sceneDef.nameToken);
            return true;
        }
    }
}
