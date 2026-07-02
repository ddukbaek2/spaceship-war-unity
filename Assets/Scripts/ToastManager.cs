using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 토스트 메시지 매니저. 알림 로그에 새 항목이 추가되면 화면 상단에 토스트를 띄운다.
/// 여러 개가 겹치면 세로로 스택되어 순차적으로 나타났다가 페이드로 사라진다.
/// (표시되는 내용은 NotificationLog 를 통해 우편함에도 기록된다)
/// </summary>
public class ToastManager : MonoBehaviour
{
	/// <summary>
	/// 화면에 떠 있는 토스트 하나.
	/// </summary>
	private class ToastItem
	{
		public RectTransform Rect;
		public CanvasGroup Group;
		public float Age;
		public float Height;
		public float CurrentY;
	}

	private const float FadeIn = 0.3f;
	private const float Hold = 2.6f;
	private const float FadeOut = 0.6f;
	private const float Spacing = 10f;

	private RectTransform m_Container;
	private readonly List<ToastItem> m_Toasts = new List<ToastItem>();

	/// <summary>
	/// 초기화됨. 토스트 컨테이너를 만들고 알림 추가 이벤트를 구독한다.
	/// </summary>
	private void Start()
	{
		var canvasObject = GameObject.Find("UI/Canvas");
		if (canvasObject == null)
		{
			return;
		}

		var containerObject = new GameObject("ToastContainer", typeof(RectTransform));
		containerObject.layer = canvasObject.layer;
		containerObject.transform.SetParent(canvasObject.transform, false);
		m_Container = (RectTransform)containerObject.transform;
		m_Container.anchorMin = new Vector2(0.5f, 1f);
		m_Container.anchorMax = new Vector2(0.5f, 1f);
		m_Container.pivot = new Vector2(0.5f, 1f);
		m_Container.sizeDelta = new Vector2(760f, 0f);
		m_Container.anchoredPosition = new Vector2(0f, -220f);
		m_Container.SetAsLastSibling();

		NotificationLog.Added += Enqueue;
	}

	/// <summary>
	/// 파괴됨.
	/// </summary>
	private void OnDestroy()
	{
		NotificationLog.Added -= Enqueue;
	}

	/// <summary>
	/// 토스트 하나를 생성해 목록 맨 위에 추가한다.
	/// </summary>
	private void Enqueue(string message)
	{
		if (m_Container == null)
		{
			return;
		}

		var lines = Mathf.Max(1, Mathf.CeilToInt(message.Length / 26f));
		var height = 40f + 30f * lines;

		var card = UiFactory.CreateImage("Toast", m_Container, new Color(0.14f, 0.16f, 0.22f, 0.96f));
		var rect = (RectTransform)card.transform;
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.sizeDelta = new Vector2(720f, height);
		rect.anchoredPosition = new Vector2(0f, 0f);

		var group = card.AddComponent<CanvasGroup>();
		group.alpha = 0f;
		group.blocksRaycasts = false;

		var text = UiFactory.CreateText("Text", card.transform, null, message, 26, new Color(0.92f, 0.94f, 0.98f, 1f), TextAnchor.MiddleCenter);
		text.raycastTarget = false;
		text.enableWordWrapping = true;
		var textRect = text.rectTransform;
		textRect.anchorMin = new Vector2(0f, 0f);
		textRect.anchorMax = new Vector2(1f, 1f);
		textRect.offsetMin = new Vector2(20f, 6f);
		textRect.offsetMax = new Vector2(-20f, -6f);

		var item = new ToastItem();
		item.Rect = rect;
		item.Group = group;
		item.Age = 0f;
		item.Height = height;
		item.CurrentY = 0f;
		m_Toasts.Insert(0, item);
	}

	/// <summary>
	/// 매 프레임 토스트 수명/투명도/스택 위치를 갱신하고, 수명이 끝난 토스트를 제거한다.
	/// </summary>
	private void Update()
	{
		var total = FadeIn + Hold + FadeOut;
		var stackY = 0f;
		for (int index = 0; index < m_Toasts.Count; index++)
		{
			var toast = m_Toasts[index];
			toast.Age += Time.unscaledDeltaTime;

			float alpha;
			if (toast.Age < FadeIn)
			{
				alpha = toast.Age / FadeIn;
			}
			else if (toast.Age > total - FadeOut)
			{
				alpha = Mathf.Max(0f, (total - toast.Age) / FadeOut);
			}
			else
			{
				alpha = 1f;
			}

			toast.Group.alpha = alpha;

			var targetY = -stackY;
			toast.CurrentY = Mathf.Lerp(toast.CurrentY, targetY, Time.unscaledDeltaTime * 12f);
			toast.Rect.anchoredPosition = new Vector2(0f, toast.CurrentY);

			stackY += toast.Height + Spacing;
		}

		for (int index = m_Toasts.Count - 1; index >= 0; index--)
		{
			if (m_Toasts[index].Age >= total)
			{
				Destroy(m_Toasts[index].Rect.gameObject);
				m_Toasts.RemoveAt(index);
			}
		}
	}
}
