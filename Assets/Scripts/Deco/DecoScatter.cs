using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 빈 GameObject에 부착해, Width 폭 안에 프리셋의 프리팹들을 무작위로 흩뿌린다.
/// Width / Seed / Preset 변경 시 에디터에서 자동으로 자식들이 재생성된다.
/// 자식들은 씬에 그대로 저장되며, 플레이 모드 진입 시 재생성되지 않는다.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class DecoScatter : MonoBehaviour
{
    [Tooltip("배치 후보 묶음 프리셋")]
    public DecoScatterPreset preset;

    [Tooltip("배치 영역 폭(유닛). 부모의 localScale은 그대로 유지하기 위해 별도 필드로 관리.")]
    [Min(0f)]
    public float width = 5f;

    [Tooltip("같은 시드는 같은 배치 결과를 보장한다.")]
    public int seed = 0;

    [Tooltip("Inspector 값 변경 시 자동으로 재생성할지 여부. 미세 조정 중에는 끄는 것이 안전.")]
    public bool autoRegenerate = true;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!autoRegenerate) return;
        EditorApplication.delayCall += DelayedRegenerate;
    }

    private void DelayedRegenerate()
    {
        if (this == null) return;
        if (Application.isPlaying) return;
        Regenerate();
    }
#endif

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        Clear();

        if (preset == null || preset.prefabs == null || preset.prefabs.Count == 0) return;
        if (width <= 0f || preset.density <= 0f) return;

        float totalWeight = 0f;
        foreach (var e in preset.prefabs)
        {
            if (e.prefab == null) continue;
            totalWeight += Mathf.Max(0f, e.weight);
        }
        if (totalWeight <= 0f) return;

        int count = Mathf.Max(0, Mathf.RoundToInt(width * preset.density));
        if (count == 0) return;

        var rng = new System.Random(seed);
        float segmentWidth = width / count;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = PickWeighted(rng, totalWeight);
            if (prefab == null) continue;

            // Stratified sampling: 폭을 count개의 균등 구간으로 나누고, 각 구간 안에서만 jitter.
            // 이렇게 하면 시드/width와 무관하게 항상 균등 분포가 보장된다.
            float segmentCenter = -width * 0.5f + (i + 0.5f) * segmentWidth;
            float xJitter = ((float)rng.NextDouble() - 0.5f) * segmentWidth;
            float x = segmentCenter + xJitter;

            float y = ((float)rng.NextDouble() * 2f - 1f) * preset.yJitter;
            float scale = 1f + ((float)rng.NextDouble() * 2f - 1f) * preset.scaleVariance;
            bool flip = rng.NextDouble() < preset.flipChance;

            GameObject child = InstantiateChild(prefab);
            child.transform.localPosition = new Vector3(x, y, 0f);
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = new Vector3(flip ? -scale : scale, scale, 1f);
        }
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private GameObject PickWeighted(System.Random rng, float totalWeight)
    {
        float pick = (float)(rng.NextDouble() * totalWeight);
        foreach (var e in preset.prefabs)
        {
            if (e.prefab == null) continue;
            float w = Mathf.Max(0f, e.weight);
            pick -= w;
            if (pick <= 0f) return e.prefab;
        }
        return null;
    }

    private GameObject InstantiateChild(GameObject prefab)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            if (instance != null) return instance;
        }
#endif
        return Instantiate(prefab, transform);
    }

    private void OnDrawGizmosSelected()
    {
        float h = preset != null ? preset.yJitter * 2f + 0.2f : 0.2f;
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.5f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, h, 0f));
    }
}
