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
            private const float IconSize = 22.5f;
            private const float RowHeight = 24f;
            private const float BoxMargin = 16f;
            private const float BoxPaddingX = 20f;
            private const float HeaderGap = 4f;

            private GUIStyle _lineStyle;
            private GUIStyle _headerStyle;
            private GUIStyle _boxStyle;
            private Vector2 _scrollPosition;

            private void OnGUI()
            {
                var belowThreshold = Discovery.Goods
                    .Where(g => g.IsBelowThreshold)
                    .OrderBy(g => g.Threshold <= 0 ? 1f : (float)g.CurrentAmount / g.Threshold)
                    .ThenBy(g => g.CurrentAmount)
                    .ToList();

                if (belowThreshold.Count == 0)
                {
                    return;
                }

                EnsureStyles();

                var contentWidth = GetContentWidth(belowThreshold);
                var boxWidth = Mathf.Min(contentWidth + BoxPaddingX, Screen.width - BoxMargin * 2f);
                var contentHeight = belowThreshold.Count * RowHeight;
                var maxVisibleContentHeight = Screen.height - 120f;
                var needsScroll = contentHeight > maxVisibleContentHeight;
                var visibleContentHeight = needsScroll ? maxVisibleContentHeight : contentHeight;
                var headerHeight = _headerStyle.CalcHeight(new GUIContent("Low Stock Alerts"), boxWidth);
                var verticalPadding = _boxStyle.padding.top + _boxStyle.padding.bottom;
                var visibleHeight = headerHeight + HeaderGap + visibleContentHeight + verticalPadding;
                var rect = new Rect(Screen.width - boxWidth - BoxMargin, Screen.height - visibleHeight - BoxMargin, boxWidth, visibleHeight);
                GUI.color = new Color(1f, 0.9f, 0.9f, 0.96f);
                GUILayout.BeginArea(rect, _boxStyle);
                GUI.color = Color.white;

                GUILayout.Label("Low Stock Alerts", _headerStyle);
                GUILayout.Space(4f);

                if (needsScroll)
                {
                    _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true, GUILayout.Height(visibleContentHeight));
                    foreach (var good in belowThreshold)
                    {
                        DrawAlertLine(good);
                    }
                    GUILayout.EndScrollView();
                }
                else
                {
                    foreach (var good in belowThreshold)
                    {
                        DrawAlertLine(good);
                    }
                }

                GUILayout.EndArea();
            }

            private void EnsureStyles()
            {
                _lineStyle ??= new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16
                };

                _headerStyle ??= new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16
                };

                _boxStyle ??= new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 8, 8)
                };
            }

            private float GetContentWidth(System.Collections.Generic.IReadOnlyList<Core.Models.GoodInfo> goods)
            {
                var headerWidth = _headerStyle.CalcSize(new GUIContent("Low Stock Alerts")).x;
                var lineWidth = headerWidth;

                foreach (var good in goods)
                {
                    var text = $"{good.DisplayName}: {good.CurrentAmount}/{good.Threshold}";
                    var width = _lineStyle.CalcSize(new GUIContent(text)).x + IconSize + 8f;
                    if (width > lineWidth)
                    {
                        lineWidth = width;
                    }
                }

                return Mathf.Max(180f, lineWidth);
            }

            private void DrawAlertLine(Core.Models.GoodInfo good)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(RowHeight));

                if (good.Icon != null && good.Icon.texture != null)
                {
                    var rect = GUILayoutUtility.GetRect(IconSize, IconSize, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
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
                    GUILayout.Label(string.Empty, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
                }

                GUILayout.Label($"{good.DisplayName}: {good.CurrentAmount}/{good.Threshold}", _lineStyle);
                GUILayout.EndHorizontal();
            }
        }
    }
}
