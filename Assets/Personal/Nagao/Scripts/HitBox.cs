using System;
using System.Collections.Generic;
using Scorpion.Gimmicks;
using UnityEngine;

/// <summary>
///     攻撃に触れたプレイヤーと落下物を弾き飛ばす
/// </summary>
public class Hitbox : MonoBehaviour
{
    /// <summary> 攻撃が有効な対象に命中したときに通知する </summary>
    public static event Action OnHitConfirmed;

    [SerializeField, Tooltip("相手を吹き飛ばす力")]
    private float _knockbackForce = 10f;
    [SerializeField, Tooltip("相手が操作できなくなる時間（秒）")]
    private float _stunDurationSeconds = 0.5f;
    [SerializeField, Min(0f), Tooltip("落下物を弾く横速度")]
    private float _obstacleHorizontalSpeed = 8f;
    [SerializeField, Min(0f), Tooltip("落下物を弾く上向きの速度")]
    private float _obstacleUpwardSpeed = 4f;

    private readonly HashSet<Rigidbody> _hitBodies = new();
    private Rigidbody _attacker;
    private PlayerAttack _playerAttack;

    private void Awake()
    {
        _attacker = GetComponentInParent<Rigidbody>();
        _playerAttack = GetComponentInParent<PlayerAttack>();
    }

    private void OnEnable()
    {
        _hitBodies.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody targetBody = other.attachedRigidbody;
        if (other.isTrigger || _attacker == null || targetBody == null ||
            targetBody == _attacker || targetBody.isKinematic)
        {
            return;
        }

        bool isPlayer = targetBody.TryGetComponent(out PlayerMove targetMovement);
        targetBody.TryGetComponent(out AttackableObstacle obstacle);
        if (!isPlayer && obstacle == null)
        {
            return;
        }

        if (isPlayer && targetBody.TryGetComponent(out PlayerDeath playerDeath) && playerDeath.IsRespawnProtected)
        {
            return;
        }

        if (!_hitBodies.Add(targetBody))
        {
            return;
        }

        float multiplier = _playerAttack != null ? _playerAttack.KnockbackMultiplier : 1f;
        if (!isPlayer)
        {
            LaunchObstacle(targetBody, obstacle, multiplier);
            OnHitConfirmed?.Invoke();
            return;
        }

        targetMovement.ApplyStun(_stunDurationSeconds);
        // 地上でも押し合いから離れるよう、吹き飛ぶ方向に上向きの成分を加える
        Vector3 direction = (targetBody.position - _attacker.position).normalized;
        Vector3 knockbackDirection = (direction + Vector3.up * 0.5f).normalized;
        targetBody.AddForce(knockbackDirection * (_knockbackForce * multiplier), ForceMode.Impulse);
        OnHitConfirmed?.Invoke();
    }

    /// <summary>
    ///     落下中でも攻撃者から離れる方向へ持ち上げる
    /// </summary>
    private void LaunchObstacle(Rigidbody body, AttackableObstacle obstacle, float multiplier)
    {
        float direction = body.position.x < _attacker.position.x ? -1f : 1f;
        Vector3 velocity = body.linearVelocity;
        velocity.x = direction * _obstacleHorizontalSpeed * multiplier;
        velocity.y = Mathf.Max(velocity.y, _obstacleUpwardSpeed * multiplier);
        obstacle.Launch(_attacker, velocity);
    }
}
