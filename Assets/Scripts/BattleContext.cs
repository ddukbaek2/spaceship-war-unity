using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 모듈 배치 정보(격자 좌표 + 종류).
/// </summary>
public struct ModulePlacement
{
	public Vector2Int Coordinate;
	public ModuleType Type;
}


/// <summary>
/// 전투 씬과 메인 씬 사이에서 데이터를 주고받는 컨텍스트.
/// </summary>
public static class BattleContext
{
	/// <summary>
	/// 플레이어 함선의 모듈 배치.
	/// </summary>
	public static List<ModulePlacement> PlayerLayout;

	/// <summary>
	/// 적 함선의 모듈 배치.
	/// </summary>
	public static List<ModulePlacement> EnemyLayout;

	/// <summary>
	/// 적 이름.
	/// </summary>
	public static string EnemyName;

	/// <summary>
	/// 적 전투력(보상 계산용).
	/// </summary>
	public static int EnemyPower;

	/// <summary>
	/// 스테이지 번호(클리어 기록용).
	/// </summary>
	public static int StageIndex;

	/// <summary>
	/// 승리 여부.
	/// </summary>
	public static bool ResultPlayerWon;

	/// <summary>
	/// 획득 재화.
	/// </summary>
	public static int ResultCurrency;

	/// <summary>
	/// 획득 경험치(승리 1, 패배 0).
	/// </summary>
	public static int ResultExperience;

	/// <summary>
	/// 아이템 획득 여부.
	/// </summary>
	public static bool ResultHasItem;

	/// <summary>
	/// 획득 아이템(모듈) 종류.
	/// </summary>
	public static ModuleType ResultItem;

	/// <summary>
	/// 전투 종료 콜백(결과는 위 필드에서 읽는다).
	/// </summary>
	public static Action OnFinished;
}
