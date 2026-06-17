# 스페이스십 워 — 게임 설계 문서

> 이 문서는 세션이 끊어져도 게임 기획 / 구조 / 설계가 보존되도록 유지하는 단일 기준 문서입니다.
> 새 작업을 시작할 때 항상 이 문서를 먼저 읽고, 설계가 바뀌면 이 문서를 갱신합니다.

최종 갱신: 2026-06-17

---

## 1. 게임 개요

- **장르**: 우주선 모듈 조립 / 진화형 게임.
- **핵심 컨셉**: 검은 우주 공간에서 **둥둥 떠다니는 우주선**을, 중앙의 **코어 모듈**을 기준으로 상하좌우에 **정사각형 모듈 블록**을 부착(attach)하고 탈착(detach)하며 진화시켜 나갑니다.
- **시점 / 표현**: **탑뷰(top-view) 2.5D**. 카메라가 위에서 약간 기울여 내려다보며, 모듈은 높이를 가진 3D 큐브로 표현되어 입체감이 있다(월드 공간).
- **화면 방향**: 세로 모드(Portrait), 기준 해상도 1080 x 1920.
- **UI 기술**: 함선 본체는 **월드 공간 3D**(큐브 모듈). HUD 등 화면 UI는 uGUI(기존 Canvas) 사용. UI Toolkit 아님.
- **기존 리소스 사용 안 함**: 아트 리소스는 신규로 구성한다(초기에는 단색 큐브로 대체).

## 2. 핵심 루프 (Core Loop)

1. 중앙 코어 모듈을 기준으로 화면에 격자(grid)가 존재한다.
2. 코어를 중심으로 상/하/좌/우 인접 칸에 모듈 블록을 부착한다.
3. 부착된 모듈은 다시 탈착할 수 있다.
4. 모듈 조합에 따라 우주선이 진화/강화된다. (성장 규칙은 추후 정의)

## 3. 좌표 / 격자 규칙

- 격자는 정사각형 셀로 구성한다(월드 공간, XZ 평면).
- **셀 크기**: 1 world unit (`m_CellSize`, 추후 조정 가능).
- **셀 좌표**: 코어를 (0, 0)으로 하는 정수 격자 좌표 `Vector2Int` 사용. (오른쪽 +x, 위 +y → 월드 +X, +Z)
- 칸의 월드 로컬 위치 = `new Vector3(x * cellSize, height/2, y * cellSize)`.
- 인접 4방향: 상(0,1) / 하(0,-1) / 좌(-1,0) / 우(1,0).
- **슬롯**: 점유된 칸(코어+모듈)에 인접한 빈 칸. 매 변경 시 재계산된다.
- **연결성 규칙**: 모듈은 항상 코어와 인접 경로로 연결되어야 한다. 탈착으로 코어와 끊긴 모듈은 자동 제거된다(BFS prune).

## 4. 씬 / UI 구조 (Scene.unity)

기존 `Scene.unity`에 구성한다. 루트 오브젝트:

```
Main Camera        (Base 카메라, 탑뷰 2.5D, 검은 배경, ShipCameraController 부착)
Directional Light
Global Volume
Ship               (ShipBuilder 부착 — 함선 루트, scale 0.5, 부유 모션의 기준)
                   └ (런타임 생성) CoreModule / Module... / Slot... 큐브
UI
├── Camera         (UI 전용 카메라, Overlay)
├── Canvas         (Canvas / CanvasScaler / GraphicRaycaster) — HUD/메뉴 용도
│   ├── Image      (기존, 비활성 — 미사용)
│   ├── HUD        (기존, RectTransform + CanvasGroup)
│   └── BottomNavigation  (하단 탭: 개조[현재] / 상점 / 전투 / 설정)
└── EventSystem    (InputSystemUIInputModule)
```

### 하단 네비게이션 (BottomNavigation)
- Canvas 하단 앵커(높이 200), 4개 버튼을 균등 분할(각 1/4 폭).
- 버튼: **개조(현재 화면, 강조색)** / 상점 / 전투 / 설정. 라벨은 legacy `Text` + OS 폰트("Malgun Gothic").
- 화면 전환 로직은 미연결(현재는 탭 바 외형만). 추후 각 메뉴 화면 연결 예정.

### 카메라 줌 (ShipCameraController)
- 대상(원점)을 바라보며 거리를 조절. 마우스 휠 + 터치 핀치(신규 Input System).
- 거리 범위 `m_MinDistance ~ m_MaxDistance`로 클램프.

### 카메라 설정 (Main Camera)
- Base 카메라(URP), 카메라 스택에 `UI/Camera`(Overlay)가 올라가 합성된다.
- 위치 `(0, 7, -4)`에서 원점을 바라보도록(`LookAt`) 기울여 탑뷰 2.5D 시점 구성. FOV 50.
- Clear Flags = Solid Color, 배경 = 거의 검정(어두운 우주색). 입력 핸들링은 신규 Input System 전용.

### 모듈 외형 (초기, 단색 큐브)
- 칸은 `GameObject.CreatePrimitive(Cube)` + URP/Lit 머티리얼(`_BaseColor`)로 생성. BoxCollider로 클릭 판정.
- CoreModule: 청록 계열, 높이 `m_ModuleHeight`. 중앙(0,0).
- Module: 파랑 계열, 높이 `m_ModuleHeight`.
- Slot: 회색 계열, 낮은 높이 `m_SlotHeight`(빈 슬롯 표현). 점유 칸 인접 4방향.

### 부유 모션
- `Ship` 루트가 매 프레임 sin 기반 상하 진동(`m_BobAmplitude`, `m_BobSpeed`) + 느린 Y축 회전(`m_SpinSpeed`).

## 5. 코드 구조

기존 베이스 계층:
- `SharedComponent<TComponent>` : 공유(싱글톤성) MonoBehaviour 베이스. (`Assets/Scripts/Base/SharedComponent.cs`)
- `GameManager : SharedComponent<GameManager>` : 게임 매니저. (`Assets/Scripts/GameManager.cs`)
- `Actor` / `Pawn` / `Character` : 액터 계층. (`Assets/Scripts/Base/`)
- `PlayerCharacter`, `MonsterPawn`, `RuntimeInitializer`.

함선 조립 시스템:
- `ShipBuilder : MonoBehaviour` (`Assets/Scripts/ShipBuilder.cs`) — 탑뷰 2.5D 함선 조립기.
  - `m_ModuleCells`(HashSet<Vector2Int>)로 부착된 모듈 좌표를 관리(코어 제외).
  - `Rebuild()`: 코어/모듈/슬롯 큐브를 모두 재생성. `CollectSlots()`로 슬롯 계산.
  - `AttachModule(coord)` / `DetachModule(coord)`: 부착/탈착. 탈착 시 `PruneDisconnected()`(BFS)로 코어와 끊긴 모듈 제거.
  - `Update()`: 부유 모션 + `HandlePointer()`(신규 Input System `Pointer.current` + `Physics.Raycast`)로 슬롯 탭→부착 / 모듈 탭→탈착.
  - 자동 회전은 `m_AutoSpin`(기본 off) 옵션으로 토글. 부유 상하 진동은 항상 적용.
- `ShipCameraController : MonoBehaviour` (`Assets/Scripts/ShipCameraController.cs`) — 카메라 줌(휠/핀치).
- 모듈 종류/스탯/진화 규칙은 추후 정의(현재는 단일 종류).

메타/UI 시스템:
- `PlayerState : MonoBehaviour` (`Assets/Scripts/PlayerState.cs`) — 레벨/경험치/활동력(`m_Activity`/`m_MaxActivity`)/재화 보관. `Changed` 이벤트로 HUD 갱신 통지. `Game` 오브젝트에 부착.
- `MainUi : MonoBehaviour` (`Assets/Scripts/MainUi.cs`) — 하단 탭 전환 + 상단 HUD. `Game` 오브젝트에 부착.
  - 씬 UI를 이름으로 탐색: `UI/Canvas/TopHud/{LevelText,ActivityText,CurrencyText}`, `UI/Canvas/Screens/Screen_{이름}`, `UI/Canvas/BottomNavigation/NavButton_{이름}`.
  - `SelectScreen(index)`: 해당 화면만 활성, 탭 강조, `개조`일 때만 월드 `Ship` 활성.

모듈/경제 시스템:
- `ModuleType`(enum): `Weapon`(무기) / `Armor`(장갑) / `Engine`(추진체).
- `ModuleDefinition` + `ModuleCatalog`(`Assets/Scripts/ModuleCatalog.cs`) — 종류별 이름/가격/스탯(공격/체력/이동/사거리)/색상 정의.
  - 무기: 가격100, 공격+10, 체력10, 사거리4. / 장갑: 가격80, 체력+40. / 추진체: 가격120, 이동+5, 체력10.
- `PlayerState` 인벤토리: `m_Inventory`(Dictionary<ModuleType,int>), `GetModuleCount`/`AddModule`/`TryRemoveModule`/`TrySpendCurrency`.
- `ShopController`(`Assets/Scripts/ShopController.cs`) — 상점 화면에 모듈 행(이름/스탯/가격/구매) 구성, 구매 시 재화 소비→인벤토리 추가. `Game`에 부착.
- `InventoryView`(`Assets/Scripts/InventoryView.cs`) — 개조 화면 하단 바에 보유 수량 표시. `Game`에 부착.
- `UiFactory`(`Assets/Scripts/UiFactory.cs`) — uGUI Image/Text 생성 헬퍼.

## 6. 진행 로그

- 2026-06-17
  - `SharedComponent2` → `SharedComponent` 리네임 (클래스/파일/참조).
  - 설계 문서(`DESIGN.md`) 신규 작성.
  - (1차) `Scene.unity`에 세로 1080x1920 uGUI 평면 스캐폴드 구성 → 컨셉 재확인 후 폐기.
  - **컨셉 전환**: uGUI 평면 그리드 → **탑뷰 2.5D 월드 공간**(둥둥 떠다니는 우주선에 동적 모듈 부착).
    - uGUI `ShipScreen` 제거(오버레이가 월드를 가림). 검은 우주는 Main Camera 배경으로 처리.
    - `Ship` 오브젝트 + `ShipBuilder` 생성. Main Camera 탑뷰 각도/검은 배경 설정.
    - `ShipBuilder` 구현: 코어+모듈+슬롯 큐브, 부착/탈착, 연결성 prune, 부유 모션, 포인터 클릭(신규 Input System).
    - 플레이 모드에서 표시·부착·탈착·연결성 정리 동작 확인. 콘솔 에러 없음.
  - 회전 옵션화(`m_AutoSpin` 기본 off), 카메라 줌(`ShipCameraController`, 휠/핀치) 추가.
  - 함선 전체 50% 축소(`Ship` scale 0.5).
  - 하단 네비게이션(개조[현재]/상점/전투/설정) uGUI 추가.
  - 탭 전환(개조/상점/전투/설정) + 상단 HUD(레벨/활동력/재화) 구현. `PlayerState`/`MainUi` 추가.
    - 상점/전투/설정은 현재 "준비 중" 플레이스홀더 패널. 플레이 모드에서 전환 동작 확인.
  - Phase 2: 모듈 데이터(`ModuleType`/`ModuleCatalog`) + 상점 구매(`ShopController`) → 인벤토리 축적/표시(`InventoryView`/`UiFactory`). 컴파일 확인(직접 실행로 검증).
  - 전투 상세 설계 확정(자동 전투 연출: 적 추진체 접근→무기 빔→피격 모듈 파괴→연결 끊긴 모듈 드리프트). 문서 7항에 기록.

## 7. 게임 루프 로드맵 (목표 설계)

사용자 의도에 따른 전체 흐름. 단계별로 구현한다.

1. **상점(상점 탭)**: 물건(모듈)을 재화로 구매 → **개조 탭의 인벤토리**에 모듈이 축적된다.
2. **개조(개조 탭)**: 인벤토리의 모듈을 **드래그**해서 함선의 빈 칸(슬롯)에 부착한다. (현재는 슬롯 탭=즉시 부착 / 추후 드래그-부착으로 확장)
3. **전투 목록(전투 탭)**: **스크롤뷰**로 대결 가능한 다른 우주선 목록 표시. "대결하기"를 누르면 **활동력 소모** → 전투 화면으로 전환.
4. **전투**: 자동 전투 연출. 결과 — 승리=**경험치+재화** 획득 / 패배=무보상. (전투당 활동력 **10** 소모, 자동 회복 없음)

### 전투 상세 설계 (자동 전투 연출)
- 시점: 전투 화면. **플레이어 함선은 제자리에서 둥둥 떠다니고**, 적 함선이 다가온다.
- 함선 구조: **코어 모듈**에 장갑/추진체/무기 모듈이 부착되고, 그 위에 또 부착되는 식으로 트리처럼 확장된다.
- 적 행동:
  1. 적은 **추진체 모듈**의 추력으로 플레이어에게 다가온다.
  2. **사거리** 안에 들어오면 **무기 모듈**에서 사거리가 고정된 **빔**을 발사한다.
  3. 빔에 적중된 모듈은 **파괴**된다.
  4. 중간 모듈이 파괴되어 **코어와의 연결이 끊긴 모듈**들은 부착이 풀리고 **흘러서(드리프트) 사라진다**. (조립의 `PruneDisconnected` 규칙과 동일 개념)
- 양측 모두 무기/추진체/장갑 모듈로 위 규칙을 따른다(상호 교전). 승패는 코어 파괴 등으로 결정(세부 규칙은 Phase 5에서 확정).
- 스탯 매핑: 무기→공격/사거리, 추진체→이동, 장갑→체력. (`ModuleCatalog` 값 사용)

## 8. 다음 단계 (TODO)

- [x] 탑뷰 2.5D 월드 공간 함선 화면 구성.
- [x] 모듈 부착/탈착 인터랙션 로직 구현(슬롯 탭→부착, 모듈 탭→탈착, 연결성 prune).
- [x] 자동 회전 옵션화 / 카메라 줌 / 전체 50% 축소 / 하단 네비게이션.
- [x] 탭 전환(개조/상점/전투/설정) + 상단 HUD(레벨/활동력/재화).
- [x] 모듈 데이터(무기/장갑/추진체) + 상점 구매 → 인벤토리 축적·표시.
- [ ] 개조: 인벤토리 모듈 드래그→슬롯 부착(모듈 종류별 스탯/색 반영, 인벤토리 소모).
- [ ] 전투 목록: 스크롤뷰 상대 목록 + 대결하기(활동력 소모) → 전투 화면.
- [ ] 전투: 결과 처리(승리=경험치+재화, 패배=무보상). 레벨업 곡선 정의.
- [ ] 모듈 종류/스탯/가격/전투력 규칙 정의.
- [ ] 함선 본체 시각 개선(별/우주 배경, 모듈 외형, 엔진 분사 등).
- [ ] `GameManager.Awake()`의 `Resources.Load("Units/PlayerCharacter")` 경로 오류 정리(현재 런타임 예외).
