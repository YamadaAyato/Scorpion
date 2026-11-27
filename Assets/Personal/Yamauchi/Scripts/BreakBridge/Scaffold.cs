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

    private Renderer _renderer;
    private MaterialPropertyBlock _colorProperties;
    private int _colorPropertyId;
    private int _warningStage;
    private double _contactSeconds;
    private double _lastContactStep = double.NegativeInfinity;
    private bool _isCollapsing;
    private bool _isPermanent;

    /// <summary>
    ///     復活用の足場として、乗っても崩れない状態にする
    /// </summary>
    public void KeepIntact()
    {
        _isPermanent = true;
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
        _renderer = GetComponentInChildren<Renderer>();
        Material material = _renderer != null ? _renderer.sharedMaterial : null;
        if (material == null)
        {
            return;
        }

        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        if (material.HasProperty(colorProperty))
        {
            _colorPropertyId = Shader.PropertyToID(colorProperty);
            _colorProperties = new MaterialPropertyBlock();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_isCollapsing || _isPermanent || !collision.gameObject.CompareTag("Player") ||
            _lastContactStep == Time.fixedTimeAsDouble)
        {
            return;
        }

        // 2人が同じ板に乗っても、1回の物理更新につき実際に経過した時間だけ加算する
        _lastContactStep = Time.fixedTimeAsDouble;
        _contactSeconds += Time.fixedDeltaTime;
        double progress = (_contactSeconds + 0.0001) / Mathf.Max(0.01f, _collapseAfterSeconds);
        if (progress >= 1d)
        {
            ZeroDurability();
            return;
        }

        SetWarningStage((int)(progress * 3d));
    }

    /// <summary>
    ///     共有マテリアルを使う他の板に影響を与えず、この板の警告色を変える
    /// </summary>
    private void SetWarningStage(int stage)
    {
        if (stage == _warningStage || _renderer == null || _colorProperties == null)
        {
            return;
        }

        _warningStage = stage;
        _renderer.GetPropertyBlock(_colorProperties);
        _colorProperties.SetColor(_colorPropertyId, stage == 1 ? _warningColor : _criticalColor);
        _renderer.SetPropertyBlock(_colorProperties);
    }
}
