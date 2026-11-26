using System;
using System.Collections;
using UnityEngine;

/// <summary>
///     プレイヤーの攻撃判定の有効時間とクールタイムを管理する。
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    /// <summary> クールタイムが明けて攻撃が成立したときに通知する。 </summary>
    public event Action OnAttackStarted;

    // 既存の呼び出し元とシーンの設定を維持する。
    public float coolTime = 3f;

    [SerializeField, Tooltip("攻撃中だけ有効にする当たり判定。")]
    private GameObject attackHitBox;
    [SerializeField, Tooltip("当たり判定が有効な時間（秒）。")]
    private float attackDuration = 0.2f;

    private Coroutine _attackRoutine;

    /// <summary>
    ///     当たり判定を有効化して攻撃を開始する。
    /// </summary>
    public void Attack()
    {
        if (_attackRoutine != null || attackHitBox == null || !isActiveAndEnabled)
        {
            return;
        }

        _attackRoutine = StartCoroutine(AttackCoroutine());
        OnAttackStarted?.Invoke();
    }

    /// <summary>
    ///     既存の呼び出し側から範囲攻撃を開始する。
    /// </summary>
    /// <param name="target"> 旧呼び出し側が指定する対象。接触した相手に判定する。 </param>
    public void Attack(GameObject target)
    {
        Attack();
    }

    private void Awake()
    {
        if (attackHitBox != null)
        {
            attackHitBox.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        if (attackHitBox != null)
        {
            attackHitBox.SetActive(false);
        }
    }

    /// <summary>
    ///     攻撃後に当たり判定を切り、クールタイムの終了を待つ。
    /// </summary>
    private IEnumerator AttackCoroutine()
    {
        attackHitBox.SetActive(true);
        yield return new WaitForSeconds(attackDuration);
        attackHitBox.SetActive(false);

        yield return new WaitForSeconds(coolTime);
        _attackRoutine = null;
    }
}
