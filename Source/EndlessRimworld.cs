using RimWorld;
using Verse;
using HarmonyLib;
using HugsLib;
using HugsLib.Settings;

namespace EndlessRimworld
{
    internal class ERSettings
    {
        public static SettingHandle<int> delayTicks;
    }

    internal class EndlessRimworld : ModBase
    {
        public override string ModIdentifier => "EndlessRimworld";

        public override void DefsLoaded()
        {
            ERSettings.delayTicks = Settings.GetHandle(
                "delayTicks",
                "EndlessRimworldDelayTicks".Translate(),
                "EndlessRimworldDelayTicksDescription".Translate(),
                GenDate.TicksPerDay
            );
            ERSettings.delayTicks.ContextMenuEntries = new[]
            {
                new ContextMenuEntry("Immediately", () => ERSettings.delayTicks.Value = 0),
                new ContextMenuEntry("One hour", () => ERSettings.delayTicks.Value = 2500),
                new ContextMenuEntry("One day (default)", () => ERSettings.delayTicks.Value = 60000),
                new ContextMenuEntry("One week", () => ERSettings.delayTicks.Value = 420000),
            };
        }
    }

    [HarmonyPatch(typeof(GameEnder), "CheckOrUpdateGameOver"), StaticConstructorOnStartup]
    internal static class Patch_GameEnder
    {
        private static bool isWandererQueued;

        private static void Postfix()
        {
            if (Find.GameEnder.gameEnding)
            {
                if (isWandererQueued)
                {
                    return;
                }

                int tick = Find.TickManager.TicksGame;
                IncidentQueue incidentQueue = Find.Storyteller.incidentQueue;
                IncidentDef wandererJoin = IncidentDefOf.WandererJoin;

                foreach (QueuedIncident current in incidentQueue)
                {
                    if (current.FireTick - tick > GenDate.TicksPerDay)
                    {
                        break;
                    }
                    if (current.FiringIncident.def == wandererJoin)
                    {
                        return;
                    }
                }

                Map anyPlayerHomeMap = Find.AnyPlayerHomeMap;
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(wandererJoin.category, anyPlayerHomeMap);
                incidentQueue.Add(wandererJoin, tick + ERSettings.delayTicks, parms);
                isWandererQueued = true;
            }
            else
            {
                isWandererQueued = false;
            }
        }
    }
}
