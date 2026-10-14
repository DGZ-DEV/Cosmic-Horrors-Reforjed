using System.Collections.Generic;
using Rework;
using RimWorld;
using Verse;

namespace CosmicHorror
{
    // Estudio de cadaveres: analizar el cadaver de un horror desbloquea su
    // investigacion (requiredAnalyzed) e investigarla descubre su entrada del
    // codex de Anomaly (discoveredResearchProjects). Una vez cada uno: el
    // progreso de analisis y el codex se guardan en partida.
    public static class CosmicHorrorStudy
    {
        private class StudySpec
        {
            public int analysisID;
            public string[] corpses;
            public float hours;
        }

        private static readonly StudySpec[] specs =
        {
            new StudySpec { analysisID = 9100001,
                corpses = new[] { "Corpse_ROM_DeepOne", "Corpse_ROM_DeepOneGreat" }, hours = 1.5f },
            new StudySpec { analysisID = 9100002,
                corpses = new[] { "Corpse_ROM_MiGo" }, hours = 1.5f },
            new StudySpec { analysisID = 9100003,
                corpses = new[] { "Corpse_ROM_Shoggoth", "Corpse_ROM_ShoggothTiny", "Corpse_ROM_ShoggothColossal" }, hours = 2f },
            new StudySpec { analysisID = 9100004,
                corpses = new[] { "Corpse_ROM_DarkYoung" }, hours = 2f },
            new StudySpec { analysisID = 9100005,
                corpses = new[] { "Corpse_ROM_StarVampire" }, hours = 2f },
            new StudySpec { analysisID = 9100006,
                corpses = new[] { "Corpse_ROM_Chthonian", "Corpse_ROM_ChthonianLarva" }, hours = 2.5f },
            new StudySpec { analysisID = 9100007,
                corpses = new[] { "Corpse_ROM_Cthulhid" }, hours = 2.5f }
        };

        public static void RegisterCorpseAnalysis()
        {
            int injected = 0;
            foreach (StudySpec spec in specs)
            {
                foreach (string corpseName in spec.corpses)
                {
                    ThingDef corpse = DefDatabase<ThingDef>.GetNamedSilentFail(corpseName);
                    if (corpse == null)
                    {
                        continue;
                    }
                    if (corpse.comps == null)
                    {
                        corpse.comps = new List<CompProperties>();
                    }
                    if (corpse.GetCompProperties<CompProperties_Analyzable>() != null)
                    {
                        continue;
                    }
                    corpse.comps.Add(new CompProperties_CompAnalyzableUnlockResearch
                    {
                        analysisID = spec.analysisID,
                        requiresMechanitor = false,
                        analysisDurationHours = spec.hours,
                        destroyedOnAnalyzed = false,
                        canStudyInPlace = true,
                        allowRepeatAnalysis = false,
                        showProgress = true,
                        activateTexPath = "UI/Icons/Study",
                        activateLabelString = "Analyze...",
                        activateDescString = "Analyze the corpse.",
                        jobString = "Analyzing corpse",
                        guiLabelString = "Choose who should analyze this",
                        inspectString = "Can be analyzed by a colonist.",
                        completedLetterLabel = "Corpse analyzed",
                        completedLetter = "By studying the corpse, {PAWN_labelShort} has learned all it can teach.",
                        completedLetterDef = LetterDefOf.NeutralEvent
                    });
                    injected++;
                }
            }
            Log.Message("CosmicHorrorStudy: analisis registrado en " + injected + " cadaveres.");
        }

        // Los defs de cadaver (implied) aun no existen en [StaticConstructorOnStartup:
        // se reintenta al terminar de cargar la partida, con los defs completos.
        [ReworkHook(ReworkHookPoint.GameInitialized)]
        public static void OnGameInitialized(Verse.Game game)
        {
            RegisterCorpseAnalysis();
            CosmicHorrorTaming.ApplyTamingSetting();
        }

        private static readonly Dictionary<string, string> corpseToCodex = new Dictionary<string, string>
        {
            { "Corpse_ROM_DeepOne", "ROM_DeepOneCodex" },
            { "Corpse_ROM_DeepOneGreat", "ROM_DeepOneCodex" },
            { "Corpse_ROM_MiGo", "ROM_MiGoCodex" },
            { "Corpse_ROM_Shoggoth", "ROM_ShoggothCodex" },
            { "Corpse_ROM_ShoggothTiny", "ROM_ShoggothCodex" },
            { "Corpse_ROM_ShoggothColossal", "ROM_ShoggothCodex" },
            { "Corpse_ROM_DarkYoung", "ROM_DarkYoungCodex" },
            { "Corpse_ROM_StarVampire", "ROM_StarVampireCodex" },
            { "Corpse_ROM_Chthonian", "ROM_ChthonianCodex" },
            { "Corpse_ROM_ChthonianLarva", "ROM_ChthonianCodex" },
            { "Corpse_ROM_Cthulhid", "ROM_CthulhidCodex" }
        };

        // El descubrir vanilla (spawn, unfog, research...) tambien habilita el
        // tameo de ese bicho al momento.
        [ReworkRedirect(Type = "RimWorld.EntityCodex", Method = "SetDiscovered")]
        public static void Redirect_SetDiscovered(EntityCodex codex, EntityCodexEntryDef entry,
            ThingDef discoveredDef, Thing discoveredThing)
        {
            codex.SetDiscovered(entry, discoveredDef, discoveredThing);
            try
            {
                if (entry != null)
                {
                    CosmicHorrorTaming.UpdateRacesForEntry(entry.defName);
                }
            }
            catch (System.Exception e)
            {
                Log.Error("CosmicHorrorStudy: fallo al aplicar tameo tras descubrir: " + e);
            }
        }

        // Al terminar de analizar el cadaver, su entrada del codex queda
        // descubierta. Solo salta una vez: si ya estaba descubierta no se repite.
        [ReworkRedirect(Type = "RimWorld.CompAnalyzable", Method = "OnAnalyzed")]
        public static void Redirect_OnAnalyzed(CompAnalyzable comp, Pawn analyst)
        {
            comp.OnAnalyzed(analyst);
            try
            {
                Thing corpse = comp?.parent;
                if (corpse == null)
                {
                    return;
                }
                if (!corpseToCodex.TryGetValue(corpse.def.defName, out string entryName))
                {
                    return;
                }
                EntityCodexEntryDef entry = DefDatabase<EntityCodexEntryDef>.GetNamedSilentFail(entryName);
                EntityCodex codex = Find.EntityCodex;
                Log.Message("CosmicHorrorStudy: analizado " + corpse.def.defName + ", entrada=" +
                    (entry != null ? entry.defName : "NULL") + ", codex=" + (codex != null ? "OK" : "NULL") +
                    ", descubiertoAntes=" + (entry != null && codex != null && codex.Discovered(entry)));
                if (entry == null || codex == null || codex.Discovered(entry))
                {
                    return;
                }
                codex.SetDiscovered(entry, corpse.def, corpse);
                Log.Message("CosmicHorrorStudy: descubierto " + entry.defName + ", descubiertoDespues=" +
                    codex.Discovered(entry));
                CosmicHorrorTaming.UpdateRacesForEntry(entryName);
                Find.LetterStack.ReceiveLetter("Entity recorded: " + entry.label, entry.description,
                    LetterDefOf.PositiveEvent);
            }
            catch (System.Exception e)
            {
                Log.Error("CosmicHorrorStudy: fallo al descubrir codex: " + e);
            }
        }
    }
}
