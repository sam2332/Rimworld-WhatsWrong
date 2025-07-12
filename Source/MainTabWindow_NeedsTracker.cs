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
            // Calculate total content height
            float totalHeight = 0f;
            float hintHeight = 24f;
            Map map = Find.CurrentMap;
            var hints = TutorialHintsUtility.GetMissingHints(map);
            if (hints != null && hints.Count > 0)
            {
                totalHeight += hints.Count * (hintHeight + 2f) + 6f;
            }
            totalHeight += 50f; // Title
            totalHeight += 30f + 10f; // Toggle buttons and spacing
            // Needs content
            if (cachedUnmetNeeds == null || cachedUnmetNeeds.Count == 0)
            {
                totalHeight += 80f; // No issues found message
            }
            else if (currentDisplayMode == DisplayMode.ByColonist)
            {
                if (cachedNeedsByColonist != null)
                {
                    foreach (var tuple in cachedNeedsByColonist)
                    {
                        totalHeight += 40f; // Colonist header
                        totalHeight += tuple.Value.Count * 25f;
                        totalHeight += 10f;
                    }
                }
            }
            else // ByNeed
            {
                var needsByType = cachedUnmetNeeds.GroupBy(n => n.needLabel).ToList();
                foreach (var needGroup in needsByType)
                {
                    totalHeight += 40f; // Need type header
                    totalHeight += needGroup.Count() * 25f;
                    totalHeight += 10f;
                }
            }

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float curY = 0f;
            // --- Tutorial Hints Section ---
            if (hints != null && hints.Count > 0)
            {
                for (int i = 0; i < hints.Count; i++)
                {
                    var hint = hints[i];
                    Rect hintRect = new Rect(viewRect.x, curY, viewRect.width, hintHeight);
                    Color hintColor = hint.HasDesignator ? new Color(0.8f, 0.95f, 1f, 0.7f) : new Color(1f, 1f, 0.8f, 0.7f);
                    Widgets.DrawBoxSolid(hintRect, hintColor);
                    Color prevColor = GUI.color;
                    GUI.color = Color.black;
                    if (hint.HasDesignator && Widgets.ButtonInvisible(hintRect))
                    {
                        hint.OnClick?.Invoke();
                    }
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(hintRect, hint.Text);
                    GUI.color = prevColor;
                    Text.Anchor = TextAnchor.UpperLeft;
                    curY += hintHeight + 2f;
                }
                curY += 6f;
            }

            // Title
            Rect titleRect = new Rect(viewRect.x, curY, viewRect.width, 50f);
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
            curY = titleRect.yMax;

            // Display mode toggle buttons
            Rect toggleRect = new Rect(viewRect.x, curY + 5f, viewRect.width, 30f);
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
            curY = toggleRect.yMax + 10f;

            // Main content area
            Rect contentRect = new Rect(viewRect.x, curY, viewRect.width, viewRect.height - curY);
            if (cachedUnmetNeeds == null || cachedUnmetNeeds.Count == 0)
            {
                DrawNoIssuesFound(contentRect);
            }
            else
            {
                if (currentDisplayMode == DisplayMode.ByColonist)
                {
                    DrawNeedsByColonist(contentRect, viewRect.x, ref curY);
                }
                else
                {
                    DrawNeedsByType(contentRect, viewRect.x, ref curY);
                }
            }

            Widgets.EndScrollView();
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

        private void DrawNeedsByColonist(Rect rect, float xOffset, ref float curY)
        {
            if (cachedNeedsByColonist == null || cachedNeedsByColonist.Count == 0)
                return;

            // curY is passed in and updated
            List<(Pawn colonist, List<NeedInfo> needs)> orderedColonists = cachedNeedsByColonist
                .OrderBy(x => x.Value.Any(n => n.isUrgent) ? 0 : 1)
                .Select(x => (x.Key, x.Value)).ToList();

            // First, calculate total height needed
            float totalHeight = 0f;
            foreach (var tuple in orderedColonists)
            {
                totalHeight += 40f; // Colonist header
                totalHeight += tuple.needs.Count * 25f; // Needs
                totalHeight += 10f; // Spacing
            }

            // All scrolling handled in DoWindowContents
            foreach (var tuple in orderedColonists)
            {
                Pawn colonist = tuple.colonist;
                List<NeedInfo> needs = tuple.needs;

                // Colonist header
                Rect colonistRect = new Rect(xOffset, curY, rect.width, 35f);
                Color bgColor = needs.Any(n => n.isUrgent) ? new Color(1f, 0.5f, 0.5f, 0.3f) : new Color(1f, 1f, 0.5f, 0.2f);
                Widgets.DrawBoxSolid(colonistRect, bgColor);
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
                    Rect needRect = new Rect(xOffset + 20f, curY, rect.width - 40f, 20f);
                    if (Widgets.ButtonInvisible(needRect))
                    {
                        ColonistNeedsUtility.JumpToPawn(colonist);
                    }
                    Color needColor = needInfo.isUrgent ? Color.red : Color.yellow;
                    string needText = $"{needInfo.needLabel}: {needInfo.statusText} ({(needInfo.needLevel * 100f):F0}%)";
                    GUI.color = needColor;
                    Widgets.Label(needRect, needText);
                    GUI.color = Color.white;
                    curY += 25f;
                }
                curY += 10f; // Spacing between colonists
            }
        }

        private void DrawNeedsByType(Rect rect, float xOffset, ref float curY)
        {
            if (cachedUnmetNeeds == null || cachedUnmetNeeds.Count == 0)
                return;

            var needsByType = cachedUnmetNeeds.GroupBy(n => n.needLabel)
                .OrderBy(g => g.Any(n => n.isUrgent) ? 0 : 1)
                .ThenByDescending(g => g.Count())
                .ToList();

            // curY is passed in and updated
            foreach (var needGroup in needsByType)
            {
                string needType = needGroup.Key;
                var needsOfType = needGroup.ToList();
                Rect headerRect = new Rect(xOffset, curY, rect.width, 35f);
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

                foreach (NeedInfo needInfo in needsOfType.OrderBy(n => n.isUrgent ? 0 : 1).ThenBy(n => n.needLevel))
                {
                    Rect colonistRect = new Rect(xOffset + 20f, curY, rect.width - 40f, 20f);
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
        }
    }
}
