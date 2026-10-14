using RimWorld;
using Verse;

namespace CosmicHorror
{
    // Shoggoths salvajes (las tres formas) en las Scarlands de Odyssey.
    internal class IncidentWorker_ShoggothWilds : IncidentWorker
    {
        private static readonly string[] shoggothKinds =
        {
            "ROM_ShoggothTiny", "ROM_Shoggoth", "ROM_ShoggothColossal"
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map?.Biome?.defName != "Scarlands")
            {
                return false;
            }
            return base.CanFireNowSub(parms: parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!RCellFinder.TryFindRandomPawnEntryCell(result: out IntVec3 cell, map: map, roadChance: 0f))
            {
                return false;
            }
            bool any = false;
            foreach (string kindName in shoggothKinds)
            {
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindName);
                if (kind == null)
                {
                    continue;
                }
                Pawn pawn = PawnGenerator.GeneratePawn(kindDef: kind, faction: null);
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(root: cell, map: map, radius: 8);
                if (loc == IntVec3.Invalid)
                {
                    Find.WorldPawns.PassToWorld(pawn: pawn,
                        discardMode: RimWorld.Planet.PawnDiscardDecideMode.Discard);
                    continue;
                }
                GenSpawn.Spawn(newThing: pawn, loc: loc, map: map);
                any = true;
            }
            if (!any)
            {
                return false;
            }
            Find.LetterStack.ReceiveLetter(label: "ROMCH_ShoggothWildsLabel".Translate(),
                text: "ROMCH_ShoggothWildsText".Translate(),
                textLetterDef: LetterDefOf.ThreatSmall,
                lookTargets: new TargetInfo(cell: cell, map: map), relatedFaction: null);
            return true;
        }
    }
}
