using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
///     プレイヤーが乗った時間に応じて耐久値を減らし、足場を崩す
/// </summary>
public class Scaffold : MonoBehaviour
{
    /// <summary> 足場の崩落を始めたときに通知する </summary>
    public event Action HandlerDestroy;

    [SerializeField, Tooltip("足場に乗られたときに減る耐久値")]
    private int _durability;

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
        HandlerDestroy?.Invoke();
        transform.DOMoveY(transform.position.y - 1f, 2f)
            .SetLink(gameObject)
            .OnComplete(() => Destroy(gameObject));
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_isCollapsing || _isPermanent || !collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        if (_durability <= 0)
        {
            ZeroDurability();
            return;
        }

        _durability--;
    }
}
