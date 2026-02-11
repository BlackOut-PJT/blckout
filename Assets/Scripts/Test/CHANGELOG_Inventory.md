# 인벤토리 시스템 변경사항 (Coder B - JY)

> 작업일: 2026-02-11
> 브랜치: feat/final_item (develop 기반)

---

## 변경 파일 목록

| 파일 | 액션 | 위험도 |
|------|------|--------|
| `GameStateManager.cs` | 2줄 추가 | LOW |
| `Resources/Player(Kill).prefab` | 직렬화 필드 자동 반영 | LOW |
| `Fonts/NeoDunggeunmoPro-Regular SDF.asset` | TMP 한글 글리프 자동 추가 | LOW |
| `Scripts/Test/TestJobToggle_JY.cs` | 새 파일 (테스트 전용) | LOW |
| `Scripts/Test/InventoryModel.cs` | 리팩토링 | MEDIUM |
| `Scripts/Test/InventoryUIController.cs` | 리팩토링 | MEDIUM |
| `Scripts/Test/InteractableObject/CraftingBox.cs` | 기능 추가 | LOW |
| `Scripts/Test/ItemRandomizer_JY.cs` | 새 파일 (아이템 랜덤 배치) | LOW |
| `Scenes/TestScene_JY.unity` | 새 파일 (테스트씬) | LOW |

**안 건드린 파일:** PlayerInteraction.cs, PlayerController.cs, TestNWManager.cs, FurnitureBox.cs, FieldFireWork.cs, SightSystemController.cs, FireworkRpcRelay.cs

---

## 1. GameStateManager.cs — 테스트 인프라 (2줄)

**목적:** 테스트씬에서 승리 판정을 끄기 위한 플래그

```csharp
// 추가된 필드 (기본값 false -> 인게임 영향 없음, Inspector에도 안 보임)
[HideInInspector] public bool skipWinCondition = false;

// CheckWinCondition() 최상단에 추가
if (skipWinCondition) return WhoWin.None;
```

- 기본값 `false`이므로 실제 게임에서는 기존과 동일하게 동작
- TestJobToggle_JY에서만 `true`로 설정

---

## 2. TestJobToggle_JY.cs — 테스트씬 전용 (새 파일)

**목적:** TestScene_JY에서 Photon 없이 단독 테스트

**기능:**
- Photon OfflineMode로 네트워크 없이 동작
- TestNWManager 자동 비활성화 (충돌 방지)
- 게임 세팅: `skipWinCondition=true`, `blackoutDelay=99999`
- **K키**: Killer <-> Survivor 전환 (인벤토리 슬롯 수 변경 테스트)

**인게임 영향:** 없음 (TestScene_JY에서만 사용)

---

## 3. InventoryModel.cs — 핵심 변경

**목적:** 단일 아이템 -> 멀티슬롯 인벤토리

### 변경 전
```
MonoBehaviourPun
ItemData item (단일)
void AddItem(ItemData)
void RemoveItem()
```

### 변경 후
```
MonoBehaviourPunCallbacks (Job 변경 감지 위해)
List<ItemData> items (복수)
int maxSlots (Survivor=1, Killer=2)
bool IsFull
bool AddItem(ItemData) -> 꽉 차면 false 반환 + OnInventoryFull 이벤트
void RemoveItem() -> items[0] 제거
void RemoveItem(ItemData target) -> 특정 아이템 제거
```

### 하위호환
- `public ItemData item` -> 프로퍼티 getter (`items[0]` 반환)
- 외부에서 `inventoryModel.item` 읽는 코드 전부 기존대로 동작
- `AddItem()` 리턴값 무시해도 컴파일 OK (FieldFireWork, FurnitureBox 수정 불필요)

### Job 연동
- `OnPlayerPropertiesUpdate`에서 Job 변경 감지
- Killer -> `maxSlots = 2` / Survivor -> `maxSlots = 1`
- 슬롯 수 변경 시 `OnInventoryChanged` 발동 -> UI 자동 갱신

### Player(Kill) 프리팹 변경
- 직렬화 필드가 `item: {fileID: 0}` -> `items: []`, `maxSlots: 1`로 자동 반영
- Unity가 스크립트 변경 감지해서 프리팹 자동 업데이트한 것

---

## 4. InventoryUIController.cs — UI 변경

**목적:** 멀티슬롯 표시 + "손이 꽉 찼습니다" 피드백

### 멀티슬롯 UI
- Slot_0(기존 슬롯)을 런타임에 복제하여 Slot_1, Slot_2... 생성
- 복제 시 InventoryUIController 컴포넌트 자동 제거 (무한 증식 방지)
- Job 변경 시 `EnsureSlotCount(maxSlots)` -> 슬롯 자동 추가/숨김

### 인벤토리 풀 메시지
- `OnInventoryFull` 이벤트 구독
- "손이 꽉 찼습니다!" 텍스트 2초 표시 후 자동 숨김
- **Unity에서 설정 필요:** InventoryCanvas에 TextMeshProUGUI 추가 -> `fullMessageText` 필드에 연결

### 씬 구조 참고
```
Canvas
+-- InventorySlot (InventoryUIController 붙어있음)
    +-- ItemIcon (Image) <- itemImage
    +-- ItemName (TMP Text) <- itemText
```

---

## 5. CraftingBox.cs — 기능 추가

### E키 자동 투입 (TryAutoDeposit)
- `Interact()` 호출 시 자동 실행
- 들고 있는 아이템이 재료와 itemID 일치 + 해당 슬롯 비어있으면 -> 자동 투입
- 매칭 안 되면 기존대로 패널만 열림

### TryRetrieveItem 안전장치
- 아이템 빼기 전 `InventoryModel.instance.IsFull` 체크
- 인벤토리 꽉 차면 빼기 거부

---

## 6. ItemRandomizer_JY.cs — 아이템 랜덤 배치 (새 파일)

**목적:** 게임 시작 시 FurnitureBox에 아이템을 랜덤으로 분배 (화약/종이/성냥 최소 1개씩 보장)

**동작 방식:**
1. 씬의 모든 FurnitureBox를 찾아서 셔플
2. **필수 배치**: 화약(ID:0), 성냥(ID:1), 종이(ID:2)를 각각 다른 가구에 1개씩 배치
3. **남는 가구**: extraItemIDs 풀에서 랜덤 배치 (비우면 빈 상태)

**설정값 (Inspector):**
- `guaranteedItemIDs`: 기본 `[0, 1, 2]` (화약, 성냥, 종이)
- `extraItemIDs`: 기본 `[0, 1, 2]` (랜덤 풀)

**아이템 ID 참고:**
| ID | 이름 | killerOnly |
|----|------|------------|
| 0 | 화약 | O |
| 1 | 성냥 | X |
| 2 | 종이 | X |
| 3 | 폭죽 | - (조합 결과) |

**주의:** FurnitureBox가 필수 아이템 수(3개)보다 적으면 일부 필수 아이템이 배치 안 됨

**인게임 영향:** 씬에 이 컴포넌트를 붙이지 않으면 영향 없음

---

## 테스트 방법 (TestScene_JY)

1. TestScene_JY 열기
2. 빈 GameObject에 `TestJobToggle_JY` 컴포넌트 붙이기 + PhotonView 추가
3. 같은 또는 다른 GameObject에 `ItemRandomizer_JY` 컴포넌트 붙이기 (아이템 자동 배치)
4. Play

| 테스트 | 조작 | 예상 결과 |
|--------|------|-----------|
| 아이템 줍기 | 옷장/서랍 앞에서 E 꾹 (1.5초) -> 패널 클릭 | 인벤토리에 아이템 표시 |
| 인벤토리 풀 | 아이템 들고 또 줍기 시도 | "손이 꽉 찼습니다!" 메시지 |
| Killer 전환 | K키 | 슬롯 2칸으로 증가 |
| Survivor 복귀 | K키 다시 | 슬롯 1칸으로 감소 |
| 자동 투입 | 재료 들고 조합대 E | 자동으로 슬롯에 투입 |
| 조합대 안전장치 | 인벤 꽉 찬 상태에서 조합대 아이템 빼기 | 빼기 거부 |
| 폭죽 사용 | 폭죽 들고 Use 버튼 | 폭죽 발동 (메인씬에서 테스트 권장) |
