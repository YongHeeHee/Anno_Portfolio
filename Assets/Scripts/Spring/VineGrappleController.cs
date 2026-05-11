using UnityEngine;

public class VineGrappleController : MonoBehaviour
{
    public static VineGrappleController Instance { get; private set; }

    [Header("References")]
    [Tooltip("PlayerBase 참조 (스킬 체크, MP 소모용)")]
    [SerializeField] private PlayerBase player;

    [Header("Connection")]
    [Tooltip("앵커 검색 시 마우스 방향 Raycast 최대 거리. 지상에서 7칸, 점프 시 8칸 도달 기준")]
    [SerializeField] private float maxConnectDistance = 8f;

    [Tooltip("조준 보정 반경. 이 반경 안에 있는 앵커도 부착 (CircleCast)")]
    [SerializeField] private float aimRadius = 0.5f;

    [Tooltip("Raycast 대상 레이어. 앵커 레이어 + 시야 차단할 Ground/VineGround 레이어를 모두 포함")]
    [SerializeField] private LayerMask raycastLayerMask;

    [Header("Launch")]
    [Tooltip("발사 시 거리당 속도 (선형). 발사속도 = stretch × forcePerUnit")]
    [SerializeField] private float forcePerUnit = 4f;

    [Tooltip("이 미만의 stretch에서는 발사되지 않음 (실수 방지)")]
    [SerializeField] private float minStretchForLaunch = 0.5f;

    [Header("MP")]
    [Tooltip("연결 순간 차감되는 고정 MP 비용")]
    [SerializeField] private float mpCost = 25f;

    [Header("Visual")]
    [Tooltip("덩굴 시각화용 LineRenderer")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Debug")]
    [Tooltip("스킬이 활성일 때 Scene 뷰에 CircleCast 범위/길이/AimRadius를 Gizmos로 표시")]
    [SerializeField] private bool drawAimGizmos = true;

    [SerializeField] private Color gizmoRayColor = new Color(0.2f, 1f, 0.4f, 0.9f);
    [SerializeField] private Color gizmoRadiusColor = new Color(1f, 0.85f, 0.2f, 0.7f);
    [SerializeField] private Color gizmoBlockedColor = new Color(1f, 0.3f, 0.3f, 0.9f);

    private Camera mainCamera;
    private Rigidbody2D playerRb;
    private Collider2D playerCollider;
    private VineLaunchInteractable connectedAnchor;
    private bool isTethered;

    public bool IsTethered => isTethered;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            playerCollider = player.GetComponent<Collider2D>();
        }

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private Vector2 GetCastOrigin()
    {
        return playerCollider != null
            ? (Vector2)playerCollider.bounds.center
            : (Vector2)player.transform.position;
    }

    private void Update()
    {
        if (SkillManager.Instance == null || !SkillManager.Instance.IsActiveSkill(SeasonSkillType.VineGrapple))
        {
            if (isTethered) Disconnect(launch: false);
            return;
        }

        if (player == null) return;

        if (Input.GetMouseButtonDown(1) && !isTethered)
        {
            TryConnect();
        }
        else if (isTethered && Input.GetMouseButtonUp(1))
        {
            Disconnect(launch: true);
        }

        if (isTethered)
            UpdateLineRenderer();
    }

    private void TryConnect()
    {
        if (player.CurrentMp < mpCost) return;

        Vector2 castOrigin = GetCastOrigin();
        Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mouseWorld - castOrigin).normalized;

        // 플레이어 자신/지면과의 초기 겹침 회피: 마우스 방향으로 살짝 앞에서 캐스트 시작
        float castSkip = aimRadius + 0.1f;
        Vector2 castStart = castOrigin + direction * castSkip;

        RaycastHit2D hit = Physics2D.CircleCast(castStart, aimRadius, direction,
            maxConnectDistance - castSkip, raycastLayerMask);
        if (hit.collider == null) return;

        var anchor = hit.collider.GetComponent<VineLaunchInteractable>();
        if (anchor == null) return; // 앵커가 아닌 다른 콜라이더(Ground 등)에 차단됨

        player.CurrentMp -= mpCost;
        connectedAnchor = anchor;
        isTethered = true;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = true;
            UpdateLineRenderer();
        }
    }

    private void Disconnect(bool launch)
    {
        if (!isTethered) return;

        if (launch && connectedAnchor != null && playerRb != null)
        {
            Vector2 anchorPos = connectedAnchor.AnchorPosition;
            Vector2 playerPos = GetCastOrigin();
            float stretch = Vector2.Distance(playerPos, anchorPos);

            if (stretch >= minStretchForLaunch)
            {
                Vector2 launchDir = (anchorPos - playerPos).normalized;
                float launchSpeed = stretch * forcePerUnit;
                player.ApplyLaunchVelocity(launchDir * launchSpeed);
            }
        }

        isTethered = false;
        connectedAnchor = null;

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null || connectedAnchor == null) return;

        lineRenderer.SetPosition(0, (Vector3)GetCastOrigin());
        lineRenderer.SetPosition(1, (Vector3)connectedAnchor.AnchorPosition);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawAimGizmos) return;
        if (!Application.isPlaying) return;
        if (player == null) return;
        if (SkillManager.Instance == null || !SkillManager.Instance.IsActiveSkill(SeasonSkillType.VineGrapple)) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector2 castOrigin = GetCastOrigin();
        Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mouseWorld - castOrigin);
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        float castSkip = aimRadius + 0.1f;
        Vector2 castStart = castOrigin + direction * castSkip;
        float castDistance = maxConnectDistance - castSkip;
        Vector2 castEnd = castStart + direction * castDistance;

        // 차단 여부에 따라 색상 변경
        RaycastHit2D hit = Physics2D.CircleCast(castStart, aimRadius, direction, castDistance, raycastLayerMask);
        bool hasHit = hit.collider != null;
        bool isAnchorHit = hasHit && hit.collider.GetComponent<VineLaunchInteractable>() != null;
        Color rayColor = hasHit ? (isAnchorHit ? gizmoRayColor : gizmoBlockedColor) : gizmoRayColor;

        // 중심 Ray (origin → 최대 도달 지점)
        Gizmos.color = rayColor;
        Vector2 fullEnd = castOrigin + direction * maxConnectDistance;
        Gizmos.DrawLine(castOrigin, fullEnd);

        // 캐스트 시작/끝 AimRadius 원
        Gizmos.color = gizmoRadiusColor;
        DrawWireCircle(castStart, aimRadius);
        DrawWireCircle(castEnd, aimRadius);

        // 캡슐 측면 라인 (cast의 스윕 영역 경계)
        Vector2 perp = new Vector2(-direction.y, direction.x) * aimRadius;
        Gizmos.color = gizmoRadiusColor;
        Gizmos.DrawLine(castStart + perp, castEnd + perp);
        Gizmos.DrawLine(castStart - perp, castEnd - perp);

        // 실제 hit 지점 표시
        if (hasHit)
        {
            Gizmos.color = isAnchorHit ? gizmoRayColor : gizmoBlockedColor;
            DrawWireCircle(hit.point, 0.08f);
            Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.4f);
        }
    }

    private static void DrawWireCircle(Vector2 center, float radius, int segments = 32)
    {
        Vector3 prev = new Vector3(center.x + radius, center.y, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float t = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = new Vector3(center.x + Mathf.Cos(t) * radius, center.y + Mathf.Sin(t) * radius, 0f);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
