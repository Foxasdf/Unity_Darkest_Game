/* SCRIPT INSPECTOR 3
 * version 3.1.11, December 2024
 * Copyright © 2012-2024, Flipbook Games
 *
 * Script Inspector 3 - World's Fastest IDE for Unity
 *
 *
 * Follow me on http://twitter.com/FlipbookGames
 * Like Flipbook Games on Facebook http://facebook.com/FlipbookGames
 * Join discussion in Unity forums http://forum.unity3d.com/threads/138329
 * Contact info@flipbookgames.com for feedback, bug reports, or suggestions.
 * Visit http://flipbookgames.com/ for more info.
 */

namespace ScriptInspector
{

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SITabsWindow : EditorWindow
{
	[SerializeField]
	private Vector2 scrollPosition;
	
	[SerializeField]
	private int selectedIndex = -1;
	
	[System.NonSerialized]
	private FGTextEditor.ScrollViewState scrollViewState = new FGTextEditor.ScrollViewState();
	
	[System.NonSerialized]
	private GUIStyle itemStyle;

	[System.NonSerialized]
	private List<FGCodeWindow> orderedWindows = new List<FGCodeWindow>();

	[System.NonSerialized]
	private List<GUIContent> items = new List<GUIContent>();

	[MenuItem("Window/Script Inspector 3/SI Tabs", false, 800)]
	public static void ShowWindow()
	{
		GetWindow<SITabsWindow>("SI Tabs");
	}
	
	private void OnEnable()
	{
		EditorApplication.update += OnUpdate;
	}
	
	private void OnDisable()
	{
		EditorApplication.update -= OnUpdate;
	}
	
	private void OnUpdate()
	{
		if (!IsWindowsListUpToDate())
		{
			Repaint();
			return;
		}
		
		if (selectedIndex < 0 || selectedIndex >= orderedWindows.Count)
		{
			Repaint();
			return;
		}

		var focusedWindow = EditorWindow.focusedWindow as FGCodeWindow;
		if (focusedWindow != null)
		{
			if (orderedWindows[selectedIndex] != focusedWindow)
			{
				Repaint();
			}
		}
	}
	
	private bool IsWindowsListUpToDate()
	{
		var codeWindows = FGCodeWindow.CodeWindows;
		
		if (orderedWindows.Count != codeWindows.Count)
		{
			return false;
		}
		
		for (var i = orderedWindows.Count; i --> 0; )
		{
			var window = orderedWindows[i];
			
			if (!codeWindows.Contains(window))
			{
				return false;
			}
			
			var title = GetWindowTitle(window);
			if (title != items[i].text)
			{
				return false;
			}
		}
		
		return true;
	}
	
	private void UpdateWindowsList()
	{
		if (IsWindowsListUpToDate())
		{
			return;
		}
		
		// Get all open code windows.
		orderedWindows.Clear();
		foreach (FGCodeWindow window in FGCodeWindow.CodeWindows)
		{
			if (window && !string.IsNullOrEmpty(window.TargetAssetGuid))
			{
				orderedWindows.Add(window);
			}
		}
		
		// Sort by title ignoring case and the leading '*'.
		orderedWindows.Sort((a, b) =>
		{
			var titleA = GetWindowTitle(a);
			var titleB = GetWindowTitle(b);
			
			var indexA = !string.IsNullOrEmpty(titleA) && titleA[0] == '*' ? 1 : 0;
			var indexB = !string.IsNullOrEmpty(titleB) && titleB[0] == '*' ? 1 : 0;
			
			return string.Compare(titleA, indexA, titleB, indexB, int.MaxValue, System.StringComparison.InvariantCultureIgnoreCase);
		});
		
		// Create list items.
		items.Clear();
		for (var i = 0; i < orderedWindows.Count; ++i)
		{
			var window = orderedWindows[i];
			var guid = window.TargetAssetGuid;
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var name = GetWindowTitle(window);
			items.Add(new GUIContent(name, AssetDatabase.GetCachedIcon(path), path));
		}
	}
	
	private string GetWindowTitle(FGCodeWindow window)
	{
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0
		return window.title;
#else
		return window.titleContent.text;
#endif
	}
	
	private void OnGUI()
	{
		if (itemStyle == null)
		{
			itemStyle = new GUIStyle("PR Label");
			itemStyle.onNormal.textColor = itemStyle.normal.textColor;
			itemStyle.padding.left = 2;
			itemStyle.padding.right = 4;
			itemStyle.border = new RectOffset();
			itemStyle.margin = new RectOffset();
			itemStyle.fixedWidth = 0;
		}
		
		if (Event.current.type == EventType.Layout)
		{
			UpdateWindowsList();
		}
		
		if (items.Count == 0)
		{
			EditorGUILayout.LabelField("No SI tabs open", EditorStyles.centeredGreyMiniLabel);
			return;
		}
		
		var focusedWindow = EditorWindow.focusedWindow;
		var thisIsFocused = this == focusedWindow;
		var position = this.position;
		
		var itemWidth = position.width;
		var itemHeight = itemStyle.CalcHeight(GUIContent.none, 1f);
		
		var scrollAreaRect = new Rect(0f, 0f, position.width, position.height);
		var contentRect = new Rect(0f, 0f, 1f, itemHeight * orderedWindows.Count);
		
		if (contentRect.height > scrollAreaRect.height)
		{
			itemWidth -= 14f;
		}
		
		scrollPosition = FGTextEditor.BeginScrollView(scrollAreaRect, scrollPosition, contentRect, scrollViewState);
		
		var  scrollToSelection = false;
		
		var fromItem = (int)(scrollPosition.y / itemHeight);
		var toItem = Mathf.Min(items.Count - 1, fromItem + Mathf.CeilToInt(scrollAreaRect.height / itemHeight));
		
		// Handle keyboard input.
		if (Event.current.type == EventType.KeyDown)
		{
			if (!Event.current.alt && !Event.current.shift)
			{
				if (EditorGUI.actionKey)
				{
					if (Event.current.keyCode == KeyCode.DownArrow)
					{
						Event.current.Use();
						if (selectedIndex >= 0 && selectedIndex < orderedWindows.Count)
						{
							orderedWindows[selectedIndex].Focus();
							Repaint();
						}
					}
				}
				else if (Event.current.keyCode == KeyCode.UpArrow)
				{
					Event.current.Use();
					if (selectedIndex > 0)
					{
						selectedIndex--;
						scrollToSelection = true;
						Repaint();
					}
				}
				else if (Event.current.keyCode == KeyCode.DownArrow)
				{
					Event.current.Use();
					if (selectedIndex < items.Count - 1)
					{
						selectedIndex++;
						scrollToSelection = true;
						Repaint();
					}
				}
				else if (Event.current.keyCode == KeyCode.PageUp)
				{
					Event.current.Use();
					if (selectedIndex > 0)
					{
						var moveBy = (int)(scrollAreaRect.height / itemHeight);
						selectedIndex = Mathf.Max(0, selectedIndex - (int)(scrollAreaRect.height / itemHeight));
						scrollPosition.y = Mathf.Max(0f, scrollPosition.y - moveBy * itemHeight);
						scrollToSelection = true;
						Repaint();
					}
				}
				else if (Event.current.keyCode == KeyCode.PageDown)
				{
					Event.current.Use();
					if (selectedIndex < items.Count - 1)
					{
						var moveBy = (int)(scrollAreaRect.height / itemHeight);
						selectedIndex = Mathf.Min(items.Count - 1, selectedIndex + moveBy);
						scrollPosition.y = Mathf.Max(0f, Mathf.Min(contentRect.height - scrollAreaRect.height, scrollPosition.y + moveBy * itemHeight));
						scrollToSelection = true;
						Repaint();
					}
				}
				else if (Event.current.keyCode == KeyCode.Home)
				{
					Event.current.Use();
					if (selectedIndex > 0)
					{
						selectedIndex = 0;
						scrollToSelection = true;
						Repaint();
					}
				}
				else if (Event.current.keyCode == KeyCode.End)
				{
					Event.current.Use();
					if (selectedIndex < items.Count - 1)
					{
						selectedIndex = items.Count - 1;
						scrollToSelection = true;
						Repaint();
					}
				}
				else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
				{
					Event.current.Use();
					if (selectedIndex >= 0 && selectedIndex < orderedWindows.Count)
					{
						orderedWindows[selectedIndex].Focus();
						Repaint();
					}
				}
				else if (Event.current.keyCode == KeyCode.Escape)
				{
					Event.current.Use();
					var guid = FGCodeWindow.GetGuidHistory().FirstOrDefault();
					if (!string.IsNullOrEmpty(guid))
					{
						for (var i = orderedWindows.Count; i --> 0; )
						{
							var window = orderedWindows[i];
							if (guid == window.TargetAssetGuid)
							{
								selectedIndex = i;
								window.Focus();
								Repaint();
								break;
							}
						}
					}
				}
				
				if (scrollToSelection)
				{
					if ((selectedIndex + 1) * itemHeight > scrollPosition.y + scrollAreaRect.height)
					{
						scrollPosition.y = (selectedIndex + 1) * itemHeight - scrollAreaRect.height;
					}
					if (selectedIndex * itemHeight < scrollPosition.y)
					{
						scrollPosition.y = selectedIndex * itemHeight;
					}
				}
			}
		}
		
		for (int i = fromItem; i <= toItem; ++i)
		{
			var rect = new Rect(0f, i * itemHeight, itemWidth, itemHeight);
			
			if (Event.current.type == EventType.Layout && !thisIsFocused && focusedWindow == orderedWindows[i])
			{
				selectedIndex = i;
			}
			
			if (Event.current.type == EventType.Repaint)
			{
				var isSelected = i == selectedIndex;
				itemStyle.Draw(rect, items[i], false, false, isSelected, thisIsFocused);
				
				if (EditorApplication.projectWindowItemOnGUI != null)
				{
					EditorApplication.projectWindowItemOnGUI(orderedWindows[i].TargetAssetGuid, rect);
				}
			}
			else
			{
				if (EditorApplication.projectWindowItemOnGUI != null)
					EditorApplication.projectWindowItemOnGUI(orderedWindows[i].TargetAssetGuid, rect);
				
				if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
				{
					if (Event.current.button == 0)
					{
						if (Event.current.clickCount == 1)
						{
							selectedIndex = i;
							Repaint();
						}
						else if (Event.current.clickCount == 2)
						{
							orderedWindows[i].Focus();
							Repaint();
						}
					}
				}
			}
		}
		
		FGTextEditor.EndScrollView(true, scrollViewState);
	}
}

}
