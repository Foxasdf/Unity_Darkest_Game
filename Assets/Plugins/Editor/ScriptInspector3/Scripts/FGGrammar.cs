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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum SemanticFlags
{
	None = 0,

	SymbolDeclarationsMask = (1 << 8) - 1,
	ScopesMask = ~SymbolDeclarationsMask,

	SymbolDeclarationsBegin = 1,

	NamespaceDeclaration,
	UsingNamespace,
	UsingAlias,
	UsingStatic,
	ExternAlias,
	ClassDeclaration,
	TypeParameterDeclaration,
	BaseListDeclaration,
	ConstructorDeclarator,
	DestructorDeclarator,
	ConstantDeclarator,
	MethodDeclarator,
	LocalFunctionDeclarator,
	LocalVariableDeclarator,
	OutVariableDeclarator,
	IsVariableDeclarator,
	TupleDeconstructVariableDeclarator,
	ForEachVariableDeclaration,
	FromClauseVariableDeclaration,
	CaseVariableDeclaration,
	LabeledStatement,
	CatchExceptionParameterDeclaration,
	FixedParameterDeclaration,
	ParameterArrayDeclaration,
	ImplicitParameterDeclaration,
	ExplicitParameterDeclaration,
	PropertyDeclaration,
	IndexerDeclaration,
	GetAccessorDeclaration,
	SetAccessorDeclaration,
	EventDeclarator,
	EventWithAccessorsDeclaration,
	AddAccessorDeclaration,
	RemoveAccessorDeclaration,
	VariableDeclarator,
	OperatorDeclarator,
	ConversionOperatorDeclarator,
	StructDeclaration,
	InterfaceDeclaration,
	InterfacePropertyDeclaration,
	InterfaceMethodDeclaration,
	InterfaceEventDeclarator,
	InterfaceIndexerDeclaration,
	InterfaceGetAccessorDeclaration,
	InterfaceSetAccessorDeclaration,
	EnumDeclaration,
	EnumMemberDeclaration,
	DelegateDeclaration,
	AnonymousObjectCreation,
	MemberDeclarator,
	LambdaExpressionDeclaration,
	AnonymousMethodDeclaration,

	SymbolDeclarationsEnd,


	ScopesBegin                   = 1 << 8,

	CompilationUnitScope          = 1 << 8,
	NamespaceBodyScope            = 2 << 8,
	ClassBaseScope                = 3 << 8,
	TypeParameterConstraintsScope = 4 << 8,
	ClassBodyScope                = 5 << 8,
	StructInterfacesScope         = 6 << 8,
	StructBodyScope               = 7 << 8,
	InterfaceBaseScope            = 8 << 8,
	InterfaceBodyScope            = 9 << 8,
	FormalParameterListScope      = 10 << 8,
	EnumBodyScope                 = 11 << 8,
	MethodBodyScope               = 12 << 8,
	ConstructorInitializerScope   = 13 << 8,
	LambdaExpressionScope         = 14 << 8,
	LambdaExpressionBodyScope     = 15 << 8,
	AnonymousMethodScope          = 16 << 8,
	AnonymousMethodBodyScope      = 17 << 8,
	CodeBlockScope                = 18 << 8,
	SwitchBlockScope              = 19 << 8,
	SwitchSectionScope            = 20 << 8,
	ForStatementScope             = 21 << 8,
	EmbeddedStatementScope        = 22 << 8,
	UsingStatementScope           = 23 << 8,
	FixedStatementScope           = 24 << 8,
	LocalVariableInitializerScope = 25 << 8,
	SpecificCatchScope            = 26 << 8,
	ArgumentListScope             = 27 << 8,
	AttributeArgumentsScope       = 28 << 8,
	MemberInitializerScope        = 29 << 8,

	TypeDeclarationScope          = 30 << 8,
	MethodDeclarationScope        = 31 << 8,
	AttributesScope               = 32 << 8,
	AccessorBodyScope             = 33 << 8,
	AccessorsListScope            = 34 << 8,
	QueryExpressionScope          = 35 << 8,
	QueryBodyScope                = 36 << 8,
	MemberDeclarationScope        = 37 << 8,

	LocalFunctionBodyScope        = 38 << 8,
	
	FoldingScope                  = 39 << 8,

	ScopesEnd,
}

public interface IVisitableTreeNode<NonLeaf, Leaf>
{
	bool Accept(IHierarchicalVisitor<NonLeaf, Leaf> visitor);
}

public interface IHierarchicalVisitor<NonLeaf, Leaf>
{
	bool Visit(Leaf leafNode);
	bool VisitEnter(NonLeaf nonLeafNode);
	bool VisitLeave(NonLeaf nonLeafNode);
}

public class ParseTree
{
	public static uint resolverVersion = 2;
	
	public abstract class BaseNode
	{
		public Node parent;
		public BaseNode nextSibling;
		
		public bool missing { get { return syntaxError != null && syntaxError.IsMissingToken; } }
		public FGGrammar.Node grammarNode;
		public FGGrammar.ErrorMessageProvider syntaxError;
		public string semanticError;
		
		public short childIndex;
		
		protected uint _resolvedVersion = 1;
		protected SymbolDefinition _resolvedSymbol;
		public SymbolDefinition resolvedSymbol {
			get {
				if (_resolvedSymbol == null)
					return null;
				if (_resolvedVersion == 0)
					return _resolvedSymbol;
				if (_resolvedVersion != resolverVersion || !_resolvedSymbol.IsValid())
					_resolvedSymbol = null;
				return _resolvedSymbol;
			}
			set {
//#if SI3_WARNINGS
//				if (value != null && value.kind == SymbolKind.Error)
//				{
//					Debug.Log(ToString() + " => _resolvedSymbol: " + _resolvedSymbol + " -> value: " + value);
//				}
//#endif

				if (_resolvedVersion == 0 && _resolvedSymbol != null)
				{
#if SI3_WARNINGS
					Debug.LogWarning("Whoops! " + _resolvedSymbol);
#endif
					return;
				}
				_resolvedVersion = resolverVersion;
				_resolvedSymbol = value;
				semanticError = null;
			}
		}

		public void UnResolve()
		{
			_resolvedVersion = 1;
			_resolvedSymbol = null;
			semanticError = null;
		}

		public void CacheResolvedSymbol(SymbolDefinition symbol)
		{
			if (_resolvedVersion != 0)
			{
				_resolvedVersion = resolverVersion;
				_resolvedSymbol = symbol;
			}
		}
		
		public SymbolDefinition GetDeclaredSymbol()
		{
			if (_resolvedVersion != 0)
				return null;
			return _resolvedSymbol;
		}
		
		public void SetDeclaredSymbol(SymbolDefinition symbol)
		{
			_resolvedSymbol = symbol;
			_resolvedVersion = 0;
			semanticError = null;
		}

		public Leaf FindPreviousLeaf()
		{
			var result = this;
			while (result.childIndex == 0 && result.parent != null)
				result = result.parent;
			if (result.parent == null)
				return null;
			result = result.parent.ChildAt(result.childIndex - 1);
			Node node;
			while ((node = result as Node) != null)
			{
				if (node.numValidNodes == 0)
					return node.FindPreviousLeaf();
				result = node.lastValid;
			}
			return result as Leaf;
		}

		public Leaf FindNextLeaf()
		{
			var result = this;
			while (result.parent != null && result.childIndex == result.parent.numValidNodes - 1)
				result = result.parent;
			if (result.parent == null)
				return null;
			result = result.nextSibling;
			Node node;
			while ((node = result as Node) != null)
			{
				if (node.numValidNodes == 0)
					return node.FindNextLeaf();
				result = node.ChildAt(0);
			}
			return result as Leaf;
		}
		
		public BaseNode FindPreviousNode()
		{
			var result = this;
			while (result.childIndex == 0 && result.parent != null)
				result = result.parent;
			if (result.parent == null)
				return null;
			result = result.parent.ChildAt(result.childIndex - 1);
			return result;
		}

		public abstract void Dump(StringBuilder sb, int indent);

		public bool IsAncestorOf(BaseNode node)
		{
			while (node != null)
				if (node.parent == this)
					return true;
				else
					node = node.parent;
			return false;
		}
		
		public Node FindParentByName(string ruleName)
		{
			var result = parent;
			while (result != null && result.RuleName != ruleName)
				result = result.parent;
			return result;
		}
		
		public Node FirstNonTrivialParent()
		{
			var result = parent;
			while (result != null && result.numValidNodes == 1)
				result = result.parent;
			if (result != null && result.numValidNodes == 0)
				result = null;
			return result;
		}
		
		public Node FirstNonTrivialParent(out int childIndex)
		{
			childIndex = this.childIndex;
			var result = parent;
			while (result != null && result.numValidNodes == 1)
			{
				childIndex = result.childIndex;
				result = result.parent;
			}
			if (result != null && result.numValidNodes == 0)
				result = null;
			return result;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			Dump(sb, 1);
			return sb.ToString();
		}

		public abstract CharSpan Print();

		public bool HasLeafs()
		{
			var it = this;
			do
			{
				var node = it as Node;
				if (node == null)
					return true;
				
				it = node.firstChild;
				while (it != null && it.childIndex < node.numValidNodes)
				{
					node = it as Node;
					if (node == null)
						return true;
					it = node.firstChild;
				}
				
				if (node == this)
					return false;
				
				it = node.nextSibling;
				while (it == null || it.childIndex >= it.parent.numValidNodes)
				{
					node = node.parent;
					if (node == this)
						return false;
					it = node.nextSibling;
				}
			} while (true);
		}
		
		public bool HasLeafs(bool validNodesOnly)
		{
			if (validNodesOnly)
				return HasLeafs();
				
			var it = this;
			do
			{
				var node = it as Node;
				if (node == null)
					return true;
				
				it = node.firstChild;
				while (it != null)
				{
					node = it as Node;
					if (node == null)
						return true;
					it = node.firstChild;
				}
				
				if (node == this)
					return false;
				
				it = node.nextSibling;
				while (it == null)
				{
					node = node.parent;
					if (node == this)
						return false;
					it = node.nextSibling;
				}
			} while (true);
		}

		public abstract bool IsLit(string litText);
		
		public Leaf GetFirstLeaf() { return GetFirstLeaf(true); }
		public abstract Leaf GetFirstLeaf(bool validNodesOnly);
		
		public Leaf GetLastLeafInParent()
		{
			if (parent == null || childIndex >= parent.numValidNodes)
				return null;

			if (nextSibling != null && childIndex < parent.numValidNodes - 1)
			{
				var nextLeaf = nextSibling.GetLastLeafInParent();
				if (nextLeaf != null)
					return nextLeaf;
			}

			var asLeaf = this as Leaf;
			if (asLeaf != null && asLeaf.token != null)
				return asLeaf;

			var asNode = this as Node;
			if (asNode.firstChild != null)
				return asNode.firstChild.GetLastLeafInParent();
			
			return null;
		}
	}

	public class Leaf : BaseNode
	{
		public int line {
			get {
				return token != null ? token.Line : 0;
			}
		}
		public int tokenIndex {
			get {
				return token != null && token.formattedLine != null ? token.formattedLine.tokens.IndexOf(token) : 0;
			}
		}
		
		public uint extraChecksResolverVersion;
		public SyntaxToken token;

		public Leaf() {}

		public Leaf(FGGrammar.IScanner scanner)
		{
			//line = scanner.CurrentLine();
			//tokenIndex = scanner.CurrentTokenIndex();
			token = scanner.Current;
			token.parent = this;
		}

		public bool TryReuse()
		{
			if (token == null)
				return false;
			var current = FGGrammar.Node.scanner.Current;
			if (current.parent == this)//token.text == current.text && token.tokenKind == current.tokenKind)
			{
				//	line = scanner.CurrentLine();
				//	tokenIndex = scanner.CurrentTokenIndex();
				token.parent = this;
				return true;
			}
			return false;
		}

		public override void Dump(StringBuilder sb, int indent)
		{
			sb.Append(' ', 2 * indent);
			sb.Append(childIndex);
			sb.Append(" ");
			if (syntaxError != null)
				sb.Append("? ");
			sb.Append(token);
			sb.Append(' ');
			sb.Append((line + 1));
			sb.Append(':');
			sb.Append(tokenIndex);
			if (syntaxError != null)
				sb.Append(' ').Append(syntaxError);
			sb.AppendLine();
		}

		public void ReparseToken()
		{
			if (token != null)
			{
				token.parent = null;
				token = null;
			}
			if (parent != null)
				parent.RemoveNodeAt(childIndex/*, false*/);
		}

		public override CharSpan Print()
		{
			var lit = grammarNode as FGGrammar.Lit;
			if (lit != null)
				return lit.body;//lit.pretty;
			return token != null ? token.text : CharSpan.Empty;
		}

		public override bool IsLit(string litText)
		{
			var lit = grammarNode as FGGrammar.Lit;
			return lit != null && lit.body == litText;
		}

		public override Leaf GetFirstLeaf(bool validNodesOnly)
		{
			return this;
		}

		public bool HasErrors()
		{
			return syntaxError != null;
		}
	}

	public class Node : BaseNode
	{
		public BaseNode firstChild;
		//public BaseNode FirstChild {
		//	get { return firstChild; }
		//}
		public BaseNode lastValid;
		//public BaseNode LastValid {
		//	get { return lastValid; }
		//}
		public short numValidNodes;

		public Scope scope;
		public SymbolDeclaration declaration;

		public SemanticFlags semantics
		{
			get
			{
				var peer = ((FGGrammar.Id) grammarNode).peer;
				if (peer == null)
					Debug.Log("no peer for " + grammarNode);
				return peer != null ? ((FGGrammar.Rule) peer).semantics : SemanticFlags.None;
			}
		}

		private Node(FGGrammar.Id rule)
		{
			grammarNode = rule;
		}
		
		private static Node pool;
		
		public static Node Create(FGGrammar.Id rule)
		{
			if (pool != null)
			{
				var node = pool;
				pool = (Node) node.nextSibling;
				node.nextSibling = null;
				
				node.grammarNode = rule;
				
				node.childIndex = 0;
				node.declaration = null;
				node.scope = null;
				node.semanticError = null;
				node.syntaxError = null;
				node._resolvedVersion = 1;
				node._resolvedSymbol = null;

				return node;
			}
			
			return new Node(rule);
		}
		
		public void Recycle()
		{
			nextSibling = pool;
			pool = this;
		}

		public BaseNode ChildAt(int index)
		{
			if (index == -1)
				return lastValid;
			if (index < 0)
				index += numValidNodes;
			if (index < 0 || index >= numValidNodes)
				return null;
			
			var result = firstChild;
			while (result != null && index --> 0)
				result = result.nextSibling;
				
			return result;
		}

		public Leaf LeafAt(int index)
		{
			if (index < 0)
				index += numValidNodes;
			if (index < 0 || index >= numValidNodes)
				return null;
			
			var result = firstChild;
			while (result != null && index --> 0)
				result = result.nextSibling;
				
			return result as Leaf;
		}

		public Node NodeAt(int index)
		{
			if (index < 0)
				index += numValidNodes;
			if (index < 0 || index >= numValidNodes)
				return null;

			var result = firstChild;
			while (result != null && index --> 0)
				result = result.nextSibling;
				
			return result as Node;
		}

		public string RuleName
		{
			get { return ((FGGrammar.Id) grammarNode).GetName(); }
		}

		//static int createdTokensCounter;
		//static int reusedTokensCounter;
		
		public Leaf AddToken()
		{
			var firstInvalid = lastValid != null ? lastValid.nextSibling : firstChild;
			
			var reused = firstInvalid as Leaf;
			if (reused != null && reused.TryReuse())
			{
				//	reused.missing = false;
				//	reused.errors = null;

				//reused.parent = this;
				//reused.childIndex = numValidNodes;
				++numValidNodes;
				lastValid = reused;

				//if (++reusedTokensCounter + createdTokensCounter == 1)
				//{
				//	UnityEditor.EditorApplication.delayCall += () => {
				//		Debug.Log("Tokens - Created: " + createdTokensCounter + " Reused: " + reusedTokensCounter);
				//		createdTokensCounter = 0;
				//		reusedTokensCounter = 0;
				//	};
				//}
				
				//Debug.Log("reused " + reused.token.text + " at line " + (scanner.CurrentLine() + 1));
				return reused;
			}
			
			//if (reusedTokensCounter + ++createdTokensCounter == 1)
			//{
			//	UnityEditor.EditorApplication.delayCall += () => {
			//		Debug.Log("Tokens - Created: " + createdTokensCounter + " Reused: " + reusedTokensCounter);
			//		createdTokensCounter = 0;
			//		reusedTokensCounter = 0;
			//	};
			//}
			
			var leaf = new Leaf(FGGrammar.Node.scanner) { parent = this, childIndex = numValidNodes };
			if (lastValid != null)
			{
				leaf.nextSibling = lastValid.nextSibling;
				lastValid.nextSibling = leaf;
				++numValidNodes;
			}
			else
			{
				leaf.nextSibling = firstChild;
				firstChild = leaf;
				++numValidNodes;
			}
			lastValid = leaf;
			
			var next = leaf.nextSibling;
			while (next != null)
			{
				next.childIndex++;
				next = next.nextSibling;
			}

			return leaf;
		}

		public Leaf AddToken(SyntaxToken token)
		{
			//var firstInvalid = lastValid != null ? lastValid.nextSibling : firstChild;
			
			//if (!token.IsMissing())
			//{
			//	var reused = firstInvalid as Leaf;
			//	if (reused != null && reused.token.text == token.text && reused.token.tokenKind == token.tokenKind)
			//	{
			//		reused.syntaxError = null;

			//		reused.token = token;
			//		reused.parent = this;
			//		reused.childIndex = numValidNodes;
			//		++numValidNodes;
			//		lastValid = reused;

			//		Debug.Log("reused " + reused.token + " from line " + (reused.line + 1));
			//		return reused;
			//	}
			//}

			var leaf = new Leaf { token = token, parent = this, childIndex = numValidNodes };
			if (lastValid != null)
			{
				leaf.nextSibling = lastValid.nextSibling;
				lastValid.nextSibling = leaf;
				++numValidNodes;
			}
			else
			{
				leaf.nextSibling = firstChild;
				firstChild = leaf;
				++numValidNodes;
			}
			lastValid = leaf;
			
			var next = leaf.nextSibling;
			while (next != null)
			{
				next.childIndex++;
				next = next.nextSibling;
			}

			return leaf;
		}

		public int InvalidateFrom(int index)
		{
			var numInvalidated = index >= numValidNodes ? 0 : numValidNodes - index;
			if (numInvalidated == 0)
				return 0;
			numValidNodes -= (short) numInvalidated;
			if (numValidNodes == 0)
				lastValid = null;
			else
				lastValid = ChildAt(numValidNodes - 1);
			return numInvalidated;
		}

		public void RemoveNodeAt(int index/*, bool canReuse = true*/)
		{
			if (index == 0)
			{
				if (firstChild == null)
					return;
				
				var next = firstChild.nextSibling;

				var node = firstChild as Node;
				if (node != null)
					node.Dispose();
				firstChild.parent = null;
				
				if (numValidNodes > 0)
				{
					--numValidNodes;
					if (numValidNodes == 0)
						lastValid = null;
				}
				
				firstChild = next;
				while (next != null)
				{
					next.childIndex--;
					next = next.nextSibling;
				}
			}
			else
			{
				var prevChild = firstChild;
				for (var i = 1; prevChild != null && i < index; ++i)
					prevChild = prevChild.nextSibling;
				if (prevChild == null || prevChild.nextSibling == null)
					return;
				
				var child = prevChild.nextSibling;
				var next = child.nextSibling;

				var node = child as Node;
				if (node != null)
					node.Dispose();
				child.parent = null;
				
				if (index < numValidNodes)
				{
					--numValidNodes;
					
					if (ReferenceEquals(child, lastValid))
						lastValid = prevChild;
				}
				
				prevChild.nextSibling = next;
				while (next != null)
				{
					next.childIndex--;
					next = next.nextSibling;
				}
			}
			
			if (index == 0 && parent != null && !HasLeafs(false))
				parent.RemoveNodeAt(childIndex/*, canReuse*/);
		}

		//static int reusedNodesCounter, createdNodesCounter;
		
		public Node AddNode(FGGrammar.Id rule, FGGrammar.IScanner scanner, out bool skipParsing)
		{
			skipParsing = false;

			bool removedReusable = false;
			
			var firstInvalid = lastValid != null ? lastValid.nextSibling : firstChild;
			
			var reusable = firstInvalid as Node;
			if (reusable != null)
			{
				var firstLeaf = reusable.GetFirstLeaf(false);
				if (reusable.grammarNode != rule)
				{
					//	Debug.Log("Cannot reuse (different rules) " + rule.GetName() + " at line " + (scanner.CurrentLine() + 1) + ":"
					//		+ scanner.CurrentTokenIndex() + " vs. " + reusable.RuleName);
					if (firstLeaf == null || firstLeaf.token == null || firstLeaf.line <= scanner.CurrentLine())
					{
						reusable.Dispose();
						removedReusable = true;
					}
				}
				else
				{
					if (firstLeaf != null && firstLeaf.token != null && firstLeaf.line > scanner.CurrentLine())
					{
						// Ignore this node for now
					}
					else if (firstLeaf == null || firstLeaf.token != null && firstLeaf.syntaxError != null)
					{
						//	Debug.Log("Could reuse " + rule.GetName() + " at line " + (scanner.CurrentLine() + 1) + ":"
					//		+ scanner.CurrentTokenIndex() + " (firstLeaf is null) ");
						reusable.Dispose();
						removedReusable = true;
					}
					else if (firstLeaf.token == scanner.Current)
					{
						var lastLeaf = reusable.GetLastLeaf();
						if (lastLeaf != null && !reusable.HasErrors())
						{
							if (lastLeaf.token != null)
							{
								//if (++reusedNodesCounter + createdNodesCounter == 1)
								//{
								//	UnityEditor.EditorApplication.delayCall += () => {
								//		Debug.Log("Nodes - Created: " + createdNodesCounter + " Reused: " + reusedNodesCounter);
								//		createdNodesCounter = 0;
								//		reusedNodesCounter = 0;
								//	};
								//}
								
								/*var moved =*/ ((CsGrammar.Scanner) scanner).MoveAfterLeaf(lastLeaf);
							//	Debug.Log(moved  + " skipping to " + scanner.CurrentGrammarNode + " at " + lastLeaf.line +":" + lastLeaf.tokenIndex);
								skipParsing = true;
							//}

							//if (lastLeaf == null || lastLeaf.token == null)
							//{
							////	Debug.LogWarning("lastLeaf has no token! " + lastLeaf);
							//}
							//else
							//{
								//	Debug.Log("Reused full " + rule.GetName() + " from line " + (firstLeaf.line + 1) + " up to line " + (scanner.CurrentLine() + 1) + ":"
								//		+ scanner.CurrentTokenIndex() + " (" + scanner.Current.text + "...) ");
								++numValidNodes;
								lastValid = reusable;
								return scanner.CurrentParseTreeNode;
							}
						}
						else
						{
							//Debug.Log(firstLeaf.line);
							reusable.Dispose();
							removedReusable = true;
						}
					}
					else if (reusable.numValidNodes == 0)
					{
						//if (++reusedNodesCounter + createdNodesCounter == 1)
						//{
						//	UnityEditor.EditorApplication.delayCall += () => {
						//		Debug.Log("Nodes - Created: " + createdNodesCounter + " Reused: " + reusedNodesCounter);
						//		createdNodesCounter = 0;
						//		reusedNodesCounter = 0;
						//	};
						//}
						
						//Debug.Log("Reusing " + rule.GetName() + " at line " + (scanner.CurrentLine() + 1) + ":"
						//	+ scanner.CurrentTokenIndex() + " (" + scanner.Current.text + "...) reusable.numValidNodes is 0");
						++numValidNodes;
						lastValid = reusable;
						reusable.syntaxError = null;
						return reusable;
					}
					else if (scanner.Current != null && (firstLeaf.token == null || firstLeaf.line <= scanner.CurrentLine()))
					{
						//	Debug.Log("Cannot reuse " + rule.GetName() + " at line " + (scanner.CurrentLine() + 1) + ":"
						//		+ scanner.CurrentTokenIndex() + " (" + scanner.Current.text + "...) ");
						reusable.Dispose();
						if (firstLeaf.token == null || firstLeaf.line == scanner.CurrentLine())
						{
							removedReusable = true;
						}
						else
						{
							if (lastValid != null)
								lastValid.nextSibling = firstInvalid.nextSibling;
							else
								firstChild = firstInvalid.nextSibling;
							
							var next = firstInvalid.nextSibling;
							while (next != null)
							{
								next.childIndex--;
								next = next.nextSibling;
							}

							return AddNode(rule, scanner, out skipParsing);
						}
					}
					else
					{
					//	Debug.Log("Not reusing anything (scanner.Current is null). reusable is " + reusable.RuleName);
					}
				}
			}

			//if (reusedNodesCounter + ++createdNodesCounter == 1)
			//{
			//	UnityEditor.EditorApplication.delayCall += () => {
			//		Debug.Log("Nodes - Created: " + createdNodesCounter + " Reused: " + reusedNodesCounter);
			//		createdNodesCounter = 0;
			//		reusedNodesCounter = 0;
			//	};
			//}
			
			var node = Node.Create(rule);
			node.parent = this;
			node.childIndex = numValidNodes;
			
			if (firstInvalid == null)
			{
				if (lastValid != null)
					lastValid.nextSibling = node;
				else
					firstChild = node;
				++numValidNodes;
				lastValid = node;
			}
			else
			{
				if (removedReusable)
				{
					if (lastValid != null)
					{
						lastValid.nextSibling = node;
						node.nextSibling = firstInvalid.nextSibling;
					}
					else
					{
						firstChild = node;
						node.nextSibling = firstInvalid.nextSibling;
					}
				}
				else if (lastValid != null)
				{
					node.nextSibling = lastValid.nextSibling;
					lastValid.nextSibling = node;
				}
				else
				{
					node.nextSibling = firstChild;
					firstChild = node;
				}
				++numValidNodes;
				lastValid = node;
			}
			
			var nextNotValid = node.nextSibling;
			if (nextNotValid != null && nextNotValid.childIndex != numValidNodes)
			{
				for (var i = numValidNodes; nextNotValid != null; ++i, nextNotValid = nextNotValid.nextSibling)
					nextNotValid.childIndex = i;
			}
			
			return node;
		}

		public bool HasErrors()
		{
			BaseNode it = this;
			do
			{
				var node = it as Node;
				if (node == null)
				{
					if (it.syntaxError != null)
						return true;

					var next = it.nextSibling;
					while (next == null || next.childIndex >= next.parent.numValidNodes)
					{
						it = it.parent;
						if (ReferenceEquals(it, this))
							return false;
						next = it.nextSibling;
					}
					
					it = next;
					continue;
				}
				
				it = node.firstChild;
				while (it != null && it.childIndex < node.numValidNodes)
				{
					var nextNode = it as Node;
					if (nextNode == null)
					{
						if (it.syntaxError != null)
							return true;
							
						it = it.nextSibling;
						continue;
					}
					node = nextNode;
					it = node.firstChild;
				}
				
				if (ReferenceEquals(node, this))
					return false;
				
				it = node.nextSibling;
				while (it == null || it.childIndex >= it.parent.numValidNodes)
				{
					node = node.parent;
					if (ReferenceEquals(node, this))
						return false;
					it = node.nextSibling;
				}
			} while (true);
		}

		public Leaf FindLit(string litText)
		{
			for (var child = firstChild; child != null && child.childIndex < numValidNodes; child = child.nextSibling)
			{
				var lit = child.grammarNode as FGGrammar.Lit;
				if (lit != null && lit.body == litText)
					return child as Leaf;
			}
			return null;
		}
		
		public BaseNode FindChildByName(string name)
		{
			for (var child = firstChild; child != null && child.childIndex < numValidNodes; child = child.nextSibling)
			{
				if (child.grammarNode != null && child.grammarNode.IsName(name))
					return child;
			}
			return null;
		}

		public BaseNode FindChildByName(string name, string name1)
		{
			for (var child = firstChild; child != null && child.childIndex < numValidNodes; child = child.nextSibling)
			{
				if (child.grammarNode != null && child.grammarNode.IsName(name))
				{
					var node = child as Node;
					if (node == null)
						return null;

					return node.FindChildByName(name1);
				}
			}
			return null;
		}

		public BaseNode FindChildByName(string name, string name1, string name2)
		{
			for (var child = firstChild; child != null && child.childIndex < numValidNodes; child = child.nextSibling)
			{
				if (child.grammarNode != null && child.grammarNode.IsName(name))
				{
					var node = child as Node;
					if (node == null)
						return null;

					return node.FindChildByName(name1, name2);
				}
			}
			return null;
		}

		public BaseNode FindChildByName(params string[] name)
		{
			BaseNode result = this;
			foreach (var n in name)
			{
				var node = result as Node;
				if (node == null)
					return null;

				result = null;
				for (var child = node.firstChild; child != null && child.childIndex < node.numValidNodes; child = child.nextSibling)
				{
					if (child.grammarNode != null && child.grammarNode.IsName(n))
					{
						result = child;
						break;
					}
				}
				if (result == null)
					return null;
			}
			return result;
		}

		public override void Dump(StringBuilder sb, int indent)
		{
			sb.Append(' ', 2 * indent);
			sb.Append(childIndex);
			sb.Append(' ');
			var id = grammarNode as FGGrammar.Id;
			if (id != null && id.Rule != null)
			{
				if (syntaxError != null)
					sb.Append("? ");
				sb.AppendLine(id.Rule.GetNt());
				if (syntaxError != null)
					sb.Append(' ').AppendLine(syntaxError.GetErrorMessage());
			}

			++indent;
			for (var child = firstChild; child != null && child.childIndex < numValidNodes; child = child.nextSibling)
				child.Dump(sb, indent);
		}

		public override CharSpan Print()
		{
			var result = string.Empty;
			for (var child = firstChild; child != null && child.childIndex < numValidNodes; child = child.nextSibling)
				result += child.Print();
			return result;
		}

		public override bool IsLit(string litText)
		{
			return false;
		}

		public override Leaf GetFirstLeaf(bool validNodesOnly)
		{
			for (var child = firstChild; child != null && (!validNodesOnly || child.childIndex < numValidNodes); child = child.nextSibling)
			{
				var leaf = child as Leaf;
				if (leaf != null)
					return leaf;
				leaf = ((Node) child).GetFirstLeaf(validNodesOnly);
				if (leaf != null)
					return leaf;
			}
			return null;
		}

		private static Stack<BaseNode> baseNodesStack = new Stack<BaseNode>();

		public Leaf GetLastLeaf()
		{
			if (numValidNodes == 0)
				return null;
				
			var leaf = lastValid as Leaf;
			if (leaf != null && leaf.token != null)
				return leaf;
				
			Node node;
			BaseNode child, current, lastValidChild;
			
			baseNodesStack.Clear();	
			for (child = firstChild; child != lastValid; child = child.nextSibling)
				baseNodesStack.Push(child);
			baseNodesStack.Push(lastValid);

			while (baseNodesStack.Count > 0)
			{
				current = baseNodesStack.Pop();

				leaf = current as Leaf;
				if (leaf != null && leaf.token != null)
					return leaf;
				
				node = (Node)current;
				if (node.numValidNodes == 0)
					continue;
				
				lastValidChild = node.lastValid;
				
				leaf = lastValidChild as Leaf;
				if (leaf != null && leaf.token != null)
					return leaf;
				
				for (child = node.firstChild; child != lastValidChild; child = child.nextSibling)
					baseNodesStack.Push(child);
				baseNodesStack.Push(lastValidChild);
			}
			
			return null;
		}

		public void Exclude()
		{
			if (parent == null || numValidNodes != 1 || firstChild == null || firstChild.nextSibling != null)
				return;
			if (childIndex == 0)
			{
				parent.firstChild = firstChild;
			}
			else
			{
				var prevSibling = parent.ChildAt(childIndex - 1);
				prevSibling.nextSibling = firstChild;
				firstChild.childIndex = childIndex;
			}
			if (parent.lastValid == this)
				parent.lastValid = firstChild;

			firstChild.parent = parent;
			firstChild.nextSibling = nextSibling;
			
			parent = null;
			nextSibling = null;
			firstChild = null;
			lastValid = null;
			numValidNodes = 0;
			
			Recycle();
		}

		public void RemoveDeclarations(bool onlyInvalid)
		{
			var child = firstChild;

			if (!onlyInvalid || numValidNodes == 0)
			{
				while (child != null)
				{
					var asNode = child as Node;
					child = child.nextSibling;
					if (asNode != null)
						asNode.RemoveDeclarations(false);
				}

				// Remove declaration
				if (declaration != null)
				{
					if (declaration.scope != null)
						declaration.scope.RemoveDeclaration(declaration);
					//declaration = null;
				
					ParseTree.resolverVersion = unchecked(ParseTree.resolverVersion + 1) | 1;
					//if (ParseTree.resolverVersion == 0)
					//	++ParseTree.resolverVersion;
				}

				return;
			}

			while (child != null)
			{
				var asNode = child as Node;
				var next = child.nextSibling;
				if (asNode != null)
					asNode.RemoveDeclarations(true);
				
				if (ReferenceEquals(child, lastValid))
				{
					while (next != null)
					{
						asNode = next as Node;
						next = next.nextSibling;
						if (asNode != null)
							asNode.RemoveDeclarations(false);
					}
					
					return;
				}
				
				child = next;
			}
		}

		public void CleanUp()
		{
			BaseNode current = this;
			while (current != null)
			{
				// Cleanup the current node
				var node = current as Node;
				if (node != null)
				{
					if (node.numValidNodes == 0)
					{
						var child = node.firstChild;
						while (child != null)
						{
							var next = child.nextSibling;
							var childNode = child as Node;
							if (childNode != null)
								childNode.Dispose();
							child = next;
						}
						node.firstChild = null;
					}
					else
					{
						var child = node.lastValid.nextSibling;
						while (child != null)
						{
							var next = child.nextSibling;
							var childNode = child as Node;
							if (childNode != null)
								childNode.Dispose();
							child = next;
						}
						node.lastValid.nextSibling = null;
					}
				}

				var currentNode = current as Node;
				if (currentNode != null && currentNode.firstChild != null)
				{
					// Move to the first child
					current = currentNode.firstChild;
				}
				else if (current.nextSibling != null)
				{
					// Move to the next sibling
					current = current.nextSibling;
				}
				else
				{
					// Move up to the parent until a node with a next sibling is found
					while (current.parent != null && current.nextSibling == null)
					{
						current = current.parent;
					}
					// Move to the next sibling if available
					if (current.nextSibling != null)
					{
						current = current.nextSibling;
					}
					else
					{
						// If no next sibling is found, traversal is complete
						current = null;
					}
				}
			}
		}

		public void CleanUp_Old()
		{
			var child = firstChild;
			if (numValidNodes == 0)
			{
				while (child != null)
				{
					var asNode = child as Node;
					child = child.nextSibling;
					if (asNode != null)
						asNode.Dispose();
				}
				
				firstChild = null;
				return;
			}

			while (child != null)
			{
				var asNode = child as Node;
				var next = child.nextSibling;
				if (asNode != null)
					asNode.CleanUp();
				
				if (ReferenceEquals(child, lastValid))
				{
					while (next != null)
					{
						asNode = next as Node;
						next = next.nextSibling;
						if (asNode != null)
							asNode.Dispose();
					}
					
					child.nextSibling = null;
					return;
				}
				
				child = next;
			}
		}
		
		public void Dispose()
		{
			if (declaration != null)// && declaration.scope != null)
			{
				//if (declaration.definition != null)
				//	Debug.Log("Removing " + declaration.definition.ReflectionName);
				//else
				//	Debug.Log("Removing null declaration! " + declaration.kind);
				
				if (declaration.scope != null)
					declaration.scope.RemoveDeclaration(declaration);
				//declaration = null;
				
				++ParseTree.resolverVersion;
				if (ParseTree.resolverVersion == 0)
					++ParseTree.resolverVersion;
			}

			var child = firstChild;
			while (child != null)
			{
				var asNode = child as Node;
				child = child.nextSibling;
				if (asNode != null)
					asNode.Dispose();
			}
			
			parent = null;
			//firstChild = null;
			//lastValid = null;
			//nextSibling = null;
		}
	}

	public Node root;

	public override string ToString()
	{
		var sb = new StringBuilder();
		root.Dump(sb, 0);
		return sb.ToString();
	}
}

[Flags]
public enum IdentifierCompletionsType
{
	None				= 1<<0,
	Namespace			= 1<<1,
	TypeName			= 1<<2,
	ArrayType			= 1<<3,
	NonArrayType		= 1<<4,
	ValueType			= 1<<5,
	SimpleType			= 1<<6,
	ExceptionClassType	= 1<<7,
	AttributeClassType	= 1<<8,
	Member				= 1<<9,
	Static				= 1<<10,
	Value				= 1<<11,
	ArgumentName		= 1<<12,
	MemberName          = 1<<13,
}

public abstract class FGGrammar
{
	public abstract Parser GetParser { get; }

	public abstract IdentifierCompletionsType GetCompletionTypes(ParseTree.BaseNode afterNode);

	public abstract class ErrorMessageProvider
	{
		protected TokenSet lookahead;
		protected Parser parser;
		
		protected ErrorMessageProvider(Parser parser, TokenSet lookahead)
		{
			this.parser = parser;
			this.lookahead = lookahead;
		}
		
		public virtual bool IsMissingToken { get { return false; } }
		
		public abstract string GetErrorMessage();
	}
	
	public class MissingTokenErrorMessage : ErrorMessageProvider
	{
		public MissingTokenErrorMessage(Parser parser, TokenSet lookahead)
			: base(parser, lookahead)
		{}
		
		public override bool IsMissingToken { get { return true; } }
		
		public override string GetErrorMessage()
		{
			return "Syntax error: Expected " + lookahead.ToString(parser);
		}
	}
	
	public class UnexpectedTokenErrorMessage : ErrorMessageProvider
	{
		public UnexpectedTokenErrorMessage(Parser parser, TokenSet lookahead)
			: base(parser, lookahead)
		{}
		
		public override string GetErrorMessage()
		{
			return "Unexpected token! Expected " + lookahead.ToString(parser);
		}
	}
	
	public class IntegerConstantIsTooLargeErrorMessage : ErrorMessageProvider
	{
		public static readonly IntegerConstantIsTooLargeErrorMessage Instance = new IntegerConstantIsTooLargeErrorMessage();
		
		public IntegerConstantIsTooLargeErrorMessage()
			: base(null, null)
		{}
		
		public override string GetErrorMessage()
		{
			return "Integer constant is too large";
		}
	}
	
	public abstract class IScanner : IDisposable //: IEnumerator<SyntaxToken>
	{
		public SyntaxToken Current;
		//{
		//	get
		//	{
		//		return tokens != null ? tokens[currentTokenIndex] : EOF;
		//	}
		//	set {}
		//}
		//{
		//	get
		//	{
		//		return currentTokenCache ?? (tokens != null ? tokens[currentTokenIndex] : EOF);
		//	}
		//}
		
		//object System.Collections.IEnumerator.Current
		//{
		//	get
		//	{
		//		return currentTokenCache ?? (tokens != null ? tokens[currentTokenIndex] : EOF);
		//	}
		//}
		
		public void Dispose()
		{
		}

		public abstract bool MoveNext();

		public void Reset()
		{
			currentLine = -1;
			currentTokenIndex = -1;
			//	nonTriviaTokenIndex = 0;
			tokens = null;
			Current = null;
		}

		protected string fileName;

		protected FGTextBuffer textBuffer;
		protected List<FGTextBuffer.FormattedLine> lines;
		protected List<SyntaxToken> tokens;

		protected int currentLine = -1;
		protected int currentTokenIndex = -1;

		protected static SyntaxToken EOF;

		//protected SyntaxToken currentTokenCache;

		public int maxScanDistance;
		//public bool KeepScanning { get { return maxScanDistance > 0; } }


		public int CurrentLine() { return currentLine; }
		public int CurrentTokenIndex() { return currentTokenIndex; }
		
		//public SyntaxToken CurrentToken()
		//{
		//	return currentTokenCache ?? (tokens != null ? tokens[currentTokenIndex] : EOF);
		//}
		
		public abstract IScanner Clone();
		public abstract void Delete();

		public bool Lookahead(Node node, int maxDistance = int.MaxValue)
		{
			if (tokens == null && currentLine > 0)
				return false;

			//			long laTime;
			//			if (!timeLookaheads.TryGetValue(node, out laTime))
			//				laTime = 0;
			//
			//			int numLAs;
			//			if (!numLookaheads.TryGetValue(node, out numLAs))
			//				numLAs = 0;
			//			numLookaheads[node] = numLAs + 1;
			//
			//			var timer = new Stopwatch();
			//			timer.Start();

			//bool memValue;
			//var id = node as Id;
			//if (id != null)
			//{
			//    if (memoizationTable.TryGetValue(id.peer, out memValue))
			//        return memValue;
			//}
			//else
			//{
			//    if (memoizationTable.TryGetValue(node, out memValue))
			//        return memValue;
			//}
			
			var c = Current;
			var t = tokens;
			var line = currentLine;
			var index = currentTokenIndex;
			//	var realIndex = nonTriviaTokenIndex;

			var temp = maxScanDistance;
			maxScanDistance = maxDistance;
			var match = node.Scan(this);
			maxScanDistance = temp;

			for (var i = currentLine < lines.Count ? currentLine : lines.Count - 1; i > line; --i)
				if (i - line > lines[i].laLines)
					lines[i].laLines = i - line;
			
			currentLine = line;
			currentTokenIndex = index;
			//	nonTriviaTokenIndex = realIndex;
			tokens = t;
			Current = c;
			
			//if (id != null)
			//    memoizationTable[id.peer] = match;
			//else
			//    memoizationTable[node] = match;

			//			timer.Stop();
			//			laTime += timer.ElapsedTicks;
			//			timeLookaheads[node] = laTime;

			return match;
		}

		public SyntaxToken Lookahead(/*int offset, bool skipWhitespace = true*/)
		{
			//if (!skipWhitespace)
			//{
			//	return currentTokenIndex + 1 < tokens.Count ? tokens[currentTokenIndex + 1] : EOF;
			//}
			
			var c = Current;
			var t = tokens;
			var cl = currentLine;
			var cti = currentTokenIndex;

			//while (offset --> 0)
			//{
				if (!MoveNext())
				{
					Current = c;
					tokens = t;
					currentLine = cl;
					currentTokenIndex = cti;
					return EOF;
				}
			//}
			var token = tokens[currentTokenIndex];
			
			for (var i = currentLine < lines.Count ? currentLine : lines.Count - 1; i > cl; --i)
				if (i - cl > lines[i].laLines)
					lines[i].laLines = i - cl;

			Current = c;
			tokens = t;
			currentLine = cl;
			currentTokenIndex = cti;
			
			return token;
		}

		public FGGrammar.Node CurrentGrammarNode;
		public ParseTree.Node CurrentParseTreeNode;

		public ParseTree.Leaf ErrorToken;
		public ErrorMessageProvider ErrorMessage;
		public FGGrammar.Node ErrorGrammarNode;
		public ParseTree.Node ErrorParseTreeNode;

		public bool Seeking;

		public abstract void InsertMissingToken(ErrorMessageProvider errorMessage);

		public abstract void CollectCompletions(TokenSet tokenSet);

		public abstract void OnReduceSemanticNode(ParseTree.Node node);

		public abstract void SyntaxErrorExpected(TokenSet lookahead);
	}

	public abstract class Node
	{
		public static IScanner scanner;
		protected static Parser initializingParser;
		
		public Node parent;
		public int childIndex;

		public TokenSet lookahead;
		public TokenSet follow;

		public static implicit operator Node(string s)
		{
			return new Lit(s);
		}

		public static Node operator | (Node a, Node b)
		{
			return new Alt(a, b);
		}

		public static Node operator | (Alt a, Node b)
		{
			a.Add(b);
			return a;
		}

		public static Node operator - (Node a, Node b)
		{
			return new Seq(a, b);
		}

		public static Node operator - (Seq a, Node b)
		{
			a.Add(b);
			return a;
		}

		//public static implicit operator Predicate<IScanner> (Node node)
		//{
		//    return (IScanner scanner) =>
		//        {
		//            try
		//            {
		//                node.Parse(scanner.Clone(), new GoalAdapter());
		//            }
		//            catch
		//            {
		//                return false;
		//            }
		//            return true;
		//        };
		//}

		public virtual Node GetNode()
		{
			return this;
		}

		public virtual void Add(Node node)
		{
			throw new Exception(GetType() + ": cannot Add()");
		}

		//public virtual TokenSet GetLookahead()
		//{
		//    return lookahead;
		//}

		public virtual bool Matches()
		{
			return lookahead.Matches(scanner.Current.tokenId);
		}

		public virtual bool Matches(IScanner scanner)
		{
			return lookahead.Matches(scanner.Current.tokenId);
		}

		public abstract TokenSet SetLookahead();
		
		public abstract TokenSet SetFollow(TokenSet succ);
  		
		public virtual void CheckLL1()
		{
			CachedNextAfterChild(null);

			if (follow == null)
				throw new Exception(this + ": follow not set");
			if (lookahead.MatchesEmpty() && lookahead.Accepts(follow))
				throw new Exception(this + ": ambiguous\n"
				+ "  lookahead " + lookahead.ToString(initializingParser) + "\n"
				+ "  follow " + follow.ToString(initializingParser));
		}
		
		public abstract bool Scan(IScanner scanner);

		public void SyntaxError(IScanner scanner, ErrorMessageProvider errorMessage)
		{
			if (scanner.ErrorMessage != null)
				return;
			//Debug.LogError(errorMessage);
			scanner.ErrorMessage = errorMessage;
		}
		
		//public virtual Node TryGetCachesNext(Node child)
		//{
		//	if (parent == null)
		//		return null;
		//	var result = parent.TryGetCachesNext(this);
		//	...
		//}
		
		public virtual void CollectLitAndIdNodes(HashSet<int> lits) {}

		public abstract Node Parse();

		public Node Recover(out int numMissing)
		{
			numMissing = 0;

			var current = this;
			while (current.parent != null)
			{
				var next = current.parent.NextAfterChild(current);
				if (next == null)
					return null;

				var nextId = next as Id;
				if (nextId != null && nextId.GetName() == "attribute")
					return nextId;

				var nextMatchesScanner = next.Matches();
				while (next != null && !nextMatchesScanner && next.lookahead.MatchesEmpty())
				{
					next = next.parent.NextAfterChild(next);
					nextMatchesScanner = next != null && next.Matches();
				}

				var text = scanner.Current.text;

				if (nextMatchesScanner && text == ";" && next is Opt)
				{
					return null;
				}

				//if (next is Many)
				//{
				//    var currentToken = scanner.Current;
				//    var n = 0;
				//    var recoverOnMany = Recover(scanner, out n);
				//    if (recoverOnMany != null && currentToken != scanner.Current)
				//    {
				//        next = recoverOnMany;
				//    }
				//    else
				//    {
				//        next = next.parent.NextAfterChild(next, scanner);
				//    }
				//}
				//if (next == null)
				//    break;

				++numMissing;
				if (nextMatchesScanner)
				{
					//if (scanner.Current.text == ";" && next is Opt)
					//{
					//	Debug.Log(next);
					//	return null;
					//}

					//var clone = scanner.Clone();
					if (text == "{" || text == "}" || scanner.Lookahead(next, 3))//next.Scan(clone))
					{
						return next;
					}
//					else
//					{
//						nextMatchesScanner = false;
//					}
				}
				
				if (numMissing <= 1 && text != "{" && text != "}")
				{
					var laScanner = scanner.Clone();
					if (//laScanner.CurrentLine() == scanner.CurrentLine() &&
						laScanner.MoveNext() && next.Matches(laScanner))
					{
						if (laScanner.Lookahead(next, 3))//.Scan(laScanner))
						{
							laScanner.Delete();
							return null;
						}
					}
					laScanner.Delete();
				}

				current = next;
			}
			return null;
		}
		
		//public Node cachedNext { get { return null; } set {} }
		public Node cachedNext;

		public virtual Node CachedNextAfterChild(Node child)
		{
			cachedNext = cachedNext ?? (parent == null ? null : parent.CachedNextAfterChild(this));
			return cachedNext;
		}

		public virtual Node NextAfterChild(Node child)
		{
			return cachedNext ?? parent.NextAfterChild(this);
		}

		public virtual Node NextAfterChild(Node child, IScanner scanner)
		{
			return cachedNext ?? parent.NextAfterChild(this, scanner);
		}

		public void CollectCompletions(TokenSet tokenSet, IScanner scanner, int identifierId)
		{
			var clone = scanner.Clone();

			var current = this;
			while (current != null && current.parent != null)
			{
				tokenSet.Add(current.lookahead);
				if (!current.lookahead.MatchesEmpty())
					break;
				current = current.parent.NextAfterChild(current, clone);
			}
			tokenSet.RemoveEmpty();
		}

		public virtual bool IsName(string name)
		{
			//throw new InvalidOperationException();
			return false;
		}

		public override string ToString()
		{
			return base.ToString();
		}
	}

	public class NameId : Id
	{
		public NameId()
			: base("NAME")
		{}

		public override TokenSet SetLookahead()
		{
			if (lookahead == null)
			{
				base.SetLookahead();
				lookahead.AddSingleTokenID(initializingParser.TokenToId("IDENTIFIER"));
			}
			return lookahead;
		}
	}

	public abstract class IdBase : Node
	{
		protected string name;
		
		public abstract Token Token { get; }
		public abstract Rule Rule { get; }
		
		public IdBase(string name)
		{
			this.name = name;
		}

		public override string ToString()
		{
			return name;
		}
			
		public override bool IsName(string name)
		{
			return this.name == name;
		}

		public string GetName()
		{
			return name;
		}
		
		public override void CheckLL1()
		{
			CachedNextAfterChild(null);
		}
		
		public sealed override void CollectLitAndIdNodes(HashSet<int> lits)
		{
			var hashCode = new CharSpan(name).GetHashCode();
			
			if (initializingParser.ids.ContainsKey(hashCode))
				return;

			Rule rule;
			initializingParser.nts.TryGetValue(hashCode, out rule);
			initializingParser.ids.Add(hashCode, rule);
			
			initializingParser.tokens.Add(name);
		}
	}

	//public class IdToken : IdBase
	//{
	//	public Token peer;

	//	public override Token Token { get { return peer; } }

	//	public override Rule Rule { get { return null; } }
		
	//	public IdToken(Id original)
	//		: base(original.GetName())
	//	{
	//		peer = ((Token)original.peer).Clone();
	//		lookahead = original.lookahead;
	//		follow = original.follow;
	//	}
		
	//	public override TokenSet SetLookahead()
	//	{
	//		//throw new InvalidOperationException();
	//		if (lookahead == null)
	//		{
	//			peer = initializingParser.GetPeer(name) as Token;
	//			if (peer == null)
	//				Debug.LogError("Parser rule \"" + name + "\" not found!!!");
	//			else
	//			{
	//				peer.parent = this;
	//				peer.childIndex = 0;
	//				lookahead = peer.SetLookahead();
	//			}
	//		}
	//		return lookahead;
	//	}

	//	public override TokenSet SetFollow(TokenSet succ)
	//	{
	//		//throw new InvalidOperationException();
	//		SetLookahead();
	//		return lookahead;
	//	}
		
	//	public override bool Scan(IScanner scanner)
	//	{
	//		return !scanner.KeepScanning || peer.Scan(scanner);
	//	}

	//	public override Node Parse()
	//	{
	//		return peer.Parse(scanner);
	//	}

	//	public override Node NextAfterChild(Node child, IScanner scanner)
	//	{
	//		return base.NextAfterChild(this, scanner);
	//	}
	//}

	//public class IdRule : IdBase
	//{
	//	public Rule peer;

	//	public override Token Token { get { return null; } }

	//	public override Rule Rule { get { return peer; } }

	//	public IdRule(Id original)
	//		: base(original.GetName())
	//	{
	//		peer = (Rule)original.peer;
	//		lookahead = original.lookahead;
	//		follow = original.follow;
	//	}

	//	public override TokenSet SetLookahead()
	//	{
	//		if (lookahead == null)
	//		{
	//			peer = initializingParser.GetPeer(name) as Rule;
	//			if (peer == null)
	//				Debug.LogError("Parser rule \"" + name + "\" not found!!!");
	//			else
	//			{
	//				peer.parent = this;
	//				peer.childIndex = 0;
	//				lookahead = peer.SetLookahead();
	//			}
	//		}
	//		return lookahead;
	//	}

	//	public override TokenSet SetFollow(TokenSet succ)
	//	{
	//		SetLookahead();
	//		peer.SetFollow(succ);

	//		return lookahead;
	//	}
		
	//	public override bool Scan(IScanner scanner)
	//	{
	//		return !scanner.KeepScanning || peer.Scan(scanner);
	//	}

	//	public override Node Parse()
	//	{
	//		peer.parent = this;
			
	//		bool skip;
	//		scanner.CurrentParseTreeNode = scanner.CurrentParseTreeNode.AddNode(this, scanner, out skip);
	//		if (skip)
	//			return scanner.CurrentGrammarNode;
			
	//		var result2 = peer.Parse(scanner);
	//		peer.parent = this;
	//		return result2;
	//	}

	//	public override Node NextAfterChild(Node child, IScanner scanner)
	//	{
	//		scanner.CurrentParseTreeNode = scanner.CurrentParseTreeNode.parent;
	//		return base.NextAfterChild(this, scanner);
	//	}
	//}

	public class Id : IdBase
	{
		// Token or Rule.
		public Node peer; // { get; protected set; }

		public override Token Token { get { return peer as Token; } }

		public override Rule Rule { get { return peer as Rule; } }

		public Id(string name)
			: base(name)
		{
		}

		public Id Clone()
		{
			var clone = new Id(name) { peer = peer, lookahead = lookahead, follow = follow, cachedNext = cachedNext };
			var token = peer as Token;
			if (token != null)
			{
				clone.peer = token.Clone();
				clone.peer.parent = this;
			}
			return clone;

			//if (peer is Token)
			//{
			//	var clone = new IdToken(this);
			//	clone.peer.parent = this;
			//	return clone;
			//}
			//else
			//{
			//	var clone = new IdRule(this);
			//	return clone;
			//}
		}

		public override TokenSet SetLookahead()
		{
			if (lookahead == null)
			{
				peer = initializingParser.GetPeer(name);
				if (peer == null)
					Debug.LogError("Parser rule \"" + name + "\" not found!!!");
				else
				{
					peer.parent = this;
					peer.childIndex = 0;
					lookahead = peer.SetLookahead();
				}
			}
			
			return lookahead;
		}

		public override TokenSet SetFollow(TokenSet succ)
		{
			SetLookahead();
			if (peer is Rule)
				peer.SetFollow(succ);

			return lookahead;
		}

		public override bool Scan(IScanner scanner)
		{
			//throw new InvalidOperationException();
			return scanner.maxScanDistance <= 0 || peer.Scan(scanner);
		}

		public override Node Parse()
		{
			peer.parent = this;
			return peer.Parse();
		}

		public override Node CachedNextAfterChild(Node child)
		{
			if (peer is Rule)
				return null;
			
			return base.CachedNextAfterChild(null);
		}

		public override Node NextAfterChild(Node child)
		{
			if (cachedNext != null)
				return cachedNext;
			if (peer is Rule)
			{
				scanner.CurrentParseTreeNode = scanner.CurrentParseTreeNode.parent;
				if (parent == null)
					return null;
			}
			return parent.NextAfterChild(this);
		}

		public override Node NextAfterChild(Node child, IScanner scanner)
		{
			if (cachedNext != null)
				return cachedNext;
			if (peer is Rule)
			{
				scanner.CurrentParseTreeNode = scanner.CurrentParseTreeNode.parent;
				if (parent == null)
					return null;
			}
			return parent.NextAfterChild(this, scanner);
		}
	}

	public class Lit : Node
	{
		public readonly CharSpan body;
		//public string pretty;

		public Lit(string body)
		{
			//pretty = body;
			this.body = body.Trim();
		}
		
		public override TokenSet SetLookahead()
		{
			return lookahead ?? (lookahead = new TokenSet(initializingParser.TokenToId(body)));
		}

		public override TokenSet SetFollow(TokenSet succ)
		{
			return SetLookahead();
		}
		
		public override void CheckLL1()
		{
			CachedNextAfterChild(null);
		}

		public override bool Scan(IScanner scanner)
		{
			if (scanner.maxScanDistance <= 0)
				return true;
			
			if (!lookahead.Matches(scanner.Current.tokenId))
				return false;

			scanner.MoveNext();
			return true;
		}

		public override Node Parse()
		{
			if (!lookahead.Matches(scanner.Current.tokenId))
			{
				scanner.SyntaxErrorExpected(lookahead);
				return this;
			}

			scanner.CurrentParseTreeNode.AddToken().grammarNode = this;
			scanner.MoveNext();
			if (scanner.ErrorMessage == null)
			{
				scanner.ErrorParseTreeNode = scanner.CurrentParseTreeNode;
				scanner.ErrorGrammarNode = scanner.CurrentGrammarNode;
			}

			return parent.NextAfterChild(this);
		}

		public override string ToString()
		{
			return body; //  "\"" + body + "\"";
		}

		public override bool IsName(string name)
		{
			return body == name;
		}

		public sealed override void CollectLitAndIdNodes(HashSet<int> lits)
		{
			if (lits.Add(body.GetHashCode()))
				initializingParser.tokens.Add(body.GetString());
		}
	}

	// represents alternatives: node { "|" node }.

	public class Alt : Node
	{
		protected List<Node> nodes = new List<Node>();
		
		public Alt(params Node[] nodes)
		{
			foreach (var node in nodes)
				Add(node);
		}
		
		private bool simpleScan = true;

		public override sealed void Add(Node node)
		{
			var idNode = node as Id;
			if (idNode != null)
			{
				node = idNode.Clone();
			}
			else if (!(node is Token) && !(node is Lit))
			{
				simpleScan = false;
			}

			var altNode = node as Alt;
			if (altNode != null)
			{
				var count = altNode.nodes.Count;
				for (var i = 0; i < count; ++i)
				{
					var n = altNode.nodes[i];
					n.parent = this;
					nodes.Add(n);
				}
			}
			else
			{
				node.parent = this;
				nodes.Add(node);
			}
		}
		
		public override Node GetNode()
		{
			return nodes.Count == 1 ? nodes[0] : this;
		}
		
		// lookahead of each alternative must be
		// different, but more than one alternative
		// with empty input is allowed.
		// returns lookahead - union of alternatives.
		public override TokenSet SetLookahead()
		{
			if (lookahead == null)
			{
				lookahead = new TokenSet();
				for (var i = 0; i < nodes.Count; ++i)
				{
					var t = nodes[i];
					if (t is If)
						continue;
					var set = t.SetLookahead();
					if (lookahead.Accepts(set))
					{
						Debug.LogError(this + ": ambiguous alternatives");
						Debug.LogWarning(lookahead.Intersecton(set).ToString(initializingParser));
					}
					lookahead.Add(set);
				}
				for (var i = 0; i < nodes.Count; ++i)
				{
					var t = nodes[i];
					if (t is If)
					{
						var set = t.SetLookahead();
						lookahead.Add(set);
					}
					if (simpleScan)
					{
						var asId = t as Id;
						if (asId != null && asId.Token == null)
							simpleScan = false;
					}
				}
			}
			return lookahead;
		}
		
		// each alternative gets same succ.

		public override TokenSet SetFollow(TokenSet succ)
		{
			SetLookahead();
			follow = succ;
			var count = nodes.Count;
			for (var i = 0; i < count; ++i)
			{
				var node = nodes[i];
				node.SetFollow(succ);
			}
			return lookahead;
		}

		public override void CheckLL1()
		{
			base.CheckLL1();
			var count = nodes.Count;
			for (var i = 0; i < count; ++i)
			{
				var node = nodes[i];
				node.CheckLL1();
			}
		}
   
		public override bool Scan(IScanner scanner)
		{
			if (scanner.maxScanDistance <= 0)
				return true;
			
			if (simpleScan)
			{
				if (!lookahead.Matches(scanner.Current.tokenId))
					return false;

				scanner.MoveNext();
				return true;
			}
			
			var count = nodes.Count;
			for (var i = 0; i < count; ++i)
			{
				var node = nodes[i];
				if (node.Matches(scanner))
					return node.Scan(scanner);
			}
			
			if (!lookahead.MatchesEmpty())
				return false;
			return true;
		}

		public override Node Parse()
		{
			var count = nodes.Count;
			for (var i = 0; i < count; ++i)
			{
				var node = nodes[i];
				if (node.Matches())
				{
					return node.Parse();
				}
			}
			if (lookahead.MatchesEmpty())
				return NextAfterChild(this);
			
			scanner.SyntaxErrorExpected(lookahead);
			return this;
		}

		public override string ToString()
		{
			var s = new StringBuilder("( " + nodes[0]);
			for (var n = 1; n < nodes.Count; ++n)
				s.Append(" | " + nodes[n]);
			s.Append(" )");
			return s.ToString();
		}
		
		public sealed override void CollectLitAndIdNodes(HashSet<int> lits)
		{
			var count = nodes.Count;
			for (var i = 0; i < count; ++i)
				nodes[i].CollectLitAndIdNodes(lits);
		}
	}

	public class Many : Node
	{
		protected readonly Node node;
		private readonly If ifNode;
  
		public Many(Node node)
		{
			var idNode = node as Id;
			if (idNode != null)
				node = idNode.Clone();

			if (node != null)
				node.parent = this;
			this.node = node;
			this.ifNode = node as If;
		}

		public override Node GetNode()
		{
			if (node is Opt)	// [{ [ n ] }] -> [{ n }]
				return new Many(node.GetNode());
			
			if (node is Many)	// [{ [{ n }] }] -> [{ n }]
				return node;
			
			return this;
		}

		// lookahead includes empty.
		public override TokenSet SetLookahead()
		{
			if (lookahead == null)
			{
				lookahead = new TokenSet(node.SetLookahead());
				lookahead.AddEmpty();
			}
			return lookahead;
		}
		
		// subtree gets succ.

		public override TokenSet SetFollow(TokenSet succ)
		{
			SetLookahead();
			follow = succ;
			if (node != null)
				node.SetFollow(succ);
			return lookahead;
		}
		
		// subtree is checked.
		public override void CheckLL1()
		{
			CachedNextAfterChild(null);

			// trust the predicate!
			//base.CheckLL1(initializingParser);
			if (follow == null)
				throw new Exception(this + ": follow not set");
			node.CheckLL1();
		}

		public override bool Matches()
		{
			return node.Matches(scanner);
		}

		public override bool Matches(IScanner scanner)
		{
			return node.Matches(scanner);
		}

		public override bool Scan(IScanner scanner)
		{
			if (scanner.maxScanDistance <= 0)
				return true;
			
			if (ifNode != null)
			{
				int tokenIndex, line;
				do
				{
					tokenIndex = scanner.CurrentTokenIndex();
					line = scanner.CurrentLine();
					if (!node.Scan(scanner))
						return false;
					if (scanner.maxScanDistance <= 0)
						return true;
				} while (scanner.CurrentTokenIndex() != tokenIndex || scanner.CurrentLine() != line);
			}
			else
			{
				while (lookahead.Matches(scanner.Current.tokenId))
				{
					int tokenIndex = scanner.CurrentTokenIndex();
					int line = scanner.CurrentLine();
					if (!node.Scan(scanner))
						return false;
					if (scanner.maxScanDistance <= 0)
						return true;
					if (scanner.CurrentTokenIndex() == tokenIndex && scanner.CurrentLine() == line)
						throw new Exception("Infinite loop!!!");
				}
			}
			return true;
		}

		public override Node CachedNextAfterChild(Node child)
		{
			cachedNext = this;
			return this;
		}

		public override Node NextAfterChild(Node child)
		{
			return this;
		}

		public override Node NextAfterChild(Node child, IScanner scanner)
		{
			return this;
		}

		public override Node Parse()
		{
			if (!lookahead.Matches(scanner.Current.tokenId))
				return parent.NextAfterChild(this);

			//var ifNode = node as If;
			if (ifNode == null || ifNode.CheckPredicate(scanner))
			{
				var currentToken = scanner.Current;
				var nextNode = node.Parse();
				if (nextNode != this || currentToken != scanner.Current)
					return nextNode;
				//Debug.Log("Exiting Many " + this + " in goal: " + scanner.CurrentParseTreeNode);
			}
			return parent.NextAfterChild(this);
		}

		public override String ToString()
		{
			return "[{ "+node+" }]";
		}
		
		public sealed override void CollectLitAndIdNodes(HashSet<int> lits)
		{
			node.CollectLitAndIdNodes(lits);
		}
	}

	//protected class Some : Many
	//{
	//    public Some(Node node)
	//        : base(node)
	//    {
	//    }

	//    public override Node GetNode()
	//    {
	//        if (node is Some)			// { { n } } -> { n }
	//            return node;
			
	//        if (node is Opt)		// { [ n ] } -> [{ n }]
	//            return new Many(node.GetNode());
			
	//        if (node is Many)		// { [{ n }] } -> [{ n }]
	//            return node;
			
	//        return this;
	//    }
		
	//    // lookahead results from subtree.
	//    public override TokenSet SetLookahead()
	//    {
	//        if (lookahead == null)
	//            lookahead = node.SetLookahead();
			
	//        return lookahead;
	//    }
  		
	//    // lookahead != follow; check subtree.
	//    public override void CheckLL1()
	//    {
	//        if (follow == null)
	//            throw new Exception(this + ": follow not set");
	//        if (lookahead.Accepts(follow))
	//            throw new Exception(this + ": ambiguous\n"
	//                + "  lookahead " + lookahead.ToString(initializingParser) + "\n"
	//                + "  follow " + follow.ToString(initializingParser));
	//        node.CheckLL1();
	//    }

	//    public override void Parse(IScanner scanner, Goal goal)
	//    {
	//        if (lookahead.Matches(scanner.Current.tokenId))
	//            do
	//                node.Parse(scanner, goal);
	//            while (lookahead.Matches(scanner.Current.tokenId));
	//        else if (!lookahead.MatchesEmpty())
	//            throw new Exception(scanner + ": syntax error in Some");
	//    }

	//    public override string ToString()
	//    {
	//        return "{ " + node + " }";
	//    }
	//}

	protected class Opt : Many
	{
		public Opt(Node node)
			: base(node)
		{
		}

		public override Node GetNode()
		{
		//	if (node is Some)	// [ { n } ] -> [{ n }]
		//		return new Many(node.GetNode());
			
			if (node is Opt)	// [ [ n ] ] -> [ n ]
				return node;
			
			if (node is Many)	// [ [{ n }] ] -> [{ n }]
				return node;
			
			return this;
		}
  
		public override bool Scan(IScanner scanner)
		{
			if (scanner.maxScanDistance <= 0)
				return true;
			
			if (node != null && lookahead.Matches(scanner.Current.tokenId))
				return node.Scan(scanner);
			return true;
		}

		public override Node Parse()
		{
			//try
			//{
				if (lookahead.Matches(scanner.Current.tokenId))
					return node.Parse();
			//}
			//catch
			//{
			//	Debug.Log(lookahead);
			//}
			return parent.NextAfterChild(this);
		}
		
		public override Node CachedNextAfterChild(Node child)
		{
			cachedNext = cachedNext ?? parent.CachedNextAfterChild(this);
			return cachedNext;
		}

		public override Node NextAfterChild(Node child)
		{
			return cachedNext ?? parent.NextAfterChild(this);
		}

		public override Node NextAfterChild(Node child, IScanner scanner)
		{
			return cachedNext ?? parent.NextAfterChild(this, scanner);
		}

		public override string ToString()
		{
			return "[ " + (node != null ? node : "null") + " ]";
		}
	}
	
	protected class Ifs : If
	{
		protected readonly string currentTokenText;

		public Ifs(string currentText, Predicate<IScanner> pred, Node node, bool debug = false)
			: base(pred, node, debug)
		{
			currentTokenText = currentText;
		}

		public Ifs(string currentText, Node pred, Node node, bool debug = false)
			: base(pred, node, debug)
		{
			currentTokenText = currentText;
		}

		public Ifs(string currentText, Node node, bool debug = false)
			: base((Node) null, node, debug)
		{
			currentTokenText = currentText;
		}

		public override bool CheckPredicate(IScanner scanner)
		{
			//if (debug)
			//{
			//    var s = scanner.Clone();
			//    Debug.Log(s.Current.tokenKind + " " + s.Current.text);
			//    s.MoveNext();
			//    Debug.Log(s.Current.tokenKind + " " + s.Current.text);
			//}
			if (scanner.Current.text != currentTokenText)
			{
				return false;
			}
			else if (predicate != null)
			{
				return predicate(scanner);
			}
			else if (nodePredicate != null)
			{
				return scanner.Lookahead(nodePredicate);
			}
			else
			{
				return true;
			}
		}
	}

	protected class Ifs2 : If
	{
		protected readonly string currentTokenText1;
		protected readonly string currentTokenText2;

		public Ifs2(string currentText1, string currentText2, Predicate<IScanner> pred, Node node, bool debug = false)
			: base(pred, node, debug)
		{
			currentTokenText1 = currentText1;
			currentTokenText2 = currentText2;
		}

		public Ifs2(string currentText1, string currentText2, Node pred, Node node, bool debug = false)
			: base(pred, node, debug)
		{
			currentTokenText1 = currentText1;
			currentTokenText2 = currentText2;
		}

		public Ifs2(string currentText1, string currentText2, Node node, bool debug = false)
			: base((Node) null, node, debug)
		{
			currentTokenText1 = currentText1;
			currentTokenText2 = currentText2;
		}

		public override bool CheckPredicate(IScanner scanner)
		{
			var text = scanner.Current.text;
			if (text != currentTokenText1 && text != currentTokenText2)
			{
				return false;
			}
			else if (predicate != null)
			{
				return predicate(scanner);
			}
			else if (nodePredicate != null)
			{
				return scanner.Lookahead(nodePredicate);
			}
			else
			{
				return true;
			}
		}
	}

	protected class If : Opt
	{
		protected readonly Predicate<IScanner> predicate;
		protected readonly Node nodePredicate;
		protected readonly bool debug;

		public If(Predicate<IScanner> pred, Node node, bool debug = false)
			: base(node)
		{
			predicate = pred;
			this.debug = debug;
		}

		public If(Node pred, Node node, bool debug = false)
			: base(node)
		{
			nodePredicate = pred;
			this.debug = debug;
		}

		public override Node GetNode()
		{
		//    if (node is Some)	// [ { n } ] -> [{ n }]
		//        return new Many(node.GetNode());

		//    if (node is Opt)	// [ [ n ] ] -> [ n ]
		//        return node;

		//    if (node is Many)	// [ [{ n }] ] -> [{ n }]
		//        return node;

			return this;
		}

		public virtual bool CheckPredicate(IScanner scanner)
		{
			//if (debug)
			//{
			//    var s = scanner.Clone();
			//    Debug.Log(s.Current.tokenKind + " " + s.Current.text);
			//    s.MoveNext();
			//    Debug.Log(s.Current.tokenKind + " " + s.Current.text);
			//}
			if (predicate != null)
			{
				return predicate(scanner);
			}
			else if (nodePredicate != null)
			{
				return scanner.Lookahead(nodePredicate);
			}
			else
			{
				return false;
			}
		}

		public override bool Matches()
		{
			return lookahead.Matches(scanner.Current.tokenId) && CheckPredicate(scanner);
		}

		public override bool Matches(IScanner scanner)
		{
			return lookahead.Matches(scanner.Current.tokenId) && CheckPredicate(scanner);
		}

		public override bool Scan(IScanner scanner)
		{
			if (scanner.maxScanDistance <= 0)
				return true;

			if (lookahead.Matches(scanner.Current.tokenId) && CheckPredicate(scanner))
				return node.Scan(scanner);
			return true;
		}

		public override Node Parse()
		{
			if (lookahead.Matches(scanner.Current.tokenId) && CheckPredicate(scanner))
				return node.Parse();
			else
				return parent.NextAfterChild(this);
		}

		// lookahead doesn't include empty.

		public override TokenSet SetLookahead()
		{
			if (lookahead == null)
				lookahead = node != null ? new TokenSet(node.SetLookahead()) : new TokenSet();
			return lookahead;
		}

		public override TokenSet SetFollow(TokenSet succ)
		{
			if (nodePredicate != null && nodePredicate.follow == null)
				nodePredicate.SetFollow(new TokenSet());
			return base.SetFollow(succ);
		}

		public override string ToString()
		{
			return "[ ?(" + predicate + ") " + node + " ]";
		}
	}

	protected class IfNot : If
	{
		//public IfNot(Predicate<IScanner> pred, Node node)
		//    : base(pred, node)
		//{
		//}

		public IfNot(Node pred, Node node)
			: base(pred, node)
		{
		}

		public override bool CheckPredicate(IScanner scanner)
		{
			return !base.CheckPredicate(scanner);
		}
	}

	public class Seq : Node
	{
		public readonly List<Node> nodes = new List<Node>();
		public bool hasBraces;
		//private readonly int debugLine = -1;

		public Seq(Node node1, Node node2)
		{
			Add(node1);
			Add(node2);
		}

		public Seq(Node node1, Node node2, Node node3)
		{
			Add(node1);
			Add(node2);
			Add(node3);
		}

		public Seq(Node node1, Node node2, Node node3, Node node4)
		{
			Add(node1);
			Add(node2);
			Add(node3);
			Add(node4);
		}

		public Seq(Node node1, Node node2, Node node3, Node node4, Node node5)
		{
			Add(node1);
			Add(node2);
			Add(node3);
			Add(node4);
			Add(node5);
		}

		public Seq(Node node1, Node node2, Node node3, Node node4, Node node5,
			Node node6)
		{
			Add(node1);
			Add(node2);
			Add(node3);
			Add(node4);
			Add(node5);
			Add(node6);
		}

		public Seq(Node node1, Node node2, Node node3, Node node4, Node node5,
			Node node6, Node node7)
		{
			Add(node1);
			Add(node2);
			Add(node3);
			Add(node4);
			Add(node5);
			Add(node6);
			Add(node7);
		}

		public Seq(Node node1, Node node2, Node node3, Node node4, Node node5,
			Node node6, Node node7, Node node8)
		{
			Add(node1);
			Add(node2);
			Add(node3);
			Add(node4);
			Add(node5);
			Add(node6);
			Add(node7);
			Add(node8);
		}

		public Seq(Node node1, Node node2, Node node3, Node node4, Node node5,
			Node node6, Node node7, Node node8, Node node9)
		{
			Add(node1);
			Add(node2);
			Add(node3);
			Add(node4);
			Add(node5);
			Add(node6);
			Add(node7);
			Add(node8);
			Add(node9);
		}

		public Seq(Node node1, Node node2, Node node3, Node node4, Node node5,
			Node node6, Node node7, Node node8, Node node9, Node node10)
		{
			Add(node1);
			Add(node2);
			Add(node3);
			Add(node4);
			Add(node5);
			Add(node6);
			Add(node7);
			Add(node8);
			Add(node9);
			Add(node10);
		}

		//public Seq(params Node[] nodes)
		//{
		//	foreach (var t in nodes)
		//		Add(t);
		//}

		//public Seq(int debugLine, params Node[] nodes)
		//{
		//	//this.debugLine = debugLine;
		//	foreach (var t in nodes)
		//		Add(t);
		//}

		public override sealed void Add(Node node)
		{
			var idNode = node as Id;
			if (idNode != null)
				node = idNode.Clone();

			var seqNode = node as Seq;
			if (seqNode != null)
				for (var i = 0; i < seqNode.nodes.Count; ++i)
					Add(seqNode.nodes[i]);
			else
			{
				node.parent = this;
				node.childIndex = nodes.Count;
				nodes.Add(node);
				
				if (!hasBraces)
				{
					var litNode = node as Lit;
					if (litNode != null && litNode.body.length == 1)
					{
						var c = litNode.body[0];
						hasBraces = c == '(' || c == ')' || c == '{' || c == '}' || c == '[' || c == ']';
					}
				}
			}
		}
		
		// @return nodes[0] if only one.
		public override Node GetNode()
		{
			return nodes.Count == 1 ? nodes[0] : this;
		}
		
		// lookahead is union including first element
		// that does not accept empty input; it
		// includes empty input only if there is
		// no such element.
		public override TokenSet SetLookahead()
		{
			if (lookahead == null)
			{
				lookahead = new TokenSet();
				if (nodes.Count == 0)
					lookahead.AddEmpty();
				else
					for (int i = 0; i < nodes.Count; ++i)
					{
						var t = nodes[i];
						var set = t.SetLookahead();
						lookahead.Add(set);
						if (!set.MatchesEmpty())
						{
							lookahead.RemoveEmpty();
							//for (int j = i + 1; j < nodes.Count; ++j)
							//	nodes[j].SetLookahead();
							break;
						}
					}
			}
			return lookahead;
		}
		
		// each element gets successor's lookahead.
		
		public override TokenSet SetFollow(TokenSet succ)
		{
			SetLookahead();
			follow = succ;
			for (var n = nodes.Count; n-- > 0; )
			{
				var prev = nodes[n].SetFollow(succ);
				if (prev.MatchesEmpty())
				{
					prev = new TokenSet(prev);
					prev.RemoveEmpty();
					prev.Add(succ);
				}
				succ = prev;
			}
			return lookahead;
		}

		public override void CheckLL1()
		{
			base.CheckLL1();
			var count = nodes.Count;
			for (var i = 0; i < count; ++i)
			{
				var t = nodes[i];
				t.CheckLL1();
			}
		}

		public override bool Scan(IScanner scanner)
		{
			var count = nodes.Count;
			for (var i = 0; i < count; ++i)
			{
				var t = nodes[i];
				if (scanner.maxScanDistance <= 0)
					return true;
				if (!t.Scan(scanner))
					return false;
			}
			return true;
		}

		public override Node CachedNextAfterChild(Node child)
		{
			if (child != null)
			{
				var index = child.childIndex;
				if (++index < nodes.Count)
					return nodes[index];
			}

			return base.CachedNextAfterChild(null);
		}

		public override Node NextAfterChild(Node child)
		{
			var index = child.childIndex;
			if (++index < nodes.Count)
				return nodes[index];
			return cachedNext ?? parent.NextAfterChild(this);
		}

		public override Node NextAfterChild(Node child, IScanner scanner)
		{
			var index = child.childIndex;
			if (++index < nodes.Count)
				return nodes[index];
			return cachedNext ?? parent.NextAfterChild(this, scanner);
		}

		public override Node Parse()
		{
			return nodes[0].Parse();
		}

		public override string ToString()
		{
			var s = new StringBuilder("( ");
			foreach (var t in nodes)
				s.Append(" " + t);
			s.Append(" )");
			return s.ToString();
		}
		
		public sealed override void CollectLitAndIdNodes(HashSet<int> lits)
		{
			var count = nodes.Count;
			for (var i = 0; i < count; ++i)
				nodes[i].CollectLitAndIdNodes(lits);
		}
	}

	public class Token : Node
	{
		protected string name;

		public Token(string name, TokenSet lookahead)
		{
			this.name = name;
			this.lookahead = lookahead;
		}

		public Token Clone()
		{
			var clone = new Token(name, lookahead);
			return clone;
		}
		
		// returns a one-element set,
		// initialized by the parser.
		public override TokenSet SetLookahead()
		{
			return lookahead;
		}
		
		// follow doesn't need to be set.
		public override TokenSet SetFollow(TokenSet succ)
		{
			return lookahead;
		}
		
		// follow is not set; nothing to check.
		public override void CheckLL1()
		{
			CachedNextAfterChild(null);
		}
  
		public override bool Scan(IScanner scanner)
		{
			if (scanner.maxScanDistance <= 0)
				return true;
			
			if (!lookahead.Matches(scanner.Current.tokenId))
				return false;
			scanner.MoveNext();
			return true;
		}

		public override Node Parse()
		{
			if (!lookahead.Matches(scanner.Current.tokenId))
			{
				scanner.SyntaxErrorExpected(lookahead);
				return this;
			}

			scanner.CurrentParseTreeNode.AddToken().grammarNode = this;
			scanner.MoveNext();
			if (scanner.ErrorMessage == null)
			{
				scanner.ErrorParseTreeNode = scanner.CurrentParseTreeNode;
				scanner.ErrorGrammarNode = scanner.CurrentGrammarNode;
			}

			return parent.NextAfterChild(this);
		}

		public override string ToString()
		{
			return name;
		}

		public override bool IsName(string name)
		{
			return this.name == name;
		}
	}

	public class Parser : Node
	{
		private readonly List<Rule> rules = new List<Rule>();
		public Rule Start { get { return rules[0]; } }

		// maps Rule.nt hash code to rule.
		public readonly Dictionary<int, Rule> nts = new Dictionary<int, Rule>(BitArray.Length);

		// maps Id.name hash code to Token or Rule; kept for scanner.
		public Dictionary<int, Node> ids = new Dictionary<int, Node>(BitArray.Length);

		// maps token index to token name;
		public List<string> tokens;

		protected Dictionary<int, short> tokenToID = new Dictionary<int, short>(BitArray.Length);

		public Parser(Rule start, FGGrammar grammar)
		{
		//	Grammar = grammar;
			rules.Add(start);
		}

		// adds each rule.
		public void Add(Rule rule)
		{
			CharSpan nt = rule.GetNt();
			var hashCode = nt.GetHashCode();
			if (nts.ContainsKey(hashCode))
				throw new Exception(nt + ": duplicate");
			nts.Add(hashCode, rule);
			rules.Add(rule);
		}
		
		public void Add(Id nt, Node rhs)
		{
			Add(new Rule(nt, rhs));
		}

		public void InitializeGrammar()
		{
			initializingParser = this;
			
			// maps Lit.body to set containing token
			var lits = new HashSet<int>(/*BitArray.Length*/);
			tokens = new List<String>(BitArray.Length);

			CollectLitAndIdNodes(lits);
			
			tokens.Sort(System.StringComparer.Ordinal);
			
			TokenSet idLookahead = null;

			var nameHashCode = new CharSpan("NAME").GetHashCode();

			for (short i = 0; i < tokens.Count; ++i)
			{
				string name = tokens[i];
				var hashCode = new CharSpan(name).GetHashCode();
				
				tokenToID[hashCode] = i;
				
				if (!lits.Contains(hashCode) && ids[hashCode] == null)
				{
					var newToken = new Token(name, new TokenSet(i));
					ids[hashCode] = newToken;

					if (hashCode == nameHashCode)
					{
						if (idLookahead == null)
							idLookahead = ids[new CharSpan("IDENTIFIER").GetHashCode()].lookahead;
						newToken.lookahead.Add(idLookahead);
					}
				}
			}

			initializingParser = this;
			SetLookahead();
			SetFollow(null);
			CheckLL1();
			
			initializingParser = null;

			//var sb = new StringBuilder();
			//foreach (var rule in rules)
			//    if (rule.lookahead.Matches(CsGrammar.Instance.tokenIdentifier))
			//        sb.AppendLine("Lookahead(" + rule.GetNt() + "): " + rule.lookahead.ToString(this));
			//UnityEngine.Debug.Log(sb.ToString());
		}
		
		public override TokenSet SetLookahead()
		{
			var count = rules.Count;
			for (var i = 0; i < count; ++i)
			{
				var rule = rules[i];
				rule.SetLookahead();
			}
			return null;
		}

		public override TokenSet SetFollow(TokenSet succ)
		{
			Start.SetFollow();
			bool followChanged;
			do
			{
				var count = rules.Count;
				for (var i = 0; i < count; ++i)
					rules[i].SetFollowFromParser(this);
				followChanged = false;
				count = rules.Count;
				for (var i = 0; i < count; ++i)
					followChanged |= rules[i].FollowChanged();
			} while (followChanged);
			return null;
		}

		public override void CheckLL1()
		{
			var count = rules.Count;
			for (var i = 0; i < count; ++i)
				rules[i].CheckLL1();
		}

		public override bool Scan(IScanner scanner)
		{
			throw new InvalidOperationException();
		}

		public override Node Parse()
		{
			throw new InvalidOperationException();
		}

		public ParseTree ParseAll(IScanner scanner)
		{
			//if (!scanner.MoveNext())
			//	return null;
			scanner.MoveNext();
			
			var oldScanner = Node.scanner;
			Node.scanner = scanner;

			var parseTree = new ParseTree();
			var rootId = new Id(Start.GetNt());
			ids[new CharSpan(Start.GetNt()).GetHashCode()] = Start;
			initializingParser = this;
			rootId.SetLookahead();
			initializingParser = null;
			Start.parent = rootId;
			scanner.CurrentParseTreeNode = parseTree.root = ParseTree.Node.Create(rootId);
			scanner.CurrentGrammarNode = Start.Parse();

			scanner.ErrorParseTreeNode = scanner.CurrentParseTreeNode;
			scanner.ErrorGrammarNode = scanner.CurrentGrammarNode;

			while (scanner.CurrentGrammarNode != null)
			{
				var line = scanner.CurrentLine();
				var tokenIndex = scanner.CurrentTokenIndex();
				var rule = scanner.CurrentGrammarNode;
				//var node = scanner.CurrentParseTreeNode;

				if (!ParseStep(scanner))
					break;
				
				if (scanner.ErrorMessage == null)
				{
					if (/*scanner.CurrentParseTreeNode == node &&*/ scanner.CurrentGrammarNode == rule && scanner.CurrentTokenIndex() == tokenIndex && scanner.CurrentLine() == line)
					{
						tryToRecover = false;
						//Debug.LogError("Cannot continue parsing - stuck at line " + line + ", token index " + tokenIndex);
						//break;
					}
				}
			}

			//if (scanner.MoveNext())
			//	Debug.LogWarning(scanner + ": trash at end");
			Node.scanner = oldScanner;
			return parseTree;
		}

		public bool tryToRecover = true;

		public bool ParseStep(IScanner scanner)
		{
			Node.scanner = scanner;
			
//			if (scanner.ErrorMessage == null && scanner.ErrorParseTreeNode == null)
//			{
//				scanner.ErrorParseTreeNode = scanner.CurrentParseTreeNode;
//				scanner.ErrorGrammarNode = scanner.CurrentGrammarNode;
//			}

			//scanner.CurrentParseTreeNode.AddToken(scanner).grammarNode = this;
			//scanner.MoveNext();

			//return parent.NextAfterChild(this, scanner);

			if (scanner.CurrentGrammarNode == null)
				return false;

			//var errorGrammarNode = scanner.CurrentGrammarNode;
			//var errorParseTreeNode = scanner.CurrentParseTreeNode;

			//var numValidNodes = scanner.CurrentParseTreeNode.numValidNodes;
			
			var token = scanner.Current;
			if (scanner.ErrorMessage == null)
			{
				while (scanner.CurrentGrammarNode != null)
				{
					scanner.CurrentGrammarNode = scanner.CurrentGrammarNode.Parse();
					if (scanner.ErrorMessage != null || !ReferenceEquals(token, scanner.Current))
						break;
				}

				//if (scanner.CurrentGrammarNode == null)
				//{
				//	Debug.LogError("scanner.CurrentGrammarNode == null");
				//	return false;
				//}

				//if (scanner.ErrorMessage != null)
				//{
				//    Debug.Log("ErrorGrammarNode: " + scanner.ErrorGrammarNode +
				//        "\nErrorParseTreeNode: " + scanner.ErrorParseTreeNode);
				//}

				if (scanner.ErrorMessage == null && token != scanner.Current)
				{
					scanner.ErrorParseTreeNode = scanner.CurrentParseTreeNode;
					scanner.ErrorGrammarNode = scanner.CurrentGrammarNode;
				}
			}
			
			if (scanner.ErrorMessage == null)
			{
				return true;
			}
			
			if (token.tokenKind == SyntaxToken.Kind.EOF)
			{
			//	Debug.LogError("Unexpected end of file in ParseStep");
				return false;
			}

			var missingParseTreeNode = scanner.CurrentParseTreeNode;
			var missingGrammarNode = scanner.CurrentGrammarNode;

			// Rolling back all recent parser state changes
			scanner.CurrentParseTreeNode = scanner.ErrorParseTreeNode;
			scanner.CurrentGrammarNode = scanner.ErrorGrammarNode;
			if (scanner.CurrentParseTreeNode != null)
			{
				var cpt = scanner.CurrentParseTreeNode;
				while (cpt.lastValid != null && !cpt.lastValid.HasLeafs())
					cpt.InvalidateFrom(cpt.lastValid.childIndex);
			}

			if (!tryToRecover)
			{
				tryToRecover = true;
				scanner.CurrentGrammarNode = null;
			}
			else if (scanner.CurrentGrammarNode != null)
			{
				int numSkipped;
				scanner.CurrentGrammarNode = scanner.CurrentGrammarNode.Recover(out numSkipped);
			}

			if (scanner.CurrentGrammarNode == null)
			{
				if (token.parent != null)
					token.parent.ReparseToken();
//					if (scanner.ErrorToken == null)
//						scanner.ErrorToken = scanner.ErrorParseTreeNode.AddToken(scanner);
//					else
//						scanner.ErrorParseTreeNode.AddToken(scanner);
				var temp = new ParseTree.Leaf(scanner);

				if (cachedErrorGrammarNode == scanner.ErrorGrammarNode)
				{
					token.parent.syntaxError = cachedErrorMessage;
				}
				else
				{
					token.parent.syntaxError = new UnexpectedTokenErrorMessage(this, scanner.ErrorGrammarNode.lookahead);
					cachedErrorMessage = token.parent.syntaxError;
					cachedErrorGrammarNode = scanner.ErrorGrammarNode;
				}
			//	scanner.ErrorMessage = cachedErrorMessage;
			//	Debug.LogError("Skipped " + token + "added to " + token.parent + "\nparent: " + token.parent.parent);


				scanner.CurrentGrammarNode = scanner.ErrorGrammarNode;
				scanner.CurrentParseTreeNode = scanner.ErrorParseTreeNode;

			//	token = scanner.Current;
			//	token.parent = errorParseTreeNode;

				//Debug.Log("Skipping " + scanner.Current.tokenKind + " \"" + scanner.Current.text + "\"");
				if (!scanner.MoveNext())
				{
				//	Debug.LogError("Unexpected end of file");
					return false;
				}
				scanner.ErrorMessage = null;
			}
			else
			{
				//var sb = new StringBuilder();
				//scanner.CurrentParseTreeNode.Dump(sb, 0);
				//Debug.Log("Recovered on " + scanner.CurrentGrammarNode + " (current token: " + scanner.Current +
				//	" at line " + (scanner.CurrentLine() + 1) + ":" + scanner.CurrentTokenIndex() +
				//	")" + //"\nnumSkipped: " + numSkipped +
				//	"\nin parent: " + scanner.CurrentGrammarNode.parent +
				//	"\nCurrentParseTreeNode is:\n" + sb);

				//var n = scanner.ErrorGrammarNode;
				//while (n != null && !(n is Id))
				//    n = n.parent;
				//scanner.ErrorParseTreeNode.errors = scanner.ErrorParseTreeNode.errors ?? new List<string>();
				//scanner.ErrorParseTreeNode.errors.Add("Not a valid " + n + "! Expected " + scanner.ErrorGrammarNode.lookahead.ToString(this));

				if (missingGrammarNode != null && missingParseTreeNode != null)
				{
					scanner.CurrentParseTreeNode = missingParseTreeNode;
					scanner.CurrentGrammarNode = missingGrammarNode;
				}

				scanner.InsertMissingToken(scanner.ErrorMessage
					?? new MissingTokenErrorMessage(this, missingGrammarNode.lookahead));

				if (missingGrammarNode != null && missingParseTreeNode != null)
				{
					scanner.ErrorMessage = null;
					scanner.ErrorToken = null;
					scanner.CurrentParseTreeNode = missingParseTreeNode;
					scanner.CurrentGrammarNode = missingGrammarNode;
					scanner.CurrentGrammarNode = missingGrammarNode.parent.NextAfterChild(missingGrammarNode);
				}

				scanner.ErrorMessage = null;
				scanner.ErrorToken = null;
			}

			return true;
		}

		private static ErrorMessageProvider cachedErrorMessage;
		private static Node cachedErrorGrammarNode;

		public override string ToString()
		{
			var s = new StringBuilder(GetType().Name + " {\n");
			foreach (var rule in rules)
				s.AppendLine(rule.ToString(this));
			s.Append("}");
			return s.ToString();
		}

		public short TokenToId(CharSpan s)
		{
			short id = -1;
			tokenToID.TryGetValue(s.GetHashCode(), out id);
			return id;
		}

		public string GetToken(int tokenId)
		{
			return tokenId >= 0 && tokenId < tokens.Count ? tokens[tokenId] : tokenId + "?";
		}

		// returns Token or Rule for Id.
		public Node GetPeer(CharSpan name)
		{
			try
			{
				var peer = ids[name.GetHashCode()];
				var token = peer as Token;
				if (token != null)
					peer = token.Clone();
				return peer;
			}
			catch
			{
				Debug.Log(name);
				throw;
			}
		}
		
		public sealed override void CollectLitAndIdNodes(HashSet<int> lits)
		{
			var count = rules.Count;
			for (var i = 0; i < count; ++i)
				rules[i].CollectLitAndIdNodes(lits);
		}
	}

	public class Rule : Node
	{
		public static bool debug;
		public SemanticFlags semantics;
		public bool autoExclude;
		public bool contextualKeyword;

		// nonterminal name.
		protected string nonTerminal;

		// right hand side subtree.
		protected Node rhs;
		
		public Rule(Id nt, Node rhs)
			: this(nt.GetName(), rhs)
		{
		}

		public Rule(string nonTerminal, Node rhs)
		{
			var idNode = rhs as Id;
			if (idNode != null)
				rhs = idNode.Clone();

			this.nonTerminal = nonTerminal;

			rhs.parent = this;
			this.rhs = rhs;
		}
		
		// used to detect left recursion and to
		// flag if follow needs to be recomputed.
		protected bool inProgress;
				
		// gets lookahead from rhs, sets inProgress.
		public override TokenSet SetLookahead()
		{
			if (lookahead == null)
			{
				if (inProgress)
					throw new Exception(nonTerminal + ": recursive lookahead");
				inProgress = true;
				lookahead = rhs.SetLookahead();

			}
			return lookahead;
		}
		
		// set if follow has changed.
		protected bool followChanged;

		// resets before recomputing follow.
		public bool FollowChanged()
		{
			inProgress = followChanged;
			followChanged = false;
			return inProgress;
		}
		
		// initializes follow with an empty set.
		// used only once, only for the start rule.
		public void SetFollow()
		{
			follow = new TokenSet();
		}

		// traverses rhs;
		// should reach all rules from start rule.
		public void SetFollowFromParser(Parser parser)
		{
			if (lookahead == null)
				throw new Exception(nonTerminal + ": lookahead not set");
			if (follow == null)
				return;// throw new Exception(nt + ": not connected");
			if (inProgress)
				rhs.SetFollow(follow);
		}
		
		// sets or adds to (new) follow set
		// and reports changes to parser.
		// returns lookahead.
		public override TokenSet SetFollow(TokenSet succ)
		{
			if (follow == null)
			{
				followChanged = true;
				follow = new TokenSet(succ);

			}
			else if (follow.Add(succ))
			{
				followChanged = true;
			}
			return lookahead;
		}
		
		public override void CheckLL1()
		{
			if (!contextualKeyword)
			{
				base.CheckLL1();
				rhs.CheckLL1();
			}
		}

		public override bool Scan(IScanner scanner)
		{
			if (scanner.maxScanDistance <= 0)
				return true;
			
			if (lookahead.Matches(scanner.Current.tokenId))
				return rhs.Scan(scanner);
			
			return lookahead.MatchesEmpty();
		}

		public override Node Parse()
		{
			bool skip;
			scanner.CurrentParseTreeNode = scanner.CurrentParseTreeNode.AddNode((Id)parent, scanner, out skip);
			if (skip)
				return scanner.CurrentGrammarNode;

			return RhsParse2();
		}

		public override Node CachedNextAfterChild(Node child)
		{
			return null;
		}

		public override Node NextAfterChild(Node child)
		{
			var temp = scanner.CurrentParseTreeNode;
			if (temp == null)
				return null;
			var res = /*temp.grammarNode == null ? null :*/ temp.grammarNode.NextAfterChild(this);

			if (scanner.Seeking)
				return res;

			if (contextualKeyword && temp.numValidNodes == 1)
			{
				var token = temp.LeafAt(0).token;
				token.tokenKind = SyntaxToken.Kind.ContextualKeyword;
			}

			/*if (autoExclude && temp.numValidNodes == 1)
			{
				temp.Exclude();
			}
			else*/ if (temp.semantics != SemanticFlags.None)
			{
				scanner.OnReduceSemanticNode(temp);
			}

			return res;
		}

		public override Node NextAfterChild(Node child, IScanner scanner)
		{
			var temp = scanner.CurrentParseTreeNode;
			if (temp == null)
				return null;
			var res = /*temp.grammarNode == null ? null :*/ temp.grammarNode.NextAfterChild(this, scanner);

			if (scanner.Seeking)
				return res;

			if (contextualKeyword && temp.numValidNodes == 1)
			{
				var token = temp.LeafAt(0).token;
				token.tokenKind = SyntaxToken.Kind.ContextualKeyword;
			}

			/*if (autoExclude && temp.numValidNodes == 1)
			{
				temp.Exclude();
			}
			else*/ if (temp.semantics != SemanticFlags.None)
			{
				scanner.OnReduceSemanticNode(temp);
			}

			return res;
		}

		private Node RhsParse2()
		{
			bool wasError = scanner.ErrorMessage != null;
			Node res = null;
			if (lookahead.Matches(scanner.Current.tokenId))
			{
				//try
				//{
					res = rhs.Parse();
				//}
				//catch (Exception e)
				//{
				//	throw new Exception(e.Message + "<=" + this.nt, e);
				//}
			}
			if ((res == null || !wasError && scanner.ErrorMessage != null) && !lookahead.MatchesEmpty())
			{
				scanner.SyntaxErrorExpected(lookahead);
				return res ?? this;
			}
			if (res != null)
				return res;

			return NextAfterChild(rhs); // ready to be reduced
		}

		public override string ToString()
		{
			return nonTerminal + " : " + rhs + " .";
		}

		public string ToString(Parser parser)
		{
			var result = new StringBuilder(nonTerminal + " : " + rhs + " .");
			if (lookahead != null)
				result.Append("\n  lookahead " + lookahead.ToString(parser));
			if (follow != null)
				result.Append("\n  follow " + follow.ToString(parser));
			return result.ToString();
		}

		public string GetNt()
		{
			return nonTerminal;
		}
		
		public sealed override void CollectLitAndIdNodes(HashSet<int> lits)
		{
			rhs.CollectLitAndIdNodes(lits);
		}
	}

	public struct BitArray
	{
		const int arraySize = 32;
		public const int Length = arraySize * 32;
		
		public uint[] bits;
		
		public BitArray(BitArray other)
		{
			if (other.bits == null)
			{
				bits = null;
				return;
			}
			
			bits = new uint[arraySize];
			for (var i = 0; i < arraySize; ++i)
				bits[i] = other.bits[i];
		}
		
		public bool IsEmpty { get { return bits == null; } }
		
		public bool Add(BitArray other)
		{
			if (other.bits == null)
				return false;
			
			if (bits == null)
				bits = new uint[arraySize];
			
			var modified = false;
			for (var i = 0; i < arraySize; ++i)
			{
				var src = other.bits[i];
				var dst = bits[i];
				var newValue = dst | src;
				modified = modified || (newValue != dst);
				bits[i] = newValue;
			}
			return modified;
		}
		
		public bool ContainsAny(BitArray other)
		{
			if (bits == null || other.bits == null)
				return false;
			
			for (var i = 0; i < arraySize; ++i)
			{
				var src = other.bits[i];
				var dst = bits[i];
				if ((dst & src) != 0 )
					return true;
			}
			return false;
		}
		
		public void And(BitArray other)
		{
			if (other.bits == null)
				return;
			
			if (bits == null)
				bits = new uint[arraySize];
		
			for (var i = 0; i < arraySize; ++i)
			{
				var src = other.bits[i];
				var dst = bits[i];
				bits[i] = dst & src;
			}
		}
		
		public bool this[int index]
		{
			get
			{
				if (bits == null)
					return false;
				return (bits[index >> 5] & (1u << (index & 31))) != 0;
			}
			set
			{
				if (bits == null)
					bits = new uint[arraySize];
				if (value)
					bits[index >> 5] |= 1u << (index & 31);
				else
					bits[index >> 5] &= ~(1u << (index & 31));
			}
		}
	}

	public class TokenSet
	{
		// true if empty input is acceptable.
		protected bool empty;

		// else if >= 0: single element.
		private short tokenId = -1;
  
		// if !set.isEmpty: many elements.
		private BitArray set;

		public int GetDataSet(out BitArray bitArray)
		{
			bitArray = set;
			return tokenId;
		}

		// empty set, doesn't accept even empty input.
		public TokenSet() {}

		public TokenSet(short tokenId)
		{
			this.tokenId = tokenId;
		}

		public TokenSet(TokenSet s)
		{
			empty = s.empty;
			if (!s.set.IsEmpty)
				set = new BitArray(s.set);
			else
				tokenId = s.tokenId;
		}

		public TokenSet(BitArray set)
		{
			this.set = set;
		}

		public void AddEmpty()
		{
			empty = true;
		}

		public void RemoveEmpty()
		{
			empty = false;
		}

		public bool Remove(short token)
		{
			if (set.IsEmpty)
			{
				if (token != tokenId)
					return false;
				tokenId = -1;
				return true;
			}
			if (token >= BitArray.Length)
				Debug.LogError("Unknown token " + token);
			bool result = set[token];
			set[token] = false;
			return result;
		}

		// set to accept one additional token.
		// returns true if set changed.
		public bool AddSingleTokenID(short additionalTokenId)
		{
			if (!set.IsEmpty)
			{
				if (set[additionalTokenId])
					return false;
				
				set[additionalTokenId] = true;
				return true;
			}
			
			if (tokenId == additionalTokenId)
				return false;
			
			if (tokenId >= 0)
			{
				set[tokenId] = true;
				set[additionalTokenId] = true;
				tokenId = -1;
			}
			else
			{
				tokenId = additionalTokenId;
			}
			return true;
		}

		// set to accept additional set of tokens.
		// returns true if set changed.
		public bool Add(TokenSet s)
		{
			var result = false;
			if (s.empty && !empty)
			{
				empty = true;
				result = true;
			}
			if (!s.set.IsEmpty)
			{
				if (!set.IsEmpty)
				{
					result = set.Add(s.set) || result;
				}
				else
				{
					set = new BitArray(s.set);
					if (tokenId >= 0)
					{
						set[tokenId] = true;
						tokenId = -1;
					}
					result = true;
				}
			}
			else if (s.tokenId >= 0)
			{
				if (!set.IsEmpty)
				{
					if (!set[s.tokenId])
					{
						set[s.tokenId] = true;
						result = true;
					}
				}
				else if (tokenId >= 0)
				{
					if (tokenId != s.tokenId)
					{
						set[s.tokenId] = true;
						set[tokenId] = true;
						tokenId = -1;
						result = true;
					}
				}
				else if (tokenId != s.tokenId)
				{
					tokenId = s.tokenId;
					result = true;
				}
			}
			return result;
		}

		// checks if lookahead accepts empty input.
		public bool MatchesEmpty()
		{
			return empty;
		}

		// checks if lookahead accepts input symbol.
		//public bool Matches(TokenSet tokenSet)
		//{
		//	if (tokenSet == null)
		//		return false;
		//	var tokenSetTokenId = tokenSet.tokenId;
		//	if (tokenSetTokenId >= 0)
		//		return set.bits == null ? tokenId == tokenSetTokenId : set[tokenSetTokenId];
		//	throw new Exception("matches() botched");
		//}

		//// checks if lookahead accepts input symbol.
		//public bool Matches(SyntaxToken token)
		//{
		//	return set.bits == null ? token.tokenId == tokenId : set[token.tokenId];
		//}

		// checks if lookahead accepts input symbol.
		public bool Matches(short tokenId)
		{
			return set.bits == null ? tokenId == this.tokenId : set[tokenId];
		}

		//// checks if lookahead accepts input symbol.
		//public bool Matches(short token)
		//{
		//	if (set.IsEmpty)
		//		return token == tokenId;
		//	if (token >= BitArray.Length)
		//		Debug.LogError("Unknown token " + token);
		//	return set[token];
		//}

		// checks for ambiguous lookahead.
		public bool Accepts(TokenSet s)
		{
			if (!s.set.IsEmpty)
			{
				if (!set.IsEmpty)
				{
					if (set.ContainsAny(s.set))
							return true;
				}
				else if (tokenId >= 0)
					return s.set[tokenId];
			}
			else if (s.tokenId >= 0)
			{
				if (!set.IsEmpty)
					return set[s.tokenId];
				if (tokenId >= 0)
					return tokenId == s.tokenId;
			}
			return false;
		}

		public TokenSet Intersecton(TokenSet s)
		{
			if (!s.set.IsEmpty)
			{
				if (!set.IsEmpty)
				{
					var intersection = new BitArray(set);
					intersection.And(s.set);
					
					var ts = new TokenSet(intersection);
					return ts;
				}
				else if (tokenId >= 0 && s.set[tokenId])
					return this;
			}
			else if (s.tokenId >= 0)
			{
				if (!set.IsEmpty && set[s.tokenId])
					return s;
				if (tokenId >= 0 && tokenId == s.tokenId)
					return this;
			}
			return new TokenSet();
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			var delim = "";
			if (empty)
			{
				result.Append("empty");
				delim = ", ";
			}
			if (!set.IsEmpty)
				result.Append(delim + "set " + set);
			else if (tokenId >= 0)
				result.Append(delim + "token " + tokenId);
			return "{" + result + "}";
		}
  
		private string cached;
		public string ToString(Parser parser)
		{
			if (cached != null)
				return cached;

			var result = new StringBuilder();
			var delim = string.Empty;
			if (empty)
			{
				result.Append("[empty]");
				delim = ", ";
			}
			if (!set.IsEmpty)
			{
				for (var n = 0; n < BitArray.Length; ++n)
				{
					if (set[n])
					{
						result.Append(delim + parser.GetToken(n));
						delim = ", ";
					}
				}
			}
			else if (tokenId >= 0)
			{
				result.Append(delim + parser.GetToken(tokenId));
			}
			return cached = result.ToString();
		}
	}

	public abstract short TokenToId(CharSpan s);

	public abstract string GetToken(int tokenId);
}

public static class FGGrammarExtensions
{
	public static FGGrammar.Lit ToLit(this string s)
	{
		return new FGGrammar.Lit(s);
	}
}

}
