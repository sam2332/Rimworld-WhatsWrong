using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using WhatsWrong;

namespace WhatsWrong
{
    public class MainTabWindow_NeedsTracker : MainTabWindow
    {
        private Vector2 scrollPosition;
        private List<NeedInfo> cachedUnmetNeeds;
        private Dictionary<Pawn, List<NeedInfo>> cachedNeedsByColonist;
        private int lastUpdateTick;
        private const int UPDATE_INTERVAL = 60; // Update every 60 ticks (1 second at normal speed)
        
        private enum DisplayMode
        {
            ByNeed,
            ByColonist
        }
        
        private DisplayMode currentDisplayMode = DisplayMode.ByColonist;

        public override Vector2 RequestedTabSize => new Vector2(900f, 600f);

        public override void PreOpen()
        {
            base.PreOpen();
            UpdateCachedData();
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Update data periodically
            if (Find.TickManager.TicksGame - lastUpdateTick > UPDATE_INTERVAL)
            {
                UpdateCachedData();
            }

            Rect rect = inRect.ContractedBy(10f);
            // --- Tutorial Hints Section ---
            Map map = Find.CurrentMap;
            var hints = TutorialHintsUtility.GetMissingHints(map);
            if (hints != null && hints.Count > 0)
            {
                float hintY = rect.y;
                float hintHeight = 24f;
                for (int i = 0; i < hints.Count; i++)
                {
                    var hint = hints[i];
                    Rect hintRect = new Rect(rect.x, hintY, rect.width, hintHeight);
                    Color hintColor = hint.HasDesignator ? new Color(0.8f, 0.95f, 1f, 0.7f) : new Color(1f, 1f, 0.8f, 0.7f);
                    Widgets.DrawBoxSolid(hintRect, hintColor);
                    if (hint.HasDesignator && Widgets.ButtonInvisible(hintRect))
                    {
                        hint.OnClick?.Invoke();
                    }
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(hintRect, hint.Text);
                    Text.Anchor = TextAnchor.UpperLeft;
                    hintY += hintHeight + 2f;
                }
                rect.yMin += (hintHeight + 2f) * hints.Count + 6f;
            }
            
            // Title
            Rect titleRect = new Rect(rect.x, rect.y, rect.width, 50f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            
            string title = "What's Wrong - Colonist Needs Tracker";
            if (cachedUnmetNeeds?.Count > 0)
            {
                int urgentCount = cachedUnmetNeeds.Count(n => n.isUrgent);
                if (urgentCount > 0)
                {
                    title += $" ({urgentCount} urgent, {cachedUnmetNeeds.Count} total)";
                }
                else
                {
                    title += $" ({cachedUnmetNeeds.Count} unmet needs)";
                }
            }
            
            Widgets.Label(titleRect, title);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Display mode toggle buttons
            Rect toggleRect = new Rect(rect.x, titleRect.yMax + 5f, rect.width, 30f);
            Rect byColonistRect = new Rect(toggleRect.x, toggleRect.y, 120f, 30f);
            Rect byNeedRect = new Rect(byColonistRect.xMax + 10f, toggleRect.y, 120f, 30f);
            
            if (Widgets.ButtonText(byColonistRect, "By Colonist", currentDisplayMode == DisplayMode.ByColonist))
            {
                currentDisplayMode = DisplayMode.ByColonist;
            }
            if (Widgets.ButtonText(byNeedRect, "By Need Type", currentDisplayMode == DisplayMode.ByNeed))
            {
                currentDisplayMode = DisplayMode.ByNeed;
            }

            // Main content area
            Rect contentRect = new Rect(rect.x, toggleRect.yMax + 10f, rect.width, rect.height - toggleRect.yMax - 20f);
            
            if (cachedUnmetNeeds == null || cachedUnmetNeeds.Count == 0)
            {
                DrawNoIssuesFound(contentRect);
            }
            else
            {
                if (currentDisplayMode == DisplayMode.ByColonist)
                {
                    DrawNeedsByColonist(contentRect);
                }
                else
                {
                    DrawNeedsByType(contentRect);
                }
            }
        }

        private void UpdateCachedData()
        {
            cachedUnmetNeeds = ColonistNeedsUtility.GetAllUnmetNeeds();
            cachedNeedsByColonist = ColonistNeedsUtility.GetUnmetNeedsByColonist();
            lastUpdateTick = Find.TickManager.TicksGame;
        }

        private void DrawNoIssuesFound(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "No urgent needs found!\nAll colonists are doing well.");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawNeedsByColonist(Rect rect)
        {
            if (cachedNeedsByColonist == null || cachedNeedsByColonist.Count == 0)
                return;

            // Calculate total height needed
            float totalHeight = 0f;
            foreach (var kvp in cachedNeedsByColonist)
            {
                totalHeight += 40f; // Colonist header
                totalHeight += kvp.Value.Count * 25f; // Needs
                totalHeight += 10f; // Spacing
            }

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float curY = 0f;
            
            foreach (var kvp in cachedNeedsByColonist.OrderBy(x => x.Value.Any(n => n.isUrgent) ? 0 : 1))
            {
                Pawn colonist = kvp.Key;
                List<NeedInfo> needs = kvp.Value;
                
                // Colonist header
                Rect colonistRect = new Rect(0f, curY, viewRect.width, 35f);
                
                // Determine background color based on urgency
                Color bgColor = needs.Any(n => n.isUrgent) ? new Color(1f, 0.5f, 0.5f, 0.3f) : new Color(1f, 1f, 0.5f, 0.2f);
                Widgets.DrawBoxSolid(colonistRect, bgColor);
                
                // Colonist name and button
                Text.Font = GameFont.Medium;
                Rect nameRect = new Rect(colonistRect.x + 10f, colonistRect.y + 5f, colonistRect.width - 20f, 25f);
                
                string colonistLabel = colonist.Name?.ToStringShort ?? colonist.LabelShort;
                if (needs.Any(n => n.isUrgent))
                {
                    colonistLabel = "⚠ " + colonistLabel + " (URGENT)";
                }
                
                if (Widgets.ButtonText(nameRect, colonistLabel, false))
                {
                    ColonistNeedsUtility.JumpToPawn(colonist);
                }
                Text.Font = GameFont.Small;
                
                curY += 40f;
                
                // Individual needs
                foreach (NeedInfo needInfo in needs.OrderBy(n => n.isUrgent ? 0 : 1).ThenBy(n => n.needLevel))
                {
                    Rect needRect = new Rect(20f, curY, viewRect.width - 40f, 20f);
                    
                    if (Widgets.ButtonInvisible(needRect))
                    {
                        ColonistNeedsUtility.JumpToPawn(colonist);
                    }
                    
                    // Need status color
                    Color needColor = needInfo.isUrgent ? Color.red : Color.yellow;
                    
                    string needText = $"{needInfo.needLabel}: {needInfo.statusText} ({(needInfo.needLevel * 100f):F0}%)";
                    
                    GUI.color = needColor;
                    Widgets.Label(needRect, needText);
                    GUI.color = Color.white;
                    
                    curY += 25f;
                }
                
                curY += 10f; // Spacing between colonists
            }

            Widgets.EndScrollView();
        }

        private void DrawNeedsByType(Rect rect)
        {
            if (cachedUnmetNeeds == null || cachedUnmetNeeds.Count == 0)
                return;

            // Group needs by type
            var needsByType = cachedUnmetNeeds.GroupBy(n => n.needLabel)
                                              .OrderBy(g => g.Any(n => n.isUrgent) ? 0 : 1)
                                              .ThenByDescending(g => g.Count());

            float totalHeight = needsByType.Sum(g => 40f + g.Count() * 25f + 10f);
            
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float curY = 0f;
            
            foreach (var needGroup in needsByType)
            {
                string needType = needGroup.Key;
                var needsOfType = needGroup.ToList();
                
                // Need type header
                Rect headerRect = new Rect(0f, curY, viewRect.width, 35f);
                
                Color bgColor = needsOfType.Any(n => n.isUrgent) ? new Color(1f, 0.5f, 0.5f, 0.3f) : new Color(1f, 1f, 0.5f, 0.2f);
                Widgets.DrawBoxSolid(headerRect, bgColor);
                
                Text.Font = GameFont.Medium;
                Rect labelRect = new Rect(headerRect.x + 10f, headerRect.y + 5f, headerRect.width - 20f, 25f);
                
                string headerLabel = $"{needType} ({needsOfType.Count} colonists)";
                if (needsOfType.Any(n => n.isUrgent))
                {
                    headerLabel = "⚠ " + headerLabel;
                }
                
                Widgets.Label(labelRect, headerLabel);
                Text.Font = GameFont.Small;
                
                curY += 40f;
                
                // Individual colonists with this need
                foreach (NeedInfo needInfo in needsOfType.OrderBy(n => n.isUrgent ? 0 : 1).ThenBy(n => n.needLevel))
                {
                    Rect colonistRect = new Rect(20f, curY, viewRect.width - 40f, 20f);
                    
                    if (Widgets.ButtonInvisible(colonistRect))
                    {
                        ColonistNeedsUtility.JumpToPawn(needInfo.pawn);
                    }
                    
                    Color needColor = needInfo.isUrgent ? Color.red : Color.yellow;
                    
                    string colonistName = needInfo.pawn.Name?.ToStringShort ?? needInfo.pawn.LabelShort;
                    string needText = $"{colonistName}: {needInfo.statusText} ({(needInfo.needLevel * 100f):F0}%)";
                    
                    GUI.color = needColor;
                    Widgets.Label(colonistRect, needText);
                    GUI.color = Color.white;
                    
                    curY += 25f;
                }
                
                curY += 10f; // Spacing between need types
            }

            Widgets.EndScrollView();
        }
    }
}
