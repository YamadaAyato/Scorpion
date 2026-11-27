using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトルロゴの「キラーン」演出。一定の間隔で、次の流れをくり返す。
/// ・流れ星：左上の外から星がしっぽを引いて飛んできて、ロゴの右上で止まる
/// ・光の帯：流れ星が通るのに合わせて、斜めの光がロゴの文字の上だけを左から右へ走る
/// ・キラーン：流れ星が止まった場所で、十字の光がパッと開いて回りながら消え、小さな星が散る
/// ロゴの Image には Mask を付けておく（光の帯を文字の形で切り抜くため）。流れ星と光は、このオブジェクトの子に再生中だけ作る。
/// 光の帯としっぽの絵は、画像を用意しなくていいように、起動時にグラデーションの絵を作って使う。
/// </summary>
public class TitleLogoShine : MonoBehaviour
{
    [Tooltip("ロゴの Image（Mask を付けたもの）。光の帯はこの形で切り抜かれる")]
    [SerializeField] private Image _logo;

    [Tooltip("キラーンをくり返す間隔（秒）")]
    [SerializeField] private float _interval = 4f;

    private RectTransform _logoRect;
    private RectTransform _shine;
    private RectTransform _star;
    private RectTransform _starBody;
    private RectTransform _tail;
    private Image _tailImage;
    private StarShapeGraphic _starGraphic;
    private StarShapeGraphic _sparkle;
    private Texture2D _bandTexture;
    private Texture2D _tailTexture;
    private Sequence _sequence;

    // 流れ星の通り道（ロゴの大きさに対する割合。ロゴの中心が 0）。始まりは左上の外、終わりは右上の文字のあたり
    private static readonly Vector2 STAR_FROM = new Vector2(-0.75f, 0.75f);
    private static readonly Vector2 STAR_TO = new Vector2(0.36f, 0.36f);

    // 大きさ（Canvas 上の単位）：流れ星、しっぽの長さと太さ、キラーンの十字、散る星、光の帯の太さ
    private const float STAR_SIZE = 46f;
    private const float TAIL_LENGTH = 220f;
    private const float TAIL_WIDTH = 18f;
    private const float SPARKLE_SIZE = 150f;
    private static readonly Vector2 SPARK_SIZE_RANGE = new Vector2(14f, 24f);
    private static readonly Vector2 SPARK_DISTANCE_RANGE = new Vector2(50f, 100f);
    private const int SPARK_COUNT = 5;
    private const float SHINE_WIDTH_RATE = 0.22f;

    // 光の帯の傾き（度）と濃さ
    private const float SHINE_ANGLE = -25f;
    private const float SHINE_ALPHA = 0.85f;

    // 時間（秒）：最初の待ち、流れ星が飛ぶ、光の帯が走り始めるまで、光の帯が走る、キラーンが開く・消える、流れ星が消える
    private const float FIRST_DELAY = 0.8f;
    private const float STAR_FLY_DURATION = 0.45f;
    private const float SHINE_DELAY = 0.25f;
    private const float SHINE_DURATION = 0.55f;
    private const float SPARKLE_OPEN_DURATION = 0.12f;
    private const float SPARKLE_CLOSE_DURATION = 0.3f;
    private const float STAR_FADE_DURATION = 0.2f;
    private const float SPARK_DURATION = 0.45f;

    // 色
    private static readonly Color STAR_COLOR = new Color32(0xFF, 0xF4, 0xB0, 0xFF);
    private static readonly Color SPARKLE_COLOR = Color.white;
    private static readonly Color SPARK_COLOR = new Color32(0xFF, 0xE0, 0x6A, 0xFF);

    // 形（StarShapeGraphic）：流れ星は5つの角、キラーンは細い十字、散る星は5つの角
    private const int STAR_POINTS = 5;
    private const float STAR_INNER_RATIO = 0.5f;
    private const int SPARKLE_POINTS = 4;
    private const float SPARKLE_INNER_RATIO = 0.18f;

    // 起動時に作るグラデーションの絵の大きさ（ピクセル）
    private const int GRADIENT_RESOLUTION = 64;

    private void Awake()
    {
        if (_logo == null) return;

        _logoRect = _logo.rectTransform;
        _bandTexture = CreateGradientTexture(true);
        _tailTexture = CreateGradientTexture(false);
        CreateShine();
        CreateShootingStar();
        HideAll();
    }

    private void OnEnable()
    {
        if (_logo == null) return;
        _sequence = DOTween.Sequence()
            .AppendInterval(FIRST_DELAY)
            .AppendCallback(PlayOnce)
            .AppendInterval(_interval)
            .SetLoops(-1)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        if (_sequence != null && _sequence.IsActive()) _sequence.Kill();
        DOTween.Kill(this);
        if (_logo != null) HideAll();
    }

    private void OnDestroy()
    {
        if (_bandTexture != null) Destroy(_bandTexture);
        if (_tailTexture != null) Destroy(_tailTexture);
    }

    /// <summary> このコンポーネントを付けたときに、子の Logo を自動で探して入れる。 </summary>
    private void Reset()
    {
        Transform logo = transform.Find("Logo");
        _logo = logo != null ? logo.GetComponent<Image>() : null;
    }

    /// <summary> キラーンを1回流す（流れ星 → 光の帯 → キラーン）。 </summary>
    private void PlayOnce()
    {
        DOTween.Kill(this);
        Vector2 size = _logoRect.rect.size;
        Vector2 center = _logoRect.anchoredPosition;
        Vector2 from = center + Vector2.Scale(STAR_FROM, size);
        Vector2 to = center + Vector2.Scale(STAR_TO, size);

        // しっぽは、飛ぶ向きと反対へ伸びるように回しておく
        Vector2 direction = (to - from).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _tail.localRotation = Quaternion.Euler(0f, 0f, angle);
        _star.anchoredPosition = from;
        _starBody.localRotation = Quaternion.identity;
        _starGraphic.color = STAR_COLOR;
        _tailImage.color = Color.white;
        _star.gameObject.SetActive(true);

        float shineHalfTravel = size.x * 0.5f + _shine.rect.width;
        _shine.anchoredPosition = new Vector2(-shineHalfTravel, 0f);

        DOTween.Sequence()
            .Append(DOTween.To(() => _star.anchoredPosition, value => _star.anchoredPosition = value, to, STAR_FLY_DURATION).SetEase(Ease.OutQuad))
            .Join(_starBody.DOLocalRotate(new Vector3(0f, 0f, -360f), STAR_FLY_DURATION, RotateMode.LocalAxisAdd))
            .Insert(SHINE_DELAY, DOVirtual.Float(-shineHalfTravel, shineHalfTravel, SHINE_DURATION, x => _shine.anchoredPosition = new Vector2(x, 0f))
                .SetEase(Ease.InOutSine)
                .OnStart(() => _shine.gameObject.SetActive(true))
                .OnComplete(() => _shine.gameObject.SetActive(false)))
            .InsertCallback(STAR_FLY_DURATION, () => Sparkle(to))
            .Insert(STAR_FLY_DURATION, DOVirtual.Float(1f, 0f, STAR_FADE_DURATION, SetStarAlpha))
            .AppendCallback(() => _star.gameObject.SetActive(false))
            .SetUpdate(true)
            .SetLink(gameObject)
            .SetTarget(this);
    }

    /// <summary> キラーン：十字の光がパッと開いて回りながら閉じ、まわりに小さな星が散る。 </summary>
    private void Sparkle(Vector2 position)
    {
        RectTransform sparkle = _sparkle.rectTransform;
        sparkle.anchoredPosition = position;
        sparkle.localScale = Vector3.zero;
        sparkle.localRotation = Quaternion.identity;
        _sparkle.gameObject.SetActive(true);
        DOTween.Sequence()
            .Append(DOVirtual.Float(0f, 1f, SPARKLE_OPEN_DURATION, value => sparkle.localScale = Vector3.one * value).SetEase(Ease.OutBack))
            .Append(DOVirtual.Float(1f, 0f, SPARKLE_CLOSE_DURATION, value => sparkle.localScale = Vector3.one * value).SetEase(Ease.InQuad))
            .Insert(0f, sparkle.DOLocalRotate(new Vector3(0f, 0f, 90f), SPARKLE_OPEN_DURATION + SPARKLE_CLOSE_DURATION, RotateMode.LocalAxisAdd))
            .OnComplete(() => _sparkle.gameObject.SetActive(false))
            .SetUpdate(true)
            .SetLink(gameObject)
            .SetTarget(this);

        for (int i = 0; i < SPARK_COUNT; i++)
        {
            float angle = (360f / SPARK_COUNT * i + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            Vector2 end = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(SPARK_DISTANCE_RANGE.x, SPARK_DISTANCE_RANGE.y);
            StarShapeGraphic spark = CreateShape("Spark", Random.Range(SPARK_SIZE_RANGE.x, SPARK_SIZE_RANGE.y), STAR_POINTS, STAR_INNER_RATIO, SPARK_COLOR);
            RectTransform rect = spark.rectTransform;
            rect.anchoredPosition = position;
            DOTween.Sequence()
                .Append(DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value, end, SPARK_DURATION).SetEase(Ease.OutCubic))
                .Join(rect.DOScale(Vector3.zero, SPARK_DURATION).SetEase(Ease.InQuad))
                .Join(rect.DOLocalRotate(new Vector3(0f, 0f, 180f), SPARK_DURATION, RotateMode.LocalAxisAdd))
                .OnKill(() => { if (spark != null) Destroy(spark.gameObject); })
                .SetUpdate(true)
                .SetLink(spark.gameObject)
                .SetTarget(this);
        }
    }

    /// <summary> 光の帯を作る。ロゴの子に置くので、ロゴの Mask で文字の形に切り抜かれる。 </summary>
    private void CreateShine()
    {
        var go = new GameObject("Shine", typeof(RectTransform));
        go.layer = gameObject.layer;
        _shine = (RectTransform)go.transform;
        _shine.SetParent(_logoRect, false);
        Vector2 size = _logoRect.rect.size;
        // 傾けても上下が切れないよう、縦はロゴより長くする
        _shine.sizeDelta = new Vector2(size.x * SHINE_WIDTH_RATE, size.magnitude);
        _shine.localRotation = Quaternion.Euler(0f, 0f, SHINE_ANGLE);

        var image = go.AddComponent<Image>();
        image.sprite = CreateSprite(_bandTexture);
        image.color = new Color(1f, 1f, 1f, SHINE_ALPHA);
        image.raycastTarget = false;
    }

    /// <summary>
    /// 流れ星とキラーンの十字を作る。ロゴの外まで飛ぶので、ロゴと同じ親（このオブジェクト）の子に置く。
    /// 流れ星は、動く入れ物の中に「しっぽ」と「回る星」を分けて入れる（しっぽが星と一緒に回らないように）。
    /// </summary>
    private void CreateShootingStar()
    {
        var go = new GameObject("ShootingStar", typeof(RectTransform));
        go.layer = gameObject.layer;
        _star = (RectTransform)go.transform;
        _star.SetParent(transform, false);
        _star.sizeDelta = Vector2.one * STAR_SIZE;

        // しっぽ：星の中心から後ろへ伸びる
        var tailObject = new GameObject("Tail", typeof(RectTransform));
        tailObject.layer = gameObject.layer;
        _tail = (RectTransform)tailObject.transform;
        _tail.SetParent(_star, false);
        _tail.pivot = new Vector2(1f, 0.5f);
        _tail.sizeDelta = new Vector2(TAIL_LENGTH, TAIL_WIDTH);
        _tailImage = tailObject.AddComponent<Image>();
        _tailImage.sprite = CreateSprite(_tailTexture);
        _tailImage.raycastTarget = false;

        var body = new GameObject("Star", typeof(RectTransform));
        body.layer = gameObject.layer;
        _starBody = (RectTransform)body.transform;
        _starBody.SetParent(_star, false);
        _starBody.sizeDelta = Vector2.one * STAR_SIZE;
        _starGraphic = body.AddComponent<StarShapeGraphic>();
        _starGraphic.PointCount = STAR_POINTS;
        _starGraphic.InnerRatio = STAR_INNER_RATIO;
        _starGraphic.raycastTarget = false;

        _sparkle = CreateShape("Sparkle", SPARKLE_SIZE, SPARKLE_POINTS, SPARKLE_INNER_RATIO, SPARKLE_COLOR);
    }

    /// <summary> このオブジェクトの子（ロゴより手前）に星の形を作る。 </summary>
    private StarShapeGraphic CreateShape(string objectName, float size, int points, float innerRatio, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.layer = gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.sizeDelta = Vector2.one * size;

        var shape = go.AddComponent<StarShapeGraphic>();
        shape.PointCount = points;
        shape.InnerRatio = innerRatio;
        shape.color = color;
        shape.raycastTarget = false;
        return shape;
    }

    private void HideAll()
    {
        _shine.gameObject.SetActive(false);
        _star.gameObject.SetActive(false);
        _sparkle.gameObject.SetActive(false);
    }

    /// <summary> 流れ星の星としっぽを一緒に薄くする。 </summary>
    private void SetStarAlpha(float alpha)
    {
        _starGraphic.color = new Color(STAR_COLOR.r, STAR_COLOR.g, STAR_COLOR.b, alpha);
        _tailImage.color = new Color(1f, 1f, 1f, alpha);
    }

    /// <summary>
    /// グラデーションの絵を作る。isBand なら横方向の真ん中が一番濃い帯（光の帯）、そうでなければ右へいくほど濃くなる帯（しっぽ）。
    /// 縦方向も、端が薄くなるようにする。
    /// </summary>
    private static Texture2D CreateGradientTexture(bool isBand)
    {
        var texture = new Texture2D(GRADIENT_RESOLUTION, GRADIENT_RESOLUTION, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };
        for (int y = 0; y < GRADIENT_RESOLUTION; y++)
        {
            float v = (y + 0.5f) / GRADIENT_RESOLUTION;
            float vertical = isBand ? 1f : 1f - Mathf.Abs(v * 2f - 1f);
            for (int x = 0; x < GRADIENT_RESOLUTION; x++)
            {
                float u = (x + 0.5f) / GRADIENT_RESOLUTION;
                float horizontal = isBand ? 1f - Mathf.Abs(u * 2f - 1f) : u * u;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, horizontal) * vertical));
            }
        }
        texture.Apply();
        return texture;
    }

    private static Sprite CreateSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}
