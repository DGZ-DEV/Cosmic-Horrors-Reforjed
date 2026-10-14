using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace CosmicHorror
{
    public class DamageWorker_Constrict : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult result = new DamageResult();
            Pawn instigator = dinfo.Instigator as Pawn;
            Pawn target = victim as Pawn;
            if (instigator == null || target == null || victim == null || victim.Map == null)
            {
                return base.Apply(dinfo: dinfo, thing: victim);
            }
            if (Rework.Core.ReworkGrappleUtility.TryGrapple(grappler: instigator, victim: target, stunTicks: 300))
            {
                return base.Apply(dinfo: dinfo, thing: victim);
            }

            return result;
        }
    }
}