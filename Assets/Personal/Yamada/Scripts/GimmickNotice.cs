using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scorpion.UI
{
    /// <summary>
    ///     発動したギミック名を試合HUDに一時表示する
    /// </summary>
    internal sealed class GimmickNotice
    {
        /// <summary>
        ///     HUDにギミック発動通知を作る
        /// </summary>
        internal GimmickNotice(Canvas canvas, Font font, bool emphasize = false)
        {
            _emphasize = emphasize;
            var item = new GameObject(emphasize ? "Score Multiplier Notice" : "Gimmick Notice", typeof(RectTransform));
            item.transform.SetParent(canvas.transform, false);
            _text = item.AddComponent<Text>();
            _text.font = font;
            _text.fontSize = emphasize ? 54 : 32;
            _text.color = emphasize ? new Color(1f, 0.84f, 0.24f) : new Color(0.52f, 0.94f, 0.78f);
            _text.alignment = emphasize ? TextAnchor.MiddleCenter : TextAnchor.UpperCenter;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.supportRichText = false;
            _text.raycastTarget = false;

            RectTransform rect = _text.rectTransform;
            float anchorY = emphasize ? 0f : 1f;
            rect.anchorMin = new Vector2(0.1f, anchorY);
            rect.anchorMax = new Vector2(0.9f, anchorY);
            rect.pivot = new Vector2(0.5f, emphasize ? 0.5f : 1f);
            rect.sizeDelta = new Vector2(0f, emphasize ? 76f : 40f);
            // 倍率は画面下に分け、ラッシュ告知や通常ギミックと重ならない位置に置く
            rect.anchoredPosition = new Vector2(0f, emphasize ? 100f : -196f);
            PopOutline outline = item.AddComponent<PopOutline>();
            outline.effectColor = new Color(0.27f, 0.16f, 0.4f, 1f);
            outline.effectDistance = emphasize ? new Vector2(4f, -4f) : new Vector2(2.5f, -2.5f);
            Shadow shadow = item.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.27f, 0.16f, 0.4f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -3f);
            Clear();
        }

        private readonly Text _text;
        private readonly bool _emphasize;
        private readonly List<string> _names = new();
        private Tween _pulse;
        private float _hideAt;

        private const float DISPLAY_SECONDS = 1.8f;

        /// <summary>
        ///     表示中に別のギミックが発動したら、前の名前と一緒に表示する
        /// </summary>
        internal void Show(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            HideWhenExpired();
            if (_emphasize)
            {
                _text.text = name;
            }
            else if (!_names.Contains(name))
            {
                _names.Add(name);
                _text.text = string.Join("  /  ", _names);
            }

            _text.gameObject.SetActive(true);
            _hideAt = Time.unscaledTime + (_emphasize ? 2.4f : DISPLAY_SECONDS);
            if (_emphasize)
            {
                _pulse?.Kill();
                RectTransform rect = _text.rectTransform;
                rect.localScale = Vector3.one * 0.85f;
                _pulse = rect.DOScale(Vector3.one, 0.32f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .SetLink(_text.gameObject);
            }
        }

        /// <summary>
        ///     指定時間が過ぎたら通知を片付ける
        /// </summary>
        internal void HideWhenExpired()
        {
            if (_text.gameObject.activeSelf && Time.unscaledTime >= _hideAt)
            {
                Clear();
            }
        }

        /// <summary>
        ///     現在の発動通知を消す
        /// </summary>
        internal void Clear()
        {
            _pulse?.Kill();
            _pulse = null;
            _names.Clear();
            _text.text = string.Empty;
            _text.gameObject.SetActive(false);
        }
    }
}
