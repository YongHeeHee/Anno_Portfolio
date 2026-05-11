# Autumn (가을) 시즌 메카닉

## 개요
- 가을 시즌은 **경감(Lighten)** 스킬과 **고스트(Ghost)** 스킬을 제공하며, 오브젝트의 무게를 줄이거나 플레이어가 벽을 통과할 수 있는 능력을 부여한다.

## 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| LightenController | 경감 스킬의 MP 소모 및 적용 로직을 관리하는 싱글턴 컨트롤러 |
| LightenInteractable | 경감 효과를 받을 수 있는 오브젝트. 클릭 시 무게가 감소하며 시각 피드백 제공 |
| GhostController | 고스트 스킬 관리. 플레이어가 GhostWall 레이어의 벽을 통과할 수 있게 해주는 싱글턴 컨트롤러 |

## 각 스크립트 상세

### LightenController (`Assets/Scripts/Autumn/LightenController.cs`)
- **상속:** MonoBehaviour
- **역할:** 경감 스킬의 핵심 로직. MP를 소모하여 대상 오브젝트의 중력과 질량을 줄인다.
- **주요 변수:**
  - `player` (PlayerBase) - MP 소모를 위한 플레이어 참조
  - `duration` (float, 기본값 5) - 경감 효과 지속 시간(초)
  - `gravityMultiplier` (float, 기본값 0.1) - 중력 배율. 원래 gravityScale에 곱하며 낮을수록 가벼움
  - `massMultiplier` (float, 기본값 0.1) - 질량 배율. 원래 mass에 곱하며 낮을수록 가벼움
  - `mpCost` (float, 기본값 20) - 1회 사용 시 소모 MP
- **주요 메서드:**
  - `TryLighten(LightenInteractable target)` - MP가 충분한지 확인 후 MP를 소모하고, 대상의 `ApplyLighten()`을 호출한다. 성공 시 `true` 반환
- **다른 스크립트와의 관계:** Singleton 패턴으로 Instance를 노출. LightenInteractable에서 `LightenController.Instance.TryLighten(this)`로 호출. PlayerBase의 `CurrentMp`를 직접 차감한다.

---

### LightenInteractable (`Assets/Scripts/Autumn/LightenInteractable.cs`)
- **상속:** MonoBehaviour
- **역할:** 경감 효과를 적용받는 개별 오브젝트. 마우스 클릭으로 활성화되며, 지속 시간이 끝나면 원래 상태로 복원된다.
- **RequireComponent:** Collider2D, Rigidbody2D, SpriteRenderer
- **주요 변수:**
  - `lightenTint` (Color) - 경감 상태일 때 적용되는 틴트 색상 (반투명 민트색)
  - `blinkStartRatio` (float, 기본값 0.3) - 남은 시간의 30% 시점부터 깜박임 경고 시작
  - `minBlinkInterval` (float, 기본값 0.08) - 깜박임 최소 간격 (복원 직전 가장 빠름)
  - `maxBlinkInterval` (float, 기본값 0.4) - 깜박임 최대 간격 (깜박임 시작 시 가장 느림)
  - `isLightened` (bool) - 현재 경감 상태 여부 (프로퍼티 `IsLightened`으로 노출)
- **주요 메서드:**
  - `OnMouseDown()` - 마우스 클릭 시 SkillManager에서 Lighten 스킬 활성 여부를 확인하고, LightenController를 통해 경감을 시도
  - `ApplyLighten(float gravityMultiplier, float massMultiplier, float duration)` - Rigidbody2D의 gravityScale과 mass를 배율로 줄이고, 틴트 색상을 적용한 뒤 코루틴 시작
  - `LightenRoutine(float duration)` - 지속 시간 동안 대기 후, 남은 시간 비율에 따라 점점 빨라지는 깜박임 경고를 표시. `Mathf.Lerp`로 interval을 보간
  - `Restore()` - gravityScale, mass, constraints, 색상을 원래 값으로 복원하고 velocity를 초기화
- **다른 스크립트와의 관계:** SkillManager.Instance에서 스킬 활성 상태 확인, LightenController.Instance에 경감 요청 전달

---

### GhostController (`Assets/Scripts/Autumn/GhostController.cs`)
- **상속:** MonoBehaviour
- **역할:** 고스트 스킬 관리. 마우스 우클릭을 누르고 있는 동안 플레이어가 반투명해지며 GhostWall 레이어의 벽을 통과할 수 있다. MP를 초당 소모하며, MP가 바닥나면 자동 해제된다.
- **주요 변수:**
  - `player` (PlayerBase) - 플레이어 참조
  - `ghostAlpha` (float, 기본값 0.4) - 고스트 상태의 플레이어 투명도 (0~1)
  - `mpCostPerSecond` (float, 기본값 10) - 초당 소모 MP
  - `ghostWallLayer` (LayerMask) - 통과 가능한 벽 레이어
  - `safePosition` (Vector2) - GhostWall 밖에서의 마지막 안전 위치 (해제 시 벽 속이면 이 위치로 복귀)
  - `isGhostActive` (bool) - 고스트 상태 여부 (프로퍼티 `IsGhostActive`로 노출)
- **주요 메서드:**
  - `Update()` - 스킬 활성 여부 확인, 마우스 우클릭 입력 처리, MP 소모 및 안전 위치 갱신
  - `ActivateGhost()` - `Physics2D.IgnoreLayerCollision()`으로 플레이어-GhostWall 충돌을 무시하고, 플레이어 알파값을 낮춤
  - `DeactivateGhost()` - 레이어 충돌 복원, 알파값 복원. 벽 속에 있으면 `safePosition`으로 텔레포트
  - `IsInsideGhostWall()` - `Physics2D.OverlapCapsule()`로 플레이어 콜라이더가 GhostWall과 겹치는지 판정
  - `LayerMaskToIndex(LayerMask mask)` - LayerMask 비트값에서 레이어 인덱스를 추출하는 유틸리티
- **다른 스크립트와의 관계:** SkillManager.Instance에서 Ghost 스킬 활성 확인. PlayerBase의 CurrentMp를 매 프레임 차감. CheckpointManager.DeactivateActiveSkills()에서 `DeactivateGhost()`가 호출됨

## 동작 흐름

### Lighten (경감) 스킬
1. 플레이어가 Lighten 스킬이 활성화된 상태에서 LightenInteractable 오브젝트를 **마우스 클릭**
2. `OnMouseDown()` -> SkillManager에서 Lighten 스킬 활성 여부 확인
3. `LightenController.TryLighten()` 호출 -> MP 충분한지 검사
4. MP 차감 후 `LightenInteractable.ApplyLighten()` 호출
5. Rigidbody2D의 gravityScale과 mass가 배율만큼 감소, 틴트 색상 적용
6. `LightenRoutine` 코루틴 시작: duration 동안 대기
7. 남은 시간 30%부터 깜박임 경고 (간격이 점점 빨라짐)
8. 시간 종료 시 `Restore()`로 모든 물리값과 색상 원복

### Ghost (고스트) 스킬
1. 플레이어가 Ghost 스킬이 활성화된 상태에서 **마우스 우클릭을 누름**
2. `ActivateGhost()` -> 현재 위치를 safePosition으로 저장
3. `Physics2D.IgnoreLayerCollision()`으로 Player-GhostWall 충돌 비활성화
4. 플레이어 스프라이트 알파값 감소 (반투명)
5. 매 프레임 MP를 `mpCostPerSecond * Time.deltaTime`만큼 차감
6. GhostWall 밖에 있을 때마다 safePosition 갱신
7. 마우스 우클릭을 **놓거나** MP가 바닥나면 `DeactivateGhost()` 호출
8. 레이어 충돌 복원, 알파값 복원
9. 만약 해제 시점에 GhostWall 내부에 있으면 safePosition으로 텔레포트하여 벽 속 끼임 방지
