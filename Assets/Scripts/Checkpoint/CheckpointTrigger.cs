using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckpointTrigger : MonoBehaviour
{
    [Tooltip("Player 태그")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("체크포인트 등록 위치 오프셋 (오브젝트 기준). 리스폰 시 이 위치로 이동")]
    [SerializeField] private Vector2 registerOffset = Vector2.zero;

    [Tooltip("활성화 시 재생할 시그널 사운드 (선택)")]
    [SerializeField] private AudioClip activationSound;

    [Tooltip("활성화 시 켤 시각 오브젝트 (깃발 등, 선택)")]
    [SerializeField] private GameObject activatedVisual;

    private bool activated;

    public bool IsActivated => activated;
    public Vector3 RegisterPosition => (Vector2)transform.position + registerOffset;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        if (activatedVisual != null) activatedVisual.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;
        if (!other.CompareTag(playerTag)) return;
        if (CheckpointManager.Instance == null) return;

        activated = true;
        CheckpointManager.Instance.RegisterCheckpoint(RegisterPosition);

        if (activationSound != null)
            AudioSource.PlayClipAtPoint(activationSound, transform.position);

        if (activatedVisual != null) activatedVisual.SetActive(true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(RegisterPosition, 0.25f);
    }
}
