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
using System.Reflection;
using UnityEngine.UIElements;


[CustomEditor(typeof(Shader))]
public class ShaderInspector : ScriptInspector
{
	private static System.Type unityShaderInspectorType;
	private static MethodInfo internalSetTargetsMethod;
	private static MethodInfo onEnableMethod;
	private static MethodInfo onDisableMethod;
	private static MethodInfo onDestroyMethod;

	public bool showInfo = true;
	private Editor unityShaderInspector;

	protected new void OnEnable()
	{
		base.OnEnable();
		
		if (unityShaderInspectorType == null)
		{
			unityShaderInspectorType = typeof(Editor).Assembly.GetType("UnityEditor.ShaderInspector");
			var amplifyShaderEditorInspectorType = System.Type.GetType("UnityEditor.CustomShaderInspector,AmplifyShaderEditor");
			if (amplifyShaderEditorInspectorType != null)
			{
				unityShaderInspectorType = amplifyShaderEditorInspectorType;
			}
			if (unityShaderInspectorType != null)
			{
				const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				internalSetTargetsMethod = unityShaderInspectorType.GetMethod("InternalSetTargets", flags);
				onEnableMethod = unityShaderInspectorType.GetMethod("OnEnable", flags);
				onDisableMethod = unityShaderInspectorType.GetMethod("OnDisable", flags);
				onDestroyMethod = unityShaderInspectorType.GetMethod("OnDestroy", flags);
			}
		}
		if (targets != null && internalSetTargetsMethod != null)
		{
			if (unityShaderInspector == null)
			{
				unityShaderInspector = Editor.CreateEditor(target, unityShaderInspectorType);
				if (unityShaderInspector)
				{
					internalSetTargetsMethod.Invoke(unityShaderInspector, new object[] { targets.Clone() });
				}
			}
		}
		if (onEnableMethod != null)
			onEnableMethod.Invoke(unityShaderInspector, null);	
	}

	protected new void OnDisable()
	{
		if (onDisableMethod != null)
			onDisableMethod.Invoke(unityShaderInspector, null);
	
		base.OnDisable();
	}

	protected void OnDestroy()
	{
		if (onDestroyMethod != null && unityShaderInspector != null)
			onDestroyMethod.Invoke(unityShaderInspector, null);
	}

	public override void OnInspectorGUI()
	{
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
		EditorGUIUtility.LookLikeControls();
#else
		EditorGUIUtility.labelWidth = 0f;
		EditorGUIUtility.fieldWidth = 0f;
#endif
		EditorGUI.indentLevel = 0;
	
		var rc = GUILayoutUtility.GetRect(1f, 13f);
		rc.yMin -= 5f;
		var enabled = GUI.enabled;
		GUI.enabled = true;
		showInfo = InspectorFoldout(rc, showInfo, targets);
		GUI.enabled = enabled;
		if (showInfo)
		{
			if (unityShaderInspector)
				unityShaderInspector.OnInspectorGUI();
		}
	
		var assetPath = AssetDatabase.GetAssetPath(target);
		if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWithIgnoreCase("assets/"))
			base.OnInspectorGUI();
	}

	protected override void DoGUI()
	{
		var currentInspector = GetCurrentInspector();
	
#if UNITY_4_3
		textEditor.OnInspectorGUI(false, new RectOffset(0, -4, showInfo ? 29 : 22, -13), currentInspector);
#else
		textEditor.OnInspectorGUI(false, new RectOffset(0, 0, showInfo ? 29 : 22, -13), currentInspector);
#endif
	}

	private static GUIStyle inspectorTitlebar;
	private static GUIStyle inspectorTitlebarText;

	public static bool InspectorFoldout(Rect position, bool foldout, UnityEngine.Object[] targetObjs)
	{
		if (inspectorTitlebar == null)
		{
			inspectorTitlebar = new GUIStyle("IN Title");
			inspectorTitlebarText = "IN TitleText";
		}
	
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3
		EditorGUIUtility.LookLikeControls(Screen.width, 0f);
#elif UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
		EditorGUIUtility.LookLikeControls(EditorGUIUtility.currentViewWidth, 0f);
#else
		EditorGUIUtility.fieldWidth = 0f;
		EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth;
#endif
		foldout = EditorGUI.Foldout(position, foldout, GUIContent.none, true, EditorStyles.foldout);
	
		position = inspectorTitlebar.padding.Remove(position);
		if (Event.current.type == EventType.Repaint)
			inspectorTitlebarText.Draw(position, "Shader Info", false, false, foldout, false);
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
		EditorGUIUtility.LookLikeControls();
#else
		EditorGUIUtility.labelWidth = 0f;
		EditorGUIUtility.fieldWidth = 0f;
#endif
	
		return foldout;
	}

	public override bool HasPreviewGUI()
	{
		if (unityShaderInspector)
			return unityShaderInspector.HasPreviewGUI();

		return false;
	}

	public override void OnPreviewGUI(Rect r, GUIStyle background)
	{
		if (unityShaderInspector)
			unityShaderInspector.OnPreviewGUI(r, background);
	}

	public override void OnPreviewSettings()
	{
		if (unityShaderInspector)
			unityShaderInspector.OnPreviewSettings();

		base.OnPreviewSettings();
	}

#if UNITY_2023_2_OR_NEWER
	public override UnityEngine.UIElements.VisualElement CreatePreview(VisualElement inspectorPreviewWindow)
	{
		if (unityShaderInspector)
			return unityShaderInspector.CreatePreview(inspectorPreviewWindow);

		return null;
	}
#endif
}

}
