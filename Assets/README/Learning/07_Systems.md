# 게임 시스템 (Systems)

## 개요
- 체크포인트, 카메라, 대화, UI, 전투, 에디터 도구 등 계절 스킬 외의 핵심 게임 시스템들을 정리한다.

---

## 1. Checkpoint 시스템

### 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| CheckpointManager | 체크포인트 등록 및 리스폰 관리 싱글턴 |
| CheckpointTrigger | 개별 체크포인트 트리거 영역 |
| IResettable | 리스폰 시 상태 초기화가 필요한 오브젝트용 인터페이스 |
| KillZone | 플레이어 사망 영역 |

### 각 스크립트 상세

#### CheckpointManager (`Assets/Scripts/Checkpoint/CheckpointManager.cs`)
- **상속:** MonoBehaviour
- **역할:** 체크포인트 위치를 저장하고, 플레이어 리스폰을 처리하는 싱글턴 매니저. 리스폰 시 활성 스킬을 해제하고, 등록된 `OnRespawn` 이벤트를 발행한다.
- **주요 변수:**
  - `spawnPointTag` (string, 기본값 "SpawnPoint") - 체크포인트가 없을 때 fallback으로 사용할 태그
  - `playerTag` (string, 기본값 "Player") - 플레이어 태그
  - `currentCheckpointPos` (Vector3) - 현재 등록된 체크포인트 위치
  - `hasCheckpoint` (bool) - 체크포인트 등록 여부
  - `OnRespawn` (event Action) - 리스폰 직후 발행되는 이벤트. IResettable 구현 오브젝트가 구독
- **주요 메서드:**
  - `RegisterCheckpoint(Vector3 pos)` - 체크포인트 위치 등록
  - `RespawnPlayer(PlayerBase player)` - 활성 스킬(Ghost, Ignite, Anchor) 해제 후, 체크포인트 또는 SpawnPoint로 플레이어 이동. MP를 최대치로 회복하고 OnRespawn 이벤트 발행
  - `DeactivateActiveSkills()` - GhostController, IgniteController, AnchorController의 활성 스킬을 비활성화
  - `FindSpawnPoint(PlayerBase player)` - SpawnPoint 태그를 가진 오브젝트를 찾아 위치 반환. 없으면 현재 플레이어 위치 반환
- **다른 스크립트와의 관계:** CheckpointTrigger에서 `RegisterCheckpoint()` 호출. PlayerBase의 `Die()`에서 `RespawnPlayer()` 호출. GhostController, IgniteController, AnchorController를 참조하여 스킬 해제

#### CheckpointTrigger (`Assets/Scripts/Checkpoint/CheckpointTrigger.cs`)
- **상속:** MonoBehaviour
- **RequireComponent:** Collider2D
- **역할:** 플레이어가 트리거 영역에 진입하면 해당 위치를 체크포인트로 등록한다. 시각/청각 피드백을 제공한다.
- **주요 변수:**
  - `playerTag` (string) - 플레이어 태그
  - `registerOffset` (Vector2) - 체크포인트 등록 위치 오프셋
  - `activationSound` (AudioClip) - 활성화 사운드 (선택)
  - `activatedVisual` (GameObject) - 활성화 시 표시할 시각 오브젝트 (선택)
  - `activated` (bool) - 이미 활성화되었는지 여부 (1회만 동작)
- **주요 메서드:**
  - `OnTriggerEnter2D(Collider2D other)` - Player 태그 확인 후, CheckpointManager에 위치 등록. 사운드 재생 및 시각 오브젝트 활성화
  - `OnDrawGizmosSelected()` - Scene 뷰에서 등록 위치를 시안색 구체로 표시
- **다른 스크립트와의 관계:** CheckpointManager.Instance.RegisterCheckpoint()를 호출

#### IResettable (`Assets/Scripts/Checkpoint/IResettable.cs`)
- **역할:** 리스폰 시 상태를 초기화해야 하는 오브젝트가 구현하는 인터페이스
- **주요 메서드:**
  - `ResetState()` - 오브젝트 상태를 초기 상태로 복원
- **다른 스크립트와의 관계:** CheckpointManager.OnRespawn 이벤트에 구독하여 리스폰 시 호출됨

#### KillZone (`Assets/Scripts/Checkpoint/KillZone.cs`)
- **상속:** MonoBehaviour
- **RequireComponent:** Collider2D
- **역할:** 플레이어가 트리거 영역에 진입하면 사망 처리
- **주요 변수:**
  - `playerTag` (string, 기본값 "Player") - 플레이어 태그
- **주요 메서드:**
  - `OnTriggerEnter2D(Collider2D other)` - Player 태그 확인 후 `PlayerBase.Die()` 호출
- **다른 스크립트와의 관계:** PlayerBase.Die()를 직접 호출

### Checkpoint 동작 흐름
1. 플레이어가 CheckpointTrigger 영역에 진입 -> `CheckpointManager.RegisterCheckpoint()` 호출
2. 플레이어가 KillZone에 진입하거나 기타 사유로 `PlayerBase.Die()` 호출
3. `CheckpointManager.RespawnPlayer()` 실행
4. 활성 스킬(Ghost, Ignite, Anchor) 모두 비활성화
5. 플레이어 위치를 체크포인트(또는 SpawnPoint)로 이동, MP 전체 회복
6. `OnRespawn` 이벤트 발행 -> IResettable 구현 오브젝트들의 `ResetState()` 호출

---

## 2. Camera 시스템

### 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| CameraShake | Cinemachine Impulse를 활용한 카메라 흔들림 효과 |
| ParallaxBackground | 카메라 이동에 따른 배경 레이어별 시차(Parallax) 스크롤 |

### 각 스크립트 상세

#### CameraShake (`Assets/Scripts/Camera/CameraShake.cs`)
- **상속:** MonoBehaviour
- **RequireComponent:** CinemachineImpulseSource
- **역할:** 피격, 패리, 공격 등 게임 이벤트에 따라 카메라를 흔드는 싱글턴. Cinemachine의 ImpulseSource를 사용한다.
- **주요 변수:**
  - `hitIntensity` (float, 기본값 0.5) - 플레이어 피격 시 흔들림 강도
  - `parryIntensity` (float, 기본값 0.8) - 패리 성공 시 흔들림 강도
  - `attackIntensity` (float, 기본값 0.15) - 공격 시 흔들림 강도
  - `impulseSource` (CinemachineImpulseSource) - Cinemachine Impulse 소스 컴포넌트
- **주요 메서드:**
  - `Shake(float intensity)` - 지정된 강도로 임펄스 생성
  - `ShakeOnHit()` - hitIntensity로 흔들림
  - `ShakeOnParry()` - parryIntensity로 흔들림
  - `ShakeOnAttack()` - attackIntensity로 흔들림
- **다른 스크립트와의 관계:** Projectile에서 `CameraShake.Instance?.ShakeOnHit()`, `ShakeOnParry()` 호출

#### ParallaxBackground (`Assets/Scripts/Camera/ParallaxBackground.cs`)
- **상속:** MonoBehaviour
- **역할:** 카메라 이동에 따라 배경 레이어별로 다른 속도로 스크롤하여 원근감(Parallax)을 표현한다. 무한 스크롤이 활성화된 레이어는 좌/중앙/우 3장으로 무한 반복한다.
- **주요 구조체:**
  - `ParallaxLayer` - target(Transform), parallaxFactorX(float, 0~1), parallaxFactorY(float, 0~1), infiniteScroll(bool)
    - parallaxFactor 0 = 카메라에 완전 고정 (가장 먼 배경), 1 = 월드에 고정 (가장 가까운 배경)
- **주요 변수:**
  - `layers` (ParallaxLayer[]) - 배경 레이어 목록 (뒤에서 앞 순서)
  - `cam` (Transform) - 메인 카메라 Transform
  - `camStartPosition` (Vector3) - 시작 시 카메라 위치
  - `leftClones`, `rightClones` (Transform[]) - 무한 스크롤용 좌/우 복제본
  - `spriteWidths` (float[]) - 각 레이어의 스프라이트 너비
- **주요 메서드:**
  - `Start()` - 카메라 참조, 시작 위치 저장, 무한 스크롤 레이어 복제본 생성
  - `SetupInfiniteScroll(int index)` - 좌/우 복제본을 생성하여 무한 반복 준비
  - `LateUpdate()` - 각 레이어의 위치를 parallaxFactor에 따라 카메라 이동량 대비 오프셋 계산. `offsetX = camDelta.x * (1 - parallaxFactorX)`
  - `HandleInfiniteScroll(int index)` - 카메라가 스프라이트 범위를 벗어나면 3장 모두 시프트하여 끊김 없는 스크롤 구현
- **다른 스크립트와의 관계:** Camera.main을 참조. 다른 스크립트와 직접 의존 관계 없음

---

## 3. Dialogue 시스템

### 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| DialogueData | 대화 데이터를 저장하는 ScriptableObject |
| DialogueManager | 대화 흐름을 관리하는 싱글턴 매니저 |
| DialoguePanel | 대화 UI 패널을 코드로 생성하고 타이핑 효과를 제공 |
| NPCInteraction | NPC와의 상호작용 트리거 및 대화 시작 |

### 각 스크립트 상세

#### DialogueData (`Assets/Scripts/Dialogue/DialogueData.cs`)
- **상속:** ScriptableObject
- **역할:** 대화 데이터를 에셋으로 저장. `[CreateAssetMenu]`로 에디터에서 생성 가능
- **주요 변수:**
  - `speakerName` (string) - NPC 이름
  - `lines` (Line[]) - 대화 줄 배열
  - `typingSpeed` (float, 기본값 30) - 타이핑 속도 (초당 글자 수)
  - `nextDialogue` (DialogueData) - 다음 대화 데이터 (연결 리스트 형태)
- **내부 클래스/열거형:**
  - `Line` - speaker(Speaker), text(string) 구성
  - `Speaker` enum - NPC, Player
- **다른 스크립트와의 관계:** DialogueManager, NPCInteraction, DialogueEditorWindow에서 사용

#### DialogueManager (`Assets/Scripts/Dialogue/DialogueManager.cs`)
- **상속:** MonoBehaviour
- **역할:** 대화 진행을 관리하는 싱글턴. 대화 시작, 줄 넘기기, 타이핑 스킵, 대화 종료를 처리한다. 대화 중 플레이어 입력을 잠근다.
- **주요 변수:**
  - `IsDialogueActive` (bool) - 대화 진행 중 여부
  - `playerName` (string, 기본값 "???") - 플레이어 이름
  - `playerPortrait` (Sprite) - 플레이어 초상화
  - `panel` (DialoguePanel) - UI 패널 참조
  - `interactAction` (InputAction) - Input System의 Interact 액션
  - `currentDialogue` (DialogueData) - 현재 재생 중인 대화 데이터
  - `currentLineIndex` (int) - 현재 줄 인덱스
  - `lineComplete` (bool) - 현재 줄 타이핑 완료 여부
- **주요 메서드:**
  - `StartDialogue(DialogueData dialogue, Sprite npcPortrait)` - 대화 시작. 플레이어 InputLocked 설정, 패널 표시
  - `ShowCurrentLine()` - 현재 인덱스의 대화 줄을 패널에 표시. 모든 줄을 다 보여줬으면 `EndDialogue()` 호출
  - `AdvanceLine()` - 다음 줄로 진행
  - `EndDialogue()` - 대화 종료. 패널 숨기기, InputLocked 해제
  - `Update()` - Interact 입력 또는 마우스 좌클릭 시: 타이핑 중이면 즉시 완료, 타이핑 완료 상태면 다음 줄로 진행
- **다른 스크립트와의 관계:** NPCInteraction에서 `StartDialogue()` 호출. DialoguePanel에 UI 표시 위임. PlayerBase.InputLocked를 제어

#### DialoguePanel (`Assets/Scripts/Dialogue/DialoguePanel.cs`)
- **상속:** MonoBehaviour
- **역할:** 대화 UI 패널을 런타임에 코드로 생성한다. Screen Space Overlay Canvas를 사용하며, DOTween으로 타이핑 효과를 구현한다.
- **주요 변수:**
  - `font` (Font) - 대화 텍스트 폰트 (비워두면 기본 폰트)
  - `panelHeight` (float, 기본값 180) - 패널 높이(px)
  - `portraitSize` (float, 기본값 130) - 초상화 이미지 크기(px)
  - `playerPortraitImage`, `npcPortraitImage` (Image) - 좌/우 초상화 이미지
  - `speakerNameText`, `dialogueText` (Text) - 화자 이름과 대화 텍스트
  - `typingTween` (Tweener) - DOTween 타이핑 애니메이션
  - `isTyping` (bool) - 타이핑 진행 중 여부 (프로퍼티 `IsTyping`으로 노출)
- **주요 메서드:**
  - `CreatePanelUI()` - Canvas, Panel, Portrait, Text 등 UI 요소를 코드로 생성 (기준 해상도 1920x1080)
  - `Show(Sprite playerPortrait, Sprite npcPortrait)` - 초상화 설정 및 패널 활성화
  - `ShowLine(string speakerName, string text, Speaker speaker, float typingSpeed, Action onComplete)` - 화자에 따라 초상화 밝기 조절, DOTween으로 한 글자씩 타이핑 효과
  - `CompleteTyping()` - 타이핑 즉시 완료
  - `Hide()` - 패널 비활성화, 트윈 정리
- **다른 스크립트와의 관계:** DialogueManager에서 생성 및 제어. DOTween(DG.Tweening) 라이브러리 사용

#### NPCInteraction (`Assets/Scripts/Dialogue/NPCInteraction.cs`)
- **상속:** MonoBehaviour
- **역할:** NPC 오브젝트에 부착. 플레이어가 범위에 들어오면 상호작용 인디케이터("E")를 표시하고, Interact 입력 시 대화를 시작한다.
- **주요 변수:**
  - `dialogue` (DialogueData) - NPC의 대화 데이터 SO
  - `npcPortrait` (Sprite) - NPC 초상화
  - `indicatorOffset` (Vector2, 기본값 (0, 1.5)) - 인디케이터 표시 위치 오프셋
  - `interactIndicator` (GameObject) - 커스텀 인디케이터 (비워두면 기본 "E" 자동 생성)
  - `playerInRange` (bool) - 플레이어가 범위 내에 있는지
  - `currentDialogue` (DialogueData) - 현재 재생할 대화 (nextDialogue로 연결 시 자동 진행)
  - `waitOneFrame` (bool) - 대화 종료 직후 재입력 방지용 1프레임 대기
- **주요 메서드:**
  - `OnTriggerEnter2D()` / `OnTriggerExit2D()` - 플레이어 범위 진입/이탈 시 인디케이터 표시/숨김
  - `StartDialogue()` - DialogueManager에 대화 시작 요청. nextDialogue가 있으면 currentDialogue를 다음으로 갱신
  - `CreateDefaultIndicator()` - World Space Canvas 기반의 기본 "E" 인디케이터를 코드로 생성
- **다른 스크립트와의 관계:** DialogueManager.Instance.StartDialogue()를 호출. DialogueData의 nextDialogue 연결 리스트를 따라감

### Dialogue 동작 흐름
1. 플레이어가 NPC의 트리거 영역에 진입 -> 인디케이터("E") 표시
2. Interact 키(E) 입력 -> `NPCInteraction.StartDialogue()` 호출
3. `DialogueManager.StartDialogue()` -> 플레이어 InputLocked = true, 패널 표시
4. `DialoguePanel.ShowLine()` -> DOTween으로 한 글자씩 타이핑
5. Interact 또는 마우스 클릭:
   - 타이핑 중이면 -> `CompleteTyping()`으로 즉시 완료
   - 타이핑 완료 상태면 -> `AdvanceLine()`으로 다음 줄 표시
6. 모든 줄을 다 보여주면 `EndDialogue()` -> InputLocked 해제, 패널 숨김
7. nextDialogue가 있으면 다음 대화 시작 시 연결된 DialogueData 사용

---

## 4. UI 시스템

### 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| SeasonSkillUI | 계절 스킬 슬롯 및 MP 바 UI |

### 각 스크립트 상세

#### SeasonSkillUI (`Assets/Scripts/UI/SeasonSkillUI.cs`)
- **상속:** MonoBehaviour
- **역할:** 화면 좌상단에 계절별 스킬 슬롯 아이콘과 MP 바를 표시하는 UI를 런타임에 코드로 생성한다. 활성/해금/잠금 상태에 따라 색상이 변경된다.
- **주요 구조체:**
  - `SeasonSlotConfig` - icon(Sprite) 계절 아이콘 스프라이트
- **주요 변수:**
  - `slotPrefab` (GameObject) - 슬롯 UI 프리팹 (null이면 기본 Image 생성)
  - `seasonConfigs` (SeasonSlotConfig[]) - 계절별 아이콘 설정 (0=봄, 1=여름, 2=가을, 3=겨울)
  - `player` (PlayerBase) - MP 표시용 플레이어 참조
  - `slotSize` (Vector2, 기본값 64x64) - 슬롯 크기
  - `spacing` (float, 기본값 8) - 슬롯 간 간격
  - `margin` (Vector2, 기본값 20x20) - 화면 모서리 여백
  - `borderThickness` (float, 기본값 4) - 활성 슬롯 테두리 두께
  - `activeBorderColor` (Color) - 활성 슬롯 테두리 색상 (금색)
  - `activeColor`, `unlockedColor`, `lockedColor` (Color) - 각 상태별 아이콘 색상
  - MP 바 관련: `mpBarHeight`, `mpBarGap`, `mpBarFillColor` 등
  - `cachedActiveIndex` (int) - 이전 프레임의 활성 계절 인덱스 (변경 시에만 갱신)
- **주요 메서드:**
  - `CreateUI()` - Canvas, 슬롯 패널, MP 바를 코드로 생성
  - `CreateSlots(Transform parent, float totalWidth)` - 계절별 슬롯과 테두리 Image 생성
  - `CreateMpBar(Transform canvasTransform, float totalWidth)` - MP 바 배경 및 Fill Image 생성 (Filled 타입, Horizontal 방향)
  - `RefreshSlots()` - SkillManager의 활성 계절 인덱스와 해금 상태에 따라 슬롯 색상 갱신
  - `UpdateMpBar()` - PlayerBase의 CurrentMp/MaxMp 비율로 fillAmount 갱신
- **다른 스크립트와의 관계:** SkillManager.Instance에서 ActiveSeasonIndex, IsSeasonUnlocked() 참조. PlayerBase에서 CurrentMp, MaxMp 참조

---

## 5. Combat 시스템 (전투)

### 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| Projectile | 투사체 로직. 플레이어 피격, 가드, 패리, 반사를 처리 |
| EnemyShooter | 적 슈터. 일정 간격으로 플레이어를 향해 투사체를 발사 |

### 각 스크립트 상세

#### Projectile (`Assets/Scripts/Projectile.cs`)
- **상속:** MonoBehaviour
- **역할:** 적이 발사한 투사체의 이동, 충돌 판정, 패리 반사 로직을 담당한다.
- **주요 변수:**
  - `lifetime` (float, 기본값 5) - 자동 파괴 시간
  - `hitRadius` (float, 기본값 0.2) - OverlapCircle 충돌 반지름
  - `direction` (Vector2) - 이동 방향
  - `speed` (float) - 이동 속도
  - `reflected` (bool) - 반사 여부
  - `preReflectMask` (int) - 반사 전 충돌 레이어 마스크 (Default + Ground)
  - `postReflectMask` (int) - 반사 후 충돌 레이어 마스크 (Enemy + Ground)
- **주요 메서드:**
  - `Init(Vector2 dir, float spd)` - 방향과 속도 초기화
  - `Awake()` - 스프라이트가 없으면 16x16 원형 텍스처 자동 생성. 색상 Cyan. 레이어 마스크 설정. `Destroy(gameObject, lifetime)` 예약
  - `FixedUpdate()` - `Physics2D.OverlapCircle()`로 충돌 감지. reflected 상태에 따라 다른 마스크 사용
  - `HandleCollision(Collider2D other)` - 반사 전: PlayerBase에 충돌 시 가드 판정 -> 패리 성공이면 `Reflect()`, 가드 판정 밖이면 `Die()` + CameraShake. 반사 후: EnemyShooter에 충돌 시 `Die()` 호출
  - `Reflect()` - 방향 반전, 속도 1.5배 증가, 레이어를 ReflectedProjectile로 변경, 색상을 금색으로 변경
- **다른 스크립트와의 관계:** EnemyShooter에서 생성. PlayerBase.Die(), PlayerCombat.IsGuarding/TryParry() 호출. CameraShake.Instance 참조. EnemyShooter.Die() 호출 (반사 시)

#### EnemyShooter (`Assets/Scripts/EnemyShooter.cs`)
- **상속:** CharacterBase
- **역할:** 일정 간격으로 플레이어를 향해 투사체를 발사하는 적 캐릭터. 사망 시 깜박임 연출 후 파괴된다.
- **주요 변수:**
  - `fireInterval` (float, 기본값 2.5) - 발사 간격(초)
  - `projectileSpeed` (float, 기본값 6) - 투사체 속도
  - `firePoint` (Transform) - 투사체 생성 위치 (비워두면 적 위치)
  - `playerTransform` (Transform) - 플레이어 위치 참조
  - `playerHitPoint` (Transform) - 플레이어의 HitPoint 자식 오브젝트 (조준 대상)
  - `fireTimer` (float) - 발사 타이머
- **주요 메서드:**
  - `Awake()` - Player 태그로 플레이어 찾기, HitPoint 자식 탐색
  - `Update()` - 플레이어 방향으로 스프라이트 flipX, fireTimer 갱신, 간격 도달 시 `Fire()` 호출
  - `Fire()` - GameObject를 새로 생성하여 Rigidbody2D, CircleCollider2D(trigger), Projectile 컴포넌트 추가. EnemyProjectile 레이어 설정. `Projectile.Init()`으로 방향/속도 전달
  - `Die()` - IsDead 플래그 설정 후 `DieSequence()` 코루틴 시작
  - `DieSequence()` - 5회 깜박임(0.08초 간격) 후 `Destroy(gameObject)`
- **다른 스크립트와의 관계:** CharacterBase를 상속. Projectile 컴포넌트를 동적 생성. Projectile.HandleCollision()에서 `Die()` 호출됨

### Combat 동작 흐름
1. EnemyShooter가 fireInterval마다 `Fire()` 호출 -> Projectile 오브젝트 동적 생성
2. Projectile이 direction * speed로 이동, FixedUpdate에서 `OverlapCircle`로 충돌 감지
3. **플레이어와 충돌 시:**
   - 가드 중이 아니면 -> `PlayerBase.Die()` + CameraShake
   - 가드 중이면 -> 가드 영역(GuardRect) 내인지 판정
     - 가드 영역 밖 -> `PlayerBase.Die()` + CameraShake
     - 가드 영역 내 + 패리 성공 -> `Reflect()` (방향 반전, 속도 1.5배, 색상 금색) + CameraShake
     - 가드 영역 내 + 패리 실패 -> 투사체 파괴
4. **반사된 투사체가 적과 충돌 시:** `EnemyShooter.Die()` -> 깜박임 연출 후 파괴

---

## 6. Editor Tools (에디터 도구)

### 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| CinemachineSetupTool | Cinemachine 카메라 자동 셋업 에디터 메뉴 도구 |
| DialogueEditorWindow | 대화 데이터 SO를 GUI로 생성하는 에디터 윈도우 |

### 각 스크립트 상세

#### CinemachineSetupTool (`Assets/Scripts/Editor/CinemachineSetupTool.cs`)
- **상속:** static class (에디터 전용)
- **역할:** `Tools/Setup Cinemachine Camera` 메뉴를 통해 Cinemachine 카메라 환경을 한번에 구성한다.
- **주요 메서드:**
  - `SetupCinemachineCamera()` - (1) Main Camera에 CinemachineBrain 추가, (2) CM Camera 오브젝트 생성 + CinemachineCamera, CinemachineFollow(FollowOffset (0,0,-10)), CinemachineImpulseListener 컴포넌트 추가, (3) PlayerController를 찾아 Follow 타겟 설정, (4) CameraShake 오브젝트 생성
- **다른 스크립트와의 관계:** CameraShake 컴포넌트를 자동 추가. PlayerController를 찾아 카메라 추적 대상으로 설정

#### DialogueEditorWindow (`Assets/Scripts/Editor/DialogueEditorWindow.cs`)
- **상속:** EditorWindow
- **역할:** `Tools/Dialogue Editor` 메뉴를 통해 열리는 대화 에디터 윈도우. NPC 이름, 대화 이름, 타이핑 속도를 설정하고, 대화 줄을 추가/삭제/정렬하며, 미리보기를 통해 확인한 뒤 DialogueData SO를 생성한다.
- **주요 변수:**
  - `npcName` (string) - NPC 이름
  - `dialogueName` (string) - 대화 이름 (에셋 파일명)
  - `typingSpeed` (float, 기본값 30) - 타이핑 속도
  - `speakers` (List\<DialogueData.Speaker\>) - 각 줄의 화자
  - `lines` (List\<string\>) - 각 줄의 텍스트
  - `showPreview` (bool) - 미리보기 펼침 여부
  - `previewIndex` (int) - 미리보기 인덱스
- **주요 메서드:**
  - `DrawDialogueInfo()` - NPC 이름, 대화 이름, 타이핑 속도 입력 필드 및 저장 경로 표시
  - `DrawDialogueLines()` - 대화 줄 리스트 UI. 화자 선택, 텍스트 입력, 순서 변경(상/하), 삭제 기능
  - `SwapLines(int a, int b)` - 두 줄의 순서를 교환
  - `DrawPreview()` - 게임 내 대화 패널과 유사한 형태의 미리보기. 좌/우 버튼으로 줄 탐색
  - `DrawCreateButton()` - 유효성 검증 후 "Create Dialogue SO" 버튼 표시
  - `CreateDialogueSO()` - `Assets/ScriptableObject/Dialogue/{npcName}/{dialogueName}.asset` 경로에 DialogueData SO 생성. 기존 에셋이 있으면 덮어쓰기 확인
  - `EnsureFolderExists(string path)` - 폴더 경로가 없으면 재귀적으로 생성
- **다른 스크립트와의 관계:** DialogueData ScriptableObject를 생성. AssetDatabase를 사용하여 에셋 저장

---

## 7. Debug 도구

### 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| PingPongMover | 오브젝트를 좌우로 왕복 이동시키는 디버그용 컴포넌트 |

### 각 스크립트 상세

#### PingPongMover (`Assets/Scripts/Debug/PingPongMover.cs`)
- **상속:** MonoBehaviour
- **역할:** 오브젝트를 시작 위치 기준으로 좌우로 왕복 이동시킨다. Rigidbody2D.MovePosition을 사용하여 물리 시스템과 호환된다. Freeze 스킬 테스트 등에 활용 가능.
- **주요 변수:**
  - `speed` (float, 기본값 3) - 왕복 이동 속도
  - `distance` (float, 기본값 3) - 시작 위치 기준 좌우 이동 거리
  - `rb` (Rigidbody2D) - Rigidbody2D 참조
  - `startPosition` (Vector2) - 시작 위치
  - `direction` (int) - 현재 이동 방향 (1 또는 -1)
- **주요 메서드:**
  - `FixedUpdate()` - `rb.MovePosition()`으로 이동. constraints가 FreezeAll이면 이동하지 않음 (Freeze 스킬과 호환). distance 범위를 벗어나면 방향 반전
- **다른 스크립트와의 관계:** FreezeInteractable의 constraints = FreezeAll 설정과 호환 (FreezeAll일 때 이동 중지)
