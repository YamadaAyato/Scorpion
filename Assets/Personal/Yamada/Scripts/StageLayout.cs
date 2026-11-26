using UnityEngine;

/// <summary>
///     ステージPrefab内の試合開始地点と復活地点を保持する
/// </summary>
[DisallowMultipleComponent]
public sealed class StageLayout : MonoBehaviour
{
    [SerializeField, Tooltip("ステージ開始時に1Pを配置する地点")]
    private Transform _playerOneSpawn;
    [SerializeField, Tooltip("ステージ開始時に2Pを配置する地点")]
    private Transform _playerTwoSpawn;
    [SerializeField, Tooltip("落下後に1Pが復活する地点。未指定の場合は開始地点を使用する")]
    private Transform _playerOneRespawn;
    [SerializeField, Tooltip("落下後に2Pが復活する地点。未指定の場合は開始地点を使用する")]
    private Transform _playerTwoRespawn;

    /// <summary>
    ///     プレイヤーと配置の用途に対応する地点を取得する
    /// </summary>
    public Transform GetSpawnPoint(int playerIndex, bool isRespawn)
    {
        Transform spawn = playerIndex == 1 ? _playerOneSpawn : _playerTwoSpawn;
        Transform respawn = playerIndex == 1 ? _playerOneRespawn : _playerTwoRespawn;
        return isRespawn && respawn != null ? respawn : spawn;
    }
}
