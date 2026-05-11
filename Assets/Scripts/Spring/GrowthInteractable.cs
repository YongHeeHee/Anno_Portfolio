using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GrowthInteractable : MonoBehaviour
{
    [Tooltip("성장 시작 위치 오프셋 (오브젝트 기준)")]
    [SerializeField] private Vector2 growthOriginOffset = Vector2.zero;

    private GameObject activeVine;

    public Vector2 GrowthOrigin => (Vector2)transform.position + growthOriginOffset;

    /// <summary>이 Interactable이 생성한 Vine이 아직 존재하는지 여부</summary>
    public bool HasActiveVine => activeVine != null;

    private void OnMouseDown()
    {
        if (SkillManager.Instance == null || !SkillManager.Instance.IsActiveSkill(SeasonSkillType.Growth)) return;
        if (SplineGrowthController.Instance == null) return;
        if (SplineGrowthController.Instance.IsGrowing) return;
        if (HasActiveVine) return;

        SplineGrowthController.Instance.StartGrowth(GrowthOrigin, this);
    }

    public void SetActiveVine(GameObject vine)
    {
        activeVine = vine;
    }

    public void ClearActiveVine(GameObject vine)
    {
        if (activeVine == vine) activeVine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GrowthOrigin, 0.15f);
    }
}
