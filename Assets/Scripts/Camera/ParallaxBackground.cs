using UnityEngine;

/// <summary>
/// 카메라 이동에 따라 배경 레이어별로 다른 속도로 스크롤하여 원근감을 표현합니다.
/// 각 시즌 씬의 배경 부모 오브젝트에 부착합니다.
/// infiniteScroll이 활성화된 레이어는 좌/중앙/우 3장으로 무한 반복합니다.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public struct ParallaxLayer
    {
        [Tooltip("배경 SpriteRenderer가 있는 오브젝트")]
        public Transform target;

        [Tooltip("X축 Parallax 비율 (0=카메라에 완전 고정, 1=월드에 고정)")]
        [Range(0f, 1f)]
        public float parallaxFactorX;

        [Tooltip("Y축 Parallax 비율 (0=카메라에 완전 고정, 1=월드에 고정)")]
        [Range(0f, 1f)]
        public float parallaxFactorY;

        [Tooltip("카메라가 시작 Y보다 아래로 이동할 때 적용되는 Y Parallax 비율. " +
                 "0이면 카메라와 완전 동행하여 하단 배경 공백을 방지.")]
        [Range(0f, 1f)]
        public float parallaxFactorYDown;

        [Tooltip("무한 스크롤 활성화 (숲 등 반복 가능한 레이어)")]
        public bool infiniteScroll;
    }

    [Header("Parallax Layers")]
    [Tooltip("배경 레이어 목록 (뒤→앞 순서로 등록)")]
    [SerializeField] private ParallaxLayer[] layers;

    private Transform cam;
    private Vector3 camStartPosition;
    private Vector3[] startPositions;

    // 무한 스크롤용: 좌/우 복제본과 스프라이트 너비
    private Transform[] leftClones;
    private Transform[] rightClones;
    private float[] spriteWidths;

    private void Start()
    {
        cam = Camera.main.transform;
        camStartPosition = cam.position;

        int count = layers.Length;
        startPositions = new Vector3[count];
        leftClones = new Transform[count];
        rightClones = new Transform[count];
        spriteWidths = new float[count];

        for (int i = 0; i < count; i++)
        {
            if (layers[i].target == null) continue;

            startPositions[i] = layers[i].target.position;

            if (layers[i].infiniteScroll)
                SetupInfiniteScroll(i);
        }
    }

    private void SetupInfiniteScroll(int index)
    {
        SpriteRenderer sr = layers[index].target.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        float width = sr.bounds.size.x;
        spriteWidths[index] = width;
        Transform parent = layers[index].target.parent;
        Vector3 pos = layers[index].target.position;

        // 왼쪽 복제본
        GameObject left = Instantiate(
            layers[index].target.gameObject,
            pos + Vector3.left * width,
            Quaternion.identity,
            parent
        );
        left.name = layers[index].target.name + "_Left";
        leftClones[index] = left.transform;

        // 오른쪽 복제본
        GameObject right = Instantiate(
            layers[index].target.gameObject,
            pos + Vector3.right * width,
            Quaternion.identity,
            parent
        );
        right.name = layers[index].target.name + "_Right";
        rightClones[index] = right.transform;
    }

    private void LateUpdate()
    {
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].target == null) continue;

            Vector3 camDelta = cam.position - camStartPosition;

            float factorY = camDelta.y < 0f
                ? layers[i].parallaxFactorYDown
                : layers[i].parallaxFactorY;

            float offsetX = camDelta.x * (1f - layers[i].parallaxFactorX);
            float offsetY = camDelta.y * (1f - factorY);

            Vector3 pos = startPositions[i];
            pos.x += offsetX;
            pos.y += offsetY;
            layers[i].target.position = pos;

            if (layers[i].infiniteScroll)
                HandleInfiniteScroll(i);
        }
    }

    private void HandleInfiniteScroll(int index)
    {
        Transform center = layers[index].target;
        float width = spriteWidths[index];
        float camX = cam.position.x;
        float centerX = center.position.x;

        // 카메라가 오른쪽으로 벗어남 → 3장 모두 오른쪽으로 1칸 시프트
        if (camX > centerX + width * 0.5f)
        {
            startPositions[index].x += width;
            center.position += Vector3.right * width;
        }
        // 카메라가 왼쪽으로 벗어남 → 3장 모두 왼쪽으로 1칸 시프트
        else if (camX < centerX - width * 0.5f)
        {
            startPositions[index].x -= width;
            center.position += Vector3.left * width;
        }

        // 좌/우 복제본은 항상 중앙 기준으로 배치
        Vector3 centerPos = center.position;

        if (leftClones[index] != null)
            leftClones[index].position = new Vector3(centerPos.x - width, centerPos.y, centerPos.z);

        if (rightClones[index] != null)
            rightClones[index].position = new Vector3(centerPos.x + width, centerPos.y, centerPos.z);
    }
}
