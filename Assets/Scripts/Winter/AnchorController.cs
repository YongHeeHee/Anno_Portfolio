using UnityEngine;

public class AnchorController : MonoBehaviour
{
    public static AnchorController Instance { get; private set; }

    [Header("References")]
    [Tooltip("PlayerBase 참조 (스킬 체크, MP 소모용)")]
    [SerializeField] private PlayerBase player;

    [Header("Anchor Settings")]
    [Tooltip("앵커 상태일 때 스프라이트에 적용할 틴트 색상")]
    [SerializeField] private Color anchorTint = new Color(0.6f, 0.85f, 1f, 1f);

    [Header("MP")]
    [Tooltip("앵커 상태 유지 중 초당 소모 MP")]
    [SerializeField] private float mpCostPerSecond = 10f;

    private Rigidbody2D playerRb;
    private SpriteRenderer playerSprite;
    private bool isAnchored;
    private float originalGravityScale;
    private RigidbodyConstraints2D originalConstraints;
    private Color originalColor;

    public bool IsAnchored => isAnchored;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            playerSprite = player.GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (SkillManager.Instance == null || !SkillManager.Instance.IsActiveSkill(SeasonSkillType.Anchor))
        {
            if (isAnchored) DeactivateAnchor();
            return;
        }

        if (player == null) return;

        if (Input.GetMouseButtonDown(1) && !isAnchored)
        {
            Activate();
        }
        else if (isAnchored && Input.GetMouseButtonUp(1))
        {
            DeactivateAnchor();
        }

        if (isAnchored)
        {
            float mpCost = mpCostPerSecond * Time.deltaTime;
            if (player.CurrentMp < mpCost)
            {
                DeactivateAnchor();
                return;
            }

            player.CurrentMp -= mpCost;
        }
    }

    private void Activate()
    {
        if (player.CurrentMp <= 0) return;

        isAnchored = true;

        originalGravityScale = playerRb.gravityScale;
        originalConstraints = playerRb.constraints;

        playerRb.linearVelocity = Vector2.zero;
        playerRb.gravityScale = 0f;
        playerRb.constraints = RigidbodyConstraints2D.FreezeAll;

        if (playerSprite != null)
        {
            originalColor = playerSprite.color;
            playerSprite.color = anchorTint;
        }
    }

    public void DeactivateAnchor()
    {
        isAnchored = false;

        playerRb.gravityScale = originalGravityScale;
        playerRb.constraints = originalConstraints;

        if (playerSprite != null)
            playerSprite.color = originalColor;
    }
}
