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
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

public class BooParser : FGParser
{
	public override HashIDSet Keywords { get { return keywords; } }
	public override HashIDSet BuiltInLiterals { get { return scriptLiterals; } }

	public override bool IsBuiltInType(CharSpan word)
	{
		return builtInTypes.Contains(word.GetHashCode());
	}
	
	public override bool IsBuiltInLiteral(CharSpan word)
	{
		return scriptLiterals.Contains(word.GetHashCode());
	}
	
	private static readonly HashIDSet keywords = new HashIDSet {
		"abstract", "and", "as", "break", "callable", "cast", "class", "const", "constructor", "destructor", "continue",
		"def", "do", "elif", "else", "enum", "ensure", "event", "except", "final", "for", "from", "given", "get", "goto",
		"if", "interface", "in", "include", "import", "is", "isa", "mixin", "namespace", "not", "or", "otherwise",
		"override", "pass", "raise", "retry", "self", "struct", "return", "set", "success", "try", "transient", "virtual",
		"while", "when", "unless", "yield",
		"public", "protected", "private", "internal", "static",
		
		// builtin
		"len", "__addressof__", "__eval__", "__switch__", "array", "matrix", "typeof", "assert", "print", "gets", "prompt",
		"enumerate", "zip", "filter", "map",
	};

	private static readonly HashIDSet operators = new HashIDSet{
		"++", "--", "->", "+", "-", "!", "~", "++", "--", "&", "*", "/", "%", "+", "-", "<<", ">>", "<", ">",
		"<=", ">=", "==", "!=", "&", "^", "|", "&&", "||", "??", "?", "::", ":",
		"=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", "=>"
	};

	private static readonly HashIDSet builtInTypes = new HashIDSet {
		"bool", "byte", "char", "date", "decimal", "double", "int", "long", "object", "sbyte", "short", "single", "string",
		"timespan", "uint", "ulong", "ushort", "void"
	};

	public override void LexLine(int currentLine, FGTextBuffer.FormattedLine formattedLine)
	{
		formattedLine.index = currentLine;

		if (parserThread != null)
			parserThread.Join();
		parserThread = null;

		string textLine = textBuffer.lines[currentLine];

		//Stopwatch sw1 = new Stopwatch();
		//Stopwatch sw2 = new Stopwatch();
		
		if (currentLine == 0)
		{
			var defaultScriptDefines = UnityEditor.EditorUserBuildSettings.activeScriptCompilationDefines;
			if (scriptDefines == null || !scriptDefines.SetEquals(defaultScriptDefines))
			{
				if (scriptDefines == null)
				{
					scriptDefines = new HashSet<string>(defaultScriptDefines);
				}
				else
				{
					scriptDefines.Clear();
					scriptDefines.UnionWith(defaultScriptDefines);
				}
			}
		}
		
		//sw2.Start();
		Tokenize(textLine, formattedLine);

		var lineTokens = formattedLine.tokens;

		if (textLine.Length == 0)
		{
			formattedLine.tokens.Clear();
		}
		else if (textBuffer.styles != null)
		{
			//var lineWidth = textBuffer.CharIndexToColumn(textLine.Length, currentLine);
			//if (lineWidth > textBuffer.longestLine)
			//	textBuffer.longestLine = lineWidth;

			for (var i = 0; i < lineTokens.Count; ++i)
			{
				var token = lineTokens[i];
				switch (token.tokenKind)
				{
					case SyntaxToken.Kind.Whitespace:
					case SyntaxToken.Kind.Missing:
						token.style = FGTextEditor.StyleID.Normal;
						break;

					case SyntaxToken.Kind.Punctuator:
						token.style = IsOperator(token.text) ? FGTextEditor.StyleID.Operator : FGTextEditor.StyleID.Punctuator;
						break;

					case SyntaxToken.Kind.Keyword:
						if (IsBuiltInType(token.text))
						{
							if (token.text == "string" || token.text == "object")
								token.style = FGTextEditor.StyleID.BuiltInRefType;
							else
								token.style = FGTextEditor.StyleID.BuiltInValueType;
						}
						else
						{
							token.style = FGTextEditor.StyleID.Keyword;
						}
						break;

					case SyntaxToken.Kind.Identifier:
						if (IsBuiltInLiteral(token.text))
						{
							token.style = FGTextEditor.StyleID.BuiltInLiteral;
							token.tokenKind = SyntaxToken.Kind.BuiltInLiteral;
						}
						//else if (IsUnityType(token.text))
						//{
						//	token.style = textBuffer.styles.referenceTypeStyle;
						//}
						else
						{
							token.style = FGTextEditor.StyleID.Normal;
						}
						break;

					case SyntaxToken.Kind.IntegerLiteral:
					case SyntaxToken.Kind.RealLiteral:
						token.style = FGTextEditor.StyleID.Constant;
						break;

					case SyntaxToken.Kind.Comment:
						token.style = FGTextEditor.StyleID.Comment;
						break;

					case SyntaxToken.Kind.CharLiteral:
					case SyntaxToken.Kind.StringLiteral:
					case SyntaxToken.Kind.VerbatimStringBegin:
					case SyntaxToken.Kind.VerbatimStringLiteral:
						token.style = FGTextEditor.StyleID.String;
						break;
				}
				lineTokens[i] = token;
			}
		}
	}
	
	protected void Tokenize(CharSpan line, FGTextBuffer.FormattedLine formattedLine)
	{
		var tokens = new List<SyntaxToken>();
		formattedLine.tokens = tokens;

		int startAt = 0;
		int length = line.length;
		SyntaxToken token;

		SyntaxToken ws = ScanWhitespace(line, ref startAt);
		if (ws != null)
		{
			tokens.Add(ws);
			ws.formattedLine = formattedLine;
		}

		var inactiveLine = formattedLine.regionTree.kind > FGTextBuffer.RegionTree.Kind.LastActive;
		
		while (startAt < length)
		{
			switch (formattedLine.blockState)
			{
				case FGTextBuffer.BlockState.None:
					ws = ScanWhitespace(line, ref startAt);
					if (ws != null)
					{
						tokens.Add(ws);
						ws.formattedLine = formattedLine;
						continue;
					}
					
					if (inactiveLine)
					{
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, new CharSpan(line, startAt)) { formattedLine = formattedLine });
						startAt = length;
						break;
					}

					if (line[startAt] == '#')
					{
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, "#") { formattedLine = formattedLine });
						++startAt;
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, new CharSpan(line, startAt)) { formattedLine = formattedLine });
						startAt = length;
						break;
					}
					
					if (line[startAt] == '/' && startAt < length - 1)
					{
						if (line[startAt + 1] == '/')
						{
							tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, "//") { formattedLine = formattedLine });
							startAt += 2;
							tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, new CharSpan(line, startAt)) { formattedLine = formattedLine });
							startAt = length;
							break;
						}
						else if (line[startAt + 1] == '*')
						{
							tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, "/*") { formattedLine = formattedLine });
							startAt += 2;
							formattedLine.blockState = FGTextBuffer.BlockState.CommentBlock;
							break;
						}
					}

					if (line[startAt] == '\'')
					{
						token = ScanCharLiteral(line, ref startAt);
						tokens.Add(token);
						token.formattedLine = formattedLine;
						break;
					}

					if (line[startAt] == '\"')
					{
						if (startAt < length - 2 && line[startAt + 1] == '\"' && line[startAt + 2] == '\"')
						{
							token = new SyntaxToken(SyntaxToken.Kind.VerbatimStringBegin, new CharSpan(line, startAt, 3)) { formattedLine = formattedLine };
							tokens.Add(token);
							startAt += 3;
							formattedLine.blockState = FGTextBuffer.BlockState.StringBlock;
							break;
						}
	
						token = ScanStringLiteral(line, ref startAt);
						tokens.Add(token);
						token.formattedLine = formattedLine;
						break;
					}

					if (line[startAt] >= '0' && line[startAt] <= '9'
					    || startAt < length - 1 && line[startAt] == '.' && line[startAt + 1] >= '0' && line[startAt + 1] <= '9')
					{
						token = ScanNumericLiteral_JS(line, ref startAt);
						tokens.Add(token);
						token.formattedLine = formattedLine;
						break;
					}

					token = ScanIdentifierOrKeyword(line, ref startAt);
					if (token != null)
					{
						tokens.Add(token);
						token.formattedLine = formattedLine;
						break;
					}

					// Multi-character operators / punctuators
					// "++", "--", "<<", ">>", "<=", ">=", "==", "!=", "&&", "||", "??", "+=", "-=", "*=", "/=", "%=",
					// "&=", "|=", "^=", "<<=", ">>=", "=>", "::", "->", "^="
					var punctuatorStart = startAt++;
					if (startAt < line.length)
					{
						switch (line[punctuatorStart])
						{
							case '?':
								if (line[startAt] == '?')
									++startAt;
								break;
							case '+':
								if (line[startAt] == '+' || line[startAt] == '=' || line[startAt] == '>')
									++startAt;
								break;
							case '-':
								if (line[startAt] == '-' || line[startAt] == '=')
									++startAt;
								break;
							case '<':
								if (line[startAt] == '=')
									++startAt;
								else if (line[startAt] == '<')
								{
									++startAt;
									if (startAt < line.length && line[startAt] == '=')
										++startAt;
								}
								break;
							case '>':
								if (line[startAt] == '=')
									++startAt;
								else if (startAt < line.length && line[startAt] == '>')
								{
									++startAt;
									if (line[startAt] == '=')
										++startAt;
								}
								break;
							case '=':
								if (line[startAt] == '=' || line[startAt] == '>')
									++startAt;
								break;
							case '&':
								if (line[startAt] == '=' || line[startAt] == '&')
									++startAt;
								break;
							case '|':
								if (line[startAt] == '=' || line[startAt] == '|')
									++startAt;
								break;
							case '*':
							case '/':
							case '%':
							case '^':
							case '!':
								if (line[startAt] == '=')
									++startAt;
								break;
							case ':':
								if (line[startAt] == ':')
									++startAt;
								break;
						}
					}
					tokens.Add(new SyntaxToken(SyntaxToken.Kind.Punctuator, new CharSpan(line, punctuatorStart, startAt - punctuatorStart)) { formattedLine = formattedLine });
					break;

				case FGTextBuffer.BlockState.CommentBlock:
					int commentBlockEnd = line.IndexOf("*/", startAt);
					if (commentBlockEnd == -1)
					{
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, new CharSpan(line, startAt)) { formattedLine = formattedLine });
						startAt = length;
					}
					else
					{
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, new CharSpan(line, startAt, commentBlockEnd + 2 - startAt)) { formattedLine = formattedLine });
						startAt = commentBlockEnd + 2;
						formattedLine.blockState = FGTextBuffer.BlockState.None;
					}
					break;

				case FGTextBuffer.BlockState.StringBlock:
					int i = startAt;
					int closingQuote = line.IndexOf('\"', startAt);
					while (closingQuote != -1 && closingQuote < length - 2 &&
						(line[closingQuote + 1] != '\"' || line[closingQuote + 2] != '\"'))
					{
						i = closingQuote + 2;
						closingQuote = line.IndexOf('\"', i);
					}
					if (closingQuote == -1)
					{
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.VerbatimStringLiteral, new CharSpan(line, startAt)) { formattedLine = formattedLine });
						startAt = length;
					}
					else
					{
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.VerbatimStringLiteral, new CharSpan(line, startAt, closingQuote - startAt)) { formattedLine = formattedLine });
						startAt = closingQuote;
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.VerbatimStringLiteral, new CharSpan(line, startAt, 3)) { formattedLine = formattedLine });
						startAt += 3;
						formattedLine.blockState = FGTextBuffer.BlockState.None;
					}
					break;
			}
		}
	}

	private new SyntaxToken ScanIdentifierOrKeyword(CharSpan line, ref int startAt)
	{
		bool identifier = false;
		int i = startAt;
		if (i >= line.length)
			return null;
		
		char c = line[i];

		if (!char.IsLetter(c) && c != '_')
			return null;
		++i;

		while (i < line.length)
		{
			var ch = line[i];
			if (char.IsLetterOrDigit(ch) || ch == '_')
				++i;
			else
				break;
		}
		
		var word = new CharSpan(line, startAt, i - startAt);
		startAt = i;
		var token = new SyntaxToken(identifier ? SyntaxToken.Kind.Identifier : SyntaxToken.Kind.Keyword, word);
		
		if (token.tokenKind == SyntaxToken.Kind.Keyword && !IsKeyword(token.text) && !IsBuiltInType(token.text))
			token.tokenKind = SyntaxToken.Kind.Identifier;
		return token;
	}

	private bool IsKeyword(CharSpan word)
	{
		return keywords.Contains(word.GetHashCode());
	}

	private bool IsOperator(CharSpan text)
	{
		return operators.Contains(text.GetHashCode());
	}
}

}
