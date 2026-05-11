using UnityEngine;

public class GhostController : MonoBehaviour
{
    public static GhostController Instance { get; private set; }

    [Header("References")]
    [Tooltip("PlayerBase 참조 (스킬 체크, MP 소모용)")]
    [SerializeField] private PlayerBase player;

    [Header("Ghost Settings")]
    [Tooltip("고스트 상태의 플레이어 알파값 (0~1). 낮을수록 투명")]
    [SerializeField] private float ghostAlpha = 0.4f;

    [Tooltip("고스트 상태 유지 중 초당 소모 MP")]
    [SerializeField] private float mpCostPerSecond = 10f;

    [Header("Layer")]
    [Tooltip("통과 가능한 벽 레이어 (GhostWall 타일맵에 설정된 레이어 선택)")]
    [SerializeField] private LayerMask ghostWallLayer;

    private bool isGhostActive;
    private Vector2 safePosition;
    private SpriteRenderer playerSprite;
    private CapsuleCollider2D playerCollider;
    private int playerLayerIndex;
    private int ghostWallLayerIndex;

    public bool IsGhostActive => isGhostActive;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (player != null)
        {
            playerSprite = player.GetComponent<SpriteRenderer>();
            playerCollider = player.GetComponent<CapsuleCollider2D>();
            playerLayerIndex = player.gameObject.layer;
        }

        ghostWallLayerIndex = LayerMaskToIndex(ghostWallLayer);
    }

    private void Update()
    {
        if (SkillManager.Instance == null || !SkillManager.Instance.IsActiveSkill(SeasonSkillType.Ghost))
        {
            if (isGhostActive) DeactivateGhost();
            return;
        }

        if (player == null) return;

        if (Input.GetMouseButtonDown(1) && !isGhostActive)
        {
            ActivateGhost();
        }
        else if (isGhostActive && Input.GetMouseButtonUp(1))
        {
            DeactivateGhost();
        }

        if (isGhostActive)
        {
            float mpCost = mpCostPerSecond * Time.deltaTime;
            if (player.CurrentMp < mpCost)
            {
                DeactivateGhost();
                return;
            }

            player.CurrentMp -= mpCost;

            if (!IsInsideGhostWall())
                safePosition = player.transform.position;
        }
    }

    private void ActivateGhost()
    {
        if (player.CurrentMp <= 0) return;

        isGhostActive = true;
        safePosition = player.transform.position;

        SetPlayerAlpha(ghostAlpha);
        Physics2D.IgnoreLayerCollision(playerLayerIndex, ghostWallLayerIndex, true);
    }

    public void DeactivateGhost()
    {
        isGhostActive = false;

        Physics2D.IgnoreLayerCollision(playerLayerIndex, ghostWallLayerIndex, false);
        SetPlayerAlpha(1f);

        if (IsInsideGhostWall())
            player.transform.position = (Vector3)safePosition;
    }

    private void SetPlayerAlpha(float alpha)
    {
        if (playerSprite == null) return;

        Color color = playerSprite.color;
        color.a = alpha;
        playerSprite.color = color;
    }

    private bool IsInsideGhostWall()
    {
        if (playerCollider == null) return false;

        Vector2 center = (Vector2)player.transform.position + playerCollider.offset;
        Vector2 size = playerCollider.size * 0.9f;
        CapsuleDirection2D direction = playerCollider.direction;

        Collider2D hit = Physics2D.OverlapCapsule(center, size, direction, 0f, ghostWallLayer);
        return hit != null;
    }

    private static int LayerMaskToIndex(LayerMask mask)
    {
        int value = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((value & (1 << i)) != 0) return i;
        }
        return 0;
    }
}
