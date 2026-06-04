using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Eremite.View.HUD.Reputation;
using StockAlert.Config;
using StockAlert.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StockAlert.UI.HUD
{
    internal sealed class ZeroBuildingBlueprintHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static FieldInfo _fiButton;
        private static GUIStyle _panelStyle;
        private static GUIStyle _titleStyle;
        private static GUIStyle _itemStyle;
        private static GUIStyle _emptyStyle;

        private bool _hovered;
        private List<string> _buildingNames = new List<string>();
        private float _nextRefreshTime;

        public static void Attach(ReputationRewardButton rewardButton)
        {
            if (!ConfigManager.ShowZeroBuildingBlueprintHover || rewardButton == null)
            {
                return;
            }

            _fiButton ??= typeof(ReputationRewardButton).GetField("button", BindingFlags.Instance | BindingFlags.NonPublic);
            var button = _fiButton?.GetValue(rewardButton) as Button;
            var target = button != null ? button.gameObject : rewardButton.gameObject;
            if (target == null || target.GetComponent<ZeroBuildingBlueprintHover>() != null)
            {
                return;
            }

            target.AddComponent<ZeroBuildingBlueprintHover>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!ConfigManager.ShowZeroBuildingBlueprintHover)
            {
                return;
            }

            _hovered = true;
            RefreshList();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        private void OnDisable()
        {
            _hovered = false;
        }

        private void Update()
        {
            if (!_hovered || !ConfigManager.ShowZeroBuildingBlueprintHover || Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            RefreshList();
        }

        private void OnGUI()
        {
            if (!_hovered || !ConfigManager.ShowZeroBuildingBlueprintHover)
            {
                return;
            }

            EnsureStyles();

            const float width = 360f;
            const float titleHeight = 28f;
            const float lineHeight = 19f;
            const float padding = 12f;
            var shownCount = Mathf.Min(_buildingNames.Count, 18);
            var hasMore = _buildingNames.Count > shownCount;
            var height = padding * 2f + titleHeight + (shownCount > 0 ? shownCount * lineHeight : 28f) + (hasMore ? lineHeight : 0f);
            var mouse = Event.current.mousePosition;
            var x = Mathf.Min(mouse.x + 18f, Screen.width - width - 8f);
            var y = Mathf.Min(mouse.y + 18f, Screen.height - height - 8f);
            x = Mathf.Max(8f, x);
            y = Mathf.Max(8f, y);

            var rect = new Rect(x, y, width, height);
            GUI.Box(rect, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(x + padding, y + padding, width - padding * 2f, titleHeight), "Unlocked buildings with 0 placed", _titleStyle);

            var lineY = y + padding + titleHeight;
            if (_buildingNames.Count == 0)
            {
                GUI.Label(new Rect(x + padding, lineY, width - padding * 2f, 28f), "None right now.", _emptyStyle);
                return;
            }

            for (var i = 0; i < shownCount; i++)
            {
                GUI.Label(new Rect(x + padding, lineY, width - padding * 2f, lineHeight), _buildingNames[i], _itemStyle);
                lineY += lineHeight;
            }

            if (hasMore)
            {
                GUI.Label(new Rect(x + padding, lineY, width - padding * 2f, lineHeight), $"+ {_buildingNames.Count - shownCount} more", _emptyStyle);
            }
        }

        private void RefreshList()
        {
            _nextRefreshTime = Time.unscaledTime + 0.5f;
            _buildingNames = GameAPI.GetUnlockedZeroCountBuildings()
                .Select(info => GameAPI.GetBuildingDisplayName(info.Model))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();
        }

        private static void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal =
                {
                    background = MakeTexture(new Color32(12, 10, 8, 238), new Color32(178, 143, 72, 255))
                }
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color32(255, 232, 151, 255) }
            };

            _itemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color32(244, 238, 219, 255) }
            };

            _emptyStyle = new GUIStyle(_itemStyle)
            {
                normal = { textColor = new Color32(186, 178, 158, 255) }
            };
        }

        private static Texture2D MakeTexture(Color32 fill, Color32 border)
        {
            const int size = 8;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var isBorder = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    texture.SetPixel(x, y, isBorder ? border : fill);
                }
            }

            texture.Apply(false, true);
            return texture;
        }
    }
}
