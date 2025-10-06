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

#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3 && !UNITY_4_5 && !UNITY_4_6 && !UNITY_4_7

namespace ScriptInspector
{

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(DefaultAsset), true)]
public class FGDefaultAssetInspector : ScriptInspector
{
	public static readonly HashSet<string> textFiles = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) {
		".md",
		".xaml",
		".text",
		".bat",
		".cmd",
		".sh",
		".command",
		".ini",
		".rsp",
		".plist",
		".log",
		".lua",
		".php",
		".py",
		".html",
		".htm",
		".css",
		".txt",
		".xml",
		".json",
		".js",
		".csv",
		".sql",
		".yarn",
		//".h",
		//".c",
		//".cpp",
		//".jslib",
		//".jspre",
	};
	
	[System.NonSerialized]
	private string checkedPath;
	[System.NonSerialized]
	private bool checkResult;

	private static string lastCheckedAssetPath;
	private static bool lastCheckedAssetResult;

	public static bool IsTextFile(UnityEngine.Object target)
	{
		var path = AssetDatabase.GetAssetPath(target);
		if (path != lastCheckedAssetPath)
		{
			lastCheckedAssetPath = path;
		
			var extension = System.IO.Path.GetExtension(path);
			lastCheckedAssetResult = !AssetDatabase.IsValidFolder(path) && textFiles.Contains(extension);
		}
		return lastCheckedAssetResult;
	}
	
	protected new void OnEnable()
	{
		CheckCanEditTarget();
		
		if (checkResult)
			base.OnEnable();
	}
	
	private void CheckCanEditTarget()
	{
		var path = AssetDatabase.GetAssetPath(target);
		if (path != checkedPath)
		{
			checkedPath = path;
			
			var extension = System.IO.Path.GetExtension(path);
			checkResult = !AssetDatabase.IsValidFolder(path) && textFiles.Contains(extension);
		}
	}

	public override void OnInspectorGUI()
	{
		CheckCanEditTarget();
		
		if (checkResult)
		{
			base.OnInspectorGUI();
		}
		else
		{
			DrawDefaultInspector();
		}
	}
	
	protected override void DoGUI()
	{
		var currentInspector = GetCurrentInspector();
		
		textEditor.OnInspectorGUI(false, new RectOffset(0, 0, 14, -13), currentInspector);
	}
}

}
#endif
