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

#if !UNITY3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3 && !UNITY_4_5

namespace ScriptInspector.Extensions.FlipbookGames
{

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditorInternal;

//[InitializeOnLoad]
[CustomPropertyDrawer(typeof(UnityEventBase), true)]
public class Si3UnityEventDrawer : UnityEventDrawer
{
	private const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
	private const BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
	
	static Si3UnityEventDrawer()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
			Initialize();
		else
			EditorApplication.update += Initialize;
	}
	
	static void Initialize()
	{
		EditorApplication.update -= Initialize;
		
		var sauType = typeof(UnityEventDrawer).Assembly.GetType("UnityEditor.ScriptAttributeUtility");
		if (sauType == null)
			return;
		var dtftField = sauType.GetField("s_DrawerTypeForType", staticFlags);
		if (dtftField == null)
			dtftField = sauType.GetField("k_DrawerTypeForType", staticFlags);
		if (dtftField == null)
			return;
		var dictionary = dtftField.GetValue(null) as IDictionary;
		if (dictionary == null)
		{
			var bdtftMethod = sauType.GetMethod("BuildDrawerTypeForTypeDictionary", staticFlags);
			if (bdtftMethod == null)
				return;
			
			bdtftMethod.Invoke(null, null);
			
			var value = dtftField.GetValue(null);
			dictionary = value as IDictionary;
			if (dictionary == null)
			{
				// Check if it's a Lazy<Dictionary<,>>
				var lazyType = value?.GetType();
				if (lazyType != null && lazyType.IsGenericType && lazyType.GetGenericTypeDefinition() == typeof(System.Lazy<>))
				{
					var valueProperty = lazyType.GetProperty("Value");
					if (valueProperty != null)
					{
						// Force creation of the lazy value
						dictionary = valueProperty.GetValue(value) as IDictionary;
					}
				}
			}
		}
		
		var drawerKeySetType = typeof(UnityEventDrawer).Assembly.GetType("UnityEditor.ScriptAttributeUtility+DrawerKeySet");
		
		var thisType = typeof(Si3UnityEventDrawer);
		var baseType = typeof(UnityEventBase);
		var unityDrawerType = typeof(UnityEventDrawer);
		
		if (drawerKeySetType == null)
		{
			//var drawerContainerType = typeof(UnityEventDrawer).Assembly.GetType("UnityEditor.ScriptAttributeUtility+CustomPropertyDrawerContainer");
			//if (drawerContainerType == null)
			//{
			//	return;
			//}
			
			//var drawerTypeField = drawerContainerType.GetField("drawerType", instanceFlags);
		
			//foreach (var i in dictionary)
			//{
			//	var kv = (DictionaryEntry) i;
			//	var key = kv.Key as System.Type;
			//	if (baseType.IsAssignableFrom(key))
			//	{
			//		var array = kv.Value as System.Array;
			//		if (array != null)
			//		{
			//			for (int j = array.GetLength(0); j --> 0; )
			//			{
			//				var element = array.GetValue(j);
			//				var drawerType = drawerTypeField.GetValue(element) as System.Type;
			//				if (drawerType == unityDrawerType)
			//				{
			//					drawerTypeField.SetValue(element, thisType);
			//				}
			//			}
			//		}
			//	}
			//}
		}
		else
		{
			var drawerField = drawerKeySetType.GetField("drawer", instanceFlags);
			var typeField = drawerKeySetType != null ? drawerKeySetType.GetField("type", instanceFlags) : null;
			if (typeField == null)
			{
				return;
			}
		
		resync:
			foreach (var i in dictionary)
			{
				var kv = (DictionaryEntry) i;
				var key = kv.Key as System.Type;
				var value = kv.Value;
				if (baseType.IsAssignableFrom(key))
				{
					var drawer = drawerField.GetValue(value) as System.Type;
					if (drawer == unityDrawerType)
					{
						drawerField.SetValue(value, thisType);
						dictionary[key] = value;
						goto resync;
					}
				}
			}
		}
	}
	
	[System.NonSerialized]
	private UnityEditorInternal.ReorderableList.ElementCallbackDelegate originalCallback;
	
	[System.NonSerialized]
	private SerializedProperty listenersArray;
	[System.NonSerialized]
	private UnityEventBase currentEvent;
	private FieldInfo targetField;
	[System.NonSerialized]
	private object persistentCallGroup;
	
	// static fields from Reflection.
	private static System.Type persistentCallType;
	private static System.Type persistentCallGroupType;
	private static FieldInfo persistentCallsField;
	private static FieldInfo listenersArrayField;
	private static MethodInfo findMethodMethod;
	private static MethodInfo getListenerMethod;
	private static MethodInfo restoreStateMethod;
	private static FieldInfo reorderableListField;
	private static MethodInfo rebuildMethod;
	private static FieldInfo dummyEventField;
	private static FieldInfo textField;
	
	private static readonly char[] propertyPathSeparators = { '.', '[', ']' };
	private static Dictionary<string, string[]> cachedSplits = new Dictionary<string, string[]>();
	
	private static readonly GUIContent tempContent = new GUIContent();
	[System.NonSerialized]
	private GUIStyle headerBackground;
	
	[System.NonSerialized]
	private bool expanded = true;
	
	protected override void DrawEventHeader(Rect headerRect)
	{
		var indentLevel = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		
		headerRect.xMin += 10f;
		if (currentEvent != null)
		{
			if (currentEvent.GetType().ToString() == "UnityEngine.EventSystems.EventTrigger+TriggerEvent")
				headerRect.xMax -= 20f;
			
			var rc = headerRect;
			rc.height = 16f;
			
			tempContent.text = "";
			tempContent.tooltip = "";
			expanded = EditorGUI.Foldout(rc, expanded, tempContent, true);
			
			var count = currentEvent.GetPersistentEventCount();
			tempContent.text = count.ToString();
			var size = EditorStyles.label.CalcSize(tempContent);
			headerRect.xMax -= size.x + 4f;
			if (Event.current.type == EventType.Repaint)
			{
				rc.xMin = rc.xMax - size.x;
				rc.y += 1.0f;
				EditorStyles.label.Draw(rc, tempContent, false, false, false, false);
			}
		}
		base.DrawEventHeader(headerRect);
		
		EditorGUI.indentLevel = indentLevel;
	}
	
	private static object[] objectArray1 = new object[1] { null };
	
	protected bool InitializeForProperty(SerializedProperty property, bool onGui)
	{
		if (restoreStateMethod == null)
		{
			restoreStateMethod = typeof(UnityEventDrawer).GetMethod("RestoreState", instanceFlags);
			reorderableListField = typeof(UnityEventDrawer).GetField("m_ReorderableList", instanceFlags);
			
			dummyEventField = typeof(UnityEventDrawer).GetField("m_DummyEvent", instanceFlags);
			textField = typeof(UnityEventDrawer).GetField("m_Text", instanceFlags);
			
			persistentCallType = System.Type.GetType("UnityEngine.Events.PersistentCall,UnityEngine");
			if (persistentCallType != null)
			{
				findMethodMethod = typeof(UnityEventBase).GetMethod("FindMethod", instanceFlags, null, new System.Type[] { persistentCallType }, null);
				persistentCallGroupType = System.Type.GetType("UnityEngine.Events.PersistentCallGroup,UnityEngine");
				if (persistentCallGroupType != null)
					getListenerMethod = persistentCallGroupType.GetMethod("GetListener", instanceFlags);
				persistentCallsField = typeof(UnityEventBase).GetField("m_PersistentCalls", instanceFlags);
				listenersArrayField = typeof(UnityEventDrawer).GetField("m_ListenersArray", instanceFlags);
				
				rebuildMethod = typeof(UnityEventBase).GetMethod("RebuildPersistentCallsIfNeeded", instanceFlags | BindingFlags.FlattenHierarchy);
			}
		}
		
		if (onGui)
		{
			if (restoreStateMethod == null || reorderableListField == null || findMethodMethod == null ||
				getListenerMethod == null || persistentCallsField == null || listenersArrayField == null ||
				rebuildMethod == null)
			{
				return false;
			}
		
			expanded = property.isExpanded;
			
			objectArray1[0] = property;
			restoreStateMethod.Invoke(this, objectArray1);
		
			var reorderableList = reorderableListField.GetValue(this) as UnityEditorInternal.ReorderableList;
			if (reorderableList == null)
			{
				return false;
			}
		
			if (originalCallback == null || originalCallback.Target == null)
				originalCallback = reorderableList.drawElementCallback;
			reorderableList.drawElementCallback = DrawEventListener;
		}
		else
		{
			if (listenersArrayField == null || restoreStateMethod == null)
			{
				return false;
			}
			objectArray1[0] = property;
			restoreStateMethod.Invoke(this, objectArray1);
		}
		
		//...
		
		listenersArray = listenersArrayField.GetValue(this) as SerializedProperty;
		if (listenersArray != null && !listenersArray.isArray)
		{
#if SI3_WARNINGS
			Debug.LogWarning(listenersArray);
#endif
			listenersArray = null;
		}
		
		object targetObject = property.serializedObject.targetObject;
		targetField = null;
		
		string[] fieldNames;
		if (!cachedSplits.TryGetValue(property.propertyPath, out fieldNames))
		{
			fieldNames = property.propertyPath.Split(propertyPathSeparators, System.StringSplitOptions.RemoveEmptyEntries);
			cachedSplits[property.propertyPath] = fieldNames;
		}
		
		for (var i = 0; targetObject != null && i < fieldNames.Length; ++i)
		{
			if (fieldNames[i] == "Array")
			{
				targetField = null;
				
				var array = targetObject as IList;
				if (array == null)
					break;
				var index = int.Parse(fieldNames[i += 2]);
				if (index >= array.Count)
					break;
				targetObject = array[index];
			}
			else
			{
				for (var type = targetObject.GetType(); type != typeof(object); type = type.BaseType)
				{
					targetField = type.GetField(fieldNames[i], instanceFlags | BindingFlags.DeclaredOnly);
					if (targetField != null)
						break;
				}
				if (targetField == null)
				{
					Debug.LogWarning("Could not find field #" + i + " in type " + targetObject.GetType().FullName + "\n" + string.Join(", ", fieldNames));
					targetObject = null;
					break;
				}
				targetObject = targetField.GetValue(targetObject);
			}
		}
		currentEvent = targetObject as UnityEventBase;
		
		return true;
	}
	
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		if (!InitializeForProperty(property, false))
			return base.GetPropertyHeight(property, label);
		
		var height = base.GetPropertyHeight(property, label);
		return property.isExpanded ? height : 16f;
	}

	[System.NonSerialized]
	private SerializedProperty currentProperty;
	
#if UNITY_2022_2_OR_NEWER
	private System.Action<UnityEngine.UIElements.VisualElement, int> originalBindItem;
	
	public override UnityEngine.UIElements.VisualElement CreatePropertyGUI(SerializedProperty property)
	{
		currentProperty = property;
		
		var visualElement = base.CreatePropertyGUI(property);
		
		if (!InitializeForProperty(property, false))
			return visualElement;

		var listView = visualElement.contentContainer.hierarchy.Children().ElementAtOrDefault(1) as UnityEngine.UIElements.ListView;
		if (listView == null)
			return visualElement;

		originalBindItem = listView.bindItem;
		listView.bindItem = OnListViewBindItem;
		//listView.headerTitle = "Targets";
		//listView.showFoldoutHeader = true;

		return visualElement;
	}

	private void OnListViewBindItem(UnityEngine.UIElements.VisualElement element, int index)
	{
		var parent = element;
		if (parent.parent != null)
			parent = element.parent;
		if (parent.parent != null)
			parent = element.parent;
		parent.UnregisterCallback<UnityEngine.UIElements.MouseDownEvent, int>(OnMouseDown, UnityEngine.UIElements.TrickleDown.NoTrickleDown);
		parent.RegisterCallback<UnityEngine.UIElements.MouseDownEvent, int>(OnMouseDown, index, UnityEngine.UIElements.TrickleDown.NoTrickleDown);
		
		UnityEngine.UIElements.Button button = null;
		
		if (element.childCount == 2)
		{
			button = new UnityEngine.UIElements.Button();
			button.clicked += () => OnEditScriptButton(button);
			button.tooltip = "Edit Script";
			button.style.paddingLeft = 0f;
			button.style.paddingRight = 0f;
			button.style.paddingTop = 0f;
			button.style.paddingBottom = 0f;
			button.style.minWidth = 19f;
			button.style.maxWidth = 19f;
			button.style.maxHeight = 19f;
			button.style.marginLeft = -1f;
			
			var image = new UnityEngine.UIElements.Image();
			image.image = icon;
			image.style.marginTop = 1f;
			image.style.marginBottom = 1f;
			
			button.Insert(0, image);
			
			element.Insert(2, button);
			
			var target = element.ElementAt(0);
			target = target != null ? target.ElementAt(1) : null;
			if (target != null)
			{
				target.RegisterCallback<UnityEngine.UIElements.ChangeEvent<UnityEngine.Object>>(e =>
				{
					int targetIndex = button.userData as int? ?? -1;
					var method = GetMethodInfo(targetIndex);
					button.SetEnabled(method != null);
				});
			}

			var column2 = element.ElementAt(1);
			var function = column2 != null ? column2.ElementAt(0) as UnityEngine.UIElements.DropdownField : null;
			if (function != null)
			{
				function.RegisterValueChangedCallback(e =>
				{
					int index = button.userData as int? ?? -1;
					var method = GetMethodInfo(index);
					button.SetEnabled(method != null);
				});
			}
		}
		else
		{
			button = element.ElementAt(2) as UnityEngine.UIElements.Button;
		}
		
		if (button != null)
		{
			button.userData = index;
	
			var method = GetMethodInfo(index);
			button.SetEnabled(method != null);
		}
		
		originalBindItem(element, index);
	}
	
	void OnEditScriptButton(UnityEngine.UIElements.Button button)
	{
		int index = button.userData as int? ?? -1;
		EditScript(index);
	}
	
	void OnMouseDown(UnityEngine.UIElements.MouseDownEvent e, int index)
	{
		if (e.modifiers != EventModifiers.None && e.modifiers != EventModifiers.Control && e.modifiers != EventModifiers.Command)
			return;
		if (e.button != 0)
			return;
		if (e.clickCount != 2)
			return;
		
		EditScript(index);
	}
#endif
	
	void EditScript(int index)
	{
		if (currentProperty == null || !InitializeForProperty(currentProperty, false))
			return;

		var method = GetMethodInfo(index);
		if (method == null)
			return;
		
		GoToMethod(method);
	}
	
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		currentProperty = property;
		
		if (!InitializeForProperty(property, true))
		{
			base.OnGUI(position, property, label);
			return;
		}
		
		expanded = property.isExpanded;
		if (!expanded)
			position.height = 16f;
		
		if (expanded || dummyEventField == null || textField == null)
		{
			base.OnGUI(position, property, label);
		}
		else
		{
			if (currentEvent != null)
			{
				if (Event.current.type == EventType.Repaint)
				{
					if (headerBackground == null)
						headerBackground = "RL Header";
					if (headerBackground != null)
						headerBackground.Draw(position, false, false, false, false);
				}
				
				dummyEventField.SetValue(this, currentEvent);
				textField.SetValue(this, label.text);
				
				var rc = position;
				rc.xMin += 6f;
				rc.xMax -= 6f;
				rc.height -= 2f;
				rc.y++;
				DrawEventHeader(rc);
			}
		}
		
		property.isExpanded = expanded;
		
		if (targetField != null)
		{
			if (System.Attribute.IsDefined(targetField, typeof(TooltipAttribute)))
			{
				var attribute = (TooltipAttribute) System.Attribute.GetCustomAttribute(fieldInfo, typeof(TooltipAttribute));
				if (attribute.tooltip != "")
				{
					position.height = 18f;
					tempContent.text = "";
					tempContent.tooltip = attribute.tooltip;
					GUI.Label(position, tempContent);
				}
			}
		}
	}
	
	private MethodInfo GetMethodInfo(int index)
	{
		if (currentEvent == null || listenersArray == null || index < 0)
			return null;
		
		var element = listenersArray.GetArrayElementAtIndex(index);
		if (element == null)
			return null;
		
		var propertyMethodName = element.FindPropertyRelative("m_MethodName");
		if (propertyMethodName == null)
			return null;
		
		if (string.IsNullOrEmpty(propertyMethodName.stringValue))
			return null;
		
		try
		{
			rebuildMethod.Invoke(currentEvent, null);
		} catch {}
		
		persistentCallGroup = persistentCallsField.GetValue(currentEvent);
		objectArray1[0] = index;
		var persistentCall = getListenerMethod.Invoke(persistentCallGroup, objectArray1);
		
		MethodInfo method = null;
		objectArray1[0] = persistentCall;
		method = findMethodMethod.Invoke(currentEvent, objectArray1) as MethodInfo;
		if (method == null)
			return null;
		
		var declaringType = method.DeclaringType;
		if (declaringType == null)
			return null;

		var assembly = AssemblyDefinition.FromAssembly(declaringType.Assembly);
		if (assembly != null && assembly.isScriptAssembly)
			return method;
		else
			return null;
	}
	
	private static Texture icon = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_editicon.sml" : "editicon.sml").image;
	private static GUIContent buttonContent = new GUIContent(icon, "Edit Script");
	
	private void DrawEventListener(Rect rect, int index, bool isactive, bool isfocused)
	{
		MethodInfo method = GetMethodInfo(index);
		
		bool isDoubleClick = Event.current.type == EventType.MouseDown
			&& Event.current.clickCount == 2 && rect.Contains(Event.current.mousePosition);
		
		Rect rc = rect;
		rc.y += 3f;
		rc.height = 15f;
		rc.xMin = rc.xMax - 21f;
		rc.width = 18f;
		
		rect.width -= 22f;
		if (originalCallback != null)
			originalCallback(rect, index, isactive, isfocused);
		
		if (isactive && isfocused && Event.current.type == EventType.KeyDown && Event.current.character == '\n')
			isDoubleClick = true;
		
		bool wasEnabled = GUI.enabled;
		GUI.enabled = method != null;
		bool isButtonClick = GUI.Button(rc, buttonContent, EditorStyles.miniButtonRight);
		if (method != null && (isButtonClick || isDoubleClick && Event.current.type != EventType.Used))
		{
			GoToMethod(method);
		}
		GUI.enabled = wasEnabled;
	}
	
	private void GoToMethod(MethodInfo method)
	{
		var declaringType = TypeReference.To(method.DeclaringType).definition;
		if (declaringType != null)
		{
			var member = declaringType.FindName(method.IsSpecialName ? method.Name.Substring("set_".Length) : method.Name, 0, false);
			if (member != null)
			{
				SymbolDeclaration declarations = null;
					
				var methodGroup = member as MethodGroupDefinition;
				if (member.kind == SymbolKind.MethodGroup && methodGroup != null)
				{
					var parameters = method.GetParameters();
					foreach (var m in methodGroup.methods)
					{
						//if (m.IsStatic)
						//	continue;
							
						var p = m.GetParameters() ?? new List<ParameterDefinition>();
						if (p.Count != parameters.Length)
							continue;
							
						for (var i = p.Count; i --> 0; )
						{
							var pType = p[i].TypeOf();
							if (pType == null)
								goto nextMethod;
								
							var parameterType = TypeReference.To(parameters[i].ParameterType).definition as TypeDefinitionBase;
							if (!pType.IsSameType(parameterType))
								goto nextMethod;
						}
							
						// Yay! We found it :)
						declarations = m.declarations;
						if (declarations == null)
						{
							declarations = FGFindInFiles.FindDeclarations(m);
							if (declarations == null)
							{
								// Boo! Something went wrong.
								break;
							}
						}
							
					nextMethod:
					continue;
					}
				}
				else
				{
					// It's a property
						
					declarations = member.declarations;
					if (declarations == null)
						declarations = FGFindInFiles.FindDeclarations(member);
				}
					
				if (declarations != null)
				{
					SymbolDeclaration next;
					for (var decl = declarations; decl != null; decl = next)
					{
						next = decl.next;
							
						var node = decl.NameNode();
						if (node == null || !node.HasLeafs())
							continue;
							
						string cuPath = null;
						for (var scope = decl.scope; scope != null; scope = scope.parentScope)
						{
							var cuScope = scope as CompilationUnitScope;
							if (cuScope != null)
							{
								cuPath = cuScope.path;
								break;
							}
						}
						if (cuPath == null)
							continue;
							
						var cuObject = AssetDatabase.LoadAssetAtPath(cuPath, typeof(MonoScript));
						if (cuObject == null)
							continue;
							
						var buffer = FGTextBufferManager.GetBuffer(cuObject);
						if (buffer == null)
							continue;
							
						// Declaration is valid!
							
						if (buffer.lines.Count == 0)
						{
							buffer.LoadImmediately();
						}
						var span = buffer.GetParseTreeNodeSpan(node);
						EditorApplication.delayCall += () =>
						{
							FGCodeWindow.OpenAssetInTab(AssetDatabase.AssetPathToGUID(cuPath), span.line + 1);
						};
					}
				}
			}
		}
	}
}
	
}


#endif
