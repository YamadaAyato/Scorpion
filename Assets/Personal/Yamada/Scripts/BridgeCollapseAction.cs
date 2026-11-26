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
        [SerializeField, Min(1), Tooltip("一度の発動で崩す板の最大枚数")]
        private int _boardCount = 2;
        [SerializeField, Min(0f), Tooltip("次の板が崩れるまでの時間（秒）")]
        private float _intervalSeconds = 0.55f;

        /// <inheritdoc/>
        public bool TryActivate(GimmickContext context)
        {
            StageGimmickTargets targets = context.StageTargets;
            if (targets == null)
            {
                return false;
            }

            var candidates = new List<GameObject>();
            foreach (AuthorScaffold support in targets.BridgeSupports)
            {
                if (support != null && support.CurrentObject != null && support.CurrentObject.activeSelf)
                {
                    candidates.Add(support.CurrentObject);
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var hiddenBoards = new List<GameObject>();
            Sequence sequence = DOTween.Sequence();
            int count = Mathf.Min(Mathf.Max(1, _boardCount), candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                GameObject board = candidates[index];
                candidates.RemoveAt(index);
                sequence.AppendCallback(() =>
                {
                    if (board != null && board.activeSelf)
                    {
                        board.SetActive(false);
                        hiddenBoards.Add(board);
                    }
                });
                if (i + 1 < count)
                {
                    sequence.AppendInterval(Mathf.Max(0f, _intervalSeconds));
                }
            }

            context.RegisterCleanup(() =>
            {
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
