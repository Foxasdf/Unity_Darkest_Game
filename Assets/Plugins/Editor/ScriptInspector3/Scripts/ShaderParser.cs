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

public class ShaderParser : FGParser
{
	public override HashIDSet Keywords { get { return keywords; } }
	public override HashIDSet BuiltInLiterals { get { return builtInLiterals; } }

	public override bool IsBuiltInType(CharSpan word)
	{
		return Array.BinarySearch(builtInTypes, word.GetString(), StringComparer.OrdinalIgnoreCase) >= 0;
	}
	
	public override bool IsBuiltInLiteral(CharSpan word)
	{
		return Array.BinarySearch(builtInLiteralsArray, word.GetString(), StringComparer.OrdinalIgnoreCase) >= 0;
	}
	
	private static readonly HashIDSet keywords = new HashIDSet();
	private static readonly string[] keywordsArray = {
		"AlphaTest", "Ambient", "Bind", "BindChannels", "Blend", "BorderScale", "Category", "CGINCLUDE", "CGPROGRAM", "ColorMask",
		"ColorMaterial", "Combine", "ConstantColor", "Cull", "Density", "Diffuse", "Emission", "ENDCG", "Fallback",
		"Fog", "GLSLEND", "GLSLPROGRAM", "GrabPass", "Lerp", "Lighting", "LightmapMode", "LightMode", "LightTexCount",
		"LOD", "Material", "Matrix", "Mode", "Name", "Offset", "Pass", "Properties", "RequireOptions",
		"SeparateSpecular", "SetTexture", "Shader", "Shininess", "Specular", "SubShader", "Tags", "TexGen",
		"TextureScale", "TextureSize", "UsePass", "ZTest", "ZWrite",
	};

	private static readonly HashIDSet operators = new HashIDSet {
		"++", "--", "->", "+", "-", "!", "~", "&", "*", "/", "%", "<<", ">>", "<", ">",
		"<=", ">=", "==", "!=", "^", "|", "&&", "||", "??", "?", "::", ":",
		"=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", "=>"
	};

	private static readonly HashIDSet preprocessorKeywords = new HashIDSet {
		"define", "elif", "else", "endif", "error", "if", "ifdef", "ifndef", "include", "pragma", "undef",
	};

	private static readonly string[] builtInTypes = new string[] {
		"2D", "3D", "Color", "Constant", "Cube", "Float", "Previous", "Primary", "Range",
		"Rect", "Texture", "Vector", "_CosTime", "_CubeNormalize", "_Light2World", "_ModelLightColor", "_Object2Light",
		"_Object2World", "_ObjectSpaceCameraPos", "_ObjectSpaceLightPos", "_ProjectionParams", "_SinTime",
		"_SpecFalloff", "_SpecularLightColor", "_Time", "_World2Light", "_World2Object",
	};
	
	protected static readonly HashIDSet builtInLiterals;
	protected static readonly string[] builtInLiteralsArray = new string[] {
		"A", "Always", "AmbientAndDiffuse", "AppDstAdd", "AppSrcAdd", "Back", "CubeNormal", "CubeReflect", "DstAlpha",
		"DstColor", "Emission", "EyeLinear", "Exp", "Exp2", "Front", "GEqual", "Greater", "LEqual", "Less", "Linear",
		"None", "Normal", "NotEqual", "ObjectLinear", "Off", "On", "One", "OneMinusDstAlpha", "OneMinusDstColor",
		"OneMinusSrcAlpha", "OneMinusSrcColor", "Pixel", "PixelOnly", "PixelOrNone", "RGB", "SoftVegetation",
		"SrcAlpha", "SrcColor", "SphereMap", "Vertex", "VertexAndPixel", "VertexOnly", "VertexOrNone", "VertexOrPixel",
		"Tangent", "Texcoord", "Texcoord0", "Texcoord1", "Zero",
	};
	
	static ShaderParser()
	{
		keywords = new HashIDSet(keywordsArray.Length);
		foreach (var item in keywordsArray)
			keywords.Add(item.ToLowerInvariant());

		builtInLiterals = new HashIDSet(builtInLiteralsArray.Length);
		foreach (var item in builtInLiteralsArray)
			builtInLiterals.Add(item.ToLowerInvariant());
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
		if (formattedLine.tokens != null)
			formattedLine.tokens.Clear();
		Tokenize(textLine, formattedLine);

//		syntaxTree.SetLineTokens(currentLine, lineTokens);
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
							token.style = FGTextEditor.StyleID.ReferenceType;
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
						else if (IsBuiltInType(token.text))
						{
							token.style = FGTextEditor.StyleID.ReferenceType;
						}
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
						var regionKind = formattedLine.regionTree.kind;
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
						token.style = FGTextEditor.StyleID.String;
						break;
				}
				lineTokens[i] = token;
			}
		}
	}
	
	protected void Tokenize(CharSpan line, FGTextBuffer.FormattedLine formattedLine)
	{
		var tokens = formattedLine.tokens;
		if (tokens == null)
		{
			tokens = new List<SyntaxToken>();
			formattedLine.tokens = tokens;
		}

		int startAt = 0;
		int length = line.length;
		SyntaxToken token;

		SyntaxToken ws = ScanWhitespace(line, ref startAt);
		if (ws != null)
		{
			tokens.Add(ws);
			ws.formattedLine = formattedLine;
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
			if (!preprocessorKeywords.Contains(token.text))
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

				//if (token.text == "if")
				//{
				//	bool active = ParsePPOrExpression(line, formattedLine, ref startAt);
				//	bool inactiveParent = formattedLine.regionTree.kind > FGTextBuffer.RegionTree.Kind.LastActive;
				//	if (active && !inactiveParent)
				//	{
				//		OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.If);
				//		commentsOnly = true;
				//	}
				//	else
				//	{
				//		OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.InactiveIf);
				//		commentsOnly = true;
				//	}
				//}
				//else if (token.text == "elif")
				//{
				//	bool active = ParsePPOrExpression(line, formattedLine, ref startAt);
				//	bool inactiveParent = formattedLine.regionTree.kind > FGTextBuffer.RegionTree.Kind.LastActive;
				//	if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.If ||
				//		formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.Elif ||
				//		formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveElif)
				//	{
				//		OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.InactiveElif);
				//	}
				//	else if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveIf)
				//	{
				//		inactiveParent = formattedLine.regionTree.parent.kind > FGTextBuffer.RegionTree.Kind.LastActive;
				//		if (active && !inactiveParent)
				//		{
				//			OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.Elif);
				//		}
				//		else
				//		{
				//			OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.InactiveElif);
				//		}
				//	}
				//	else
				//	{
				//		token.tokenKind = SyntaxToken.Kind.PreprocessorUnexpectedDirective;
				//	}
				//}
				//else if (token.text == "else")
				//{
				//	if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.If ||
				//		formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.Elif)
				//	{
				//		OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.InactiveElse);
				//	}
				//	else if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveIf ||
				//		formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveElif)
				//	{
				//		bool inactiveParent = formattedLine.regionTree.parent.kind > FGTextBuffer.RegionTree.Kind.LastActive;
				//		if (inactiveParent)
				//			OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.InactiveElse);
				//		else
				//			OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.Else);
				//	}
				//	else
				//	{
				//		token.tokenKind = SyntaxToken.Kind.PreprocessorUnexpectedDirective;
				//	}
				//}
				//else if (token.text == "endif")
				//{
				//	if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.If ||
				//		formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.Elif ||
				//		formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.Else ||
				//		formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveIf ||
				//		formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveElif ||
				//		formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveElse)
				//	{
				//		CloseRegion(formattedLine);
				//	}
				//	else
				//	{
				//		token.tokenKind = SyntaxToken.Kind.PreprocessorUnexpectedDirective;
				//	}
				//}
				//else if (token.text == "region")
				//{
				//	var inactive = formattedLine.regionTree.kind > FGTextBuffer.RegionTree.Kind.LastActive;
				//	if (inactive)
				//	{
				//		OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.InactiveRegion);
				//	}
				//	else
				//	{
				//		OpenRegion(formattedLine, FGTextBuffer.RegionTree.Kind.Region);
				//	}
				//	preprocessorCommentsAllowed = false;
				//}
				//else if (token.text == "endregion")
				//{
				//	if (formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.Region ||
				//		formattedLine.regionTree.kind == FGTextBuffer.RegionTree.Kind.InactiveRegion)
				//	{
				//		CloseRegion(formattedLine);
				//	}
				//	else
				//	{
				//		token.tokenKind = SyntaxToken.Kind.PreprocessorUnexpectedDirective;
				//	}
				//	preprocessorCommentsAllowed = false;
				//}
				//else if (token.text == "define" || token.text == "undef")
				//{
				//	var symbol = FGParser.ScanIdentifierOrKeyword(line, ref startAt);
				//	if (symbol != null && symbol.text != "true" && symbol.text != "false")
				//	{
				//		symbol.tokenKind = SyntaxToken.Kind.PreprocessorSymbol;
				//		formattedLine.tokens.Add(symbol);
				//		symbol.formattedLine = formattedLine;

				//		scriptDefinesChanged = true;
						
				//		var inactive = formattedLine.regionTree.kind > FGTextBuffer.RegionTree.Kind.LastActive;
				//		if (!inactive)
				//		{
				//			if (token.text == "define")
				//			{
				//				if (!scriptDefines.Contains(symbol.text))
				//				{
				//					scriptDefines.Add(symbol.text);
				//					//scriptDefinesChanged = true;
				//				}
				//			}
				//			else
				//			{
				//				if (scriptDefines.Contains(symbol.text))
				//				{
				//					scriptDefines.Remove(symbol.text);
				//					//scriptDefinesChanged = true;
				//				}
				//			}
				//		}
				//	}
				//}
				//else if (token.text == "error" || token.text == "warning")
				//{
				//	preprocessorCommentsAllowed = false;
				//}
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
					startAt = length - textArgument.length;
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
					token = ScanStringLiteral(line, ref startAt);
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
						token = ScanStringLiteral(line, ref startAt);
						tokens.Add(token);
						token.formattedLine = formattedLine;
						break;
					}

					//if (startAt < length - 1 && line[startAt] == '@' && line[startAt + 1] == '\"')
					//{
					//	token = new SyntaxToken(SyntaxToken.Kind.VerbatimStringBegin, new CharSpan(line, startAt, 2)) { formattedLine = formattedLine };
					//	tokens.Add(token);
					//	startAt += 2;
					//	formattedLine.blockState = FGTextBuffer.BlockState.StringBlock;
					//	break;
					//}

					if (line[startAt] >= '0' && line[startAt] <= '9'
					    || startAt < length - 1 && line[startAt] == '.' && line[startAt + 1] >= '0' && line[startAt + 1] <= '9')
					{
						token = ScanNumericLiteral(line, ref startAt);
						if (token.text == "2D" || token.text == "2d" || token.text == "3D" || token.text == "3d")
							token.tokenKind = SyntaxToken.Kind.Identifier;
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

				//case FGTextBuffer.BlockState.StringBlock:
				//	int i = startAt;
				//	int closingQuote = line.IndexOf('\"', startAt);
				//	while (closingQuote != -1 && closingQuote < length - 1 && line[closingQuote + 1] == '\"')
				//	{
				//		i = closingQuote + 2;
				//		closingQuote = line.IndexOf('\"', i);
				//	}
				//	if (closingQuote == -1)
				//	{
				//		tokens.Add(new SyntaxToken(SyntaxToken.Kind.VerbatimStringLiteral, new CharSpan(line, startAt)) { formattedLine = formattedLine });
				//		startAt = length;
				//	}
				//	else
				//	{
				//		tokens.Add(new SyntaxToken(SyntaxToken.Kind.VerbatimStringLiteral, new CharSpan(line, startAt, closingQuote - startAt)) { formattedLine = formattedLine });
				//		startAt = closingQuote;
				//		tokens.Add(new SyntaxToken(SyntaxToken.Kind.VerbatimStringLiteral, new CharSpan(line, startAt, 1)) { formattedLine = formattedLine });
				//		++startAt;
				//		formattedLine.blockState = FGTextBuffer.BlockState.None;
				//	}
				//	break;
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

	private bool IsKeyword(string word)
	{
		return Array.BinarySearch(keywordsArray, word, StringComparer.OrdinalIgnoreCase) >= 0;
	}

	private bool IsOperator(string text)
	{
		return operators.Contains(text);
	}
}

}
