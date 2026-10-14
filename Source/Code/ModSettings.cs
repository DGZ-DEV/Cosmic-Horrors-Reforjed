using UnityEngine;
using Verse;

namespace CosmicHorror
{
    public class ModMain : Mod
    {
        Settings settings;

        public ModMain(ModContentPack content) : base(content: content)
        {
            settings = GetSettings<Settings>();
        }

        public override string SettingsCategory() => "Call of Cthulhu - Cosmic Horrors";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect row = new Rect(x: inRect.x, y: inRect.y, width: inRect.width, height: 30f);
            bool before = settings.allowTaming;
            Widgets.CheckboxLabeled(rect: row, label: "ROMCH_AllowTaming".Translate(),
                checkOn: ref settings.allowTaming);
            TooltipHandler.TipRegion(rect: row, tip: "ROMCH_AllowTamingDesc".Translate());
            if (settings.allowTaming != before)
            {
                CosmicHorrorTaming.ApplyTamingSetting();
            }

            settings.Write();
        }
    }

    public class Settings : ModSettings
    {
        public bool allowTaming = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(value: ref allowTaming, label: "allowTaming", defaultValue: false);
        }
    }
}