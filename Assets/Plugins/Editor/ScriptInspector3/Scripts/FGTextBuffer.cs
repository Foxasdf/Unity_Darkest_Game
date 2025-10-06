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

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;

using Debug = UnityEngine.Debug;


using ScriptInspector;


[System.Serializable]
public class FGTextBuffer : ScriptableObject
{
	public enum BlockState : byte
	{
		None = 0,
		CommentBlock = 1,
		StringBlock = 2,
		InterpolatedStringBlock = 3,
	}

	public class RegionTree
	{
		public enum Kind
		{
			None,
			Region,
			If,
			Elif,
			Else,

			LastActive,
			
			InactiveRegion,
			InactiveIf,
			InactiveElif,
			InactiveElse
		}
		public Kind kind;
		public FormattedLine line;
		public RegionTree parent;
		public List<RegionTree> children;
	}
	public RegionTree rootRegion = new RegionTree();

	[System.Serializable]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(
		autoUpdateAPI: false, //not sure what's the purpose of this, seems to work whether true or false
		sourceNamespace: null, //or null if did not change
		sourceAssembly: null, //or null if did not change
		sourceClassName: "FormatedLine" //or null if did not change
	)]
	public class FormattedLine
	{
		[System.NonSerialized]
		public BlockState blockState;
		[System.NonSerialized]
		public RegionTree regionTree;
		[SerializeField, HideInInspector]
		public int lastChange = -1;
		[SerializeField, HideInInspector]
		public int savedVersion = -1;
		[System.NonSerialized]
		public List<SyntaxToken> tokens;
		[System.NonSerialized]
		public int laLines;
		[System.NonSerialized]
		public int index;
		
		//public FormattedLine() {}
		
		//public static implicit operator string(FormattedLine fl)
		//{
		//	Debug.LogError("implicit conversion to string: " + fl.text);
		//	return fl.text;
		//}
		
		public string GetRegionName()
		{
			var region = regionTree;
			if (region == null)
				return null;
			
			while (region.parent != null &&
				region.kind != FGTextBuffer.RegionTree.Kind.Region &&
				region.kind != FGTextBuffer.RegionTree.Kind.InactiveRegion)
			{
				region = region.parent;
			}
			
			if (region.parent == null)
				return null;
			
			FormattedLine ppLine = region.line;
			if (ppLine == null)
				return null;
			
			for (var j = ppLine.tokens.Count; j --> 0; )
				if (ppLine.tokens[j].tokenKind == SyntaxToken.Kind.PreprocessorArguments)
					return ppLine.tokens[j].text.GetString();
			
			return null;
		}
	}
	
	[System.Serializable]
	public class CollapsibleTextSpan :
		System.IComparable<CollapsibleTextSpan>,
		System.IEquatable<CollapsibleTextSpan>
	{
		public TextInterval span;
		public string text;
		
		[SerializeField]
		public bool IsCollapsed;
		
		//public bool IsCollapsed
		//{
		//	get { return isCollapsed; }
		//	set { isCollapsed = value; }
		//}
		
		[System.NonSerialized]
		public System.Object owner;
		
		public CollapsibleTextSpan()
		{
		}
		
		public CollapsibleTextSpan(TextPosition from, TextPosition to)
		{
			if (from < to)
			{
				span = new TextInterval(from, to);
			}
			else
			{
				span = new TextInterval(to, from);
			}
		}
		
		public override bool Equals(object obj)
		{
			var asCollapsibleTextSpan = obj as CollapsibleTextSpan;
			if (asCollapsibleTextSpan == null)
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return Equals(asCollapsibleTextSpan);
		}
		
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = (int) 2166136261;
				hash = (hash * 16777619) ^ span.Start.GetHashCode();
				hash = (hash * 16777619) ^ span.End.GetHashCode();
				return hash;
			}
		}
		
		public bool Equals(CollapsibleTextSpan other)
		{
			return span.Start == other.span.Start && span.End == other.span.End;
		}
		
		public int CompareTo(CollapsibleTextSpan other)
		{
			// If other is null, this instance is greater.
			if (other == null)
				return 1;

			var compareStartPosition = span.Start.CompareTo(other.span.Start);
			if (compareStartPosition != 0)
				return compareStartPosition;
			
			return -(span.End.CompareTo(other.span.End));
		}

		public static bool operator > (CollapsibleTextSpan lhs, CollapsibleTextSpan rhs)
		{
			return lhs.CompareTo(rhs) > 0;
		}

		public static bool operator < (CollapsibleTextSpan lhs, CollapsibleTextSpan rhs)
		{
			return lhs.CompareTo(rhs) < 0;
		}

		public static bool operator >= (CollapsibleTextSpan lhs, CollapsibleTextSpan rhs)
		{
			return lhs.CompareTo(rhs) >= 0;
		}

		public static bool operator <= (CollapsibleTextSpan lhs, CollapsibleTextSpan rhs)
		{
			return lhs.CompareTo(rhs) <= 0;
		}
	}
	
	//public class CollapsibleTextSpanEndComparer : IComparer<CollapsibleTextSpan>
	//{
	//	public int Compare(CollapsibleTextSpan lhs, CollapsibleTextSpan rhs)
	//	{
	//		var compareEnds = lhs.End.CompareTo(rhs.End);
	//		if (compareEnds != 0)
	//			return compareEnds;
			
	//		return lhs.Start.CompareTo(rhs.Start);
	//	}
	//}
	
	[SerializeField]
	public List<CollapsibleTextSpan> collapsibleTextSpans = new List<CollapsibleTextSpan>();
	
	public TextIntervalTree<CollapsibleTextSpan> collapsibleTextSpansTree;
	
	[SerializeField, HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("formatedLines")]
	public List<FormattedLine> formattedLines = new List<FormattedLine>();
	[SerializeField, HideInInspector]
	public List<string> lines = new List<string>();
	[SerializeField, HideInInspector]
	public int numFormatedLines = 0;
	[SerializeField, HideInInspector]
	private string lineEnding = "\n";
	[System.NonSerialized]
	public HashSet<string> hyperlinks = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
	[System.NonSerialized]
	private StreamReader streamReader;
	[SerializeField, HideInInspector]
	public int codePage = Encoding.UTF8.CodePage;
	public Encoding fileEncoding
	{
		get
		{
			return isShader || (isText && codePage == Encoding.UTF8.CodePage) ? new UTF8Encoding(false) : Encoding.GetEncoding(codePage);
		}
	}
	[System.NonSerialized]
	public int numParsedLines = 0;
	[SerializeField, HideInInspector]
	public int longestLine = 0;

	[SerializeField, HideInInspector]
	public bool isJsFile = false;
	[SerializeField, HideInInspector]
	public bool isCsFile = false;
	[SerializeField, HideInInspector]
	public bool isBooFile = false;
	[SerializeField, HideInInspector]
	public bool isText = false;
	[SerializeField, HideInInspector]
	public bool isShader = false;
	
	public static int tabSize = 4;
	
	[System.NonSerialized]
	public FGTextEditor.Styles styles = null;

	[SerializeField, HideInInspector]
	public string guid = "";
	public string assetPath { get; private set; }
	[System.NonSerialized]
	public GUIContent assetPathContent;
	[System.NonSerialized]
	public float assetPathContentWidth;
	[System.NonSerialized]
	public GUIContent assetFileNameContent;
	[SerializeField, HideInInspector]
	public bool justSavedNow = false;
	[SerializeField, HideInInspector]
	public bool needsReload = true;
	[System.NonSerializedAttribute]
	public System.DateTime lastModifiedTime;
	[SerializeField, HideInInspector]
	public long lastModifiedTimeInTicks;

	[System.NonSerialized]
	private List<FGTextEditor> editors = new List<FGTextEditor>();

	[SerializeField, HideInInspector]
	public FGTextEditor inspectorEditor;

	public void AddEditor(FGTextEditor editor)
	{
		if (!editors.Contains(editor))
		{
			editors.Add(editor);
			if (!IsLoading && lines.Count > 0)
				editor.ValidateCarets();
		}
	}

	public void RemoveEditor(FGTextEditor editor)
	{
		editors.Remove(editor);
		if (IsModified && editors.Count == 0)
		{
			EditorApplication.update -= AskToSaveOnUpdate;
			EditorApplication.update += AskToSaveOnUpdate;
		}
	}
	
	public void RepaintAllEditors()
	{
		foreach (var item in editors)
			item.Repaint();
	}
	
	public void NotifyAllEditorsOnFold(CollapsibleTextSpan span)
	{
		foreach (var item in editors)
			item.OnFoldText(span.span.Start, span.span.End);
		RepaintAllEditors();
	}
	
	public void NotifyAllEditorsOnUnfold(CollapsibleTextSpan span)
	{
		foreach (var item in editors)
			item.OnUnfoldText(span.span.Start, span.span.End);
		RepaintAllEditors();
	}

	public void AskToSaveOnUpdate()
	{
		EditorApplication.update -= AskToSaveOnUpdate;

		if (!FGCodeWindow.hidingFloatingTabs && !SISettings.alwaysKeepInMemory && IsModified && editors.Count == 0 && !IsAnyWindowMaximized())
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);

			switch (EditorUtility.DisplayDialogComplex(
				"Script Inspector",
				"Save changes to the following asset?          \n\n" + path,
				"Save",
				"Discard Changes",
				"Keep in Memory"))
			{
				case 0:
					if (Save())
						AssetDatabase.ImportAsset(path, ImportAssetOptions.Default);
					//EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript)));
					break;
				case 1:
					FGTextBufferManager.DestroyBuffer(this);
					break;
				case 2:
					break;
			}
		}
	}

	public static FGTextBuffer GetBuffer(UnityEngine.Object target)
	{
		return FGTextBufferManager.GetBuffer(target);
	}

	public void OnEnable()
	{
		hideFlags = HideFlags.HideAndDontSave;
		var assetPathFromGuid = AssetDatabase.GUIDToAssetPath(guid);
		if (!string.IsNullOrEmpty(assetPathFromGuid))
			assetPath = assetPathFromGuid;
//#if SI3_WARNINGS
//		else
//			Debug.LogWarning("GUIDToAssetPath failed for GUID " + guid);
//#endif
		
		//Debug.Log("Enabling <b>textBuffer</b> " + this.assetPath);
		
		if (!string.IsNullOrEmpty(assetPath) && lastModifiedTimeInTicks == 0)
		{
			try {
				lastModifiedTime = File.GetLastWriteTime(assetPath).ToUniversalTime();
				lastModifiedTimeInTicks = lastModifiedTime.Ticks;
			} catch {}
		}
		else
		{
			lastModifiedTime = new System.DateTime(lastModifiedTimeInTicks);
		}

		if (needsReload)
		{
			//Debug.LogError("needsReload == true !!! " + Path.GetFileName(assetPath));
			EditorApplication.update -= ReloadOnNextUpdate;
			EditorApplication.update += ReloadOnNextUpdate;
		}
	}

	public void OnDisable()
	{
		guidsToLoadFirst.Remove(guid);
	}

	public void OnDestroy()
	{
		guidsToLoadFirst.Remove(guid);
	}

	[System.Serializable]
	public class CaretPos : System.IComparable<CaretPos>, System.IEquatable<CaretPos>
	{
		[SerializeField]
		public int virtualColumn;
		[SerializeField]
		public int column;
		[SerializeField]
		public int characterIndex;
		[SerializeField]
		public int line;
		
		public override string ToString()
		{
			return "line: " + line + ", index: " + characterIndex + ", col: " + column + ", vc: " + virtualColumn;
		}

		public CaretPos Clone()
		{
			return new CaretPos { virtualColumn = virtualColumn, column = column, characterIndex = characterIndex, line = line };
		}

		public void Set(int line, int characterIndex, int column)
		{
			this.column = column;
			this.characterIndex = characterIndex;
			this.line = line;
		}

		public void Set(int line, int characterIndex, int column, int virtualColumn)
		{
			this.virtualColumn = virtualColumn;
			this.column = column;
			this.characterIndex = characterIndex;
			this.line = line;
		}

		public void Set(CaretPos other)
		{
			virtualColumn = other.virtualColumn;
			column = other.column;
			characterIndex = other.characterIndex;
			line = other.line;
		}

		public bool IsSameAs(CaretPos other)
		{
			return Equals(other) && column == other.column && virtualColumn == other.virtualColumn;
		}

		public int CompareTo(CaretPos other)
		{
			return line == other.line ? characterIndex - other.characterIndex : line - other.line;
		}
		public static bool operator <  (CaretPos A, CaretPos B) { return A.CompareTo(B) < 0; }
		public static bool operator >  (CaretPos A, CaretPos B) { return A.CompareTo(B) > 0; }
		public static bool operator <= (CaretPos A, CaretPos B) { return A.CompareTo(B) <= 0; }
		public static bool operator >= (CaretPos A, CaretPos B) { return A.CompareTo(B) >= 0; }

		public static bool operator == (CaretPos A, CaretPos B)
		{
			if (object.ReferenceEquals(A, B))
				return true;
			if (object.ReferenceEquals(A, null))
				return false;
			if (object.ReferenceEquals(B, null))
				return false;
			return A.Equals(B);
		}
		public static bool operator != (CaretPos A, CaretPos B) { return !(A == B); }

		public bool Equals(CaretPos other)
		{
			return line == other.line && characterIndex == other.characterIndex;
		}

		public override bool Equals(System.Object obj)
		{
			if (obj == null)
				return false;

			if (!(obj is CaretPos))
				return false;
			else
				return Equals((CaretPos) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = (int) 2166136261;
				hash = (hash * 16777619) ^ line;
				hash = (hash * 16777619) ^ characterIndex;
				return hash;
			}
		}
	}

	[SerializeField, HideInInspector]
	private bool initialized = false;

	public void InitializeWithCSharpCode(string content)
	{
		tabSize = SISettings.tabSize;
		
		assetPath = guid;
		
		isJsFile = false;
		isCsFile = true;
		isBooFile = false;
		isShader = false;
		isText = false;
		
		styles = isText ? FGTextEditor.StylesText : FGTextEditor.StylesCode;
		
		needsReload = false;
		lastModifiedTime = new System.DateTime();
		lastModifiedTimeInTicks = 0;
		
		parser = FGParser.Create(this, assetPath);
		//Debug.Log("Parser created: " + parser + " for " + System.IO.Path.GetFileName(assetPath));
		
		lines = new List<string>(content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
		lineEnding = "\n";
		codePage = Encoding.UTF8.CodePage;
		
		hyperlinks.Clear();
		longestLine = 0;
		savedAtUndoPosition = undoPosition;
		
		formattedLines.Clear();
		if (formattedLines.Capacity < lines.Count)
			formattedLines.Capacity = lines.Count;
		ReformatLines(0, lines.Count - 1);
		numParsedLines = lines.Count;
		
		UpdateViews();
		ValidateCarets();
		
		parser.OnLoaded();
	}
	
	public void Initialize()
	{
		tabSize = SISettings.tabSize;
		
		var assetPathFromGuid = AssetDatabase.GUIDToAssetPath(guid);
		if (string.IsNullOrEmpty(assetPathFromGuid))
			return;
		assetPath = assetPathFromGuid;
		
		isJsFile = assetPath.EndsWithJS();
		isCsFile = assetPath.EndsWithCS();
		isBooFile = assetPath.EndsWithBoo();
		isShader = assetPath.EndsWithIgnoreCase(".shader") ||
			assetPath.EndsWithIgnoreCase(".cg") ||
			assetPath.EndsWithIgnoreCase(".cginc") ||
			assetPath.EndsWithIgnoreCase(".hlsl") ||
			assetPath.EndsWithIgnoreCase(".hlslinc");
		isText = !(isJsFile || isCsFile || isBooFile || isShader);
		
		if (collapsibleTextSpansTree == null)
		{
			collapsibleTextSpansTree = new TextIntervalTree<CollapsibleTextSpan>();
			
			for (var i = collapsibleTextSpans.Count; i --> 0; )
			{
				var item = collapsibleTextSpans[i];
				
				if (!collapsibleTextSpansTree.Add(item.span, item))
				{
					Debug.LogError("CollapsibleTextSpan not added: " + item.span);
					collapsibleTextSpans.RemoveAt(i);
				}
			}
		}
		
		styles = FGTextEditor.GetStyles(isText);

		if (!needsReload && initialized && numParsedLines > 0)
			return;
		
		if (needsReload || lines == null || lines.Count == 0)
		{
			try {
				lastModifiedTime = File.GetLastWriteTime(assetPath).ToUniversalTime();
				lastModifiedTimeInTicks = lastModifiedTime.Ticks;
			} catch {}
			
			parser = FGParser.Create(this, assetPath);
			//Debug.Log("Parser created: " + parser + " for " + System.IO.Path.GetFileName(assetPath));

			lines.Clear();
			lineEnding = "\n";
			try
			{
				Stream stream = new BufferedStream(new FileStream(assetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), 1024);
				streamReader = new StreamReader(stream, true);
				codePage = Encoding.UTF8.CodePage;
			}
			catch (System.Exception error)
			{
				Debug.LogError("Could not read the content of '" + assetPath + "' because of the following error:");
				Debug.LogError(error);
				if (streamReader != null)
				{
					streamReader.Close();
					streamReader.Dispose();
					streamReader = null;
				}
				lastModifiedTime = new System.DateTime();
				lastModifiedTimeInTicks = 0;
			}

			formattedLines.Clear();
			hyperlinks.Clear();
			longestLine = 0;
			numParsedLines = 0;
			savedAtUndoPosition = undoPosition;

			EditorApplication.update -= ProgressiveLoadOnUpdate;
			EditorApplication.update += ProgressiveLoadOnUpdate;
		}
		else if (numParsedLines == 0)
		{
			if (parser == null)
			{
				parser = FGParser.Create(this, assetPath);
			}

			EditorApplication.update -= ProgressiveLoadOnUpdate;
			EditorApplication.update += ProgressiveLoadOnUpdate;
		}
		else
		{
			initialized = true;
		}
		
		if (IsLoading)
			ProgressiveLoadOnUpdate();
	}
	
	public void LoadImmediately()
	{
		//Debug.Log("<color=\"red\">Load immediately</color> " + assetPath);
		
		if (IsLoading)
		{
			EditorApplication.update -= ReloadOnNextUpdate;
			EditorApplication.update -= ProgressiveLoadOnUpdate;
			
			if (streamReader == null)
				Initialize();
			//Debug.LogWarning("LoadImmediately " + assetPath);
			while (IsLoading)
				ProgressiveLoadOnUpdate();
			ProgressiveLoadOnUpdate();
		}
    }

    public void Reload()
	{
		try
		{
			var lastModifiedTime = File.GetLastWriteTime(assetPath).ToUniversalTime();
			justSavedNow = lastModifiedTime == this.lastModifiedTime;
		} catch {}
		
		needsReload = needsReload && !justSavedNow;
		EditorApplication.update -= ReloadOnNextUpdate;
		EditorApplication.update += ReloadOnNextUpdate;
		EditorApplication.update -= ProgressiveLoadOnUpdate;
	}

	private void ReloadOnNextUpdate()
	{
		EditorApplication.update -= ReloadOnNextUpdate;

		if (justSavedNow)
		{
			justSavedNow = false;
			RescanHyperlinks();

			if (parser == null)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				parser = FGParser.Create(this, assetPath);
			}

			UpdateViews();
		}
		else
		{
			FGCodeWindow.CheckAssetRename(guid);

			if (IsModified)
			{
				if (!EditorUtility.DisplayDialog(
					"Script Inspector",
					AssetDatabase.GUIDToAssetPath(guid)
						+ "\n\nThis asset has been modified outside of Unity Editor.\nDo you want to reload it and lose the changes made in Script Inspector?",
					"Reload",
					"Keep changes"))
				{
					needsReload = false;

					savedAtUndoPosition = -1;
					UpdateViews();
					return;
				}
			}

			ReloadNow();
		}
	}

	void ReloadNow()
	{
		RemoveAllCollapsibleTextSpans();
		
		formattedLines.Clear();
		lines.Clear();
		lineEnding = "\n";
		hyperlinks.Clear();
		if (streamReader != null)
		{
			streamReader.Close();
			streamReader.Dispose();
			streamReader = null;
		}
		codePage = Encoding.UTF8.CodePage;
		numParsedLines = 0;
		longestLine = 0;

		isJsFile = false;
		isCsFile = false;
		isBooFile = false;
		isShader = false;
		isText = false;

		undoBuffer = new List<UndoRecord>();
		undoPosition = 0;
		currentChangeId = 0;
		savedAtUndoPosition = 0;
		//recordUndo = true;
		//beginEditDepth = 0;

		initialized = false;
		Initialize();
	}

	public void RescanHyperlinks()
	{
		hyperlinks.Clear();
		//foreach (var line in formattedLines)
		//{
		//	if (line.tokens == null)
		//		continue;
		
		//	foreach (var token in line.tokens)
		//	{
		//		if (token.style == styles.mailtoStyle || token.style == styles.hyperlinkStyle)
		//		{
		//			string text = token.text;
		//			if (token.style == styles.hyperlinkStyle && text.EndsWith("/"))
		//				text.Remove(text.Length - 1);
		//			hyperlinks.Add(text);
		//		}
		//	}
		//}
	}

	public delegate void OnPreSaveDelegate(List<string> lines);
	public static OnPreSaveDelegate onPreSaveCSharp;

	static List<int> tempIntList = new List<int>();

	bool OnPreSave()
	{
		if (!isCsFile)
			return true;

		if (onPreSaveCSharp == null)
			return true;

		var linesCopy = new List<string>(lines);
		try
		{
			onPreSaveCSharp(linesCopy);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
			return true;
		}

		if (linesCopy.Count != lines.Count)
		{
			Debug.LogError("Changing the number of lines by onPreSaveCSharp callback is not supported... Ignoring changes!");
			return true;
		}

		var savedSettings = SISettings.insertSpacesOnTab.Value;

		tempIntList.Clear();
		for (int i = 0; i < lines.Count; ++i)
		{
			var line = lines[i];
			var copy = linesCopy[i];
			if (line == copy)
				continue;

			if (copy == null || copy.IndexOf('\n') >= 0)
			{
				Debug.LogError("Changing the number of lines by onPreSaveCSharp callback is not supported... Ignoring changes on line " + (i + 1));
				continue;
			}

			if (tempIntList.Count == 0)
			{
				SISettings.insertSpacesOnTab.Value = false;
				BeginEdit("Auto-format on Save");
			}

			var from = new CaretPos { line = i, characterIndex = 0, column = 0, virtualColumn = 0 };
			var to = new CaretPos{ line = i, characterIndex = line.Length, column = line.Length, virtualColumn = line.Length };
			if (line != "")
				from = DeleteText(from, to);
			if (copy != "")
				to = InsertText(from, copy);

			tempIntList.Add(i);
		}

		if (tempIntList.Count != 0)
		{
			foreach (var i in tempIntList)
			{
				UpdateHighlighting(i, i);
			}
			tempIntList.Clear();

			ValidateCarets();
			EndEdit();

			// Force creating an undo record
			EndEdit();
			BeginEdit("change");

			SISettings.insertSpacesOnTab.Value = savedSettings;
		}

		return true;
	}

	public bool Save()
	{
		if (!TryEdit())
			return false;

		var assetPath = AssetDatabase.GUIDToAssetPath(guid);
		if (string.IsNullOrEmpty(assetPath))
			return false;

		if (!OnPreSave())
			return false;

		var result = true;
		justSavedNow = true;

		StreamWriter writer = null;
		try
		{
			using (writer = new StreamWriter(assetPath, false, fileEncoding))
			{
				writer.NewLine = lineEnding;
				int numLines = lines.Count;
				for (int i = 0; i < numLines - 1; ++i)
					writer.WriteLine(lines[i]);
				writer.Write(lines[numLines - 1]);
				writer.Close();
			}

			for (int i = 0; i < numParsedLines; ++i)
				formattedLines[i].savedVersion = formattedLines[i].lastChange;

			savedAtUndoPosition = undoPosition;

			foreach (UndoRecord record in undoBuffer)
				foreach (UndoRecord.TextChange change in record.changes)
					change.savedVersions = null;
		}
		catch
		{
			if (writer != null)
			{
				writer.Close();
				writer.Dispose();
			}
			EditorUtility.DisplayDialog("Error Saving Script", "The script '" + AssetDatabase.GUIDToAssetPath(guid) + "' could not be saved!", "OK");
			justSavedNow = false;
			result = false;
		}

		if (writer != null)
		{
			writer.Close();
			writer.Dispose();
		}
		
		try {
			lastModifiedTime = File.GetLastWriteTime(assetPath).ToUniversalTime();
			lastModifiedTimeInTicks = lastModifiedTime.Ticks;
		} catch {}
		
		UpdateViews();
		
		return result;
	}
	
	private static bool IsAnyWindowMaximized()
	{
		System.Type maximizedType = typeof(EditorWindow).Assembly.GetType("UnityEditor.MaximizedHostView");
		return Resources.FindObjectsOfTypeAll(maximizedType).Length != 0;
	}

	public delegate void ChangeDelegate();
	public ChangeDelegate onChange;

	public void UpdateViews()
	{
		if (onChange != null)
			onChange();
	}

	private static List<string> guidsToLoadFirst = new List<string>();

	public void LoadFaster()
	{
		//if (!guidsToLoadFirst.Contains(guid))
		//	guidsToLoadFirst.Add(guid);
	}

	public void ProgressiveLoadOnUpdate()
	{
		initialized = true;

		if (guidsToLoadFirst.Count > 0 && !guidsToLoadFirst.Contains(guid))
			return;
		
		if (streamReader != null)
		{
			try
			{
				Parse(numParsedLines + 128);
			}
			catch (System.Exception error)
			{
				Debug.LogError("Could not read the content of '" + AssetDatabase.GUIDToAssetPath(guid) + "' because of the following error:");
				Debug.LogError(error);
				if (streamReader != null)
				{
					streamReader.Close();
					streamReader.Dispose();
					streamReader = null;
				}
			}

			if (streamReader == null)
			{
				//if (searchString != string.Empty)
				//    SetSearchText(searchString);
				//focusCodeView = false;
				for (int i = formattedLines.Count; i-- > 0; )
					formattedLines[i].lastChange = -1;
				UpdateViews();
			}
		}
		else if (lines.Count == 0)
		{
			// Loading failed...
			lines.Add(string.Empty);
			formattedLines.Add(new FormattedLine { index = 0, lastChange = -1 });
		}
		else if (numParsedLines < lines.Count)
		{
			int toLine = System.Math.Min(numParsedLines + 128, lines.Count - 1);
			ReformatLines(numParsedLines, toLine);
			numParsedLines = toLine + 1;
			//UpdateViews();
		}
		else
		{
			needsReload = false;
			EditorApplication.update -= ProgressiveLoadOnUpdate;
			guidsToLoadFirst.Remove(guid);

			if (parser != null)
			{
				parser.OnLoaded();
			}

			ValidateCarets();
		}
	}

	public void ValidateCarets()
	{
		foreach (FGTextEditor editor in editors)
			editor.ValidateCarets();
	}
	
	public int GetUndoChangeId()
	{
		if (!CanUndo())
			return 0;
		return undoBuffer[undoPosition - 1].changeId;
	}
	
	public int GetRedoChangeId()
	{
		if (!CanRedo())
			return 0;
		return undoBuffer[undoPosition].changeId;
	}

	public bool CanUndo()
	{
		return undoPosition > 0;
	}

	public bool CanRedo()
	{
		return undoPosition < undoBuffer.Count;
	}

	public void Undo()
	{
		if (!CanUndo())
			return;

		recordUndo = false;
		
		int updateFrom = int.MaxValue;
		int updateTo = -1;

		UndoRecord record = undoBuffer[--undoPosition];
		for (int i = record.changes.Count; i-- != 0; )
		{
			UndoRecord.TextChange change = record.changes[i];

			int changeFromLine = change.from.line;
			int changeToLine = change.to.line;
			if (changeFromLine > changeToLine)
			{
				int temp = changeToLine;
				changeToLine = changeFromLine;
				changeFromLine = temp;
			}

			int[] tempSavedVersions = null;

			CaretPos insertAt = change.from.Clone();
			if (change.newText != string.Empty)
			{
				// Undo inserting text
				string[] textLines = change.newText.Split('\n');
				CaretPos to = change.from.Clone();
				to.characterIndex = textLines.Length > 1 ? textLines[textLines.Length - 1].Length
					: to.characterIndex + change.newText.Length;
				to.line += textLines.Length - 1;
				to.virtualColumn = to.column = CharIndexToColumn(to.characterIndex, to.line);

				int numLinesChanging = 1 + to.line - changeFromLine;

				tempSavedVersions = new int[numLinesChanging];
				for (int j = 0; j < numLinesChanging; ++j)
					tempSavedVersions[j] = formattedLines[changeFromLine + j].savedVersion;

				insertAt = DeleteText(change.from, to);
				
				if (updateFrom > insertAt.line)
					updateFrom = insertAt.line;
				if (updateTo > insertAt.line)
					updateTo -= numLinesChanging - 1;
				if (updateTo < insertAt.line)
					updateTo = insertAt.line;
			}
			if (change.oldText != string.Empty)
			{
				// Undo deleting text
				CaretPos insertedTo = InsertText(insertAt, change.oldText);
				
				if (updateFrom > insertAt.line)
					updateFrom = insertAt.line;
				if (updateTo < insertAt.line)
					updateTo = insertAt.line;
				//if (updateTo >= insertAt.line)
				updateTo += insertedTo.line - insertAt.line;
			}
			//UpdateHighlighting(changeFromLine, changeToLine);
			for (int j = change.oldLineChanges.Length; j-- > 0; )
			{
				formattedLines[j + changeFromLine].lastChange = change.oldLineChanges[j];
				if (change.savedVersions != null && change.savedVersions.Length == 1 + changeToLine - changeFromLine)
				{
					formattedLines[j + changeFromLine].savedVersion = change.savedVersions[j];
				}
			}

			change.savedVersions = tempSavedVersions;
		}
		
		var editor = activeEditor != null && activeEditor.TextBuffer == this ? activeEditor : null;
		if (editor != null && record.preCaretPos.line >= 0)
		{
			editor.caretPosition = record.preCaretPos.Clone();
			if (record.preCaretPos == record.preSelectionPos)
				editor.selectionStartPosition = null;
			else
				editor.selectionStartPosition = record.preSelectionPos.Clone();
			editor.caretMoveTime = FGTextEditor.frameTime;
			editor.scrollToCaret = true;
		}
		
		//var preserveTo = Mathf.Min(updateTo + 1, formattedLines.Length - 1);
		//var lastChanges = new int[preserveTo - updateFrom + 1];
		//for (int i = updateFrom; i <= preserveTo; ++i)
		//	lastChanges[i - updateFrom] = formattedLines[i].lastChange;
		UpdateHighlighting(updateFrom, updateTo, true);
		//for (int i = updateFrom; i <= preserveTo; ++i)
		//	formattedLines[i].lastChange = lastChanges[i - updateFrom];
		
		recordUndo = true;
		
		foreach (var e in editors)
			e.UnfoldAtCaret();
	}

	public void Redo()
	{
		if (!CanRedo())
			return;

		recordUndo = false;

		int updateFrom = int.MaxValue;
		int updateTo = -1;

		UndoRecord record = undoBuffer[undoPosition++];

		for (int i = 0; i < record.changes.Count; ++i)
		{
			UndoRecord.TextChange change = record.changes[i];

			int changeFromLine = change.from.line;
			int changeToLine = change.to.line;
			if (changeFromLine > changeToLine)
			{
				int temp = changeToLine;
				changeToLine = changeFromLine;
				changeFromLine = temp;
			}

			int numLinesChanging = 1 + changeToLine - changeFromLine;

			int[] tempSavedVersions = new int[numLinesChanging];
			for (int j = numLinesChanging; j-- > 0; )
				tempSavedVersions[j] = formattedLines[j + changeFromLine].savedVersion;

			CaretPos newPos = change.from.Clone();
			if (change.oldText != string.Empty)
			{
				// Redo deleting text
				newPos = DeleteText(change.from, change.to);
				
				if (updateFrom > newPos.line)
					updateFrom = newPos.line;
				if (updateTo > newPos.line)
					updateTo -= numLinesChanging - 1;
				if (updateTo < newPos.line)
					updateTo = newPos.line;
			}
			if (change.newText != string.Empty)
			{
				// Redo inserting text
				newPos = InsertText(newPos, change.newText);
				
				if (updateFrom > changeFromLine)
					updateFrom = changeFromLine;
				if (updateTo < changeFromLine)
					updateTo = changeFromLine;
				//if (updateTo >= changeFromLine)
				updateTo += newPos.line - changeFromLine;
			}
			//UpdateHighlighting(changeFromLine, newPos.line);
			for (int j = changeFromLine; j <= newPos.line; ++j)
			{
				formattedLines[j].lastChange = record.changeId;
				if (change.savedVersions != null && change.savedVersions.Length != 0)
				{
					formattedLines[j].savedVersion = change.savedVersions[j - changeFromLine];
				}
			}

			change.savedVersions = tempSavedVersions;
		}
		var editor = activeEditor != null && activeEditor.TextBuffer == this ? activeEditor : null;
		if (editor != null && record.postCaretPos.line >= 0)
		{
			editor.caretPosition = record.postCaretPos.Clone();
			if (record.postCaretPos == record.postSelectionPos)
				editor.selectionStartPosition = null;
			else
				editor.selectionStartPosition = record.postSelectionPos.Clone();
			editor.caretMoveTime = FGTextEditor.frameTime;
			editor.scrollToCaret = true;
		}

		//var preserveTo = Mathf.Min(updateTo + 1, formattedLines.Length - 1);
		//var lastChanges = new int[preserveTo - updateFrom + 1];
		//for (int i = updateFrom; i <= preserveTo; ++i)
		//	lastChanges[i - updateFrom] = formattedLines[i].lastChange;
		UpdateHighlighting(updateFrom, updateTo, true);
		//for (int i = updateFrom; i <= preserveTo; ++i)
		//	formattedLines[i].lastChange = lastChanges[i - updateFrom];
		
		recordUndo = true;
		
		foreach (var e in editors)
			e.UnfoldAtCaret();
	}

	public int CharIndexToColumn(int charIndex, int line, int start)
	{
		if (line >= lines.Count)
			return 0;
		string s = lines[line];
		if (s.Length < charIndex)
			charIndex = s.Length;

		int col = 0;
		if (tabSize == 4)
			for (int i = start; i < charIndex; ++i)
				col += s[i] != '\t' ? 1 : 4 - (col & 3);
		else if (tabSize == 2)
			for (int i = start; i < charIndex; ++i)
				col += s[i] != '\t' ? 1 : 2 - (col & 1);
		else
			for (int i = start; i < charIndex; ++i)
				col += s[i] != '\t' ? 1 : tabSize - (col % tabSize);
		return col;
	}

	public int CharIndexToColumn(int charIndex, int line)
	{
		if (line >= lines.Count)
			return 0;
		string s = lines[line];
		if (s.Length < charIndex)
			charIndex = s.Length;

		int col = 0;
		for (int i = 0; i < charIndex; ++i)
			col += s[i] != '\t' ? 1 : tabSize - (col % tabSize);
		return col;
	}

	public int ColumnToCharIndex(ref int column, int line, int rowStart)
	{
		line = System.Math.Max(0, System.Math.Min(line, lines.Count - 1));
		column = System.Math.Max(0, column);

		if (lines.Count == 0)
			return 0;
		var s = lines[line];

		var i = rowStart;
		var col = 0;
		while (i < s.Length && col < column)
		{
			if (s[i] == '\t')
				col += tabSize - (col % tabSize);
			else
				++col;
			++i;
		}
		if (i == s.Length)
		{
			column = col;
		}
		else if (col > column)
		{
			var mod = col % tabSize;
			if (mod < tabSize / 2)
			{
				--col;
				--i;
				column -= column % tabSize;
			}
			else
			{
				column += tabSize - (column % tabSize);
			}
		}
		return i;
	}

	public int ColumnToCharIndex(ref int column, int line)
	{
		line = System.Math.Max(0, System.Math.Min(line, numParsedLines - 1));
		column = System.Math.Max(0, column);

		if (lines.Count == 0 || line >= lines.Count)
			return 0;
		string s = lines[line];

		int i = 0;
		int col = 0;
		while (i < s.Length && col < column)
		{
			if (s[i] == '\t')
				col += tabSize - (col % tabSize);
			else
				++col;
			++i;
		}
		if (i == s.Length)
		{
			column = col;
		}
		else if (col > column)
		{
			if ((column % tabSize) < tabSize / 2)
			{
				--col;
				--i;
				column -= column % tabSize;
			}
			else
			{
				column += tabSize - (column % tabSize);
			}
		}
		return i;
	}

	public string GetTextRange(CaretPos from, CaretPos to)
	{
		int fromCharIndex, fromLine, toCharIndex, toLine;
		if (from < to)
		{
			fromCharIndex = from.characterIndex;
			fromLine = from.line;
			toCharIndex = to.characterIndex;
			toLine = to.line;
		}
		else
		{
			fromCharIndex = to.characterIndex;
			fromLine = to.line;
			toCharIndex = from.characterIndex;
			toLine = from.line;
		}

		StringBuilder buffer = new StringBuilder();
		if (fromLine == toLine)
		{
			try
			{
				var substring = lines[fromLine].Substring(fromCharIndex, toCharIndex - fromCharIndex);
				buffer.Append(substring);
			}
			catch
			{
				Debug.Log(lines[fromLine] + " (L: " + lines[fromLine].Length + ")");
				Debug.Log("fromCharIndex: " + fromCharIndex + " toCharIndex: " + toCharIndex);
			}
		}
		else
		{
			buffer.Append(lines[fromLine].Substring(fromCharIndex) + "\n");
			for (int i = fromLine + 1; i < toLine; ++i)
			{
				buffer.Append(lines[i]);
				buffer.Append('\n');
			}
			buffer.Append(lines[toLine].Substring(0, toCharIndex));
		}

		return buffer.ToString();
	}

	public static int GetCharClass(char c, bool digitsAsLetters = false, bool ignorePunctuations = false)
	{
		if (c == ' ' || c == '\t')
			return 0;
		if (c >= '0' && c <= '9')
			return 1;
		if (c == '_' || char.IsLetter(c))
			return digitsAsLetters ? 1 : 2;
		return ignorePunctuations ? 0 : 3;
	}

	public bool GetWordExtents(int charIndex, int line, out int wordStart, out int wordEnd)
	{
		wordStart = charIndex;
		wordEnd = charIndex;
		if (line >= formattedLines.Count)
			return false;

		string text = lines[line];
		int length = text.Length;
		wordStart = wordEnd = System.Math.Min(charIndex, length - 1);
		if (wordStart < 0)
			return false;

		int cc = GetCharClass(text[wordStart]);
		if (wordStart > 0 && cc == 0)
		{
			--wordStart;
			cc = GetCharClass(text[wordStart]);
			if (cc != 0)
				--wordEnd;
		}
		if (cc == 3)
		{
			++wordEnd;
		}
		else if (cc == 0)
		{
			while (wordStart > 0 && GetCharClass(text[wordStart - 1]) == 0)
				--wordStart;
			while (wordEnd < length && GetCharClass(text[wordEnd]) == 0)
				++wordEnd;
		}
		else
		{
			while (wordStart > 0)
			{
				char ch = text[wordStart - 1];
				int c = GetCharClass(ch);
				if (c == 1 || c == 2 || cc == 1 && ch == '.')
					--wordStart;
				else
					break;
				cc = c;
			}
			while (wordEnd < length)
			{
				int c = GetCharClass(text[wordEnd]);
				if (c == 1 || c == 2 || cc == 1 && text[wordEnd] == '.')
					++wordEnd;
				else
					break;
			}
		}
		return true;
	}

	public CaretPos WordStopLeft(CaretPos from, bool stopOnSubwords)
	{
		bool ignorePunctuations = SISettings.wordBreak_IgnorePunctuations;
		int column = from.characterIndex;
		int line = from.line;

		if (column == 0)
		{
			if (line == 0)
				return new CaretPos { characterIndex = 0, column = 0, line = 0, virtualColumn = 0 };

			--line;
			column = lines[line].Length;
		}

		string s = lines[line];

		if (column > 0)
		{
			if (stopOnSubwords)
			{
				int characterClass = GetCharClass(s[--column], false, ignorePunctuations);
				
				while (column > 0 && characterClass == 0)
					characterClass = GetCharClass(s[--column], false, ignorePunctuations);
				
				var thisChar = s[column];
				while (column > 0)
				{
					var prevChar = s[column - 1];
					var prevCharClass = GetCharClass(prevChar, false, ignorePunctuations);
					if (!SISettings.wordBreak_IgnorePunctuations)
					{
						if (prevCharClass != characterClass)
							break;
					}
					else
					{
						if (prevCharClass != 2 && characterClass == 2)
							break;
						if (prevCharClass != 1 && characterClass == 1)
							break;
					}
					if (prevChar == '_' && thisChar != '_')
						break;
					var isThisUpper = char.IsUpper(thisChar);
					var isPrevUpper = char.IsUpper(prevChar);
					if (isThisUpper && !isPrevUpper)
						break;
					--column;
					if (isPrevUpper && !isThisUpper)
						break;
					thisChar = prevChar;
				}
			}
			else
			{
				int characterClass = GetCharClass(s[--column], true, ignorePunctuations);
				
				while (column > 0 && characterClass == 0)
					characterClass = GetCharClass(s[--column], true, ignorePunctuations);
				
				while (column > 0 && GetCharClass(s[column - 1], true, ignorePunctuations) == characterClass)
					--column;
			}
			
			if (column == 0)
			{
				if (line == 0)
					return new CaretPos { characterIndex = 0, column = 0, line = 0, virtualColumn = 0 };
				
				--line;
				column = lines[line].Length;
			}
		}

		return new CaretPos { characterIndex = column, column = CharIndexToColumn(column, line), line = line, virtualColumn = column };
	}

	public CaretPos WordStopRight(CaretPos from, bool stopOnSubwords)
	{
		bool ignorePunctuations = SISettings.wordBreak_IgnorePunctuations;
		int column = from.characterIndex;
		int line = from.line;

		if (column >= lines[line].Length)
		{
			if (line == lines.Count - 1)
				return new CaretPos { characterIndex = column, column = CharIndexToColumn(column, line), line = line, virtualColumn = column };

			++line;
			column = 0;
		}

		string s = lines[line];

		if (column < s.Length)
		{
			int characterClass = GetCharClass(s[column++], !stopOnSubwords, ignorePunctuations);
			
			if (SISettings.wordBreak_RightArrowStopsAtWordEnd)
			{
				if (characterClass == 0)
				{
					while (column < s.Length)
					{
						characterClass = GetCharClass(s[column++], !stopOnSubwords, ignorePunctuations);
						if (characterClass != 0)
							break;
					}
					
					if (column >= s.Length)
						return new CaretPos { characterIndex = column, column = CharIndexToColumn(column, line), line = line, virtualColumn = column };
				}
			}
			
			if (characterClass != 0)
			{
				if (stopOnSubwords)
				{
					char thisChar = column > 0 ? s[column - 1] : 'A';
					char nextChar = '\0';
					int nextClass = 4;
					while (column < s.Length)
					{
						nextChar = s[column];
						nextClass = GetCharClass(nextChar, false, ignorePunctuations);
						if (nextClass != characterClass)
						{
							characterClass = nextClass;
							break;
						}
						else
						{
							var isThisUpper = char.IsUpper(thisChar);
							var isNextUpper = char.IsUpper(nextChar);
							
							if (!isThisUpper && thisChar != '_' && isNextUpper)
								break;
							if (SISettings.wordBreak_RightArrowStopsAtWordEnd ?
								thisChar != '_' && nextChar == '_' :
								thisChar == '_' && nextChar != '_')
							{
								break;
							}
							if (isThisUpper && isNextUpper)
							{
								if (column + 1 < s.Length)
								{
									var c = s[column + 1];
									if (GetCharClass(c, false, ignorePunctuations) == 2 && !char.IsUpper(c))
										break;
								}
							}
							++column;
						}
						thisChar = nextChar;
						characterClass = nextClass;
					}
					if (characterClass == 2 && nextClass == 2 &&
						!SISettings.wordBreak_RightArrowStopsAtWordEnd && thisChar != '_')
					{
						while (column < s.Length && nextChar == '_')
							nextChar = s[++column];
					}
				}
				else
				{
					while (column < s.Length)
					{
						int nextClass = GetCharClass(s[column], true, ignorePunctuations);
						if (nextClass != characterClass)
						{
							characterClass = nextClass;
							break;
						}
						else
						{
							++column;
						}
					}
				}
			}

			if (!SISettings.wordBreak_RightArrowStopsAtWordEnd)
			{
				if (characterClass == 0)
					while (column < s.Length && GetCharClass(s[column], false, ignorePunctuations) == 0)
						++column;
			}
		}

		return new CaretPos { characterIndex = column, column = CharIndexToColumn(column, line), line = line, virtualColumn = column };
	}

	[System.Serializable]
	private class UndoRecord
	{
		[System.Serializable]
		public class TextChange
		{
			[SerializeField, HideInInspector]
			public CaretPos from;
			[SerializeField, HideInInspector]
			public CaretPos to;
			[SerializeField, HideInInspector]
			public string oldText;
			[SerializeField, HideInInspector]
			public string newText;
			[SerializeField, HideInInspector]
			public int[] oldLineChanges;
			[SerializeField, HideInInspector]
			public int[] savedVersions;
		}
		[SerializeField, HideInInspector]
		public List<TextChange> changes;
		[SerializeField, HideInInspector]
		public int changeId;
		[SerializeField, HideInInspector]
		public CaretPos preCaretPos;
		[SerializeField, HideInInspector]
		public CaretPos preSelectionPos;
		[SerializeField, HideInInspector]
		public CaretPos postCaretPos;
		[SerializeField, HideInInspector]
		public CaretPos postSelectionPos;
		[SerializeField, HideInInspector]
		public string actionType;
	}
	[SerializeField, HideInInspector]
	private List<UndoRecord> undoBuffer = new List<UndoRecord>();
	[System.NonSerialized]
	private UndoRecord tempUndoRecord;
	[SerializeField, HideInInspector]
	public int undoPosition = 0;
	[SerializeField, HideInInspector]
	public int savedAtUndoPosition = 0;
	[SerializeField, HideInInspector]
	public int currentChangeId = 0;
	[System.NonSerialized]
	private bool recordUndo = true;
	[System.NonSerialized]
	private int beginEditDepth = 0;
	[System.NonSerialized]
	private List<FormattedLine> updatedLines = new List<FormattedLine>();

	public bool IsModified { get { return undoPosition != savedAtUndoPosition; } }
	public bool IsLoading { get { return needsReload || streamReader != null || numParsedLines != lines.Count || lines.Count == 0; } }

	public void BeginEdit(string description)
	{
		var editor = activeEditor != null && activeEditor.TextBuffer == this ? activeEditor : null;
		
		if (!recordUndo)
			return;

		if (beginEditDepth++ == 0)
		{
			var caretPosition = editor != null ? editor.caretPosition : new CaretPos { line = -1 };
			var selectionStartPosition = editor != null ? editor.selectionStartPosition : null;
			
			tempUndoRecord = tempUndoRecord ?? new UndoRecord();
			tempUndoRecord.changes = tempUndoRecord.changes ?? new List<UndoRecord.TextChange>();
			tempUndoRecord.changeId = currentChangeId + 1;
			tempUndoRecord.actionType = description;
			if (tempUndoRecord.preCaretPos == null)
			{
				tempUndoRecord.preCaretPos = caretPosition.Clone();
			}
			else
			{
				tempUndoRecord.preCaretPos.line = caretPosition.line;
				tempUndoRecord.preCaretPos.characterIndex = caretPosition.characterIndex;
				tempUndoRecord.preCaretPos.column = caretPosition.column;
				tempUndoRecord.preCaretPos.virtualColumn = caretPosition.virtualColumn;
			}
			if (selectionStartPosition != null)
			{
				if (tempUndoRecord.preSelectionPos == null)
				{
					tempUndoRecord.preSelectionPos = selectionStartPosition.Clone();
				}
				else
				{
					tempUndoRecord.preSelectionPos.line = selectionStartPosition.line;
					tempUndoRecord.preSelectionPos.characterIndex = selectionStartPosition.characterIndex;
					tempUndoRecord.preSelectionPos.column = selectionStartPosition.column;
					tempUndoRecord.preSelectionPos.virtualColumn = selectionStartPosition.virtualColumn;
				}
			}
			else
			{
				if (tempUndoRecord.preSelectionPos == null)
				{
					tempUndoRecord.preSelectionPos = caretPosition.Clone();
				}
				else
				{
					tempUndoRecord.preSelectionPos.line = caretPosition.line;
					tempUndoRecord.preSelectionPos.characterIndex = caretPosition.characterIndex;
					tempUndoRecord.preSelectionPos.column = caretPosition.column;
					tempUndoRecord.preSelectionPos.virtualColumn = caretPosition.virtualColumn;
				}
			}

			if (updatedLines != null)
				updatedLines.Clear();
			else
				updatedLines = new List<FormattedLine>();
		}
	}

	private void RegisterUndoText(string actionType, CaretPos from, CaretPos to, string text)
	{
		if (!recordUndo)
			return;

		UndoRecord.TextChange change = new UndoRecord.TextChange();
		if (from < to)
		{
			change.from = from.Clone();
			change.to = to.Clone();
		}
		else
		{
			change.from = to.Clone();
			change.to = from.Clone();
		}
		change.oldText = GetTextRange(change.from, change.to);
		change.newText = text;
		change.oldLineChanges = new int[1 + change.to.line - change.from.line];
		change.savedVersions = new int[1 + change.to.line - change.from.line];
		for (int i = change.oldLineChanges.Length; i-- > 0; )
		{
			change.oldLineChanges[i] = formattedLines[i + change.from.line].lastChange;
			change.savedVersions[i] = formattedLines[i + change.from.line].savedVersion;
		}
		tempUndoRecord.changes.Add(change);

		tempUndoRecord.actionType = actionType;
	}

	public static FGTextEditor activeEditor = null;

	public void EndEdit()
	{
		if (!recordUndo)
			return;

		if (--beginEditDepth > 0)
			return;

		if (tempUndoRecord == null || tempUndoRecord.changes == null || tempUndoRecord.changes.Count == 0)
			return;

		var editor = activeEditor != null && activeEditor.TextBuffer == this ? activeEditor : null;
		
		tempUndoRecord.postCaretPos = editor != null ? editor.caretPosition.Clone() : new CaretPos { line = -1 };
		tempUndoRecord.postSelectionPos = editor != null && editor.selectionStartPosition != null ? editor.selectionStartPosition.Clone() : tempUndoRecord.postCaretPos.Clone();

		bool addNewRecord = true;

		if (undoPosition < undoBuffer.Count)
		{
			undoBuffer.RemoveRange(undoPosition, undoBuffer.Count - undoPosition);
			if (savedAtUndoPosition > undoPosition)
				savedAtUndoPosition = -1;
		}
		else
		{
			// Check is it fine to combine with previous record
			if (editor != null && undoPosition > 0 && tempUndoRecord.changes.Count == 1)
			{
				UndoRecord last = undoBuffer[undoPosition - 1];
				if (IsModified && last.changes.Count == 1 && last.postCaretPos == tempUndoRecord.preCaretPos &&
					last.postSelectionPos == tempUndoRecord.preSelectionPos &&
					last.actionType == tempUndoRecord.actionType && !last.actionType.FastStartsWith("*"))
				{
					UndoRecord.TextChange currChange = tempUndoRecord.changes[0];
					UndoRecord.TextChange prevChange = last.changes[0];
					if (currChange.oldText == string.Empty && currChange.newText.Length == 1 && prevChange.newText != string.Empty)
					{
						int currCharClass = GetCharClass(currChange.newText[0]);
						int prevCharClass = GetCharClass(prevChange.newText[prevChange.newText.Length - 1]);
						if (currCharClass == prevCharClass)
						{
							addNewRecord = false;
							prevChange.newText += currChange.newText;
							last.changes[0] = prevChange;
							last.postCaretPos = tempUndoRecord.postCaretPos.Clone();
							last.postSelectionPos = tempUndoRecord.postSelectionPos.Clone();
							//undoBuffer[undoPosition - 1] = prevRecord;
						}
					}
				}
			}
		}

		if (addNewRecord)
		{
			undoBuffer.Add(tempUndoRecord);
			++undoPosition;
			++currentChangeId;
			
			tempUndoRecord = new UndoRecord();
		}
		else
		{
			tempUndoRecord.changes.Clear();
			tempUndoRecord.postCaretPos = null;
			tempUndoRecord.postSelectionPos = null;
		}

		foreach (FormattedLine formattedLine in updatedLines)
			formattedLine.lastChange = currentChangeId;
	}

	public CaretPos DeleteText(CaretPos fromPos, CaretPos toPos)
	{
		CaretPos from = fromPos.Clone();
		CaretPos to = toPos.Clone();

		int fromTo = from.CompareTo(to);
		if (fromTo == 0)
			return from.Clone();

		RegisterUndoText("Delete Text", from, to, string.Empty);

		if (fromTo > 0)
		{
			CaretPos temp = from;
			from = to;
			to = temp;
		}

		if (from.line == to.line)
		{
			try
			{
				lines[from.line] = lines[from.line].Remove(from.characterIndex, to.characterIndex - from.characterIndex);
			}
			catch
			{
				Debug.Log(lines[from.line] + " (L: " + lines[from.line].Length + ")");
				Debug.Log("from.chararacterIndex: " + from.characterIndex + " to.characterIndex: " + to.characterIndex);
			}
		}
		else
		{
			lines[from.line] = lines[from.line].Substring(0, from.characterIndex) + lines[to.line].Substring(to.characterIndex);
			lines.RemoveRange(from.line + 1, to.line - from.line);
			for (int i = 1; to.line + i < formattedLines.Count; ++i)
			{
				formattedLines[from.line + i] = formattedLines[to.line + i];
				formattedLines[from.line + i].index = from.line + i;
			}
			formattedLines.RemoveRange(formattedLines.Count - to.line + from.line, to.line - from.line);
			numParsedLines -= to.line - from.line;

			NotifyRemovedLines(from.line + 1, to.line - from.line);
		}
		
		var fromTextPos = new TextPosition(from.line, from.characterIndex);
		var toTextPos = new TextPosition(to.line, to.characterIndex);
		NotifyRemovedText(fromTextPos, toTextPos);

		return from;
	}
	
	public bool CanEdit()
	{
		if (IsModified)
			return true;
		
		if (!File.Exists(assetPath) || (File.GetAttributes(assetPath) & FileAttributes.ReadOnly) == 0)
			return true;
		
		return false;
	}
	
	private static System.Type P4Connect_Engine;
	private static System.Reflection.MethodInfo method_CheckoutAssets;
	private static System.Type P4Connect_Queries;
	private static System.Reflection.MethodInfo method_GetFileState;
	
	private void TryP4Checkout()
	{
		if (P4Connect_Engine == null)
		{
			P4Connect_Engine = System.Type.GetType("P4Connect.Engine,P4Connect");
			
			if (P4Connect_Engine != null)
			{
				method_CheckoutAssets = P4Connect_Engine.GetMethod("CheckoutAssets");
				
				P4Connect_Queries = System.Type.GetType("P4Connect.Queries,P4Connect");
				if (P4Connect_Queries != null)
					method_GetFileState = P4Connect_Queries.GetMethod("GetFileState");
			}
		}
		
		if (method_CheckoutAssets == null || method_GetFileState == null)
			return;
		
		//var assetPathsArray = new object[]{ assetPath };
		//var returnedObject = method_GetFileState.Invoke(null, assetPathsArray);
		//var checkout = returnedObject.ToString() == "InDepot";
		//if (checkout)
			method_CheckoutAssets.Invoke(null, new object[]{ new[]{assetPath} });
		EditorApplication.RepaintProjectWindow();
	}
	
	public bool TryEdit()
	{
		if (IsModified || !File.Exists(assetPath) || (File.GetAttributes(assetPath) & FileAttributes.ReadOnly) == 0)
			return true;
		
#if !UNITY_4_0
		var versionControlAsset = UnityEditor.VersionControl.Provider.GetAssetByGUID(guid);
		if (versionControlAsset == null)
			return true;
		
		if ((versionControlAsset.state & UnityEditor.VersionControl.Asset.States.ReadOnly) == 0 ||
			(versionControlAsset.state & UnityEditor.VersionControl.Asset.States.AddedLocal) != 0)
		{
			if ((versionControlAsset.state & UnityEditor.VersionControl.Asset.States.AddedLocal) == 0)
#endif
				if (File.Exists(assetPath) && (File.GetAttributes(assetPath) & FileAttributes.ReadOnly) != 0)
					TryP4Checkout();
			
			if (File.Exists(assetPath) && (File.GetAttributes(assetPath) & FileAttributes.ReadOnly) != 0)
				return EditReadOnly();
		
			return true;
#if !UNITY_4_0
		}
		
		var result = false;
		
		var checkoutTask = UnityEditor.VersionControl.Provider.Checkout(versionControlAsset, UnityEditor.VersionControl.CheckoutMode.Both);
		try
		{
			checkoutTask.Wait();
			foreach (var message in checkoutTask.messages)
				if (message.severity == UnityEditor.VersionControl.Message.Severity.Warning || message.severity == UnityEditor.VersionControl.Message.Severity.Error)
					message.Show();
			result = checkoutTask.success;
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
		finally
		{
			if (checkoutTask != null)
				checkoutTask.Dispose();
		}
		
		return result || EditReadOnly();
#endif
	}
	
	private bool EditReadOnly()
	{
		var fileName = Path.GetFileName(assetPath);
		var focus = EditorWindow.focusedWindow;
		var answer = EditorUtility.DisplayDialog(
			"Script Inspector",
			"The file '" + fileName + "' is read-only! You may not be able to save the changes.\n\n" +
			"Would you still like to edit it?          \n\n",
			"Yes, Edit in Memory",
			"No, Don't Edit");
		if (focus)
			focus.Focus();
		return answer;
	}

	public CaretPos InsertText(CaretPos position, string text)
	{
		TextPosition pos = new TextPosition(position.line, position.characterIndex);
		CaretPos end = new CaretPos { characterIndex = position.characterIndex, column = position.column, virtualColumn = position.column, line = position.line };

		string[] insertLines = text.Split(new char[] { '\n' }, System.StringSplitOptions.None);
		
		if (recordUndo && SISettings.insertSpacesOnTab)
		{
			var tabSize = SISettings.tabSize;
			var column = position.column;
			for (var i = 0; i < insertLines.Length; i++)
			{
				text = insertLines[i];
				int tabIndex;
				while ((tabIndex = text.IndexOf('\t')) >= 0)
				{
					var c = column + tabIndex;
					c = tabSize - c % tabSize;
					text = string.Concat(text.Substring(0, tabIndex), new string(' ', c), text.Substring(tabIndex + 1));
				}
				insertLines[i] = text;
				column = 0;
			}
			if (insertLines.Length > 1)
				text = string.Join("\n", insertLines);
		}
		
		RegisterUndoText("Insert Text", position, position, text);
		
		if (insertLines.Length == 1)
		{
			lines[pos.line] = lines[pos.line].Insert(pos.index, text);

			end.characterIndex += text.Length;
			end.column = end.virtualColumn = CharIndexToColumn(end.characterIndex, end.line);
		}
		else
		{
			var insert = insertLines[insertLines.Length - 1] + lines[pos.line].Substring(pos.index);
			lines.Insert(pos.line + 1, insert);
			
			lines[pos.line] = lines[pos.line].Substring(0, pos.index) + insertLines[0];
			for (int i = 1; i < insertLines.Length - 1; ++i)
			{
				insert = insertLines[i];
				lines.Insert(pos.line + i, insert);
			}

			end.characterIndex = insertLines[insertLines.Length - 1].Length;
			end.line = pos.line + insertLines.Length - 1;
			end.column = end.virtualColumn = CharIndexToColumn(end.characterIndex, end.line);

			for (int i = insertLines.Length - 1; i --> 0; )
				formattedLines.Add(null);
			for (int i = formattedLines.Count - 1; i > end.line; --i)
			{
				formattedLines[i] = formattedLines[i - insertLines.Length + 1];
				formattedLines[i].index = i;
			}
			for (int i = 1; i <= insertLines.Length - 1; ++i)
				formattedLines[pos.line + i] = new FormattedLine { index = pos.line + i };
			numParsedLines = formattedLines.Count;

			NotifyInsertedLines(pos.line + 1, insertLines.Length - 1);
		}
		
		var endPos = new TextPosition(end.line, end.characterIndex);
		NotifyInsertedText(pos, endPos);

		return end;
	}

	public delegate void InsertedLinesDelegate(int lineIndex, int numLines);
	public InsertedLinesDelegate onInsertedLines;
	
	public delegate void InsertedLinesAllDelegate(string assetGuid, int lineIndex, int numLines);
	public static InsertedLinesAllDelegate onInsertedLinesAll;
	
	private void NotifyInsertedLines(int lineIndex, int numLines)
	{
		if (onInsertedLines != null)
			onInsertedLines(lineIndex, numLines);
		if (onInsertedLinesAll != null)
			onInsertedLinesAll(guid, lineIndex, numLines);
	}

	public delegate void InsertedTextDelegate(TextPosition from, TextPosition to);
	public InsertedTextDelegate onInsertedText;

	public delegate void InsertedTextAllDelegate(string assetGuid, TextPosition from, TextPosition to);
	public static InsertedTextAllDelegate onInsertedTextAll;

	private void NotifyInsertedText(TextPosition from, TextPosition to)
	{
		FGTextBufferManager.OnInsertedText(this, from, to);
		
		var modified = false;
		
		for (var i = collapsibleTextSpans.Count; i --> 0; )
		{
			var entry = collapsibleTextSpans[i];

			modified = entry.span.End.OnInsertedText(from, to, false) || modified;
			modified = entry.span.Start.OnInsertedText(from, to, true) || modified;
		}
		
		if (modified && collapsibleTextSpansTree != null)
			collapsibleTextSpansTree.OnInsertedText(from, to);
		
		if (onInsertedText != null)
			onInsertedText(from, to);
		if (onInsertedTextAll != null)
			onInsertedTextAll(guid, from, to);
	}

	public delegate void RemovedLinesDelegate(int lineIndex, int numLines);
	public RemovedLinesDelegate onRemovedLines;

	public delegate void RemovedLinesAllDelegate(string assetGuid, int lineIndex, int numLines);
	public static RemovedLinesAllDelegate onRemovedLinesAll;

	private void NotifyRemovedLines(int lineIndex, int numLines)
	{
		if (onRemovedLines != null)
			onRemovedLines(lineIndex, numLines);
		if (onRemovedLinesAll != null)
			onRemovedLinesAll(guid, lineIndex, numLines);
	}

	public delegate void RemovedTextDelegate(TextPosition from, TextPosition to);
	public RemovedTextDelegate onRemovedText;
	
	public delegate void RemovedTextAllDelegate(string assetGuid, TextPosition from, TextPosition to);
	public static RemovedTextAllDelegate onRemovedTextAll;
	
	private void NotifyRemovedText(TextPosition from, TextPosition to)
	{
		FGTextBufferManager.OnRemovedText(this, from, to);
		
		if (collapsibleTextSpansTree != null)
		{
			for (var i = collapsibleTextSpans.Count; i --> 0; )
			{
				var entry = collapsibleTextSpans[i];
				var prevSpan = entry.span;
			
				var updated = entry.span.End.OnRemovedText(from, to);
				if (entry.span.Start.OnRemovedText(from, to) || updated)
				{
					collapsibleTextSpansTree.Delete(prevSpan);
					if (entry.span.Start == entry.span.End)
					{
						collapsibleTextSpans.RemoveAt(i);
					}
					else
					{
						if (!collapsibleTextSpansTree.Add(entry.span, entry))
						{
							Debug.LogError("CollapsibleTextSpan not added: " + entry.span);
							collapsibleTextSpans.RemoveAt(i);
						}
					}
				}
			}
		}
		
		if (onRemovedText != null)
			onRemovedText(from, to);
		if (onRemovedTextAll != null)
			onRemovedTextAll(guid, from, to);
	}
	
	public CollapsibleTextSpan AddCollapsibleTextSpan(CollapsibleTextSpan span)
	{
		//Debug.Log("Adding span: " + span.span.Start + " - " + span.span.End);
		var index = collapsibleTextSpans.BinarySearch(span);
		if (index >= 0)
			return collapsibleTextSpans[index];
		
		index = ~index;
		if (collapsibleTextSpansTree.Add(span.span, span))
		{
			collapsibleTextSpans.Insert(index, span);
			return span;
		}
		
		#if SI3_WARNINGS
		Debug.LogError("CollapsibleTextSpan not added: " + span.span);
		#endif
		return null;
	}
	
	public void RemoveCollapsibleTextSpan(CollapsibleTextSpan span)
	{
		var index = collapsibleTextSpans.BinarySearch(span);
		if (index < 0)
		{
			//Debug.Log("Removing span: " + span.span.Start + " - " + span.span.End);
			//Debug.LogWarning("Not found span!");
			return;
		}
		
		collapsibleTextSpans.RemoveAt(index);
		if (!collapsibleTextSpansTree.Delete(span.span))
		{
			#if SI3_WARNINGS
			Debug.LogError("Span not removed from collapsibleTextSpansTree: " + span.span.ToString() + " for " + span.owner);
			#endif
		}
		
		#if SI3_WARNINGS
		if (collapsibleTextSpans.Count != collapsibleTextSpansTree.Count)
			Debug.LogError("collapsibleTextSpans.Count != collapsibleTextSpansTree.Count: " + collapsibleTextSpans.Count + "/" + collapsibleTextSpansTree.Count);
		#endif
	}
	
	public void RemoveAllCollapsibleTextSpans()
	{
		collapsibleTextSpans.Clear();
		if (collapsibleTextSpansTree != null)
			collapsibleTextSpansTree.Clear();
	}
	
	private CollapsibleTextSpan tempCollapsibleTextSpan = new CollapsibleTextSpan();
	public int IndexOfFirstCollapsibleTextSpanAtLine(int line)
	{
		tempCollapsibleTextSpan.span.Start.Set(line, -1);
		tempCollapsibleTextSpan.span.End.Set(line, 0);
		var index = collapsibleTextSpans.BinarySearch(tempCollapsibleTextSpan);
		if (index < 0)
			index = ~index;
		if (index >= collapsibleTextSpans.Count)
			return -1;
		var span = collapsibleTextSpans[index];
		if (span.span.Start.line > line)
			return -1;
		return index;
	}
	
	public int IndexOfFirstCollapsibleTextSpanAtLineOrAfter(int line)
	{
		tempCollapsibleTextSpan.span.Start.Set(line, -1);
		tempCollapsibleTextSpan.span.End.Set(line, 0);
		var index = collapsibleTextSpans.BinarySearch(tempCollapsibleTextSpan);
		if (index < 0)
			index = ~index;
		if (index >= collapsibleTextSpans.Count)
			return -1;
		return index;
	}
	
	public int FirstNonWhitespace(int atLine)
	{
		int index = 0;
		string line = lines[atLine];
		while (index < line.Length)
		{
			char c = line[index];
			if (c != ' ' && c != '\t')
				break;
			++index;
		}
		return index;
	}
	
	public TextPosition FirstNonWhitespacePos(int fromLine, int fromCharIndex)
	{
		int index = fromCharIndex;
		for (var i = fromLine; i < lines.Count; ++i)
		{
			string line = lines[i];
			while (index < line.Length)
			{
				char c = line[index];
				if (c != ' ' && c != '\t')
				{
					return new TextPosition(i, index);
				}
				++index;
			}
			index = 0;
		}
		return new TextPosition(lines.Count, 0);
	}
	
	private void Parse(int parseToLine)
	{
		// Is there still anything left for reading/parsing?
		if (streamReader == null)
			return;

		// Reading lines till parseToLine-th line
		for (int i = numParsedLines; i < parseToLine; ++i)
		{
			string line = "";
			if (i == 0)
			{
				var sb = new StringBuilder();
				while (!streamReader.EndOfStream)
				{
					char[] buffer = new char[1];
					streamReader.ReadBlock(buffer, 0, 1);
					if (buffer[0] == '\r' || buffer[0] == '\n')
					{
						lineEnding = buffer[0].ToString();
						if (!streamReader.EndOfStream)
						{
							string next = char.ConvertFromUtf32(streamReader.Peek());
							if (next != lineEnding && (next == "\r" || next == "\n"))
							{
								lineEnding += next;
								streamReader.ReadBlock(buffer, 0, 1);
							}
						}
						break;
					}
					else
					{
						sb.Append(buffer[0]);
					}
				}
				line = sb.ToString();

				if (streamReader != null)
				{
					codePage = streamReader.CurrentEncoding.CodePage;
				}
			}
			else
			{
				line = streamReader.ReadLine();
			}

			if (line == null)
			{
				if (streamReader.BaseStream.Position > 0)
				{
					streamReader.BaseStream.Position -= 1;
					int last = streamReader.BaseStream.ReadByte();
					if (last == 0 && streamReader.BaseStream.Position > 1)
					{
						streamReader.BaseStream.Position -= 2;
						last = streamReader.BaseStream.ReadByte();
					}
					if (last == 10 || last == 13)
					{
						lines.Add("");
					}
				}

				streamReader.Close();
				streamReader.Dispose();
				streamReader = null;
				needsReload = false;
				break;
			}

			lines.Add(line);
		}
		if (formattedLines.Count == parseToLine)
			return;

		parseToLine = System.Math.Min(parseToLine, lines.Count);
		for (int i = formattedLines.Count; i < parseToLine; ++i)
			formattedLines.Add(null);

		for (int currentLine = numParsedLines; currentLine < parseToLine; ++currentLine)
		{
			FormatLine(currentLine);
		}

		numParsedLines = parseToLine;
	}

	System.Func<bool> progressiveParser;

	void ProgressiveParseOnUpdate()
	{
		if (progressiveParser == null || !progressiveParser())
		{
			EditorApplication.update -= ProgressiveParseOnUpdate;
			progressiveParser = null;
		}
	}
	
	public SyntaxToken FirstNonTriviaToken(int line)
	{
		var formattedLine = formattedLines[line];
		if (formattedLine == null)
			return null;
		
		var tokensInLine = formattedLine.tokens;
		if (tokensInLine == null || tokensInLine.Count == 0)
			return null;

		SyntaxToken firstToken = null;
		for (var i = 0; i < tokensInLine.Count; ++i)
		{
			if (tokensInLine[i].tokenKind > SyntaxToken.Kind.LastWSToken)
			{
				firstToken = tokensInLine[i];
				break;
			}
		}
		return firstToken;
	}
	
	public void GetFirstTokens(int line, out SyntaxToken firstToken, out SyntaxToken firstNonTrivia)
	{
		firstToken = null;
		firstNonTrivia = null;
		
		var tokens = formattedLines[line].tokens;
		for (var i = 0; i < tokens.Count; ++i)
		{
			var t = tokens[i];
			if (t.tokenKind > SyntaxToken.Kind.Whitespace)
			{
				firstToken = t;
				do {
					if (tokens[i].tokenKind > SyntaxToken.Kind.LastWSToken &&
						tokens[i].parent != null && tokens[i].parent.parent != null)
					{
						firstNonTrivia = tokens[i];
						break;
					}
					++i;
				} while (i < tokens.Count);
				break;
			}
		}
	}
	
	public SyntaxToken FirstNonWhitespaceToken(int line)
	{
		var tokens = formattedLines[line].tokens;
		if (tokens == null || tokens.Count == 0)
			return null;
		
		for (int i = 0; i < tokens.Count; ++i)
			if (tokens[i].tokenKind != SyntaxToken.Kind.Whitespace)
				return tokens[i];
		
		return null;
	}
	
	public TextPosition GetOpeningBraceLeftOf(int tokenLine, int tokenIndex, int maxLinesDistance)
	{
		var firstLine = maxLinesDistance >= 0 ? Mathf.Max(0, tokenLine - maxLinesDistance) : 0;
		var bracePosition = TextPosition.invalid;
		var tokens = formattedLines[tokenLine].tokens;
		
		var skipOver = 0;
		while (tokenIndex < 0)
		{
			if (--tokenLine < firstLine)
				break;
			tokens = formattedLines[tokenLine].tokens;
			tokenIndex = tokens.Count - 1;
		}
		while (tokenIndex >= 0)
		{
			var tokenLeft = tokens[tokenIndex];
			var text = tokenLeft.text;
			if (tokenLeft.tokenKind == SyntaxToken.Kind.Punctuator && text.length == 1)
			{
				var c = text[0];
				if ('(' == c || '[' == c || '{' == c)
				{
					if (skipOver > 0)
					{
						--skipOver;
					}
					else
					{
						bracePosition = new TextPosition(tokenLine, tokenIndex);
						break;
					}
				}
				else if (')' == c || ']' == c || '}' == c)
				{
					++skipOver;
				}
			}
			while (--tokenIndex < 0)
			{
				if (--tokenLine < firstLine)
					break;
				tokens = formattedLines[tokenLine].tokens;
				tokenIndex = tokens.Count;
			}
		}
		
		return bracePosition;
	}

	public TextPosition GetClosingBraceRightOf(int tokenLine, int tokenIndex, int maxLinesDistance)
	{
		var lastLine = maxLinesDistance >= 0 ? Mathf.Min(formattedLines.Count - 1, tokenLine + maxLinesDistance) : formattedLines.Count - 1;
		var tokens = formattedLines[tokenLine].tokens;
		if (tokens == null)
			return new TextPosition();

		var skipOver = 0;
		var numTokens = tokens.Count;
		while (tokenLine <= lastLine)
		{
			while (++tokenIndex >= numTokens)
			{
				if (++tokenLine > lastLine)
					break;
				tokens = formattedLines[tokenLine].tokens;
				if (tokens == null)
					return new TextPosition();
				tokenIndex = -1;
				numTokens = tokens.Count;
			}
			if (tokenIndex >= numTokens)
				break;
			var tokenRight = tokens[tokenIndex];
			if (tokenRight.tokenKind == SyntaxToken.Kind.Punctuator)
			{
				var text = tokenRight.text;
				if (text.length == 1)
				{
					var c = text[0];
					if (')' == c || ']' == c || '}' == c)
					{
						if (skipOver > 0)
							--skipOver;
						else
							return new TextPosition(tokenLine, tokenIndex);
					}
					else if ('(' == c || '[' == c || '{' == c)
					{
						++skipOver;
					}
				}
			}
		}

		return new TextPosition();
	}
	
	public string GetLineIndent(int line)
	{
		//List<SyntaxToken> tokens;
		//SyntaxToken firstNonWSToken = null;
		//int tokenIndex = 0;
		//while (line >= 0)
		//{
		//	firstNonWSToken = FirstNonWhitespaceToken(line);
		//	if (firstNonWSToken == null || firstNonWSToken.tokenKind == SyntaxToken.Kind.Preprocessor)
		//	{
		//		--line;
		//	}
		//	else
		//	{
		//		tokens = formattedLines[line].tokens;
		//		tokenIndex = firstNonWSToken.TokenIndex;
		//		while (firstNonWSToken.tokenKind == SyntaxToken.Kind.VerbatimStringLiteral ||
		//			firstNonWSToken.tokenKind == SyntaxToken.Kind.Comment ||
		//			firstNonWSToken.tokenKind == SyntaxToken.Kind.Missing ||
		//			firstNonWSToken.tokenKind == SyntaxToken.Kind.Whitespace)
		//		{
		//			++tokenIndex;
		//			if (tokenIndex == tokens.Count)
		//			{
		//				--line;
		//				continue;
		//			}
		//			firstNonWSToken = tokens[tokenIndex];
		//		}
		//		break;
		//	}
		//}
		if (line < 0)
			return "";
		
		var lineToken = line;
		var indexToken = formattedLines[line].tokens.Count;
		var token = GetNonTriviaTokenLeftOf(ref lineToken, ref indexToken);
		if (token == null)
			return "";
		
		var tokens = formattedLines[lineToken].tokens;
		
		//var lineTokenLeft = lineToken;
		//var indexTokenLeft = indexToken;
		//var tokenLeft = GetNonTriviaTokenLeftOf(ref lineTokenLeft, ref indexTokenLeft);
		
		return tokens[0].tokenKind == SyntaxToken.Kind.Whitespace ? tokens[0].text.GetString() : "";
	}
	
	public string CalcAutoIndent(int line)
	{
		if (isText)
		{
			if (!SISettings.autoIndentText)
				return null;
		}
		else
		{
			if (!SISettings.autoIndentCode)
				return null;
		}
		
		SyntaxToken firstToken, firstNonTrivia;
		GetFirstTokens(line, out firstToken, out firstNonTrivia);
		
		if (firstNonTrivia != null && firstNonTrivia.parent.syntaxError != null)
			return null;
		
		var leaf = firstNonTrivia != null ? firstNonTrivia.parent : null;
		if (leaf == null && (firstToken == null || firstToken.text.FastStartsWith("#")))
			return null;
		
		string indent = null;
		var delta = 0;
		ParseTree.Leaf reference = null;
		var parent = leaf != null ? leaf.parent : null;
		
		var childIndex = leaf != null ? leaf.childIndex : (short) 0;
		if (leaf == null)
		{
			var previousLeaf = GetNonTriviaTokenLeftOf(line, 0);
			if (previousLeaf == null)
				return "";
			if (previousLeaf.parent == null)
				return null;
			
			ParseTree.BaseNode previousNode = previousLeaf.parent;
			if (previousNode == null)
				return null;

			var scanner = parser.MoveAfterLeaf(previousLeaf.parent);
			if (scanner == null)
				return null;
			var grammarNode = scanner.CurrentGrammarNode;
			grammarNode.parent.NextAfterChild(grammarNode, scanner);
			
			parent = scanner.CurrentParseTreeNode;
			scanner.Delete();
			
			while (previousNode.parent != null && previousNode.parent != parent)
				previousNode = previousNode.parent;
			if (previousNode.parent != parent)
				return null;
		//	Debug.Log(previousLeaf.parent.ToString() + previousNode.childIndex);
			childIndex = (short) (previousNode.childIndex + (short) 1);
		}
		
		if (leaf != null && (leaf.IsLit("{") || leaf.IsLit("[") || leaf.IsLit("(")))
		{
			do {
				while (parent != null && parent.childIndex > 0)
				{
					var checkBracket = true;
					var checkParen = true;
					for (var i = parent.childIndex; i --> 0; )
					{
						var leafLeft = parent.parent.LeafAt(i);
						if (leafLeft == null)
							continue;
						if (leafLeft.IsLit("{") ||
							checkBracket && leafLeft.IsLit("[") ||
							checkParen && leafLeft.IsLit("("))
						{
							reference = leafLeft;
							delta = 1;
						//	Debug.Log(parent.RuleName + " " + leafLeft.token.text + " found");
							break;
						}
						else if (leafLeft.IsLit(","))
						{
							parent = leafLeft.FindPreviousNode() as ParseTree.Node;
							reference = parent != null ? parent.GetFirstLeaf() : leafLeft;
							parent = null;
							delta = 0;
							break;
						}
						else if (leafLeft.IsLit(")"))
							checkParen = false;
						else if (leafLeft.IsLit("]"))
							checkBracket = false;
					}
					if (reference != null)
						break;
					parent = parent.parent;
				}
				if (parent != null)
					parent = parent.parent;
			} while (parent != null && parent.GetFirstLeaf() == leaf);
			if (parent != null)
			{
				reference = parent.GetFirstLeaf();
				if (reference != null && reference.IsLit("{"))
					delta = 1;
			}
		}
		else if (leaf != null && (leaf.IsLit("}") || leaf.IsLit("]") || leaf.IsLit(")")))
		{
			string openParen = leaf.token.text;
			openParen = openParen == "}" ? "{" : openParen == "]" ? "[" : "(";
			for (var i = leaf.childIndex; i --> 0; )
			{
				reference = parent.LeafAt(i);
				if (reference != null)
				{
					if (reference.IsLit(openParen))
						break;
					else
						reference = null;
				}
			}
		}
		else if (parent != null)
		{
			while (parent != null)
			{
				var thisIndex = childIndex;
				childIndex = parent.childIndex;
				var rule = parent.RuleName;
				if (rule == null)
					break;
				
				if (rule == "embeddedStatement")
				{
					if (parent.parent.RuleName != "statement")
					{
						reference = parent.parent.GetFirstLeaf();
						delta = 1;
						break;
					}
				}
				else if (rule == "statement")
				{
					reference = parent.GetFirstLeaf();
					if (reference != leaf)
					{
						delta = 1;
						break;
					}
					else
					{
						reference = null;
					}
				}
				else if (rule == "elseStatement")
				{
					parent = parent.parent;
					reference = parent.GetFirstLeaf();
					break;
				}
				else if (rule == "switchLabel")
				{
					parent = parent.parent;
					if (childIndex == 0)
					{
						if (parent.childIndex >= 2)
							parent = parent.parent.NodeAt(1);
						else
							parent = parent.parent;
					}
					reference = parent.GetFirstLeaf();
					break;
				}
				else if (rule == "switchSection")// && childIndex > 1)
				{
					if (thisIndex > 0)
					{
						reference = parent.GetFirstLeaf();
						delta = 1;
					}
					else
					{
						reference = parent.parent.GetFirstLeaf();
					}
					break;
				}
				else if (rule == "labeledStatement" && childIndex < 2)
				{
					parent = parent.parent.parent.parent;
					reference = parent.GetFirstLeaf();
				//	Debug.Log(reference.ToString() + parent.RuleName + " " + childIndex);
					break;
				}
				else if (rule == "fieldDeclaration" || rule == "eventDeclaration")
				{
					parent = parent.parent;
					reference = parent.GetFirstLeaf();
					delta = 1;
					break;
				}
				else if (rule == "constantDeclaration")
				{
					parent = parent.parent.parent;
					reference = parent.GetFirstLeaf();
					delta = 1;
					break;
				}
				else if (rule == "formalParameterList")
				{
					parent = parent.parent;
					reference = parent.GetFirstLeaf();
					delta = 1;
					break;
				}
				
				for (var i = childIndex; i --> 0; )
				{
					var leafLeft = parent.LeafAt(i);
					if (leafLeft != null && leafLeft.IsLit("{"))
					{
						reference = leafLeft;
						delta = 1;
					//	Debug.Log(parent.RuleName + " { found");
						break;
					}
				}
				if (reference != null)
					break;
				
				parent = parent.parent;
			}
		}
		
		if (reference != null)
		{
			//Debug.Log(reference.ToString() + parent.RuleName);

			indent = lines[reference.line].Substring(0, FirstNonWhitespace(reference.line));
			if (delta > 0)
			{
				indent = new string('\t', delta) + indent;
			}
			else
			{
				var skip = 0;
				while (delta < 0 && skip < indent.Length)
				{
					for (var column = 0; column < 4; ++column)
						if (indent[skip++] == '\t')
							break;
					++delta;
				}
				indent = indent.Substring(skip);
			}
		}
		
		if (string.IsNullOrEmpty(indent))
			return indent;
		
		if (SISettings.insertSpacesOnTab)
		{
			var tabSize = SISettings.tabSize;
			var column = 0;
			int tabIndex;
			while ((tabIndex = indent.IndexOf('\t')) >= 0)
			{
				var c = column + tabIndex;
				c = tabSize - c % tabSize;
				indent = string.Concat(indent.Substring(0, tabIndex), new string(' ', c), indent.Substring(tabIndex + 1));
			}
		}

		return indent;
	}

	static List<FGTextBuffer.CollapsibleTextSpan> collapsibleTextSpansToUpdate = new List<FGTextBuffer.CollapsibleTextSpan>();

	public void UpdateHighlighting(int fromLine, int toLineInclusive, bool keepLastChangeId = false)
	{
		if (progressiveParser != null)
		{
			EditorApplication.update -= ProgressiveParseOnUpdate;
			progressiveParser = null;
		}

		//Debug.Log("Updating HL from line " + (fromLine + 1) + " to " + (toLineInclusive + 1));
		if (parser != null)
			parser.CutParseTree(toLineInclusive + 1, formattedLines);

		for (var i = fromLine; i <= toLineInclusive; ++i)
		{
			var line = formattedLines[i];
			if (line == null || line.tokens == null)
				continue;
			var tokens = line.tokens;
			for (var t = 0; t < tokens.Count; t++)
			{
				var token = tokens[t];
				if (token.parent != null)
					token.parent.ReparseToken();
			}
		}
		
		if (parser != null)
			parser.scriptDefinesChanged = false;
		var laLine = UpdateLexer(fromLine, toLineInclusive, keepLastChangeId);

		if (parser != null)
		{
			parser.CutParseTree(laLine, formattedLines);
			if (parser.scriptDefinesChanged)
			{
				if (fromLine != 0)
				{
					parser.scriptDefinesChanged = false;
					var savedRecordUndo = recordUndo;
					recordUndo = false;
					UpdateHighlighting(0, formattedLines.Count - 1, true);
					recordUndo = savedRecordUndo;
					return;
				}
			}
		}

		if (collapsibleTextSpansTree != null)
		{
			collapsibleTextSpansToUpdate.Clear();
			
			//Debug.Log("Updating lines: " + laLine + " - " + toLineInclusive);
			
			var textInterval = new TextInterval(new TextPosition(laLine, 0), new TextPosition(toLineInclusive + 1, 0));
			collapsibleTextSpansTree.GetIntervalsOverlappingWith(textInterval, collapsibleTextSpansToUpdate);

			for (var i = collapsibleTextSpansToUpdate.Count; i --> 0; )
			{
				var span = collapsibleTextSpansToUpdate[i];
				
				//Debug.Log("Updating span: " + span.Value.span.Start + " - " + span.Value.span.End);
				
				var ownerNode = span.owner as ParseTree.BaseNode;
				if (ownerNode != null)
				{
					span.owner = null;
				}
				else
				{
					collapsibleTextSpansToUpdate.RemoveAt(i);
				}
			}
		}

		//Debug.Log("Updated HL from " + (fromLine + 1) + " (" + (laLine + 1) + ") to " + (toLineInclusive + 1));
		//if (laLine <= toLineInclusive)
		{
			for (var i = laLine; i < fromLine; ++i)
			{
				for (var t = 0; t < formattedLines[i].tokens.Count; t++)
				{
					var token = formattedLines[i].tokens[t];
					if (token.parent != null)
						token.parent.ReparseToken();
				}
			}
			//parser.CutParseTree(laLine, formattedLines);
		}
		//var timer = new System.Diagnostics.Stopwatch();
		//timer.Start();
		
		if (parser != null)
		{
			var updater = parser.Update(laLine, toLineInclusive);
			if (updater != null)
			{
				progressiveParser = updater;
				EditorApplication.update += ProgressiveParseOnUpdate;
			}
		}
		
		if (collapsibleTextSpansTree != null)
		{
			for (var i = collapsibleTextSpansToUpdate.Count; i --> 0; )
			{
				var span = collapsibleTextSpansToUpdate[i];
				
				var ownerNode = span.owner as ParseTree.BaseNode;
				if (ownerNode == null)
				{
					RemoveCollapsibleTextSpan(span);
					
					if (span.IsCollapsed)
						NotifyAllEditorsOnUnfold(span);
				}
			}

			collapsibleTextSpansToUpdate.Clear();
			var textInterval = new TextInterval(new TextPosition(laLine, 0), new TextPosition(toLineInclusive + 1, 0));
			collapsibleTextSpansTree.GetIntervalsOverlappingWith(textInterval, collapsibleTextSpansToUpdate);

			for (var i = collapsibleTextSpansToUpdate.Count; i --> 0; )
			{
				var span = collapsibleTextSpansToUpdate[i];
				
				var ownerRegionTree = span.owner as RegionTree;
				if (ownerRegionTree != null && ownerRegionTree != rootRegion)
				{
					bool delete = false;
					if (ownerRegionTree != ownerRegionTree.line.regionTree)
					{
						delete = true;
					}
					else
					{
						var toLine = span.span.End.line;
						if (toLine >= formattedLines.Count ||
							formattedLines[toLine].regionTree != ownerRegionTree.parent ||
							formattedLines[toLine-1].regionTree != ownerRegionTree)
						{
							delete = true;
						}
					}
					
					if (delete)
					{
						RemoveCollapsibleTextSpan(span);
						
						if (span.IsCollapsed)
							NotifyAllEditorsOnUnfold(span);
					}
				}
			}
		}

		//timer.Stop();
		//Debug.Log("Parsing - " + timer.Elapsed.TotalMilliseconds.ToString());

		UpdateViews();
	}

	private int UpdateLexer(int fromLine, int toLineInclusive, bool keepLastChangeId)
	{
		var laLine = fromLine;
		var line = fromLine;
		while (line <= toLineInclusive)
		{
			//laLine = System.Math.Min(laLine, line - formattedLines[line].laLines);
			laLine = Mathf.Clamp(line - formattedLines[line].laLines, 0, laLine);
			FormatLine(line);
			if (!keepLastChangeId)
				formattedLines[line].lastChange = currentChangeId;
			if (recordUndo)
				updatedLines.Add(formattedLines[line]);
			++line;
		}
		
		if (fromLine != 0 && parser != null && parser.scriptDefinesChanged)
			return -1;

		while (line < formattedLines.Count)
		{
			var formattedLine = formattedLines[line];
			laLine = Mathf.Clamp(line - formattedLine.laLines, 0, laLine);
			var prevState = formattedLine.blockState;
			var prevRegion = formattedLine.regionTree;
		
			FormatLine(line);
			
			if (fromLine != 0 && parser != null && parser.scriptDefinesChanged)
				return -1;
			
			formattedLine = formattedLines[line++];
			if ((parser == null || !parser.scriptDefinesChanged) && prevState == formattedLine.blockState && prevRegion == formattedLine.regionTree)
				break;
		}

		// TODO: Optimize this!!!
		
		//for (var i = line; i < formattedLines.Length; ++i)
		//{
		//	var formattedLine = formattedLines[i];
		//	if (formattedLine == null)
		//		continue;
				
		//	for (var j = 0; j < formattedLine.tokens.Count; ++j)
		//	{
		//		var token = formattedLine.tokens[j];
		//		if (token.parent != null)
		//		{
		//			//if (token.parent.line == i)
		//			//{
		//			//	i = formattedLines.Length;
		//			//	break;
		//			//}
					
		//			//token.parent.line = i;
		//			if (token.parent.tokenIndex != j)
		//			{
		//				//Debug.Log("Index of token " + token + " on line " + i
		//				//	+ " was " + token.parent.tokenIndex + " instead of " + j);
		//				token.parent.tokenIndex = j;
		//			}
		//		}
		//	}
		//}

	//	if (laLine < fromLine)
	//		Debug.LogWarning("laLine: " + laLine + ", fromLine: " + fromLine);
		return laLine;
	}

	private void ReformatLines(int fromLine, int toLineInclusive)
	{
		var line = fromLine;
		while (line <= toLineInclusive)
		{
			FormatLine(line);
			++line;
		}
	}

	public delegate void LineFormattedDelegate(int line);
	public LineFormattedDelegate onLineFormatted;

	[System.NonSerialized]
	private FGParser parser;

	public FGParser Parser { get { return parser; } }

	private void FormatLine(int currentLine)
	{
		FormattedLine formattedLine = formattedLines[currentLine];
		if (formattedLine == null)
		{
			formattedLine = formattedLines[currentLine] = new FormattedLine { index = currentLine };
			formattedLine.lastChange = currentChangeId;
		}
		else
		{
			var tokens = formattedLine.tokens;
			if (tokens != null)
			{
				for (var i = tokens.Count; i --> 0; )
				{
					var token = tokens[i];
					if (token.parent != null)
					{
						token.parent.token = null;
						token.parent = null;
					}
				}
			}
		}

		if (currentLine > 0)
		{
			var prevLine = formattedLines[currentLine - 1];
			formattedLine.blockState = prevLine.blockState;
			formattedLine.regionTree = prevLine.regionTree;
		}
		else
		{
			formattedLine.blockState = 0;
			formattedLine.regionTree = rootRegion;
		}

		if (parser != null)
			parser.LexLine(currentLine, formattedLine);
		if (onLineFormatted != null)
			onLineFormatted(currentLine);
	}

	public TextSpan GetTokenSpan(ParseTree.Leaf parseTreeLeaf)
	{
		//var tokens = formattedLines[parseTreeLeaf.line].tokens;
		//var tokenIndex = parseTreeLeaf.tokenIndex;
		//for (var j = 0; j <= tokenIndex; ++j)
		//    if (tokens[j].tokenKind < SyntaxToken.Kind.LastWSToken)
		//        ++tokenIndex;
		//Debug.Log(parseTreeLeaf.tokenIndex + " -> " + tokenIndex);
		return GetTokenSpan(parseTreeLeaf.line, parseTreeLeaf.tokenIndex);
	}

	public TextSpan GetTokenSpan(int lineIndex, int tokenIndex)
	{
		if (lineIndex >= formattedLines.Count)
			return new TextSpan();
		
		var tokens = formattedLines[lineIndex].tokens;

		var tokenStart = 0;
		for (var i = 0; i < tokenIndex; ++i)
			if (i < tokens.Count)
				tokenStart += tokens[i].text.length;
			else
			{
				//Debug.LogWarning("Token at line " + (lineIndex + 1) + ", index " + i + " is out of range!");
				return new TextSpan();
			}
		
		var tokenLength = tokens[tokenIndex].text.length;
		return TextSpan.Create(new TextPosition { line = lineIndex, index = tokenStart }, new TextOffset { indexOffset = tokenLength });
	}

	public TextSpan GetParseTreeNodeSpan(ParseTree.BaseNode parseTreeNode)
	{
		var leaf = parseTreeNode as ParseTree.Leaf;
		if (leaf != null)
			return GetTokenSpan(leaf);

		var node = (ParseTree.Node) parseTreeNode;
		ParseTree.Leaf from = node.GetFirstLeaf();
		ParseTree.Leaf to = node.GetLastLeaf();
		if (from == null || to == null)
			return new TextSpan();
		
		if (from == to)
			return GetTokenSpan(from);
		else
			return TextSpan.Create(GetTokenSpan(from).StartPosition, GetTokenSpan(to).EndPosition);
	}

	public SyntaxToken GetTokenLeftOf(ref int lineIndex, ref int tokenIndex)
	{
		while (lineIndex >= 0)
		{
			var tokens = formattedLines[lineIndex].tokens;
			if (tokenIndex == -1)
				tokenIndex = tokens.Count;
			if (--tokenIndex >= 0)
				return tokens[tokenIndex];
			--lineIndex;
		}
		return null;
	}

	public SyntaxToken GetTokenAt(CaretPos caretPosition, out int lineIndex, out int tokenIndex, out bool atTokenEnd)
	{
		return GetTokenAt(new TextPosition(caretPosition.line, caretPosition.characterIndex), out lineIndex, out tokenIndex, out atTokenEnd);
	}

	public SyntaxToken GetTokenAt(TextPosition position, out int lineIndex, out int tokenIndex, out bool atTokenEnd)
	{
		atTokenEnd = true;
		lineIndex = position.line;
		tokenIndex = 0;
		if (lineIndex < 0 || lineIndex >= formattedLines.Count)
			return null;

		var characterIndex = position.index;
		var tokens = formattedLines[lineIndex].tokens;
		if (tokens == null)
			return null;

		while (tokenIndex < tokens.Count && tokens[tokenIndex].IsMissing())
			++tokenIndex;

		if (tokenIndex == tokens.Count)
		{
			tokenIndex = -1;
			return null;
		}

		if (characterIndex == 0)
		{
			atTokenEnd = false;
			return tokens[0];
		}

		SyntaxToken result = null;
		while (characterIndex >= 0)
		{
			if (tokens[tokenIndex].IsMissing())
			{
				if (++tokenIndex == tokens.Count)
				{
					--tokenIndex;
					while (tokenIndex >= 0 && tokens[tokenIndex].IsMissing())
						--tokenIndex;
					characterIndex = 0;
					break;
				}
				continue;
			}
			if (characterIndex == 0)
				break;
			result = tokens[tokenIndex];
			if (tokenIndex < tokens.Count)
				characterIndex -= result.text.length;
			if (characterIndex > 0 && tokenIndex < tokens.Count - 1)
				++tokenIndex;
			else
				break;
		}

		atTokenEnd = characterIndex == 0;
		return result; //tokens[tokenIndex];
	}

	public SyntaxToken GetNonTriviaTokenAfter(SyntaxToken token)
	{
		int lineIndex = token.Line;
		int tokenIndex = token.TokenIndex;
		while (lineIndex < formattedLines.Count)
		{
			var tokens = formattedLines[lineIndex].tokens;
			
			++tokenIndex;
			while (tokenIndex < tokens.Count && tokens[tokenIndex].tokenKind <= SyntaxToken.Kind.LastWSToken)
				++tokenIndex;
			
			if (tokenIndex < tokens.Count)
				return tokens[tokenIndex];
			
			++lineIndex;
			if (lineIndex < formattedLines.Count)
				tokenIndex = -1;
		}
		return null;
	}
	
	public SyntaxToken GetNonTriviaTokenLeftOf(ref int lineIndex, ref int tokenIndex)
	{
		while (lineIndex > 0)
		{
			var tokens = formattedLines[lineIndex].tokens;
			
			--tokenIndex;
			while (tokenIndex >= 0 && tokens[tokenIndex].tokenKind <= SyntaxToken.Kind.LastWSToken)
				--tokenIndex;
			
			if (tokenIndex >= 0)
				return tokens[tokenIndex];
			
			--lineIndex;
			if (lineIndex >= 0)
				tokenIndex = formattedLines[lineIndex].tokens.Count;
		}
		return null;
	}
	
	public SyntaxToken GetNonTriviaTokenLeftOf(CaretPos position, out int lineIndex, out int tokenIndex)
	{
		lineIndex = position.line;
		tokenIndex = -1;

		var characterIndex = position.characterIndex;
		var tokens = formattedLines[lineIndex].tokens;
		if (tokens == null)
			return null;
		
		if (tokens.Count > 0)
		{
			while (characterIndex > 0)
			{
				if (++tokenIndex == tokens.Count - 1)
					break;
				characterIndex -= tokens[tokenIndex].text.length;
			}
		}

		while (tokenIndex < 0 || tokens[tokenIndex].tokenKind <= SyntaxToken.Kind.LastWSToken)
		{
			if (tokenIndex >= 0)
			{
				--tokenIndex;
			}
			else if (lineIndex > 0)
			{
				tokens = formattedLines[--lineIndex].tokens;
				tokenIndex = tokens.Count - 1;
			}
			else
			{
				break;
			}
		}

		return tokenIndex >= 0 ? tokens[tokenIndex] : null;
	}

	public SyntaxToken GetNonTriviaTokenLeftOf(int lineIndex, int characterIndex)
	{
		var tokenIndex = -1;

		var tokens = formattedLines[lineIndex].tokens;
		if (tokens == null)
			return null;
		if (tokens.Count > 0)
		{
			while (characterIndex > 0)
			{
				if (++tokenIndex == tokens.Count - 1)
					break;
				characterIndex -= tokens[tokenIndex].text.length;
			}
		}

		while (tokenIndex < 0 || tokens[tokenIndex].tokenKind <= SyntaxToken.Kind.LastWSToken)
		{
			if (tokenIndex >= 0)
			{
				--tokenIndex;
			}
			else if (lineIndex > 0)
			{
				tokens = formattedLines[--lineIndex].tokens;
				tokenIndex = tokens.Count - 1;
			}
			else
			{
				break;
			}
		}

		return tokenIndex >= 0 ? tokens[tokenIndex] : null;
	}
}
