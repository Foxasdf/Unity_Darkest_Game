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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using FormattedLine = FGTextBuffer.FormattedLine;
using Debug = UnityEngine.Debug;

[Serializable]
public struct TextPosition : IComparable<TextPosition>, IEquatable<TextPosition>
{
	public static TextPosition invalid = new TextPosition(-1, -1);
	
	public int line;
	public int index;

	public TextPosition(int line, int index)
	{
		this.line = line;
		this.index = index;
	}

	public void Set(int line, int index)
	{
		this.line = line;
		this.index = index;
	}
	
	public int CompareTo(TextPosition other)
	{
		var diff = line - other.line;
		return diff == 0 ? index - other.index : diff;
	}
	
	public bool Equals(TextPosition other)
	{
		return line == other.line && index == other.index;
	}

	public static TextPosition operator + (TextPosition other, int offset)
	{
		return new TextPosition { line = other.line, index = other.index + offset };
	}

	public static bool operator == (TextPosition lhs, TextPosition rhs)
	{
		return lhs.line == rhs.line && lhs.index == rhs.index;
	}
	
	public static bool operator != (TextPosition lhs, TextPosition rhs)
	{
		return lhs.line != rhs.line || lhs.index != rhs.index;
	}
	
	public static bool operator < (TextPosition lhs, TextPosition rhs)
	{
		return lhs.line < rhs.line || lhs.line == rhs.line && lhs.index < rhs.index;
	}

	public static bool operator <= (TextPosition lhs, TextPosition rhs)
	{
		return lhs.line < rhs.line || lhs.line == rhs.line && lhs.index <= rhs.index;
	}
	
	public static bool operator > (TextPosition lhs, TextPosition rhs)
	{
		return lhs.line > rhs.line || lhs.line == rhs.line && lhs.index > rhs.index;
	}

	public static bool operator >= (TextPosition lhs, TextPosition rhs)
	{
		return lhs.line > rhs.line || lhs.line == rhs.line && lhs.index >= rhs.index;
	}

	public override bool Equals(object obj)
	{
		if (obj == null || !(obj is TextPosition))
			return false;

		var rhs = (TextPosition) obj;
		return line == rhs.line && index == rhs.index;
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = (int)2166136261;
			hash = hash * 16777619 ^ line;
			hash = hash * 16777619 ^ index;
			return hash;
		}
	}
	
	public bool OnInsertedText(TextPosition from, TextPosition to, bool moveIfEqual)
	{
		if (moveIfEqual)
		{
			if (from > this)
				return false;
		}
		else
		{
			if (from >= this)
				return false;
		}
		
		if (from.line == line)
			index += to.index - from.index;
		line += to.line - from.line;
		return true;
	}
	
	public bool OnRemovedText(TextPosition from, TextPosition to)
	{
		if (from >= this)
			return false;
		
		if (to >= this)
		{
			line = from.line;
			index = from.index;
		}
		else
		{
			if (to.line == line)
				index -= to.index - from.index;
			line -= to.line - from.line;
		}
		return true;
	}

	public bool Move(FGTextBuffer textBuffer, int offset)
	{
		while (offset > 0)
		{
			var lineLength = textBuffer.lines[line].Length;
			if (index + offset <= lineLength)
			{
				index += offset;
				if (index == lineLength)
				{
					index = 0;
					++line;
				}
				return true;
			}

			offset -= lineLength - index;
			++line;
			index = 0;

			if (line >= textBuffer.lines.Count)
			{
				line = textBuffer.lines.Count;
				index = 0;
				return false;
			}
		}

		while (offset < 0)
		{
			if (index + offset >= 0)
			{
				index += offset;
				return true;
			}

			offset += index;
			--line;
			if (line < 0)
			{
				line = 0;
				index = 0;
				return false;
			}
			index = textBuffer.lines[line].Length;
		}

		return true;
	}

	public override string ToString()
	{
		return "TextPosition (line: " + line + ", index: " + index + ")";
	}
}

public struct TextOffset
{
	public int lines;
	public int indexOffset;
}

public struct TextSpan : IEquatable<TextSpan>
{
	public int line;
	public int index;
	public int lineOffset;
	public int indexOffset;
	
	public override int GetHashCode()
	{
		unchecked
		{
			int hash = (int)2166136261;
			hash = (hash * 16777619) ^ line;
			hash = (hash * 16777619) ^ index;
			hash = (hash * 16777619) ^ lineOffset;
			hash = (hash * 16777619) ^ indexOffset;
			return hash;
		}
	}
	
	public override bool Equals(object obj)
	{
		if (obj == null || !(obj is TextSpan))
			return false;
		
		var other = (TextSpan)obj;
		return Equals(other);
	}
	
	public bool Equals(TextSpan other)
	{
		return line == other.line && index == other.index && lineOffset == other.lineOffset && indexOffset == other.indexOffset;
	}
	
	public static bool operator == (TextSpan lhs, TextSpan rhs)
	{
		return lhs.line == rhs.line && lhs.index == rhs.index && lhs.lineOffset == rhs.lineOffset && lhs.indexOffset == rhs.indexOffset;
	}
	
	public static bool operator != (TextSpan lhs, TextSpan rhs)
	{
		return lhs.line != rhs.line || lhs.index != rhs.index || lhs.lineOffset != rhs.lineOffset || lhs.indexOffset != rhs.indexOffset;
	}

	public override string ToString()
	{
		if (lineOffset != 0)
			return "TextSpan{ line = " + (line+1) + ", fromChar = " + index + ", lineOffset = " + lineOffset + ", toChar = " + indexOffset + " }";
		else
			return "TextSpan{ line = " + (line+1) + ", fromChar = " + index + ", lineOffset = " + lineOffset + ", toChar = " + (index + indexOffset) + " }";
	}

	public static TextSpan CreateEmpty(TextPosition position)
	{
		return new TextSpan { line = position.line, index = position.index };
	}

	public static TextSpan Create(TextPosition from, TextPosition to)
	{
		return new TextSpan
		{
			line = from.line,
			index = from.index,
			lineOffset = to.line - from.line,
			indexOffset = to.index - (to.line == from.line ? from.index : 0)
		};
	}

	public static TextSpan CreateBetween(TextSpan from, TextSpan to)
	{
		return Create(from.EndPosition, to.StartPosition);
	}

	public static TextSpan CreateEnclosing(TextSpan from, TextSpan to)
	{
		return Create(from.StartPosition, to.EndPosition);
	}

	public static TextSpan Create(TextPosition start, TextOffset length)
	{
		return new TextSpan
		{
			line = start.line,
			index = start.index,
			lineOffset = length.lines,
			indexOffset = length.indexOffset
		};
	}

	public TextPosition StartPosition
	{
		get { return new TextPosition { line = line, index = index }; }
		set
		{
			if (value.line == line + lineOffset)
			{
				line = value.line;
				lineOffset = 0;
				indexOffset = index + indexOffset - value.index;
				index = value.index;
			}
			else
			{
				lineOffset = line + lineOffset - value.line;
				line = value.line;
				index = value.index;
			}
		}
	}
	
	public bool IsEmpty
	{
		get { return lineOffset == 0 && indexOffset == 0; }
	}

	public TextPosition EndPosition
	{
		get { return new TextPosition { line = line + lineOffset, index = indexOffset + (lineOffset == 0 ? index : 0) }; }
		set
		{
			if (value.line == line)
			{
				lineOffset = 0;
				indexOffset = value.index - index;
			}
			else
			{
				lineOffset = value.line - line;
				indexOffset = value.index;
			}
		}
	}

	public void Offset(int deltaLines, int deltaIndex)
	{
		line += deltaLines;
		index += deltaIndex;
	}

	public bool Contains(TextPosition position)
	{
		return !(position.line < line
			|| position.line == line && (position.index < index || lineOffset == 0 && position.index > index + indexOffset)
			|| position.line > line + lineOffset
			|| position.line == line + lineOffset && position.index > indexOffset);
	}
}

public struct HashID : IEquatable<HashID>, IEquatable<int>, IComparable<HashID>, IComparable<int>
{
	//	private static Dictionary<int, string> values = new Dictionary<int, string>();
	
	public int hashID;
	
	public HashID(int hashID) { this.hashID = hashID; }
	public HashID(string text) { hashID = SymbolDefinition.GetHashID(text); /*values[hashID] = text;*/ }
	
	public override int GetHashCode() { return hashID; }
	public override bool Equals(object x)
	{
		return hashID == ((HashID)x).hashID;
	}
	
	public static bool operator == (HashID lhs, HashID rhs) { return lhs.hashID == rhs.hashID; }
	public static bool operator != (HashID lhs, HashID rhs) { return lhs.hashID != rhs.hashID; }
	public bool Equals(HashID x)
	{
		return hashID == x.hashID;
	}
	public bool Equals(int x)
	{
		return hashID == x;
	}
	
	public int CompareTo(HashID x) { return hashID < x.hashID ? -1 : hashID == x.hashID ? 0 : 1; }
	public int CompareTo(int x) { return hashID < x ? -1 : hashID == x ? 0 : 1; }
	
	public static implicit operator int(HashID x) { return x.hashID; }
	public static implicit operator HashID(int x) { return new HashID { hashID = x }; }
	
	//public static implicit operator string(HashID x) { return values[x.hashID]; }
	public static implicit operator HashID(string x) { return new HashID(x); }
}

public class _HashIDSet : Dictionary<HashID, bool>
{
	public _HashIDSet() : base() {}
	public _HashIDSet(int capacity) : base(capacity) {}
	
	public new void Add(HashID hashID, bool b)
	{
		base.Add(hashID, false);
	}
	public void Add(string text)
	{
		base.Add(new HashID(text), false);
	}
	//public void Add(int hashID)
	//{
	//	base.Add(new HashID { hashID = hashID }, false);
	//}
	
	public bool Contains(int hashID)
	{
		//HashID discard;
		//return base.TryGetValue(hashID, out _);
		return base.ContainsKey(hashID);
	}
	
	public bool Contains(string text)
	{
		//HashID discard;
		//return base.TryGetValue(SymbolDefinition.GetHashID(text), out _);
		return base.ContainsKey(SymbolDefinition.GetHashID(text));
	}
	
	public bool Contains(HashID hashID)
	{
		//HashID discard;
		//return base.TryGetValue(hashID, out _);
		return base.ContainsKey(hashID);
	}
}

public class HashIDSet : Dictionary<HashID, string>
{
	public HashIDSet() : base() {}
	public HashIDSet(int capacity) : base(capacity) {}
	
	public void Add(string text)
	{
		base.Add(SymbolDefinition.GetHashID(text), text);
	}
	//public void Add(int hashID)
	//{
	//	base.Add(new HashID { hashID = hashID }, false);
	//}
	
	public bool Contains(int hashID)
	{
		//HashID discard;
		//return base.TryGetValue(hashID, out _);
		return base.ContainsKey(hashID);
	}
	
	public bool Contains(string text)
	{
		//HashID discard;
		//return base.TryGetValue(SymbolDefinition.GetHashID(text), out _);
		return base.ContainsKey(SymbolDefinition.GetHashID(text));
	}
	
	public bool Contains(HashID hashID)
	{
		//HashID discard;
		//return base.TryGetValue(hashID, out _);
		return base.ContainsKey(hashID);
	}
}

public struct CharSpan : IEquatable<CharSpan>, IEquatable<string>, IComparable<CharSpan>
{
	public static readonly CharSpan Empty = new CharSpan(string.Empty);
	
	private string source;
	private int start;
	public int length;
	
	public string Source() { return source; }
	
	public CharSpan(CharSpan x)
	{
		source = x.source;
		start = x.start;
		length = x.length;;
	}
	
	public CharSpan(string text)
	{
		source = text;
		start = 0;
		length = text.Length;;
	}
	
	public CharSpan(string text, int startIndex)
	{
		source = text;
		start = startIndex;
		length = text.Length - startIndex;
		
		//if (startIndex < 0 || this.length < 0)
		//	throw new ArgumentOutOfRangeException("startIndex");
	}
	
	public CharSpan(CharSpan x, int startIndex)
	{
		source = x.source;
		start = x.start + startIndex;
		length = x.length - startIndex;
		
		//if (startIndex < 0 || this.length < 0)
		//	throw new ArgumentOutOfRangeException("startIndex");
	}
	
	public CharSpan(string text, int startIndex, int length)
	{
		source = text;
		start = startIndex;
		this.length = length;
		
		//var textLength = text.Length;
		//if (startIndex < 0 || startIndex > textLength)
		//	throw new ArgumentOutOfRangeException("startIndex");
		//if (startIndex + length > textLength)
		//	throw new ArgumentOutOfRangeException("length");
	}
	
	public CharSpan(CharSpan x, int startIndex, int length)
	{
		source = x.source;
		start = x.start + startIndex;
		this.length = length;
		
		//var textLength = text.Length;
		//if (startIndex < 0 || startIndex > textLength)
		//	throw new ArgumentOutOfRangeException("startIndex");
		//if (startIndex + length > textLength)
		//	throw new ArgumentOutOfRangeException("length");
	}
	
	public override bool Equals(object obj)
	{
		throw new InvalidOperationException();
	}
	
	public bool Equals(CharSpan x)
	{
		if (length != x.length)
			return false;
		var xSource = x.source;
		int xStart = x.start;
		if (source == xSource && start == xStart)
			return true;
		for (var i = 0; i < length; ++i)
			if (source[start + i] != xSource[xStart + i])
				return false;
		return true;
	}
	
	public bool Equals(string x)
	{
		if (length != x.Length)
			return false;
		if (start == 0 && source == x)
			return true;
		for (int i = 0; i < length; ++i)
			if (source[start + i] != x[i])
				return false;
		return true;
	}
	
	public int CompareTo(CharSpan x)
	{
		var xSource = x.source;
		int xStart = x.start;
		var len = length <= x.length ? length : x.length;
		for (int i = 0; i < len; ++i)
		{
			int d = source[i + start] - xSource[i + xStart];
			if (d < 0)
				return -1;
			if (d > 0)
				return 1;
		}
		return length < len ? -1 : x.length < len ? 1 : 0;
	}
	
	public int CompareToIgnoreCase(CharSpan x)
	{
		var xSource = x.source;
		int xStart = x.start;
		var len = length <= x.length ? length : x.length;
		for (int i = 0; i < len; ++i)
		{
			int d = source[i + start].ToLowerAsciiInvariant() - xSource[i + xStart].ToLowerAsciiInvariant();
			if (d < 0)
				return -1;
			if (d > 0)
				return 1;
		}
		return length < len ? -1 : x.length < len ? 1 : 0;
	}
	
	public override int GetHashCode()
	{
		unchecked
		{
			int hash1 = 5381;
			int hash2 = hash1;
			if (source != null)
			{
				int end = start + length;
				for (int i = start; i < end; )
				{
					hash1 = ((hash1 << 5) + hash1) ^ source[i++];
					hash2 = i < end ? ((hash2 << 5) + hash2) ^ source[i++] : hash2;
				}
			}
			return hash1 + (hash2 * 1566083941);
		}
	}
	
	public void DecodeId()
	{
		if (length != 0 && source[start] == '@')
		{
			++start;
			--length;
		}
	}
	
	public void SetText(string text)
	{
		source = text;
		start = 0;
		length = text.Length;
	}
	
	public int IndexOf(string s)
	{
		int sLength = s.Length;
		int end = start + length - sLength + 1;
		for (int i = start; i < end; ++i)
		{
			bool match = true;
			for (int j = sLength; j --> 0; )
			{
				if (source[i + j] != s[j])
				{
					match = false;
					break;
				}
			}
			if (match)
				return i - start;
		}
		return -1;
	}
	
	public int IndexOf(string s, int startAt)
	{
		int sLength = s.Length;
		int end = start + length - sLength + 1;
		for (int i = start + startAt; i < end; ++i)
		{
			bool match = true;
			for (int j = sLength; j --> 0; )
			{
				if (source[i + j] != s[j])
				{
					match = false;
					break;
				}
			}
			if (match)
				return i - start;
		}
		return -1;
	}
	
	public int IndexOf(char c)
	{
		int end = start + length;
		for (int i = start; i < end; ++i)
			if (source[i] == c)
				return i - start;
		return -1;
	}
	
	public int IndexOf(char c, int startAt)
	{
		int end = start + length;
		for (int i = start + startAt; i < end; ++i)
			if (source[i] == c)
				return i - start;
		return -1;
	}
	
	public char this[int index]
	{
		get
		{
			return source[start + index];
		}
	}
	
	public bool StartsWithIgnoreCase(string s)
	{
		var len = s.Length;
		if (length < len)
			return false;

		var i = 0;
		while (i < len && source[start + i].ToLowerAsciiInvariant() == s[i].ToLowerAsciiInvariant())
		{
			i++;
		}

		return i == len;
	}
	
	public bool StartsWithIgnoreCase(CharSpan span)
	{
		var len = span.length;
		if (length < len)
			return false;

		var spanSource = span.source;
		int spanStart = span.start;

		var i = 0;
		while (i < len && source[start + i].ToLowerAsciiInvariant() == spanSource[spanStart + i].ToLowerAsciiInvariant())
		{
			i++;
		}

		return i == len;
	}
	
	public bool FastStartsWith(string s)
	{
		var len = s.Length;
		if (length < len)
			return false;

		var i = 0;
		while (i < len && source[start + i] == s[i])
		{
			i++;
		}

		return i == len;
	}
	
	public bool FastEndsWith(string s)
	{
		var i = length - 1;
		var j = s.Length - 1;
		if (i < j)
			return false;

		i += start;
		while (j >= 0 && source[i] == s[j])
		{
			i--;
			j--;
		}

		return j < 0;
	}
	
	public CharSpan Replace(string a, string b)
	{
		if (source.IndexOf(a, start, length) < 0)
			return this;
		return GetString().Replace(a, b);
	}
	
	public CharSpan Replace(char a, char b)
	{
		if (IndexOf(a) < 0)
			return this;
		return GetString().Replace(a, b);
	}
	
	public CharSpan TrimEnd(char c)
	{
		int last = start + this.length - 1;
		while (last >= 0)
		{
			var lastChar = source[last];
			if (lastChar != c)
				break;
			--last;
		}
		
		int length = last + 1 - start;
		if (start == 0 && length == this.length)
			return this;
		else if (length == 0)
			return Empty;
		else
			return new CharSpan(source, start, length);
	}
	
	public CharSpan TrimEnd(char c1, char c2)
	{
		int last = start + this.length - 1;
		while (last >= 0)
		{
			var lastChar = source[last];
			if (lastChar != c1 && lastChar != c2)
				break;
			--last;
		}
		
		int length = last + 1 - start;
		if (start == 0 && length == this.length)
			return this;
		else if (length == 0)
			return Empty;
		else
			return new CharSpan(source, start, length);
	}
	
	public CharSpan TrimEnd(params char[] chars)
	{
		int last = start + this.length - 1;
		while (last >= 0)
		{
			var trim = false;
			var lastChar = source[last];
			for (int i = chars.Length; i --> 0; )
				if (lastChar == chars[i])
				{
					trim = true;
					break;
				}
			
			if (trim)
				--last;
			else
				break;
		}
		
		int length = last + 1 - start;
		if (start == 0 && length == this.length)
			return this;
		else if (length == 0)
			return Empty;
		else
			return new CharSpan(source, start, length);
	}
	
	public static bool operator == (CharSpan a, CharSpan b)
	{
		return a.Equals(b);
	}
	
	public static bool operator != (CharSpan a, CharSpan b)
	{
		return !a.Equals(b);
	}
	
	public static bool operator == (CharSpan a, string b)
	{
		return a.Equals(b);
	}
	
	public static bool operator != (CharSpan a, string b)
	{
		return !a.Equals(b);
	}
	
	public static bool operator == (string a, CharSpan b)
	{
		return b.Equals(a);
	}
	
	public static bool operator != (string a, CharSpan b)
	{
		return !b.Equals(a);
	}
	
	public bool IsEmpty { get { return length == 0; } }
	
	//public int Length { get { return length; } }
	
	public CharSpan Substring(int startIndex)
	{
		return new CharSpan(source, start + startIndex, length - startIndex);
	}
	
	public CharSpan Substring(int startIndex, int length)
	{
		return new CharSpan(source, start + startIndex, length);
	}
	
	public string GetString()
	{
		if (source == null)
			return null;
		source = source.Substring(start, length);
		start = 0;
		return source;
	}
	
	public override string ToString()
	{
		source = source.Substring(start, length);
		start = 0;
		return source;
	}
	
	public static implicit operator CharSpan(string text)
	{
		return text != null ? new CharSpan(text) : Empty;
	}
	
	public static implicit operator string(CharSpan span)
	{
		if (span.length == 0)
			return "";
		span.source = span.source.Substring(span.start, span.length);
		span.start = 0;
		return span.source;
	}
}

public class SyntaxToken
{
	public enum Kind : byte
	{
		Missing,
		Whitespace,
		Comment,
		Preprocessor,
		PreprocessorArguments,
		PreprocessorSymbol,
		PreprocessorDirectiveExpected,
		PreprocessorCommentExpected,
		PreprocessorUnexpectedDirective,
		VerbatimStringLiteral,
		
		LastWSToken, // Marker only
		
		VerbatimStringBegin,
		BuiltInLiteral,
		CharLiteral,
		StringLiteral,
		InterpolatedStringWholeLiteral,
		InterpolatedStringStartLiteral,
		InterpolatedStringMidLiteral,
		InterpolatedStringEndLiteral,
		InterpolatedStringFormatLiteral,
		IntegerLiteral,
		RealLiteral,
		Punctuator,
		Keyword,
		Identifier,
		ContextualKeyword,
		
		Partial,
		Collapsed,
		
		EOF,
	}

	public Kind tokenKind;
	public FGTextEditor.StyleID style;
	public short tokenId;
	public ParseTree.Leaf parent;
	public CharSpan text;

	public FormattedLine formattedLine;

	public int Line { get { return formattedLine == null ? 0 : formattedLine.index; } }
	public int TokenIndex { get { return formattedLine.tokens.IndexOf(this); } }

	public SyntaxToken(Kind kind, CharSpan text)
	{
		tokenKind = kind;
		this.text = text;
		tokenId = -1;
	}
	
	public void SetText(CharSpan text)
	{
		this.text = text;
	}

	public bool IsMissing()
	{
		return tokenKind == Kind.Missing;
	}

	public override string ToString() { return tokenKind +"(\"" + text + "\")"; }

	public string Dump() { return "[Token: " + tokenKind + " \"" + text + "\"]"; }
}

public class PartialSyntaxToken : SyntaxToken
{
	public SyntaxToken wholeToken;
	
	public PartialSyntaxToken(SyntaxToken wholeToken, string text)
		: base(wholeToken.tokenKind, text)
	{
		this.wholeToken = wholeToken;
		
		style = wholeToken.style;
		parent = wholeToken.parent;
		tokenId = wholeToken.tokenId;
		formattedLine = wholeToken.formattedLine;
	}
}

[UnityEditor.InitializeOnLoad]
public abstract class FGParser
{
	public static bool isCSharp4 =
#if UNITY_2019_3_OR_NEWER
		false;
#elif UNITY_2017_1_OR_NEWER
		UnityEditor.PlayerSettings.scriptingRuntimeVersion == UnityEditor.ScriptingRuntimeVersion.Legacy;
#else
		true;
#endif

	public static bool isCSharp8 =
#if UNITY_2020_2_OR_NEWER
		true;
#else
		false;
#endif

	public static bool isCSharp9 =
#if UNITY_2021_2_OR_NEWER
		true;
#else
		false;
#endif

	protected static readonly char[] whitespaces = { ' ', '\t' };

	private static readonly Dictionary<string, Type> parserTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

	public static FGParser Create(FGTextBuffer textBuffer, string path)
	{
		Type parserType;
		FGParser parser;
		var extension = Path.GetExtension(path) ?? String.Empty;
		if (!AssemblyDefinition.IsIgnoredScript(path) && parserTypes.TryGetValue(extension, out parserType))
		{
			parser = (FGParser) Activator.CreateInstance(parserType);
		}
		else
		{
			parser = new TextParser();
		}
		
		parser.textBuffer = textBuffer;
		parser.assetPath = path;
		return parser;
	}

	private static void RegisterParsers()
	{
		parserTypes.Add(".cs", typeof(CsParser));
		parserTypes.Add(".js", typeof(JsParser));
		parserTypes.Add(".boo", typeof(BooParser));
		
		parserTypes.Add(".shader", typeof(ShaderParser));
		parserTypes.Add(".cg", typeof(ShaderParser));
		parserTypes.Add(".cginc", typeof(ShaderParser));
		parserTypes.Add(".hlsl", typeof(ShaderParser));
		parserTypes.Add(".hlslinc", typeof(ShaderParser));
		
		parserTypes.Add(".txt", typeof(TextParser));
	}

	static FGParser()
	{
		RegisterParsers();
	}


	// Instance members

	protected string assetPath;

	protected FGTextBuffer textBuffer;
	public ParseTree parseTree { get; protected set; }

	public HashSet<string> scriptDefines;
	public bool scriptDefinesChanged;

	protected static List<SyntaxToken> tokens = new List<SyntaxToken>(128);

	public void OnLoaded()
	{
		//scriptDefines = new HashSet<string>(UnityEditor.EditorUserBuildSettings.activeScriptCompilationDefines);
		scriptDefinesChanged = false;
		
		ParseAll(assetPath);
	}

	public virtual FGGrammar.IScanner MoveAfterLeaf(ParseTree.Leaf leaf)
	{
		return null;
	}

	public virtual bool ParseLines(int fromLine, int toLineInclusive)
	{
		return true;
	}

	protected Thread parserThread;
	
	public virtual void FullRefresh()
	{
		if (parserThread != null)
			parserThread.Join();
		parserThread = null;
	}

	public virtual void LexLine(int currentLine, FormattedLine formattedLine)
	{
		formattedLine.index = currentLine;

		if (parserThread != null)
			parserThread.Join();
		parserThread = null;

		string textLine = textBuffer.lines[currentLine];
		var lineTokens = formattedLine.tokens ?? new List<SyntaxToken>();
		lineTokens.Clear();
		formattedLine.tokens = lineTokens;

		if (!string.IsNullOrEmpty(textLine))
		{
			//Tokenize(lineTokens, textLine, ref formattedLine.blockState);
			lineTokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, textLine) { formattedLine = formattedLine });

			//var lineWidth = textBuffer.CharIndexToColumn(textLine.Length, currentLine);
			//if (lineWidth > textBuffer.longestLine)
			//	textBuffer.longestLine = lineWidth;
		}
	}
	
	protected virtual void ParseAll(string bufferName)
	{
	}

	public virtual void CutParseTree(int fromLine, List<FormattedLine> formattedLines)
	{
		if (parseTree == null)
			return;

		ParseTree.BaseNode cut = null;
		var prevLine = fromLine;
		while (cut == null && prevLine --> 0)
		{
			var tokens = textBuffer.formattedLines[prevLine].tokens;
			if (tokens != null)
			{
				for (var i = tokens.Count; i --> 0; )
				{
					if (tokens[i].tokenKind > SyntaxToken.Kind.LastWSToken && tokens[i].parent != null &&
						tokens[i].parent.syntaxError == null)
					{
						cut = tokens[i].parent;
						break;
					}
				}
			}
		}

		var cutThis = false;
		if (cut == null)
		{
			cut = parseTree.root.ChildAt(0);
			cutThis = true;
		}

		while (cut != null)
		{
			var cutParent = cut.parent;
			if (cutParent == null)
				break;
			var cutIndex = cutThis ? cut.childIndex : cut.childIndex + 1;
			while (cutIndex > 0)
			{
				var child = cutParent.ChildAt(cutIndex - 1);
				if (child != null && !child.HasLeafs())
					--cutIndex;
				else
					break;
			}
			cutThis = /*cutThis &&*/ cutIndex == 0;
			if (cutIndex < cutParent.numValidNodes)
			{
				cutParent.InvalidateFrom(cutIndex);
			}
			cut = cutParent;
			cut.syntaxError = null;
		}
	}
	
	public virtual void SetTokenStyleID(SyntaxToken token)
	{
		token.style = FGTextEditor.StyleID.Normal;
	}

//int hyperlink = IndexOf3(line, startAt, "http://", "https://", "ftp://");
//if (hyperlink == -1)
//	hyperlink = line.Length;

//while (hyperlink != startAt)
//{
//	Match emailMatch = emailRegex.Match(line, startAt, hyperlink - startAt);
//	if (emailMatch.Success)
//	{
//		if (emailMatch.Index > startAt)
//			blocks.Add(new TextBlock(line.Substring(startAt, emailMatch.Index - startAt), commentStyle));

//		address = line.Substring(emailMatch.Index, emailMatch.Length);
//		blocks.Add(new TextBlock(address, textBuffer.styles.mailtoStyle));
//		address = "mailto:" + address;
//		if (textBuffer.IsLoading)
//		{
//			index = Array.BinarySearch<string>(textBuffer.hyperlinks, address, StringComparer.OrdinalIgnoreCase);
//			if (index < 0)
//				ArrayUtility.Insert(ref textBuffer.hyperlinks, -1 - index, address);
//		}

//		startAt = emailMatch.Index + emailMatch.Length;
//		continue;
//	}

//	blocks.Add(new TextBlock(line.Substring(startAt, hyperlink - startAt), commentStyle));
//	startAt = hyperlink;
//}

//if (startAt == line.Length)
//	break;

//int i = line.IndexOf(':', startAt) + 3;
//while (i < line.Length)
//{
//	char c = line[i];
//	if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_' || c == '.' ||
//		c == '-' || c == '=' || c == '+' || c == '%' || c == '&' || c == '?' || c == '/' || c == '#')
//		++i;
//	else
//		break;
//}

//address = line.Substring(startAt, i - startAt);
//blocks.Add(new TextBlock(address, textBuffer.styles.hyperlinkStyle));
//if (textBuffer.IsLoading)
//{
//	index = Array.BinarySearch<string>(textBuffer.hyperlinks, address, StringComparer.OrdinalIgnoreCase);
//	if (index < 0)
//		ArrayUtility.Insert(ref textBuffer.hyperlinks, -1 - index, address);
//}

	private HashIDSet emptyHashIDSet = new HashIDSet();
	public virtual HashIDSet Keywords { get { return emptyHashIDSet; } }
	public virtual HashIDSet BuiltInLiterals { get { return emptyHashIDSet; } }
	
	protected static HashIDSet scriptLiterals = new HashIDSet { "false", "null", "true", };

	//protected static HashSet<string> unityTypes;

	public virtual bool IsBuiltInType(CharSpan word)
	{
		return false;
	}
	
	public abstract bool IsBuiltInLiteral(CharSpan word);

	//protected bool IsUnityType(string word)
	//{
	//	return textBuffer.isShader ? false : unityTypes.Contains(word);
	//}

	public Func<bool> Update(int fromLine, int toLineInclusive)
	{
		//	var t = new Stopwatch();
		//	t.Start();

		var lastLine = textBuffer.formattedLines.Count - 1; // Mathf.Min(textBuffer.formattedLines.Length - 1, toLineInclusive);
		try
		{
			if (this.parseTree != null)
				ParseLines(fromLine, lastLine);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
		//if (toLineInclusive < textBuffer.lines.Count)
		//{
		//    progressiveParseLine = toLineInclusive + 1;
		//    return ProgressiveParser;
		//}
		//else
		{
			// TODO: Temporary solution, discarding all unused invalid parse tree nodes
			if (parseTree != null && parseTree.root != null)
	 			parseTree.root.CleanUp();
		}
		//ParseAll(assetPath);

	//	t.Stop();
	//	Debug.Log("Updated parser for lines " + (fromLine + 1) + "-" + (toLineInclusive + 1) + " in " + t.ElapsedMilliseconds + " ms");
		return null;
	}

	//private int progressiveParseLine = -1;
	//private bool ProgressiveParser()
	//{
	//	if (textBuffer == null || textBuffer.lines == null || textBuffer.lines.Count <= progressiveParseLine)
	//	{
	//		progressiveParseLine = -1;
	//		return false;
	//	}

	//	if (!ParseLines(progressiveParseLine, progressiveParseLine))
	//		return false;
	//	++progressiveParseLine;
	//	if (progressiveParseLine < textBuffer.lines.Count)
	//		return true;

	//	progressiveParseLine = -1;
	//	return false;
	//}

	protected static SyntaxToken ScanWhitespace(CharSpan line, ref int startAt)
	{
		int i = startAt;
		int length = line.length;
		while (i < length)
		{
			var c = line[i];
			if (c == ' ' || c == '\t' || c == '\xa0')
				++i;
			else
				break;
		}
		if (i == startAt)
			return null;

		var token = new SyntaxToken(SyntaxToken.Kind.Whitespace, new CharSpan(line, startAt, i - startAt));
		startAt = i;
		return token;
	}

	protected static SyntaxToken ScanWord(CharSpan line, ref int startAt)
	{
		int i = startAt;
		while (i < line.length)
		{
			var c = line[i];
			if (!Char.IsLetterOrDigit(c) && c != '_')
				break;
			++i;
		}
		var token = new SyntaxToken(SyntaxToken.Kind.Identifier, new CharSpan(line, startAt, i - startAt));
		startAt = i;
		return token;
	}

	protected static bool ScanUnicodeEscapeChar(CharSpan line, ref int startAt)
	{
		if (startAt >= line.length - 5)
			return false;
		if (line[startAt] != '\\')
			return false;
		int i = startAt + 1;
		if (line[i] != 'u' && line[i] != 'U')
			return false;
		var n = line[i] == 'u' ? 4 : 8;
		++i;
		while (n > 0)
		{
			if (!ScanHexDigit(line, ref i))
				break;
			--n;
		}
		if (n == 0)
		{
			startAt = i;
			return true;
		}
		return false;
	}

	protected static SyntaxToken ScanCharLiteral(CharSpan line, ref int startAt)
	{
		var i = startAt + 1;
		while (i < line.length)
		{
			if (line[i] == '\'')
			{
				++i;
				break;
			}
			if (line[i] == '\\' && i < line.length - 1)
				++i;
			++i;
		}
		var token = new SyntaxToken(SyntaxToken.Kind.CharLiteral, new CharSpan(line, startAt, i - startAt));
		startAt = i;
		return token;
	}

	protected static SyntaxToken ScanStringLiteral(CharSpan line, ref int startAt)
	{
		var i = startAt + 1;
		if (line[startAt] == '$')
			++i;
		while (i < line.length)
		{
			if (line[i] == '\"')
			{
				++i;
				break;
			}
			if (line[i] == '\\' && i < line.length - 1)
				++i;
			++i;
		}
		var token = new SyntaxToken(SyntaxToken.Kind.StringLiteral, new CharSpan(line, startAt, i - startAt));
		startAt = i;
		return token;
	}
	
	protected static void ScanDigitsWithUnderscore(CharSpan line, ref int i)
	{
		if (isCSharp4)
		{
			while (i < line.length)
			{
				char c = line[i];
				if (c >= '0' && c <= '9')
					++i;
				else
					break;
			}
			return;
		}
		
		while (i < line.length)
		{
			char c = line[i];
			if (c >= '0' && c <= '9')
			{
				++i;
				continue;
			}
			
			var underscore = i;
			while (underscore < line.length && line[underscore] == '_')
				++underscore;
			
			if (underscore == i || underscore == line.length)
				break;
			
			c = line[underscore];
			if (c < '0' || c > '9')
				break;
	
			i = underscore + 1;
		};
	}
	
	protected static void ScanBinaryDigitsWithUnderscore(CharSpan line, ref int i)
	{
		while (i < line.length)
		{
			char c = line[i];
			if (c == '0' || c == '1')
			{
				++i;
				continue;
			}
			
			var underscore = i;
			while (underscore < line.length && line[underscore] == '_')
				++underscore;
			
			if (underscore == i || underscore == line.length)
				break;
			
			c = line[underscore];
			if (c != '0' && c != '1')
				break;
	
			i = underscore + 1;
		};
	}
	
	protected static void ScanHexDigitsWithUnderscore(CharSpan line, ref int i)
	{
		while (i < line.length)
		{
			char c = line[i];
			if (c >= '0' && c <= '9' || c >= 'A' && c <= 'F' || c >= 'a' && c <= 'f')
			{
				++i;
				continue;
			}
			
			var underscore = i;
			while (underscore < line.length && line[underscore] == '_')
				++underscore;
			
			if (underscore == i || underscore == line.length)
				break;
			
			c = line[underscore];
			if (c < '0' || c > '9' && c < 'A' || c > 'F' && c < 'a' || c > 'f')
				break;
	
			i = underscore + 1;
		};
	}

	protected static SyntaxToken ScanNumericLiteral(CharSpan line, ref int startAt)
	{
		bool hex = false;
		bool binary = false;
		bool point = false;
		bool exponent = false;
		var i = startAt;

		SyntaxToken token;

		char c;
		if (line[i] == '0' && i < line.length - 1 && (line[i + 1] == 'x' || line[i + 1] == 'X'))
		{
			i += 2;
			hex = true;
			if (isCSharp4)
			{
				while (i < line.length)
				{
					c = line[i];
					if (c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F')
						++i;
					else
						break;
				}
			}
			else
			{
				ScanHexDigitsWithUnderscore(line, ref i);
			}
		}
		else if (line[i] == '0' && i < line.length - 1 && (line[i + 1] == 'b' || line[i + 1] == 'B'))
		{
			i += 2;
			binary = true;
			if (isCSharp4)
			{
				while (i < line.length)
				{
					c = line[i];
					if (c == '0' || c == '1')
						++i;
					else
						break;
				}
			}
			else
			{
				ScanBinaryDigitsWithUnderscore(line, ref i);
			}
		}
		else
		{
			ScanDigitsWithUnderscore(line, ref i);
		}

		if (i > startAt && i < line.length)
		{
			c = line[i];
			if (c == 'l' || c == 'L' || c == 'u' || c == 'U')
			{
				++i;
				if (i < line.length)
				{
					if (c == 'l' || c == 'L')
					{
						if (line[i] == 'u' || line[i] == 'U')
							++i;
					}
					else if (line[i] == 'l' || line[i] == 'L')
						++i;
				}
				token = new SyntaxToken(SyntaxToken.Kind.IntegerLiteral, new CharSpan(line, startAt, i - startAt));
				startAt = i;
				return token;
			}
		}

		if (hex || binary)
		{
			token = new SyntaxToken(SyntaxToken.Kind.IntegerLiteral, new CharSpan(line, startAt, i - startAt));
			startAt = i;
			return token;
		}

		while (i < line.length)
		{
			c = line[i];

			if (!point && !exponent && c == '.')
			{
				if (i < line.length - 1 && line[i+1] >= '0' && line[i+1] <= '9')
				{
					point = true;
					++i;
					continue;
				}
				else
				{
					break;
				}
			}
			if (!exponent && i > startAt && (c == 'e' || c == 'E'))
			{
				exponent = true;
				++i;
				if (i < line.length && (line[i] == '-' || line[i] == '+'))
					++i;
				continue;
			}
			if (c == 'f' || c == 'F' || c == 'd' || c == 'D' || c == 'm' || c == 'M')
			{
				point = true;
				++i;
				break;
			}
			if (c < '0' || c > '9')
				break;
			ScanDigitsWithUnderscore(line, ref i);
		}
		token = new SyntaxToken(
			point || exponent ? SyntaxToken.Kind.RealLiteral : SyntaxToken.Kind.IntegerLiteral,
			new CharSpan(line, startAt, i - startAt));
		startAt = i;
		return token;
	}

	protected static SyntaxToken ScanNumericLiteral_JS(CharSpan line, ref int startAt)
	{
		bool hex = false;
		bool point = false;
		bool exponent = false;
		var i = startAt;

		SyntaxToken token;

		char c;
		if (line[i] == '0' && i < line.length - 1 && (line[i + 1] == 'x'))
		{
			i += 2;
			hex = true;
			while (i < line.length)
			{
				c = line[i];
				if (c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F')
					++i;
				else
					break;
			}
		}
		else
		{
			while (i < line.length && line[i] >= '0' && line[i] <= '9')
				++i;
		}

		if (i > startAt && i < line.length)
		{
			c = line[i];
			if (c == 'l' || c == 'L')
			{
				++i;
				token = new SyntaxToken(SyntaxToken.Kind.IntegerLiteral, new CharSpan(line, startAt, i - startAt));
				startAt = i;
				return token;
			}
		}

		if (hex)
		{
			token = new SyntaxToken(SyntaxToken.Kind.IntegerLiteral, new CharSpan(line, startAt, i - startAt));
			startAt = i;
			return token;
		}

		while (i < line.length)
		{
			c = line[i];

			if (!point && !exponent && c == '.')
			{
				if (i < line.length - 1 && line[i+1] >= '0' && line[i+1] <= '9')
				{
					point = true;
					++i;
					continue;
				}
				else
				{
					break;
				}
			}
			if (!exponent && i > startAt && (c == 'e' || c == 'E'))
			{
				exponent = true;
				++i;
				if (i < line.length && (line[i] == '-' || line[i] == '+'))
					++i;
				continue;
			}
			if (c == 'f' || c == 'F' || c == 'd' || c == 'D')
			{
				point = true;
				++i;
				break;
			}
			if (c < '0' || c > '9')
				break;
			++i;
		}
		token = new SyntaxToken(
			point || exponent ? SyntaxToken.Kind.RealLiteral : SyntaxToken.Kind.IntegerLiteral,
			new CharSpan(line, startAt, i - startAt));
		startAt = i;
		return token;
	}

	protected static bool ScanHexDigit(CharSpan line, ref int i)
	{
		if (i >= line.length)
			return false;
		char c = line[i];
		if (c >= '0' && c <= '9' || c >= 'A' && c <= 'F' || c >= 'a' && c <= 'f')
		{
			++i;
			return true;
		}
		return false;
	}

	protected static SyntaxToken ScanIdentifierOrKeyword(CharSpan line, ref int startAt)
	{
		bool identifier = false;
		int i = startAt;
		int lineLength = line.length;
		if (i >= lineLength)
			return null;
		
		char c = line[i];
		if (c == '@')
		{
			identifier = true;
			++i;
			if (i < lineLength)
				c = line[i];
			else
			{
				startAt = i;
				return new SyntaxToken(SyntaxToken.Kind.Punctuator, "@");
			}
		}
		if (i < lineLength)
		{
			if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || c >= 129 && char.IsLetter(c))
			{
				++i;
			}
			else if (c != '\\' || !ScanUnicodeEscapeChar(line, ref i))
			{
				if (i == startAt)
					return null;
				var partialWord = new CharSpan(line, startAt, i - startAt);
				startAt = i;
				return new SyntaxToken(SyntaxToken.Kind.Identifier, partialWord);
			}
			else
			{
				identifier = true;
			}
			
			while (i < line.length)
			{
				c = line[i];
				if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || c >= '0' && c <= '9' || c >= 128 && char.IsLetterOrDigit(c))
					++i;
				else if (c != '\\' || !ScanUnicodeEscapeChar(line, ref i))
					break;
				else
					identifier = true;
			}
		}
		
		var word = new CharSpan(line, startAt, i - startAt);
		startAt = i;
		return new SyntaxToken(identifier ? SyntaxToken.Kind.Identifier : SyntaxToken.Kind.Keyword, word);
	}
	
	protected bool ParsePPOrExpression(string line, FGTextBuffer.FormattedLine formattedLine, ref int startAt)
	{
		if (startAt >= line.Length)
		{
			//TODO: Insert missing token
			return true;
		}
		
		var lhs = ParsePPAndExpression(line, formattedLine, ref startAt);
		
		var ws = ScanWhitespace(line, ref startAt);
		if (ws != null)
		{
			tokens.Add(ws);
			ws.formattedLine = formattedLine;
		}
		
		if (startAt + 1 < line.Length && line[startAt] == '|' && line[startAt + 1] == '|')
		{
			tokens.Add(new SyntaxToken(SyntaxToken.Kind.PreprocessorArguments, "||") { formattedLine = formattedLine });
			startAt += 2;
			
			ws = ScanWhitespace(line, ref startAt);
			if (ws != null)
			{
				tokens.Add(ws);
				ws.formattedLine = formattedLine;
			}
			
			var rhs = ParsePPOrExpression(line, formattedLine, ref startAt);
			
			ws = ScanWhitespace(line, ref startAt);
			if (ws != null)
			{
				tokens.Add(ws);
				ws.formattedLine = formattedLine;
			}
			
			return lhs || rhs;
		}
		
		return lhs;
	}
	
	protected bool ParsePPAndExpression(string line, FGTextBuffer.FormattedLine formattedLine, ref int startAt)
	{
		if (startAt >= line.Length)
		{
			//TODO: Insert missing token
			return true;
		}
		
		var lhs = ParsePPEqualityExpression(line, formattedLine, ref startAt);
		
		var ws = ScanWhitespace(line, ref startAt);
		if (ws != null)
		{
			tokens.Add(ws);
			ws.formattedLine = formattedLine;
		}
		
		if (startAt + 1 < line.Length && line[startAt] == '&' && line[startAt + 1] == '&')
		{
			tokens.Add(new SyntaxToken(SyntaxToken.Kind.PreprocessorArguments, "&&") { formattedLine = formattedLine });
			startAt += 2;
			
			ws = ScanWhitespace(line, ref startAt);
			if (ws != null)
			{
				tokens.Add(ws);
				ws.formattedLine = formattedLine;
			}
			
			var rhs = ParsePPAndExpression(line, formattedLine, ref startAt);
			
			ws = ScanWhitespace(line, ref startAt);
			if (ws != null)
			{
				tokens.Add(ws);
				ws.formattedLine = formattedLine;
			}
			
			return lhs && rhs;
		}
		
		return lhs;
	}
	
	protected bool ParsePPEqualityExpression(string line, FGTextBuffer.FormattedLine formattedLine, ref int startAt)
	{
		if (startAt >= line.Length)
		{
			//TODO: Insert missing token
			return true;
		}
		
		var lhs = ParsePPUnaryExpression(line, formattedLine, ref startAt);
		
		var ws = ScanWhitespace(line, ref startAt);
		if (ws != null)
		{
			tokens.Add(ws);
			ws.formattedLine = formattedLine;
		}
		
		if (startAt + 1 < line.Length && (line[startAt] == '=' || line[startAt + 1] == '!') && line[startAt + 1] == '=')
		{
			var equality = line[startAt] == '=';
			tokens.Add(new SyntaxToken(SyntaxToken.Kind.PreprocessorArguments, equality ? "==" : "!=") { formattedLine = formattedLine });
			startAt += 2;
			
			ws = ScanWhitespace(line, ref startAt);
			if (ws != null)
			{
				tokens.Add(ws);
				ws.formattedLine = formattedLine;
			}
			
			var rhs = ParsePPEqualityExpression(line, formattedLine, ref startAt);
			
			ws = ScanWhitespace(line, ref startAt);
			if (ws != null)
			{
				tokens.Add(ws);
				ws.formattedLine = formattedLine;
			}
			
			return equality ? lhs == rhs : lhs != rhs;
		}
		
		return lhs;
	}
	
	protected bool ParsePPUnaryExpression(string line, FGTextBuffer.FormattedLine formattedLine, ref int startAt)
	{
		if (startAt >= line.Length)
		{
			//TODO: Insert missing token
			return true;
		}
		
		if (line[startAt] == '!')
		{
			tokens.Add(new SyntaxToken(SyntaxToken.Kind.PreprocessorArguments, "!") { formattedLine = formattedLine });
			++startAt;
			
			var ws = ScanWhitespace(line, ref startAt);
			if (ws != null)
			{
				tokens.Add(ws);
				ws.formattedLine = formattedLine;
			}
			
			var result = ParsePPUnaryExpression(line, formattedLine, ref startAt);
			
			ws = ScanWhitespace(line, ref startAt);
			if (ws != null)
			{
				tokens.Add(ws);
				ws.formattedLine = formattedLine;
			}
			
			return !result;
		}
		
		return ParsePPPrimaryExpression(line, formattedLine, ref startAt);
	}
	
	protected bool ParsePPPrimaryExpression(string line, FGTextBuffer.FormattedLine formattedLine, ref int startAt)
	{
		if (line[startAt] == '(')
		{
			tokens.Add(new SyntaxToken(SyntaxToken.Kind.PreprocessorArguments, "(") { formattedLine = formattedLine });
			++startAt;
			
			var ws = ScanWhitespace(line, ref startAt);
			if (ws != null)
			{
				tokens.Add(ws);
				ws.formattedLine = formattedLine;
			}
			
			var result = ParsePPOrExpression(line, formattedLine, ref startAt);
			
			if (startAt >= line.Length)
			{
				//TODO: Insert missing token
				return result;
			}
			
			if (line[startAt] == ')')
			{
				tokens.Add(new SyntaxToken(SyntaxToken.Kind.PreprocessorArguments, ")") { formattedLine = formattedLine });
				++startAt;
				
				ws = ScanWhitespace(line, ref startAt);
				if (ws != null)
				{
					tokens.Add(ws);
					ws.formattedLine = formattedLine;
				}
				
				return result;
			}
			
			//TODO: Insert missing token
			return result;
		}
		
		var symbolResult = ParsePPSymbol(line, formattedLine, ref startAt);
		
		var ws2 = ScanWhitespace(line, ref startAt);
		if (ws2 != null)
		{
			tokens.Add(ws2);
			ws2.formattedLine = formattedLine;
		}
		
		return symbolResult;
	}
	
	protected bool ParsePPSymbol(string line, FGTextBuffer.FormattedLine formattedLine, ref int startAt)
	{
		var word = FGParser.ScanIdentifierOrKeyword(line, ref startAt);
		if (word == null)
			return true;
		
		word.tokenKind = SyntaxToken.Kind.PreprocessorSymbol;
		tokens.Add(word);
		word.formattedLine = formattedLine;
		
		if (word.text == "true")
		{
			return true;
		}
		if (word.text == "false")
		{
			return false;
		}
		
		if (scriptDefines == null)
			scriptDefines = new HashSet<string>(UnityEditor.EditorUserBuildSettings.activeScriptCompilationDefines);
		
		var isDefined = scriptDefines.Contains(word.text);
		return isDefined;
	}
	
	protected void OpenRegion(FGTextBuffer.FormattedLine formattedLine, FGTextBuffer.RegionTree.Kind regionKind)
	{
		var parentRegion = formattedLine.regionTree;
		FGTextBuffer.RegionTree reuseRegion = null;
		
		switch (regionKind)
		{
		case FGTextBuffer.RegionTree.Kind.Else:
		case FGTextBuffer.RegionTree.Kind.Elif:
		case FGTextBuffer.RegionTree.Kind.InactiveElse:
		case FGTextBuffer.RegionTree.Kind.InactiveElif:
			parentRegion = parentRegion.parent;
			break;
		}
		
		if (parentRegion.children != null)
		{
			for (var i = parentRegion.children.Count; i-- > 0;)
			{
				if (parentRegion.children[i].line == formattedLine)
				{
					reuseRegion = parentRegion.children[i];
					break;
				}
			}
		}
		if (reuseRegion != null)
		{
			//if (reuseRegion.kind == regionKind)
			//{
			//	formattedLine.regionTree = reuseRegion;
			//	return;
			//}
			
			reuseRegion.parent = null;
			parentRegion.children.Remove(reuseRegion);
		}
		
		formattedLine.regionTree = new FGTextBuffer.RegionTree {
			parent = parentRegion,
			kind = regionKind,
			line = formattedLine,
		};
		
		if (parentRegion.children == null)
			parentRegion.children = new List<FGTextBuffer.RegionTree>();
		parentRegion.children.Add(formattedLine.regionTree);
	}
	
	protected void CloseRegion(FGTextBuffer.FormattedLine formattedLine)
	{
		var regionTree = formattedLine.regionTree;
		formattedLine.regionTree = regionTree.parent;
		
		if (regionTree.kind != FGTextBuffer.RegionTree.Kind.Region)
			return;
		
		var lineIndex = regionTree.line.index;
		var from = textBuffer.FirstNonWhitespacePos(lineIndex, 0);

		FGTextBuffer.CollapsibleTextSpan oldSpan = null;
		var oldSpanIndex = textBuffer.IndexOfFirstCollapsibleTextSpanAtLine(lineIndex);
		if (oldSpanIndex >= 0)
		{
			oldSpan = textBuffer.collapsibleTextSpans[oldSpanIndex];
		}

		var to = new TextPosition{line = formattedLine.index, index = textBuffer.lines[formattedLine.index].Length};
		
		var lineText = textBuffer.lines[regionTree.line.index];
		var indexOfRegionText = lineText.IndexOf("region", StringComparison.Ordinal);
		var regionName = string.Concat(" ", lineText.Substring(indexOfRegionText + 6).Trim(), " ");

		if (oldSpan != null)
		{
			if (from != oldSpan.span.Start || to != oldSpan.span.End)
			{
				textBuffer.RemoveCollapsibleTextSpan(oldSpan);
				if (oldSpan.IsCollapsed)
					textBuffer.NotifyAllEditorsOnUnfold(oldSpan);
			}
			else
			{
				oldSpan.owner = regionTree;
				oldSpan.text = regionName;
				return;
			}
		}

		var span = new FGTextBuffer.CollapsibleTextSpan(from, to);
		span.owner = regionTree;
		span.text = regionName;
		
		var addedSpan = textBuffer.AddCollapsibleTextSpan(span);

		if (addedSpan == span)
		{
			if (textBuffer.IsLoading && SISettings.autoFoldRegions)
			{
				addedSpan.IsCollapsed = true;
				textBuffer.NotifyAllEditorsOnFold(addedSpan);
			}
		}
		else
		{
			addedSpan.owner = regionTree;
			addedSpan.text = regionName;
		}
	}
}

internal class TextParser : FGParser
{
	public override bool IsBuiltInLiteral(CharSpan word)
	{
		return false;
	}
}

}
