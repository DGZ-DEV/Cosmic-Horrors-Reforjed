# Call of Cthulhu - Cosmic Horrors Reforjed

Port of Jecrell's Call of Cthulhu - Cosmic Horrors to RimWorld 1.6, running on
[Rework Reforjed](https://github.com/DGZ-DEV/Rework-Framework) instead of Harmony.
Lovecraftian monsters raiding and lurking around your colony: Star-Spawn of
Cthulhu, Shoggoths, Mi-Go, Deep Ones, Star Vampires, Dark Young and Chthonians.

## Requirements

- RimWorld **1.6**
- **Rework Reforjed** (`dgz.rework`), loaded before this mod
- Anomaly and Odyssey are optional. Each unlocks extra content (see below);
  nothing breaks without them.

## What the port adds over the original

- **Optional taming** (off by default, in the mod settings). Each monster keeps
  its own taming difficulty. With Anomaly installed, a species becomes tameable
  once recorded in the entity codex; without Anomaly, once its corpse is analyzed.
  Tamed horrors leave their raiding party and stop attacking your people.
- **Anomaly entity codex**: 7 entries (Basic / Advanced / Ultimate) with icons.
  Undiscovered entries show as silhouettes. Studying a corpse records it.
- **Corpse analysis**: select a horror corpse and order Analyze. Recording one
  of each unlocks its codex entry.
- **Dynamic spawns**: rain can bring Deep One packs ashore; Star Vampires drift
  in space maps (Odyssey) or fall in pods; Chthonian larvae nest in desert
  ancient dangers; Dark Young stick to swamps and pollution; Shoggoths roam the
  Scarlands; and on rare foggy rainy nights, something bigger visits.
- **Star Vampire invisibility** reworked for 1.6 rendering: unseen until it feeds.
- No Harmony dependency. Works alongside it, doesn't need it.

## Install

Unzip into your Mods folder so the layout is
`Mods/Cosmic Horrors Reforjed/About/About.xml`, enable it below Rework Reforjed
in the mod list. New colonies recommended; old saves load, but horrors already
on the map keep their state.

## Notes

- Compatibility patches for Giddy-up! and A Dog Said... apply only when those
  mods are active.
- Sanity loss and mood effects around horrors work as in the original.
- If the Analyze order doesn't show on a corpse, build a research bench first;
  the game requires one on the map.

## Credits

Original mod by Jecrell. This port keeps his Defs, art and sounds, and replaces
the code layer for 1.6 + Rework. MIT licensed, see LICENSE.
