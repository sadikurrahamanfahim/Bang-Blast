using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace FGUIStarter
{
    public class CustomButton : Button, IPointerDownHandler, IPointerUpHandler
    {
        RectTransform textRect;
        Vector2 originalTextPos;

        bool isHeld;
        protected override void Awake()
        {
            base.Awake();
            textRect = GetComponentInChildren<TextMeshProUGUI>().rectTransform;
            originalTextPos = textRect.anchoredPosition;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            isHeld = true;
            ApplyPressedVisual();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            isHeld = false;
            ApplyNormalVisual();
        }

        private void ApplyPressedVisual()
        {
            if (textRect != null)
            {
                float height = ((RectTransform)transform).rect.height;
                float offset = height - (height * 0.86718f);
                textRect.anchoredPosition = originalTextPos - new Vector2(0, offset);
            }
        }

        private void ApplyNormalVisual()
        {
            if (textRect != null)
            {
                textRect.anchoredPosition = originalTextPos;
            }
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            if (state == SelectionState.Pressed)
            {
                ApplyPressedVisual();
            }

            else
            {
                ApplyNormalVisual();
            }
        }
    }
}
