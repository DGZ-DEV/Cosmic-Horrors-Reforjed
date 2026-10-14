using RimWorld;
using Verse;


namespace CosmicHorror
{
    internal class IncidentWorker_DarkYoung : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (GenDate.DaysPassed < (ModInfo.cosmicHorrorRaidDelay + def.earliestDay))
            {
                return false;
            }
            if (!CosmicHorrorDynamicSpawns.IsSwampOrPolluted(map: map))
            {
                return false;
            }

            return base.CanFireNowSub(parms: parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!RCellFinder.TryFindRandomPawnEntryCell(result: out IntVec3 intVec, map: map, roadChance: 0.5f))
            {
                return false;
            }

            Find.LetterStack.ReceiveLetter(label: "DarkYoungIncidentLabel".Translate(),
                text: "DarkYoungIncidentDesc".Translate(), textLetterDef: LetterDefOf.ThreatSmall,
                lookTargets: new TargetInfo(cell: intVec, map: map), relatedFaction: null);

            SpawnDarkYoung(parms: parms, vecparms: intVec);

            return true;
        }

        public void SpawnDarkYoung(IncidentParms parms, IntVec3 vecparms)
        {
            int iwCount = 1;
            IntVec3 intVec = vecparms;
            Map map = (Map)parms.target;
            PawnKindDef DarkYoung = PawnKindDef.Named(defName: "ROM_DarkYoung");

            iwCount = parms.points switch
            {
                <= 500f => Rand.RangeInclusive(1, 2),
                <= 600f => Rand.RangeInclusive(2, 3),
                <= 1400f => Rand.RangeInclusive(4, 5),
                _ => Rand.RangeInclusive(5, 6)
            };

            for (int i = 0; i < iwCount; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(root: intVec, map: map, radius: 10);
                if (loc == IntVec3.Invalid)
                {
                    continue;
                }
                Pawn newThing = PawnGenerator.GeneratePawn(kindDef: DarkYoung, faction: null);
                CosmicHorrorPawn newDarkYoung = newThing as CosmicHorrorPawn;
                if (newDarkYoung == null)
                {
                    Find.WorldPawns.PassToWorld(pawn: newThing, discardMode: RimWorld.Planet.PawnDiscardDecideMode.Discard);
                    continue;
                }
                GenSpawn.Spawn(newThing: newDarkYoung, loc: loc, map: map);
            }
        }
    }
}