using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CosmicHorror
{
    // Tameo opcional de horrores, apagado por defecto. Cuando se activa, cada
    // raza descubierta recupera su Wildness (el del XML, o el de la tabla si no
    // trae); el resto queda en 1 y TameUtility.CanTame da false (< 1).
    public static class CosmicHorrorTaming
    {
        private static readonly string[] horrorRaces =
        {
            "ROM_Cthulhid", "ROM_DarkYoung", "ROM_DeepOne", "ROM_DeepOneGreat",
            "ROM_MiGo", "ROM_Shoggoth", "ROM_ShoggothColossal", "ROM_ShoggothTiny",
            "ROM_StarVampire", "ROM_Chthonian", "ROM_ChthonianLarva"
        };

        private static readonly Dictionary<string, string> raceToEntry = new Dictionary<string, string>
        {
            { "ROM_Cthulhid", "ROM_CthulhidCodex" },
            { "ROM_DarkYoung", "ROM_DarkYoungCodex" },
            { "ROM_DeepOne", "ROM_DeepOneCodex" },
            { "ROM_DeepOneGreat", "ROM_DeepOneCodex" },
            { "ROM_MiGo", "ROM_MiGoCodex" },
            { "ROM_Shoggoth", "ROM_ShoggothCodex" },
            { "ROM_ShoggothColossal", "ROM_ShoggothCodex" },
            { "ROM_ShoggothTiny", "ROM_ShoggothCodex" },
            { "ROM_StarVampire", "ROM_StarVampireCodex" },
            { "ROM_Chthonian", "ROM_ChthonianCodex" },
            { "ROM_ChthonianLarva", "ROM_ChthonianCodex" }
        };

        // Dificultad de tameo para las razas que no traen Wildness en el XML.
        // Las que lo traen (DarkYoung 0.885, Chthonian 0.989, larva 0.95) mandan.
        private static readonly Dictionary<string, float> fallbackWildness = new Dictionary<string, float>        {
            { "ROM_Cthulhid", 0.99f },
            { "ROM_DeepOne", 0.82f },
            { "ROM_DeepOneGreat", 0.88f },
            { "ROM_MiGo", 0.90f },
            { "ROM_Shoggoth", 0.96f },
            { "ROM_ShoggothColossal", 0.995f },
            { "ROM_ShoggothTiny", 0.78f },
            { "ROM_StarVampire", 0.94f }
        };

        private static readonly Dictionary<string, float> originalWildness = new Dictionary<string, float>();

        public static void ApplyTamingSetting()
        {
            Settings settings = LoadedModManager.GetMod<ModMain>()?.GetSettings<Settings>();
            System.Text.StringBuilder report = new System.Text.StringBuilder("Tameo ROM: allowTaming=");
            report.Append(settings != null && settings.allowTaming ? "ON" : "OFF").Append(' ');
            foreach (string defName in horrorRaces)
            {
                report.Append(defName).Append('=').Append(ApplyRace(defName)).Append(' ');
            }
            Log.Message(report.ToString());
        }

        public static void UpdateRacesForEntry(string entryName)
        {
            foreach (string defName in horrorRaces)
            {
                if (raceToEntry.TryGetValue(defName, out string e) && e == entryName)
                {
                    float value = ApplyRace(defName);
                    Log.Message("Tameo ROM: " + defName + " descubierto (" + entryName + "), Wildness=" + value);
                }
            }
        }

        private static float ApplyRace(string defName)
        {
            ThingDef race = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (race == null)
            {
                return -1f;
            }
            float original = EnsureOriginal(defName, race);
            Settings settings = LoadedModManager.GetMod<ModMain>()?.GetSettings<Settings>();
            bool tameable = settings != null && settings.allowTaming && RaceDiscovered(defName);
            float value = tameable ? original : 1f;
            SetWildness(race, value);
            return value;
        }

        private static float EnsureOriginal(string defName, ThingDef race)
        {
            if (!originalWildness.TryGetValue(defName, out float value))
            {
                StatModifier existing = race.statBases?.Find(sm => sm.stat == StatDefOf.Wildness);
                value = existing != null ? existing.value
                    : (fallbackWildness.TryGetValue(defName, out float fb) ? fb : 0.9f);
                originalWildness[defName] = value;
            }
            return value;
        }

        // Sin codex (sin Anomaly), el analisis del cadaver habilita el tameo.
        private static readonly Dictionary<string, int> raceAnalysis = new Dictionary<string, int>
        {
            { "ROM_Cthulhid", 9100007 },
            { "ROM_DarkYoung", 9100004 },
            { "ROM_DeepOne", 9100001 },
            { "ROM_DeepOneGreat", 9100001 },
            { "ROM_MiGo", 9100002 },
            { "ROM_Shoggoth", 9100003 },
            { "ROM_ShoggothColossal", 9100003 },
            { "ROM_ShoggothTiny", 9100003 },
            { "ROM_StarVampire", 9100005 },
            { "ROM_Chthonian", 9100006 },
            { "ROM_ChthonianLarva", 9100006 }
        };

        private static bool RaceDiscovered(string raceDefName)
        {
            try
            {
                if (raceToEntry.TryGetValue(raceDefName, out string entryName))
                {
                    EntityCodexEntryDef entry = DefDatabase<EntityCodexEntryDef>.GetNamedSilentFail(entryName);
                    EntityCodex codex = entry != null ? Find.EntityCodex : null;
                    if (entry != null && codex != null)
                    {
                        return codex.Discovered(entry);
                    }
                }
                if (raceAnalysis.TryGetValue(raceDefName, out int analysisID))
                {
                    AnalysisManager manager = Find.AnalysisManager;
                    if (manager == null)
                    {
                        return true;
                    }
                    return manager.TryGetAnalysisProgress(analysisID, out AnalysisDetails details) &&
                        details != null && details.Satisfied;
                }
                return true;
            }
            catch
            {
                return true;
            }
        }

        // Para logica en Pawns (p. ej. perdonar derribados): ajuste ON y descubierto.
        public static bool IsRaceTameableNow(string raceDefName)
        {
            Settings settings = LoadedModManager.GetMod<ModMain>()?.GetSettings<Settings>();
            return settings != null && settings.allowTaming && RaceDiscovered(raceDefName);
        }

        private static void SetWildness(ThingDef race, float value)
        {
            if (race.statBases == null)
            {
                race.statBases = new List<StatModifier>();
            }
            StatModifier mod = race.statBases.Find(sm => sm.stat == StatDefOf.Wildness);
            if (mod != null)
            {
                mod.value = value;
            }
            else
            {
                race.statBases.Add(new StatModifier { stat = StatDefOf.Wildness, value = value });
            }
        }
    }
}
