/// <summary>
/// 모듈 종류(총 9종). 종류 이름은 Resources/Modules 프리팹 이름과 일치한다.
/// </summary>
public enum ModuleType
{
	WeaponMachineGun,
	WeaponLaser,
	WeaponCannon,
	ArmorLight,
	ArmorHeavy,
	ArmorReactive,
	EngineSmall,
	EngineThrust,
	EngineTwin,
}


/// <summary>
/// 모듈 카테고리(동작 분류). 무기는 사격, 추진체는 추진/회전, 장갑은 체력.
/// </summary>
public enum ModuleCategory
{
	Weapon,
	Armor,
	Engine,
}
