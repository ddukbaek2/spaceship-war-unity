# 스페이스십 워 — 게임 설계 문서

> 이 문서는 세션이 끊어져도 게임 기획 / 구조 / 설계가 보존되도록 유지하는 단일 기준 문서입니다.
> 새 작업을 시작할 때 항상 이 문서를 먼저 읽고, 설계가 바뀌면 이 문서를 갱신합니다.

최종 갱신: 2026-06-23

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
  - `m_ModuleCells`(Dictionary<Vector2Int,ModuleType>)로 부착된 모듈의 좌표별 종류를 관리(코어 제외). 종류별 색은 `ModuleCatalog`.
  - `Rebuild()`: 코어/모듈/슬롯 큐브를 모두 재생성. `CollectSlots()`로 슬롯 계산.
  - `AttachModule(coord, type)`: 부착. `TryAttachAtScreenPosition(screenPos, type)`: 드롭 위치의 슬롯에 부착(드래그용).
  - `DetachModule(coord)`: 탈착. 탈착분 + `PruneDisconnected()`(BFS)로 끊긴 모듈을 `PlayerState`로 환원.
  - `Update()`: 부유 모션 + `HandlePointer()`(신규 Input System `Pointer.current` + `Physics.Raycast`)로 모듈 탭→탈착. (슬롯 부착은 인벤토리 드래그로 일원화)
  - 자동 회전은 `m_AutoSpin`(기본 off) 옵션으로 토글. 부유 상하 진동은 항상 적용.
  - 슬롯은 **드래그 중에만 반투명 표시**(`SetSlotsVisible`), 드롭 위치 슬롯에 **부착 미리보기**(`UpdateAttachPreview`: 모듈 색·높이로 변형). `BeginDragAttach`/`EndDragAttach`로 토글.
  - 모듈은 **개별 머티리얼 + 꾸밈**: URP/Lit 이미션·메탈릭, 종류별 액센트 형상(무기=포신, 추진체=노즐, 장갑=상판). 슬롯은 투명 머티리얼.
- `InventoryDragItem : MonoBehaviour` (`Assets/Scripts/InventoryDragItem.cs`) — 개별 모듈 드래그(고스트). 드래그 시작/중/종료에 `ShipBuilder.BeginDragAttach`/`UpdateAttachPreview`/`EndDragAttach` 호출. 슬롯 드롭 시 부착 + 인벤토리에서 해당 인스턴스 소모. `InventoryView`가 런타임에 부착.
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
- 개별 모듈: `ModuleInstance`(struct: `Id`/`Type`). 인벤토리는 **스택이 아니라 개별 개체** 목록.
- `PlayerState` 인벤토리: `m_Modules`(List<ModuleInstance>), `Modules`/`AddModule(type)`/`ContainsModule(id)`/`RemoveModule(id)`/`TrySpendCurrency`. `Awake`에서 기본 13개(무기5/장갑4/추진체4) 지급.
- `ShopController`(`Assets/Scripts/ShopController.cs`) — 상점 화면에 모듈 행(이름/스탯/가격/구매) 구성, 구매 시 재화 소비→인벤토리에 개별 추가. `Game`에 부착.
- `InventoryView`(`Assets/Scripts/InventoryView.cs`) — 개조 화면 하단에 **2열 스크롤뷰**(ScrollRect+GridLayout), 보유 모듈을 개별 셀로 표시(드래그 가능). `Game`에 부착.
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
  - Phase 3: `ShipBuilder`를 모듈 종류 인식(`Dictionary<Vector2Int,ModuleType>`)으로 전환. 종류별 색 반영. 인벤토리 드래그→슬롯 드롭 부착(`InventoryDragItem` + `TryAttachAtScreenPosition`), 부착 시 인벤토리 소모, 탈착/연결끊김 시 인벤토리 환원. 슬롯 탭 부착은 제거(드래그로 일원화), 모듈 탭 탈착 유지.
  - Phase 3 보완(피드백): 인벤토리를 **개별 개체**(`ModuleInstance`) 2열 스크롤뷰로 전환, 기본 13개 지급. 슬롯은 **드래그 중에만 반투명 표시 + 부착 미리보기**. 모듈마다 **개별 머티리얼(이미션/메탈릭) + 종류별 액센트 형상**으로 꾸밈.
  - Phase 4~6(피드백): 모듈을 **평평한 연결형 타일**(꽉 찬 셀, 낮은 높이, 위에 장식)로 재설계(빌딩 쌓기 느낌 제거). 인벤토리 **드래그(길게 눌러 픽업)와 스크롤 분리**. 상점을 **카드형(궁수의전설풍)** UI로 재구성. **전투 시스템**: 랜덤 적 목록(`BattleController`) + 전투하기(활동력 10) → **자동 전투 오버레이**(HP 바 연출) → 승리 시 재화+경험치, 패배 무보상. 상단 HUD에 **EXP** 추가, 레벨업 규칙(레벨 N에서 N승 시 레벨업). WebGL2 무압축 빌드(`build/WebGL`).
    - 빌드 차단 해소: SPUM 서드파티 스크립트 5종의 `using UnityEditor` 미가드 → `#if UNITY_EDITOR` 추가. WebGL은 OS 동적 폰트 미지원 → `Assets/Resources/Fonts/malgun.ttf` 포함 + `UiFont`로 모든 텍스트가 폰트 에셋 사용(씬의 14개 Text도 일괄 재지정).
    - 빌드 성공: `build/WebGL/`(index.html + Build/WebGL.{data,wasm,framework.js,loader.js} + TemplateData), 무압축(.gz/.br 없음), WebGL2(OpenGLES3), 약 76MB. 시놀로지 NAS 정적 호스팅 시 그대로 업로드.
  - 피드백 반영(웹/조작):
    - 커스텀 WebGL 템플릿 `Assets/WebGLTemplates/Portrait/index.html` 추가. 제품명/타이틀 `spaceship-war-unity`. (초기 빌드는 설정 미저장으로 기본 가로 템플릿이 나와 → ProjectSettings 저장 후 재빌드로 해결)
    - 캔버스를 **창 전체(100%×100%)로 채움** + `matchWebGLToCanvasSize=false` & **devicePixelRatio 버퍼**로 가로 잘림 해소 + 텍스트 선명도 개선.
    - **드래그 폐기 → 선택형**: 인벤토리 탭=가장 가까운 빈 칸 자동 부착(`ShipBuilder.AttachToFirstAvailable`), 함선 모듈 탭=탈착. `InventoryDragItem` 삭제, 슬롯 표시/미리보기 로직 제거(스크롤 충돌도 해소).
    - 배포: `\\DS216PLUSII\web\ddukbaek2\portfolio\spaceship-war-unity` (robocopy /MIR).

- 2026-06-18 (대규모 개편 — 현재 시스템 스냅샷)
  - **텍스트 전부 TextMeshPro(SDF) 전환**: `TmpFont`(malgun.ttf 동적 SDF), `UiFactory.CreateText`가 TMP 생성. TMP Essential Resources 임포트 + TMP 셰이더 Always Included.
  - **라운드 코너 UI 전면 적용**: 절차 생성 스프라이트 `Assets/Resources/UI/RoundedCorner.png`(9-슬라이스) + `UiSprite`/`UiFactory.CreateImage` 기본 라운드.
  - **전투 = 별도 씬**(`Assets/Scenes/Battle.unity`, 가산 로드로 메인 상태 보존): `BattleManager`/`BattleShip`/`BattleEffects`/`BattleContext`/`BattleCameraController`. 전투하기→활동력10→전투씬→결과→복귀. 나가기(경고→패배), 결과 팝업(승패/경험치/재화/아이템 획득·미획득). 드래그 패닝.
  - **인벤토리 선택/장착 모델 전면 개편**: `ModuleInstance`(class, Equipped/Coordinate), `PlayerState`가 단일 진실 공급원(Select/Equip/Unequip). `ShipBuilder`는 장착 모듈을 렌더(선택 시 슬롯 표시). `InventoryView`는 **썸네일 그리드 + 선택 시 상단 정보 표시**(선택/장착 상태 배지).
  - **모듈 테이블화**: `ModuleTable`(SO)+`Assets/Resources/ModuleTable.asset`, `ModuleCatalog`가 읽음(폴백 포함).
  - **WebGL 셰이더 null 크래시 수정**: URP Lit/Unlit/Simple Lit Always Included + `MaterialFactory.CreateLit`(셰이더/속성 가드)로 런타임 머티리얼 일원화.
  - **전투 고도화**: 정면(+Z) 추진/회전(추진체 수 ∝ 이동·회전), 무기 전방 빔 발사, **모듈 비주얼 `ModuleVisual`**(무기=전방 빔포/추진체=후방 부스터·분사/장갑=상판), **블록별 체력 HUD**(uGUI 추적, 만피 숨김), 1m 격자 기준 효과.
  - **모듈 시스템 기준 문서 `ITEMMODULE.md` 신규 작성**(격자/정면/종류/인벤토리/장착/비주얼/전투 규칙). 모듈 관련 작업은 이 문서를 기준으로 한다.

- 2026-06-23
  - **전투 수동 이동(가상 조이스틱) 추가**: `VirtualJoystick`(uGUI, 방향 -1~1 제공). `PlayerState.ManualMove`(저장 영속) ↔ 설정 탭 토글 버튼(`MainUi.ToggleMoveMode`, "전투 이동: 자동/수동"). `BattleContext.ManualMove`로 전투씬 전달 → `BattleManager`가 수동일 때 화면 하단 중앙 조이스틱 생성·매 프레임 입력 전달, `BattleShip.SetManualControl`/`SetMoveInput`로 입력 방향 전진/회전(자동 추격 대체).
  - **전투력 산정 통합**: `ModuleCatalog.ComputePower`(코어 기본 `CorePower=10` + 모듈별 `Attack*4+Health+Armor*3+Speed*5`)로 일원화. `InventoryView`/`BattleController`의 중복 계산 제거.
  - **스테이지 난이도 티어화**: `BattleController`가 스테이지가 오를수록 모듈 수 증가 + 약→강 티어 풀(`s_StageTiers`)에서 적 모듈 추첨. 전투 목록에 누적 승리 표시(`PlayerState.Wins`).
  - **개조 강조 효과**: `ShipBuilder`가 빈 슬롯·선택 장착 모듈을 깜빡임(이미션 펄스)으로 강조.
  - WebGL2 무압축 재빌드(`build/WebGL`, 약 79MB, 에러 0) → NAS 미러 배포(`\\DS216PLUSII\web\ddukbaek2\portfolio\spaceship-war-unity`).

- 2026-06-24
  - **전투 스테이지 순차 잠금**: `BattleController`가 스테이지를 `이전 스테이지 클리어 시 해금`(0번은 항상 해금)으로 처리. 잠긴 행은 어둡게 + `전투하기` 대신 `잠김` 표시(클릭 불가).
  - **클리어 배지/버튼 겹침 수정**: `✔ 클리어` 배지를 `전투하기` 버튼 좌측으로 분리 배치(20px 간격)해 가림 해소.
  - **활동력 자동 회복(분당 1)**: `PlayerState`가 마지막 기준 시각(`ActivityTimestamp`, UTC ticks 저장)으로부터 경과 분만큼 회복(최대치까지). 게임을 닫았다 열어도 경과 시간 반영. 가득이면 기준 시각만 현재로 유지.
  - **설정에 `활동력 가득 채우기` 버튼 추가**: `PlayerState.FillActivity` → 즉시 최대치.
  - **전투 화면에 `조작: 자동/수동` 토글 버튼 추가**(나가기 버튼 왼쪽): 전투 중 바로 우주선 조작을 **자동 추격 ↔ 조이스틱 수동**으로 전환하고 조이스틱 표시를 함께 토글. 값은 `PlayerState`에 저장(설정 탭과 동기화). **카메라 드래그 패닝은 모드와 무관하게 항상 동작**(우주선 이동≠카메라 이동). 조이스틱은 항상 생성하되 수동일 때만 표시.
  - **조이스틱 원형화**: `UiSprite.Circle`(절차 생성 흰색 원, 가장자리 안티에일리어싱, 캐시) 추가. 조이스틱 배경/핸들을 원형 스프라이트(`Image.type=Simple`)로 표시.
  - **화면 드래그 ↔ 조이스틱 분리**: `BattleCameraController`가 누름 **시작 시점**에 UI 위 여부(`IsOverUi`)를 판정해 그 누름이 끝날 때까지 카메라 패닝을 무시. (기존엔 시작 판정만 해서, 조이스틱 위에서 누른 채 포인터가 밖으로 나가면 카메라가 따라 움직이던 버그 수정.)
  - 재빌드·NAS 재배포(4회).

- 2026-06-27
  - **별 우주 스카이박스**: 절차 생성한 별 큐브맵 + `Skybox/Cubemap` 머티리얼을 `Assets/Resources/Skybox/`(Starfield.cubemap, StarfieldSkybox.mat) 에셋으로 굳힘(WebGL 셰이더 스트리핑 방지). `SkyboxFactory.Apply()`가 `RenderSettings.skybox`로 적용. 메인 카메라(`ShipCameraController`)와 전투 카메라(`BattleManager`)의 Clear Flags 를 Skybox 로 변경. 전투 배경 구체(`Modules/Skydome` 인스턴스화)는 스카이박스로 대체하며 제거(프리팹 에셋은 잔존 — 미사용). 별무리 피드백으로 512px·조밀(작은별 1400/중간 80/큰 14 per face, 밝기·색 다양성)로 재생성.
  - **레이저 발사체(블룸+파티클)**: `BattleProjectile`을 세장형 빛나는 코어(HDR 이미션 ×6) + `TrailRenderer` 잔상 + `ParticleSystem` 스파크로 재구성. 전투 카메라 post-processing 활성 + 전역 `Bloom` 볼륨(`BattleManager.BuildPostProcessing`)으로 레이저가 빛나게. (메인 씬 Global Volume 덕분에 URP post 셰이더가 빌드에 포함됨.)
  - 재빌드·NAS 재배포(2회).
  - **밸런스 테이블 엑셀→JSON 파이프라인**: `Assets/Tables/Modules.xlsx`·`Stages.xlsx`(엑셀 편집) → `tools/tables_to_json.py`(openpyxl) → `Assets/Resources/Tables/Modules.json`·`Stages.json`. 초기 엑셀은 `tools/gen_tables_xlsx.py`로 생성. 런타임은 `ModuleCatalog`(JSON 우선 → ModuleTable SO → 기본값), `BattleController.LoadStageRows`(JSON 우선 → 기본값)가 JSON을 읽음. 모듈 Type/Category는 enum 문자열, 색은 hex(`#RRGGBBAA`)로 표기. 스테이지 행: Index/EnemyName/ModuleCount/MaxTier/Seed.
  - **별 스카이박스 가는 점 재조정**: 글로우/큰 별 제거, 1px 점 위주(면당 2600개, Point 필터)로 더 가늘고 넓게 분포.
  - 재빌드·NAS 재배포.

- 2026-06-27 (2차)
  - **동력(전력) 시스템 + 동력로 모듈 1종(`ReactorCore`)**: 새 카테고리 `Reactor`. 동력로가 동력을 공급(`PowerSupply`), 무기/추진체가 소비(`PowerCost`). 모듈 테이블에 `PowerSupply`/`PowerCost` 컬럼 추가, 코어 기본 공급 `CorePowerSupply=6`. 총 공급 < 소비면 **부족분만 일부 정지**(코어에서 가까운 소비 모듈부터 켜고 예산 초과분 비활성, `ModuleCatalog.ComputeActive`). 전투(`BattleShip`)는 **플레이어 함선만** 적용 — 비활성 무기 미발사·비활성 추진체 추진 제외·비활성 모듈 흐리게. 인벤토리 헤더에 `동력 소비/공급` 표시. `ReactorCore.prefab`은 중장갑 프리팹 복제 + 청록 머티리얼. 기본 지급 10종(동력로 포함).
  - **하단 메뉴 6탭화**: 개조/상점/스테이지/**모험**/**이벤트**/설정. 모험·이벤트는 런타임 생성 '준비 중' 플레이스홀더. `MainUi`가 탭/화면을 균등(1/6) 재배치(씬 미존재 탭은 버튼 복제·화면 생성).
  - **기본 진입 탭 = 스테이지**(`DefaultScreenIndex=2`).
  - **메뉴 전환 시 화면 초기화**: `MainUi.ResetActiveScreen` → 개조(카메라 팬/거리 복귀 `ShipCameraController.ResetView` + 인벤토리 스크롤 top), 상점/스테이지 스크롤 top(+목록 갱신).
  - **카메라 2배 멀리**: 개조(`ShipCameraController` 초기 거리 ×2, MaxDistance 확장), 전투(`BattleManager` 카메라 (0,40,-20)).
  - **별 개수 1/3**(면당 730), 더 흐리게(exposure 0.85, 밝기 분포 r³).
  - 재빌드·NAS 재배포.

- 2026-06-28~29
  - **하단 네비 씬에 정식 6탭 배치**: 런타임 복제 폐기. 씬(`Scene.unity`)에 `NavButton_모험`/`이벤트` + `Screen_모험`/`이벤트` 추가, 버튼 사각 이미지. `MainUi`는 씬 오브젝트를 그대로 사용(생성 로직 제거).
  - **설정/우편함을 상단 우측 아이콘으로 이동**: 하단 설정 탭 제거(하단 5탭: 개조/상점/스테이지/모험/이벤트, 5등분). `TopIcons` 컨테이너에 설정(⚙)·우편함(✉) 아이콘 버튼(우상단 세로). 클릭 시 전체 화면 전환(`Screen_설정`/`Screen_우편함`). `MainUi`는 화면 7개 + 하단 5/상단 2 버튼을 통합 관리(앞 5는 라벨 교체, 뒤 2는 글리프 유지). 우편함은 '준비 중' 플레이스홀더.
  - **동력로 전용 외형**: `ReactorCore.prefab`을 베이스(어두운 금속) + 금속 하우징(실린더) + 청록 발광 에너지 코어(구체, HDR 이미션)로 재구성.
  - 재빌드·NAS 재배포.

- 2026-06-29
  - **상단 HUD 개편**: 레벨/경험치 영역 통합 — `Lv. N` 텍스트 + 경험치 **게이지 바**(숫자 미표시, fill 폭=경험치/레벨). 활동력 아래 **회복 타이머**(`+1까지 m:ss`, 가득이면 '가득 참') — `PlayerState.SecondsToNextRecovery` + `MainUi.Update`로 매 프레임 갱신(분당 1 회복은 기존 구현). **재화→크레딧** 표기 변경(HUD/상점/전투 결과).
  - **데크 서브탭**: 하단 '개조' 탭 라벨을 '데크'로 변경, 데크 화면 상단에 서브탭(개조/연구/승무원, 기본 개조). 개조=인벤토리 패널+월드 함선, 연구/승무원='준비 중' 플레이스홀더. `InventoryView.SetPanelVisible`로 토글, `MainUi.SelectSubTab`이 함선/패널/카메라 초기화 관리.
  - **설정/우편함 라벨 한글화**: 상단 아이콘 ⚙/✉ → '설정'/'우편함' 텍스트(버튼 가로 확장).
  - 재빌드·NAS 재배포.

- 2026-06-30
  - **금속 자원 추가**: `PlayerState.Metal` + `AddMetal`/`TrySpendMetal` + 저장, 상단 HUD 우측에 크레딧/금속 세로 2줄. 전투 승리 보상에 금속 연결(`BattleContext.ResultMetal`).
  - **전투 결과 팝업 리디자인**: 승/패 색 배너 + 큰 타이틀('★ 승 리 ★'/'패 배'), 보상을 **칩 카드**(경험치/크레딧/금속/아이템, 좌측 항목명 + 우측 값)로 표시.
  - **모듈 외곽선 URP 셰이더 `Spaceship/ModuleLit`**: 2-pass(인버티드 헐 외곽선 + 간이 URP 람베르트 라이팅). `_BaseColor`/`_EmissionColor`/`_OutlineColor`/`_OutlineWidth`. Resources/Modules의 모든 모듈 머티리얼(31개)을 이 셰이더로 교체(색 보존), Always Included 등록. `ModuleFactory`의 런타임 인버티드 헐(AddOutline) 제거 — 외곽선은 셰이더가 담당. (빌드 용량 79→68MB 감소.)
  - 재빌드·NAS 재배포.

## 7. 게임 루프 로드맵 (목표 설계)

사용자 의도에 따른 전체 흐름. 단계별로 구현한다.

1. **상점(상점 탭)**: 물건(모듈)을 재화로 구매 → **개조 탭의 인벤토리**에 모듈이 축적된다.
2. **개조(개조 탭)**: 인벤토리의 모듈을 **드래그**해서 함선의 빈 칸(슬롯)에 부착한다. (현재는 슬롯 탭=즉시 부착 / 추후 드래그-부착으로 확장)
3. **전투 목록(전투 탭)**: **스크롤뷰**로 대결 가능한 다른 우주선 목록 표시. "대결하기"를 누르면 **활동력 소모** → 전투 화면으로 전환.
4. **전투**: 자동 전투 연출. 결과 — 승리=**경험치+재화** 획득 / 패배=무보상. (전투당 활동력 **10** 소모, **분당 1 자동 회복** + 설정에서 즉시 충전)

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
- [x] 개조: 인벤토리 모듈 드래그→슬롯 부착(종류별 색 반영, 인벤토리 소모, 탈착 시 환원).
- [x] 전투 목록: 스크롤뷰 랜덤 적 + 대결하기(활동력 10) → 자동 전투 오버레이.
- [x] 전투: 결과 처리(승리=경험치+재화, 패배=무보상). 레벨업 규칙(레벨 N에서 N승).
- [x] 상단 HUD에 경험치(EXP) 추가.
- [x] 인벤토리 드래그/스크롤 분리, 상점 카드 UI, 모듈 평평한 연결 타일.
- [x] WebGL2 무압축 빌드(`build/WebGL`).
- [ ] 전투 상세 연출 고도화(빔/모듈 파괴/드리프트 — 현재는 HP 바 추상 연출).
- [ ] 모듈 종류/스탯/가격/전투력 밸런스 조정.
- [ ] 함선 본체 시각 개선(별/우주 배경, 엔진 분사 등).
- [ ] `GameManager.Awake()`의 `Resources.Load("Units/PlayerCharacter")` 경로 오류 정리(현재 런타임 예외).
