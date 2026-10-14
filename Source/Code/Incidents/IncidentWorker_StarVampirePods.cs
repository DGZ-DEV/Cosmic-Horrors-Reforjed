using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace CosmicHorror
{
    // Sin Odyssey no hay asteroides: los vampiros caen en capsulas sobre la
    // colonia tras desoir una peticion de auxilio. Una sola subespecie por
    // caida para que no se peleen entre ellas.
    internal class IncidentWorker_StarVampirePods : IncidentWorker
    {
        private static readonly string[] vampireKinds =
        {
            "ROM_StarVampire", "ROM_StarVampireAlbino", "ROM_StarVampireAbberation", "ROM_StarVampireNight"
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (CosmicHorrorDynamicSpawns.OdysseyActive)
            {
                return false;
            }
            Map map = (Map)parms.target;
            if (GenDate.DaysPassed < (ModInfo.cosmicHorrorRaidDelay + def.earliestDay))
            {
                return false;
            }
            return base.CanFireNowSub(parms: parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
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
                return false;
            }
            PawnKindDef picked = options.RandomElement();
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named(defName: "ROM_StarVampire"));
            List<Pawn> pawns = new List<Pawn>();
            int count = Rand.RangeInclusive(2, 3);
            for (int i = 0; i < count; i++)
            {
                pawns.Add(item: PawnGenerator.GeneratePawn(kindDef: picked, faction: faction));
            }
            PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms: parms, pawns: pawns);
            Lord lord = LordMaker.MakeNewLord(faction: faction,
                lordJob: new LordJob_AssaultColony(), map: map);
            foreach (Pawn pawn in pawns)
            {
                lord.AddPawn(p: pawn);
            }
            Find.LetterStack.ReceiveLetter(label: "ROMCH_PodsLabel".Translate(),
                text: "ROMCH_PodsText".Translate(),
                textLetterDef: LetterDefOf.ThreatBig,
                lookTargets: new TargetInfo(cell: parms.spawnCenter, map: map), relatedFaction: faction);
            return true;
        }
    }
}
