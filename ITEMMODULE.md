# 모듈/아이템 시스템 설계 (ITEMMODULE)

> 모듈 블록 처리의 **단일 기준 문서**. 개조·전투·인벤토리에서 모듈을 다룰 때 이 규칙을 따른다.
> (해석이 흔들리지 않도록, 변경 시 이 문서를 먼저 갱신한다.)

최종 갱신: 2026-06-18

---

## 1. 격자/좌표/정면 (불변 규칙)

- 모든 모듈은 **정사각형, 1 칸 = 1 m** (`CellSize = 1`).
- 코어는 `(0,0)`. 모듈 좌표는 코어 기준 정수 격자 `Vector2Int` (오른쪽 +x, 위 +y).
- 로컬 위치 = `new Vector3(x * CellSize, 0, y * CellSize)`. (XZ 평면, 탑뷰)
- **함선 정면(front) = 로컬 +Z.** "코어 기준 정면"은 항상 +Z 방향이다.
- 인접 4방향: 상(0,1)/하(0,-1)/좌(-1,0)/우(1,0).
- **연결성**: 모든 모듈은 코어와 인접 경로로 연결되어야 한다. 끊기면 이탈(BFS prune).

## 2. 모듈 종류와 데이터 (테이블)

- 종류(`ModuleType`): `Weapon`(무기) / `Armor`(장갑) / `Engine`(추진체).
- 데이터는 **테이블**로 관리: `ModuleTable`(ScriptableObject, `Assets/Resources/ModuleTable.asset`).
  - 행(`ModuleRow`): Type, DisplayName, Price, Attack, Health, Speed, Range, Color.
  - `ModuleCatalog`가 테이블을 읽어 `ModuleDefinition`을 제공(테이블 없으면 코드 기본값 폴백).
- 현재 값:
  | 종류 | 이름 | 가격 | 공격 | 체력 | 이동 | 사거리 |
  |---|---|---|---|---|---|---|
  | Weapon | 무기 | 100 | 10 | 10 | 0 | 4 |
  | Armor | 장갑 | 80 | 0 | 40 | 0 | 0 |
  | Engine | 추진체 | 120 | 0 | 10 | 5 | 0 |

## 3. 개별 모듈 인스턴스 / 인벤토리 / 장착

- 모듈은 **스택 아님**. 각 개체 = `ModuleInstance { Id, Type, Equipped, Coordinate }`.
- 상태 축 2개:
  - **선택/비선택**: 현재 배치하려고 고른 모듈(`PlayerState.SelectedId`, 1개만).
  - **장착/미장착**: 함선에 부착됨 여부(`Equipped` + `Coordinate`).
- 단일 진실 공급원 = `PlayerState`.
  - `AddModule(type)`: 미장착으로 추가(상점 구매/전투 보상).
  - `SelectModule(id)`: 미장착 모듈 선택(같은 것 다시 누르면 해제 토글).
  - `TryEquip(id, coord)`: 빈 칸이면 장착(좌표 기록, 선택 해제).
  - `Unequip(id)`: 장착 해제(인벤토리에 남음).
  - `GetEquipped()`: 장착 모듈 배치 목록(함선/전투용).

## 4. 개조 화면 동작 (ShipBuilder)

- `PlayerState`의 **장착 모듈**만 렌더(코어 + 장착 모듈). 슬롯(빈 칸)은 **선택 중일 때만** 반투명 표시.
- 흐름: 인벤토리 썸네일 탭 → **선택**(노란 테두리, 상단 정보 표시) + 함선에 빈 칸 표시 → 빈 칸 탭 → **장착**. 장착 모듈 탭 → **해제**.
- 드래그 방식은 폐기(탭 선택 방식).

## 5. 모듈 비주얼 (ModuleVisual) — "발판" 아님

- 컨테이너 루트(빈 오브젝트) 아래에 본체 큐브 + 종류별 장식을 둔다(함선 회전 시 함께 정면 기준 회전).
- **본체**: 1m 정사각형 큐브(높이 ~0.34, 슬라브 느낌 회피).
- **무기**: 전방(+Z)에 **빔포**(포신 + 발광 머즐) 부착.
- **추진체**: 후방(-Z)에 **부스터 블록 + 분사 글로우**(깜빡임, `BattleThruster`) 부착.
- **장갑**: 상판(겹판) 부착.
- **코어**: 발광 중심부.
- 핵심 원칙: **무기는 전방, 추진/분사는 후방** (부착물 없는 방향 = 빈 공간으로 발사/분사).

## 6. 전투 동작 (BattleShip)

- 함선은 `GetEquipped()` 배치로 동일하게 구성. **모듈마다 개별 체력**(= Health, 코어 120).
- **이동**: 정면(+Z)이 상대를 향하도록 회전(회전속도 ∝ 추진체 수) + **전진 추진**(속도 ∝ 추진체 수). 사거리 유지.
- **사격**: 무기 모듈이 **전방(+Z, 빈 공간)** 으로 빔 발사. 빔은 1m 기준 크기, 상대 최근접 모듈로 약한 유도 후 명중.
- **추진 분사**: 추진체 부스터에서 후방으로 분사 글로우.
- **파괴**: 모듈 체력 0 → 떨어져 나감(파편). 코어와 끊긴 모듈도 이탈. 코어 0 → 격침.
- **체력 HUD**: 각 블록 체력을 **uGUI로 화면 추적** 표시. **최대 체력이면 숨김**, 감소 시 표시.

## 7. 관련 스크립트

- 데이터: `ModuleType`, `ModuleInstance`, `ModuleTable`/`ModuleRow`, `ModuleCatalog`/`ModuleDefinition`.
- 상태: `PlayerState`.
- 비주얼: `ModuleVisual`, `MaterialFactory`.
- 개조: `ShipBuilder`, `InventoryView`(썸네일 그리드 + 선택 정보).
- 전투: `BattleManager`, `BattleShip`, `BattleEffects`(BattleProjectile/Debris/Thruster), `BattleCameraController`, `BattleContext`.
