using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 左側のフチが、雲や波のような丸いもこもこになった板を描く UI 部品（画像を用意しなくていい）。
/// もこもこ波ボタンで、右から押し寄せる色の波に使う（板の Mask の中で横に動かす）。
/// Phase を変えると、もこもこが上下にずれる。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class WaveEdgeGraphic : MaskableGraphic
{
    /// <summary> もこもこ1つの半径（Canvas 上の単位）。フチの山の高さもこの値になる。 </summary>
    public float BumpRadius
    {
        get => _bumpRadius;
        set
        {
            _bumpRadius = Mathf.Max(1f, value);
            SetVerticesDirty();
        }
    }

    /// <summary> もこもこの上下のずれ（1 で山1つぶん）。 </summary>
    public float Phase
    {
        get => _phase;
        set
        {
            _phase = value;
            SetVerticesDirty();
        }
    }

    [Tooltip("もこもこ1つの半径（Canvas 上の単位）。フチの山の高さもこの値になる")]
    [SerializeField] private float _bumpRadius = 14f;

    [Tooltip("もこもこ1つを何本の線で作るか。多いほど滑らかになる")]
    [SerializeField, Range(2, 24)] private int _segmentsPerBump = 8;

    [Tooltip("もこもこの上下のずれ（1 で山1つぶん）")]
    [SerializeField] private float _phase;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float bumpHeight = _bumpRadius * 2f;
        int sampleCount = Mathf.Max(2, Mathf.CeilToInt(rect.height / bumpHeight * _segmentsPerBump) + 1);

        // 上から下へ、左のフチ（もこもこ）と右端の点を1組ずつ並べる
        for (int i = 0; i < sampleCount; i++)
        {
            float y = Mathf.Lerp(rect.yMax, rect.yMin, i / (float)(sampleCount - 1));
            float bumpPosition = (rect.yMax - y) / bumpHeight + _phase;

            // |sin| にすると、山と山のつなぎ目がくぼみ、山の真ん中が左へ丸くふくらむ（雲のフチの形）
            float offset = _bumpRadius * (1f - Mathf.Abs(Mathf.Sin(bumpPosition * Mathf.PI)));
            AddVertex(vh, new Vector2(rect.xMin + offset, y));
            AddVertex(vh, new Vector2(rect.xMax, y));
        }

        for (int i = 0; i < sampleCount - 1; i++)
        {
            int left = i * 2;
            int right = left + 1;
            int nextLeft = left + 2;
            int nextRight = left + 3;
            vh.AddTriangle(left, right, nextRight);
            vh.AddTriangle(left, nextRight, nextLeft);
        }
    }

    /// <summary> 色付きの頂点を1つ足す。 </summary>
    private void AddVertex(VertexHelper vh, Vector2 position)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        vh.AddVert(vertex);
    }
}
