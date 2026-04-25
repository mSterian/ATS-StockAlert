using System;
using System.Linq;
using UnityEngine;

namespace StockAlert
{
    internal static class Utils
    {
        public static string CleanIdForDisplay(string id)
        {
            if (string.IsNullOrEmpty(id)) return "<unknown id>";
            int printable = id.Count(c => c >= 32 && c < 127);
            if (printable < Math.Max(1, id.Length / 2))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(id);
                var hex = BitConverter.ToString(bytes.Take(6).ToArray()).Replace("-", "");
                var tail = new string(id.Where(c => c >= 32 && c < 127).Take(8).ToArray());
                if (string.IsNullOrEmpty(tail)) tail = "...";
                return $"id_{hex} ({tail})";
            }
            return id;
        }

        public static bool IsKeyPressed(KeyCode key)
        {
            try
            {
                return Input.GetKeyDown(key);
            }
            catch
            {
                return false;
            }
        }
    }
}
