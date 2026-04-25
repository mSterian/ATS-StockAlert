using UnityEngine;
using System.Linq;

namespace StockAlert
{
    public class UI : MonoBehaviour
    {
        private Rect _rect = new Rect(200, 100, 500, 600);
        private string _search = "";
        private Vector2 _scroll;

        private void OnGUI()
        {
            _rect = GUILayout.Window(GetInstanceID(), _rect, Draw, "StockAlert Configuration");
        }

        private void Draw(int id)
        {
            GUILayout.Label("Configure thresholds and visibility.", GUILayout.Height(20));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(60));
            _search = GUILayout.TextField(_search);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            _scroll = GUILayout.BeginScrollView(_scroll);

            var goods = Discovery.Goods;

            if (!string.IsNullOrEmpty(_search))
            {
                string s = _search.ToLowerInvariant();
                goods = goods.Where(g =>
                    g.DisplayName.ToLowerInvariant().Contains(s) ||
                    g.Id.ToLowerInvariant().Contains(s)
                ).ToList();
            }

            foreach (var g in goods)
                DrawGood(g);

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            if (GUILayout.Button("Close", GUILayout.Width(80)))
                enabled = false;

            GUI.DragWindow();
        }

        private void DrawGood(GoodInfo g)
        {
            GUILayout.BeginHorizontal();

            bool newEnabled = GUILayout.Toggle(g.Enabled, "", GUILayout.Width(20));
            if (newEnabled != g.Enabled)
            {
                g.Enabled = newEnabled;
                ConfigManager.UpdateGoodConfig(g);
            }

            GUILayout.Label(g.DisplayName, GUILayout.Width(200));

            GUILayout.Label("Threshold:", GUILayout.Width(70));

            string t = GUILayout.TextField(g.Threshold.ToString(), GUILayout.Width(60));
            if (int.TryParse(t, out int newT) && newT >= 0 && newT != g.Threshold)
            {
                g.Threshold = newT;
                ConfigManager.UpdateGoodConfig(g);
            }

            GUILayout.EndHorizontal();
        }
    }
}
