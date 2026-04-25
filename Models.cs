using UnityEngine;

namespace StockAlert
{
    public class GoodInfo
    {
        public string Id;
        public string DisplayName;
        public Texture2D Icon;
        public int CurrentAmount;
        public int Threshold;
        public bool Enabled;

        public bool IsBelowThreshold =>
            Enabled && Threshold > 0 && CurrentAmount < Threshold;
    }
}
