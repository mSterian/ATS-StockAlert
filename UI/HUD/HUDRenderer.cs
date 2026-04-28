using System.Linq;
using System.Collections.Generic;
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
            private const int MaxRowsPerColumn = 15;
            private const float ColumnGap = 12f;

            private GUIStyle _lineStyle;
            private GUIStyle _boxStyle;
            private readonly HashSet<string> _activeAlertIds = new HashSet<string>();
            private readonly Dictionary<string, long> _alertOrder = new Dictionary<string, long>();
            private long _nextAlertOrder;

            private void OnGUI()
            {
                var belowThreshold = Discovery.Goods
                    .Where(g => g.IsBelowThreshold)
                    .ToList();

                if (belowThreshold.Count == 0)
                {
                    _activeAlertIds.Clear();
                    _alertOrder.Clear();
                    return;
                }

                RefreshAlertOrdering(belowThreshold);

                belowThreshold = belowThreshold
                    .OrderByDescending(g => _alertOrder.TryGetValue(g.Id, out var order) ? order : 0L)
                    .ThenBy(g => g.DisplayName)
                    .ToList();

                EnsureStyles();

                var leftColumnCount = Mathf.Min(MaxRowsPerColumn, belowThreshold.Count);
                var rightColumnCount = belowThreshold.Count > MaxRowsPerColumn ? belowThreshold.Count - MaxRowsPerColumn : 0;
                var leftColumn = belowThreshold.Take(leftColumnCount).ToList();
                var rightColumn = rightColumnCount > 0
                    ? belowThreshold.Skip(MaxRowsPerColumn).Take(MaxRowsPerColumn).ToList()
                    : new List<Core.Models.GoodInfo>();

                var leftWidth = GetColumnWidth(leftColumn);
                var rightWidth = rightColumnCount > 0 ? GetColumnWidth(rightColumn) : 0f;
                var contentWidth = leftWidth + (rightColumnCount > 0 ? ColumnGap + rightWidth : 0f);
                var boxWidth = Mathf.Min(contentWidth + BoxPaddingX, Screen.width - BoxMargin * 2f);
                var visibleRowCount = Mathf.Max(leftColumnCount, rightColumnCount);
                var visibleContentHeight = visibleRowCount * RowHeight;
                var verticalPadding = _boxStyle.padding.top + _boxStyle.padding.bottom;
                var visibleHeight = visibleContentHeight + verticalPadding;
                var rect = new Rect(Screen.width - boxWidth - BoxMargin, Screen.height - visibleHeight - BoxMargin, boxWidth, visibleHeight);
                GUI.color = new Color(1f, 0.9f, 0.9f, 0.96f);
                GUILayout.BeginArea(rect, _boxStyle);
                GUI.color = Color.white;

                if (rightColumnCount > 0)
                {
                    GUILayout.BeginHorizontal();
                    DrawColumn(leftColumn, leftWidth);
                    GUILayout.Space(ColumnGap);
                    DrawColumn(rightColumn, rightWidth);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    DrawColumn(leftColumn, leftWidth);
                }

                GUILayout.EndArea();
            }

            private void EnsureStyles()
            {
                _lineStyle ??= new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16
                };

                _boxStyle ??= new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 8, 8)
                };
            }

            private void DrawColumn(IReadOnlyList<Core.Models.GoodInfo> goods, float width)
            {
                GUILayout.BeginVertical(GUILayout.Width(width));
                foreach (var good in goods)
                {
                    DrawAlertLine(good);
                }
                GUILayout.EndVertical();
            }

            private void RefreshAlertOrdering(IReadOnlyList<Core.Models.GoodInfo> belowThreshold)
            {
                var currentIds = new HashSet<string>(belowThreshold.Select(g => g.Id));

                foreach (var good in belowThreshold)
                {
                    if (_activeAlertIds.Contains(good.Id))
                    {
                        continue;
                    }

                    _activeAlertIds.Add(good.Id);
                    _alertOrder[good.Id] = ++_nextAlertOrder;
                }

                var removedIds = _activeAlertIds
                    .Where(id => !currentIds.Contains(id))
                    .ToList();

                foreach (var removedId in removedIds)
                {
                    _activeAlertIds.Remove(removedId);
                    _alertOrder.Remove(removedId);
                }
            }

            private float GetColumnWidth(IReadOnlyList<Core.Models.GoodInfo> goods)
            {
                var lineWidth = 0f;

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
