using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     プレイヤーの攻撃判定の有効時間とクールタイムを管理する
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    /// <summary> 攻撃が始まったときに通知する </summary>
    public event Action OnAttackStarted;

    /// <summary> 命中時の吹っ飛ばし倍率を取得する </summary>
    internal float KnockbackMultiplier => _knockbackMultipliers.Last?.Value ?? 1f;

    [SerializeField, Tooltip("攻撃中だけ有効にする当たり判定")]
    private GameObject _attackHitbox;
    [SerializeField, Tooltip("当たり判定を有効にする時間（秒）")]
    private float _attackDurationSeconds = 0.2f;
    [SerializeField, Tooltip("当たり判定が消えてから次の攻撃までの待ち時間（秒）")]
    private float _cooldownSeconds = 3f;

    private readonly LinkedList<float> _knockbackMultipliers = new();
    private Coroutine _attackRoutine;

    /// <summary>
    ///     攻撃可能な場合に当たり判定を有効化する
    /// </summary>
    public void Attack()
    {
        if (!isActiveAndEnabled || _attackHitbox == null || _attackRoutine != null)
        {
            return;
        }

        _attackRoutine = StartCoroutine(RunAttack());
        OnAttackStarted?.Invoke();
    }

    /// <summary>
    ///     既存の対象指定付きの呼び出しから範囲攻撃を開始する
    /// </summary>
    /// <param name="target"> 互換性のために受け取る攻撃対象 </param>
    public void Attack(GameObject target)
    {
        Attack();
    }

    /// <summary>
    ///     吹っ飛ばしに一時倍率を適用し、この効果だけを解除する処理を返す
    /// </summary>
    internal Action ApplyKnockbackMultiplier(float multiplier)
    {
        LinkedListNode<float> effect = _knockbackMultipliers.AddLast(multiplier);
        return () => _knockbackMultipliers.Remove(effect);
    }

    private void Awake()
    {
        if (_attackHitbox != null)
        {
            _attackHitbox.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        if (_attackHitbox != null)
        {
            _attackHitbox.SetActive(false);
        }
    }

    /// <summary>
    ///     攻撃時間が過ぎたら当たり判定を無効化し、クールタイムの終了を待つ
    /// </summary>
    private IEnumerator RunAttack()
    {
        _attackHitbox.SetActive(true);
        yield return new WaitForSeconds(_attackDurationSeconds);
        _attackHitbox.SetActive(false);

        yield return new WaitForSeconds(_cooldownSeconds);
        _attackRoutine = null;
    }
}
