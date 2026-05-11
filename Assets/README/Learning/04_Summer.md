# Summer (여름) 시즌 메카닉

## 개요
- 여름 시즌은 Explosion(폭발)과 Ignite(점화) 두 가지 스킬을 제공하며, 차징 기반 범위 폭발로 오브젝트를 날리거나 자기 강화 버프로 이동/점프 능력을 높이는 메카닉

## 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| `ExplosionController.cs` | 좌클릭 차징으로 범위와 위력을 결정하여 폭발을 일으키는 스킬 컨트롤러 |
| `ExplosionInteractable.cs` | 폭발에 반응하여 날아가고 착지하는 상호작용 오브젝트 |
| `IgniteController.cs` | 우클릭 유지로 이동속도/점프력 버프를 적용하는 자기 강화 스킬 |

## 각 스크립트 상세

### ExplosionController (`Assets/Scripts/Summer/ExplosionController.cs`)
- **상속:** `MonoBehaviour`
- **역할:** Singleton. 좌클릭 길게 누르기(차징)로 폭발 범위와 위력을 결정하고, 릴리즈 시 범위 내 `ExplosionInteractable`을 탐지하여 힘을 가함. 차징 범위를 시각적으로 표시하는 인디케이터 기능 포함
- **주요 변수:**
  - `player` (PlayerBase) - 플레이어 참조
  - `maxChargeTime` (float) - 최대 차징 시간(초) (기본값 3)
  - `minRadius` / `maxRadius` (float) - 최소/최대 폭발 반경 (기본값 0.5 / 5)
  - `minForce` / `maxForce` (float) - 최소/최대 발사 힘 (기본값 5 / 20)
  - `upwardBias` (float) - 발사 방향에 추가하는 상향 비율 (기본값 0.5). 포물선을 높이는 역할
  - `minMpCost` / `maxMpCost` (float) - 최소/최대 MP 소모량 (기본값 10 / 50)
  - `chargeIndicatorPrefab` (GameObject) - 차징 범위 시각화용 원형 프리팹
  - `chargeRatio` (float) - 0~1 사이의 차징 비율
  - `chargeCenter` (Vector2) - 차징 시작 시 마우스 위치 (폭발 중심)
- **주요 프로퍼티:**
  - `IsCharging` (bool) - 현재 차징 중인지 여부
  - `CurrentMpCost` (float) - 현재 차징 비율에 따른 MP 소모량
- **주요 메서드:**
  - `TryStartCharge()` - MP >= `minMpCost`일 때 차징 시작. 마우스 위치를 폭발 중심으로 저장, 인디케이터 표시
  - `UpdateCharge()` - 매 프레임 차징 시간을 누적하여 `chargeRatio` 계산. 반경과 MP 비용을 Lerp로 보간. 현재 MP가 부족하면 `chargeRatio`를 MP 한도에 맞게 역산(`InverseLerp`). 실시간으로 `player.CurrentMp` 갱신
  - `Explode()` - 차징 종료 시 호출. `Physics2D.OverlapCircleAll()`로 범위 내 모든 Collider 탐지. `ExplosionInteractable` 컴포넌트가 있고 비행 중이 아닌 오브젝트에 방향 + `upwardBias`로 `Launch()` 호출. `CameraShake.Instance.ShakeOnHit()` 호출
  - `ShowIndicator()` / `UpdateIndicator()` / `HideIndicator()` - 인디케이터 프리팹의 생성, 크기 갱신(반경 * 2), 비활성화
- **다른 스크립트와의 관계:** `SkillManager.IsActiveSkill(SeasonSkillType.Explosion)`으로 스킬 활성 확인. `ExplosionInteractable.Launch()`를 호출하여 오브젝트에 힘을 가함. `CameraShake`로 화면 흔들림 연출

### ExplosionInteractable (`Assets/Scripts/Summer/ExplosionInteractable.cs`)
- **상속:** `MonoBehaviour`, `IResettable` 인터페이스 구현
- **RequireComponent:** `Collider2D`, `Rigidbody2D`
- **역할:** 폭발에 반응하여 물리적으로 날아가는 오브젝트. 평소에는 모든 물리 제약이 걸려 있고(`FreezeAll`), `Launch()` 호출 시 제약을 풀고 힘을 받아 날아감. 착지 시 다시 고정되며, 체크포인트 리스폰 시 원래 위치로 복귀
- **주요 변수:**
  - `flyGravityScale` (float) - 비행 중 중력 배율 (기본값 3). 빠르게 떨어지게 함
  - `groundLayerIndex` (int) - Ground 레이어 번호 (기본값 6)
  - `destroyDelay` (float) - 착지 후 파괴까지 대기 시간(초). 0이면 파괴하지 않음
  - `initialPosition` (Vector3) - 초기 위치 저장 (리셋용)
  - `initialRotation` (Quaternion) - 초기 회전 저장 (리셋용)
  - `initialGravityScale` (float) - 원래 중력 배율 저장
- **주요 프로퍼티:**
  - `IsFlying` (bool) - 현재 비행 중인지 여부
- **주요 메서드:**
  - `Launch(Vector2 force)` - 이미 비행 중이면 무시. `FreezeRotation`만 남기고 제약 해제, 중력을 `flyGravityScale`로 변경, `Rigidbody2D.AddForce(force, Impulse)`로 발사
  - `OnCollisionEnter2D(Collision2D collision)` - 비행 중 다른 오브젝트와 충돌 시, 다른 `ExplosionInteractable`이면 무시. 접촉 법선의 Y가 0.5 초과면(위에서 착지) `Land()` 호출
  - `Land()` - 비행 종료. 속도 초기화, 중력 복원, `FreezeAll`로 고정. `destroyDelay > 0`이면 지연 파괴
  - `ResetState()` - 위치/회전/물리 상태를 초기값으로 복원. `CheckpointManager.OnRespawn` 이벤트에 등록
- **다른 스크립트와의 관계:** `ExplosionController.Explode()`에서 `Launch()` 호출됨. `CheckpointManager.Instance.OnRespawn` 이벤트에 구독하여 리스폰 시 상태 복원

### IgniteController (`Assets/Scripts/Summer/IgniteController.cs`)
- **상속:** `MonoBehaviour`
- **역할:** Singleton. 우클릭 유지로 점화 상태를 활성화하여 플레이어의 이동속도와 점프력을 배율로 증가시키고, 스프라이트에 틴트 색상을 적용. MP를 초당 소모하며 MP 부족 시 자동 해제
- **주요 변수:**
  - `player` (PlayerBase) - 플레이어 참조
  - `speedMultiplier` (float) - 이동속도 배율 (기본값 2)
  - `jumpMultiplier` (float) - 점프력 배율 (기본값 1.5)
  - `igniteTint` (Color) - 점화 시 스프라이트 틴트 색상 (기본값 주황색 RGBA 1, 0.5, 0.2, 1)
  - `mpCostPerSecond` (float) - 초당 MP 소모량 (기본값 12)
  - `originalColor` (Color) - 원래 스프라이트 색상 저장
- **주요 프로퍼티:**
  - `IsIgnited` (bool) - 현재 점화 상태인지 여부
- **주요 메서드:**
  - `ActivateIgnite()` - MP > 0일 때 점화 활성화. `player.SpeedMultiplier`와 `player.JumpMultiplier` 설정, 스프라이트 색상을 `igniteTint`로 변경
  - `DeactivateIgnite()` - 점화 해제. 배율을 1로 복원, 스프라이트 색상 원래대로 복원
  - `Update()` - 스킬 비활성 시 자동 해제. 우클릭 입력 처리. 점화 중 초당 `mpCostPerSecond`만큼 MP 차감, MP 부족 시 자동 해제
- **다른 스크립트와의 관계:** `SkillManager.IsActiveSkill(SeasonSkillType.Ignite)`로 스킬 활성 확인. `PlayerBase`의 `SpeedMultiplier`, `JumpMultiplier`, `CurrentMp`를 직접 조작

## 동작 흐름

### Explosion (폭발) 스킬
1. 플레이어가 숫자 키 2를 눌러 여름 계절을 활성화
2. 마우스 좌클릭을 누르면 `TryStartCharge()` 호출
3. MP >= `minMpCost`(10)이면 차징 시작. 마우스 위치를 폭발 중심으로 저장, 인디케이터 표시
4. 좌클릭 유지 중 매 프레임 `UpdateCharge()`:
   - `chargeTimer`가 `maxChargeTime`(3초)까지 누적
   - `chargeRatio`에 따라 반경(`minRadius` ~ `maxRadius`)과 MP 비용(`minMpCost` ~ `maxMpCost`)이 Lerp로 보간
   - 현재 MP가 부족하면 비율을 역산하여 MP 한도 내에서 최대 차징
   - 인디케이터 크기가 실시간 갱신
5. 좌클릭 릴리즈 시 `Explode()`:
   - `Physics2D.OverlapCircleAll(chargeCenter, currentRadius)`로 범위 내 모든 Collider 탐지
   - `ExplosionInteractable` 컴포넌트가 있고 비행 중이 아닌 오브젝트를 필터링
   - 폭발 중심 -> 오브젝트 방향 + `upwardBias`를 합산한 방향으로 `currentForce`를 곱한 힘으로 `Launch()` 호출
   - `CameraShake`로 화면 흔들림
6. `ExplosionInteractable.Launch()`에서 물리 제약 해제 후 `AddForce(Impulse)`로 발사
7. 비행 중 충돌 시 `OnCollisionEnter2D`에서 착지 판정 (법선 Y > 0.5)
8. `Land()`에서 속도 초기화, 중력 복원, `FreezeAll`로 고정
9. `destroyDelay > 0`이면 지연 후 오브젝트 파괴
10. 체크포인트 리스폰 시 `ResetState()`로 원래 위치/상태 복원

### Ignite (점화) 스킬
1. 여름 계절 활성 상태에서 마우스 우클릭
2. `ActivateIgnite()`에서 MP > 0 확인 후:
   - `player.SpeedMultiplier = 2`, `player.JumpMultiplier = 1.5`로 버프 적용
   - 스프라이트 색상을 주황색 틴트로 변경
3. 우클릭 유지 중 매 프레임:
   - `mpCostPerSecond`(12) * `Time.deltaTime`만큼 MP 차감
   - MP 부족 시 자동으로 `DeactivateIgnite()` 호출
4. 우클릭 해제 또는 스킬 비활성화 시 `DeactivateIgnite()`:
   - 배율을 1로 복원
   - 스프라이트 색상을 원래대로 복원
