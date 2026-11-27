using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Scorpion.UI
{
    /// <summary>
    ///     文字の周囲を円状に複数回ずらして描き、太くてもガタつかない縁取りを付ける
    ///     （標準の Outline は斜め4方向だけなので、太くすると角が欠けて粗く見える）
    /// </summary>
    [AddComponentMenu("UI/Effects/Pop Outline")]
    public class PopOutline : Shadow
    {
        [SerializeField, Range(4, 24), Tooltip("縁取りを描く方向の数。太い縁取りほど多いほうが滑らか")]
        private int _sampleCount = 12;

        /// <summary>
        ///     元の頂点を円周上の各方向へずらした縁取り用の頂点を追加する
        /// </summary>
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }

            List<UIVertex> verts = ListPool<UIVertex>.Get();
            vh.GetUIVertexStream(verts);

            int neededCapacity = verts.Count * (_sampleCount + 1);
            if (verts.Capacity < neededCapacity)
            {
                verts.Capacity = neededCapacity;
            }

            float radiusX = Mathf.Abs(effectDistance.x);
            float radiusY = Mathf.Abs(effectDistance.y);
            int start = 0;
            int end = verts.Count;
            for (int i = 0; i < _sampleCount; i++)
            {
                float angle = Mathf.PI * 2f * i / _sampleCount;
                // ずらした側が縁取りになり、元の文字は末尾へ移るので、次は末尾の範囲をずらす
                ApplyShadowZeroAlloc(verts, effectColor, start, end, Mathf.Cos(angle) * radiusX,
                    Mathf.Sin(angle) * radiusY);
                start = end;
                end = verts.Count;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
            ListPool<UIVertex>.Release(verts);
        }
    }
}
