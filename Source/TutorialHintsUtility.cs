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
                // Match any building whose defName contains 'butcher' (case-insensitive)
                if (!string.IsNullOrEmpty(building.def.defName) && building.def.defName.ToLower().Contains("butcher"))
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
                if (zone is RimWorld.Zone_Stockpile stockpileZone)
                {
                    var settings = stockpileZone.settings;
                    if (settings != null)
                    {
                        var filter = settings.filter;
                        if (filter != null)
                        {
                            bool allowsCorpse = false;
                            bool allowsChunk = false;
                            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                            {
                                if (def.defName != null && def.defName.StartsWith("Corpse_"))
                                {
                                    if (filter.Allows(def))
                                    {
                                        allowsCorpse = true;
                                        break;
                                    }
                                }
                            }
                            if (!allowsCorpse)
                            {
                                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                                {
                                    if (def.thingCategories != null && def.thingCategories.Contains(RimWorld.ThingCategoryDefOf.Chunks))
                                    {
                                        if (filter.Allows(def))
                                        {
                                            allowsChunk = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (!allowsCorpse && !allowsChunk)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        // Checks for dumping stockpile zones (by label)
        public static bool IsDumpingStockpileZonePresent(Map map)
        {
            foreach (Zone zone in map.zoneManager.AllZones)
            {
                if (zone is RimWorld.Zone_Stockpile stockpileZone)
                {
                    var settings = stockpileZone.settings;
                    if (settings != null)
                    {
                        var filter = settings.filter;
                        if (filter != null)
                        {
                            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                            {
                                if (def.defName != null && def.defName.StartsWith("Corpse_"))
                                {
                                    if (filter.Allows(def))
                                    {
                                        return true;
                                    }
                                }
                            }
                            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                            {
                                if (def.thingCategories != null && def.thingCategories.Contains(RimWorld.ThingCategoryDefOf.Chunks))
                                {
                                    if (filter.Allows(def))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
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
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                // Allow modded and vanilla work tables for cooking by defName
                if (building is Building_WorkTable)
                {
                    string defName = building.def.defName.ToLower();
                    if (defName.Contains("stove") || defName.Contains("campfire") || defName.Contains("cook"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Checks if there is a table and chair for eating
        public static bool IsTableAndChairPresent(Map map){
            bool hasTable = false;
            bool hasChair = false;
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                // Table detection: surfaceType == SurfaceType.Eat and HasComp(typeof(RimWorld.CompGatherSpot))
                if (building.def.surfaceType == Verse.SurfaceType.Eat && building.def.HasComp(typeof(RimWorld.CompGatherSpot)))
                {
                    hasTable = true;
                }
                // Chair detection: building.isSittable
                if (building.def.building != null && building.def.building.isSittable)
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
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                // If the building provides a joyKind, it's a recreation source (mod-compatible)
                if (building.def.building != null && building.def.building.joyKind != null)
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
                hints.Add(CreateHint("No butcher table available.", "TableButcher"));
            }
            if (!IsGrowingZonePresent(map))
            {
                hints.Add(CreateHint("No growing zones found.", null, true));
            }
            // Only show fishing hint if Vanilla Fishing Expanded is loaded (designator type exists)
            var fishingDesignatorType = Type.GetType("RimWorld.Designator_ZoneAdd_Fishing, Assembly-CSharp");
            if (fishingDesignatorType != null && !IsFishingZonePresent(map))
            {
                hints.Add(CreateHint("No fishing spots placed.", null));
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
                hints.Add(CreateHint("No cooking facilities found. Colonists need a stove or campfire to cook meals.", "FueledStove"));
            }
            // Table and Chair Detector
            if (!IsTableAndChairPresent(map))
            {
                hints.Add(CreateHint("No table and chair found. Colonists need a place to eat.", "Table2x2c"));
            }

            //Recreation Source Detector
            if (!IsRecreationSourcePresent(map))
            {
                hints.Add(CreateHint("No recreation sources found. Colonists need a place to relax.", "HorseshoesPin"));
            }
            return hints;
        }

        // Helper to create a TutorialHint with designator activation
        private static TutorialHint CreateHint(string text, string defName, bool isZone = false)
        {
            var hint = new TutorialHint { Text = text };
            if (!string.IsNullOrEmpty(defName))
            {
                hint.DesignatorDef = DefDatabase<ThingDef>.GetNamed(defName);
                hint.HasDesignator = hint.DesignatorDef != null;
                if (hint.HasDesignator)
                {
                    hint.OnClick = () =>
                    {
                        Designator designator = BuildCopyCommandUtility.FindAllowedDesignator(hint.DesignatorDef, true);
                        if (designator == null)
                        {
                            // Try to find in dropdowns if not found directly
                            designator = BuildCopyCommandUtility.FindAllowedDesignatorRoot(hint.DesignatorDef, true);
                        }
                        if (designator != null && designator.Visible)
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
            else
            {
                hint.HasDesignator = true;
                string lowerText = text.ToLower();
                // Assign correct designators or UI actions for each hint type
                if (lowerText.Contains("ideology"))
                {
                    hint.OnClick = () =>
                    {
                        Messages.Message("Open the Ideology tab to select an ideology.", MessageTypeDefOf.NeutralEvent, false);
                    };
                }
                else if (lowerText.Contains("biotech") || lowerText.Contains("gene"))
                {
                    hint.OnClick = () =>
                    {
                        var designator = BuildCopyCommandUtility.FindAllowedDesignator(DefDatabase<ThingDef>.GetNamed("GeneAssembler"));
                        if (designator != null)
                        {
                            Find.DesignatorManager.Select(designator);
                        }
                        else
                        {
                            Messages.Message("No gene assembler designator found.", MessageTypeDefOf.NeutralEvent, false);
                        }
                    };
                }
                else if (lowerText.Contains("food") || lowerText.Contains("starve"))
                {
                    hint.OnClick = () =>
                    {
                        var zoneDesignator = new Designator_ZoneAdd_Growing();
                        Find.DesignatorManager.Select(zoneDesignator);
                    };
                }
                else if (lowerText.Contains("butcher") || lowerText.Contains("meat"))
                {
                    var butcherDef = DefDatabase<ThingDef>.GetNamed("TableButcher");
                    if (butcherDef != null)
                    {
                        var designator = BuildCopyCommandUtility.FindAllowedDesignator(butcherDef);
                        if (designator != null)
                        {
                            Find.DesignatorManager.Select(designator);
                        }
                        else
                        {
                            Messages.Message("No butchery designator found.", MessageTypeDefOf.NeutralEvent, false);
                        }
                    }
                }
                else if (lowerText.Contains("fishing"))
                {
                    var fishingDesignatorType = Type.GetType("RimWorld.Designator_ZoneAdd_Fishing, Assembly-CSharp");
                    if (fishingDesignatorType != null)
                    {
                        hint.OnClick = () =>
                        {
                            var zoneDesignator = Activator.CreateInstance(fishingDesignatorType) as Designator;
                            if (zoneDesignator != null)
                            {
                                Find.DesignatorManager.Select(zoneDesignator);
                            }
                        };
                    }
                    else
                    {
                        hint.OnClick = () =>
                        {
                            Messages.Message("Fishing zone designator not found. Is Vanilla Fishing Expanded loaded?", MessageTypeDefOf.NeutralEvent, false);
                        };
                    }
                }
                else if (lowerText.Contains("recreation") || lowerText.Contains("relax"))
                {
                    hint.OnClick = () =>
                    {
                        var horseshoePinDef = DefDatabase<ThingDef>.GetNamed("HorseshoesPin");
                        if (horseshoePinDef != null)
                        {
                            var designator = new Designator_Build(horseshoePinDef);
                            Find.DesignatorManager.Select(designator);
                        }
                        else
                        {
                            Messages.Message("No recreation designator found.", MessageTypeDefOf.NeutralEvent, false);
                        }
                    };
                }
                else
                {
                    hint.OnClick = () =>
                    {
                        Messages.Message(string.Format("Hint: {0}", text), MessageTypeDefOf.NeutralEvent, false);
                    };
                }
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
