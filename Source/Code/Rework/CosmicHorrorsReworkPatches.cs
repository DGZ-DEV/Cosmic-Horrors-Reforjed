using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RimWorld;
using Rework;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CosmicHorror
{
    /// <summary>
    /// Parches y enganches nativos para Cosmic Horrors mediante Rework Reforjed.
    /// Sin Harmony. Todo procesado mediante atributos declarativos y cirugía IL.
    /// </summary>
    public static class CosmicHorrorsReworkPatches
    {
        // ---------------------------------------------------------------------
        // 1. JobDriver_PredatorHunt.CheckWarnPlayerInterval
        // ---------------------------------------------------------------------
        // Si el depredador es un horror cósmico con huntAlert = false, suprime la alerta
        // marcando notifiedPlayerAttacking = true y retornando de inmediato.
        [ReworkPatch(Type = "RimWorld.JobDriver_PredatorHunt", Method = "CheckWarnPlayerInterval", Priority = 10)]
        public static IEnumerable<Instruction> Transpile_CheckWarnPlayerInterval(IEnumerable<Instruction> instructions, ModuleDefinition module)
        {
            var list = instructions.ToList();
            if (list.Count == 0) return list;

            // Target method: bool ShouldSuppressHuntAlert(JobDriver_PredatorHunt driver)
            var helperMethod = module.ImportReference(
                typeof(CosmicHorrorsReworkPatches).GetMethod(nameof(ShouldSuppressHuntAlert)));

            var first = list[0];
            var newInstructions = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, helperMethod),
                Instruction.Create(OpCodes.Brfalse_S, first),
                Instruction.Create(OpCodes.Ret)
            };

            newInstructions.AddRange(list);
            return newInstructions;
        }

        public static bool ShouldSuppressHuntAlert(JobDriver_PredatorHunt driver)
        {
            try
            {
                if (driver?.pawn?.def != null &&
                    driver.pawn.def.HasModExtension<PawnExtension>() &&
                    !driver.pawn.def.GetModExtension<PawnExtension>().huntAlert)
                {
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        // ---------------------------------------------------------------------
        // 2. RimWorld.PawnApparelGenerator.GenerateStartingApparelFor
        // ---------------------------------------------------------------------
        // Si el peón es un monstruo con generateApparel = false, salta la generación de ropa.
        [ReworkRedirect(Type = "RimWorld.PawnApparelGenerator", Method = "GenerateStartingApparelFor")]
        public static void Redirect_GenerateStartingApparelFor(Pawn pawn, PawnGenerationRequest request)
        {
            PawnExtension pawnEx = pawn?.def?.GetModExtension<PawnExtension>();
            if (pawnEx != null && !pawnEx.generateApparel)
            {
                return;
            }

            PawnApparelGenerator.GenerateStartingApparelFor(pawn, request);
        }

        // ---------------------------------------------------------------------
        // 3. RimWorld.LordToil_AssaultColony.UpdateAllDuties
        // ---------------------------------------------------------------------
        // Asigna duty ROM_AssaultAndKidnap a los horrores cósmicos en asaltos (excepto Shoggoth).
        [ReworkPatch(Type = "RimWorld.LordToil_AssaultColony", Method = "UpdateAllDuties", Priority = 10)]
        public static IEnumerable<Instruction> Transpile_UpdateAllDuties(IEnumerable<Instruction> instructions, ModuleDefinition module)
        {
            var list = instructions.ToList();
            var helperMethod = module.ImportReference(
                typeof(CosmicHorrorsReworkPatches).GetMethod(nameof(Postfix_UpdateAllDuties)));

            // Inserta una llamada a Postfix_UpdateAllDuties(this) antes de cada 'Ret'
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].OpCode == OpCodes.Ret)
                {
                    list.Insert(i, Instruction.Create(OpCodes.Ldarg_0));
                    list.Insert(i + 1, Instruction.Create(OpCodes.Call, helperMethod));
                    i += 2;
                }
            }

            return list;
        }

        public static void Postfix_UpdateAllDuties(LordToil_AssaultColony toil)
        {
            if (toil?.lord?.ownedPawns == null) return;

            DutyDef assaultAndKidnapDef = DefDatabase<DutyDef>.GetNamedSilentFail("ROM_AssaultAndKidnap");
            if (assaultAndKidnapDef == null) return;

            for (int i = 0; i < toil.lord.ownedPawns.Count; i++)
            {
                if (toil.lord.ownedPawns[i] is CosmicHorrorPawn cPawn && cPawn.def.defName != "ROM_Shoggoth")
                {
                    cPawn.mindState.duty = new PawnDuty(assaultAndKidnapDef);
                }
            }
        }

        // ---------------------------------------------------------------------
        // 4. RimWorld.TrashUtility.TrashJob
        // ---------------------------------------------------------------------
        // Los horrores cósmicos siempre usan ataque cuerpo a cuerpo para destruir estructuras.
        [ReworkRedirect(Type = "RimWorld.TrashUtility", Method = "TrashJob")]
        public static Job Redirect_TrashJob(Pawn pawn, Thing t, bool allowPunchingInert, bool killIncappedTarget)
        {
            if (pawn is CosmicHorrorPawn)
            {
                Job job = new Job(JobDefOf.AttackMelee, t);
                return job;
            }

            return TrashUtility.TrashJob(pawn, t, allowPunchingInert, killIncappedTarget);
        }

        // ---------------------------------------------------------------------
        // 5. Verse.HediffSet.CalculatePain
        // ---------------------------------------------------------------------
        // Escala de dolor modificada por painFactor para los horrores cósmicos.
        [ReworkRedirect(Type = "Verse.HediffSet", Method = "CalculatePain")]
        public static float Redirect_CalculatePain(HediffSet hediffSet)
        {
            if (hediffSet?.pawn is CosmicHorrorPawn p)
            {
                if (p.Dead) return 0f;

                float num = 0f;
                float num2 = 0f;
                if (hediffSet.hediffs != null && hediffSet.hediffs.Count > 0)
                {
                    foreach (Hediff hediff in hediffSet.hediffs)
                    {
                        num += hediff?.PainOffset ?? 0;
                    }

                    num2 = num / (p.HealthScale > 0 ? p.HealthScale : 1f);
                    num2 *= p.PawnExtension?.painFactor ?? 1f;
                    foreach (Hediff hedifft in hediffSet.hediffs)
                    {
                        num2 *= hedifft?.PainFactor ?? 1f;
                    }
                }

                return Mathf.Clamp(num2, 0f, 1f);
            }

            if (_calculatePainMethod == null)
            {
                _calculatePainMethod = typeof(HediffSet).GetMethod("CalculatePain", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            }
            return (float)_calculatePainMethod.Invoke(hediffSet, null);
        }

        private static System.Reflection.MethodInfo _calculatePainMethod;

        // ---------------------------------------------------------------------
        // 6. RimWorld.ThingSelectionUtility.SelectableByMapClick
        // ---------------------------------------------------------------------
        // Monstruos invisibles (Star Vampires antes de alimentarse) no son seleccionables con click.
        [ReworkRedirect(Type = "RimWorld.ThingSelectionUtility", Method = "SelectableByMapClick")]
        public static bool Redirect_SelectableByMapClick(Thing t)
        {
            if (t is CosmicHorrorPawn cosmicPawn && cosmicPawn.IsInvisible)
            {
                return false;
            }

            return ThingSelectionUtility.SelectableByMapClick(t);
        }

        // ---------------------------------------------------------------------
        // 7. Verse.AI.AttackTargetFinder.BestAttackTarget
        // ---------------------------------------------------------------------
        // Monstruos invisibles no pueden ser detectados como objetivo de ataque.
        [ReworkRedirect(Type = "Verse.AI.AttackTargetFinder", Method = "BestAttackTarget")]
        public static IAttackTarget Redirect_BestAttackTarget(
            IAttackTargetSearcher searcher,
            TargetScanFlags flags,
            Predicate<Thing> validator,
            float minDist,
            float maxDist,
            IntVec3 locus,
            float maxTravelRadiusFromLocus,
            bool canBashDoors,
            bool canTakeTargetsCloserThanEffectiveMinRange,
            bool canBashFences,
            bool onlyRanged)
        {
            Predicate<Thing> origValidator = validator;
            Predicate<Thing> wrappedValidator = delegate(Thing t)
            {
                return (origValidator == null || origValidator(t))
                       && (!(t is CosmicHorrorPawn cosmicPawn) || !cosmicPawn.IsInvisible);
            };

            return AttackTargetFinder.BestAttackTarget(
                searcher,
                flags,
                wrappedValidator,
                minDist,
                maxDist,
                locus,
                maxTravelRadiusFromLocus,
                canBashDoors,
                canTakeTargetsCloserThanEffectiveMinRange,
                canBashFences,
                onlyRanged);
        }
    }
}
