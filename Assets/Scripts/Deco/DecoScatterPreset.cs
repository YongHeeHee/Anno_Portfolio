using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DecoScatter가 사용하는 데코 묶음 프리셋.
/// 잔디·돌 등 같은 분위기의 프리팹들과 분포 파라미터를 한 곳에 모아둔다.
/// 여러 DecoScatter 인스턴스가 같은 프리셋을 공유하므로,
/// 프리셋만 바꾸면 모든 배치 지점에 반영된다.
/// </summary>
[CreateAssetMenu(fileName = "Preset_Deco", menuName = "Anno/Deco Scatter Preset")]
public class DecoScatterPreset : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        [Tooltip("배치 후보 프리팹")]
        public GameObject prefab;

        [Tooltip("선택 가중치 (높을수록 자주 선택됨)")]
        [Min(0f)]
        public float weight;
    }

    [Tooltip("배치 후보 프리팹 리스트")]
    public List<Entry> prefabs = new();

    [Tooltip("단위 폭(1유닛)당 배치되는 평균 개수")]
    [Min(0f)]
    public float density = 3f;

    [Tooltip("Y축 위아래 흔들림 (±)")]
    [Min(0f)]
    public float yJitter = 0f;

    [Tooltip("개별 스케일 변형 범위 (1 ± value). 0이면 변형 없음.")]
    [Range(0f, 0.5f)]
    public float scaleVariance = 0.1f;

    [Tooltip("좌우 반전 확률 (0=반전 없음, 1=항상 반전)")]
    [Range(0f, 1f)]
    public float flipChance = 0.5f;
}
