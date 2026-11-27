using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     橋の板を順番に崩し、ギミック終了後に元へ戻す
    /// </summary>
    [Serializable]
    public sealed class BridgeCollapseAction : IGimmickAction
    {
        [SerializeField, Min(1), Tooltip("一度の発動で崩す最大組数。単独の板も1組として扱う")]
        private int _boardCount = 2;
        [SerializeField, Min(0f), Tooltip("次の組が崩れるまでの時間（秒）")]
        private float _intervalSeconds = 0.55f;

        /// <inheritdoc/>
        public bool TryActivate(GimmickContext context)
        {
            StageGimmickTargets targets = context.StageTargets;
            if (targets == null)
            {
                return false;
            }

            var candidates = new List<(GameObject First, GameObject Second)>();
            foreach (AuthorScaffold support in targets.BridgeSupports)
            {
                if (support == null || support.CurrentObject == null || !support.CurrentObject.activeSelf)
                {
                    continue;
                }

                AuthorScaffold partner = support.PairedScaffold;
                GameObject second = partner != null ? partner.CurrentObject : null;
                if (partner == null || (second != null && second.activeSelf))
                {
                    candidates.Add((support.CurrentObject, second));
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            // 複数回の崩落が重なっても、この発動で実際に消した板だけを復元する
            var hiddenBoards = new List<GameObject>();
            Sequence sequence = DOTween.Sequence();
            int count = Mathf.Min(Mathf.Max(1, _boardCount), candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                (GameObject first, GameObject second) = candidates[index];
                bool paired = second != null;
                candidates.RemoveAt(index);
                sequence.AppendCallback(() =>
                {
                    if (first == null || !first.activeSelf || (paired && (second == null || !second.activeSelf)))
                    {
                        return;
                    }

                    first.SetActive(false);
                    hiddenBoards.Add(first);
                    if (paired)
                    {
                        second.SetActive(false);
                        hiddenBoards.Add(second);
                    }
                });
                if (i + 1 < count)
                {
                    sequence.AppendInterval(Mathf.Max(0f, _intervalSeconds));
                }
            }

            context.RegisterCleanup(() =>
            {
                // 先に予約済みの崩落を止め、復元した板が後から消えないようにする
                sequence.Kill();
                foreach (GameObject board in hiddenBoards)
                {
                    if (board != null)
                    {
                        board.SetActive(true);
                    }
                }
            });
            return true;
        }
    }
}
