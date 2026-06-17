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

## 7. 다음 단계 (TODO)

- [x] 탑뷰 2.5D 월드 공간 함선 화면 구성.
- [x] 모듈 부착/탈착 인터랙션 로직 구현(슬롯 탭→부착, 모듈 탭→탈착, 연결성 prune).
- [x] 자동 회전 옵션화 / 카메라 줌 / 전체 50% 축소 / 하단 네비게이션.
- [ ] 하단 네비게이션 탭별 화면(개조/상점/전투/설정) 실제 전환 연결.
- [ ] 모듈 종류/스탯/진화 규칙 정의.
- [ ] 함선 본체 시각 개선(별/우주 배경, 모듈 외형, 엔진 분사 등).
- [ ] 게임뷰 해상도를 세로(예: 1080x1920)로 고정해 실제 비율 확인.
- [ ] `GameManager.Awake()`의 `Resources.Load("Units/PlayerCharacter")` 경로 오류 정리(현재 런타임 예외).
