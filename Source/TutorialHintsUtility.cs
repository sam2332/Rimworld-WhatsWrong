using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WhatsWrong
{
    // Represents a tutorial hint for the UI
    public class TutorialHint
    {
        public string Text;
        public ThingDef DesignatorDef; // Thing to build, if applicable
        public Action OnClick; // Action to activate designator
        public bool HasDesignator;
    }

    // Utility class for scanning gameplay elements and generating hints
    public static class TutorialHintsUtility
    {
        // Checks if any beds are placed on the map
        public static bool IsBedPlaced(Map map)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building is Building_Bed)
                    return true;
            }
            return false;
        }

        // Checks if any ideology is selected/active
        public static bool IsIdeologySelected()
        {
            return Find.FactionManager.OfPlayer.ideos != null && Find.FactionManager.OfPlayer.ideos.PrimaryIdeo != null;
        }

        // Checks for biotech buildings or relevant pawns
        public static bool IsBiotechPresent(Map map)
        {
            if (!ModsConfig.BiotechActive)
                return false;
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                // TODO: Add specific biotech building checks
                if (building.def.defName.Contains("Gene") || building.def.defName.Contains("Biotech"))
                    return true;
            }
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                if (pawn.genes != null && pawn.genes.GenesListForReading.Count > 0)
                    return true;
            }
            return false;
        }

        // Checks for butcher tables or spots
        public static bool IsButcherTablePlaced(Map map)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building.def.defName == "ButcherySpot" || building.def.defName == "ButcheryTable")
                    return true;
            }
            return false;
        }

        // Checks for growing zones
        public static bool IsGrowingZonePresent(Map map)
        {
            foreach (Zone zone in map.zoneManager.AllZones)
            {
                if (zone is Zone_Growing)
                    return true;
            }
            return false;
        }

        // Checks for fishing spots/buildings
        public static bool IsFishingSpotPlaced(Map map)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building.def.defName == "FishingSpot")
                    return true;
            }
            return false;
        }

        // Returns a list of missing hints for the current map (with UI and designator info)
        public static List<TutorialHint> GetMissingHints(Map map)
        {
            var hints = new List<TutorialHint>();
            if (!IsBedPlaced(map))
            {
                hints.Add(CreateHint("No beds placed.", "Bed"));
            }
            if (!IsIdeologySelected())
            {
                hints.Add(CreateHint("No ideology selected.", null));
            }
            if (!IsBiotechPresent(map))
            {
                hints.Add(CreateHint("No biotech buildings or gene pawns found.", null));
            }
            if (!IsButcherTablePlaced(map))
            {
                hints.Add(CreateHint("No butcher table available.", "ButcheryTable"));
            }
            if (!IsGrowingZonePresent(map))
            {
                hints.Add(CreateHint("No growing zones found.", null, true));
            }
            if (!IsFishingSpotPlaced(map))
            {
                hints.Add(CreateHint("No fishing spots placed.", "FishingSpot"));
            }
            return hints;
        }

        // Helper to create a TutorialHint with designator activation
        private static TutorialHint CreateHint(string text, string defName, bool isZone = false)
        {
            var hint = new TutorialHint { Text = text };
            if (!string.IsNullOrEmpty(defName))
            {
                hint.DesignatorDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                hint.HasDesignator = hint.DesignatorDef != null;
                if (hint.HasDesignator)
                {
                    hint.OnClick = () =>
                    {
                        var designator = BuildCopyCommandUtility.FindAllowedDesignator(hint.DesignatorDef);
                        if (designator != null)
                        {
                            Find.DesignatorManager.Select(designator);
                        }
                    };
                }
            }
            else if (isZone)
            {
                hint.HasDesignator = true;
                hint.OnClick = () =>
                {
                    var zoneDesignator = new Designator_ZoneAdd_Growing();
                    Find.DesignatorManager.Select(zoneDesignator);
                };
            }
            return hint;
        }

        // Detection and activation logic references:
        // - Bed detection: Map.listerBuildings.allBuildingsColonist, Building_Bed
        // - Ideology detection: Find.FactionManager.OfPlayer.ideos, PrimaryIdeo
        // - Biotech detection: ModsConfig.BiotechActive, Building.def.defName, Pawn.genes
        // - Butcher table detection: Building.def.defName == "ButcheryTable"/"ButcherySpot"
        // - Growing zone detection: Map.zoneManager.AllZones, Zone_Growing
        // - Fishing spot detection: Building.def.defName == "FishingSpot"
        // - Designator activation: BuildCopyCommandUtility.FindAllowedDesignator, Find.DesignatorManager.Select
        // These patterns are based on RimWorld 1.6 decompiled game files.
        // Utility methods are designed for easy extension: add new detection and hint logic as needed.
        // All detection logic references actual game data structures and APIs for compatibility.
    }
}
