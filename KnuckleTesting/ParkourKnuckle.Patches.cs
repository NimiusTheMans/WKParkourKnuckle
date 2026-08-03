using HarmonyLib;
using UnityEngine.EventSystems;
using ParkourKnuckle.UI;

namespace ParkourKnuckle.Patches
{
    [HarmonyPatch(typeof(EventSystem), "Update")]
    public static class GlobalUIUpdatePatch
    {
        static void Postfix()
        {
            if (ParkourUI.Instance == null)
            {
                ParkourUI.Initialize();

                ParkourUI.Instance?.ToggleUIPanel(Plugin.isUIVisible);
            }

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F4))
            {
                Plugin.isUIVisible = !Plugin.isUIVisible;

                ParkourUI.Instance?.ToggleUIPanel(Plugin.isUIVisible);
            }
        }
    }
}