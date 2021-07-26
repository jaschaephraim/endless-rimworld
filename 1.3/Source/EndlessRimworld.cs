using RimWorld;
using RimWorld.QuestGen;
using Verse;
using HarmonyLib;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;

namespace EndlessRimworld
{
    internal class State
    {
        internal static ModLogger logger;
        internal static SettingHandle<int> delayTicks;
        internal static Map queuedIncidentMap;

        internal static bool IsIncidentQueued
        {
            get => queuedIncidentMap != null;
            set
            {
                queuedIncidentMap = value ? Find.AnyPlayerHomeMap : null;
            }
        }
    }

    internal class EndlessRimworld : ModBase
    {
        public override string ModIdentifier => "EndlessRimworld";

        private EndlessRimworld()
        {
            State.logger = Logger;
        }

        public override void DefsLoaded()
        {
            State.delayTicks = Settings.GetHandle(
                "delayTicks",
                "EndlessRimworldDelayTicks".Translate(),
                "EndlessRimworldDelayTicksDescription".Translate(),
                GenDate.TicksPerDay
            );
            State.delayTicks.ContextMenuEntries = new[]
            {
                new ContextMenuEntry("Immediately", () => State.delayTicks.Value = 0),
                new ContextMenuEntry("One hour", () => State.delayTicks.Value = 2500),
                new ContextMenuEntry("One day (default)", () => State.delayTicks.Value = 60000),
                new ContextMenuEntry("One week", () => State.delayTicks.Value = 420000),
            };
        }
    }

    [HarmonyPatch(typeof(Root), "Start")]
    internal static class Patch_Root_Start
    {
        private static void Prefix()
        {
            State.logger.Trace("Resetting queued incident");
            State.IsIncidentQueued = false;
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_GiveQuest), "CanFireNowSub")]
    internal static class Patch_IncidentWorker_GiveQuest_CanFireNowSub
    {
        private static void Postfix(ref bool __result, IncidentWorker __instance)
        {
            if (__instance.def.defName == "EndlessRimworld_WandererJoin")
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(QuestGen_Get), "GetMap")]
    internal static class Patch_QuestGen_Get_GetMap
    {
        private static void Postfix(ref Map __result)
        {
            if (State.IsIncidentQueued)
            {
                __result = State.queuedIncidentMap;
            }
        }
    }

    [HarmonyPatch(typeof(GameEnder), "CheckOrUpdateGameOver")]
    internal static class Patch_GameEnder
    {
        private static void Postfix()
        {
            if (Find.GameEnder.gameEnding)
            {
                if (State.IsIncidentQueued)
                {
                    return;
                }

                int tick = Find.TickManager.TicksGame;
                IncidentQueue incidentQueue = Find.Storyteller.incidentQueue;

                IncidentDef wandererJoin = IncidentDefOf.WandererJoin;
                IncidentDef erWandererJoin = IncidentDef.Named("EndlessRimworld_WandererJoin");

                foreach (QueuedIncident current in incidentQueue)
                {
                    if (current.FireTick - tick > GenDate.TicksPerDay)
                    {
                        break;
                    }
                    if (current.FiringIncident.def == wandererJoin)
                    {
                        State.logger.Trace("Incident already queued");
                        State.logger.Trace(current);
                        return;
                    }
                }

                State.IsIncidentQueued = true;
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(erWandererJoin.category, State.queuedIncidentMap);
                parms.forced = true;

                FiringIncident firingIncident = new FiringIncident(erWandererJoin, null, parms);
                QueuedIncident queuedIncident = new QueuedIncident(firingIncident, tick + State.delayTicks, 0);

                State.logger.Trace("Queueing incident");
                State.logger.Trace(queuedIncident);
                incidentQueue.Add(queuedIncident);
            }
            else
            {
                State.IsIncidentQueued = false;
            }
        }
    }
}
