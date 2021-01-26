using RimWorld;
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

    [HarmonyPatch(typeof(GameEnder), "CheckOrUpdateGameOver")]
    internal static class Patch_GameEnder
    {
        private static bool isIncidentQueued;

        private static void Postfix()
        {
            if (Find.GameEnder.gameEnding)
            {
                if (isIncidentQueued)
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
                        State.logger.Trace("Incident already queued");
                        State.logger.Trace(current);
                        return;
                    }
                }

                Map anyPlayerHomeMap = Find.AnyPlayerHomeMap;
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(wandererJoin.category, anyPlayerHomeMap);
                parms.forced = true;

                FiringIncident firingIncident = new FiringIncident(wandererJoin, null, parms);
                QueuedIncident queuedIncident = new QueuedIncident(firingIncident, tick + State.delayTicks, 0);

                State.logger.Trace("Queueing incident");
                State.logger.Trace(queuedIncident);
                incidentQueue.Add(queuedIncident);
                isIncidentQueued = true;
            }
            else
            {
                isIncidentQueued = false;
            }
        }
    }
}
