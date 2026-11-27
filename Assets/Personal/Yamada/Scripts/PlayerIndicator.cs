using UnityEngine;
using UnityEngine.UI;

namespace Scorpion.UI
{
    /// <summary>
    ///     プレイヤーの頭上にHUD上の矢印と名前を表示する
    /// </summary>
    internal sealed class PlayerIndicator
    {
        /// <summary>
        ///     HUD上に矢印と名前を作り、追従先を登録する
        /// </summary>
        internal PlayerIndicator(Canvas canvas, Font font, GameObject player, string label, Color arrowColor)
        {
            _canvas = canvas;
            _canvasRect = (RectTransform)canvas.transform;
            _player = player;
            _body = player != null ? player.GetComponent<Collider>() : null;

            Font arrowFont = font != null && font.HasCharacter('▼')
                ? font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _arrow = CreateText(_canvasRect, arrowFont, label + " Arrow", "▼", 32, arrowColor);
            _label = CreateText(_arrow.rectTransform, font, label + " Label", label, 24, Color.white);
            _label.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            Hide();
        }

        private readonly Canvas _canvas;
        private readonly RectTransform _canvasRect;
        private readonly GameObject _player;
        private readonly Collider _body;
        private readonly Text _arrow;
        private readonly Text _label;

        /// <summary>
        ///     プレイヤーの頭上を画面座標に変換して矢印と名前を配置する
        /// </summary>
        internal void Refresh(Camera camera, bool showLabel)
        {
            if (camera == null || _player == null || !_player.activeInHierarchy)
            {
                Hide();
                return;
            }

            // Rigidbodyの当たり判定を基準にして、攻撃アニメーションで頭上表示が揺れないようにする
            Vector3 position = _player.transform.position + Vector3.up;
            if (_body != null && _body.enabled && !_body.isTrigger)
            {
                Bounds bounds = _body.bounds;
                position = new Vector3(bounds.center.x, bounds.max.y + 0.25f, bounds.center.z);
            }

            Vector3 screen = camera.WorldToScreenPoint(position);
            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            if (screen.z <= 0f || !camera.pixelRect.Contains(screen) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, uiCamera, out Vector2 local))
            {
                Hide();
                return;
            }

            _arrow.rectTransform.localPosition = local;
            _arrow.gameObject.SetActive(true);
            _label.gameObject.SetActive(showLabel);
        }

        /// <summary>
        ///     矢印と名前を隠す
        /// </summary>
        internal void Hide()
        {
            _arrow.gameObject.SetActive(false);
        }

        /// <summary>
        ///     背景に埋もれないよう縁取りを付けた表示用テキストを作る
        /// </summary>
        private static Text CreateText(RectTransform parent, Font font, string objectName, string text, int size, Color color)
        {
            var item = new GameObject(objectName, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            Text graphic = item.AddComponent<Text>();
            graphic.font = font;
            graphic.text = text;
            graphic.fontSize = size;
            graphic.alignment = TextAnchor.MiddleCenter;
            graphic.verticalOverflow = VerticalWrapMode.Overflow;
            graphic.color = color;
            graphic.raycastTarget = false;
            graphic.rectTransform.sizeDelta = new Vector2(110f, 32f);
            PopOutline outline = item.AddComponent<PopOutline>();
            outline.effectColor = new Color(0.27f, 0.16f, 0.4f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
            Shadow shadow = item.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.27f, 0.16f, 0.4f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -3f);
            return graphic;
        }
    }
}
