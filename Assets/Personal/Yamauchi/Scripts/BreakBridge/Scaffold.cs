using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
///     プレイヤーとの接触時間に応じて足場の色を変え、崩落させる
/// </summary>
public class Scaffold : MonoBehaviour
{
    /// <summary> 足場の崩落を始めたときに通知する </summary>
    public event Action HandlerDestroy;

    [SerializeField, Min(0.01f), Tooltip("崩落するまでに必要な累積接触時間（秒）")]
    private float _collapseAfterSeconds = 3f;
    [SerializeField, Tooltip("接触時間が3分の1に達したときの警告色")]
    private Color _warningColor = new(1f, 0.8f, 0.12f);
    [SerializeField, Tooltip("接触時間が3分の2に達したときの警告色")]
    private Color _criticalColor = new(1f, 0.2f, 0.12f);

    private Renderer[] _renderers;
    private MaterialPropertyBlock _colorProperties;
    private Scaffold _contactSource;
    private Scaffold _pairedBoard;
    private int _warningStage;
    private double _contactSeconds;
    private double _lastContactStep = double.NegativeInfinity;
    private bool _isCollapsing;
    private bool _isPermanent;

    private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
    private static readonly int COLOR_ID = Shader.PropertyToID("_Color");

    /// <summary>
    ///     復活用の足場として、乗っても崩れない状態にする
    /// </summary>
    public void KeepIntact()
    {
        _isPermanent = true;
    }

    /// <summary>
    ///     相方と接触時間および警告色を共有する
    /// </summary>
    internal void LinkContact(Scaffold partner)
    {
        _pairedBoard = partner;
        _contactSource = null;
        partner._contactSource = this;
    }

    /// <summary>
    ///     足場を沈めて崩落させる
    /// </summary>
    public void ZeroDurability()
    {
        if (_isCollapsing || _isPermanent)
        {
            return;
        }

        _isCollapsing = true;
        SetWarningStage(2);
        HandlerDestroy?.Invoke();
        transform.DOMoveY(transform.position.y - 1f, 2f)
            .SetLink(gameObject)
            .OnComplete(() => Destroy(gameObject));
    }

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _colorProperties = new MaterialPropertyBlock();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_isCollapsing || _isPermanent || !collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // 静止したプレイヤーも、足場が崩れるまで接触時間を数え続ける
        collision.rigidbody?.WakeUp();
        Scaffold source = _contactSource != null ? _contactSource : this;
        source.AccumulateContact();
    }

    /// <summary>
    ///     同じ組の複数枚や複数人が接触しても、物理更新1回分の時間だけ加算する
    /// </summary>
    private void AccumulateContact()
    {
        if (_isCollapsing || _lastContactStep == Time.fixedTimeAsDouble)
        {
            return;
        }

        _lastContactStep = Time.fixedTimeAsDouble;
        _contactSeconds += Time.fixedDeltaTime;
        double progress = (_contactSeconds + 0.0001) / Mathf.Max(0.01f, _collapseAfterSeconds);
        if (progress >= 1d)
        {
            ZeroDurability();
            return;
        }

        int warningStage = (int)(progress * 3d);
        SetWarningStage(warningStage);
        if (_pairedBoard != null)
        {
            _pairedBoard.SetWarningStage(warningStage);
        }
    }

    /// <summary>
    ///     共有マテリアルを使う他の板に影響を与えず、この板の警告色を変える
    /// </summary>
    private void SetWarningStage(int stage)
    {
        if (stage == _warningStage)
        {
            return;
        }

        _warningStage = stage;
        foreach (Renderer renderer in _renderers)
        {
            Material material = renderer.sharedMaterial;
            if (material == null)
            {
                continue;
            }

            int colorProperty = material.HasProperty(BASE_COLOR_ID) ? BASE_COLOR_ID : COLOR_ID;
            if (material.HasProperty(colorProperty))
            {
                renderer.GetPropertyBlock(_colorProperties);
                _colorProperties.SetColor(colorProperty, stage == 1 ? _warningColor : _criticalColor);
                renderer.SetPropertyBlock(_colorProperties);
            }
        }
    }
}
