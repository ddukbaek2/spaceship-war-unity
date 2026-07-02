using System;
using System.Collections.Generic;


/// <summary>
/// 전역 알림 로그. 게임 중 발생한 토스트/확인 팝업 등 주요 알림이 여기에 기록되어
/// 우편함의 '알림' 탭에서 모아볼 수 있다. (세션 단위 보관, 최신순)
/// </summary>
public static class NotificationLog
{
	private const int MaxEntries = 50;
	private static readonly List<string> s_Entries = new List<string>();

	/// <summary>
	/// 기록된 알림 목록(최신순).
	/// </summary>
	public static IReadOnlyList<string> Entries
	{
		get { return s_Entries; }
	}

	/// <summary>
	/// 목록이 변경되면 발생한다.
	/// </summary>
	public static event Action Changed;

	/// <summary>
	/// 알림이 새로 추가되면 발생한다(토스트 표시용).
	/// </summary>
	public static event Action<string> Added;

	/// <summary>
	/// 알림을 기록한다(최신이 맨 위). 토스트로도 표시된다.
	/// </summary>
	public static void Add(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return;
		}

		s_Entries.Insert(0, message);
		if (s_Entries.Count > MaxEntries)
		{
			s_Entries.RemoveAt(s_Entries.Count - 1);
		}

		if (Added != null)
		{
			Added(message);
		}

		if (Changed != null)
		{
			Changed();
		}
	}
}
