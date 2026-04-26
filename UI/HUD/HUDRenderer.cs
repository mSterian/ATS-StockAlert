using System.Linq;
using UnityEngine;
using StockAlert.Game.Discovery;
using StockAlert.Infrastructure.Plugin;

namespace StockAlert.UI.HUD
{
    public static class HUD
    {
        private static GameObject _hudRoot;

        public static void Initialize()
        {
            if (_hudRoot != null)
            {
                return;
            }

            Plugin.Log("HUD.Initialize()");

            _hudRoot = new GameObject("StockAlertHUD");
            Object.DontDestroyOnLoad(_hudRoot);
            _hudRoot.AddComponent<AlertHudBehaviour>();
        }

        private sealed class AlertHudBehaviour : MonoBehaviour
        {
            private GUIStyle _lineStyle;
            private GUIStyle _boxStyle;

            private void OnGUI()
            {
                var belowThreshold = Discovery.Goods
                    .Where(g => g.IsBelowThreshold)
                    .OrderBy(g => g.CurrentAmount)
                    .ToList();

                if (belowThreshold.Count == 0)
                {
                    return;
                }

                EnsureStyles();

                var height = Mathf.Min(36 + belowThreshold.Count * 26, 240);
                var rect = new Rect(Screen.width - 336f, Screen.height - height - 16f, 320f, height);
                GUI.color = new Color(1f, 0.9f, 0.9f, 0.96f);
                GUILayout.BeginArea(rect, _boxStyle);
                GUI.color = Color.white;

                GUILayout.Label("Low Stock Alerts");
                GUILayout.Space(4f);

                foreach (var good in belowThreshold.Take(8))
                {
                    DrawAlertLine(good);
                }

                if (belowThreshold.Count > 8)
                {
                    GUILayout.Label($"+ {belowThreshold.Count - 8} more", _lineStyle);
                }

                GUILayout.EndArea();
            }

            private void EnsureStyles()
            {
                _lineStyle ??= new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13
                };

                _boxStyle ??= new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 8, 8)
                };
            }

            private void DrawAlertLine(Core.Models.GoodInfo good)
            {
                GUILayout.BeginHorizontal();

                if (good.Icon != null && good.Icon.texture != null)
                {
                    var rect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f), GUILayout.Height(18f));
                    var uv = new Rect(
                        good.Icon.textureRect.x / good.Icon.texture.width,
                        good.Icon.textureRect.y / good.Icon.texture.height,
                        good.Icon.textureRect.width / good.Icon.texture.width,
                        good.Icon.textureRect.height / good.Icon.texture.height
                    );
                    GUI.DrawTextureWithTexCoords(rect, good.Icon.texture, uv);
                }
                else
                {
                    GUILayout.Label(good.DisplayName, GUILayout.Width(130f), GUILayout.Height(18f));
                }

                GUILayout.Label($"{good.DisplayName}: {good.CurrentAmount}/{good.Threshold}", _lineStyle);
                GUILayout.EndHorizontal();
            }
        }
    }
}
