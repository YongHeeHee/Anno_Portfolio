using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Animator))]
public abstract class PlayerBase : CharacterBase, IMovable
{
    [Header("Movement")]
    [Tooltip("플레이어 이동 속도")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("점프 시 적용되는 수직 속도")]
    [SerializeField] private float jumpForce = 12f;

    [Header("Physics")]
    [Tooltip("플레이어에 적용되는 중력 배율")]
    [SerializeField] private float gravityScale = 3f;

    [Header("Mana")]
    [Tooltip("최대 MP")]
    [SerializeField] private float maxMp = 100f;

    [Tooltip("MP 자동 회복 간격(초)")]
    [SerializeField] private float mpRegenInterval = 1f;

    [Tooltip("회복 간격마다 충전되는 MP 양")]
    [SerializeField] private float mpRegenAmount = 5f;

    [Header("MP Regen Delay")]
    [Tooltip("MP 소모 후 회복이 시작되기까지의 대기 시간(초)")]
    [SerializeField] private float mpRegenDelay = 2f;

    [Header("Ground Check")]
    [Tooltip("착지 판정에 사용할 레이어 마스크 (Ground 레이어 선택)")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("착지 판정 OverlapBox의 중심 오프셋 (캐릭터 기준)")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, 0f);

    [Tooltip("착지 판정 OverlapBox의 크기")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);

    [Header("Effects")]
    [Tooltip("점프 시 발밑에 생성되는 이펙트 Prefab")]
    [SerializeField] private GameObject jumpEffectPrefab;

    [Tooltip("착지 시 발밑에 생성되는 이펙트 Prefab")]
    [SerializeField] private GameObject landEffectPrefab;

    [Tooltip("이펙트 생성 위치 오프셋 (캐릭터 발밑 기준)")]
    [SerializeField] private Vector2 effectOffset = new Vector2(0f, -0.5f);

    [Header("Death")]
    [Tooltip("환경 위험 오브젝트에 부여하는 태그")]
    [SerializeField] private string hazardTag = "Hazard";

    [Tooltip("Death 애니메이션 재생 후 리스폰까지 대기 시간(초)")]
    [SerializeField] private float respawnDelay = 1.2f;

    [Header("Rest")]
    [Tooltip("입력이 없을 때 휴식 애니메이션이 시작되기까지의 대기 시간(초)")]
    [SerializeField] private float restDelay = 5f;

    [Header("Launch")]
    [Tooltip("외부 발사(VineGrapple 슬링샷 등) 후 X 속도가 유지되는 최대 시간(초). 그 전에 착지하면 즉시 해제")]
    [SerializeField] private float launchOverrideDuration = 3f;

    protected Rigidbody2D rb;
    protected Animator animator;

    public bool InputLocked { get; set; }
    public bool MovementLocked { get; set; }
    public bool IsLaunched { get; private set; }

    protected float moveInput;
    private bool jumpRequested;
    private bool isGroundedField;
    private bool wasGroundedLastFrame;
    private float idleTimer;
    private float mpRegenTimer;
    private float mpRegenDelayTimer;
    private float currentMp;
    private float launchTimer;

    public float SpeedMultiplier { get; set; } = 1f;
    public float JumpMultiplier { get; set; } = 1f;

    public float CurrentMp
    {
        get => currentMp;
        set
        {
            float clamped = Mathf.Clamp(value, 0f, maxMp);
            if (clamped < currentMp)
                mpRegenDelayTimer = mpRegenDelay;
            currentMp = clamped;
        }
    }

    public float MaxMp => maxMp;
    public float JumpForce => jumpForce * JumpMultiplier;
    public bool IsGrounded => isGroundedField;
    public Vector2 GroundCheckOffset => groundCheckOffset;
    public Vector2 GroundCheckSize => groundCheckSize;

    protected static readonly int SpeedHash = Animator.StringToHash("Speed");
    protected static readonly int VelocityYHash = Animator.StringToHash("VelocityY");
    protected static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    protected static readonly int RestHash = Animator.StringToHash("Rest");
    protected static readonly int DeathHash = Animator.StringToHash("Death");
    protected static readonly int RespawnHash = Animator.StringToHash("Respawn");

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = gravityScale;
        currentMp = maxMp;
    }

    // --- IMovable ---

    public void SetMoveInput(float input)
    {
        moveInput = input;
    }

    public void RequestJump()
    {
        if (isGroundedField && !IsMovementBlocked())
            jumpRequested = true;
    }

    // --- Public API ---

    public bool IsLocked()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsTag("Lock");
    }

    public void ResetIdleTimer()
    {
        idleTimer = 0f;
        animator.SetBool(RestHash, false);
    }

    /// <summary>
    /// 외부 발사 (VineGrapple 슬링샷 등). X 입력으로 인한 속도 덮어쓰기를 차단하여 모멘텀 보존.
    /// 착지하거나 launchOverrideDuration 경과 시 자동 해제.
    /// </summary>
    public void ApplyLaunchVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        IsLaunched = true;
        launchTimer = launchOverrideDuration;
    }

    // --- Unity Lifecycle ---

    private void Update()
    {
        if (IsDead || InputLocked)
        {
            moveInput = 0f;
            return;
        }

        ReadInput();
        RegenMp();

        bool hasInput = Mathf.Abs(moveInput) > 0.1f || jumpRequested;
        HandleRest(hasInput);

        // Flip sprite
        if (moveInput != 0 && !IsMovementBlocked())
            spriteRenderer.flipX = moveInput < 0;

        // Animator parameters
        animator.SetFloat(SpeedHash, Mathf.Abs(moveInput));
        animator.SetFloat(VelocityYHash, rb.linearVelocity.y);
        animator.SetBool(IsGroundedHash, isGroundedField);
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        Collider2D groundCollider = Physics2D.OverlapBox(
            (Vector2)transform.position + groundCheckOffset,
            groundCheckSize, 0f, groundLayer);
        isGroundedField = groundCollider != null;

        // 착지 감지: 공중 → 지면
        bool justLanded = isGroundedField && !wasGroundedLastFrame;
        if (justLanded)
            SpawnEffect(landEffectPrefab);
        wasGroundedLastFrame = isGroundedField;

        // 발사 상태 갱신: 착지 또는 시간 초과 시 해제
        if (IsLaunched)
        {
            launchTimer -= Time.fixedDeltaTime;
            if (justLanded || launchTimer <= 0f)
                IsLaunched = false;
        }

        Vector2 platformVelocity = Vector2.zero;
        if (groundCollider != null && groundCollider.attachedRigidbody != null)
            platformVelocity = groundCollider.attachedRigidbody.linearVelocity;

        if (InputLocked)
        {
            rb.linearVelocity = new Vector2(platformVelocity.x, rb.linearVelocity.y + platformVelocity.y);
            return;
        }

        if (IsLaunched)
        {
            // 발사 중: X 속도 보존 (입력 무시), 플랫폼 속도만 추가
            rb.linearVelocity = new Vector2(rb.linearVelocity.x + platformVelocity.x, rb.linearVelocity.y + platformVelocity.y);
        }
        else
        {
            float speed = IsMovementBlocked() ? 0f : moveInput * moveSpeed * SpeedMultiplier;
            rb.linearVelocity = new Vector2(speed + platformVelocity.x, rb.linearVelocity.y + platformVelocity.y);
        }

        if (jumpRequested && isGroundedField)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * JumpMultiplier);
            SpawnEffect(jumpEffectPrefab);
        }
        jumpRequested = false;
    }

    public override void Die()
    {
        if (IsDead) return;
        IsDead = true;
        InputLocked = true;
        animator.SetTrigger(DeathHash);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        StartCoroutine(RespawnRoutine());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDead) return;
        if (!string.IsNullOrEmpty(hazardTag) && other.CompareTag(hazardTag))
            Die();
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScale;
        rb.linearVelocity = Vector2.zero;

        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.RespawnPlayer(this);
        else
            CurrentMp = maxMp;

        animator.ResetTrigger(DeathHash);
        animator.SetTrigger(RespawnHash);
        animator.Play("Idle", 0, 0f);

        IsDead = false;
        InputLocked = false;
    }

    // --- Internal ---

    private void SpawnEffect(GameObject prefab)
    {
        if (prefab == null) return;
        Vector3 pos = transform.position + (Vector3)effectOffset;
        Destroy(Instantiate(prefab, pos, Quaternion.identity), 2f);
    }

    private bool IsMovementBlocked() => MovementLocked || IsLocked();

    private void HandleRest(bool hasInput)
    {
        if (hasInput)
        {
            idleTimer = 0f;
            animator.SetBool(RestHash, false);
            return;
        }

        if (isGroundedField && !IsMovementBlocked())
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= restDelay)
                animator.SetBool(RestHash, true);
        }
        else
        {
            idleTimer = 0f;
        }
    }

    private void RegenMp()
    {
        if (currentMp >= maxMp) return;

        if (mpRegenDelayTimer > 0f)
        {
            mpRegenDelayTimer -= Time.deltaTime;
            mpRegenTimer = 0f;
            return;
        }

        mpRegenTimer += Time.deltaTime;
        if (mpRegenTimer >= mpRegenInterval)
        {
            mpRegenTimer = 0f;
            currentMp = Mathf.Min(currentMp + mpRegenAmount, maxMp);
        }
    }

    /// <summary>
    /// Subclass reads input and calls SetMoveInput() / RequestJump().
    /// </summary>
    protected abstract void ReadInput();

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + (Vector3)groundCheckOffset;
        Vector3 size = new Vector3(groundCheckSize.x, groundCheckSize.y, 0f);

        Gizmos.color = Application.isPlaying && isGroundedField
            ? new Color(0f, 1f, 0f, 0.5f)
            : new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = Application.isPlaying && isGroundedField ? Color.green : Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}
