# Winter (겨울) 시즌 메카닉

## 개요
- 겨울 시즌은 **결빙(Freeze)** 스킬과 **앵커(Anchor)** 스킬을 제공하며, 오브젝트를 공중에 고정하거나 플레이어 자신을 공중에 고정할 수 있는 능력을 부여한다.

## 스크립트 목록
| 스크립트 | 역할 |
|----------|------|
| FreezeController | 결빙 스킬의 MP 소모 및 적용 로직을 관리하는 싱글턴 컨트롤러 |
| FreezeInteractable | 결빙 효과를 받을 수 있는 오브젝트. 클릭 시 모든 물리 동작이 정지되며 시각 피드백 제공 |
| AnchorController | 앵커 스킬 관리. 플레이어 자신을 현재 위치에 고정하는 싱글턴 컨트롤러 |

## 각 스크립트 상세

### FreezeController (`Assets/Scripts/Winter/FreezeController.cs`)
- **상속:** MonoBehaviour
- **역할:** 결빙 스킬의 핵심 로직. MP를 소모하여 대상 오브젝트의 물리 동작을 완전히 정지시킨다.
- **주요 변수:**
  - `player` (PlayerBase) - MP 소모를 위한 플레이어 참조
  - `duration` (float, 기본값 5) - 결빙 효과 지속 시간(초)
  - `mpCost` (float, 기본값 20) - 1회 사용 시 소모 MP
- **주요 메서드:**
  - `TryFreeze(FreezeInteractable target)` - MP가 충분한지 확인 후 MP를 소모하고, 대상의 `ApplyFreeze()`를 호출한다. 성공 시 `true` 반환
- **다른 스크립트와의 관계:** Singleton 패턴으로 Instance를 노출. FreezeInteractable에서 `FreezeController.Instance.TryFreeze(this)`로 호출. PlayerBase의 `CurrentMp`를 직접 차감한다.

---

### FreezeInteractable (`Assets/Scripts/Winter/FreezeInteractable.cs`)
- **상속:** MonoBehaviour
- **역할:** 결빙 효과를 적용받는 개별 오브젝트. 마우스 클릭으로 활성화되면 모든 물리 동작(이동, 회전, 중력)이 멈추고, 지속 시간이 끝나면 원래 상태로 복원된다.
- **RequireComponent:** Collider2D, Rigidbody2D, SpriteRenderer
- **주요 변수:**
  - `freezeTint` (Color) - 결빙 상태일 때 적용되는 틴트 색상 (연한 파란색)
  - `blinkStartRatio` (float, 기본값 0.3) - 남은 시간의 30% 시점부터 깜박임 경고 시작
  - `minBlinkInterval` (float, 기본값 0.08) - 깜박임 최소 간격
  - `maxBlinkInterval` (float, 기본값 0.4) - 깜박임 최대 간격
  - `isFrozen` (bool) - 현재 결빙 상태 여부 (프로퍼티 `IsFrozen`으로 노출)
- **주요 메서드:**
  - `OnMouseDown()` - 마우스 클릭 시 SkillManager에서 Freeze 스킬 활성 여부를 확인하고, FreezeController를 통해 결빙을 시도
  - `ApplyFreeze(float duration)` - Rigidbody2D의 linearVelocity와 angularVelocity를 0으로 설정, gravityScale을 0으로, constraints를 `FreezeAll`로 설정. 틴트 색상 적용 후 코루틴 시작
  - `FreezeRoutine(float duration)` - 지속 시간 동안 대기 후, 남은 시간 비율에 따라 점점 빨라지는 깜박임 경고를 표시
  - `Restore()` - gravityScale, constraints, 색상을 원래 값으로 복원
- **다른 스크립트와의 관계:** SkillManager.Instance에서 스킬 활성 상태 확인, FreezeController.Instance에 결빙 요청 전달

---

### AnchorController (`Assets/Scripts/Winter/AnchorController.cs`)
- **상속:** MonoBehaviour
- **역할:** 앵커 스킬 관리. 마우스 우클릭을 누르고 있는 동안 플레이어를 현재 위치에 완전히 고정한다. MP를 초당 소모하며, MP가 바닥나면 자동 해제된다.
- **주요 변수:**
  - `player` (PlayerBase) - 플레이어 참조
  - `anchorTint` (Color) - 앵커 상태일 때 플레이어에 적용되는 틴트 색상 (연한 파란색)
  - `mpCostPerSecond` (float, 기본값 10) - 초당 소모 MP
  - `playerRb` (Rigidbody2D) - 플레이어의 Rigidbody2D 참조
  - `playerSprite` (SpriteRenderer) - 플레이어의 SpriteRenderer 참조
  - `isAnchored` (bool) - 앵커 상태 여부 (프로퍼티 `IsAnchored`로 노출)
  - `originalGravityScale` (float) - 활성화 전 원래 gravityScale 저장
  - `originalConstraints` (RigidbodyConstraints2D) - 활성화 전 원래 constraints 저장
  - `originalColor` (Color) - 활성화 전 원래 색상 저장
- **주요 메서드:**
  - `Update()` - 스킬 활성 여부 확인, 마우스 우클릭 입력 처리, MP 소모
  - `Activate()` - MP 확인 후 linearVelocity를 0으로 설정, gravityScale을 0으로, constraints를 `FreezeAll`로 설정하여 플레이어를 완전히 고정. 틴트 색상 적용
  - `DeactivateAnchor()` - gravityScale, constraints, 색상을 원래 값으로 복원
- **다른 스크립트와의 관계:** SkillManager.Instance에서 Anchor 스킬 활성 확인. PlayerBase의 CurrentMp를 매 프레임 차감. CheckpointManager.DeactivateActiveSkills()에서 `DeactivateAnchor()`가 호출됨

## 동작 흐름

### Freeze (결빙) 스킬
1. 플레이어가 Freeze 스킬이 활성화된 상태에서 FreezeInteractable 오브젝트를 **마우스 클릭**
2. `OnMouseDown()` -> SkillManager에서 Freeze 스킬 활성 여부 확인
3. `FreezeController.TryFreeze()` 호출 -> MP 충분한지 검사
4. MP 차감 후 `FreezeInteractable.ApplyFreeze()` 호출
5. Rigidbody2D의 linearVelocity = 0, angularVelocity = 0, gravityScale = 0, constraints = FreezeAll
6. 결빙 틴트 색상 적용
7. `FreezeRoutine` 코루틴 시작: duration 동안 대기
8. 남은 시간 30%부터 깜박임 경고 (간격이 점점 빨라짐)
9. 시간 종료 시 `Restore()`로 모든 물리값과 색상 원복

### Anchor (앵커) 스킬
1. 플레이어가 Anchor 스킬이 활성화된 상태에서 **마우스 우클릭을 누름**
2. `Activate()` -> MP 확인, 현재 물리 상태(gravityScale, constraints) 저장
3. linearVelocity = 0, gravityScale = 0, constraints = FreezeAll로 플레이어 완전 고정
4. 플레이어 스프라이트에 앵커 틴트 색상 적용
5. 매 프레임 MP를 `mpCostPerSecond * Time.deltaTime`만큼 차감
6. 마우스 우클릭을 **놓거나** MP가 바닥나면 `DeactivateAnchor()` 호출
7. gravityScale, constraints, 색상을 저장해둔 원래 값으로 복원

### Freeze vs Anchor 비교
| 항목 | Freeze | Anchor |
|------|--------|--------|
| 대상 | 환경 오브젝트 | 플레이어 자신 |
| 입력 | 마우스 좌클릭 (오브젝트) | 마우스 우클릭 (토글) |
| MP 소모 | 1회 고정 비용 (20 MP) | 초당 지속 소모 (10 MP/s) |
| 지속 시간 | duration 후 자동 해제 | 우클릭 해제 또는 MP 부족 시 해제 |
| 시각 피드백 | 오브젝트 틴트 + 깜박임 경고 | 플레이어 틴트 |
