using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
///     高さのしきい値を下回ったプレイヤーに落下を通知する
/// </summary>
public class FallZone : MonoBehaviour
{
    [FormerlySerializedAs("players")]
    [SerializeField, Tooltip("落下を監視するプレイヤー")]
    private PlayerDeath[] _players;

    [FormerlySerializedAs("fallHeight")]
    [SerializeField, Tooltip("この高さを下回ると落下として扱う")]
    private float _fallHeight = -10f;

    private void Update()
    {
        foreach (PlayerDeath player in _players)
        {
            if (player != null && player.transform.position.y < _fallHeight)
            {
                player.Death();
            }
        }
    }
}
