using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WhatsWrong
{
    /// <summary>
    /// Utility class to gather and filter colonist needs
    /// </summary>
    public static class NeedsUtility
    {
        /// <summary>
        /// Gets all colonists with unmet needs
        /// </summary>
        public static List<NeedInfo> GetAllUnmetNeeds()
        {
            List<NeedInfo> unmetNeeds = new List<NeedInfo>();
            
            // Get all free colonists from all maps
            foreach (Pawn colonist in PawnsFinder.AllMaps_FreeColonists)
            {
                if (colonist == null || colonist.needs == null || !colonist.Spawned)
                    continue;

                // Check each need
                foreach (Need need in colonist.needs.AllNeeds)
                {
                    if (need == null)
                        continue;

                    NeedInfo needInfo = new NeedInfo(colonist, need);
                    
                    // Only include needs that are unmet
                    if (needInfo.IsNeedUnmet())
                    {
                        unmetNeeds.Add(needInfo);
                    }
                }
            }

            // Sort by urgency first, then by severity
            return unmetNeeds.OrderByDescending(x => x.isUrgent)
                            .ThenBy(x => x.needLevel)
                            .ToList();
        }

        /// <summary>
        /// Gets colonists with urgent needs (below critical thresholds)
        /// </summary>
        public static List<NeedInfo> GetUrgentNeeds()
        {
            return GetAllUnmetNeeds().Where(x => x.isUrgent).ToList();
        }

        /// <summary>
        /// Gets needs grouped by type
        /// </summary>
        public static Dictionary<string, List<NeedInfo>> GetNeedsGroupedByType()
        {
            var allNeeds = GetAllUnmetNeeds();
            return allNeeds.GroupBy(x => x.needLabel)
                          .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Gets a summary count of issues
        /// </summary>
        public static (int total, int urgent) GetNeedsCounts()
        {
            var allNeeds = GetAllUnmetNeeds();
            int total = allNeeds.Count;
            int urgent = allNeeds.Count(x => x.isUrgent);
            return (total, urgent);
        }

        /// <summary>
        /// Checks if a colonist has any unmet needs
        /// </summary>
        public static bool HasUnmetNeeds(Pawn pawn)
        {
            if (pawn?.needs?.AllNeeds == null)
                return false;

            foreach (Need need in pawn.needs.AllNeeds)
            {
                if (need == null)
                    continue;

                NeedInfo needInfo = new NeedInfo(pawn, need);
                if (needInfo.IsNeedUnmet())
                    return true;
            }

            return false;
        }
    }
}
