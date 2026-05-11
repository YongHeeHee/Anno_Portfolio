# Spring (봄) 시즌 메카닉

## 개요
- 봄 시즌은 Growth(성장)와 VineGrapple(덩굴 새총) 두 가지 스킬을 제공하며, 마우스 드래그로 덩굴 플랫폼을 생성하거나 앵커에 덩굴을 연결한 뒤 새총처럼 발사하는 메카닉

## 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| `GrowthInteractable.cs` | Growth 스킬의 상호작용 대상. 클릭 시 성장 시작 지점 제공 |
| `SplineGrowthController.cs` | 마우스 드래그를 따라 SpriteShape 기반 덩굴 플랫폼을 실시간 생성 |
| `VineGrappleController.cs` | 우클릭 hold로 앵커(`VineLaunchInteractable`)에 테더 연결, 떼면 stretch × forcePerUnit 속도로 새총 발사 |
| `VineLaunchInteractable.cs` | VineGrapple의 앵커. `AnchorPosition` 프로퍼티만 노출하는 단순 컴포넌트 |

## 각 스크립트 상세

### GrowthInteractable (`Assets/Scripts/Spring/GrowthInteractable.cs`)
- **상속:** `MonoBehaviour`
- **RequireComponent:** `Collider2D`
- **역할:** Growth 스킬의 상호작용 지점. 씬에 배치된 오브젝트에 부착하여 클릭 시 성장 시작 위치를 제공하고, 생성된 Vine의 소유권을 관리
- **주요 변수:**
  - `growthOriginOffset` (Vector2) - 성장 시작 위치 오프셋 (오브젝트 기준)
  - `activeVine` (GameObject) - 현재 이 Interactable이 생성한 Vine 오브젝트
- **주요 프로퍼티:**
  - `GrowthOrigin` (Vector2) - 오브젝트 위치 + 오프셋으로 계산된 실제 성장 시작 좌표
  - `HasActiveVine` (bool) - 이미 생성한 Vine이 존재하는지 여부
- **주요 메서드:**
  - `OnMouseDown()` - Growth 스킬이 활성 상태이고, 성장 중이 아니며, 기존 Vine이 없을 때 `SplineGrowthController.StartGrowth()` 호출
  - `SetActiveVine(GameObject vine)` - 생성된 Vine을 소유로 등록
  - `ClearActiveVine(GameObject vine)` - Vine 파괴 시 소유 해제
- **다른 스크립트와의 관계:** `SkillManager.IsActiveSkill(SeasonSkillType.Growth)`로 스킬 활성 확인, `SplineGrowthController.Instance`에 성장 시작을 위임

### SplineGrowthController (`Assets/Scripts/Spring/SplineGrowthController.cs`)
- **상속:** `MonoBehaviour`
- **역할:** Singleton. 마우스 좌클릭 드래그를 따라 SpriteShape Spline 포인트를 실시간으로 추가하여 덩굴 플랫폼을 생성하고, 완성 후 EdgeCollider2D를 부착하여 밟을 수 있는 지형으로 만든 뒤, 일정 시간 후 끝에서부터 줄어들며 소멸
- **주요 변수:**
  - `mpCostPerUnit` (float) - 단위 거리당 소모 MP (기본값 10)
  - `minPointDistance` (float) - Spline 포인트 사이 최소 거리 (기본값 0.3). 낮을수록 촘촘한 곡선
  - `groundLayerIndex` (int) - 완성된 Vine에 설정할 Ground 레이어 (기본값 6)
  - `splineHeight` (float) - Spline 시각적 굵기 (기본값 0.5)
  - `shrinkDelay` (float) - 성장 완료 후 줄어들기 시작까지 대기 시간(초) (기본값 3)
  - `shrinkDuration` (float) - 완전히 줄어드는 데 걸리는 시간(초) (기본값 2)
  - `shapePrefab` (SpriteShapeController) - 미리 설정된 SpriteShape Prefab
  - `growthStartEffectPrefab` (GameObject) - 시작 이펙트
  - `growthTrailEffectPrefab` (GameObject) - 진행 중 마우스를 따라다니는 파티클
  - `growthEndEffectPrefab` (GameObject) - 완료 시 끝점 이펙트
  - `player` (PlayerBase) - MP 소모 대상
- **주요 프로퍼티:**
  - `IsGrowing` (bool) - 현재 성장 중인지 여부
- **주요 메서드:**
  - `GetShape()` - `shapePrefab`을 Instantiate하고 `SpriteShapeRenderer.SetLocalAABB`로 큰 bounds를 강제 지정. 동적 spline의 stale LocalAABB로 인한 frustum culling 방지 (Prefab에 캐시된 AABB와 실제 mesh 위치가 어긋나 렌더러가 culling되는 버그 회피)
  - `StartGrowth(Vector2 origin, GrowthInteractable owner)` - MP가 있으면 Spline 초기 포인트를 생성하고 성장 모드 진입. 시작 이펙트와 트레일 이펙트 생성
  - `CancelGrowth()` - 성장 강제 취소. 트레일과 Shape 오브젝트 파괴
  - `Update()` - 성장 중일 때 매 프레임 마우스 위치를 추적하여 `minPointDistance` 이상 이동하면 `AddSplinePoint()` 호출. MP 부족 시 자동 종료. 스킬 비활성 시 강제 취소
  - `AddSplinePoint(Vector2 point)` - Spline에 새 포인트 추가, Tangent 자동 계산, 메시 즉시 갱신(`BakeMesh()`), BakeMesh가 AABB를 재계산하므로 `SetLocalAABB`를 다시 적용해 culling 방지
  - `AutoCalculateTangent(Spline spline, int index)` - 이전/다음 포인트를 기반으로 부드러운 Tangent를 자동 계산 (방향 * 0.25f)
  - `FinishGrowth()` - 마우스 릴리즈 시 호출. 트레일 정리, `EdgeCollider2D` 추가, Ground 레이어 설정, `ShrinkAndDestroy()` 코루틴 시작
  - `ShrinkAndDestroy(...)` - `shrinkDelay` 후 `shrinkDuration` 동안 끝점부터 Spline 포인트를 순차 제거하며 줄어들다 파괴. EdgeCollider도 동기화
- **다른 스크립트와의 관계:** `GrowthInteractable`에서 `StartGrowth()` 호출됨. `PlayerBase.CurrentMp`를 직접 차감. `SkillManager.IsActiveSkill()`로 스킬 활성 확인

### VineLaunchInteractable (`Assets/Scripts/Spring/VineLaunchInteractable.cs`)
- **상속:** `MonoBehaviour`
- **RequireComponent:** `Collider2D`
- **역할:** VineGrapple이 연결할 수 있는 앵커. 씬에 배치할 오브젝트(나뭇가지, 절벽 끝 덩굴 등)에 부착
- **주요 프로퍼티:**
  - `AnchorPosition` (Vector2) - `transform.position` 반환. VineGrappleController가 발사 방향/거리 계산에 사용

### VineGrappleController (`Assets/Scripts/Spring/VineGrappleController.cs`)
- **상속:** `MonoBehaviour`
- **역할:** Singleton. *앵커 테더 + 새총 발사* 메카닉. 우클릭 hold로 앵커에 덩굴 테더 연결, 플레이어가 ground 위에서 자유롭게 이동해 줄을 늘림(stretch). 우클릭 release 시 (앵커 - 플레이어) 방향으로 stretch × forcePerUnit 속도 발사. LineRenderer로 덩굴 시각화
- **주요 변수:**
  - `player` (PlayerBase) - 플레이어 참조
  - `maxConnectDistance` (float) - 앵커 검색 최대 거리 (기본값 8, 지상 7칸 / 점프 8칸 도달 보장)
  - `aimRadius` (float) - CircleCast 조준 보정 반경 (기본값 0.5)
  - `raycastLayerMask` (LayerMask) - 앵커 + 시야 차단할 Ground/VineGround 레이어
  - `forcePerUnit` (float) - 거리당 속도 (선형, 기본값 4). 발사속도 = stretch × forcePerUnit
  - `minStretchForLaunch` (float) - 이 미만의 stretch에서는 발사되지 않음 (기본값 0.5)
  - `mpCost` (float) - 연결 순간 차감 MP (기본값 25)
  - `lineRenderer` (LineRenderer) - 덩굴 시각화용
- **주요 프로퍼티:**
  - `IsTethered` (bool) - 현재 앵커에 연결된 상태인지 여부
- **주요 메서드:**
  - `TryConnect()` - 우클릭 Down 시 호출. MP 확인 → 마우스 방향 `Physics2D.CircleCast`로 앵커 검색. 앵커가 아닌 콜라이더(Ground 등)에 차단되면 실패. MP 차감, `isTethered = true`, LineRenderer 활성
  - `Disconnect(bool launch)` - 우클릭 Up 시 `launch:true`. `stretch = Distance(player, anchor)`이 `minStretchForLaunch` 이상이면 `(anchor - player).normalized × stretch × forcePerUnit` 속도로 `player.ApplyLaunchVelocity()` 호출. 스킬 비활성 시 `launch:false`로 단순 해제
  - `UpdateLineRenderer()` - 매 프레임 플레이어 ~ 앵커 위치를 잇는 선 갱신
- **다른 스크립트와의 관계:** `SkillManager.IsActiveSkill(SeasonSkillType.VineGrapple)`로 스킬 활성 확인. `PlayerBase`의 `CurrentMp`, `ApplyLaunchVelocity()` 사용. `VineLaunchInteractable.AnchorPosition`으로 앵커 위치 조회

## 동작 흐름

### Growth (성장) 스킬
1. 플레이어가 숫자 키 1을 눌러 봄 계절을 활성화
2. 씬에 배치된 `GrowthInteractable` 오브젝트를 마우스 좌클릭
3. `GrowthInteractable.OnMouseDown()`에서 스킬 활성 여부, 성장 중 여부, 기존 Vine 여부를 확인
4. 조건 충족 시 `SplineGrowthController.StartGrowth(GrowthOrigin, this)`를 호출
5. `StartGrowth()`에서 SpriteShape Prefab을 인스턴스화하고 초기 Spline 포인트 생성, 시작 이펙트 재생
6. 마우스 드래그 중 `Update()`에서 `minPointDistance` 이상 이동할 때마다:
   - 이동 거리 * `mpCostPerUnit`만큼 MP 차감
   - `AddSplinePoint()`로 Spline 포인트 추가 및 Tangent 자동 계산
   - 트레일 이펙트가 마우스를 따라 이동
7. 마우스 릴리즈 시 `FinishGrowth()`:
   - `EdgeCollider2D` 부착으로 밟을 수 있는 지형으로 변환
   - Ground 레이어 설정
   - `GrowthInteractable.SetActiveVine()`으로 소유 등록
8. `shrinkDelay` 후 `ShrinkAndDestroy()` 시작:
   - `shrinkDuration` 동안 끝점부터 Spline 포인트를 순차적으로 제거
   - EdgeCollider도 동기화하여 점점 짧아짐
   - 완전히 줄어들면 Vine 오브젝트 파괴, `GrowthInteractable.ClearActiveVine()` 호출

### VineGrapple (덩굴 새총) 스킬
1. 봄 계절 활성 상태에서 마우스 우클릭 Down
2. `TryConnect()`에서 MP 확인 후 플레이어 → 마우스 방향으로 `Physics2D.CircleCast`(반경 `aimRadius`, 거리 `maxConnectDistance`)
3. 앵커(`VineLaunchInteractable`)가 검출되고 시야 차단(Ground 등) 없으면:
   - MP 차감, `isTethered = true`
   - LineRenderer 활성화
   - **일반 이동 잠금 없음** — 플레이어가 ground 위에서 자유롭게 이동해 줄을 늘림(stretch)
4. 우클릭 Up 시 `Disconnect(launch:true)`:
   - `stretch = Distance(player, anchor)`
   - `stretch ≥ minStretchForLaunch`이면 `(anchor - player).normalized × stretch × forcePerUnit` 속도로 `player.ApplyLaunchVelocity()` 호출
   - LineRenderer 비활성화
5. 다른 종료 조건:
   - 스킬 비활성화 → `Disconnect(launch:false)` (발사 없이 단순 해제)

### 레벨 디자인 — 앵커 배치 거리
- **플레이어가 밟고 있는 타일 기준** 앵커까지의 도달 거리 상한:
  - 점프 없이(지상): 최대 **7칸**
  - 점프 시: 최대 **8칸**
- `maxConnectDistance = 8`로 코드 측에서 위 도달 거리 보장
- 발사 도약 거리는 *앵커-플레이어 사이 ground 길이*(stretch 가능 범위)로 통제: 좁은 발판 = 약한 도약, 넓은 발판 = 강한 도약
