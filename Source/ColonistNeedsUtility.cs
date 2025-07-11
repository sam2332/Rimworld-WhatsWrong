using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WhatsWrong
{
    /// <summary>
    /// Utility class for gathering and analyzing colonist needs
    /// </summary>
    public static class ColonistNeedsUtility
    {
        /// <summary>
        /// Gets all colonists with unmet needs across all maps
        /// </summary>
        public static List<NeedInfo> GetAllUnmetNeeds()
        {
            List<NeedInfo> unmetNeeds = new List<NeedInfo>();
            
            // Get all free colonists across all maps
            foreach (Pawn colonist in PawnsFinder.AllMaps_FreeColonists)
            {
                if (colonist.needs?.AllNeeds == null)
                    continue;

                // Check each need for this colonist
                foreach (Need need in colonist.needs.AllNeeds)
                {
                    if (need == null || !ShouldTrackNeed(need))
                        continue;

                    NeedInfo needInfo = new NeedInfo(colonist, need);
                    if (needInfo.IsNeedUnmet())
                    {
                        unmetNeeds.Add(needInfo);
                    }
                }
            }

            // Sort by urgency first, then by need level (lowest first)
            unmetNeeds = unmetNeeds.OrderBy(n => n.isUrgent ? 0 : 1)
                                   .ThenBy(n => n.needLevel)
                                   .ToList();

            return unmetNeeds;
        }

        /// <summary>
        /// Gets all colonists grouped by colonist with their unmet needs
        /// </summary>
        public static Dictionary<Pawn, List<NeedInfo>> GetUnmetNeedsByColonist()
        {
            Dictionary<Pawn, List<NeedInfo>> needsByColonist = new Dictionary<Pawn, List<NeedInfo>>();
            
            List<NeedInfo> allUnmetNeeds = GetAllUnmetNeeds();
            
            foreach (NeedInfo needInfo in allUnmetNeeds)
            {
                if (!needsByColonist.ContainsKey(needInfo.pawn))
                {
                    needsByColonist[needInfo.pawn] = new List<NeedInfo>();
                }
                needsByColonist[needInfo.pawn].Add(needInfo);
            }

            return needsByColonist;
        }

        /// <summary>
        /// Determines if we should track this specific need type
        /// </summary>
        private static bool ShouldTrackNeed(Need need)
        {
            // Track most important needs that players need to monitor
            return need is Need_Food ||
                   need is Need_Rest ||
                   need is Need_Joy ||
                   need is Need_Mood ||
                   need is Need_Beauty ||
                   need is Need_Comfort ||
                   need is Need_Outdoors ||
                   need is Need_RoomSize ||
                   need is Need_Chemical;
        }

        /// <summary>
        /// Jumps camera to a specific pawn and selects them
        /// </summary>
        public static void JumpToPawn(Pawn pawn)
        {
            if (pawn?.Spawned == true && pawn.Map != null)
            {
                CameraJumper.TryJumpAndSelect(pawn);
            }
        }

        /// <summary>
        /// Gets a count of colonists with urgent needs
        /// </summary>
        public static int GetUrgentNeedsCount()
        {
            return GetAllUnmetNeeds().Count(n => n.isUrgent);
        }

        /// <summary>
        /// Gets a count of total unmet needs
        /// </summary>
        public static int GetTotalUnmetNeedsCount()
        {
            return GetAllUnmetNeeds().Count;
        }
    }
}
