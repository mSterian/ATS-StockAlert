using UnityEngine;
using System.Linq;

namespace StockAlert
{
    public class HUD : MonoBehaviour
    {
        private GUIStyle _text;

        private void Awake()
        {
            _text = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };
        }

        private void Update()
        {
            Discovery.UpdateStock();
        }

        private void OnGUI()
        {
            var goods = Discovery.GetCriticalGoods().ToList();
            if (goods.Count == 0) return;

            float iconSize = 24f;
            float line = 26f;
            float pad = 6f;
            float width = 220f;

            float totalHeight = goods.Count * line + pad * 2;
            float x = Screen.width - width - pad;
            float y = Screen.height - totalHeight - pad;

            float cy = y + pad;

            foreach (var g in goods)
            {
                if (g.Icon != null)
                    GUI.DrawTexture(new Rect(x + width - iconSize, cy, iconSize, iconSize), g.Icon);

                GUI.Label(
                    new Rect(x, cy, width - iconSize - pad, line),
                    $"{g.DisplayName} {g.CurrentAmount}/{g.Threshold}",
                    _text
                );

                cy += line;
            }
        }
    }
}
