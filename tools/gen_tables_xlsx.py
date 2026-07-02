# -*- coding: utf-8 -*-
"""
모듈/스테이지 밸런스 엑셀(.xlsx)을 생성한다(초기 1회). 이후에는 엑셀을 직접 편집하고
tables_to_json.py 로 JSON 으로 변환해 게임에서 사용한다.
"""
import os
from openpyxl import Workbook

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TABLES_DIR = os.path.join(ROOT, "Assets", "Tables")
os.makedirs(TABLES_DIR, exist_ok=True)


def rgba_hex(r, g, b, a=1.0):
    return "#{:02X}{:02X}{:02X}{:02X}".format(
        round(r * 255), round(g * 255), round(b * 255), round(a * 255)
    )


# (Type, Category, DisplayName, Price, Attack, Health, Armor, Speed, Range, PowerSupply, PowerCost, ColorRGB)
MODULES = [
    ("WeaponMachineGun", "Weapon", "기관포", 90, 4, 8, 1, 0, 3, 0, 2, (0.95, 0.45, 0.40)),
    ("WeaponLaser", "Weapon", "레이저", 130, 7, 10, 1, 0, 6, 0, 4, (1.00, 0.35, 0.50)),
    ("WeaponCannon", "Weapon", "캐논", 180, 12, 14, 2, 0, 4, 0, 6, (0.80, 0.25, 0.25)),
    ("ArmorLight", "Armor", "경장갑", 60, 0, 30, 3, 0, 0, 0, 0, (0.55, 0.66, 0.80)),
    ("ArmorHeavy", "Armor", "중장갑", 110, 0, 65, 6, 0, 0, 0, 0, (0.42, 0.50, 0.62)),
    ("ArmorReactive", "Armor", "반응장갑", 130, 0, 45, 5, 0, 0, 0, 0, (0.50, 0.62, 0.70)),
    ("EngineSmall", "Engine", "소형엔진", 90, 0, 8, 1, 4, 0, 0, 2, (0.95, 0.72, 0.35)),
    ("EngineThrust", "Engine", "추진엔진", 140, 0, 10, 1, 7, 0, 0, 3, (1.00, 0.62, 0.25)),
    ("EngineTwin", "Engine", "쌍발엔진", 190, 0, 12, 1, 10, 0, 0, 5, (1.00, 0.55, 0.18)),
    ("ReactorCore", "Reactor", "동력로", 100, 0, 16, 2, 0, 0, 12, 0, (0.50, 0.95, 0.80)),
]

ENEMY_NAMES = [
    "붉은 약탈자", "검은 유성", "강철 전갈", "녹빛 추적자", "푸른 망령",
    "황금 독수리", "심연의 포식자", "은빛 칼날", "혹한의 사냥꾼", "폭풍 전위",
]
STAGE_COUNT = 8
STAGE_TIER_COUNT = 3


def build_modules():
    workbook = Workbook()
    sheet = workbook.active
    sheet.title = "Modules"
    sheet.append(["Type", "Category", "DisplayName", "Price", "Attack", "Health", "Armor", "Speed", "Range", "PowerSupply", "PowerCost", "ColorHex"])
    for entry in MODULES:
        fields = list(entry[:11])
        color = entry[11]
        sheet.append(fields + [rgba_hex(*color)])
    workbook.save(os.path.join(TABLES_DIR, "Modules.xlsx"))


def build_stages():
    workbook = Workbook()
    sheet = workbook.active
    sheet.title = "Stages"
    sheet.append(["Index", "EnemyName", "ModuleCount", "MaxTier", "Seed"])
    for index in range(STAGE_COUNT):
        enemy_name = ENEMY_NAMES[index % len(ENEMY_NAMES)]
        module_count = min(10 + index * 12, 120)
        max_tier = min(index // 3, STAGE_TIER_COUNT - 1)
        seed = 7919 + index * 131
        sheet.append([index, enemy_name, module_count, max_tier, seed])
    workbook.save(os.path.join(TABLES_DIR, "Stages.xlsx"))


if __name__ == "__main__":
    build_modules()
    build_stages()
    print("generated:", os.path.join(TABLES_DIR, "Modules.xlsx"))
    print("generated:", os.path.join(TABLES_DIR, "Stages.xlsx"))
