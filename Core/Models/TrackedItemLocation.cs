using UnityEngine;

namespace StockAlert.Core.Models
{
    internal sealed class TrackedItemLocation
    {
        public string Key { get; set; }

        public Object Source { get; set; }

        public Vector3 FallbackPosition { get; set; }

        public int Amount { get; set; }
    }
}
