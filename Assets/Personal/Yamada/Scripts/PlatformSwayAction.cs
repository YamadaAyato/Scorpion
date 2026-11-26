using System;
using DG.Tweening;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     足場を左右に揺らしてから元の位置へ戻す
    /// </summary>
    [Serializable]
    public sealed class PlatformSwayAction : IGimmickAction
    {
        [SerializeField, Min(0f), Tooltip("左右に移動する幅")]
        private float _distance = 2f;
        [SerializeField, Min(0.1f), Tooltip("片側へ移動する時間（秒）")]
        private float _travelSeconds = 1.4f;

        /// <inheritdoc/>
        public bool TryActivate(GimmickContext context)
        {
            Transform platform = context.StageTargets != null ? context.StageTargets.MovingPlatform : null;
            if (platform == null || DOTween.IsTweening(platform))
            {
                return false;
            }

            float startX = platform.localPosition.x;
            float travel = Mathf.Max(0.1f, _travelSeconds);
            Sequence sequence = DOTween.Sequence().SetTarget(platform).SetUpdate(UpdateType.Fixed);
            sequence.Append(platform.DOLocalMoveX(startX + _distance, travel).SetEase(Ease.InOutSine));
            sequence.Append(platform.DOLocalMoveX(startX - _distance, travel * 2f).SetEase(Ease.InOutSine));
            sequence.Append(platform.DOLocalMoveX(startX, travel).SetEase(Ease.InOutSine));
            context.RegisterCleanup(() =>
            {
                sequence.Kill();
                if (platform != null)
                {
                    Vector3 position = platform.localPosition;
                    position.x = startX;
                    platform.localPosition = position;
                }
            });
            return true;
        }
    }
}
