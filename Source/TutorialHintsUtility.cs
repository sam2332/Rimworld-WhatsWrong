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
        // Checks if there are enough beds for all colonists
        public static bool AreEnoughBedsPlaced(Map map)
        {
            int bedCount = 0;
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building is Building_Bed)
                    bedCount++;
            }
            int colonistCount = map.mapPawns.FreeColonists.Count;
            return bedCount >= colonistCount && colonistCount > 0;
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

        // Checks for fishing zones
        public static bool IsFishingZonePresent(Map map)
        {
            foreach (Zone zone in map.zoneManager.AllZones)
            {
                if (zone is RimWorld.Zone_Fishing)
                    return true;
            }
            return false;
        }
        // Checks for stockpile zones (by label)
        public static bool IsStockpileZonePresent(Map map)
        {
            foreach (Zone zone in map.zoneManager.AllZones)
            {
                if (zone is RimWorld.Zone_Stockpile stockpileZone &&
                    stockpileZone.label == RimWorld.StorageSettingsPreset.DefaultStockpile.PresetName())
                {
                    return true;
                }
            }
            return false;
        }

        // Checks for dumping stockpile zones (by label)
        public static bool IsDumpingStockpileZonePresent(Map map)
        {
            foreach (Zone zone in map.zoneManager.AllZones)
            {
                if (zone is RimWorld.Zone_Stockpile stockpileZone &&
                    stockpileZone.label == RimWorld.StorageSettingsPreset.DumpingStockpile.PresetName())
                {
                    return true;
                }
            }
            return false;
        }

        // Checks if there is any food in storage
        public static bool IsFoodInStorage(Map map)
        {
            // Check all storage facilities including stockpiles and shelves
            IEnumerable<SlotGroup> allGroups = map.haulDestinationManager.AllGroups;
            
            foreach (SlotGroup slotGroup in allGroups)
            {
                foreach (Thing thing in slotGroup.HeldThings)
                {
                    // Check if the item is food
                    if (thing.def.IsNutritionGivingIngestible && thing.def.ingestible.HumanEdible)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        // Checks if there are cooking facilities (stove, campfire, etc.)
        public static bool IsCookingFacilityPresent(Map map)
        {
            List<string> cookingFacilities = new List<string>
            {
                "Stove",
                "Campfire",
                "ElectricStove",
                "ButcherTable" // Butcher tables can also be used for cooking
            };
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (cookingFacilities.Contains(building.def.defName))
                {
                    return true;
                }
            }
            return false;
        }

        // Checks if there is a table and chair for eating
        public static bool IsTableAndChairPresent(Map map){
            List<string> tables = new List<string>
            {
                "Table",
                "DiningTable",
                "LongTable"
            };
            List<string> chairs = new List<string>
            {
                "Chair",
                "Armchair",
                "Stool"
            };
            bool hasTable = false;
            bool hasChair = false;
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (tables.Contains(building.def.defName))
                {
                    hasTable = true;
                }
                if (chairs.Contains(building.def.defName))
                {
                    hasChair = true;
                }
                if (hasTable && hasChair)
                {
                    return true; // Both table and chair found
                }
            }
            return false;
        }
        // Checks if there are any recreation sources (horseshoe pin, chess table, etc.)
        public static bool IsRecreationSourcePresent(Map map)
        {
            List<string> recreationSources = new List<string>
            {
                "HorseshoePin",
                "ChessTable",
                "Jukebox",
                "BilliardsTable"
            };
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (recreationSources.Contains(building.def.defName))
                {
                    return true;
                }
            }
            return false;
        }
        // Returns a list of missing hints for the current map (with UI and designator info)
        public static List<TutorialHint> GetMissingHints(Map map)
        {
            var hints = new List<TutorialHint>();
            if (!AreEnoughBedsPlaced(map))
            {
                int colonistCount = map.mapPawns.FreeColonists.Count;
                int bedCount = 0;
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building is Building_Bed)
                        bedCount++;
                }
                string bedHintText = colonistCount > 0
                    ? $"Not enough beds for colonists. ({bedCount}/{colonistCount})"
                    : "No colonists present!";
                hints.Add(CreateHint(bedHintText, "Bed"));
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
            if (!IsFishingZonePresent(map))
            {
                hints.Add(CreateHint("No fishing spots placed.", "FishingSpot"));
            }
            if (!IsStockpileZonePresent(map))
            {
                hints.Add(CreateStockpileZoneHint("No stockpile zone found.", RimWorld.StorageSettingsPreset.DefaultStockpile));
            }
            if (!IsDumpingStockpileZonePresent(map))
            {
                hints.Add(CreateStockpileZoneHint("No dumping stockpile zone found.", RimWorld.StorageSettingsPreset.DumpingStockpile));
            }
            if (!IsFoodInStorage(map))
            {
                hints.Add(CreateHint("No food in storage. Your colonists may starve soon.", null));
            }
             //Cooking Facility Detector
            if (!IsCookingFacilityPresent(map))
            {
                hints.Add(CreateHint("No cooking facilities found. Colonists need a stove or campfire to cook meals.", "Stove"));
            }
            // Table and Chair Detector
            if (!IsTableAndChairPresent(map))
            {
                hints.Add(CreateHint("No table and chair found. Colonists need a place to eat.", "Table"));
            }

            //Recreation Source Detector
            if (!IsRecreationSourcePresent(map))
            {
                hints.Add(CreateHint("No recreation sources found. Colonists need a place to relax.", "HorseshoePin"));
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
        // Helper to create a TutorialHint for stockpile/dumping stockpile zones
        private static TutorialHint CreateStockpileZoneHint(string text, RimWorld.StorageSettingsPreset preset)
        {
            var hint = new TutorialHint { Text = text, HasDesignator = true };
            hint.OnClick = () =>
            {
                RimWorld.Designator_ZoneAddStockpile designator = null;
                if (preset == RimWorld.StorageSettingsPreset.DefaultStockpile)
                {
                    designator = new RimWorld.Designator_ZoneAddStockpile_Resources();
                }
                else if (preset == RimWorld.StorageSettingsPreset.DumpingStockpile)
                {
                    designator = new RimWorld.Designator_ZoneAddStockpile_Dumping();
                }
                if (designator != null)
                {
                    Find.DesignatorManager.Select(designator);
                }
            };
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
