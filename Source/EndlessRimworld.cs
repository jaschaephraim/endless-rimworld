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
            Harmony harmony = new Harmony("com.github.jaschaephraim.endless-rimworld");
            harmony.PatchAll();
        }

        static void Postfix()
        {
            GameEnder gameEnder = Find.GameEnder;
            if (gameEnder.gameEnding)
            {
                gameEnder.gameEnding = false;

                IncidentDef wandererJoin = IncidentDefOf.WandererJoin;
                Map anyPlayerHomeMap = Find.AnyPlayerHomeMap;
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(wandererJoin.category, anyPlayerHomeMap);
                Find.Storyteller.incidentQueue.Add(wandererJoin, 0, parms);
            }
        }
    }
}
