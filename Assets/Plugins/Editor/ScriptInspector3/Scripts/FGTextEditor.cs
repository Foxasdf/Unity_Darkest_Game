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
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using Themes;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

static class HotReloadIntegration
{
	static Type editorIndicationStateType = Type.GetType("SingularityGroup.HotReload.Editor.EditorIndicationState,SingularityGroup.HotReload.Editor");
	static MethodInfo getIndicationStatusCoreMethod = editorIndicationStateType != null ? editorIndicationStateType.GetMethod("GetIndicationStatusCore", BindingFlags.NonPublic | BindingFlags.Static) : null;
	static PropertyInfo indicationStatusTextProperty = editorIndicationStateType != null ? editorIndicationStateType.GetProperty("IndicationStatusText", BindingFlags.NonPublic | BindingFlags.Static) : null;
	static PropertyInfo indicationIconPathProperty = editorIndicationStateType != null ? editorIndicationStateType.GetProperty("IndicationIconPath", BindingFlags.NonPublic | BindingFlags.Static) : null;

	static Dictionary<string, string> statusTexts = new Dictionary<string, string>();
	static Dictionary<int, string> enumNames = new Dictionary<int, string>();
	static Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();
	
	static bool active;
	public static bool Active {
		get { return active; }
	}

	static Texture2D icon;
	public static Texture2D Icon {
		get { return icon; }
	}
	
	static string text;
	public static string Text {
		get { return text; }
	}
	
	static HotReloadIntegration()
	{
		if (getIndicationStatusCoreMethod == null || indicationIconPathProperty == null)
			return;
		
		EditorApplication.update += OnUpdate;
	}

	static void OnUpdate()
	{
		var currentIcon = GetIcon();
		var currentText = GetStatusText();
		
		if (currentIcon == icon && currentText == text)
			return;
		
		icon = currentIcon;
		text = currentText;
		
		active = icon != null && icon.name != "grey";
		
		FGTextEditor.RepaintAllInstances();
	}
	
	static string GetStatusText()
	{
		if (indicationStatusTextProperty == null)
			return GetStatus();
		
		try
		{
			var text = (indicationStatusTextProperty.GetValue(null) ?? GetStatus() ?? "").ToString();
			
			string result;
			if (statusTexts.TryGetValue(text, out result))
				return result;
			
			result = "Hot Reload:\n" + text;
			statusTexts[text] = result;
			
			return result;
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
			return null;
		}
	}

	static string GetStatus()
	{
		if (getIndicationStatusCoreMethod == null)
			return null;
		
		try
		{
			var status = getIndicationStatusCoreMethod.Invoke(null, null);
			if (!(status is System.Enum))
				return null;
			
			var intStatus = (int) status;
			
			var result = "";
			if (enumNames.TryGetValue(intStatus, out result))
			{
				return result;
			}
			
			result = status.ToString();
			enumNames[intStatus] = result;
			
			return result;
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
			return null;
		}
	}
	
	static Texture2D GetIcon()
	{
		if (indicationIconPathProperty == null)
			return null;
		
		var status = GetStatus();
		
		Texture2D icon = null;
		if (icons.TryGetValue(status, out icon))
		{
			return icon;
		}
		
		var iconPath = indicationIconPathProperty.GetValue(null) as string;
		if (iconPath == null)
			return null;
		
		icon = Resources.Load<Texture2D>(iconPath);
		icons[status] = icon;
		
		return icon;
	}
}

[Serializable]
public class FGTextEditor
{
//#if UNITY_EDITOR_WIN
//	[DllImport("user32.dll")]
//	public static extern IntPtr PostMessage(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);
//#endif

    internal static List<string> availableThemes = new List<string>() { "Darcula", "Xcode" }; // Load Defaults
	internal static List<Theme> themes = new List<Theme>() { Darcula._colourTheme, Xcode._colourTheme }; // Load Defaults
	private static Theme currentThemeCode = themes[0];
	private static Theme currentThemeText = themes[0];
	
	public Theme CurrentTheme {
		get {
			return textBuffer != null && textBuffer.isText ? currentThemeText : currentThemeCode;
		}
	}

	public static void AddTheme(Theme t, string name)
	{
        if (availableThemes.Contains(name))
            return;

		themes.Add(t);
		availableThemes.Add(name);
	}
	
	internal static readonly string[] availableFonts = {
		"Fonts/DejaVu Sans Mono.ttf",
		"Fonts/DejaVu Sans Mono Bold.ttf",
		"Fonts/Inconsolata.otf",
		"Fonts/JetBrains Mono.ttf",
		"Fonts/Monaco.ttf",
		"Fonts/SourceCodePro-Regular.otf",
		"Fonts/SourceCodePro-Semibold.otf",
	};
	private static string currentFont = null;
	private static bool resetCodeFont = true;
	private static bool resetTextFont = true;
	
	[NonSerialized]
	private float marginLeft = 0f;
	[NonSerialized]
	private float marginRight = 1f;
	
	[NonSerialized]
	private bool isTextAsset = true;
	[NonSerialized]
	private bool isShader = false;
	
	public enum StyleID : byte
	{
		NotSet,
		
		Normal,
		Hyperlink,
		MailTo,
	
		Keyword,
		ControlKeyword,
		Constant,
		String,
		BuiltInLiteral,
		Operator,
		Punctuator,
			
		ReferenceType,
		ValueType,
		InterfaceType,
		EnumType,
		DelegateType,
		BuiltInRefType,
		BuiltInValueType,
			
		Namespace,
		Method,
		Field,
		Property,
		Event,
			
		Parameter,
		Variable,
		TypeParameter,
		EnumMember,
	
		Preprocessor,
		DefineSymbol,
		InactiveCode,
		FoldedText,
		Comment,
		XmlDocs,
		XmlDocsTag,
	}

	public class Styles
	{
		public Color scrollViewColor;
		public GUIStyle normalStyle;
		public GUIStyle hyperlinkStyle;
		public GUIStyle mailtoStyle;

		public GUIStyle keywordStyle;
		public GUIStyle controlKeywordStyle;
		public GUIStyle constantStyle;
		public GUIStyle stringStyle;
		public GUIStyle builtInLiteralsStyle;
		public GUIStyle operatorStyle;
		public GUIStyle punctuatorStyle;
		
		public GUIStyle referenceTypeStyle;
		public GUIStyle valueTypeStyle;
		public GUIStyle interfaceTypeStyle;
		public GUIStyle enumTypeStyle;
		public GUIStyle delegateTypeStyle;
		public GUIStyle builtInRefTypeStyle;
		public GUIStyle builtInValueTypeStyle;
		
		public GUIStyle namespaceStyle;
		public GUIStyle methodStyle;
		public GUIStyle fieldStyle;
		public GUIStyle propertyStyle;
		public GUIStyle eventStyle;
		
		public GUIStyle parameterStyle;
		public GUIStyle variableStyle;
		public GUIStyle typeParameterStyle;
		public GUIStyle enumMemberStyle;

		public GUIStyle preprocessorStyle;
		public GUIStyle defineSymbols;
		public GUIStyle inactiveCodeStyle;
		public GUIStyle foldedTextStyle;
		public GUIStyle commentStyle;
		public GUIStyle xmlDocsStyle;
		public GUIStyle xmlDocsTagsStyle;
		
		public GUIStyle lineNumbersStyle;
		public GUIStyle currentLineNumberStyle;
		public Color lineNumbersBackground;
		public Color lineNumbersSeparator;
		public Color foldingButton;

		public Color caretColor;
		public Color activeSelectionColor;
		public Color passiveSelectionColor;
		public Color searchResultColor;
		
		public Color trackChangesAfterSaveColor;
		public Color trackChangesBeforeSaveColor;
		public Color trackChangesRevertedColor;
		
		public Color currentLineColor;
		public Color currentLineInactiveColor;
		
		public Color referenceHighlightColor;
		public Color referenceModifyHighlightColor;
		
		public Color tooltipBgColor;
		public Color tooltipFrameColor;
		public GUIStyle tooltipTextStyle;
		
		public Color listFrameColor;
		public Color listBgColor;
		
		public GUIStyle ping;
		public GUIStyle toolbarSearchField;
		public GUIStyle toolbarSearchFieldCancelButton;
		public GUIStyle toolbarSearchFieldCancelButtonEmpty;
		public GUIStyle upArrowStyle;
		public GUIStyle downArrowStyle;
		
		public GUIStyle this[StyleID styleID]
		{
			get
			{
				switch (styleID)
				{
				case StyleID.NotSet:
				case StyleID.Normal:
					return normalStyle;
				case StyleID.Hyperlink:
					return hyperlinkStyle;
				case StyleID.MailTo:
					return mailtoStyle;
				
				case StyleID.Keyword:
					return keywordStyle;
				case StyleID.ControlKeyword:
					return controlKeywordStyle;
				case StyleID.Constant:
					return constantStyle;
				case StyleID.String:
					return stringStyle;
				case StyleID.BuiltInLiteral:
					return builtInLiteralsStyle;
				case StyleID.Operator:
					return operatorStyle;
				case StyleID.Punctuator:
					return punctuatorStyle;
				
				case StyleID.ReferenceType:
					return referenceTypeStyle;
				case StyleID.ValueType:
					return valueTypeStyle;
				case StyleID.InterfaceType:
					return interfaceTypeStyle;
				case StyleID.EnumType:
					return enumTypeStyle;
				case StyleID.DelegateType:
					return delegateTypeStyle;
				case StyleID.BuiltInRefType:
					return builtInRefTypeStyle;
				case StyleID.BuiltInValueType:
					return builtInValueTypeStyle;
				
				case StyleID.Namespace:
					return namespaceStyle;
				case StyleID.Method:
					return methodStyle;
				case StyleID.Field:
					return fieldStyle;
				case StyleID.Property:
					return propertyStyle;
				case StyleID.Event:
					return eventStyle;
				
				case StyleID.Parameter:
					return parameterStyle;
				case StyleID.Variable:
					return variableStyle;
				case StyleID.TypeParameter:
					return typeParameterStyle;
				case StyleID.EnumMember:
					return enumMemberStyle;
				
				case StyleID.Preprocessor:
					return preprocessorStyle;
				case StyleID.DefineSymbol:
					return defineSymbols;
				case StyleID.InactiveCode:
					return inactiveCodeStyle;
				case StyleID.FoldedText:
					return foldedTextStyle;
				case StyleID.Comment:
					return commentStyle;
				case StyleID.XmlDocs:
					return xmlDocsStyle;
				case StyleID.XmlDocsTag:
					return xmlDocsTagsStyle;
				
				default:
					return null;
				}
			}
		}
	}
	
	static Styles stylesCode = new Styles();
	static Styles stylesText = new Styles();
	[NonSerialized]
	public Styles styles = stylesCode;
	
	[NonSerialized]
	private GUIStyle orangeBoldLabel;
	[NonSerialized]
	private GUIStyle normalLabel;

	[SerializeField, HideInInspector]
	private Vector2 scrollPosition;
	[SerializeField, HideInInspector]
	private int scrollPositionLine;
	[SerializeField, HideInInspector]
	private float scrollPositionOffset;
	[NonSerialized]
	private Vector2 smoothScrollPosition;
	[NonSerialized]
	private Vector2 currentScrollVelocity;
	[NonSerialized]
	private System.DateTime lastSmoothScrollTime;
	[SerializeField, HideInInspector]
	private bool scrollPositionInitialized;
	
	[NonSerialized]
	private Rect scrollViewRect;
	[NonSerialized]
	private Rect contentRect;
	[NonSerialized]
	private float widestLine = 1f;
	[NonSerialized]
	private bool needsRepaint;
	[NonSerialized]
	private bool needsReformat;
	[NonSerialized]
	private int lastTabSize;
	[NonSerialized]
	private int lastLineSpacing;

	public float LineHeight
	{
		get { return charSize.y + SISettings.lineSpacing.Value; }
	}
	
	public bool hasCodeViewFocus { get; private set; }
	
	[NonSerialized]
	private EditorWindow parentWindow;
	[NonSerialized]
	public Vector2 charSize;
	[NonSerialized]
	public float pointSize;
	//private static float[] charWidths = new float[65536];
	//private Dictionary<string, float> tokenWidths = new Dictionary<string, float>();
	
	private bool wordWrapping;
	private bool WordWrapping {
		get {
			if (!object.ReferenceEquals(parentWindow, null))
				return isTextAsset ? SISettings.wordWrapText : SISettings.wordWrapCode;
			return isTextAsset ? SISettings.wordWrapTextInspector : SISettings.wordWrapCodeInspector;
		}
	}

	private bool trackChanges {
		get {
			if (!object.ReferenceEquals(parentWindow, null))
				return isTextAsset ? SISettings.trackChangesText : SISettings.trackChangesCode;
			return isTextAsset ? SISettings.trackChangesTextInspector : SISettings.trackChangesCodeInspector;
		}
	}

	[NonSerialized]
	private bool enableCodeFolding;
	private bool CacheEnableCodeFolding()
	{
		if (parentWindow != null)
			return isTextAsset ? false : SISettings.codeFoldingCode;
		else
			return isTextAsset ? false : SISettings.codeFoldingCodeInspector;
	}

	private static readonly int buttonHash = "Button".GetHashCode();
	private static Texture2D wrenchIcon;
	private static Texture2D newIcon;
	private static Texture2D wavyUnderline;
	private static Texture2D saveIcon;
	private static Texture2D buildIcon;
	private static Texture2D undoIcon;
	private static Texture2D redoIcon;
#if !UNITY_4_0 && !UNITY_4_1
	private static Texture2D versionControlIcon;
#endif
	//private static Texture2D hyperlinksIcon;
	private static Texture2D popOutIcon;

	[NonSerialized]
	public bool hasSearchBoxFocus;
	[NonSerialized]
	private bool focusSearchBox = false;
	[NonSerialized]
	private bool focusCodeView = true;
	[NonSerialized]
	private bool focusCodeViewOnEscapeUp = false;
	private static string defaultSearchString = string.Empty;
	[SerializeField]
	private string searchString = "";
	[NonSerialized]
	private List<TextPosition> searchResults = new List<TextPosition>();
	[NonSerialized]
	private int currentSearchResult = -1;
	[NonSerialized]
	private int searchResultAge = 0;
	[NonSerialized]
	private bool atLastSearchResult = false;
	[NonSerialized]
	private bool highlightSearchResults;
	[NonSerialized]
	private float pingTimer = 0f;
	[NonSerialized]
	private System.DateTime pingStartTime;
	[NonSerialized]
	private GUIContent pingContent = new GUIContent();
	[NonSerialized]
	private Color pingColor = yellowPingColor;
	public static readonly Color yellowPingColor = new Color32(243, 228, 61, 255);
	public static readonly Color redPingColor = new Color32(243, 124, 119, 255);
	[NonSerialized]
	private Rect scrollToRect;
	[NonSerialized]
	public bool scrollToCaret = false;
	[NonSerialized]
	private SymbolDefinition highlightedSymbol = null;
	[NonSerialized]
	private string highlightedPPSymbol = null;
	[NonSerialized]
	private System.DateTime highightReferencesTime;
	[NonSerialized]
	private TextPosition matchingBraceLeft;
	[NonSerialized]
	private TextPosition matchingBraceRight;
	[NonSerialized]
	private int matchedBracesAtUndoPosition = -1;
	[NonSerialized]
	private FGTextBuffer.CaretPos matchedBracesAtCaretPosition;
	
	private static string[] lineNumberCachedStrings = new string[0];

	// Editor

	public static bool nextF12GoesBack;
	
	[NonSerialized]
	private bool isCaretOn = true;
	[NonSerialized]
	public System.DateTime caretMoveTime;
	[SerializeField, HideInInspector]
	public FGTextBuffer.CaretPos caretPosition = new FGTextBuffer.CaretPos();
	[SerializeField, HideInInspector]
	private FGTextBuffer.CaretPos _selectionStartPosition = null;
	[SerializeField, HideInInspector]
	private bool hasSelection = false;

	public FGTextBuffer.CaretPos selectionStartPosition
	{
		get { return hasSelection ? _selectionStartPosition : null; }
		set
		{
			if (value == null)
			{
				_selectionStartPosition = null;
				hasSelection = false;
			}
			else
			{
				_selectionStartPosition = value;
				hasSelection = true;
			}
		}
	}

	[NonSerialized]
	private bool codeViewDragging = false;
	[NonSerialized]
	private bool mouseDownOnSelection = false;
	[NonSerialized]
	private FGTextBuffer.CaretPos mouseDropPosition = new FGTextBuffer.CaretPos();
	[NonSerialized]
	private Vector2 autoScrolling = Vector2.zero;
	[NonSerialized]
	private Vector2 autoScrollDelta = Vector2.zero;
	[NonSerialized]
	private System.DateTime lastAutoScrollTime;

	[NonSerialized]
	private bool autoScrollLeft = false;
	[NonSerialized]
	private bool autoScrollRight = false;
	[NonSerialized]
	private bool autoScrollUp = false;
	[NonSerialized]
	private bool autoScrollDown = false;

	[NonSerialized]
	private FGTextBuffer textBuffer = null;
	public FGTextBuffer TextBuffer { get { return textBuffer; } }
	public bool IsModified { get { return textBuffer != null && textBuffer.IsModified; } }
	public bool IsLoading { get { return textBuffer == null || textBuffer.IsLoading; } }
	public bool CanEdit() { return !IsLoading /*&& !EditorApplication.isCompiling /*&& !textBuffer.justSavedNow*/; }

	public string targetGuid { get { return textBuffer != null ? textBuffer.guid : string.Empty; } }
	public string targetPath { get { return textBuffer != null ? textBuffer.assetPath : string.Empty; } }
	
	enum TryEditState { Ask, Yes, No };
	[NonSerialized] TryEditState tryEditState;
	private bool TryEdit()
	{
		if (tryEditState == TryEditState.Ask)
		{
			var editState = textBuffer.TryEdit();
			tryEditState = editState ? TryEditState.Yes : TryEditState.No;
			return editState;
		}
		return tryEditState == TryEditState.Yes;
	}

	[SerializeField]
	public int lineNumbersOffset = 0;
	
	public class EditorLine
	{
		public int lineFrom;
		public int lineTo;
		
		public List<Rect> selectionRects;
		
		public List<SyntaxToken> tokens;
		public List<SyntaxToken> tempTokens;
		
		public List<SyntaxToken> lineTokens;
		public List<string> lineTexts;
		public List<Rect> lineRects;
		public List<TextPosition> lineTokenStartPos;
		public List<FGTextBuffer.CollapsibleTextSpan> collapsedTokenSpans;

		static Stack<EditorLine> pool = new Stack<EditorLine>();

		static public EditorLine Create(int numTokens)
		{
			EditorLine editorLine;
			if (pool.Count > 0)
			{
				editorLine = pool.Pop();
				
				editorLine.Clear();

				if (editorLine.tempTokens.Capacity < numTokens)
				{
					editorLine.tempTokens.Capacity = numTokens;
					editorLine.lineTokens.Capacity = numTokens;
					editorLine.lineTexts.Capacity = numTokens;
					editorLine.lineRects.Capacity = numTokens;
					editorLine.lineTokenStartPos.Capacity = numTokens + 1;
					editorLine.collapsedTokenSpans.Capacity = numTokens;
				}
			}
			else
			{
				editorLine = new EditorLine();

				editorLine.selectionRects = new List<Rect>();
				editorLine.tempTokens = new List<SyntaxToken>(numTokens);
				editorLine.lineTokens = new List<SyntaxToken>(numTokens);
				editorLine.lineTexts = new List<string>(numTokens);
				editorLine.lineRects = new List<Rect>(numTokens);
				editorLine.lineTokenStartPos = new List<TextPosition>(numTokens + 1);
				editorLine.collapsedTokenSpans = new List<FGTextBuffer.CollapsibleTextSpan>(numTokens);
				
				editorLine.tokens = editorLine.tempTokens;
			}
			
			return editorLine;
		}
	
		static public void Release(EditorLine editorLine)
		{
			pool.Push(editorLine);
		}
		
		public void Clear()
		{
			selectionRects.Clear();
			tempTokens.Clear();
			lineTokens.Clear();
			lineTexts.Clear();
			lineRects.Clear();
			lineTokenStartPos.Clear();
			collapsedTokenSpans.Clear();
			
			tokens = tempTokens;
		}
		
		public void AddSelectionRect(Rect rect)
		{
			var numRects = selectionRects.Count;
			if (numRects == 0)
			{
				selectionRects.Add(rect);
				return;
			}
			
			var lastRect = selectionRects[numRects - 1];
			if (lastRect.y == rect.y && Mathf.Approximately(lastRect.xMax, rect.x))
			{
				lastRect.width += rect.width;
				selectionRects[numRects - 1] = lastRect;
				return;
			}
			
			selectionRects.Add(rect);
		}
		
		public int FindTokenIndexAfter(TextPosition charPos)
		{
			var count = lineTokenStartPos.Count;
		
			var index = 1;
			while (index < count)
			{
				if (lineTokenStartPos[index] > charPos)
					break;
				++index;
			}
		
			return index - 1;
		}
	}
	
	List<EditorLine> linesInView = new List<EditorLine>();

	[NonSerialized]
	private List<int> yLineOffsets;
	public float GetLineOffset(int index, bool checkIfCollapsed = true)
	{
		if (index == 0)
			return 0f;
		
		//if (!wordWrapping && !enableCodeFolding/*|| !CanEdit()*/)
		//	return LineHeight * index;
		
		var numTextBufferLines = textBuffer.lines.Count;
		
		if (checkIfCollapsed && enableCodeFolding)
			index = BufferToEditorLine(index);
		
		if (index == 0)
			return 0f;
		
		if (index > numTextBufferLines)
			index = numTextBufferLines;
		
		if (yLineOffsets == null)
		{
			yLineOffsets = new List<int>(numTextBufferLines + 1);
		}
		if (yLineOffsets.Count == 0)
		{
			yLineOffsets.Add(0);
		}
		
		var diff = yLineOffsets.Count - (numTextBufferLines + 1);
		if (diff > 0)
		{
			yLineOffsets.RemoveRange(numTextBufferLines + 1, diff);
		}
		
		if (index >= yLineOffsets.Count)
		{
			var last = yLineOffsets.Count - 1;
			var y = yLineOffsets[last];

			var nextSpanIndex = -1;
			FGTextBuffer.CollapsibleTextSpan nextSpan = null;
			int nextFoldedLine = -1;
			
			var spans = textBuffer.collapsibleTextSpans;
			var numSpans = spans != null ? spans.Count : 0;
			
			if (numSpans > 0)
			{
				var line = BufferToEditorLine(last);
				nextSpanIndex = textBuffer.IndexOfFirstCollapsibleTextSpanAtLineOrAfter(line);
				if (nextSpanIndex < 0)
					nextSpanIndex = numSpans;
				
				while (nextSpanIndex < numSpans && !spans[nextSpanIndex].IsCollapsed)
					++nextSpanIndex;
				
				if (nextSpanIndex < numSpans)
				{
					nextSpan = spans[nextSpanIndex];
					nextFoldedLine = nextSpan.span.Start.line;
				}
			}

			if (wordWrapping)
			{
				var numSoftLineBreaks = _softLineBreaks != null ? _softLineBreaks.Count : 0;
				var softLineBreaksCount = last < numSoftLineBreaks ? (_softLineBreaks[last] ?? NO_SOFT_LINE_BREAKS).Count : 0;
				
				while (index > last)
				{
					while (last == nextFoldedLine)
					{
						var toLine = nextSpan.span.End.line;
						while (last <= toLine)
						{
							yLineOffsets.Add(y);
							++last;
						}
						
						nextFoldedLine = -1;
						while (++nextSpanIndex < numSpans)
						{
							nextSpan = spans[nextSpanIndex];
							if (nextSpan.span.Start.line < last)
							{
								continue;
							}
							
							if (nextSpan.IsCollapsed)
							{
								nextFoldedLine = nextSpan.span.Start.line;
								break;
							}
						}
					}
					//var overlappingSpan = GetCharCollapsedSpan(last, -1);
					//while (overlappingSpan != null)
					//{
					//	var toLine = overlappingSpan.span.End.line;
					//	while (last <= toLine)
					//	{
					//		yLineOffsets.Add(y);
					//		++last;
					//	}
					//	overlappingSpan = GetCharCollapsedSpan(last, -1);
					//}

					y += softLineBreaksCount + 1;
					yLineOffsets.Add(y);
					++last;
					
					var softLineBreaks = last < numSoftLineBreaks ? _softLineBreaks[last] : null;
					softLineBreaksCount = softLineBreaks != null ? softLineBreaks.Count : 0;
				}
			}
			else
			{
				while (index > last)
				{
					while (last == nextFoldedLine)
					{
						var toLine = nextSpan.span.End.line;
						while (last <= toLine)
						{
							yLineOffsets.Add(y);
							++last;
						}
						
						nextFoldedLine = -1;
						while (++nextSpanIndex < numSpans)
						{
							nextSpan = spans[nextSpanIndex];
							if (nextSpan.span.Start.line < last)
							{
								continue;
							}
							
							if (nextSpan.IsCollapsed)
							{
								nextFoldedLine = nextSpan.span.Start.line;
								break;
							}
						}
					}
					//var overlappingSpan = GetCharCollapsedSpan(last, -1);
					//while (overlappingSpan != null)
					//{
					//	var toLine = overlappingSpan.span.End.line;
					//	while (last <= toLine)
					//	{
					//		yLineOffsets.Add(y);
					//		++last;
					//	}
					//	overlappingSpan = GetCharCollapsedSpan(last, -1);
					//}

					++y;
					yLineOffsets.Add(y);
					++last;
				}
			}
		}

		float lineHeight = charSize.y + lastLineSpacing;
		return lineHeight * yLineOffsets[index];
	}

	public int GetEditorLineAt(float yOffset, out int softLine)
	{
		var line = GetLineAt(yOffset);
		line = BufferToEditorLine(line);
		
		var softLineBreaks = GetSoftLineBreaks(line);
		
		yOffset -= GetLineOffset(line);
		softLine = (int)(yOffset / (charSize.y + lastLineSpacing));
		softLine = Mathf.Clamp(softLine, 0, softLineBreaks.Count);
		
		return line;
	}

	public int GetLineAt(float yOffset)
	{
		//if (!wordWrapping /*|| !CanEdit()*/ || textBuffer.lines.Count <= 1)
		//	return Mathf.Min((int) (yOffset / LineHeight), textBuffer.lines.Count - 1);
		
		if (textBuffer.lines.Count <= 1)
			return 0;

		var height = GetLineOffset(textBuffer.lines.Count);
		if (height == 0f || yLineOffsets == null)
			return 0;
		
		var y = (int)(yOffset / LineHeight);
		if (y <= 0)
			return 0;

		int line = FindFirstIndexGreaterThanOrEqualTo(yLineOffsets, y);
		if (line >= yLineOffsets.Count)
		{
			line = yLineOffsets.Count - 1;
		}
		else if (yLineOffsets[line] > y)
        {
			--line;
        }

		return line;
	}
	
	public void FocusCodeView()
	{
		caretMoveTime = default(System.DateTime);
		focusCodeView = true;
		Repaint();
	}
	
	private static void InitializeFont(bool forText)
	{
		if (string.IsNullOrEmpty(currentFont))
		{
			currentFont = SISettings.editorFont;
			if (currentFont == null)
				currentFont = Array.Find(availableFonts, x => x.Contains("SourceCodePro"));
			if (currentFont == null)
				currentFont = availableFonts[0];
			if (currentFont == "VeraMono")
				currentFont = availableFonts[0];
		}
	}

	public void OnEnable(UnityEngine.Object targetFile, EditorWindow owner, bool fromInspector)
	{
		if (fromInspector)
		{
			currentInspector = owner;
		
			//Debug.Log("FGTextEditor.OnEnable inspector target: " + targetFile.name);
		}
		else if (owner != currentInspector)
		{
			parentWindow = owner;
			
			//Debug.Log("FGTextEditor.OnEnable window target: " + targetFile.name);
		}
		
		lastTabSize = SISettings.tabSize;
		lastLineSpacing = SISettings.lineSpacing;

		if (selectionStartPosition != null && selectionStartPosition.line == -1)
			selectionStartPosition = null;

		isShader = targetFile is Shader;
		isTextAsset = !(targetFile is MonoScript) && !isShader;
		var targetAsTextBuffer = targetFile as FGTextBuffer;
		if (targetAsTextBuffer != null)
		{
			isTextAsset = targetAsTextBuffer.isText;
			if (textBuffer == null)
				textBuffer = targetAsTextBuffer;
		}
		
		if (textBuffer == null)
		{
			try
			{
				//Debug.Log("  FGTextEditor.OnEnable will get textBuffer for <b>" + targetFile.name + "</b>");
				textBuffer = FGTextBuffer.GetBuffer(targetFile);
#if SI3_WARNINGS
				if (textBuffer == null)
					Debug.LogWarning("    textBuffer not found!!!");
#endif
			}
			catch (Exception e)
			{
				Debug.LogError("Exception while trying to get buffer!!!\n" + e);
				return;
			}
		}
		
		InitializeFont(isTextAsset);

		styles = isTextAsset ? stylesText : stylesCode;
		textBuffer.styles = styles;
		Initialize();
		textBuffer.Initialize();

		//caretMoveTime = frameTime;

		textBuffer.onChange -= Repaint;
		textBuffer.onChange += Repaint;

		textBuffer.onLineFormatted -= OnLineFormatted;
		textBuffer.onLineFormatted += OnLineFormatted;

		textBuffer.onInsertedLines -= OnInsertedLines;
		textBuffer.onInsertedLines += OnInsertedLines;

		textBuffer.onRemovedLines -= OnRemovedLines;
		textBuffer.onRemovedLines += OnRemovedLines;

		textBuffer.AddEditor(this);
		
		EditorApplication.update -= OnUpdate;
		EditorApplication.update += OnUpdate;
		
		EditorApplication.update -= SearchOnLoaded;
		EditorApplication.update += SearchOnLoaded;

		if (!fromInspector)
			textBuffer.LoadImmediately();
	}
	
	private void SearchOnLoaded()
	{
		if (CanEdit())
		{
			EditorApplication.update -= SearchOnLoaded;
			
			var s = defaultSearchString;
			SetSearchText(searchString);
			defaultSearchString = s;
		}
	}
	
	private void InvalidateSoftLineBreaks()
	{
		if (_softLineBreaks != null)
			for (int i = 0; i < _softLineBreaks.Count; ++i)
				_softLineBreaks[i] = null;
		if (yLineOffsets != null)
			yLineOffsets.Clear();
	}

	private void InvalidateSoftLineBreaks(int fromLine, int toLine)
	{
		if (_softLineBreaks != null)
		{
			if (_softLineBreaks.Count - 1 < toLine)
				toLine = _softLineBreaks.Count - 1;
			for (int i = fromLine; i <= toLine; ++i)
				_softLineBreaks[i] = null;
		}
		
		++fromLine;
		if (yLineOffsets != null && fromLine < yLineOffsets.Count)
			yLineOffsets.RemoveRange(fromLine, yLineOffsets.Count - fromLine);
	}

	private void OnLineFormatted(int line)
	{
		if (_softLineBreaks != null && line < _softLineBreaks.Count)
			_softLineBreaks[line] = null;
		
		++line;
		if (yLineOffsets == null)
			return;
		
		if (line < yLineOffsets.Count)
			yLineOffsets.RemoveRange(line, yLineOffsets.Count - line);
		else if (line == textBuffer.lines.Count - 1 && EditorWindow.focusedWindow != OwnerWindow)
		{
			//Debug.Log("Repaint");
			Repaint();
		}
	}

	private void OnInsertedLines(int lineIndex, int numLines)
	{
		if (_softLineBreaks != null && lineIndex <= _softLineBreaks.Count)
		{
			_softLineBreaks.InsertRange(lineIndex, new List<int>[numLines]);
		}
		if (yLineOffsets != null && lineIndex + 1 < yLineOffsets.Count)
		{
			yLineOffsets.RemoveRange(lineIndex + 1, yLineOffsets.Count - lineIndex - 1);
		}
		if (lineIndex < scrollPositionLine)
		{
			scrollPositionOffset = 0f;
			scrollPositionLine += numLines;
		}
	}

	private void OnRemovedLines(int lineIndex, int numLines)
	{
		if (_softLineBreaks != null && lineIndex < _softLineBreaks.Count)
		{
			_softLineBreaks.RemoveRange(lineIndex, Math.Min(numLines, _softLineBreaks.Count - lineIndex));
		}
		if (yLineOffsets != null && lineIndex + 1 < yLineOffsets.Count)
		{
			yLineOffsets.RemoveRange(lineIndex + 1, yLineOffsets.Count - lineIndex - 1);
		}
		if (lineIndex < scrollPositionLine)
		{
			scrollPositionOffset = 0f;
			if (lineIndex + numLines <= scrollPositionLine)
				scrollPositionLine -= numLines;
			else
				scrollPositionLine = lineIndex;
		}
	}
	
	private void SaveBuffer()
	{
		CloseAllPopups();
			
		if (CanEdit())
		{
			if (IsModified)
			{
				if (!textBuffer.Save())
					return;
				
				if (isTextAsset || textBuffer.isShader)
				{
					AssetDatabase.ImportAsset(targetPath);
				}
				else
				{
					FGTextBufferManager.AddPendingAssetImport(textBuffer.guid);

					if (SISettings.compileOnSave && !HotReloadIntegration.Active)
					{
						//if (!SISettings.autoReloadAssemblies)
						//{
						//	//Debug.Log("... Will compile in background");
						//	holdingAssemblies = 1;
						//	compilers.Clear();
						//	EditorApplication.update += HoldReloadingAssemblies;
						//}
						//else
						{
#if UNITY_2019_2_OR_NEWER
							UnlockReloadAssemblies();
#endif
						}
						FGTextBufferManager.ImportPendingAssets();
						if (OwnerWindow)
							OwnerWindow.Focus();
					}
				}
			}
			else if (SISettings.compileOnDoubleSave)
			{
				MenuReloadAssemblies();
				return;
			}
						
			RepaintAllInstances();
		}
	}
	
	static private void UnlockReloadAssemblies()
	{
		EditorApplication.UnlockReloadAssemblies();
		
//#if UNITY_EDITOR_WIN && UNITY_2019_2_OR_NEWER && !UNITY_2019_4_OR_NEWER
//		// HACK: Workaround for Unity Editor not unlocking reloading assemblies after dragging a floating tab
//		EditorApplication.update += UnlockReloadAssembliesOnUpdate;
//#endif
	}

//#if UNITY_EDITOR_WIN && UNITY_2019_2_OR_NEWER
//	static MethodInfo canReloadAssembliesMethod;
//	static double delayUnlocking;
//	static void UnlockReloadAssembliesOnUpdate()
//	{
//		//if (EditorApplication.isPlaying)
//		{
//			var scriptCompilationDuringPlay = EditorPrefs.GetInt("ScriptCompilationDuringPlay", 0);
//			//if (scriptCompilationDuringPlay == 1)
//			{
//				EditorApplication.update -= UnlockReloadAssembliesOnUpdate;
//				return;
//			}
//		}
//		for (int i = 0; i < 10; ++i)
//			EditorApplication.UnlockReloadAssemblies();
		
//		if (canReloadAssembliesMethod == null)
//		{
//			canReloadAssembliesMethod = typeof(EditorApplication).GetMethod("CanReloadAssemblies", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
//		}
//		if (canReloadAssembliesMethod == null)
//		{
//			//Debug.Log("canReloadAssembliesMethod is null");
//			EditorApplication.update -= UnlockReloadAssembliesOnUpdate;
//		}
//		else if ((bool)canReloadAssembliesMethod.Invoke(null, null))
//		{
//			//Debug.Log("canReloadAssembliesMethod returned true");
//			EditorApplication.update -= UnlockReloadAssembliesOnUpdate;
//			return;
//		}
		
//		if (delayUnlocking > EditorApplication.timeSinceStartup)
//			return;
//		delayUnlocking = EditorApplication.timeSinceStartup + 1.0f;
//		//Debug.Log("Unlocking hack...");
//#if UNITY_2019_2
//		const uint WM_KEYDOWN = 0x0100;
//		IntPtr hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
//		PostMessage(hWnd, WM_KEYDOWN, (IntPtr)0x1B, IntPtr.Zero);
//		GenericMenu menu = new GenericMenu();
//		menu.AddSeparator("");
//		menu.DropDown(new Rect());
//#else
//		Debug.Log("CMD");
//		System.Diagnostics.Process.Start("CMD.exe", "/C exit");
//#endif
//	}
//#endif //UNITY_2019_2_OR_NEWER

#if UNITY_EDITOR_OSX
	[MenuItem("Window/Script Inspector 3/Save All and Reload Assemblies _&%r", false, 510)]
#else
	[MenuItem("Window/Script Inspector 3/Save All and Reload Assemblies _%r", false, 510)]
#endif
	static public void MenuReloadAssemblies()
	{
		FGTextBufferManager.SaveAllModified(false);
		
		if (!EditorApplication.isCompiling && !FGTextBufferManager.IsReloadingAssemblies)
		{
			//Debug.Log("early out");
#if UNITY_2019_2_OR_NEWER
			UnlockReloadAssemblies();
#endif
			return;
		}
		
		//if (!SISettings.compileOnSave)
		//	FGTextBufferManager.ImportPendingAssets();
		
		//if (holdingAssemblies > 0)
		//{
		//	//Debug.Log("Releasing hold of reload");
		//	holdingAssemblies = 0;
		//	EditorApplication.update -= HoldReloadingAssemblies;
		//	EditorApplication.update += ReloadAssemblies;
		//	UnlockReloadAssemblies();
		//}
		//else
		if (!FGTextBufferManager.IsReloadingAssemblies)
		{
			//Debug.Log("Reloading is false...");
			//if (SISettings.compileOnSave && !SISettings.autoReloadAssemblies)
			//{
			//	//Debug.Log("... Will compile in background");
			//	holdingAssemblies = 1;
			//	compilers.Clear();
			//	EditorApplication.update += HoldReloadingAssemblies;
			//}
			//else
			{
#if UNITY_2019_2_OR_NEWER
				UnlockReloadAssemblies();
#endif
			}
			if (SISettings.compileOnSave)
			{
				var focusedWindow = EditorWindow.focusedWindow;
				FGTextBufferManager.ImportPendingAssets();
				if (focusedWindow)
					focusedWindow.Focus();
			}
		}
		else
		{
			//Debug.Log("Ignoring...");
#if UNITY_2019_2_OR_NEWER
			UnlockReloadAssemblies();
#endif
		}
	}
	
//	static int holdingAssemblies;
//	static HashSet<System.Diagnostics.Process> compilers = new HashSet<System.Diagnostics.Process>();
//	static void HoldReloadingAssemblies()
//	{
//		if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
//		{
//			EditorApplication.update -= HoldReloadingAssemblies;
//			EditorApplication.update += ReloadAssemblies;
//			holdingAssemblies = 10;
//		}
//		else
//		{
//			var compiling = false;
//			var errors = false;
//			var processes = System.Diagnostics.Process.GetProcesses();
//			foreach (var p in processes)
//				try {
//					if (p.ProcessName == "mono" || p.ProcessName == "csc")
//					{
//						compilers.Add(p);
//						if (p.ExitCode != 0)
//						{
//							errors = true;
//							break;
//						}
//						else
//						{
//							compiling = true;
//						}
//					}
//				} catch {}
////				where Array.IndexOf(
////					compilerFileNames,
////					Path.GetFileNameWithoutExtension(p.StartInfo.FileName).ToLowerInvariant()
////				) >= 0
//			//Debug.Log(processes.Length + " : " + compilers.Count());
//			compilers.RemoveWhere(x => x.HasExited && x.ExitCode == 0);
//			errors = errors || compilers.Any(x => x.HasExited && x.ExitCode != 0);
//			if (errors)
//			{
//				//Debug.Log("ERRORS!");
//				EditorApplication.update -= HoldReloadingAssemblies;
//				EditorApplication.update += ReloadAssemblies;
//				return;
//			}

//			if (compiling || holdingAssemblies < 10)
//			{
//				if (!compiling)
//					++holdingAssemblies;
//				EditorApplication.LockReloadAssemblies();
//			}
//			else if (!compiling && compilers.Count == 0)
//			{
//				holdingAssemblies = 10;
//				EditorApplication.update -= HoldReloadingAssemblies;
//				RepaintAllInstances();
//			}
//		}
//	}
	
#if !UNITY_2019_2_OR_NEWER
	static bool progressBarShown;
	static EditorWindow restoreFocusToWnd;
#endif

	static void ReloadAssemblies()
	{
		//holdingAssemblies = 0;
		//compilers.Clear();
		//for (var i = 0; i < 10; ++i)
		//	EditorApplication.UnlockReloadAssemblies();

#if UNITY_2019_2_OR_NEWER
		UnlockReloadAssemblies();
#else
		if (!progressBarShown ||
			EditorApplication.isCompiling && !EditorApplication.isUpdating)
		{
			if (!progressBarShown)
			{
				restoreFocusToWnd = EditorWindow.focusedWindow;
				
				progressBarShown = true;
				EditorUtility.DisplayProgressBar("Script Inspector", "Reloading assemblies...", 0f);
				AppDomain.CurrentDomain.DomainUnload -= HideProgressBarOnUnload;
				AppDomain.CurrentDomain.DomainUnload += HideProgressBarOnUnload;
				
				return;
			}
		}
		
		progressBarShown = false;
		EditorUtility.ClearProgressBar();
		
		if (restoreFocusToWnd)
			restoreFocusToWnd.Focus();
		restoreFocusToWnd = null;
#endif

		EditorApplication.update -= ReloadAssemblies;
		RepaintAllInstances();
	}
	
#if !UNITY_2019_2_OR_NEWER
	static void HideProgressBarOnUnload(object sender, EventArgs args)
	{
		EditorUtility.ClearProgressBar();
	}
#endif

	private void CheckFocusDelayed()
	{
		EditorApplication.update -= CheckFocusDelayed;

		if (EditorWindow.focusedWindow == autocompleteWindow)
			return;
		
		if (EditorWindow.focusedWindow == argumentsHint)
		{
			CloseAllPopups();
			FocusCodeView();
			OwnerWindow.Focus();
			return;
		}

		if (EditorWindow.focusedWindow == OwnerWindow)
			return;

		Input.imeCompositionMode = IMECompositionMode.Auto;
		
		CloseAllPopups();
		OnReallyLostFocus();
	}

	private void OnReallyLostFocus()
	{
		if (addRecentLocationOnFocusLost)
			AddRecentLocation(0, true);
		addRecentLocationOnFocusLost = true;
		
	//	if (textBuffer != null && textBuffer.Parser != null)
	//		textBuffer.Parser.FullRefresh();
	}
	
	private static bool addRecentLocationOnFocusLost = true;
	
	public void OnLostFocus()
	{
		if (tokenTooltip != null || autocompleteWindow != null || argumentsHint != null)
		{
			if (tokenTooltip != null)
			{
				tokenTooltip.Hide();
				tokenTooltip = null;
			}
			
			EditorApplication.update += CheckFocusDelayed;
		}
		else
		{
			Input.imeCompositionMode = IMECompositionMode.Auto;
			
			if (CanEdit())
				OnReallyLostFocus();
		}
	}

	public void OnDisable()
	{
		if (autocompleteWindow != null)
			CloseAutocomplete();

		//if (tokenTooltip != null)
		//	tokenTooltip.Hide();
		
		//if (argumentsHint != null)
		//	CloseArgumentsHint();

		EditorApplication.update -= OnUpdate;
		EditorApplication.update -= SearchOnLoaded;

		if (textBuffer != null)
		{
			textBuffer.RemoveEditor(this);
			textBuffer.onChange -= Repaint;
			textBuffer.onLineFormatted -= OnLineFormatted;
			textBuffer.onInsertedLines -= OnInsertedLines;
			textBuffer.onRemovedLines -= OnRemovedLines;
		}
		
		if (FGTextBuffer.activeEditor == this)
		{
			AddRecentLocation(1, false);
			FGTextBuffer.activeEditor = null;
		}
	}

	public delegate void NotificationDelegate();
	public NotificationDelegate onRepaint;
	//public NotificationDelegate onChange;

	public void Repaint()
	{
		if (onRepaint != null)
			onRepaint();
	}

	//private void NotifyChange()
	//{
	//    if (onChange != null)
	//        onChange();
	//}
	
	public static System.DateTime frameTime = System.DateTime.Now;
	
	static FGTextEditor()
	{
		EditorApplication.update += UpdateTime;
	}
	
	static void UpdateTime()
	{
		frameTime = System.DateTime.Now;
	}
	
	public void OnUpdate()
	{
		if (autoScrolling != Vector2.zero || autoScrollLeft || autoScrollRight || autoScrollUp || autoScrollDown)
		{
			var deltaTime = (float) (frameTime - lastAutoScrollTime).TotalSeconds;

			if (!autoScrollLeft && !autoScrollRight)
			{
				autoScrolling.x = autoScrolling.x * 0.9f;
				if (autoScrolling.x != 0f)
					autoScrolling.x = autoScrolling.x > 0f ? Mathf.Max(0f, autoScrolling.x - 50f * deltaTime) : Mathf.Min(0f, autoScrolling.x + 50f * deltaTime);
			}
			else
			{
				autoScrolling.x = Mathf.Clamp(autoScrolling.x + (autoScrollLeft ? -500f : 500f) * deltaTime, -2000f, 2000f);
			}
			if (!autoScrollUp && !autoScrollDown)
			{
				autoScrolling.y = autoScrolling.y * 0.9f;
				if (autoScrolling.y != 0f)
					autoScrolling.y = autoScrolling.y > 0f ? Mathf.Max(0f, autoScrolling.y - 50f * deltaTime) : Mathf.Min(0f, autoScrolling.y + 50f * deltaTime);
			}
			else
			{
				autoScrolling.y = Mathf.Clamp(autoScrolling.y + (autoScrollUp ? -500f : 500f) * deltaTime, -2000f, 2000f);
			}

			autoScrollDelta = autoScrolling * deltaTime;
			if (lastMouseEvent != null)
			{
				simulateLastMouseEvent = codeViewDragging && !mouseDownOnSelection;
			}
			lastAutoScrollTime = frameTime;
			if (EditorWindow.focusedWindow == OwnerWindow)
				EditorWindow.focusedWindow.wantsMouseMove = true;
			Repaint();
		}
		else if (hasCodeViewFocus)
		{
			lastAutoScrollTime = frameTime;

			float caretTime = FGTextBufferManager.isApplicationActive ? ((float) (frameTime - caretMoveTime).TotalSeconds) % 1f : 0f;

			if (!hasSelection && highightReferencesTime != caretMoveTime)
			{
				int lineIndex, tokenIndex;
				bool atTokenEnd;
				var token = textBuffer.GetTokenAt(caretPosition, out lineIndex, out tokenIndex, out atTokenEnd);
				if (token != null)
				{
					if (atTokenEnd && token.tokenKind != SyntaxToken.Kind.Identifier &&
						token.tokenKind != SyntaxToken.Kind.Keyword &&
						token.tokenKind != SyntaxToken.Kind.ContextualKeyword &&
						token.tokenKind != SyntaxToken.Kind.PreprocessorSymbol)
					{
						var tokens = textBuffer.formattedLines[lineIndex].tokens;
						if (tokenIndex < tokens.Count - 1)
							token = tokens[tokenIndex + 1];
					}
				}

				if (!IsLoading && ((float) (frameTime - caretMoveTime).TotalSeconds) >= 0.35f)
				{
					if (token != null)
					{
						highightReferencesTime = caretMoveTime;

						if (token.parent != null && token.parent.resolvedSymbol != null &&
							(token.tokenKind == SyntaxToken.Kind.Identifier ||
								token.tokenKind == SyntaxToken.Kind.ContextualKeyword ||
								token.tokenKind == SyntaxToken.Kind.Keyword) &&
							token.parent.resolvedSymbol.kind != SymbolKind.Error)
						{
							if (highlightedSymbol != token.parent.resolvedSymbol)
							{
								highlightedSymbol = token.parent.resolvedSymbol.GetGenericSymbol();
								highlightedPPSymbol = null;
								Repaint();
								return;
							}
						}
						else if (token.tokenKind == SyntaxToken.Kind.PreprocessorSymbol)
						{
							highlightedSymbol = null;
							highlightedPPSymbol = token.text;
							Repaint();
							return;
						}
						else if (!SISettings.keepLastHighlight && (highlightedSymbol != null || highlightedPPSymbol != null))
						{
							highlightedSymbol = null;
							highlightedPPSymbol = null;
							Repaint();
							return;
						}
					}
					else if (!SISettings.keepLastHighlight && (highlightedSymbol != null || highlightedPPSymbol != null))
					{
						highlightedSymbol = null;
						highlightedPPSymbol = null;
						Repaint();
						return;
					}
				}
				else if (!SISettings.keepLastHighlight && highlightedSymbol != null)
				{
					if (token == null || token.parent == null || token.parent.resolvedSymbol == null
						|| token.parent.resolvedSymbol.kind == SymbolKind.Error
						|| (token.tokenKind != SyntaxToken.Kind.Identifier
							&& token.tokenKind != SyntaxToken.Kind.ContextualKeyword
							&& token.tokenKind != SyntaxToken.Kind.Keyword)
						|| (highlightedSymbol != token.parent.resolvedSymbol
							&& highlightedSymbol != token.parent.resolvedSymbol.GetGenericSymbol()))
					{
						highlightedSymbol = null;
						Repaint();
						return;
					}
				}
				else if (!SISettings.keepLastHighlight && highlightedPPSymbol != null)
				{
					if (token == null || token.tokenKind != SyntaxToken.Kind.PreprocessorSymbol)
					{
						highlightedPPSymbol = null;
						Repaint();
						return;
					}
				}
			}
			
			//if (GetFocusedInspector() != null)
			//	Debug.Log(GetFocusedInspector().title);
			if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow == OwnerWindow)
				EditorWindow.focusedWindow.wantsMouseMove = true;

			var shouldCaretBeVisible = caretTime < 0.5f;
			if (!IsLoading && isCaretOn != shouldCaretBeVisible)
			{
				Repaint();
			}
		}

		if (showArgumentsHintForLeaf != null && hasCodeViewFocus)
		{
			ShowArgumentsHint(showArgumentsHintForLeaf);
			showArgumentsHintForLeaf = null;
		}
		else if (showArgumentsHintForLeaf != null)
		{
#if SI3_WARNINGS
			Debug.LogWarning(showArgumentsHintForLeaf);
#endif
		}
		else if (tokenTooltip == null && (hasCodeViewFocus || hasSearchBoxFocus))
		{
			if (mouseHoverToken != null && mouseHoverTime != default(System.DateTime) &&
				((float) (frameTime - mouseHoverTime).TotalSeconds) > 0.25f)
			{
				if (mouseHoverToken.parent != null && EditorWindow.mouseOverWindow == OwnerWindow)
				{
					var resolvedSymbol = mouseHoverToken.parent.resolvedSymbol;
					if (resolvedSymbol == null ||
						!resolvedSymbol.IsValid() ||
						resolvedSymbol.kind == SymbolKind.Error)
					{
						mouseHoverToken.parent.resolvedSymbol = null;
						FGResolver.ResolveNode(mouseHoverToken.parent.parent);
					}
					if (mouseHoverToken.parent.resolvedSymbol != null)
					{
						tokenTooltip = FGTooltip.Create(this, mouseHoverTokenRect, mouseHoverToken.parent);
					}
					else if (mouseHoverToken.parent.syntaxError != null || mouseHoverToken.parent.semanticError != null)
					{
						tokenTooltip = FGTooltip.Create(this, mouseHoverTokenRect, mouseHoverToken.parent);
					}
				}
				else
				{
					mouseHoverToken = null;
				}
			}
		}
		else if (mouseHoverTime == default(System.DateTime) && tokenTooltip != null)
		{
			tokenTooltip.Hide();
		}

		if (!string.IsNullOrEmpty(searchString) && searchResultAge == textBuffer.undoPosition)
		{
			if (searchedLinesFrom > 0)
			{
				var line = Mathf.Max(0, searchedLinesFrom - 200);
				var numFound = ProgressiveSearch(line);
				if (numFound > 0)
					Repaint();
			}
			if (searchedLinesTo < textBuffer.lines.Count)
			{
				var line = Mathf.Min(textBuffer.lines.Count, searchedLinesTo + 200);
				var numFound = ProgressiveSearch(line);
				if (numFound > 0)
					Repaint();
			}
		}
	}

	[NonSerialized]
	private int searchedLinesFrom = 0;
	[NonSerialized]
	private int searchedLinesTo = 0;
	private static List<TextPosition> tempSearchResults = new List<TextPosition>();
	private int ProgressiveSearch(int toLine)
	{
		var searchUp = toLine < searchedLinesFrom;
		if (searchUp)
		{
			tempSearchResults.Clear();
			SearchInLines(tempSearchResults, toLine, searchedLinesFrom);
			searchedLinesFrom = toLine;

			if (tempSearchResults.Count == 0)
				return 0;

			if (currentSearchResult >= 0)
			{
				currentSearchResult += tempSearchResults.Count;
			}

			tempSearchResults.AddRange(searchResults);
			var t = searchResults;
			searchResults = tempSearchResults;
			tempSearchResults = t;
			
			return searchResults.Count - tempSearchResults.Count;
		}
		
		var searchDown = toLine > searchedLinesTo;
		if (searchDown)
		{
			var startCount = searchResults.Count;
			
			SearchInLines(searchResults, searchedLinesTo, toLine);
			searchedLinesTo = toLine;
			
			return searchResults.Count - startCount;
		}

		return 0;
	}

	private void SearchInLines(List<TextPosition> results, int fromLine, int toLine)
	{
		if (string.IsNullOrEmpty(searchString))
			return;

		for (int i = fromLine; i < toLine; ++i)
		{
			var line = textBuffer.lines[i];
			int start = 0, index;
			while ((index = line.IndexOf(searchString, start, StringComparison.OrdinalIgnoreCase)) >= 0)
			{
				results.Add(new TextPosition(i, index));
				start = index + searchString.Length;
			}
		}
	}

	private static string editorResourcesPath;

	public static T LoadEditorResource<T>(string indieAndProName) where T : UnityEngine.Object
	{
		return LoadEditorResource<T>(indieAndProName, null);
	}
	
	public static T LoadEditorResource<T>(string indieName, string proName) where T : UnityEngine.Object
	{
		if (editorResourcesPath == null)
		{
			MonoScript managerScript = MonoScript.FromScriptableObject(FGTextBufferManager.Instance());
			editorResourcesPath = AssetDatabase.GetAssetPath(managerScript);
			if (string.IsNullOrEmpty(editorResourcesPath))
				editorResourcesPath = "Assets/Plugins/Editor/ScriptInspector3/Scripts/FGTextEditor.cs";
			editorResourcesPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(editorResourcesPath));
			editorResourcesPath = System.IO.Path.Combine(editorResourcesPath, "EditorResources");
		}
		
		string fileName = proName == null ? indieName : EditorGUIUtility.isProSkin ? proName : indieName;
		string path = System.IO.Path.Combine(editorResourcesPath, fileName);
		return AssetDatabase.LoadMainAssetAtPath(path) as T;
	}

	// returns 0 for non-dynamic fonts
	private static int GetDynamicFontSize(Font font)
	{
		if (font == null)
			return 0;
		
		TrueTypeFontImporter fontImporter = TrueTypeFontImporter.GetAtPath(AssetDatabase.GetAssetPath(font)) as TrueTypeFontImporter;
		return fontImporter != null && fontImporter.fontTextureCase == FontTextureCase.Dynamic ? fontImporter.fontSize : 0;
	}
	
	private static readonly GUIContent cachedContentW = new GUIContent(".");
	private static readonly GUIContent cachedContentW2 = new GUIContent("..\n..");
	public void Initialize()
	{
		Vector2 newCharSize;
		if (styles.normalStyle != null && styles.normalStyle.font != null)
		{
#if !UNITY_2023_2_OR_NEWER
			newCharSize = styles.normalStyle.CalcSize(cachedContentW2) - styles.normalStyle.CalcSize(cachedContentW);
#else
			newCharSize = styles.normalStyle.CalcSize(cachedContentW);
#endif
		}
		else
		{
			newCharSize = charSize;
		}
		
		Vector2 cs = styles.normalStyle != null && styles.normalStyle.font != null ? newCharSize : charSize;
		bool isText = textBuffer != null && textBuffer.isText;
		bool reset = isText ? resetTextFont : resetCodeFont;

		if (reset || cs != charSize ||
			styles.normalStyle == null || styles.normalStyle.font == null ||
			undoIcon == null ||
			(SISettings.fontSizeDelta == 0) != (styles.normalStyle.fontSize == 0)
		)
		{
			_softLineBreaks = null;
			yLineOffsets = new List<int>();
			yLineOffsets.Add(0);
			//tokenWidths.Clear();

			if (isText)
				resetTextFont = false;
			else
				resetCodeFont = false;
			
			LoadStyles(styles, isText);
			
#if !UNITY_2023_2_OR_NEWER
			charSize = styles.normalStyle.CalcSize(cachedContentW2) - styles.normalStyle.CalcSize(cachedContentW);
#else
			charSize = styles.normalStyle.CalcSize(cachedContentW);
#endif
			pointSize = charSize.y / 13f;
		}
	}
	
	public static Styles StylesCode {
		get {
			if (stylesCode.normalStyle == null)
			{
				InitializeFont(false);
				LoadStyles(stylesCode, false);
			}
			return stylesCode;
		}
	}
	
	public static Styles StylesText {
		get {
			if (stylesText.normalStyle == null)
			{
				InitializeFont(true);
				LoadStyles(stylesText, true);
			}
			return stylesText;
		}
	}
	
	public static Styles GetStyles(bool forText)
	{
		var styles = forText ? stylesText : stylesCode;
		if (styles.normalStyle == null)
		{
			InitializeFont(forText);
			LoadStyles(styles, forText);
		}
		return styles;
	}
	
	private static void LoadStyles(Styles styles, bool forText)
	{
		var themeIndex = availableThemes.IndexOf(forText ? SISettings.themeNameText : SISettings.themeNameCode);
		themeIndex = themeIndex < 0 ? 0 : themeIndex;
		if (forText)
			currentThemeText = themes[themeIndex];
		else
			currentThemeCode = themes[themeIndex];
		
		styles.normalStyle = styles.normalStyle ?? new GUIStyle(GUIStyle.none);
		styles.normalStyle.richText = false;
		var fontPath = SISettings.fontHinting ? currentFont : "Smooth " + currentFont;
		styles.normalStyle.font = LoadEditorResource<Font>(fontPath);
		for (int i = 0; styles.normalStyle.font == null && i < availableFonts.Length; ++i)
		{
			currentFont = availableFonts[i];
			fontPath = SISettings.fontHinting ? currentFont : "Smooth " + currentFont;
			styles.normalStyle.font = LoadEditorResource<Font>(fontPath);
		}

		int currentFontSize = GetDynamicFontSize(styles.normalStyle.font);
		bool isDynamicFont = currentFontSize != 0;
		if (SISettings.fontSizeDelta != 0)
			styles.normalStyle.fontSize = currentFontSize + SISettings.fontSizeDelta;
		else
			styles.normalStyle.fontSize = 0;
		
		styles.hyperlinkStyle = styles.hyperlinkStyle ?? new GUIStyle(styles.normalStyle);
		styles.mailtoStyle = styles.mailtoStyle ?? new GUIStyle(styles.hyperlinkStyle);
		styles.keywordStyle = styles.keywordStyle ?? new GUIStyle(styles.normalStyle);
		styles.controlKeywordStyle = styles.controlKeywordStyle ?? new GUIStyle(styles.normalStyle);
		styles.constantStyle = styles.constantStyle ?? new GUIStyle(styles.normalStyle);
		styles.stringStyle = styles.stringStyle ?? new GUIStyle(styles.normalStyle);
		styles.builtInLiteralsStyle = styles.builtInLiteralsStyle ?? new GUIStyle(styles.normalStyle);
		styles.operatorStyle = styles.operatorStyle ?? new GUIStyle(styles.normalStyle);
		styles.punctuatorStyle = styles.punctuatorStyle ?? new GUIStyle(styles.normalStyle);
		styles.referenceTypeStyle = styles.referenceTypeStyle ?? new GUIStyle(styles.normalStyle);
		styles.valueTypeStyle = styles.valueTypeStyle ?? new GUIStyle(styles.normalStyle);
		styles.interfaceTypeStyle = styles.interfaceTypeStyle ?? new GUIStyle(styles.normalStyle);
		styles.enumTypeStyle = styles.enumTypeStyle ?? new GUIStyle(styles.normalStyle);
		styles.delegateTypeStyle = styles.delegateTypeStyle ?? new GUIStyle(styles.normalStyle);
		styles.builtInRefTypeStyle = styles.builtInRefTypeStyle ?? new GUIStyle(styles.normalStyle);
		styles.builtInValueTypeStyle = styles.builtInValueTypeStyle ?? new GUIStyle(styles.normalStyle);
		styles.namespaceStyle = styles.namespaceStyle ?? new GUIStyle(styles.normalStyle);
		styles.methodStyle = styles.methodStyle ?? new GUIStyle(styles.normalStyle);
		styles.fieldStyle = styles.fieldStyle ?? new GUIStyle(styles.normalStyle);
		styles.propertyStyle = styles.propertyStyle ?? new GUIStyle(styles.normalStyle);
		styles.eventStyle = styles.eventStyle ?? new GUIStyle(styles.normalStyle);
		styles.parameterStyle = styles.parameterStyle ?? new GUIStyle(styles.normalStyle);
		styles.variableStyle = styles.variableStyle?? new GUIStyle(styles.normalStyle);
		styles.typeParameterStyle = styles.typeParameterStyle ?? new GUIStyle(styles.normalStyle);
		styles.enumMemberStyle = styles.enumMemberStyle ?? new GUIStyle(styles.normalStyle);
		styles.preprocessorStyle = styles.preprocessorStyle ?? new GUIStyle(styles.normalStyle);
		styles.defineSymbols = styles.defineSymbols ?? new GUIStyle(styles.normalStyle);
		styles.inactiveCodeStyle = styles.inactiveCodeStyle ?? new GUIStyle(styles.normalStyle);
		styles.foldedTextStyle = styles.foldedTextStyle ?? new GUIStyle(styles.normalStyle);
		styles.commentStyle = styles.commentStyle ?? new GUIStyle(styles.normalStyle);
		styles.xmlDocsStyle = styles.xmlDocsStyle ?? new GUIStyle(styles.normalStyle);
		styles.xmlDocsTagsStyle = styles.xmlDocsTagsStyle ?? new GUIStyle(styles.normalStyle);
		styles.lineNumbersStyle = styles.lineNumbersStyle ?? new GUIStyle(styles.normalStyle);
		styles.currentLineNumberStyle = styles.currentLineNumberStyle ?? new GUIStyle(styles.normalStyle);
		styles.tooltipTextStyle = styles.tooltipTextStyle ?? new GUIStyle(styles.normalStyle);

		styles.hyperlinkStyle.font = styles.normalStyle.font;
		styles.mailtoStyle.font = styles.normalStyle.font;
		styles.keywordStyle.font = styles.normalStyle.font;
		styles.controlKeywordStyle.font = styles.normalStyle.font;
		styles.constantStyle.font = styles.normalStyle.font;
		styles.stringStyle.font = styles.normalStyle.font;
		styles.builtInLiteralsStyle.font = styles.normalStyle.font;
		styles.operatorStyle.font = styles.normalStyle.font;
		styles.punctuatorStyle.font = styles.normalStyle.font;
		styles.referenceTypeStyle.font = styles.normalStyle.font;
		styles.valueTypeStyle.font = styles.normalStyle.font;
		styles.interfaceTypeStyle.font = styles.normalStyle.font;
		styles.enumTypeStyle.font = styles.normalStyle.font;
		styles.delegateTypeStyle.font = styles.normalStyle.font;
		styles.builtInRefTypeStyle.font = styles.normalStyle.font;
		styles.builtInValueTypeStyle.font = styles.normalStyle.font;
		styles.namespaceStyle.font = styles.normalStyle.font;
		styles.methodStyle.font = styles.normalStyle.font;
		styles.fieldStyle.font = styles.normalStyle.font;
		styles.propertyStyle.font = styles.normalStyle.font;
		styles.eventStyle.font = styles.normalStyle.font;
		styles.parameterStyle.font = styles.normalStyle.font;
		styles.variableStyle.font = styles.normalStyle.font;
		styles.typeParameterStyle.font = styles.normalStyle.font;
		styles.enumMemberStyle.font = styles.normalStyle.font;
		styles.preprocessorStyle.font = styles.normalStyle.font;
		styles.defineSymbols.font = styles.normalStyle.font;
		styles.inactiveCodeStyle.font = styles.normalStyle.font;
		styles.foldedTextStyle.font = styles.normalStyle.font;
		styles.commentStyle.font = styles.normalStyle.font;
		styles.xmlDocsStyle.font = styles.normalStyle.font;
		styles.xmlDocsTagsStyle.font = styles.normalStyle.font;
		styles.lineNumbersStyle.font = styles.normalStyle.font;
		styles.currentLineNumberStyle.font = styles.normalStyle.font;
		styles.tooltipTextStyle.font = styles.normalStyle.font;
		styles.tooltipTextStyle.wordWrap = true;

		if (isDynamicFont)
		{
			styles.hyperlinkStyle.fontSize = styles.normalStyle.fontSize;
			styles.mailtoStyle.fontSize = styles.normalStyle.fontSize;
			styles.keywordStyle.fontSize = styles.normalStyle.fontSize;
			styles.controlKeywordStyle.fontSize = styles.normalStyle.fontSize;
			styles.constantStyle.fontSize = styles.normalStyle.fontSize;
			styles.stringStyle.fontSize = styles.normalStyle.fontSize;
			styles.builtInLiteralsStyle.fontSize = styles.normalStyle.fontSize;
			styles.operatorStyle.fontSize = styles.normalStyle.fontSize;
			styles.punctuatorStyle.fontSize = styles.normalStyle.fontSize;
			styles.referenceTypeStyle.fontSize = styles.normalStyle.fontSize;
			styles.valueTypeStyle.fontSize = styles.normalStyle.fontSize;
			styles.interfaceTypeStyle.fontSize = styles.normalStyle.fontSize;
			styles.enumTypeStyle.fontSize = styles.normalStyle.fontSize;
			styles.delegateTypeStyle.fontSize = styles.normalStyle.fontSize;
			styles.builtInRefTypeStyle.fontSize = styles.normalStyle.fontSize;
			styles.builtInValueTypeStyle.fontSize = styles.normalStyle.fontSize;
			styles.namespaceStyle.fontSize = styles.normalStyle.fontSize;
			styles.methodStyle.fontSize = styles.normalStyle.fontSize;
			styles.fieldStyle.fontSize = styles.normalStyle.fontSize;
			styles.propertyStyle.fontSize = styles.normalStyle.fontSize;
			styles.eventStyle.fontSize = styles.normalStyle.fontSize;
			styles.parameterStyle.fontSize = styles.normalStyle.fontSize;
			styles.variableStyle.fontSize = styles.normalStyle.fontSize;
			styles.typeParameterStyle.fontSize = styles.normalStyle.fontSize;
			styles.enumMemberStyle.fontSize = styles.normalStyle.fontSize;
			styles.preprocessorStyle.fontSize = styles.normalStyle.fontSize;
			styles.defineSymbols.fontSize = styles.normalStyle.fontSize;
			styles.inactiveCodeStyle.fontSize = styles.normalStyle.fontSize;
			styles.foldedTextStyle.fontSize = styles.normalStyle.fontSize;
			styles.commentStyle.fontSize = styles.normalStyle.fontSize;
			styles.xmlDocsStyle.fontSize = styles.normalStyle.fontSize;
			styles.xmlDocsTagsStyle.fontSize = styles.normalStyle.fontSize;
			styles.lineNumbersStyle.fontSize = styles.normalStyle.fontSize;
			styles.currentLineNumberStyle.fontSize = styles.normalStyle.fontSize;
			styles.tooltipTextStyle.fontSize = styles.normalStyle.fontSize;
		}
		else
		{
			styles.normalStyle.fontSize = 0;
			styles.hyperlinkStyle.fontSize = 0;
			styles.mailtoStyle.fontSize = 0;
			styles.keywordStyle.fontSize = 0;
			styles.controlKeywordStyle.fontSize = 0;
			styles.constantStyle.fontSize = 0;
			styles.stringStyle.fontSize = 0;
			styles.builtInLiteralsStyle.fontSize = 0;
			styles.operatorStyle.fontSize = 0;
			styles.punctuatorStyle.fontSize = 0;
			styles.referenceTypeStyle.fontSize = 0;
			styles.valueTypeStyle.fontSize = 0;
			styles.interfaceTypeStyle.fontSize = 0;
			styles.enumTypeStyle.fontSize = 0;
			styles.delegateTypeStyle.fontSize = 0;
			styles.builtInRefTypeStyle.fontSize = 0;
			styles.builtInValueTypeStyle.fontSize = 0;
			styles.namespaceStyle.fontSize = 0;
			styles.methodStyle.fontSize = 0;
			styles.fieldStyle.fontSize = 0;
			styles.propertyStyle.fontSize = 0;
			styles.eventStyle.fontSize = 0;
			styles.parameterStyle.fontSize = 0;
			styles.variableStyle.fontSize = 0;
			styles.typeParameterStyle.fontSize = 0;
			styles.enumMemberStyle.fontSize = 0;
			styles.preprocessorStyle.fontSize = 0;
			styles.defineSymbols.fontSize = 0;
			styles.inactiveCodeStyle.fontSize = 0;
			styles.foldedTextStyle.fontSize = 0;
			styles.commentStyle.fontSize = 0;
			styles.xmlDocsStyle.fontSize = 0;
			styles.xmlDocsTagsStyle.fontSize = 0;
			styles.lineNumbersStyle.fontSize = 0;
			styles.currentLineNumberStyle.fontSize = 0;
			styles.tooltipTextStyle.fontSize = 0;
		}

		styles.upArrowStyle = styles.upArrowStyle ?? new GUIStyle();
		styles.downArrowStyle = styles.downArrowStyle ?? new GUIStyle();
		styles.upArrowStyle.normal.background = LoadEditorResource<Texture2D>("upArrowOff.png", "d_upArrowOff.png");
		styles.upArrowStyle.hover.background = styles.upArrowStyle.active.background
			= LoadEditorResource<Texture2D>("upArrow.png", "d_upArrow.png");
		styles.downArrowStyle.normal.background = LoadEditorResource<Texture2D>("downArrowOff.png", "d_downArrowOff.png");
		styles.downArrowStyle.hover.background = styles.downArrowStyle.active.background
			= LoadEditorResource<Texture2D>("downArrow.png", "d_downArrow.png");

		wavyUnderline = LoadEditorResource<Texture2D>("wavyUnderline.png");

		wrenchIcon = LoadEditorResource<Texture2D>("l_wrench.png", "d_wrench.png");
		newIcon = EditorGUIUtility.Load("icons/d_AS Badge New.png") as Texture2D;

		saveIcon = LoadEditorResource<Texture2D>("saveIconBW.png");
		buildIcon = LoadEditorResource<Texture2D>("buildIconBW.png");
		undoIcon = LoadEditorResource<Texture2D>("editUndoIconBW.png");
		redoIcon = LoadEditorResource<Texture2D>("editRedoIconBW.png");
#if !UNITY_4_0 && !UNITY_4_1
		versionControlIcon = LoadEditorResource<Texture2D>("versionControl.png");
#endif
		//hyperlinksIcon = LoadEditorResource<Texture2D>("hyperlinksIconBW.png");
		popOutIcon = LoadEditorResource<Texture2D>("popOutIconBW.png");

		styles.ping = styles.ping ?? new GUIStyle();
#if !UNITY_3_5
		styles.ping.richText = false;
#endif
		styles.ping.normal.background = LoadEditorResource<Texture2D>("yellowPing.png");
		styles.ping.normal.textColor = Color.black;
		styles.ping.font = styles.normalStyle.font;
		if (isDynamicFont)
			styles.ping.fontSize = styles.normalStyle.fontSize;
		else
			styles.ping.fontSize = 0;
		styles.ping.border = new RectOffset(10, 10, 10, 10);
		styles.ping.overflow = new RectOffset(7, 7, 6, 6);
		styles.ping.stretchWidth = false;
		styles.ping.stretchHeight = false;

		ApplyTheme(styles, forText ? currentThemeText : currentThemeCode);
	}

	private static void ApplyTheme(Styles styles, Theme currentTheme)
	{
		styles.scrollViewColor = currentTheme.background;
		styles.searchResultColor = currentTheme.searchResults;
		styles.caretColor = currentTheme.text;
		styles.activeSelectionColor = currentTheme.activeSelection;
		styles.passiveSelectionColor = currentTheme.passiveSelection;
		styles.trackChangesBeforeSaveColor = currentTheme.trackChanged;
		styles.trackChangesAfterSaveColor = currentTheme.trackSaved;
		styles.trackChangesRevertedColor = currentTheme.trackReverted;
		styles.currentLineColor = currentTheme.currentLine;
		styles.currentLineInactiveColor = currentTheme.currentLineInactive;
		styles.referenceHighlightColor = currentTheme.referenceHighlight;
		styles.referenceModifyHighlightColor = currentTheme.referenceModifyHighlight;
		styles.tooltipBgColor = currentTheme.tooltipBackground;
		styles.tooltipFrameColor = currentTheme.tooltipFrame;
		
		styles.listFrameColor = currentTheme.listPopupFrame == Color.clear ? currentTheme.fold : currentTheme.listPopupFrame;
		styles.listBgColor = currentTheme.listPopupBackground;
		
		styles.normalStyle.normal.textColor = currentTheme.text;
		styles.keywordStyle.normal.textColor = currentTheme.keywords;
		styles.controlKeywordStyle.normal.textColor = currentTheme.controlKeywords;
		styles.constantStyle.normal.textColor = currentTheme.constants;
		styles.stringStyle.normal.textColor = currentTheme.strings;
		styles.builtInLiteralsStyle.normal.textColor = currentTheme.builtInLiterals;
		styles.operatorStyle.normal.textColor = currentTheme.operators;
		styles.punctuatorStyle.normal.textColor = currentTheme.punctuators.a > 0f ? currentTheme.punctuators : currentTheme.text;
		styles.referenceTypeStyle.normal.textColor = currentTheme.referenceTypes;
		styles.valueTypeStyle.normal.textColor = currentTheme.valueTypes;
		styles.interfaceTypeStyle.normal.textColor = currentTheme.interfaceTypes;
		styles.enumTypeStyle.normal.textColor = currentTheme.enumTypes;
		styles.delegateTypeStyle.normal.textColor = currentTheme.delegateTypes;
		styles.builtInRefTypeStyle.normal.textColor = currentTheme.builtInTypes.a > 0f ? currentTheme.builtInTypes : currentTheme.referenceTypes;
		styles.builtInValueTypeStyle.normal.textColor = currentTheme.builtInTypes.a > 0f ? currentTheme.builtInTypes : currentTheme.valueTypes;
		styles.namespaceStyle.normal.textColor = currentTheme.namespaces;
		styles.methodStyle.normal.textColor = currentTheme.methods;
		styles.fieldStyle.normal.textColor = currentTheme.fields;
		styles.propertyStyle.normal.textColor = currentTheme.properties;
		styles.eventStyle.normal.textColor = currentTheme.events;
		styles.parameterStyle.normal.textColor = currentTheme.parameters;
		styles.variableStyle.normal.textColor = currentTheme.variables;
		styles.typeParameterStyle.normal.textColor = currentTheme.typeParameters;
		styles.enumMemberStyle.normal.textColor = currentTheme.enumMembers.a != 0f ? currentTheme.enumMembers : currentTheme.text;
		styles.preprocessorStyle.normal.textColor = currentTheme.preprocessor;
		styles.defineSymbols.normal.textColor = currentTheme.defineSymbols;
		styles.inactiveCodeStyle.normal.textColor = currentTheme.inactiveCode;
		styles.foldedTextStyle.normal.textColor = currentTheme.foldedText;
		styles.commentStyle.normal.textColor = currentTheme.comments;
		styles.xmlDocsStyle.normal.textColor = currentTheme.xmlDocs;
		styles.xmlDocsTagsStyle.normal.textColor = currentTheme.xmlDocsTags;
		styles.tooltipTextStyle.normal.textColor = SISettings.useStandardColorInPopups ? currentTheme.text : currentTheme.tooltipText;

		styles.hyperlinkStyle.normal.textColor = currentTheme.hyperlinks;
		styles.mailtoStyle.normal.textColor = currentTheme.hyperlinks;
		styles.hyperlinkStyle.normal.background = styles.mailtoStyle.normal.background =
			UnderlineTexture(currentTheme.hyperlinks, (int)styles.mailtoStyle.lineHeight);

		styles.lineNumbersBackground = currentTheme.lineNumbersBackground;
		styles.lineNumbersSeparator = currentTheme.fold;
		styles.foldingButton = currentTheme.foldingButton;

		styles.lineNumbersStyle.normal.textColor = currentTheme.lineNumbers;
		styles.lineNumbersStyle.alignment = TextAnchor.UpperRight;
		styles.currentLineNumberStyle.normal.textColor = currentTheme.lineNumbersHighlight;
		styles.currentLineNumberStyle.alignment = TextAnchor.UpperRight;

		bool isDynamic = GetDynamicFontSize(styles.normalStyle.font) != 0;
		int boldFilter = currentFont == "Fonts/DejaVu Sans Mono.ttf" ? 3 : 2;
		styles.commentStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.commentsStyle & boldFilter) : 0;
		styles.stringStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.stringsStyle & boldFilter) : 0;
		styles.keywordStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.keywordsStyle & boldFilter) : 0;
		styles.controlKeywordStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.keywordsStyle & boldFilter) : 0;
		styles.constantStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.constantsStyle & boldFilter) : 0;
		styles.referenceTypeStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.typesStyle & boldFilter) : 0;
		styles.valueTypeStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.typesStyle & boldFilter) : 0;
		styles.interfaceTypeStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.typesStyle & boldFilter) : 0;
		styles.enumTypeStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.typesStyle & boldFilter) : 0;
		styles.delegateTypeStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.typesStyle & boldFilter) : 0;
		styles.builtInRefTypeStyle.fontStyle = isDynamic ? (FontStyle)((int)(currentTheme.builtInTypes == Color.clear ? currentTheme.typesStyle : currentTheme.keywordsStyle) & boldFilter) : 0;
		styles.builtInValueTypeStyle.fontStyle = isDynamic ? (FontStyle)((int)(currentTheme.builtInTypes == Color.clear ? currentTheme.typesStyle : currentTheme.keywordsStyle) & boldFilter) : 0;
		styles.namespaceStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.namespacesStyle & boldFilter) : 0;
		styles.methodStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.methodsStyle & boldFilter) : 0;
		styles.fieldStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.fieldsStyle & boldFilter) : 0;
		styles.propertyStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.propertiesStyle & boldFilter) : 0;
		styles.eventStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.eventsStyle & boldFilter) : 0;
		styles.hyperlinkStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.hyperlinksStyle & boldFilter) : 0;
		styles.mailtoStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.hyperlinksStyle & boldFilter) : 0;
		styles.preprocessorStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.preprocessorStyle & boldFilter) : 0;
		styles.defineSymbols.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.preprocessorStyle & boldFilter) : 0;
		styles.inactiveCodeStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.inactiveCodeStyle & boldFilter) : 0;
		styles.foldedTextStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.foldedTextStyle & boldFilter) : 0;
		styles.parameterStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.parametersStyle & boldFilter) : 0;
		styles.variableStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.variablesStyle & boldFilter) : 0;
		styles.typeParameterStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.typeParametersStyle & boldFilter) : 0;
		styles.enumMemberStyle.fontStyle = isDynamic ? (FontStyle)((int)currentTheme.enumMembersStyle & boldFilter) : 0;
	}

	public static void FillRect(Rect rect, Color color)
	{
		if (Event.current.type != EventType.Repaint)
			return;
		
		Color oldColor = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
		GUI.color = oldColor;
	}

	public static void DrawRectFrame(Rect rect, Color color)
	{
		if (Event.current.type != EventType.Repaint)
			return;
		
		Color oldColor = GUI.color;
		GUI.color = color;
		
		var sideRect = rect;
		sideRect.height = 1f;
		GUI.DrawTexture(sideRect, EditorGUIUtility.whiteTexture);
		
		sideRect = rect;
		sideRect.width = 1f;
		GUI.DrawTexture(sideRect, EditorGUIUtility.whiteTexture);
		
		sideRect = rect;
		sideRect.xMin = sideRect.xMax - 1f;
		GUI.DrawTexture(sideRect, EditorGUIUtility.whiteTexture);
		
		sideRect = rect;
		sideRect.yMin = sideRect.yMax - 1f;
		GUI.DrawTexture(sideRect, EditorGUIUtility.whiteTexture);
		
		GUI.color = oldColor;
	}

	private static Texture2D UnderlineTexture(Color color, int lineHeight)
	{
		return CreateUnderlineTexture(color, lineHeight, Color.clear);
	}
	
	private static void OpenAssetStorePage()
	{
		Help.BrowseURL("http://u3d.as/2ZC");
	}

	private static Texture2D CreateUnderlineTexture(Color color, int lineHeight, Color bgColor)
	{
		Texture2D underlined = new Texture2D(1, lineHeight, TextureFormat.RGBA32, false, true);
		underlined.SetPixel(0, 0, color);
		for (int i = 1; i < lineHeight; ++i)
			underlined.SetPixel(0, i, new Color32(0, 0, 0, 0));
		underlined.Apply();
		underlined.hideFlags = HideFlags.HideAndDontSave;
		return underlined;
	}

	private EditorWindow currentInspector;
	
	public EditorWindow OwnerWindow { get { return parentWindow ?? currentInspector; } }
	
	private bool clearSearchButtonClicked = false;
	
	private void ShowWrenchMenu(Rect rc)
	{
		GenericMenu wrenchMenu = new GenericMenu();
		
		if (FGTextBufferManager.IsUpdateAvailable())
		{
			wrenchMenu.AddItem(new GUIContent("UPDATE to " + FGTextBufferManager.versionInfo.name +
				", version " + FGTextBufferManager.versionInfo.version), false, OpenAssetStorePage);
			wrenchMenu.AddSeparator("");
		}
		
#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1
		wrenchMenu.AddItem(new GUIContent("Always open in Script Inspector"), SISettings.handleOpenAssets, ToggleHandleOpenAsset);
		wrenchMenu.AddItem(new GUIContent("Always open in External IDE"), SISettings.dontOpenAssets, ToggleDontOpenAsset);
		if (SISettings.handleOpenAssets || SISettings.dontOpenAssets)
		{
			wrenchMenu.AddDisabledItem(new GUIContent("Open on Double-Click/Scripts"));
			wrenchMenu.AddDisabledItem(new GUIContent("Open on Double-Click/Shaders"));
			wrenchMenu.AddDisabledItem(new GUIContent("Open on Double-Click/Text Assets"));
		}
		else
#endif
		{
			wrenchMenu.AddItem(new GUIContent("Open on Double-Click/Scripts"), SISettings.handleOpeningScripts, ToggleHandleOpenScriptsFromProject);
			wrenchMenu.AddItem(new GUIContent("Open on Double-Click/Shaders"), SISettings.handleOpeningShaders, ToggleHandleOpenShadersFromProject);
			wrenchMenu.AddItem(new GUIContent("Open on Double-Click/Text Assets"), SISettings.handleOpeningText, ToggleHandleOpenTextsFromProject);
		}
		
		wrenchMenu.AddSeparator(string.Empty);
		
		bool isInspector = parentWindow == null;
		if (textBuffer.isText)
		{
			wrenchMenu.AddItem(new GUIContent("View Options/Word Wrap (Text)"),
				isInspector ? SISettings.wordWrapTextInspector : SISettings.wordWrapText, ToggleWordWrapText);
			wrenchMenu.AddItem(new GUIContent("View Options/Highlight Current Line"), SISettings.highlightCurrentLine, ToggleHighlightCurrentLine);
			wrenchMenu.AddItem(new GUIContent("View Options/Frame Current Line"), SISettings.frameCurrentLine, ToggleFrameCurrentLine);
			wrenchMenu.AddItem(new GUIContent("View Options/Line Numbers (Text)"),
				isInspector ? SISettings.showLineNumbersTextInspector : SISettings.showLineNumbersText, ToggleLineNumbersText);
			wrenchMenu.AddItem(new GUIContent("View Options/Track Changes (Text)"),
				isInspector ? SISettings.trackChangesTextInspector : SISettings.trackChangesText, ToggleTrackChangesText);
		}
		else
		{
			wrenchMenu.AddItem(new GUIContent("View Options/Word Wrap (Code)"),
				isInspector ? SISettings.wordWrapCodeInspector : SISettings.wordWrapCode, ToggleWordWrapCode);
			wrenchMenu.AddItem(new GUIContent("View Options/Highlight Current Line"), SISettings.highlightCurrentLine, ToggleHighlightCurrentLine);
			wrenchMenu.AddItem(new GUIContent("View Options/Frame Current Line"), SISettings.frameCurrentLine, ToggleFrameCurrentLine);
			wrenchMenu.AddItem(new GUIContent("View Options/Line Numbers (Code)"),
				isInspector ? SISettings.showLineNumbersCodeInspector : SISettings.showLineNumbersCode, ToggleLineNumbersCode);
			wrenchMenu.AddItem(new GUIContent("View Options/Track Changes (Code)"),
				isInspector ? SISettings.trackChangesCodeInspector : SISettings.trackChangesCode, ToggleTrackChangesCode);
			wrenchMenu.AddItem(new GUIContent("View Options/Code Folding"),
				isInspector ? SISettings.codeFoldingCodeInspector : SISettings.codeFoldingCode, ToggleEnableCodeFolding);
		}

		//popupMenu.AddSeparator(string.Empty);
		for (int i = 0; i < availableFonts.Length; ++i)
			wrenchMenu.AddItem(new GUIContent("Font/" + Path.GetFileNameWithoutExtension(availableFonts[i])), currentFont == availableFonts[i], x => SelectFont((int)x), i);
		wrenchMenu.AddSeparator("Font//");
		wrenchMenu.AddItem(new GUIContent("Font/Use Font Hinting"), SISettings.fontHinting, ToggleFontHinting);
		
        string[] sortedThemes = availableThemes.ToArray();
		Array.Sort<string>(sortedThemes, StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < sortedThemes.Length; ++i)
		{
			int themeIndex = availableThemes.IndexOf(sortedThemes[i]);
			if (textBuffer.isText)
				wrenchMenu.AddItem(new GUIContent("Color Scheme (Text)/" + sortedThemes[i]), currentThemeText == themes[themeIndex], x => SelectTheme((int)x, true), themeIndex);
			else
				wrenchMenu.AddItem(new GUIContent("Color Scheme (Code)/" + sortedThemes[i]), currentThemeCode == themes[themeIndex], x => SelectTheme((int)x, false), themeIndex);
		}
		
		wrenchMenu.AddSeparator(string.Empty);
		wrenchMenu.AddItem(new GUIContent("Preferences..."), false, SISettings.OpenSIPreferences);
		
		wrenchMenu.AddSeparator(string.Empty);
		wrenchMenu.AddItem(new GUIContent("About..."), false, About);

		wrenchMenu.DropDown(rc);
		GUIUtility.ExitGUI();
	}

	public void OnWindowGUI(EditorWindow window, RectOffset margins)
	{
		if (window != currentInspector)
			parentWindow = window;
		if (EditorWindow.focusedWindow == window)
			FGTextBuffer.activeEditor = this;
		
		if (Event.current.type != EventType.Layout)
		{
			if (parentWindow)
				scrollViewRect = new Rect(0f, 0f, parentWindow.position.width, parentWindow.position.height);
			else
				scrollViewRect = GUILayoutUtility.GetRect(1f, Screen.width, 1f, Screen.height);
			//Debug.Log("GetRect: " + scrollViewRect + "\nposition: " + parentWindow.position);
			if (!(window is FGCodeWindow))
			{
				scrollViewRect.xMin = 0;
			//	scrollViewRect.xMax = Screen.width - 1;
			}
			scrollViewRect = margins.Remove(scrollViewRect);
		}
		else if (!parentWindow)
		{
			GUILayoutUtility.GetRect(1f, Screen.width, 112f, Screen.height);
		}
		
		if (Screen.width < 10f || Screen.height < 10f || scrollViewRect.width < 10f || scrollViewRect.height < 10f)
		{
			return;
		}

		bool enabled = GUI.enabled;
		GUI.enabled = CanEdit();

		if (textBuffer != null)
		{
			//if (Event.current.type == EventType.Repaint && textBuffer.isCsFile && textBuffer.IsLoading)
			//{
			//	EditorApplication.delayCall += textBuffer.LoadImmediately;
			//}
			
			Rect rc = new Rect(scrollViewRect.xMax - 21f, scrollViewRect.yMin - 17f, 18f, 16f);
			if (GUI.Button(rc, GUIContent.none, EditorStyles.toolbarButton))
			{
				ShowWrenchMenu(rc);
			}
		}

		Color oldColor = GUI.color;
		if (!GUI.enabled && Event.current.type == EventType.Repaint)
		{
			//GUI.color = new Color(0.85f, 0.85f, 0.85f);
			if (textBuffer != null)
				textBuffer.LoadFaster();
		}
		GUI.color = Color.white;

		try
		{
			if (hasCodeViewFocus && GUI.enabled)
			{
				FGTextBuffer.activeEditor = this;
				
				textBuffer.BeginEdit("change");
				try
				{
					DoGUIWithAutocomplete(enabled);
				}
				finally
				{
					textBuffer.EndEdit();
				}
			}
			else
			{
				DoGUIWithAutocomplete(enabled);
			}
		}
		finally
		{
			GUI.color = oldColor;
			GUI.enabled = enabled;
		}
	}
	
	private Vector2 inspectorEditorTopLeft;
	
	private float GetMaxHeightInInspector()
	{
#if !UNITY_2019_1_OR_NEWER
		return this.currentInspector.position.height;
#else
		if (isShader)
		{
			return this.currentInspector.position.height - 118f;
		}
		
		var height = this.currentInspector.position.height - (isTextAsset ? 118f : 195f);
		if (this.currentInspector.rootVisualElement.childCount == 0)
			return height;
		
		var element = this.currentInspector.rootVisualElement[this.currentInspector.rootVisualElement.childCount == 1 ? 0 : 1];
		if (element == null)
			return height;
		element = element.Query(null, "unity-inspector-main-container").First();
		if (element == null)
			return height;
		
		var inspectorRootScrollView = element.Query(null, "unity-inspector-root-scrollview").First() as ScrollView;
		if (inspectorRootScrollView == null)
			return height;
		var rc1 = inspectorRootScrollView.worldBound;
		
		var inspectorEditorList = inspectorRootScrollView
			.contentViewport//.Query("unity-content-viewport").First()
			.Query(null, "unity-inspector-editors-list").First();
		if (inspectorEditorList == null)
			return height;
		
		var inspectorElement = inspectorEditorList.Children().Last()//[inspectorEditorList.childCount - 1]// (isTextAsset ? inspectorEditorList[1] : inspectorEditorList[1])
			.Query(null, "unity-inspector-element").First();
		if (inspectorElement == null)
			return height;
			
		var topLeft = inspectorElement.ChangeCoordinatesTo(inspectorRootScrollView, Vector2.zero);
		inspectorEditorTopLeft = topLeft;
#if UNITY_2019_3_OR_NEWER
		height = rc1.height - topLeft.y - (isTextAsset ? 1f : 12f);
#else
		height = rc1.height - topLeft.y - (isTextAsset ? 1f : 8f);
#endif
		if (topLeft.y == 0f || height < 1f)
			return this.currentInspector.position.height - 195f;

		if (inspectorRootScrollView.verticalScroller.visible)
		{
#if UNITY_2021_1_OR_NEWER
			inspectorRootScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
#else
			inspectorRootScrollView.showVertical = false;
#endif
			//HACK: Force re-layout
			height -= 1f;
		}
		return height;
#endif
	}

	
	public void OnInspectorGUI(bool isScriptInspector, RectOffset margins, EditorWindow currentInspector)
	{
		if (currentInspector)
			this.currentInspector = currentInspector;
		
		if (Event.current.type == EventType.KeyDown || Event.current.type == EventType.KeyUp)
		{
			if (TabSwitcher.OnGUIGlobal())
				return;
		}
		
#if !UNITY_2019_1_OR_NEWER
		if (!isScriptInspector)
		{
			OnWindowGUI(currentInspector, margins);
			return;
		}
#endif
		if (!this.currentInspector)
		{
			return;
		}
		
#if !UNITY_2019_1_OR_NEWER
		// Disabling the functionality of the default inspector's header help button
		// (located just below the cancel search button) by zeroing hotControl on
		// mouse down, which effectivly deactivates the button so it doesn't fire up
		// on mouse up. Detection is done by comparing hotControl with the next available
		// controlID - 2, which is super-hacky, but so far I haven't found any nicer way
		// of doing this.
		int nextControlID = GUIUtility.GetControlID(buttonHash, FocusType.Passive, new Rect());
		if (GUIUtility.hotControl != 0)
		{
			//Debug.Log("hotControl: " + GUIUtility.hotControl + "  nextControlID: " + nextControlID + "  Event: " + Event.current);
			if (GUIUtility.hotControl >= nextControlID - 3 && GUIUtility.hotControl <= nextControlID - 2)
			{
				GUIUtility.hotControl = 0;
				clearSearchButtonClicked = true;
			//	Repaint();
			}
		}
#endif

		var height = GetMaxHeightInInspector();

		if (Event.current.type != EventType.Layout)
		{
			scrollViewRect = GUILayoutUtility.GetRect(1f, this.currentInspector.position.width, 1f, height);
			
			scrollViewRect.xMin = 0f;
			scrollViewRect.xMax += isTextAsset ? 0f : 4f;
#if UNITY_2019_1_OR_NEWER
#if UNITY_2020_1_OR_NEWER
			scrollViewRect.yMin += isTextAsset ? 19f : 15f;
#else
			scrollViewRect.yMin += isTextAsset ? 16f : 12f;
#endif
			scrollViewRect.yMax += isTextAsset ? 0f : 6f;
#else
			scrollViewRect.yMin -= 30f;
			scrollViewRect.yMax += 13f;
#endif
			if (isShader)
			{
				scrollViewRect.yMin += 7f;
			}
		}
		else
		{
#if UNITY_2019_1_OR_NEWER
			GUILayoutUtility.GetRect(1f, this.currentInspector.position.width, 1f, height);
#else
			GUILayoutUtility.GetRect(1f, Screen.width, 1f, Screen.height);
#endif
		}

		bool enabled = GUI.enabled;
		GUI.enabled = CanEdit();

		Color oldColor = GUI.color;
		if (!GUI.enabled && Event.current.type == EventType.Repaint)
		{
			//GUI.color = new Color(0.85f, 0.85f, 0.85f);
			//if (textBuffer != null)
			//	textBuffer.LoadFaster();
		}
		GUI.color = Color.white;
		
		try
		{
			if (hasCodeViewFocus)
			{
				FGTextBuffer.activeEditor = this;
	
				textBuffer.BeginEdit("change");
				try
				{
					DoGUIWithAutocomplete(enabled);
				}
				finally
				{
					textBuffer.EndEdit();
				}
			}
			else
			{
				DoGUIWithAutocomplete(enabled);
			}
		}
		finally
		{
			GUI.color = oldColor;
			GUI.enabled = enabled;
		}
	}

	public Rect codeViewRect;
	private Rect fullCodeViewRect;
	private static int codeViewControlID = Mathf.Abs("SiCodeView".GetHashCode());
	
	private Event lastMouseEvent;
	private bool simulateLastMouseEvent;

	private bool tryAutocomplete;
	private bool tryAutoSuggestion;
	private bool tryArgumentsHint;
	
	private string lastTypedText;
	[NonSerialized]
	private bool tempIgnoreSmartSemicolon;

	private void DoGUIWithAutocomplete(bool enableGUI)
	{
		FGTextBuffer.tabSize = SISettings.tabSize;
		wordWrapping = WordWrapping;
		
		if (caretMoveTime == default(System.DateTime))
		{
			caretMoveTime = frameTime;
			lastCaretMoveWasSearch = false;
//			if (EditorWindow.focusedWindow == parentWindow)
//				parentWindow.Focus();
		}
		
		tryEditState = TryEditState.Ask;

		if (tryAutocomplete)
		{
			Autocomplete(tryAutoSuggestion);
			tryAutoSuggestion = false;
			tryAutocomplete = false;
			Repaint();
		}
		else
		{
			if (autocompleteWindow == null)
			{
				try
				{
					autocompleteInitialized = false;
					DoGUI(enableGUI);
				}
				catch (ExitGUIException) { }
			}
			else
			{
				FGTextBuffer.CaretPos caretPosBefore = caretPosition.Clone();
				try
				{
					DoGUI(enableGUI);
				}
				catch (ExitGUIException) { }

				if (autocompleteWindow != null)
				{
					if (!hasCodeViewFocus && EditorWindow.focusedWindow != autocompleteWindow)
					{
						//	Debug.Log("Closing. Focus lost...");
						CloseAutocomplete();
					}
					else if (caretPosition.line == caretPosBefore.line && caretPosition != caretPosBefore)
					{
						FGListPopup.UpdateTypedInPart();
						autocompleteWindow.Repaint();
					}
					
#if UNITY_EDITOR_OSX
					if (autocompleteWindow != null && Event.current.type == EventType.Repaint)
					{
						Vector2 offset = new Vector2(autocompleteWindow.position.x, autocompleteWindow.position.y);
						if (offset.x != 0f || offset.y != 0f)
						{
							offset = GUIUtility.ScreenToGUIPoint(offset);
							autocompleteWindow.OnExternalGUI(offset);
						}
						else
						{
							OwnerWindow.Repaint();
						}
					}
#endif
				}

#if UNITY_EDITOR_OSX
				if (Event.current.type == EventType.Repaint)
				{
					if (tokenTooltip != null)
					{
						Vector2 offset = new Vector2(tokenTooltip.position.x, tokenTooltip.position.y);
						if (offset.x != 0f || offset.y != 0f)
						{
							offset = GUIUtility.ScreenToGUIPoint(offset);
							tokenTooltip.OnExternalGUI(offset);
						}
						else
						{
							tokenTooltip.Repaint();
						}
					}

					if (argumentsHint != null)
					{
						Vector2 offset = new Vector2(argumentsHint.position.x, argumentsHint.position.y);
						if (offset.x != 0f || offset.y != 0f)
						{
							offset = GUIUtility.ScreenToGUIPoint(offset);
							argumentsHint.OnExternalGUI(offset);
						}
						else
						{
							argumentsHint.Repaint();
						}
					}
				}
#endif
			}

			if (Event.current.type == EventType.KeyDown)
			{
				if (MightBePrintableKey(Event.current))
					Event.current.Use();
			}
		}
		
		//FillRect(new Rect(Event.current.mousePosition.x - 1f, Event.current.mousePosition.y - 1f, 3f, 3f), Color.black);
		
		if (argumentsHint != null)
		{
			if (!hasCodeViewFocus && EditorWindow.focusedWindow != argumentsHint)
			{
				//	Debug.Log("Closing. Focus lost...");
				CloseArgumentsHint();
				Repaint();
			}
		}
		
		if (textBuffer == null)
			return;
		
		if (Event.current.type == EventType.Repaint)
			lastCodeViewRect = codeViewRect;
		
		if (caretPosition != matchedBracesAtCaretPosition || scrollToCaret || caretMoveTime == frameTime)
		{
			if (showingArgumentsForMethod != null)
			{
				UpdateArgumentsHint(false);
			}
			
			if (autoClosingStack.Count > 0)
			{
				var caretPos = new TextPosition(caretPosition.line, caretPosition.characterIndex);
				for (var i = autoClosingStack.Count; i --> 0; )
				{
					var entry = autoClosingStack[i];
					if (caretPos <= entry.openingPos || caretPos > entry.closingPos)
						autoClosingStack.RemoveAt(i);
				}
				if (autoClosingStack.Count == 0)
				{
					textBuffer.onInsertedText -= OnInsertedText;
					textBuffer.onRemovedText -= OnRemovedText;
				}
			}
		}
		
		if (caretPosition != matchedBracesAtCaretPosition || textBuffer.undoPosition != matchedBracesAtUndoPosition)
		{
			if (selectionStartPosition == null && !IsLoading)
			{
				UpdateMatchingBraces();
				matchedBracesAtUndoPosition = textBuffer.undoPosition;
				matchedBracesAtCaretPosition = caretPosition.Clone();
			}
		}
	}
	
	private static bool MightBePrintableKey(Event evt)
	{
		if (evt.command || evt.control)
			return false;
		if (evt.alt)
		{
			return false;
		}
		if (evt.keyCode >= KeyCode.Mouse0 && evt.keyCode <= KeyCode.Mouse6)
			return false;
		if (evt.keyCode >= KeyCode.JoystickButton0)
			return false;
		if (evt.keyCode >= KeyCode.F1 && evt.keyCode <= KeyCode.F15)
			return false;
		switch (evt.keyCode)
		{
			case KeyCode.AltGr:
			case KeyCode.Backspace:
			case KeyCode.Break:
			case KeyCode.CapsLock:
			case KeyCode.Clear:
			case KeyCode.Delete:
			case KeyCode.DownArrow:
			case KeyCode.End:
			case KeyCode.Escape:
			case KeyCode.Help:
			case KeyCode.Home:
			case KeyCode.Insert:
			case KeyCode.LeftAlt:
			case KeyCode.LeftArrow:
			case KeyCode.LeftCommand: // same as LeftApple
			case KeyCode.LeftControl:
			case KeyCode.LeftShift:
			case KeyCode.LeftWindows:
			case KeyCode.Menu:
			case KeyCode.Numlock:
			case KeyCode.PageDown:
			case KeyCode.PageUp:
			case KeyCode.Pause:
			case KeyCode.Print:
			case KeyCode.RightAlt:
			case KeyCode.RightArrow:
			case KeyCode.RightCommand: // same as RightApple
			case KeyCode.RightControl:
			case KeyCode.RightShift:
			case KeyCode.RightWindows:
			case KeyCode.ScrollLock:
			case KeyCode.SysReq:
			case KeyCode.UpArrow:
				return false;
			case KeyCode.None:
				return evt.character != 0;
			default:
				return true;
		}
	}

	private void UpdateMatchingBracesFromParseTree(ParseTree.BaseNode treeNode)
	{
		for ( ; treeNode != null && treeNode.grammarNode != null; )
		{
			var parent = treeNode.parent;
			if (parent == null)
				break;
			
			for (var grammarNode = treeNode.grammarNode.parent; grammarNode != null && !(grammarNode is FGGrammar.Rule); grammarNode = grammarNode.parent)
			{
				var asSeq = grammarNode as FGGrammar.Seq;
				if (asSeq == null)
					continue;

				if (!asSeq.hasBraces)
					continue;

				var closingBrace = '\0';
				var index = treeNode.childIndex;
				ParseTree.Leaf braceNodeLeft = null, braceNodeRight = null;
				
				for (var childNode = parent.firstChild; childNode != null && childNode.childIndex <= index; childNode = childNode.nextSibling)
				{
					var asLeaf = childNode as ParseTree.Leaf;
					if (asLeaf != null && asLeaf.grammarNode.parent == asSeq)
					{
						var token = asLeaf.token;
						if (token.tokenKind == SyntaxToken.Kind.Punctuator && token.text.length == 1)
						{
							var c = token.text[0];
							if (c == '(')
							{
								closingBrace = ')';
								braceNodeLeft = asLeaf;
							}
							else if (c == '{')
							{
								closingBrace = '}';
								braceNodeLeft = asLeaf;
							}
							else if (c == '[')
							{
								closingBrace = ']';
								braceNodeLeft = asLeaf;
							}
						}
					}
				}
				
				if (braceNodeLeft != null)
				{
					var numValidNodes = parent.numValidNodes;
					for (var childNode = treeNode; childNode != null && childNode.childIndex < numValidNodes; childNode = childNode.nextSibling)
					{
						var asLeaf = childNode as ParseTree.Leaf;
						if (asLeaf == null || asLeaf.grammarNode.parent != asSeq)
							continue;

						var token = asLeaf.token;
						if (token.tokenKind != SyntaxToken.Kind.Punctuator || token.text.length != 1)
							continue;

						var c = token.text[0];
						if (c != closingBrace)
							continue;

						braceNodeRight = asLeaf;
						break;
					}
					
					if (braceNodeRight != null)
					{
						matchingBraceLeft.line = braceNodeLeft.line;
						matchingBraceLeft.index = braceNodeLeft.tokenIndex;
						matchingBraceRight.line = braceNodeRight.line;
						matchingBraceRight.index = braceNodeRight.tokenIndex;

						return;
					}
				}
			}
			
			treeNode = parent;
		}
	}
	
	private void UpdateMatchingBraces()
	{
		matchingBraceLeft = matchingBraceRight = TextPosition.invalid;
		
		int line, index;
		bool atTokenEnd;
		var tokenAtCursor = textBuffer.GetTokenAt(caretPosition, out line, out index, out atTokenEnd);

		var tokenIndex = index;
		var tokenLine = line;

		var formattedLines = textBuffer.formattedLines;
		if (line >= formattedLines.Count)
			return;
		
		var tokens = formattedLines[line].tokens;
		if (tokens == null)
			return;
		
		if (tokenAtCursor != null)
		{
			char c;
		
			if ((atTokenEnd || caretPosition.characterIndex == 0) && tokenAtCursor.text.length == 1 &&
				((c = tokenAtCursor.text[0]) == '}' || c == ']' || c == ')'))
			{
				var nextToken = index + 1 < tokens.Count ? tokens[index + 1] : null;
				if (nextToken == null || nextToken.text.length != 1 ||
					(c = nextToken.text[0]) != '}' && c != ']' && c != ')')
				{
					--index;
				}
				else
                {
					tokenAtCursor = nextToken;
					++tokenIndex;
                }
			}
			else if (atTokenEnd)
			{
				if (tokenAtCursor.text.length != 1 ||
					(c = tokenAtCursor.text[0]) != '{' && c != '[' && c != '(')
				{
					var nextToken = index + 1 < tokens.Count ? tokens[index + 1] : null;
					if (nextToken != null && nextToken.text.length == 1 &&
						((c = nextToken.text[0]) == '{' || c == '[' || c == '('))
					{
						tokenAtCursor = nextToken;
						++tokenIndex;
						++index;
					}
				}
			}
		}

		ParseTree.BaseNode node = null;
		if (tokenAtCursor != null)
			node = tokenAtCursor.parent;

		if (node == null)
        {
			while (tokenAtCursor == null || tokenAtCursor.parent == null)
			{
				if (tokenIndex > 0)
				{
					--tokenIndex;
				}
				else if (--tokenLine >= 0)
				{
					tokens = formattedLines[tokenLine].tokens;
					tokenIndex = tokens.Count - 1;
				}
				else
				{
					break;
				}

				if (tokenIndex >= 0)
					tokenAtCursor = tokens[tokenIndex];
			}

			if (tokenAtCursor != null && tokenAtCursor.parent != null)
			{
				char c;
				if (tokenAtCursor.text.length == 1 && ((c = tokenAtCursor.text[0]) == ')' || c == '}' || c == ']'))
					node = tokenAtCursor.parent.parent;
				else
					node = tokenAtCursor.parent;
			}
        }

		if (node != null)
		{
			UpdateMatchingBracesFromParseTree(node);
			return;
		}

		matchingBraceLeft = textBuffer.GetOpeningBraceLeftOf(line, index, -1);
		if (matchingBraceLeft.line >= 0)
			matchingBraceRight = textBuffer.GetClosingBraceRightOf(line, index, -1);
	}
	
	private static List<FGTextBuffer.CollapsibleTextSpan> tempCollapsibleTextSpans = new List<FGTextBuffer.CollapsibleTextSpan>();
	
	void FindCollapsibleTextSpansAtLines(int fromLine, int toLine)
	{
		tempCollapsibleTextSpans.Clear();
		if (enableCodeFolding && textBuffer.collapsibleTextSpansTree != null)
		{
			var textInterval = new TextInterval(new TextPosition(fromLine, -1), new TextPosition(toLine, int.MaxValue));
			textBuffer.collapsibleTextSpansTree.GetIntervalsOverlappingWith(textInterval, tempCollapsibleTextSpans);
		}
	}

	private static List<FGTextBuffer.CollapsibleTextSpan> tempOverlappingSpans = new List<FGTextBuffer.CollapsibleTextSpan>();
	
	private FGTextBuffer.CollapsibleTextSpan GetCollapsedSpanForTextSpan(TextSpan span)
	{
		tempOverlappingSpans.Clear();
		
		if (!enableCodeFolding || textBuffer.collapsibleTextSpansTree == null)
			return null;
		
		var charInterval = new TextInterval(span.StartPosition, span.EndPosition);
		textBuffer.collapsibleTextSpansTree.GetIntervalsOverlappingWith(charInterval, tempOverlappingSpans);
		
		foreach (var item in tempOverlappingSpans)
			if (item.IsCollapsed)
				return item;
		
		return null;
	}
	
	public void UnfoldTextRange(int fromLine, int fromCharIndex, int toLine, int toCharIndex)
	{
		tempOverlappingSpans.Clear();

		enableCodeFolding = CacheEnableCodeFolding();
		if (!enableCodeFolding || textBuffer.collapsibleTextSpansTree == null)
			return;
		
		var startPos = new TextPosition{line = fromLine, index = fromCharIndex - 1};
		var endPos = new TextPosition{line = toLine, index = toCharIndex + 1};
		var charInterval = new TextInterval{ Start = startPos, End = endPos };
		textBuffer.collapsibleTextSpansTree.GetIntervalsOverlappingWith(charInterval, tempOverlappingSpans);

		startPos.index++;
		endPos.index--;
		
		var numSpans = tempOverlappingSpans.Count;
		for (var i = 0; i < numSpans; ++i)
		{
			var span = tempOverlappingSpans[i];
			if (!span.IsCollapsed)
				continue;
			if (span.span.Start == endPos || span.span.End == startPos)
				continue;

			ToggleFolding(span);
		}
	}
	
	private FGTextBuffer.CollapsibleTextSpan GetCharCollapsedSpan(int line, int index)
	{
		if (!enableCodeFolding || textBuffer.collapsibleTextSpansTree == null)
			return null;
		
		tempOverlappingSpans.Clear();
		
		var charInterval = new TextInterval{
			Start = new TextPosition{line = line, index = index},
			End = new TextPosition{line = line, index = index + 1} };
		textBuffer.collapsibleTextSpansTree.GetIntervalsOverlappingWith(charInterval, tempOverlappingSpans);
		
		var numSpans = tempOverlappingSpans.Count;
		for (var i = 0; i < numSpans; ++i)
		{
			var span = tempOverlappingSpans[i];
			if (span.IsCollapsed)
				return span;
		}
		
		return null;
	}
	
	private bool IsLineBeginningCollapsed(int line)
	{
		var overlappingSpan = GetCharCollapsedSpan(line, -1);
		return overlappingSpan != null;
	}
	
	private int BufferToEditorLine(int line)
	{
		if (line == 0)
			return 0;
		if (!enableCodeFolding)
			return line;
		
		var overlappingCollapsedSpan = GetCharCollapsedSpan(line, -1);
		while (overlappingCollapsedSpan != null)
		{
			line = overlappingCollapsedSpan.span.Start.line;
			if (line == 0)
				return 0;
			overlappingCollapsedSpan = GetCharCollapsedSpan(line, -1);
		}
		return line;
	}
	
	private FGTextBuffer.FormattedLine tempFormattedLine =
		new FGTextBuffer.FormattedLine()
		{
			tokens = new List<SyntaxToken>()
		};
	
	[NonSerialized]
	private List<SyntaxToken> tempSingleTokens = new List<SyntaxToken>();
	[NonSerialized]
	private int nextTempSingleToken;
	
	private SyntaxToken GetTempTokenForLine(int line)
	{
		var tempToken = GetTempToken();
		if (line < textBuffer.lines.Count)
		{
			tempToken.text = textBuffer.lines[line];
		}
		else
		{
			tempToken.text = "";
		}
		return tempToken;
	}
	
	private SyntaxToken GetTempToken()
	{
		if (nextTempSingleToken < tempSingleTokens.Count)
			return tempSingleTokens[nextTempSingleToken++];

		var tempToken = new SyntaxToken(SyntaxToken.Kind.PreprocessorArguments, "");
		tempSingleTokens.Add(tempToken);
		return tempToken;
	}
	
	[NonSerialized]
	private List<SyntaxToken> tempTokens = new List<SyntaxToken>();
	
	EditorLine CreateEditorLine(int line)
	{
		var editorLine = CreateEditorLineTokens(line);
		
		var numTokens = editorLine.tokens.Count;
		if (editorLine.lineTokens.Capacity < numTokens)
		{
			editorLine.lineTokens.Capacity = numTokens;
			editorLine.lineTexts.Capacity = numTokens;
			editorLine.lineRects.Capacity = numTokens;
			editorLine.lineTokenStartPos.Capacity = numTokens + 1;
			editorLine.collapsedTokenSpans.Capacity = numTokens;
		}
		
		return editorLine;
	}
	
	EditorLine CreateEditorLineTokens(int line)
	{
		if (line < textBuffer.formattedLines.Count)
		{
			var formattedLineTokens = textBuffer.formattedLines[line].tokens;
			if (formattedLineTokens != null)
			{
				var editorLine = EditorLine.Create(formattedLineTokens.Count);
				
				editorLine.lineFrom = line;
				editorLine.lineTo = line;

				if (!enableCodeFolding)
				{
					editorLine.tokens = formattedLineTokens;
				}
				else
				{
					var spanIndex = textBuffer.IndexOfFirstCollapsibleTextSpanAtLine(line);
					if (spanIndex < 0)
					{
						editorLine.tokens = formattedLineTokens;
					}
					else if (!GetLineTokensInternal(editorLine.tokens, line, ref spanIndex, editorLine.collapsedTokenSpans))
					{
						editorLine.tokens = formattedLineTokens;
					}
					else
					{
						var editorLineTokens = editorLine.tokens;
						var lastIndex = editorLineTokens.Count - 1;
							var lastToken = editorLineTokens[lastIndex];
						if (lastToken.tokenKind != SyntaxToken.Kind.Collapsed)
						{
							editorLine.lineTo = lastToken.Line;
						}
						else
						{
							var span = editorLine.collapsedTokenSpans[lastIndex];
							editorLine.lineTo = span.span.End.line;
						}
					}
				}
			
				return editorLine;
			}
		}

		var tempTokenForLine = GetTempTokenForLine(line);
		var plainLine = EditorLine.Create(1);
		plainLine.lineFrom = line;
		plainLine.lineTo = line;
		plainLine.tokens.Add(tempTokenForLine);
		return plainLine;
	}
	
	List<SyntaxToken> GetLineTokens(int line, List<FGTextBuffer.CollapsibleTextSpan> collapsedTokenSpans = null)
	{
		if (collapsedTokenSpans != null)
			collapsedTokenSpans.Clear();
		
		var formattedLines = textBuffer.formattedLines;
		
		if (line >= formattedLines.Count)
			return null;
		
		var formattedLineTokens = formattedLines[line].tokens;
		if (formattedLineTokens == null)
			return null;
		
		if (!enableCodeFolding)
			return formattedLineTokens;

		var spanIndex = textBuffer.IndexOfFirstCollapsibleTextSpanAtLine(line);
		if (spanIndex < 0)
			return formattedLineTokens;
		
		if (!GetLineTokensInternal(tempTokens, line, ref spanIndex, collapsedTokenSpans))
			return formattedLineTokens;
		
		return tempTokens;
	}
	
	static Dictionary<string, SyntaxToken> sharedCollapsedTokens = new Dictionary<string, SyntaxToken>();
	
	bool GetLineTokensInternal(List<SyntaxToken> lineTokens, int line, ref int nextCollapsibleTextSpanIndex, List<FGTextBuffer.CollapsibleTextSpan> collapsedTokenSpans = null)
	{
		var numSpans = textBuffer.collapsibleTextSpans.Count;
		var nextSpan = nextCollapsibleTextSpanIndex >= numSpans ? null
			: textBuffer.collapsibleTextSpans[nextCollapsibleTextSpanIndex];
		while (nextSpan != null)
		{
			if (nextSpan.span.Start.line > line)
			{
				nextSpan = null;
				break;
			}

			if (nextSpan.IsCollapsed && nextSpan.span.Start.line == line)
				break;

			if (++nextCollapsibleTextSpanIndex == numSpans)
			{
				nextSpan = null;
				break;
			}
			else
			{
				nextSpan = textBuffer.collapsibleTextSpans[nextCollapsibleTextSpanIndex];
			}
		}
		
		var formattedLines = textBuffer.formattedLines;
		var numFormattedLines = formattedLines.Count;

		if (line >= numFormattedLines)
		{
			return false;
		}
		var currentFormattedLine = formattedLines[line];
		if (currentFormattedLine == null)
		{
			return false;
		}

		if (nextSpan == null)
		{
			return false;
		}
		
		var nextSpanCharIndex = nextSpan.span.Start.index;
		
		lineTokens.Clear();
		
		SyntaxToken token = null;
		var tokenTextStartAt = 0;
		var charIndex = 0;
		var currentTokens = currentFormattedLine.tokens;
		if (currentTokens == null)
		{
			return false;
		}
				
		var numCurrentTokens = currentTokens.Count;

		for (var i = 0; i <= numCurrentTokens; ++i)
		{
			if (i < numCurrentTokens)
			{
				token = currentTokens[i];
				if (token == null)
					continue;
			
				var tokenLength = token.text.length - tokenTextStartAt;
				if (nextSpan == null || charIndex + tokenLength <= nextSpanCharIndex)
				{
					charIndex += tokenLength;
					if (tokenTextStartAt == 0)
					{
						lineTokens.Add(token);
					}
					else
					{
						var partialToken = new PartialSyntaxToken(token, token.text.Substring(tokenTextStartAt));
						lineTokens.Add(partialToken);
						tokenTextStartAt = 0;
					}
					if (collapsedTokenSpans != null)
					{
						collapsedTokenSpans.Add(null);
					}
					continue;
				}
			
				if (charIndex < nextSpanCharIndex)
				{
					var partialToken = new PartialSyntaxToken(token, token.text.Substring(tokenTextStartAt, nextSpanCharIndex - charIndex));
					lineTokens.Add(partialToken);
					if (collapsedTokenSpans != null)
					{
						collapsedTokenSpans.Add(null);
					}
					tokenTextStartAt = 0;
				}
			}
			else if (nextSpan == null)
			{
				break;
			}
			else
			{
				token = null;
			}
			
			if (string.IsNullOrEmpty(nextSpan.text))
				nextSpan.text = "...";
			
			SyntaxToken sharedCollapsedToken;
			if (!sharedCollapsedTokens.TryGetValue(nextSpan.text, out sharedCollapsedToken))
			{
				sharedCollapsedToken = new SyntaxToken(SyntaxToken.Kind.Collapsed, nextSpan.text);
				sharedCollapsedTokens.Add(nextSpan.text, sharedCollapsedToken);
			}
			lineTokens.Add(sharedCollapsedToken);
			
			if (collapsedTokenSpans != null)
			{
				collapsedTokenSpans.Add(nextSpan);
			}
			
			line = nextSpan.span.End.line;
			if (line >= numFormattedLines)
			{
				break;
			}
			else
			{
				currentFormattedLine = formattedLines[line];
				currentTokens = currentFormattedLine.tokens;
				numCurrentTokens = currentTokens != null ? currentTokens.Count : 0;
			}
			
			charIndex = nextSpan.span.End.index;
			
			var numChars = 0;
			var nextTokenIndex = 0;
			for ( ; nextTokenIndex < numCurrentTokens; ++nextTokenIndex)
			{
				token = currentTokens[nextTokenIndex];
				if (token == null)
					continue;
				
				numChars += token.text.length;
				if (numChars >= charIndex)
					break;
			}
			
			nextSpan = ++nextCollapsibleTextSpanIndex >= numSpans ? null
				: textBuffer.collapsibleTextSpans[nextCollapsibleTextSpanIndex];
			while (nextSpan != null)
			{
				if (nextSpan.span.Start.line > line)
				{
					nextSpan = null;
					break;
				}

				if (nextSpan.IsCollapsed && nextSpan.span.Start.line == line && nextSpan.span.Start.index >= charIndex)
					break;

				if (++nextCollapsibleTextSpanIndex == numSpans)
				{
					nextSpan = null;
					break;
				}
				else
				{
					nextSpan = textBuffer.collapsibleTextSpans[nextCollapsibleTextSpanIndex];
				}
			}

			nextSpanCharIndex = nextSpan == null ? int.MaxValue : nextSpan.span.Start.index;
			
			if (numChars == charIndex || token == null)
			{
				i = nextTokenIndex;
			}
			else
			{
				i = nextTokenIndex - 1;
				tokenTextStartAt = token.text.length - (numChars - charIndex);
			}
		}
		
		return true;
	}
	
	[NonSerialized]
	private List<List<int>> _softLineBreaks;
	private static readonly List<int> NO_SOFT_LINE_BREAKS = new List<int>();
	
	//[NonSerialized]
	//private List<int> lineHeights = new List<int>();
	
	private class BinaryIndexTree
	{
		public int size;
		public int[] tree;
		
		public BinaryIndexTree()
		{
			size = 0;
			tree = new int[256];
		}
		
		public void SetSize(int newSize)
		{
			if (newSize < size)
			{
				for (int i = newSize + 1; i <= size; ++i)
				{
					tree[i] = 0;
				}
				size = newSize;
			}
			else if (newSize > size)
			{
				var minTreeSize = newSize + 1;
				var treeSize = tree.Length;
				if (minTreeSize > treeSize)
				{
					var newTreeSize = (minTreeSize + 255) & ~255;
					var newTree = new int[newTreeSize];
					for (int i = 0; i <= size; ++i)
					{
						newTree[i] = tree[i];
					}
					tree = newTree;
					size = newSize;
				}
			}
		}
		
		public void Add(int value)
		{
			SetSize(size + 1);
			Update(size - 1, value);
		}
		
		public void Update(int index, int delta)
		{
			++index;
			while (index <= size)
			{
				tree[index] += delta;
				index += index & -index;
			}
		}
		
		public int Query(int index)
		{
			++index;
			int sum = 0;
			while (index > 0)
			{
				sum += tree[index];
				index -= index & -index;
			}
			return sum;
		}
	}
	
	private List<int> GetSoftLineBreaks(int line)
	{
		var result = GetSoftLineBreaksInternal(line);
		
		//int numLineHeights = lineHeights.Count;
		//for (int i = numLineHeights; i < line; ++i)
		//{
		//	lineHeights.Add(1);
		//}
		//numLineHeights = lineHeights.Count;
		
		//var thisLineHeight = result.Count + 1;
		//if (numLineHeights == line)
		//{
		//	lineHeights.Add(thisLineHeight);
		//}
		//else
		//{
		//	var delta = thisLineHeight - lineHeights[line];
		//	if (delta != 0)
		//	{
		//		lineHeights[line] = thisLineHeight;
		//	}
		//}

		if (yLineOffsets != null && line + 1 < yLineOffsets.Count)
		{
			int y = yLineOffsets[line];
			int next = y + (result.Count + 1);
			FGTextBuffer.CollapsibleTextSpan overlappingSpan;
			while ((overlappingSpan = GetCharCollapsedSpan(line + 1, -1)) != null) //IsLineBeginningCollapsed(line + 1))
			{
				//++line;
				line = overlappingSpan.span.End.line;
			}
			if (line + 1 < yLineOffsets.Count)
			{
				y = next - yLineOffsets[line + 1];
				if (y != 0)
				{
					for (int i = line + 1; i < yLineOffsets.Count; ++i)
						yLineOffsets[i] += y;
				}
			}
		}

		return result;
	}

	private List<int> GetSoftLineBreaksInternal(int line)
	{
		if (!wordWrapping /*|| IsLoading*/)
			return NO_SOFT_LINE_BREAKS;

		if (_softLineBreaks == null)
			_softLineBreaks = new List<List<int>>(textBuffer.lines.Count);
		if (line < _softLineBreaks.Count && _softLineBreaks[line] != null)
			return _softLineBreaks[line];

		if (line >= _softLineBreaks.Count)
		{
			if (_softLineBreaks.Capacity < textBuffer.lines.Count)
				_softLineBreaks.Capacity = textBuffer.lines.Count + 32;
			for (int i = _softLineBreaks.Count; i < textBuffer.lines.Count; ++i)
				_softLineBreaks.Add(null);
		}

		if (charSize.x == 0 || charSize.x * 2f > codeViewRect.width)
			return NO_SOFT_LINE_BREAKS;
		
		if (line >= textBuffer.formattedLines.Count)
			return NO_SOFT_LINE_BREAKS;
		
		var tokens = GetLineTokens(line, tempCollapsedTokenSpans);
		if (tokens == null)
			return NO_SOFT_LINE_BREAKS;
		
		var isLineBeginningCollapsed = IsLineBeginningCollapsed(line);
		if (isLineBeginningCollapsed)
		{
			Debug.LogError("Calling GetSoftLineBreaks() for a line with collapsed beginning!");
			return NO_SOFT_LINE_BREAKS;
		}
		
		_softLineBreaks[line] = NO_SOFT_LINE_BREAKS;
		var lineBreaks = NO_SOFT_LINE_BREAKS;
		var maxWidth = codeViewRect.width;
		maxWidth = maxWidth < 8f * charSize.x ? 8f * charSize.x : maxWidth;

		//var tabSize = SISettings.tabSize;
		//lastTabSize = tabSize;
		//needsReformat = false;
		
		int charIndex = 0;
		int breakCharIndex = 0;
		int lineIndex = line;
		var xOffset = 0f;
		var numTokens = tokens.Count;
		for (var i = 0; i < numTokens; ++i)
		{
			var token = tokens[i];
			if (token == null)
				continue;

			var tokenLength = token.text.length;
			if (tokenLength == 0)
				continue;
			
			var isBreakableToken =
				token.tokenKind <= SyntaxToken.Kind.Comment ||
				token.tokenKind == SyntaxToken.Kind.PreprocessorArguments ||
				token.tokenKind == SyntaxToken.Kind.VerbatimStringLiteral ||
				token.tokenKind <= SyntaxToken.Kind.InterpolatedStringEndLiteral
				&& token.tokenKind >= SyntaxToken.Kind.StringLiteral;
			
			if (!isBreakableToken)
			{
				// Non-breakable token

				if (token.tokenKind == SyntaxToken.Kind.Collapsed)
				{
					var collapsedWidth = GetTextWidth(token.text.ToString(), 0, tokenLength, xOffset);
					var spanEnd = tempCollapsedTokenSpans[i].span.End;

					if (xOffset + collapsedWidth < maxWidth)
					{
						breakCharIndex += tokenLength;
						charIndex = spanEnd.index;
						lineIndex = spanEnd.line;
						xOffset += collapsedWidth;
						continue;
					}

					// Doesn't fit in current row

					if (lineBreaks == NO_SOFT_LINE_BREAKS)
						_softLineBreaks[line] = lineBreaks = new List<int>();

					if (xOffset > 0f)
					{
						lineBreaks.Add(breakCharIndex);
						//xOffset = 0f;
					}

					breakCharIndex += tokenLength;
					charIndex = spanEnd.index;
					lineIndex = spanEnd.line;
					xOffset = collapsedWidth;

					continue;
				}

				var tokenWidth = GetTextWidth(lineIndex, charIndex, charIndex + tokenLength, xOffset);
				if (xOffset + tokenWidth < maxWidth)
				{
					breakCharIndex += tokenLength;
					charIndex += tokenLength;
					xOffset += tokenWidth;
					continue;
				}

				// Doesn't fit in current row

				if (lineBreaks == NO_SOFT_LINE_BREAKS)
					_softLineBreaks[line] = lineBreaks = new List<int>();

				if (xOffset > 0f)
				{
					lineBreaks.Add(breakCharIndex);
					//xOffset = 0f;
				}

				if (tokenWidth > maxWidth)
				{
					// Doesn't fit in a single row
					int rowWidth = (int)(maxWidth / charSize.x);
					
					int j;
					for (j = rowWidth; j < tokenLength; j += rowWidth)
						lineBreaks.Add(breakCharIndex + j);
					xOffset = GetTextWidth(lineIndex, charIndex + j, charIndex + tokenLength, 0f);
				}
				else
				{
					xOffset = tokenWidth;
				}

				breakCharIndex += tokenLength;
				charIndex += tokenLength;
			}
			else
			{
				// Breakable token or whitespaces

				var lastLineBreak = xOffset > 0f ? -1 : 0;
				var lastWhitespace = -1;
				for (var j = 0; j < tokenLength; ++j)
				{
					var c = token.text[j];
					xOffset += GetCharWidth(c, xOffset);
					if (c == ' ' || c == '\t')
						lastWhitespace = j;
					if (xOffset >= maxWidth)
					{
						if (lineBreaks == NO_SOFT_LINE_BREAKS)
							lineBreaks = _softLineBreaks[line] = new List<int>();

						if (lastWhitespace >= lastLineBreak)
						{
							if (j == lastWhitespace)
								lastLineBreak = lastWhitespace;
							else
								lastLineBreak = lastWhitespace + 1;
							lineBreaks.Add(breakCharIndex + lastLineBreak);
							lastWhitespace = -1;
						}
						else if (lastLineBreak >= 0)
						{
							lastLineBreak = j;
							lineBreaks.Add(breakCharIndex + lastLineBreak);
						}
						else
						{
							lineBreaks.Add(breakCharIndex);
						}
						xOffset = GetTextWidth(line, charIndex + lastLineBreak, charIndex + j, 0f);
					}
				}

				breakCharIndex += tokenLength;
				charIndex += tokenLength;
			}
		}

		return lineBreaks;
	}
	
#if UNITY_5_3 || UNITY_5_4_OR_NEWER
	internal sealed class ScrollViewState
	{
		public Rect position;
		public Rect visibleRect;
		public Rect viewRect;
		public Vector2 scrollPosition;
		public bool apply;
		
		internal void ScrollTo(Rect position)
		{
			ScrollTowards(position, float.PositiveInfinity);
		}
		
		internal bool ScrollTowards(Rect position, float maxDelta)
		{
			var b = this.ScrollNeeded(position);
			if (b.sqrMagnitude < 0.0001f)
				return false;
	
			if (maxDelta == 0f)
				return true;
	
			if (b.magnitude > maxDelta)
				b = b.normalized * maxDelta;
			scrollPosition += b;
			apply = true;
			return true;
		}
		
		internal Vector2 ScrollNeeded(Rect position)
		{
			var rect = visibleRect;
			rect.x += scrollPosition.x;
			rect.y += scrollPosition.y;
			var delta = position.width - visibleRect.width;
			if (delta > 0f)
			{
				position.width -= delta;
				position.x += delta * 0.5f;
			}
			delta = position.height - visibleRect.height;
			if (delta > 0f)
			{
				position.height -= delta;
				position.y += delta * 0.5f;
			}
	
			var result = Vector2.zero;
			if (position.xMax > rect.xMax)
			{
				result.x += position.xMax - rect.xMax;
			}
			else if (position.xMin < rect.xMin)
			{
				result.x -= rect.xMin - position.xMin;
			}
			if (position.yMax > rect.yMax)
			{
				result.y += position.yMax - rect.yMax;
			}
			else if (position.yMin < rect.yMin)
			{
				result.y -= rect.yMin - position.yMin;
			}
	
			Rect rcView = viewRect;
			rcView.width = Mathf.Max(rcView.width, visibleRect.width);
			rcView.height = Mathf.Max(rcView.height, visibleRect.height);
	
			result.x = Mathf.Clamp(result.x, rcView.xMin - scrollPosition.x, rcView.xMax - visibleRect.width - scrollPosition.x);
			result.y = Mathf.Clamp(result.y, rcView.yMin - scrollPosition.y, rcView.yMax - visibleRect.height - scrollPosition.y);
			return result;
		}
	}

	static int scrollViewHash = "ScrollView".GetHashCode();
	static int sliderHash = "Slider".GetHashCode();
	static int repeatButtonHash = "RepeatButton".GetHashCode();
	static GUIStyle horizontalScrollbar;
	static GUIStyle verticalScrollbar;
	
	[NonSerialized]
	private ScrollViewState scrollViewState = new ScrollViewState();
#endif
	
	internal static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, ScrollViewState scrollViewState)
	{
#if UNITY_5_3 || UNITY_5_4_OR_NEWER
		horizontalScrollbar = GUI.skin.horizontalScrollbar;
		verticalScrollbar = GUI.skin.verticalScrollbar;
		
		if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition))
		{
			if (Mathf.Abs(Event.current.mousePosition.y - position.y) < 8f)
			{
				scrollPosition.y -= 16f;
				//GUI.InternalRepaintEditorWindow();
			}
			else if (Mathf.Abs(Event.current.mousePosition.y - position.yMax) < 8f)
			{
				scrollPosition.y += 16f;
				//GUI.InternalRepaintEditorWindow();
			}
		}
		
		if (scrollViewState.apply)
		{
			scrollPosition = scrollViewState.scrollPosition;
			scrollViewState.apply = false;
		}
		scrollViewState.position = position;
		scrollViewState.scrollPosition = scrollPosition;
		scrollViewState.visibleRect = scrollViewState.viewRect = viewRect;
		scrollViewState.visibleRect.width = position.width;
		scrollViewState.visibleRect.height = position.height;
		
		var screenRect = position;
		var type = Event.current.type;
		if (type != EventType.Layout)
		{
			if (type != EventType.Used)
			{
				bool showVertical = false;
				bool showHorizontal = false;
				if (viewRect.width > screenRect.width)
				{
					scrollViewState.visibleRect.height = position.height - horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
					screenRect.height -= horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
					showHorizontal = true;
				}
		
				if (viewRect.height > screenRect.height)
				{
					scrollViewState.visibleRect.width = position.width - verticalScrollbar.fixedWidth + verticalScrollbar.margin.left;
					screenRect.width -= verticalScrollbar.fixedWidth + verticalScrollbar.margin.left;
					showVertical = true;
		
					if (!showHorizontal && viewRect.width > screenRect.width)
					{
						scrollViewState.visibleRect.height = position.height - horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
						screenRect.height -= horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
						showHorizontal = true;
					}
				}
		
				if (showHorizontal && horizontalScrollbar != GUIStyle.none)
				{
					var rect = new Rect(
						position.x,
						position.yMax - horizontalScrollbar.fixedHeight,
						screenRect.width,
						horizontalScrollbar.fixedHeight);
					var size = Mathf.Min(screenRect.width, viewRect.width);
					var value = Mathf.Clamp(scrollPosition.x, 0f, viewRect.width - size);
					scrollPosition.x = GUI.HorizontalScrollbar(rect, value, size, 0f, viewRect.width);
				}
				else
				{
					GUIUtility.GetControlID(sliderHash, FocusType.Passive);
					GUIUtility.GetControlID(repeatButtonHash, FocusType.Passive);
					GUIUtility.GetControlID(repeatButtonHash, FocusType.Passive);
					if (horizontalScrollbar != GUIStyle.none)
					{
						scrollPosition.x = 0f;
					}
					else
					{
						scrollPosition.x = Mathf.Clamp(scrollPosition.x, 0f, Mathf.Max(viewRect.width - position.width, 0f));
					}
				}
		
				if (showVertical && verticalScrollbar != GUIStyle.none)
				{
					var rect = new Rect(
						screenRect.xMax + (float)verticalScrollbar.margin.left,
						screenRect.y,
						verticalScrollbar.fixedWidth,
						screenRect.height);
					var size = Mathf.Min(screenRect.height, viewRect.height);
					var value = Mathf.Clamp(scrollPosition.y, 0f, viewRect.height - size);
					scrollPosition.y = GUI.VerticalScrollbar(
						rect,
						value,
						size,
						0f,
						viewRect.height);
				}
				else
				{
					GUIUtility.GetControlID(sliderHash, FocusType.Passive);
					GUIUtility.GetControlID(repeatButtonHash, FocusType.Passive);
					GUIUtility.GetControlID(repeatButtonHash, FocusType.Passive);
					if (verticalScrollbar != GUIStyle.none)
					{
						scrollPosition.y = 0f;
					}
					else
					{
						scrollPosition.y = Mathf.Clamp(scrollPosition.y, 0f, Mathf.Max(viewRect.height - position.height, 0f));
					}
				}
			}
		}
		else
		{
			GUIUtility.GetControlID(sliderHash, FocusType.Passive);
			GUIUtility.GetControlID(repeatButtonHash, FocusType.Passive);
			GUIUtility.GetControlID(repeatButtonHash, FocusType.Passive);
			GUIUtility.GetControlID(sliderHash, FocusType.Passive);
			GUIUtility.GetControlID(repeatButtonHash, FocusType.Passive);
			GUIUtility.GetControlID(repeatButtonHash, FocusType.Passive);
		}
		
		GUI.BeginClip(
			screenRect,
			new Vector2(Mathf.Round(-scrollPosition.x - viewRect.x), Mathf.Round(-scrollPosition.y - viewRect.y)),
			Vector2.zero,
			false);
		
		return scrollPosition;
#else
		return GUI.BeginScrollView(position, scrollPosition, viewRect);
#endif
	}
	
	internal static void EndScrollView(bool handleScrollWheel, ScrollViewState scrollViewState)
	{
#if UNITY_5_3 || UNITY_5_4_OR_NEWER
		GUI.EndClip();
		if (handleScrollWheel && Event.current.type == EventType.ScrollWheel && scrollViewState.position.Contains(Event.current.mousePosition))
		{
			scrollViewState.scrollPosition.x = Mathf.Clamp(scrollViewState.scrollPosition.x + Event.current.delta.x * 20f, 0f, scrollViewState.viewRect.width - scrollViewState.visibleRect.width);
			scrollViewState.scrollPosition.y = Mathf.Clamp(scrollViewState.scrollPosition.y + Event.current.delta.y * 20f, 0f, scrollViewState.viewRect.height - scrollViewState.visibleRect.height);
			scrollViewState.apply = true;
			Event.current.Use();
		}
#else
		GUI.EndScrollView(handleScrollWheel);
#endif
	}
	
	static List<FGTextBuffer.CollapsibleTextSpan> collapsibleTextSpansInView = new List<FGTextBuffer.CollapsibleTextSpan>();
	
	[NonSerialized]
	private FGTextBuffer.CollapsibleTextSpan highlightedCollapsibleTextSpan;
	[NonSerialized]
	private Rect highlightedCollapsibleTextSpanRect;
	[NonSerialized]
	private bool findHighlightedCollapsibleTextSpan = true;

	[NonSerialized]
	private bool allowFontResizeWithWheel = true;

	public static EditorWindow lastFocusedWindow;
	public static FGTextEditor lastFocusedEditor { get; private set; }
	private static bool scrollInstantly = false;
	[NonSerialized]
	private Rect lastCodeViewRect;
	
	public void DoGUI(bool enableGUI)
	{
		if (textBuffer == null)
			return;

		if (textBuffer.assetPath == null)
			return;

		if (textBuffer.assetPath.EndsWithDll())
			return;
		
		if (textBuffer.assetPath.EndsWithExe())
			return;
		
		textBuffer.styles = styles = textBuffer.isText ? stylesText : stylesCode;
		
		if (Event.current.type == EventType.Layout)
			Initialize();
		else if (Event.current.type == EventType.Repaint)
			enableCodeFolding = CacheEnableCodeFolding();
		
		if (Event.current.rawType == EventType.MouseMove && mouseDownOnSelection && codeViewDragging)
		{
			mouseDownOnSelection = false;
			codeViewDragging = false;
			lineSelectMode = false;
		}

		var newFocusedWindow = EditorWindow.focusedWindow;
		if (newFocusedWindow == OwnerWindow)
			lastFocusedEditor = this;
		if (newFocusedWindow != null)
		{
			if (newFocusedWindow == tokenTooltip || newFocusedWindow == argumentsHint || newFocusedWindow == autocompleteWindow)
				newFocusedWindow = lastFocusedWindow;
		}
		bool windowFocusChanged = lastFocusedWindow != EditorWindow.focusedWindow;
		lastFocusedWindow = EditorWindow.focusedWindow;
		if (windowFocusChanged)
		{
			if (OwnerWindow == lastFocusedWindow)
				focusCodeView = true;
		}
		
		var toolbarsHeight = DoToolbar();
		toolbarsHeight += DoCodeNavigationToolbar();
		
		var lineHeight = LineHeight;

		bool showLineNumbers = parentWindow != null ?
			(isTextAsset ? SISettings.showLineNumbersText : SISettings.showLineNumbersCode) :
			(isTextAsset ? SISettings.showLineNumbersTextInspector : SISettings.showLineNumbersCodeInspector);
		
		if (hasCodeViewFocus)
		{
			if (tokenTooltip != null && tokenTooltip.text != null)
				tokenTooltip.OnOwnerGUI();
			
			if (autocompleteWindow != null)
			{
				var typedInLength = FGListPopup.TypedInPart.Length;

				var committedSymbol = autocompleteWindow.OnOwnerGUI();
				var committedWord = committedSymbol != null ? committedSymbol.name.GetString() ?? "" : null;
				if (FGListPopup.shortAttributeNames && committedWord != null && committedWord.FastEndsWith("Attribute"))
					committedWord = FGListPopup.NameOf(committedSymbol);
				if (committedWord == FGListPopup.TypedInPart && Event.current.character != '\n' && Event.current.keyCode != KeyCode.KeypadEnter)
				{
					//Debug.Log("Ignoring " + committedWord);
					committedWord = "";
				}
				if (committedWord != null)
				{
					//Debug.Log("Closing. Word commited: " + committedWord + " typedInPart: " + autocompleteWindow.TypedInPart);
					CloseAutocomplete();
					focusCodeView = true;
					var ownerWnd = OwnerWindow;
					if (ownerWnd != null && EditorWindow.focusedWindow != ownerWnd)
						ownerWnd.Focus();
				}
				if (!string.IsNullOrEmpty(committedWord))
				{
					if (!TryEdit())
					{
						committedWord = null;
						Event.current.Use();
					}
				}
				if (!string.IsNullOrEmpty(committedWord))
				{
					//var numLines = textBuffer.lines.Count;
					var originalLineText = textBuffer.lines[caretPosition.line];
					var insertAt = caretPosition.Clone();
					var codeSnippet = committedSymbol as SnippetCompletion;
					if (codeSnippet != null)
					{
						if (Event.current.character != '\n' && Event.current.character != '\t' && Event.current.keyCode != KeyCode.KeypadEnter)
							goto ignoreSnippet;
						if (committedWord.FastEndsWith("..."))
							committedWord = committedWord.Substring(0, committedWord.Length - 3);
					}
					if (Event.current.isKey && codeSnippet == null)
					{
						try
						{
							ProcessEditorKeyboard(Event.current, true);
							if (Event.current == null || Event.current.type == EventType.Used)
								nextF12GoesBack = false;
						}
						catch (ExitGUIException) {}
					}
					
					textBuffer.EndEdit();
					textBuffer.BeginEdit("Auto Completion '" + committedWord + "'");

					string insertedChar = null;
					if (codeSnippet == null && caretPosition > insertAt
						&& textBuffer.lines[insertAt.line].Length >= insertAt.characterIndex)
					{
						insertedChar = textBuffer.GetTextRange(insertAt, caretPosition);
						if (insertedChar[0] == '\n')
						{
							caretPosition = textBuffer.DeleteText(insertAt , caretPosition);
							var lineText = textBuffer.lines[caretPosition.line];
							var deltaLength = lineText.Length - originalLineText.Length;
							if (deltaLength < 0)
								textBuffer.InsertText(caretPosition, originalLineText.Substring(caretPosition.characterIndex, -deltaLength));
						}
						else if (Event.current.character != '\t')
						{
							committedWord += insertedChar;
							caretPosition = textBuffer.DeleteText(insertAt, caretPosition);
						}
						else
						{
							caretPosition = textBuffer.DeleteText(insertAt, caretPosition);
						}
					}
					if (codeSnippet != null)
					{
						codeSnippet.OverrideTypedInLength(ref typedInLength);
					}
					if (typedInLength > 0)
					{
						//var endTypedIn = insertAt.Clone();
						insertAt.characterIndex -= typedInLength;
						caretPosition = textBuffer.DeleteText(insertAt, caretPosition);
					}
					caretPosition = textBuffer.InsertText(insertAt, committedWord);

					string autoTextAfter = null;
					if (insertedChar != null)
						autoTextAfter = CheckAutoClose(insertedChar[0]);
					var updateToLine = caretPosition.line;
					if (autoTextAfter != null)
					{
						FGTextBufferManager.insertingTextAfterCaret = true;
						updateToLine = textBuffer.InsertText(caretPosition, autoTextAfter).line;
						FGTextBufferManager.insertingTextAfterCaret = false;

						var newAutoClose = new AutoClosePair
						{
							openingPos = new TextPosition(caretPosition.line, caretPosition.characterIndex - 1),
							closingPos = new TextPosition(caretPosition.line, caretPosition.characterIndex)
						};
						autoClosingStack.Add(newAutoClose);
						if (autoClosingStack.Count == 1)
						{
							textBuffer.onRemovedText += OnRemovedText;
							textBuffer.onInsertedText += OnInsertedText;
						}
					}

					if (wordWrapping)
						caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);
					textBuffer.UpdateHighlighting(insertAt.line, updateToLine);
					if (codeSnippet != null)
						ExpandSnippet(codeSnippet);
					else
						ReindentLines(insertAt.line, updateToLine);

					caretMoveTime = frameTime;
					lastCaretMoveWasSearch = false;
					scrollToCaret = true;
					//Repaint();
					
					//if (committedWord == "new " || committedWord == "case ")
					if (lastTypedText != null && codeSnippet == null)
					{
						AfterCharecterTyped(lastTypedText, caretPosition.line, caretPosition.characterIndex);
						tryArgumentsHint = true;
					}
					
					GUIUtility.ExitGUI();
				}
				if (Event.current.type == EventType.Used)
					return;
			}
			
		ignoreSnippet:

			if (argumentsHint != null && argumentsHint.text != null)
				argumentsHint.OnOwnerGUI();
				
			if (textBuffer != null && ProcessCodeViewCommands())
			{
				return;
			}
			
			bool isOSX = Application.platform == RuntimePlatform.OSXEditor;
			bool contextClick = Event.current.type == EventType.ContextClick
				|| isOSX && Event.current.type == EventType.MouseUp && Event.current.button == 1;
			bool contextKeyDown = Event.current.type == EventType.KeyDown &&
				(Event.current.keyCode == KeyCode.Menu || Event.current.Equals(Event.KeyboardEvent("#f10")));
			if (contextKeyDown || contextClick && scrollViewRect.Contains(Event.current.mousePosition))
			{
				Event.current.Use();
				var codeViewPopupMenu = new GenericMenu();
				bool addFindInFilesMenuItem = selectionStartPosition != null && selectionStartPosition.line == caretPosition.line;

				if (textBuffer.isCsFile)
				{
					int lineIndex, tokenIndex;
					bool atTokenEnd;
					var token = textBuffer.GetTokenAt(caretPosition, out lineIndex, out tokenIndex, out atTokenEnd);
					if (token != null)
					{
						if (atTokenEnd &&
							token.tokenKind != SyntaxToken.Kind.Identifier &&
							token.tokenKind != SyntaxToken.Kind.ContextualKeyword &&
							token.tokenKind != SyntaxToken.Kind.Keyword)
						{
							var tokens = textBuffer.formattedLines[lineIndex].tokens;
							if (tokenIndex < tokens.Count - 1)
								token = tokens[tokenIndex + 1];
						}
						
						if (token.parent != null && token.parent.resolvedSymbol != null && token.parent.semanticError == "unknown symbol")
						{
							var fixes = FGResolver.GetFixes(textBuffer, token);
							foreach (var fix in fixes)
							{
								var captured = fix;
								codeViewPopupMenu.AddItem(
									new GUIContent("Resolve/" + captured.GetTitle(token)),
									false,
									() => {
										BeginRefactoring(captured.GetTitle(token));
										captured.Apply(this, token);
										EndRefactoring();
									});
							}
							if (fixes.Count > 0)
								codeViewPopupMenu.AddSeparator("");
						}
						
						string helpUrl = HelpURL();
						if (helpUrl != null)
						{
							if (token.tokenKind == SyntaxToken.Kind.Keyword)
							{
								codeViewPopupMenu.AddItem("MSDN C# Reference", "%'", "MSDN C# Reference", "f1", false,
									() => Help.BrowseURL(helpUrl));
							}
							else if (helpUrl.StartsWithIgnoreCase("file:///unity/scriptreference/"))
							{
								codeViewPopupMenu.AddItem(
									"Unity Script Reference", "%'", "Unity Scripting Reference", "f1", false,
									() => Help.ShowHelpPage(helpUrl));
							}
							else if (helpUrl.StartsWithIgnoreCase("http://docs.unity3d.com/"))
							{
								codeViewPopupMenu.AddItem(
									"Unity Script Reference", "%'", "Unity Scripting Reference", "f1", false,
									() => Help.BrowseURL(helpUrl));
							}
							else
							{
								codeViewPopupMenu.AddItem(
									"MSDN .Net Reference", "%'", "MSDN .Net Reference", "f1", false,
									() => Help.BrowseURL(helpUrl));
							}
						}
						
						if (token.parent != null && token.parent.resolvedSymbol != null)
						{
							var symbol = token.parent.resolvedSymbol;
							var assembly = symbol.Assembly;

							if (assembly == null && symbol.declarations != null)
								assembly = ((CompilationUnitScope) textBuffer.Parser.parseTree.root.scope).assembly;

							if (assembly != null)
							{
								var assemblyName = assembly.AssemblyName;
								if (assemblyName == "mscorlib" || assemblyName == "system" || assemblyName.FastStartsWith("system."))
								{
									if (symbol.kind != SymbolKind.Namespace)
									{
										var input = symbol.XmlDocsName;
										if (input != null)
										{
											const int digits = 16;
											var md5 = System.Security.Cryptography.MD5.Create();
											var bytes = Encoding.UTF8.GetBytes(input);
											var hashBytes = md5.ComputeHash(bytes);
		
											var c = new char[digits];
											byte b;
											for (var i = 0; i < digits / 2; ++i)
											{
												b = ((byte) (hashBytes[i] >> 4));
												c[i * 2] = (char) (b > 9 ? b + 87 : b + 0x30);
												b = ((byte) (hashBytes[i] & 0xF));
												c[i * 2 + 1] = (char) (b > 9 ? b + 87 : b + 0x30);
											}
		
											codeViewPopupMenu.AddItem(
												"Go To Definition (.Net)", "%y", "Go To Definition (.Net)", "f12", false,
												() => Help.BrowseURL("http://referencesource.microsoft.com/mscorlib/a.html#" + new string(c)));
										}
									}
								}
								else if (assembly.fromCsScripts)
								{
									var declarations = GetSymbolDeclarations();
									symbol = symbol.Rebind() ?? symbol;
									
									if (declarations != null && declarations.Count == 1)
									{
										codeViewPopupMenu.AddItem(
											"Go To Definition", "%y", "Go To Definition", "f12",
											false,
											() => GoToSymbolDeclaration(declarations[0]));
									}
									else if (declarations != null && declarations.Count > 0)
									{
										foreach (var decl in declarations)
										{
											var co = decl.scope;
											while (co.parentScope != null)
												co = co.parentScope;
											var fileName = ((CompilationUnitScope) co).path;
											fileName = AssetDatabase.AssetPathToGUID(fileName);
											fileName = AssetDatabase.GUIDToAssetPath(fileName);
											fileName = Path.GetFileName(fileName);
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
											fileName = fileName.Replace('_', '\xFF3F');
#endif
											var nameNode = decl.NameNode();
											var firstLeaf = nameNode as ParseTree.Leaf ?? (nameNode as ParseTree.Node).GetFirstLeaf();
											
											var signature = "";
											if (decl.definition.kind == SymbolKind.Method)
											{
												var definition = decl.definition;
												signature = decl.Name + " (" + definition.PrintParameters(definition.GetParameters(), true) + ")";
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
												signature = signature.Replace('_', '\xFF3F');
												codeViewPopupMenu.AddItem(
													new GUIContent("Go To Overload Definition/" + signature + " _"),
#else
												codeViewPopupMenu.AddItem(
													new GUIContent("Go To Overload Definition/" + signature),
#endif
													false,
													d => GoToSymbolDeclaration((SymbolDeclaration) d),
													decl);
											}
											else
											{
												codeViewPopupMenu.AddItem(
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
													new GUIContent("Go To Definition/" + fileName + " : " + (firstLeaf.token.Line + 1) + " _"),
#else
													new GUIContent("Go To Definition/" + fileName + " : " + (firstLeaf.token.Line + 1)),
#endif
													false,
													d => GoToSymbolDeclaration((SymbolDeclaration) d),
													decl);
											}
											
											//if (declarations != null && declarations.Count > 0)
											//{
											//	codeViewPopupMenu.AddItem(
											//		"Rename...", "f2", "Rename...", "&%r",
											//		false,
											//		() => FGFindInFiles.RenameSymbol(symbol, targetPath));
											//}
										}
									}
									
									//if (declarations != null && declarations.Count > 0)
									//{
									//	codeViewPopupMenu.AddItem(
									//		"Rename...", "&%r", "Rename...", "f2",
									//		false,
									//		() => FGFindInFiles.RenameSymbol(symbol, targetPath));
									//}
								}
								
								var symbolType =  symbol != null ? symbol.TypeOf() as TypeDefinitionBase : null;
								if (symbolType != symbol && symbolType != null)
									AddGoToTypeDefinitionMenuItems(codeViewPopupMenu, symbolType);
								
								var tokenSpan = textBuffer.GetTokenSpan(token.Line, token.TokenIndex);
								if (selectionStartPosition == null
									||
									selectionStartPosition.line == tokenSpan.StartPosition.line && selectionStartPosition.characterIndex == tokenSpan.StartPosition.index
									&& caretPosition.line == tokenSpan.EndPosition.line && caretPosition.characterIndex == tokenSpan.EndPosition.index
									||
									selectionStartPosition.line == tokenSpan.EndPosition.line && selectionStartPosition.characterIndex == tokenSpan.EndPosition.index
									&& caretPosition.line == tokenSpan.StartPosition.line && caretPosition.characterIndex == tokenSpan.StartPosition.index)
								{
									if (symbol != null)
									{
										addFindInFilesMenuItem = false;
										
										codeViewPopupMenu.AddItem(
											"Find All References", "%#y", "Find All References", "#f12",
											false,
											() => FGFindInFiles.FindAllReferences(symbol, targetPath));
									}
								}
							}
							
							if (symbol != null && symbol.kind == SymbolKind.Method && symbol.GetParameters().Count == 0 && symbol.IsStatic)
							{
								if (addFindInFilesMenuItem)
								{
									addFindInFilesMenuItem = false;
									
									codeViewPopupMenu.AddItem(
										"Find in Files...", "%#f", "Find in Files...", "%#f",
										false,
										FindReplaceWindow.ShowFindInFilesWindow);
								}
								
								if (codeViewPopupMenu.GetItemCount() > 0)
									codeViewPopupMenu.AddSeparator("");
								codeViewPopupMenu.AddItem(
									"Execute", "%e", "Execute", "%e"
									, false, ExecuteStaticMethod);
							}
						}
					}
				}
				
				if (addFindInFilesMenuItem)
				{
					codeViewPopupMenu.AddItem(
						"Find in Files...", "%#f", "Find in Files...", "%#f",
						false,
						FindReplaceWindow.ShowFindInFilesWindow);
				}
				
				if (codeViewPopupMenu.GetItemCount() > 0)
					codeViewPopupMenu.AddSeparator(string.Empty);

				if (selectionStartPosition != null)
				{
					codeViewPopupMenu.AddItem("Copy", "%c", "Copy", "%c", false, () => EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Copy")));
					codeViewPopupMenu.AddItem("Cut", "%x", "Cut", "%x", false, () => EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Cut")));
				}
				else if (SISettings.copyCutFullLine)
				{
					codeViewPopupMenu.AddItem("Copy Line", "%c", "Copy Line", "%c", false, () => EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Copy")));
					codeViewPopupMenu.AddItem("Cut Line", "%x", "Cut Line", "%x", false, () => EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Cut")));
				}
				else
				{
					codeViewPopupMenu.AddItem("Copy", "%c", "Copy", "%c", false, null);
					codeViewPopupMenu.AddItem("Cut", "%x", "Cut", "%x", false, null);
				}
				if (string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer))
					codeViewPopupMenu.AddItem("Paste", "%v", "Paste", "%v", false, null);
				else
					codeViewPopupMenu.AddItem("Paste", "%v", "Paste", "%v", false, () => EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Paste")));
				codeViewPopupMenu.AddSeparator(string.Empty);

				codeViewPopupMenu.AddItem("Select All", "%a", "Select All", "%a", false, () => EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("SelectAll")));
				codeViewPopupMenu.AddSeparator(string.Empty);
				if (!textBuffer.isText)
					codeViewPopupMenu.AddItem("Toggle Comment Selection", "%k", "Toggle Comment Selection", "%k", false, ToggleCommentSelection);
				codeViewPopupMenu.AddItem("Increase Line Indent", "%]", "Increase Line Indent", "%]", false, IndentMore);
				codeViewPopupMenu.AddItem("Decrease Line Indent", "%[", "Decrease Line Indent", "%[", false, IndentLess);
				
				if (enableCodeFolding)
				{
					codeViewPopupMenu.AddSeparator(string.Empty);
					#if SI3_WARNINGS
					codeViewPopupMenu.AddItem(new GUIContent("Code Folding/Fold Selection"), false, FoldSelection);
					codeViewPopupMenu.AddSeparator("Code Folding/");
					#endif
					codeViewPopupMenu.AddItem("Code Folding/Toggle", "%m", "Code Folding/Toggle", "%m", false, ToggleFoldingAtCaret);
					codeViewPopupMenu.AddItem("Code Folding/Fold", "%&LEFT", "Code Folding/Fold", "&#LEFT", false, FoldAtCaret);
					codeViewPopupMenu.AddItem("Code Folding/Unfold", "%&RIGHT", "Code Folding/Unfold", "&#RIGHT", false, UnfoldAtCaret);
					codeViewPopupMenu.AddSeparator("Code Folding/");
					codeViewPopupMenu.AddItem("Code Folding/Toggle Fold to Definitions", "&%d", "Code Folding/Toggle Fold to Definitions", "#%d", false, ToggleFoldToDefinitions);
					#if SI3_WARNINGS
					codeViewPopupMenu.AddSeparator("Code Folding/");
					codeViewPopupMenu.AddItem(new GUIContent("Code Folding/Stop Folding"), false, StopFolding);
					#endif
				}
				
				codeViewPopupMenu.AddSeparator(string.Empty);
				var swapShiftAndCtrlEnter = SISettings.swapShiftAndCtrlEnter.Value;
				codeViewPopupMenu.AddItem(
					"Open at Line " + (caretPosition.line + 1) + "...", swapShiftAndCtrlEnter ? "#\n" : "%\n",
					"Open at Line " + (caretPosition.line + 1) + "...", swapShiftAndCtrlEnter ? "#Enter" : "%Enter", false,
					() => EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("OpenAtCursor")));
				
				var tokenAtCursor = GetTokenAtCursor();
				if (tokenAtCursor != null && tokenAtCursor.tokenKind < SyntaxToken.Kind.Keyword && tokenAtCursor.tokenKind != SyntaxToken.Kind.BuiltInLiteral)
					tokenAtCursor = null;
				Rect rcCaret;
				if (mouseDownOnSelection)
				{
					rcCaret = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 1f, 1f);
				}
				else
				{
					rcCaret = tokenAtCursor != null ? GetTokenRect(tokenAtCursor) : GetCaretRect(caretPosition);
					rcCaret.x += scrollViewRect.x - scrollPosition.x;
					rcCaret.y += 4f + scrollViewRect.y - scrollPosition.y;
					if (tokenAtCursor != null)
					{
						var ssTopLeft = GUIUtility.ScreenToGUIPoint(new Vector2(rcCaret.x, rcCaret.y));
						rcCaret.x += ssTopLeft.x - rcCaret.x;
						rcCaret.y += ssTopLeft.y - rcCaret.y;
					}
				}
				codeViewPopupMenu.DropDown(rcCaret);
				return;
			}

			if (Event.current.isKey)
			{
				ProcessEditorKeyboard(Event.current, false);
				if (Event.current == null || Event.current.type == EventType.Used)
				{
					nextF12GoesBack = false;
					//GUIUtility.ExitGUI();
					return;
				}
				//Event.current.Use();
			}
		}

		if (Event.current.type == EventType.ScrollWheel)
		{
			mouseHoverToken = null;
			mouseHoverTime = default(System.DateTime);
			if (tokenTooltip != null)
				tokenTooltip.Hide();
			
			if (argumentsHint != null)
				CloseArgumentsHint();
		}
		
		if (SISettings.changeFontSizeUsingWheel)
		{
			if (Event.current.type == EventType.ScrollWheel)
			{
				if (EditorGUI.actionKey)
				{
					if (allowFontResizeWithWheel)
					{
						Event.current.Use();
						ModifyFontSize(-(int)Event.current.delta.y);
						return;
					}
				}
				else
				{
					allowFontResizeWithWheel = false;
				}
			}
			else if (!allowFontResizeWithWheel)
			{
				if (Event.current.isMouse || Event.current.character != '\0')
					allowFontResizeWithWheel = true;
			}
		}

		widestLine = Mathf.Round(Mathf.Max(widestLine, textBuffer.longestLine * charSize.x));
		var contentWidth = Mathf.Max(contentRect.width - 8f, widestLine);
		float contentHeight = wordWrapping || CacheEnableCodeFolding()
			? GetLineOffset(textBuffer.lines.Count) + 8f
			: 8f + lineHeight * textBuffer.formattedLines.Count;
		
		contentRect.Set(-4f, -4f, contentWidth + 8f, contentHeight);

		var lineNumbersWidth = 0f;
		var lineNumbersMaxLength = 0;

		// Find left margin size
		marginLeft = 0f;		
		if (showLineNumbers)
		{
			lineNumbersMaxLength = Mathf.FloorToInt(Mathf.Log10(textBuffer.formattedLines.Count + 1 + lineNumbersOffset) + 1f);
			lineNumbersWidth = charSize.x * lineNumbersMaxLength;
			marginLeft = lineNumbersWidth;
		}
		if (trackChanges)
		{
			marginLeft += showLineNumbers ? 7f : 3f;
		}
		if (enableCodeFolding)
		{
			marginLeft += pointSize * 9f;
		}
		if (marginLeft > 0f)
		{
			marginLeft += 9f;
		}

		var canGoToOnScroll = true;
		
	onScroll:
		
		scrollPosition.y = GetLineOffset(scrollPositionLine) + scrollPositionOffset;

		int fromLine = GetLineAt(scrollPosition.y);
		int toLine = GetLineAt(scrollPosition.y + scrollViewRect.height);

		if (toLine >= textBuffer.lines.Count)
		{
			toLine = textBuffer.lines.Count - 1;
			fromLine = Mathf.Max(0, Mathf.Min(fromLine, toLine - (int)(scrollViewRect.height / lineHeight)));
		}

		if (scrollToCaret && Event.current.type != EventType.Layout)
		{
			scrollToCaret = false;
			var caretPos = codeViewDragging && mouseDownOnSelection ? mouseDropPosition : caretPosition;
			//var caretRect = GetCaretRect(caretPos);

//			if (showLineNumbers || trackChanges)
//				contentRect.xMax += marginLeft;

			/*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/

			codeViewRect.x = scrollPosition.x + marginLeft;
			codeViewRect.y = scrollPosition.y;
			codeViewRect.width = scrollViewRect.width - marginLeft - 4f;
			codeViewRect.height = scrollViewRect.height - 4f;

			float contentHeight2 = wordWrapping || enableCodeFolding
				? GetLineOffset(textBuffer.lines.Count) + 8f
				: contentRect.height;
			
			bool hasHorizontalSB = !wordWrapping && contentRect.width - 4f - marginLeft - marginRight > codeViewRect.width;
			bool hasVerticalSB = contentHeight2 - 4f > codeViewRect.height;
			if (hasHorizontalSB && hasVerticalSB)
			{
				codeViewRect.width -= 15f;
				codeViewRect.height -= 15f;
			}
			else if (hasHorizontalSB)
			{
				codeViewRect.height -= 15f;
				hasVerticalSB = contentRect.height - 4f > codeViewRect.height;
				if (hasVerticalSB)
					codeViewRect.width -= 15f;
			}
			else if (hasVerticalSB)
			{
				codeViewRect.width -= 15f;
				hasHorizontalSB = contentRect.width - 4f - marginLeft - marginRight > codeViewRect.width;
				if (hasHorizontalSB)
					codeViewRect.height -= 15f;
			}
			
			//codeViewRect.xMin = Mathf.Ceil((codeViewRect.x - 1f - marginLeft) / charSize.x) * charSize.x + 0f + marginLeft;
			//codeViewRect.width = Mathf.Floor(codeViewRect.width / charSize.x) * charSize.x;
			//codeViewRect.yMin = Mathf.Ceil(codeViewRect.y / lineHeight) * lineHeight;
			//codeViewRect.height = Mathf.Floor(codeViewRect.height / lineHeight) * lineHeight;
			
			float yOffset = GetCaretRect(caretPosition).y;
			//if (wordWrapping /*&& !IsLoading*/)
			//{
			//	int row, column;
			//	BufferToViewPosition(caretPosition, out row, out column);
			//	yOffset = lineHeight * row + GetLineOffset(caretPos.line);
			//}
			//else
			//{
			//	yOffset = lineHeight * caretPos.line;
			//}
				
			if (yOffset < scrollPosition.y)
			{
				scrollPosition.y = yOffset;
				needsRepaint = true;
				scrollToCaret = true;
			}
			var tempOffset = yOffset + lineHeight - scrollViewRect.height + (hasHorizontalSB ? 23f : 8f);
			if (tempOffset > 0f && tempOffset > scrollPosition.y)
			{
				scrollPosition.y = tempOffset;
				needsRepaint = true;
				scrollToCaret = true;
			}

			if (!wordWrapping)
			{
				var caretRect = GetCaretRect(caretPos);
				caretRect.x -= marginLeft;

				/*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/

				if (caretRect.x < scrollPosition.x)
				{
					var oldX = scrollPosition.x;
					scrollPosition.x = Mathf.Round(Mathf.Max(0f, caretRect.x - 20f * charSize.x));
					if (oldX != scrollPosition.x)
					{
						needsRepaint = true;
						scrollToCaret = true;
						if (autocompleteWindow != null)
						{
							var newPos = autocompleteWindow.position;
							newPos.x += oldX - scrollPosition.x;
							autocompleteWindow.position = newPos;
						}
					}
				}
				else if (caretRect.x + charSize.x > scrollPosition.x + scrollViewRect.width - marginLeft - 22f)
				{
					var oldX = scrollPosition.x;
					scrollPosition.x = Mathf.Max(0f, caretRect.x + 21f * charSize.x - scrollViewRect.width + marginLeft + 22f);
					if (oldX != scrollPosition.x)
					{
						needsRepaint = true;
						scrollToCaret = true;
						if (autocompleteWindow != null)
						{
							var newPos = autocompleteWindow.position;
							newPos.x += oldX - scrollPosition.x;
							autocompleteWindow.position = newPos;
						}
					}
				}
			}
			
			if (needsRepaint)
			{
				if (scrollToCaret && argumentsHint != null)
				{
					//Debug.Log("Closing [Scroll]");//
					CloseArgumentsHint();
				}
				if (CanEdit())
				{
					scrollPositionLine = GetLineAt(scrollPosition.y);
					scrollPositionOffset = scrollPosition.y - GetLineOffset(scrollPositionLine);
				}
				needsRepaint = false;
				if (canGoToOnScroll)
				{
					canGoToOnScroll = false;
					goto onScroll;
				}
			}
		}

		if (pingTimer >= 1f && scrollViewRect.height > 1f && Event.current.type == EventType.Layout)
		{
			if (scrollToRect.yMin < scrollPosition.y + 30f ||
				scrollToRect.yMax > scrollPosition.y + scrollViewRect.height - 50f)
			{
				var y = Mathf.Floor(Mathf.Max(0f, scrollToRect.center.y - scrollViewRect.height * 0.5f));
				if (scrollPosition.y != y)
				{
					scrollPosition.y = y;
					needsRepaint = true;
				}
			}

			if (scrollToRect.xMin < scrollPosition.x + 30f ||
				scrollToRect.xMax > scrollPosition.x + scrollViewRect.width - 30f - marginLeft)
			{
				var x = Mathf.Floor(Mathf.Max(0f, scrollToRect.center.x - scrollViewRect.width * 0.5f));
				if (scrollPosition.x != x)
				{
					scrollPosition.x = x;
					needsRepaint = true;
				}
			}

			pingTimer = 0.999f;

			if (needsRepaint)
			{
				pingStartTime = frameTime;
				
				if (CanEdit())
				{
					scrollPositionLine = GetLineAt(scrollPosition.y);
					scrollPositionOffset = scrollPosition.y - GetLineOffset(scrollPositionLine);
				}
				needsRepaint = false;
				if (canGoToOnScroll)
				{
					canGoToOnScroll = false;
					goto onScroll;
				}
			}
		}

		if (Event.current.type == EventType.Repaint)
		{
			if (needsRepaint)
			{
				needsRepaint = false;
				if (CanEdit())
				{
					var line = scrollPositionLine;
					var offset = scrollPositionOffset;
					scrollPositionLine = GetLineAt(scrollPosition.y);
					scrollPositionOffset = scrollPosition.y - GetLineOffset(scrollPositionLine);
					if (line != scrollPositionLine || offset != scrollPositionOffset)
					{
						if (canGoToOnScroll)
						{
							canGoToOnScroll = false;
							goto onScroll;
						}
					}
				}
				else
				{
				//	Repaint();
				//	DoGUI(enableGUI);
					if (canGoToOnScroll)
					{
						canGoToOnScroll = false;
						goto onScroll;
					}
				//	return;
				}
			}
		}

		scrollPosition.y = Mathf.Clamp(scrollPosition.y, 0f, Mathf.Max(contentRect.height - codeViewRect.height, 0f));

		if (Event.current.type == EventType.Layout)
		{
			if (CanEdit())
			{
				scrollPositionLine = GetLineAt(scrollPosition.y);
				scrollPositionOffset = scrollPosition.y - GetLineOffset(scrollPositionLine);
			}
			//return;
		}

		//if (marginLeft > 0f)
		//	contentRect.xMax += marginLeft + marginRight;

		// Filling the background
		FillRect(scrollViewRect, styles.scrollViewColor);

		if (lastMouseEvent != null && autoScrollDelta != Vector2.zero)
		{
			lastMouseEvent.mousePosition -= scrollPosition;
			scrollPosition += autoScrollDelta;
			scrollPositionLine = GetLineAt(scrollPosition.y);
			scrollPositionOffset = scrollPosition.y - GetLineOffset(scrollPositionLine);
			
			fromLine = scrollPositionLine;
			toLine = GetLineAt(scrollPosition.y + scrollViewRect.height);
			
			if (toLine >= textBuffer.lines.Count)
			{
				toLine = textBuffer.lines.Count - 1;
				fromLine = Mathf.Max(0, Mathf.Min(fromLine, toLine - (int)(scrollViewRect.height / lineHeight)));
			}
		}

		if (!scrollPositionInitialized)
		{
			scrollPositionInitialized = true;
			smoothScrollPosition = scrollPosition;
			currentScrollVelocity = Vector2.zero;
			lastSmoothScrollTime = default(System.DateTime);
		}

		if (float.IsNaN(smoothScrollPosition.x) || float.IsNaN((smoothScrollPosition.y)))
		{
			smoothScrollPosition = scrollPosition;
			currentScrollVelocity = Vector2.zero;
			lastSmoothScrollTime = default(System.DateTime);
			if (float.IsNaN(smoothScrollPosition.x) || float.IsNaN((smoothScrollPosition.y)))
				scrollPositionInitialized = false;
		}
		
		bool isScrollWheelEvent = Event.current.type == EventType.ScrollWheel && scrollViewRect.Contains(Event.current.mousePosition);
		
		//if (isScrollWheelEvent)
		//	Event.current.delta *= 2f;
		
#if !UNITY_2022_3_OR_NEWER || UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2023_1_0 || UNITY_2023_1_1 || UNITY_2023_1_2 || UNITY_2023_1_3 || UNITY_2023_1_4 || UNITY_2023_1_5 || UNITY_2023_1_6 || UNITY_2023_1_7 || UNITY_2023_1_8 || UNITY_2023_1_9
		if (!wordWrapping && isScrollWheelEvent && Event.current.shift)
		{
			var current = Event.current;
			current.delta = new Vector2(current.delta.y, current.delta.x);
			Event.current = current;
		}
#endif
		
		if (isScrollWheelEvent)
		{
			scrollPosition.x = Mathf.Clamp(scrollPosition.x + Event.current.delta.x * lineHeight, 0f, contentRect.width - codeViewRect.width + 4f);
			scrollPosition.y = Mathf.Clamp(scrollPosition.y + Event.current.delta.y * lineHeight, 0f, contentRect.height - codeViewRect.height);
			Event.current.Use();

			scrollPositionLine = GetLineAt(scrollPosition.y);
			scrollPositionOffset = scrollPosition.y - GetLineOffset(scrollPositionLine);
		}
		
		if (SISettings.smoothScrolling && !scrollInstantly && !isScrollWheelEvent)
		{
			if (Event.current.type == EventType.Repaint)
			{
				var deltaTime = Mathf.Clamp01((float) (frameTime - lastSmoothScrollTime).TotalSeconds);
				lastSmoothScrollTime = frameTime;
				if (smoothScrollPosition != scrollPosition)
				{
					smoothScrollPosition.x = Mathf.SmoothDamp(smoothScrollPosition.x, scrollPosition.x, ref currentScrollVelocity.x, 0.05f, Mathf.Infinity, deltaTime);
					smoothScrollPosition.y = Mathf.SmoothDamp(smoothScrollPosition.y, scrollPosition.y, ref currentScrollVelocity.y, 0.05f, Mathf.Infinity, deltaTime);
				}
			}
		}
		else
		{
			smoothScrollPosition = scrollPosition;
		}

		//if (Event.current.type == EventType.scrollWheel)
		//	Event.current.delta *= 2f;
		
		var canBeginScrollViewAgain = true;
		
	beginScrollViewAgain:
		var roundedSmoothScrollPosition = new Vector2(Mathf.Round(smoothScrollPosition.x), Mathf.Round(smoothScrollPosition.y));
		
		var rounderContentRect = wordWrapping ? new Rect(contentRect.x, contentRect.y, 1, contentHeight) : contentRect;
		rounderContentRect = new Rect(rounderContentRect.x, rounderContentRect.y,
			Mathf.Ceil(rounderContentRect.width), Mathf.Ceil(rounderContentRect.height));
		Vector2 newScrollPosition = BeginScrollView(scrollViewRect, roundedSmoothScrollPosition, rounderContentRect, scrollViewState);
		
		if (CanEdit() && (scrollPosition != newScrollPosition || isScrollWheelEvent))
		{
			if (canBeginScrollViewAgain && !scrollInstantly && roundedSmoothScrollPosition != newScrollPosition)
			{
				isScrollWheelEvent = false;
				canBeginScrollViewAgain = false;
				
				scrollPosition = newScrollPosition;

				scrollPositionLine = GetLineAt(newScrollPosition.y);
				scrollPositionOffset = newScrollPosition.y - GetLineOffset(scrollPositionLine);

				newScrollPosition = roundedSmoothScrollPosition;

				EndScrollView(true, scrollViewState);
				goto beginScrollViewAgain;
			}

			var smoothScrollPositionLine = GetLineAt(newScrollPosition.y);
			
			fromLine = smoothScrollPositionLine;
			toLine = GetLineAt(newScrollPosition.y + scrollViewRect.height);
			
			if (toLine >= textBuffer.lines.Count)
			{
				toLine = textBuffer.lines.Count - 1;
				fromLine = Mathf.Max(0, Mathf.Min(fromLine, toLine - (int)(scrollViewRect.height / lineHeight)));
			}
		}
		else
		{
			smoothScrollPosition = newScrollPosition;
		}
		if (textBuffer.lines.Count == 0)
		{
			EndScrollView(true, scrollViewState);
			scrollInstantly = false;
			return;
		}

		// Hack: Workaround for not receiving mouseUp event if dragged outside of the scrollview's clipped content.
		// Note: mousePosition here will be incorrect, don't use it!
		if (Event.current.rawType == EventType.MouseUp && GUIUtility.hotControl == codeViewControlID)
		{
			//Event.current.mousePosition = Event.current.mousePosition - new Vector2(scrollViewRect.x, scrollViewRect.y);
			ProcessEditorMouse(marginLeft, Event.current);
			goto endScrollViewAndExit;
		}

		//if (Event.current.type == EventType.Layout)
		{
			collapsibleTextSpansInView.Clear();
			if (textBuffer.collapsibleTextSpansTree != null && fromLine <= toLine)
			{
				var textIntervalInView = new TextInterval(new TextPosition(fromLine, -1), new TextPosition(toLine + 1, -1));
				textBuffer.collapsibleTextSpansTree.GetIntervalsOverlappingWith(textIntervalInView, collapsibleTextSpansInView);
			}
		}

		if (lastMouseEvent != null && autoScrollDelta != Vector2.zero)
		{
			lastMouseEvent.mousePosition += scrollPosition;
			autoScrollDelta = Vector2.zero;
		}

		if (focusCodeView)
		{
			focusCodeView = false;
			caretMoveTime = frameTime;
			GUIUtility.keyboardControl = codeViewControlID;
			Repaint();
		}
		hasCodeViewFocus = GUIUtility.keyboardControl == codeViewControlID;
		if (hasCodeViewFocus && Event.current.rawType != EventType.MouseUp)
		{
			EditorWindow wnd = EditorWindow.focusedWindow;
			if (wnd == null)
			{
				hasCodeViewFocus = false;
			}
			else if (wnd != autocompleteWindow)
			{
				hasCodeViewFocus = wnd == OwnerWindow;
			}
		}
		if (hasCodeViewFocus)
			Input.imeCompositionMode = IMECompositionMode.On;

		if (Event.current.type != EventType.Layout)
		{
			codeViewRect.x = scrollPosition.x + marginLeft;
			codeViewRect.y = scrollPosition.y;
			codeViewRect.width = scrollViewRect.width - marginLeft - 4f - marginRight;
			codeViewRect.height = scrollViewRect.height - 4f;

			bool hasHorizontalSB = !wordWrapping && contentRect.width - 4f - marginLeft - marginRight > codeViewRect.width;
			bool hasVerticalSB = (wordWrapping ? contentHeight : contentRect.height) - 4f > codeViewRect.height;
			if (hasHorizontalSB && hasVerticalSB)
			{
				codeViewRect.width -= 15f;
				codeViewRect.height -= 15f;
			}
			else if (hasHorizontalSB)
			{
				codeViewRect.height -= 15f;
				hasVerticalSB = contentRect.height - 4f > codeViewRect.height;
				if (hasVerticalSB)
					codeViewRect.width -= 15f;
			}
			else if (hasVerticalSB)
			{
				codeViewRect.width -= 15f;
				hasHorizontalSB = contentRect.width - 4f - marginLeft - marginRight > codeViewRect.width;
				if (hasHorizontalSB)
					codeViewRect.height -= 15f;
			}
		
			fullCodeViewRect = codeViewRect;
			fullCodeViewRect.xMin += -4f;
			fullCodeViewRect.yMin += -4f;
			fullCodeViewRect.yMax += 4f;
		
			//codeViewRect.xMin = Mathf.Ceil((codeViewRect.x - 1f - marginLeft) / charSize.x) * charSize.x + 0f + marginLeft;
			//codeViewRect.width = Mathf.Floor(codeViewRect.width / charSize.x) * charSize.x;
			//codeViewRect.yMin = Mathf.Ceil(codeViewRect.y / lineHeight) * lineHeight;
			//codeViewRect.height = Mathf.Floor(codeViewRect.height / lineHeight) * lineHeight;

			// Uncomment for debugging only
			//GUI.Box(codeViewRect, GUIContent.none);
		}

		//if (needsRepaint)
		//{
		//	GUI.EndScrollView();
		//	Repaint();
		//	return;
		//}

		if (Event.current.type == EventType.Repaint)
		{
			needsReformat = wordWrapping &&
				(lastCodeViewRect.width != codeViewRect.width || SISettings.tabSize != lastTabSize);
			needsReformat |= SISettings.lineSpacing != lastLineSpacing;
		}
		
		if (Event.current.type == EventType.Layout)
		{
			ClearLinesInView();
			LayoutLinesInView(fromLine, toLine);

			laidOutLinesFrom = fromLine;
			laidOutLinesTo = toLine;
		}
		else if (Event.current.type == EventType.Repaint && (fromLine != laidOutLinesFrom || toLine != laidOutLinesTo))
		{
			ClearLinesInView();
			LayoutLinesInView(fromLine, toLine);
			laidOutLinesFrom = fromLine;
			laidOutLinesTo = toLine;
		}
		
		if (Event.current.type == EventType.Repaint /*&& !IsLoading*/)
		{
			// Add cursor rect for left margin
			var textAreaRect = wordWrapping ? new Rect(contentRect.x, contentRect.y, contentRect.width, contentHeight) : contentRect;
			textAreaRect.xMin += marginLeft + scrollPosition.x;
			EditorGUIUtility.AddCursorRect(textAreaRect, MouseCursor.Text);
			
			// Current line highlighting
			if (!hasSelection && !IsLoading
				&& caretPosition.line >= fromLine && caretPosition.line <= toLine
				&& (SISettings.highlightCurrentLine || SISettings.frameCurrentLine))
			{
				var caretRect = GetCaretRect(caretPosition);
				Rect currentLineRect = new Rect(marginLeft - 4f + smoothScrollPosition.x, caretRect.y, codeViewRect.width + charSize.x + 4f, 1f);
				if (SISettings.frameCurrentLine)
					FillRect(currentLineRect, hasCodeViewFocus ? styles.currentLineColor : styles.currentLineInactiveColor);

				currentLineRect.y += lineHeight - 1f;
				if (SISettings.frameCurrentLine)
					FillRect(currentLineRect, hasCodeViewFocus ? styles.currentLineColor : styles.currentLineInactiveColor);

				currentLineRect.y -= lineHeight - 2f;
				currentLineRect.height = lineHeight - 2f;
				Color oldColor = GUI.color;
				GUI.color = new Color(1f, 1f, 1f, SISettings.highlightCurrentLineAlpha);
				if (SISettings.highlightCurrentLine)
					FillRect(currentLineRect, hasCodeViewFocus ? styles.currentLineColor : styles.currentLineInactiveColor);
				GUI.color = oldColor;
			}

			// Highlighting search results
			if (textBuffer.undoPosition == searchResultAge && highlightSearchResults)
			{
				var oldCVD = codeViewDragging;
				codeViewDragging = true;
				
				int searchStringLength = searchString.Length;
				var startFrom = searchResults.BinarySearch(new TextPosition{ line = fromLine, index = -1 });
				if (startFrom < 0)
					startFrom = ~startFrom;
				for (var i = startFrom; i < searchResults.Count; ++i)
				{
					var highlightRect = searchResults[i];
					if (highlightRect.line >= fromLine)
					{
						if (highlightRect.line > toLine)
							break;
						
						DrawSelectionRect(highlightRect.line, highlightRect.index, searchStringLength, false, styles.searchResultColor, false);
					}
				}
				
				codeViewDragging = oldCVD;
			}
		}
		
		if (Event.current.type == EventType.Repaint)
			DrawLinesInView();
		
		if (Event.current.type == EventType.Repaint)
		{
			// Matching braces highlighting
			if (!IsLoading && !hasSelection && pingTimer == 0f &&
				matchingBraceLeft < matchingBraceRight &&
				matchingBraceLeft.line >= 0 && matchingBraceLeft.line < textBuffer.formattedLines.Count &&
				matchingBraceRight.line >= 0 && matchingBraceRight.line < textBuffer.formattedLines.Count &&
				matchingBraceLeft.index < textBuffer.formattedLines[matchingBraceLeft.line].tokens.Count &&
				matchingBraceRight.index < textBuffer.formattedLines[matchingBraceRight.line].tokens.Count)
			{
				var spanLeft = textBuffer.GetTokenSpan(matchingBraceLeft.line, matchingBraceLeft.index);
				var spanRight = textBuffer.GetTokenSpan(matchingBraceRight.line, matchingBraceRight.index);
				var isHiddenLeft = GetCollapsedSpanForTextSpan(spanLeft) != null;
				var isHiddenRight = GetCollapsedSpanForTextSpan(spanRight) != null;
				
				if (!isHiddenLeft || !isHiddenRight)
				{
					var charLeft = textBuffer.lines[spanLeft.line][spanLeft.index];
					var charRight = textBuffer.lines[spanRight.line][spanRight.index];
					var diff = charRight - charLeft;
					var matchingColor = diff == 1 || diff == 2 ? styles.referenceHighlightColor : styles.referenceModifyHighlightColor;
					
					if (!isHiddenRight)
					{
						var rcRight = GetTextRect(spanRight);
						var rcRightPlus = new Rect(rcRight.x - 1f, rcRight.yMax - 1f, rcRight.width + 2f, 1f);
				
						var tokenRight = textBuffer.formattedLines[matchingBraceRight.line].tokens[matchingBraceRight.index];
				
						FillRect(rcRight, matchingColor);
					
						var oldColor = GUI.color;
						GUI.color = styles.normalStyle.normal.textColor;
						GUI.DrawTexture(rcRightPlus, EditorGUIUtility.whiteTexture);
						GUI.color = oldColor;
						
						styles.normalStyle.Draw(rcRight, tokenRight.text.GetString(), false, false, false, false);

						if (tokenRight.parent != null && tokenRight.parent.syntaxError != null)
							DrawWavyUnderline(rcRight, new Color(1f, 0f, 0f, .8f));
					}
				
					if (!isHiddenLeft)
					{
						var rcLeft = GetTextRect(spanLeft);
						var rcLeftPlus = new Rect(rcLeft.x - 1f, rcLeft.yMax - 1f, rcLeft.width + 2f, 1f);
					
						var tokenLeft = textBuffer.formattedLines[matchingBraceLeft.line].tokens[matchingBraceLeft.index];
						
						FillRect(rcLeft, matchingColor);
					
						var oldColor = GUI.color;
						GUI.color = styles.normalStyle.normal.textColor;
						GUI.DrawTexture(rcLeftPlus, EditorGUIUtility.whiteTexture);
						GUI.color = oldColor;
						
						styles.normalStyle.Draw(rcLeft, tokenLeft.text.GetString(), false, false, false, false);
				
						if (tokenLeft.parent != null && tokenLeft.parent.syntaxError != null)
							DrawWavyUnderline(rcLeft, new Color(1f, 0f, 0f, .8f));
					}
				}
			}
		}

		if (enableCodeFolding && Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			var rect = new Rect(
				x: marginLeft - 9f * pointSize + smoothScrollPosition.x - 4f,
				y: 2f * pointSize,
				width: 9f * pointSize,
				height: 9f * pointSize);
			
			if (Event.current.mousePosition.x >= rect.x && Event.current.mousePosition.x < rect.xMax)
			{
				var nextLine = fromLine;
				var numSpans = collapsibleTextSpansInView.Count;
				for (var i = 0; i < numSpans; ++i)
				{
					var span = collapsibleTextSpansInView[i];
					if (span.span.Start.line < fromLine)
						continue;
					rect.y = GetLineOffset(span.span.Start.line) + 2f * pointSize;
					if (rect.Contains(Event.current.mousePosition))
					{
						Event.current.Use();
						span.IsCollapsed = !span.IsCollapsed;
						textBuffer.NotifyAllEditorsOnUnfold(span);
						goto endScrollViewAndExit;
					}
					if (span.IsCollapsed)
						nextLine = span.span.End.line + 1;
					else
						nextLine = span.span.Start.line + 1;
				}
			}
		}
		if (enableCodeFolding)
		{
			var eventType = Event.current.type;
			if (eventType == EventType.MouseMove || findHighlightedCollapsibleTextSpan && eventType == EventType.Repaint)
			{
				findHighlightedCollapsibleTextSpan = false;
				
				if (codeViewDragging)
				{
					if (highlightedCollapsibleTextSpan != null)
					{
						highlightedCollapsibleTextSpan = null;
						needsRepaint = true;
					}
				}
				else
				{
					var rect = new Rect(
						x: marginLeft - 9f * pointSize + smoothScrollPosition.x - 4f,
						y: 0f,
						width: 9f * pointSize - 1f,
						height: 0f);
					
					var oldHighlighted = highlightedCollapsibleTextSpan;
					highlightedCollapsibleTextSpan = null;
					
					if (Event.current.mousePosition.x >= rect.x && Event.current.mousePosition.x <= rect.xMax)
					{
						for (var i = collapsibleTextSpansInView.Count; i -- > 0; )
						{
							var span = collapsibleTextSpansInView[i];

							if (IsLineBeginningCollapsed(span.span.Start.line) ||
								!span.IsCollapsed && IsLineBeginningCollapsed(span.span.End.line))
							{
								continue;
							}
					
							rect.y = GetLineOffset(span.span.Start.line) + pointSize;
							if (span.IsCollapsed)
							{
								rect.height = (GetSoftLineBreaks(span.span.Start.line).Count + 1) * lineHeight;
							}
							else
							{
								rect.yMax = GetLineOffset(span.span.End.line) + (GetSoftLineBreaks(span.span.End.line).Count + 1) * lineHeight;
							}

							if (rect.Contains(Event.current.mousePosition))
							{
								highlightedCollapsibleTextSpan = span;
								highlightedCollapsibleTextSpanRect = rect;
								break;
							}
						}
					}
					
					if (eventType != EventType.Repaint && oldHighlighted != highlightedCollapsibleTextSpan)
					{
						needsRepaint = true;
					}
				}
			}
		}
		
		bool isDragEvent = Event.current.type == EventType.DragPerform || Event.current.type == EventType.DragUpdated;
		if (isDragEvent || Event.current.isMouse || Event.current.type == EventType.Repaint && simulateLastMouseEvent)
		{
			simulateLastMouseEvent = simulateLastMouseEvent && !Event.current.isMouse && !isDragEvent;
			Event current = simulateLastMouseEvent ? new Event(lastMouseEvent) : Event.current;
			if (!simulateLastMouseEvent)
				lastMouseEvent = new Event(Event.current);
			simulateLastMouseEvent = false;

			ProcessEditorMouse(marginLeft, current);

			if (codeViewDragging)
			{
				autoScrollLeft = !wordWrapping && lastMouseEvent.mousePosition.x < codeViewRect.x;
				autoScrollRight = !wordWrapping && lastMouseEvent.mousePosition.x >= codeViewRect.xMax;
				autoScrollUp = lastMouseEvent.mousePosition.y < codeViewRect.y;
				autoScrollDown = lastMouseEvent.mousePosition.y >= codeViewRect.yMax;
			}

			if (Event.current.type == EventType.Used)
			{
				goto endScrollViewAndExit;
			}
		}

		// Draw caret
		if (hasCodeViewFocus && Event.current.type == EventType.Repaint && CanEdit())
		{
			FGTextBuffer.CaretPos position = codeViewDragging && mouseDownOnSelection ? mouseDropPosition : caretPosition;

			float caretTime = FGTextBufferManager.isApplicationActive ? ((float) (frameTime - caretMoveTime).TotalSeconds) % 1f : 0f;
			isCaretOn = caretTime < 0.5f;
			if ((isCaretOn || pingTimer > 0f && pingStartTime != default(System.DateTime)) && position.line >= fromLine && position.line <= toLine)
			{
				var caretRect = GetCaretRect(position);
				FillRect(caretRect, isCaretOn ? styles.caretColor : styles.scrollViewColor);
			}
			
			//if (Input.compositionString != "" && selectionStartPosition != null)
			//{
			//	var caretRect = GetCaretRect(selectionStartPosition);
			//	var caretPos = new Vector2(caretRect.x, caretRect.y + lineHeight);
			//	Input.compositionCursorPos = caretPos;//EditorGUIUtility.GUIToScreenPoint(caretPos);
			//}
		}

		var scrollPosX = (Mathf.Floor(smoothScrollPosition.x * EditorGUIUtility.pixelsPerPoint + 0.85f) - 0.35f) / EditorGUIUtility.pixelsPerPoint;

		// Draw left margin
		if (Event.current.type == EventType.Repaint)
		{
			if (showLineNumbers || trackChanges || enableCodeFolding)
			{
				var rect = new Rect(-4f, -4f, marginLeft - 1f + scrollPosX, contentHeight);
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Arrow);

				if (enableCodeFolding)
					rect.Set(-4f, -4f, marginLeft /*- 0f * pointSize*/ + scrollPosX, contentHeight);
				else
					rect.Set(-4f, -4f, marginLeft - 1f + scrollPosX, contentHeight);

				// if the source code is shorter than the view...
				if (rect.height < scrollViewRect.height)
					rect.height = scrollViewRect.height;
				FillRect(rect, styles.lineNumbersBackground);

				if (!enableCodeFolding)
				{
					rect.xMin = marginLeft - 5f + scrollPosX;
					rect.width = 1f;
					FillRect(rect, styles.lineNumbersSeparator);
				}
			}
		}
		
		if (enableCodeFolding)
		{
			if (Event.current.type == EventType.Repaint)
			{
				var rect = new Rect(
					x: marginLeft - 9f * pointSize + scrollPosX - 4f,
					y: 0f,
					width: 9f * pointSize,
					height: 0f);
				
				var drawFromLine = fromLine;
				
				for (var i = collapsibleTextSpansInView.Count; i --> 0; )
				{
					var span = collapsibleTextSpansInView[i];
					if (span.span.End.line < fromLine)
						continue;
					
					if (span.IsCollapsed)
					{
						drawFromLine = span.span.End.line + 1;
						if (span != highlightedCollapsibleTextSpan)
							continue;
					}
					
					rect.y = GetLineOffset(span.span.Start.line, false) + pointSize;
					rect.yMax = GetLineOffset(span.span.End.line + 1, true);
					
					if (rect.height <= 0.0f)
						continue;
					
					//if (!rect.Overlaps(codeViewRect))
					//	continue;
					
					if (span == highlightedCollapsibleTextSpan)
					{
						FillRect(highlightedCollapsibleTextSpanRect, styles.currentLineColor);
					}
					
					if (!span.IsCollapsed)
					{
						var fillRect = rect;
						
						fillRect.x += 4f * pointSize;
						fillRect.yMin += 11f * pointSize;
						fillRect.width = pointSize;
						FillRect(fillRect, styles.foldingButton);
						
						fillRect.yMin = fillRect.yMax - pointSize;
						fillRect.width = 5f * pointSize;
						FillRect(fillRect, styles.foldingButton);
						
#if Si3_WARNINGS
						var firstNWSPos = textBuffer.FirstNonWhitespacePos(span.span.Start.line, 0);
						var topLeftStart = BufferToViewPosition(firstNWSPos, false);
						fillRect.x = topLeftStart.x + marginLeft;
						fillRect.yMin = rect.y + topLeftStart.y + lineHeight;
						fillRect.width = 1.0f;
						fillRect.height -= 2.0f;
						FillRect(fillRect, styles.currentLineColor);
#endif
					}
				}

				var spanIndex = 0;
				for (var i = fromLine; i <= toLine && spanIndex < collapsibleTextSpansInView.Count; ++i)
				{
					var span = collapsibleTextSpansInView[spanIndex];
					while (i > span.span.Start.line)
					{
						++spanIndex;
						if (spanIndex == collapsibleTextSpansInView.Count)
						{
							i = toLine + 1;
							break;
						}
						span = collapsibleTextSpansInView[spanIndex];
					}
					if (i < span.span.Start.line)
					{
						i = span.span.Start.line - 1;
						continue;
					}
					else if (i > toLine)
					{
						break;
					}
					
					var isCollapsed = span.IsCollapsed;
					//while (++spanIndex < collapsibleTextSpansInView.Count)
					//{
					//	span = collapsibleTextSpansInView[spanIndex];
					//	if (span.span.Start.line != i)
					//		break;
					//	if (span.isCollapsed)
					//	{
					//		isCollapsed = true;
					//		break;
					//	}
					//}

					rect.y = GetLineOffset(i) + pointSize;
					rect.height = 11f * pointSize;
					
					var fillRect = rect;
					
					FillRect(fillRect, styles.lineNumbersBackground);
					fillRect.y += pointSize;
					fillRect.height -= 2f * pointSize;
					FillRect(fillRect, styles.foldingButton);
					fillRect.x += pointSize;
					fillRect.y += pointSize;
					fillRect.width -= 2f * pointSize;
					fillRect.height -= 2f * pointSize;
					FillRect(fillRect, styles.lineNumbersBackground);
					fillRect.x += pointSize;
					fillRect.y += 3f * pointSize;
					fillRect.width -= 2f * pointSize;
					fillRect.height = pointSize;
					FillRect(fillRect, styles.foldingButton);
					if (isCollapsed)
					{
						fillRect.x += 2f * pointSize;
						fillRect.y -= 2f * pointSize;
						fillRect.width = pointSize;
						fillRect.height += 4f * pointSize;
						FillRect(fillRect, styles.foldingButton);
						
						i = span.span.End.line;
					}
				}
			}
		}

		if (Event.current.type == EventType.Repaint)
		{
			if (showLineNumbers)
			{
				var cache = lineNumberCachedStrings ?? (lineNumberCachedStrings = new string[textBuffer.lines.Count]);
				if (cache.Length < textBuffer.lines.Count)
					Array.Resize(ref cache, textBuffer.lines.Count);

				for (int i = fromLine; i <= toLine; ++i)
				{
					var overlappingSpan = GetCharCollapsedSpan(i, -1);
					//var isLineBeginningCollapsed = IsLineBeginningCollapsed(i);
					if (overlappingSpan != null)
					{
						//if (toLine < textBuffer.lines.Count)
						//	++toLine;
						i = overlappingSpan.span.End.line;
						continue;
					}
					
					tempContent.text = cache[i] ?? (cache[i] = (i + 1 + lineNumbersOffset).ToString());
					var rect = new Rect(scrollPosX, GetLineOffset(i), lineNumbersWidth, lineHeight);
					if (caretPosition.line == i)
						styles.currentLineNumberStyle.Draw(rect, tempContent, false, false, false, false);
					else
						styles.lineNumbersStyle.Draw(rect, tempContent, false, false, false, false);
				}

				lineNumberCachedStrings = cache;
			}

			if (trackChanges)
			{
				Rect rect = new Rect(
					x: marginLeft - 13f + scrollPosX,
					y: 0f,
					width: 5f,
					height: 0f);
				
				if (enableCodeFolding)
					rect.x -= pointSize * 9f;

				for (int i = fromLine; i <= toLine; ++i)
				{
					rect.yMin = GetLineOffset(i, false);
					rect.height = lineHeight;
						
					int savedVersion = textBuffer.formattedLines[i].savedVersion;
					int version = textBuffer.formattedLines[i].lastChange;
					var change = savedVersion > 0 || version > 0 ? version.CompareTo(savedVersion) : -2;
					
					while (i + 1 <= toLine)
					{
						var overlappingSpan = GetCharCollapsedSpan(i + 1, -1);
						if (overlappingSpan == null)
							break;
						
						var collapsedSpanEndLine = overlappingSpan.span.End.line;
						for (var j = i + 1; change < 1 && j <= collapsedSpanEndLine; ++j)
						{
							int sv = textBuffer.formattedLines[j].savedVersion;
							int v = textBuffer.formattedLines[j].lastChange;
							var c = sv > 0 || v > 0 ? v.CompareTo(sv) : -2;
							if (c > change)
								change = c;
						}
						i = collapsedSpanEndLine;
					}

					if (change == 0)
						FillRect(rect, styles.trackChangesAfterSaveColor);
					else if (change == 1)
						FillRect(rect, styles.trackChangesBeforeSaveColor);
					else if (change == -1)
						FillRect(rect, styles.trackChangesRevertedColor);
				}
			}
		}
		
		if (needsReformat)
		{
			needsReformat = false;
			lastTabSize = SISettings.tabSize;
			lastLineSpacing = SISettings.lineSpacing;
			
			InvalidateSoftLineBreaks();
		
			if (scrollPositionInitialized)
			{
				smoothScrollPosition = scrollPosition;
				currentScrollVelocity = Vector2.zero;
				lastSmoothScrollTime = default(System.DateTime);
			}
			
			EndScrollView(true, scrollViewState);
			Repaint();
			return;
		}
		
		if (Event.current.type == EventType.Repaint && pingTimer > 0f && pingStartTime != default(System.DateTime))
		{
			var rcPing = scrollToRect;
			var caretRect = GetCaretRect(caretPosition);
			rcPing.y = caretRect.y;
			if (selectionStartPosition != null)
			{
				if (selectionStartPosition < caretPosition)
					rcPing.x = caretRect.xMax - scrollToRect.width - 1f - marginLeft;
				else
					rcPing.x = caretRect.x - marginLeft + 1f;
			}
			
			DrawPing(marginLeft, rcPing, false);
			Repaint();
		}

	endScrollViewAndExit:

		if (needsRepaint)
		{
			needsRepaint = false;
			Repaint();
		}
		else if (smoothScrollPosition != scrollPosition)
			Repaint();
		else if (Event.current.type == EventType.Repaint)
			scrollInstantly = false;
		
		if (Event.current.type == EventType.Repaint)
		{
			if (tryArgumentsHint)
				UpdateArgumentsHint(true);
			else if (argumentsHint != null && caretMoveTime == frameTime)
				UpdateArgumentsHint(true);
			tryArgumentsHint = false;
		}
		
		EndScrollView(true, scrollViewState);

		if (hasCodeViewFocus && Event.current.type == EventType.Repaint && CanEdit())
		{
			//if (Input.compositionString != "" && selectionStartPosition != null)
			{
				var caretRect = GetCaretRect(caretPosition);
				var caretPos = new Vector2(caretRect.x, caretRect.y + charSize.y + 7f) - scrollPosition + scrollViewRect.min;
				caretPos += inspectorEditorTopLeft;
				Input.compositionCursorPos = EditorGUIUtility.pixelsPerPoint * caretPos;
			}
		}
	}
	
	void ClearLinesInView()
	{
		for (var i = linesInView.Count; i --> 0; )
			EditorLine.Release(linesInView[i]);
		linesInView.Clear();
		nextTempSingleToken = 0;
	}
	
	[NonSerialized]
	private int laidOutLinesFrom;
	[NonSerialized]
	private int laidOutLinesTo;

	void LayoutLinesInView(int fromLine, int toLine)
	{
		TextPosition selectionFromPos;
		TextPosition selectionToPos;
		if (!IsLoading && hasSelection)
		{
			if (caretPosition < selectionStartPosition)
			{
				selectionFromPos.line = caretPosition.line;
				selectionFromPos.index = caretPosition.characterIndex;
				selectionToPos.line = selectionStartPosition.line;
				selectionToPos.index = selectionStartPosition.characterIndex;
			}
			else
			{
				selectionFromPos.line = selectionStartPosition.line;
				selectionFromPos.index = selectionStartPosition.characterIndex;
				selectionToPos.line = caretPosition.line;
				selectionToPos.index = caretPosition.characterIndex;
			}
		}
		else
		{
			selectionFromPos = TextPosition.invalid;
			selectionToPos = TextPosition.invalid;
		}
		
		float lineHeight = LineHeight;
		
		var editorLineIndex = BufferToEditorLine(fromLine);
		if (editorLineIndex < fromLine)
		{
			toLine += fromLine - editorLineIndex;
			fromLine = editorLineIndex;
		}
		
		Rect rect = new Rect();
		rect.height = lineHeight;
		rect.y = GetLineOffset(fromLine) - lineHeight;
				
		for (int i = fromLine; i <= toLine; ++i)
		{
			var overlappingSpan = GetCharCollapsedSpan(i, -1);
			//var isLineBeginningCollapsed = IsLineBeginningCollapsed(i);
			if (overlappingSpan != null)
			{
				if (i == fromLine)
				{
					var startLine = overlappingSpan.span.Start.line;
					//toLine += i - startLine - 1;
					i = startLine - 1; // Continue with the startLine (-1 to account for the ++i in the for loop).
				}
				else
				{
					var endLine = overlappingSpan.span.End.line;
					//var numLinesToSkip = endLine - i;
					if (endLine > i)
						i = endLine; // Continue with the line after endLine.
				}
				//if (toLine >= textBuffer.lines.Count)
				//	toLine = textBuffer.lines.Count - 1;
				//if (i == fromLine)
				//	i = --fromLine;
				//if (toLine < textBuffer.lines.Count)
				//	++toLine;
				continue;
			}
			
			var editorLine = CreateEditorLine(i);
			linesInView.Add(editorLine);
			
			rect.x = marginLeft;
			rect.y += lineHeight;

			var charIndex = 0;
			var tokens = editorLine.tokens;
			
			var lineTokens = editorLine.lineTokens;
			var lineTexts = editorLine.lineTexts;
			var lineRects = editorLine.lineRects;
			var lineTokenStartPos = editorLine.lineTokenStartPos;
			var collapsedTokenSpans = editorLine.collapsedTokenSpans;
				
			var softLineBreaks = GetSoftLineBreaks(i);
			var numBreaks = softLineBreaks.Count;

			//int column = 0;
			int softRow = 0;
			int numTokens = tokens.Count;
				
			lineRects.Clear();
			lineTexts.Clear();
			lineTokens.Clear();
			lineTokenStartPos.Clear();

			if (numTokens == 0)
			{
				if (selectionFromPos.line <= i && selectionToPos.line > i)
				{
					var selectionRect = rect;
					selectionRect.width = charSize.x;
					editorLine.AddSelectionRect(selectionRect);
				}
				
				lineTokenStartPos.Add(new TextPosition(i, 0));
				lineRects.Add(rect);

				continue;
			}
			
			SyntaxToken token = null;
			int currentTokenLine = i;
			int lineCharIndex = 0;
			bool isCollapsedToken = false;

			for (var j = 0; j < numTokens; ++j)
			{
				token = tokens[j];
					
				if (token == null)
					continue;
					
				isCollapsedToken = token.tokenKind == SyntaxToken.Kind.Collapsed;

				// Add missing token error
				if (token.tokenKind == SyntaxToken.Kind.Missing)
				{
					var rcMissing = new Rect(rect.xMax, rect.yMin, charSize.x * 2f, charSize.y + lastLineSpacing);
					lineRects.Add(rcMissing);
					lineTexts.Add("");
					lineTokens.Add(token);
					lineTokenStartPos.Add(new TextPosition(token.Line, lineCharIndex));

					continue;
				}
				
				//currentTokenLine = token.Line;
				//if (j > 0 && collapsedTokenSpans.Count > 0)
				//{
				//	var prevCollapsedTokenSpan = collapsedTokenSpans[j - 1];
				//	if (prevCollapsedTokenSpan != null)
				//	{
				//		var spanEnd = prevCollapsedTokenSpan.span.End;
				//		currentTokenLine = spanEnd.line;
				//		lineCharIndex = spanEnd.index;
				//	}
				//	//if (currentTokenLine != tokenLine)
				//	//{
				//	//	tokenLine = currentTokenLine;
						
				//	//	lineCharIndex = 0;
						
				//	//	var tokenFormattedLine = token.formattedLine;
				//	//	if (tokenFormattedLine != null)
				//	//	{
				//	//		var tokenLineToken = tokenFormattedLine.tokens;
				//	//		for (var t = token.TokenIndex; t --> 0; )
				//	//		{
				//	//			lineCharIndex += token.text.length;
				//	//		}
				//	//	}
				//	//}
				//}
					
				int tokenTextStart = 0;
				int tokenTextLength = token.text.length;
				while (tokenTextStart < tokenTextLength)
				{
					int rowStart = softRow > 0 ? softLineBreaks[softRow - 1] : 0;
					int rowLength = softRow < numBreaks ? softLineBreaks[softRow] - charIndex : int.MaxValue;
					int charsToDraw = Math.Min(tokenTextLength - tokenTextStart, rowLength);

					if (charsToDraw > 0)
					{
						string tokenTextPart;
						if (tokenTextStart == 0 && charsToDraw == token.text.length)
							tokenTextPart = token.text.GetString();
						else
							tokenTextPart = token.text.Substring(tokenTextStart, charsToDraw);
						
						/*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/ /*test: 蔵岡 恒一郎 / Koichiro Kuraoka*/
						
						charIndex += charsToDraw;
						rect.width = GetTextWidth(tokenTextPart, 0, charsToDraw, rect.x - marginLeft);
						
						var textFromPos = new TextPosition(currentTokenLine, lineCharIndex);
						var textToPos = new TextPosition(currentTokenLine, lineCharIndex + charsToDraw);
						if (isCollapsedToken)
						{
							var span = collapsedTokenSpans[j].span;
							//textFromPos = span.Start;
							textToPos = span.End;
						}
						
						//lineCharIndex += charsToDraw;
						
						if (selectionFromPos < textToPos && selectionToPos > textFromPos)
						{
							var selectionRect = rect;
							
							if (!isCollapsedToken && textToPos.line == selectionToPos.line)
							{
								var endOffset = textToPos.index - selectionToPos.index;
								if (endOffset > 0)
								{
									selectionRect.width = GetTextWidth(tokenTextPart, 0, charsToDraw - endOffset, rect.x - marginLeft);
								}
							}
							else if (rowLength == int.MaxValue && textToPos.line < selectionToPos.line && j == numTokens - 1)
							{
								selectionRect.width += charSize.x;
							}
							
							if (!isCollapsedToken && textFromPos.line == selectionFromPos.line)
							{
								var startOffset = selectionFromPos.index - textFromPos.index;
								if (startOffset > 0)
								{
									selectionRect.xMin += GetTextWidth(tokenTextPart, 0, startOffset, rect.x - marginLeft);
								}
							}
							
							editorLine.AddSelectionRect(selectionRect);
						}
						
						//var tokenStyle = GetTokenStyle(token);
						var tokenStyleID = token.style;
						if (tokenStyleID == StyleID.NotSet)
						{
							textBuffer.Parser.SetTokenStyleID(token);
							tokenStyleID = token.style;
						}

						// Add hyperlink
						if (tokenStyleID == FGTextEditor.StyleID.Hyperlink || tokenStyleID == FGTextEditor.StyleID.MailTo)
						{
							lineRects.Add(rect);
							lineTexts.Add(tokenTextPart);
							lineTokens.Add(token);
							lineTokenStartPos.Add(textFromPos);
							lineCharIndex += tokenTextPart.Length;
						}
						else
						{
							// Add tokenTextPart text
							
							int indexOfTab;
							var text = tokenTextPart;
							while (text != "")
							{
								indexOfTab = text.IndexOf('\t');
								if (indexOfTab < 0)
								{
									lineRects.Add(rect);
									lineTexts.Add(text);
									lineTokens.Add(token);
									lineTokenStartPos.Add(new TextPosition(textFromPos.line, lineCharIndex));
									lineCharIndex += text.Length;
									break;
								}

								if (indexOfTab > 0)
								{
									var textToTab = text.Substring(0, indexOfTab);

									var rectPart = rect;
									rectPart.width = GetTextWidth(text, 0, indexOfTab, rectPart.x - marginLeft);

									lineRects.Add(rectPart);
									lineTexts.Add(textToTab);
									lineTokens.Add(token);
									lineTokenStartPos.Add(new TextPosition(textFromPos.line, lineCharIndex));
									lineCharIndex += textToTab.Length;

									rect.xMin = rectPart.xMax;
								}

								var tabsStartIndex = indexOfTab;
								do
								{
									++indexOfTab;
								} while (indexOfTab < text.Length && text[indexOfTab] == '\t');

								var numTabs = indexOfTab - tabsStartIndex;
								{
									// Insert tabs as a line token
									var rectPart = rect;
									rectPart.width = GetTextWidth(text, tabsStartIndex, indexOfTab, rectPart.x - marginLeft);

									lineRects.Add(rectPart);
									lineTexts.Add(text.Substring(tabsStartIndex, numTabs));
									lineTokens.Add(token);
									lineTokenStartPos.Add(new TextPosition(textFromPos.line, lineCharIndex));
									lineCharIndex += numTabs;

									rect.xMin = rectPart.xMax;
								}

								if (indexOfTab < text.Length)
								{
									text = text.Substring(indexOfTab);
									//rect.xMin = marginLeft + GetCharXOffset(charIndex - text.Length, i, rowStart);
								}
								else
								{
									break;
								}
							}
						}
					}

					rect.xMin = rect.xMax;

					tokenTextStart += charsToDraw;
					if (tokenTextStart < tokenTextLength)
					{
						rect.x = marginLeft;
						rect.width = 0f;
						rect.y += lineHeight;
						++softRow;
					}
				}

				rect.xMin = rect.xMax;
				
				if (isCollapsedToken)
				{
					var spanEnd = collapsedTokenSpans[j].span.End;
					currentTokenLine = spanEnd.line;
					lineCharIndex = spanEnd.index;
				}
			}
			
			// Add end position of last token
			lineTokenStartPos.Add(new TextPosition(currentTokenLine, lineCharIndex));

			// Add rect of newline character
			rect.width = charSize.x;
			lineRects.Add(rect);

			if (selectionFromPos.line == currentTokenLine && selectionFromPos.index == lineCharIndex)
			{
				editorLine.AddSelectionRect(rect);
			}
			
			widestLine = Mathf.Ceil(Mathf.Max(widestLine, rect.xMax));
		}
	}

	void DrawLinesInView()
	{
		var selectionColor = hasCodeViewFocus ? styles.activeSelectionColor : styles.passiveSelectionColor;
		var addCursorRect = !codeViewDragging;
			
		var numLinesInView = linesInView.Count;
		for (int i = 0; i < numLinesInView; ++i)
		{
			var editorLine = linesInView[i];

			// Draw selection
			var selectionRects = editorLine.selectionRects;
			if (selectionRects.Count > 0)
			{
				var numRects = selectionRects.Count;
				for (var j = 0; j < numRects; ++j)
				{
					DrawSelectionRect(selectionRects[j], selectionColor, addCursorRect);
				}
			}
		}
		
		// Draw ping
		if (pingTimer > 0f && pingStartTime != default(System.DateTime))
		{
			pingTimer = 1f - ((float) (frameTime - pingStartTime).TotalSeconds) * 0.5f;
			if (pingTimer < 0f)
			{
				pingTimer = 0f;
				pingStartTime = default(System.DateTime);
			}

			var rcPing = scrollToRect;
			var caretRect = GetCaretRect(caretPosition);
			rcPing.y = caretRect.y;
			if (selectionStartPosition != null)
			{
				if (selectionStartPosition < caretPosition)
					rcPing.x = caretRect.xMax - scrollToRect.width - 1f - marginLeft;
				else
					rcPing.x = caretRect.x - marginLeft + 1f;
			}
			scrollToRect = rcPing;
				
			DrawPing(marginLeft, rcPing, true);
		}

		for (int i = 0; i < numLinesInView; ++i)
		{
			var editorLine = linesInView[i];

			var lineTokens = editorLine.lineTokens;
			var lineTexts = editorLine.lineTexts;
			var lineRects = editorLine.lineRects;
			var lineTokenStartPos = editorLine.lineTokenStartPos;
			var collapsedTokenSpans = editorLine.collapsedTokenSpans;
			
			// Draw text
			var numLineTexts = lineTexts.Count;
			for (var j = 0; j < numLineTexts; ++j)
			{
				var rect = lineRects[j];
				var text = lineTexts[j];
				var token = lineTokens[j];
					
				// Draw missing token error
				if (token.tokenKind == SyntaxToken.Kind.Missing)
				{
					DrawWavyUnderline(rect, new Color(1f, 0f, 0f, .8f));
					continue;
				}
					
				var tokenStyle = GetTokenStyle(token);
					
				// Draw hyperlink
				if (token.style == FGTextEditor.StyleID.Hyperlink || token.style == FGTextEditor.StyleID.MailTo)
				{
					if (GUI.Button(rect, text, tokenStyle))
					{
						if (token.style == FGTextEditor.StyleID.Hyperlink)
							Application.OpenURL(token.text);
						else
							Application.OpenURL("mailto:" + token.text);
					}

					// show the "Link" cursor when the mouse is hovering over this rectangle.
					EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
						
					continue;
				}

				// Highlighting references
				if (!hasSelection && pingTimer == 0f && SISettings.referenceHighlighting)
				{
					if (highlightedSymbol != null)
					{
						if (token.parent != null && token.parent.resolvedSymbol != null)
						{
							var resolvedSymbol = token.parent.resolvedSymbol.GetGenericSymbol();
							if (SymbolDefinition.builtInTypes_dynamic == null ||
								resolvedSymbol != SymbolDefinition.builtInTypes_dynamic.GetThisInstance())
							{
								if (resolvedSymbol == highlightedSymbol)
								{
									var referenceColor = GetReferenceHighlightColor(token);
									FillRect(rect, referenceColor);
								}
							}
						}
					}
					else if (highlightedPPSymbol != null)
					{
						if (token.tokenKind == SyntaxToken.Kind.PreprocessorSymbol && token.text == highlightedPPSymbol)
						{
							var referenceColor = GetReferenceHighlightColor(token);
							FillRect(rect, referenceColor);
						}
					}
				}
					
				// Underline errors
				var errorNode = token.parent;
				if (errorNode != null && token.tokenKind != SyntaxToken.Kind.Missing)
				{
					if (errorNode.syntaxError != null)
					{
						DrawWavyUnderline(rect, new Color(1f, 0f, 0f, .8f));
					}
					else if (errorNode.semanticError != null ||
						errorNode.resolvedSymbol != null && errorNode.resolvedSymbol.kind == SymbolKind.Error)
					{
						DrawWavyUnderline(rect, new Color(1f, 0f, 1f, .8f));
					}
				}

				// Draw token text, or part of it
				tokenStyle.Draw(rect, text, false, false, false, false);
				
				if (token.tokenKind == SyntaxToken.Kind.Collapsed)
				{
					rect.yMin -= 1.0f;
					rect.x += 2.0f;
					rect.width -= 4.0f;
					DrawRectFrame(rect, styles.foldedTextStyle.normal.textColor);
				}
			}
		}
	}

	private GUIStyle GetTokenStyle(SyntaxToken token)
	{
		var tokenStyleID = token.style;
		if (tokenStyleID == StyleID.NotSet)
		{
			textBuffer.Parser.SetTokenStyleID(token);
			tokenStyleID = token.style;
		}
		
		if (token.tokenKind == SyntaxToken.Kind.ContextualKeyword)
		{
			tokenStyleID = token.text == "value" ? FGTextEditor.StyleID.Parameter : FGTextEditor.StyleID.Keyword;
			if (token.text == "var" && token.parent != null && (token.parent.resolvedSymbol == null || token.parent.resolvedSymbol.kind == SymbolKind.Error))
				FGResolver.ResolveNode(token.parent.parent);
			return textBuffer.styles[tokenStyleID];
		}
		
		var leaf = token.parent;
		if (leaf != null && leaf.parent != null)
		{
			if (token.tokenKind == SyntaxToken.Kind.Keyword)
			{
				if ((token.text == "base" || token.text == "this" || token.text == "new") && (leaf.resolvedSymbol == null || leaf.syntaxError != null))
					FGResolver.ResolveNode(leaf.parent);
			}
			else if (token.tokenKind == SyntaxToken.Kind.Identifier)
			{
				if (leaf.resolvedSymbol == null || leaf.syntaxError != null)
					FGResolver.ResolveNode(leaf.parent);
				
				var symbol = leaf.resolvedSymbol;
				
				if (symbol != null)
				{
					var kind = symbol.kind;
					
					if (kind == SymbolKind.Constructor || kind == SymbolKind.Destructor)
					{
						var type = symbol.parentSymbol != null ? symbol.parentSymbol : null;
						if (type != null && !(type is TypeDefinitionBase))
							type = type.parentSymbol;
						if (type is TypeDefinitionBase)
						{
							kind = type.kind;
							symbol = type;
						}
					}
					
					switch (kind)
					{
					case SymbolKind.Null:
						tokenStyleID = FGTextEditor.StyleID.BuiltInLiteral;
						break;
					case SymbolKind.Namespace:
						tokenStyleID = FGTextEditor.StyleID.Namespace;
						break;
					case SymbolKind.Class:
						tokenStyleID = FGTextEditor.StyleID.ReferenceType;
						break;
					case SymbolKind.Struct:
						tokenStyleID = FGTextEditor.StyleID.ValueType;
						break;
					case SymbolKind.Interface:
						tokenStyleID = FGTextEditor.StyleID.InterfaceType;
						break;
					case SymbolKind.Enum:
						tokenStyleID = FGTextEditor.StyleID.EnumType;
						break;
					case SymbolKind.Delegate:
						tokenStyleID = FGTextEditor.StyleID.DelegateType;
						break;
					case SymbolKind.Parameter:
					case SymbolKind.CatchParameter:
						tokenStyleID = FGTextEditor.StyleID.Parameter;
						break;
					case SymbolKind.TypeParameter:
						tokenStyleID = FGTextEditor.StyleID.TypeParameter;
						break;
					case SymbolKind.Label:
					case SymbolKind.EnumMember:
						tokenStyleID = FGTextEditor.StyleID.EnumMember;
						break;
					case SymbolKind.Method:
					case SymbolKind.MethodGroup:
					case SymbolKind.Accessor:
					case SymbolKind.Constructor:
					case SymbolKind.Destructor:
						tokenStyleID = FGTextEditor.StyleID.Method;
						break;
					case SymbolKind.CaseVariable:
					case SymbolKind.ForEachVariable:
					case SymbolKind.FromClauseVariable:
					case SymbolKind.Variable:
					case SymbolKind.TupleDeconstructVariable:
					case SymbolKind.OutVariable:
					case SymbolKind.IsVariable:
					case SymbolKind.LocalConstant:
						tokenStyleID = FGTextEditor.StyleID.Variable;
						break;
					case SymbolKind.Event:
						tokenStyleID = FGTextEditor.StyleID.Event;
						break;
					case SymbolKind.Property:
						tokenStyleID = FGTextEditor.StyleID.Property;
						break;
					case SymbolKind.ConstantField:
					case SymbolKind.Field:
						if (symbol.parentSymbol != null && symbol.parentSymbol.kind == SymbolKind.Enum)
						{
							tokenStyleID = FGTextEditor.StyleID.EnumMember;
						}
						else
						{
							var typeOfSymbol = symbol.TypeOf();
							if (typeOfSymbol != null && typeOfSymbol.kind == SymbolKind.Delegate)
								tokenStyleID = FGTextEditor.StyleID.Event;
							else
								tokenStyleID = FGTextEditor.StyleID.Field;
						}
						break;
					}
				}
			}
		}
		
		return textBuffer.styles[tokenStyleID];
	}

	private static void DrawWavyUnderline(Rect rect, Color color)
	{
		var oldColor = GUI.color;
		GUI.color = color;

		rect.yMin = rect.yMax - 2f;
		rect.yMax += 1f;
		GUI.DrawTextureWithTexCoords(rect, wavyUnderline, new Rect(rect.xMin / 6f, 0f, rect.width / 6f, 1f));

		GUI.color = oldColor;
	}
	
	private void DrawSelectionRect(Rect selectionRect, Color color, bool addCursorRect)
	{
		FillRect(selectionRect, color);

		if (addCursorRect)
		{
			// show the "Arrow" cursor when the mouse is hovering over this rectangle.
			EditorGUIUtility.AddCursorRect(selectionRect, MouseCursor.MoveArrow);
		}
	}

	private void DrawSelectionRect(int line, int startCharIndex, int numChars, bool newLine)
	{
		DrawSelectionRect(line, startCharIndex, numChars, newLine, hasCodeViewFocus ? styles.activeSelectionColor : styles.passiveSelectionColor, !codeViewDragging);
	}
	
	private void DrawSelectionRect(int line, int startCharIndex, int numChars, bool newLine, Color color, bool addCursorRect)
	{
		var fromPos = new TextPosition(line, startCharIndex);
		var toPos = new TextPosition(line, startCharIndex + numChars);
		var span = TextSpan.Create(fromPos, toPos);
		
		var collapsedSpan = GetCollapsedSpanForTextSpan(span);
		if (collapsedSpan != null)
			return;
		
		var textRect = GetTextRect(span);
		if (newLine)
			textRect.width += charSize.x;
		DrawSelectionRect(textRect, color, addCursorRect);
		
		////var addCursorRect = !codeViewDragging && color == styles.activeSelectionColor;
		//var lineHeight = LineHeight;

		//if (!wordWrapping)
		//{
		//	var yOffset = GetLineOffset(line);
		//	var fromXOffset = GetCharXOffset(startCharIndex, line, 0);
		//	var toXOffset = GetCharXOffset(startCharIndex + numChars, line, 0);
		//	if (newLine)
		//		toXOffset += charSize.x;
		//	var selectionRect = new Rect(fromXOffset + marginLeft, yOffset, toXOffset - fromXOffset, lineHeight);
		//	DrawSelectionRect(selectionRect, color, addCursorRect);
		//}
		//else
		//{
		//	var yOffset = GetLineOffset(line);
		//	var softLineBreaks = GetSoftLineBreaks(line);

		//	var row = FindFirstIndexGreaterThanOrEqualTo<int>(softLineBreaks, startCharIndex);
		//	if (row < softLineBreaks.Count && startCharIndex == softLineBreaks[row])
		//		++row;
		//	var rowStart = row > 0 ? softLineBreaks[row - 1] : 0;

		//	if (newLine && numChars == 0 && startCharIndex == textBuffer.lines[line].Length)
		//	{
		//		var xOffset = GetCharXOffset(startCharIndex, line, rowStart);
		//		var selectionRect = new Rect(xOffset + marginLeft, yOffset + row * lineHeight, charSize.x, lineHeight);
		//		DrawSelectionRect(selectionRect, color, addCursorRect);

		//		return;
		//	}
			
		//	yOffset += lineHeight * row;
		//	startCharIndex -= rowStart;
		//	while (numChars > 0)
		//	{
		//		var rowLength = (row < softLineBreaks.Count ? softLineBreaks[row] - rowStart : startCharIndex + numChars);
		//		var nChars = Math.Min(numChars, rowLength - startCharIndex);

		//		var fromXOffset = GetCharXOffset(rowStart + startCharIndex, line, rowStart);
		//		var toXOffset = GetCharXOffset(rowStart + startCharIndex + nChars, line, rowStart)
		//			+ (numChars == nChars && newLine ? charSize.x : 0);
		//		var selectionRect = new Rect(fromXOffset + marginLeft, yOffset, toXOffset - fromXOffset, lineHeight);
		//		DrawSelectionRect(selectionRect, color, addCursorRect);

		//		numChars -= nChars;
		//		rowStart += rowLength;
		//		startCharIndex = 0;
		//		++row;
		//		yOffset += lineHeight;
		//	}
		//}
	}
	
	public Color GetReferenceHighlightColor(SyntaxToken token)
	{
		var color = styles.referenceHighlightColor;
		if (!SISettings.highlightWritesInRed)
			return color;
		
		if (highlightedSymbol != null && FGResolver.IsWriteReference(token))
			color = styles.referenceModifyHighlightColor;
		
		return color;
	}

	public void ValidateCarets()
	{
		if (CanEdit())
		{
			enableCodeFolding = CacheEnableCodeFolding();

			if (!ValidateCaret(ref caretPosition))
			{
				selectionStartPosition = null;
				Repaint();
			}
			else if (hasSelection)
			{
				if (!ValidateCaret(ref _selectionStartPosition))
				{
					selectionStartPosition = null;
					Repaint();
				}
			}

			matchedBracesAtCaretPosition = null;
		}
	}

	private bool ValidateCaret(ref FGTextBuffer.CaretPos caret)
	{
		if (caret != null && textBuffer.lines.Count > 0)
		{
			if (caret.line < 0)
			{
				return false;
			}
			else if (caret.line >= textBuffer.lines.Count)
			{
				caret = new FGTextBuffer.CaretPos() { line = textBuffer.lines.Count - 1, characterIndex = 0, column = 0, virtualColumn = 0 };
				return false;
			}
			else if (caret.characterIndex > textBuffer.lines[caret.line].Length)
			{
				caret = new FGTextBuffer.CaretPos() { line = caret.line, characterIndex = 0, column = 0, virtualColumn = 0 };
				return false;
			}
			else
			{
				caret.column = caret.virtualColumn = BufferPositionToEditorColumn(caret.line, caretPosition.characterIndex, caretPosition.virtualColumn);
			}
		}
		return true;
	}
	
	private Rect GetCaretRect(FGTextBuffer.CaretPos position)
	{
		var textPosition = new TextPosition(position.line, position.characterIndex);
		Vector2 topLeft = BufferToViewPosition(textPosition, position.virtualColumn == 0);
		Rect caretRect = new Rect(
			topLeft.x + marginLeft,
			topLeft.y + GetLineOffset(position.line),
			1,
			LineHeight);
		if (SISettings.showThickerCaret)
			caretRect.xMin -= 1f;
		
		return caretRect;
	}
	
	private Rect GetTokenRect(SyntaxToken token)
	{
		Rect result = new Rect();
		
		if (textBuffer == null || token.parent == null)
			return result;
		
		var tokenSpan = textBuffer.GetTokenSpan(token.parent.line, token.parent.tokenIndex);
		result = GetTextRect(tokenSpan);
		
		Vector2 screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(result.xMin, result.yMin));
		result.x = screenPoint.x;
		result.y = screenPoint.y;
		
		return result;
	}
	
	private Rect GetTextRect(TextSpan span)
	{
		var topLeftStart = BufferToViewPosition(span.StartPosition, true);
		var width = GetTextWidth(span.line, span.index, span.index + span.indexOffset, topLeftStart.x);
		Rect result = new Rect(topLeftStart.x + marginLeft, topLeftStart.y + GetLineOffset(span.line), width, LineHeight);
		
		return result;
	}
	
	private float GetCharWidth(char c, float xOffset)
	{
		var charWidth = 0f;
		if (c == '\t')
		{
			var tabSize = lastTabSize * charSize.x;
			var oldXOffest = xOffset;
			xOffset += 0.75f * charSize.x;
			xOffset += tabSize - (xOffset % tabSize);
			charWidth = xOffset - oldXOffest;
		}
		else if (c < 0x7f)
		{
			charWidth = charSize.x;
		}
		else
		{
			tempContent.text = c.ToString();
			charWidth = styles.normalStyle.CalcSize(tempContent).x;
		}
		return charWidth;
	}
	
	private float GetTextWidth(string text, int fromChar, int toChar, float xOffset)
	{
		Font font = null;
		int fontSize = 0;
		FontStyle fontStyle = FontStyle.Normal;

		var length = text.Length;
		if (fromChar >= length)
			return 0f;
		if (toChar >= length)
			toChar = length;
		if (fromChar >= toChar)
			return 0f;

		var result = 0f;
		var tabSize = lastTabSize * charSize.x;
		for (int i = fromChar; i < toChar; ++i)
		{
			var charWidth = 0f;
			var c = text[i];
			if (c == '\t')
			{
				var newXOffset = xOffset + 0.75f * charSize.x;
				newXOffset += tabSize - (newXOffset % tabSize);
				charWidth = newXOffset - xOffset;
			}
			else if (c < 0x7f)
				charWidth = charSize.x;
			else
			{
				if (font == null)
				{
					font = styles.normalStyle.font;
					fontSize = styles.normalStyle.fontSize;
					fontStyle = styles.normalStyle.fontStyle;
					font.RequestCharactersInTexture(text, fontSize, fontStyle);
				}
				
				CharacterInfo chInfo;
				for ( ; i < toChar; i++)
				{
					c = text[i];
					if (c < 0x7f)
					{
						--i;
						break;
					}
					
					font.GetCharacterInfo(c, out chInfo, fontSize, fontStyle);
					charWidth += chInfo.advance;
				}
			}
			xOffset += charWidth;
			result += charWidth;
		}
		return result;
	}
	
	private float GetTextWidth(int line, int fromChar, int toChar, float xOffset)
	{
		Font font = null;
		int fontSize = 0;
		FontStyle fontStyle = FontStyle.Normal;
		
		var lines = textBuffer.lines;
		if (line >= lines.Count)
			return 0f;
		var s = lines[line];
		if (fromChar >= s.Length)
			return 0f;
		if (toChar >= s.Length)
			toChar = s.Length;
		if (fromChar >= toChar)
			return 0f;

		var result = 0f;
		var tabSize = lastTabSize * charSize.x;
		for (int i = fromChar; i < toChar; ++i)
		{
			var charWidth = 0f;
			var c = s[i];
			if (c == '\t')
			{
				var newXOffset = xOffset + 0.75f * charSize.x;
				newXOffset += tabSize - (newXOffset % tabSize);
				charWidth = newXOffset - xOffset;
			}
			else if (c < 0x7f)
				charWidth = charSize.x;
			else
			{
				if (font == null)
				{
					font = styles.normalStyle.font;
					fontSize = styles.normalStyle.fontSize;
					fontStyle = styles.normalStyle.fontStyle;
					font.RequestCharactersInTexture(s, fontSize, fontStyle);
				}
				
				CharacterInfo chInfo;
				for ( ; i < toChar; i++)
				{
					c = s[i];
					if (c < 0x7f)
					{
						--i;
						break;
					}
					
					font.GetCharacterInfo(c, out chInfo, fontSize, fontStyle);
					charWidth += chInfo.advance;
				}
			}
			xOffset += charWidth;
			result += charWidth;
		}
		return result;
	}
	
	public Vector2 GetCharPosition(EditorLine editorLine, int charIndex, int line, int start, bool extendRight)
	{
		if (editorLine == null)
			return new Vector2(0f, GetLineOffset(line));
		
		var lineTokens = editorLine.lineTokens;
		
		var tokenStarts = editorLine.lineTokenStartPos;
		var numTokens = tokenStarts.Count;
		if (numTokens == 0)
			return new Vector2(0f, GetLineOffset(line));
		
		var key = new TextPosition(line, charIndex);
		var i = FindFirstIndexGreaterThanOrEqualTo(tokenStarts, key);
		
		var atTokenStart = i < numTokens && (tokenStarts[i] == key || lineTokens[i].tokenKind == SyntaxToken.Kind.Collapsed);
		if (atTokenStart)
		{
			var tokenRect = editorLine.lineRects[i];
			return new Vector2(tokenRect.x, tokenRect.y);
		}
		
		--i;
		var textIndex = charIndex - tokenStarts[i].index;
		var text = editorLine.lineTexts[i];
		var textLength = text.Length;
		if (textIndex > textLength)
			textIndex = textLength;
		
		var textRect = editorLine.lineRects[i];
		var x = GetTextWidth(text, 0, textIndex, textRect.x);
		return new Vector2(x, textRect.y);
	}
	
	public float GetCharXOffset(int charIndex, int line, int start)
	{
		Font font = null;
		int fontSize = 0;
		FontStyle fontStyle = FontStyle.Normal;
		
		var lines = textBuffer.lines;
		if (line >= lines.Count)
			return 0f;

		var s = lines[line];

		if (s.Length < charIndex)
			charIndex = s.Length;

		var tabSize = lastTabSize * charSize.x;
		var xOffset = 0f;
		for (int i = start; i < charIndex; ++i)
		{
			var c = s[i];
			if (c == ' ')
				xOffset += charSize.x;
			else if (c == '\t')
			{
				xOffset += 0.75f * charSize.x;
				xOffset += tabSize - (xOffset % tabSize);
			}
			else if (c < 0x7f)
				xOffset += charSize.x;
			else
			{
				if (font == null)
				{
					font = styles.normalStyle.font;
					fontSize = styles.normalStyle.fontSize;
					fontStyle = styles.normalStyle.fontStyle;
					font.RequestCharactersInTexture(s, fontSize, fontStyle);
				}
				
				CharacterInfo chInfo;
				for ( ; i < charIndex; i++)
				{
					c = s[i];
					if (c < 0x7f)
					{
						--i;
						break;
					}
					
					font.GetCharacterInfo(c, out chInfo, fontSize, fontStyle);
					xOffset += chInfo.advance;
				}
			}
		}
		return xOffset;
	}
	
	public float GetCharAt(float x, float y, int line)
	{
		Font font = null;
		int fontSize = 0;
		FontStyle fontStyle = FontStyle.Normal;

		var text = textBuffer.lines[line];
		var softLineBreaks = GetSoftLineBreaks(line);
		var numSoftBreaks = softLineBreaks.Count;
		int row = Mathf.Clamp((int) ((y - GetLineOffset(line)) / LineHeight), 0, numSoftBreaks);
		var rowStart = row > 0 ? softLineBreaks[row - 1] : 0;
		var rowEnd = row == numSoftBreaks ? text.Length : softLineBreaks[row];

		var tabSize = lastTabSize * charSize.x;
		float xOffset = 0f, charWidth;
		for (int i = rowStart; i < rowEnd; ++i)
		{
			var c = text[i];
			if (c == '\t')
			{
				var newXOffset = xOffset + 0.75f * charSize.x;
				newXOffset += tabSize - (newXOffset % tabSize);
				charWidth = newXOffset - xOffset;
			}
			else if (c < 0x7f)
				charWidth = charSize.x;
			else
			{
				if (font == null)
				{
					font = styles.normalStyle.font;
					fontSize = styles.normalStyle.fontSize;
					fontStyle = styles.normalStyle.fontStyle;
					font.RequestCharactersInTexture(text, fontSize, fontStyle);
				}
				
				CharacterInfo chInfo;
				font.GetCharacterInfo(c, out chInfo, fontSize, fontStyle);
				charWidth = chInfo.advance;

				//tempContent.text = text[i].ToString();
				//charWidth = styles.normalStyle.CalcSize(tempContent).x;
			}
			if (x <= charWidth)
			{
				return ((float)i) + (x / charWidth);
			}
			x -= charWidth;
			xOffset += charWidth;
		}
		return rowEnd;
	}
	
	[NonSerialized]
	private bool wordSelectMode = false;
	[NonSerialized]
	private FGTextBuffer.CaretPos anchorWordStart;
	[NonSerialized]
	private FGTextBuffer.CaretPos anchorWordEnd;
	[NonSerialized]
	private bool lineSelectMode = false;
	[NonSerialized]
	private bool mouseIsDown = false;
	//[NonSerialized]
	//private int mouseDownOnFolddingLine = -1;
	[NonSerialized]
	public static System.DateTime lastDoubleClickTime;

	[NonSerialized]
	public SyntaxToken mouseHoverToken;
	[NonSerialized]
	private Rect mouseHoverTokenRect;
	[NonSerialized]
	public System.DateTime mouseHoverTime;
	//[NonSerialized]
	public FGTooltip tokenTooltip;
	
	//[NonSerialized]
	public FGTooltip argumentsHint;
	
	[NonSerialized]
	FGTextBuffer.CaretPos nextCaretPos = new FGTextBuffer.CaretPos();
	[NonSerialized]
	FGTextBuffer.CaretPos clickedPos = new FGTextBuffer.CaretPos();
	[NonSerialized]
	FGTextBuffer.CaretPos mouseOverPos = new FGTextBuffer.CaretPos();
	
	private void ProcessEditorMouse(float margin, Event current)
	{
		if (current.type == EventType.MouseDrag || current.type == EventType.MouseDown)
			pingTimer = 0f;
		
		if (!CanEdit())
			return;

		if (current.pointerType != UnityEngine.PointerType.Mouse && current.type == EventType.MouseDrag)
		{
			current.type = EventType.ScrollWheel;
			current.delta = -current.delta / 3f;
			return;
		}
		
		if (GUIUtility.hotControl != 0 && GUIUtility.hotControl != codeViewControlID && DragAndDrop.GetGenericData("ScriptInspector.Text") == null)
			return;

		clickedPos.line = -1;
		mouseOverPos.line = -1;
		//nextCaretPos.line = -1;
		if (current.type == EventType.MouseDown)
		{
			nextF12GoesBack = false;

			if (current.button == 0)
			{
				mouseIsDown = true;
				
				// if (enableCodeFolding)
				//{
				//	// Check code folding buttons
				//	if (current.mousePosition.x >= marginLeft - charSize.x - 6f &&
				//		current.mousePosition.x < marginLeft - charSize.x - 6f + charSize.x)
				//	{
				//		var lineIndex = GetLineAt(current.mousePosition.y);
				//		if (lineIndex >= 0 && lineIndex < textBuffer.lines.Count)
				//		{
				//			if (current.mousePosition.y - GetLineOffset(lineIndex) < lineHeight)
				//			{
				//				var region = textBuffer.formattedLines[lineIndex].regionTree;
				//				if (region != null && region.line != null)
				//				{
				//					if (region.kind == FGTextBuffer.RegionTree.Kind.Region ||
				//						region.kind == FGTextBuffer.RegionTree.Kind.InactiveRegion)
				//					{
				//						var regionStartsAt = region.line.index;
				//						if (regionStartsAt == lineIndex)
				//						{
				//							mouseDownOnFolddingLine = lineIndex;
				//						}
				//					}
				//				}
				//			}
				//		}
				//	}
				//}
			}
		}
		if (!mouseIsDown && current.button == 0 && current.rawType != EventType.MouseUp && current.type != EventType.MouseMove)
			return;
		if (current.rawType == EventType.MouseUp && current.button == 0)
		{
			mouseIsDown = false;
			codeViewDragging = false;
			lineSelectMode = false;
		}

		EventType eventType = current.type;
		EventModifiers modifiers = current.modifiers & ~(EventModifiers.CapsLock | EventModifiers.Numeric | EventModifiers.FunctionKey);
		bool isDrag = (eventType == EventType.MouseDrag || eventType == EventType.DragPerform || eventType == EventType.DragUpdated) && current.button == 0;

		int nextCaretColumn = caretPosition.virtualColumn;
		int nextCharacterIndex = caretPosition.characterIndex;
		int nextCaretLine = caretPosition.line;
		
		nextCaretPos.line = caretPosition.line;
		nextCaretPos.characterIndex = caretPosition.characterIndex;
		nextCaretPos.column = caretPosition.column;
		nextCaretPos.virtualColumn = caretPosition.virtualColumn;

		var currentMousePosition = current.mousePosition;
		float x = currentMousePosition.x;
		float y = currentMousePosition.y;
		if (isDrag)
		{
			x = Mathf.Clamp(x, codeViewRect.x, codeViewRect.xMax);
			y = Mathf.Clamp(y, codeViewRect.y, codeViewRect.yMax);
		}
		x -= margin;
		if (x >= -4f && x < 0f)
		{
			x = 0f;
			currentMousePosition.x = margin;
		}

		int softRow;
		int clickedLine = GetEditorLineAt(y, out softRow);
		
		var clickedColumn = Mathf.RoundToInt(x / charSize.x);
		var mouseOverColumn = (int) (x / charSize.x);

		var textPosition = EditorToBufferPosition(clickedLine, ref clickedColumn, softRow);
		var mouseOverPosition = EditorToBufferPosition(clickedLine, ref mouseOverColumn, softRow);

		clickedLine = textPosition.line;

		int clickedCharIndex = textPosition.index;
		int mouseOverCharIndex = mouseOverPosition.index;

		clickedPos = new FGTextBuffer.CaretPos
		{
			characterIndex = clickedCharIndex,
			column = clickedColumn,
			line = clickedLine,
			virtualColumn = clickedColumn
		};
		//clickedCharIndex = clickedPos.characterIndex;
		if (mouseOverPosition == textPosition)
		{
			mouseOverPos = clickedPos;
			mouseOverCharIndex = clickedCharIndex;
		}
		else
		{
			mouseOverPos = new FGTextBuffer.CaretPos
			{
				characterIndex = mouseOverCharIndex,
				column = mouseOverColumn,
				line = mouseOverPosition.line,
				virtualColumn = mouseOverColumn
			};
			//mouseOverPos = ViewToBufferPosition(clickedLine, row, mouseOverColumn);
		}
		//clickedColumn = clickedPos.column;
		//if (mouseIsDown)
		//	Debug.Log(clickedPos);
		//ㅎ호ㅗ횰퓨ㅜㅎ퓨

		if (textBuffer.isCsFile
			&& current.type == EventType.MouseMove
			&& modifiers == 0
			&& fullCodeViewRect.Contains(currentMousePosition)
			&& clickedLine >= 0
			&& clickedLine < textBuffer.lines.Count
			&& mouseOverCharIndex < textBuffer.lines[clickedLine].Length
			&& FGTextBufferManager.isApplicationActive)
		{
			var hoverToken = GetTokenAtPosition(clickedLine, mouseOverCharIndex + 1);
			if (hoverToken != mouseHoverToken)
			{
				mouseHoverTime = default(System.DateTime);
				mouseHoverToken = null;
				if (tokenTooltip != null)
					tokenTooltip.Hide();

				if (hoverToken != null && hoverToken.tokenKind == SyntaxToken.Kind.Collapsed)
				{
					Debug.Log("Collapsed");
				}
				else if (hoverToken != null && hoverToken.parent != null &&
					(hoverToken.style == FGTextEditor.StyleID.ReferenceType
					|| hoverToken.parent.syntaxError != null
					|| hoverToken.parent.semanticError != null
					|| hoverToken.tokenKind >= SyntaxToken.Kind.BuiltInLiteral
						&& (hoverToken.tokenKind != SyntaxToken.Kind.Keyword
					|| hoverToken.text == "this" || hoverToken.text == "base" || hoverToken.text == "new"
							|| textBuffer.Parser.IsBuiltInType(hoverToken.text)
							|| textBuffer.Parser.IsBuiltInLiteral(hoverToken.text))
						&& hoverToken.tokenKind != SyntaxToken.Kind.Punctuator))
				{
					mouseHoverTokenRect = GetTokenRect(hoverToken);
					Vector2 mouseScreenPoint = GUIUtility.GUIToScreenPoint(currentMousePosition);
					if (mouseHoverTokenRect.Contains(mouseScreenPoint))
					{
						mouseHoverToken = hoverToken;
						mouseHoverTime = hoverToken != null ? frameTime : default(System.DateTime);
					}
				}
			}
		}
		else
		{
			mouseHoverTime = default(System.DateTime);
			mouseHoverToken = null;
		}

		if (isDrag && (eventType == EventType.DragPerform || eventType == EventType.DragUpdated))
		{
			int mousePosToCaretPos = clickedPos.CompareTo(caretPosition);
			int mousePosToSelStartPos = selectionStartPosition != null ? clickedPos.CompareTo(selectionStartPosition) : mousePosToCaretPos;
			bool mouseOnSelection = !((mousePosToCaretPos < 0 && mousePosToSelStartPos < 0) || (mousePosToCaretPos > 0 && mousePosToSelStartPos > 0));
			if (EditorGUI.actionKey && (mousePosToCaretPos == 0 || mousePosToSelStartPos == 0))
				mouseOnSelection = false;

			DragAndDrop.visualMode = mouseOnSelection ? DragAndDropVisualMode.Rejected : EditorGUI.actionKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;

			if (eventType == EventType.DragPerform)
			{
				object data = DragAndDrop.GetGenericData("ScriptInspector.Text");
				if (!string.IsNullOrEmpty(data as string) && TryEdit())
				{
					textBuffer.BeginEdit("Drag selection");

					if (!EditorGUI.actionKey)
					{
						FGTextBuffer.CaretPos temp = textBuffer.DeleteText(selectionStartPosition, caretPosition);
						textBuffer.UpdateHighlighting(temp.line, temp.line);

						if (mouseDropPosition > caretPosition)
						{
							int linesDeleted = Math.Abs(caretPosition.line - selectionStartPosition.line);
							if (linesDeleted == 0)
							{
								if (caretPosition.line == mouseDropPosition.line)
									mouseDropPosition.characterIndex -= Math.Abs(caretPosition.characterIndex - selectionStartPosition.characterIndex);
							}
							else
							{
								if (Math.Max(caretPosition.line, selectionStartPosition.line) == mouseDropPosition.line)
								{
									mouseDropPosition.line = temp.line;
									mouseDropPosition.characterIndex -= (caretPosition > selectionStartPosition ?
										caretPosition.characterIndex : selectionStartPosition.characterIndex) - temp.characterIndex;
								}
								else
								{
									mouseDropPosition.line -= linesDeleted;
								}
							}
							mouseDropPosition.column = mouseDropPosition.virtualColumn = CharIndexToColumn(mouseDropPosition.characterIndex, mouseDropPosition.line);
						}
					}

					caretPosition = textBuffer.InsertText(mouseDropPosition, data as string);
					if (wordWrapping)
						caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);
					textBuffer.UpdateHighlighting(mouseDropPosition.line, caretPosition.line);
					selectionStartPosition = mouseDropPosition.Clone();

					textBuffer.EndEdit();
				}
				
				DragAndDrop.AcceptDrag();
				DragAndDrop.SetGenericData("ScriptInspector.Text", null);

				GUIUtility.hotControl = 0;
				lineSelectMode = false;
				codeViewDragging = false;
				mouseDownOnSelection = false;
				autoScrollLeft = false;
				autoScrollRight = false;
				autoScrollUp = false;
				autoScrollDown = false;
				current.Use();
				return;
			}

			if (mouseOnSelection)
			{
				clickedPos.line = caretPosition.line;
				clickedPos.characterIndex = caretPosition.characterIndex;
				clickedPos.column = caretPosition.column;
				clickedPos.virtualColumn = caretPosition.virtualColumn;
			}

			if (clickedPos != mouseDropPosition)
			{
				mouseDropPosition.line = clickedPos.line;
				mouseDropPosition.characterIndex = clickedPos.characterIndex;
				mouseDropPosition.column = clickedPos.column;
				mouseDropPosition.virtualColumn = clickedPos.virtualColumn;
				caretMoveTime = frameTime;
				lastCaretMoveWasSearch = false;
				//scrollToCaret = true;
				needsRepaint = true;
			}

			//GUIUtility.hotControl = codeViewControlID;
			focusCodeView = true;
			current.Use();
			return;
		}

		if (!codeViewDragging && currentMousePosition.y >= 0 &&
			eventType == EventType.MouseDown &&
		(isDrag && currentMousePosition.x >= codeViewRect.x || eventType == EventType.MouseDown && currentMousePosition.x >= 0))
		{
			if (hasSelection)
			{
				int clickedPosToCaretPos = mouseOverPos.CompareTo(caretPosition);
				int clickedPosToSelStartPos = mouseOverPos.CompareTo(selectionStartPosition);
				mouseDownOnSelection = !((clickedPosToCaretPos < 0 && clickedPosToSelStartPos < 0) || (clickedPosToCaretPos >= 0 && clickedPosToSelStartPos >= 0));
			}
			else
			{
				mouseDownOnSelection = false;
			}
		}

		if (isDrag && current.button == 0)
		{
			if (!codeViewDragging)
			{
//				if (!codeViewRect.Contains(currentMousePosition))
//					return;

				lastAutoScrollTime = frameTime;

				if (mouseDownOnSelection && !current.shift)
				{
					DragAndDrop.PrepareStartDrag();
					DragAndDrop.objectReferences = new UnityEngine.Object[] { textBuffer };
					DragAndDrop.StartDrag("Dragging selected text");
					DragAndDrop.SetGenericData("ScriptInspector.Text", textBuffer.GetTextRange(selectionStartPosition, caretPosition));

					GUIUtility.hotControl = 0;
					current.Use();
					codeViewDragging = true;
					mouseDropPosition = caretPosition.Clone();
					return;
				}
				else
				{
					mouseDownOnSelection = false;
				}
			}
			codeViewDragging = true;
			//needsRepaint = true;
		}
		else if (current.button == 1)
		{
			if (eventType == EventType.MouseDown && currentMousePosition.x >= 0 && currentMousePosition.y >= 0)
			{
				current.Use();
				if (!mouseDownOnSelection)
				{
					nextCharacterIndex = clickedPos.characterIndex;
					nextCaretColumn = clickedColumn;
					nextCaretLine = clickedLine;
					nextCaretPos.line = clickedPos.line;
					nextCaretPos.characterIndex = clickedPos.characterIndex;
					nextCaretPos.column = clickedPos.column;
					nextCaretPos.virtualColumn = clickedPos.virtualColumn;
					scrollToCaret = true;
				}
				else
				{
					return;
				}
			}
			else
			{
				return;
			}
		}

		if (isDrag || eventType == EventType.MouseDown)
		{
			GUIUtility.hotControl = codeViewControlID;
			focusCodeView = true;
			if (currentMousePosition.x >= 0 && currentMousePosition.y >= 0 || isDrag)
			{
				if (!isDrag && current.button == 0)
				{
					if (currentMousePosition.x < margin + scrollPosition.x)
					{
						lineSelectMode = true;
						wordSelectMode = false;
						anchorWordStart = anchorWordEnd = null;
					}
					else if (SISettings.tripleclickSelectsFullLine)
					{
						if (frameTime > lastDoubleClickTime &&
							((float) (frameTime - lastDoubleClickTime).TotalSeconds) <= 0.5f)
						{
							lineSelectMode = true;
							wordSelectMode = false;
							anchorWordStart = anchorWordEnd = null;
							current.clickCount = 3;
							mouseDownOnSelection = false;
						}
					}
				}

				scrollToCaret = !isDrag && !mouseDownOnSelection;

				nextCharacterIndex = clickedCharIndex;
				nextCaretColumn = clickedColumn;
				nextCaretLine = clickedLine;
				nextCaretPos.line = clickedPos.line;
				nextCaretPos.characterIndex = clickedPos.characterIndex;
				nextCaretPos.column = clickedPos.column;
				nextCaretPos.virtualColumn = clickedPos.virtualColumn;
					
				if (current.button == 0)
				{
					if (current.clickCount == 1 && mouseDownOnSelection)
					{
						needsRepaint = true;
						return;
					}

					if (!lineSelectMode && (current.clickCount == 2 || EditorGUI.actionKey && eventType == EventType.MouseDown || wordSelectMode))
					{
						if (current.clickCount == 2)
							lastDoubleClickTime = frameTime;
						
						int wordStart;
						int wordEnd;
						if (textBuffer.GetWordExtents(nextCharacterIndex, clickedLine, out wordStart, out wordEnd))
						{
							// select word
							if (anchorWordStart != null && ((modifiers & EventModifiers.Shift) != 0 || wordSelectMode))
							{
								var forward = clickedLine > anchorWordStart.line ||
									clickedLine == anchorWordStart.line && wordStart >= anchorWordStart.characterIndex;
								if (forward)
								{
									selectionStartPosition = anchorWordStart.Clone();
									nextCharacterIndex = wordEnd;
									nextCaretColumn = CharIndexToColumn(wordEnd, clickedLine);
									nextCaretPos = clickedPos.Clone();
									nextCaretPos.column = nextCaretColumn;
									nextCaretPos.characterIndex = nextCharacterIndex;
								}
								else
								{
									selectionStartPosition = anchorWordEnd.Clone();
									nextCharacterIndex = wordStart;
									nextCaretColumn = CharIndexToColumn(wordStart, clickedLine);
									nextCaretPos = clickedPos.Clone();
									nextCaretPos.column = nextCaretColumn;
									nextCaretPos.characterIndex = nextCharacterIndex;
								}
							}
							else
							{
								if (selectionStartPosition != null)
									caretPosition = selectionStartPosition;
								selectionStartPosition = null;
								
								if ((modifiers & EventModifiers.Shift) == 0)
								{
									caretPosition.line = clickedLine;
									caretPosition.characterIndex = wordStart;
									caretPosition.virtualColumn = caretPosition.column = CharIndexToColumn(wordStart, clickedLine);
									nextCharacterIndex = wordEnd;
									nextCaretColumn = CharIndexToColumn(wordEnd, clickedLine);
									nextCaretPos = clickedPos.Clone();
									nextCaretPos.column = nextCaretColumn;
									nextCaretPos.characterIndex = nextCharacterIndex;
									
									anchorWordStart = caretPosition.Clone();
									anchorWordEnd = nextCaretPos.Clone();
									anchorWordEnd.virtualColumn = anchorWordEnd.column;
								}
								else
								{
									bool forward = clickedLine > caretPosition.line ||
										clickedLine == caretPosition.line && clickedCharIndex > caretPosition.characterIndex;
									
									int fromWordStart;
									int fromWordEnd;
									if (textBuffer.GetWordExtents(caretPosition.characterIndex, caretPosition.line, out fromWordStart, out fromWordEnd))
									{
										caretPosition.characterIndex = forward ? fromWordStart : fromWordEnd;
										caretPosition.virtualColumn = caretPosition.column = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);

										anchorWordStart = caretPosition.Clone();
										anchorWordStart.characterIndex = fromWordStart;
										anchorWordStart.virtualColumn = anchorWordStart.column = CharIndexToColumn(fromWordStart, anchorWordStart.line);
										anchorWordEnd = caretPosition.Clone();
										anchorWordEnd.characterIndex = fromWordEnd;
										anchorWordEnd.virtualColumn = anchorWordEnd.column = CharIndexToColumn(fromWordEnd, anchorWordEnd.line);
									}
									else
									{
										anchorWordStart = caretPosition.Clone();
										anchorWordEnd = caretPosition.Clone();
										//Debug.Log(anchorWordEnd.characterIndex >= anchorWordStart.characterIndex ? "OK" : "WRONG");
									}
									
									nextCharacterIndex = forward ? wordEnd : wordStart;
									nextCaretColumn = CharIndexToColumn(nextCharacterIndex, clickedLine);
									nextCaretPos = clickedPos.Clone();
									nextCaretPos.column = nextCaretColumn;
									nextCaretPos.characterIndex = nextCharacterIndex;
								}
							}
							modifiers |= EventModifiers.Shift;
							
							lineSelectMode = false;
							wordSelectMode = true;
						}
					}
					else
					{
						anchorWordStart = null;
						anchorWordEnd = null;
					}
				}
			}
			current.Use();
		}

		int lineSelectOffset = 0;
		if (lineSelectMode && selectionStartPosition != null && selectionStartPosition < caretPosition)
			lineSelectOffset = -1;

		if (current.rawType == EventType.MouseUp && current.button == 0 && GUIUtility.hotControl != 0)
		{
			// if (enableCodeFolding)
			//{
			//	// Code folding buttons
			//	if (mouseDownOnFolddingLine >= 0)
			//	{
			//		if (currentMousePosition.x >= marginLeft - charSize.x - 6f &&
			//			currentMousePosition.x < marginLeft - charSize.x - 6f + charSize.x)
			//		{
			//			var lineIndex = GetLineAt(currentMousePosition.y);
			//			if (lineIndex == mouseDownOnFolddingLine)
			//			{
			//				mouseDownOnFolddingLine = -1;
							
			//				if (currentMousePosition.y - GetLineOffset(lineIndex) < lineHeight)
			//				{
			//					var region = textBuffer.formattedLines[lineIndex].regionTree;
			//					if (region != null && region.line != null)
			//					{
			//						if (region.kind == FGTextBuffer.RegionTree.Kind.Region ||
			//							region.kind == FGTextBuffer.RegionTree.Kind.InactiveRegion)
			//						{
			//							var regionStartsAt = region.line.index;
			//							if (regionStartsAt == lineIndex)
			//							{
			//								ToggleFolding(lineIndex);
			//								Event.current.Use();
			//								return;
			//							}
			//						}
			//					}
			//				}
			//			}
			//		}
			//	}
			//}
			
			if (mouseDownOnSelection)
			{
				if (!codeViewDragging)
				{
					nextCharacterIndex = clickedCharIndex;
					nextCaretColumn = clickedColumn;
					nextCaretLine = clickedLine;
					nextCaretPos = clickedPos.Clone();
					--caretPosition.virtualColumn;
				}
			}

			GUIUtility.hotControl = 0;
			//autoScrolling = Vector2.zero;
			lineSelectMode = false;
			wordSelectMode = false;
			codeViewDragging = false;
			mouseDownOnSelection = false;
			autoScrollLeft = false;
			autoScrollRight = false;
			autoScrollUp = false;
			autoScrollDown = false;
			current.Use();

			needsRepaint = true;
			lineSelectOffset = 0;
		}

		//if (nextCaretColumn != caretPosition.virtualColumn ||
		//    nextCaretLine != (caretPosition.line + lineSelectOffset) ||
		//    eventType == EventType.mouseDown && current.button == 0)
		if (!nextCaretPos.IsSameAs(caretPosition) ||
			nextCaretPos.line != (caretPosition.line + lineSelectOffset) ||
			eventType == EventType.MouseDown && current.button == 0)
		{
			caretMoveTime = frameTime;
			lastCaretMoveWasSearch = false;

			//if (nextCaretLine < 0)
			//    nextCaretLine = 0;
			if (nextCaretPos.line < 0)
				nextCaretPos.Set(0, 0, 0, 0);
			//if (nextCaretLine >= textBuffer.numParsedLines)
			//    nextCaretLine = textBuffer.numParsedLines - 1;
			if (nextCaretPos.line >= textBuffer.numParsedLines)
				nextCaretPos.Set(textBuffer.numParsedLines - 1, 0, 0, 0);
			nextCaretLine = nextCaretPos.line;

			if (selectionStartPosition == null && (isDrag || (modifiers & EventModifiers.Shift) != 0))
				selectionStartPosition = caretPosition.Clone();

			if (lineSelectMode)
			{
				if (selectionStartPosition == null || !isDrag && (modifiers & EventModifiers.Shift) == 0)
					selectionStartPosition = new FGTextBuffer.CaretPos { column = 0, virtualColumn = 0, characterIndex = 0, line = nextCaretLine };

				nextCharacterIndex = 0;
				nextCaretColumn = 0;
				if (nextCaretLine >= selectionStartPosition.line)
				{
					++nextCaretLine;
					nextCaretPos.Set(nextCaretPos.line + 1, 0, 0, 0);
					if (nextCaretLine >= textBuffer.numParsedLines)
					{
						nextCaretLine = textBuffer.numParsedLines - 1;
						nextCharacterIndex = textBuffer.lines[nextCaretLine].Length;
						nextCaretColumn = textBuffer.CharIndexToColumn(nextCharacterIndex, nextCaretLine);
						nextCaretPos.Set(textBuffer.numParsedLines - 1, textBuffer.lines[nextCaretLine].Length, nextCaretColumn);
					}
					selectionStartPosition = new FGTextBuffer.CaretPos { characterIndex = 0, column = 0, virtualColumn = 0, line = selectionStartPosition.line };
				}
				else
				{
					int charIndex = textBuffer.lines[selectionStartPosition.line].Length;
					int column = textBuffer.CharIndexToColumn(charIndex, selectionStartPosition.line);
					nextCaretPos.Set(nextCaretPos.line, 0, 0, 0);
					selectionStartPosition = new FGTextBuffer.CaretPos { characterIndex = charIndex, column = column, virtualColumn = column, line = selectionStartPosition.line };
				}
			}

			if (!mouseDownOnSelection)
			{
				caretPosition.line = nextCaretPos.line;
				caretPosition.characterIndex = nextCaretPos.characterIndex;
				caretPosition.column = nextCaretPos.column;
				caretPosition.virtualColumn = nextCaretPos.virtualColumn;
				//caretPosition.line = nextCaretLine;
				if (nextCaretLine >= 0)
				{
					//caretPosition.characterIndex = nextCharacterIndex;
					//caretPosition.column = caretPosition.virtualColumn = Math.Min(nextCaretColumn, CharIndexToColumn(textBuffer.lines[nextCaretLine].Length, nextCaretLine));
					//if (wordWrapping)
					//{
					//	List<int> softLineBreaks = GetSoftLineBreaks(caretPosition.line);
					//	int row = FindFirstIndexGreaterThanOrEqualTo<int>(softLineBreaks, caretPosition.column);
					//	if (row < softLineBreaks.Count && caretPosition.column == softLineBreaks[row] && clickedPos.virtualColumn == 0)
					//		++row;
					//	caretPosition.virtualColumn -= row > 0 ? softLineBreaks[row - 1] : 0;
					//}
					caretPosition.virtualColumn = caretPosition.column;
				}
			}

			if (!isDrag && !lineSelectMode && (modifiers & EventModifiers.Shift) == 0)
				selectionStartPosition = null;

			if (!codeViewDragging)
			{
				AddRecentLocation(11, true);
				scrollToCaret = true;
			}
			//Repaint();
			needsRepaint = true;
		}
	}

	private bool ProcessCodeViewCommands()
	{
		if (Event.current.type == EventType.ValidateCommand)
		{
			if (Event.current.commandName == "SelectAll")
			{
				Event.current.Use();
				return true;
			}
			else if (Event.current.commandName == "Copy" || Event.current.commandName == "Cut")
			{
				if (selectionStartPosition != null || SISettings.copyCutFullLine)
				{
					Event.current.Use();
					return true;
				}
			}
			else if (Event.current.commandName == "Paste")
			{
				if (!string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer))
				{
					Event.current.Use();
					return true;
				}
			}
			else if (Event.current.commandName == "Delete")
			{
				Event.current.Use();
				return true;
			}
			else if (Event.current.commandName == "Duplicate")
			{
				Event.current.Use();
				return true;
			}
			else if (Event.current.commandName == "OpenAtCursor")
			{
				Event.current.Use();
				return true;
			}
			else if (Event.current.commandName == "ScriptInspector.Autocomplete")
			{
				Event.current.Use();
				return true;
			}
		}
		else if (!CanEdit())
		{
			return false;
		}
		else if (Event.current.type == EventType.ExecuteCommand)
		{
			pingTimer = 0f;
			
			if (Event.current.commandName == "SelectAll")
			{
				selectionStartPosition = new FGTextBuffer.CaretPos { column = 0, virtualColumn = 0, characterIndex = 0, line = 0 };

				caretPosition.line = textBuffer.numParsedLines - 1;
				caretPosition.characterIndex = textBuffer.numParsedLines > 0 ? textBuffer.lines[textBuffer.numParsedLines - 1].Length : 0;
				caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, textBuffer.numParsedLines - 1);

				Event.current.Use();
				caretMoveTime = frameTime;
				lastCaretMoveWasSearch = false;
				scrollToCaret = true;
				Repaint();
				return true;
			}
			else if (Event.current.commandName == "Paste")
			{
				if (!string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer) && TryEdit())
				{
					var pasteFullLine = FGTextBufferManager.Instance().fullLineCopied == EditorGUIUtility.systemCopyBuffer;
					if (selectionStartPosition != null)
					{
						caretPosition = textBuffer.DeleteText(selectionStartPosition, caretPosition);
						selectionStartPosition = null;
						pasteFullLine = false;
					}
					
					string copyBuffer = EditorGUIUtility.systemCopyBuffer;
					copyBuffer = copyBuffer.Replace("\r\n", "\n");
					copyBuffer = copyBuffer.Replace('\r', '\n');

					textBuffer.BeginEdit("Paste");
					int insertedAtLine = caretPosition.line;
					bool emptyLine = textBuffer.FirstNonWhitespace(insertedAtLine) == textBuffer.lines[insertedAtLine].Length;
					if (pasteFullLine)
					{
						textBuffer.InsertText(new FGTextBuffer.CaretPos { line = insertedAtLine }, copyBuffer);
						++caretPosition.line;
					}
					else
					{
						caretPosition = textBuffer.InsertText(caretPosition, copyBuffer);
						if (wordWrapping)
							caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);
					}
					textBuffer.UpdateHighlighting(insertedAtLine, caretPosition.line);
					if (insertedAtLine < caretPosition.line || emptyLine)
						ReindentLines(insertedAtLine, caretPosition.line);
					textBuffer.EndEdit();
					
					AddRecentLocation(0, true);
					tryArgumentsHint = true;
					
					Event.current.Use();
					caretMoveTime = frameTime;
					lastCaretMoveWasSearch = false;
					scrollToCaret = true;
					Repaint();
					return true;
				}
			}
			else if (Event.current.commandName == "Copy" || Event.current.commandName == "Cut" && TryEdit())
			{
				if (selectionStartPosition != null)
				{
					FGTextBufferManager.Instance().fullLineCopied = null;
					EditorGUIUtility.systemCopyBuffer = textBuffer.GetTextRange(caretPosition, selectionStartPosition);

					if (Event.current.commandName == "Cut")
					{
						textBuffer.BeginEdit("Cut Selection");
						caretPosition = textBuffer.DeleteText(selectionStartPosition, caretPosition);
						if (wordWrapping)
							caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);
						textBuffer.UpdateHighlighting(caretPosition.line, caretPosition.line);
						textBuffer.EndEdit();
						selectionStartPosition = null;
						
						AddRecentLocation(0, true);
						tryArgumentsHint = true;
						
						caretMoveTime = frameTime;
						lastCaretMoveWasSearch = false;
						scrollToCaret = true;
						Repaint();
					}
				}
				else if (SISettings.copyCutFullLine)
				{
					var fullLineToCopy = textBuffer.lines[caretPosition.line];
					if (fullLineToCopy != "")
					{
						FGTextBufferManager.Instance().fullLineCopied = fullLineToCopy + "\n";
						EditorGUIUtility.systemCopyBuffer = FGTextBufferManager.Instance().fullLineCopied;
						
						if (Event.current.commandName == "Cut")
						{
							textBuffer.BeginEdit("Cut Line");
							var cutTo = new FGTextBuffer.CaretPos { line = caretPosition.line + 1 };
							if (textBuffer.lines.Count == cutTo.line)
							{
								cutTo.characterIndex = textBuffer.lines[textBuffer.lines.Count - 1].Length;
								--cutTo.line;
								cutTo.column = cutTo.virtualColumn = CharIndexToColumn(cutTo.characterIndex, cutTo.line);
							}
							caretPosition = textBuffer.DeleteText(new FGTextBuffer.CaretPos{ line = caretPosition.line }, cutTo);
							if (wordWrapping)
								caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);
							textBuffer.UpdateHighlighting(caretPosition.line, caretPosition.line);
							textBuffer.EndEdit();
							
							AddRecentLocation(0, true);
							
							caretMoveTime = frameTime;
							lastCaretMoveWasSearch = false;
							scrollToCaret = true;
							Repaint();
						}
					}
				}
				
				Event.current.Use();
				return true;
			}
			else if (Event.current.commandName == "Delete" && TryEdit())
			{
				if (Application.platform == RuntimePlatform.OSXEditor ||
					selectionStartPosition == null && !SISettings.copyCutFullLine)
				{
					CommandDeleteLine();
				}
				else if (selectionStartPosition == null && textBuffer.lines[caretPosition.line] == "")
				{
					CommandDeleteLine();
				}
				else
				{
					Event.current.commandName = "Cut";
					return ProcessCodeViewCommands();
				}

				Event.current.Use();
				return true;
			}
			else if (Event.current.commandName == "Duplicate" && TryEdit())
			{
				CommandDuplicateLinesDown();
				
				tryArgumentsHint = true;
				Event.current.Use();
				return true;
			}
			else if (Event.current.commandName == "OpenAtCursor")
			{
				Event.current.Use();
				OpenAtCursor();
				return true;
			}
			else if (Event.current.commandName == "ScriptInspector.Autocomplete" && TryEdit())
			{
				Event.current.Use();
				tryAutocomplete = true;
				tryArgumentsHint = true;
				Repaint();
				return true;
			}
		}
		return false;
	}
	
	private void CommandFindAllReferences()
	{
		if (!textBuffer.isCsFile)
			return;
		
		int lineIndex, tokenIndex;
		bool atTokenEnd;
		var token = textBuffer.GetTokenAt(caretPosition, out lineIndex, out tokenIndex, out atTokenEnd);
		if (token == null)
			return;
		
		if (atTokenEnd &&
			token.tokenKind != SyntaxToken.Kind.Identifier &&
			token.tokenKind != SyntaxToken.Kind.ContextualKeyword &&
			token.tokenKind != SyntaxToken.Kind.Keyword)
		{
			var tokens = textBuffer.formattedLines[lineIndex].tokens;
			if (tokenIndex < tokens.Count - 1)
				token = tokens[tokenIndex + 1];
		}
		
		if (token.parent == null)
			return;
		
		if (token.tokenKind != SyntaxToken.Kind.Identifier &&
			token.tokenKind != SyntaxToken.Kind.ContextualKeyword &&
			token.tokenKind != SyntaxToken.Kind.Keyword
			||
			string.IsNullOrEmpty(token.text))
		{
			return;
		}
		
		var symbol = token.parent.resolvedSymbol;
		if (symbol == null || symbol.kind == SymbolKind.Error || !symbol.IsValid())
			return;
		
		FGFindInFiles.FindAllReferences(symbol, targetPath);
	}
	
	private void CommandDeleteLine()
	{
		Event simKey = new Event();
		simKey.type = EventType.KeyDown;
		simKey.keyCode = KeyCode.Delete;
		simKey.modifiers = EventModifiers.Shift;
		ProcessEditorKeyboard(simKey, true);
	}
	
	private void CommandDuplicateLinesDown()
	{
		var numLines = selectionStartPosition == null ? 1 : Mathf.Abs(caretPosition.line - selectionStartPosition.line) + 1;
		if (selectionStartPosition != null)
		{
			if (selectionStartPosition < caretPosition)
			{
				if (caretPosition.characterIndex == 0)
					--numLines;
			}
			else if (selectionStartPosition.characterIndex == 0)
			{
				--numLines;
			}
		}
		
		var insertAt = selectionStartPosition != null ?
		(caretPosition < selectionStartPosition ? caretPosition.Clone() : selectionStartPosition.Clone()) :
			caretPosition.Clone();
		insertAt.column = 0;
		insertAt.characterIndex = 0;
		
		var insertText = new StringBuilder();
		for (int i = 0; i < numLines; ++i)
			insertText.Append(textBuffer.lines[i + insertAt.line]).Append('\n');
		
		textBuffer.InsertText(insertAt, insertText.ToString());
		if (selectionStartPosition != null)
			selectionStartPosition.line += numLines;
		caretPosition.line += numLines;
		
		textBuffer.UpdateHighlighting(insertAt.line, insertAt.line + numLines - 1);
	}
	
	private void OpenAtCursor()
	{
		if (IsModified)
		{
			switch (EditorUtility.DisplayDialogComplex(
				"Script Inspector",
				AssetDatabase.GUIDToAssetPath(textBuffer.guid)
					+ "\n\nThis asset has been modified inside the Script Inspector.\nDo you want to save the changes before opening it in the external IDE?",
				"Save",
				"Cancel",
				"Open Anyway"))
			{
			case 0:
				SaveBuffer();
				break;
				
			case 1:
				return;
				
			case 2:
				break;
			}
		}
		FGCodeWindow.openInExternalIDE = true;
		AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(textBuffer.guid), typeof(UnityEngine.Object)), caretPosition.line + 1);
	}

	private void UseSelectionForSearch()
	{
		var text = GetSearchTextFromSelection();
		if (text != "")
		{
			searchString = text;
			SetSearchText(text);
		}
	}
	
	public string GetSearchTextFromSelection()
	{
		var text = "";
		if (selectionStartPosition != null && selectionStartPosition.line == caretPosition.line)
		{
			if (caretPosition > selectionStartPosition)
				text = textBuffer.lines[caretPosition.line].Substring(
					selectionStartPosition.characterIndex, caretPosition.characterIndex - selectionStartPosition.characterIndex);
			else
				text = textBuffer.lines[caretPosition.line].Substring(
					caretPosition.characterIndex, selectionStartPosition.characterIndex - caretPosition.characterIndex);
			return text;
		}

		int wordStart;
		int wordEnd;
		if (!textBuffer.GetWordExtents(caretPosition.characterIndex, caretPosition.line, out wordStart, out wordEnd))
			return "";
		
		var line = textBuffer.lines[caretPosition.line];
		text = line.Substring(wordStart, wordEnd - wordStart);
		if (text.Trim() == "")
			return "";
		
		if (wordStart > 0 && caretPosition.characterIndex == wordStart &&
			!(line[wordStart] == '_' || char.IsLetterOrDigit(line, wordStart)) &&
			(line[wordStart] == '_' || char.IsLetterOrDigit(line, wordStart - 1)))
		{
			// Taking the previous word
			if (textBuffer.GetWordExtents(caretPosition.characterIndex - 1, caretPosition.line, out wordStart, out wordEnd))
				return line.Substring(wordStart, wordEnd - wordStart);
		}
		
		return text;
	}
	
	private bool ExpandSnippet(SnippetCompletion completion)
	{
		if (!textBuffer.isCsFile)
			return false;
		
		int lineIndex, tokenIndex;
		bool atTokenEnd;
		var tokenLeft = textBuffer.GetTokenAt(caretPosition, out lineIndex, out tokenIndex, out atTokenEnd);
		if (tokenLeft != null && (tokenLeft.tokenKind >= SyntaxToken.Kind.Keyword || tokenLeft.tokenKind == SyntaxToken.Kind.Preprocessor || tokenLeft.tokenKind == SyntaxToken.Kind.Comment))
		{
			var span = textBuffer.GetTokenSpan(lineIndex, tokenIndex);
			var text = textBuffer.lines[lineIndex].Substring(span.index, caretPosition.characterIndex - span.index);
			
			string snippet = completion != null ? completion.Expand() : null;
			snippet = snippet ?? CodeSnippets.Get(text, codePathSymbol, null);
			if (snippet != null)
			{
				textBuffer.BeginEdit("Expand Snippet");
				
				var enclosingScopeNode = tokenLeft.parent != null ? CsGrammar.EnclosingScopeNode(tokenLeft.parent.parent) : null;
				var enclosingScope = enclosingScopeNode != null ? enclosingScopeNode.scope : null;
				
				if (enclosingScope == null && textBuffer.Parser != null &&
					textBuffer.Parser.parseTree != null && textBuffer.Parser.parseTree.root != null)
				{
					enclosingScope = textBuffer.Parser.parseTree.root.scope;
				}
				
				var context = codePathSymbol;
				if (context == null)
				{
					var cuScope = enclosingScope as CompilationUnitScope;
					if (cuScope != null)
						context = cuScope.assembly;
				}
				
				var indent = "\n" + textBuffer.lines[lineIndex].Substring(0, textBuffer.FirstNonWhitespace(lineIndex));
				//Debug.Log(lineIndex + "'"+textBuffer.lines[lineIndex]+"'"+indent.Length);
				var lines = snippet.Split('\n');
				var indented = string.Join(indent, lines);
				
				var replaceMacro = caretPosition.Clone();
				replaceMacro.characterIndex = span.index;
				replaceMacro.column -= text.Length;
				replaceMacro.virtualColumn -= text.Length;
				caretPosition = textBuffer.DeleteText(replaceMacro, caretPosition);
				
				var end = indented.IndexOf("$end$");
				if (end < 0)
					end = indented.Length;
				
				FGTextBuffer.CaretPos newSelectionStart = null;
				var start = indented.IndexOf("$start$");
				if (start >= 0 && start < end)
				{
					var part0 = indented.Substring(0, start);
					CodeSnippets.Substitute(ref part0, context, enclosingScope);
					caretPosition = textBuffer.InsertText(caretPosition, part0);
					if (wordWrapping)
						caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);
					
					newSelectionStart = caretPosition.Clone();
					
					start += 7;
				}
				else
				{
					start = 0;
				}
				
				var part = indented.Substring(start, end - start);
				CodeSnippets.Substitute(ref part, context, enclosingScope);
				caretPosition = textBuffer.InsertText(caretPosition, part);
				if (wordWrapping)
					caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);
				
				if (end < indented.Length)
				{
					part = indented.Substring(end + 5);
					CodeSnippets.Substitute(ref part, context, enclosingScope);
					textBuffer.InsertText(caretPosition, part);
				}
				
				if (newSelectionStart != null && newSelectionStart != caretPosition)
					selectionStartPosition = newSelectionStart;
					
				textBuffer.UpdateHighlighting(lineIndex, lineIndex + snippet.Count(x => x == '\n'));
				textBuffer.EndEdit();
				return true;
			}
		}
		
		return false;
	}
	
	[Flags]
	enum FindFoldingSpansOptions
	{
		None = 1<<0,
		OnlyFullyContained = 1<<1,
		OnlyFolded = 1<<2,
		OnlyUnfolded = 1<<3,
	}
	
	private FGTextBuffer.CollapsibleTextSpan FindCollapsibleSpansInSelection(FindFoldingSpansOptions options)
	{
		tempOverlappingSpans.Clear();
		
		TextPosition from, to;
		TextInterval interval;
		if (hasSelection && selectionStartPosition.line != caretPosition.line)
		{
			from = new TextPosition(selectionStartPosition.line, selectionStartPosition.characterIndex);
			to = new TextPosition(caretPosition.line, caretPosition.characterIndex);
			if (from > to)
			{
				var temp = from;
				from = to;
				to = temp;
			}
			interval = new TextInterval{ Start = from, End = to };
		}
		else
		{
			from = new TextPosition(caretPosition.line, 0);
			to = new TextPosition(caretPosition.line + 1, 0);
			interval = new TextInterval{ Start = from, End = to };
		}
		
		textBuffer.collapsibleTextSpansTree.GetIntervalsOverlappingWith(interval, tempOverlappingSpans);
		
		var numSpans = tempOverlappingSpans.Count;
		if (numSpans == 0)
			return null;
		
		if ((options & (FindFoldingSpansOptions.OnlyFolded | FindFoldingSpansOptions.OnlyUnfolded)) != 0)
		{
			var onlyFolded = (options & FindFoldingSpansOptions.OnlyFolded) != 0;
			for (var i = numSpans; i --> 0; )
			{
				var span = tempOverlappingSpans[i];
				if (span.IsCollapsed != onlyFolded)
					tempOverlappingSpans.RemoveAt(i);
			}
			
			numSpans = tempOverlappingSpans.Count;
			if (numSpans == 0)
				return null;
		}
		
		var result = tempOverlappingSpans[numSpans - 1];
		
		if (!hasSelection || selectionStartPosition.line == caretPosition.line)
		{
			tempOverlappingSpans.Clear();
			return result;
		}

		if ((options & FindFoldingSpansOptions.OnlyFullyContained) != 0)
		{
			for (var i = numSpans; i --> 0; )
			{
				var span = tempOverlappingSpans[i].span;
				if (span.Start < from || span.End > to)
					tempOverlappingSpans.RemoveAt(i);
			}
		}
		return result;
	}
	
	private void ToggleFoldingAtCaret()
	{
		if (!enableCodeFolding || textBuffer.collapsibleTextSpansTree == null)
			return;
		
		var lastSpan = FindCollapsibleSpansInSelection(FindFoldingSpansOptions.OnlyFullyContained);
		if (lastSpan == null)
			return;
		
		var numSpans = tempOverlappingSpans.Count;
		if (numSpans == 0)
		{
			ToggleFolding(lastSpan);
			return;
		}
		
		bool unfold = true;
		for (var i = numSpans; i --> 0; )
		{
			var span = tempOverlappingSpans[i];
			if (!span.IsCollapsed)
			{
				unfold = false;
				break;
			}
		}

		for (var i = numSpans; i --> 0; )
		{
			var span = tempOverlappingSpans[i];
			if (span.IsCollapsed == unfold)
				ToggleFolding(span);
		}
	}
	
	private void FoldAtCaret()
	{
		if (!enableCodeFolding || textBuffer.collapsibleTextSpansTree == null)
			return;
		
		var lastSpan = FindCollapsibleSpansInSelection(FindFoldingSpansOptions.OnlyFullyContained | FindFoldingSpansOptions.OnlyUnfolded);
		if (lastSpan == null)
			return;
		
		var numSpans = tempOverlappingSpans.Count;
		if (numSpans == 0)
		{
			if (!lastSpan.IsCollapsed)
				ToggleFolding(lastSpan);
			return;
		}
		
		for (var i = numSpans; i --> 0; )
		{
			var span = tempOverlappingSpans[i];
			if (!span.IsCollapsed)
				ToggleFolding(span);
		}
	}
	
	public void UnfoldAtCaret()
	{
		if (!enableCodeFolding || textBuffer.collapsibleTextSpansTree == null)
			return;
		
		var lastSpan = FindCollapsibleSpansInSelection(FindFoldingSpansOptions.OnlyFullyContained | FindFoldingSpansOptions.OnlyFolded);
		if (lastSpan == null)
			return;
		
		var numSpans = tempOverlappingSpans.Count;
		if (numSpans == 0)
		{
			if (lastSpan.IsCollapsed)
				ToggleFolding(lastSpan);
			return;
		}
		
		for (var i = numSpans; i --> 0; )
		{
			var span = tempOverlappingSpans[i];
			if (span.IsCollapsed)
				ToggleFolding(span);
		}
	}
	
	private void ToggleFoldToDefinitions()
	{
		FoldToDefinitions(false);
	}
	
	private void FoldToDefinitions(bool unfold)
	{
		if (!enableCodeFolding || textBuffer.collapsibleTextSpansTree == null)
			return;
		
		var allSpans = textBuffer.collapsibleTextSpans;
		if (allSpans == null)
			return;
		
		var caretY = GetCaretRect(caretPosition).y - GetLineOffset(scrollPositionLine);
		
		var doneSomething = false;
		
		var nextLine = 0;
		foreach (var span in allSpans)
		{
			if (span.span.Start.line < nextLine)
				continue;
			
			var node = span.owner as ParseTree.Node;
			if (node == null)
				continue;
			
			var parent = node.parent;
			if (parent == null)
				continue;
			
			var rule = node.RuleName;
			var parentRule = parent.RuleName;
			if (parentRule == "methodDeclaration" ||
				parentRule == "constructorDeclaration" ||
				parentRule == "destructorDeclaration" ||
				//parentRule == "enumDeclaration" ||
				parentRule == "eventDeclaration" ||
				rule == "propertyDeclaration" ||
				rule == "indexerDeclaration" ||
				rule == "operatorBody" ||
				rule == "objectOrCollectionInitializer" ||
				rule == "arrayInitializer" ||
				rule == "lambdaExpressionBody")
			{
				if (unfold)
				{
					// Unfold definitions.
					if (span.IsCollapsed)
					{
						ToggleFolding(span);
						doneSomething = true;
						
						// And skip to span end.
						nextLine = span.span.End.line;
					}
				}
				else
				{
					// Fold definitions.
					if (!span.IsCollapsed)
					{
						ToggleFolding(span);
						doneSomething = true;
					}
				}
			}
			else
			{
				// Unfold the rest.
				if (span.IsCollapsed)
				{
					ToggleFolding(span);
					doneSomething = true;
				}
			}

			if (span.IsCollapsed)
				nextLine = span.span.End.line;
		}
		
		if (doneSomething)
		{
			scrollInstantly = true;
			if (caretY >= 0f && caretY < codeViewRect.height)
			{
				scrollPositionLine = GetLinesOffset(caretPosition, -(int)(caretY / LineHeight)).line;
			}
		}
		else if (!unfold)
		{
			// Fold didn't do anything, so try unfolding instead.
			FoldToDefinitions(true);
		}
	}
	
	private void StopFolding()
	{
		textBuffer.RemoveAllCollapsibleTextSpans();
		InvalidateSoftLineBreaks();
	}
	
	private void FoldSelection()
	{
		if (selectionStartPosition == null)
			return;
		
		var from = new TextPosition(caretPosition.line, caretPosition.characterIndex);
		var to = new TextPosition(selectionStartPosition.line, selectionStartPosition.characterIndex);
		
		FoldText(from, to);
	}
	
	private void FoldText(TextPosition from, TextPosition to)
	{
		if (from > to)
		{
			var swap = from;
			from = to;
			to = swap;
		}
		var span = new FGTextBuffer.CollapsibleTextSpan(from, to);
		span.IsCollapsed = true;
		
		var addedSpan = textBuffer.AddCollapsibleTextSpan(span);
		if (addedSpan == span)
		{
			textBuffer.NotifyAllEditorsOnFold(span);
			return;
		}
		
		textBuffer.RemoveCollapsibleTextSpan(addedSpan);
		textBuffer.NotifyAllEditorsOnUnfold(addedSpan);
	}

	private void ToggleFolding(FGTextBuffer.CollapsibleTextSpan span)
	{
		if (span.IsCollapsed)
		{
			span.IsCollapsed = false;
			textBuffer.NotifyAllEditorsOnUnfold(span);
		}
		else
		{
			span.IsCollapsed = true;
			textBuffer.NotifyAllEditorsOnFold(span);
		}
	}
	
	public void OnUnfoldText(TextPosition from, TextPosition to)
	{
		findHighlightedCollapsibleTextSpan = true;
		InvalidateSoftLineBreaks(from.line, to.line);
	}
	
	public void OnFoldText(TextPosition from, TextPosition to)
	{
		findHighlightedCollapsibleTextSpan = true;
		InvalidateSoftLineBreaks(from.line, to.line);

		var pos = new TextPosition(caretPosition.line, caretPosition.characterIndex);
		if (pos > from && pos < to)
		{
			caretPosition.line = from.line;
			caretPosition.characterIndex = from.index;
		}

		if (selectionStartPosition != null)
		{
			pos = new TextPosition(selectionStartPosition.line, selectionStartPosition.characterIndex);
			if (pos > from && pos < to)
			{
				selectionStartPosition.line = from.line;
				selectionStartPosition.characterIndex = from.index;
			}
			if (selectionStartPosition == caretPosition)
			{
				selectionStartPosition = null;
			}
		}
	}

	private void IndentMoreOrInsertTab(bool expandSnippets)
	{
		if (!TryEdit())
			return;
		
		if (selectionStartPosition != null)
		{
			IndentMore();
		}
		else
		{
			if (expandSnippets && ExpandSnippet(null))
				return;

			caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);

			textBuffer.BeginEdit("Insert Tab");

			int spaces = 0;
			while (((caretPosition.column - spaces) % lastTabSize) > 0)
			{
				int prev = caretPosition.characterIndex - spaces - 1;
				if (prev >= 0 && textBuffer.lines[caretPosition.line][prev] == ' ')
					++spaces;
				else
					break;
			}
			if (spaces > 0)
			{
				FGTextBuffer.CaretPos replaceSpaces = caretPosition.Clone();
				replaceSpaces.characterIndex -= spaces;
				replaceSpaces.column -= spaces;
				replaceSpaces.virtualColumn -= spaces;
				caretPosition = textBuffer.DeleteText(replaceSpaces, caretPosition);
			}
			caretPosition = textBuffer.InsertText(caretPosition, "\t");
			caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);
			textBuffer.UpdateHighlighting(caretPosition.line, caretPosition.line);

			textBuffer.EndEdit();
		}
	}

	private void IndentMore()
	{
		if (!TryEdit())
			return;
		
		textBuffer.BeginEdit("Increase Indent");
		
		bool hasSelection = selectionStartPosition != null;
		if (!hasSelection)
			selectionStartPosition = caretPosition.Clone();
		
		FGTextBuffer.CaretPos from = caretPosition.Clone();
		FGTextBuffer.CaretPos to = caretPosition.Clone();
		int fromLine = caretPosition.line;
		int toLine = caretPosition.line;
		if (caretPosition < selectionStartPosition)
		{
			to = selectionStartPosition.Clone();
			toLine = to.line;
		}
		else
		{
			from = selectionStartPosition.Clone();
			fromLine = from.line;
		}
		if (to.characterIndex == 0 && fromLine < toLine)
			--toLine;
		
		bool moveFromPos = from.characterIndex > 0;
		bool moveToPos = to.line == toLine && to.characterIndex > 0;
		for (FGTextBuffer.CaretPos i = new FGTextBuffer.CaretPos { characterIndex = 0, column = 0, line = fromLine, virtualColumn = 0 }; i.line <= toLine; ++i.line)
		{
			var resultPos = textBuffer.InsertText(i, "\t");
			if (moveFromPos && i.line == fromLine)
				from.characterIndex += resultPos.characterIndex;
			if (moveToPos && i.line == toLine)
				to.characterIndex += resultPos.characterIndex;
		}
		textBuffer.UpdateHighlighting(fromLine, toLine);
		
		from.column = from.virtualColumn = textBuffer.CharIndexToColumn(from.characterIndex, from.line);
		to.column = to.virtualColumn = textBuffer.CharIndexToColumn(to.characterIndex, to.line);
		
		if (caretPosition < selectionStartPosition)
		{
			caretPosition = from.Clone();
			selectionStartPosition = to.Clone();
		}
		else
		{
			selectionStartPosition = from.Clone();
			caretPosition = to.Clone();
		}
		
		if (!hasSelection)
			selectionStartPosition = null;
		
		if (wordWrapping)
		{
			caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);
			if (selectionStartPosition != null)
				selectionStartPosition.column = selectionStartPosition.virtualColumn = CharIndexToColumn(selectionStartPosition.characterIndex, selectionStartPosition.line);
		}
		
		textBuffer.EndEdit();
	}

	private void IndentLess()
	{
		if (!TryEdit())
			return;
		
		textBuffer.BeginEdit("Decrease Indent");
		
		bool hasSelection = selectionStartPosition != null;
		if (!hasSelection)
			selectionStartPosition = caretPosition.Clone();
		
		FGTextBuffer.CaretPos from = caretPosition.Clone();
		FGTextBuffer.CaretPos to = caretPosition.Clone();
		int fromLine = caretPosition.line;
		int toLine = caretPosition.line;
		if (caretPosition < selectionStartPosition)
		{
			to = selectionStartPosition.Clone();
			toLine = to.line;
		}
		else
		{
			from = selectionStartPosition.Clone();
			fromLine = from.line;
		}
		if (to.characterIndex == 0 && fromLine < toLine)
			--toLine;
		
		bool moveFromPos = from.characterIndex > 0;
		bool moveToPos = to.characterIndex > 0;
		for (FGTextBuffer.CaretPos i = new FGTextBuffer.CaretPos { characterIndex = 0, column = 0, line = fromLine, virtualColumn = 0 }; i.line <= toLine; ++i.line)
		{
			FGTextBuffer.CaretPos j = i.Clone();
			var line = textBuffer.lines[i.line];
			while (j.characterIndex < line.Length && FGTextBuffer.GetCharClass(line[j.characterIndex]) == 0)
			{
				j.column = j.virtualColumn = textBuffer.CharIndexToColumn(++j.characterIndex, j.line);
				if (j.column == lastTabSize)
					break;
			}
			if (i != j)
			{
				textBuffer.DeleteText(i, j);
				if (moveFromPos && i.line == fromLine)
					from.characterIndex = Mathf.Max(0, from.characterIndex - (j.characterIndex - i.characterIndex));
				if (moveToPos && i.line == toLine)
					to.characterIndex = Mathf.Max(0, to.characterIndex - (j.characterIndex - i.characterIndex));
			}
			else
			{
				if (i.line == toLine)
					moveToPos = false;
				
				if (fromLine <= i.line - 1)
					textBuffer.UpdateHighlighting(fromLine, i.line - 1);
				fromLine = i.line + 1;
				moveFromPos = false;
			}
		}
		if (fromLine <= toLine)
			textBuffer.UpdateHighlighting(fromLine, toLine);
		
		from.column = from.virtualColumn = textBuffer.CharIndexToColumn(from.characterIndex, from.line);
		to.column = to.virtualColumn = textBuffer.CharIndexToColumn(to.characterIndex, to.line);
		
		if (caretPosition < selectionStartPosition)
		{
			caretPosition = from.Clone();
			selectionStartPosition = to.Clone();
		}
		else
		{
			selectionStartPosition = from.Clone();
			caretPosition = to.Clone();
		}
		
		if (!hasSelection)
			selectionStartPosition = null;
		
		if (wordWrapping)
		{
			caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);
			if (selectionStartPosition != null)
				selectionStartPosition.column = selectionStartPosition.virtualColumn = CharIndexToColumn(selectionStartPosition.characterIndex, selectionStartPosition.line);
		}
		
		textBuffer.EndEdit();
	}

	private FGListPopup autocompleteWindow;

	private class KeywordAsSD : SymbolDefinition
	{
		public KeywordAsSD(string keyword)
		{
			name = keyword;
			kind = SymbolKind._Keyword;
		}
	}
	
	[NonSerialized]
	private SymbolDefinition showingArgumentsForMethod;
	[NonSerialized]
	private TextPosition showingArgumentsForToken;
	[NonSerialized]
	private Rect methodTokenRect;
	
	private void ShowArgumentsHint(ParseTree.Leaf methodLeaf)
	{
		showingArgumentsForMethod = null;
		showingArgumentsForToken = new TextPosition(-1, -1);

		if (methodLeaf.resolvedSymbol == null)
			FGResolver.ResolveNode(methodLeaf.parent);
		var methodSymbol = methodLeaf.resolvedSymbol;
		if (methodSymbol == null)
		{
			if (argumentsHint != null)
				argumentsHint.Hide();
		}
		else if(methodSymbol.kind == SymbolKind.Method || methodSymbol.kind == SymbolKind.Constructor || methodSymbol.kind == SymbolKind.MethodGroup)
		{
			var wasFlipped = false;
			if (argumentsHint != null)
			{
				wasFlipped = argumentsHint.Flipped;
				argumentsHint.Hide();
			}
			
			argumentsHint = FGTooltip.CreateTokenWidget(this, methodTokenRect, methodLeaf, false);
			if (argumentsHint)
			{
				argumentsHint.CurrentParameterIndex = currentArgumentIndex;
				
				showingArgumentsForMethod = methodLeaf.resolvedSymbol;
				showingArgumentsForToken = new TextPosition(methodLeaf.line, methodLeaf.tokenIndex);
				
				if (GetSoftLineBreaks(BufferToEditorLine(caretPosition.line)) != NO_SOFT_LINE_BREAKS)
					argumentsHint.Flipped = true;
				else if (wasFlipped && autocompleteWindow == null || methodLeaf.line < caretPosition.line)
					argumentsHint.Flipped = true;
				else if (autocompleteWindow != null)
					autocompleteWindow.Flipped = !argumentsHint.Flipped;
			}
		}
	}
	
	private bool InitAutocomplete(bool suggestionsOnly)
	{
		if (hasSelection || !textBuffer.isCsFile)
			return false;
		
		if (!FGListPopup.Init(this))
			return false;
			
		var data = new HashSet<SymbolDefinition>();
		FGListPopup.shortAttributeNames = false;

		bool typeArgumentList = false;
		bool expectedDelegateType = false;

		var tokenLeft = FGListPopup.tokenLeft;
		if (tokenLeft != null)
		{
			var completionLeaf = tokenLeft.parent;
			if (completionLeaf != null && completionLeaf.parent == null)
			{
				var lineIndex = completionLeaf.line;
				var tokenIndex = completionLeaf.tokenIndex;
				while (completionLeaf != null && completionLeaf.parent == null)
				{
					var token = textBuffer.GetTokenLeftOf(ref lineIndex, ref tokenIndex);
					if (token == null)
						break;
					completionLeaf = token.parent;
				}
				if (completionLeaf != null)
				{
					if (tokenLeft.text == "," &&
						completionLeaf.resolvedSymbol != null &&
						completionLeaf.resolvedSymbol is TypeDefinitionBase)
					{
						typeArgumentList = true;
					}
					tokenLeft = completionLeaf.token;
				}
			}
		}
			
		if (tokenLeft != null && (tokenLeft.parent == null || tokenLeft.parent.grammarNode == null))
		{
			//	Debug.Log(tokenLeft + "\nkind: " + tokenLeft.tokenKind);
		}
		else //if (tokenLeft != null)
		{
			//Debug.Log("tokenLeft: " + tokenLeft);
			//Debug.Log("tokenLeft: " + tokenLeft.parent.grammarNode);

			var scanner = textBuffer.Parser.MoveAfterLeaf(tokenLeft != null ? tokenLeft.parent : null);
			var currentScannerNode = scanner != null ? scanner.CurrentParseTreeNode : null;
			//	Debug.Log("scanner.CurrentParseTreeNode before: " + scanner.CurrentParseTreeNode);

			var grammar = CsGrammar.Instance;

			var tokenSet = new FGGrammar.TokenSet();
			if (scanner != null)
			{
				scanner.CollectCompletions(tokenSet);
				scanner.Delete();

				if (tryAutocomplete && tokenSet.Matches(grammar.tokenName))
				{
					CloseAutocomplete();
					return false;
				}
			}

			var expectedTypeTokenId = grammar.tokenExpectedType;
			if (suggestionsOnly && !tokenSet.Matches(expectedTypeTokenId) && (tokenLeft == null || tokenLeft.text != "override"))
			{
				CloseAutocomplete();
				return false;
			}
			
			tokenSet.Remove(grammar.tokenEOF);
			if (!CsParser.isCSharp4)
			{
				tokenSet.Remove(grammar.tokenInterpStrWhole);
				tokenSet.Remove(grammar.tokenInterpStrStart);
				tokenSet.Remove(grammar.tokenInterpStrMid);
				tokenSet.Remove(grammar.tokenInterpStrEnd);
				tokenSet.Remove(grammar.tokenInterpStrFormat);
			}
			tokenSet.Remove(grammar.tokenLiteral);
			
			var enclosingScopeNode = CsGrammar.EnclosingScopeNode(currentScannerNode);
			var enclosingScope = enclosingScopeNode != null ? enclosingScopeNode.scope : null;
			
			var parser = grammar.GetParser;
			var identifierTokenId = grammar.tokenIdentifier;
			var memberInitializerTokenId = grammar.tokenMemberInitializer;

			global::ScriptInspector.FGGrammar.BitArray bitArray;
			var tokenId = tokenSet.GetDataSet(out bitArray);

			var addSymbols = typeArgumentList || FGListPopup.IdentifiersOnly;
			var addSnippets = false;
			var addMemberInitializerNames = false;
			TypeDefinitionBase expectedType = null;
				
			if (tokenId != -1)
			{
				if (tokenId == identifierTokenId)
				{
					addSymbols = true;
				}
				else if (tokenId == memberInitializerTokenId)
				{
					addMemberInitializerNames = true;
				}
				else if (!FGListPopup.IdentifiersOnly)
				{
					var token = parser.GetToken(tokenId);
					if (token[0] == '_' || char.IsLetterOrDigit(token[0]))
					{
						data.Add(new KeywordAsSD(token));
						addSnippets = true;
					}
				}
			}
			else if (!bitArray.IsEmpty)
			{
				for (var i = 0; i < global::ScriptInspector.FGGrammar.BitArray.Length; ++i)
				{
					if (bitArray[i])
					{
						if (i == expectedTypeTokenId)
						{
							var argumentIndex = -1;
							ParseTree.Node parentNode = null;
							ParseTree.BaseNode typeDefiningNode = null;
								
							if (currentScannerNode != null && currentScannerNode.RuleName == "primaryExpression" &&
								tokenLeft != null && (tokenLeft.text == "new" || tokenLeft.text == "case"))
							{
								currentScannerNode = currentScannerNode.parent;
								while (currentScannerNode != null && currentScannerNode.childIndex == 0)
									currentScannerNode = currentScannerNode.parent;
								if (currentScannerNode!= null && currentScannerNode.RuleName == "expression")
									currentScannerNode = currentScannerNode.parent;
							}
								
							ParseTree.Node unaryExpressionNode = null;
							while (currentScannerNode != null && currentScannerNode.RuleName == "unaryExpression")
							{
								unaryExpressionNode = currentScannerNode;
								currentScannerNode = currentScannerNode.parent;
							}
								
							if (currentScannerNode != null)
							{
								switch (currentScannerNode.RuleName)
								{
								case "switchLabel":
									typeDefiningNode = currentScannerNode.parent.parent.parent.NodeAt(2);
									break;
								case "assignment":
								case "localVariableDeclarator":
								case "variableDeclarator":
								case "eventDeclarator":
								case "relationalExpression":
								case "inclusiveOrExpression":
								case "exclusiveOrExpression":
								case "andExpression":
								case "equalityExpression":
								case "multiplicativeExpression":
									typeDefiningNode = currentScannerNode.ChildAt(0);
									break;
								case "flatExpression":
									typeDefiningNode = currentScannerNode.firstChild;
									if (unaryExpressionNode != null && unaryExpressionNode.childIndex > 0)
									{
										var nextUnary = typeDefiningNode as ParseTree.Node;
										if (nextUnary.childIndex < unaryExpressionNode.childIndex)
										{
											var next = nextUnary.nextSibling;
											while (next != null)
											{
												if (next is ParseTree.Leaf)
												{
													next = next.nextSibling;
												}
												else if (next.childIndex >= unaryExpressionNode.childIndex)
												{
													break;
												}
												else
												{
													var nextNode = next as ParseTree.Node;
													if (nextNode.RuleName == "unaryExpression")
														nextUnary = nextNode;
													next = next.nextSibling;
												}
											}
										}
										typeDefiningNode = nextUnary;
									}
									break;
								case "variableInitializerList":
									parentNode = currentScannerNode.parent.parent;
									if (parentNode.RuleName == "arrayCreationExpression")
									{
										typeDefiningNode = parentNode.parent.NodeAt(1);
									}
									else
									{
										typeDefiningNode = null;
									}
									break;
								case "localVariableInitializer":
									typeDefiningNode = currentScannerNode.parent.parent.parent.NodeAt(0);
									break;
								case "variableInitializer":
									typeDefiningNode = currentScannerNode.parent.LeafAt(0);
									break;
								case "arguments":
								case "attributeArguments":
									typeDefiningNode = currentScannerNode;
									argumentIndex = 0;
									break;
								case "argumentList":
								case "attributeArgumentList":
									typeDefiningNode = currentScannerNode.parent;
									argumentIndex = currentArgumentIndex;
									break;
								case "argument":
									typeDefiningNode = currentScannerNode.parent.parent;
									argumentIndex = currentArgumentIndex;
									break;
								case "attributeArgument":
									typeDefiningNode = currentScannerNode.parent.parent;
									argumentIndex = currentArgumentIndex;
									break;
								case "fixedParameter":
									typeDefiningNode = currentScannerNode.FindChildByName("type") as ParseTree.Node;
									break;
								case "throwStatement":
								case "throwExpression":
									expectedType = SymbolDefinition.builtInTypes_Exception;
									break;
								case "constantDeclarator":
									typeDefiningNode = currentScannerNode.parent.parent.NodeAt(1);
									break;
								case "arrayInitializer":
									typeDefiningNode = currentScannerNode.parent.FindPreviousNode();
									break;
								case "defaultArgument":
									typeDefiningNode = currentScannerNode.parent.ChildAt(0);
									break;
								case "memberInitializer":
									typeDefiningNode = currentScannerNode.ChildAt(0);
									break;
								case "statementList":
								case "statement":
								case "objectOrCollectionInitializer":
									break;
								default:
#if SI3_WARNINGS
									Debug.LogWarning(tokenLeft);
									Debug.LogWarning("CurrentParseTreeNode: " + currentScannerNode);
#endif
									break;
								}
							}
								
							if (typeDefiningNode != null && enclosingScope != null)
							{
								var resolved = FGResolver.GetResolvedSymbol(typeDefiningNode);
								if (resolved != null && resolved.kind != SymbolKind.Error)
								{
									if (argumentIndex < 0)
									{
										expectedType = resolved.TypeOf() as TypeDefinitionBase;
									}
									else
									{
										ConstructedTypeDefinition constructedType = null;
											
										if (resolved.kind == SymbolKind.MethodGroup)
										{
											var methodGroup = resolved as MethodGroupDefinition;
											var constructedGroup = resolved as ConstructedSymbolReference;
											constructedType = resolved.parentSymbol as ConstructedTypeDefinition;
											if (methodGroup == null && constructedGroup != null)
												methodGroup = constructedGroup.referencedSymbol as MethodGroupDefinition;
											if (methodGroup != null)
											{
												foreach (var method in methodGroup.methods)
												{
													var parameters = method.GetParameters();
													if (parameters != null && argumentIndex < parameters.Count)
													{
														resolved = method;
														break;
													}
												}
											}
										}
										if (resolved.kind == SymbolKind.Method)
										{
											if (resolved.IsExtensionMethod)
												++argumentIndex;
											var parameters = resolved.GetParameters();
											if (argumentIndex < parameters.Count)
												expectedType = parameters[argumentIndex].TypeOf() as TypeDefinitionBase;
											if (expectedType != null && constructedType != null)
												expectedType = expectedType.SubstituteTypeParameters(constructedType);
										}
									}
								}
							}
						}
						else if (i == identifierTokenId)
						{
							addSymbols = true;
						}
						else if (i == memberInitializerTokenId)
						{
							addMemberInitializerNames = true;
						}
						else if (!FGListPopup.IdentifiersOnly)
						{
							var token = parser.GetToken(i);
							if (token[0] == '_' || char.IsLetterOrDigit(token[0]))
							{
								data.Add(new KeywordAsSD(token));
								addSnippets = true;
							}
						}
					}
				}
			}
			else if (!FGListPopup.IdentifiersOnly)
			{
				//Debug.LogWarning("Empty BitArray!!! Adding all keywords...");
				foreach (var i in textBuffer.Parser.Keywords)
					data.Add(new KeywordAsSD(i.Value));
				foreach (var i in textBuffer.Parser.BuiltInLiterals)
					data.Add(new KeywordAsSD(i.Value));

				addSymbols = true;
				addSnippets = true;
			}

			//	Debug.Log("CurrentParseTreeNode.declaration: " + scanner.CurrentParseTreeNode.declaration);
			if (addSymbols && tokenLeft != null)
			{
				ParseTree.BaseNode completionNode = tokenLeft.parent;
				if (completionNode.IsLit("}"))
				{
					completionNode = completionNode.FindNextLeaf();
				}
				else if (completionNode.IsLit("=>"))
				{
					completionNode = completionNode.parent.NodeAt(completionNode.childIndex + 1) ?? completionNode;
				}
				else if (completionNode.IsLit("]") && completionNode.parent.RuleName == "attributes")
				{
					completionNode = completionNode.parent.parent.NodeAt(completionNode.parent.childIndex + 1);
				}

				var completionTypes = grammar.GetCompletionTypes(completionNode);
				//	Debug.Log(completionTypes);
				if (addMemberInitializerNames)
				{
					if (!addSnippets)
						completionTypes = IdentifierCompletionsType.MemberName;
					else
						completionTypes |= IdentifierCompletionsType.MemberName;
				}
				FGResolver.GetCompletions(completionTypes, completionNode, data, textBuffer.assetPath);

				data.RemoveWhere(x => !x.IsValid() || x.name == "" || x.name[0] == '<' || x.name[0] == '.');
					
				FGListPopup.shortAttributeNames = (completionTypes & IdentifierCompletionsType.AttributeClassType) != 0;
				if (FGListPopup.shortAttributeNames)
					FilterCompletions(data);
					
				SymbolDefinition topSuggestion = null;
				if (expectedType != null)
				{
					if (expectedType.kind == SymbolKind.Delegate)
					{
						if (tryAutocomplete)
							expectedDelegateType = true;
					}
					else if (expectedType.kind == SymbolKind.Enum)
					{
						if (!data.Contains(expectedType))
						{
							var typeName = expectedType.RelativeName(enclosingScope);
								
							var suggestion = new KeywordAsSD(typeName);
							suggestion.kind = SymbolKind.Enum;
							data.Add(suggestion);
								
							topSuggestion = topSuggestion ?? suggestion;
						}
						else
						{
							topSuggestion = topSuggestion ?? expectedType;
						}
					}
					else if (tokenLeft != null && tokenLeft.text == "new")
					{
						foreach (TypeDefinitionBase type in enclosingScope.GetAssembly().EnumAssignableTypesFor(expectedType))
						{
							if (type.kind == SymbolKind.Enum || type.kind == SymbolKind.Delegate ||
								type.kind == SymbolKind.Interface || type.IsStatic || type.IsAbstract)
							{
								continue;
							}
								
							if (data.Contains(type))
							{
								topSuggestion = topSuggestion ?? type;
								continue;
							}
								
							var typeName = expectedType.RelativeName(enclosingScope);
								
							var suggestion = new KeywordAsSD(typeName);
							suggestion.kind = SymbolKind.Enum;
							data.Add(suggestion);
								
							topSuggestion = topSuggestion ?? suggestion;
						}
					}
						
					if (topSuggestion != null)
					{
						FGListPopup.SetTopSuggestion(topSuggestion);
					}
				}
					
				if (suggestionsOnly && topSuggestion == null && (tokenLeft == null || tokenLeft.text != "override"))
				{
					CloseAutocomplete();
					return false;
				}
			}
			else if (addMemberInitializerNames && tokenLeft != null)
			{
				FGResolver.GetCompletions(IdentifierCompletionsType.MemberName, tokenLeft.parent, data, textBuffer.assetPath);
			}
				
			if (tokenLeft != null && !addSymbols && addSnippets && data.Count == 2)
			{
				var first = data.First();
				if (first.name == "as" || first.name == "is")
				{
					FGListPopup.defensiveMode = true;
				}
			}
				
			if (addSnippets)
			{
				if (suggestionsOnly && tokenLeft != null && tokenLeft.text == "override")
					data.Clear();
					
				try
				{
					foreach (var snippet in CodeSnippets.EnumSnippets(codePathSymbol, tokenSet, tokenLeft, enclosingScope))
						data.Add(snippet);
				}
					catch (System.Exception e)
					{
						Debug.LogException(e);
					}
			}
		}

		if (expectedDelegateType)
			FGListPopup.defensiveMode = true;
				
		if (data.Count == 0)
			return false;

		var anyMatches = FGListPopup.SetCompletionData(data);
		if (!anyMatches)
			return false;
		
		return true;
	}
	
	[NonSerialized]
	private bool autocompleteInitialized;

	private void Autocomplete(bool suggestionsOnly)
	{
		if (hasSelection || !textBuffer.isCsFile)
			return;
		
		if (autocompleteWindow == null)
		{
			if (!autocompleteInitialized && !InitAutocomplete(suggestionsOnly))
				return;
			autocompleteInitialized = false;
			
			Rect caretRect = GetCaretRect(caretPosition);
			
			caretRect.x += 4f + scrollViewRect.x - scrollPosition.x;
			caretRect.y += 4f + scrollViewRect.y - scrollPosition.y;

			autocompleteWindow = FGListPopup.Create(this, caretRect, argumentsHint != null && !argumentsHint.Flipped);
			if (autocompleteWindow == null)
				return;
	
			FGListPopup.UpdateTypedInPart();
			autocompleteWindow.Repaint();
	
			if (argumentsHint != null)
				argumentsHint.Flipped = !autocompleteWindow.Flipped;
					
			//if (argumentsHint != null)
			//	argumentsHint.Flipped = !autocompleteWindow.Flipped;
		}
	}

	private void FilterCompletions(HashSet<SymbolDefinition> data)
	{
		data.RemoveWhere(x => !DerivesFromOrContains(x, attributeType, true));
	}

	private static TypeDefinitionBase _attributeType;
	private static TypeDefinitionBase attributeType {
		get
		{
			if (_attributeType == null)
				_attributeType = TypeReference.To(typeof(System.Attribute)).definition as TypeDefinitionBase;
			return _attributeType;
		}
	}

	private bool DerivesFromOrContains(SymbolDefinition symbol, TypeDefinitionBase type, bool checkReferencedAssemblies)
	{
		if (symbol.kind == SymbolKind.Namespace)
		{
			for (var i = 0; i < symbol.members.Count; ++i)
			{
				var child = symbol.members[i];
				if (DerivesFromOrContains(child, type, true))
					return true;
			}
			
			if (checkReferencedAssemblies)
			{
				var namespaceDefinition = (NamespaceDefinition) symbol;
				var assembly = ((CompilationUnitScope) textBuffer.Parser.parseTree.root.scope).assembly;
				if (assembly != null)
				{
					foreach (var ra in assembly.referencedAssemblies)
					{
						var nsDef = ra.FindSameNamespace(namespaceDefinition);
						if (nsDef != null)
						{
							for (var i = 0; i < nsDef.members.Count; ++i)
							{
								var child = nsDef.members[i];
								if (DerivesFromOrContains(child, type, false))
									return true;
							}
						}
					}
				}
			}
		}
		else if (symbol.kind == SymbolKind.Class)
		{
			var asType = symbol as TypeDefinitionBase;
			if (asType.DerivesFrom(type))
				return true;
		}
		return false;
	}

	//private string ParentsLog(ParseTree.BaseNode node)
	//{
	//	if (node == null)
	//		return "null";

	//	var s = string.Empty;
	//	for (int i = node.Depth; i >= 0; --i)
	//	{
	//		s = '\n' + new string(' ', i * 2) + (node is ParseTree.Node ? ((ParseTree.Node) node).RuleName : ((ParseTree.Leaf) node).grammarNode) + " " + node.childIndex + s;
	//		node = node.parent;
	//	}
	//	return s;
	//}
	
	public void CloseAllPopups()
	{
		if (tokenTooltip != null)
			tokenTooltip.Hide();
		if (argumentsHint != null)
			CloseArgumentsHint();
		if (autocompleteWindow != null)
			CloseAutocomplete();
	}

	public void CloseAutocomplete()
	{
		if (autocompleteWindow != null)
		{
			EditorApplication.delayCall += autocompleteWindow.Close;
			autocompleteWindow = null;
		}
	}

	public void CloseArgumentsHint()
	{
		if (argumentsHint != null)
		{
			argumentsHint.Hide();
			argumentsHint = null;

			showingArgumentsForMethod = null;
			showingArgumentsForToken = new TextPosition(-1, -1);
		}
	}
	
	EditorLine GetEditorLine(int line)
	{
		var result = GetLineInView(line);
		if (result != null)
			return result;
		
		result = CreateEditorLine(line);
		return result;
	}
	
	EditorLine GetOrCreateLineInView(int line)
	{
		var result = GetLineInView(line);
		if (result != null)
			return result;

		line = BufferToEditorLine(line);

		LayoutLinesInView(line, line);
		result = GetLineInView(line);
		return result;
	}
	
	EditorLine GetLineInView(int line)
	{
		for (int i = linesInView.Count; i --> 0; )
		{
			var lineInView = linesInView[i];
			if (line >= lineInView.lineFrom && line <= lineInView.lineTo)
				return lineInView;
		}
		return null;
	}

	public Vector2 BufferToViewPosition(TextPosition position, bool preferNextLine)
	{
		var editorLine = GetLineInView(position.line);
		if (editorLine == null)
		{
			LayoutLinesInView(position.line, position.line);
			editorLine = GetLineInView(position.line);
		}
			
		if (editorLine != null)
		{
			var tokenIndex = editorLine.FindTokenIndexAfter(position);
			if (tokenIndex >= 0)
			{
				Vector2 result;
				
				if (tokenIndex == editorLine.lineTokens.Count && tokenIndex < editorLine.lineRects.Count)
				{
					result = editorLine.lineRects[tokenIndex].position;
				}
				else
				{
					result = editorLine.lineRects[tokenIndex].position;
					var startPos = editorLine.lineTokenStartPos[tokenIndex];
					if (startPos < position)
					{
						if (editorLine.lineTokens[tokenIndex].tokenKind != SyntaxToken.Kind.Collapsed)
							result.x += GetTextWidth(startPos.line, startPos.index, position.index, result.x - marginLeft);
					}
					else if (!preferNextLine && tokenIndex > 0 && result.x == marginLeft)
					{
						var tokenRect = editorLine.lineRects[tokenIndex - 1];
						result.x = tokenRect.xMax;
						result.y = tokenRect.y;
					}
				}

				result.x -= marginLeft;
				result.y -= editorLine.lineRects[0].y;
				return result;
			}
		}
		
		var line = BufferToEditorLine(position.line);
		
		int row;
		//ㅎ호ㅗ횰퓨ㅜㅎ퓨
		var softLineBreaks = GetSoftLineBreaks(line);
		row = FindFirstIndexGreaterThanOrEqualTo<int>(softLineBreaks, position.index);
		if (preferNextLine && row < softLineBreaks.Count && position.index == softLineBreaks[row])
			++row;

		var rowStart = row > 0 ? softLineBreaks[row - 1] : 0;
		float xOffset = GetCharXOffset(position.index, position.line, rowStart);

		return new Vector2(xOffset, LineHeight * row);
	}

	public FGTextBuffer.CaretPos ViewToBufferPosition(int line, int row, int column)
	{
		if (line >= textBuffer.formattedLines.Count)
			line = textBuffer.formattedLines.Count - 1;
		if (line < 0)
			line = 0;
		if (row < 0)
			row = 0;

		var position = new FGTextBuffer.CaretPos { column = column, line = line, virtualColumn = column };

		var softLineBreaks = GetSoftLineBreaks(line);
		if (row > softLineBreaks.Count)
			row = softLineBreaks.Count;
		var rowStart = row > 0 ? softLineBreaks[row - 1] : 0;

		position.characterIndex = textBuffer.ColumnToCharIndex(ref position.column, line, rowStart);

		var rowEnd = row < softLineBreaks.Count ? softLineBreaks[row] : textBuffer.lines[line].Length;
		if (position.characterIndex > rowEnd)
			position.characterIndex = rowEnd;

	//	position.column += rowStart;
	//	if (row < softLineBreaks.Count && position.column > softLineBreaks[row])
	//		position.column = softLineBreaks[row];
	//	position.characterIndex = textBuffer.ColumnToCharIndex(ref position.column, line);
		return position;
	}
	
	static List<TextInterval> tempTextIntervals = new List<TextInterval>();
	List<FGTextBuffer.CollapsibleTextSpan> tempCollapsedTokenSpans = new List<FGTextBuffer.CollapsibleTextSpan>();
	List<TextPosition> tempSoftLineBreakPositions = new List<TextPosition>();
	
	int BufferPositionToEditorColumn(int line, int characterIndex, int virtualColumn = 0)
	{
		var firstLine = BufferToEditorLine(line);
		if (characterIndex == 0 && firstLine == line)
			return 0;
		
		var textPosition = new TextPosition(line, characterIndex);
		var viewPos = BufferToViewPosition(textPosition, virtualColumn == 0);
		var column = Mathf.RoundToInt(viewPos.x / charSize.x);
		
		return column;
		
		//var editorLine = GetLineInView(firstLine);
		//if (editorLine == null)
		//{
		//	LayoutLinesInView(firstLine, firstLine);
		//	editorLine = GetLineInView(firstLine);
		//}
		
		//var tokens = GetLineTokens(firstLine, tempCollapsedTokenSpans);
		//if (tokens == null)
		//	return 0;
		
		//var textPos = new TextPosition(line, characterIndex);
		
		//List<int> softLineBreaks = GetSoftLineBreaks(firstLine);
		//int numSoftRows = softLineBreaks.Count + 1;
		//int softRow;
		
		//if (numSoftRows == 1)
		//{
		//	softRow = 0;
		//}
		//else if (tempCollapsedTokenSpans.Count > 0)
		//{
		//	tempSoftLineBreakPositions.Clear();
			
		//	var tokenIndex = 0;
		//	var token = tokens[tokenIndex];
		//	var tokenLength = token.text.length;
			
		//	var collapsedTokenSpan = tempCollapsedTokenSpans[tokenIndex];
			
		//	var charIndex = 0;
		//	var currentPosition = new TextPosition(firstLine, 0);
		//	for (var i = 0; i < softLineBreaks.Count; ++i)
		//	{
		//		var nextBreak = softLineBreaks[i];
		//		while (charIndex < nextBreak)
		//		{
		//			while (tokenLength == 0)
		//			{
		//				++tokenIndex;
		//				collapsedTokenSpan = tempCollapsedTokenSpans[tokenIndex];
		//				token = tokens[tokenIndex];
		//				tokenLength = token.text.length;
		//			}
					
		//			var nextCharIndex = charIndex + tokenLength;
		//			var overflow = nextCharIndex - nextBreak;
		//			if (overflow < 0)
		//			{
		//				if (collapsedTokenSpan != null)
		//				{
		//					currentPosition = collapsedTokenSpan.span.End;
		//				}
		//				else
		//				{
		//					currentPosition.index += tokenLength;
		//				}
		//				charIndex += tokenLength;
		//				tokenLength = 0;
		//				continue;
		//			}
					
		//			var lengthToBreak = tokenLength - overflow;
		//			charIndex += lengthToBreak;
		//			tokenLength -= lengthToBreak;
					
		//			if (collapsedTokenSpan != null)
		//			{
		//				currentPosition = collapsedTokenSpan.span.End;
		//			}
		//			else
		//			{
		//				currentPosition.index += lengthToBreak;
		//			}
		//			tempSoftLineBreakPositions.Add(currentPosition);
		//			break;
		//		}
		//	}
			
		//	softRow = FindFirstIndexGreaterThanOrEqualTo(tempSoftLineBreakPositions, new TextPosition(line, characterIndex));
		//	if (softRow < softLineBreaks.Count && textPos == tempSoftLineBreakPositions[softRow] && virtualColumn == 0)
		//		return 0; //++softRow;
		//}
		//else
		//{
		//	softRow = FindFirstIndexGreaterThanOrEqualTo<int>(softLineBreaks, characterIndex);
		//	if (softRow < softLineBreaks.Count && characterIndex == softLineBreaks[softRow] && virtualColumn == 0)
		//		return 0; //++softRow;
		//}
		
		//var column = 0;
		//var currentPos = softRow > 0 ? tempSoftLineBreakPositions[softRow - 1] : 0;
		
		//for (var i = 0; i < tokens.Count; ++i)
		//{
			
		//}
		
		//return column;
	}

	public FGTextBuffer.CaretPos GetLinesOffset(FGTextBuffer.CaretPos position, int linesDown)
	{
		var pos = position.Clone();

		//if (!wordWrapping)
		//{
		//	pos.line += linesDown;
		//	if (pos.line < 0)
		//	{
		//		pos.line = 0;
		//		pos.characterIndex = 0;
		//		pos.column = 0;
		//		pos.virtualColumn = 0;
		//	}
		//	else if (pos.line >= textBuffer.lines.Count)
		//	{
		//		pos.line = textBuffer.lines.Count - 1;
		//		pos.characterIndex = textBuffer.lines[pos.line].Length;
		//		pos.column = textBuffer.CharIndexToColumn(pos.characterIndex, pos.line);
		//		pos.virtualColumn = pos.column;
		//	}
		//	else
		//	{
		//		if (pos.line != position.line)
		//			pos.column = pos.virtualColumn;
		//		pos.characterIndex = textBuffer.ColumnToCharIndex(ref pos.column, pos.line);
		//	}
		//}
		//else
		{
			var firstLine = BufferToEditorLine(pos.line);

			var tokens = GetLineTokens(firstLine, tempCollapsedTokenSpans);
			if (tokens == null)
				return pos;
			
			List<int> softLineBreaks = GetSoftLineBreaks(firstLine);
			int numSoftRows = softLineBreaks.Count + 1;
			int softRow;
			
			if (numSoftRows == 1)
			{
				softRow = 0;
			}
			else if (tempCollapsedTokenSpans.Count > 0)
			{
				tempSoftLineBreakPositions.Clear();
				
				var tokenIndex = 0;
				var token = tokens[tokenIndex];
				var tokenLength = token.text.length;
				
				var collapsedTokenSpan = tempCollapsedTokenSpans[tokenIndex];
				
				var charIndex = 0;
				var currentPosition = new TextPosition(firstLine, 0);
				for (var i = 0; i < softLineBreaks.Count; ++i)
				{
					var nextBreak = softLineBreaks[i];
					while (charIndex < nextBreak)
					{
						while (tokenLength == 0)
						{
							++tokenIndex;
							collapsedTokenSpan = tempCollapsedTokenSpans[tokenIndex];
							token = tokens[tokenIndex];
							tokenLength = token.text.length;
						}
						
						var nextCharIndex = charIndex + tokenLength;
						var overflow = nextCharIndex - nextBreak;
						if (overflow < 0)
						{
							if (collapsedTokenSpan != null)
							{
								currentPosition = collapsedTokenSpan.span.End;
							}
							else
							{
								currentPosition.index += tokenLength;
							}
							charIndex += tokenLength;
							tokenLength = 0;
							continue;
						}
						
						var lengthToBreak = tokenLength - overflow;
						charIndex += lengthToBreak;
						tokenLength -= lengthToBreak;
						
						if (collapsedTokenSpan != null)
						{
							currentPosition = collapsedTokenSpan.span.End;
						}
						else
						{
							currentPosition.index += lengthToBreak;
						}
						tempSoftLineBreakPositions.Add(currentPosition);
						break;
					}
				}
				
				softRow = FindFirstIndexGreaterThanOrEqualTo(tempSoftLineBreakPositions, new TextPosition(pos.line, pos.characterIndex));
			}
			else
			{
				softRow = FindFirstIndexGreaterThanOrEqualTo<int>(softLineBreaks, pos.characterIndex);
			}
			
			if (softRow < softLineBreaks.Count && pos.characterIndex == softLineBreaks[softRow] && pos.virtualColumn == 0)
				++softRow;

			while (linesDown > 0)
			{
				--linesDown;
				if (softRow < numSoftRows - 1)
				{
					++softRow;
				}
				else
				{
					if (pos.line == textBuffer.lines.Count - 1)
					{
						int lastRowStart = softRow > 0 ? softLineBreaks[softRow - 1] : 0;
						pos.virtualColumn = softRow < softLineBreaks.Count ? softLineBreaks[softRow] : textBuffer.CharIndexToColumn(textBuffer.lines[pos.line].Length, pos.line, lastRowStart);
						break;
					}
					
					++pos.line;
					FGTextBuffer.CollapsibleTextSpan overlappingSpan;
					while ((overlappingSpan = GetCharCollapsedSpan(pos.line, -1)) != null)
					{
						pos.line = overlappingSpan.span.End.line + 1;
					}
					if (pos.line >= textBuffer.lines.Count)
					{
						pos.line = textBuffer.lines.Count - 1;
					}
					//while (pos.line < textBuffer.lines.Count && IsLineBeginningCollapsed(pos.line))
					//{
					//	++pos.line;
					//}
					firstLine = pos.line;
					if (pos.line == textBuffer.lines.Count)
					{
						--pos.line;
						int lastRowStart = softRow > 0 ? softLineBreaks[softRow - 1] : 0;
						pos.virtualColumn = softRow < softLineBreaks.Count ? softLineBreaks[softRow] : textBuffer.CharIndexToColumn(textBuffer.lines[pos.line].Length, pos.line, lastRowStart);
						break;
					}
					
					softRow = 0;
					softLineBreaks = GetSoftLineBreaks(pos.line);
					numSoftRows = softLineBreaks.Count + 1;
				}
			}
			while (linesDown < 0)
			{
				++linesDown;
				if (softRow > 0)
				{
					--softRow;
				}
				else
				{
					if (firstLine == 0)
					{
						pos.virtualColumn = 0;
						break;
					}
					
					--firstLine;
					FGTextBuffer.CollapsibleTextSpan overlappingSpan;
					while ((overlappingSpan = GetCharCollapsedSpan(firstLine, -1)) != null)
					{
						firstLine = overlappingSpan.span.Start.line;
					}
					//while (IsLineBeginningCollapsed(firstLine))
					//	--firstLine;
					
					softLineBreaks = GetSoftLineBreaks(firstLine);
					numSoftRows = softLineBreaks.Count + 1;
					softRow = numSoftRows - 1;
				}
			}
			
			var column = pos.virtualColumn;
			var textPosition = EditorToBufferPosition(firstLine, ref column, softRow);
			
			pos.column = column;
			pos.line = textPosition.line;
			pos.characterIndex = textPosition.index;

            //int rowStart = softRow > 0 ? softLineBreaks[softRow - 1] : 0;
			//int rowEnd = softRow < softLineBreaks.Count ? softLineBreaks[softRow] : textBuffer.lines[pos.line].Length;

			//pos.column = pos.virtualColumn;
			//pos.characterIndex = textBuffer.ColumnToCharIndex(ref pos.column, pos.line, rowStart);
			//if (pos.characterIndex > rowEnd)
			//{
			//	pos.characterIndex = rowEnd;
			//	pos.column = textBuffer.CharIndexToColumn(pos.characterIndex, pos.line, rowStart);
			//}
		}

		return pos;
    }

	public TextPosition EditorToBufferPosition(int line, int column, int softRow)
	{
		var tempColumn = column;
		return EditorToBufferPosition(line, ref tempColumn, softRow);
	}

	public TextPosition EditorToBufferPosition(int line, ref int column, int softRow)
	{
		var firstLine = BufferToEditorLine(line);

		var tokens = GetLineTokens(firstLine, tempCollapsedTokenSpans);
		if (tokens == null || tokens.Count == 0)
		{
			column = 0;
			return new TextPosition(line, 0);
		}
				
		var softLineBreaks = GetSoftLineBreaks(firstLine);
		var numSoftRows = softLineBreaks.Count + 1;
		
		var tokenIndex = 0;
		var token = tokens[tokenIndex];
		var tokenLength = token.text.length;
			
		var collapsedTokenSpan =
			token.tokenKind == SyntaxToken.Kind.Collapsed ? tempCollapsedTokenSpans[tokenIndex] : null;
			
		var charIndex = 0;
		var currentPosition = new TextPosition(line, 0);
		
		
		// First, find the soft row's beginning.
		
		for (var i = 0; i < softRow; ++i)
		{
			var nextBreak = softLineBreaks[i];
			while (charIndex < nextBreak)
			{
				while (tokenLength == 0)
				{
					++tokenIndex;
					token = tokens[tokenIndex];
					tokenLength = token.text.length;
					collapsedTokenSpan =
						token.tokenKind == SyntaxToken.Kind.Collapsed ? tempCollapsedTokenSpans[tokenIndex] : null;
				}
				
				var nextCharIndex = charIndex + tokenLength;
				var overflow = nextCharIndex - nextBreak;
				if (overflow < 0)
				{
					if (collapsedTokenSpan != null)
					{
						currentPosition = collapsedTokenSpan.span.End;
					}
					else
					{
						currentPosition.index += tokenLength;
					}
					charIndex += tokenLength;
					tokenLength = 0;
					continue;
				}
				
				var lengthToBreak = tokenLength - overflow;
				charIndex += lengthToBreak;
				tokenLength -= lengthToBreak;
				
				//if (collapsedTokenSpan != null)
				//{
				//	currentPosition = collapsedTokenSpan.span.End;
				//}
				//else
				//{
					currentPosition.index += lengthToBreak;
				//}

				break;
			}
		}


		// Then find text buffer position for the given column in this soft row.
		
		var nextSoftBreak = softRow < softLineBreaks.Count ? softLineBreaks[softRow] : int.MaxValue;
		
		//var currentColumn = 0;
		//var prevColumn = 0;
		var prevPosition = currentPosition;
		
		var currentX = 0f;
		var prevX = 0f;
		var x = column * charSize.x - 0.1f;
		
		var tokenText = token.text;
		var tokenTextIndex = tokenText.length - tokenLength;
		tokenLength = tokenText.length;

		Font font = null;
		int fontSize = 0;
		FontStyle fontStyle = FontStyle.Normal;

		var numTokens = tokens.Count;
		while (tokenIndex < numTokens && currentX < x)
		{
			while (tokenLength == tokenTextIndex)
			{
				++tokenIndex;
				if (tokenIndex == numTokens)
				{
					column = (int)((currentX + 0.1f) / charSize.x);
					return currentPosition;
				}
				
				token = tokens[tokenIndex];
				tokenText = token.text;
				tokenLength = tokenText.length;
				tokenTextIndex = 0;
				collapsedTokenSpan =
					token.tokenKind == SyntaxToken.Kind.Collapsed ? tempCollapsedTokenSpans[tokenIndex] : null;
			}
			
			if (collapsedTokenSpan != null)
			{
				prevPosition = currentPosition;
				currentPosition = collapsedTokenSpan.span.End;
				
				prevX = currentX;
				currentX += tokenLength * charSize.x;
				
				charIndex += tokenLength;
				tokenTextIndex = tokenLength;
				
				continue;
			}
			
			var tabSize = lastTabSize * charSize.x;
			
			while (tokenTextIndex < tokenLength && currentX < x)
			{
				prevX = currentX;
				prevPosition = currentPosition;
				
				float charWidth;
				var currentChar = tokenText[tokenTextIndex];
				if (currentChar == '\t')
				{
					var newXOffset = currentX + 0.75f * charSize.x;
					newXOffset += tabSize - (newXOffset % tabSize);
					charWidth = newXOffset - currentX;
				}
				else if (currentChar < 0x7f)
				{
					charWidth = charSize.x;
				}
				else
				{
					if (font == null)
					{
						font = styles.normalStyle.font;
						fontSize = styles.normalStyle.fontSize;
						fontStyle = styles.normalStyle.fontStyle;
						//font.RequestCharactersInTexture(text, fontSize, fontStyle);
					}
				
					CharacterInfo chInfo;
					font.GetCharacterInfo(currentChar, out chInfo, fontSize, fontStyle);
					charWidth = chInfo.advance;
				}
				currentX += charWidth;
				//if (currentChar == '\t')
				//	currentColumn += lastTabSize - (currentColumn % lastTabSize);
				//else
				//	++currentColumn;
				
				++currentPosition.index;
				++charIndex;
				++tokenTextIndex;
				
				if (charIndex == nextSoftBreak)
				{
					tokenIndex = numTokens;
					break;
				}
			}
		}
		//ㅎ호ㅗ횰퓨ㅜㅎ퓨

		if (tokenIndex == numTokens)
		{
			column = (int)((currentX + 0.1f) / charSize.x);
		}
		else if (currentX - x > x - prevX)
		{
			currentPosition = prevPosition;
			column = (int)((prevX + 0.1f) / charSize.x);
		}
		else
		{
			column = (int)((currentX + 0.1f) / charSize.x);
		}
		//if (currentColumn > column)
		//{
		//	var columnDifference = currentColumn - column;
			
		//	var mod = col % tabSize;
		//	if (mod < tabSize / 2)
		//	{
		//		--col;
		//		--i;
		//		column -= column % tabSize;
		//	}
		//	else
		//	{
		//		column += tabSize - (column % tabSize);
		//	}
		//}
		return currentPosition;
	}

	public SyntaxToken GetTokenAtCursor()
	{
		int lineIndex, tokenIndex;
		return GetTokenAtCursor(out lineIndex, out tokenIndex);
	}
	
	public SyntaxToken GetTokenAtCursor(out int lineIndex, out int tokenIndex)
	{
		if (textBuffer == null)
		{
			lineIndex = -1;
			tokenIndex = -1;
			return null;
		}
		
		bool atTokenEnd;
		var token = textBuffer.GetTokenAt(caretPosition, out lineIndex, out tokenIndex, out atTokenEnd);
		if (token != null)
		{
			if (atTokenEnd && (
				token.tokenKind == SyntaxToken.Kind.Whitespace ||
				token.tokenKind != SyntaxToken.Kind.Identifier &&
				token.tokenKind != SyntaxToken.Kind.BuiltInLiteral &&
				token.tokenKind != SyntaxToken.Kind.ContextualKeyword &&
				token.tokenKind != SyntaxToken.Kind.Keyword &&
				token.tokenKind != SyntaxToken.Kind.Preprocessor &&
				token.tokenKind != SyntaxToken.Kind.PreprocessorSymbol))
			{
				var tokens = textBuffer.formattedLines[lineIndex].tokens;
				if (tokenIndex < tokens.Count - 1)
					token = tokens[tokenIndex + 1];
			}
		}
		return token;
	}
	
	private SyntaxToken GetTokenAtPosition(int line, int characterIndex)
	{
		int lineIndex, tokenIndex;
		bool atTokenEnd;
		var token = textBuffer.GetTokenAt(new TextPosition(line, characterIndex),
			out lineIndex, out tokenIndex, out atTokenEnd);
		return token;
	}
	
	private string HelpURL()
	{
	//	var result = _HelpURL();
	//	Debug.Log(result);
	//	return result;
	//}
	
	//private string _HelpURL()
	//{
		var token = GetTokenAtCursor();

		if ((token.tokenKind == SyntaxToken.Kind.Identifier ||
			token.tokenKind == SyntaxToken.Kind.Keyword && textBuffer.Parser.IsBuiltInType(token.text)) &&
			token.parent != null && token.parent.resolvedSymbol == null)
				FGResolver.ResolveNode(token.parent.parent);

		string id = null;
		string keyword = null;
		string proposal = null;
		string builtinType = null;
		if (token.tokenKind == SyntaxToken.Kind.Keyword || token.tokenKind == SyntaxToken.Kind.ContextualKeyword)
		{
			switch (token.text)
			{
				case "abstract": id = "sf985hc5"; break;
				case "as": id = "cscsdfbt"; break;
				case "base": id = "hfw7t1ce"; break;

				case "break": id = "adbctzc4"; break;
				case "switch": case "case": id = "06tc147t"; break;
				case "try": case "catch": id = "0yd65esw"; break;

				case "checked": keyword = "checked"; break;
				case "class": id = "0b0thckt"; break;
				case "const": id = "e6w8fe1b"; break;

				case "continue": id = "923ahwt1"; break;
				case "default": id = token.parent.parent.RuleName == "defaultValueExpression" ? "xwth0h0d" : "06tc147t"; break;
				case "delegate": builtinType = "reference-types#the-delegate-type"; break;

				case "do": id = "370s1zax"; break;
				case "if": case "else": id = "5011f09h"; break;
				case "enum": id = "sbbt4032"; break;

				case "event": id = "8627sbea"; break;
				case "explicit": id = "xhbhezf4"; break;
				case "extern": id = "e59b22c5"; break;

				case "finally": id = "zwc8s4fz"; break;
				case "fixed": keyword = "fixed-statement"; break;
				case "for": id = "ch45axte"; break;

				case "foreach": case "in": id = "ttw7t8t6"; break;
				case "goto": id = "13940fs2"; break;
				case "implicit": id = "z5z9kes2"; break;

				case "interface": id = "87d83y5b"; break;
				case "internal": id = "7c5ka91b"; break;
				
				case "is": id = "scekt9xw"; break;
				case "lock": id = "c5kehkcz"; break;
				case "namespace": id = "z2kcy19k"; break;
				
				case "new": id = "51y09td4"; break; // TODO: Make this more awesome!
				case "operator": id = "s53ehcz3"; break;
				
				case "out": id = "ee332485"; break;
				case "override": id = "ebca9ah3"; break;
				case "params": id = "w5zay9db"; break;
				case "private": id = "st6sy9xe"; break;
				
				case "protected": id = "bcd5672a"; break;
				case "public": id = "yzh058ae"; break;
				case "readonly": id = "acdd6hb7"; break;
				case "ref":
					if (token.parent.parent == null)
					{
						keyword = "ref";
						break;
					}
					//Debug.Log(token.parent.parent.RuleName);
					switch (token.parent.parent.RuleName)
					{
					case "namespaceMemberDeclaration":
						builtinType = "struct#ref-struct";
						break;
					case "conditionalExpression":
						proposal = "csharp-7.2/conditional-ref";
						break;
					case "classMemberDeclaration":
					case "structMemberDeclaration":
						{
							var nextSibling = token.parent.nextSibling as ParseTree.Node;
							if (nextSibling == null)
								goto case "methodBody";
							if (nextSibling.RuleName == "PARTIAL")
								nextSibling = nextSibling.nextSibling as ParseTree.Node;
							if (nextSibling == null)
								goto case "methodBody";
							if (nextSibling.RuleName == "structDeclaration")
								goto case "namespaceMemberDeclaration";
							else
								goto case "methodBody";
						}
					case "statement":
					case "localVariableDeclarator":
					case "assignment":
						keyword = "ref#ref-locals";
						break;
					case "returnStatement":
					case "readonlyAccessorDeclation":
					case "accessorBody":
					case "methodBody":
					case "lambdaExpressionBody":
						keyword = "ref#reference-return-values";
						break;
					default:
						keyword = "ref#passing-an-argument-by-reference";
						break;
					}
					break;
				
				case "return": id = "1h3swy84"; break;
				case "sealed": id = "88c54tsw"; break;
				
				case "sizeof": id = "eahchzkf"; break;
				case "stackalloc": id = "cx9s2sy4"; break;
				case "static": id = "98f28cdx"; break;
				
				case "struct": id = "ah19swz4"; break;
				case "this": id = "dk1507sz"; break;
				case "throw": id = "1ah5wsex"; break;

				case "typeof": id = "58918ffs"; break;
				
				case "unchecked": keyword = "unchecked"; break;
				case "unsafe": keyword = "unsafe"; break;
				case "using": id = token.parent.parent.RuleName == "usingStatement" ? "yh598w02" : "sf0df423"; break;
				case "virtual": id = "9fkccyh4"; break;

				case "volatile": id = "x13ttww7"; break;
				case "while": id = "2aeyhxcd"; break;
				
				case "add": keyword = "add"; break;
				case "alias": keyword = "extern-alias"; break;
				case "ascending": keyword = "ascending"; break;
				
				case "async": keyword = "async"; break;
				case "await": keyword = "await"; break;
				case "by": keyword = "by"; break;
				
				case "descending": keyword = "descending"; break;
				case "equals": keyword = "equals"; break;
				
				case "from": keyword = "from-clause"; break;
				case "get": keyword = "get"; break;
				case "global": keyword = "global"; break;
				
				case "group": keyword = "group-clause"; break;
				case "into": keyword = "into"; break;
				case "join": keyword = "join-clause"; break;
				
				case "let": keyword = "let-clause"; break;
				case "nameof": keyword = "nameof"; break;
				case "on": keyword = "on"; break;
				
				case "orderby": keyword = "orderby-clause"; break;
				case "partial": keyword = token.parent.parent.RuleName == "namespaceMemberDeclaration" ? "partial-type" : "partial-method"; break;
				
				case "remove": keyword = "remove"; break;
				case "select": keyword = "select-clause"; break;
				case "set": keyword = "set"; break;
				
				case "value": keyword = "value"; break;
				case "var": keyword = "var"; break;
				case "when": keyword = token.parent.parent.parent.RuleName == "specificCatchClause"
					? "when#when-in-a-catch-statement" : "when#when-in-a-switch-statement"; break;
				
				case "where": keyword = token.parent.parent.parent.RuleName == "typeParameterConstraintsClause" ? "where-generic-type-constraint" : "where-clause"; break;
				case "yield": keyword = "yield"; break;
			}
		}

		if (id != null)
		{
			return "http://msdn.microsoft.com/library/" + id;
		}
		else if (keyword != null)
		{
			return "http://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/" + keyword;
		}
		else if (proposal != null)
		{
			return "http://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/" + proposal;
		}
		else if (builtinType != null)
		{
			return "http://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/" + builtinType;
		}
		else if (token.parent != null && token.parent.resolvedSymbol != null)
		{
			var symbol = token.parent.resolvedSymbol;
			if (symbol.kind == SymbolKind.Error)
			{
				//if (SISettings.useLocalUnityDocumentation || Application.internetReachability == NetworkReachability.NotReachable)
				//	return "file:///unity/ScriptReference/30_search.html?q=" + token.text;
				//else
					return "http://docs.unity3d.com/ScriptReference/30_search.html?q=" + token.text;
			}
			
			var assembly = symbol.Assembly;

			if (assembly == null && symbol.declarations != null)
				assembly = ((CompilationUnitScope) textBuffer.Parser.parseTree.root.scope).assembly;

			if (assembly != null)
			{
				var assemblyName = assembly.AssemblyName;
				if (assemblyName == "unityengine" || assemblyName == "unityeditor" ||
					assemblyName.FastStartsWith("unityengine.") ||
					assemblyName.FastStartsWith("unityeditor."))
				{
					var localHelpURL = Help.GetHelpURLForObject(OwnerWindow);
					var isLocalDocumentationAvailable = !string.IsNullOrEmpty(localHelpURL) && localHelpURL[0] == 'f';
					var useLocalDocumentation = isLocalDocumentationAvailable
						&& (SISettings.useLocalUnityDocumentation || Application.internetReachability == NetworkReachability.NotReachable);
					if (useLocalDocumentation)
						return "file:///unity/ScriptReference/" + symbol.UnityHelpName;
					else
						return "http://docs.unity3d.com/ScriptReference/" + symbol.UnityHelpName + ".html";
				}
				else if (assemblyName == "mscorlib" || assemblyName == "system" || assemblyName.FastStartsWith("system."))
				{
					var nonEnumMember = symbol;
					if (nonEnumMember.kind == SymbolKind.EnumMember)
						nonEnumMember = nonEnumMember.parentSymbol;
					//if (nonEnumMember.FullName == nonEnumMember.FullReflectionName)
					//{
					//	//return "https://docs.microsoft.com/en-us/dotnet/api/" + nonEnumMember.FullName + "?view=net-5.0";
					//	var urlName = nonEnumMember.FullName;
					//	if (urlName.EndsWith(".ctor"))
					//		urlName = urlName.Substring(0, urlName.Length - 5) + "%23ctor";
					//	return "http://msdn.microsoft.com/library/" + urlName + "(v=vs.90)";
					//}
					//else
					{
						var urlName = nonEnumMember.GetGenericSymbol().FullReflectionName;
						if (urlName.EndsWith(".ctor"))
							urlName = urlName.Substring(0, urlName.Length - 5) + "%23ctor";
						return
							"http://msdn.microsoft.com/query/dev12.query?appId=Dev12IDEF1&l=EN-US&k=k(" +
							urlName +
							");k(TargetFrameworkMoniker-.NETFramework,Version%3Dv4.7);k(DevLang-csharp)";
					}
				}
			}
		}
		
		return null;
	}
	
	private AssemblyDefinition GetSymbolAssembly(SyntaxToken token)
	{
		if (token.tokenKind == SyntaxToken.Kind.Identifier && token.parent != null && token.parent.resolvedSymbol == null)
			FGResolver.ResolveNode(token.parent.parent);
		
		if (token.parent == null || token.parent.resolvedSymbol == null)
			return null;
		
		var symbol = token.parent.resolvedSymbol;
		var assembly = symbol.Assembly;
		
		if (assembly == null && symbol.declarations != null)
			assembly = ((CompilationUnitScope) textBuffer.Parser.parseTree.root.scope).assembly;
		
		return assembly;
	}
	
	private void ExecuteStaticMethod()
	{
		var token = GetTokenAtCursor();
		if (token == null || token.parent == null)
			return;
				
		var symbol = token.parent.resolvedSymbol;
		if (symbol == null || !symbol.IsStatic || symbol.kind != SymbolKind.Method || symbol.GetParameters().Count != 0)
			return;
		
		if (symbol.GetParameters() != null && symbol.GetParameters().Count != 0)
			return;
		
		var runtimeType = symbol.GetRuntimeType();
		if (runtimeType == null)
			return;
		
		var methodInfo = runtimeType.GetMethod(
			symbol.name,
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
			null,
			Type.EmptyTypes,
			null);
		
		if (methodInfo == null)
			return;
		
		if (methodInfo.GetParameters().Length != 0)
			return;
		
		var constructedSymbol = symbol as ConstructedMethodDefinition;
		if (constructedSymbol != null)
		{
			var typeArguments = constructedSymbol.typeArguments;
			if (typeArguments != null && typeArguments.Length > 0)
			{
				var runtimeTypeArguments = new System.Type[typeArguments.Length];
				for (int i = 0; i < typeArguments.Length; ++i)
				{
					var typeArgument = typeArguments[i].definition;
					if (typeArgument == null || typeArgument.kind == SymbolKind.Error)
						return;
					runtimeTypeArguments[i] = typeArgument.GetRuntimeType();
				}
				methodInfo = methodInfo.MakeGenericMethod(runtimeTypeArguments);
			}
		}
		
		if (!methodInfo.ContainsGenericParameters)
		{
			methodInfo.Invoke(null, null);
		}
	}
	
	private void GoToDefinition()
	{
		var declarations = GetSymbolDeclarations();
		if (declarations == null || declarations.Count == 0)
			return;
		
		if (declarations.Count == 1)
		{
			GoToSymbolDeclaration(declarations[0]);
		}
		else
		{
			var menu = new GenericMenu();
			foreach (var decl in declarations)
			{
				var co = decl.scope;
				while (co.parentScope != null)
					co = co.parentScope;
				var fileName = ((CompilationUnitScope) co).path;
				fileName = AssetDatabase.AssetPathToGUID(fileName);
				fileName = AssetDatabase.GUIDToAssetPath(fileName);
				fileName = Path.GetFileName(fileName);
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
				fileName = fileName.Replace('_', '\xFF3F');
#endif
				var nameNode = decl.NameNode();
				var firstLeaf = nameNode as ParseTree.Leaf ?? (nameNode as ParseTree.Node).GetFirstLeaf();
				var signature = "";
				if (decl.kind == SymbolKind.Method)
				{
					var definition = decl.definition;
					signature = decl.Name + " (" + definition.PrintParameters(definition.GetParameters(), true) + ")";
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
					signature = signature.Replace('_', '\xFF3F');
					menu.AddItem(
						new GUIContent(signature + " _"),
#else
					menu.AddItem(
						new GUIContent(signature),
#endif
						false,
						d => GoToSymbolDeclaration((SymbolDeclaration) d),
						decl);
				}
				else
				{
					menu.AddItem(new GUIContent(fileName + " : " + (firstLeaf.token.Line + 1)),
						false, d => GoToSymbolDeclaration((SymbolDeclaration) d), decl);
				}
			}
			
			var token = GetTokenAtCursor();
			var rcToken = GetTokenRect(token);
			rcToken.x += scrollViewRect.x - scrollPosition.x;
			rcToken.y += 4f + scrollViewRect.y - scrollPosition.y;
			var ssTopLeft = GUIUtility.ScreenToGUIPoint(new Vector2(rcToken.x, rcToken.y));
			rcToken.x += ssTopLeft.x - rcToken.x;
			rcToken.y += ssTopLeft.y - rcToken.y;
			menu.DropDown(rcToken);
		}
	}
	
	private List<SymbolDeclaration> GetSymbolDeclarations()
	{
		var token = GetTokenAtCursor();
		if (token == null)
			return null;
		
		var assembly = GetSymbolAssembly(token);
		if (assembly == null)
			return null;
		
		var symbol = token.parent.resolvedSymbol;
		if (symbol != null)
			symbol = symbol.GetGenericSymbol();

		var assemblyName = assembly.AssemblyName;
		if (assemblyName == "mscorlib" || assemblyName == "system" || assemblyName.FastStartsWith("system."))
		{
			if (symbol.kind != SymbolKind.Namespace)
			{
				var input = symbol.XmlDocsName;
				if(input != null)
				{
					const int digits = 16;
					var md5 = System.Security.Cryptography.MD5.Create();
					var bytes = Encoding.UTF8.GetBytes(input);
					var hashBytes = md5.ComputeHash(bytes);
	
					var c = new char[digits];
					byte b;
					for (var i = 0; i < digits / 2; ++i)
					{
						b = ((byte) (hashBytes[i] >> 4));
						c[i * 2] = (char) (b > 9 ? b + 87 : b + 0x30);
						b = ((byte) (hashBytes[i] & 0xF));
						c[i * 2 + 1] = (char) (b > 9 ? b + 87 : b + 0x30);
					}
	
					Help.BrowseURL("http://referencesource.microsoft.com/mscorlib/a.html#" + new string(c));
				
					return null;
				}
			}
		}
		
		if (!assembly.fromCsScripts)
		{
			return null;
		}
		
		var declarations = symbol.declarations;
		if (declarations == null)
		{
			declarations = FGFindInFiles.FindDeclarations(symbol);
			if (declarations == null)
			{
				token.parent.resolvedSymbol = null;
				FGResolver.ResolveNode(token.parent.parent);
				symbol = token.parent.resolvedSymbol;
				if (symbol != null)
				{
					symbol = symbol.GetGenericSymbol();
					declarations = symbol.declarations;
					
					if (symbol.kind == SymbolKind.Constructor && declarations == null)
					{
						symbol = symbol.TypeOf();
						if (symbol != null)
						{
							symbol = symbol.GetGenericSymbol();
							declarations = symbol.declarations;
						}
					}
				}
			}
		}

		var validDeclarations = new List<SymbolDeclaration>();
		
		if (symbol.kind == SymbolKind.MethodGroup)
		{
			var asMethodGroup = symbol as MethodGroupDefinition;
			if (asMethodGroup == null)
			{
				var asConstructedSymbolReference = symbol as ConstructedSymbolReference;
				if (asConstructedSymbolReference != null)
					asMethodGroup = asConstructedSymbolReference.referencedSymbol as MethodGroupDefinition;
			}
			foreach (var method in asMethodGroup.methods)
			{
				var methodDeclaration = method.declarations;
				while (methodDeclaration != null)
				{
					var next = methodDeclaration.next;
					if (IsValidSymbolDeclaration(methodDeclaration))
						validDeclarations.Add(methodDeclaration);
					methodDeclaration = next;
				}
			}
			
			declarations = null;
		}

		if (declarations != null)
		{
			for (var decl = declarations; decl != null; )
			{
				var next = decl.next;
				if (IsValidSymbolDeclaration(decl))
					validDeclarations.Add(decl);
				decl = next;
			}
		}
		
		return validDeclarations != null && validDeclarations.Count > 0 ? validDeclarations : null;
	}
	
	static bool ignoreNextKeyDownEvent;
	
	private void ProcessEditorKeyboard(Event current, bool acceptingAutoComplete)
	{
		if (ignoreNextKeyDownEvent && current.type == EventType.KeyDown)
		{
			ignoreNextKeyDownEvent = false;
			current.Use();
			return;
		}
		
		lastTypedText = null;
		if (!CanEdit())
			return;
		
		var lineHeight = LineHeight;
		
		if (current.type == EventType.KeyDown &&
			current.keyCode != KeyCode.LeftControl && current.keyCode != KeyCode.RightControl &&
			current.keyCode != KeyCode.LeftCommand && current.keyCode != KeyCode.RightCommand &&
			current.keyCode != KeyCode.LeftShift && current.keyCode != KeyCode.RightShift)
		{
			pingTimer = 0f;
		}
		
		bool addRecentLocationIfUsed = true;
		
		bool isOSX = Application.platform == RuntimePlatform.OSXEditor;
		bool isLinux = Application.platform == RuntimePlatform.LinuxEditor;
		EventModifiers modifiers = current.modifiers & ~(EventModifiers.CapsLock | EventModifiers.Numeric | EventModifiers.FunctionKey);
		bool isActionKey = 0 != (current.modifiers & (isOSX ? EventModifiers.Command : EventModifiers.Control));

		if (current.character == '\n' && !current.alt && SISettings.swapShiftAndCtrlEnter)
		{
			if (!isOSX && modifiers == EventModifiers.Control)
			{
				modifiers = EventModifiers.Shift;
				current.modifiers = (current.modifiers & ~EventModifiers.Control) | EventModifiers.Shift;
				isActionKey = false;
			}
			else if (isOSX && modifiers == EventModifiers.Command)
			{
				modifiers = EventModifiers.Shift;
				current.modifiers = (current.modifiers & ~EventModifiers.Command) | EventModifiers.Shift;
				isActionKey = false;
			}
			else if (!isOSX && modifiers == EventModifiers.Shift)
			{
				modifiers = EventModifiers.Control;
				current.modifiers = (current.modifiers & ~EventModifiers.Shift) | EventModifiers.Control;
				isActionKey = true;
			}
			else if (isOSX && modifiers == EventModifiers.Shift)
			{
				modifiers = EventModifiers.Command;
				current.modifiers = (current.modifiers & ~EventModifiers.Shift) | EventModifiers.Command;
				isActionKey = true;
			}
		}
		
		int reindentLinesFrom = -1;
		int reindentLinesTo = -1;

		if (isLinux && current.type == EventType.KeyDown)
		{
			if (current.keyCode == KeyCode.Z)
			{
				if (modifiers == EventModifiers.Control ||
					modifiers == EventModifiers.Command ||
					modifiers == (EventModifiers.Command | EventModifiers.Alt))
				{
					Undo();
					current.Use();
					tryArgumentsHint = true;
					return;
				}
				else if (modifiers == (EventModifiers.Control | EventModifiers.Shift) ||
					modifiers == (EventModifiers.Command | EventModifiers.Shift) ||
					modifiers == (EventModifiers.Command | EventModifiers.Alt | EventModifiers.Shift))
				{
					Redo();
					current.Use();
					tryArgumentsHint = true;
					return;
				}
			}
			else if (current.keyCode == KeyCode.Y)
			{
				if (modifiers == EventModifiers.Control ||
					modifiers == EventModifiers.Command ||
					modifiers == (EventModifiers.Command | EventModifiers.Alt))
				{
					Redo();
					current.Use();
					tryArgumentsHint = true;
					return;
				}
			}
			else if (current.keyCode == KeyCode.S && (modifiers == EventModifiers.Control || modifiers == EventModifiers.Command))
			{
				current.Use();
				SaveBuffer();
				return;
			}
		}

		if (isOSX && current.type == EventType.KeyDown)
		{
			if ((current.modifiers & ~(EventModifiers.Numeric | EventModifiers.CapsLock)) == EventModifiers.FunctionKey)
			{
				if (current.keyCode == KeyCode.Home)
				{
					current.Use();
					FoldAtCaret();
					return;
				}
				else if (current.keyCode == KeyCode.End)
				{
					current.Use();
					UnfoldAtCaret();
					return;
				}
			}
			
			if (current.keyCode == KeyCode.Z)
			{
				if (modifiers == EventModifiers.Control ||
					modifiers == EventModifiers.Command ||
					modifiers == (EventModifiers.Command | EventModifiers.Alt))
				{
					Undo();
					current.Use();
					tryArgumentsHint = true;
					return;
				}
				else if (modifiers == (EventModifiers.Control | EventModifiers.Shift) ||
					modifiers == (EventModifiers.Command | EventModifiers.Shift) ||
					modifiers == (EventModifiers.Command | EventModifiers.Alt | EventModifiers.Shift))
				{
					Redo();
					current.Use();
					tryArgumentsHint = true;
					return;
				}
			}
			else if (current.keyCode == KeyCode.A && modifiers == EventModifiers.Control
				|| current.keyCode == KeyCode.LeftArrow && isActionKey && (modifiers & EventModifiers.Alt) == 0)
			{
				current.Use();
				var simKey = Event.KeyboardEvent("home");
				simKey.modifiers = modifiers & (EventModifiers.Shift);
				ProcessEditorKeyboard(simKey, false);
				return;
			}
			else if (current.keyCode == KeyCode.E && modifiers == EventModifiers.Control
				|| current.keyCode == KeyCode.RightArrow && isActionKey && (modifiers & EventModifiers.Alt) == 0)
			{
				current.Use();
				var simKey = Event.KeyboardEvent("end");
				simKey.modifiers = modifiers & (EventModifiers.Shift);
				ProcessEditorKeyboard(simKey, false);
				return;
			}
			else if (current.keyCode == KeyCode.S && (modifiers == EventModifiers.Control || modifiers == EventModifiers.Command))
			{
				current.Use();
				SaveBuffer();
				return;
			}
			else if (current.keyCode == KeyCode.Y && modifiers == (EventModifiers.Shift | EventModifiers.Command))
			{
				current.Use();
				CommandFindAllReferences();
				return;
			}
			else if (current.keyCode == KeyCode.G && modifiers == EventModifiers.Control)
			{
				current.Use();
				EditorApplication.delayCall += () => GoToLineWindow.Create(this);
				return;
			}
			else if (current.keyCode == KeyCode.DownArrow && modifiers == (EventModifiers.Command | EventModifiers.Alt))
			{
				CommandDuplicateLinesDown();
				current.Use();
				tryArgumentsHint = true;
				return;
			}
		}
		
		if (current.type == EventType.KeyDown && !current.shift && !current.alt
			&& (isOSX ? !current.control : !current.command))
		{
			if (isActionKey && current.keyCode == KeyCode.Quote ||
				!isOSX && !current.control && current.keyCode == KeyCode.F1)
			{
				var helpUrl = HelpURL();
				if (helpUrl != null)
				{
					current.Use();
					
					if (helpUrl.StartsWithIgnoreCase("file:///unity/scriptreference/"))
						Help.ShowHelpPage(helpUrl);
					else
						Help.BrowseURL(helpUrl);
				}
			}
			else if (isOSX && current.command && current.keyCode == KeyCode.Y
				|| !current.control && !current.command && current.keyCode == KeyCode.F12)
			{
				current.Use();
				if (nextF12GoesBack && CanGoBack())
				{
					nextF12GoesBack = false;
					GoToRecentLocation(false);
					return;
				}
				else
				{
					addRecentLocationIfUsed = false;
					
					var prevLine = caretPosition.line;
					GoToDefinition();
					if (prevLine != caretPosition.line)
						EditorApplication.delayCall += () => { nextF12GoesBack = true; };
				}
			}
			else if (isOSX
				? current.command && current.keyCode == KeyCode.L
				: current.control && current.keyCode == KeyCode.G)
			{
				current.Use();
				EditorApplication.delayCall += () => GoToLineWindow.Create(this);
				return;
			}
			else if (isOSX
				? current.command && current.keyCode == KeyCode.E
				: current.control && current.keyCode == KeyCode.E)
			{
				current.Use();
				ExecuteStaticMethod();
				return;
			}
		}

		if (isActionKey && current.type == EventType.KeyDown)
		{
			EventModifiers mods = modifiers & ~(EventModifiers.Control | EventModifiers.Command);

			if (current.keyCode == KeyCode.Space && mods == 0)
			{
				current.Use();
				Autocomplete(false);
				return;
			}
			else if (current.keyCode == KeyCode.T && mods == 0)
			{
				current.Use();
				EditorApplication.delayCall += OpenInNewTab;
				return;
			}
			else if ((current.keyCode == KeyCode.K || current.keyCode == KeyCode.Slash) && mods == 0)
			{
				current.Use();
				ToggleCommentSelection();
				return;
			}
			else if (!isOSX && current.keyCode == KeyCode.Z && mods == EventModifiers.Shift)
			{
				current.Use();
				Undo();
				tryArgumentsHint = true;
				return;
			}
			else if (!isOSX && current.keyCode == KeyCode.Y && mods == EventModifiers.Shift)
			{
				current.Use();
				Redo();
				tryArgumentsHint = true;
				return;
			}
			else if (current.keyCode == KeyCode.S && mods == EventModifiers.Alt)
			{
				current.Use();
				SaveBuffer();
				return;
			}
			else if (current.keyCode == KeyCode.F && (mods & ~EventModifiers.Alt) == EventModifiers.Shift)
			{
				current.Use();
				FindReplaceWindow.ShowFindInFilesWindow();
				return;
			}
			else if (current.keyCode == KeyCode.H && (mods & ~EventModifiers.Alt) == EventModifiers.Shift)
			{
				current.Use();
				FindReplaceWindow.ShowReplaceInFilesWindow();
				return;
			}
			else if (current.keyCode == KeyCode.R && mods == (isOSX ? EventModifiers.Alt : EventModifiers.Shift))
			{
				current.Use();
				MenuReloadAssemblies();
				Repaint();
				return;
			}
			else if (current.keyCode == KeyCode.M && mods == 0)
			{
				ignoreNextKeyDownEvent = !isOSX;
				current.Use();
				ToggleFoldingAtCaret();
				return;
			}
			else if (current.keyCode == KeyCode.D && mods == (isOSX ? EventModifiers.Alt : EventModifiers.Shift))
			{
				current.Use();
				ToggleFoldToDefinitions();
				return;
			}
			else if (!current.alt)
			{
				if (current.keyCode == KeyCode.Minus || current.keyCode == KeyCode.KeypadMinus)
				{
					current.Use();
					ModifyFontSize(-1);
					return;
				}
				else if (current.keyCode == KeyCode.Plus || current.keyCode == KeyCode.Equals || current.keyCode == KeyCode.KeypadPlus)
				{
					current.Use();
					ModifyFontSize(1);
					return;
				}
			}
		}

		if (current.type == EventType.KeyDown)
		{
			if ((modifiers == EventModifiers.Command || modifiers == EventModifiers.Control) &&
				(current.keyCode == KeyCode.Period || current.keyCode == KeyCode.KeypadPeriod))
			{
				if (textBuffer.isCsFile)
				{
					int lineIndex, tokenIndex;
					bool atTokenEnd;
					var token = textBuffer.GetTokenAt(caretPosition, out lineIndex, out tokenIndex, out atTokenEnd);
					var tokens = textBuffer.formattedLines[lineIndex].tokens;
					if (token != null)
					{
						if (atTokenEnd &&
							token.tokenKind != SyntaxToken.Kind.Identifier &&
							token.tokenKind != SyntaxToken.Kind.ContextualKeyword &&
							token.tokenKind != SyntaxToken.Kind.Keyword)
						{
							if (tokenIndex < tokens.Count - 1)
								token = tokens[tokenIndex + 1];
						}
					}
					
					tokenIndex = -1;
					while (tokenIndex < tokens.Count)
					{
						if (token != null && token.parent != null &&
							token.parent.resolvedSymbol != null && token.parent.semanticError == "unknown symbol")
						{
							var fixes = FGResolver.GetFixes(textBuffer, token);
							if (fixes.Count > 0)
							{
								current.Use();
								var tokenMenu = new GenericMenu();
								foreach (var fix in fixes)
								{
									var captured = fix;
									tokenMenu.AddItem(
										new GUIContent(captured.GetTitle(token)),
										false,
										() => {
											BeginRefactoring(captured.GetTitle(token));
											captured.Apply(this, token);
											EndRefactoring();
										});
								}
								
								var rc = GetTokenRect(token);
								rc.x += scrollViewRect.x - scrollPosition.x;
								rc.y += 4f + scrollViewRect.y - scrollPosition.y;
								var ssTopLeft = GUIUtility.ScreenToGUIPoint(new Vector2(rc.x, rc.y));
								rc.x += ssTopLeft.x - rc.x;
								rc.y += ssTopLeft.y - rc.y;
								tokenMenu.DropDown(rc);
								
								caretMoveTime = frameTime;
								lastCaretMoveWasSearch = false;
								scrollToCaret = true;
								AddRecentLocation(2, true);
								return;
							}
						}
						
						// Check fixes for the next token;
						++tokenIndex;
						token = tokenIndex < tokens.Count ? tokens[tokenIndex] : null;
					}
				}
			}
		}
		
		if (isOSX && current.type == EventType.KeyDown && current.keyCode == KeyCode.S
			&& modifiers == (EventModifiers.Control | EventModifiers.Alt))
		{
			current.Use();
			SaveBuffer();
			return;
		}

		if (current.type == EventType.KeyDown && current.keyCode == KeyCode.F12 && modifiers == EventModifiers.Shift)
		{
			current.Use();
			CommandFindAllReferences();
			return;
		}
		
		if (!isOSX && current.type == EventType.KeyDown && modifiers == (EventModifiers.Alt | EventModifiers.Shift))
		{
			if (current.keyCode == KeyCode.LeftArrow)
			{
				current.Use();
				FoldAtCaret();
				return;
			}
			else if (current.keyCode == KeyCode.RightArrow)
			{
				current.Use();
				UnfoldAtCaret();
				return;
			}
		}
		
		bool moveByWordMode = SISettings.wordBreak_UseBothModifiers ?
			(modifiers & (EventModifiers.Control | EventModifiers.Alt)) != 0 :
			isOSX
			? (modifiers & EventModifiers.Control) != 0
			: isActionKey;
		bool stopOnSubwords = false;
		if (SISettings.wordBreak_StopOnSubwords && !SISettings.wordBreak_UseBothModifiers)
			stopOnSubwords = true;
		else if (SISettings.wordBreak_UseBothModifiers)
			stopOnSubwords =
				(modifiers & (EventModifiers.Control | EventModifiers.Alt))
				== (isOSX ? EventModifiers.Control : EventModifiers.Alt);

		int nextCharacterIndex = caretPosition.characterIndex;
		int nextCaretColumn = caretPosition.virtualColumn;
		int nextCaretLine = caretPosition.line;
		bool isHorizontal = false;
		bool clearSelection = false;

		if (current.type == EventType.KeyDown)
		{
			EditorLine editorLine;
			TextPosition caretPos;
			int tokenIndex;

			switch (current.keyCode)
			{
				case KeyCode.Escape:
					addRecentLocationIfUsed = false;
				
					if (autocompleteWindow != null)
					{
						CloseAutocomplete();
						current.Use();
					}
					else if (argumentsHint != null)
					{
						argumentsHint.Hide();
						argumentsHint = null;
						//showingArgumentsForMethod = null;
						current.Use();
					}
					else if (!string.IsNullOrEmpty(searchString) && searchResultAge == textBuffer.undoPosition)
					{
						searchResultAge = -1;
						atLastSearchResult = false;
						current.Use();
					}
					else if (selectionStartPosition != null)
					{
						clearSelection = true;
						current.Use();
					}
					else if (SISettings.openAutoCompleteOnEscape)
					{
						current.Use();
						Autocomplete(false);
						return;
					}
					else if (highlightedSymbol != null)
					{
						highlightedSymbol = null;
						current.Use();
						return;
					}
					break;

				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					if (isActionKey && EditorWindow.focusedWindow)
					{
#if UNITY_EDITOR_OSX && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3 && !UNITY_4_5 && !UNITY_4_6 && !UNITY_4_7
						current.Use();
						//OpenAtCursor();
						EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("OpenAtCursor"));
						GUIUtility.ExitGUI();
#endif
						return;
					}
					break;

				case KeyCode.PageUp:
					if (isActionKey)
					{
						isHorizontal = true;
						
						nextCaretLine = 0;
						nextCharacterIndex = 0;
					}
					else
					{
						FGTextBuffer.CaretPos nextCaretPos = GetLinesOffset(caretPosition, -(int) (codeViewRect.height / lineHeight));
						nextCaretLine = nextCaretPos.line;
						nextCaretColumn = nextCaretPos.virtualColumn;
						nextCharacterIndex = nextCaretPos.characterIndex;
						
						scrollPositionLine = GetLinesOffset(new FGTextBuffer.CaretPos { line = scrollPositionLine }, -(int) (codeViewRect.height / lineHeight)).line;
						scrollInstantly = true;
						
						addRecentLocationIfUsed = false;
					}
					current.Use();
					break;

				case KeyCode.PageDown:
					if (isActionKey)
					{
						isHorizontal = true;
						
						nextCaretLine = textBuffer.lines.Count - 1;
						nextCharacterIndex = textBuffer.lines[nextCaretLine].Length;
					}
					else
					{
						FGTextBuffer.CaretPos nextCaretPos = GetLinesOffset(caretPosition, (int) (codeViewRect.height / lineHeight));
						nextCaretLine = nextCaretPos.line;
						nextCaretColumn = nextCaretPos.virtualColumn;
						nextCharacterIndex = nextCaretPos.characterIndex;
						
						scrollPositionLine = GetLinesOffset(new FGTextBuffer.CaretPos { line = scrollPositionLine }, (int) (codeViewRect.height / lineHeight)).line;
						scrollInstantly = true;
						
						addRecentLocationIfUsed = false;
					}
					current.Use();
					break;

				case KeyCode.Home:
					if (isActionKey)
					{
						isHorizontal = true;
						
						nextCaretLine = 0;
						nextCharacterIndex = 0;
					}
					else
					{
						isHorizontal = true;

						nextCaretLine = BufferToEditorLine(nextCaretLine);
						editorLine = GetEditorLine(nextCaretLine);

						var firstNonWhitespace = 0;
						var numTokens = editorLine.tokens.Count;
						for (var i = 0; i < numTokens; ++i)
						{
							var token = editorLine.tokens[i];
							if (token.tokenKind > SyntaxToken.Kind.LastWSToken)
								break;
							
							var tokenLength = token.text.length;
							for (var j = 0; j < tokenLength; ++j)
							{
								var c = token.text[j];
								if (c == ' ' || c == '\t')
								{
									++firstNonWhitespace;
								}
								else
								{
									i = numTokens;
									break;
								}
							}
						}
						
						//if (firstNonWhitespace == textBuffer.lines[nextCaretLine].Length)
						//	firstNonWhitespace = 0;
						if (nextCharacterIndex == firstNonWhitespace)
							nextCharacterIndex = 0;
						else
							nextCharacterIndex = firstNonWhitespace;
						
						addRecentLocationIfUsed = false;
					}
					current.Use();
					break;

				case KeyCode.End:
					if (isActionKey)
					{
						isHorizontal = true;
						
						nextCaretLine = textBuffer.lines.Count - 1;
						nextCharacterIndex = textBuffer.lines[nextCaretLine].Length;
					}
					else
					{
						isHorizontal = true;

						if (!current.shift && selectionStartPosition != null)
						{
							if (selectionStartPosition.line > nextCaretLine)
								nextCaretLine = selectionStartPosition.line;
						}
						
						nextCaretLine = BufferToEditorLine(nextCaretLine);
						editorLine = GetEditorLine(nextCaretLine);
						nextCaretLine = editorLine.lineTo;
						
						nextCharacterIndex = textBuffer.lines[nextCaretLine].Length;
						
						addRecentLocationIfUsed = false;
					}
					current.Use();
					break;

				case KeyCode.UpArrow:
					if (isOSX && modifiers == EventModifiers.Command)
					{
						isHorizontal = true;
						
						nextCaretLine = 0;
						nextCharacterIndex = 0;
					}
					else if (modifiers == EventModifiers.Alt)
					{
						var moveLinesFrom = caretPosition.line;
						var moveLinesTo = selectionStartPosition != null ? selectionStartPosition.line : caretPosition.line;
						if (moveLinesTo < moveLinesFrom)
						{
							var temp = moveLinesTo;
							moveLinesTo = moveLinesFrom;
							moveLinesFrom = temp;
						}
						
						if (selectionStartPosition != null)
						{
							if (selectionStartPosition > caretPosition && selectionStartPosition.characterIndex == 0)
								--moveLinesTo;
							else if (selectionStartPosition < caretPosition && caretPosition.characterIndex == 0)
								--moveLinesTo;
						}
						
						moveLinesFrom = BufferToEditorLine(moveLinesFrom);
						editorLine = GetEditorLine(moveLinesTo);
						moveLinesTo = editorLine.lineTo;
						
						if (moveLinesFrom > 0)
						{
							var linesAboveTo = moveLinesFrom - 1;
							var linesAboveFrom = BufferToEditorLine(linesAboveTo);
							
							var deltaLines = moveLinesFrom - linesAboveFrom;
							
							var textLines = "\n" + textBuffer.lines[linesAboveFrom];
							for (var i = linesAboveFrom + 1; i <= linesAboveTo; ++i)
								textLines += "\n" + textBuffer.lines[i];
							
							FindCollapsibleTextSpansAtLines(linesAboveFrom, linesAboveTo);
							for (var i = tempCollapsibleTextSpans.Count; i --> 0; )
							{
								var span = tempCollapsibleTextSpans[i];
								var startLine = span.span.Start.line;
								var endLine = span.span.End.line;
								if (startLine < linesAboveFrom && endLine > linesAboveTo)
									tempCollapsibleTextSpans.RemoveAt(i);
								else
									textBuffer.RemoveCollapsibleTextSpan(span);
							}
							
							//GetLineTokens(linesAboveFrom, tempCollapsedTokenSpans);
							//
							//tempTextIntervals.Clear();
							//for (var i = 0; i < tempCollapsedTokenSpans.Count; ++i)
							//{
							//	var ct = tempCollapsedTokenSpans[i];
							//	if (ct != null)
							//	{
							//		var span = ct.span;
							//		tempTextIntervals.Add(span);
							//	}
							//}

							var toPos = new FGTextBuffer.CaretPos {
								line = moveLinesFrom,
								characterIndex = 0,
								column = 0,
								virtualColumn = 0
							};
							var fromPos = toPos.Clone();
							fromPos.line = linesAboveFrom;
							textBuffer.DeleteText(fromPos, toPos);
							
							toPos = new FGTextBuffer.CaretPos {
								line = moveLinesTo - deltaLines,
								characterIndex = textBuffer.lines[moveLinesTo - deltaLines].Length
							};
							toPos.column = toPos.virtualColumn = CharIndexToColumn(toPos.characterIndex, toPos.line);
							textBuffer.InsertText(toPos, textLines);

							nextCaretLine = caretPosition.line - deltaLines;
							nextCaretColumn = caretPosition.column;
							nextCharacterIndex = caretPosition.characterIndex;
							if (selectionStartPosition != null)
							{
								modifiers |= EventModifiers.Shift;
								selectionStartPosition = selectionStartPosition.Clone();
								selectionStartPosition.line -= deltaLines;
							}
							
							var numLinesMoved = moveLinesTo - moveLinesFrom + 1;
							//for (var i = 0; i < tempTextIntervals.Count; ++i)
							//{
							//	var span = tempTextIntervals[i];
							//	var start = span.Start;
							//	var end = span.End;
							//	start.line += numLinesMoved;
							//	end.line += numLinesMoved;
							//	FoldText(start, end);
							//}
							
							for (var i = tempCollapsibleTextSpans.Count; i --> 0; )
							{
								var span = tempCollapsibleTextSpans[i];
								if (span.span.Start.line >= linesAboveFrom)
									span.span.Start.line += numLinesMoved;
								if (span.span.End.line <= linesAboveTo)
									span.span.End.line += numLinesMoved;
								if (span.span.Start < span.span.End)
								{
									var addedSpan = textBuffer.AddCollapsibleTextSpan(span);
									if (addedSpan != span)
									{
										Debug.LogWarning("span not added - already exists");
									}
								}
								else
								{
									Debug.LogWarning("span not added");
								}
							}

							textBuffer.UpdateHighlighting(moveLinesFrom - deltaLines, moveLinesTo);
							
							reindentLinesFrom = moveLinesFrom - deltaLines;
							reindentLinesTo = moveLinesTo - deltaLines;
						}
						else
						{
							return;
						}
					}
					else if (!current.control && !isActionKey)
					{
						if (!current.shift && selectionStartPosition != null && selectionStartPosition.line < nextCaretLine)
							//nextCaretLine = selectionStartPosition.line;
							caretPosition = selectionStartPosition.Clone();
						
						//--nextCaretLine;
						FGTextBuffer.CaretPos nextCaretPos = GetLinesOffset(caretPosition, -1);
						nextCaretLine = nextCaretPos.line;
						//nextCaretColumn = nextCaretPos.virtualColumn;
						nextCharacterIndex = nextCaretPos.characterIndex;
					}
					else if (!current.shift)
					{
						scrollPosition.y -= lineHeight;
						if (scrollPosition.y < 0f)
							scrollPosition.y = 0f;
						scrollPositionLine = GetLineAt(scrollPosition.y);
						scrollPositionOffset = scrollPosition.y - GetLineOffset(scrollPositionLine);
						//Repaint();
						needsRepaint = true;
						current.Use();
						return;
					}
					else
					{
						UseSelectionForSearch();
						SearchPrevious();
						current.Use();
						focusCodeView = true;
						return;
					}
					current.Use();
					addRecentLocationIfUsed = false;
					break;

				case KeyCode.DownArrow:
					if (isOSX && modifiers == EventModifiers.Command)
					{
						isHorizontal = true;
						
						nextCaretLine = textBuffer.lines.Count - 1;
						nextCharacterIndex = textBuffer.lines[nextCaretLine].Length;
					}
					else if (modifiers == EventModifiers.Alt)
					{
						var moveLinesFrom = caretPosition.line;
						var moveLinesTo = selectionStartPosition != null ? selectionStartPosition.line : caretPosition.line;
						if (moveLinesTo < moveLinesFrom)
						{
							var temp = moveLinesTo;
							moveLinesTo = moveLinesFrom;
							moveLinesFrom = temp;
						}
						
						if (selectionStartPosition != null)
						{
							if (selectionStartPosition > caretPosition && selectionStartPosition.characterIndex == 0)
								--moveLinesTo;
							else if (selectionStartPosition < caretPosition && caretPosition.characterIndex == 0)
								--moveLinesTo;
						}
						
						moveLinesFrom = BufferToEditorLine(moveLinesFrom);
						editorLine = GetEditorLine(moveLinesTo);
						moveLinesTo = editorLine.lineTo;
						
						if (moveLinesTo < textBuffer.lines.Count - 1)
						{
							var linesBelowFrom = moveLinesTo + 1;
							editorLine = GetEditorLine(linesBelowFrom);
							var linesBelowTo = editorLine.lineTo;
							
							var numSkipOverLines = linesBelowTo - moveLinesTo;
							
							var textLines = textBuffer.lines[linesBelowFrom] + "\n";
							for (var i = linesBelowFrom + 1; i <= linesBelowTo; ++i)
								textLines += textBuffer.lines[i] + "\n";
							
							FindCollapsibleTextSpansAtLines(linesBelowFrom, linesBelowTo);
							for (var i = tempCollapsibleTextSpans.Count; i --> 0; )
							{
								var span = tempCollapsibleTextSpans[i];
								var startLine = span.span.Start.line;
								var endLine = span.span.End.line;
								if (startLine < linesBelowFrom && endLine > linesBelowTo)
									tempCollapsibleTextSpans.RemoveAt(i);
								else
									textBuffer.RemoveCollapsibleTextSpan(span);
							}
							
							//GetLineTokens(linesBelowFrom, tempCollapsedTokenSpans);
							//tempTextIntervals.Clear();
							//for (var i = 0; i < tempCollapsedTokenSpans.Count; ++i)
							//{
							//	var ct = tempCollapsedTokenSpans[i];
							//	if (ct != null)
							//	{
							//		var span = ct.span;
							//		tempTextIntervals.Add(span);
							//	}
							//}
							
							var fromPos = new FGTextBuffer.CaretPos {
								line = moveLinesTo,
								characterIndex = textBuffer.lines[moveLinesTo].Length
							};
							fromPos.column = fromPos.virtualColumn = CharIndexToColumn(fromPos.characterIndex, fromPos.line);
							var toPos = new FGTextBuffer.CaretPos {
								line = linesBelowTo,
								characterIndex = textBuffer.lines[linesBelowTo].Length
							};
							toPos.column = toPos.virtualColumn = CharIndexToColumn(toPos.characterIndex, toPos.line);
							textBuffer.DeleteText(fromPos, toPos);
							
							fromPos = new FGTextBuffer.CaretPos {
								line = moveLinesFrom,
								characterIndex = 0,
								column = 0,
								virtualColumn = 0
							};
							textBuffer.InsertText(fromPos, textLines);

							nextCaretLine = caretPosition.line + numSkipOverLines;
							nextCaretColumn = caretPosition.column;
							nextCharacterIndex = caretPosition.characterIndex;
							if (selectionStartPosition != null)
							{
								modifiers |= EventModifiers.Shift;
								selectionStartPosition = selectionStartPosition.Clone();
								selectionStartPosition.line += numSkipOverLines;
							}
							
							var numLinesMoved = moveLinesTo - moveLinesFrom + 1;
							//for (var i = 0; i < tempTextIntervals.Count; ++i)
							//{
							//	var span = tempTextIntervals[i];
							//	var start = span.Start;
							//	var end = span.End;
							//	start.line -= numLinesMoved;
							//	end.line -= numLinesMoved;
							//	FoldText(start, end);
							//}
							
							for (var i = tempCollapsibleTextSpans.Count; i --> 0; )
							{
								var span = tempCollapsibleTextSpans[i];
								if (span.span.Start.line >= linesBelowFrom)
									span.span.Start.line -= numLinesMoved;
								if (span.span.End.line <= linesBelowTo)
									span.span.End.line -= numLinesMoved;
								if (span.span.Start < span.span.End)
								{
									var addedSpan = textBuffer.AddCollapsibleTextSpan(span);
									if (addedSpan != span)
									{
										Debug.LogWarning("span not added - already exists");
									}
								}
								else
								{
									Debug.LogWarning("span not added");
								}
							}
							
							textBuffer.UpdateHighlighting(moveLinesFrom, moveLinesTo + numSkipOverLines);
							
							reindentLinesFrom = moveLinesFrom + numSkipOverLines;
							reindentLinesTo = moveLinesTo + numSkipOverLines;
						}
						else
						{
							return;
						}
					}
					else if (!current.control && !isActionKey)
					{
						if (!current.shift && selectionStartPosition != null && selectionStartPosition.line > nextCaretLine)
							//nextCaretLine = selectionStartPosition.line;
							caretPosition = selectionStartPosition.Clone();
						
						//++nextCaretLine;
						FGTextBuffer.CaretPos nextCaretPos = GetLinesOffset(caretPosition, 1);
						nextCaretLine = nextCaretPos.line;
						//nextCaretColumn = nextCaretPos.virtualColumn;
						nextCharacterIndex = nextCaretPos.characterIndex;
					}
					else if (!current.shift)
					{
						scrollPosition.y += lineHeight;
						scrollPositionLine = GetLineAt(scrollPosition.y);
						scrollPositionOffset = scrollPosition.y - GetLineOffset(scrollPositionLine);
						//Repaint();
						needsRepaint = true;
						current.Use();
						return;
					}
					else
					{
						UseSelectionForSearch();
						SearchNext();
						current.Use();
						focusCodeView = true;
						return;
					}
					current.Use();
					addRecentLocationIfUsed = false;
					break;

				case KeyCode.LeftArrow:
					if (modifiers == (EventModifiers.Alt | (SISettings.wordBreak_UseBothModifiers ? (isOSX ? EventModifiers.Command : EventModifiers.Control) : 0)))
					{
						current.Use();
						if (CanGoBack())
							GoToRecentLocation(false);
						return;
					}
					
					isHorizontal = true;
					addRecentLocationIfUsed = false;

					if (!current.shift && !moveByWordMode && selectionStartPosition != null)
					{
						if (selectionStartPosition < caretPosition)
						{
							nextCaretLine = selectionStartPosition.line;
							nextCaretColumn = selectionStartPosition.column;
							nextCharacterIndex = selectionStartPosition.characterIndex;
						}
						else
						{
							clearSelection = true;
						}
						current.Use();
						break;
					}
					
					if (moveByWordMode)
					{
						FGTextBuffer.CaretPos nextPos = textBuffer.WordStopLeft(caretPosition, stopOnSubwords);
						nextCaretLine = nextPos.line;
						nextCaretColumn = nextPos.column;
						nextCharacterIndex = nextPos.characterIndex;
						
						editorLine = GetOrCreateLineInView(nextCaretLine);
						if (editorLine.collapsedTokenSpans.Count > 0)
						{
							caretPos = new TextPosition(nextCaretLine, nextCharacterIndex);
							tokenIndex = editorLine.FindTokenIndexAfter(caretPos);
							if (caretPos > editorLine.lineTokenStartPos[tokenIndex])
							{
								var collapsedTokenSpan = editorLine.collapsedTokenSpans[tokenIndex];
								if (collapsedTokenSpan != null)
								{
									var span = collapsedTokenSpan.span;
									nextCaretLine = span.Start.line;
									nextCharacterIndex = span.Start.index;
								}
							}
						}
					
						current.Use();
						break;
					}
					
					editorLine = GetOrCreateLineInView(caretPosition.line);
					caretPos = new TextPosition(caretPosition.line, caretPosition.characterIndex);
					tokenIndex = editorLine.FindTokenIndexAfter(caretPos);
					
					// Is the caret between tokens?
					if (editorLine.lineTokenStartPos[tokenIndex] == caretPos)
					{
						// Is the caret at the beginning of line?
						if (tokenIndex == 0)
						{
							if (nextCaretLine > 0)
							{
								nextCaretLine = editorLine.lineFrom - 1;
								nextCharacterIndex = textBuffer.lines[nextCaretLine].Length;
							}
							
							current.Use();
							break;
						}

						// Is the token before the caret a collapsed token?
						if (editorLine.collapsedTokenSpans.Count > 0)
						{
							var collapsedTokenSpan = editorLine.collapsedTokenSpans[tokenIndex - 1];
							if (collapsedTokenSpan != null)
							{
								// If yes, skip it by moving to its beginning.
								var span = collapsedTokenSpan.span;
								nextCaretLine = span.Start.line;
								nextCharacterIndex = span.Start.index;
							
								current.Use();
								break;
							}
						}
					}

					// Not a special case - just move left by one charater.
					--nextCharacterIndex;
					current.Use();
					break;

				case KeyCode.RightArrow:
					if (modifiers == (EventModifiers.Alt | (SISettings.wordBreak_UseBothModifiers ? (isOSX ? EventModifiers.Command : EventModifiers.Control) : 0)))
					{
						current.Use();
						if (CanGoForward())
							GoToRecentLocation(true);
						return;
					}
					
					isHorizontal = true;
					addRecentLocationIfUsed = false;

					if (!current.shift && !moveByWordMode && selectionStartPosition != null)
					{
						if (selectionStartPosition > caretPosition)
						{
							nextCaretLine = selectionStartPosition.line;
							nextCaretColumn = selectionStartPosition.column;
							nextCharacterIndex = selectionStartPosition.characterIndex;
						}
						else
						{
							clearSelection = true;
						}
						current.Use();
						break;
					}

					if (nextCaretLine < 0)
					{
						current.Use();
						break;
					}
					
					if (moveByWordMode)
					{
						FGTextBuffer.CaretPos nextPos = textBuffer.WordStopRight(caretPosition, stopOnSubwords);
						nextCaretLine = nextPos.line;
						nextCaretColumn = nextPos.column;
						nextCharacterIndex = nextPos.characterIndex;
						
						editorLine = GetOrCreateLineInView(nextCaretLine);
						if (editorLine.collapsedTokenSpans.Count > 0)
						{
							caretPos = new TextPosition(nextCaretLine, nextCharacterIndex);
							tokenIndex = editorLine.FindTokenIndexAfter(caretPos);
							if (caretPos > editorLine.lineTokenStartPos[tokenIndex])
							{
								var collapsedTokenSpan = editorLine.collapsedTokenSpans[tokenIndex];
								if (collapsedTokenSpan != null)
								{
									var span = collapsedTokenSpan.span;
									nextCaretLine = span.End.line;
									nextCharacterIndex = span.End.index;
								}
							}
						}
						
						current.Use();
						break;
					}

					editorLine = GetOrCreateLineInView(caretPosition.line);
					caretPos = new TextPosition(caretPosition.line, caretPosition.characterIndex);
					tokenIndex = editorLine.FindTokenIndexAfter(caretPos);
					
					// Is the caret at the end of line?
					if (tokenIndex == editorLine.lineTokens.Count)
					{
						if (++nextCaretLine < textBuffer.numParsedLines)
						{
							nextCharacterIndex = 0;
						}
						else
						{
							--nextCaretLine;
						}
						
						current.Use();
						break;
					}

					// Is the token after caret a collapsed token?
					if (tokenIndex < editorLine.collapsedTokenSpans.Count)
					{
						var collapsedTokenSpan = editorLine.collapsedTokenSpans[tokenIndex];
						if (collapsedTokenSpan != null)
						{
							var span = collapsedTokenSpan.span;
							nextCaretLine = span.End.line;
							nextCharacterIndex = span.End.index;
							
							current.Use();
							break;
						}
					}
					else if (editorLine.collapsedTokenSpans.Count > 0)
					{
						//Debug.Log(caretPosition);
						//Debug.Log("tokenIndex: " + tokenIndex + "  count: " + editorLine.collapsedTokenSpans.Count);
					}
					
					++nextCharacterIndex;
					current.Use();
					break;
				
				case KeyCode.Mouse3:
					current.Use();
					if (CanGoBack())
						GoToRecentLocation(false);
					break;
				
				case KeyCode.Mouse4:
					current.Use();
					if (CanGoForward())
						GoToRecentLocation(true);
					break;
					
				case KeyCode.F3:
					if (isActionKey && !current.alt)
					{
						UseSelectionForSearch();
						if (current.shift)
							SearchPrevious();
						else
							SearchNext();
						current.Use();
						focusCodeView = true;
						return;
					}
					break;
			}
		}

		if (current.shift && current.keyCode == KeyCode.Space)
		{
			current.Use();
			addRecentLocationIfUsed = false;
		}
		
		char typedChar = '\0';
		
		if (current.type == EventType.KeyDown)
		{
			if (modifiers == EventModifiers.Control && current.character == ' ')
			{
				current.Use();
				return;
			}
			else if (!current.alt && current.character == '\n' && isActionKey && !current.shift)
			{
				AddRecentLocation(1, true);
				
				if (EditorWindow.focusedWindow)
					OpenAtCursor();
				current.Use();
				return;
			}
			else if ((current.keyCode == KeyCode.Tab || current.character == '\t' || current.character == 25) && (modifiers & ~EventModifiers.Shift) == 0)
			{
				if (current.keyCode != KeyCode.Tab)
				{
					if (modifiers == EventModifiers.Shift)
						IndentLess();
					else
						IndentMoreOrInsertTab(!acceptingAutoComplete);
					
					focusCodeView = true;
					caretMoveTime = frameTime;
					lastCaretMoveWasSearch = false;
					needsRepaint = true;
				}
				current.Use();
				return;
			}
			else if (isActionKey && (current.keyCode == KeyCode.LeftBracket || current.keyCode == KeyCode.RightBracket))
			{
				if (current.keyCode == KeyCode.LeftBracket)
					IndentLess();
				else
					IndentMore();
				
				focusCodeView = true;
				caretMoveTime = frameTime;
				lastCaretMoveWasSearch = false;
				needsRepaint = true;
				current.Use();
				return;
			}
			else if ((current.character >= ' ' && current.character != 0x7f || current.character == '\n' || current.character == 0 && Input.compositionString != "")
				&& (!isActionKey || (modifiers & EventModifiers.Command) == 0 && current.keyCode == KeyCode.None)
				&& TryEdit())
			{
				typedChar = current.character;
				
				if (typedChar == '\n' && modifiers == EventModifiers.Shift && selectionStartPosition == null)
					caretPosition.characterIndex = textBuffer.lines[caretPosition.line].Length;
				
				string text = typedChar != 0 ? typedChar.ToString() : Input.compositionString;
				string autoTextAfter = null;
				var putOpeningBraceOnNextLine = TextPosition.invalid;
				
				if (autoClosingStack.Count > 0 && "}])\">".IndexOf(typedChar) != -1)
				{
					if (selectionStartPosition == null)
					{
						var nextCharPos = textBuffer.FirstNonWhitespacePos(caretPosition.line, caretPosition.characterIndex);
						if (nextCharPos == autoClosingStack.Last().closingPos && textBuffer.lines[nextCharPos.line][nextCharPos.index] == typedChar)
						{
							selectionStartPosition = new FGTextBuffer.CaretPos {
								line = nextCharPos.line,
								characterIndex = nextCharPos.index + 1
							};
							selectionStartPosition.column = selectionStartPosition.virtualColumn =
								CharIndexToColumn(selectionStartPosition.characterIndex, selectionStartPosition.line);
							
							autoClosingStack.RemoveAt(autoClosingStack.Count - 1);
							if (autoClosingStack.Count == 0)
							{
								textBuffer.onInsertedText -= OnInsertedText;
								textBuffer.onRemovedText -= OnRemovedText;
							}
						}
					}
				}
				if (!acceptingAutoComplete && selectionStartPosition == null && "{[(\"<".IndexOf(typedChar) != -1)
				{
					autoTextAfter = CheckAutoClose(typedChar);
				}
				else if (typedChar == '\n')
				{
					// Keep the same indent level for newly inserted lines
					int firstNonWhitespace = textBuffer.FirstNonWhitespace(caretPosition.line);
					if (firstNonWhitespace > caretPosition.characterIndex)
						firstNonWhitespace = caretPosition.characterIndex;
					var indent = textBuffer.lines[caretPosition.line].Substring(0, firstNonWhitespace);
					text += indent;
					
					if (caretPosition.characterIndex > 0)
					{
						var tokenLeft = textBuffer.GetNonTriviaTokenLeftOf(caretPosition.line, caretPosition.characterIndex);
						if (tokenLeft != null &&
							tokenLeft.tokenKind == SyntaxToken.Kind.Punctuator &&
							tokenLeft.text == "{" &&
							tokenLeft.parent != null &&
							tokenLeft.parent.line == caretPosition.line)
						{
							var nextLeaf = tokenLeft.parent.FindNextLeaf();
							if (nextLeaf != null && nextLeaf.line == caretPosition.line && nextLeaf.IsLit("}"))
							{
								autoTextAfter = text;
								
								if (SISettings.moveOpeningBraceOnEmptyLine)
								{
									if (tokenLeft.TokenIndex > 1 || tokenLeft.TokenIndex == 1 && tokenLeft.formattedLine.tokens[0].tokenKind != SyntaxToken.Kind.Whitespace)
									{
										putOpeningBraceOnNextLine = textBuffer.GetTokenSpan(tokenLeft.parent).StartPosition;
									}
								}
							}
							
							if (SISettings.autoIndentCode)
								text += "\t";
						}
						else if (tokenLeft != null && tokenLeft.tokenKind == SyntaxToken.Kind.StringLiteral)
						{
							var charLeftOfCaret = textBuffer.lines[caretPosition.line][caretPosition.characterIndex - 1];
							if (charLeftOfCaret != '"' && charLeftOfCaret != '\\')
							{
								text = "\" +" + text + "\"";
							}
						}
						else if (tokenLeft == null || tokenLeft.Line < caretPosition.line)
						{
							var xmlDocs = GetXMLDocsText(caretPosition.line);
							if (xmlDocs != null && caretPosition.characterIndex >= textBuffer.lines[caretPosition.line].Length - xmlDocs.Length)
							{
								if (xmlDocs.TrimStart() != "")
								{
									text += "/// ";
								}
								else
								{
									var nextLine = GetXMLDocsText(caretPosition.line + 1);
									if (nextLine != null)
										text += "/// ";
								}
							}
						}
					}
		
					if (argumentsHint != null)
						argumentsHint.Flipped = true;
				}

				FGTextBuffer.CaretPos newCaretPosition = caretPosition.Clone();
				if (selectionStartPosition != null)
				{
					newCaretPosition = textBuffer.DeleteText(selectionStartPosition, caretPosition);
					clearSelection = true;
				}
				
				int insertedAtLine = newCaretPosition.line;

				if (current.character == '\n' && !acceptingAutoComplete)
				{
					string breakingLine = textBuffer.lines[insertedAtLine];
					int lineLength = breakingLine.Length;
					int deleteSpaces = 0;
					while (deleteSpaces + newCaretPosition.characterIndex < lineLength)
						if (char.IsWhiteSpace(breakingLine[deleteSpaces + newCaretPosition.characterIndex]))
							++deleteSpaces;
						else
							break;
					if (deleteSpaces > 0)
					{
						var deleteSpacesTo = newCaretPosition.Clone();
						deleteSpacesTo.characterIndex += deleteSpaces;
						textBuffer.DeleteText(newCaretPosition, deleteSpacesTo);
					}
				}
				
				if (typedChar == 0)
				{
					selectionStartPosition = newCaretPosition.Clone();
					clearSelection = false;
				}
				
				newCaretPosition = textBuffer.InsertText(newCaretPosition, text);
				var updateToLine = newCaretPosition.line;
				if (!acceptingAutoComplete && autoTextAfter != null)
				{
					FGTextBufferManager.insertingTextAfterCaret = true;
					updateToLine = textBuffer.InsertText(newCaretPosition, autoTextAfter).line;
					if (putOpeningBraceOnNextLine.line >= 0)
					{
						var beforeBrace = new FGTextBuffer.CaretPos();
						beforeBrace.Set(putOpeningBraceOnNextLine.line, putOpeningBraceOnNextLine.index, 0);
						textBuffer.InsertText(beforeBrace, autoTextAfter);
						newCaretPosition.line++;
						++updateToLine;
						putOpeningBraceOnNextLine = TextPosition.invalid;
					}
					FGTextBufferManager.insertingTextAfterCaret = false;

					if (typedChar != '\n')
					{
						var newAutoClose = new AutoClosePair
						{
							openingPos = new TextPosition(caretPosition.line, caretPosition.characterIndex),
							closingPos = new TextPosition(caretPosition.line, caretPosition.characterIndex + 1)
						};
						autoClosingStack.Add(newAutoClose);
						if (autoClosingStack.Count == 1)
						{
							textBuffer.onRemovedText += OnRemovedText;
							textBuffer.onInsertedText += OnInsertedText;
						}
					}
				}
				nextCharacterIndex = newCaretPosition.characterIndex;
				nextCaretColumn = newCaretPosition.column;
				nextCaretLine = newCaretPosition.line;
				isHorizontal = true;
				
				if (!acceptingAutoComplete)
					textBuffer.UpdateHighlighting(insertedAtLine, updateToLine);
				//if (typedChar == '\n' || typedChar == '{' || typedChar == '}' || typedChar == ':')
				if (typedChar != 0 && typedChar != ' ' && !acceptingAutoComplete)
				{
					reindentLinesFrom = insertedAtLine;
					reindentLinesTo = updateToLine;
				}

				modifiers &= ~EventModifiers.Shift;
				current.Use();
				
				if (!acceptingAutoComplete)
				{
					AfterCharecterTyped(text, nextCaretLine, nextCharacterIndex);
					tryArgumentsHint = true;
				}
				
				lastTypedText = text;
			}
			else if (current.keyCode == KeyCode.Delete && modifiers == EventModifiers.Shift && selectionStartPosition == null
				&& TryEdit())
			{
				modifiers = 0;

				if (caretPosition.line == textBuffer.numParsedLines - 1)
				{
					textBuffer.lines[caretPosition.line] = string.Empty;
					nextCharacterIndex = 0;
					nextCaretColumn = 0;
					//isHorizontal = true;
				}
				else
				{
					textBuffer.DeleteText(new FGTextBuffer.CaretPos { characterIndex = 0, column = 0, virtualColumn = 0, line = caretPosition.line },
						new FGTextBuffer.CaretPos { characterIndex = 0, column = 0, virtualColumn = 0, line = caretPosition.line + 1 });
					nextCaretColumn = caretPosition.column;
					nextCharacterIndex = textBuffer.ColumnToCharIndex(ref nextCaretColumn, caretPosition.line);
				}
				
				clearSelection = true;
				textBuffer.UpdateHighlighting(caretPosition.line, caretPosition.line);
				current.Use();
			}
			else if ((current.keyCode == KeyCode.Backspace || current.keyCode == KeyCode.Delete)
				&& TryEdit())
			{
				modifiers &= ~EventModifiers.Shift;
				
				FGTextBuffer.CaretPos newCaretPosition = caretPosition.Clone();
				if (selectionStartPosition == null)
				{
					Event simKey = new Event(current);
					bool checkAutoClosed = false;
					
					if (current.keyCode == KeyCode.Delete)
					{
						simKey.keyCode = KeyCode.RightArrow;
						if (caretPosition.characterIndex == textBuffer.lines[caretPosition.line].Length
							&& caretPosition.line + 1 < textBuffer.lines.Count)
						{
							var nextLineText = textBuffer.lines[caretPosition.line + 1];
							if (nextLineText != "" && (nextLineText[0] == ' ' || nextLineText[0] == '\t'))
							{
								simKey = null;
								selectionStartPosition = new FGTextBuffer.CaretPos {
									line = caretPosition.line + 1,
									characterIndex = textBuffer.FirstNonWhitespace(caretPosition.line + 1),
								};
								selectionStartPosition.column = CharIndexToColumn(selectionStartPosition.characterIndex, selectionStartPosition.line);
								selectionStartPosition.virtualColumn = selectionStartPosition.column;
							}
						}
					}
					else
					{
						simKey.keyCode = KeyCode.LeftArrow;
						checkAutoClosed = true;
					}
					if (simKey != null)
					{
						simKey.modifiers |= EventModifiers.Shift;
						ProcessEditorKeyboard(simKey, true);
					}
					
					if (checkAutoClosed && autoClosingStack.Count > 0)
					{
						AutoClosePair entry = autoClosingStack.Last();
						{
							var textPos = entry.openingPos;
							if (textPos.line == caretPosition.line && textPos.index == caretPosition.characterIndex)
							{
								textPos.Move(textBuffer, 1);
								while (textPos < entry.closingPos)
								{
									var textLine = textBuffer.lines[textPos.line];
									var toIndex = textPos.line == entry.closingPos.line ? entry.closingPos.index : textLine.Length;
									for (; textPos.index < toIndex; ++textPos.index)
										if (!char.IsWhiteSpace(textLine, textPos.index))
											break;
									if (textPos.index < toIndex)
										break;
									textPos.index = 0;
									++textPos.line;
								}
								if (textPos >= entry.closingPos)
								{
									selectionStartPosition = new FGTextBuffer.CaretPos {
										line = entry.closingPos.line,
										characterIndex = entry.closingPos.index + 1
										};
									selectionStartPosition.column = selectionStartPosition.virtualColumn =
										CharIndexToColumn(selectionStartPosition.characterIndex, selectionStartPosition.line);
									
									autoClosingStack.RemoveAt(autoClosingStack.Count - 1);
									if (autoClosingStack.Count == 0)
									{
										textBuffer.onInsertedText -= OnInsertedText;
										textBuffer.onRemovedText -= OnRemovedText;
									}
								}
							}
						}
					}
				}

				modifiers &= ~EventModifiers.Shift;

				if (selectionStartPosition != null)
				{
					newCaretPosition = textBuffer.DeleteText(selectionStartPosition, caretPosition);

					nextCharacterIndex = newCaretPosition.characterIndex;
					nextCaretColumn = newCaretPosition.column;
					nextCaretLine = newCaretPosition.line;
					isHorizontal = true;
					clearSelection = true;

					textBuffer.UpdateHighlighting(newCaretPosition.line, newCaretPosition.line);
					current.Use();
				}
				
				tryArgumentsHint = true;
			}
		}

		//if (clearSelection ||
		//    isHorizontal && nextCharacterIndex != caretPosition.characterIndex ||
		//    nextCaretColumn != caretPosition.virtualColumn ||
		//    nextCaretLine != caretPosition.line)
		if (current.type == EventType.Used)
		{
			caretMoveTime = frameTime;
			lastCaretMoveWasSearch = false;

			if (selectionStartPosition == null && current.shift)
			{
				selectionStartPosition = new FGTextBuffer.CaretPos
				{
					column = caretPosition.column,
					line = caretPosition.line,
					virtualColumn = caretPosition.column,
					characterIndex = caretPosition.characterIndex
				};
				if (!isHorizontal && nextCaretLine != caretPosition.line)
					nextCaretColumn = caretPosition.column;
			}

			if (nextCaretLine < 0)
				nextCaretLine = 0;
			if (nextCaretLine >= textBuffer.numParsedLines)
				nextCaretLine = textBuffer.numParsedLines - 1;

			//caretPosition.virtualColumn = nextCaretColumn;
			if (isHorizontal)
			{
				caretPosition.virtualColumn = nextCaretColumn =
					BufferPositionToEditorColumn(nextCaretLine, nextCharacterIndex, nextCaretColumn);
				//if (wordWrapping)
				//{
				//	List<int> softLineBreaks = GetSoftLineBreaks(nextCaretLine);
				//	int softRow = FindFirstIndexGreaterThanOrEqualTo<int>(softLineBreaks, nextCharacterIndex);
				//	if (softRow < softLineBreaks.Count && nextCharacterIndex == softLineBreaks[softRow])
				//		++softRow;
				//	caretPosition.virtualColumn = nextCaretColumn =
				//		textBuffer.CharIndexToColumn(nextCharacterIndex, nextCaretLine, softRow > 0 ? softLineBreaks[softRow - 1] : 0);
				//	caretPosition.virtualColumn -= softRow > 0 ? softLineBreaks[softRow - 1] : 0;
				//}
				//else
				//{
				//	caretPosition.virtualColumn = nextCaretColumn =
				//		textBuffer.CharIndexToColumn(nextCharacterIndex, nextCaretLine);
				//}
			}
			else
			{
//				nextCharacterIndex = textBuffer.ColumnToCharIndex(ref nextCaretColumn, nextCaretLine);
			}
			caretPosition.column = nextCaretColumn;
			caretPosition.characterIndex = nextCharacterIndex;
			caretPosition.line = nextCaretLine;

			if (!isHorizontal && nextCaretLine >= 0)
			{
			    //caretPosition.characterIndex = Math.Min(nextCharacterIndex, textBuffer.lines[nextCaretLine].Length);
				caretPosition.virtualColumn = nextCaretColumn;
			    caretPosition.column = textBuffer.CharIndexToColumn(caretPosition.characterIndex, nextCaretLine);
			}
			
			if (current.character != 0 || Input.compositionString == "")
			{
				if (clearSelection || selectionStartPosition != null && ((modifiers & EventModifiers.Shift) == 0 || selectionStartPosition == caretPosition))
					selectionStartPosition = null;
			}
			
			scrollToCaret = true;
			//Repaint();
			needsRepaint = true;
			
			if (reindentLinesFrom >= 0 && reindentLinesFrom <= reindentLinesTo && textBuffer.CanEdit())
			{
				ReindentLines(reindentLinesFrom, reindentLinesTo);
			}
			
			if (addRecentLocationIfUsed)
			{
				AddRecentLocation(0, true);
			}
			
			if (SISettings.smartSemicolonPlacement && !tempIgnoreSmartSemicolon && lastTypedText == ";")
			{
				if (acceptingAutoComplete)
					textBuffer.UpdateHighlighting(caretPosition.line, caretPosition.line);

				lastTypedText = null;
				var tokenLeft = GetTokenAtPosition(caretPosition.line, caretPosition.characterIndex);
				if (tokenLeft != null && tokenLeft.tokenKind == SyntaxToken.Kind.Punctuator)
				{
					var tokens = textBuffer.formattedLines[tokenLeft.Line].tokens;
					var tokenIndex = tokenLeft.TokenIndex;
					if (tokenIndex < tokens.Count - 1)
					{
						if (!tokens.Exists(
							t => t.TokenIndex != tokenIndex &&
							t.tokenKind > SyntaxToken.Kind.LastWSToken &&
							(t.text == "}" || t.text == "{" || t.text == ";" || t.text == "for")
						))
						{
							var lastTokenIndex = tokens.FindLastIndex(
								t => t.tokenKind > SyntaxToken.Kind.LastWSToken && t.TokenIndex > tokenIndex);
							if (lastTokenIndex > tokenIndex)
							{
								textBuffer.EndEdit();
								textBuffer.BeginEdit("Smart Semicolon Placement");
								ProcessEditorKeyboard(Event.KeyboardEvent("backspace"), true);
								
								tokens = textBuffer.formattedLines[caretPosition.line].tokens;
								lastTokenIndex = tokens.FindLastIndex(t => t.tokenKind > SyntaxToken.Kind.LastWSToken);
								
								var lastTokenSpan = textBuffer.GetTokenSpan(caretPosition.line, lastTokenIndex);
								caretPosition = new FGTextBuffer.CaretPos {
									line = lastTokenSpan.line,
									characterIndex = lastTokenSpan.EndPosition.index
								};
								caretPosition.column = CharIndexToColumn(caretPosition.characterIndex, caretPosition.line);
								caretPosition.virtualColumn = caretPosition.column;
								
								tempIgnoreSmartSemicolon = true;
								ProcessEditorKeyboard(Event.KeyboardEvent(";"), false);
								tempIgnoreSmartSemicolon = false;
								lastTypedText = null;
								return;
							}
						}
					}
				}
			}
			
			if (lastTypedText == "/")
			{
				var tokenLeft = GetTokenAtPosition(caretPosition.line, caretPosition.characterIndex);
				if (tokenLeft != null && tokenLeft.tokenKind == SyntaxToken.Kind.Comment)
				{
					var tokens = textBuffer.formattedLines[tokenLeft.Line].tokens;
					var tokenIndex = tokenLeft.TokenIndex;
					if (tokenIndex >= 1 && tokenIndex <= 2 && tokenIndex == tokens.Count - 1 &&
						tokenLeft.text == "/" && tokens[tokenIndex - 1].text == "//")
					{
						if (tokenIndex == 1 || tokens[0].tokenKind == SyntaxToken.Kind.Whitespace)
						{
							var nextToken = textBuffer.GetNonTriviaTokenAfter(tokenLeft);
							if (nextToken != null && nextToken.parent != null && nextToken.parent.parent != null)
							{
								var node = nextToken.parent.parent;
								var declarationNode = node;
								while (declarationNode != null)
								{
									switch (declarationNode.RuleName)
									{
									case "namespaceMemberDeclaration":
									case "classMemberDeclaration":
									case "structMemberDeclaration":
									case "interfaceMemberDeclaration":
										if (nextToken.parent != declarationNode.GetFirstLeaf())
											declarationNode = null;
										else
											declarationNode = (
												declarationNode.FindChildByName("methodDeclaration") ??
												declarationNode.FindChildByName("classDeclaration") ??
												declarationNode.FindChildByName("structDeclaration") ??
												declarationNode.FindChildByName("interfaceDeclaration") ??
												declarationNode.FindChildByName("enumDeclaration") ??
												declarationNode.FindChildByName("delegateDeclaration") ??
												declarationNode.FindChildByName("constructorDeclaration") ??
												declarationNode.FindChildByName("destructorDeclaration") ??
												declarationNode.FindChildByName("propertyDeclaration") ??
												declarationNode.FindChildByName("indexerDeclaration") ??
												declarationNode.FindChildByName("fieldDeclaration") ??
												declarationNode.FindChildByName("operatorDeclaration") ??
												declarationNode.FindChildByName("eventDeclaration") ??
												declarationNode.FindChildByName("conversionOperatorDeclaration") ??
												declarationNode.FindChildByName("constantDeclaration") ??
												declarationNode.FindChildByName("interfaceMethodDeclaration") ??
												declarationNode.FindChildByName("interfaceEventDeclaration") ??
												declarationNode.FindChildByName("interfacePropertyDeclaration") ??
												declarationNode.FindChildByName("interfaceIndexerDeclaration") ??
												declarationNode.FindChildByName("enumMemberDeclaration")
												) as ParseTree.Node;
											break;
									default:
										declarationNode = declarationNode.parent;
										continue;
									}
									break;
								}
								if (declarationNode != null)
								{
									textBuffer.EndEdit();
									textBuffer.BeginEdit("Autocomplete XMLDocs");
									
									var updateFromLine = caretPosition.line;
									var indent = tokenIndex == 1 ? "" : tokens[0].text.GetString();
									
									caretPosition = textBuffer.InsertText(caretPosition, " <summary>\n" + indent + "/// ");
									
									var sb = new StringBuilder("\n");
									sb.Append(indent);
									sb.Append("/// </summary>");
									
									var definition = declarationNode.declaration != null ? declarationNode.declaration.definition : null;
									if (definition != null)
									{
										var typeParameters = definition.GetTypeParameters();
										if (typeParameters != null)
										{
											for (var i = 0; i < typeParameters.Count; i++)
											{
												sb.Append("\n");
												sb.Append(indent);
												sb.Append("/// <typeparam name=\"");
												sb.Append(typeParameters[i].name);
												sb.Append("\"></typeparam>");
											}
										}
										
										var parameters = definition.GetParameters();
										if (parameters != null)
										{
											for (var i = 0; i < parameters.Count; i++)
											{
												sb.Append("\n");
												sb.Append(indent);
												sb.Append("/// <param name=\"");
												sb.Append(parameters[i].name);
												sb.Append("\"></param>");
											}
										}
										
										var methodDefinition = definition as InvokeableSymbolDefinition
											?? definition as ConstructedMethodDefinition;
										if (definition.kind == SymbolKind.Indexer ||
											methodDefinition != null && definition.TypeOf() != SymbolDefinition.builtInTypes_void)
										{
											sb.Append("\n");
											sb.Append(indent);
											sb.Append("/// <returns></returns>");
										}
									}
									
									FGTextBufferManager.insertingTextAfterCaret = true;
									var updateToLine = textBuffer.InsertText(caretPosition, sb.ToString()).line;
									FGTextBufferManager.insertingTextAfterCaret = false;
									
									textBuffer.UpdateHighlighting(updateFromLine, updateToLine);
									lastTypedText = null;
								}
							}
						}
					}
				}
			}
		}
	}
	
	private string CheckAutoClose(char typedChar)
	{
		string autoTextAfter = null;
		var tokenLeft = GetTokenAtPosition(caretPosition.line, caretPosition.characterIndex);

		if (tokenLeft == null ||
			tokenLeft.tokenKind != SyntaxToken.Kind.Comment &&
			tokenLeft.tokenKind != SyntaxToken.Kind.CharLiteral &&
			tokenLeft.tokenKind != SyntaxToken.Kind.StringLiteral &&
			tokenLeft.tokenKind != SyntaxToken.Kind.InterpolatedStringFormatLiteral &&
			tokenLeft.tokenKind != SyntaxToken.Kind.VerbatimStringBegin &&
			tokenLeft.tokenKind != SyntaxToken.Kind.VerbatimStringLiteral)
		{
			if (typedChar == '{')
			{
				autoTextAfter = TryAutoClose("}");
			}
			else if (typedChar == '"')
			{
				autoTextAfter = TryAutoClose("\"");
			}
			else if (tokenLeft == null ||
				tokenLeft.tokenKind != SyntaxToken.Kind.InterpolatedStringWholeLiteral &&
				tokenLeft.tokenKind != SyntaxToken.Kind.InterpolatedStringStartLiteral &&
				tokenLeft.tokenKind != SyntaxToken.Kind.InterpolatedStringMidLiteral &&
				tokenLeft.tokenKind != SyntaxToken.Kind.InterpolatedStringEndLiteral)
			{
				if (typedChar == '[')
				{
					autoTextAfter = TryAutoClose("]");
				}
				else if (typedChar == '(')
				{
					autoTextAfter = TryAutoClose(")");
				}
				else if (typedChar == '<')
				{
					if (tokenLeft != null && tokenLeft.parent != null)
					{
						var symbolLeft = tokenLeft.parent.resolvedSymbol;
						if (symbolLeft != null)
						{
							if (symbolLeft is TypeDefinitionBase ||
								symbolLeft.kind == SymbolKind.Method ||
								symbolLeft.kind == SymbolKind.MethodGroup)
							{
								autoTextAfter = TryAutoClose(">");
							}
						}
					}
				}
			}
		}
		return autoTextAfter;
	}
	
	private string GetXMLDocsText(int line)
	{
		if (line >= textBuffer.lines.Count)
			return null;
		
		var formattedLine = line > 0 ? textBuffer.formattedLines[line - 1] : null;
		if (formattedLine != null && formattedLine.blockState != FGTextBuffer.BlockState.None)
			return null;
		
		formattedLine = textBuffer.formattedLines[line];
		if (formattedLine.tokens.Count < 2 || formattedLine.tokens.Count > 3)
			return null;
		
		if (formattedLine.tokens.Count == 3 && formattedLine.tokens[0].tokenKind != SyntaxToken.Kind.Whitespace)
			return null;
		
		var commentToken = formattedLine.tokens[formattedLine.tokens.Count - 2];
		if (commentToken.tokenKind != SyntaxToken.Kind.Comment || commentToken.text != "//")
			return null;
		
		commentToken = formattedLine.tokens[formattedLine.tokens.Count - 1];
		if (!commentToken.text.FastStartsWith("/"))
			return null;
		if (commentToken.text.FastStartsWith("//"))
			return null;
		
		return commentToken.text.Substring(1);
	}

	private void AfterCharecterTyped(string text, int nextCaretLine, int nextCharacterIndex)
	{
		var autoPopupCompletions = false;
		if (text.Length == 1 && autocompleteWindow == null)
		{
			var tokenLeft = GetTokenAtPosition(nextCaretLine, nextCharacterIndex);
			if (tokenLeft != null && tokenLeft.tokenKind == SyntaxToken.Kind.Whitespace && tokenLeft.TokenIndex > 0)
			{
				var tokens = textBuffer.formattedLines[tokenLeft.Line].tokens;
				tokenLeft = tokens[tokenLeft.TokenIndex - 1];

				var numSkippedTokens = 1;
				while (tokenLeft != null && tokenLeft.TokenIndex > 0
					&& (tokenLeft.tokenKind == SyntaxToken.Kind.Missing || tokenLeft.tokenKind == SyntaxToken.Kind.Whitespace))
				{
					++numSkippedTokens;
					tokenLeft = textBuffer.formattedLines[tokenLeft.Line].tokens[tokenLeft.TokenIndex - 1];
				}
	
				if (tokenLeft != null && tokenLeft.parent != null && tokenLeft.parent.syntaxError == null)
				{
					var tokenLeftText = tokenLeft.text;
					var lastChar = tokenLeftText[tokenLeftText.length - 1];
					if (lastChar == '=' || tokenLeftText == ">" || tokenLeftText == "<" || tokenLeftText == "~" ||
						tokenLeftText == "|" || tokenLeftText == "&" || tokenLeftText == "(" || tokenLeftText == "," ||
						tokenLeftText == "case" || tokenLeftText == "new" ||
						tokenLeftText == "override" && tokenLeft.tokenKind == SyntaxToken.Kind.Keyword && tokenLeft.TokenIndex == tokens.Count - numSkippedTokens - 1)
					{
						autoPopupCompletions = true;
						tryAutoSuggestion = true;
					}
				}
			}
			else if (tokenLeft != null && tokenLeft.tokenKind == SyntaxToken.Kind.Punctuator && tokenLeft.text == "~")
			{
				autoPopupCompletions = true;
				tryAutoSuggestion = true;
			}
			else if (tokenLeft != null && tokenLeft.parent != null && tokenLeft.parent.syntaxError == null)
			{
				if (tokenLeft.tokenKind == SyntaxToken.Kind.Punctuator)
				{
					if (text == "." || text == "->" /*|| text == "?."*/)
					{
						autoPopupCompletions = true;
						var nextLineText = textBuffer.lines[nextCaretLine];
						if (nextCharacterIndex > 1 && nextLineText[nextCharacterIndex - 2] == '.')
						{
							autoPopupCompletions = false;
						}
						else if (nextCharacterIndex > 1 && char.IsDigit(nextLineText, nextCharacterIndex - 2))
						{
							autoPopupCompletions = false;
							for (var checkDigit = nextCharacterIndex - 3; checkDigit >= 0; --checkDigit)
							{
								if (!char.IsDigit(nextLineText, checkDigit))
								{
									var c = nextLineText[checkDigit];
									autoPopupCompletions = c == '.' || c == '_' || char.IsLetter(c);
									break;
								}
							}
						}
					}
					else if (text == ":")
					{
						if (nextCharacterIndex >= 1 && textBuffer.lines[nextCaretLine][nextCharacterIndex - 2] == ':')
						{
							autoPopupCompletions = true;
						}
					}
				}
			}
			
			var uc = text[0];//char.ToUpper(text[0]);
			
			if (uc == '_' && (tokenLeft == null || tokenLeft.text != "."))
			{
				return;
			}
			
			if (tokenLeft == null || tokenLeft.tokenKind != SyntaxToken.Kind.Comment)
			{
				//if (uc == ' ')
				//{
				//	var line = textBuffer.formattedLines[insertedAtLine];
				//	var tokenLeft = GetTokenAtCursor();
				//	if (tokenLeft.tokenKind == SyntaxToken.Kind.Whitespace)
				//		Debug.Log(tokenLeft);
				//}
				
				if (char.IsLetterOrDigit(uc) || uc == '_')
				{
					autoPopupCompletions = true;
					//if (nextCharacterIndex >= 2)
					//{
					//	var prevChar = textBuffer.lines[nextCaretLine][nextCharacterIndex - 2];
					//	if (char.IsLetterOrDigit(prevChar) || prevChar == '_')
					//		autoPopupCompletions = false;
					//}
				}
			}
		}
		
		if (autoPopupCompletions)
			tryAutocomplete = true;
	}
	
	private struct AutoClosePair
	{
		public TextPosition openingPos;
		public TextPosition closingPos;
	}
	[NonSerialized]
	private List<AutoClosePair> autoClosingStack = new List<AutoClosePair>();
	
	private void OnRemovedText(TextPosition from, TextPosition to)
	{
		for (var i = autoClosingStack.Count; i --> 0; )
		{
			var entry = autoClosingStack[i];
			if (from <= entry.openingPos)
			{
				if (to > entry.openingPos)
				{
					autoClosingStack.RemoveAt(i);
					continue;
				}
				
				var updated = entry.openingPos.OnRemovedText(from, to);
				if (entry.closingPos.OnRemovedText(from, to) || updated)
					autoClosingStack[i] = entry;
			}
			else if (from <= entry.closingPos)
			{
				if (to > entry.closingPos)
				{
					autoClosingStack.RemoveAt(i);
					continue;
				}

				if (entry.closingPos.OnRemovedText(from, to))
					autoClosingStack[i] = entry;
			}
		}

		if (autoClosingStack.Count == 0)
		{
			textBuffer.onInsertedText -= OnInsertedText;
			textBuffer.onRemovedText -= OnRemovedText;
		}
	}
	
	private void OnRemovedTextTrackSelection(TextPosition from, TextPosition to)
	{
		savedCaretPos.OnRemovedText(from, to);
		savedSelectionPos.OnRemovedText(from, to);
	}
	
	private void OnInsertedText(TextPosition from, TextPosition to)
	{
		for (var i = autoClosingStack.Count; i --> 0; )
		{
			var entry = autoClosingStack[i];
			var updated = entry.closingPos.OnInsertedText(from, to, true);
			if (entry.openingPos.OnInsertedText(from, to, true) || updated)
			{
				autoClosingStack[i] = entry;
			}
		}
	}
	
	private void OnInsertedTextTrackSelection(TextPosition from, TextPosition to)
	{
		if (isRefactoring)
		{
			savedCaretPos.OnInsertedText(from, to, true);
			savedSelectionPos.OnInsertedText(from, to, true);
		}
	}
	
	[NonSerialized]
	private bool isRefactoring;
	[NonSerialized]
	private TextPosition savedCaretPos;
	[NonSerialized]
	private TextPosition savedSelectionPos;
	
	public void BeginRefactoring(string description)
	{
		if (isRefactoring)
			return;
		
		textBuffer.onInsertedText += OnInsertedTextTrackSelection;
		textBuffer.onRemovedText += OnRemovedTextTrackSelection;
		
		textBuffer.BeginEdit("Refactoring");
		isRefactoring = true;
		savedCaretPos = new TextPosition(caretPosition.line, caretPosition.characterIndex);
		if (selectionStartPosition != null)
			savedSelectionPos = new TextPosition(selectionStartPosition.line, selectionStartPosition.characterIndex);
		else
			savedSelectionPos = TextPosition.invalid;
	}
	
	public void EndRefactoring()
	{
		if (!isRefactoring)
			Debug.LogError("EndRefactoring() called without calling BeginRefactoring()");
		
		textBuffer.onInsertedText -= OnInsertedTextTrackSelection;
		textBuffer.onRemovedText -= OnRemovedTextTrackSelection;
		
		isRefactoring = false;
		
		var column = CharIndexToColumn(savedCaretPos.index, savedCaretPos.line);
		caretPosition = new FGTextBuffer.CaretPos {
			line = savedCaretPos.line,
			characterIndex = savedCaretPos.index,
			column = column,
			virtualColumn = column
		};
		
		selectionStartPosition = null;
		if (savedSelectionPos.line >= 0 && savedSelectionPos != savedCaretPos)
		{
			column = CharIndexToColumn(savedSelectionPos.index, savedSelectionPos.line);
			selectionStartPosition = new FGTextBuffer.CaretPos {
				line = savedSelectionPos.line,
				characterIndex = savedSelectionPos.index,
				column = column,
				virtualColumn = column
			};
		}
		
		ValidateCarets();
		if (hasSelection && selectionStartPosition.IsSameAs(caretPosition))
			selectionStartPosition = null;
		
		caretMoveTime = frameTime;
		lastCaretMoveWasSearch = false;
		scrollToCaret = true;
		
		textBuffer.EndEdit();
		
		AddRecentLocation(1, true);
	}
	
	private string TryAutoClose(string closeWith)
	{
		var textLine = textBuffer.lines[caretPosition.line];
		var scanPos = caretPosition.characterIndex;
		var toPos = textLine.Length;
		if (autoClosingStack.Count > 0)
		{
			var lastAutoClose = autoClosingStack.Last().closingPos;
			if (lastAutoClose.line == caretPosition.line)
				toPos = Math.Min(lastAutoClose.index, textLine.Length);
		}

		for (; scanPos < toPos; ++scanPos)
			if (!char.IsWhiteSpace(textLine, scanPos))
				return null;
		
		return closeWith;
	}
	
	public void ReindentLines(int from, int to)
	{
		if (autocompleteWindow != null)
			return;
		
		textBuffer.BeginEdit("Auto-indent");
		
		//var updatedFrom = int.MaxValue;
		//var updatedTo = -1;
		
		for (var line = from; line <= to; ++line)
		{
			var newIndent = textBuffer.CalcAutoIndent(line);
			if (newIndent == null)
				continue;
			
			var newIndentLength = newIndent.Length;
			
			var formattedLine = textBuffer.formattedLines[line];
			var tokens = formattedLine.tokens;
			var firstToken = tokens != null && tokens.Count > 0 ? tokens[0] : null;
			if (firstToken.tokenKind != SyntaxToken.Kind.Whitespace)
				firstToken = null;
			
			var oldIndentLength = firstToken == null ? 0 : firstToken.text.length;
			
			var firstChar = oldIndentLength;//textBuffer.FirstNonWhitespace(line);
			if (firstChar != newIndentLength)
			{
				//updatedFrom = Mathf.Min(updatedFrom, line);
				//updatedTo = Mathf.Max(updatedTo, line);
				
				var tcp = textBuffer.DeleteText(
					new FGTextBuffer.CaretPos { line = line, characterIndex = 0 },
					new FGTextBuffer.CaretPos { line = line, characterIndex = firstChar });
				tcp = textBuffer.InsertText(tcp, newIndent);
				
				if (caretPosition.line == line && firstChar <= caretPosition.characterIndex)
				{
					caretPosition.characterIndex += tcp.characterIndex - firstChar;
					caretPosition.column = caretPosition.virtualColumn = CharIndexToColumn(caretPosition.characterIndex, line);
				}
				if (selectionStartPosition != null && selectionStartPosition.line == line && firstChar <= selectionStartPosition.characterIndex)
				{
       				selectionStartPosition.characterIndex += tcp.characterIndex - firstChar;
					selectionStartPosition.column = selectionStartPosition.virtualColumn = CharIndexToColumn(selectionStartPosition.characterIndex, line);
				}
				
				if (oldIndentLength == 0)
				{
					var newToken = new SyntaxToken(SyntaxToken.Kind.Whitespace, newIndent);
					newToken.formattedLine = formattedLine;
					tokens.Insert(0, newToken);
				}
				else if (newIndentLength == 0)
				{
					tokens.RemoveAt(0);
				}
				else
				{
					firstToken.text = newIndent;
				}
			}
		}
		
		textBuffer.EndEdit();
		//if (updatedTo > -1)
		//	textBuffer.UpdateHighlighting(updatedFrom, updatedTo);
	}
	
	[NonSerialized]
	private ParseTree.Leaf showArgumentsHintForLeaf;
	[NonSerialized]
	private int currentArgumentIndex = -1;
		
	private void UpdateArgumentsHint(bool canShow)
	{
		showArgumentsHintForLeaf = null;
		currentArgumentIndex = -1;
		
		if (caretPosition.line >= textBuffer.formattedLines.Count)
			return;
		
		int line, characterIndex;
		var tokenLeft = textBuffer.GetNonTriviaTokenLeftOf(caretPosition, out line, out characterIndex);
		if (tokenLeft != null && tokenLeft.parent != null)
		{
			int tokenIndex = tokenLeft.parent.tokenIndex;
			while (tokenLeft != null && (tokenLeft.parent == null ||tokenLeft.parent.syntaxError != null))
				tokenLeft = textBuffer.GetTokenLeftOf(ref line, ref tokenIndex);
		}
		if (tokenLeft != null && tokenLeft.parent != null && tokenLeft.parent.syntaxError == null)
		{
			var argumentsNode = tokenLeft.parent.parent;
			if (tokenLeft.text == ")" && argumentsNode != null && argumentsNode.RuleName == "arguments")
			{
				argumentsNode = argumentsNode.parent;
			}
			
			if (argumentsNode != null)
			{
				if (argumentsNode.RuleName == "argumentList")
					currentArgumentIndex = (tokenLeft.parent.childIndex + 1) / 2;
				else if (argumentsNode.RuleName == "arguments")
					currentArgumentIndex = 0;
				
				while (argumentsNode != null && argumentsNode.RuleName != "arguments")
				{
					if (argumentsNode.RuleName == "argument" ||
						argumentsNode.parent != null && argumentsNode.parent.RuleName == "argumentList")
					{
						currentArgumentIndex = (argumentsNode.childIndex + 1) / 2;
					}
					if (argumentsNode.RuleName == "lambdaExpressionBody" ||
						argumentsNode.RuleName == "objectOrCollectionInitializer" ||
						argumentsNode.RuleName == "stringInterpolation")
					{
						argumentsNode = null;
					}
					else
					{
						argumentsNode = argumentsNode.parent;
					}
				}
			}
			
			if (argumentsNode != null)
			{
				// Are we typing in the method arguments?
				
				var leafBeforeArgs = argumentsNode.FindPreviousLeaf();
				if (leafBeforeArgs != null && leafBeforeArgs.parent != null)
				{
					var nodeLeft = leafBeforeArgs.parent;
					if (nodeLeft != null)
					{
						ParseTree.Leaf methodLeaf = null;
						if (nodeLeft.RuleName == "typeArgumentList")
							nodeLeft = nodeLeft.parent;
						if (nodeLeft.RuleName == "primaryExpressionStart" || nodeLeft.RuleName == "typeOrGeneric")
							methodLeaf = nodeLeft.LeafAt(0);
						else if (nodeLeft.RuleName == "accessIdentifier")
							methodLeaf = nodeLeft.LeafAt(1);
						else if (nodeLeft.RuleName == "constructorInitializer")
							methodLeaf = leafBeforeArgs;
						if (methodLeaf != null)
						{
							if (methodLeaf.parent != null)
							{
								showArgumentsHintForLeaf = methodLeaf;
							}
						}
					}
				}
			}
		}
		
		if (showArgumentsHintForLeaf == null)
		{
			showingArgumentsForMethod = null;
			showingArgumentsForToken = new TextPosition(-1, -1);
			if (argumentsHint != null)
			{
				//Debug.Log("Closing [null]");
				CloseArgumentsHint();
			}
		}
		else if (!canShow)
		{
			if (showingArgumentsForToken.line >= 0 &&
				(showingArgumentsForToken.line != showArgumentsHintForLeaf.line ||
				showingArgumentsForToken.index != showArgumentsHintForLeaf.tokenIndex))
			{
					//Debug.Log("Closing [showingArgumentsForToken != showArgumentsHintForLeaf]");
				CloseArgumentsHint();
			}
			else if (argumentsHint)
			{
				argumentsHint.CurrentParameterIndex = currentArgumentIndex;
				
				if (GetSoftLineBreaks(caretPosition.line) != NO_SOFT_LINE_BREAKS)
					argumentsHint.Flipped = true;
				else if (showingArgumentsForToken.line < caretPosition.line)
					argumentsHint.Flipped = true;
				if (autocompleteWindow != null)
					autocompleteWindow.Flipped = !argumentsHint.Flipped;
			}
			showArgumentsHintForLeaf = null;
			return;
		}
		else
		{
			if (showArgumentsHintForLeaf.resolvedSymbol == null)
				FGResolver.ResolveNode(showArgumentsHintForLeaf.parent.parent);
			
			if (showingArgumentsForMethod == showArgumentsHintForLeaf.resolvedSymbol)
			{
				if (showingArgumentsForToken.line != showArgumentsHintForLeaf.line ||
					showingArgumentsForToken.index != showArgumentsHintForLeaf.tokenIndex)
				{
					//Debug.Log("Closing [showingArgumentsForToken != showArgumentsHintForLeaf]");
					CloseArgumentsHint();
				}
				
				if (argumentsHint)
					argumentsHint.CurrentParameterIndex = currentArgumentIndex;
				
				showArgumentsHintForLeaf = null;
				return;
			}

			if (argumentsHint)
			{
				argumentsHint.CurrentParameterIndex = currentArgumentIndex;
			}
			else if (showingArgumentsForToken.line == showArgumentsHintForLeaf.line &&
				showingArgumentsForToken.index == showArgumentsHintForLeaf.tokenIndex)
			{
				showArgumentsHintForLeaf = null;
				return;
			}

			if (argumentsHint && showingArgumentsForMethod != null)
			{
				var toHide = showingArgumentsForMethod;
				while (toHide != null && toHide.kind == SymbolKind.Method)
					toHide = toHide.parentSymbol;
				var toShow = showArgumentsHintForLeaf.resolvedSymbol;
				while (toShow != null && toShow.kind == SymbolKind.Method)
					toShow = toShow.parentSymbol;
				if (toHide == toShow)
				{
					showArgumentsHintForLeaf = null;
					return;
				}
			}
			
			methodTokenRect = GetTokenRect(showArgumentsHintForLeaf.token);
			methodTokenRect.y -= 2f;
			methodTokenRect.height += 4f;
		}
	}
	
	private void DrawPing(float indent, Rect rcPing, bool bgOnly)
	{
		if (styles.ping == null)
			return;

		float t = (1f - pingTimer) * 64f;
		if (t > 0f && t < 64f)
		{
			rcPing.x += indent;

			GUIStyle ping = styles.ping;
			int left = ping.padding.left;
			ping.padding.left = 0;

			Color oldColor = GUI.color;
			Color oldBgColor = GUI.backgroundColor;
			if (t > 4f)
			{
				if (!bgOnly)
					GUI.backgroundColor = new Color(pingColor.r, pingColor.g, pingColor.b, pingColor.a * (8f - t) * 0.125f);
				else
					GUI.backgroundColor = pingColor;
				//else
				//	GUI.backgroundColor = new Color(pingColor.r, pingColor.g, pingColor.b, (64f - t) * 0.125f);
				if (t > 56f)
					GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, pingColor.a * (64f - t) * 0.125f);
			}
			else
			{
				GUI.backgroundColor = pingColor;
			}

			Matrix4x4 matrix = GUI.matrix;
			if (t < 4f)
			{
				float scale = 2f - Mathf.Abs(1f - t * 0.5f);
				Vector2 pos = rcPing.center;
				GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pos);
			}
			ping.Draw(rcPing, bgOnly ? GUIContent.none : pingContent, false, false, false, false);
			GUI.matrix = matrix;

			ping.padding.left = left;
			GUI.color = oldColor;
			GUI.backgroundColor = oldBgColor;
		}
	}

	// "Links" drop-down menu items handler
	private void FollowHyperlink(object hyperlink)
	{
		Application.OpenURL((string) hyperlink);
	}

	private bool CanUndo()
	{
		return CanEdit() && textBuffer.CanUndo();
	}

	private bool CanRedo()
	{
		return CanEdit() && textBuffer.CanRedo();
	}

	private void Undo()
	{
		if (!TryEdit())
			return;
		
		if (!FGTextBufferManager.GlobalUndo(textBuffer))
			textBuffer.Undo();
		AddRecentLocation(0, true);
	}

	private void Redo()
	{
		if (!TryEdit())
			return;
		
		if (!FGTextBufferManager.GlobalRedo(textBuffer))
			textBuffer.Redo();
		AddRecentLocation(0, true);
	}

	private void ToggleCommentSelection()
	{
		if (!TryEdit())
			return;
		
		textBuffer.BeginEdit("Toggle Comment");

		FGTextBuffer.CaretPos from = caretPosition.Clone();
		FGTextBuffer.CaretPos to = caretPosition.Clone();
		int fromLine = caretPosition.line;
		int toLine = caretPosition.line;

		if (selectionStartPosition != null)
		{
			if (caretPosition < selectionStartPosition)
			{
				to = selectionStartPosition.Clone();
				toLine = to.line;
			}
			else
			{
				from = selectionStartPosition.Clone();
				fromLine = from.line;
		    }
		    if (to.characterIndex == 0)
		        --toLine;
		}
		
		int leftmostNWS = int.MaxValue;
		int[] fnws = new int[toLine - fromLine + 1];
		for (int i = 0; i < fnws.Length; ++i)
		{
			fnws[i] = textBuffer.FirstNonWhitespace(fromLine + i);
			if (fnws[i] < textBuffer.lines[fromLine + i].Length)
			{
				int column = CharIndexToColumn(fnws[i], fromLine + i);
				if (column < leftmostNWS)
					leftmostNWS = column;
			}
		}
		if (leftmostNWS == int.MaxValue)
		{
			textBuffer.EndEdit();
			return; // No code is selected, nothing to comment out
		}

		bool allCommentedOut = true;
		for (int i = 0; i < fnws.Length; ++i)
		{
			int lineLength = textBuffer.lines[fromLine + i].Length;
			if (fnws[i] < lineLength)
			{
				int index = textBuffer.ColumnToCharIndex(ref leftmostNWS, fromLine + i);
				if (textBuffer.isBooFile ? textBuffer.lines[fromLine + i][index] != '#' : textBuffer.lines[fromLine + i][index] != '/'
					|| index + 1 >= lineLength || textBuffer.lines[fromLine + i][index + 1] != '/')
				{
					allCommentedOut = false;
					break;
				}
			}
		}

		bool moveFromPos = from.line == fromLine && (allCommentedOut ? fnws[0] < from.characterIndex : fnws[0] <= from.characterIndex);
		bool moveToPos = to.line == toLine && (allCommentedOut ? fnws[fnws.Length - 1] < to.characterIndex : fnws[fnws.Length - 1] <= to.characterIndex);
		if (allCommentedOut)
		{
			for (FGTextBuffer.CaretPos i = new FGTextBuffer.CaretPos { characterIndex = 0, column = leftmostNWS, line = fromLine, virtualColumn = leftmostNWS }; i.line <= toLine; ++i.line)
			{
				if (fnws[i.line - fromLine] < textBuffer.lines[i.line].Length)
				{
					i.characterIndex = textBuffer.ColumnToCharIndex(ref leftmostNWS, i.line);
					FGTextBuffer.CaretPos j = i.Clone();
					j.column = j.virtualColumn += textBuffer.isBooFile ? 1 : 2;
					j.characterIndex += textBuffer.isBooFile ? 1 : 2;
					textBuffer.DeleteText(i, j);
				}
			}
		}
		else
		{
			for (FGTextBuffer.CaretPos i = new FGTextBuffer.CaretPos { characterIndex = 0, column = leftmostNWS, line = fromLine, virtualColumn = leftmostNWS }; i.line <= toLine; ++i.line)
			{
				if (fnws[i.line - fromLine] < textBuffer.lines[i.line].Length)
				{
					i.characterIndex = textBuffer.ColumnToCharIndex(ref leftmostNWS, i.line);
					textBuffer.InsertText(i, textBuffer.isBooFile ? "#" : "//");
				}
			}
		}
		textBuffer.UpdateHighlighting(fromLine, toLine);

		if (moveFromPos)
			from.characterIndex += textBuffer.isBooFile ? (allCommentedOut ? -1 : 1) : (allCommentedOut ? -2 : 2);
		if (moveToPos)
			to.characterIndex += textBuffer.isBooFile ? (allCommentedOut ? -1 : 1) : (allCommentedOut ? -2 : 2);
		from.column = from.virtualColumn = CharIndexToColumn(from.characterIndex, from.line);
		to.column = to.virtualColumn = CharIndexToColumn(to.characterIndex, to.line);

		if (selectionStartPosition != null)
		{
			if (caretPosition < selectionStartPosition)
			{
				caretPosition = from;
				selectionStartPosition = to;
			}
			else
			{
				selectionStartPosition = from;
				caretPosition = to;
			}
		}
		else
		{
			caretPosition = from;
		}
		caretMoveTime = frameTime;
		lastCaretMoveWasSearch = false;
		scrollToCaret = true;

		textBuffer.EndEdit();
		
		AddRecentLocation(1, true);
	}

	public static char[] spaceAndTab = {' ', '\t'};
	
	public void PingLine(int line)
	{
		CloseAllPopups();
		
		if (line == 0)
			line = 1;
		if (line > textBuffer.lines.Count)
			line = textBuffer.lines.Count;
		
		int fnws = textBuffer.FirstNonWhitespace(line - 1);
		int fromColumn = CharIndexToColumn(fnws, line - 1);
		string expanded = textBuffer.lines[line - 1];
		
		pingContent.text = expanded.Trim(spaceAndTab);
			
		UnfoldTextRange(line - 1, fnws, line - 1, fnws + pingContent.text.Length);

		if (!string.IsNullOrEmpty(pingContent.text))
		{
			int toColumn = expanded.Length;
			caretPosition = new FGTextBuffer.CaretPos { line = line - 1, column = fromColumn, virtualColumn = fromColumn, characterIndex = fnws };
			selectionStartPosition = new FGTextBuffer.CaretPos { line = line - 1, column = toColumn, virtualColumn = toColumn, characterIndex = fnws + pingContent.text.Length };
		}
		else
		{
			caretPosition = new FGTextBuffer.CaretPos { line = line - 1, column = 0, virtualColumn = 0, characterIndex = 0 };
			if (line < textBuffer.lines.Count)
				selectionStartPosition = new FGTextBuffer.CaretPos { line = line, column = 0, virtualColumn = 0, characterIndex = 0 };
			else
				selectionStartPosition = null;
		}
		//ValidateCarets();

		Initialize();

		pingTimer = 1f;
		pingStartTime = frameTime;
		pingColor = yellowPingColor;
		scrollToRect.x = GetCharXOffset(fnws, caretPosition.line, 0);
		scrollToRect.y = GetLineOffset(caretPosition.line);
		scrollToRect.xMax = GetCharXOffset(fnws + pingContent.text.Length, caretPosition.line, 0);
		scrollToRect.height = LineHeight;

		caretMoveTime = frameTime;
		Repaint();
	}
	
	private bool CanGoBack()
	{
		var recentLocations = FGTextBufferManager.Instance().recentLocations;
		if (recentLocations.Count == 0)
			return false;
		
		var offset = FGTextBufferManager.Instance().recentLocationsOffset;
		if (offset < recentLocations.Count - 1)
			return true;
		
		var firstLocation = recentLocations[0];
		if (firstLocation.assetGuid != targetGuid || caretPosition.line != firstLocation.line)
			return true;
		
		return false;
	}
	
	private bool CanGoForward()
	{
		return FGTextBufferManager.Instance().recentLocationsOffset > 0;
	}
	
	public bool AddRecentLocation(int minLinesDistance, bool insert)
	{
		if (targetGuid == "")
			return false;

		var recentLocations = FGTextBufferManager.Instance().recentLocations;
		var offset = FGTextBufferManager.Instance().recentLocationsOffset;
		
		if (recentLocations.Count <= offset)
		{
			FGTextBufferManager.AddRecentLocation(targetGuid, caretPosition, false);
			return true;
		}
		else
		{
			var prevLocation = recentLocations[recentLocations.Count - 1 - offset];
			var prevLinesDistance = Mathf.Abs(caretPosition.line - prevLocation.line);
			var nextLocation = offset > 0 ? recentLocations[recentLocations.Count - offset] : null;
			var nextLinesDistance = nextLocation != null ? Mathf.Abs(caretPosition.line - nextLocation.line) : -1;
			if ((prevLocation.assetGuid != targetGuid ||
				prevLinesDistance >= minLinesDistance &&
				(prevLinesDistance > 0 || caretPosition.characterIndex != prevLocation.index)
			) && (
				nextLocation == null || nextLocation.assetGuid != targetGuid ||
				nextLinesDistance >= minLinesDistance &&
				(nextLinesDistance > 0 || caretPosition.characterIndex != nextLocation.index))
			)
			{
				FGTextBufferManager.AddRecentLocation(targetGuid, caretPosition, insert);
				return true;
			}
		}
		
		return false;
	}
	
	private void GoToRecentLocation(bool forward)
	{
		bool added = AddRecentLocation(1, true);
		
		var recentLocations = FGTextBufferManager.Instance().recentLocations;
		var offset = FGTextBufferManager.Instance().recentLocationsOffset;
		
		bool atCurrentLocation = added;
		if (!atCurrentLocation)
		{
			var currentLocation = recentLocations[recentLocations.Count - 1 - offset];
			atCurrentLocation = currentLocation.assetGuid == targetGuid && caretPosition.line == currentLocation.line;
		}
		
		if (forward)
			--offset;
		else if (atCurrentLocation)
			++offset;
		FGTextBufferManager.Instance().recentLocationsOffset = offset;
		
		var location = recentLocations[recentLocations.Count - 1 - offset];
		if (location.assetGuid == targetGuid)
		{
			SetCursorPosition(location.line, location.index);
		}
		else
		{
			addRecentLocationOnFocusLost = false;
			//if (location.assetGuid == "assets/editor/fakepath.cs")
			//	EditorApplication.ExecuteMenuItem("Window/Immediate");
			//else
				FGCodeWindow.OpenAssetInTab(location.assetGuid, location.line, location.index);
		}
	}
	
	public void SetCursorPosition(int line, int characterIndex)
	{
		selectionStartPosition = null;
		var column = CharIndexToColumn(characterIndex, line);
		caretPosition = new FGTextBuffer.CaretPos {
			line = line,
			characterIndex = characterIndex,
			column = column,
			virtualColumn = column
		};
		ValidateCaret(ref caretPosition);
		
		if (!isRefactoring)
		{
			UnfoldTextRange(line, characterIndex, line, characterIndex);
			
			scrollToCaret = true;
			caretMoveTime = frameTime;
		
			pingContent = new GUIContent();
			pingColor = yellowPingColor;
			pingColor.a = 0.4f;
			pingTimer = 1f;
			pingStartTime = frameTime;
			scrollToRect.x = charSize.x * caretPosition.column - 3f;
			scrollToRect.y = GetLineOffset(caretPosition.line);
			scrollToRect.width = 6f;
			scrollToRect.height = LineHeight;
			
			Repaint();
		}
	}
	
	struct CodePath
	{
		public SymbolDefinition symbol;
		public GUIContent content;
		public bool down;
	}

	private SymbolDefinition codePathSymbol;
	private readonly List<CodePath> codePaths = new List<CodePath>();

	private static GUIStyle breadCrumbLeft, breadCrumbLeftOn, breadcrumbLeftBg;
	private static GUIStyle breadCrumbMid, breadCrumbMidOn, breadcrumbMidBg;
	
	[NonSerialized]
	private readonly GUIContent cachedContentRegion = new GUIContent("#region ");
	private static readonly GUIContent cachedContentNoRegion = new GUIContent("No Region");
	private static readonly GUIContent tempContent = new GUIContent();
	
	private static Font standardFont;
	private static bool isInterFont;
	
	private float DoCodeNavigationToolbar()
	{
		if (!textBuffer.isCsFile)
			return 0f;

#if UNITY_2019_3_OR_NEWER
		scrollViewRect.yMin += 21f;
		//	codeViewRect.yMin += 21f;
#else
		scrollViewRect.yMin += 18f;
		//	codeViewRect.yMin += 18f;
#endif

		var wasGuiEnabled = GUI.enabled;
		GUI.enabled = true;
		
#if UNITY_2019_3_OR_NEWER
		var toolbarRect = new Rect(scrollViewRect.xMin, scrollViewRect.yMin - 21f, scrollViewRect.width, 20f);
#else
		var toolbarRect = new Rect(scrollViewRect.xMin, scrollViewRect.yMin - 18f, scrollViewRect.width, 17f);
#endif
		GUI.Label(toolbarRect, GUIContent.none, EditorStyles.toolbar);
		
		if (IsLoading)
		{
			GUI.enabled = wasGuiEnabled;
#if UNITY_2019_3_OR_NEWER
			return 21f;
#else
			return 18f;
#endif
		}
		
		toolbarRect.width -= 60f;
		
		if (EditorStyles.standardFont != standardFont)
		{
			standardFont = EditorStyles.standardFont;
			isInterFont = standardFont.name[0] == 'I';
		}

		var isOSX = Application.platform == RuntimePlatform.OSXEditor;
#if UNITY_2019_3_OR_NEWER
		var rc = new Rect(5f, toolbarRect.yMin, 25f, 20f);
#else
		var rc = new Rect(5f, toolbarRect.yMin, 25f, 17f);
#endif
		GUI.enabled = CanGoBack() && CanEdit();
		tempContent.image = null;
		tempContent.text = isInterFont ? "\u2190" : "\u25c4";
		tempContent.tooltip = SISettings.wordBreak_UseBothModifiers ?
			isOSX ? ("Go Back ⌥⌘←") : ("Go Back\n(Ctrl+Alt+Left)") :
			isOSX ? ("Go Back ⌥←") : ("Go Back\n(Alt+Left)");
		if (GUI.Button(rc, tempContent, EditorStyles.toolbarButton))
		{
			GoToRecentLocation(false);
		}
		rc.x += 25f;
		GUI.enabled = CanGoForward() && CanEdit();
		tempContent.text = isInterFont ? "\u2192" : "\u25ba";
		tempContent.tooltip = SISettings.wordBreak_UseBothModifiers ?
			isOSX ? ("Go Forward ⌥⌘→") : ("Go Forward\n(Ctrl+Alt+Right)") :
			isOSX ? ("Go Forward ⌥→") : ("Go Forward\n(Alt+Right)");
		if (GUI.Button(rc, tempContent, EditorStyles.toolbarButton))
		{
			GoToRecentLocation(true);
		}
		rc.x += 37f;

		GUI.enabled = CanEdit();
		
		if (caretPosition.line >= textBuffer.formattedLines.Count)
		{
			GUI.enabled = wasGuiEnabled;
#if UNITY_2019_3_OR_NEWER
			return 21f;
#else
			return 18f;
#endif
		}
		
		FGTextBuffer.FormattedLine ppLine = null;
		SyntaxToken token = null;
		var i = caretPosition.characterIndex;
		var formattedLines = textBuffer.formattedLines;
		var textBufferLines = textBuffer.lines;
		var totalLinesCount = textBufferLines.Count;
		for (var atLine = caretPosition.line; token == null && atLine < totalLinesCount; ++atLine)
		{
			var line = formattedLines[atLine];
			if (line == null || line.tokens == null)
			{
				GUI.enabled = wasGuiEnabled;
#if UNITY_2019_3_OR_NEWER
				return 21f;
#else
				return 18f;
#endif
			}

			var tokens = line.tokens;
			var tokensCount = tokens.Count;
			var tokenIndex = 0;
			while (i > 0 && tokenIndex < tokensCount - 1)
			{
				i -= tokens[tokenIndex].text.length;
				if (i > 0)
					++tokenIndex;
			}

			for ( ; tokenIndex < tokensCount; ++tokenIndex)
			{
				var t = tokens[tokenIndex];
				if (t.tokenKind > SyntaxToken.Kind.LastWSToken)
				{
					if (t.parent != null && t.parent.parent != null)
					{
						token = t;
					}
					break;
				}
				else if (ppLine == null && t.tokenKind >= SyntaxToken.Kind.Preprocessor
					&& t.tokenKind != SyntaxToken.Kind.VerbatimStringLiteral)
				{
					ppLine = line;
				}
			}
		}

		if (token == null)
		{
			for (var atLine = caretPosition.line - 1; token == null && atLine >= 0; --atLine)
			{
				var line = formattedLines[atLine];
				if (line == null || line.tokens == null)
				{
					GUI.enabled = wasGuiEnabled;
#if UNITY_2019_3_OR_NEWER
					return 21f;
#else
					return 18f;
#endif
				}

				var tokens = line.tokens;
				for (var tokenIndex = tokens.Count; tokenIndex --> 0; )
				{
					var t = tokens[tokenIndex];
					if (t.tokenKind > SyntaxToken.Kind.LastWSToken)
					{
						if (t.parent != null && t.parent.parent != null)
						{
							token = tokens[tokenIndex];
						}
						break;
					}
					else if (ppLine == null && t.tokenKind >= SyntaxToken.Kind.Preprocessor
						&& t.tokenKind != SyntaxToken.Kind.VerbatimStringLiteral)
					{
						ppLine = line;
					}
				}
			}
		}
		
		if (token != null)
		{
			SymbolDefinition symbol = null;
			if (token.parent != null)
			{
				var node = token.parent.parent;
				while (node != null)
				{
					var ruleName = node.RuleName;
					if (ruleName == "namespaceDeclaration" ||
						ruleName == "namespaceMemberDeclaration" ||
						ruleName == "classMemberDeclaration" ||
						ruleName == "structMemberDeclaration" ||
						ruleName == "interfaceMemberDeclaration" ||
						ruleName == "enumMemberDeclaration" ||
						ruleName == "delegateDeclaration" ||
						ruleName == "variableDeclarator" ||
						ruleName == "getAccessorDeclaration" ||
						ruleName == "readonlyAccessorDeclaration" ||
						ruleName == "setAccessorDeclaration" ||
						ruleName == "addAccessorDeclaration" ||
						ruleName == "removeAccessorDeclaration")
					{
						if (ruleName == "namespaceMemberDeclaration" ||
							ruleName == "classMemberDeclaration" ||
							ruleName == "structMemberDeclaration" ||
							ruleName == "interfaceMemberDeclaration")
						{
							node = node.NodeAt(-1);
							ruleName = node != null ? node.RuleName : null;
						}

						if (ruleName == "constructorDeclaration" ||
							ruleName == "destructorDeclaration" ||
							ruleName == "operatorDeclaration" ||
							ruleName == "conversionOperatorDeclaration")
						{
							if (node.declaration == null)
							{
								node = node.NodeAt(0);
								ruleName = node != null ? node.RuleName : null;
							}
						}
						else if (ruleName == "fieldDeclaration")
						{
							node = node.FindChildByName("variableDeclarators", "variableDeclarator") as ParseTree.Node;
						}
						else if (ruleName == "constantDeclaration")
						{
							node = node.FindChildByName("constantDeclarators", "constantDeclarator") as ParseTree.Node;
						}
						else if (ruleName == "eventDeclaration")
						{
							node = node.FindChildByName("eventWithAccessorsDeclaration") as ParseTree.Node
								?? node.FindChildByName("eventDeclarators", "eventDeclarator") as ParseTree.Node;
						}

						if (node != null && node.declaration != null && node.declaration.definition != null)
						{
							symbol = node.declaration.definition;
						}
						break;
					}
					node = node.parent;
				}
			}
			
			if (breadCrumbLeft == null)
			{
				breadCrumbLeft = "GUIEditor.BreadcrumbLeft";
				breadCrumbMid = "GUIEditor.BreadcrumbMid";
#if UNITY_2019_3_OR_NEWER
				breadcrumbLeftBg = "GUIEditor.BreadcrumbLeftBackground";
				breadcrumbMidBg = "GUIEditor.BreadcrumbMidBackground";
#endif

				if (breadCrumbLeft == null)
				{
					breadCrumbLeft = new GUIStyle(EditorStyles.toolbarButton);
					breadCrumbLeftOn = new GUIStyle(EditorStyles.toolbarButton);
					breadCrumbLeftOn.normal = breadCrumbLeftOn.onNormal;
					breadCrumbLeftOn.active = breadCrumbLeftOn.onActive;
					breadCrumbLeftOn.hover = breadCrumbLeftOn.onHover;
					breadCrumbLeftOn.focused = breadCrumbLeftOn.onFocused;

					breadCrumbMid = breadCrumbLeft;
					breadCrumbMidOn = breadCrumbLeftOn;
				}
				else
				{
					breadCrumbLeft = new GUIStyle(breadCrumbLeft);
					breadCrumbLeft.padding.top -= 1;
					breadCrumbLeft.padding.left = 0;
					breadCrumbLeft.alignment = TextAnchor.MiddleLeft;
					breadCrumbLeftOn = new GUIStyle(breadCrumbLeft);
					breadCrumbLeftOn.normal = breadCrumbLeftOn.onNormal;
					breadCrumbLeftOn.active = breadCrumbLeftOn.onActive;
					breadCrumbLeftOn.hover = breadCrumbLeftOn.onHover;
					breadCrumbLeftOn.focused = breadCrumbLeftOn.onFocused;
					breadCrumbLeft.onNormal = breadCrumbLeft.normal;
					breadCrumbLeft.onActive = breadCrumbLeft.active;
					breadCrumbLeft.onHover = breadCrumbLeft.hover;
					breadCrumbLeft.onFocused = breadCrumbLeft.focused;

					breadCrumbMid = new GUIStyle(breadCrumbMid);
					breadCrumbMid.padding.top -= 1;
					breadCrumbMid.alignment = TextAnchor.MiddleLeft;
					breadCrumbMidOn = new GUIStyle(breadCrumbMid);
					breadCrumbMidOn.normal = breadCrumbMidOn.onNormal;
					breadCrumbMidOn.active = breadCrumbMidOn.onActive;
					breadCrumbMidOn.hover = breadCrumbMidOn.onHover;
					breadCrumbMidOn.focused = breadCrumbMidOn.onFocused;
					breadCrumbMid.onNormal = breadCrumbMid.normal;
					breadCrumbMid.onActive = breadCrumbMid.active;
					breadCrumbMid.onHover = breadCrumbMid.hover;
					breadCrumbMid.onFocused = breadCrumbMid.focused;
				}
			}

			if (symbol != null && !symbol.IsValid())
				symbol = null;

			if (symbol != codePathSymbol || codePaths.Count == 0)
			{
				codePathSymbol = symbol;
				codePaths.Clear();

				if (symbol == null)
				{
					codePaths.Add(new CodePath { content = new GUIContent("...") });
				}
				else
				{
					while (symbol != null && symbol.name != "")
					{
						if (symbol.IsOperator)
						{
							bool addParameters;
							var name = ReadableOperatorName(symbol, out addParameters);
							if (addParameters)
							{
								var methodDef = symbol as MethodDefinition;
								if (methodDef != null)
								{
									name += " (" + methodDef.PrintParameters(methodDef.GetParameters(), true) + ")";
								}
							}
							
							var path = new CodePath
							{
								symbol = symbol,
								content = new GUIContent(" " + name, FGListPopup.GetSymbolIcon(symbol)),
								down = codePaths.Count == 0,
							};
							codePaths.Insert(0, path);
						}
						else if (symbol.kind != SymbolKind.MethodGroup)
						{
							var name = symbol.GetName();
							if (name == ".ctor" || name == ".cctor")
							{
								name = symbol.parentSymbol.GetName();
								if (name == ".ctor" || name == ".cctor")
									name = symbol.parentSymbol.parentSymbol.GetName();
							}
							if (symbol.kind == SymbolKind.Destructor)
								name = "~" + symbol.parentSymbol.GetName() + " ()";
							
							var methodDef = symbol as MethodDefinition;
							if (methodDef != null)
							{
								name += " (" + methodDef.PrintParameters(methodDef.GetParameters(), true) + ")";
							}
							
							var path = new CodePath
							{
								symbol = symbol,
								content = new GUIContent(" " + name, FGListPopup.GetSymbolIcon(symbol)),
								down = codePaths.Count == 0,
							};
							codePaths.Insert(0, path);
						}
						
						symbol = symbol.parentSymbol;
					}

					if (codePathSymbol.kind == SymbolKind.Class || codePathSymbol.kind == SymbolKind.Enum ||
						codePathSymbol.kind == SymbolKind.Interface || codePathSymbol.kind == SymbolKind.Namespace ||
						codePathSymbol.kind == SymbolKind.Struct)
					{
						codePaths.Add(new CodePath { content = new GUIContent("...") });
					}
				}
			}
			
			//var codeFocused = hasCodeViewFocus;

			var buttonStyle = breadCrumbLeft;
			var buttonStyleOn = breadCrumbLeftOn;
			var buttonStyleBg = breadcrumbLeftBg;
			for (var j = 0; j < codePaths.Count; ++j)
			{
				if (rc.x >= toolbarRect.xMax)
					break;
				
				var path = codePaths[j];
				var content = path.content;
				var size = buttonStyle.CalcSize(content);
				rc.width = size.x;
				if (rc.xMax > toolbarRect.xMax)
				{
					rc.xMax = toolbarRect.xMax;
					size.x = rc.width;
				}
				var modifiers = Event.current.modifiers & ~(EventModifiers.FunctionKey | EventModifiers.CapsLock | EventModifiers.Numeric);
				if (buttonStyleBg != null && Event.current.type == EventType.Repaint)
				{
					var hot = EditorGUIUtility.hotControl == 1326 + j;
					var active = EditorGUIUtility.hotControl == 0 || hot;
					var hover = rc.Contains(Event.current.mousePosition);
					buttonStyleBg.Draw(rc, hover != hot, active, path.down || hover && hot, false);
				}
				if (GUI.Button(rc, content, path.down ? buttonStyleOn : buttonStyle) ||
					j == codePaths.Count - 1 &&
					Event.current.type == EventType.KeyDown &&
					modifiers == (isOSX ? EventModifiers.Control : EventModifiers.Alt) &&
					Event.current.keyCode == KeyCode.M)
				{
					Event.current.Use();
					
					var selectedSymbol = path.symbol;

					if (!path.down && path.symbol != null)
					{
						GoToSymbol(selectedSymbol);
					}
					else
					{
						var menu = new GenericMenu();

						var compilationUnitRoot = textBuffer.Parser.parseTree.root;
						if (j == 0)
						{
							var declarations = new List<SymbolDeclaration>();
							EnumScopeDeclarations(compilationUnitRoot, declarations.Add);
							declarations.Sort((x, y) => x.Name.CompareTo(y.Name));
							
							var regionNames = new List<string>(declarations.Count);
							if (SISettings.navToolbarGroupByRegion)
							{
								var allRegions = new HashSet<string>();
								for (var d = 0; d < declarations.Count; d++)
								{
									regionNames.Add(null);
									
									var decl = declarations[d];
									var nameNode = decl == null ? null : decl.NameNode();
									if (nameNode != null)
									{
										var firstLeaf = nameNode.GetFirstLeaf();
										if (firstLeaf != null)
										{
											var regionName = firstLeaf.token.formattedLine.GetRegionName();
											if (!string.IsNullOrEmpty(regionName))
											{
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
												regionName = regionName.Replace('_', '\xFF3F');
#endif
												regionName = " " + regionName;
												regionNames[d] = regionName;
												allRegions.Add(regionName);
											}
										}
									}
								}
								var insertAt = 0;
								foreach (var r in from x in allRegions orderby x select x)
								{
									var index = regionNames.IndexOf(r);
									
									var d = declarations[index];
									declarations.RemoveAt(index);
									declarations.Insert(insertAt, d);
									
									regionNames.RemoveAt(index);
									regionNames.Insert(insertAt, r);
									
									++insertAt;
								}
							}
							
							for (var d = 0; d < declarations.Count; ++d)
							{
								var decl = declarations[d];
								if (decl.kind == SymbolKind.LambdaExpression)
									continue;
								
								var name = decl.Name;
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
								name = name.Replace('_', '\xFF3F');
#endif
								
								if (SISettings.navToolbarGroupByRegion)
								{
									var regionName = regionNames[d];
									if (!string.IsNullOrEmpty(regionName))
										name = regionName + "/" + name;
								}
								
								bool isCurrentSymbol = decl.definition == codePaths[0].symbol;
								menu.AddItem(new GUIContent(name), isCurrentSymbol, () => GoToSymbolDeclaration(decl));
							}
						}
						else
						{
							var members = codePaths[j - 1].symbol.members;
							var symbols = new List<SymbolDefinition>(members.Count);
							for (var memberIndex = 0; memberIndex < members.Count; ++memberIndex)
							{
								var m = members[memberIndex];
								if (m.kind == SymbolKind.MethodGroup)
								{
									var methodGroup = (MethodGroupDefinition) m;
									foreach (var method in methodGroup.methods)
									{
										for (var decl = method.declarations; decl != null; )
										{
											var next = decl.next;
											if (decl.IsValid() && compilationUnitRoot.IsAncestorOf(decl.parseTreeNode))
												symbols.Add(method);
											decl = next;
										}
									}
								}
								else if (m.kind == SymbolKind.LambdaExpression)
								{
									continue;
								}
								else
								{
									for (var decl = m.declarations; decl != null; )
									{
										var next = decl.next;
										if (decl.IsValid() && compilationUnitRoot.IsAncestorOf(decl.parseTreeNode))
											symbols.Add(m);
										decl = next;
									}
								}
							}
							
							if (SISettings.navToolbarSortByName)
							{
								symbols.Sort((x, y) => {
									var xName = x.GetName();
									var yName = y.GetName();
									if (xName == ".ctor" || xName == ".cctor")
										xName = x.parentSymbol.kind == SymbolKind.MethodGroup ? x.parentSymbol.parentSymbol.GetName() : x.parentSymbol.GetName();
									else if (x.kind == SymbolKind.Destructor)
										xName = "~" + x.parentSymbol.GetName();
									if (yName == ".ctor" || xName == ".cctor")
										yName = y.parentSymbol.kind == SymbolKind.MethodGroup ? y.parentSymbol.parentSymbol.GetName() : y.parentSymbol.GetName();
									else if (y.kind == SymbolKind.Destructor)
										yName = "~" + y.parentSymbol.GetName();
									return xName.CompareTo(yName);
								});
							}
							else
							{
								symbols.Sort((x, y) => {
									var xDecl = x.FindFirstValidDeclaration();
									if (xDecl == null)
										return 1;
									var yDecl = y.FindFirstValidDeclaration();
									if (yDecl == null)
										return -1;
									var xToken = xDecl.parseTreeNode.GetFirstLeaf().token;
									var yToken = yDecl.parseTreeNode.GetFirstLeaf().token;
									var xLine = xToken.Line;
									var yLine = yToken.Line;
									return xLine != yLine ? xLine.CompareTo(yLine) :
										xToken.TokenIndex.CompareTo(yToken.TokenIndex);
								});
							}
							
							var insertAt = 0;
							
							var regionNames = new List<string>(symbols.Count);
							if (SISettings.navToolbarGroupByRegion)
							{
								var allRegions = new HashSet<string>();
								for (var s = 0; s < symbols.Count; s++)
								{
									regionNames.Add(null);
									
									var target = symbols[s];
									var decl = target.declarations;
									var firstLeaf = decl == null || decl.parseTreeNode == null ? null : decl.parseTreeNode.GetFirstLeaf();
									if (firstLeaf != null)
									{
										var regionName = firstLeaf.token.formattedLine.GetRegionName();
										if (!string.IsNullOrEmpty(regionName))
										{
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
											regionName = regionName.Replace('_', '\xFF3F');
#endif
											regionName = " " + regionName;
											regionNames[s] = regionName;
											allRegions.Add(regionName);
										}
									}
								}
								foreach (var r in from x in allRegions orderby x select x)
								{
									var index = regionNames.IndexOf(r);
									
									var s = symbols[index];
									symbols.RemoveAt(index);
									symbols.Insert(insertAt, s);
									
									regionNames.RemoveAt(index);
									regionNames.Insert(insertAt, r);
									
									++insertAt;
								}
							}
							
							//if (SISettings.navToolbarGroupNonMethods &&
							//	codePaths[j-1].symbol is TypeDefinitionBase &&
							//	!codePaths[j-1].symbol.kind == SymbolKind.Enum)
							//{
							//	var nestedTypeIndex = -1;
							//	while ((nestedTypeIndex = members.FindIndex(insertAt, m => m is TypeDefinitionBase)) != -1)
							//	{
							//		if (string.IsNullOrEmpty(regionNames[nestedTypeIndex]))
							//			break;
									
							//	}
							//	var constantIndex = members.FindIndex(m => m.kind == SymbolKind.ConstantField);
							//	var fieldIndex = members.FindIndex(m => m.kind == SymbolKind.Field);
							//	var propertyIndex = members.FindIndex(m => m.kind == SymbolKind.Property ||
							//		m.kind == SymbolKind.Indexer || m.kind == SymbolKind.Event);
							//	var operatorIndex = members.FindIndex(m => m.kind == SymbolKind.Operator);
								
							//	foreach (var r in from x in allRegions orderby x select x)
							//	{
							//		var index = regionNames.IndexOf(r);
									
							//		var s = symbols[index];
							//		symbols.RemoveAt(index);
							//		symbols.Insert(insertAt, s);
									
							//		regionNames.RemoveAt(index);
							//		regionNames.Insert(insertAt, r);
									
							//		++insertAt;
							//	}
							//}
							
							for (var s = 0; s < symbols.Count; s++)
							{
								var target = symbols[s];
								var name = target.GetName();
								if (name == ".ctor" || name == ".cctor")
									name = target.parentSymbol.kind == SymbolKind.MethodGroup ? target.parentSymbol.parentSymbol.GetName() : target.parentSymbol.GetName();
								else if (target.kind == SymbolKind.Destructor)
									name = "~" + target.parentSymbol.GetName() + " ()";
								
								var methodDef = target as MethodDefinition;
								if (methodDef != null)
								{
									bool addParameters = true;
									if (methodDef.IsOperator)
										name = ReadableOperatorName(methodDef, out addParameters);
									
									if (addParameters)
										name += " (" + methodDef.PrintParameters(methodDef.GetParameters(), true) + ")";
								}
								
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
								name = name.Replace('_', '\xFF3F');
#endif
								
								if (SISettings.navToolbarGroupByRegion)
								{
									var regionName = regionNames[s];
									if (!string.IsNullOrEmpty(regionName))
										name = regionName + "/" + name;
								}
								
								bool isCurrentSymbol = target == codePaths[j].symbol;
								menu.AddItem(new GUIContent(name), isCurrentSymbol, () => GoToSymbol(target));
							}
						}

						rc.x -= buttonStyle.overflow.left;
						rc.width = 1f;
						menu.DropDown(rc);
					}
				}
				rc.x += size.x;

				buttonStyle = breadCrumbMid;
				buttonStyleOn = breadCrumbMidOn;
				buttonStyleBg = breadcrumbMidBg;
			}
		}
		
		var caretLine = textBuffer.formattedLines[caretPosition.line];
		var region = caretLine.regionTree;
		if (ppLine == caretLine && caretPosition.line > 0)
		{
			for (var j = ppLine.tokens.Count; j --> 0; )
			{
				if (ppLine.tokens[j].tokenKind == SyntaxToken.Kind.Preprocessor && ppLine.tokens[j].text == "endregion")
				{
					region = textBuffer.formattedLines[caretPosition.line - 1].regionTree;
					break;
				}
			}
		}
		while (region.parent != null &&
			region.kind != FGTextBuffer.RegionTree.Kind.Region &&
			region.kind != FGTextBuffer.RegionTree.Kind.InactiveRegion)
		{
			region = region.parent;
		}
		if (region.parent != null)
			ppLine = region.line;
		else
			ppLine = null;
		var regionContent = ppLine != null ? cachedContentRegion : cachedContentNoRegion;
		if (ppLine != null)
		{
			for (var j = ppLine.tokens.Count; j --> 0; )
			{
				if (ppLine.tokens[j].tokenKind == SyntaxToken.Kind.PreprocessorArguments)
				{
					var regionName = ppLine.tokens[j].text;
					if (cachedContentRegion.text.Length != "#region ".Length + regionName.length ||
						!cachedContentRegion.text.FastEndsWith(regionName))
					{
						cachedContentRegion.text = "#region " + regionName;
					}
					break;
				}
			}
		}
		
		toolbarRect.xMax += 60f;
		
		rc.x += 3f;
		rc.xMax = toolbarRect.xMax - 6f;
		var regionButtonSize = EditorStyles.toolbarDropDown.CalcSize(regionContent);
		if (regionButtonSize.x < rc.width)
			rc.xMin = rc.xMax - regionButtonSize.x;
		if (GUI.Button(rc, regionContent, EditorStyles.toolbarDropDown))
		{
			var allRegions = new List<FGTextBuffer.RegionTree>();
			ListAllRegions(textBuffer.rootRegion, allRegions);
			if (!SISettings.sortRegionsByName)
				allRegions.Sort((a, b) => a.line.index.CompareTo(b.line.index));
			
			var names = new string[allRegions.Count];
			for (var j = allRegions.Count; j --> 0; )
			{
				names[j] = "";
				
				var r = allRegions[j];
				var tokens = r.line.tokens;
				for (var k = 0; k < tokens.Count; ++k)
				{
					if (tokens[k].tokenKind == SyntaxToken.Kind.PreprocessorArguments)
					{
						names[j] = tokens[k].text;
						break;
					}
				}
			}
			var regions = allRegions.ToArray();
			if (SISettings.sortRegionsByName)
				Array.Sort(names, regions);
			var menu = new GenericMenu();
			for (var j = 0; j < names.Length; ++j)
			{
				menu.AddItem(new GUIContent(names[j]),
					ppLine != null && regions[j] == ppLine.regionTree,
					x => GoToRegion((FGTextBuffer.RegionTree)x), regions[j]);
			}
			if (names.Length > 0)
			{
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Sort by name"), SISettings.sortRegionsByName,
					() => {SISettings.sortRegionsByName.Toggle();});
			}
			else
			{
				menu.AddDisabledItem(new GUIContent("No regions"));
			}
			menu.DropDown(rc);
		}
			
		GUI.enabled = wasGuiEnabled;
#if UNITY_2019_3_OR_NEWER
		return 21f;
#else
		return 18f;
#endif
	}
	
	private string ReadableOperatorName(SymbolDefinition symbol, out bool addParameters)
	{
		addParameters = true;
		var name = symbol.GetName();
		
		switch (name)
		{
		case "op_Implicit":
			//addParameters = false;
			name = "implicit operator " + symbol.TypeOf().GetName();
			break;
		case "op_Explicit":
			//addParameters = false;
			name = "explicit operator " + symbol.TypeOf().GetName();
			break;
		case "op_Addition":
			name = "operator +";
			break;
		case "op_Subtraction":
			name = "operator -";
			break;
		case "op_Multiply":
			name = "operator *";
			break;
		case "op_Division":
			name = "operator /";
			break;
		case "op_Modulus":
			name = "operator %";
			break;
		case "op_ExclusiveOr":
			name = "operator ^";
			break;
		case "op_BitwiseAnd":
			name = "operator &";
			break;
		case "op_BitwiseOr":
			name = "operator |";
			break;
		case "op_LogicalAnd":
			name = "operator &&";
			break;
		case "op_LogicalOr":
			name = "operator ||";
			break;
		case "op_Assign":
			name = "operator =";
			break;
		case "op_LeftShift":
			name = "operator <<";
			break;
		case "op_RightShift":
		case "op_SignedRightShift":
		case "op_UnsignedRightShift":
			name = "operator >>";
			break;
		case "op_Equality":
			name = "operator ==";
			break;
		case "op_GreaterThan":
			name = "operator >";
			break;
		case "op_LessThan":
			name = "operator <";
			break;
		case "op_Inequality":
			name = "operator !=";
			break;
		case "op_GreaterThanOrEqual":
			name = "operator >=";
			break;
		case "op_LessThanOrEqual":
			name = "operator <=";
			break;
		case "op_Decrement":
			name = "operator --";
			break;
		case "op_Increment":
			name = "operator ++";
			break;
		case "op_UnaryNegation":
			addParameters = false;
			name = "operator -";
			break;
		case "op_UnaryPlus":
			addParameters = false;
			name = "operator +";
			break;
		case "op_OnesComplement":
			addParameters = false;
			name = "operator ~";
			break;
		case "op_LogicalNot":
			addParameters = false;
			name = "operator !";
			break;
		case "op_True":
			addParameters = false;
			name = "operator true";
			break;
		case "op_False":
			addParameters = false;
			name = "operator false";
			break;
		case "op_MemberSelection":
		case "op_PointerToMemberSelection":
		case "op_AddressOf":
		case "op_PointerDereference":
		default:
			name = "operator UNKNOWN";
			break;
		}
		
		return name;
	}
	
	private void GoToRegion(FGTextBuffer.RegionTree region)
	{
		var line = region.line.index;
		if (line >= 0 && line < textBuffer.lines.Count && textBuffer.formattedLines[line] == region.line)
		{
			var tokens = region.line.tokens;
			for (var i = 0; i < tokens.Count; ++i)
			{
				if (tokens[i].tokenKind == SyntaxToken.Kind.Preprocessor /*&& tokens[i].text == "region"*/)
				{
					AddRecentLocation(0, true);
					
					var span = textBuffer.GetTokenSpan(line, i);
					UnfoldTextRange(line, span.index, line, span.index);
					
					PingText(new FGTextBuffer.CaretPos{ line = line, characterIndex = span.index }, 0, yellowPingColor);
					break;
				}
			}
		}
	}
	
	private void ListAllRegions(FGTextBuffer.RegionTree root, List<FGTextBuffer.RegionTree> regions)
	{
		if (root.children == null)
			return;
		var numLines = textBuffer.lines.Count;
		for (var i = root.children.Count; i --> 0; )
		{
			var r = root.children[i];
			var line = r.line;
			var lineIndex = line.index;
			if (lineIndex < 0 || lineIndex >= numLines || textBuffer.formattedLines[lineIndex] != line)
			{
				root.children.RemoveAt(i);
				continue;
			}
			
			var tokens = r.line.tokens;
			for (var j = 0; j < tokens.Count; ++j)
			{
				var t = tokens[j];
				if (t.tokenKind == SyntaxToken.Kind.Preprocessor && t.text != "#")
				{
					if (t.text == "region")
						regions.Add(r);
					ListAllRegions(r, regions);
					break;
				}
				if (t.tokenKind > SyntaxToken.Kind.Preprocessor)
					break;
			}
		}
		if (root.children.Count == 0)
			root.children = null;
	}

	private static void EnumScopeDeclarations(ParseTree.Node root, Action<SymbolDeclaration> action)
	{
		for (var i = 0; i < root.numValidNodes; ++i)
		{
			var node = root.NodeAt(i);
			if (node == null)
				continue;
			var flags = node.semantics;
			var decl = flags & SemanticFlags.SymbolDeclarationsMask;
			if (node.declaration != null &&
				(decl == SemanticFlags.ExternAlias || decl == SemanticFlags.NamespaceDeclaration ||
				decl == SemanticFlags.ClassDeclaration || decl == SemanticFlags.StructDeclaration ||
				decl == SemanticFlags.InterfaceDeclaration || decl == SemanticFlags.EnumDeclaration ||
				decl == SemanticFlags.DelegateDeclaration))
			{
				action.Invoke(node.declaration);
			}
			if ((flags & SemanticFlags.ScopesMask) == 0)
				EnumScopeDeclarations(node, action);
		}
	}
	
	public void GoToSymbol(SymbolDefinition symbol)
	{
		var declarations = symbol.declarations;
		if (declarations == null)
			declarations = FGFindInFiles.FindDeclarations(symbol);
		if (declarations == null)
			return;
		
		for (var declaration = declarations; declaration != null; )
		{
			var next = declaration.next;
			if (declaration.IsValid())
			{
				GoToSymbolDeclaration(declaration);
				break;
			}
			declaration = next;
		}
	}
	
	public bool IsValidSymbolDeclaration(SymbolDeclaration declaration)
	{
		var node = declaration.NameNode();
		if (node == null)
			return false;
		
		string cuPath = null;
		for (var scope = declaration.scope; scope != null; scope = scope.parentScope)
		{
			var cuScope = scope as CompilationUnitScope;
			if (cuScope != null)
			{
				cuPath = cuScope.path;
				break;
			}
		}
		if (cuPath == null)
			return false;
		
		var cuObject = AssetDatabase.LoadAssetAtPath(cuPath, typeof(MonoScript));
		if (cuObject == null)
			return false;
		
		var buffer = FGTextBufferManager.GetBuffer(cuObject);
		if (buffer == null)
			return false;
		
		return true;
	}
	
	public static bool GoToSymbolDeclaration(SymbolDeclaration declaration, out string assetGuid, out TextSpan textSpan)
	{
		assetGuid = null;
		textSpan = new TextSpan();
		
		ParseTree.BaseNode node = declaration.NameNode() ?? declaration.parseTreeNode;
		if (node == null || !node.HasLeafs())
			return false;

		string cuPath = null;
		for (var scope = declaration.scope; scope != null; scope = scope.parentScope)
		{
			var cuScope = scope as CompilationUnitScope;
			if (cuScope != null)
			{
				cuPath = cuScope.path;
				break;
			}
		}
		if (cuPath == null)
		{
			Debug.Log("Source code for '" + node.Print() + "' is not available.");
			return false;
		}

		string cuGuid = AssetDatabase.AssetPathToGUID(cuPath);
        if (string.IsNullOrEmpty(cuGuid))
            cuGuid = cuPath;
		var buffer = FGTextBufferManager.GetBuffer(cuGuid);
		if (buffer == null)
		{
			Debug.Log("Error: Failed to load " + cuPath);
			return false;
		}

		if (buffer.IsLoading)
		{
			buffer.LoadImmediately();
		}
		
		var span = buffer.GetParseTreeNodeSpan(node);

		textSpan = span;
		assetGuid = cuGuid;

		EditorApplication.delayCall += () =>
		{
			FGCodeWindow.OpenAssetInTab(AssetDatabase.AssetPathToGUID(cuPath), span.line + 1);
			nextF12GoesBack = true;
		};
		
		return true;
	}
	
	public bool GoToSymbolDeclaration(SymbolDeclaration declaration)
	{
		ParseTree.BaseNode node = declaration.NameNode() ?? declaration.parseTreeNode;
		if (node == null || !node.HasLeafs())
			return false;

		string cuPath = null;
		for (var scope = declaration.scope; scope != null; scope = scope.parentScope)
		{
			var cuScope = scope as CompilationUnitScope;
			if (cuScope != null)
			{
				cuPath = cuScope.path;
				break;
			}
		}
		if (cuPath == null)
		{
			Debug.Log("Source code for '" + node.Print() + "' is not available.");
			return false;
		}

		string cuGuid = AssetDatabase.AssetPathToGUID(cuPath);
        if (string.IsNullOrEmpty(cuGuid))
            cuGuid = cuPath;
		var buffer = FGTextBufferManager.GetBuffer(cuGuid);
		if (buffer == null)
		{
			Debug.Log("Error: Failed to load " + cuPath);
			return false;
		}

		if (buffer.IsLoading && cuGuid != cuPath)
		{
			buffer.LoadImmediately();
		}
		
		AddRecentLocation(0, true);

		var span = buffer.GetParseTreeNodeSpan(node);
		if (buffer == textBuffer)
		{
			var startAtColumn = CharIndexToColumn(span.index, span.line);
			var declarationPosition = new FGTextBuffer.CaretPos
			{
				characterIndex = span.index,
				column = startAtColumn,
				virtualColumn = startAtColumn,
				line = span.line
			};
			PingText(declarationPosition, span.lineOffset == 0 ? span.indexOffset : buffer.lines[span.line].Length - span.index, yellowPingColor);
		}
		else
		{
			EditorApplication.delayCall += () =>
			{
				FGCodeWindow.OpenAssetInTab(AssetDatabase.AssetPathToGUID(cuPath), span.line + 1);
				nextF12GoesBack = true;
			};
		}
		
		return true;
	}
	
	private void AddGoToTypeDefinitionMenuItems(GenericMenu menu, TypeDefinitionBase symbolType)
	{
		AddGoToTypeDefinitionMenuItems(menu, null, symbolType);
	}
	
	private void AddGoToTypeDefinitionMenuItems(GenericMenu menu, string parentItem, TypeDefinitionBase symbolType)
	{
		if (symbolType == null)
			return;
		
		var asArrayType = symbolType as ArrayTypeDefinition;
		if (asArrayType != null)
			symbolType = asArrayType.elementType.definition as TypeDefinitionBase;
		
		if (symbolType == null)
			return;
		
		var assembly = symbolType.Assembly;
		if (assembly == null && symbolType.declarations != null)
			assembly = ((CompilationUnitScope) textBuffer.Parser.parseTree.root.scope).assembly;
		if (assembly == null)
			return;
		
		var asConstructedType = symbolType as ConstructedTypeDefinition;
		if (asConstructedType != null && asConstructedType.typeArguments != null)
		{
			foreach (var t in asConstructedType.typeArguments)
			{
				var typeDef = t.definition as TypeDefinitionBase;
				if (typeDef != null)
				{
					parentItem = "Go To Type Definition/";
					AddGoToTypeDefinitionMenuItems(menu, parentItem, typeDef);
				}
			}
		}
		
		var assemblyName = assembly.AssemblyName;
		if (assemblyName == "mscorlib" || assemblyName == "system" || assemblyName.FastStartsWith("system."))
		{
			var input = symbolType.XmlDocsName;
			if (input != null)
			{
				const int digits = 16;
				var md5 = System.Security.Cryptography.MD5.Create();
				var bytes = Encoding.UTF8.GetBytes(input);
				var hashBytes = md5.ComputeHash(bytes);
				
				var c = new char[digits];
				byte b;
				for (var i = 0; i < digits / 2; ++i)
				{
					b = ((byte) (hashBytes[i] >> 4));
					c[i * 2] = (char) (b > 9 ? b + 87 : b + 0x30);
					b = ((byte) (hashBytes[i] & 0xF));
					c[i * 2 + 1] = (char) (b > 9 ? b + 87 : b + 0x30);
				}
				
				var text = "Go To Type Definition (.Net)";
				if (parentItem != null)
					text = parentItem + symbolType.name.Replace('_', '\xFF3F') + " (.Net)";
				
				menu.AddItem(
					new GUIContent(text),
					false,
					() => Help.BrowseURL("http://referencesource.microsoft.com/mscorlib/a.html#" + new string(c)));
			}
		}
		else if (assembly.fromCsScripts)
		{
			var declarations = symbolType.declarations;
			if (declarations == null)
			{
				declarations = FGFindInFiles.FindDeclarations(symbolType);
				//symbolType = symbolType.Rebind() as TypeDefinitionBase;
				//if (symbolType == null)
				//	return;
			}
			
			var validDeclarations = new List<SymbolDeclaration>();
			if (declarations != null)
			{
				for (var decl = declarations; decl != null; )
				{
					var next = decl.next;
					if (IsValidSymbolDeclaration(decl))
						validDeclarations.Add(decl);
					decl = next;
				}
			}
			
			if (validDeclarations.Count == 1)
			{
				var text = "Go To Type Definition";
				if (parentItem != null)
					text = parentItem + symbolType.name.Replace('_', '\xFF3F');
				
				menu.AddItem(
					new GUIContent(text),
					false,
					() => GoToSymbolDeclaration(validDeclarations[0]));
			}
			else if (validDeclarations.Count > 0)
			{
				var text = "Go To Type Definition/";
				if (parentItem != null)
					text += parentItem + symbolType.name.Replace('_', '\xFF3F') + "/";
				
				foreach (var decl in validDeclarations)
				{
					var co = decl.scope;
					while (co.parentScope != null)
						co = co.parentScope;
					var fileName = ((CompilationUnitScope) co).path;
					fileName = AssetDatabase.AssetPathToGUID(fileName);
					fileName = AssetDatabase.GUIDToAssetPath(fileName);
					fileName = Path.GetFileName(fileName);
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
					fileName = fileName.Replace('_', '\xFF3F');
#endif
					var nameNode = decl.NameNode();
					var firstLeaf = nameNode as ParseTree.Leaf ?? (nameNode as ParseTree.Node).GetFirstLeaf();
					
					menu.AddItem(
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
						new GUIContent(text + fileName + " : " + (firstLeaf.token.Line + 1) + " _"),
#else
						new GUIContent(text + fileName + " : " + (firstLeaf.token.Line + 1)),
#endif
						false,
						d => GoToSymbolDeclaration((SymbolDeclaration) d),
						decl);
				}
			}
		}
	}

	private class P4MenuItem
	{
		public string menuItem;
		public int index;
		public int priority;
		public MethodInfo validateMethod;
		public MethodInfo executeMethod;
	}
	private static Type p4MenusType = Type.GetType("P4Connect.Menus,P4Connect");
	private static MethodInfo[] p4MenusMethods;
	private static PropertyInfo p4ValidConfigurationProperty;
	
	private GUIContent popOutButtonContent = new GUIContent();
	private GUIContent hotReloadButtonContent = new GUIContent();
	private GUIContent saveButtonContent = new GUIContent();
	private GUIContent buildButtonContent = new GUIContent();
	private GUIContent buildButtonContent2 = new GUIContent();
	private GUIContent undoButtonContent = new GUIContent();
	private GUIContent redoButtonContent = new GUIContent();
	private GUIContent toggleCommentButtonContent = new GUIContent();
	
	private static GUIStyle toolbarDropDownToggleStyle;

	public static bool DropDownToggle(ref bool toggled, Rect toggleRect, GUIContent content)
	{
		Rect arrowRightRect = new Rect(toggleRect.xMax - EditorStyles.toolbarDropDown.padding.right, toggleRect.y, EditorStyles.toolbarDropDown.padding.right, toggleRect.height);

		var guiWasEnabled = GUI.enabled;
		GUI.enabled = true;
		var clicked = EditorGUI.DropdownButton(arrowRightRect, GUIContent.none, FocusType.Passive, EditorStyles.toolbarButton);
		GUI.enabled = guiWasEnabled;

		if (!clicked)
		{
			toggled = GUI.Toggle(toggleRect, toggled, content, EditorStyles.toolbarDropDown);
		}
		
		if (Event.current.type == EventType.Repaint)
		{
			GUI.enabled = true;
			var isHover = arrowRightRect.Contains(Event.current.mousePosition);
			EditorStyles.toolbarDropDown.Draw(arrowRightRect, isHover, Event.current.button == 1, false, isHover);
			GUI.enabled = guiWasEnabled;
		}

		return clicked;
	}
	
	private float DoToolbar()
	{
		bool isOSX = Application.platform == RuntimePlatform.OSXEditor;

		if (!saveButtonContent.image)
		{
			string targetName = System.IO.Path.GetFileName(targetPath);

			if (isOSX)
			{
				popOutButtonContent = new GUIContent(popOutIcon, "New Tab ⌘T");
				saveButtonContent = new GUIContent(saveIcon, "Save " + targetName + " ⌘S");
				buildButtonContent = new GUIContent(buildIcon, "Build Assemblies ⌘S");
				buildButtonContent2 = new GUIContent(buildIcon, "Build Assemblies ⌥⌘R");
				undoButtonContent = new GUIContent(undoIcon, "Undo ⌘Z");
				redoButtonContent = new GUIContent(redoIcon, "Redo ⇧⌘Z");
				toggleCommentButtonContent = new GUIContent(textBuffer.isBooFile ? "#..." : "//...", "Toggle Comment Selection\n⌘K or ⌘/");
			}
			else
			{
				popOutButtonContent = new GUIContent(popOutIcon, "New Tab\n(Ctrl+T)");
				saveButtonContent = new GUIContent(saveIcon, "Save " + targetName + "\n(Ctrl+S)");
				buildButtonContent = new GUIContent(buildIcon, "Build Assemblies\n(Ctrl+S)");
				buildButtonContent2 = new GUIContent(buildIcon, "Build Assemblies\n(Ctrl+R)");
				undoButtonContent = new GUIContent(undoIcon, "Undo\n(Ctrl+Z)");
				redoButtonContent = new GUIContent(redoIcon, "Redo\n(Ctrl+Shift+Z)");
				toggleCommentButtonContent = new GUIContent(textBuffer.isBooFile ? "#..." : "//...", "Toggle Comment Selection\n(Ctrl+K or Ctrl+/)");
			}
		}

		Vector2 contentSize = EditorStyles.toolbarButton.CalcSize(popOutButtonContent);

#if UNITY_2019_3_OR_NEWER
		if (toolbarDropDownToggleStyle == null)
			toolbarDropDownToggleStyle = "toolbarDropDownToggle";

		var height = contentSize.y + 1f;
		scrollViewRect.yMin = scrollViewRect.yMin + height - 18f;
		Rect toolbarRect = new Rect(scrollViewRect.xMin, scrollViewRect.yMin - height, scrollViewRect.width, height + 1f);
#else
		var height = 18f;
		Rect toolbarRect = new Rect(scrollViewRect.xMin, scrollViewRect.yMin - 18f, scrollViewRect.width, 17f);
#endif
		
		if (Event.current.type == EventType.Layout)
			return height;
		
		GUI.enabled = true;
		GUI.Label(toolbarRect, GUIContent.none, EditorStyles.toolbar);

		Color oldColor = GUI.color;
		GUI.contentColor = EditorStyles.toolbarButton.normal.textColor;

		Rect rc = new Rect(toolbarRect.xMin + 6f, toolbarRect.yMin, contentSize.x, contentSize.y);
		if (GUI.Button(rc, popOutButtonContent, EditorStyles.toolbarButton))
		{
			EditorApplication.delayCall += OpenInNewTab;
			return height;
		}
		
		bool isPatched = false;
		string hotReloadStatusText = null;
		
		if (textBuffer != null && textBuffer.isCsFile)
		{
			var hotReloadIcon = HotReloadIntegration.Icon;
			
			if (hotReloadIcon != null)
			{
				hotReloadButtonContent.image = hotReloadIcon;
				hotReloadButtonContent.text = null;
				hotReloadStatusText = HotReloadIntegration.Text;
				hotReloadButtonContent.tooltip = hotReloadStatusText;
				
				//contentSize = EditorStyles.toolbarButton.CalcSize(hotReloadButtonContent);
				contentSize.x = contentSize.y + 1f;
				
				GUI.contentColor = oldColor;
				rc = new Rect(rc.xMax + 6f, toolbarRect.yMin, contentSize.x, contentSize.y);
				if (GUI.Button(rc, hotReloadButtonContent, EditorStyles.toolbarButton))
				{
					// First, try to open the latest version of Hot Reload.
					if (!EditorApplication.ExecuteMenuItem("Window/Hot Reload/Open"))
					{
						// If that fails, try the older version.
						EditorApplication.ExecuteMenuItem("Window/Hot Reload");
					}
				}
				GUI.contentColor = EditorStyles.toolbarButton.normal.textColor;
				rc.x -= 6f;
				
				isPatched = hotReloadStatusText == "Hot Reload:\nAll patches applied"
					|| hotReloadStatusText == "Hot Reload:\nReload finished";
			}
		}
		
		var enableBuildIcon = !IsModified && FGTextBufferManager.HasPendingAssetImports;
		var enableReload = !EditorApplication.isCompiling && !FGTextBufferManager.IsReloadingAssemblies;
		
		GUI.enabled = CanEdit() && (IsModified || enableBuildIcon && enableReload);
		
		if (textBuffer != null && textBuffer.isCsFile && toolbarDropDownToggleStyle != null)
		{
			contentSize = toolbarDropDownToggleStyle.CalcSize(saveButtonContent);
		 	
			rc.Set(rc.xMax + 6f, toolbarRect.yMin, contentSize.x, contentSize.y);
			
			bool toggled = false;
			if (DropDownToggle(ref toggled, rc, enableBuildIcon ? (SISettings.compileOnDoubleSave ? buildButtonContent : buildButtonContent2) : saveButtonContent))
			{
				GenericMenu menu = new GenericMenu();
				
				if (SISettings.compileOnDoubleSave || !enableBuildIcon) 
				{
					if (GUI.enabled)
						menu.AddItem(new GUIContent(enableBuildIcon ? "Save All, Compile, and Reload _%s" : "Save _%s"), false, SaveBuffer);
					else
						menu.AddItem(new GUIContent(enableBuildIcon ? "Save All, Compile, and Reload _%s" : "Save _%s"), false, null);
				}
			
#if UNITY_EDITOR_OSX
				if (enableReload)
					menu.AddItem(new GUIContent("Save All, Compile, and Reload _&%r"), false, MenuReloadAssemblies);
				else
					menu.AddItem(new GUIContent("Save All, Compile, and Reload _&%r"), false, null);
#else
				if (enableReload)
					menu.AddItem(new GUIContent("Save All, Compile, and Reload _%r"), false, MenuReloadAssemblies);
				else
					menu.AddItem(new GUIContent("Save All, Compile, and Reload _%r"), false, null);
#endif
				menu.AddSeparator(string.Empty);
				if (HotReloadIntegration.Active)
					menu.AddDisabledItem(new GUIContent("Compile and Reload on Save"));
				else
					menu.AddItem(new GUIContent("Compile and Reload on Save"), SISettings.compileOnSave, () => SISettings.compileOnSave.Toggle());
				if (HotReloadIntegration.Active || !SISettings.compileOnSave)
					menu.AddItem(new GUIContent("Save Twice to Compile and Reload"), SISettings.compileOnDoubleSave, () => SISettings.compileOnDoubleSave.Toggle());
				else
					menu.AddDisabledItem(new GUIContent("Save Twice to Compile and Reload"));
				menu.DropDown(rc);
			}
			else if (toggled)
			{
				if (enableReload)
					MenuReloadAssemblies();
				else
					SaveBuffer();
			}
		}
		else
		{
			contentSize = EditorStyles.toolbarButton.CalcSize(saveButtonContent);
			rc.Set(rc.xMax + 6f, toolbarRect.yMin, contentSize.x, contentSize.y);
			if (GUI.Button(rc, enableBuildIcon ? (SISettings.compileOnDoubleSave ? buildButtonContent : buildButtonContent2) : saveButtonContent, EditorStyles.toolbarButton))
			{
				if (enableReload)
					MenuReloadAssemblies();
				else
					SaveBuffer();
			}
		}

		GUI.enabled = CanUndo();
		contentSize = EditorStyles.toolbarButton.CalcSize(undoButtonContent);
		rc = new Rect(rc.xMax + 6f, toolbarRect.yMin, contentSize.x, contentSize.y);
		if (GUI.Button(rc, undoButtonContent, EditorStyles.toolbarButton))
		{
			focusCodeView = true;
			Undo();
		}

		GUI.enabled = CanRedo();
		contentSize = EditorStyles.toolbarButton.CalcSize(redoButtonContent);
		rc = new Rect(rc.xMax, toolbarRect.yMin, contentSize.x, contentSize.y);
		if (GUI.Button(rc, redoButtonContent, EditorStyles.toolbarButton))
		{
			focusCodeView = true;
			Redo();
		}

		//if (textBuffer != null && !textBuffer.isText)
		//{
		//	GUI.enabled = CanEdit();
		//	contentSize = EditorStyles.toolbarButton.CalcSize(toggleCommentButtonContent);
		//	rc = new Rect(rc.xMax, toolbarRect.yMin, contentSize.x, contentSize.y);
		//	if (GUI.Button(rc, toggleCommentButtonContent, EditorStyles.toolbarButton))
		//	{
		//		focusCodeView = true;
		//		ToggleCommentSelection();
		//	}
		//}

		//GUI.enabled = textBuffer.hyperlinks.Count > 0;
		//GUIContent links = new GUIContent(textBuffer.hyperlinks.Count.ToString(), hyperlinksIcon);

		//contentSize = EditorStyles.toolbarDropDown.CalcSize(links);
		//rc.Set(rc.xMax + 6f, toolbarRect.yMin, contentSize.x, EditorStyles.toolbar.fixedHeight);
		//if (GUI.Button(rc, links, EditorStyles.toolbarDropDown))
		//{
		//	GenericMenu menu = new GenericMenu();
		//	foreach (string hyperlink in textBuffer.hyperlinks)
		//	{
		//		if (hyperlink.StartsWithIgnoreCase("mailto:"))
		//		{
		//			menu.AddItem(new GUIContent(hyperlink.Substring(7)), false, FollowHyperlink, hyperlink);
		//		}
		//		else
		//		{
		//			// Shortening the URL since we cannot display slashes in the menu item,
		//			// so in most cases we'll end up with something like www.flipbookgames.com...
		//			// The first two or three slashes and the last one will be trimmed too.
		//			string escaped = hyperlink.Substring(hyperlink.IndexOf(':') + 1);

		//			// Unity cannot display a slash in menu items. Replacing any remaining slashes with
		//			// the best alternative - backslash
		//			escaped = escaped.Replace('/', '\\');

		//			// On Windows the '&' has special meaning, unless it's escaped with another '&'
		//			if (Application.platform == RuntimePlatform.WindowsEditor)
		//			{
		//				int index = 0;
		//				while (index < escaped.Length && (index = escaped.IndexOf('&', index + 1)) >= 0)
		//					escaped = escaped.Insert(index++, "&");
		//			}

		//			menu.AddItem(new GUIContent(escaped.Trim('\\')), false, FollowHyperlink, hyperlink);
		//		}
		//	}
		//	menu.DropDown(rc);
		//}
		
		
		if (p4MenusType != null)
		{
			GUI.enabled = true;
			GUIContent p4Content = new GUIContent("P4");
			
			contentSize = EditorStyles.toolbarDropDown.CalcSize(p4Content);
			rc.Set(rc.xMax + 6f, toolbarRect.yMin, contentSize.x, EditorStyles.toolbar.fixedHeight);
			if (GUI.Button(rc, p4Content, EditorStyles.toolbarDropDown))
			{
				if (p4ValidConfigurationProperty == null)
				{
					var p4ConfigType = Type.GetType("P4Connect.Config,P4Connect");
					p4ValidConfigurationProperty = p4ConfigType != null ? p4ConfigType.GetProperty("ValidConfiguration") : null;
				}
				if(p4ValidConfigurationProperty != null && !(bool) p4ValidConfigurationProperty.GetValue(null, null))
				{
					var menu = new GenericMenu();
					menu.AddItem(new GUIContent("Perforce Settings"), false, () => EditorApplication.ExecuteMenuItem("Edit/Perforce Settings"));
					menu.DropDown(rc);
				}
				else
				{
					var menuItems = new Dictionary<string, P4MenuItem>();
					
					if (p4MenusMethods == null)
						p4MenusMethods = p4MenusType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					for (int index = 0; index < p4MenusMethods.Length; ++index)
					{
						var methodInfo = p4MenusMethods[index];
						foreach (MenuItem menuItem in methodInfo.GetCustomAttributes(typeof(MenuItem), false))
						{
							P4MenuItem p4MenuItem;
							if (!menuItem.menuItem.StartsWithIgnoreCase("assets/perforce/"))
								continue;
							
							if (!menuItems.TryGetValue(menuItem.menuItem, out p4MenuItem))
								menuItems[menuItem.menuItem] = p4MenuItem = new P4MenuItem();
							p4MenuItem.menuItem = menuItem.menuItem;
							if (menuItem.validate)
							{
								p4MenuItem.validateMethod = methodInfo;
							}
							else
							{
								p4MenuItem.executeMethod = methodInfo;
								p4MenuItem.index = index;
								p4MenuItem.priority = menuItem.priority;
							}
						}
					}
					
					if (menuItems.Count > 0)
					{
						var p4MenuItemsArray = System.Linq.Enumerable.ToArray(menuItems.Values);
						Array.Sort(p4MenuItemsArray, (a, b) =>
							a.priority != b.priority ?
							a.priority.CompareTo(b.priority) :
							a.index.CompareTo(b.index));
						
						var savedSelection = Selection.objects;
						var targetAsset = AssetDatabase.LoadAssetAtPath(targetPath, typeof(TextAsset)) as TextAsset;
						Selection.objects = new[] { targetAsset };
						
						var menu = new GenericMenu();
						foreach (var item in p4MenuItemsArray)
						{
							var enabled = item.validateMethod == null || (bool) item.validateMethod.Invoke(null, null);
							var menuItemContent = new GUIContent(item.menuItem.Substring("Assets/Perforce/".Length));
							if (enabled)
							{
								menu.AddItem(menuItemContent, false, executeMethod => ((MethodInfo) executeMethod).Invoke(null, null), item.executeMethod);
							}
							else
							{
								menu.AddDisabledItem(menuItemContent);
							}
						}
						
						menu.DropDown(rc);
						
						EditorApplication.delayCall += () => { Selection.objects = savedSelection; };
					}
				}
			}
		}
#if !UNITY_4_0 && !UNITY_4_1
		else if (UnityEditor.VersionControl.Provider.enabled)
		{
			GUI.enabled = true;
			
			contentSize = EditorStyles.toolbarDropDown.CalcSize(new GUIContent(versionControlIcon));
			rc.Set(rc.xMax + 6f, toolbarRect.yMin, contentSize.x, EditorStyles.toolbar.fixedHeight);
			if (GUI.Button(rc, new GUIContent(versionControlIcon), EditorStyles.toolbarDropDown))
			{
				var savedSelection = Selection.objects;
				var targetAsset = AssetDatabase.LoadAssetAtPath(targetPath, typeof(TextAsset)) as TextAsset;
				Selection.objects = new[] { targetAsset };
				
				EditorUtility.DisplayPopupMenu(rc, "Assets/Version Control/", new MenuCommand(targetAsset));
				
				EditorApplication.delayCall += () => { Selection.objects = savedSelection; };
			}
		}
#endif
		

		//GUI.enabled = true;
		//GUIContent dump = new GUIContent("Dump");

		//contentSize = EditorStyles.toolbarButton.CalcSize(dump);
		//rc.Set(rc.xMax + 6f, toolbarRect.yMin, contentSize.x, EditorStyles.toolbar.fixedHeight);
		//if (GUI.Button(rc, dump, EditorStyles.toolbarDropDown))
		//{
		//	var debug = textBuffer.Parser.parseTree.root.ToString();
		//	Debug.Log(debug);
		//	EditorGUIUtility.systemCopyBuffer = debug;
		//}


		//if (EditorApplication.isCompiling && holdingAssemblies > 0 && compilers.Count == 0)
		//{
		//	GUI.enabled = true;
		//	GUI.backgroundColor = Color.green;
		//	GUIContent reload = new GUIContent("RELOAD");
				
		//	contentSize = EditorStyles.toolbarButton.CalcSize(reload);
		//	rc.Set(rc.xMax + 6f, toolbarRect.yMin, contentSize.x, EditorStyles.toolbar.fixedHeight);
		//	if (GUI.Button(rc, reload, EditorStyles.toolbarButton))
		//	{
		//		MenuReloadAssemblies();
		//		Repaint();
		//	}
		//	GUI.backgroundColor = Color.white;
		//}

		GUI.contentColor = oldColor;
		
		rc.xMin = rc.xMax + 8f;
#if UNITY_2019_1_OR_NEWER
		rc.xMax = toolbarRect.xMax - 28f;
#else
		rc.xMax = toolbarRect.xMax - 25f;
#endif

		var infoRect = rc;
		infoRect.x -= 7f;
		
		if (Event.current.type == EventType.Repaint)
		{
			bool isCompiling = EditorApplication.isCompiling;
			bool makeItOrange = true;
			
			string infoText = null;
			if (textBuffer.IsLoading)
				infoText = "Loading...";
			else if (isCompiling)
				infoText = "Compiling and reloading assemblies...";
			else if (FGTextBufferManager.HasPendingAssetImports && !textBuffer.IsModified)
			{
				if (isPatched)
				{
					if (SISettings.compileOnDoubleSave)
					{
						infoText = "Patched by Hot Reload... Save again to rebuild assemblies";
					}
					else
					{
						var ctrlR = isOSX ? "⌥⌘R" : "Ctrl+R";
						infoText = "Patched by Hot Reload... " + ctrlR + " to rebuild assemblies";
					}
					makeItOrange = false;
				}
				else
				{
					if (SISettings.compileOnDoubleSave)
					{
						infoText = "Saved... Save again to compile!";
					}
					else
					{
						var ctrlR = isOSX ? "⌥⌘R" : "Ctrl+R";
						infoText = "Saved... " + ctrlR + " to rebuild assemblies";
					}
				}
			}
			
			//compilers.Count > 0 ? "Compiling in background..." :
			//holdingAssemblies == 0 ? "Compiling and reloading assemblies..." :
			//IsModified && isOSX ? "Cmd-Alt-R to reload assemblies..." :
			//IsModified ? "Ctrl+R to reload assemblies..." :
			//isOSX ? "Cmd-Alt-R or Save again to reload assemblies..." :
			//"Ctrl+R or Save again to reload assemblies...";
			
			if (infoText == null)
			{
				if (parentWindow != null && !string.IsNullOrEmpty(targetPath) && textBuffer != null)
				{
					if (textBuffer.assetPathContent == null)
					{
						string path;
						int fileNameStartIndex = targetPath.LastIndexOf("/", StringComparison.Ordinal) + 1;
						if (targetPath.StartsWithIgnoreCase("assets/"))
						{
							path = targetPath.Substring("Assets/".Length, fileNameStartIndex - "Assets/".Length);
						}
						else if (targetPath.StartsWithIgnoreCase("packages/"))
						{
							var packageRootStringEnd = targetPath.IndexOf('/', "Packages/".Length);
							path = targetPath.Substring(0, fileNameStartIndex);

							try
							{
								var jsonFilename = targetPath.Substring(0, packageRootStringEnd) + "/package.json";
								if (System.IO.File.Exists(jsonFilename))
								{
									var json = System.IO.File.ReadAllText(jsonFilename);
									if (json != null)
									{
										var start = json.IndexOf("\"displayName\"", StringComparison.Ordinal);
										if (start > 0)
										{
											start += "\"displayName\"".Length;
											start = json.IndexOf('"', start);
											if (start > 0)
											{
												++start;
												var end = json.IndexOf('"', start);
												if (end > start)
												{
													path = "Packages/" + json.Substring(start, end - start);
												}
											}
										}
									}
								}
							}
							catch {}
							path += targetPath.Substring(packageRootStringEnd, fileNameStartIndex - packageRootStringEnd);
						}
						else
						{
							path = targetPath.Substring(0, fileNameStartIndex);
						}

						textBuffer.assetPathContent = new GUIContent(path);
						textBuffer.assetPathContentWidth = EditorStyles.label.CalcSize(textBuffer.assetPathContent).x;
						var fileName = targetPath.Substring(fileNameStartIndex);
						textBuffer.assetFileNameContent = new GUIContent(fileName);
					}
					
					GUI.enabled = false;
					EditorStyles.label.Draw(rc, textBuffer.assetPathContent, false, false, false, false);
					rc.xMin += textBuffer.assetPathContentWidth - 3f;
					//GUI.enabled = true;
					EditorStyles.boldLabel.Draw(rc, textBuffer.assetFileNameContent, false, false, false, false);
				}
				else
				{
					GUI.enabled = false;
					EditorStyles.label.Draw(rc, textBuffer.fileEncoding.EncodingName, false, false, false, false);
				}
			}
			else
			{
				GUI.enabled = true;
				if (!makeItOrange)
				{
					if (normalLabel == null)
					{
						normalLabel = new GUIStyle(EditorStyles.label);
						//greenLabel.normal.textColor =
						//	EditorGUIUtility.isProSkin ? new Color(0f, 0.9f, 0f) : new Color(0f, 0.6f, 0f);
					}
					normalLabel.Draw(rc, infoText, false, false, false, false);
				}
				else
				{
					if (orangeBoldLabel == null)
					{
						orangeBoldLabel = new GUIStyle(EditorStyles.boldLabel);
						orangeBoldLabel.normal.textColor =
							EditorGUIUtility.isProSkin ? new Color(1f, 0.5f, 0f) : new Color(0.9f, 0.4f, 0f);
					}
					orangeBoldLabel.Draw(rc, infoText, false, false, false, false);
				}
			}
		}
		
		GUI.enabled = CanEdit();

		rc.y += 2f;
		rc.height = 16f;
		if (rc.width > 331f)
		{
			rc.xMin = rc.xMax - 331f;
		}
		if (rc.width > 200f)
		{
			if (textBuffer.undoPosition != searchResultAge || string.IsNullOrEmpty(searchString))
			{
				rc.xMin = rc.xMax - 200f;
			}
		}
		DoSearchBox(rc);
		
		infoRect.xMax = rc.x;
		
		GUI.enabled = true;
		if (wrenchIcon != null)
		{
#if UNITY_2019_1_OR_NEWER
			rc = new Rect(toolbarRect.xMax - 24f, toolbarRect.yMin, 18f, contentSize.y);
			if (GUI.Button(rc, GUIContent.none, EditorStyles.toolbarButton))
			{
				ShowWrenchMenu(rc);
			}
			
			rc = new Rect(toolbarRect.xMax - 22f, toolbarRect.yMin + 4f, 15f, 14f);
			
			if (newIcon != null && FGTextBufferManager.IsUpdateAvailable())
			{
				rc.y += 2f;
				GUI.Label(rc, wrenchIcon, GUIStyle.none);

				rc = new Rect(toolbarRect.xMax - 35f, toolbarRect.yMin, 30f, 9f);
				GUI.Label(rc, newIcon, GUIStyle.none);
			}
			else
			{
				GUI.Label(rc, wrenchIcon, GUIStyle.none);
			}
#else
			// Only redrawing the default wrench icon after being covered with our toolbar.
			// The default icon still handles the functionality in the Inspector tab.
			rc = new Rect(toolbarRect.xMax - 20f, toolbarRect.yMin + 2f, 15f, 14f);
			GUI.Label(rc, wrenchIcon, GUIStyle.none);
#endif
		}

		if (toolbarRect.Contains(Event.current.mousePosition))
		{
			if (Event.current.type == EventType.ContextClick)
			{
				Event.current.Use();
			}
			else if (Event.current.type == EventType.MouseDown && Event.current.button == 0
				&& infoRect.Contains(Event.current.mousePosition))
			{
				if (!string.IsNullOrEmpty(targetGuid))
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(targetGuid);
					if (!string.IsNullOrEmpty(assetPath))
					{
#if UNITY_2018_3_OR_NEWER
						EditorApplication.ExecuteMenuItem("Window/General/Project");
#else
						EditorApplication.ExecuteMenuItem("Window/Project");
#endif
						var target = AssetDatabase.LoadMainAssetAtPath(assetPath);
						if (target != null)
							EditorGUIUtility.PingObject(target);
					}
				}
			}
		}

		GUI.enabled = true;
		return height;
	}

	private void OpenInNewTab()
	{
		if (currentInspector != null)
			FGCodeWindow.OpenNewWindow(null, null, false);
		else
			EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("ScriptInspector.AddTab"));
	}

	private void DoSearchBox(Rect position)
	{
		if (Event.current.type == EventType.ValidateCommand)
		{
			if (Event.current.commandName == "Find")
			{
				Event.current.Use();
				return;
			}
		}
		else if (Event.current.type == EventType.ExecuteCommand)
		{
			if (Event.current.commandName == "Find")
			{
				if (hasCodeViewFocus)
					UseSelectionForSearch();
				focusSearchBox = true;
				Event.current.Use();
				if (hasSearchBoxFocus)
					return;
			}
		}

		if (textBuffer.undoPosition != searchResultAge)
		{
			currentSearchResult = -1;
			atLastSearchResult = false;
		}

		if ((Event.current.type == EventType.MouseDown) && position.Contains(Event.current.mousePosition))
		{
			focusSearchBox = true;
		}

		if (Event.current.type == EventType.KeyDown && !Event.current.alt)
		{
			var isOSX = Application.platform == RuntimePlatform.OSXEditor;
			if (Event.current.keyCode == KeyCode.F3 && !EditorGUI.actionKey ||
				isOSX && Event.current.keyCode == KeyCode.G && EditorGUI.actionKey)
			{
				if (Event.current.shift)
					SearchPrevious();
				else
					SearchNext();
				
				Event.current.Use();
			}
		}

		if (focusCodeViewOnEscapeUp && Event.current.rawType == EventType.KeyUp &&
			(Event.current.keyCode == KeyCode.Escape || Event.current.keyCode == KeyCode.Tab))
		{
			focusCodeViewOnEscapeUp = false;
			focusCodeView = true;
			Event.current.Use();
		}

		if (hasSearchBoxFocus && Event.current.type == EventType.KeyDown)
		{
			if (Event.current.keyCode == KeyCode.Escape)
			{
				focusCodeViewOnEscapeUp = true;
				Event.current.Use();
			}
			else if (Event.current.keyCode == KeyCode.UpArrow ||
				Event.current.shift && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
			{
				SearchPrevious();
				focusSearchBox = true;
				Event.current.Use();
				return;
			}
			else if (Event.current.keyCode == KeyCode.DownArrow ||
				!Event.current.shift && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
			{
				SearchNext();
				focusSearchBox = true;
				Event.current.Use();
				return;
			}
			else if (Event.current.keyCode == KeyCode.Tab)
			{
				focusCodeViewOnEscapeUp = true;
				Event.current.Use();
				return;
			}
		}

		if (hasSearchBoxFocus && Event.current.type == EventType.KeyUp
			&& (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
		{
			Event.current.Use();
			return;
		}
		
		string text = ToolbarSearchField(position, searchString);
		
		if (searchString != text)
		{
			searchString = text;
			SetSearchText(searchString);
			hasSearchBoxFocus = true;
		}
	}

	private void SetSearchText(string text)
	{
		if (searchResultAge == textBuffer.undoPosition && defaultSearchString.Equals(text, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		
		searchResultAge = textBuffer.undoPosition;
		//atLastSearchResult = false;
		defaultSearchString = text;

		searchResults.Clear();
		currentSearchResult = -1;
		int textLength = text.Length;

		if (textLength == 0)
		{
			Repaint();
			return;
		}

		searchedLinesFrom = caretPosition.line;
		searchedLinesTo = caretPosition.line;
		
		//var lines = textBuffer.lines;
		//for (int i = 0; i < lines.Count; i++)
		//{
		//	var line = lines[i];
		//	for (int pos = 0; (pos = line.IndexOf(text, pos, StringComparison.OrdinalIgnoreCase)) != -1; pos += textLength )
		//	{
		//		var caretPos = new TextPosition{ line = i, index = pos };
		//		searchResults.Add(caretPos);
		//	}
		//}
		
		highlightSearchResults = text != ""; //text.Trim() != "";
		Repaint();
	}

	public bool OverrideButton(Rect position, GUIContent content, GUIStyle style, bool forceHot)
	{
		int controlID = GUIUtility.GetControlID(buttonHash, FocusType.Passive, position);
		if (forceHot)
			GUIUtility.hotControl = controlID;

		switch (Event.current.GetTypeForControl(controlID))
		{
			case EventType.MouseDown:
			case EventType.MouseMove:
				if (position.Contains(Event.current.mousePosition))
				{
					GUIUtility.hotControl = controlID;
					Event.current.Use();
					needsRepaint = true;
				}
				else if (GUIUtility.hotControl == controlID && !position.Contains(Event.current.mousePosition))
				{
					GUIUtility.hotControl = 0;
					needsRepaint = true;
				}
				return false;

			case EventType.MouseUp:
				if (GUIUtility.hotControl != controlID)
					return false;

				needsRepaint = true;
				GUIUtility.hotControl = 0;
				Event.current.Use();
				return position.Contains(Event.current.mousePosition);

			case EventType.MouseDrag:
				if (GUIUtility.hotControl == controlID)
					Event.current.Use();
				break;

			case EventType.Repaint:
				style.Draw(position, content, controlID);
				break;
			
			case EventType.Used:
				if (Event.current.rawType == EventType.MouseUp)
				{
					if (GUIUtility.hotControl == controlID)
					{
						GUIUtility.hotControl = 0;
						needsRepaint = true;
					}
				}
				break;
		}

		return false;
	}

	[System.NonSerialized]
	private string searchInfoString;
	[System.NonSerialized]
	private int cachedSearchResultsCount;
	[System.NonSerialized]
	private int cachedCurrentSearchResult;

	private static GUIStyle toolbarSearchFieldClear;
	private static GUIStyle toolbarSearchFieldInfo;

	private string ToolbarSearchField(Rect position, string text)
    {
		if (styles.toolbarSearchField == null)
		{
			var editorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
			var searchFieldStyle = editorSkin.FindStyle("ToolbarSearchTextField");
			if (searchFieldStyle != null)
			{
				styles.toolbarSearchField = new GUIStyle("ToolbarSearchTextField");
				styles.toolbarSearchFieldCancelButton = new GUIStyle("ToolbarSearchCancelButton");
				styles.toolbarSearchFieldCancelButtonEmpty = new GUIStyle("ToolbarSearchCancelButtonEmpty");
			}
			else
			{
				styles.toolbarSearchField = new GUIStyle("ToolbarSeachTextField");
				styles.toolbarSearchFieldCancelButton = new GUIStyle("ToolbarSeachCancelButton");
				styles.toolbarSearchFieldCancelButtonEmpty = new GUIStyle("ToolbarSeachCancelButtonEmpty");
			}

			toolbarSearchFieldClear = new GUIStyle(styles.toolbarSearchField);
			toolbarSearchFieldClear.name = "Si3SearchFieldClear";
			toolbarSearchFieldClear.imagePosition = ImagePosition.TextOnly;
			toolbarSearchFieldInfo = new GUIStyle(styles.toolbarSearchField);
			toolbarSearchFieldInfo.name = "Si3SearchFieldInfo";
			toolbarSearchFieldInfo.alignment = TextAnchor.MiddleRight;
			toolbarSearchFieldInfo.imagePosition = ImagePosition.TextOnly;
		}

		Rect rc = position;
		#if UNITY_2019_3_OR_NEWER
	    rc.width += 2f;
	    #else
		rc.width -= 14f;
		#endif
		if (Event.current.type == EventType.Repaint)
		{
			styles.toolbarSearchField.Draw(rc, GUIContent.none, false, false, false, hasSearchBoxFocus);

			if (!string.IsNullOrEmpty(text))
			{
				bool enabled = GUI.enabled;
				GUI.enabled = false;
				Color color = GUI.backgroundColor;
				GUI.backgroundColor = Color.clear;
				if (searchResults.Count > 0)
				{
					if (searchString == null || searchResults.Count != cachedSearchResultsCount || currentSearchResult != cachedCurrentSearchResult)
					{
						cachedCurrentSearchResult = currentSearchResult;
						cachedSearchResultsCount = searchResults.Count;
						searchInfoString = (
						currentSearchResult >= 0 ?
							(currentSearchResult + 1).ToString() + " of " + cachedSearchResultsCount.ToString() :
							cachedSearchResultsCount.ToString() + " results");
					}
					rc.width -= 33f;
					toolbarSearchFieldInfo.Draw(rc, searchInfoString, false, false, false, hasSearchBoxFocus);
					rc.width += 33f;
				}
				else
				{
					rc.width -= 4f;
					toolbarSearchFieldInfo.Draw(rc, "no results", false, false, false, hasSearchBoxFocus);
					rc.width += 4f;
				}
				GUI.enabled = enabled;
				GUI.backgroundColor = color;
			}
		}
		#if UNITY_2019_3_OR_NEWER
		rc.x = rc.x + styles.toolbarSearchField.fixedWidth;
		rc.width -= 46f;
		#else
		rc.width -= 40f;
		#endif
		
	    GUI.SetNextControlName("SearchBox");
		
		Color bgColor = GUI.backgroundColor;
		GUI.backgroundColor = Color.clear;
		text = EditorGUI.TextField(rc, text, toolbarSearchFieldClear);

		hasSearchBoxFocus = focusSearchBox || GUI.GetNameOfFocusedControl() == "SearchBox";
		
	    if (hasSearchBoxFocus)
	    {
#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		    EditorGUI.FocusTextInControl("SearchBox");
#else
		    GUI.FocusControl("SearchBox");
#endif

		    if (Event.current.type == EventType.Repaint)
		    {
			    focusSearchBox = false;
		    }
	    }
		
		bool isEmpty = text == string.Empty;
		
		if (!hasSearchBoxFocus && !isEmpty && text.Trim() == "")
			GUI.Label(rc, text.Replace("\t", "<tab>").Replace(" ", "<space>"), toolbarSearchFieldClear);
		GUI.backgroundColor = bgColor;
		
		rc = position;
		rc.x += position.width - 14f;
		rc.width = 14f;
		if (!isEmpty)
		{
			if (OverrideButton(rc, GUIContent.none, styles.toolbarSearchFieldCancelButton, clearSearchButtonClicked))
			{
				text = string.Empty;
				focusCodeView = true;
				//GUIUtility.keyboardControl = 0;
			}
		}
		else
		{
			GUI.Label(rc, GUIContent.none, styles.toolbarSearchFieldCancelButtonEmpty);
			if (clearSearchButtonClicked)
				focusSearchBox = true;
		}
	    clearSearchButtonClicked = false;

		rc.x -= 15f;
		rc.width = 14f;
		rc.height = 15f;
		if (!isEmpty && searchResults.Count != 0 && GUI.Button(rc, GUIContent.none, styles.downArrowStyle))
			SearchNext();

		rc.x -= 14f;
		if (!isEmpty && searchResults.Count != 0 && GUI.Button(rc, GUIContent.none, styles.upArrowStyle))
			SearchPrevious();

		return text;
	}

	private static int FindFirstIndexGreaterThanOrEqualTo<T>(IList<T> sortedCollection, T key)
	{
		return FindFirstIndexGreaterThanOrEqualTo<T>(sortedCollection, key, Comparer<T>.Default);
	}

	private static int FindFirstIndexGreaterThanOrEqualTo<T>(IList<T> sortedCollection, T key, IComparer<T> comparer)
	{
		int begin = 0;
		int end = sortedCollection.Count;
		while (end > begin)
		{
			int index = (begin + end) / 2;
			T el = sortedCollection[index];
			if (comparer.Compare(el, key) >= 0)
				end = index;
			else
				begin = index + 1;
		}
		return end;
	}
	
	[NonSerialized]
	private bool lastCaretMoveWasSearch;

	private void SearchPrevious()
	{
		if (string.IsNullOrEmpty(searchString))
			return;

		if (searchResultAge != textBuffer.undoPosition)
			SetSearchText(searchString);

		var numFound = 0;
		while (searchedLinesFrom > 0)
		{
			var line = Mathf.Max(0, searchedLinesFrom - 50);
			numFound = ProgressiveSearch(line);
			if (numFound > 0)
			{
				break;
			}
		}
		if (numFound == 0 && searchedLinesTo < textBuffer.lines.Count)
		{
			ProgressiveSearch(textBuffer.lines.Count);
		}

		TextPosition beginning;
		if (selectionStartPosition != null && selectionStartPosition < caretPosition)
		{
			beginning.line = selectionStartPosition.line;
			beginning.index = selectionStartPosition.characterIndex;
		}
		else
		{
			beginning.line = caretPosition.line;
			beginning.index = caretPosition.characterIndex;
		}
		currentSearchResult = FindFirstIndexGreaterThanOrEqualTo(searchResults, beginning) - 1;
		currentSearchResult = Math.Min(searchResults.Count - 1, currentSearchResult);
		if (currentSearchResult == -1)
		{
			if (atLastSearchResult && SISettings.loopSearchResults)
			{
				currentSearchResult = searchResults.Count - 1;
				atLastSearchResult = searchResults.Count == 1;
			}
			else
			{
				currentSearchResult = 0;
				atLastSearchResult = true;
			}
		}
		else
		{
			atLastSearchResult = searchResults.Count == 1;
		}
		ShowSearchResult(currentSearchResult);
	}

	private void SearchNext()
	{
		if (string.IsNullOrEmpty(searchString))
			return;

		if (searchResultAge != textBuffer.undoPosition)
			SetSearchText(searchString);
		
		var numFound = 0;
		var numLines = textBuffer.lines.Count;
		while (searchedLinesTo < numLines)
		{
			var line = Mathf.Min(numLines, searchedLinesTo + 50);
			numFound = ProgressiveSearch(line);
			if (numFound > 0)
				break;
		}
		if (numFound == 0 && searchedLinesFrom > 0)
		{
			ProgressiveSearch(0);
		}
		
		TextPosition end;
		if (selectionStartPosition != null && selectionStartPosition > caretPosition)
		{
			end.line = selectionStartPosition.line;
			end.index = selectionStartPosition.characterIndex;
		}
		else
		{
			end.line = caretPosition.line;
			end.index = caretPosition.characterIndex;
		}
		currentSearchResult = FindFirstIndexGreaterThanOrEqualTo(searchResults, end);
		currentSearchResult = Math.Max(0, currentSearchResult);
		if (currentSearchResult >= searchResults.Count)
		{
			if (atLastSearchResult && SISettings.loopSearchResults)
			{
				currentSearchResult = 0;
				atLastSearchResult = searchResults.Count == 1;
			}
			else
			{
				currentSearchResult = searchResults.Count - 1;
				atLastSearchResult = true;
			}
		}
		else
		{
			atLastSearchResult = searchResults.Count == 1;
		}
		ShowSearchResult(currentSearchResult);
	}

	private void ShowSearchResult(int index)
	{
		if (!lastCaretMoveWasSearch)
		{
			lastCaretMoveWasSearch = true;
			AddRecentLocation(0, true);
		}
		
		if (searchResultAge != textBuffer.undoPosition)
			SetSearchText(searchString);

		if (index >= 0 && index < searchResults.Count)
		{
			PingText(searchResults[index], searchString.Length, atLastSearchResult ? redPingColor : yellowPingColor);
		}
	}

	private int CharIndexToColumn(int charIndex, int line)
	{
		return BufferPositionToEditorColumn(line, charIndex);
		//if (wordWrapping)
		//{
		//	List<int> softLineBreaks = GetSoftLineBreaks(line);
		//	int softRow = FindFirstIndexGreaterThanOrEqualTo<int>(softLineBreaks, charIndex);
		//	return textBuffer.CharIndexToColumn(charIndex, line, softRow > 0 ? softLineBreaks[softRow - 1] : 0);
		//}
		//else
		//{
		//	return textBuffer.CharIndexToColumn(charIndex, line);
		//}
	}

	public void PingText(FGTextBuffer.CaretPos startPosition, int numChars, Color color)
	{
		PingText(new TextPosition{ line = startPosition.line, index = startPosition.characterIndex }, numChars, color);
	}
	
	public void PingText(TextPosition startPosition, int numChars, Color color, bool unfold = true)
	{
		CloseAllPopups();

		if (startPosition.line >= textBuffer.lines.Count)
			startPosition.line = textBuffer.lines.Count - 1;
		
		string lineText;
		int lineTextLength;
		
		//if (unfold)
		{
			lineText = textBuffer.lines[startPosition.line];
			lineTextLength = lineText.Length;
		}
		
		startPosition.index = Mathf.Min(startPosition.index, lineTextLength);
		if (startPosition.index + numChars > lineTextLength)
			numChars = lineTextLength - startPosition.index;

		//if (unfold)
		{
			UnfoldTextRange(startPosition.line, startPosition.index, startPosition.line, startPosition.index + numChars);
		}
		
		selectionStartPosition = new FGTextBuffer.CaretPos();
		selectionStartPosition.line = startPosition.line;
		selectionStartPosition.characterIndex = startPosition.index;
		
		selectionStartPosition.column = selectionStartPosition.virtualColumn = CharIndexToColumn(startPosition.index, startPosition.line);

		int fromCharIndex = startPosition.index;
		int toCharIndex = startPosition.index + numChars;
		int columnTo = CharIndexToColumn(toCharIndex, selectionStartPosition.line);
		caretPosition = new FGTextBuffer.CaretPos {
			characterIndex = toCharIndex,
			column = columnTo,
			virtualColumn = columnTo,
			line = selectionStartPosition.line
		};
		caretMoveTime = frameTime;

		var textPosition = new TextPosition(selectionStartPosition.line, selectionStartPosition.characterIndex);
		Vector2 topLeft = BufferToViewPosition(textPosition, selectionStartPosition.virtualColumn == 0);
		scrollToRect = new Rect(
			topLeft.x,
			topLeft.y + GetLineOffset(selectionStartPosition.line),
			GetTextWidth(selectionStartPosition.line, selectionStartPosition.characterIndex, toCharIndex, topLeft.x),
			LineHeight);

		pingTimer = 1f;
		pingStartTime = frameTime;
		numChars = Mathf.Min(numChars, lineTextLength - fromCharIndex);
		pingContent.text = lineText.Substring(fromCharIndex, numChars);
		pingColor = color;

		Repaint();
	}

	public static void RepaintAllInstances()
	{
		var allInspectors = (ScriptInspector[]) Resources.FindObjectsOfTypeAll(typeof(ScriptInspector));
		if (allInspectors != null)
			foreach (var wnd in allInspectors)
				wnd.Repaint();
		FGCodeWindow.RepaintAllWindows();
	}

	[MenuItem("CONTEXT/MonoScript/Word Wrap (Code)", false, 141)]
	private static void ToggleWordWrapCode()
	{
		if (!ScriptInspector.IsFocused())
			SISettings.wordWrapCode.Toggle();
		else
			SISettings.wordWrapCodeInspector.Toggle();
	}

	[MenuItem("CONTEXT/MonoScript/Highlight Current Line", false, 142)]
	private static void ToggleHighlightCurrentLine()
	{
		SISettings.highlightCurrentLine.Toggle();
	}

	[MenuItem("CONTEXT/MonoScript/Frame Current Line", false, 143)]
	private static void ToggleFrameCurrentLine()
	{
		SISettings.frameCurrentLine.Toggle();
	}

	[MenuItem("CONTEXT/MonoScript/Line Numbers (Code)", false, 144)]
	private static void ToggleLineNumbersCode()
	{
		if (!ScriptInspector.IsFocused())
			SISettings.showLineNumbersCode.Toggle();
		else
			SISettings.showLineNumbersCodeInspector.Toggle();
	}

	private static void ToggleLineNumbersText()
	{
		if (!ScriptInspector.IsFocused())
			SISettings.showLineNumbersText.Toggle();
		else
			SISettings.showLineNumbersTextInspector.Toggle();
	}
	
	[MenuItem("CONTEXT/MonoScript/Track Changes (Code)", false, 145)]
	private static void ToggleTrackChangesCode()
	{
		if (!ScriptInspector.IsFocused())
			SISettings.trackChangesCode.Toggle();
		else
			SISettings.trackChangesCodeInspector.Toggle();
	}

	private static void ToggleTrackChangesText()
	{
		if (!ScriptInspector.IsFocused())
			SISettings.trackChangesText.Toggle();
		else
			SISettings.trackChangesTextInspector.Toggle();
	}
	
	[MenuItem("CONTEXT/MonoScript/Code Folding", false, 146)]
	private static void ToggleEnableCodeFolding()
	{
		if (!ScriptInspector.IsFocused())
			SISettings.codeFoldingCode.Toggle();
		else
			SISettings.codeFoldingCodeInspector.Toggle();
	}

	private static void ToggleWordWrapText()
	{
		if (!ScriptInspector.IsFocused())
			SISettings.wordWrapText.Toggle();
		else
			SISettings.wordWrapTextInspector.Toggle();
	}

	[MenuItem("CONTEXT/MonoScript/Darcula Theme", false, 160)]
	private static void SetCodeStyleDarcula() { SelectTheme(8, false); }

	[MenuItem("CONTEXT/MonoScript/MD Brown Theme", false, 160)]
	private static void SetCodeStyleMDBrown() { SelectTheme(4, false); }

	[MenuItem("CONTEXT/MonoScript/MD Brown - Dark Theme", false, 161)]
	private static void SetCodeStyleMDBrownDark() { SelectTheme(5, false); }

	[MenuItem("CONTEXT/MonoScript/Monokai Theme", false, 162)]
	private static void SetCodeStyleMonokai() { SelectTheme(6, false); }

	[MenuItem("CONTEXT/MonoScript/Solarized Dark Theme", false, 163)]
	private static void SetCodeStyleSolarideDark() { SelectTheme(12, false); }

	[MenuItem("CONTEXT/MonoScript/Solarized Light Theme", false, 163)]
	private static void SetCodeStyleSolarizedLight() { SelectTheme(13, false); }

	[MenuItem("CONTEXT/MonoScript/Son of Obsidian Theme", false, 163)]
	private static void SetCodeStyleSonOfObsidian() { SelectTheme(7, false); }

	[MenuItem("CONTEXT/MonoScript/Tango Dark (Oblivion) Theme", false, 164)]
	private static void SetCodeStyleTangoDark() { SelectTheme(2, false); }

	[MenuItem("CONTEXT/MonoScript/Tango Light Theme", false, 165)]
	private static void SetCodeStyleTangoLight() { SelectTheme(3, false); }

	[MenuItem("CONTEXT/MonoScript/Visual Studio Theme", false, 166)]
	private static void SetCodeStyleVisualStudio() { SelectTheme(0, false); }

	[MenuItem("CONTEXT/MonoScript/Visual Studio Dark Theme", false, 166)]
	private static void SetCodeStyleVisualStudioDark() { SelectTheme(9, false); }

	[MenuItem("CONTEXT/MonoScript/VS Dark with ReSharper Theme", false, 166)]
	private static void SetCodeStyleVSDarkWithReSharper() { SelectTheme(14, false); }

	[MenuItem("CONTEXT/MonoScript/VS Dark with VA X Theme", false, 166)]
	private static void SetCodeStyleVSDarkWithVAX() { SelectTheme(11, false); }

	[MenuItem("CONTEXT/MonoScript/VS Light with VA X Theme", false, 166)]
	private static void SetCodeStyleVSLightWithVAX() { SelectTheme(10, false); }

	[MenuItem("CONTEXT/MonoScript/Xcode Theme", false, 167)]
	private static void SetCodeStyleXcode() { SelectTheme(1, false); }
	
	private static void SelectTheme(int themeIndex, bool forText)
	{
		if (forText)
			currentThemeText = themes[themeIndex];
		else
			currentThemeCode = themes[themeIndex];
		ApplyTheme(forText ? stylesText : stylesCode, themes[themeIndex]);
		if (forText)
			SISettings.themeNameText.Value = availableThemes[themeIndex];
		else
			SISettings.themeNameCode.Value = availableThemes[themeIndex];
		RepaintAllInstances();
	}

	private static void ResetFontSize()
	{
		SISettings.fontSizeDelta.Value = 0;
		SISettings.fontSizeDeltaInspector.Value = 0;
		resetCodeFont = true;
		resetTextFont = true;
		if (stylesCode.normalStyle != null)
		{
			stylesCode.normalStyle.fontSize = 0;
			stylesCode.hyperlinkStyle.fontSize = 0;
			stylesCode.mailtoStyle.fontSize = 0;
			stylesCode.keywordStyle.fontSize = 0;
			stylesCode.controlKeywordStyle.fontSize = 0;
			stylesCode.constantStyle.fontSize = 0;
			stylesCode.referenceTypeStyle.fontSize = 0;
			stylesCode.commentStyle.fontSize = 0;
			stylesCode.stringStyle.fontSize = 0;
			stylesCode.lineNumbersStyle.fontSize = 0;
			stylesCode.currentLineNumberStyle.fontSize = 0;
			stylesCode.preprocessorStyle.fontSize = 0;
			stylesCode.parameterStyle.fontSize = 0;
			stylesCode.typeParameterStyle.fontSize = 0;
			stylesCode.enumMemberStyle.fontSize = 0;
			stylesCode.tooltipTextStyle.fontSize = 0;
			stylesCode.ping.fontSize = 0;

			stylesCode.normalStyle.fontStyle = 0;
			stylesCode.hyperlinkStyle.fontStyle = 0;
			stylesCode.mailtoStyle.fontStyle = 0;
			stylesCode.keywordStyle.fontStyle = 0;
			stylesCode.controlKeywordStyle.fontStyle = 0;
			stylesCode.constantStyle.fontStyle = 0;
			stylesCode.referenceTypeStyle.fontStyle = 0;
			stylesCode.commentStyle.fontStyle = 0;
			stylesCode.stringStyle.fontStyle = 0;
			stylesCode.lineNumbersStyle.fontStyle = 0;
			stylesCode.currentLineNumberStyle.fontStyle = 0;
			stylesCode.preprocessorStyle.fontStyle = 0;
			stylesCode.parameterStyle.fontStyle = 0;
			stylesCode.typeParameterStyle.fontStyle = 0;
			stylesCode.enumMemberStyle.fontStyle = 0;
			stylesCode.ping.fontStyle = 0;
		}
		if (stylesText.normalStyle != null)
		{
			stylesText.normalStyle.fontSize = 0;
			stylesText.hyperlinkStyle.fontSize = 0;
			stylesText.mailtoStyle.fontSize = 0;
			stylesText.keywordStyle.fontSize = 0;
			stylesText.controlKeywordStyle.fontSize = 0;
			stylesText.constantStyle.fontSize = 0;
			stylesText.referenceTypeStyle.fontSize = 0;
			stylesText.commentStyle.fontSize = 0;
			stylesText.stringStyle.fontSize = 0;
			stylesText.lineNumbersStyle.fontSize = 0;
			stylesText.currentLineNumberStyle.fontSize = 0;
			stylesText.preprocessorStyle.fontSize = 0;
			stylesText.parameterStyle.fontSize = 0;
			stylesText.typeParameterStyle.fontSize = 0;
			stylesText.enumMemberStyle.fontSize = 0;
			stylesText.tooltipTextStyle.fontSize = 0;
			stylesText.ping.fontSize = 0;

			stylesText.normalStyle.fontStyle = 0;
			stylesText.hyperlinkStyle.fontStyle = 0;
			stylesText.mailtoStyle.fontStyle = 0;
			stylesText.keywordStyle.fontStyle = 0;
			stylesText.controlKeywordStyle.fontStyle = 0;
			stylesText.constantStyle.fontStyle = 0;
			stylesText.referenceTypeStyle.fontStyle = 0;
			stylesText.commentStyle.fontStyle = 0;
			stylesText.stringStyle.fontStyle = 0;
			stylesText.lineNumbersStyle.fontStyle = 0;
			stylesText.currentLineNumberStyle.fontStyle = 0;
			stylesText.preprocessorStyle.fontStyle = 0;
			stylesText.parameterStyle.fontStyle = 0;
			stylesText.typeParameterStyle.fontStyle = 0;
			stylesText.enumMemberStyle.fontStyle = 0;
			stylesText.ping.fontStyle = 0;
		}
	}

	private static void SelectFont(int fontIndex)
	{
		resetCodeFont = true;
		resetTextFont = true;
		SISettings.editorFont.Value = currentFont = availableFonts[fontIndex];
		//ApplyTheme();
		//RepaintAllInstances();
	}

	private static void ToggleFontHinting()
	{
		resetCodeFont = true;
		resetTextFont = true;
		SISettings.fontHinting.Toggle();
	}

	private void ModifyFontSize(int delta)
	{
		resetCodeFont = true;
		resetTextFont = true;
		scrollInstantly = true;
		//var isInspector = parentWindow == null;
		//if (isInspector)
		//	SISettings.fontSizeDeltaInspector.Value = Math.Max(-10, Math.Min(10, SISettings.fontSizeDeltaInspector + Math.Sign(delta)));
		//else
			SISettings.fontSizeDelta.Value = Math.Max(-10, Math.Min(10, SISettings.fontSizeDelta + Math.Sign(delta)));
	}

	private static void ToggleHandleOpenAsset()
	{
		SISettings.handleOpenAssets.Toggle();
		SISettings.dontOpenAssets.Value = false;
	}

	private static void ToggleDontOpenAsset()
	{
		SISettings.dontOpenAssets.Toggle();
		SISettings.handleOpenAssets.Value = false;
	}

	private static void ToggleHandleOpenScriptsFromProject()
	{
		SISettings.handleOpeningScripts.Toggle();
	}

	private static void ToggleHandleOpenTextsFromProject()
	{
		SISettings.handleOpeningText.Toggle();
	}

	private static void ToggleHandleOpenShadersFromProject()
	{
		SISettings.handleOpeningShaders.Toggle();
	}

	[MenuItem("Window/Script Inspector 3/About...", false, 2000)]
	[MenuItem("CONTEXT/MonoScript/About Script Inspector 3...", false, 210)]
	private static void About()
	{
		var wnd = EditorWindow.GetWindow<AboutScriptInspector>(true);
		wnd.ShowAuxWindow();
	}
}

}
