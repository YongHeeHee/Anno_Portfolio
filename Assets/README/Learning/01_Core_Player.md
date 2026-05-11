# 핵심 플레이어 시스템 (Core Player System)

## 1. 상속 구조

```
MonoBehaviour
  └── CharacterBase          (abstract)   -- 모든 캐릭터 공통
        ├── PlayerBase        (abstract)   -- 플레이어 전용, IMovable 구현
        │     └── PlayerController         -- 실제 입력 바인딩
        └── EnemyShooter                   -- 적 (별도 문서)

PlayerCombat                               -- PlayerBase와 같은 GameObject에 부착 (컴포지션)
IMovable                                   -- PlayerBase가 구현하는 인터페이스
```

핵심 설계 원칙:
- **CharacterBase**: 캐릭터라면 반드시 가져야 할 최소 기능 (스프라이트, 사망)
- **PlayerBase**: 이동/점프/MP 등 플레이어 고유 로직. 입력 읽기는 자식에게 위임 (`ReadInput()`)
- **PlayerController**: Input System 바인딩만 담당
- **PlayerCombat**: 전투 로직을 별도 컴포넌트로 분리 (컴포지션 패턴)

---

## 2. IMovable

`Assets/Scripts/IMovable.cs`

이동 가능한 캐릭터가 구현해야 하는 인터페이스.

| 멤버 | 타입 | 설명 |
|------|------|------|
| `IsGrounded` | `bool` (get) | 현재 지면에 닿아 있는지 여부 |
| `SetMoveInput(float input)` | `void` | 좌우 이동 입력값 전달 (-1 ~ 1) |
| `RequestJump()` | `void` | 점프 요청 (지면 판정은 구현 측에서 처리) |

현재 `PlayerBase`만 구현하지만, 향후 AI 캐릭터 등에도 적용할 수 있도록 인터페이스로 분리되어 있다.

---

## 3. CharacterBase

`Assets/Scripts/CharacterBase.cs`

### 역할
모든 캐릭터(플레이어, 적)의 최상위 추상 클래스. 공통으로 필요한 `SpriteRenderer` 참조, 사망 상태, 바라보는 방향을 제공한다.

### 주요 변수

| 변수 | 타입 | 접근 | 설명 |
|------|------|------|------|
| `spriteRenderer` | `SpriteRenderer` | `protected` | `Awake()`에서 자동 할당 |
| `IsDead` | `bool` | `public get`, `protected set` | 사망 상태 플래그 |
| `FacingDirection` | `int` | `public get` | `1`=오른쪽, `-1`=왼쪽 (`spriteRenderer.flipX` 기반) |

### 주요 메서드

| 메서드 | 설명 |
|--------|------|
| `Awake()` | `virtual`. `SpriteRenderer`를 `GetComponent`로 캐싱 |
| `Die()` | `abstract`. 자식 클래스에서 사망 처리를 구현해야 함 |

---

## 4. PlayerBase

`Assets/Scripts/PlayerBase.cs`

### 역할
플레이어 캐릭터의 핵심 로직을 담당하는 추상 클래스. 이동, 점프, 지면 판정, MP 시스템, 사망/리스폰, 휴식 애니메이션까지 모두 처리한다. 입력 읽기(`ReadInput()`)만 자식에게 위임한다.

### RequireComponent
- `Rigidbody2D`
- `CapsuleCollider2D`
- `Animator`

### Inspector 설정값 (SerializeField)

| 카테고리 | 변수 | 타입 | 기본값 | 설명 |
|---------|------|------|--------|------|
| Movement | `moveSpeed` | `float` | 5 | 이동 속도 |
| Movement | `jumpForce` | `float` | 12 | 점프 수직 속도 |
| Physics | `gravityScale` | `float` | 3 | 중력 배율 |
| Mana | `maxMp` | `float` | 100 | 최대 MP |
| Mana | `mpRegenInterval` | `float` | 1 | MP 회복 간격(초) |
| Mana | `mpRegenAmount` | `float` | 5 | 회복당 MP 양 |
| MP Regen Delay | `mpRegenDelay` | `float` | 2 | MP 소모 후 회복 시작 대기(초) |
| Ground Check | `groundLayer` | `LayerMask` | - | 지면 판정 레이어 |
| Ground Check | `groundCheckOffset` | `Vector2` | (0, 0) | OverlapBox 중심 오프셋 |
| Ground Check | `groundCheckSize` | `Vector2` | (0.6, 0.1) | OverlapBox 크기 |
| Effects | `jumpEffectPrefab` | `GameObject` | null | 점프 이펙트 프리팹 |
| Effects | `landEffectPrefab` | `GameObject` | null | 착지 이펙트 프리팹 |
| Effects | `effectOffset` | `Vector2` | (0, -0.5) | 이펙트 생성 오프셋 |
| Death | `minYThreshold` | `float` | -50 | 이 Y 이하면 낙사 |
| Death | `hazardTag` | `string` | "Hazard" | 위험 오브젝트 태그 |
| Death | `respawnDelay` | `float` | 1.2 | Death 애니메이션 후 리스폰 대기 |
| Rest | `restDelay` | `float` | 5 | 입력 없을 때 휴식 시작까지 대기 |

### 주요 Public 프로퍼티/필드

| 이름 | 타입 | 설명 |
|------|------|------|
| `InputLocked` | `bool` | `true`면 모든 입력 무시 (대화 중, 사망 중 등) |
| `MovementLocked` | `bool` | `true`면 이동만 차단 (가드 중 등). 입력은 읽음 |
| `SpeedMultiplier` | `float` | 이동 속도 배율 (Ignite 스킬에서 사용) |
| `JumpMultiplier` | `float` | 점프력 배율 (Ignite 스킬에서 사용) |
| `CurrentMp` | `float` | 현재 MP. setter에서 MP 감소 시 `mpRegenDelayTimer` 리셋 |
| `MaxMp` | `float` | 최대 MP (읽기 전용) |
| `JumpForce` | `float` | `jumpForce * JumpMultiplier` (읽기 전용) |
| `IsGrounded` | `bool` | 현재 지면 접촉 여부 (읽기 전용) |

### Animator Parameter Hash

| Hash 변수 | Parameter 이름 | 타입 |
|----------|---------------|------|
| `SpeedHash` | "Speed" | `float` |
| `VelocityYHash` | "VelocityY" | `float` |
| `IsGroundedHash` | "IsGrounded" | `bool` |
| `RestHash` | "Rest" | `bool` |
| `DeathHash` | "Death" | `trigger` |
| `RespawnHash` | "Respawn" | `trigger` |

### 주요 메서드

| 메서드 | 접근 | 설명 |
|--------|------|------|
| `SetMoveInput(float)` | `public` | IMovable 구현. `moveInput` 값 설정 |
| `RequestJump()` | `public` | IMovable 구현. 지면 위이고 이동 차단 아닐 때 점프 예약 |
| `IsLocked()` | `public` | Animator 현재 상태에 "Lock" 태그가 있으면 `true` |
| `ResetIdleTimer()` | `public` | 휴식 타이머 초기화 (PlayerCombat에서 호출) |
| `Die()` | `public override` | 사망 처리: 입력 잠금, 애니메이션, Kinematic 전환, `RespawnRoutine` 시작 |
| `ReadInput()` | `protected abstract` | 자식 클래스에서 입력을 읽고 `SetMoveInput()`/`RequestJump()` 호출 |

### 내부 동작 흐름

#### Update() 프레임 흐름
```
1. 낙사 체크 (Y < minYThreshold -> Die())
2. 사망/InputLocked 상태면 moveInput = 0 후 리턴
3. ReadInput() 호출 (자식 클래스가 구현)
4. RegenMp() - MP 자동 회복
5. HandleRest() - 휴식 애니메이션 판정
6. 스프라이트 좌우 반전 처리
7. Animator 파라미터 갱신 (Speed, VelocityY, IsGrounded)
```

#### FixedUpdate() 물리 흐름
```
1. 사망 상태면 리턴
2. OverlapBox로 지면 판정
3. 착지 감지 (공중 -> 지면) -> 착지 이펙트
4. 이동 플랫폼 속도 계산 (platformVelocity)
5. InputLocked면 플랫폼 속도만 적용
6. 이동 속도 계산: moveInput * moveSpeed * SpeedMultiplier + platformVelocity
7. 점프 처리: jumpRequested && isGrounded -> linearVelocity.y = jumpForce * JumpMultiplier
```

#### 사망/리스폰 흐름
```
Die()
  1. IsDead = true, InputLocked = true
  2. "Death" 트리거 애니메이션
  3. 속도 초기화, Kinematic 전환
  4. RespawnRoutine 코루틴 시작

RespawnRoutine() (respawnDelay 초 대기 후)
  1. Dynamic으로 복원, 중력 복원, 속도 초기화
  2. CheckpointManager.RespawnPlayer() 호출 (위치 이동 + MP 회복)
  3. "Respawn" 트리거, "Idle" 강제 재생
  4. IsDead = false, InputLocked = false
```

#### MP 회복 흐름
```
RegenMp()
  1. MP가 가득 차면 리턴
  2. mpRegenDelayTimer > 0 이면 타이머 감소 후 리턴 (소모 직후 쿨다운)
  3. mpRegenTimer 누적 -> mpRegenInterval마다 mpRegenAmount 회복
```

---

## 5. PlayerController

`Assets/Scripts/PlayerController.cs`

### 역할
Unity Input System을 사용하여 실제 키보드/게임패드 입력을 읽고, `PlayerBase`의 `SetMoveInput()`과 `RequestJump()`에 전달한다.

### 주요 변수

| 변수 | 타입 | 설명 |
|------|------|------|
| `moveAction` | `InputAction` | "Move" 액션 (Vector2, X축만 사용) |
| `jumpAction` | `InputAction` | "Jump" 액션 |

### 주요 메서드

| 메서드 | 설명 |
|--------|------|
| `Awake()` | `InputSystem.actions`에서 "Move", "Jump" 액션을 찾아 캐싱 |
| `ReadInput()` | `moveAction`의 X값으로 `SetMoveInput()`, `jumpAction` 프레임 입력으로 `RequestJump()` 호출 |

### 다른 스크립트와의 관계
- `PlayerBase`를 상속하므로, 이동/점프/MP/사망 등 모든 기능을 그대로 사용
- 이 클래스는 **입력 바인딩만** 담당하며, 로직은 부모에 있음
- `PlayerCombat`이 같은 GameObject에 부착되어 공격/가드 입력을 별도 처리

---

## 6. PlayerCombat

`Assets/Scripts/PlayerCombat.cs`

### 역할
공격, 가드, 패리 시스템을 담당하는 **컴포지션 컴포넌트**. `PlayerBase`와 같은 GameObject에 부착(`RequireComponent`)되어 동작한다.

### RequireComponent
- `PlayerBase`

### Inspector 설정값

| 변수 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `guardDuration` | `float` | 0.4 | 가드 지속 시간(초) |
| `parryWindow` | `float` | 0.15 | 가드 시작 후 패리 판정 가능 시간(초) |
| `guardCheckOffset` | `Vector2` | (0.5, 0.5) | 가드 히트박스 중심 오프셋 (방향 자동 반전) |
| `guardCheckSize` | `Vector2` | (0.8, 1.0) | 가드 히트박스 크기 |

### 주요 변수

| 변수 | 타입 | 설명 |
|------|------|------|
| `player` | `PlayerBase` | 같은 오브젝트의 PlayerBase 참조 |
| `IsGuarding` | `bool` | 현재 가드 상태 |
| `parryTimer` | `float` | 패리 가능 남은 시간 |
| `guardTimer` | `float` | 가드 남은 시간 |

### Input Action 매핑

| 액션 이름 | 실제 입력 | 기능 |
|----------|----------|------|
| "Attack" | (설정에 따라) | 공격 트리거 |
| "Crouch" | (설정에 따라) | 가드 시작 |

### 주요 메서드

| 메서드 | 접근 | 설명 |
|--------|------|------|
| `Update()` | `private` | 공격/가드 입력 처리, `MovementLocked` 동기화 |
| `HandleGuard()` | `private` | 가드 시작/타이머 감소/종료 처리 |
| `GetGuardCenter()` | `public` | 현재 가드 영역 중심 좌표 반환 (`FacingDirection` 반영) |
| `GetGuardSize()` | `public` | 가드 영역 크기 반환 |
| `TryParry()` | `public` | 패리 시도. 타이머 내이면 `true` + "Parry" 애니메이션 |

### 가드/패리 동작 흐름

```
1. Crouch 키 입력 (지면, 비가드, 비잠금 상태)
   -> IsGuarding = true
   -> guardTimer = guardDuration (0.4초)
   -> parryTimer = parryWindow (0.15초)

2. 매 프레임 타이머 감소
   -> guardTimer <= 0 이면 IsGuarding = false

3. 가드 중 투사체가 가드 영역에 진입 (Projectile에서 판정)
   -> parryTimer > 0 이면 패리 성공: 투사체 반사, 카메라 흔들림
   -> parryTimer <= 0 이면 가드 성공: 투사체 파괴
   -> 가드 영역 밖이면 피격: Die()

4. 가드 중 player.MovementLocked = true (이동 불가)
```

### 다른 스크립트와의 관계

```
PlayerCombat
  ├── PlayerBase.InputLocked    : 사망/대화 중이면 전투 입력 무시
  ├── PlayerBase.IsGrounded     : 지면 위에서만 공격/가드 가능
  ├── PlayerBase.IsLocked()     : "Lock" 애니메이션 중이면 가드 불가
  ├── PlayerBase.MovementLocked : 가드 중 이동 차단
  ├── PlayerBase.ResetIdleTimer : 전투 입력 시 휴식 타이머 리셋
  ├── CameraShake.Instance      : 공격 시 ShakeOnAttack() 호출
  └── Projectile                : TryParry(), GetGuardCenter(), GetGuardSize() 호출됨
```

---

## 7. 전체 동작 흐름 (실제 게임 기준)

### 7.1 일반 이동 시나리오

```
[매 프레임]
PlayerController.ReadInput()
  -> PlayerBase.SetMoveInput(x)     # moveInput 설정
  -> PlayerBase.RequestJump()       # 점프 예약 (지면일 때만)

PlayerBase.Update()
  -> 스프라이트 방향 반전
  -> Animator 파라미터 갱신

PlayerBase.FixedUpdate()
  -> OverlapBox 지면 판정
  -> rb.linearVelocity.x = moveInput * moveSpeed * SpeedMultiplier
  -> 점프 예약 있으면 rb.linearVelocity.y = jumpForce * JumpMultiplier
```

### 7.2 전투 시나리오 (투사체 가드)

```
EnemyShooter.Fire()
  -> Projectile 생성, 플레이어 방향으로 발사

PlayerCombat.Update()
  -> Crouch 키 입력 -> 가드 시작 (0.4초, 패리 윈도우 0.15초)
  -> player.MovementLocked = true

Projectile.FixedUpdate()
  -> OverlapCircle로 충돌 감지
  -> PlayerCombat.IsGuarding 체크
  -> GetGuardCenter/Size로 영역 확인
  -> 패리 윈도우 내: TryParry() -> 투사체 반사 (속도 1.5배, 색상 변경)
  -> 반사된 투사체 -> EnemyShooter 피격 -> Die()
```

### 7.3 사망/리스폰 시나리오

```
PlayerBase.Die() 또는 KillZone/Hazard 접촉
  -> IsDead = true, InputLocked = true
  -> "Death" 애니메이션
  -> Kinematic 전환

[1.2초 후 RespawnRoutine]
  -> Dynamic 복원
  -> CheckpointManager.RespawnPlayer()
     -> 활성 스킬 해제 (Ghost, Ignite, Anchor)
     -> 체크포인트 위치로 이동
     -> MP 풀 회복
     -> OnRespawn 이벤트 -> ExplosionInteractable.ResetState() 등
  -> "Respawn" -> "Idle" 애니메이션
  -> IsDead = false, InputLocked = false
```

### 7.4 스킬 사용 시 PlayerBase 상호작용

```
[Ignite 스킬 활성화]
IgniteController.ActivateIgnite()
  -> player.SpeedMultiplier = 2.0
  -> player.JumpMultiplier = 1.5
  -> 매 프레임 player.CurrentMp -= mpCostPerSecond * deltaTime

[VineGrapple 사용]
VineGrappleController.TryConnect()         # 우클릭 Down — 앵커에 테더 연결
  -> player.CurrentMp -= 25                # MP setter가 mpRegenDelayTimer 리셋
  -> isTethered = true
  # 일반 이동 잠금 없음 — 플레이어가 ground 위에서 자유롭게 이동해 줄을 늘림(stretch)

VineGrappleController.Disconnect(launch:true)  # 우클릭 Up — 새총 발사
  -> stretch = Distance(player, anchor)
  -> player.ApplyLaunchVelocity((anchor - player).normalized * (stretch * forcePerUnit))

[대화 시작]
DialogueManager.StartDialogue()
  -> player.InputLocked = true  # 모든 이동/점프/전투 입력 차단
DialogueManager.EndDialogue()
  -> player.InputLocked = false
```

---

## 8. 클래스 관계 요약 다이어그램

```
                  IMovable
                    |
  CharacterBase ----+
    |               |
    |         PlayerBase --------+--------- SkillManager
    |           |       \        |          (SpeedMultiplier,
    |    PlayerController \      |           JumpMultiplier,
    |                PlayerCombat|           CurrentMp)
    |                   |        |
    |              Projectile    |
    |                   |    CheckpointManager
    |              CameraShake   |
    |                        DialogueManager
  EnemyShooter                (InputLocked)
```

| 관계 | 방식 | 설명 |
|------|------|------|
| `PlayerController` -> `PlayerBase` | 상속 | `ReadInput()` 구현 |
| `PlayerCombat` -> `PlayerBase` | 컴포지션 | `GetComponent<PlayerBase>()` |
| `Projectile` -> `PlayerCombat` | 런타임 참조 | 가드/패리 판정 시 |
| `Projectile` -> `PlayerBase` | 런타임 참조 | 피격 시 `Die()` 호출 |
| `DialogueManager` -> `PlayerBase` | `FindAnyObjectByType` | `InputLocked` 제어 |
| `CheckpointManager` -> `PlayerBase` | 메서드 파라미터 | `RespawnPlayer(player)` |
| 각종 Controller -> `PlayerBase` | Inspector 참조 | `CurrentMp`, `SpeedMultiplier` 등 |
