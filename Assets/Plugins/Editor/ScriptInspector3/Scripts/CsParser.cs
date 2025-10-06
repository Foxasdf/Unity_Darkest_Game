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

public class CsParser : FGParser
{
	public override HashIDSet Keywords { get { return keywords; } }
	public override HashIDSet BuiltInLiterals { get { return scriptLiterals; } }

	public override bool IsBuiltInType(CharSpan word)
	{
		return builtInTypes.Contains(word.GetHashCode());
	}

	public override bool IsBuiltInLiteral(CharSpan word)
	{
		return word == "true" || word == "false" || word == "null";
	}

	private static readonly HashIDSet keywords = new HashIDSet {
		"abstract",  "as",        "base",     "break",    "case",   "catch",    "checked",   "class",      "const",    "continue",
		"default",   "delegate",  "do",       "else",     "enum",   "event",    "explicit",  "extern",     "finally",
		"fixed",     "for",       "foreach",  "goto",     "if",     "implicit", "in",        "interface",  "internal", "is",
		"lock",      "namespace", "new",      "operator", "out",    "override", "params",    "private",
		"protected", "public",    "readonly", "ref",      "return", "sealed",   "sizeof",    "stackalloc", "static",
		"struct",    "switch",    "this",     "throw",    "try",    "typeof",   "unchecked", "unsafe",     "using",    "virtual",
		"volatile",  "while"
	};

	private static readonly HashIDSet controlKeywords = new HashIDSet {
		"break", "case", "catch", "continue",
		"do", "else", "finally",
		"for", "foreach", "goto", "if",
		"return", "yield", "switch", "when", "throw", "try", "while"
	};

	//private static readonly string[] csPunctsAndOps = {
	//	"{", "}", ";", "#", ".", "(", ")", "[", "]", "++", "--", "->", "+", "-",
	//	"!", "~", "++", "--", "&", "*", "/", "%", "+", "-", "<<", ">>", "<", ">",
	//	"<=", ">=", "==", "!=", "&", "^", "|", "&&", "||", "??", "?", "::", ":",
	//	"=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", "=>"
	//};

	//private static readonly HashIDSet csOperators = new HashIDSet{
	//	"=", "+", "-", "!", "~", "&", "|", "^", "*", "/", "%", "<", ">", "?", ":",
	//	"++", "--", "->", "&&", "||", "??", "::", "=>", "<<", ">>",
	//	"!=", "==", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<=", ">=",
	//	"<<=", ">>=",
	//	"..",
	//};

	private static readonly HashIDSet preprocessorKeywords = new HashIDSet{
		"define", "elif", "else", "endif", "endregion", "error", "if", "line", "pragma", "region", "undef", "warning"
	};

	private static readonly HashIDSet builtInTypes = new HashIDSet() {
		"bool", "byte", "char", "decimal", "double", "float", "int", "long", "object", "sbyte", "short",
		"string", "uint", "ulong", "ushort", "void"
	};
	
	private static readonly HashIDSet keywordsAndBuiltInTypes;

	//static void DisplaySizeOf<T>() where T : unmanaged
	//{
	//	unsafe
	//	{
	//		Debug.Log("Size of " + typeof(T) + " is " + sizeof(T));
	//	}
	//}

	static CsParser()
	{
		keywordsAndBuiltInTypes = new HashIDSet(keywords.Count + builtInTypes.Count);
		foreach (var item in keywords)
			keywordsAndBuiltInTypes.Add(item.Value);
		foreach (var item in builtInTypes)
			keywordsAndBuiltInTypes.Add(item.Value);
		
		//var all = new HashSet<string>(csKeywords);
		//all.UnionWith(csTypes);
		//all.UnionWith(csPunctsAndOps);
		//all.UnionWith(scriptLiterals);
		//tokenLiterals = new string[all.Count];
		//all.CopyTo(tokenLiterals);
		//Array.Sort<string>(tokenLiterals);
	}

	protected override void ParseAll(string bufferName)
	{
		var scanner = CsGrammar.Scanner.New(CsGrammar.Instance, textBuffer, bufferName);
		parseTree = CsGrammar.Instance.ParseAll(scanner);
		scanner.Delete();
	}

	public override FGGrammar.IScanner MoveAfterLeaf(ParseTree.Leaf leaf)
	{
		var scanner = CsGrammar.Scanner.New(CsGrammar.Instance, textBuffer, assetPath);
		var result = leaf == null ? scanner : scanner.MoveAfterLeaf(leaf) ? scanner : null;
		if (result == null)
			scanner.Delete();
		return result;
	}

	public override bool ParseLines(int fromLine, int toLineInclusive)
	{
		var formattedLines = textBuffer.formattedLines;

		for (var line = Math.Max(0, fromLine); line <= toLineInclusive; ++line)
		{
			var tokens = formattedLines[line].tokens;
			var tokensCount = tokens.Count;
			for (var i = 0; i < tokensCount; ++i )
			{
				var t = tokens[i];
				if (t.tokenKind == SyntaxToken.Kind.Missing)
				{
					if (t.parent != null && t.parent.parent != null)
						t.parent.parent.syntaxError = null;
					
					var removed = 1;
					while (++i < tokensCount)
					{
						t = tokens[i];
						
						if (t.tokenKind == SyntaxToken.Kind.Missing)
						{
							if (t.parent != null && t.parent.parent != null)
								t.parent.parent.syntaxError = null;
							++removed;
						}
						else
						{
							tokens[i - removed] = t;
						}
					}
					
					tokens.RemoveRange(tokensCount - removed, removed);
					break;
				}
			}
		}

		var scanner = CsGrammar.Scanner.New(CsGrammar.Instance, textBuffer, assetPath);
		//CsGrammar.Instance.ParseAll(scanner);
		scanner.MoveToLine(fromLine, parseTree);
//        if (scanner.CurrentGrammarNode == null)
//        {
//            if (!scanner.MoveNext())
//                return false;
			
//            FGGrammar.Rule startRule = CsGrammar.Instance.r_compilationUnit;

////			if (parseTree == null)
////			{
////				parseTree = new ParseTree();
////				var rootId = new Id(startRule.GetNt());
////				ids[Start.GetNt()] = startRule;
////			rootId.SetLookahead(this);
////			Start.parent = rootId;
//                scanner.CurrentParseTreeNode = parseTree.root;// = new ParseTree.Node(rootId);
//                scanner.CurrentGrammarNode = startRule;//.Parse(scanner);
			
//                scanner.ErrorParseTreeNode = scanner.CurrentParseTreeNode;
//                scanner.ErrorGrammarNode = scanner.CurrentGrammarNode;
//            //}
//        }

		//Debug.Log("Parsing line " + (fromLine + 1) + " starting from " + (scanner.CurrentLine() + 1) + ", token " + scanner.CurrentTokenIndex() + " currentToken " + scanner.Current);
		
		var oldScanner = FGGrammar.Node.scanner;

		var grammar = CsGrammar.Instance;
		var canContinue = true;
		for (var line = Math.Max(0, scanner.CurrentLine()); canContinue && line <= toLineInclusive; line = scanner.CurrentLine())
			canContinue = grammar.ParseLine(scanner, line);
			//if (!(canContinue = grammar.ParseLine(scanner, line)))
			//	if (scanner.Current.tokenKind != SyntaxToken.Kind.EOF)
			//		Debug.Log("can't continue at line " + (line + 1) + " token " + scanner.Current);

		if (canContinue && toLineInclusive == formattedLines.Count - 1)
			canContinue = grammar.GetParser.ParseStep(scanner);
		
		FGGrammar.Node.scanner = oldScanner;
		
		scanner.Delete();

		//Debug.Log("canContinue == " + canContinue);

		//for (var line = fromLine; line <= toLineInclusive; ++line)
		//{
		//	var tokens = formattedLines[line].tokens;
		//	var numTokens = tokens.Count;
		//	for (var t = 0; t < numTokens; ++t)
		//	{
		//		var token = tokens[t];
		//		if (token.tokenKind == SyntaxToken.Kind.ContextualKeyword && token.text == "value")
		//			token.style = FGTextEditor.StyleID.Parameter;
		//	}
		//}

		return canContinue;
		//return true;
	}
	
	public override void FullRefresh()
	{
		base.FullRefresh();
		
		parserThread = new System.Threading.Thread(() =>
		{
			this.OnLoaded();
			this.parserThread = null;
		});
		parserThread.Start();
	}
	
	private static string[] rspFileNames = { "Assets/mcs.rsp", "Assets/smcs.rsp", "Assets/gmcs.rsp", "Assets/csc.rsp" };
	private static char[] optionsSeparators = { ' ', '\n', '\r' };
	private static char[] definesSeparators = { ';', ',' };
	private static HashSet<string> activeScriptCompilationDefines;
	protected static HashSet<string> GetActiveScriptCompilationDefines()
	{
		if (activeScriptCompilationDefines != null)
			return activeScriptCompilationDefines;
		
		activeScriptCompilationDefines = new HashSet<string>(UnityEditor.EditorUserBuildSettings.activeScriptCompilationDefines);
		string rspText = null;
		try
		{
			string rspFileName = null;
			for (var i = 0; i < rspFileNames.Length; ++i )
			{
				if (System.IO.File.Exists(rspFileNames[i]))
				{
					rspFileName = rspFileNames[i];
					break;
				}
			}
		
			if (rspFileName != null)
			{
				rspText = System.IO.File.ReadAllText(rspFileName);
			}
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
		
		if (rspText != null)
		{
			var options = rspText.Split(optionsSeparators, StringSplitOptions.RemoveEmptyEntries);
			for (var i = options.Length; i --> 0; )
			{
				var option = options[i];
				if (option.StartsWithIgnoreCase("-define:") || option.StartsWithIgnoreCase("/define:"))
				{
					option = option.Substring("-define:".Length);
				}
				else if (option.StartsWithIgnoreCase("-d:") || option.StartsWithIgnoreCase("/d:"))
				{
					option = option.Substring("-d:".Length);
				}
				else
				{
					continue;
				}
					
				var defineOptions = option.Split(definesSeparators, StringSplitOptions.RemoveEmptyEntries);
				for (var j = defineOptions.Length; j --> 0; )
					activeScriptCompilationDefines.Add(defineOptions[j]);
			}
		}
		
		return activeScriptCompilationDefines;
	}

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
			var defaultScriptDefines = GetActiveScriptCompilationDefines();
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
		if (formattedLine.tokens != null)
			formattedLine.tokens.Clear();
		CsParser.tokens.Clear();
		
		Tokenize(textLine, formattedLine);
		
//		syntaxTree.SetLineTokens(currentLine, lineTokens);
		var lineTokens = formattedLine.tokens;
		if (lineTokens == null)
		{
			lineTokens = new List<SyntaxToken>(CsParser.tokens.Count);
			formattedLine.tokens = lineTokens;
		}

		//if (lineTokens.Capacity < CsParser.tokens.Count)
		//	lineTokens.Capacity = CsParser.tokens.Count;
		lineTokens.AddRange(CsParser.tokens);
		
		CsParser.tokens.Clear();

		if (textLine.Length == 0)
		{
			//formattedLine.tokens.Clear();
		}
		//else if (textBuffer.styles != null)
		//{
		//	// TODO: Remove this!
		//	var lineWidth = textBuffer.CharIndexToColumn(textLine.Length, currentLine);
		//	if (lineWidth > textBuffer.longestLine)
		//		textBuffer.longestLine = lineWidth;
		//}
	}
	
	public override void SetTokenStyleID(SyntaxToken token)
	{
		switch (token.tokenKind)
		{
		case SyntaxToken.Kind.Whitespace:
		case SyntaxToken.Kind.Missing:
			token.style = FGTextEditor.StyleID.Normal;
			break;

		case SyntaxToken.Kind.Punctuator:
			token.style = IsOperator(token.text) ? FGTextEditor.StyleID.Operator : FGTextEditor.StyleID.Punctuator;
			break;

		case SyntaxToken.Kind.ContextualKeyword:
			token.style = FGTextEditor.StyleID.Keyword;
			break;
		
		case SyntaxToken.Kind.Keyword:
			if (IsBuiltInType(token.text))
			{
				if (token.text == "string" || token.text == "object")
					token.style = FGTextEditor.StyleID.BuiltInRefType;
				else
					token.style = FGTextEditor.StyleID.BuiltInValueType;
			}
			else if (IsControlKeyword(token.text))
			{
				token.style = FGTextEditor.StyleID.ControlKeyword;
			}
			else
			{
				token.style = FGTextEditor.StyleID.Keyword;
			}
			break;

		case SyntaxToken.Kind.Identifier:
			//if (token.text == "true" || token.text == "false" || token.text == "null")
			//{
			//	token.style = FGTextEditor.StyleID.BuiltInLiteral;
			//	token.tokenKind = SyntaxToken.Kind.BuiltInLiteral;
			//}
			//else
			//{
				token.style = FGTextEditor.StyleID.Normal;
			//}
			break;

		case SyntaxToken.Kind.IntegerLiteral:
		case SyntaxToken.Kind.RealLiteral:
			token.style = FGTextEditor.StyleID.Constant;
			break;
			
		case SyntaxToken.Kind.BuiltInLiteral:
			token.style = FGTextEditor.StyleID.BuiltInLiteral;
			break;

		case SyntaxToken.Kind.Comment:
			var regionKind = token.formattedLine.regionTree.kind;
			var inactiveLine = regionKind > FGTextBuffer.RegionTree.Kind.LastActive;
			token.style = inactiveLine ? FGTextEditor.StyleID.InactiveCode : FGTextEditor.StyleID.Comment;
			break;

		case SyntaxToken.Kind.Preprocessor:
			token.style = FGTextEditor.StyleID.Preprocessor;
			break;

		case SyntaxToken.Kind.PreprocessorSymbol:
			token.style = FGTextEditor.StyleID.DefineSymbol;
			break;

		case SyntaxToken.Kind.PreprocessorArguments:
		case SyntaxToken.Kind.PreprocessorCommentExpected:
		case SyntaxToken.Kind.PreprocessorDirectiveExpected:
		case SyntaxToken.Kind.PreprocessorUnexpectedDirective:
			token.style = FGTextEditor.StyleID.Normal;
			break;

		case SyntaxToken.Kind.CharLiteral:
		case SyntaxToken.Kind.StringLiteral:
		case SyntaxToken.Kind.VerbatimStringBegin:
		case SyntaxToken.Kind.VerbatimStringLiteral:
		case SyntaxToken.Kind.InterpolatedStringWholeLiteral:
		case SyntaxToken.Kind.InterpolatedStringStartLiteral:
		case SyntaxToken.Kind.InterpolatedStringMidLiteral:
		case SyntaxToken.Kind.InterpolatedStringEndLiteral:
		case SyntaxToken.Kind.InterpolatedStringFormatLiteral:
			token.style = FGTextEditor.StyleID.String;
			break;
		
		case SyntaxToken.Kind.Collapsed:
			token.style = FGTextEditor.StyleID.FoldedText;
			break;
		
		default:
			Debug.LogError("Unhandled token kind " + token.tokenKind + " while setting token style!");
			token.style = FGTextEditor.StyleID.Normal;
			break;
		}
	}

	protected bool IsControlKeyword(CharSpan word)
	{
		return controlKeywords.Contains(word.GetHashCode());
	}
	
	protected void Tokenize(CharSpan line, FGTextBuffer.FormattedLine formattedLine)
	{
		int startAt = 0;
		int length = line.length;
		SyntaxToken token, ws;

		if (formattedLine.blockState == FGTextBuffer.BlockState.None || formattedLine.blockState == FGTextBuffer.BlockState.CommentBlock)
		{
			ws = ScanWhitespace(line, ref startAt);
			if (ws != null)
			{
				tokens.Add(ws);
				ws.formattedLine = formattedLine;
			}
		}

		if (formattedLine.blockState == FGTextBuffer.BlockState.None && startAt < length && line[startAt] == '#')
		{
			tokens.Add(new SyntaxToken(SyntaxToken.Kind.Preprocessor, "#") { formattedLine = formattedLine });
			++startAt;

			ws = ScanWhitespace(line, ref startAt);
			if (ws != null)
			{
				tokens.Add(ws);
				ws.formattedLine = formattedLine;
			}

			var error = false;
			var commentsOnly = false;
			var preprocessorCommentsAllowed = true;
			
			token = ScanWord(line, ref startAt);
			if (!preprocessorKeywords.Contains(token.text.GetHashCode()))
			{
				token.tokenKind = SyntaxToken.Kind.PreprocessorDirectiveExpected;
				tokens.Add(token);
				token.formattedLine = formattedLine;
				
				error = true;
			}
			else
			{
				token.tokenKind = SyntaxToken.Kind.Preprocessor;
				tokens.Add(token);
				token.formattedLine = formattedLine;
	
				ws = ScanWhitespace(line, ref startAt);
				if (ws != null)
				{
					tokens.Add(ws);
					ws.formattedLine = formattedLine;
				}

				if (token.text == "if")
				{
					bool active = ParsePPOrExpression(line, formattedLine, ref startAt);
					bool inactiveParent = formattedLine.regionTree.kind > FGTextBuffer.RegionTree.Kind.LastActive;
					if (active && !inactiveParent)
					{
						OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.If);
						commentsOnly = true;
					}
					else
					{
						OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.InactiveIf);
						commentsOnly = true;
					}
				}
				else if (token.text == "elif")
				{
					if (formattedLine.regionTree.parent == null)
					{
						token.tokenKind = SyntaxToken.Kind.PreprocessorUnexpectedDirective;
					}
					else
					{
						bool active = ParsePPOrExpression(line, formattedLine, ref startAt);
						bool inactiveParent = formattedLine.regionTree.parent.kind > FGTextBuffer.RegionTree.Kind.LastActive;
						bool setActive = active && !inactiveParent;
						if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.If)
						{
							setActive = false;
						}
						else if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.Elif ||
							formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveElif)
						{
							if (setActive && formattedLine.regionTree.parent != null)
							{
								var lineIndex = formattedLine.index;
								var ifIndex = -1;
								var activeIndex = -1;
								var siblings = formattedLine.regionTree.parent.children;
								for (var i = siblings.Count; i --> 0; )
								{
									var siblingLine = siblings[i].line;
									var siblingIndex = siblingLine.index;
									if (siblingIndex < lineIndex)
									{
										if (siblingIndex > activeIndex &&
											siblingLine.regionTree.kind < FGTextBuffer.RegionTree.Kind.LastActive)
										{
											activeIndex = siblingIndex;
										}
										if (siblingIndex > ifIndex &&
											(siblingLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.If ||
											siblingLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveIf))
										{
											ifIndex = siblingIndex;
										}
									}
								}
								
								if (activeIndex >= ifIndex)
								{
									setActive = false;
								}
							}
						}
						else if (formattedLine.regionTree.kind != FGTextBuffer.RegionTree.Kind.InactiveIf)
						{
							token.tokenKind = SyntaxToken.Kind.PreprocessorUnexpectedDirective;
							setActive = !inactiveParent;
						}

						if (token.tokenKind != SyntaxToken.Kind.PreprocessorUnexpectedDirective)
						{
							if (setActive)
							{
								OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.Elif);
							}
							else
							{
								OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.InactiveElif);
							}
						}
					}
				}
				else if (token.text == "else")
				{
					if (formattedLine.regionTree.parent == null)
					{
						token.tokenKind = SyntaxToken.Kind.PreprocessorUnexpectedDirective;
					}
					else
					{
						bool inactiveParent = formattedLine.regionTree.parent.kind > FGTextBuffer.RegionTree.Kind.LastActive;
						bool setActive = !inactiveParent;
						if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.If ||
							formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.Elif)
						{
							setActive = false;
						}
						else if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveIf ||
							formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveElif)
						{
							if (setActive)
							{
								var lineIndex = formattedLine.index;
								var ifIndex = -1;
								var activeIndex = -1;
								var siblings = formattedLine.regionTree.parent.children;
								for (var i = siblings.Count; i --> 0; )
								{
									var siblingLine = siblings[i].line;
									var siblingIndex = siblingLine.index;
									if (siblingIndex < lineIndex)
									{
										if (siblingIndex > activeIndex &&
											siblingLine.regionTree.kind < FGTextBuffer.RegionTree.Kind.LastActive)
										{
											activeIndex = siblingIndex;
										}
										if (siblingIndex > ifIndex &&
										(siblingLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.If ||
											siblingLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveIf))
										{
											ifIndex = siblingIndex;
										}
									}
								}
								
								if (activeIndex >= ifIndex)
								{
									setActive = false;
								}
							}
						}
						else if (formattedLine.regionTree.kind != FGTextBuffer.RegionTree.Kind.InactiveIf)
						{
							token.tokenKind = SyntaxToken.Kind.PreprocessorUnexpectedDirective;
						}

						if (token.tokenKind != SyntaxToken.Kind.PreprocessorUnexpectedDirective)
						{
							if (setActive)
							{
								OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.Else);
							}
							else
							{
								OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.InactiveElse);
							}
						}
					}
				}
				else if (token.text == "endif")
				{
					if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.If ||
						formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.Elif ||
						formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.Else ||
						formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveIf ||
						formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveElif ||
						formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveElse)
					{
						CloseRegion(formattedLine);
					}
					else
					{
						token.tokenKind = SyntaxToken.Kind.PreprocessorUnexpectedDirective;
					}
				}
				else if (token.text == "region")
				{
					var inactive = formattedLine.regionTree.kind > FGTextBuffer.RegionTree.Kind.LastActive;
					if (inactive)
					{
						OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.InactiveRegion);
					}
					else
					{
						OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.Region);
					}
					preprocessorCommentsAllowed = false;
				}
				else if (token.text == "endregion")
				{
					if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.Region ||
						formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveRegion)
					{
						CloseRegion(formattedLine);
					}
					else
					{
						token.tokenKind = SyntaxToken.Kind.PreprocessorUnexpectedDirective;
					}
					preprocessorCommentsAllowed = false;
				}
				else if (token.text == "define" || token.text == "undef")
				{
					var symbol = FGParser.ScanIdentifierOrKeyword(line, ref startAt);
					if (symbol != null && symbol.text != "true" && symbol.text != "false")
					{
						symbol.tokenKind = SyntaxToken.Kind.PreprocessorSymbol;
						tokens.Add(symbol);
						symbol.formattedLine = formattedLine;

						scriptDefinesChanged = true;
						
						var inactive = formattedLine.regionTree.kind > FGTextBuffer.RegionTree.Kind.LastActive;
						if (!inactive)
						{
							if (token.text == "define")
							{
								if (!scriptDefines.Contains(symbol.text))
								{
									scriptDefines.Add(symbol.text);
									//scriptDefinesChanged = true;
								}
							}
							else
							{
								if (scriptDefines.Contains(symbol.text))
								{
									scriptDefines.Remove(symbol.text);
									//scriptDefinesChanged = true;
								}
							}
						}
					}
				}
				else if (token.text == "error" || token.text == "warning")
				{
					preprocessorCommentsAllowed = false;
				}
			}
			
			if (!preprocessorCommentsAllowed)
			{
				ws = ScanWhitespace(line, ref startAt);
				if (ws != null)
				{
					tokens.Add(ws);
					ws.formattedLine = formattedLine;
				}
				if (startAt < length)
				{
					var textArgument = line.Substring(startAt);
					textArgument = textArgument.TrimEnd(FGTextEditor.spaceAndTab);
					tokens.Add(new SyntaxToken(SyntaxToken.Kind.PreprocessorArguments, textArgument) { formattedLine = formattedLine });
					startAt += textArgument.length;
					if (startAt < length)
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.Whitespace, new CharSpan(line, startAt)) { formattedLine = formattedLine });
				}
				return;
			}
			
			while (startAt < length)
			{
				ws = ScanWhitespace(line, ref startAt);
				if (ws != null)
				{
					tokens.Add(ws);
					ws.formattedLine = formattedLine;
					continue;
				}
				
				var firstChar = line[startAt];
				if (startAt < length - 1 && firstChar == '/' && line[startAt + 1] == '/')
				{
					tokens.Add(new SyntaxToken(SyntaxToken.Kind.Comment, new CharSpan(line, startAt)) { formattedLine = formattedLine });
					break;
				}
				else if (commentsOnly)
				{
					tokens.Add(new SyntaxToken(SyntaxToken.Kind.PreprocessorCommentExpected, new CharSpan(line, startAt)) { formattedLine = formattedLine });
					break;
				}
				
				if (char.IsLetterOrDigit(firstChar) || firstChar == '_')
				{
					token = ScanWord(line, ref startAt);
					token.tokenKind = SyntaxToken.Kind.PreprocessorArguments;
					tokens.Add(token);
					token.formattedLine = formattedLine;
				}
				else if (firstChar == '"')
				{
					token = FGParser.ScanStringLiteral(line, ref startAt);
					token.tokenKind = SyntaxToken.Kind.PreprocessorArguments;
					tokens.Add(token);
					token.formattedLine = formattedLine;
				}
				else if (firstChar == '\'')
				{
					token = ScanCharLiteral(line, ref startAt);
					token.tokenKind = SyntaxToken.Kind.PreprocessorArguments;
					tokens.Add(token);
					token.formattedLine = formattedLine;
				}
				else
				{
					token = new SyntaxToken(SyntaxToken.Kind.PreprocessorArguments, firstChar.ToString()) { formattedLine = formattedLine };
					tokens.Add(token);
					++startAt;
				}
				
				if (error)
				{
					token.tokenKind = SyntaxToken.Kind.PreprocessorDirectiveExpected;
				}
			}
			
			return;
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
						ScanStringLiteral(line, ref startAt, formattedLine);
						break;
					}

					if (startAt < length - 1 && line[startAt] == '@' && line[startAt + 1] == '\"')
					{
						token = new SyntaxToken(SyntaxToken.Kind.VerbatimStringBegin, new CharSpan(line, startAt, 2)) { formattedLine = formattedLine };
						tokens.Add(token);
						startAt += 2;
						formattedLine.blockState = FGTextBuffer.BlockState.StringBlock;
						break;
					}

					if (startAt < length - 2 && line[startAt] == '$' && line[startAt + 1] == '@' && line[startAt + 2] == '\"')
					{
						token = new SyntaxToken(SyntaxToken.Kind.InterpolatedStringStartLiteral, new CharSpan(line, startAt, 3)) { formattedLine = formattedLine };
						tokens.Add(token);
						startAt += 3;
						formattedLine.blockState = FGTextBuffer.BlockState.InterpolatedStringBlock;
						break;
					}

					if (!isCSharp4 && line[startAt] == '$')
					{
						ScanStringLiteral(line, ref startAt, formattedLine);
						break;
					}

					if (line[startAt] >= '0' && line[startAt] <= '9'
					    || startAt < length - 1 && line[startAt] == '.' && line[startAt + 1] >= '0' && line[startAt + 1] <= '9')
					{
						token = ScanNumericLiteral(line, ref startAt);
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
					// "&=", "|=", "^=", "<<=", ">>=", "=>", "::", "..", "->"
					var punctuatorStart = startAt++;
					if (startAt < line.length)
					{
						var nextCharacter = line[startAt];
						switch (line[punctuatorStart])
						{
							case '?':
								if (nextCharacter == '?')
									++startAt;
								break;
							case '+':
								if (nextCharacter == '+' || nextCharacter == '=')
									++startAt;
								break;
							case '-':
								if (nextCharacter == '-' || nextCharacter == '=' || nextCharacter == '>')
									++startAt;
								break;
							case '<':
								if (nextCharacter == '=')
									++startAt;
								else if (nextCharacter == '<')
								{
									++startAt;
									if (startAt < line.length && line[startAt] == '=')
										++startAt;
								}
								break;
							case '>':
								if (nextCharacter == '=')
									++startAt;
								//else if (startAt < line.Length && line[startAt] == '>')
								//{
								//    ++startAt;
								//    if (line[startAt] == '=')
								//        ++startAt;
								//}
								break;
							case '=':
								if (nextCharacter == '=' || nextCharacter == '>')
									++startAt;
								break;
							case '&':
								if (nextCharacter == '=' || nextCharacter == '&')
									++startAt;
								break;
							case '|':
								if (nextCharacter == '=' || nextCharacter == '|')
									++startAt;
								break;
							case '*':
							case '/':
							case '%':
							case '^':
							case '!':
								if (nextCharacter == '=')
									++startAt;
								break;
							case ':':
								if (nextCharacter == ':')
									++startAt;
								break;
							case '.':
								if (nextCharacter == '.')
								{
									tokens.Add(new SyntaxToken(SyntaxToken.Kind.Punctuator, new CharSpan(line, punctuatorStart, 1)) { formattedLine = formattedLine });
									++punctuatorStart;
									++startAt;
								}
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

				case FGTextBuffer.BlockState.InterpolatedStringBlock:
					ScanInterpolatedStringLiteral(line, ref startAt, formattedLine, true);
					break;
				
				case FGTextBuffer.BlockState.StringBlock:
					int i = startAt;
					int closingQuote = line.IndexOf('\"', startAt);
					while (closingQuote != -1 && closingQuote < length - 1 && line[closingQuote + 1] == '\"')
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
						tokens.Add(new SyntaxToken(SyntaxToken.Kind.VerbatimStringLiteral, new CharSpan(line, startAt, 1)) { formattedLine = formattedLine });
						++startAt;
						formattedLine.blockState = FGTextBuffer.BlockState.None;
					}
					break;
			}
		}
	}

	private new SyntaxToken ScanIdentifierOrKeyword(CharSpan line, ref int startAt)
	{
		var token = FGParser.ScanIdentifierOrKeyword(line, ref startAt);
		if (token != null)
		{
			if (token.tokenKind == SyntaxToken.Kind.Keyword && !IsKeywordOrBuiltInType(token.text))
				token.tokenKind = IsBuiltInLiteral(token.text) ? SyntaxToken.Kind.BuiltInLiteral : SyntaxToken.Kind.Identifier;
			else if (token.text == "@")
				token.tokenKind = SyntaxToken.Kind.Punctuator;
		}
		return token;
	}

	private bool IsKeyword(CharSpan word)
	{
		return word.length <= 10 && word.length >= 2 && keywords.Contains(word.GetHashCode());
	}

	private bool IsKeywordOrBuiltInType(CharSpan word)
	{
		return word.length <= 10 && word.length >= 2 && keywordsAndBuiltInTypes.Contains(word.GetHashCode());
	}

	private bool IsOperator(CharSpan text)
	{
		var len = text.length;
		if (len > 3)
			return false;
		var c0 = text[0];
		if (len == 1)
		{
			return
				c0 == '+' ||
				c0 == '-' ||
				c0 == '!' ||
				c0 == '~' ||
				c0 == '&' ||
				c0 == '*' ||
				c0 == '/' ||
				c0 == '%' ||
				c0 == '<' ||
				c0 == '>' ||
				c0 == '=' ||
				c0 == '^' ||
				c0 == '|' ||
				c0 == '?' ||
				c0 == ':';
		}
		var c1 = text[1];
		if (len == 2)
		{
			if (c1 == '=')
			{
				return
					c0 == '=' ||
					c0 == '!' ||
					c0 == '<' ||
					c0 == '>' ||
					c0 == '+' ||
					c0 == '-' ||
					c0 == '*' ||
					c0 == '/' ||
					c0 == '%' ||
					c0 == '&' ||
					c0 == '|' ||
					c0 == '^';
			}
			if (c0 == c1)
			{
				return
					c0 == '+' ||
					c0 == '-' ||
					c0 == '&' ||
					c0 == '|' ||
					c0 == '?' ||
					c0 == ':' ||
					c0 == '<' ||
					c0 == '>';
			}
			return c1 == '>' && (c0 == '-' || c0 == '=');
		}
		if (text[2] != '=')
			return false;
		return c0 == c1 && (c0 == '<' || c0 == '>');
		//return csOperators.Contains(text);
	}
	
	private void ScanStringLiteral(CharSpan line, ref int startAt, FGTextBuffer.FormattedLine formattedLine)
	{
		if (line[startAt] == '$')
		{
			var verbatim = false;//startAt + 1 < line.length && line[startAt + 1] == '@';
			ScanInterpolatedStringLiteral(line, ref startAt, formattedLine, verbatim);
			return;
		}
		
		var i = startAt + 1;
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
		
		if (formattedLine != null)
		{
			var token = new SyntaxToken(SyntaxToken.Kind.StringLiteral, new CharSpan(line, startAt, i - startAt));
			tokens.Add(token);
			token.formattedLine = formattedLine;
		}
		
		startAt = i;
	}
	
	private void ScanInterpolatedStringLiteral(CharSpan line, ref int startAt, FGTextBuffer.FormattedLine formattedLine, bool verbatim)
	{
		var i = verbatim ? startAt : startAt + 1;
		
		var interpolatedStringTokenKind = verbatim && (tokens.Count == 0 || tokens[tokens.Count - 1].tokenKind == SyntaxToken.Kind.InterpolatedStringStartLiteral) ?
			SyntaxToken.Kind.InterpolatedStringMidLiteral :
			SyntaxToken.Kind.InterpolatedStringStartLiteral;
		
		//if (i < line.length && line[i] == '@')
		//{
		//	formattedLine.blockState = FGTextBuffer.BlockState.StringBlock;
		//	++i;
		//}
		
		if (i < line.length && (verbatim || line[i] == '"'))
		{
			if (!verbatim)
				++i;

			while (i < line.length)
			{
				char c = line[i];
				
				if (c == '{')
				{
					if (i + 1 < line.length && line[i + 1] == '{')
					{
						++i;
					}
					else
					{
						if (formattedLine != null)
						{
							var token = new SyntaxToken(
								interpolatedStringTokenKind,
								new CharSpan(line, startAt, i - startAt));
							tokens.Add(token);
							token.formattedLine = formattedLine;
						}
						
						interpolatedStringTokenKind = SyntaxToken.Kind.InterpolatedStringMidLiteral;

						startAt = i;
						int formatStartAt = SkipStringInterpolation(line, ref i, verbatim);
						if (formattedLine != null)
						{
							if (formatStartAt >= 0)
							{
								Tokenize(line.Substring(startAt, formatStartAt - startAt), formattedLine);
								
								if (line[formatStartAt] == ':')
								{
									var token = new SyntaxToken(SyntaxToken.Kind.Punctuator, ":");
									tokens.Add(token);
									token.formattedLine = formattedLine;
									
									int formatLength = i - (formatStartAt + 1);
									if (formatLength > 0)
									{
										token = new SyntaxToken(
											SyntaxToken.Kind.InterpolatedStringFormatLiteral,
											new CharSpan(line, formatStartAt + 1, formatLength));
										tokens.Add(token);
										token.formattedLine = formattedLine;
									}
								}
							}
							else
							{
								if (formattedLine.blockState == FGTextBuffer.BlockState.InterpolatedStringBlock)
								{
									formattedLine.blockState = FGTextBuffer.BlockState.None;
									Tokenize(line.Substring(startAt, i - startAt), formattedLine);
									formattedLine.blockState = FGTextBuffer.BlockState.InterpolatedStringBlock;
								}
								else
								{
									Tokenize(line.Substring(startAt, i - startAt), formattedLine);
								}
							}
										
							if (i < line.length && line[i] == '}')
							{
								++i;
								SyntaxToken token = new SyntaxToken(SyntaxToken.Kind.Punctuator, "}");
								tokens.Add(token);
								token.formattedLine = formattedLine;
							}
						}
						startAt = i;
						
						continue;
					}
				}
				
				++i;
				
				if (c == '"')
				{
					if (!verbatim)
						break;
					if (i == line.length || line[i] != '"')
					{
						if (formattedLine != null)
							formattedLine.blockState = FGTextBuffer.BlockState.None;
						break;
					}
					else
					{
						++i;
					}
				}
				
				if (!verbatim && c == '\\' && i < line.length)
				{
					++i;
				}
			}
		}

		if (formattedLine != null)
		{
			SyntaxToken token;
			if (formattedLine.blockState == FGTextBuffer.BlockState.InterpolatedStringBlock)
			{
				token = new SyntaxToken(
					interpolatedStringTokenKind,
					new CharSpan(line, startAt, i - startAt));
				tokens.Add(token);
				token.formattedLine = formattedLine;
			}
			else if (interpolatedStringTokenKind == SyntaxToken.Kind.InterpolatedStringStartLiteral)
			{
				token = new SyntaxToken(
					SyntaxToken.Kind.InterpolatedStringWholeLiteral,
					new CharSpan(line, startAt, i - startAt));
				tokens.Add(token);
				token.formattedLine = formattedLine;
			}
			else
			{
				token = new SyntaxToken(
					SyntaxToken.Kind.InterpolatedStringEndLiteral,
					new CharSpan(line, startAt, i - startAt));
				tokens.Add(token);
				token.formattedLine = formattedLine;
				
				if (verbatim)
					formattedLine.blockState = FGTextBuffer.BlockState.None;
			}
		}
		
		startAt = i;
	}

	private int SkipStringInterpolation(CharSpan line, ref int i, bool verbatim)
	{
		var length = line.length;
		++i;
		while (i < length)
		{
			char c = line[i];
			if (c == '}' || c == ':')
				break;
			
			if (!SkipRegularBalancedText(line, ref i, true, verbatim))
				break;
		}
		
		if (i >= length)
			return -1;
		
		if (line[i] == ':')
		{
			var formatStartAt = i;
			++i;
			
			// skip format string (without closing brace)
			while (i < length)
			{
				char c = line[i];
				if (/*c == ':' ||*/ c == '"')
					break;
				if (c == '{')
				{
					if (i + 1 < line.length && line[i + 1] == '{')
						++i;
					else
						break;
				}
				else if (c == '}')
				{
					if (i + 1 < line.length && line[i + 1] == '}')
						++i;
					else
						break;
				}
				
				++i;
				
				if (c == '\\')
				{
					if (i < line.length)
						++i;
					else
						break;
				}
			}

			return formatStartAt;
		}
		
		return -1;
	}

	private bool SkipRegularBalancedText(CharSpan line, ref int i, bool scanInterpolationFormat, bool verbatim)
	{
		var startAt = i;
		var length = line.length;
		if (i >= length)
			return false;
		
		while (i < length)
		{
			char c = line[i];
			if (c == '$' || c == '"' || c == '@' && c + 1 < length && line[c + 1] == '"')
			{
				ScanStringLiteral(line, ref i, null);
				continue;
			}
			if (c == '/' && i + 1 < length)
			{
				++i;
				
				char next = line[i];
				if (next == '/')
				{
					i = length;
					break;
				}
				else if (next == '*')
				{
					++i;
					while (i < length)
					{
						if (line[i] != '*')
						{
							++i;
							continue;
						}
						++i;
						if (i < length && line[i] == '/')
						{
							++i;
							break;
						}
					}
					continue;
				}
			}
			else if (c == '}' || c == ')' || c == ']' || scanInterpolationFormat && c == ':')
			{
				break;
			}
			
			++i;
			
			if (c == '{')
			{
				SkipRegularBalancedText(line, ref i, false, verbatim);
				if (i < length && line[i] == '}')
					++i;
			}
			else if (c == '[')
			{
				SkipRegularBalancedText(line, ref i, false, verbatim);
				if (i < length && line[i] == ']')
					++i;
			}
			else if (c == '(')
			{
				SkipRegularBalancedText(line, ref i, false, verbatim);
				if (i < length && line[i] == ')')
					++i;
			}
		}
		
		return i > startAt;
	}
	
#if false
	private static string[] classDeclarationParents =
	{
		"namespaceMemberDeclaration",
		"namespaceDeclaration",
		"namespaceBody",
		"classDeclaration",
		"classBody",
		"classMemberDeclaration",
		"structDeclaration",
		"structBody",
		"structMemberDeclaration",
		"compilationUnit"
	};
	
	public static IEnumerable<ParseTree.Node> EnumerateClassDeclarationNodes(ParseTree.Node rootNode)
	{
		return EnumerateNodes(rootNode, "classDeclaration", classDeclarationParents);
	}
	
	public static void LogClassDeclarations(ParseTree.Node rootNode)
	{
		foreach (var node in EnumerateClassDeclarationNodes(rootNode))
		{
			Debug.Log("class " + node.declaration.Name.GetString());
		}
	}
	
	public static IEnumerable<ParseTree.Node> EnumerateNodes(ParseTree.Node rootNode, string findRuleName, params string[] parentRuleNames)
	{
		ParseTree.BaseNode current = rootNode;
		while (current != null)
		{
			var node = current as ParseTree.Node;
			if (node != null)
			{
				if (node.RuleName == findRuleName)
					yield return node;
				
				if (node.firstChild != null)
				{
					if (Array.IndexOf<string>(parentRuleNames, node.RuleName) >= 0)
					{
						current = node.firstChild;
						continue;
					}
				}
			}
						
			var next = current.nextSibling;
			while (next == null)
			{
				current = current.parent;
				if (current == null)
					yield break;
				next = current.nextSibling;
			}
			current = next;
		}
	}
#endif
}

}
