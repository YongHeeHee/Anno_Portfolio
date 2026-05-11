using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class FreezeInteractable : MonoBehaviour
{
    [Header("Freeze Visual")]
    [Tooltip("결빙 상태일 때 스프라이트에 적용할 틴트 색상")]
    [SerializeField] private Color freezeTint = new Color(0.6f, 0.85f, 1f, 1f);

    [Tooltip("복원 경고 깜박임이 시작되기까지 남은 시간 비율 (0.3 = 남은 시간 30%부터 깜박임)")]
    [SerializeField] private float blinkStartRatio = 0.3f;

    [Tooltip("깜박임 최소 간격(초). 복원 직전 가장 빠른 속도")]
    [SerializeField] private float minBlinkInterval = 0.08f;

    [Tooltip("깜박임 최대 간격(초). 깜박임 시작 시 가장 느린 속도")]
    [SerializeField] private float maxBlinkInterval = 0.4f;

    private Rigidbody2D rb;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private float originalGravityScale;
    private RigidbodyConstraints2D originalConstraints;
    private bool isFrozen;
    private Coroutine restoreCoroutine;

    public bool IsFrozen => isFrozen;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
        originalGravityScale = rb.gravityScale;
        originalConstraints = rb.constraints;
    }

    private void SetTint(Color c)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = c;
    }

    private void RestoreColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = originalColors[i];
    }

    private void OnMouseDown()
    {
        if (SkillManager.Instance == null || !SkillManager.Instance.IsActiveSkill(SeasonSkillType.Freeze)) return;
        if (FreezeController.Instance == null) return;
        if (isFrozen) return;

        FreezeController.Instance.TryFreeze(this);
    }

    public void ApplyFreeze(float duration)
    {
        isFrozen = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        SetTint(freezeTint);

        if (restoreCoroutine != null)
            StopCoroutine(restoreCoroutine);
        restoreCoroutine = StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        float blinkTime = duration * blinkStartRatio;
        yield return new WaitForSeconds(duration - blinkTime);

        float elapsed = 0f;
        bool showOriginal = false;

        while (elapsed < blinkTime)
        {
            float progress = elapsed / blinkTime;
            float interval = Mathf.Lerp(maxBlinkInterval, minBlinkInterval, progress);

            showOriginal = !showOriginal;
            if (showOriginal)
                RestoreColors();
            else
                SetTint(freezeTint);

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        Restore();
    }

    private void Restore()
    {
        rb.gravityScale = originalGravityScale;
        rb.constraints = originalConstraints;
        RestoreColors();
        isFrozen = false;
        restoreCoroutine = null;
    }
}
