# Architecture

## 캐릭터 상속 구조

```
IMovable (interface)
│   - IsGrounded, SetMoveInput(), RequestJump()
│   - 이동/점프 추상화 → 다른 캐릭터 타입에서 재사용 가능
│
CharacterBase (abstract, MonoBehaviour)
│   - IsDead, FacingDirection
│   - spriteRenderer (자동 캐시)
│   - abstract Die()
│
├── PlayerBase (abstract, implements IMovable)
│   │   - 이동 파라미터 직접 보유: moveSpeed, jumpForce, gravityScale
│   │   - MP 관리: maxMp, mpRegenInterval, mpRegenAmount, CurrentMp
│   │   - SpeedMultiplier, JumpMultiplier → 스킬(Ignite)이 이동 배율 조작
│   │   - MaxMp, JumpForce → public getter (스킬 컨트롤러/UI에서 접근)
│   │   - 이동/점프/착지 판정 (Rigidbody2D + Physics2D.OverlapBox)
│   │   - 스프라이트 방향 전환 (flipX)
│   │   - Animator 파라미터 자동 업데이트 (Speed, VelocityY, IsGrounded, Rest, Death)
│   │   - 휴식 애니메이션 (일정 시간 입력 없으면 Rest 트리거)
│   │   - InputLocked 프로퍼티 (대화 중 이동 잠금)
│   │   - MovementLocked 프로퍼티 (전투 등 외부 시스템이 이동 잠금)
│   │   - "Lock" 애니메이터 태그 → 해당 상태에서 이동 불가
│   │   - 이동 플랫폼 지원: GroundCheck에서 밟은 Rigidbody2D 속도를 플레이어 속도에 합산
│   │   - abstract ReadInput() → 서브클래스가 입력 소스 정의
│   │
│   └── PlayerController (구체 클래스)
│       - New Input System (Move, Jump 액션)
│       - ReadInput() → SetMoveInput() / RequestJump() 호출
│
├── PlayerCombat (MonoBehaviour, 별도 컴포넌트)
│   - 공격 (Attack 입력 → 애니메이션 + 카메라 쉐이크)
│   - 가드 (Crouch 입력 → 일정 시간 가드 자세, MovementLocked 설정)
│   - 패리 (가드 시작 후 parryWindow 이내 → 투사체 반사)
│   - Guard 히트박스 (guardCheckOffset, guardCheckSize)
│   - TryParry() → Projectile에서 호출
│   - PlayerBase 참조 (IsGrounded, FacingDirection, MovementLocked)
│
└── EnemyShooter
    - fireInterval마다 Projectile 생성
    - 플레이어 HitPoint 방향으로 조준
    - Die() → 깜빡임 후 Destroy
```

## 전투 시스템 흐름

```
EnemyShooter.Fire()
    → Projectile 생성 (EnemyProjectile 레이어)
    → 플레이어 HitPoint 방향으로 이동

Projectile.FixedUpdate()
    → Physics2D.OverlapCircle로 충돌 감지

충돌 시 (HandleCollision):
    ├── 가드 중 & 히트박스 안:
    │   ├── 패리 성공 (parryWindow 내) → Reflect() → 반사 (속도 1.5배, 금색, ReflectedProjectile 레이어)
    │   └── 패리 실패 (가드만) → 투사체 파괴 (막기 성공)
    ├── 가드 중이지만 히트박스 밖 → 플레이어 사망
    └── 가드 안 함 → 플레이어 사망

반사된 Projectile이 EnemyShooter와 충돌:
    → enemy.Die()
```

## 대화 시스템

```
DialogueData (ScriptableObject)
    - speakerName, lines[], typingSpeed
    - nextDialogue → 대화 체이닝 지원
    - 에셋 경로: Assets/ScriptableObject/Dialogue/{NPC이름}/{대화이름}.asset

NPCInteraction (트리거 기반)
    - npcPortrait → NPC 초상화 스프라이트
    - 플레이어가 Trigger 진입 → 상호작용 인디케이터("E") 표시
    - Interact 입력 → DialogueManager.StartDialogue(dialogue, npcPortrait) 호출
    - 대화 끝나면 nextDialogue로 자동 전환

DialogueManager (싱글톤)
    - playerName, playerPortrait → 플레이어 정보 (Inspector에서 설정)
    - 대화 시작 시 PlayerBase.InputLocked = true
    - DialoguePanel을 통해 대화 표시
    - Interact 또는 마우스 클릭으로 진행
    - 타이핑 중 입력 → 즉시 완성 / 완성 후 입력 → 다음 줄
    - 모든 줄 끝나면 InputLocked = false

DialoguePanel (UI 컴포넌트)
    - 코드로 전체 UI 생성 (Screen Space Overlay Canvas)
    - 화면 하단 패널: 왼쪽 Player 초상화 / 오른쪽 NPC 초상화
    - 말하는 캐릭터 초상화 강조, 상대방은 어둡게 처리
    - 화자 이름 + DOTween 타이핑 효과
```

## 카메라 시스템

```
CameraShake (싱글톤)
    - CinemachineImpulseSource 래핑
    - ShakeOnAttack() → 약한 흔들림 (0.15)
    - ShakeOnHit()    → 중간 흔들림 (0.5)
    - ShakeOnParry()  → 강한 흔들림 (0.8)
    - CameraShake.Instance로 어디서든 접근

CinemachineSetupTool (에디터 전용)
    - Tools > Setup Cinemachine Camera
    - Brain, Camera, Follow, ImpulseListener 자동 생성
```

## 에디터 도구

| 도구 | 메뉴 경로 | 기능 |
|------|-----------|------|
| DialogueEditorWindow | Tools > Dialogue Editor | DialogueData SO 생성/편집 GUI |
| CinemachineSetupTool | Tools > Setup Cinemachine Camera | Cinemachine 카메라 리그 자동 세팅 |

## Spline 개념 설명

Spline(스플라인)은 여러 개의 제어점(Control Point)을 부드러운 곡선으로 연결하는 수학적 곡선이다.
Unity의 `SpriteShapeController`는 Spline 경로를 따라 스프라이트를 타일링하여 시각적 형태를 만든다.

```
제어점(Control Point)
    - 곡선이 지나가는 위치 좌표
    - 각 포인트에 Height(두께) 설정 가능 → Open Spline에서 시각적 굵기 결정

탄젠트(Tangent)
    - 각 제어점에서 곡선의 방향과 곡률을 결정하는 벡터
    - Left/Right Tangent로 진입/진출 방향을 각각 제어
    - TangentMode.Continuous → 좌우 탄젠트가 대칭 → 부드러운 곡선

Open vs Closed Spline
    - Open (isOpenEnded = true): 시작점과 끝점이 연결되지 않는 선형 경로 (덩굴, 줄기)
    - Closed (기본값): 마지막 점이 첫 점으로 연결되는 닫힌 도형

SpriteShape Profile
    - Spline 경로를 따라 타일링할 스프라이트 정의
    - Angle Range: Spline 방향(각도)에 따라 다른 스프라이트 적용 가능
    - 스프라이트는 가로 방향으로 타일링됨 → 세로 이미지는 90° 회전 필요
```

## 계절별 스킬 시스템

```
SeasonSkillType (enum)
    - Growth (봄), VineGrapple (봄), Explosion (여름), Ignite (여름), Lighten (가을), Ghost (가을), Freeze (겨울), Anchor (겨울)

SkillManager (싱글톤, 씬에 하나 배치)
    - 숫자 키(1~4)로 계절 선택/해제 (토글)
    - SeasonKeyBinding[] seasonBindings: Inspector에서 계절별 단축키 및 스킬 그룹 확인/변경
      - seasonName: 계절 이름 (Inspector 식별용)
      - key: 단축키
      - skills: 해당 계절에 포함된 스킬 목록
    - bool[] unlockedSeasons: Inspector에서 계절별 해금 여부 체크박스로 관리
    - activeSeasonIndex: 현재 활성 계절 인덱스 (-1이면 없음)
    - IsActiveSkill(SeasonSkillType) → 활성 계절에 해당 스킬이 포함되어 있는지 확인
    - SetActiveSeason() → unlockedSeasons[] 체크 후 활성화
    - UnlockSeason(int) → 계절 해금 API (게임 시스템이 호출)
    - IsSeasonUnlocked(int) → 해금 여부 조회
    - OnSeasonUnlocked 이벤트 → UI/사운드 등 반응용
    - 하나의 계절을 선택하면 해당 계절의 모든 스킬이 동시 활성 (입력 구분은 각 컨트롤러가 처리)

PlayerBase - 스탯/MP 직접 관리 + 이동 배율
    - 이동 스탯: moveSpeed, jumpForce, gravityScale (SerializeField)
    - MP 스탯: maxMp, mpRegenInterval, mpRegenAmount (SerializeField)
    - CurrentMp 프로퍼티: 런타임 MP 관리 (maxMp로 클램프)
    - SpeedMultiplier 프로퍼티: 이동속도 배율 (기본 1f, Ignite가 조작)
    - JumpMultiplier 프로퍼티: 점프력 배율 (기본 1f, Ignite가 조작)
    - MaxMp, JumpForce: public getter (스킬 컨트롤러/UI에서 접근)
    - PlayerBase.Awake()에서 maxMp로 초기화
    - PlayerBase.RegenMp()에서 자동 충전
    - 모든 스킬 컨트롤러는 player.CurrentMp로 MP 접근

스킬별 MP 비용 관리 원칙
    - 스킬별 MP 비용은 각 스킬 컨트롤러의 SerializeField로 관리
    - Growth: SplineGrowthController.mpCostPerUnit (거리당 소모)
    - Explosion: ExplosionController.mpCostPerCharge (1회 고정 소모)
    - Lighten: LightenController.mpCost (1회 고정 소모)
    - Ghost: GhostController.mpCostPerSecond (초당 소모)
    - VineGrapple: VineGrappleController.mpCost (1회 고정 소모)
    - Ignite: IgniteController.mpCostPerSecond (초당 소모)
    - Freeze: FreezeController.mpCost (1회 고정 소모)
    - Anchor: AnchorController.mpCostPerSecond (초당 소모)
    - 밸런싱은 각 스킬 컨트롤러의 수치로 관리
```

## 이동 플랫폼 시스템

```
PlayerBase.FixedUpdate()
    - GroundCheck 시 밟고 있는 Collider2D의 attachedRigidbody를 감지
    - 해당 Rigidbody2D의 linearVelocity를 플레이어 속도에 합산
    - 일반 지면(Rigidbody2D 없음): platformVelocity = zero → 기존 동작과 동일
    - 이동 플랫폼(Rigidbody2D 있음): 플랫폼 속도가 플레이어 이동에 반영

    적용 예시:
    - ExplosionInteractable 위에 서 있을 때 폭발 → 오브젝트와 함께 자연스럽게 이동
    - 비행 중 점프 입력으로 이탈 가능
    - 특정 스킬에 의존하지 않는 범용 시스템 (향후 다른 이동 플랫폼에도 적용 가능)
```

## 봄 스킬 - Growth (생명력 부여)

```
스크립트 경로: Assets/Scripts/Spring/

GrowthInteractable (맵 오브젝트에 부착하는 컴포넌트)
    RequireComponent: Collider2D (마우스 클릭 감지용)

    Inspector 설정:
    - growthOriginOffset (Vector2): 성장 시작 위치 오프셋 (오브젝트 기준). 기본값 (0, 0)

    동작:
    - OnMouseDown() → SplineGrowthController.Instance.StartGrowth(GrowthOrigin) 호출
    - IsGrowing 중이면 추가 클릭 무시 (중복 성장 방지)
    - Scene 뷰에서 Gizmo(초록 원)로 시작점 시각화

SplineGrowthController (싱글톤, 씬에 하나 배치)
    스킬 제한: HasSkill(SeasonSkillType.Growth) → SpringStats에서만 사용 가능

    Inspector 설정:
    ┌─ Growth Settings ─────────────────────────────────────────────┐
    │ mpCostPerUnit (float, 기본 10)    : 거리 1 unit당 소모 MP     │
    │ minPointDistance (float, 기본 0.3) : Spline 포인트 간 최소 거리│
    │ groundLayerIndex (int, 기본 6)    : Ground 레이어 번호        │
    │ splineHeight (float, 기본 0.5)    : Spline 시각적 두께        │
    ├─ Shrink Settings ─────────────────────────────────────────────┤
    │ shrinkDelay (float, 기본 3)       : 성장 완료 후 수축 대기(초)│
    │ shrinkDuration (float, 기본 2)    : 수축 완료까지 걸리는 시간 │
    ├─ SpriteShape ─────────────────────────────────────────────────┤
    │ shapePrefab (SpriteShapeController): 미리 설정된 Prefab       │
    ├─ References ──────────────────────────────────────────────────┤
    │ player (PlayerBase)               : MP 소모용 플레이어 참조   │
    └───────────────────────────────────────────────────────────────┘

    성장 흐름:
        1. GrowthInteractable 마우스 클릭
           → StartGrowth(origin)
           → HasSkill(Growth) 체크, MP > 0 체크
           → SpriteShape Prefab Instantiate (GetShape)
             · SpriteShapeRenderer.SetLocalAABB로 큰 bounds 강제 지정
               → 동적 spline의 stale LocalAABB 기반 frustum culling 방지
           → Spline 초기화 (origin에 첫 번째 포인트)

        2. 마우스 드래그 중 (Update)
           → 마우스 월드 좌표 추적
           → 이전 포인트와 거리 >= minPointDistance이면 새 포인트 추가
           → 포인트마다 Height(두께), TangentMode(Continuous) 설정
           → AutoCalculateTangent()로 인접 포인트 기반 부드러운 곡선 자동 계산
           → MP 소모: distance × mpCostPerUnit
           → MP 부족 시 자동 종료

        3. 마우스 릴리즈 OR MP 부족
           → FinishGrowth()
           → PlayerBase.CurrentMp 갱신
           → EdgeCollider2D 추가 (splinePoints 기반)
           → Ground 레이어 설정 → 플레이어가 밟을 수 있는 발판

        4. 수축/소멸 (코루틴: ShrinkAndDestroy)
           → shrinkDelay(3초) 대기
           → shrinkDuration(2초)에 걸쳐 끝점부터 시작점 방향으로 Spline 포인트 순차 제거
           → EdgeCollider2D 포인트도 동기화
           → 완전히 줄어들면 GameObject Destroy
```

## 여름 스킬 - Explosion (폭발)

```
스크립트 경로: Assets/Scripts/Summer/

ExplosionInteractable (맵 오브젝트에 부착하는 컴포넌트)
    RequireComponent: Collider2D, Rigidbody2D

    Inspector 설정:
    ┌─ Physics ─────────────────────────────────────────────────────────┐
    │ flyGravityScale (float, 기본 3)        : 비행 중 중력 배율       │
    │ groundLayerIndex (int, 기본 6)         : Ground 레이어 번호      │
    ├─ Landing ─────────────────────────────────────────────────────────┤
    │ destroyDelay (float, 기본 0)           : 착지 후 파괴 대기(초)   │
    │                                          0이면 파괴하지 않음     │
    │ landingVelocityThreshold (float, 기본 0.5): 착지 판정 최소 속도  │
    └───────────────────────────────────────────────────────────────────┘

    Awake 초기 상태:
    - Rigidbody2D: Dynamic, constraints = FreezeAll (위치/회전 고정)
    - Layer: Ground (플레이어가 위에 올라설 수 있음)

    Launch(Vector2 force):
        → constraints = FreezeRotation (위치 고정 해제, 회전만 잠금)
        → gravityScale = flyGravityScale
        → AddForce(force, Impulse) → 물리 기반 포물선 비행

    착지 판정 (2가지 조건 중 하나):
        1. FixedUpdate: 속도 < landingVelocityThreshold && 하강 중(velocity.y <= 0)
        2. OnCollisionEnter2D: 바닥과 충돌 (contact.normal.y > 0.5)

    Land():
        → velocity/angularVelocity 초기화
        → gravityScale 원래 값 복원
        → constraints = FreezeAll (다시 고정)
        → destroyDelay > 0이면 코루틴으로 파괴 예약

    플레이어 탑승:
        → ExplosionInteractable이 직접 관리하지 않음
        → PlayerBase의 이동 플랫폼 시스템이 자동 처리
        → 오브젝트가 비행 중일 때 위에 서 있으면 플레이어가 자연스럽게 따라감

ExplosionController (싱글톤, 씬에 하나 배치)
    스킬 제한: HasSkill(SeasonSkillType.Explosion) → SummerStats에서만 사용 가능

    Inspector 설정:
    ┌─ References ──────────────────────────────────────────────────────┐
    │ player (PlayerBase)                    : 스킬 체크, MP 소모용    │
    ├─ Charge Settings ─────────────────────────────────────────────────┤
    │ maxChargeTime (float, 기본 3)          : 최대 차징 시간(초)      │
    │ minRadius (float, 기본 0.5)            : 최소 폭발 반경          │
    │ maxRadius (float, 기본 5)              : 최대 폭발 반경          │
    ├─ Force Settings ──────────────────────────────────────────────────┤
    │ minForce (float, 기본 5)               : 최소 발사 힘            │
    │ maxForce (float, 기본 20)              : 최대 발사 힘            │
    │ upwardBias (float, 기본 0.5)           : 상향 보정 (포물선 높이) │
    ├─ MP ──────────────────────────────────────────────────────────────┤
    │ mpCostPerCharge (float, 기본 30)       : 1회 폭발 고정 MP 비용   │
    ├─ Indicator ───────────────────────────────────────────────────────┤
    │ chargeIndicatorPrefab (GameObject)     : 차징 범위 시각화 프리팹 │
    │                                          null이면 인디케이터 없음│
    └───────────────────────────────────────────────────────────────────┘

    차징 → 폭발 흐름:
        1. 마우스 좌클릭 Down
           → TryStartCharge()
           → MP >= mpCostPerCharge 확인 (부족하면 무시)
           → chargeCenter = 마우스 월드 좌표
           → 차징 인디케이터 생성/표시

        2. 홀드 중 (매 프레임 Update)
           → chargeTimer 누적 (maxChargeTime 상한)
           → chargeRatio = chargeTimer / maxChargeTime (0~1)
           → currentRadius = Lerp(minRadius, maxRadius, chargeRatio)
           → 인디케이터 스케일 업데이트 (반경 × 2)

        3. 마우스 릴리즈
           → Explode()
           → MP 차감: currentMp -= mpCostPerCharge
           → currentForce = Lerp(minForce, maxForce, chargeRatio)
           → Physics2D.OverlapCircleAll(chargeCenter, currentRadius)
           → 범위 내 ExplosionInteractable 필터링 (IsFlying인 것은 제외)
           → 각 오브젝트에 대해:
               direction = (오브젝트 위치 - 폭발 중심).normalized
               direction.y += upwardBias → re-normalize (포물선 상향 보정)
               interactable.Launch(direction × currentForce)
           → CameraShake.Instance.ShakeOnHit() (화면 흔들림)

    차징 인디케이터:
        → chargeIndicatorPrefab을 Instantiate (최초 1회)
        → 차징 중 SetActive(true), 스케일 동적 변경
        → 폭발 시 SetActive(false)
        → 프리팹 설정: 반투명 원형 SpriteRenderer 권장
```

## 가을 스킬 - Lighten (무게 경감)

```
스크립트 경로: Assets/Scripts/Autumn/

LightenInteractable (맵 오브젝트에 부착하는 컴포넌트)
    RequireComponent: Collider2D, Rigidbody2D

    동작:
    - OnMouseDown() → LightenController.Instance.TryLighten(this) 호출
    - IsLightened 중이면 추가 클릭 무시 (중복 경감 방지)
    - Awake()에서 원본 gravityScale, mass, constraints 캐시

    ApplyLighten(gravityMultiplier, massMultiplier, duration):
        → constraints = FreezeRotation (위치 고정 해제)
        → gravityScale × gravityMultiplier (중력 경감)
        → mass × massMultiplier (질량 경감)
        → duration(초) 후 원래 값 복원 (코루틴)
        → 복원 시 velocity 초기화

LightenController (싱글톤, 씬에 하나 배치)
    스킬 제한: SkillManager.IsActiveSkill(SeasonSkillType.Lighten)

    Inspector 설정:
    ┌─ References ──────────────────────────────────────────────────────┐
    │ player (PlayerBase)                    : MP 소모용 플레이어 참조  │
    ├─ Lighten Settings ────────────────────────────────────────────────┤
    │ duration (float, 기본 5)               : 경감 지속 시간(초)      │
    │ gravityMultiplier (float, 기본 0.1)    : 중력 배율 (낮을수록 가벼움) │
    │ massMultiplier (float, 기본 0.1)       : 질량 배율 (낮을수록 가벼움) │
    ├─ MP ──────────────────────────────────────────────────────────────┤
    │ mpCost (float, 기본 20)                : 1회 경감 고정 MP 비용   │
    └───────────────────────────────────────────────────────────────────┘

    TryLighten(target):
        → MP >= mpCost 확인 (부족하면 실패)
        → MP 차감
        → target.ApplyLighten() 호출
```

## 가을 스킬 - Ghost (유체화)

```
스크립트 경로: Assets/Scripts/Autumn/

GhostController (싱글톤, 씬에 하나 배치)
    스킬 제한: SkillManager.IsActiveSkill(SeasonSkillType.Ghost)

    Inspector 설정:
    ┌─ References ──────────────────────────────────────────────────────┐
    │ player (PlayerBase)                    : 스킬 체크, MP 소모용    │
    ├─ Ghost Settings ──────────────────────────────────────────────────┤
    │ ghostAlpha (float, 기본 0.4)           : 고스트 상태 알파값      │
    │ mpCostPerSecond (float, 기본 10)       : 초당 MP 소모            │
    ├─ Layer ────────────────────────────────────────────────────────────┤
    │ ghostWallLayerIndex (int, 기본 7)      : GhostWall 레이어 번호   │
    │ playerLayerIndex (int, 기본 0)         : 플레이어 레이어 번호    │
    └───────────────────────────────────────────────────────────────────┘

    고스트 활성화/비활성화 흐름:
        1. 우클릭 (Mouse Button 1)
           → ActivateGhost()
           → safePosition 저장 (현재 위치)
           → 플레이어 알파값 감소 (반투명)
           → Physics2D.IgnoreLayerCollision(player, ghostWall, true)

        2. 고스트 활성 중 (매 프레임 Update)
           → MP 초당 소모 (mpCostPerSecond × deltaTime)
           → MP 부족 시 강제 해제
           → GhostWall 내부가 아니면 safePosition 갱신

        3. 우클릭 재입력 / MP 부족 / 스킬 전환
           → DeactivateGhost()
           → Physics2D.IgnoreLayerCollision(player, ghostWall, false)
           → 알파값 1로 복원
           → GhostWall 내부에 있으면 safePosition으로 텔레포트

    안전 위치 추적:
        → 고스트 중 GhostWall 바깥에 있을 때만 safePosition 갱신
        → GhostWall 내부 진입 시 갱신 중단 → 벽 진입 직전 위치 보존
        → IsInsideGhostWall(): OverlapCapsule로 플레이어 콜라이더 기반 판정

    레이어 설정 필요:
        → Unity Layer 설정에서 GhostWall 레이어 생성
        → 통과 대상 타일맵에 GhostWall 레이어 할당
        → GhostController Inspector에서 ghostWallLayerIndex 설정
```

## 봄 스킬 - VineGrapple (덩굴 새총)

```
스크립트 경로: Assets/Scripts/Spring/

메카닉 개요:
    *앵커 테더 + 새총 발사* 방식. 우클릭 hold로 앵커(VineLaunchInteractable)에
    덩굴을 연결하고, 플레이어가 ground 위에서 자유롭게 이동해 줄을 늘린다(stretch).
    우클릭 release 시 (앵커 - 플레이어) 방향으로 stretch × forcePerUnit 속도로 발사.
    줄 자체에 길이 cap 없음 — 플레이어가 이동 가능한 ground 범위가 곧 stretch 한계.
    레벨 디자인이 stretch 가능 거리를 통제: 좁은 발판 = 약한 발사, 넓은 발판 = 강한 발사.

VineLaunchInteractable (앵커로 사용할 오브젝트에 부착)
    RequireComponent: Collider2D
    AnchorPosition 프로퍼티 → transform.position 반환

VineGrappleController (싱글톤, 씬에 하나 배치)
    스킬 제한: SkillManager.IsActiveSkill(SeasonSkillType.VineGrapple)

    Inspector 설정:
    ┌─ References ──────────────────────────────────────────────────────┐
    │ player (PlayerBase)                    : MP 소모용 플레이어 참조  │
    ├─ Connection ──────────────────────────────────────────────────────┤
    │ maxConnectDistance (float, 기본 8)     : 앵커 검색 최대 거리      │
    │                                          (지상 7칸 / 점프 8칸 도달) │
    │ aimRadius (float, 기본 0.5)            : CircleCast 조준 보정 반경│
    │ raycastLayerMask (LayerMask)           : 앵커 + 시야 차단 레이어  │
    ├─ Launch ──────────────────────────────────────────────────────────┤
    │ forcePerUnit (float, 기본 4)           : 거리당 속도 (선형)      │
    │ minStretchForLaunch (float, 기본 0.5)  : 이 미만이면 발사 안 됨  │
    ├─ MP ──────────────────────────────────────────────────────────────┤
    │ mpCost (float, 기본 25)                : 연결 순간 차감 MP 비용   │
    ├─ Visual ──────────────────────────────────────────────────────────┤
    │ lineRenderer (LineRenderer)            : 덩굴 시각화 컴포넌트    │
    └───────────────────────────────────────────────────────────────────┘

    연결 → 발사 흐름:
        1. 우클릭 Down (Mouse Button 1)
           → TryConnect()
           → MP >= mpCost 확인 (부족하면 무시)
           → 플레이어 → 마우스 방향 Physics2D.CircleCast(aimRadius)
           → raycastLayerMask 히트 시 VineLaunchInteractable 컴포넌트 확인
           → 앵커가 아닌 콜라이더(Ground 등)에 차단되면 실패
           → MP 차감, isTethered = true, LineRenderer 활성

        2. 테더 연결 중 (매 프레임 Update)
           → 플레이어 일반 이동 가능 (MovementLocked 설정 없음)
           → LineRenderer로 플레이어 ~ 앵커 시각화
           → 줄 자체에 물리 제약 없음 — 길이는 플레이어 위치로만 결정

        3. 우클릭 Up
           → Disconnect(launch: true)
           → stretch = Distance(player, anchor)
           → stretch >= minStretchForLaunch이면:
               launchDir = (앵커 - 플레이어).normalized
               launchSpeed = stretch × forcePerUnit
               player.ApplyLaunchVelocity(launchDir × launchSpeed)
           → LineRenderer 비활성화

        4. 스킬 전환 시
           → Disconnect(launch: false) → 발사 없이 단순 해제

    PlayerBase.ApplyLaunchVelocity():
        → rb.linearVelocity 덮어쓰기
        → IsLaunched = true → 일정 시간 X 입력으로 인한 속도 덮어쓰기 차단(모멘텀 보존)
        → 착지하거나 launchOverrideDuration 경과 시 자동 해제
```

## 여름 스킬 - Ignite (점화 부스트)

```
스크립트 경로: Assets/Scripts/Summer/

IgniteController (싱글톤, 씬에 하나 배치)
    스킬 제한: SkillManager.IsActiveSkill(SeasonSkillType.Ignite)

    Inspector 설정:
    ┌─ References ──────────────────────────────────────────────────────┐
    │ player (PlayerBase)                    : 스킬 체크, MP 소모용    │
    ├─ Ignite Settings ─────────────────────────────────────────────────┤
    │ speedMultiplier (float, 기본 2)        : 이동속도 배율           │
    │ jumpMultiplier (float, 기본 1.5)       : 점프력 배율             │
    │ igniteTint (Color, 주황)               : 활성 시 스프라이트 색상 │
    ├─ MP ──────────────────────────────────────────────────────────────┤
    │ mpCostPerSecond (float, 기본 12)       : 초당 MP 소모            │
    └───────────────────────────────────────────────────────────────────┘

    점화 활성화/비활성화 흐름:
        1. 우클릭 (Mouse Button 1)
           → ActivateIgnite()
           → player.SpeedMultiplier = speedMultiplier
           → player.JumpMultiplier = jumpMultiplier
           → 스프라이트 색상 igniteTint로 변경

        2. 점화 활성 중 (매 프레임 Update)
           → MP 초당 소모 (mpCostPerSecond × deltaTime)
           → MP 부족 시 강제 해제

        3. 우클릭 재입력 / MP 부족 / 스킬 전환
           → DeactivateIgnite()
           → SpeedMultiplier = 1, JumpMultiplier = 1 복원
           → 스프라이트 색상 복원

    PlayerBase 연동:
        → SpeedMultiplier: FixedUpdate에서 moveSpeed × SpeedMultiplier 적용
        → JumpMultiplier: FixedUpdate에서 jumpForce × JumpMultiplier 적용
        → SO 데이터를 직접 수정하지 않는 안전한 방식
```

## 겨울 스킬 - Freeze (결빙)

```
스크립트 경로: Assets/Scripts/Winter/

FreezeInteractable (맵 오브젝트에 부착하는 컴포넌트)
    RequireComponent: Collider2D, Rigidbody2D, SpriteRenderer

    Inspector 설정:
    - freezeTint (Color): 결빙 상태 틴트 색상 (청/하얀색)
    - blinkStartRatio, minBlinkInterval, maxBlinkInterval: 깜박임 설정

    동작:
    - OnMouseDown() → FreezeController.Instance.TryFreeze(this) 호출
    - IsFrozen 중이면 추가 클릭 무시
    - Awake()에서 원본 gravityScale, constraints 캐시

    ApplyFreeze(duration):
        → velocity/angularVelocity 초기화
        → gravityScale = 0, constraints = FreezeAll (완전 정지)
        → 스프라이트에 freezeTint 적용
        → duration 후 깜박임 경고 → 원래 값 복원

FreezeController (싱글톤, 씬에 하나 배치)
    스킬 제한: SkillManager.IsActiveSkill(SeasonSkillType.Freeze)

    Inspector 설정:
    ┌─ References ──────────────────────────────────────────────────────┐
    │ player (PlayerBase)                    : MP 소모용 플레이어 참조  │
    ├─ Freeze Settings ─────────────────────────────────────────────────┤
    │ duration (float, 기본 5)               : 결빙 지속 시간(초)      │
    ├─ MP ──────────────────────────────────────────────────────────────┤
    │ mpCost (float, 기본 20)                : 1회 결빙 고정 MP 비용   │
    └───────────────────────────────────────────────────────────────────┘

    TryFreeze(target):
        → MP >= mpCost 확인
        → MP 차감
        → target.ApplyFreeze() 호출
```

## 겨울 스킬 - Anchor (자기 결빙)

```
스크립트 경로: Assets/Scripts/Winter/

AnchorController (싱글톤, 씬에 하나 배치)
    스킬 제한: SkillManager.IsActiveSkill(SeasonSkillType.Anchor)

    Inspector 설정:
    ┌─ References ──────────────────────────────────────────────────────┐
    │ player (PlayerBase)                    : 스킬 체크, MP 소모용    │
    ├─ Anchor Settings ─────────────────────────────────────────────────┤
    │ anchorTint (Color, 청/하얀색)          : 활성 시 스프라이트 색상  │
    ├─ MP ──────────────────────────────────────────────────────────────┤
    │ mpCostPerSecond (float, 기본 10)       : 초당 MP 소모            │
    └───────────────────────────────────────────────────────────────────┘

    앵커 활성화/비활성화 흐름:
        1. 우클릭 홀드 (Mouse Button 1)
           → Activate()
           → 원본 gravityScale, constraints 캐시
           → velocity 초기화, gravityScale = 0, constraints = FreezeAll
           → 스프라이트 anchorTint 적용
           → 공중/지상 모두 사용 가능

        2. 앵커 활성 중 (매 프레임 Update)
           → MP 초당 소모 (mpCostPerSecond × deltaTime)
           → MP 부족 시 강제 해제

        3. 우클릭 릴리즈 / MP 부족 / 스킬 전환
           → Deactivate()
           → 원래 gravityScale, constraints 복원
           → 스프라이트 색상 복원
           → velocity는 0 유지 (자연스러운 복귀)

    활용 예시:
        → 이동 플랫폼 타이밍 맞추기: 공중에서 앵커로 대기 후 플랫폼 착지
        → 바람/빙판 맵: 밀려나는 상황에서 앵커로 위치 고정
        → 낙하 중 일시 정지: 정밀 타이밍 조작
```

## 계절 스킬 UI

```
스크립트 경로: Assets/Scripts/UI/

SeasonSkillUI (씬에 하나 배치)
    화면 좌상단에 계절별 스킬 아이콘 표시

    Inspector 설정:
    ┌─ Slot Prefab ─────────────────────────────────────────────────────┐
    │ slotPrefab (GameObject)                : 슬롯 UI 프리팹 (Image 필수)│
    │                                          null이면 기본 Image 생성 │
    ├─ Season Icons ────────────────────────────────────────────────────┤
    │ seasonConfigs[] (SeasonSlotConfig)      : 계절별 아이콘 스프라이트│
    ├─ Layout ──────────────────────────────────────────────────────────┤
    │ slotSize (Vector2, 기본 64x64)         : 슬롯 크기              │
    │ spacing (float, 기본 8)                : 슬롯 간 간격            │
    │ margin (Vector2, 기본 20x20)           : 화면 모서리 여백        │
    ├─ Highlight ───────────────────────────────────────────────────────┤
    │ borderThickness (float, 기본 4)        : 활성 테두리 두께        │
    │ activeBorderColor (Color)              : 활성 테두리 색상        │
    ├─ Colors ──────────────────────────────────────────────────────────┤
    │ activeColor (Color, 기본 white)        : 활성 아이콘 색상        │
    │ unlockedColor (Color)                  : 해금 비활성 아이콘 색상 │
    │ lockedColor (Color)                    : 잠금 아이콘 색상        │
    └───────────────────────────────────────────────────────────────────┘

    동작:
    - Start()에서 Canvas + 슬롯 동적 생성 (slotPrefab Instantiate)
    - Update()에서 SkillManager.ActiveSeasonIndex 변경 감지 시 RefreshSlots()
    - 활성 계절: activeColor + activeBorderColor 테두리
    - 해금 비활성: unlockedColor, 테두리 없음
    - 잠금: lockedColor, 테두리 없음
```

## 디자인 패턴 요약

| 패턴 | 사용처 |
|------|--------|
| Singleton | CameraShake, DialogueManager, SkillManager, SplineGrowthController, ExplosionController, LightenController, GhostController, VineGrappleController, IgniteController, FreezeController, AnchorController |
| Template Method | CharacterBase → PlayerBase → PlayerController (ReadInput) |
| 컴포넌트 분리 | PlayerCombat (전투를 별도 컴포넌트로 분리) |
| Interface 추상화 | IMovable (이동/점프 계약, 캐릭터 재사용) |
| ScriptableObject 데이터 | DialogueData |
| 콜백 | DialoguePanel.ShowLine(text, onComplete) |

## 주요 의존성 관계

```
PlayerCombat ──→ CameraShake (공격 시 쉐이크)
PlayerCombat ──→ PlayerBase (MovementLocked, IsGrounded, ResetIdleTimer)
Projectile ──→ PlayerCombat (가드/패리 체크)
Projectile ──→ PlayerBase (Die)
Projectile ──→ EnemyShooter (반사 시 Die)
Projectile ──→ CameraShake (피격/패리 시 쉐이크)
EnemyShooter ──→ Projectile (생성)
DialogueManager ──→ PlayerBase (InputLocked)
DialogueManager ──→ DialoguePanel (표시)
DialogueManager ──→ DialogueData (데이터)
NPCInteraction ──→ DialogueManager (대화 시작, NPC 초상화 전달)
DialoguePanel ──→ DOTween (타이핑 효과)
GrowthInteractable ──→ SplineGrowthController (성장 시작 요청)
SplineGrowthController ──→ PlayerBase (MP 소모)
SplineGrowthController ──→ SpriteShapeController (동적 생성, Spline 조작)
ExplosionController ──→ PlayerBase (MP 소모)
ExplosionController ──→ ExplosionInteractable (Launch 호출)
ExplosionController ──→ CameraShake (폭발 시 쉐이크)
PlayerBase ──→ Rigidbody2D (이동 플랫폼 속도 감지, GroundCheck 시 attachedRigidbody)
SkillManager ──→ (독립, unlockedSeasons로 해금 관리)
GrowthInteractable ──→ SkillManager (IsActiveSkill 체크)
ExplosionController ──→ SkillManager (IsActiveSkill 체크)
LightenInteractable ──→ SkillManager (IsActiveSkill 체크)
LightenInteractable ──→ LightenController (TryLighten 호출)
LightenController ──→ PlayerBase (MP 소모)
GhostController ──→ SkillManager (IsActiveSkill 체크)
GhostController ──→ PlayerBase (MP 소모, SpriteRenderer, CapsuleCollider2D)
GhostController ──→ Physics2D (IgnoreLayerCollision, OverlapCapsule)
VineGrappleController ──→ SkillManager (IsActiveSkill 체크)
VineGrappleController ──→ PlayerBase (MP 소모, ApplyLaunchVelocity, Rigidbody2D)
VineGrappleController ──→ VineLaunchInteractable (앵커 검출, AnchorPosition 조회)
VineGrappleController ──→ Physics2D (CircleCast)
IgniteController ──→ SkillManager (IsActiveSkill 체크)
IgniteController ──→ PlayerBase (MP 소모, SpeedMultiplier, JumpMultiplier, SpriteRenderer)
FreezeInteractable ──→ SkillManager (IsActiveSkill 체크)
FreezeInteractable ──→ FreezeController (TryFreeze 호출)
FreezeController ──→ PlayerBase (MP 소모)
AnchorController ──→ SkillManager (IsActiveSkill 체크)
AnchorController ──→ PlayerBase (MP 소모, Rigidbody2D, SpriteRenderer)
SeasonSkillUI ──→ SkillManager (ActiveSeasonIndex, IsSeasonUnlocked)
```
