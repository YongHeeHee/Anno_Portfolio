# Project Structure

## Unity 버전 & 렌더 파이프라인
- URP (Universal Render Pipeline)
- Input System (New Input System)

## 외부 플러그인
- **DOTween Pro** (`Plugins/Demigiant/`) - 트윈 애니메이션 (SpeechBubble에서 사용)
- **Cinemachine** - 카메라 추적 및 카메라 쉐이크
- **TextMesh Pro** - UI 텍스트

## 폴더 구조

```
Assets/
├── Animations/
│   ├── Wizard_White/          # 플레이어 애니메이션
│   │   ├── Wizard_White.controller
│   │   ├── Idle, Walk, Jump, Fall, Landing.anim
│   │   ├── Attack1, Attack2, WalkAttack.anim
│   │   ├── Guard, Parry.anim
│   │   ├── Death, Rest.anim
│   │   └── (Animator에서 "Lock" 태그 사용 → 공격/패리 중 이동 잠금)
│   └── NPC/
│       └── Wizard/
│           ├── NPC_Wizard.controller
│           └── Idle.anim
│
├── Fonts/
│   └── MalgunGothic.ttf       # 맑은 고딕 (UI용)
│
├── Plugins/
│   └── Demigiant/             # DOTween Pro
│
├── Prefabs/
│   ├── VinePrefab.prefab      # 덩굴 SpriteShape 프리팹 (Growth 스킬)
│   ├── Interaction/
│   │   ├── Spring/
│   │   │   └── GrowthInteratable_Pot.prefab
│   │   ├── Summer/
│   │   │   └── ChargeIndicatorPrefab.prefab  # 폭발 차징 범위 인디케이터
│   │   ├── Autumn/
│   │   └── Winter/
│   └── SeasonKillUI/
│       └── SeasonSlotPrefab.prefab  # 계절 스킬 UI 슬롯
│
├── Resources/
│   └── DOTweenSettings.asset
│
├── Scenes/
│   ├── GameScene.unity        # 베이스 씬 (Player, 매니저, 카메라, UI 등 영구 오브젝트)
│   └── SpringScene.unity      # 봄 시즌 맵 (Tilemap + 데코 + NPC 배치). Additive 로드
│
├── ScriptableObject/
│   ├── Dialogue/
│   │   └── NPC_Wizard/
│   │       ├── Hello.asset    # NPC_Wizard 첫 번째 대화
│   │       └── Bye.asset      # NPC_Wizard 두 번째 대화
│
├── Scripts/                   # ★ 핵심 스크립트 (아래 ARCHITECTURE.md 참조)
│   ├── IMovable.cs
│   ├── CharacterBase.cs
│   ├── PlayerBase.cs
│   ├── PlayerController.cs
│   ├── PlayerCombat.cs
│   ├── EnemyShooter.cs
│   ├── Projectile.cs
│   ├── SeasonSceneController.cs   # 시즌 씬 Additive 로드/언로드 관리
│   ├── Camera/
│   │   ├── CameraShake.cs
│   │   └── ParallaxBackground.cs   # 카메라 위치 기반 배경 시차 스크롤
│   ├── Checkpoint/
│   │   ├── IResettable.cs          # 체크포인트 리셋 대상 인터페이스
│   │   ├── CheckpointManager.cs
│   │   ├── CheckpointTrigger.cs
│   │   └── KillZone.cs             # 진입 시 마지막 체크포인트로 리스폰
│   ├── Deco/
│   │   ├── DecoScatter.cs          # 절차적 데코 산포 (잔디·돌 등)
│   │   └── DecoScatterPreset.cs    # 산포 프리셋 SO
│   ├── Dialogue/
│   │   ├── DialogueManager.cs
│   │   ├── DialogueData.cs
│   │   ├── DialoguePanel.cs
│   │   └── NPCInteraction.cs
│   ├── SeasonSkillType.cs
│   ├── SkillManager.cs
│   ├── Spring/
│   │   ├── GrowthInteractable.cs
│   │   ├── SplineGrowthController.cs
│   │   ├── VineGrappleController.cs
│   │   └── VineLaunchInteractable.cs   # VineGrapple 앵커 컴포넌트
│   ├── Summer/
│   │   ├── ExplosionController.cs
│   │   ├── ExplosionInteractable.cs
│   │   └── IgniteController.cs
│   ├── Autumn/
│   │   ├── LightenController.cs
│   │   ├── LightenInteractable.cs
│   │   └── GhostController.cs
│   ├── Winter/
│   │   ├── FreezeController.cs
│   │   ├── FreezeInteractable.cs
│   │   └── AnchorController.cs
│   ├── UI/
│   │   └── SeasonSkillUI.cs
│   ├── Debug/
│   │   └── PingPongMover.cs   # 디버그용 좌우 왕복 이동 컴포넌트
│   └── Editor/
│       ├── DialogueEditorWindow.cs
│       └── CinemachineSetupTool.cs
│
├── Settings/                  # URP 렌더링 설정
│
├── TextMesh Pro/              # TMP 기본 리소스
│
└── README/                    # ★ 이 문서들
    ├── PROJECT_STRUCTURE.md
    ├── ARCHITECTURE.md
    ├── CODING_CONVENTIONS.md
    ├── DESIGN_PRINCIPLES.md
    ├── GAME_DESIGN.md
    └── WORK_LOG.md
```

## 씬 구조 — GameScene + 시즌 씬 Additive

### GameScene (베이스 씬, 항상 활성)
플레이 전체에서 유지되는 영구 오브젝트만 보유. 시즌 전환 시에도 살아남음.
- Main Camera (CinemachineBrain)
- CM Camera (CinemachineCamera + CinemachineFollow + ImpulseListener)
- CameraShake (CinemachineImpulseSource)
- Player (PlayerController) - Tag: "Player", 자식에 HitPoint Transform
- SkillManager, DialogueManager, CheckpointManager, SeasonSceneController
- SeasonSkillUI 등 UI 캔버스

### 시즌 씬 (SpringScene / SummerScene / AutumnScene / WinterScene)
해당 시즌의 맵 데이터만 보유. **Player 없음.** 런타임에 Additive로 로드.
- Grid (1개) + 스테이지별 Tilemap 그룹 (Stage_T1/T2/M1/M2/C1/C2/X/R)
  - 각 스테이지: `Ground_XX` (CompositeCollider2D), `Hazard_XX` (TilemapCollider2D, Trigger)
  - 단일 Grid 사용 — Grid 분할 시 좌표계 분리/경계 seam 문제로 Stage_XX는 빈 부모 GameObject로만 그룹화
- 데코 (DecoScatter 절차 산포 + 큰 단독 데코는 GameObject 직접 배치)
- NPC (NPCInteraction + Animator) - Trigger Collider로 상호작용 범위 설정
- Enemy (EnemyShooter) - firePoint Transform으로 발사 위치 지정
- 체크포인트 / KillZone 트리거
- VineLaunchInteractable 등 시즌 스킬용 인터랙터블

### 진입 원칙
- 항상 GameScene에서 시작. 시즌 씬 단독 진입 시 Player 없어 NullReference 발생 가능.
- 시즌 씬에서 Player 참조는 매니저 싱글톤 또는 `FindObjectOfType<PlayerBase>()` 사용. 직접 SerializeField 박지 말 것.

## 레이어 구성
- **Default** - 일반 오브젝트
- **Ground** (6) - 지면 (플레이어 착지 판정용)
- **GhostWall** (7) - Ghost 스킬로 통과 가능한 벽
- **EnemyProjectile** - 적 투사체 (발사 시 설정)
- **ReflectedProjectile** - 패리로 반사된 투사체 (반사 시 변경)
- **Enemy** - 적 캐릭터
- **VineGround** (11) - VineGrapple 시야 차단용 지면 분류 (필요 시 사용)
