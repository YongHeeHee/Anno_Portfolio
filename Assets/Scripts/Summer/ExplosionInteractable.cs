using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ExplosionInteractable : MonoBehaviour, IResettable
{
    [Header("Physics")]
    [Tooltip("비행 중 적용되는 중력 배율")]
    [SerializeField] private float flyGravityScale = 3f;

    [Tooltip("Ground 레이어 번호")]
    [SerializeField] private int groundLayerIndex = 6;

    [Header("Landing")]
    [Tooltip("착지 후 파괴까지 대기 시간(초). 0이면 파괴하지 않음")]
    [SerializeField] private float destroyDelay = 0f;

    private Rigidbody2D rb;
    private float initialGravityScale;
    private bool isFlying;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public bool IsFlying => isFlying;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialGravityScale = rb.gravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        gameObject.layer = groundLayerIndex;

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Start()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.OnRespawn += ResetState;
    }

    private void OnDestroy()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.OnRespawn -= ResetState;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = initialGravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        isFlying = false;
    }

    public void Launch(Vector2 force)
    {
        if (isFlying) return;

        isFlying = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = flyGravityScale;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isFlying) return;
        if (collision.gameObject.GetComponent<ExplosionInteractable>() != null) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                Land();
                return;
            }
        }
    }

    private void Land()
    {
        isFlying = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = initialGravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        if (destroyDelay > 0f)
            StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}
