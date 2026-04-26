using Eremite.Model;
using UnityEngine;

namespace StockAlert.Core.Models
{
    /// <summary>
    /// Full information about a good, used by UI, HUD, and Discovery.
    /// </summary>
    public class GoodInfo
    {
        public GoodModel Model;       // Backing game model
        public string Id;              // Internal game ID
        public string ConfigKey;       // Safe key for BepInEx config sections
        public string DisplayName;     // Localized name
        public Sprite Icon;            // Icon from GoodModel
        public int CurrentAmount;      // Updated every frame
        public int Threshold;          // User-defined threshold
        public bool Enabled;           // Whether alerts are active

        public bool IsBelowThreshold => Enabled && CurrentAmount < Threshold;
    }
}
