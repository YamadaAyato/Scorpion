using UnityEngine;

public class FallZone : MonoBehaviour
{
    [Header("プレイヤー設定")]
    [SerializeField] PlayerDeath[] players;

    [Header("落下設定")]
    [SerializeField] float fallHeight = -10f;

    void Update()
    {
        foreach (PlayerDeath player in players)
        {
            if (player == null) continue;

            // プレイヤーが落下したか確認
            if (player.transform.position.y < fallHeight)
            {
                player.Death();
            }
        }
    }
}