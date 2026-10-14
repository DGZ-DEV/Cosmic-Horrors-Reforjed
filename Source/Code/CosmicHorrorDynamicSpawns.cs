using System.Collections.Generic;
using System.Linq;
using Rework;
using RimWorld;
using Verse;

namespace CosmicHorror
{
    // Apariciones ligadas al mundo: lluvia trae Deep Ones, el espacio trae
    // Star Vampires (Odyssey), los desiertos esconden larvas en los peligros
    // antiguos. Todo con mecanica vanilla (GeneratePawn + GenSpawn).
    public static class CosmicHorrorDynamicSpawns
    {
        private static readonly Dictionary<int, int> rainCountdown = new Dictionary<int, int>();

        private static readonly string[] vampireKinds =
        {
            "ROM_StarVampire", "ROM_StarVampireAlbino", "ROM_StarVampireAbberation", "ROM_StarVampireNight"
        };

        public static bool OdysseyActive =>
            LoadedModManager.RunningModsListForReading.Any(m => m.PackageId == "ludeon.rimworld.odyssey");

        public static bool IsSwampOrPolluted(Map map)
        {
            string biome = map?.Biome?.defName;
            if (biome == "TemperateSwamp" || biome == "TropicalSwamp" || biome == "ColdBog")
            {
                return true;
            }
            if (map?.pollutionGrid != null && map.pollutionGrid.TotalPollutionPercent > 0.05f)
            {
                return true;
            }
            return false;
        }

        [ReworkHook(ReworkHookPoint.MapTick)]
        public static void OnMapTick(Verse.Map map)
        {
            if (map == null || !map.IsPlayerHome)
            {
                return;
            }
            if ((Find.TickManager.TicksGame + map.uniqueID) % 250 != 0)
            {
                return;
            }
            TickRain(map);
            TickSpace(map);
        }

        // Lluvia: 2 horas despues (5000 ticks), 1 de cada 5, manada de Deep
        // Ones con un Great liderando.
        private static void TickRain(Map map)
        {
            if (!rainCountdown.TryGetValue(map.uniqueID, out int left))
            {
                left = -1;
            }
            WeatherDef cur = map.weatherManager?.curWeather;
            string curName = cur?.defName;
            bool raining = curName == "Rain" || curName == "RainyThunderstorm";
            if (left < 0)
            {
                if (raining)
                {
                    rainCountdown[map.uniqueID] = 5000;
                }
                return;
            }
            left -= 250;
            if (left > 0)
            {
                rainCountdown[map.uniqueID] = left;
                return;
            }
            rainCountdown.Remove(map.uniqueID);
            if (!(Rand.Value < 0.2f))
            {
                return;
            }
            SpawnDeepOnePack(map);
        }

        private static void SpawnDeepOnePack(Map map)
        {
            if (!RCellFinder.TryFindRandomPawnEntryCell(result: out IntVec3 cell, map: map, roadChance: 0f))
            {
                return;
            }
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named(defName: "ROM_DeepOne"));
            PawnKindDef deepOne = DefDatabase<PawnKindDef>.GetNamedSilentFail(defName: "ROM_DeepOne");
            PawnKindDef great = DefDatabase<PawnKindDef>.GetNamedSilentFail(defName: "ROM_DeepOneGreat");
            if (deepOne == null || great == null)
            {
                return;
            }
            int count = Rand.RangeInclusive(3, 5);
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(kindDef: deepOne, faction: faction);
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(root: cell, map: map, radius: 8);
                if (loc != IntVec3.Invalid)
                {
                    GenSpawn.Spawn(newThing: pawn, loc: loc, map: map);
                }
            }
            Pawn leader = PawnGenerator.GeneratePawn(kindDef: great, faction: faction);
            GenSpawn.Spawn(newThing: leader, loc: cell, map: map);
            Find.LetterStack.ReceiveLetter(label: "Deep ones surge from the flood",
                text: "Drawn by the rain, deep ones crawl ashore, led by a greater one.",
                textLetterDef: LetterDefOf.ThreatBig,
                lookTargets: new TargetInfo(cell: cell, map: map), relatedFaction: faction);
        }

        // Espacio (Odyssey): una sola subespecie de vampiro cada vez, para que
        // no se peleen entre ellas.
        private static void TickSpace(Map map)
        {
            if (!OdysseyActive || map.Biome?.defName != "Space")
            {
                return;
            }
            if (!(Rand.Value < 0.001f))
            {
                return;
            }
            List<PawnKindDef> options = new List<PawnKindDef>();
            foreach (string kindName in vampireKinds)
            {
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindName);
                if (kind != null)
                {
                    options.Add(item: kind);
                }
            }
            if (options.Count == 0)
            {
                return;
            }
            PawnKindDef picked = options.RandomElement();
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named(defName: "ROM_StarVampire"));
            if (!RCellFinder.TryFindRandomPawnEntryCell(result: out IntVec3 cell, map: map, roadChance: 0f))
            {
                return;
            }
            int count = Rand.RangeInclusive(2, 3);
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(kindDef: picked, faction: faction);
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(root: cell, map: map, radius: 6);
                if (loc != IntVec3.Invalid)
                {
                    GenSpawn.Spawn(newThing: pawn, loc: loc, map: map);
                }
            }
            Find.LetterStack.ReceiveLetter(label: "Shapes in the void",
                text: "Star vampires drift aboard, hungry.",
                textLetterDef: LetterDefOf.ThreatBig,
                lookTargets: new TargetInfo(cell: cell, map: map), relatedFaction: faction);
        }

        // Desiertos: larvas anidando junto a los caskets de los peligros antiguos.
        [ReworkHook(ReworkHookPoint.MapGenerated)]
        public static void OnMapGenerated(Verse.Map map)
        {
            if (map == null)
            {
                return;
            }
            string biome = map.Biome?.defName;
            if (biome != "Desert" && biome != "ExtremeDesert")
            {
                return;
            }
            ThingDef casket = DefDatabase<ThingDef>.GetNamedSilentFail(defName: "AncientCryptosleepCasket");
            PawnKindDef larva = DefDatabase<PawnKindDef>.GetNamedSilentFail(defName: "ROM_ChthonianLarva");
            if (casket == null || larva == null)
            {
                return;
            }
            List<Thing> crypts = map.listerThings.ThingsOfDef(def: casket);
            int spawned = 0;
            foreach (Thing crypt in crypts)
            {
                if (spawned >= 6)
                {
                    return;
                }
                IntVec3 loc = GenAdj.CellsAdjacent8Way(pack: crypt)
                    .Where(c => c.Walkable(map: map) && c.InBounds(map: map))
                    .RandomElementWithFallback(fallback: IntVec3.Invalid);
                if (loc == IntVec3.Invalid)
                {
                    continue;
                }
                Pawn pawn = PawnGenerator.GeneratePawn(kindDef: larva, faction: null);
                GenSpawn.Spawn(newThing: pawn, loc: loc, map: map);
                spawned++;
            }
        }
    }
}
