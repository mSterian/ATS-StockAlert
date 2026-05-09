using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace StockAlert.UI.World
{
    internal sealed class TrackedGoodClickTarget : MonoBehaviour, IPointerClickHandler
    {
        public string GoodId;

        public static void Attach(Component target, string goodId)
        {
            if (target == null || string.IsNullOrWhiteSpace(goodId))
            {
                return;
            }

            var clickTarget = target.GetComponent<TrackedGoodClickTarget>();
            if (clickTarget == null)
            {
                clickTarget = target.gameObject.AddComponent<TrackedGoodClickTarget>();
            }

            clickTarget.GoodId = goodId;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null
                || eventData.button != PointerEventData.InputButton.Left
                || !IsModifierPressed()
                || string.IsNullOrWhiteSpace(GoodId))
            {
                return;
            }

            ItemLocatorOverlay.Toggle(GoodId);
            eventData.Use();
        }

        private static bool IsModifierPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                return keyboard.leftCtrlKey.isPressed
                    || keyboard.rightCtrlKey.isPressed
                    || keyboard.ctrlKey.isPressed;
            }

            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }
    }
}
