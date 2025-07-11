using RimWorld;
using Verse;

namespace WhatsWrong
{
    /// <summary>
    /// Data structure to hold information about a colonist's unmet need
    /// </summary>
    public class NeedInfo
    {
        public Pawn pawn;
        public Need need;
        public string needLabel;
        public float needLevel;
        public float needThreshold;
        public string statusText;
        public bool isUrgent;

        public NeedInfo(Pawn pawn, Need need)
        {
            this.pawn = pawn;
            this.need = need;
            this.needLabel = need.LabelCap;
            this.needLevel = need.CurLevelPercentage;
            
            // Determine thresholds and urgency based on need type
            SetThresholdAndUrgency();
            
            // Generate status text
            GenerateStatusText();
        }

        private void SetThresholdAndUrgency()
        {
            if (need is Need_Food)
            {
                needThreshold = 0.3f; // Hungry threshold
                isUrgent = needLevel < 0.1f; // Starving
            }
            else if (need is Need_Rest)
            {
                needThreshold = 0.25f; // Tired threshold
                isUrgent = needLevel < 0.1f; // Exhausted
            }
            else if (need is Need_Joy)
            {
                needThreshold = 0.3f; // Low joy threshold
                isUrgent = needLevel < 0.1f; // Very low joy
            }
            else if (need is Need_Mood)
            {
                needThreshold = 0.4f; // Unhappy threshold
                isUrgent = needLevel < 0.2f; // Very unhappy
            }
            else if (need is Need_Beauty)
            {
                needThreshold = 0.3f;
                isUrgent = needLevel < 0.1f;
            }
            else if (need is Need_Comfort)
            {
                needThreshold = 0.3f;
                isUrgent = needLevel < 0.1f;
            }
            else if (need is Need_Outdoors)
            {
                needThreshold = 0.3f;
                isUrgent = needLevel < 0.1f;
            }
            else if (need is Need_RoomSize)
            {
                needThreshold = 0.3f;
                isUrgent = needLevel < 0.1f;
            }
            else
            {
                // Default for other needs
                needThreshold = 0.3f;
                isUrgent = needLevel < 0.15f;
            }
        }

        private void GenerateStatusText()
        {
            if (need is Need_Food)
            {
                if (needLevel < 0.1f)
                    statusText = "Starving";
                else if (needLevel < 0.3f)
                    statusText = "Hungry";
                else
                    statusText = "Fed";
            }
            else if (need is Need_Rest)
            {
                if (needLevel < 0.1f)
                    statusText = "Exhausted";
                else if (needLevel < 0.25f)
                    statusText = "Tired";
                else
                    statusText = "Rested";
            }
            else if (need is Need_Joy)
            {
                if (needLevel < 0.1f)
                    statusText = "Very low joy";
                else if (needLevel < 0.3f)
                    statusText = "Low joy";
                else
                    statusText = "Content";
            }
            else if (need is Need_Mood)
            {
                if (needLevel < 0.2f)
                    statusText = "Very unhappy";
                else if (needLevel < 0.4f)
                    statusText = "Unhappy";
                else
                    statusText = "Happy";
            }
            else
            {
                // Generic status for other needs
                if (isUrgent)
                    statusText = "Very low";
                else if (needLevel < needThreshold)
                    statusText = "Low";
                else
                    statusText = "Satisfied";
            }
        }

        public bool IsNeedUnmet()
        {
            return needLevel < needThreshold;
        }
    }
}
