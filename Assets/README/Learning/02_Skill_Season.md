# Skill & Season 시스템

## 개요
- 4계절(봄/여름/가을/겨울)에 각 2개씩 총 8개의 스킬을 숫자 키로 전환하며 사용하는 계절 기반 스킬 시스템

## 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| `SeasonSkillType.cs` | 모든 스킬 종류를 정의하는 enum |
| `SkillManager.cs` | 계절별 스킬 그룹 관리, 활성 계절 전환, 잠금 해제 처리 |
| `SeasonSceneController.cs` | 계절별 씬을 Additive로 로드/언로드하고 플레이어 위치를 SpawnPoint로 이동 |

## 각 스크립트 상세

### SeasonSkillType (`Assets/Scripts/SeasonSkillType.cs`)
- **상속:** 없음 (enum)
- **역할:** 게임 내 모든 스킬 타입을 열거형으로 정의
- **정의된 값:**
  - `Growth` - 봄 스킬 1: 성장
  - `VineGrapple` - 봄 스킬 2: 덩굴 그래플
  - `Explosion` - 여름 스킬 1: 폭발
  - `Ignite` - 여름 스킬 2: 점화
  - `Lighten` - 가을 스킬 1: 빛
  - `Ghost` - 가을 스킬 2: 유령
  - `Freeze` - 겨울 스킬 1: 빙결
  - `Anchor` - 겨울 스킬 2: 앵커
- **다른 스크립트와의 관계:** `SkillManager`와 각 스킬 컨트롤러가 이 enum을 참조하여 현재 활성 스킬을 판별

### SkillManager (`Assets/Scripts/SkillManager.cs`)
- **상속:** `MonoBehaviour`
- **역할:** Singleton 패턴으로 계절별 스킬 그룹을 관리하고, 키 입력으로 활성 계절을 전환하며, 계절 잠금 해제 상태를 추적
- **주요 변수:**
  - `seasonBindings` (SeasonKeyBinding[]) - 계절별 단축키와 스킬 목록 배열. 기본값: 1=봄, 2=여름, 3=가을, 4=겨울
  - `unlockedSeasons` (bool[]) - 각 계절의 잠금 해제 여부. 기본적으로 봄(인덱스 0)만 해제
  - `activeSeasonIndex` (int) - 현재 활성화된 계절 인덱스. -1이면 비활성
- **내부 구조체:**
  - `SeasonKeyBinding` - `seasonName`(string), `key`(KeyCode), `skills`(SeasonSkillType[])로 구성
- **주요 메서드:**
  - `SetActiveSeason(int index)` - 해당 인덱스의 계절이 잠금 해제되어 있으면 활성화
  - `ClearActiveSeason()` - 활성 계절을 해제 (activeSeasonIndex = -1)
  - `IsActiveSkill(SeasonSkillType skill)` - 전달된 스킬이 현재 활성 계절에 포함되어 있는지 반환. 각 스킬 컨트롤러가 매 프레임 호출
  - `UnlockSeason(int seasonIndex)` - 특정 계절을 잠금 해제하고 `OnSeasonUnlocked` 이벤트 발행
  - `IsSeasonUnlocked(int seasonIndex)` - 특정 계절의 잠금 해제 여부 반환
- **이벤트:**
  - `OnSeasonUnlocked` (Action\<int\>) - 계절 잠금 해제 시 발행
- **다른 스크립트와의 관계:** 모든 스킬 컨트롤러(GrowthInteractable, VineGrappleController, ExplosionController, IgniteController 등)가 `SkillManager.Instance.IsActiveSkill()`을 호출하여 스킬 사용 가능 여부를 확인

### SeasonSceneController (`Assets/Scripts/SeasonSceneController.cs`)
- **상속:** `MonoBehaviour`
- **역할:** Singleton 패턴으로 계절별 씬의 로드/언로드를 관리하고, 씬 전환 시 플레이어를 SpawnPoint로 이동
- **주요 변수:**
  - `seasonScenes` (SeasonScene[]) - 계절별 씬 이름 배열. SpringScene, SummerScene, AutumnScene, WinterScene
  - `startSeasonIndex` (int) - 게임 시작 시 자동 로드할 시즌 인덱스 (기본값 0 = 봄)
  - `playerTag` (string) - Player 오브젝트를 찾을 태그 (기본값 "Player")
  - `spawnPointTag` (string) - 시작 위치를 찾을 태그 (기본값 "SpawnPoint")
  - `currentSeasonIndex` (int) - 현재 로드된 시즌 인덱스
  - `isTransitioning` (bool) - 씬 전환 중 여부
- **내부 구조체:**
  - `SeasonScene` - `seasonName`(string), `sceneName`(string)으로 구성
- **주요 메서드:**
  - `ChangeSeason(int seasonIndex)` - 전환 중이 아니고 유효한 인덱스면 코루틴으로 씬 전환 시작
  - `ChangeSeasonRoutine(int nextIndex)` - 이전 씬 언로드 -> 새 씬 Additive 로드 -> 미사용 에셋 정리 -> SpawnPoint로 플레이어 이동
  - `MovePlayerToSpawnPoint(Scene seasonScene)` - 로드된 씬의 루트 오브젝트에서 SpawnPoint 태그를 재귀 탐색하여 플레이어 위치 설정
  - `FindChildWithTag(Transform parent, string tag)` - 재귀적으로 특정 태그를 가진 자식 Transform 검색
- **이벤트:**
  - `OnSeasonSceneLoaded` (Action\<int\>) - 시즌 씬 로드 완료 시 발행
  - `OnSeasonSceneUnloaded` (Action\<int\>) - 시즌 씬 언로드 완료 시 발행
- **다른 스크립트와의 관계:** `SkillManager`와 동일한 인덱스 체계(0=봄, 1=여름, 2=가을, 3=겨울)를 사용. 씬 전환은 독립적으로 동작하며, 스킬 활성화와는 별개

## 동작 흐름

### 계절 스킬 전환
1. 게임 시작 시 `SkillManager.Awake()`에서 Singleton 초기화, `unlockedSeasons[0]`(봄)을 true로 설정
2. 플레이어가 숫자 키(1~4)를 누르면 `Update()`에서 해당 키의 `seasonBindings` 인덱스를 탐색
3. 해당 계절이 잠금 해제(`IsSeasonUnlocked`)되어 있으면 `SetActiveSeason()`으로 활성화
4. 같은 키를 다시 누르면 `ClearActiveSeason()`으로 비활성화 (토글)
5. 각 스킬 컨트롤러는 매 프레임 `IsActiveSkill()`을 호출하여 자신의 스킬이 활성 계절에 포함되어 있는지 확인 후 동작

### 계절 씬 전환
1. 게임 시작 시 `SeasonSceneController.Start()`에서 `ChangeSeason(startSeasonIndex)`를 호출하여 초기 씬 로드
2. `ChangeSeasonRoutine()` 코루틴이 실행됨:
   - 기존 씬이 있으면 `SceneManager.UnloadSceneAsync()`로 언로드하고 `OnSeasonSceneUnloaded` 발행
   - 새 씬을 `LoadSceneMode.Additive`로 로드
   - `SceneManager.SetActiveScene()`으로 활성 씬 설정
   - `Resources.UnloadUnusedAssets()`로 메모리 정리
   - `MovePlayerToSpawnPoint()`로 플레이어를 SpawnPoint 위치로 이동
   - `OnSeasonSceneLoaded` 발행
