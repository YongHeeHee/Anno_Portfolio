# Coding Conventions

## 네이밍 규칙

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스 | PascalCase | `PlayerController`, `DialogueManager` |
| public 프로퍼티 | PascalCase | `IsDead`, `IsGuarding`, `InputLocked` |
| public 메서드 | PascalCase | `Die()`, `TryParry()`, `StartDialogue()` |
| private/protected 필드 | camelCase | `moveSpeed`, `fireInterval`, `spriteRenderer` |
| SerializedField | camelCase | `[SerializeField] float guardDuration` |
| static 해시 | PascalCase + Hash 접미사 | `SpeedHash`, `DeathHash` |
| 상수/readonly | 사용하지 않음 (Animator 해시를 static readonly로 사용) | `static readonly int SpeedHash` |
| 열거형 | PascalCase | `Speaker { NPC, Player }` |

## Animator 파라미터 해시
- `Animator.StringToHash()`로 static readonly 캐시
- 명명: `{파라미터명}Hash`
```csharp
static readonly int SpeedHash = Animator.StringToHash("Speed");
```

## 컴포넌트 참조
- `[RequireComponent]`로 필수 컴포넌트 명시
- `Awake()`에서 `GetComponent<>()`로 캐시
```csharp
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
public abstract class PlayerBase : CharacterBase
{
    protected Rigidbody2D rb;
    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
    }
}
```

## Input System
- New Input System 사용 (`UnityEngine.InputSystem`)
- `InputSystem.actions.FindAction("ActionName")`으로 액션 바인딩
- `Awake()`에서 바인딩, `Update()`에서 `WasPressedThisFrame()` 체크
- 사용 중인 액션: Move, Jump, Attack, Crouch(Guard), Interact

## 싱글톤 패턴
```csharp
public static ClassName Instance { get; private set; }
void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
}
```

## 상속 구조 규칙
- 공통 로직은 부모 클래스에서 처리
- 자식 클래스는 가상 메서드를 override하여 확장
- `base.Awake()` 반드시 호출

## 물리/충돌 처리
- 2D 물리 사용 (Rigidbody2D, BoxCollider2D, CircleCollider2D)
- 지면 판정: `Physics2D.OverlapBox()` (FixedUpdate)
- 투사체 충돌: `Physics2D.OverlapCircle()` (FixedUpdate)
- 상호작용 범위: Trigger Collider + `OnTriggerEnter2D/Exit2D`

## ScriptableObject 경로 규칙
- 대화 데이터: `Assets/ScriptableObject/Dialogue/{NPC이름}/{대화이름}.asset`
- `[CreateAssetMenu]`로 에셋 생성 메뉴 등록

## 스크립트 폴더 구조 규칙
- 기능별 하위 폴더 분류
  - `Scripts/Camera/` - 카메라 관련
  - `Scripts/Dialogue/` - 대화 시스템
  - `Scripts/Spring/` - 봄 스킬 (Growth, VineGrapple)
  - `Scripts/Summer/` - 여름 스킬 (Explosion, Ignite)
  - `Scripts/Autumn/` - 가을 스킬 (Lighten, Ghost)
  - `Scripts/Winter/` - 겨울 스킬 (Freeze, Anchor)
  - `Scripts/UI/` - UI 관련
  - `Scripts/Debug/` - 디버그/테스트용 도구
  - `Scripts/Editor/` - 에디터 전용 도구
- 캐릭터 관련 기본 스크립트는 `Scripts/` 루트에 배치

## UI 생성 방식
- SpeechBubble: 코드에서 동적으로 Canvas/Image/Text 생성 (프리팹 미사용)
- InteractIndicator: 코드에서 동적 생성 또는 Inspector에서 할당

## 주의사항
- `.meta` 파일은 Unity가 자동 관리하므로 수동 편집 금지
- Editor 폴더 안의 스크립트는 빌드에 포함되지 않음
- DOTween 사용 시 `OnDestroy()`에서 `tween.Kill()` 필수 (메모리 누수 방지)
