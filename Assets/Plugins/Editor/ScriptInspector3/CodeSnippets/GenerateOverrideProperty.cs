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
using System.Collections.Generic;
using System.Linq;

class GenerateOverrideProperty : ISnippetProvider
{
	class OverrideProperty : SnippetCompletion
	{
		public static Scope context;
		public static int overrideTextLength;
		
		private readonly InstanceDefinition property;
		
		public OverrideProperty(InstanceDefinition virtualProperty)
			: base(virtualProperty.name)
		{
			property = virtualProperty;
			displayFormat = GetDisplayName(virtualProperty);
		}
		
		private static string GetDisplayName(InstanceDefinition property)
		{
			if (property.kind == SymbolKind.Event)
				return "event {0} {{...}}";
			else
				return "{0} {{...}}";
		}
		
		public override string Expand()
		{
			string expandedCode;
			
			string modifiersString =
				property.IsInternal ? (property.IsProtected ? "protected internal" : "internal") :
				property.IsPrivateProtected ? "private protected" :
				property.IsProtected ? "protected" : "public";
			string propertyType = property.TypeOf().RelativeName(context);
			
			if (property.kind == SymbolKind.Event)
			{
				expandedCode = string.Format("{0} override event {1} {2};$end$",
					modifiersString, propertyType, property.name);
				return expandedCode;
			}
			
			SymbolDefinition getter = null;
			SymbolDefinition setter = null;
			for (var i = property.members.Count; i --> 0; )
			{
				var accessor = property.members[i];
				if (accessor.name == "get")
					getter = accessor;
				else if (accessor.name == "set")
					setter = accessor;
			}
			
			var endMarker = "$end$";
			var accessorModifier = "";
			
			var getAccessor = "";
			if (getter != null)
			{
				if (getter.accessLevel < property.accessLevel)
				{
					accessorModifier =
						getter.IsInternal ? (getter.IsProtected ? "protected internal " : "internal ") :
						getter.IsPrivateProtected ? "private protected " :
						getter.IsPrivate ? "private " : getter.IsProtected ? "protected " : "";
				}
				var baseCall = property.IsAbstract ?
					"throw new " + TypeReference.To(typeof(System.NotImplementedException)).definition.RelativeName(context) + "();" :
					"base." + property.name + ";";
				var returnStatement = propertyType == "void" || property.IsAbstract ? "" : "return ";
				getAccessor = string.Format(
					"\n\t{0}get {{ {1}{2}{3} }}",
					accessorModifier, returnStatement, baseCall, endMarker);
				endMarker = "";
			}
			
			var setAccessor = "";
			if (setter != null)
			{
				if (accessorModifier != "")
				{
					accessorModifier = "";
				}
				else if (setter.accessLevel < property.accessLevel)
				{
					accessorModifier =
						setter.IsInternal ? (setter.IsProtected ? "protected internal " : "internal ") :
						setter.IsPrivateProtected ? "private protected " :
						setter.IsPrivate ? "private " : setter.IsProtected ? "protected " : "";
				}
				var baseCall = property.IsAbstract ?
					"throw new " + TypeReference.To(typeof(System.NotImplementedException)).definition.RelativeName(context) + "();" :
					"base." + property.name + " = value;";
				setAccessor = string.Format(
					"\n\t{0}set {{ {1}{2} }}",
					accessorModifier, baseCall, endMarker);
			}
			
			expandedCode = string.Format(
				"{0} override {1} {2}{3}{{{4}{5}\n}}",
				modifiersString, propertyType, property.name,
				SISettings.magicMethods_openingBraceOnSameLine ? " " : "\n",
				getAccessor, setAccessor);
			return expandedCode;
		}

		public override void OverrideTypedInLength(ref int typedInLength)
		{
			typedInLength += overrideTextLength;
		}
	}
	
	public IEnumerable<SnippetCompletion> EnumSnippets(
		SymbolDefinition context,
		FGGrammar.TokenSet expectedTokens,
		SyntaxToken tokenLeft,
		Scope scope)
	{
		OverrideProperty.context = scope;
		
		if (tokenLeft == null || tokenLeft.parent == null || tokenLeft.parent.parent == null)
			yield break;
		
		if (tokenLeft.tokenKind != SyntaxToken.Kind.Keyword)
			yield break;
		
		if (tokenLeft.text != "override")
			yield break;
		
		var bodyScope = scope as BodyScope;
		if (bodyScope == null)
			yield break;
		
		var contextType = bodyScope.definition as TypeDefinitionBase;
		if (contextType == null || contextType.kind != SymbolKind.Class && contextType.kind != SymbolKind.Struct)
			yield break;
		
		var baseType = contextType.BaseType();
		if (baseType == null || baseType.kind != SymbolKind.Class && baseType.kind != SymbolKind.Struct)
			yield break;
		
		var overridePropertyCandidates = new Dictionary<string, InstanceDefinition>();
		baseType.ListOverrideCandidates(overridePropertyCandidates, contextType.Assembly);
		if (overridePropertyCandidates.Count == 0)
			yield break;
		
		var textBuffer = FGTextBuffer.activeEditor.TextBuffer;
		var firstToken = tokenLeft.parent.parent.GetFirstLeaf().token;
		if (firstToken.formattedLine != tokenLeft.formattedLine)
		{
			firstToken = tokenLeft.formattedLine.tokens[0];
			while (firstToken.tokenKind <= SyntaxToken.Kind.LastWSToken)
				firstToken = firstToken.formattedLine.tokens[firstToken.TokenIndex + 1];
		}
		var tokenSpan = textBuffer.GetTokenSpan(firstToken.parent);
		OverrideProperty.overrideTextLength = FGTextBuffer.activeEditor.caretPosition.characterIndex - tokenSpan.StartPosition.index;
		
		foreach (var property in overridePropertyCandidates)
		{
			var existingProperty = contextType.FindName(property.Key, 0, false) as InstanceDefinition;
			if (existingProperty != null && existingProperty.kind == property.Value.kind)
				continue;

			var overrideCompletion = new OverrideProperty(property.Value);
			yield return overrideCompletion;
		}
	}
	
	public string Get(
		string shortcut,
		SymbolDefinition context,
		FGGrammar.TokenSet expectedTokens,
		Scope scope)
	{
		return null;
	}
}
	
}
