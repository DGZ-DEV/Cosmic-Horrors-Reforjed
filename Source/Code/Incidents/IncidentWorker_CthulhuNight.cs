using RimWorld;
using Verse;

namespace CosmicHorror
{
    // El star-spawn cae una rara noche de niebla con lluvia sobre la colonia,
    // con varios Deep Ones. Solo de noche y con FoggyRain.
    internal class IncidentWorker_CthulhuNight : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (GenDate.DaysPassed < (ModInfo.cosmicHorrorRaidDelay + def.earliestDay))
            {
                return false;
            }
            int hour = GenLocalDate.HourOfDay(map: map);
            if (hour > 4 && hour < 22)
            {
                return false;
            }
            WeatherDef foggyRain = DefDatabase<WeatherDef>.GetNamedSilentFail(defName: "FoggyRain");
            if (foggyRain == null || map.weatherManager?.curWeather != foggyRain)
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
            Faction faction = Find.FactionManager.FirstFactionOfDef(
                facDef: FactionDef.Named(defName: "ROM_CosmicHorrors"));
            PawnKindDef spawn = DefDatabase<PawnKindDef>.GetNamedSilentFail(defName: "ROM_StarSpawnOfCthulhu");
            PawnKindDef deepOne = DefDatabase<PawnKindDef>.GetNamedSilentFail(defName: "ROM_DeepOne");
            if (spawn == null || deepOne == null)
            {
                return false;
            }
            Pawn star = PawnGenerator.GeneratePawn(kindDef: spawn, faction: faction);
            GenSpawn.Spawn(newThing: star, loc: cell, map: map);
            int count = Rand.RangeInclusive(2, 3);
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(kindDef: deepOne, faction: faction);
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(root: cell, map: map, radius: 8);
                if (loc != IntVec3.Invalid)
                {
                    GenSpawn.Spawn(newThing: pawn, loc: loc, map: map);
                }
            }
            Find.LetterStack.ReceiveLetter(label: "It dreams in the rain",
                text: "Through fog and rain, the star-spawn of Cthulhu walks into your colony, deep ones at its heels.",
                textLetterDef: LetterDefOf.ThreatBig,
                lookTargets: new TargetInfo(cell: cell, map: map), relatedFaction: faction);
            return true;
        }
    }
}
