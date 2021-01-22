using RimWorld;
using Verse;
using HarmonyLib;

namespace EndlessRimworld
{
    [HarmonyPatch(typeof(GameEnder), "CheckOrUpdateGameOver"), StaticConstructorOnStartup]
    public static class Patch_GameEnder
    {
        static Patch_GameEnder()
        {
            Harmony harmony = new Harmony("jaschaephraim.endlessrimworld");
            harmony.PatchAll();
        }

        static void Postfix()
        {
            GameEnder gameEnder = Find.GameEnder;
            if (gameEnder.gameEnding)
            {
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
                incidentQueue.Add(wandererJoin, GenDate.TicksPerDay, parms);
            }
        }
    }
}
