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

namespace ScriptInspector.Themes
{
	using UnityEngine;
	using UnityEditor;
	
	[InitializeOnLoad]
	public class Base16TomorrowDark
	{
		/*
		Derived from Chris Kempson's work:
		https://github.com/chriskempson/tomorrow-theme/tree/master
		
		Ported to Si3 by Justin Macklin.
		
		
		LICENSE

		Tomorrow Theme is released under the MIT License:

		Copyright (C) 2011 Chris Kempson

		Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

		The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
		*/
		
		private static string _themeName = "Base16 Tomorrow Dark";
		
		static Base16TomorrowDark()
		{
			FGTextEditor.AddTheme(_colourTheme, _themeName);
		}
		
		public static Theme _colourTheme = new Theme
		{
			background              = new Color32(29, 31, 33, 255),
			text                    = new Color32(248, 248, 242, 255),
			hyperlinks              = new Color32(127, 74, 129, 255),
			foldedText              = new Color32(248, 248, 242, 255),
			
			keywords                = new Color32(178, 148, 187, 255),//
			controlKeywords         = new Color32(178, 148, 187, 255),
			constants               = new Color32(222, 147, 95, 255),
			strings                 = new Color32(181, 189, 104, 255),
			builtInLiterals         = new Color32(240, 198, 116, 255),
			operators               = new Color32(197, 200, 198, 255),//
			
			referenceTypes          = new Color32(240, 198, 116, 255),
			valueTypes              = new Color32(178, 148, 187, 255),
			interfaceTypes          = new Color32(240, 198, 116, 255),
			enumTypes               = new Color32(240, 198, 116, 255),
			delegateTypes           = new Color32(240, 198, 116, 255),
			builtInTypes            = Color.clear,
			
			namespaces              = new Color32(204, 102, 102, 255),//
			methods                 = new Color32(129, 162, 190, 230),
			fields                  = new Color32(204, 102, 102, 255),
			properties              = new Color32(204, 102, 102, 255),
			events                  = new Color32(248, 248, 242, 255),
			
			parameters              = new Color32(204, 102, 102, 255),
			variables               = new Color32(204, 102, 102, 255),
			typeParameters          = new Color32(0xFD, 0x97, 0x1F, 0xFF),
			enumMembers             = new Color32(222, 147, 95, 255),
			
			preprocessor            = new Color32(129, 162, 190, 230),
			defineSymbols           = new Color32(129, 162, 190, 230),
			inactiveCode            = new Color32(117, 113, 94, 255),
			comments                = new Color32(150, 152, 150, 255),
			xmlDocs                 = new Color32(117, 113, 94, 255),
			xmlDocsTags             = new Color32(117, 113, 94, 255),
			
			lineNumbers             = new Color32(150, 152, 150, 255),
			lineNumbersHighlight    = new Color32(248, 248, 242, 255),
			lineNumbersBackground   = new Color32(29, 31, 33, 255),
			fold                    = new Color32(59, 58, 50, 255),
			foldingButton           = new Color32(248, 248, 242, 255),
			
			activeSelection			= new Color32(73, 72, 62, 255),
			passiveSelection		= new Color32(56, 56, 48, 255),
			searchResults           = new Color32(0, 96, 96, 128),
			
			trackSaved              = new Color32(158, 189, 104, 255),
			trackChanged            = new Color32(255, 204, 102, 255),
			trackReverted           = new Color32(246, 201, 60, 255),
			
			currentLine             = new Color32(55, 59, 65, 120),
			currentLineInactive     = new Color32(50, 50, 41, 255),
			
			referenceHighlight      = new Color32(48, 65, 87, 144),
			referenceModifyHighlight = new Color32(105, 48, 49, 144),
			
			tooltipBackground       = new Color32(62, 61, 49, 255),
			tooltipFrame            = new Color32(188, 188, 188, 255),
			tooltipText             = new Color32(208, 208, 208, 255),
			
			listPopupFrame          = new Color32(188, 188, 188, 255),
			listPopupBackground     = new Color32(62, 61, 49, 255),
			
			typesStyle              = FontStyle.Italic,
			typeParametersStyle     = FontStyle.Italic,
			parametersStyle         = FontStyle.Italic,
		};
	}
}
