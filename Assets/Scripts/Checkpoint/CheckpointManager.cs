using System;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("체크포인트가 없을 때 fallback으로 사용할 SpawnPoint 태그")]
    [SerializeField] private string spawnPointTag = "SpawnPoint";

    [Tooltip("Player 오브젝트를 찾을 때 사용할 태그")]
    [SerializeField] private string playerTag = "Player";

    private Vector3 currentCheckpointPos;
    private bool hasCheckpoint;

    public bool HasCheckpoint => hasCheckpoint;
    public Vector3 CurrentCheckpointPos => currentCheckpointPos;

    /// <summary>
    /// 리스폰 직후 호출됨. IResettable 구현 오브젝트가 구독하여 상태를 초기 위치로 복원.
    /// </summary>
    public event Action OnRespawn;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterCheckpoint(Vector3 pos)
    {
        currentCheckpointPos = pos;
        hasCheckpoint = true;
    }

    public void RespawnPlayer(PlayerBase player)
    {
        if (player == null) return;

        DeactivateActiveSkills();

        player.transform.position = hasCheckpoint ? currentCheckpointPos : FindSpawnPoint(player);
        player.CurrentMp = player.MaxMp;

        OnRespawn?.Invoke();
    }

    private void DeactivateActiveSkills()
    {
        if (GhostController.Instance != null && GhostController.Instance.IsGhostActive)
            GhostController.Instance.DeactivateGhost();

        if (IgniteController.Instance != null && IgniteController.Instance.IsIgnited)
            IgniteController.Instance.DeactivateIgnite();

        if (AnchorController.Instance != null && AnchorController.Instance.IsAnchored)
            AnchorController.Instance.DeactivateAnchor();
    }

    private Vector3 FindSpawnPoint(PlayerBase player)
    {
        GameObject spawn = GameObject.FindGameObjectWithTag(spawnPointTag);
        if (spawn != null) return spawn.transform.position;
        return player.transform.position;
    }
}
