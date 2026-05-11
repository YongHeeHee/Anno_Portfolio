# Anno 프로젝트 전체 구조 Overview

## 1. 프로젝트 개요

Anno는 **사계절(봄/여름/가을/겨울) 테마의 2D 플랫포머 게임**이다.
각 계절마다 고유한 스킬 2개씩, 총 8개의 시즌 스킬을 사용하여 퍼즐과 전투를 풀어나간다.

---

## 2. 폴더 구조 (Folder Map)

```
Assets/Scripts/
├── CharacterBase.cs          # 모든 캐릭터의 최상위 abstract 클래스
├── PlayerBase.cs             # 플레이어 전용 abstract 클래스 (이동, 점프, MP)
├── PlayerController.cs       # 실제 입력 처리 (Input System)
├── PlayerCombat.cs           # 공격, 가드, 패리 시스템
├── IMovable.cs               # 이동 인터페이스
├── EnemyShooter.cs           # 투사체 발사 적
├── Projectile.cs             # 투사체 로직 (가드/패리/반사)
├── SkillManager.cs           # 계절 스킬 전환 관리 (싱글톤)
├── SeasonSkillType.cs        # 스킬 종류 열거형
├── SeasonSceneController.cs  # 계절별 씬 로드/언로드 (싱글톤)
│
├── Spring/                   # 봄 스킬
│   ├── SplineGrowthController.cs   # Growth 스킬 (덩굴 생성)
│   ├── GrowthInteractable.cs       # Growth 대상 오브젝트
│   └── VineGrappleController.cs    # VineGrapple 스킬 (덩굴 이동)
│
├── Summer/                   # 여름 스킬
│   ├── ExplosionController.cs      # Explosion 스킬 (차징 폭발)
│   ├── ExplosionInteractable.cs    # 폭발 대상 오브젝트
│   └── IgniteController.cs         # Ignite 스킬 (이속/점프 강화)
│
├── Autumn/                   # 가을 스킬
│   ├── LightenController.cs        # Lighten 스킬 (무게 감소)
│   ├── LightenInteractable.cs      # 경감 대상 오브젝트
│   └── GhostController.cs          # Ghost 스킬 (벽 통과)
│
├── Winter/                   # 겨울 스킬
│   ├── FreezeController.cs         # Freeze 스킬 (오브젝트 결빙)
│   ├── FreezeInteractable.cs       # 결빙 대상 오브젝트
│   └── AnchorController.cs         # Anchor 스킬 (공중 정지)
│
├── Checkpoint/               # 체크포인트 시스템
│   ├── CheckpointManager.cs        # 리스폰 관리 (싱글톤)
│   ├── CheckpointTrigger.cs        # 체크포인트 트리거
│   ├── KillZone.cs                 # 즉사 영역
│   └── IResettable.cs              # 리스폰 시 상태 초기화 인터페이스
│
├── Dialogue/                 # 대화 시스템
│   ├── DialogueData.cs             # 대화 데이터 ScriptableObject
│   ├── DialogueManager.cs          # 대화 진행 관리 (싱글톤)
│   ├── DialoguePanel.cs            # 대화 UI 패널 (DOTween 타이핑)
│   └── NPCInteraction.cs           # NPC 상호작용 트리거
│
├── Camera/                   # 카메라
│   ├── CameraShake.cs              # Cinemachine Impulse 기반 흔들림 (싱글톤)
│   └── ParallaxBackground.cs       # 시차 배경 스크롤
│
├── UI/                       # UI
│   └── SeasonSkillUI.cs            # 시즌 스킬 슬롯 + MP 바 HUD
│
├── Editor/                   # 에디터 전용 도구
│   ├── CinemachineSetupTool.cs     # Cinemachine 자동 설정 메뉴
│   └── DialogueEditorWindow.cs     # 대화 SO 생성 에디터 윈도우
│
└── Debug/                    # 디버그/테스트
    └── PingPongMover.cs            # 좌우 왕복 이동 (테스트용)
```

---

## 3. 시스템 간 의존 관계도

```
                        +-----------------+
                        |  SkillManager   |  (계절 전환 중앙 허브)
                        |   [Singleton]   |
                        +--------+--------+
                                 |
            +--------------------+--------------------+
            |                    |                    |
     Spring Skills        Summer Skills        Autumn/Winter Skills
  (Growth, VineGrapple)  (Explosion, Ignite)  (Lighten, Ghost, Freeze, Anchor)
            |                    |                    |
            +--------------------+--------------------+
                                 |
                        +--------v--------+
                        |   PlayerBase    |  (MP 소모, 상태 변경)
                        |  [Abstract]     |
                        +--------+--------+
                                 |
                  +--------------+--------------+
                  |                             |
         +--------v--------+          +--------v--------+
         | PlayerController |          |  PlayerCombat   |
         | (입력 처리)       |          | (공격/가드/패리) |
         +-----------------+          +--------+--------+
                                               |
                                      +--------v--------+
                                      |   Projectile    |
                                      | (반사 판정)      |
                                      +--------+--------+
                                               |
                                      +--------v--------+
                                      |  EnemyShooter   |
                                      | (투사체 발사)    |
                                      +-----------------+

         +-------------------+          +---------------------+
         | CheckpointManager |--------->| SeasonSceneController|
         |   [Singleton]     |          |    [Singleton]       |
         +--------+----------+          +---------------------+
                  |
      +-----------+-----------+
      |                       |
 CheckpointTrigger       KillZone
      |
 IResettable (ExplosionInteractable 등)

         +-------------------+
         | DialogueManager   |--------> DialoguePanel
         |   [Singleton]     |          (UI 표시, DOTween 타이핑)
         +--------+----------+
                  |
            NPCInteraction
                  |
            DialogueData (ScriptableObject)

         +-------------------+
         |   CameraShake     |  <-- PlayerCombat, Projectile,
         |   [Singleton]     |      ExplosionController 에서 호출
         +-------------------+
```

---

## 4. 싱글톤 목록

| 클래스 | 위치 | 역할 |
|--------|------|------|
| `SkillManager` | Scripts/ | 계절 스킬 전환, 해금 상태 관리 |
| `SeasonSceneController` | Scripts/ | 계절별 씬 Additive 로드/언로드 |
| `CheckpointManager` | Scripts/Checkpoint/ | 체크포인트 등록, 리스폰 처리 |
| `DialogueManager` | Scripts/Dialogue/ | 대화 시작/진행/종료 관리 |
| `CameraShake` | Scripts/Camera/ | Cinemachine Impulse 카메라 흔들림 |
| `SplineGrowthController` | Scripts/Spring/ | Growth 스킬 (덩굴 Spline 생성) |
| `VineGrappleController` | Scripts/Spring/ | VineGrapple 스킬 (덩굴 테더 + 새총 발사) |
| `ExplosionController` | Scripts/Summer/ | Explosion 스킬 (차징 폭발) |
| `IgniteController` | Scripts/Summer/ | Ignite 스킬 (이속/점프 강화) |
| `LightenController` | Scripts/Autumn/ | Lighten 스킬 (무게 경감) |
| `GhostController` | Scripts/Autumn/ | Ghost 스킬 (벽 통과) |
| `FreezeController` | Scripts/Winter/ | Freeze 스킬 (결빙) |
| `AnchorController` | Scripts/Winter/ | Anchor 스킬 (공중 정지) |

모든 싱글톤은 동일한 패턴을 사용한다:

```csharp
public static T Instance { get; private set; }

private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
}
```

---

## 5. 핵심 디자인 패턴

### 5.1 Interface

| 인터페이스 | 위치 | 멤버 | 구현 클래스 |
|-----------|------|------|------------|
| `IMovable` | Scripts/ | `IsGrounded`, `SetMoveInput()`, `RequestJump()` | `PlayerBase` |
| `IResettable` | Scripts/Checkpoint/ | `ResetState()` | `ExplosionInteractable` |

### 5.2 Abstract Class

| 클래스 | 추상 멤버 | 자식 클래스 |
|--------|----------|------------|
| `CharacterBase` | `Die()` | `PlayerBase`, `EnemyShooter` |
| `PlayerBase` | `ReadInput()` | `PlayerController` |

### 5.3 Controller + Interactable 패턴

각 시즌 스킬은 **Controller(로직 담당) + Interactable(대상 오브젝트)** 쌍으로 구성된다:

| Controller (싱글톤) | Interactable (씬 오브젝트) | 상호작용 방식 |
|---------------------|--------------------------|--------------|
| `SplineGrowthController` | `GrowthInteractable` | 마우스 클릭으로 덩굴 생성 시작 |
| `ExplosionController` | `ExplosionInteractable` | 차징 후 범위 내 오브젝트 발사 |
| `LightenController` | `LightenInteractable` | 클릭으로 무게 감소 적용 |
| `FreezeController` | `FreezeInteractable` | 클릭으로 결빙 적용 |

### 5.4 ScriptableObject

- `DialogueData` : 대화 데이터를 에셋으로 관리. `nextDialogue` 필드로 대화 체이닝 가능.

---

## 6. 스크립트 전체 목록 (한 줄 설명)

| # | 스크립트 | 한 줄 설명 |
|---|---------|-----------|
| 1 | `CharacterBase` | 모든 캐릭터 공통 기반. `SpriteRenderer`, `IsDead`, `FacingDirection`, 추상 `Die()` |
| 2 | `PlayerBase` | 플레이어 이동/점프/MP/지면판정/사망/리스폰. `IMovable` 구현 |
| 3 | `PlayerController` | Input System으로 Move/Jump 입력을 읽어 `PlayerBase`에 전달 |
| 4 | `PlayerCombat` | Attack 트리거, Guard/Parry 판정, `MovementLocked` 제어 |
| 5 | `IMovable` | 이동 가능 캐릭터 인터페이스 (`IsGrounded`, `SetMoveInput`, `RequestJump`) |
| 6 | `EnemyShooter` | 일정 간격으로 플레이어를 향해 `Projectile`을 발사하는 적 |
| 7 | `Projectile` | 투사체 이동, 플레이어 피격/가드/패리 판정, 반사 시 적 처치 |
| 8 | `SkillManager` | 계절 단축키(1~4) 입력 처리, 활성 시즌 관리, 해금 상태 |
| 9 | `SeasonSkillType` | 8개 스킬 열거형 (`Growth`, `VineGrapple`, `Explosion`, `Ignite`, `Lighten`, `Ghost`, `Freeze`, `Anchor`) |
| 10 | `SeasonSceneController` | 시즌 씬 Additive 로드/언로드, SpawnPoint 이동 |
| 11 | `SplineGrowthController` | 마우스 드래그로 SpriteShape Spline 덩굴 생성, 시간 경과 후 축소 소멸 |
| 12 | `GrowthInteractable` | Growth 스킬 대상. 클릭 시 `SplineGrowthController.StartGrowth()` 호출 |
| 13 | `VineGrappleController` | 우클릭 hold로 앵커에 테더 연결, 떼면 stretch × forcePerUnit 속도로 새총 발사 |
| 14 | `ExplosionController` | 좌클릭 홀드로 차징, 릴리즈 시 범위 내 `ExplosionInteractable` 발사 |
| 15 | `ExplosionInteractable` | 폭발로 날아가는 오브젝트. `IResettable` 구현, 체크포인트 리스폰 시 원위치 |
| 16 | `IgniteController` | 우클릭 홀드로 이속/점프력 배율 증가, 초당 MP 소모 |
| 17 | `LightenController` | 클릭한 `LightenInteractable`의 중력/질량을 감소시킴 |
| 18 | `LightenInteractable` | 경감 대상. 일정 시간 후 깜박임 경고와 함께 원래 무게로 복원 |
| 19 | `GhostController` | 우클릭 홀드로 GhostWall 레이어 충돌 무시, 투명화, 초당 MP 소모 |
| 20 | `FreezeController` | 클릭한 `FreezeInteractable`을 일정 시간 결빙(완전 고정) |
| 21 | `FreezeInteractable` | 결빙 대상. 일정 시간 후 깜박임과 함께 해동 |
| 22 | `AnchorController` | 우클릭 홀드로 플레이어 공중 정지 (중력 0, FreezeAll), 초당 MP 소모 |
| 23 | `CheckpointManager` | 체크포인트 등록/리스폰 처리, 리스폰 시 활성 스킬 해제 |
| 24 | `CheckpointTrigger` | 트리거 진입 시 `CheckpointManager`에 위치 등록 |
| 25 | `KillZone` | 트리거 진입 시 `PlayerBase.Die()` 호출 |
| 26 | `IResettable` | 리스폰 시 상태 초기화 인터페이스 (`ResetState()`) |
| 27 | `DialogueData` | 대화 ScriptableObject. 화자, 대사 목록, 타이핑 속도, 다음 대화 연결 |
| 28 | `DialogueManager` | 대화 시작/진행/종료, 진행 중 플레이어 `InputLocked` |
| 29 | `DialoguePanel` | 대화 UI 동적 생성, DOTween 타이핑 효과, 초상화 하이라이트 |
| 30 | `NPCInteraction` | NPC 트리거 범위 진입 시 "E" 인디케이터, Interact 입력으로 대화 시작 |
| 31 | `CameraShake` | `CinemachineImpulseSource`를 이용한 피격/패리/공격 카메라 흔들림 |
| 32 | `ParallaxBackground` | 카메라 이동에 따른 레이어별 시차 스크롤, 무한 반복 지원 |
| 33 | `SeasonSkillUI` | 시즌 슬롯 아이콘 + MP 바 HUD 동적 생성 |
| 34 | `PingPongMover` | 디버그용 좌우 왕복 이동 컴포넌트 |
| 35 | `CinemachineSetupTool` | 에디터 메뉴에서 Cinemachine Camera + CameraShake 자동 생성 |
| 36 | `DialogueEditorWindow` | 대화 SO를 GUI로 작성/미리보기/생성하는 에디터 윈도우 |

---

## 7. 사용 외부 패키지

| 패키지 | 용도 |
|--------|------|
| Unity Input System | 플레이어/NPC 입력 처리 |
| Unity Cinemachine | 카메라 추적, Impulse 흔들림 |
| Unity 2D SpriteShape | Growth 스킬 덩굴 시각화 |
| DOTween | 대화 타이핑 애니메이션 |
