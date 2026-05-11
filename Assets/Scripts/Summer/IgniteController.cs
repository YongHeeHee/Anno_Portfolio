using UnityEngine;

public class IgniteController : MonoBehaviour
{
    public static IgniteController Instance { get; private set; }

    [Header("References")]
    [Tooltip("PlayerBase 참조 (스킬 체크, MP 소모용)")]
    [SerializeField] private PlayerBase player;

    [Header("Ignite Settings")]
    [Tooltip("활성 시 이동속도 배율 (1 = 기본, 2 = 2배)")]
    [SerializeField] private float speedMultiplier = 2f;

    [Tooltip("활성 시 점프력 배율 (1 = 기본, 1.5 = 1.5배)")]
    [SerializeField] private float jumpMultiplier = 1.5f;

    [Tooltip("활성 시 스프라이트에 적용할 틴트 색상")]
    [SerializeField] private Color igniteTint = new Color(1f, 0.5f, 0.2f, 1f);

    [Header("MP")]
    [Tooltip("점화 상태 유지 중 초당 소모 MP")]
    [SerializeField] private float mpCostPerSecond = 12f;

    private bool isIgnited;
    private SpriteRenderer playerSprite;
    private Color originalColor;

    public bool IsIgnited => isIgnited;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (player != null)
            playerSprite = player.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (SkillManager.Instance == null || !SkillManager.Instance.IsActiveSkill(SeasonSkillType.Ignite))
        {
            if (isIgnited) DeactivateIgnite();
            return;
        }

        if (player == null) return;

        if (Input.GetMouseButtonDown(1) && !isIgnited)
        {
            ActivateIgnite();
        }
        else if (isIgnited && Input.GetMouseButtonUp(1))
        {
            DeactivateIgnite();
        }

        if (isIgnited)
        {
            float mpCost = mpCostPerSecond * Time.deltaTime;
            if (player.CurrentMp < mpCost)
            {
                DeactivateIgnite();
                return;
            }

            player.CurrentMp -= mpCost;
        }
    }

    private void ActivateIgnite()
    {
        if (player.CurrentMp <= 0) return;

        isIgnited = true;

        player.SpeedMultiplier = speedMultiplier;
        player.JumpMultiplier = jumpMultiplier;

        if (playerSprite != null)
        {
            originalColor = playerSprite.color;
            playerSprite.color = igniteTint;
        }
    }

    public void DeactivateIgnite()
    {
        isIgnited = false;

        player.SpeedMultiplier = 1f;
        player.JumpMultiplier = 1f;

        if (playerSprite != null)
            playerSprite.color = originalColor;
    }
}
