# Work Log

> 주요 작업 이력을 기록합니다. (최신순)
> 각 항목은 **무엇을**, **왜** 변경했는지 기록합니다.

---

## 2026-05-07 | 낙사(worldspace Y) 사망 조건 제거 → Hazard 단일화

### 배경
- `PlayerBase.Update()`의 `transform.position.y < minYThreshold` (기본 -50) 분기로 낙사 처리 중이었음
- 시즌 씬은 GameScene 위에 Additive 로드되는 구조라 시즌 맵을 어느 월드 Y에 배치하느냐에 따라 임계값 의미가 달라져 사고 위험 (예: 시즌 맵을 Y=-50 아래에 두면 시작과 동시에 즉사)
- Hazard 태그 Tilemap 접촉 사망은 이미 `OnTriggerEnter2D`에서 동작 중이고, SpringScene에 Hazard 타일맵도 배치되어 있음

### 변경
- **PlayerBase.cs**:
  - `minYThreshold` SerializeField 제거
  - `Update()`의 Y값 체크 분기 제거
- **GAME_DESIGN.md 섹션 14**:
  - 사망 원인 표에서 "낙사" 행 제거
  - 환경 위험 항목에 "절벽/구덩이 바닥에도 Hazard 타일을 깔아 추락 사망을 표현"이라는 룰 명문화
  - 사망 연출 본문에서 "낙사/환경 위험 접촉" → "환경 위험 접촉"으로 단일화

### 보존
- KillZone 컴포넌트(Scripts/Checkpoint/KillZone.cs)는 박스형 사망구역 보조 도구로 유지. Hazard 타일맵으로 표현하기 어려운 영역(이동 플랫폼에 붙은 가시 등)에 사용 가능

### 영향 / 디자인 룰
- 시즌 씬 제작 시 절벽·구덩이·맵 외곽에는 반드시 Hazard 타일맵을 깔아야 한다. 빈 공간으로 두면 플레이어가 무한 추락한다

---

## 2026-05-05 | VineGrapple 도달 거리 디자인 기준 확정 — 지상 7칸 / 점프 8칸

### 배경
- 새총 메카닉으로 전환된 뒤 *앵커 배치 거리* 가 레벨 디자인 가이드로 명문화돼 있지 않아 봄 T2 이후 스테이지에서 일관성이 부족했음

### 결정
- 플레이어가 밟고 있는 타일 기준 **앵커 배치 상한**:
  - **점프 없이(지상): 최대 7칸**
  - **점프 시: 최대 8칸**
- `maxConnectDistance` 기본값 `6` → `8` 로 상향 (지상 7칸, 점프 8칸 도달을 코드 측에서 보장)
- GameScene `VineGrappleController.maxConnectDistance` 도 6 → 8 로 동기화

### 적용 범위
- PLAYER_STATS.md: VineGrapple 표 업데이트 + 앵커 배치 규칙 표 추가
- ARCHITECTURE.md: VineGrappleController 인스펙터 표 갱신
- VineGrappleController.cs / GameScene.unity: 인스펙터 기본값 동기화
- 봄 시즌 스테이지(T1~) 데코/맵 제작 시 위 규칙을 따라 앵커를 배치

---

## 2026-05-05 | VineGrapple 메커니즘 전환 — Raycast 당김 → 앵커 테더 + 새총 발사

### 변경 배경
- 기존 메커니즘(우클릭 시 마우스 방향 Raycast → 히트 지점으로 일정 속도 당김)은 도달 거리/방향이 한 번에 결정되어 레벨 디자인 자유도가 낮음
- DistanceJoint2D `maxStretch`로 줄 길이 제약을 두는 방식도 시도했으나, 앵커가 플레이어보다 위에 있을 때 줄 한계 도달 시 *플레이어가 공중으로 들어올려지는* 부작용 발생

### 신규 메커니즘 (VineGrappleController.cs 재작성)
- **VineLaunchInteractable**: 앵커로 사용할 오브젝트에 부착하는 단순 컴포넌트. `AnchorPosition` 프로퍼티만 노출
- **TryConnect()**: 우클릭 Down → CircleCast(`aimRadius=0.5`)로 마우스 방향 앵커 검색. `raycastLayerMask`에 앵커 + 차단용 Ground 레이어 포함 → Ground에 막히면 연결 실패. 연결 시 즉시 mpCost 차감
- **연결 중**: 일반 이동 잠금 안 함 — 플레이어가 ground 위에서 자유롭게 이동해 줄을 늘림(stretch). 줄 자체에 길이 cap 없음
- **Disconnect(launch:true)**: 우클릭 Up → `stretch × forcePerUnit` 속도로 (앵커 - 플레이어) 방향 발사. `minStretchForLaunch=0.5` 미만이면 발사 없음
- **PlayerBase.ApplyLaunchVelocity()** 신규: 발사 속도 적용 + `IsLaunched` 플래그로 X 입력에 의한 속도 덮어쓰기 차단(모멘텀 보존). 착지하거나 `launchOverrideDuration` 경과 시 자동 해제

### 레벨 디자인 원칙
- 줄 길이 제한이 없으므로 *앵커-플레이어 사이 ground 길이*가 곧 stretch 한계 → 발판 길이로 발사 도달 거리 통제
- 수직 도달: stretch 3칸 → 약 2.5칸(일반 점프 수준), 5칸 → 약 6.8칸, 6칸 → 약 9.8칸 (gravityScale=3, forcePerUnit=4 기준)
- 좁은 발판 = 약한 도약, 넓은 발판 = 강한 도약. 절벽으로 추가 stretch 차단 가능

---

## 2026-05-01 ~ 2026-05-05 | SpringScene 스테이지 구현 (T1 → T2)

### 씬 구조 정착
- 시즌 씬은 **Player·매니저 없이 맵 데이터만** 보유. GameScene을 베이스로 두고 시즌 씬을 Additive 로드
- 단일 Grid 하위에 스테이지별 Tilemap 그룹(`Stage_T1`, `Stage_T2`, ...). 각 스테이지마다 `Ground_XX`(CompositeCollider2D), `Hazard_XX`(TilemapCollider2D, Trigger)
- 평균 5만 타일 규모에서 단일 Tilemap+Composite는 편집 시 0.5~2초 멈춤 → 스테이지 분할로 편집 중인 Composite만 재계산되도록 함

### 데코 배치 시스템 (DecoScatter)
- 잔디·돌 등 **다량의 작은 데코는 절차적 산포**: `DecoScatter` 컴포넌트(빈 GameObject) + `DecoScatterPreset`(SO, 프리팹 가중치/density/yJitter/scaleVariance/flipChance)
- 나무·집·큰 바위 등 **크고 단독 데코는 GameObject 직접 배치**
- Tilemap 데코 레이어는 사용 안 함 — 스프라이트 크기 편차(32x25/18x16/56x90 등)가 고정 셀 그리드와 안 맞아서 폐기
- 모든 데코는 콜라이더 없음. 상호작용 환경 오브젝트는 별도로 분류

### 체크포인트 / KillZone 시스템
- `IResettable` 인터페이스 + `CheckpointManager`(싱글톤) + `CheckpointTrigger` + `KillZone`
- KillZone 진입 시 마지막 체크포인트로 리스폰
- 시즌 씬 안에 트리거 배치, 매니저는 GameScene에 영구 보유

### 카메라 - ParallaxBackground
- 카메라 위치 기반 배경 시차 스크롤 컴포넌트 추가

---

## 2026-04-21 | Growth 스킬 - SpriteShape 렌더링 누락 버그 수정

### 증상
- `GrowthInteratable_Pot`을 씬에 여러 개 배치하고 Growth 스킬을 연속 사용하면 n번째(2번째 이후) 덩굴에서 **EdgeCollider2D는 정상 생성되지만 SpriteShape sprite가 렌더링되지 않음**
- 플레이어는 보이지 않는 덩굴을 밟고 이동 가능 → 체감 무결성 손상
- **Game 창 크기에 따라 증상이 달라짐**: 전체 화면일 때 안 보이고, 창을 축소하면 보임 → Frustum Culling 이슈로 확정

### 원인
- `VinePrefab`의 `SpriteShapeRenderer`에 **stale LocalAABB**(center:(0, 0.25), extent:(1, 0.75))가 캐시되어 있음
- `Instantiate` 후 `transform.position = Vector3.zero`로 둔 채 origin 좌표에 spline 포인트를 꽂아 실제 mesh는 local (origin, ...) 위치에 생성되지만, 카메라는 캐시된 AABB (-1~1, -0.5~1) 기준으로 frustum culling 판정
- 실제 mesh가 stale AABB 밖에 있으면 `SpriteShapeRenderer`가 렌더링 스킵 → sprite 미표시
- Game 창 크기(카메라 aspect ratio)에 따라 frustum이 달라져 재현/미재현이 불안정하게 관측됨

### 수정 (SplineGrowthController.cs)
- `GetShape()`: Instantiate 직후 `SpriteShapeRenderer.SetLocalAABB(new Bounds(Vector3.zero, Vector3.one * 10000f))` 호출로 동적 스플라인용 frustum culling 무력화
- `AddSplinePoint()`: `BakeMesh()`가 AABB를 mesh geometry 기준으로 재계산하므로, BakeMesh 직후에도 동일한 override 재적용
- 일반적인 게임 해상도(orthographicSize ≤ 20, aspect ≤ 21:9) 수평 extent 대비 충분히 큰 bounds라 모든 해상도에서 안전

### 교훈
- 동적으로 성장/축소하는 SpriteShape는 prefab에 캐시된 LocalAABB가 실제 mesh 위치를 따라오지 못함
- `BakeMesh()`는 AABB를 재계산하지만 그 값이 여전히 stale할 수 있음 → frustum culling 회피용 큰 AABB override가 표준 우회책
- 렌더링 버그인데 Collider는 스크립트가 직접 주입해서 항상 정상이라 "Collider만 있고 Sprite 없음" 증상이 나타남

---

## 2026-04-19 | [구상 중] Sprite Shape Spline → 타일맵 수동 배치 전환 검토

### 배경
- 현재 지형/배경 일부를 Sprite Shape Profile + Spline으로 그리고 있음
- 픽셀 아트 기반(PPU 32) 프로젝트인데 spline 곡선 구간에서 픽셀이 늘어지고 깨지는 현상 발생
- Adaptive Tile, Mesh Type, Pixel Perfect Camera 등 설정으로 일부 완화는 가능하나 곡선 보간 자체가 픽셀 무결성과 충돌

### 검토 방향
- **Sprite Shape 유지 케이스**: 직선 위주 구간, 길이 변동이 잦은 임시 지형
  - Tangent Mode를 Linear 고정, Adaptive Tile 해제로 깨짐 최소화
- **타일맵 수동 배치 전환 케이스**: 최종 스테이지 지형, 픽셀 정렬이 중요한 배경
  - Tile Palette 기반으로 32px 그리드에 스냅된 배치
  - 곡선/경사 표현은 미리 만든 코너/슬로프 타일 세트로 대응
- 두 방식 혼용 가능성 검토 (가시성 큰 전경은 타일맵, 멀리 보이는 실루엣은 Sprite Shape)

### 결정 필요 항목
- 어떤 씬/구간을 어느 방식으로 갈지 분류 기준
- 타일맵 전환 시 필요한 추가 타일 에셋 목록 (코너, 슬로프, 가장자리 변형)
- 기존 Spring/Game 씬에 적용된 Sprite Shape 마이그레이션 범위

---

## 2026-03-30 | 겨울 스킬 구현 - Freeze + Anchor

### SeasonSkillType enum 변경
- **Ice 제거 → Freeze, Anchor 추가**: 4개 계절 모두 좌클릭/우클릭 2개 스킬 체계 완성
- SkillManager 기본 바인딩: 겨울(Freeze+Anchor)

### Freeze 스킬 신규 구현 (겨울 좌클릭)
- **FreezeController (싱글톤)**: MP 체크 후 오브젝트에 결빙 효과 적용
  - 1회 고정 MP 비용 (mpCost)
- **FreezeInteractable**: 맵 오브젝트에 부착하는 컴포넌트
  - 좌클릭 시 velocity 초기화 + gravityScale=0 + FreezeAll (완전 정지)
  - 청/하얀색 틴트로 시각적 피드백
  - duration 후 깜박임 경고 → 원래 상태 복원 (LightenInteractable 패턴 재활용)

### Anchor 스킬 신규 구현 (겨울 우클릭)
- **AnchorController (싱글톤)**: 우클릭 홀드로 플레이어 위치 고정
  - velocity 초기화 + gravityScale=0 + FreezeAll → 공중/지상 모두 사용 가능
  - 청/하얀색 틴트로 시각적 피드백
  - 초당 MP 소모 (mpCostPerSecond), MP 부족 시 강제 해제
  - 활용: 이동 플랫폼 타이밍 맞추기, 바람/빙판 맵에서 위치 고정

---

## 2026-03-30 | PlayerState SO 제거 및 PlayerBase 직접 통합

### PlayerState ScriptableObject 제거
- **PlayerState.cs 삭제**: 계절별 SO 분리가 불필요 (ChangeSeasonState 미사용, MP 값 동일)
- **4개 .asset 파일 삭제**: SpringStats, SummerStats, AutumnStats, WinterStats
- 밸런싱은 각 스킬 컨트롤러의 수치로 관리하는 구조로 단순화

### PlayerBase에 스탯 직접 통합
- moveSpeed, jumpForce, gravityScale, maxMp, mpRegenInterval, mpRegenAmount → SerializeField로 직접 선언
- seasonStates[], ChangeState(), ChangeSeasonState(), CurrentState 프로퍼티 제거
- MaxMp, JumpForce public getter 추가 (스킬 컨트롤러/UI에서 접근)

### 스킬 컨트롤러 6개 + UI 수정
- `player.CurrentState == null` 체크 → `player == null`로 변경
- VineGrappleController: `player.CurrentState.jumpForce` → `player.JumpForce`
- SeasonSkillUI: `player.CurrentState.maxMp` → `player.MaxMp`

---

## 2026-03-30 | 봄/여름 우클릭 스킬 추가 - VineGrapple + Ignite

### SeasonSkillType enum 확장
- **VineGrapple, Ignite 추가**: 봄/여름에도 가을과 동일하게 좌클릭(오브젝트 상호작용) + 우클릭(플레이어 효과) 2개 스킬 체계 완성
- SkillManager 기본 바인딩: 봄(Growth+VineGrapple), 여름(Explosion+Ignite)

### PlayerBase에 이동 배율 프로퍼티 추가
- **SpeedMultiplier, JumpMultiplier**: Ignite가 조작하는 이동속도/점프력 배율 (기본 1f)
- FixedUpdate에서 `moveSpeed * SpeedMultiplier`, `jumpForce * JumpMultiplier` 적용
- SO 데이터를 직접 수정하지 않는 안전한 방식

### VineGrapple 스킬 신규 구현 (봄 우클릭)
- **VineGrappleController (싱글톤)**: 우클릭으로 마우스 방향 덩굴 발사
  - Physics2D.Raycast로 벽/천장 감지 → 히트 시 플레이어를 해당 지점으로 당김
  - LineRenderer로 덩굴 시각화 (플레이어 ~ 히트 포인트)
  - 이동 중 MovementLocked = true, 도착/재클릭 시 해제
  - 1회 고정 MP 비용 (mpCost)

### Ignite 스킬 신규 구현 (여름 우클릭)
- **IgniteController (싱글톤)**: 우클릭으로 점화 부스트 토글
  - SpeedMultiplier/JumpMultiplier로 이동속도 2배, 점프력 1.5배
  - 스프라이트 주황색 틴트로 시각적 피드백
  - 초당 MP 소모 (mpCostPerSecond), MP 부족 시 강제 해제
  - GhostController와 동일한 토글 패턴

---

## 2026-03-29 | 스킬 시스템 리팩토링 - 계절 해금 방식 + MP 통합

### currentMp를 PlayerBase로 이동
- **PlayerState.currentMp 제거**: SO의 런타임 값 → PlayerBase의 `CurrentMp` 프로퍼티로 이동
- ChangeState() 시 MP가 리셋되지 않고 유지됨 (새 maxMp로 클램프만)
- 모든 스킬 컨트롤러: `player.CurrentState.currentMp` → `player.CurrentMp` 로 변경

### PlayerState에서 availableSkills 제거
- **availableSkills 필드, HasSkill() 메서드 제거**: 스킬 중복 문제 해결
- PlayerState는 이동/물리/MP 설정만 보유하는 순수 데이터 SO로 정리

### SkillManager에 계절 해금 시스템 추가
- **bool[] unlockedSeasons**: Inspector 체크박스로 해금 상태 관리
- **UnlockSeason(int)**: 게임 시스템이 호출하는 해금 API
- **IsSeasonUnlocked(int)**: 해금 여부 조회
- **OnSeasonUnlocked 이벤트**: UI/사운드 등 반응용
- SetActiveSeason()이 HasSkill() 대신 unlockedSeasons[] 체크
- player 참조 제거 (더 이상 불필요)

### PlayerBase에 seasonStates 배열 추가
- **PlayerState[] seasonStates**: Inspector에서 4개 계절 SO 할당 (0=봄~3=겨울)
- **ChangeSeasonState(int)**: 계절 인덱스로 SO 전환

---

## 2026-03-29 | 가을 스킬 - Lighten (무게 경감) + Ghost (유체화) 구현 + SkillManager 개선

### SeasonSkillType enum 변경
- **Wind → Lighten 변경, Ghost 추가**: Growth, Explosion, Lighten, Ghost, Ice
- 가을에 두 개의 스킬 할당 (Lighten + Ghost)

### SkillManager 계절 단위 선택으로 변경
- **SeasonKeyBinding 구조체 도입**: 계절별로 스킬을 그룹화하여 Inspector에서 관리
  - seasonName (계절 이름), key (단축키), skills[] (스킬 목록)
- 기본 키 바인딩: 1=봄(Growth), 2=여름(Explosion), 3=가을(Lighten+Ghost), 4=겨울(Ice)
- 개별 스킬 선택 → 계절 선택으로 변경. 한 계절의 모든 스킬이 동시 활성화
- 입력 구분은 각 스킬 컨트롤러가 처리 (좌클릭=Lighten, 우클릭=Ghost)

### Lighten 스킬 신규 구현
- **LightenController (싱글톤)**: MP 체크 후 오브젝트에 경감 효과 적용
  - 1회 고정 MP 비용 (mpCost), gravityMultiplier/massMultiplier로 경감 정도 조절
- **LightenInteractable**: 맵 오브젝트에 부착하는 컴포넌트
  - 좌클릭 시 Rigidbody2D constraints 해제 + 중력/질량 경감
  - duration 후 원래 값 자동 복원, 복원 시 velocity 초기화

### Ghost 스킬 신규 구현
- **GhostController (싱글톤)**: 우클릭으로 고스트 모드 토글
  - 플레이어 알파값 감소 (반투명 시각 효과)
  - Physics2D.IgnoreLayerCollision으로 GhostWall 레이어 통과
  - 초당 MP 소모 (mpCostPerSecond), MP 부족 시 강제 해제
  - 안전 위치 추적: GhostWall 바깥에 있을 때만 safePosition 갱신
  - 해제 시 GhostWall 내부에 있으면 safePosition으로 텔레포트
  - OverlapCapsule로 플레이어 콜라이더 기반 내부 판정

### AutumnStats/WinterStats 에셋 업데이트
- AutumnStats: 마나 필드 추가, availableSkills에 Growth/Explosion/Lighten/Ghost 할당
- WinterStats: 마나 필드 추가, availableSkills에 전체 5개 스킬 할당

---

## 2026-03-26 | 여름 스킬 - Explosion (폭발) 구현 + 이동 플랫폼 시스템 + MP 비용 리팩토링

### Explosion 스킬 신규 구현
- **ExplosionController (싱글톤)**: 차징 → 폭발 → 오브젝트 발사 관리
  - 마우스 꾹 누르면 폭발 범위/위력이 chargeRatio(0~1)에 따라 증가
  - 놓으면 OverlapCircleAll로 범위 내 ExplosionInteractable 탐색 → 중심에서 바깥으로 AddForce(Impulse)
  - upwardBias로 상향 보정 → 물리 기반 포물선 궤적
  - 1회 고정 MP 비용 (mpCostPerCharge), CameraShake 연동
  - 차징 인디케이터 프리팹으로 범위 시각화
- **ExplosionInteractable**: 폭발에 반응하는 오브젝트 컴포넌트
  - Dynamic Rigidbody2D + FreezeAll constraints (정지 상태)
  - Launch() 시 constraints 해제 → AddForce → 포물선 비행
  - 착지 판정: 바닥 충돌(normal.y > 0.5) 또는 속도 임계값 이하
  - 착지 시 FreezeAll 복원, destroyDelay로 선택적 파괴

### 이동 플랫폼 시스템 (PlayerBase)
- GroundCheck에서 밟은 Collider2D의 attachedRigidbody 속도를 플레이어 속도에 합산
- 범용 시스템: ExplosionInteractable뿐 아니라 Rigidbody2D를 가진 모든 이동 플랫폼에 적용
- 비행 중 점프로 자유롭게 이탈 가능
- GroundCheckOffset, GroundCheckSize public getter 추가

### MP 비용 관리 리팩토링
- PlayerState에서 mpCostPerUnit 제거 → SplineGrowthController로 이동
- 원칙: 스킬별 비용은 해당 스킬 컨트롤러의 SerializeField로 관리. PlayerState는 공통 필드만 보유

---

## 2026-03-24 | Growth 스킬 시스템 구현 및 계절별 스킬 제한

### Growth 스킬 (SplineGrowthController + GrowthInteractable)
- **Spline 기반 식물 성장**: 마우스 드래그로 SpriteShape Spline을 실시간 생성
- **Prefab 방식**: 런타임 AddComponent 대신 에디터에서 설정된 SpriteShape Prefab을 Instantiate
  - Profile, Material, Open Ended, Height 등을 Prefab에서 미리 설정
- **발판 기능**: 성장 완료 시 EdgeCollider2D 추가 → Ground 레이어로 플랫폼 역할
- **수축/소멸**: shrinkDelay 후 끝점→시작점 방향으로 줄어들며 shrinkDuration 후 Destroy

### MP 시스템
- **PlayerState 확장**: mp → maxMp/currentMp 분리, mpRegenInterval/mpRegenAmount 추가
- **자동 회복**: PlayerBase.Update()에서 일정 간격마다 MP 충전 (maxMp 초과 불가)
- **Growth MP 소모**: 거리 × mpCostPerUnit, MP 부족 시 자동 성장 중단

### 계절별 스킬 제한
- **SeasonSkillType enum 생성**: Growth, Fire, Wind, Ice
- **PlayerState에 availableSkills 추가**: 계절별 SO에서 사용 가능한 스킬 목록 관리
- **HasSkill() 체크**: SplineGrowthController.StartGrowth()에서 Growth 스킬 보유 여부 확인
- SpringStats에만 Growth 할당 → 다른 계절에서는 사용 불가

---

## 2026-03-23 | 대화 시스템 패널 방식으로 전면 교체
- **SpeechBubble → DialoguePanel**: 말풍선 방식에서 화면 하단 패널 방식으로 변경
  - Screen Space Overlay 캔버스, 왼쪽 Player 초상화 / 오른쪽 NPC 초상화
  - 말하는 캐릭터 강조(밝게), 상대방 어둡게 처리
  - 화자 이름 표시 + DOTween 타이핑 효과
- **DialogueManager 수정**: playerName, playerPortrait 필드 추가, DialoguePanel 사용
- **DialogueData 수정**: BubbleStyle 제거, typingSpeed만 유지
- **NPCInteraction 수정**: npcPortrait 필드 추가, SpeechBubble 의존 제거
- **DialogueEditorWindow 수정**: BubbleStyle 섹션 제거, 패널 스타일 프리뷰로 변경
- **SpeechBubble.cs**: 더 이상 미사용 (삭제 가능)

---

## 2026-03-23 | PlayerState SO 도입 (계절별 이동 파라미터)
- **PlayerState SO 생성**: moveSpeed, jumpForce, gravityScale을 SO로 분리
  - `[CreateAssetMenu]`로 에셋 생성 메뉴 등록 (Player/Player State)
  - 계절별 에셋 생성 필요: SpringStats, SummerStats, AutumnStats, WinterStats
- **PlayerBase 수정**: 하드코딩된 moveSpeed/jumpForce 제거, PlayerState SO에서 읽도록 변경
  - `ChangeState(PlayerState)` 메서드로 런타임 SO 교체 지원
  - Awake에서 초기 gravityScale 적용

---

## 2026-03-23 | PlayerController 책임 분리 리팩토링
- **IMovable 인터페이스 생성**: 이동/점프 추상화 (IsGrounded, SetMoveInput, RequestJump)
  - 다른 캐릭터 타입(AI 적, NPC 등)에서 동일한 이동 계약 재사용 가능
- **PlayerBase 수정**: IMovable 구현, 입력 읽기를 `ReadInput()` 추상 메서드로 분리
  - `MovementLocked` 프로퍼티 추가 (외부 시스템이 이동 잠금)
  - `ResetIdleTimer()` 공개 (전투 입력 시 휴식 타이머 초기화)
  - `IsLocked()` public으로 변경
  - 기존 가상 메서드 제거 (HandleInput, HasAdditionalInput, CanMove, OnUpdate)
- **PlayerController 단순화**: 이동/점프 입력만 처리 (ReadInput → SetMoveInput/RequestJump)
- **PlayerCombat 컴포넌트 생성**: 공격/가드/패리 로직을 별도 MonoBehaviour로 분리
  - PlayerBase 참조로 IsGrounded, FacingDirection, MovementLocked 연동
- **Projectile 수정**: PlayerController 대신 PlayerCombat + PlayerBase 참조

---

## 2026-03-01 | README 문서 구조 생성
- `Assets/README/` 폴더에 프로젝트 문서화
  - `PROJECT_STRUCTURE.md` - 폴더/파일 구조
  - `ARCHITECTURE.md` - 시스템 설계 및 의존성
  - `CODING_CONVENTIONS.md` - 코딩 규칙
  - `WORK_LOG.md` - 작업 이력 (이 파일)
- Claude Code 작업 효율화를 위해 프로젝트 전체를 탐색하지 않고 README만 참조할 수 있도록 정리

---

## 초기 커밋 ~ 현재 | 기존 구현 요약
- **캐릭터 시스템**: CharacterBase → PlayerBase → PlayerController 상속 구조
- **전투 시스템**: EnemyShooter의 투사체 발사 → 플레이어 가드/패리 → 투사체 반사 → 적 처치
- **대화 시스템**: DialogueData(SO) + NPCInteraction + DialogueManager + SpeechBubble
- **카메라 시스템**: Cinemachine + CameraShake (공격/피격/패리 시 흔들림)
- **에디터 도구**: DialogueEditorWindow (대화 SO 생성 GUI), CinemachineSetupTool (카메라 자동 세팅)

---

<!-- 새 작업 기록은 이 위에 추가 -->
