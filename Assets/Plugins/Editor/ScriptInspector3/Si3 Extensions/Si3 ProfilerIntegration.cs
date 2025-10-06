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

	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using UnityEngine.UIElements;
	using UnityEditor.Profiling;
	using UnityEditorInternal;
	using System.Linq;

	[InitializeOnLoad]
	public class Si3ProfilerIntegration
	{
		static List<EditorWindow> all = new List<EditorWindow>();
	
		static Si3ProfilerIntegration()
		{
			EditorApplication.update += OnFirstUpdate;
		}
	
		static void OnFirstUpdate()
		{
			EditorApplication.update -= OnFirstUpdate;
			Integrate();
		}
	
		const System.Reflection.BindingFlags instanceFlags = System.Reflection.BindingFlags.Instance
		| System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;

		static System.Type profilerWindowType = System.Type.GetType("UnityEditor.ProfilerWindow,UnityEditor");
		static System.Reflection.FieldInfo vertSplitField = profilerWindowType == null ? null :
			profilerWindowType.GetField("m_VertSplit", instanceFlags);
		static System.Reflection.FieldInfo currentAreaField = profilerWindowType == null ? null :
			profilerWindowType.GetField("m_CurrentArea", instanceFlags);
		static System.Reflection.FieldInfo profilerModulesField = profilerWindowType == null ? null :
			profilerWindowType.GetField("m_ProfilerModules", instanceFlags);
		static System.Reflection.PropertyInfo selectedModuleProperty = profilerWindowType == null ? null :
			profilerWindowType.GetProperty("selectedModule", instanceFlags) ?? profilerWindowType.GetProperty("SelectedModule", instanceFlags);

		static System.Type CPUorGPUProfilerModuleType = System.Type.GetType("UnityEditorInternal.Profiling.CPUorGPUProfilerModule,UnityEditor");
		static System.Type cpuProfilerModuleType = System.Type.GetType("UnityEditorInternal.Profiling.CPUProfilerModule, UnityEditor");
		static System.Reflection.FieldInfo frameDataHierarchyViewField =
			cpuProfilerModuleType != null ? cpuProfilerModuleType.GetField("m_FrameDataHierarchyView", instanceFlags) :
			CPUorGPUProfilerModuleType != null ? CPUorGPUProfilerModuleType.GetField("m_FrameDataView", instanceFlags) : null;
		static System.Reflection.FieldInfo viewTypeField =
			cpuProfilerModuleType != null ? cpuProfilerModuleType.GetField("m_ViewType", instanceFlags) :
			CPUorGPUProfilerModuleType != null ? CPUorGPUProfilerModuleType.GetField("m_ViewType", instanceFlags) : null;

		static System.Type profilerFrameDataHierarchyViewType = System.Type.GetType("UnityEditorInternal.Profiling.ProfilerFrameDataHierarchyView,UnityEditor");
		static System.Reflection.FieldInfo detailedViewSpliterStateField = profilerFrameDataHierarchyViewType == null ? null :
			profilerFrameDataHierarchyViewType.GetField("m_DetailedViewSpliterState", instanceFlags) ??
			profilerFrameDataHierarchyViewType.GetField("m_DetailedViewSplitterState", instanceFlags);
		static System.Reflection.FieldInfo detailedViewTypeField = profilerFrameDataHierarchyViewType == null ? null :
			profilerFrameDataHierarchyViewType.GetField("m_DetailedViewType", instanceFlags);

		static System.Type splitterStateType = System.Type.GetType("UnityEditor.SplitterState,UnityEditor");
		static System.Reflection.FieldInfo realSizesField = splitterStateType == null ? null :
			splitterStateType.GetField("realSizes", instanceFlags);

		static void Integrate()
		{
			if (profilerWindowType == null)
				return;
		
			all.Clear();
		
			var allObjects = Resources.FindObjectsOfTypeAll(profilerWindowType);
			foreach (EditorWindow pw in allObjects)
			{
				IntegrateWith(pw);
			}
		
			EditorApplication.update += OnUpdate;
		}
	
		static EditorWindow lastFocusedWindow;
		static void OnUpdate()
		{
			var focusedWindow = EditorWindow.focusedWindow;
			if (focusedWindow == null || object.ReferenceEquals(focusedWindow, lastFocusedWindow))
				return;
		
			lastFocusedWindow = focusedWindow;
			if (lastFocusedWindow.GetType() != profilerWindowType)
				return;
		
			if (all.Contains(focusedWindow))
				return;
		
			IntegrateWith(focusedWindow);
		}
	
		static void IntegrateWith(EditorWindow pw)
		{
			var element = pw.rootVisualElement.Query("module-details-view__container").First();
			if (element == null)
				element = pw.rootVisualElement.parent;
			if (element == null)
				element = pw.rootVisualElement;

			if (element != null)
				element.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
		
			all.Add(pw);
		}
	
		static void OnMouseDown(MouseDownEvent e)
		{
			if (e.modifiers != EventModifiers.None)
				return;
			if (e.button != 0)
				return;
			if (e.clickCount != 2)
				return;

			var pw = EditorWindow.focusedWindow;
			if (pw == null)
				return;
		
#if !UNITY_2020_3_OR_NEWER
			if (vertSplitField != null && realSizesField != null)
			{
				var vertSplit = vertSplitField.GetValue(pw);
				if (vertSplit != null)
				{
#if UNITY_2020_1_OR_NEWER
					var realSizes = realSizesField.GetValue(vertSplit) as float[];
#else
					var realSizes = realSizesField.GetValue(vertSplit) as int[];
#endif
					if (realSizes != null)
					{
						var offset = pw.position.height - realSizes[1] + 65;
						var y = e.mousePosition.y - offset;
						if (y < 0)
							return;
					}
				}
			}
#endif
		
#if UNITY_2020_2_OR_NEWER
			if (cpuProfilerModuleType != null && selectedModuleProperty != null && frameDataHierarchyViewField != null
				&& detailedViewSpliterStateField != null && realSizesField != null)
			{
				var selectedModule = selectedModuleProperty.GetValue(pw);
				if (selectedModule != null && cpuProfilerModuleType == selectedModule.GetType())
				{
					var hierarchyView = frameDataHierarchyViewField.GetValue(selectedModule);
					if (hierarchyView != null)
					{
						var detailedViewType = detailedViewTypeField.GetValue(hierarchyView);
						if (detailedViewType != null && ((int)detailedViewType) != 0)
						{
							var splitterState = detailedViewSpliterStateField.GetValue(hierarchyView);
							if (splitterState != null)
							{
								var realSizes = realSizesField.GetValue(splitterState) as float[];
								if (realSizes != null)
								{
									var x = e.mousePosition.x - realSizes[0];
									if (x >= 0)
										return;
								}
							}
						}
					}
				}
			}
#else
			if (profilerModulesField != null && viewTypeField != null && frameDataHierarchyViewField != null &&
				detailedViewTypeField != null && detailedViewSpliterStateField != null && realSizesField != null)
			{
				var profilerModules = profilerModulesField.GetValue(pw) as object[];
				if (profilerModules != null && profilerModules.Length >= 1)
				{
					var cpuProfilerModule = profilerModules[0];
					if (cpuProfilerModule != null)
					{
						var viewType = viewTypeField.GetValue(cpuProfilerModule);
						if (viewType != null && ((int)viewType) != 1)
						{
							var frameDataHierarchyView = frameDataHierarchyViewField.GetValue(cpuProfilerModule);
							if (frameDataHierarchyView != null)
							{
								var detailedViewType = detailedViewTypeField.GetValue(frameDataHierarchyView);
								if (detailedViewType != null && ((int)detailedViewType) != 0)
								{
									var splitterState = detailedViewSpliterStateField.GetValue(frameDataHierarchyView);
									if (splitterState != null)
									{
#if UNITY_2020_1_OR_NEWER
										var realSizes = realSizesField.GetValue(splitterState) as float[];
#else
										var realSizes = realSizesField.GetValue(splitterState) as int[];
#endif
										if (realSizes != null)
										{
											var x = e.mousePosition.x - realSizes[0];
											if (x >= 0)
												return;
										}
									}
								}
							}
						}
					}
				}
			}
#endif

			GoToSelectedMethod(EditorWindow.focusedWindow);
		}
	
		private static void GoToMethodPath(string path)
		{
			var assemblyNameStart = path.LastIndexOf('/') + 1;
			var assemblyNameEnd = path.IndexOf(".dll!", assemblyNameStart, System.StringComparison.InvariantCultureIgnoreCase);
			if (assemblyNameEnd > 0)
			{
				var asmName = path.Substring(assemblyNameStart, assemblyNameEnd - assemblyNameStart).ToLower();
				var asmDef = AssemblyDefinition.FromName(asmName);
				if (asmDef != null && asmDef.isScriptAssembly)
				{
					var nsPathEnd = path.LastIndexOf(':') - 1;
					var nsPath = path.Substring(assemblyNameEnd + 5, nsPathEnd - 5 - assemblyNameEnd);
								
					var nsDef = asmDef.FindNamespace(nsPath.Replace("::", "."));
					if (nsDef != null)
					{
						var symbolPath = path.Substring(nsPathEnd + 1).Split(new char[]{':', '.', '(', ')'}, System.StringSplitOptions.RemoveEmptyEntries);
								
						SymbolDefinition symbol = nsDef;
						for (int i = 0; i < symbolPath.Length; ++i)
						{
							var part = symbolPath[i];
							var numTypeArgs = 0;

							var indexOfBackQuote = part.IndexOf('`');
							if (indexOfBackQuote > 0)
							{
								numTypeArgs = int.Parse(part.Substring(indexOfBackQuote + 1));
								part = part.Substring(0, indexOfBackQuote);
							}

							var nextSymbol = symbol.FindName(part, numTypeArgs, false);
							if (nextSymbol == null && i == 0)
							{
								var nestedType = asmDef.assembly.DefinedTypes.Where(x => x.IsNested && x.Name == symbolPath[0]).FirstOrDefault();
								if (nestedType != null)
								{
									var typeRef = TypeReference.To(nestedType);
									nextSymbol = typeRef.definition;
								}
							}
										
							if (nextSymbol == null)
							{
								if (part == "ctor")
								{
									nextSymbol = symbol.FindName(".ctor", 0, false);
								}
								else if (part == "cctor")
								{
									nextSymbol = symbol.FindName(".cctor", 0, false);
								}
							}
											
							if (nextSymbol == null)
							{
								var isGet = part.FastStartsWith("get_");
								var isSet = !isGet && part.FastStartsWith("set_");
								var isAdd = !isSet && part.FastStartsWith("add_");
								var isRemove = !isAdd && part.FastStartsWith("remove_");
								if (isGet || isSet || isAdd)
									nextSymbol = symbol.FindName(part.Substring(4), -1, false);
								else if (isRemove)
									nextSymbol = symbol.FindName(part.Substring("remove_".Length), -1, false);
								if (nextSymbol != null)
								{
									symbol = nextSymbol;
									if (isGet)
										nextSymbol = symbol.FindName("get", -1, false);
									else if (isSet)
										nextSymbol = symbol.FindName("set", -1, false);
									else if (isAdd)
										nextSymbol = symbol.FindName("add", -1, false);
									else if (isRemove)
										nextSymbol = symbol.FindName("remove", -1, false);
								}
							}
										
							if (nextSymbol == null)
								break;
							symbol = nextSymbol;
						}
								
						GoToSymbol(symbol);
					}
				}
			}
		}
	
		public static void GoToSymbol(SymbolDefinition symbol)
		{
			if (symbol.kind == SymbolKind.MethodGroup)
			{
				var methodGroup = symbol as MethodGroupDefinition;
				if (methodGroup != null)
				{
					symbol = methodGroup.methods[0];
				}
			}
		
			var declarations = symbol.declarations;
			if (declarations == null)
				declarations = FGFindInFiles.FindDeclarations(symbol);
			if (declarations == null)
				return;
		
			symbol = symbol.Rebind();
		
			SymbolDeclaration next;
			for (var declaration = symbol.declarations; declaration != null; declaration = next)
			{
				next = declaration.next;
			
				if (declaration.IsValid())
				{
					string assetGuid;
					TextSpan textSpan;
				
					FGTextEditor.GoToSymbolDeclaration(declaration, out assetGuid, out textSpan);
					break;
				}
			}
		}

		static void GoToSelectedMethod(EditorWindow pw)
		{
#if !UNITY_2021_1_OR_NEWER
			if (currentAreaField != null)
			{
				var currentArea = currentAreaField.GetValue(pw);
				var underlyingValue = System.Convert.ChangeType(currentArea, typeof(int)) as int?;
				if (underlyingValue != 0)
					return;
			}
			//Debug.Log("CPU");
#else
			// Get the CPU Usage Profiler module's selection controller interface to interact with the selection
			var profilerWindow = pw as ProfilerWindow;
#if UNITY_2021_2_OR_NEWER
			if (profilerWindow.selectedModuleIdentifier != ProfilerWindow.cpuModuleIdentifier)
#else
			if (profilerWindow.selectedModuleName != ProfilerWindow.cpuModuleName)
#endif
				return;
#endif

#if !UNITY_2021_1_OR_NEWER
			if (UnityEditorInternal.ProfilerDriver.lastFrameIndex < 0)
				return;
		
			var path = UnityEditorInternal.ProfilerDriver.selectedPropertyPath;
			GoToMethodPath(path);
#elif UNITY_2020_1_OR_NEWER
			// If the current selection object is null, there is no selection to print out.
			if (profilerWindow.lastAvailableFrameIndex < 0)
				return;
	
#if UNITY_2021_2_OR_NEWER
			var cpuSampleSelectionController = profilerWindow.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleIdentifier);
#else
			var cpuSampleSelectionController = profilerWindow.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleName);
#endif
			var selection = cpuSampleSelectionController.selection;
			if (selection == null)
				return;
	
			var markerNamePath = selection.markerNamePath;
			//Debug.Log(string.Join(" -> ", markerNamePath));
					
			using (var frameData = ProfilerDriver.GetRawFrameDataView((int)selection.frameIndex, cpuSampleSelectionController.focusedThreadIndex))
			{
				if (!frameData.valid)
					return;
				
				var path = markerNamePath[markerNamePath.Count - 1];
				GoToMethodPath(path);
			}
#endif
		}
	}

}
