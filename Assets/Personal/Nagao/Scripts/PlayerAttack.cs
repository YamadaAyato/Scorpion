using UnityEngine;
using System;
using System.Threading;
using System.Collections;
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] float attackPower=10;
    [SerializeField] GameObject attackHitBox;
    [SerializeField] float attackDuration = 0.2f;
    public float coolTime = 3;

    private Coroutine attackRoutine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        attackHitBox.SetActive(false);
    }
    public void Attack(GameObject target)
    {
        if(attackRoutine != null)return;
        attackRoutine = StartCoroutine(AttackCoroutine());
    }
    IEnumerator AttackCoroutine()
    {
        // 1. 攻撃判定をONにする
        if (attackHitBox != null)
        {
            attackHitBox.SetActive(true);
        }

        // 2. 攻撃の持続時間だけ待つ
        yield return new WaitForSeconds(attackDuration);

        // 3. 攻撃判定をOFFにする
        if (attackHitBox != null)
        {
            attackHitBox.SetActive(false);
        }
        //クールタイムを待つ
        yield return new WaitForSeconds(coolTime);

        // 5. クールタイムが終わったら次の攻撃を許可する
        attackRoutine = null;
    }
}
