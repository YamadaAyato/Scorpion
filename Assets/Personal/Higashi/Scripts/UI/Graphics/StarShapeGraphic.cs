using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 星の形を描く UI 部品（こぼれる星粒や、弾ける星に使う）。画像を用意しなくていい。
/// Inner Ratio を大きくするほど、ぷっくりした丸っこい星になる。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class StarShapeGraphic : MaskableGraphic
{
    /// <summary> 星の角の数。 </summary>
    public int PointCount
    {
        get => _pointCount;
        set
        {
            _pointCount = Mathf.Clamp(value, 3, 12);
            SetVerticesDirty();
        }
    }

    /// <summary> へこんだ部分の半径（とがった部分に対する割合）。1 にすると丸（多角形）になる。 </summary>
    public float InnerRatio
    {
        get => _innerRatio;
        set
        {
            _innerRatio = Mathf.Clamp(value, 0.1f, 1f);
            SetVerticesDirty();
        }
    }

    [Tooltip("星の角の数")]
    [SerializeField, Range(3, 12)] private int _pointCount = 5;

    [Tooltip("へこんだ部分の半径（とがった部分に対する割合）。大きいほどぷっくりした星になる")]
    [SerializeField, Range(0.1f, 1f)] private float _innerRatio = 0.55f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float outerRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float innerRadius = outerRadius * _innerRatio;

        // とがった点とへこんだ点を交互に並べる（真上から始める）
        int vertexCount = _pointCount * 2;
        AddVertex(vh, center);
        for (int i = 0; i < vertexCount; i++)
        {
            float angle = Mathf.PI * 0.5f + i * Mathf.PI * 2f / vertexCount;
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            AddVertex(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        for (int i = 0; i < vertexCount; i++)
        {
            vh.AddTriangle(0, i + 1, (i + 1) % vertexCount + 1);
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
