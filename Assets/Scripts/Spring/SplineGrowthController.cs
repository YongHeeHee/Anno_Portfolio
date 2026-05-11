using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SplineGrowthController : MonoBehaviour
{
    public static SplineGrowthController Instance { get; private set; }

    [Header("Growth Settings")]
    [Tooltip("단위 거리(1 unit)당 소모되는 MP")]
    [SerializeField] private float mpCostPerUnit = 10f;

    [Tooltip("Spline 포인트 사이의 최소 거리. 낮을수록 촘촘한 곡선")]
    [SerializeField] private float minPointDistance = 0.3f;

    [Tooltip("Ground 레이어 번호 (EdgeCollider2D에 설정)")]
    [SerializeField] private int groundLayerIndex = 6;

    [Tooltip("Spline의 두께 (Open Spline에서 시각적 굵기를 결정)")]
    [SerializeField] private float splineHeight = 0.5f;

    [Header("Shrink Settings")]
    [Tooltip("성장 완료 후 줄어들기 시작할 때까지 대기 시간(초)")]
    [SerializeField] private float shrinkDelay = 3f;

    [Tooltip("끝점에서 시작점까지 완전히 줄어드는 데 걸리는 시간(초)")]
    [SerializeField] private float shrinkDuration = 2f;

    [Header("SpriteShape")]
    [Tooltip("SpriteShapeController가 설정된 Prefab (Profile, Material, Open Ended 등 미리 설정)")]
    [SerializeField] private SpriteShapeController shapePrefab;

    [Header("Effects")]
    [Tooltip("Growth 시작 시 시전 위치에 생성되는 이펙트 Prefab")]
    [SerializeField] private GameObject growthStartEffectPrefab;

    [Tooltip("Growth 진행 중 마우스 끝점에 따라다니는 파티클 Prefab")]
    [SerializeField] private GameObject growthTrailEffectPrefab;

    [Tooltip("Growth 완료(마우스 릴리즈) 시 끝점에 생성되는 이펙트 Prefab")]
    [SerializeField] private GameObject growthEndEffectPrefab;

    [Header("References")]
    [Tooltip("PlayerBase 참조 (MP 소모용)")]
    [SerializeField] private PlayerBase player;

    public bool IsGrowing { get; private set; }

    private Camera mainCamera;
    private SpriteShapeController currentShape;
    private List<Vector2> splinePoints = new List<Vector2>();
    private GameObject currentTrailEffect;
    private GrowthInteractable currentOwner;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        mainCamera = Camera.main;
    }

    private SpriteShapeController GetShape()
    {
        SpriteShapeController shape = Instantiate(shapePrefab);
        shape.gameObject.SetActive(true);
        shape.enabled = true;

        // 프리팹에 캐시된 LocalAABB가 실제 동적 스플라인 위치와 어긋나 카메라
        // frustum culling으로 렌더러가 스킵되는 문제 방지 — 큰 bounds로 override
        SpriteShapeRenderer renderer = shape.GetComponent<SpriteShapeRenderer>();
        if (renderer != null)
            renderer.SetLocalAABB(new Bounds(Vector3.zero, Vector3.one * 10000f));

        return shape;
    }

    public void CancelGrowth()
    {
        if (!IsGrowing) return;

        IsGrowing = false;

        if (currentTrailEffect != null)
        {
            Destroy(currentTrailEffect);
            currentTrailEffect = null;
        }

        if (currentShape != null)
        {
            Destroy(currentShape.gameObject);
            currentShape = null;
        }

        currentOwner = null;
    }

    public void StartGrowth(Vector2 origin, GrowthInteractable owner = null)
    {
        if (player == null) return;

        if (player.CurrentMp <= 0f) return;

        // 호출자가 이미 활성 Vine을 보유 중이면 불가
        if (owner != null && owner.HasActiveVine) return;

        // 이전 Growth가 정리되지 않았으면 강제 취소
        if (IsGrowing)
            CancelGrowth();

        currentOwner = owner;

        IsGrowing = true;
        splinePoints.Clear();

        currentShape = GetShape();
        currentShape.gameObject.name = "Growth_Vine";
        currentShape.gameObject.transform.position = Vector3.zero;

        Spline spline = currentShape.spline;
        spline.Clear();
        spline.InsertPointAt(0, (Vector3)origin);
        spline.SetTangentMode(0, ShapeTangentMode.Continuous);
        spline.SetHeight(0, splineHeight);

        splinePoints.Add(origin);

        // 시작 이펙트
        SpawnEffect(growthStartEffectPrefab, origin);

        // 진행 중 트레일 파티클 생성
        if (growthTrailEffectPrefab != null)
        {
            currentTrailEffect = Instantiate(growthTrailEffectPrefab, (Vector3)origin, Quaternion.identity);
        }
    }

    private void Update()
    {
        if (!IsGrowing) return;

        // 시즌 전환 등으로 Growth 스킬이 비활성화되면 강제 종료
        if (SkillManager.Instance == null || !SkillManager.Instance.IsActiveSkill(SeasonSkillType.Growth))
        {
            CancelGrowth();
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            FinishGrowth();
            return;
        }

        Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lastPoint = splinePoints[splinePoints.Count - 1];
        float distance = Vector2.Distance(mouseWorld, lastPoint);

        // 트레일 이펙트를 마우스 위치로 이동
        if (currentTrailEffect != null)
            currentTrailEffect.transform.position = (Vector3)mouseWorld;

        if (distance >= minPointDistance)
        {
            float mpCost = distance * mpCostPerUnit;

            if (player.CurrentMp < mpCost)
            {
                FinishGrowth();
                return;
            }

            player.CurrentMp -= mpCost;
            AddSplinePoint(mouseWorld);
        }
    }

    private void AddSplinePoint(Vector2 point)
    {
        splinePoints.Add(point);

        Spline spline = currentShape.spline;
        int index = spline.GetPointCount();
        spline.InsertPointAt(index, (Vector3)point);
        spline.SetTangentMode(index, ShapeTangentMode.Continuous);
        spline.SetHeight(index, splineHeight);

        AutoCalculateTangent(spline, index);

        if (index >= 2)
            AutoCalculateTangent(spline, index - 1);

        // 포인트 추가 후 즉시 메시 갱신
        currentShape.BakeMesh();

        // BakeMesh가 AABB를 mesh geometry 기준으로 재계산하며 frustum culling을
        // 유발하는 stale bounds 문제를 일으킴 — 매번 큰 bounds로 재 override
        SpriteShapeRenderer renderer = currentShape.GetComponent<SpriteShapeRenderer>();
        if (renderer != null)
            renderer.SetLocalAABB(new Bounds(Vector3.zero, Vector3.one * 10000f));
    }

    private void AutoCalculateTangent(Spline spline, int index)
    {
        int count = spline.GetPointCount();
        if (count < 2) return;

        Vector3 prev = index > 0
            ? spline.GetPosition(index - 1)
            : spline.GetPosition(index);

        Vector3 next = index < count - 1
            ? spline.GetPosition(index + 1)
            : spline.GetPosition(index);

        Vector3 tangent = (next - prev) * 0.25f;

        spline.SetLeftTangent(index, -tangent);
        spline.SetRightTangent(index, tangent);
    }

    private IEnumerator ShrinkAndDestroy(SpriteShapeController shape, Vector2[] points, GrowthInteractable owner)
    {
        yield return new WaitForSeconds(shrinkDelay);

        EdgeCollider2D edge = shape.GetComponent<EdgeCollider2D>();
        int totalPoints = points.Length;
        float elapsed = 0f;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;

            // 끝에서부터 제거할 포인트 수 계산
            int remainCount = Mathf.Max(2, totalPoints - Mathf.FloorToInt(t * (totalPoints - 1)));

            Spline spline = shape.spline;
            // 현재 포인트가 남아야 할 수보다 많으면 끝에서 제거
            while (spline.GetPointCount() > remainCount)
            {
                spline.RemovePointAt(spline.GetPointCount() - 1);
            }

            // EdgeCollider도 동기화
            if (edge != null)
            {
                Vector2[] shrunkPoints = new Vector2[remainCount];
                System.Array.Copy(points, shrunkPoints, remainCount);
                edge.points = shrunkPoints;
            }

            yield return null;
        }

        GameObject vineObject = shape.gameObject;
        Destroy(vineObject);

        if (owner != null)
            owner.ClearActiveVine(vineObject);
    }

    private void FinishGrowth()
    {
        IsGrowing = false;

        // 트레일 이펙트 정리
        if (currentTrailEffect != null)
        {
            // ParticleSystem이 있으면 방출 중지 후 자연스럽게 소멸
            var ps = currentTrailEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop();
                Destroy(currentTrailEffect, ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(currentTrailEffect);
            }
            currentTrailEffect = null;
        }

        if (currentShape == null || splinePoints.Count < 2) return;

        // 완료 이펙트 (끝점에 생성)
        Vector2 endPoint = splinePoints[splinePoints.Count - 1];
        SpawnEffect(growthEndEffectPrefab, endPoint);

        EdgeCollider2D edge = currentShape.gameObject.AddComponent<EdgeCollider2D>();
        edge.points = splinePoints.ToArray();
        currentShape.gameObject.layer = groundLayerIndex;

        GameObject vineObject = currentShape.gameObject;
        if (currentOwner != null)
            currentOwner.SetActiveVine(vineObject);

        StartCoroutine(ShrinkAndDestroy(currentShape, splinePoints.ToArray(), currentOwner));

        currentShape = null;
        currentOwner = null;
    }

    private void SpawnEffect(GameObject prefab, Vector2 position)
    {
        if (prefab == null) return;
        Destroy(Instantiate(prefab, (Vector3)position, Quaternion.identity), 2f);
    }
}
