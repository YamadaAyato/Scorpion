using UnityEngine;

/// <summary>
///     ステージPrefab内の試合開始地点、復活地点、背景画像を保持する
/// </summary>
[DisallowMultipleComponent]
public sealed class StageLayout : MonoBehaviour
{
    /// <summary> ステージ固有の背景画像を取得する </summary>
    public Texture2D BackgroundTexture => _backgroundTexture;

    [SerializeField, Tooltip("ステージ開始時に1Pを配置する地点")]
    private Transform _playerOneSpawn;
    [SerializeField, Tooltip("ステージ開始時に2Pを配置する地点")]
    private Transform _playerTwoSpawn;
    [SerializeField, Tooltip("落下後に1Pが復活する地点。未指定の場合は開始地点を使用する")]
    private Transform _playerOneRespawn;
    [SerializeField, Tooltip("落下後に2Pが復活する地点。未指定の場合は開始地点を使用する")]
    private Transform _playerTwoRespawn;
    [SerializeField, Tooltip("このステージで表示する背景画像。未指定の場合は共通背景を使用する")]
    private Texture2D _backgroundTexture;

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
