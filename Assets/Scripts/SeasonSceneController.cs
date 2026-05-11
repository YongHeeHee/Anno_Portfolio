using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SeasonSceneController : MonoBehaviour
{
    [Serializable]
    public struct SeasonScene
    {
        [Tooltip("계절 이름 (Inspector 식별용)")]
        public string seasonName;

        [Tooltip("Build Settings에 등록된 씬 이름")]
        public string sceneName;
    }

    public static SeasonSceneController Instance { get; private set; }

    [Header("Season Scenes")]
    [Tooltip("계절별 씬 목록. SkillManager와 동일한 인덱스 (0=봄, 1=여름, 2=가을, 3=겨울)")]
    [SerializeField] private SeasonScene[] seasonScenes = new SeasonScene[]
    {
        new SeasonScene { seasonName = "봄", sceneName = "SpringScene" },
        new SeasonScene { seasonName = "여름", sceneName = "SummerScene" },
        new SeasonScene { seasonName = "가을", sceneName = "AutumnScene" },
        new SeasonScene { seasonName = "겨울", sceneName = "WinterScene" },
    };

    [Header("Startup")]
    [Tooltip("게임 시작 시 자동으로 로드할 시즌 인덱스")]
    [SerializeField] private int startSeasonIndex = 0;

    [Tooltip("Player 오브젝트를 찾을 때 사용할 태그. SpawnPoint로 이동시킬 대상")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("로드된 시즌 씬에서 Player 시작 위치를 찾을 때 사용할 태그")]
    [SerializeField] private string spawnPointTag = "SpawnPoint";

    private int currentSeasonIndex = -1;
    private bool isTransitioning;

    public int CurrentSeasonIndex => currentSeasonIndex;
    public bool IsTransitioning => isTransitioning;

    public event Action<int> OnSeasonSceneLoaded;
    public event Action<int> OnSeasonSceneUnloaded;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ChangeSeason(startSeasonIndex);
    }

    public void ChangeSeason(int seasonIndex)
    {
        if (isTransitioning) return;
        if (seasonIndex < 0 || seasonIndex >= seasonScenes.Length) return;
        if (seasonIndex == currentSeasonIndex) return;

        StartCoroutine(ChangeSeasonRoutine(seasonIndex));
    }

    private IEnumerator ChangeSeasonRoutine(int nextIndex)
    {
        isTransitioning = true;

        if (currentSeasonIndex >= 0)
        {
            string prevName = seasonScenes[currentSeasonIndex].sceneName;
            Scene prev = SceneManager.GetSceneByName(prevName);
            if (prev.IsValid() && prev.isLoaded)
                yield return SceneManager.UnloadSceneAsync(prev);

            OnSeasonSceneUnloaded?.Invoke(currentSeasonIndex);
        }

        string nextName = seasonScenes[nextIndex].sceneName;
        yield return SceneManager.LoadSceneAsync(nextName, LoadSceneMode.Additive);

        Scene loaded = SceneManager.GetSceneByName(nextName);
        SceneManager.SetActiveScene(loaded);

        // 메모리 정리: 이전 시즌 에셋 해제
        yield return Resources.UnloadUnusedAssets();

        MovePlayerToSpawnPoint(loaded);

        currentSeasonIndex = nextIndex;
        isTransitioning = false;

        OnSeasonSceneLoaded?.Invoke(nextIndex);
    }

    private void MovePlayerToSpawnPoint(Scene seasonScene)
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        // 현재 로드된 시즌 씬의 루트에서만 SpawnPoint 검색
        foreach (GameObject root in seasonScene.GetRootGameObjects())
        {
            Transform spawn = FindChildWithTag(root.transform, spawnPointTag);
            if (spawn != null)
            {
                player.transform.position = spawn.position;
                return;
            }
        }
    }

    private Transform FindChildWithTag(Transform parent, string tag)
    {
        if (parent.CompareTag(tag)) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildWithTag(parent.GetChild(i), tag);
            if (found != null) return found;
        }
        return null;
    }
}
