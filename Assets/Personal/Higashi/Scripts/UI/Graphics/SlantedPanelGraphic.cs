using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 右上と左下の角を斜めに切り落とし、さらに全部の角を丸められる、ローポリ風の板を描く UI 部品（Image の代わりに使う）。
/// Corner Segments を少なくすると「少し丸めたローポリ」、多くすると滑らかな角丸になる。
/// Cut Size を 0 にして Corner Radius を高さの半分にするとカプセル型になる。
/// Border Width を 0 にすると中まで塗りつぶし、0 より大きくするとその太さの枠線だけを描く。
/// Mask と一緒に使うと、子の表示をこの形で切り抜ける。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class SlantedPanelGraphic : MaskableGraphic
{
    /// <summary> 右上と左下の角を切り落とす大きさ（Canvas 上の単位）。 </summary>
    public float CutSize
    {
        get => _cutSize;
        set
        {
            _cutSize = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    /// <summary> 角の丸み（Canvas 上の単位）。 </summary>
    public float CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    /// <summary> 角の丸みを何本の線で作るか。 </summary>
    public int CornerSegments
    {
        get => _cornerSegments;
        set
        {
            _cornerSegments = Mathf.Clamp(value, 1, MAX_CORNER_SEGMENTS);
            SetVerticesDirty();
        }
    }

    /// <summary> 枠線の太さ。0 なら中まで塗りつぶす。 </summary>
    public float BorderWidth
    {
        get => _borderWidth;
        set
        {
            _borderWidth = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    [Tooltip("角を切り落とす大きさ（Canvas 上の単位）。右上と左下の角に入る。0 なら四角形")]
    [SerializeField] private float _cutSize = 24f;

    [Tooltip("角の丸み（Canvas 上の単位）。0 なら角ばったまま")]
    [SerializeField] private float _cornerRadius;

    [Tooltip("角の丸みを何本の線で作るか。少ないほどローポリっぽく、多いほど滑らかになる")]
    [SerializeField, Range(1, MAX_CORNER_SEGMENTS)] private int _cornerSegments = 3;

    [Tooltip("枠線の太さ（Canvas 上の単位）。0 なら中まで塗りつぶす")]
    [SerializeField] private float _borderWidth;

    // 角の丸みを作る線の数の上限
    private const int MAX_CORNER_SEGMENTS = 16;

    // 45°で切り落とした辺を内側へずらしたとき、切り落とす大きさがどれだけ小さくなるかの係数（2 - √2）
    private const float INNER_CUT_RATE = 0.5857864f;

    // これより短い辺や小さい値は「無い」ものとして扱う
    private const float EPSILON = 0.0001f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float halfShort = Mathf.Min(rect.width, rect.height) * 0.5f;
        float cut = Mathf.Clamp(_cutSize, 0f, halfShort);
        float radius = Mathf.Clamp(_cornerRadius, 0f, halfShort);

        // 外側と内側で角の数をそろえるため、切り落としがあるかどうかは外側の値で決める
        bool hasCut = cut > EPSILON;
        List<Vector2> outer = BuildOutline(rect, cut, radius, hasCut);

        if (_borderWidth <= 0f)
        {
            AddFill(vh, rect.center, outer);
            return;
        }

        // 枠線：外側の形と、枠線の太さぶん内側に縮めた形の間を塗る
        float border = Mathf.Min(_borderWidth, halfShort);
        var innerRect = new Rect(rect.xMin + border, rect.yMin + border, rect.width - border * 2f, rect.height - border * 2f);
        float innerCut = Mathf.Max(0f, cut - border * INNER_CUT_RATE);
        float innerRadius = Mathf.Max(0f, radius - border);
        List<Vector2> inner = BuildOutline(innerRect, innerCut, innerRadius, hasCut);
        AddRing(vh, outer, inner);
    }

    /// <summary> 形の輪郭の点を、左上から時計回りに返す。角ごとに同じ数の点を出すので、外側と内側の点が1対1で対応する。 </summary>
    private List<Vector2> BuildOutline(Rect rect, float cut, float radius, bool hasCut)
    {
        Vector2[] corners = hasCut ? BuildCutCorners(rect, cut) : BuildRectCorners(rect);
        var outline = new List<Vector2>(corners.Length * (_cornerSegments + 1));

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 previous = corners[(i + corners.Length - 1) % corners.Length];
            Vector2 next = corners[(i + 1) % corners.Length];
            AddRoundedCorner(outline, previous, corners[i], next, radius);
        }
        return outline;
    }

    /// <summary>
    /// 1つの角を丸めた点を足す（丸めない・丸められない角でも、同じ数の点を足す）。
    /// 角の両側の辺に接する円の一部で角を置き換える。
    /// </summary>
    private void AddRoundedCorner(List<Vector2> outline, Vector2 previous, Vector2 corner, Vector2 next, float radius)
    {
        int pointCount = _cornerSegments + 1;
        Vector2 toPrevious = previous - corner;
        Vector2 toNext = next - corner;
        float previousLength = toPrevious.magnitude;
        float nextLength = toNext.magnitude;
        float angle = (previousLength > EPSILON && nextLength > EPSILON) ? Vector2.Angle(toPrevious, toNext) * Mathf.Deg2Rad : 0f;

        // 丸めない場合や、辺が無い・まっすぐで角になっていない場合は、角の点をそのまま並べる
        if (radius <= EPSILON || angle <= EPSILON || angle >= Mathf.PI - EPSILON)
        {
            for (int i = 0; i < pointCount; i++) outline.Add(corner);
            return;
        }

        Vector2 previousDirection = toPrevious / previousLength;
        Vector2 nextDirection = toNext / nextLength;
        float halfAngle = angle * 0.5f;

        // 角から円が辺に接する点までの距離。辺の半分を超えないようにして、隣の角の丸みとぶつからないようにする
        float tangentDistance = Mathf.Min(radius / Mathf.Tan(halfAngle), Mathf.Min(previousLength, nextLength) * 0.5f);
        float actualRadius = tangentDistance * Mathf.Tan(halfAngle);

        Vector2 startPoint = corner + previousDirection * tangentDistance;
        Vector2 endPoint = corner + nextDirection * tangentDistance;
        Vector2 center = corner + (previousDirection + nextDirection).normalized * (actualRadius / Mathf.Sin(halfAngle));

        float startAngle = Mathf.Atan2(startPoint.y - center.y, startPoint.x - center.x);
        float endAngle = Mathf.Atan2(endPoint.y - center.y, endPoint.x - center.x);
        float deltaAngle = Mathf.DeltaAngle(startAngle * Mathf.Rad2Deg, endAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;

        for (int i = 0; i < pointCount; i++)
        {
            float current = startAngle + deltaAngle * i / _cornerSegments;
            outline.Add(center + new Vector2(Mathf.Cos(current), Mathf.Sin(current)) * actualRadius);
        }
    }

    /// <summary> 右上と左下を切り落とした6角形の角を、左上から時計回りに返す。 </summary>
    private static Vector2[] BuildCutCorners(Rect rect, float cut)
    {
        return new[]
        {
            new Vector2(rect.xMin, rect.yMax),
            new Vector2(rect.xMax - cut, rect.yMax),
            new Vector2(rect.xMax, rect.yMax - cut),
            new Vector2(rect.xMax, rect.yMin),
            new Vector2(rect.xMin + cut, rect.yMin),
            new Vector2(rect.xMin, rect.yMin + cut),
        };
    }

    /// <summary> 四角形の角を、左上から時計回りに返す。 </summary>
    private static Vector2[] BuildRectCorners(Rect rect)
    {
        return new[]
        {
            new Vector2(rect.xMin, rect.yMax),
            new Vector2(rect.xMax, rect.yMax),
            new Vector2(rect.xMax, rect.yMin),
            new Vector2(rect.xMin, rect.yMin),
        };
    }

    /// <summary> 中心から扇形に三角形を並べて、形の中を塗りつぶす。 </summary>
    private void AddFill(VertexHelper vh, Vector2 center, List<Vector2> outline)
    {
        AddVertex(vh, center);
        foreach (Vector2 point in outline) AddVertex(vh, point);

        int count = outline.Count;
        for (int i = 0; i < count; i++)
        {
            vh.AddTriangle(0, i + 1, (i + 1) % count + 1);
        }
    }

    /// <summary> 外側と内側の輪郭の間を四角形でつないで、枠線を描く。 </summary>
    private void AddRing(VertexHelper vh, List<Vector2> outer, List<Vector2> inner)
    {
        foreach (Vector2 point in outer) AddVertex(vh, point);
        foreach (Vector2 point in inner) AddVertex(vh, point);

        int count = outer.Count;
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            vh.AddTriangle(i, next, next + count);
            vh.AddTriangle(i, next + count, i + count);
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
