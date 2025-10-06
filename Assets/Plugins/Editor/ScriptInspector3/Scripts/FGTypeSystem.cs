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

#define MEASURE_RESOLVENODE_DEPTH

namespace ScriptInspector
{

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

using Debug = UnityEngine.Debug;

public enum SymbolKind : byte
{
	None,
	Error,
	_Keyword,
	_Snippet,
	Namespace,
	Interface,
	Enum,
	Struct,
	Class,
	Delegate,
	Field,
	ConstantField,
	LocalConstant,
	EnumMember,
	Property,
	Event,
	Indexer,
	Method,
	//ExtensionMethod,
	MethodGroup,
	Constructor,
	Destructor,
	Operator,
	Accessor,
	LambdaExpression,
	Parameter,
	CatchParameter,
	Variable,
	CaseVariable,
	ForEachVariable,
	FromClauseVariable,
	TupleDeconstructVariable,
	OutVariable,
	IsVariable,
	TypeParameter,
	TypeParameterConstraintList,
	BaseTypesList,
	Instance,
	Null,
	Label,
	ImportedNamespace,
	UsingAlias,
	ImportedStaticType,
	TupleType,
}

[Flags]
public enum Modifiers
{
	None = 0,
	Public = 1 << 0,
	Internal = 1 << 1,
	Protected = 1 << 2,
	Private = 1 << 3,
	Static = 1 << 4,
	New = 1 << 5,
	Sealed = 1 << 6,
	Abstract = 1 << 7,
	ReadOnly = 1 << 8,
	Volatile = 1 << 9,
	Virtual = 1 << 10,
	Override = 1 << 11,
	Extern = 1 << 12,
	Ref = 1 << 13,
	Out = 1 << 14,
	Params = 1 << 15,
	This = 1 << 16,
	Partial = 1 << 17,
	Async = 1 << 18,
	In = 1 << 19,

	RefOutOrIn = Ref | Out | In,
	AccessMask = Private | Protected | Internal | Public,
	InternalOrProtected = Internal | Protected,
	ProtectedOrPrivate = Protected | Private,
	ProtectedOrInternalOrPublic = Internal | Protected | Public,
}

public enum AccessLevel : byte
{
	None = 0,
	Private = 1, // private
	ProtectedAndInternal = 2, // private protected
	ProtectedOrInternal = 4, // protected internal
	Protected, // protected
	Internal, // internal
	Public, // public
}
	
public static class ToCSharpStringExtensions
{
	public static string ToCSharpString(this AccessLevel self)
	{
		switch (self)
		{
		case AccessLevel.Public:
			return "public";
		case AccessLevel.Internal:
			return "internal";
		case AccessLevel.Protected:
			return "protected";
		case AccessLevel.ProtectedOrInternal:
			return "protected internal";
		default:
			return "private";
		}
	}
}

[Flags]
public enum AccessLevelMask : byte
{
	None = 0,
	Private = 1, // private
	Protected = 2, // protected
	Internal = 4, // internal
	Public = 8, // public

	Any = Private | Protected | Internal | Public,
	NonPublic = Private | Protected | Internal,
}


internal static class Operator
{
	public enum ID : byte
	{
		None,
		op_Implicit,
		op_Explicit,
		
		// overloadable unary operators
		op_Decrement,                    // --
		op_Increment,                    // ++
		op_UnaryNegation,                // -
		op_UnaryPlus,                    // +
		op_LogicalNot,                   // !
		op_True,                         // true
		op_False,                        // false
		op_OnesComplement,               // ~
		op_Like,                         // Like (Visual Basic)
		
		// overloadable binary operators
		op_Addition,                     // +
		op_Subtraction,                  // -
		op_Division,                     // /
		op_Multiply,                     // *
		op_Modulus,                      // %
		op_BitwiseOr,                    // |
		op_BitwiseAnd,                   // &
		op_ExclusiveOr,                  // ^
		op_LeftShift,                    // <<
		op_RightShift,                   // >>

		LastNonComparisonOperator = op_RightShift,
		
		// overloadable comparision operators
		op_Equality,                     // ==
		op_Inequality,                   // !=
		op_LessThanOrEqual,              // <=
		op_LessThan,                     // <
		op_GreaterThanOrEqual,           // >=
		op_GreaterThan,                  // >

		FirstComparisonOperator = op_Equality,
		LastComparisonOperator = op_GreaterThan,
		
		LastOverloadableOperator = op_GreaterThan,
		
		// not overloadable operators
		op_AddressOf,                    // &
		op_PointerDereference,           // *
		op_LogicalAnd,                   // &&
		op_LogicalOr,                    // ||
		op_Assign,                       // Not defined (= is not the same)
		op_SignedRightShift,             // Not defined
		op_UnsignedRightShift,           // Not defined
		op_UnsignedRightShiftAssignment, // Not defined
		op_MemberSelection,              // ->
		op_RightShiftAssignment,         // >>=
		op_MultiplicationAssignment,     // *=
		op_PointerToMemberSelection,     // ->*
		op_SubtractionAssignment,        // -=
		op_ExclusiveOrAssignment,        // ^=
		op_LeftShiftAssignment,          // <<=
		op_ModulusAssignment,            // %=
		op_AdditionAssignment,           // +=
		op_BitwiseAndAssignment,         // &=
		op_BitwiseOrAssignment,          // |=
		op_Comma,                        // ,
		op_DivisionAssignment,           // /=
		
		// other operators
		op_NullCoalescing,               // ??
		op_Range,                        // ..
		op_As,                           // as
		op_Is,                           // is
		op_SwitchExpression,             // switch
	}

	public static readonly string[] dotNetName =
	{
		null,
		// overloadable conversion operators
		"op_Implicit",
		"op_Explicit",
	
		// overloadable unary operators
		"op_Decrement",                    // --
		"op_Increment",                    // ++
		"op_UnaryNegation",                // -
		"op_UnaryPlus",                    // +
		"op_LogicalNot",                   // !
		"op_True",                         // true
		"op_False",                        // false
		"op_OnesComplement",               // ~
		"op_Like",                         // Like (Visual Basic)
	
		// overloadable binary operators
		"op_Addition",                     // +
		"op_Subtraction",                  // -
		"op_Division",                     // /
		"op_Multiply",                     // *
		"op_Modulus",                      // %
		"op_BitwiseOr",                    // |
		"op_BitwiseAnd",                   // &
		"op_ExclusiveOr",                  // ^
		"op_LeftShift",                    // <<
		"op_RightShift",                   // >>
	
		// overloadable comparision operators
		"op_Equality",                     // ==
		"op_Inequality",                   // !=
		"op_LessThanOrEqual",              // <=
		"op_LessThan",                     // <
		"op_GreaterThanOrEqual",           // >=
		"op_GreaterThan",                  // >
	
		// not overloadable operators
		"op_AddressOf",                    // &
		"op_PointerDereference",           // *
		"op_LogicalAnd",                   // &&
		"op_LogicalOr",                    // ||
		"op_Assign",                       // Not defined (= is not the same)
		"op_SignedRightShift",             // Not defined
		"op_UnsignedRightShift",           // Not defined
		"op_UnsignedRightShiftAssignment", // Not defined
		"op_MemberSelection",              // ->
		"op_RightShiftAssignment",         // >>=
		"op_MultiplicationAssignment",     // *=
		"op_PointerToMemberSelection",     // ->*
		"op_SubtractionAssignment",        // -=
		"op_ExclusiveOrAssignment",        // ^=
		"op_LeftShiftAssignment",          // <<=
		"op_ModulusAssignment",            // %=
		"op_AdditionAssignment",           // +=
		"op_BitwiseAndAssignment",         // &=
		"op_BitwiseOrAssignment",          // |=
		"op_Comma",                        // ,
		"op_DivisionAssignment",           // /=
		
		// other operators
		null,                              // ??
		null,                              // as
		null,                              // is
		null,                              // switch
	};
}


public struct MiniBloom1
{
	ulong bits;
	
	public void Clear()
	{
		bits = 0;
	}
	
	public void Add(int hashCode)
	{
		unchecked
		{
			bits |= 1ul << (hashCode & 63);
		}
	}
	
	public bool Contains(int hashCode)
	{
		unchecked
		{
			ulong check = 1ul << (hashCode & 63);
			return (bits & check) != 0;
		}
	}
	
	public override string ToString()
	{
		return string.Format("0x{0:X16}", bits);
	}
}

public struct MiniBloom2
{
	ulong bits;
	
	public void Clear()
	{
		bits = 0;
	}
	
	public void Add(int hashCode)
	{
		unchecked
		{
			bits |= (1ul << (hashCode & 63)) | (1ul << ((hashCode >> 6) & 63));
		}
	}
	
	public bool Contains(int hashCode)
	{
		unchecked
		{
			ulong check = (1ul << (hashCode & 63)) | (1ul << ((hashCode >> 6) & 63));
			return (bits & check) == check;
		}
	}
}
	
public class ResolveContext
{
	public ParseTree.BaseNode completionNode;
	public string completionAssetPath;
	public int completionAtLine;
	public int completionAtTokenIndex;
	
	public Scope scope;
	public CompilationUnitScope compilationUnit;
	public AssemblyDefinition assembly;
	public TypeDefinitionBase type;
	
	public bool fromInstance;
}


public class TypeReference
{
	static List<Stack<TypeReference[]>> pool = new List<Stack<TypeReference[]>>();
	static public TypeReference[] AllocArray(int length)
	{
		while (length >= pool.Count)
			pool.Add(new Stack<TypeReference[]>());
		var stack = pool[length];
		if (stack.Count == 0)
			return new TypeReference[length];
		else
			return stack.Pop();
	}

	static public void ReleaseArray(TypeReference[] array)
	{
		if (array == null)
			return;

		var length = array.Length;
		for (int i = 0; i < length; ++i)
			array[i] = null;
		pool[length].Push(array);
	}

	protected TypeReference() {}

	protected TypeReference(ParseTree.BaseNode node)
	{
		parseTreeNode = node;
		allReferences[parseTreeNode] = this;
	}

	protected TypeReference(SymbolDefinition definedSymbol)
	{
		_definition = definedSymbol;
		allReferences[definedSymbol] = this;
	}

	protected TypeReference(Type type)
	{
		reflectedType = type;
		allReferences[type] = this;
	}

	public TypeReference Set(ParseTree.Node node)
	{
		if (reflectedType != null)
		{
			//reflectedType = null;
			return new TypeReference(node);
		}
		parseTreeNode = node;
		_resolvedVersion = 0;
		_definition = null;
		resolving = false;
		allReferences[node] = this;
		return this;
	}

	public TypeReference Set(SymbolDefinition definedSymbol)
	{
		if (reflectedType != null)
		{
			//reflectedType = null;
			return new TypeReference(definedSymbol);
		}
		parseTreeNode = null;
		_resolvedVersion = 0;
		_definition = definedSymbol;
		resolving = false;
		allReferences[definedSymbol] = this;
		return this;
	}

	public TypeReference Set(Type type)
	{
		reflectedType = type;
		parseTreeNode = null;
		_resolvedVersion = 0;
		_definition = null;
		resolving = false;
		allReferences[type] = this;
		return this;
	}
	
	private static readonly Dictionary<object, TypeReference> allReferences = new Dictionary<object, TypeReference>(2048);

	public static TypeReference To(SymbolDefinition symbol)
	{
		if (symbol == null)
			return null;
		
		TypeReference result;
		if (allReferences.TryGetValue(symbol, out result))
			return result;

		result = new TypeReference(symbol);
		return result;
	}

	public static TypeReference To(ParseTree.BaseNode node)
	{
		if (node == null)
			return null;
		
		TypeReference result;
		if (allReferences.TryGetValue(node, out result))
			return result;

		result = new TypeReference(node);
		return result;
	}

	public static TypeReference To(Type type)
	{
		if (type == null)
			return null;
		
		TypeReference result;
		if (allReferences.TryGetValue(type, out result))
			return result;

		result = new TypeReference(type);
		return result;
	}

	public bool IsError()
	{
		return _definition != null && _definition.kind == SymbolKind.Error;
	}
	
	public bool IsNullOrInvalidated()
	{
		if (_definition == null && (reflectedType != null || parseTreeNode != null))
		{
			return false;
		}
		return _definition == null || !_definition.IsValid();
	}

	public bool IsValid()
	{
		//return _definition == null || !_definition.IsValid();
		SymbolDefinition d = _definition;
		//if (reflectedType != null)
		//	d = definition;
		
		if (reflectedType == null && (parseTreeNode == null || parseTreeNode.parent == null) && (_definition == null || _definition.kind == SymbolKind.Error))
			return false;
		
		if (d == null)
			d = definition;
		return d != null && d.IsValid();
	}

	public bool IsReflectedType { get { return reflectedType != null; } }

	protected Type reflectedType;
	protected ParseTree.BaseNode parseTreeNode;
	public ParseTree.BaseNode Node { get { return parseTreeNode; } }

	protected SymbolDefinition _definition;
	protected uint _resolvedVersion;
	protected bool resolving = false;
	public static bool dontResolveNow = false;
	public virtual SymbolDefinition definition
	{
		get
		{
			if (_definition != null)
			{
				var hasParseTreeNode = parseTreeNode != null && parseTreeNode.parent != null;
				if (hasParseTreeNode && _resolvedVersion != ParseTree.resolverVersion || _definition.kind == SymbolKind.Error)// || !_definition.IsValid())
					_definition = null;
			}
			
			if (_definition != null && !_definition.IsValid())
			{
				_definition = _definition.Rebind();
				if (_definition != null && !_definition.IsValid())
				{
					_definition = null;
				}
				//if (!hasParseTreeNode && _definition != null)
				//{
				//	if (_definition.declarations != null)
				//	{
				//		parseTreeNode = _definition.declarations.parseTreeNode;
				//		_resolvedVersion = ParseTree.resolverVersion;
				//		return _definition;
				//	}
				//}
			}
			
			if (_definition == null)
			{
				// Debug.Log("Dereferencing " + parseTreeNode.Print());
				var isReflectedType = reflectedType != null;
				if (isReflectedType)
				{
					_definition = CreateDefinitionForReflectedType(reflectedType);
				}
			}
			
			if (_definition == null)
			{
				if (!resolving)
				{
					if (dontResolveNow)
						return SymbolDefinition.unknownSymbol;
					
					if (parseTreeNode != null)
					{
						resolving = true;
						_definition = SymbolDefinition.ResolveNode(parseTreeNode);
						_resolvedVersion = ParseTree.resolverVersion;
						resolving = false;
					}
				}
				else
				{
					return SymbolDefinition.unknownType;
				}
				//var leaf = parseTreeNode as ParseTree.Leaf;
				//if (leaf != null && leaf.resolvedSymbol != null)
				//{
				//    _definition = leaf.resolvedSymbol;
				//}
				//else
				//{
				//    var node = parseTreeNode as ParseTree.Node;
				//    var scopeNode = node;
				//    while (scopeNode != null && scopeNode.scope == null)
				//        scopeNode = scopeNode.parent;
				//    if (scopeNode != null)
				//    {
				//        _definition = scopeNode.scope.ResolveNode(node);
				//    }
				//}
				if (_definition == null)
				{
				//	Debug.Log("Failed to resolve TypeReference: " + parseTreeNode);
					_definition = SymbolDefinition.unknownType;
					_resolvedVersion = ParseTree.resolverVersion;
				}
			}
			return _definition ?? SymbolDefinition.unknownType;
		}
	}

	private static TypeDefinitionBase CreateDefinitionForReflectedType(Type type)
	{
		if (type.IsArray)
		{
			var elementType = type.GetElementType();
			var elementTypeDefinition = TypeReference.To(elementType).definition as TypeDefinitionBase;
			var arrayRank = type.GetArrayRank();
			var resultArrayTypeDef = elementTypeDefinition.MakeArrayType(arrayRank);
			return resultArrayTypeDef;
		}

		var tn = type.Name;
		
		var isRefType = false;
		if (tn[tn.Length - 1] == '&')
		{
			tn = tn.Substring(0, tn.Length - 1);
			isRefType = true;
		}
		
		if (type.IsGenericParameter)
		{
			var index = type.GenericParameterPosition;
			var reflectedDeclaringMethod = type.DeclaringMethod as MethodInfo;
			if (reflectedDeclaringMethod != null && reflectedDeclaringMethod.IsGenericMethod)
			{
				var declaringType = TypeReference.To(reflectedDeclaringMethod.DeclaringType).definition as TypeDefinitionBase;
				if (declaringType == null)
					return SymbolDefinition.unknownType;
				var methodName = reflectedDeclaringMethod.Name;
				var typeArgs = reflectedDeclaringMethod.GetGenericArguments();
				var numTypeArgs2 = typeArgs.Length;
				var member = declaringType.FindName(methodName, numTypeArgs2, false);
				if (member == null && numTypeArgs2 > 0)
					member = declaringType.FindName(methodName, 0, false);
				if (member != null && member.kind == SymbolKind.MethodGroup)
				{
					var methodGroup = (MethodGroupDefinition) member;
					var methods = methodGroup.methods;
					if (methods.Count == 1)
					{
						member = methods[0];
					}
					else
					{
						for (var i = methods.Count; i --> 0; )
						{
							var reflectedMethod = methods[i] as ReflectedMethod;
							if (reflectedMethod != null && reflectedMethod.reflectedMethodInfo == reflectedDeclaringMethod)
							{
								member = reflectedMethod;
								break;
							}
							else if (reflectedMethod == null)
							{
								var method = methods[i];

								if (method == null || method.IsStatic != reflectedMethod.IsStatic)
									continue;

								var methodTypeParams = method.GetTypeParameters();
								var numTypeParams = methodTypeParams == null ? 0 : methodTypeParams.Count;
								if (numTypeParams != numTypeArgs2)
									continue;

								var parameters = reflectedDeclaringMethod.GetParameters();
								var numParams = parameters.Length;

								var methodParams = method.GetParameters();
								if (methodParams.Count != numParams)
									continue;

								var sameNames = true;
								for (var j = numTypeArgs2; j --> 0; )
								{
									if (methodTypeParams[j].name != typeArgs[j].Name)
									{
										sameNames = false;
										break;
									}
								}
								if (!sameNames)
									continue;

								for (var j = numParams; j --> 0; )
								{
									var p = parameters[j];
									var m = methodParams[j];
									if (p.Name != m.name)
									{
										sameNames = false;
										break;
									}
									if (p.IsOut != m.IsOut || p.IsIn != m.IsIn || p.IsOptional != m.IsOptional || (!p.IsOut && p.ParameterType.IsByRef) != m.IsRef)
									{
										sameNames = false;
										break;
									}
								}
								if (!sameNames)
									continue;

								// TODO: Also check if the parameter types match.

								member = method;
								break;
							}
						}
					}
				}

				var methodDefinition = member as MethodDefinition;
				return methodDefinition == null ? null : methodDefinition.typeParameters.ElementAtOrDefault(index);
				//	(methodDefinition != null && methodDefinition.typeParameters != null
				//	? methodDefinition.typeParameters.ElementAtOrDefault(index) : null)
				//	?? SymbolDefinition.unknownSymbol;
			}

			{
				var reflectedDeclaringType = type.DeclaringType;
				while (true)
				{
					var parentType = reflectedDeclaringType.DeclaringType;
					if (parentType == null)
						break;
					var count = parentType.GetGenericArguments().Length;
					if (count <= index)
					{
						index -= count;
						break;
					}
					reflectedDeclaringType = parentType;
				}

				var declaringTypeRef = TypeReference.To(reflectedDeclaringType);
				var declaringType = declaringTypeRef.definition as TypeDefinition;
				if (declaringType == null || declaringType.typeParameters == null)
					return SymbolDefinition.unknownType;
				
				TypeDefinitionBase genericType = declaringType.typeParameters[index];
				if (isRefType && genericType != null)
				{
					genericType = genericType.MakeRefType();
					Debug.Log(type.Name + " -> " + genericType.GetTooltipText());
				}

				return genericType;
			}
		}

		SymbolDefinition parentSymbolDef = null;
		TypeDefinition parentTypeDef = null;

		var numParentTypeArgs = 0;
				
		if (type.IsNested)
		{
			parentSymbolDef = TypeReference.To(type.DeclaringType).definition;
			parentTypeDef = parentSymbolDef as TypeDefinition;

			for (var current = parentTypeDef; current != null;  current = current.parentSymbol as TypeDefinition)
				numParentTypeArgs += current.NumTypeParameters;
		}
		else
		{
			var assemblyDefinition = AssemblyDefinition.FromAssembly(type.Assembly);
			if (assemblyDefinition != null)
				parentSymbolDef = assemblyDefinition.FindNamespace(type.Namespace);
		}

		if (type.IsGenericType)
		{
			var reflectedTypeArgs = type.GetGenericArguments();
			var numGenericArgs = reflectedTypeArgs.Length - numParentTypeArgs;

			if (type.IsGenericTypeDefinition)
			{
				//Debug.Log(type.ToString());
				//var resultTypeDef = parentSymbolDef.FindName(tn, numGenericArgs, true) as TypeDefinition;
				//if (resultTypeDef != null)
				//	return resultTypeDef;

				//return SymbolDefinition.unknownType;
			}
			else
			{
				var reflectedTypeDef = type.GetGenericTypeDefinition();
				var genericTypeDef = TypeReference.To(reflectedTypeDef).definition as TypeDefinition;
				if (genericTypeDef == null)
					return SymbolDefinition.unknownType;

				var declaringType = type.DeclaringType;
				if (declaringType != null && declaringType.IsGenericType)
				{
					//var parentArgs = declaringType.GetGenericArguments();
					//numGenericArgs -= parentArgs.Length;

					if (parentTypeDef == null)
						return SymbolDefinition.unknownType;

					var constructedParentType = parentTypeDef as ConstructedTypeDefinition;

					if (numGenericArgs <= 0)
					{
						if (constructedParentType != null)
						{
							var nestedType = constructedParentType.GetConstructedMember(genericTypeDef) as TypeDefinition;
							if (nestedType != null)
								return nestedType;
						}

						return genericTypeDef;
					}

					var nestedTypeArguments = TypeReference.AllocArray(numGenericArgs);
					var numNestedTypeArgs = nestedTypeArguments.Length;
					for (int i = numNestedTypeArgs - numGenericArgs, j = 0; i < numNestedTypeArgs; ++i)
						nestedTypeArguments[j++] = TypeReference.To(reflectedTypeArgs[i]);

					if (declaringType.IsGenericTypeDefinition)
					{
						var result3 = genericTypeDef.ConstructType(nestedTypeArguments, parentTypeDef);
						TypeReference.ReleaseArray(nestedTypeArguments);
						return result3;
					}

					TypeReference.ReleaseArray(nestedTypeArguments);

					// TODO: Implement this!
					return SymbolDefinition.unknownType;
				}

				var typeArguments = TypeReference.AllocArray(numGenericArgs);
				for (int i = typeArguments.Length - numGenericArgs, j = 0; i < typeArguments.Length; ++i)
					typeArguments[j++] = TypeReference.To(reflectedTypeArgs[i]);
				var result4 = genericTypeDef.ConstructType(typeArguments);
				TypeReference.ReleaseArray(typeArguments);
				return result4;
			}
		}

		if (parentSymbolDef == null || parentSymbolDef.kind == SymbolKind.Error)
			return SymbolDefinition.unknownType;

		var length = tn.Length;
		
		var rank = 0;
		var rankSpecifier = tn.IndexOf('[');
		if (rankSpecifier > 0)
		{
			rank = length - rankSpecifier - 1;
			length = rankSpecifier;
		}

		var isPointer = false;
		var pointerSpecifier = tn.IndexOf('*');
		if (pointerSpecifier > 0)
		{
			length = pointerSpecifier < length ? pointerSpecifier : length;
			isPointer = true;
		}

		var numTypeArgs = 0;
		var genericMarkerIndex = tn.IndexOf('`');
		if (genericMarkerIndex > 0)
		{
			numTypeArgs = 0;
			for (int i = genericMarkerIndex + 1; i < length; ++i)
				numTypeArgs = 10*numTypeArgs + tn[i] - '0';
			//numTypeArgs = int.Parse(tn.Substring(genericMarkerIndex + 1));
			length = genericMarkerIndex;
		}

		var result = parentSymbolDef.FindName(new CharSpan(tn, 0, length), numTypeArgs, true);
		if (result == null)
		{
			//	UnityEngine.Debug.LogWarning(tn + " not found in " + result + " " + result.GetHashCode() + "\n" + "while resolving reference to " + reflectedType);
			return null;
		}
		else if (rank > 0)
		{
			var elementType = result as TypeDefinition;
			if (elementType != null)
			{
				result = elementType.MakeArrayType(rank);
			}
			else
			{
				result = null;
			}
		}
		else if (isPointer)
		{
			var elementType = result as TypeDefinitionBase;
			if (elementType != null)
			{
				result = elementType.MakePointerType();
			}
			else
			{
				result = null;
			}
		}

		if (isRefType)
		{
			var elementType = result as TypeDefinition;
			if (elementType != null)
			{
				result = elementType.MakeRefType();
			}
		}

		var resultAsTypeDefBase = result as TypeDefinitionBase;
		if (resultAsTypeDefBase != null)
			return resultAsTypeDefBase;

		return SymbolDefinition.unknownType;
	}

	//public bool IsBefore(ParseTree.Leaf leaf)
	//{
	//	if (parseTreeNode == null)
	//		return true;
	//	var lastLeaf = parseTreeNode as ParseTree.Leaf;
	//	if (lastLeaf == null)
	//		lastLeaf = ((ParseTree.Node) parseTreeNode).GetLastLeaf();
	//	return lastLeaf != null && (lastLeaf.line < leaf.line || lastLeaf.line == leaf.line && lastLeaf.tokenIndex < leaf.tokenIndex);
	//}

	public override string ToString()
	{
		return definition.GetTooltipText(false);
	}
}


public abstract class Scope
{
	public static ParseTree.BaseNode completionNode;
	public static string completionAssetPath;
	public static int completionAtLine;
	public static int completionAtTokenIndex;
	public static MethodDefinition firstAccessibleMethod;
	
	protected ParseTree.Node parseTreeNode;

	public Scope(ParseTree.Node node)
	{
		parseTreeNode = node;
	}

	public Scope _parentScope;
	public Scope parentScope {
		get {
			if (_parentScope != null || parseTreeNode == null)
				return _parentScope;
			for (var node = parseTreeNode.parent; node != null; node = node.parent)
				if (node.scope != null)
					return node.scope;
			return null;
		}
		set { _parentScope = value; }
	}
	
	public AssemblyDefinition GetAssembly()
	{
		for (Scope scope = this; scope != null; scope = scope.parentScope)
		{
			var cuScope = scope as CompilationUnitScope;
			if (cuScope != null)
				return cuScope.assembly;
		}
		return null;
		//throw new Exception("No Assembly for scope???");
	}

	public abstract SymbolDefinition AddDeclaration(SymbolDeclaration symbol);

	public abstract void RemoveDeclaration(SymbolDeclaration symbol);

	//public virtual SymbolDefinition AddDeclaration(SymbolKind symbolKind, ParseTree.Node definitionNode)
	//{
	//    var symbol = new SymbolDeclaration { scope = this, kind = symbolKind, parseTreeNode = definitionNode };
	//    var definition = AddDeclaration(symbol);
	//    return definition;
	//

	public virtual string CreateAnonymousName()
	{
		return parentScope != null ? parentScope.CreateAnonymousName() : null;
	}

	public virtual void Resolve(ParseTree.Leaf leaf, int numTypeArgs, bool asTypeOnly)
	{
		leaf.resolvedSymbol = null;
		if (parentScope != null)
			parentScope.Resolve(leaf, numTypeArgs, asTypeOnly);
	}

	public virtual void ResolveAttribute(ParseTree.Leaf leaf)
	{
		leaf.resolvedSymbol = null;
		leaf.semanticError = null;
		if (parentScope != null)
			parentScope.ResolveAttribute(leaf);
	}

	public SymbolDefinition ResolveAsExtensionMethod(ParseTree.Leaf invokedLeaf, SymbolDefinition invokedSymbol, TypeDefinitionBase memberOf, ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope context)
	{
		if (invokedLeaf == null && (invokedSymbol == null || invokedSymbol.kind == SymbolKind.Error))
			return null;
		
		var id = invokedSymbol != null && invokedSymbol.kind != SymbolKind.Error ? invokedSymbol.name : invokedLeaf != null ? SymbolDefinition.DecodeId(invokedLeaf.token.text) : CharSpan.Empty;

		firstAccessibleMethod = null;
		var result = ResolveAsExtensionMethod(id, memberOf, argumentListNode, typeArgs, context, invokedLeaf);

		if (result == null && firstAccessibleMethod != null)
		{
			invokedLeaf.resolvedSymbol = firstAccessibleMethod;
			invokedLeaf.semanticError = MethodGroupDefinition.unresolvedMethodOverload.name;
			result = firstAccessibleMethod;
		}
		firstAccessibleMethod = null;
		return result;
	}
	
	public virtual MethodDefinition FindDeconstructExtensionMethod(TypeDefinitionBase memberOf, int numOutParameters, Scope context)
	{
		var parentScope = this.parentScope;
		while (parentScope != null)
		{
			var namespaceScope = parentScope as NamespaceScope;
			if (namespaceScope != null)
			{
				var resolved = namespaceScope.FindDeconstructExtensionMethod(memberOf, numOutParameters, context);
				if (resolved != null)
					return resolved;
			}
			
			parentScope = parentScope.parentScope;
		}

		return null;
	}
	
	public virtual SymbolDefinition ResolveAsExtensionMethod(CharSpan id, TypeDefinitionBase memberOf, ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope context, ParseTree.Leaf invokedLeaf = null)
	{
		var parentScope = this.parentScope;
		while (parentScope != null)
		{
			var namespaceScope = parentScope as NamespaceScope;
			if (namespaceScope == null)
			{
				parentScope = parentScope.parentScope;
				continue;
			}
			
			var resolved = namespaceScope.ResolveAsExtensionMethod(id, memberOf, argumentListNode, typeArgs, context, invokedLeaf);
			return resolved;
		}

		return null;
	}
	
	public abstract SymbolDefinition FindName(CharSpan symbolName, int numTypeParameters);
	//{
	//    return null;
	//}

	public virtual void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		if (parentScope != null)
			parentScope.GetCompletionData(data, context);
	}

	public virtual TypeDefinition EnclosingType()
	{
		return parentScope != null ? parentScope.EnclosingType() : null;
	}

	public NamespaceScope EnclosingNamespaceScope()
	{
		for (var parent = parentScope; parent != null; parent = parent.parentScope)
		{
			var parentNamespaceScope = parent as NamespaceScope;
			if (parentNamespaceScope != null)
				return parentNamespaceScope;
		}
		return null;
	}
	
	public virtual void GetExtensionMethodsCompletionData(TypeDefinitionBase forType, Dictionary<string, SymbolDefinition> data, TypeDefinitionBase contextType)
	{
		if (parentScope != null)
			parentScope.GetExtensionMethodsCompletionData(forType, data, contextType);
	}

	public virtual IEnumerable<NamespaceDefinition> VisibleNamespacesInScope()
	{
		if (parentScope != null)
			foreach (var ns in parentScope.VisibleNamespacesInScope())
				yield return ns;
	}
}


public class ReflectedMember : InstanceDefinition
{
	private readonly MemberInfo memberInfo;

	public ReflectedMember(MemberInfo info, SymbolDefinition memberOf)
	{
		MethodInfo getMethodInfo = null;
		MethodInfo setMethodInfo = null;
		MethodInfo addMethodInfo = null;
		MethodInfo removeMethodInfo = null;
		
		switch (info.MemberType)
		{
			case MemberTypes.Constructor:
			case MemberTypes.Method:
				throw new InvalidOperationException();

			case MemberTypes.Field:
				var fieldInfo = (FieldInfo) info;
				modifiers =
					fieldInfo.IsPublic ? Modifiers.Public :
					fieldInfo.IsFamilyOrAssembly ? Modifiers.Internal | Modifiers.Protected :
					fieldInfo.IsAssembly ? Modifiers.Internal :
					fieldInfo.IsFamily ? Modifiers.Protected :
					Modifiers.Private;
				if (fieldInfo.IsStatic)// && !fieldInfo.IsLiteral)
					modifiers |= Modifiers.Static;
				break;

			case MemberTypes.Property:
				var propertyInfo = (PropertyInfo) info;
				getMethodInfo = propertyInfo.GetGetMethod(true);
				setMethodInfo = propertyInfo.GetSetMethod(true);
				modifiers = GetAccessorModifiers(getMethodInfo, setMethodInfo);
				break;

			case MemberTypes.Event:
				var eventInfo = (EventInfo) info;
				addMethodInfo = eventInfo.GetAddMethod(true);
				removeMethodInfo = eventInfo.GetRemoveMethod(true);
				modifiers = GetAccessorModifiers(addMethodInfo, removeMethodInfo);
				break;

			default:
				break;
		}
		accessLevel = AccessLevelFromModifiers(modifiers);

		memberInfo = info;
		var memberName = info.Name;
		var generic = memberName.IndexOf('`');
		name = generic < 0 ? memberName : memberName.Substring(0, generic);
		parentSymbol = memberOf;
		switch (info.MemberType)
		{
			case MemberTypes.Field:
				kind = ((FieldInfo) info).IsLiteral ?
					(memberOf.kind == SymbolKind.Enum ? SymbolKind.EnumMember : SymbolKind.ConstantField) :
					SymbolKind.Field;
				break;
			case MemberTypes.Property:
				var indexParams = ((PropertyInfo) info).GetIndexParameters();
				kind = indexParams.Length > 0 ? SymbolKind.Indexer : SymbolKind.Property;
				if (getMethodInfo != null)
				{
					var accessor = Create(SymbolKind.Accessor, "get");
					accessor.modifiers = setMethodInfo != null ? GetAccessorModifiers(getMethodInfo) : modifiers;
					modifiers |= accessor.modifiers & (Modifiers.Abstract | Modifiers.Virtual | Modifiers.Override);
					AddMember(accessor);
				}
				if (setMethodInfo != null)
				{
					var accessor = Create(SymbolKind.Accessor, "set");
					accessor.modifiers = getMethodInfo != null ? GetAccessorModifiers(setMethodInfo) : modifiers;
					modifiers |= accessor.modifiers & (Modifiers.Abstract | Modifiers.Virtual | Modifiers.Override);
					AddMember(accessor);
				}
				break;
			case MemberTypes.Event:
				kind = SymbolKind.Event;
				if (addMethodInfo != null)
				{
					var accessor = Create(SymbolKind.Accessor, "add");
					accessor.modifiers = removeMethodInfo != null ? GetAccessorModifiers(addMethodInfo) : modifiers;
					modifiers |= accessor.modifiers & (Modifiers.Abstract | Modifiers.Virtual | Modifiers.Override);
					AddMember(accessor);
				}
				if (removeMethodInfo != null)
				{
					var accessor = Create(SymbolKind.Accessor, "remove");
					accessor.modifiers = addMethodInfo != null ? GetAccessorModifiers(removeMethodInfo) : modifiers;
					modifiers |= accessor.modifiers & (Modifiers.Abstract | Modifiers.Virtual | Modifiers.Override);
					AddMember(accessor);
				}
				break;
			default:
				throw new InvalidOperationException("Importing a non-supported member type!");
		}
	}
	
	private Modifiers GetAccessorModifiers(MethodInfo accessor1, MethodInfo accessor2)
	{
		var union = GetAccessorModifiers(accessor1) | GetAccessorModifiers(accessor2);
		var result = (union & Modifiers.Public) != 0 ? Modifiers.Public : union & (Modifiers.Internal | Modifiers.Protected);
		if (result == Modifiers.None)
			result = Modifiers.Private;
		result |= union & (Modifiers.Abstract | Modifiers.Virtual | Modifiers.Static);
		return result;
	}
	
	private Modifiers GetAccessorModifiers(MethodInfo accessor)
	{
		if (accessor == null)
			return Modifiers.Private;
		
		var modifiers =
			accessor.IsPublic ? Modifiers.Public :
			accessor.IsFamilyOrAssembly ? Modifiers.Internal | Modifiers.Protected :
			accessor.IsAssembly ? Modifiers.Internal :
			accessor.IsFamily ? Modifiers.Protected :
			Modifiers.Private;
		if (accessor.IsAbstract)
			modifiers |= Modifiers.Abstract;
		if (accessor.IsVirtual)
			modifiers |= Modifiers.Virtual;
		if (accessor.IsStatic)
			modifiers |= Modifiers.Static;
		var baseDefinition = accessor.GetBaseDefinition();
		if (baseDefinition != null && baseDefinition != accessor)
		{
			modifiers = (modifiers & ~Modifiers.Virtual) | Modifiers.Override;
		}
		return modifiers;
	}

	public override SymbolDefinition TypeOf()
	{
		if (memberInfo.MemberType == MemberTypes.Constructor)
			return parentSymbol.TypeOf();
		
		if (type != null && (type.definition == null || !type.definition.IsValid()))
			type = null;
		
		if (type == null)
		{
			Type memberType = null;
			switch (memberInfo.MemberType)
			{
				case MemberTypes.Field:
					memberType = ((FieldInfo) memberInfo).FieldType;
					break;
				case MemberTypes.Property:
					memberType = ((PropertyInfo) memberInfo).PropertyType;
					break;
				case MemberTypes.Event:
					memberType = ((EventInfo) memberInfo).EventHandlerType;
					break;
				case MemberTypes.Method:
					memberType = ((MethodInfo) memberInfo).ReturnType;
					break;
			}
			type = TypeReference.To(memberType);
		}

		return type != null ? type.definition : unknownType;
	}
}


//public class ReflectedTypeReference : TypeReference
//{
//	protected ReflectedTypeReference(Type type)
//	{
//		reflectedType = type;
//	}

//	public override SymbolDefinition definition
//	{
//		get
//		{
//			if (reflectedType == null)
//				return base.definition;

//			if (_definition != null && !_definition.IsValid())
//			{
//				_definition = _definition.Rebind();
//				if (_definition != null && !_definition.IsValid())
//				{
//					_definition = null;
//				}
//				if (_definition != null)
//				{
//					if (_definition.declarations != null)
//					{
//						parseTreeNode = _definition.declarations.parseTreeNode;
//						_resolvedVersion = ParseTree.resolverVersion;
//						reflectedType = null;
//						return _definition;
//					}
//				}
//			}

//			if (_definition == null)
//			{
//				if (reflectedType.IsArray)
//				{
//					var elementType = reflectedType.GetElementType();
//					var elementTypeDefinition = TypeReference.To(elementType).definition as TypeDefinitionBase;
//					var rank = reflectedType.GetArrayRank();
//					_definition = elementTypeDefinition.MakeArrayType(rank);
//					return _definition;
//				}

//				if (reflectedType.IsGenericParameter)
//				{
//					var index = reflectedType.GenericParameterPosition;
//					var reflectedDeclaringMethod = reflectedType.DeclaringMethod as MethodInfo;
//					if (reflectedDeclaringMethod != null && reflectedDeclaringMethod.IsGenericMethod)
//					{
//						var declaringTypeRef = GetOrCreate(reflectedDeclaringMethod.DeclaringType);
//						var declaringType = declaringTypeRef.definition as ReflectedType;
//						if (declaringType == null)
//							return _definition = SymbolDefinition.unknownType;
//						var methodName = reflectedDeclaringMethod.Name;
//						var typeArgs = reflectedDeclaringMethod.GetGenericArguments();
//						var numTypeArgs = typeArgs.Length;
//						var member = declaringType.FindName(methodName, numTypeArgs, false);
//						if (member == null && numTypeArgs > 0)
//							member = declaringType.FindName(methodName, 0, false);
//						if (member != null && member.kind == SymbolKind.MethodGroup)
//						{
//							var methodGroup = (MethodGroupDefinition) member;
//							var methods = methodGroup.methods;
//							for (var i = methods.Count; i --> 0; )
//							{
//								var reflectedMethod = methods[i] as ReflectedMethod;
//								if (reflectedMethod != null && reflectedMethod.reflectedMethodInfo == reflectedDeclaringMethod)
//								{
//									member = reflectedMethod;
//									break;
//								}
//							}
//						}
//						var methodDefinition = member as MethodDefinition;
//						_definition = methodDefinition.typeParameters.ElementAtOrDefault(index);
//						//	(methodDefinition != null && methodDefinition.typeParameters != null
//						//	? methodDefinition.typeParameters.ElementAtOrDefault(index) : null)
//						//	?? SymbolDefinition.unknownSymbol;
//					}
//					else
//					{
//						var reflectedDeclaringType = reflectedType.DeclaringType;
//						while (true)
//						{
//							var parentType = reflectedDeclaringType.DeclaringType;
//							if (parentType == null)
//								break;
//							var count = parentType.GetGenericArguments().Length;
//							if (count <= index)
//							{
//								index -= count;
//								break;
//							}
//							reflectedDeclaringType = parentType;
//						}

//						var declaringTypeRef = GetOrCreate(reflectedDeclaringType);
//						var declaringType = declaringTypeRef.definition as TypeDefinition;
//						if (declaringType == null || declaringType.typeParameters == null)
//							return _definition = SymbolDefinition.unknownType;

//						_definition = declaringType.typeParameters[index];
//					}
//					return _definition;
//				}

//				if (reflectedType.IsGenericType && !reflectedType.IsGenericTypeDefinition)
//				{
//					var reflectedTypeDef = reflectedType.GetGenericTypeDefinition();
//					var genericTypeDefRef = GetOrCreate(reflectedTypeDef);
//					var genericTypeDef = genericTypeDefRef.definition as TypeDefinition;
//					if (genericTypeDef == null)
//						return _definition = SymbolDefinition.unknownType;

//					var reflectedTypeArgs = reflectedType.GetGenericArguments();
//					var numGenericArgs = reflectedTypeArgs.Length;
//					var declaringType = reflectedType.DeclaringType;
//					if (declaringType != null && declaringType.IsGenericType)
//					{
//						var parentArgs = declaringType.GetGenericArguments();
//						numGenericArgs -= parentArgs.Length;
//					}

//					var typeArguments = new TypeReference[numGenericArgs];
//					for (int i = typeArguments.Length - numGenericArgs, j = 0; i < typeArguments.Length; ++i)
//						typeArguments[j++] = GetOrCreate(reflectedTypeArgs[i]);
//					_definition = genericTypeDef.ConstructType(typeArguments);
//					return _definition;
//				}

//				var tn = reflectedType.Name;
//				SymbolDefinition declaringSymbol = null;
				
//				if (reflectedType.IsNested)
//				{
//					declaringSymbol = GetOrCreate(reflectedType.DeclaringType).definition;
//				}
//				else
//				{
//					var assemblyDefinition = AssemblyDefinition.FromAssembly(reflectedType.Assembly);
//					if (assemblyDefinition != null)
//						declaringSymbol = assemblyDefinition.FindNamespace(reflectedType.Namespace);
//				}

//				if (declaringSymbol != null && declaringSymbol.kind != SymbolKind.Error)
//				{
//					var length = tn.Length;
					
//					var rank = 0;
//					var rankSpecifier = tn.IndexOf('[');
//					if (rankSpecifier > 0)
//					{
//						rank = length - rankSpecifier - 1;
//						length = rankSpecifier;
//					}
//					var numTypeArgs = 0;
//					var genericMarkerIndex = tn.IndexOf('`');
//					if (genericMarkerIndex > 0)
//					{
//						numTypeArgs = 0;
//						for (int i = genericMarkerIndex + 1; i < length; ++i)
//							numTypeArgs = 10*numTypeArgs + tn[i] - '0';
//						//numTypeArgs = int.Parse(tn.Substring(genericMarkerIndex + 1));
//						length = genericMarkerIndex;
//					}
//					_definition = declaringSymbol.FindName(new CharSpan(tn, 0, length), numTypeArgs, true);
//					if (_definition == null)
//					{
//						//	UnityEngine.Debug.LogWarning(tn + " not found in " + result + " " + result.GetHashCode() + "\n" + "while resolving reference to " + reflectedType);
//						return null;
//					}
//					else if (rank > 0)
//					{
//						var elementType = _definition as TypeDefinition;
//						if (elementType != null)
//						{
//							_definition = elementType.MakeArrayType(rank);
//						}
//						else
//						{
//							_definition = null;
//						}
//					}
//				}
//				if (_definition == null)
//					_definition = SymbolDefinition.unknownType;
//			}
//			return _definition;
//		}
//	}

//	public override string ToString()
//	{
//		return definition.GetName();
//	}
//}

public class ReflectedMethod : MethodDefinition
{
	public readonly MethodInfo reflectedMethodInfo;

	public ReflectedMethod(MethodInfo methodInfo, SymbolDefinition memberOf)
	{
		modifiers =
			methodInfo.IsPublic ? Modifiers.Public :
			methodInfo.IsFamilyOrAssembly ? Modifiers.Internal | Modifiers.Protected :
			methodInfo.IsAssembly ? Modifiers.Internal :
			methodInfo.IsFamily ? Modifiers.Protected :
			Modifiers.Private;
		if (methodInfo.IsAbstract)
			modifiers |= Modifiers.Abstract;
		if (methodInfo.IsVirtual)
			modifiers |= Modifiers.Virtual;
		if (methodInfo.IsStatic)
			modifiers |= Modifiers.Static;
		if (methodInfo.GetBaseDefinition() != methodInfo)
			modifiers = (modifiers & ~Modifiers.Virtual) | Modifiers.Override;
		if (IsStatic && methodInfo.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
		{
			var parentType = memberOf.parentSymbol as TypeDefinitionBase;
			if (parentType.kind == SymbolKind.Class && parentType.IsStatic && parentType.NumTypeParameters == 0)
			{
				isExtensionMethod = true;
				++parentType.numExtensionMethods;
			}
		}
		accessLevel = AccessLevelFromModifiers(modifiers);

		reflectedMethodInfo = methodInfo;
		var methodName = methodInfo.Name;
		var genericMarker = methodName.IndexOf('`');
		name = genericMarker < 0 ? methodName : methodName.Substring(0, genericMarker);
		parentSymbol = memberOf;

		isOperator = IsStatic && IsPublic && methodInfo.IsSpecialName && IsOperatorName(name);
	}
	
	protected override void Initialize()
	{
		base.Initialize();

		if (reflectedMethodInfo.IsGenericMethod)
		{
			var tp = reflectedMethodInfo.GetGenericArguments();
			if (tp.Length > 0)
			{
				var numGenericArgs = tp.Length;
				_typeParameters = new List<TypeParameterDefinition>(tp.Length);
				for (var i = tp.Length - numGenericArgs; i < tp.Length; ++i)
				{
					var tpDef = new TypeParameterDefinition { kind = SymbolKind.TypeParameter, name = tp[i].Name, parentSymbol = this };
					_typeParameters.Add(tpDef);
				}
			}
		}

		_returnType = TypeReference.To(reflectedMethodInfo.ReturnType);

		var methodParameters = reflectedMethodInfo.GetParameters();
		var numParameters = methodParameters.Length;
		if (_parameters == null && numParameters != 0)
			_parameters = new List<ParameterDefinition>(methodParameters.Length);
		for (var i = 0; i < numParameters; ++i)
		{
			var p = methodParameters[i];

			var isByRef = p.ParameterType.IsByRef;
			var parameterType = isByRef ? p.ParameterType.GetElementType() : p.ParameterType;
			var parameterToAdd = new ParameterDefinition
			{
				kind = SymbolKind.Parameter,
				parentSymbol = this,
				name = p.Name,
				type = TypeReference.To(parameterType),
				modifiers = isByRef ? (p.IsOut ? Modifiers.Out : p.IsIn ? Modifiers.In : Modifiers.Ref) : parameterType.IsArray && p.IsDefined(typeof(ParamArrayAttribute), false) ? Modifiers.Params : Modifiers.None,
			};
			if (p.RawDefaultValue != DBNull.Value)
			{
				var dv = p.RawDefaultValue;
				parameterToAdd.defaultValue =
					dv == null ? "null"
					: dv is string ? "\"" + dv.ToString() + "\""
					: dv is Enum ? parameterType.ToString() + "." + dv.ToString()
					: dv.ToString();
			}
			_parameters.Add(parameterToAdd);
		}
		
		if (isExtensionMethod)
		{
			_parameters[0].modifiers |= Modifiers.This;
		}
	}
}

public class ReflectedConstructor : MethodDefinition
{
	private readonly ConstructorInfo reflectedConstructorInfo;

	public ReflectedConstructor(ConstructorInfo constructorInfo, SymbolDefinition memberOf)
	{
		modifiers =
			constructorInfo.IsPublic ? Modifiers.Public :
			constructorInfo.IsFamilyOrAssembly ? Modifiers.Internal | Modifiers.Protected :
			constructorInfo.IsAssembly ? Modifiers.Internal :
			constructorInfo.IsFamily ? Modifiers.Protected :
			Modifiers.Private;
		if (constructorInfo.IsAbstract)
			modifiers |= Modifiers.Abstract;
		if (constructorInfo.IsStatic)
			modifiers |= Modifiers.Static;
		accessLevel = AccessLevelFromModifiers(modifiers);

		reflectedConstructorInfo = constructorInfo;

		if (constructorInfo.IsStatic)
			name = ".cctor";
		else
			name = ".ctor";
		kind = SymbolKind.Constructor;
		parentSymbol = memberOf;
	}
	
	protected override void Initialize()
	{
		base.Initialize();

		_returnType = TypeReference.To(parentSymbol.kind == SymbolKind.MethodGroup ? parentSymbol.parentSymbol : parentSymbol);

		var constructorParameters = reflectedConstructorInfo.GetParameters();
		var numParameters = constructorParameters.Length;
		if (_parameters == null && numParameters != 0)
			_parameters = new List<ParameterDefinition>(numParameters);
		for (var i = 0; i < numParameters; ++i)
		{
			var p = constructorParameters[i];
			
			var isByRef = p.ParameterType.IsByRef;
			var parameterType = isByRef ? p.ParameterType.GetElementType() : p.ParameterType;
			var parameterToAdd = new ParameterDefinition
			{
				kind = SymbolKind.Parameter,
				parentSymbol = this,
				name = p.Name,
				type = TypeReference.To(parameterType),
				modifiers = isByRef ? (p.IsOut ? Modifiers.Out : p.IsIn ? Modifiers.In : Modifiers.Ref) : parameterType.IsArray && p.IsDefined(typeof(ParamArrayAttribute), false) ? Modifiers.Params : Modifiers.None,
			};
			if (p.RawDefaultValue != DBNull.Value)
			{
				var dv = p.RawDefaultValue;
				parameterToAdd.defaultValue =
					dv == null ? "null"
					: dv is string ? "\"" + dv.ToString() + "\""
					: dv is Enum ? parameterType.ToString() + "." + dv.ToString()
					: dv.ToString();
			}
			_parameters.Add(parameterToAdd);
		}
	}
}

public class ReflectedEnumType : ReflectedType
{
	private TypeReference underlyingType;
	
	public TypeReference UnderlyingType
	{
		get
		{
			if (underlyingType == null || !underlyingType.IsValid())
				underlyingType = TypeReference.To(Enum.GetUnderlyingType(reflectedType));
			return underlyingType;
		}
	}

	public ReflectedEnumType(Type type, string typeName, string typeNamespace = null)
		: base(type, typeName, typeNamespace)
	{
	}
}

public class ReflectedType : TypeDefinition
{
	protected readonly Type reflectedType;
	public Type GetReflectedType() { return reflectedType; }

	private bool allPublicMembersReflected;
	private bool allNonPublicMembersReflected;
	private bool allExtensionMethodsImported;

	//private static Dictionary<Type, ReflectedType> allReflectedTypes = new Dictionary<Type, ReflectedType>(16000);

	public ReflectedType(Type type, string typeName, string typeNamespace = null)
	{
		//allReflectedTypes[type] = this;
		
		reflectedType = type;
		modifiers = type.IsNested ?
			(	type.IsNestedPublic ? Modifiers.Public :
				type.IsNestedFamORAssem ? Modifiers.Internal | Modifiers.Protected :
				type.IsNestedAssembly ? Modifiers.Internal :
				type.IsNestedFamily ? Modifiers.Protected :
				Modifiers.Private)
			:
			(	type.IsPublic ? Modifiers.Public : Modifiers.Internal );
		if (type.IsAbstract && type.IsSealed)
			modifiers |= Modifiers.Static;
		else if (type.IsAbstract)
			modifiers |= Modifiers.Abstract;
		else if (type.IsSealed)
			modifiers |= Modifiers.Sealed;
		accessLevel = AccessLevelFromModifiers(modifiers);
		
		var generic = typeName.IndexOf('`');
		name = generic < 0 ? new CharSpan(typeName) : new CharSpan(typeName, 0, generic);
		name = name.Replace("[*]", "[]");
		if (type.IsInterface)
			kind = SymbolKind.Interface;
		else if (type.IsClass)
		{
			kind = SymbolKind.Class;
			if (type.BaseType == typeof(System.MulticastDelegate))
			{
				kind = SymbolKind.Delegate;
			}
		}
		else if (type.IsEnum)
			kind = SymbolKind.Enum;
		else if (type.IsValueType)
			kind = SymbolKind.Struct;
		else
			kind = SymbolKind.None;

//		if (type.IsArray)
//			Debug.LogError("ReflectedType is Array " + name);

		//if (!type.IsGenericTypeDefinition && type.IsGenericType)
		//	UnityEngine.Debug.LogError("Creating ReflectedType instead of ConstructedTypeDefinition from " + type.FullName);

		if (type.IsGenericTypeDefinition)// || type.IsGenericType)
		{
			var gtd = type.GetGenericTypeDefinition() ?? type;
			var tp = gtd.GetGenericArguments();
			var numGenericArgs = tp.Length;
			var declaringType = gtd.DeclaringType;
			if (declaringType != null && declaringType.IsGenericType)
			{
				var parentArgs = declaringType.GetGenericArguments();
				numGenericArgs -= parentArgs.Length;
			}

			if (numGenericArgs > 0)
			{
				typeParameters = new List<TypeParameterDefinition>(numGenericArgs);
				for (var i = tp.Length - numGenericArgs; i < tp.Length; ++i)
				{
					var tpDef = new TypeParameterDefinition { kind = SymbolKind.TypeParameter, name = tp[i].Name, parentSymbol = this };
					typeParameters.Add(tpDef);
				}
			}
		}
		
		if (kind == SymbolKind.Class && IsStatic && NumTypeParameters == 0 && !type.IsNested)
		{
			if (type.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
			{
				if (typeNamespace == null)
					typeNamespace = type.Namespace;
				var assemblyDefinition = AssemblyDefinition.FromAssembly(type.Assembly);
				parentSymbol = string.IsNullOrEmpty(typeNamespace) ? assemblyDefinition.GlobalNamespace : assemblyDefinition.FindNamespace(typeNamespace);
				
				ReflectAllExtensionMethods();
			}
		}

		if (kind == SymbolKind.Delegate)
		{
			var methodGroup = Create(SymbolKind.MethodGroup, ".ctor") as MethodGroupDefinition;
			methodGroup.parentSymbol = this;
			members[".ctor", 0] = methodGroup;

			defaultConstructor = new MethodDefinition
			{
				kind = SymbolKind.Constructor,
				parentSymbol = methodGroup,
				name = ".ctor",
				accessLevel = AccessLevel.Public,
				modifiers = Modifiers.Public,
			};

			defaultConstructor.parameters = new List<ParameterDefinition>();
			var parameter = (ParameterDefinition) Create(SymbolKind.Parameter, "method");
			parameter.type = TypeReference.To(this);
			parameter.parentSymbol = defaultConstructor;
			defaultConstructor.parameters.Add(parameter);

			methodGroup.AddMethod(defaultConstructor);

			// Predefined operators
			methodGroup = Create(SymbolKind.MethodGroup, "op_Addition") as MethodGroupDefinition;
			methodGroup.parentSymbol = this;
			members[methodGroup.name, 0] = methodGroup;
			var method = MethodDefinition.CreateOperator(methodGroup.name, this, this, this);
			methodGroup.AddMethod(method);

			methodGroup = Create(SymbolKind.MethodGroup, "op_Subtraction") as MethodGroupDefinition;
			methodGroup.parentSymbol = this;
			members[methodGroup.name, 0] = methodGroup;
			method = MethodDefinition.CreateOperator(methodGroup.name, this, this, this);
			methodGroup.AddMethod(method);
		}
		else if (kind == SymbolKind.Enum)
		{
			// Predefined operators
			EnumTypeDefinition.AddPredefinedOperators(this);
		}
	}

	public override TypeDefinitionBase BaseType()
	{
		if (this == builtInTypes_object)
		{
			return null;
		}

		if (resolvingBaseType)
			return null;
		resolvingBaseType = true;
		
		if (baseType != null && (baseType.definition == null || !baseType.definition.IsValid()))
		{
			baseType = null;
			interfaces = null;
		}
		else if (interfaces != null)
		{
			for (var i = interfaces.Count; i --> 0;)
			{
				var _interface = interfaces[i];
				if (_interface == null || !_interface.IsValid())
				{
					baseType = null;
					interfaces = null;
					break;
				}
			}
		}
		
		if (baseType == null && interfaces == null && this != builtInTypes_object)
		{
			baseType = TypeReference.To(reflectedType.BaseType != null ? reflectedType.BaseType : typeof(object));

			interfaces = new List<TypeReference>();
			var implements = reflectedType.GetInterfaces();
			for (var i = 0; i < implements.Length; ++i)
				interfaces.Add(TypeReference.To(implements[i]));
		}
		
		var result = baseType != null ? baseType.definition as TypeDefinitionBase : base.BaseType();
		if (result == this)
		{
			baseType = TypeReference.To(circularBaseType);
			result = circularBaseType;
		}
		resolvingBaseType = false;
		return result;
	}
	
	//private Dictionary<int, SymbolDefinition> importedMembers;
	private SymbolDefinition ImportReflectedMember(Type info, bool importInternal, bool importPrivate)
	{
		if (!importPrivate)
		{
			if (info.IsNestedPrivate)
				return null;
			
			if (!importInternal)
				if (info.IsNestedAssembly || info.IsNestedFamANDAssem)
					return null;
		}
		
		SymbolDefinition imported = null;

		//if (importedMembers == null)
		//	importedMembers = new Dictionary<int, SymbolDefinition>();
		//else if (importedMembers.TryGetValue(info.MetadataToken, out imported))
		//	return imported;

		imported = ImportReflectedType(info);
		
		//members[imported.name, imported.NumTypeParameters] = imported;
		//importedMembers[info.MetadataToken] = imported;
		return imported;
	}

	private SymbolDefinition ImportReflectedMember(FieldInfo info, bool importInternal, bool importPrivate)
	{
		var isPrivate = info.IsPrivate;

		if (!importPrivate)
		{
			if (isPrivate)
				return null;

			if (!importInternal)
				if (info.IsAssembly || info.IsFamilyAndAssembly)
					return null;
		}
		if (kind == SymbolKind.Enum && !info.IsStatic)
			return null;
		
		if (isPrivate)
		{
			if (info.Name[0] == '<')
				return null;

			try
			{
				if (info.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null)
					return null;
			}
			catch
			{
				return null;
			}
		}

		SymbolDefinition imported = new ReflectedMember(info, this);
		
		members[imported.name, imported.NumTypeParameters] = imported;
		return imported;
	}

	private SymbolDefinition ImportReflectedMember(PropertyInfo info, bool importInternal, bool importPrivate)
	{
		var get = info.GetGetMethod(true);
		var set = info.GetSetMethod(true);
		
		if (!importPrivate)
		{
			if ((get == null || get.IsPrivate) && (set == null || set.IsPrivate))
				return null;

			if (!importInternal)
				if (get == null || get.IsAssembly || get.IsFamilyAndAssembly)
					if (set == null || set.IsAssembly || set.IsFamilyAndAssembly)
						return null;
		}
		
		SymbolDefinition imported = null;

		//if (importedMembers == null)
		//	importedMembers = new Dictionary<int, SymbolDefinition>();
		//else if (importedMembers.TryGetValue(info.MetadataToken, out imported))
		//	return imported;

		imported = new ReflectedMember(info, this);
		
		members[imported.name, imported.NumTypeParameters] = imported;
		//importedMembers[info.MetadataToken] = imported;
		return imported;
	}

	private SymbolDefinition ImportReflectedMember(EventInfo info, bool importInternal, bool importPrivate)
	{
		var add = info.GetAddMethod(true);
		var remove = info.GetRemoveMethod(true);

		if (!importPrivate)
		{
			if ((add == null || add.IsPrivate) && (remove == null || remove.IsPrivate))
				return null;

			if (!importInternal)
				if (add == null || add.IsAssembly || add.IsFamilyAndAssembly)
					if (remove == null || remove.IsAssembly || remove.IsFamilyAndAssembly)
						return null;
		}
		
		SymbolDefinition imported = null;

		//if (importedMembers == null)
		//	importedMembers = new Dictionary<int, SymbolDefinition>();
		//else if (importedMembers.TryGetValue(info.MetadataToken, out imported))
		//	return imported;

		imported = new ReflectedMember(info, this);
		
		members[imported.name, imported.NumTypeParameters] = imported;
		//importedMembers[info.MetadataToken] = imported;
		return imported;
	}

	private SymbolDefinition ImportReflectedMember(MethodInfo info, bool importInternal, bool importPrivate)
	{
		if (!importPrivate)
		{
			if (info.IsPrivate)
				return null;
			
			if (!importInternal)
				if (info.IsAssembly || info.IsFamilyAndAssembly)
					return null;
		}
		
		SymbolDefinition imported = null;

		//if (importedMembers == null)
		//	importedMembers = new Dictionary<int, SymbolDefinition>();
		//else if (importedMembers.TryGetValue(info.MetadataToken, out imported))
		//	return imported;

		if (!info.IsGenericMethod && info.GetParameters().Length == 0 && info.Name == "Finalize")
			return null;
		
		imported = ImportReflectedMethod(info);
		
		//members[imported.name, 0] = imported;
		//importedMembers[info.MetadataToken] = imported;
		return imported;
	}
		
	public MethodGroupDefinition ImportReflectedMethod(MethodInfo info)
	{
		var importedReflectionName = info.Name;
		
		var genericRankIndex = importedReflectionName.IndexOf('`');
		if (genericRankIndex > 0)
		{
			importedReflectionName = importedReflectionName.Substring(0, genericRankIndex);
		}
		
		if (allExtensionMethodsImported &&
			!info.IsPrivate && !info.IsFamily && !info.IsFamilyAndAssembly &&
			info.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
		{
			return null;
		}

		SymbolDefinition methodGroup;
		members.TryGetValue(importedReflectionName, 0, out methodGroup);
		var asMethodGroup = methodGroup as MethodGroupDefinition;
		#if SI3_WARNINGS
		if (methodGroup != null && asMethodGroup == null)
			Debug.LogError("Si3 Error Importing Method: " + info);
		#endif
		if (asMethodGroup == null)
		{
			asMethodGroup = Create(SymbolKind.MethodGroup, importedReflectionName) as MethodGroupDefinition;
			asMethodGroup.parentSymbol = this;
			members[importedReflectionName, 0] = asMethodGroup;
		}
		var imported = new ReflectedMethod(info, asMethodGroup);
		asMethodGroup.AddMethod(imported);
		return asMethodGroup;
	}

	public SymbolDefinition ImportReflectedConstructor(ConstructorInfo info)
	{
		SymbolDefinition methodGroup;
		if (info.IsStatic)
			members.TryGetValue(".cctor", 0, out methodGroup);
		else
			members.TryGetValue(".ctor", 0, out methodGroup);
		var asMethodGroup = methodGroup as MethodGroupDefinition;
		#if SI3_WARNINGS
		if (methodGroup != null && asMethodGroup == null)
			Debug.LogError("Si3 Error Importing Constructor: " + info);
		#endif
		if (asMethodGroup == null)
		{
			asMethodGroup = Create(SymbolKind.MethodGroup, info.IsStatic ? ".cctor" : ".ctor") as MethodGroupDefinition;
			asMethodGroup.parentSymbol = this;
			members[info.IsStatic ? ".cctor" : ".ctor", 0] = asMethodGroup;
		}
		var imported = new ReflectedConstructor(info, asMethodGroup);
		asMethodGroup.AddMethod(imported);
		return asMethodGroup;
	}

	private SymbolDefinition ImportReflectedMember(ConstructorInfo info, bool importInternal, bool importPrivate)
	{
		if (!importPrivate)
		{
			if (info.IsPrivate)
				return null;
			
			if (!importInternal)
				if (info.IsAssembly || info.IsFamilyAndAssembly)
					return null;
		}

		if (kind == SymbolKind.Delegate)
			return null;

		//if (info.IsPrivate || !importInternal && info.IsAssembly)
		//	return null;
		
		SymbolDefinition imported = null;

		//if (importedMembers == null)
		//	importedMembers = new Dictionary<int, SymbolDefinition>();
		//else if (importedMembers.TryGetValue(info.MetadataToken, out imported))
		//	return imported;

		imported = ImportReflectedConstructor(info);
		
		//members[imported.name, imported.kind != SymbolKind.MethodGroup ? imported.NumTypeParameters : 0] = imported;
		//importedMembers[info.MetadataToken] = imported;
		return imported;
	}

	public override CharSpan GetName()
	{
		foreach (var kv in builtInTypes)
			if (kv.Value == this)
				return kv.Key;
		return base.GetName();
	}

	public override SymbolDefinition TypeOf()
	{
		if (kind != SymbolKind.Delegate)
			return this;
		
		GetParameters();
		return returnType.definition;
	}

	public override List<SymbolDefinition> GetAllIndexers()
	{
		if (!allPublicMembersReflected || !allNonPublicMembersReflected)
			ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
		
		return base.GetAllIndexers();
	}
	
	public override MethodDefinition GetDefaultConstructor()
	{
		if (!allPublicMembersReflected || !allNonPublicMembersReflected)
			ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
		
		return base.GetDefaultConstructor();
	}

	//protected string RankString()
	//{
	//    return reflectedType.IsArray ? '[' + new string(',', reflectedType.GetArrayRank() - 1) + ']' : string.Empty;
	//}
	
	//public override TypeDefinition MakeArrayType(int rank)
	//{
	////	Debug.LogWarning("MakeArrayType " + this + RankString());
	////	if (rank == 1)
	//        return ImportReflectedType(reflectedType.MakeArrayType(rank));
	////	return new ArrayTypeDefinition(this, rank) { kind = kind };
	//}

	public override SymbolDefinition FindName(CharSpan memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName.DecodeId();
		
		SymbolDefinition member = null;
		if (!allPublicMembersReflected || !allNonPublicMembersReflected)
			ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
		if (!members.TryGetValue(memberName, numTypeParameters, out member))
			return null;

		if (asTypeOnly && member != null && !(member is TypeDefinitionBase))
			return null;
		return member;
	}

	public void ReflectAllExtensionMethods()
	{
		var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Static;
		var allMethods = reflectedType.GetMethods(flags);
		foreach (var methodInfo in allMethods)
		{
			if (methodInfo.IsSpecialName || //IsOperatorName(methodInfo.Name))
				methodInfo.IsPrivate || methodInfo.IsFamily || methodInfo.IsFamilyAndAssembly)
			{
				continue;
			}

			if (methodInfo.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
			{
				ImportReflectedMethod(methodInfo);
			}
		}
		allExtensionMethodsImported = true;
	}

	public void ReflectAllMembers(BindingFlags flags)
	{
		if (allPublicMembersReflected && allNonPublicMembersReflected)
			return;
		
		flags |= BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Instance;
		
		if (allPublicMembersReflected)
			flags &= ~BindingFlags.Public;
		if (allNonPublicMembersReflected)
			flags &= ~BindingFlags.NonPublic;
		
		if ((flags & (BindingFlags.Public | BindingFlags.NonPublic)) == 0)
			return;
		
		var assembly = Assembly;
		var isScriptAssembly = assembly != null && assembly.isScriptAssembly;
		
		var allMembers = reflectedType.GetMembers(flags);
		//var allMembers = reflectedType.GetTypeInfo().DeclaredMembers;

		MethodInfo methodInfo;
		bool importInternals = (flags & BindingFlags.NonPublic) != 0;
		foreach (var member in allMembers)
		{
			switch (member.MemberType)
			{
			case MemberTypes.Method:
				methodInfo = (MethodInfo)member;
				if (!methodInfo.IsSpecialName || IsOperatorName(methodInfo.Name))
					ImportReflectedMember(methodInfo, importInternals, isScriptAssembly);
				break;
			case MemberTypes.Field:
				ImportReflectedMember((FieldInfo)member, importInternals, isScriptAssembly);
				break;
			case MemberTypes.Property:
				ImportReflectedMember((PropertyInfo)member, importInternals, isScriptAssembly);
				break;
			case MemberTypes.Event:
				ImportReflectedMember((EventInfo)member, importInternals, isScriptAssembly);
				break;
			case MemberTypes.Constructor:
				ImportReflectedMember((ConstructorInfo)member, importInternals, isScriptAssembly);
				break;
			case MemberTypes.NestedType:
				ImportReflectedMember((Type)member, importInternals, isScriptAssembly);
				break;
			case MemberTypes.TypeInfo:
				#if SI3_WARNINGS
				Debug.Log(member);
				#endif
				break;
			default:
				break;
			}
		}
		//foreach (var m in reflectedType.GetNestedTypes(flags))
		//	ImportReflectedMember(m, importInternals);
		//foreach (var m in reflectedType.GetFields(flags))
		//	ImportReflectedMember(m, importInternals);
		//foreach (var m in reflectedType.GetProperties(flags))
		//	ImportReflectedMember(m, importInternals);
		//foreach (var m in reflectedType.GetEvents(flags))
		//	ImportReflectedMember(m, importInternals);
		//foreach (var m in reflectedType.GetMethods(flags))
		//	if (!m.IsSpecialName || IsOperatorName(m.Name))
		//		ImportReflectedMember(m, importInternals);
		//foreach (var m in reflectedType.GetConstructors(flags))
		//	ImportReflectedMember(m, importInternals);

		if ((flags & BindingFlags.Public) == BindingFlags.Public)
			allPublicMembersReflected = true;
		if ((flags & BindingFlags.NonPublic) == BindingFlags.NonPublic)
			allNonPublicMembersReflected = true;
	}

	private TypeReference returnType;
	private List<ParameterDefinition> parameters;
	public override List<ParameterDefinition> GetParameters()
	{
		if (kind != SymbolKind.Delegate)
			return null;
		
		if (parameters == null)
		{
			var invoke = reflectedType.GetMethod("Invoke");
			
			returnType = TypeReference.To(invoke.ReturnType);
			
			parameters = new List<ParameterDefinition>();
			foreach (var p in invoke.GetParameters())
			{
				var isByRef = p.ParameterType.IsByRef;
				var parameterType = isByRef ? p.ParameterType.GetElementType() : p.ParameterType;
				parameters.Add(new ParameterDefinition
				{
					kind = SymbolKind.Parameter,
					parentSymbol = this,
					name = p.Name,
					type = TypeReference.To(parameterType),
					modifiers = isByRef ? (p.IsOut ? Modifiers.Out : p.IsIn ? Modifiers.In : Modifiers.Ref) : parameterType.IsArray && p.IsDefined(typeof(ParamArrayAttribute), false) ? Modifiers.Params : Modifiers.None,
				});
			}
		}
		
		return parameters;
	}

	private string delegateInfoText;
	public override string GetDelegateInfoText()
	{
		if (delegateInfoText == null)
		{
			var parameters = GetParameters();
			var returnType = TypeOf();
			
			delegateInfoText = returnType.GetName() + " " + GetName() + (parameters.Count == 1 ? "( " : "(");
			delegateInfoText += PrintParameters(parameters) + (parameters.Count == 1 ? " )" : ")");
		}

		return delegateInfoText;
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		if (!allPublicMembersReflected)
		{
			if (!allNonPublicMembersReflected)
				ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
			else
				ReflectAllMembers(BindingFlags.Public);
		}
		else if (!allNonPublicMembersReflected)
		{
			ReflectAllMembers(BindingFlags.NonPublic);
		}

		base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
	}

	//private Dictionary<BindingFlags, Dictionary<string, SymbolDefinition>> cachedMemberCompletions;
	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		if (!allPublicMembersReflected)
		{
			if (!allNonPublicMembersReflected && ((mask & AccessLevelMask.NonPublic) != 0 || (flags & BindingFlags.NonPublic) != 0))
				ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
			else
				ReflectAllMembers(BindingFlags.Public);
		}
		else if (!allNonPublicMembersReflected && ((mask & AccessLevelMask.NonPublic) != 0 || (flags & BindingFlags.NonPublic) != 0))
		{
			ReflectAllMembers(BindingFlags.NonPublic);
		}
		
		base.GetMembersCompletionData(data, flags, mask, context);

		//if ((mask & AccessLevelMask.Public) != 0)
		//{
		//	if (assembly.InternalsVisibleIn(this.Assembly))
		//		mask |= AccessLevelMask.Internal;
		//	else
		//		mask &= ~AccessLevelMask.Internal;
		//}
		
		//if (cachedMemberCompletions == null)
		//	cachedMemberCompletions = new Dictionary<BindingFlags, Dictionary<string, SymbolDefinition>>();
		//if (!cachedMemberCompletions.ContainsKey(flags))
		//{
		//	var cache = cachedMemberCompletions[flags] = new Dictionary<string, SymbolDefinition>();
		//	base.GetMembersCompletionData(cache, flags, mask, assembly);
		//}

		//var completions = cachedMemberCompletions[flags];
		//foreach (var entry in completions)
		//	if (entry.Value.IsAccessible(mask) && !data.ContainsKey(entry.Key))
		//		data.Add(entry.Key, entry.Value);
	}
}

public class ConstructedInstanceDefinition : InstanceDefinition
{
	public readonly InstanceDefinition genericSymbol;

	public ConstructedInstanceDefinition(InstanceDefinition genericSymbolDefinition)
	{
		genericSymbol = genericSymbolDefinition;
		kind = genericSymbol.kind;
		modifiers = genericSymbol.modifiers;
		accessLevel = genericSymbol.accessLevel;
		name = genericSymbol.name;
	}

	public override SymbolDefinition TypeOf()
	{
		var result = genericSymbol.TypeOf() as TypeDefinitionBase;

		var ctx = parentSymbol as ConstructedTypeDefinition;
		if (ctx != null && result != null)
			result = result.SubstituteTypeParameters(ctx);

		return result;
	}
	
	public override SymbolDefinition GetGenericSymbol()
	{
		return genericSymbol;
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		var symbolType = TypeOf() as TypeDefinitionBase;
		if (symbolType != null)
			symbolType.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		var symbolType = TypeOf();
		if (symbolType != null)
			symbolType.GetMembersCompletionData(data, BindingFlags.Instance, mask, context);
	}
}
	
public static class ListExtensions
{
	public static T FirstOrDefault<T>(this List<T> self)
	{
		return self.Count == 0 ? default(T) : self[0];
	}
	
	public static T ElementAtOrDefault<T>(this List<T> self, int index)
	{
		return index >= self.Count ? default(T) : self[index];
	}

	public static T FirstByName<T>(this List<T> self, string name) where T : SymbolDefinition
	{
		var count = self.Count;
		for (var i = 0; i < count; i++)
			if (self[i].name == name)
				return self[i];
		return null;
	}

	public static T LastByName<T>(this List<T> self, string name) where T : SymbolDefinition
	{
		for (var i = self.Count; i --> 0;)
			if (self[i].name == name)
				return self[i];
		return null;
	}
}

public abstract class IntegerLiteralType : TypeDefinitionBase
{
	public IntegerLiteralType()
	{
		name = "int";
		kind = TypeOf().kind;
		parentSymbol = TypeOf().parentSymbol;
	}

	public override string GetTooltipText(bool fullText = true)
	{
		return builtInTypes_int.GetTooltipText(false);
	}
	
	public override SymbolDefinition TypeOf()
	{
		return builtInTypes_int;
	}

	public abstract bool IsSameLiteralType(TypeDefinitionBase type);

	public override bool CanConvertTo(TypeDefinitionBase otherType)
	{
		return
			this == otherType ||
			IsSameLiteralType(otherType) ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double ||
			otherType == builtInTypes_decimal ||
			base.CanConvertTo(otherType);
	}
}

public class IntegerLiteralTypeZero : IntegerLiteralType
{
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return type == this || type == builtInTypes_int;
	}

	public override bool IsSameLiteralType(TypeDefinitionBase type)
	{
		var typeOfType = type.TypeOf();
		return
			typeOfType == builtInTypes_int ||
			typeOfType == builtInTypes_uint ||
			typeOfType == builtInTypes_short ||
			typeOfType == builtInTypes_ushort ||
			typeOfType == builtInTypes_byte ||
			typeOfType == builtInTypes_sbyte ||
			typeOfType == builtInTypes_long ||
			typeOfType == builtInTypes_ulong ||
			typeOfType.kind == SymbolKind.Enum;
	}
}

public class IntegerLiteralTypeByteOrSByte : IntegerLiteralType
{
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return type == this || type == builtInTypes_int;
	}

	public override bool IsSameLiteralType(TypeDefinitionBase type)
	{
		var typeOfType = type.TypeOf();
		return
			typeOfType == builtInTypes_int ||
			typeOfType == builtInTypes_uint ||
			typeOfType == builtInTypes_short ||
			typeOfType == builtInTypes_ushort ||
			typeOfType == builtInTypes_byte ||
			typeOfType == builtInTypes_sbyte ||
			typeOfType == builtInTypes_long ||
			typeOfType == builtInTypes_ulong;
	}
}

public class IntegerLiteralTypeByte : IntegerLiteralType
{
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return type == this || type == builtInTypes_int;
	}

	public override bool IsSameLiteralType(TypeDefinitionBase type)
	{
		var typeOfType = type.TypeOf();
		return
			typeOfType == builtInTypes_int ||
			typeOfType == builtInTypes_uint ||
			typeOfType == builtInTypes_short ||
			typeOfType == builtInTypes_ushort ||
			typeOfType == builtInTypes_byte ||
			typeOfType == builtInTypes_long ||
			typeOfType == builtInTypes_ulong;
	}
}

public class IntegerLiteralTypeSByte : IntegerLiteralType
{
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return type == this || type == builtInTypes_int;
	}

	public override bool IsSameLiteralType(TypeDefinitionBase type)
	{
		var typeOfType = type.TypeOf();
		return
			typeOfType == builtInTypes_int ||
			typeOfType == builtInTypes_short ||
			typeOfType == builtInTypes_sbyte ||
			typeOfType == builtInTypes_long;
	}
}

public class IntegerLiteralTypeShortOrUShort : IntegerLiteralType
{
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return type == this || type == builtInTypes_int;
	}

	public override bool IsSameLiteralType(TypeDefinitionBase type)
	{
		var typeOfType = type.TypeOf();
		return
			typeOfType == builtInTypes_int ||
			typeOfType == builtInTypes_uint ||
			typeOfType == builtInTypes_short ||
			typeOfType == builtInTypes_ushort ||
			typeOfType == builtInTypes_long ||
			typeOfType == builtInTypes_ulong;
	}
}

public class IntegerLiteralTypeShort : IntegerLiteralType
{
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return type == this || type == builtInTypes_int;
	}

	public override bool IsSameLiteralType(TypeDefinitionBase type)
	{
		var typeOfType = type.TypeOf();
		return
			typeOfType == builtInTypes_int ||
			typeOfType == builtInTypes_short ||
			typeOfType == builtInTypes_long;
	}
}

public class IntegerLiteralTypeUShort : IntegerLiteralType
{
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return type == this || type == builtInTypes_int;
	}

	public override bool IsSameLiteralType(TypeDefinitionBase type)
	{
		var typeOfType = type.TypeOf();
		return
			typeOfType == builtInTypes_int ||
			typeOfType == builtInTypes_uint ||
			typeOfType == builtInTypes_ushort ||
			typeOfType == builtInTypes_long ||
			typeOfType == builtInTypes_ulong;
	}
}

public class IntegerLiteralTypeIntOrUInt : IntegerLiteralType
{
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return type == this || type == builtInTypes_int;
	}

	public override bool IsSameLiteralType(TypeDefinitionBase type)
	{
		var typeOfType = type.TypeOf();
		return
			typeOfType == builtInTypes_int ||
			typeOfType == builtInTypes_uint ||
			typeOfType == builtInTypes_long ||
			typeOfType == builtInTypes_ulong;
	}
}

public class IntegerLiteralTypeInt : IntegerLiteralType
{
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return type == this || type == builtInTypes_int;
	}

	public override bool IsSameLiteralType(TypeDefinitionBase type)
	{
		var typeOfType = type.TypeOf();
		return
			typeOfType == builtInTypes_int ||
			typeOfType == builtInTypes_long;
	}
}

public class IntegerLiteralTypeUInt : IntegerLiteralType
{
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return type == this || type == builtInTypes_uint;
	}

	public IntegerLiteralTypeUInt()
	{
		name = "uint";
	}

	public override string GetTooltipText(bool fullText = true)
	{
		return builtInTypes_uint.GetTooltipText(false);
	}

	public override SymbolDefinition TypeOf()
	{
		return builtInTypes_uint;
	}

	public override bool IsSameLiteralType(TypeDefinitionBase type)
	{
		var typeOfType = type.TypeOf();
		return
			typeOfType == builtInTypes_uint ||
			typeOfType == builtInTypes_long ||
			typeOfType == builtInTypes_ulong;
	}
}

public class IntegerLiteralTypeLongOrULong : IntegerLiteralType
{
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return type == this || type == builtInTypes_long;
	}

	public IntegerLiteralTypeLongOrULong()
	{
		name = "long";
	}

	public override string GetTooltipText(bool fullText = true)
	{
		return builtInTypes_long.GetTooltipText(false);
	}

	public override SymbolDefinition TypeOf()
	{
		return builtInTypes_long;
	}

	public override bool IsSameLiteralType(TypeDefinitionBase type)
	{
		var typeOfType = type.TypeOf();
		return
			typeOfType == builtInTypes_long ||
			typeOfType == builtInTypes_ulong;
	}
}

public static class IntegerLiteral
{
	private static readonly IntegerLiteralTypeZero Zero = new IntegerLiteralTypeZero();
	private static readonly IntegerLiteralTypeByteOrSByte ByteOrSByte = new IntegerLiteralTypeByteOrSByte();
	private static readonly IntegerLiteralTypeByte Byte = new IntegerLiteralTypeByte();
	private static readonly IntegerLiteralTypeSByte SByte = new IntegerLiteralTypeSByte();
	private static readonly IntegerLiteralTypeShortOrUShort ShortOrUShort = new IntegerLiteralTypeShortOrUShort();
	private static readonly IntegerLiteralTypeUShort UShort = new IntegerLiteralTypeUShort();
	private static readonly IntegerLiteralTypeShort Short = new IntegerLiteralTypeShort();
	private static readonly IntegerLiteralTypeIntOrUInt IntOrUInt = new IntegerLiteralTypeIntOrUInt();
	private static readonly IntegerLiteralTypeUInt UInt = new IntegerLiteralTypeUInt();
	private static readonly IntegerLiteralTypeInt Int = new IntegerLiteralTypeInt();
	private static readonly IntegerLiteralTypeLongOrULong LongOrULong = new IntegerLiteralTypeLongOrULong();

	public static SymbolDefinition FromText(string text)
	{
		var isNegative = text[0] == '-';
		var isHex = text.StartsWithIgnoreCase(isNegative ? "-0x" : "0x");
		var isBinary = text.StartsWithIgnoreCase(isNegative ? "-0b" : "0b");
		text = isNegative ? text.Substring(isHex || isBinary ? 3 : 1) : isHex || isBinary ? text.Substring(2) : text;
		text = text.Replace("_", "");
		ulong value;
		if (isBinary)
		{
			value = 0;
			for (int i = 0; i < text.Length; ++i)
			{
				if ((value & 0x8000000000000000) != 0)
					return null; // integer literal is too large
				value = value << 1;
				var c = text[i];
				if (c == '1')
					value |= 1;
			}
		}
		else if (!ulong.TryParse(
			text,
			isHex ? System.Globalization.NumberStyles.AllowHexSpecifier : System.Globalization.NumberStyles.None,
			System.Globalization.NumberFormatInfo.InvariantInfo,
			out value))
		{
			return null; // integer literal is too large
		}
		if (value == 0UL)
		{
			return Zero.GetThisInstance();
		}
		if (isNegative)
		{
			if (value <= 128UL)
				return SByte.GetThisInstance();
			else if (value <= 32768UL)
				return Short.GetThisInstance();
			else if (value <= 0x80000000UL)
				return Int.GetThisInstance();
			else if (value <= 0x8000000000000000UL)
				return SymbolDefinition.builtInTypes_long.GetThisInstance();
			else
				return SymbolDefinition.builtInTypes_int.GetThisInstance();
		}
		else
		{
			if (value <= 127UL)
				return ByteOrSByte.GetThisInstance();
			else if (value <= 255UL)
				return Byte.GetThisInstance();
			else if (value <= 32767UL)
				return ShortOrUShort.GetThisInstance();
			else if (value <= 65535UL)
				return UShort.GetThisInstance();
			else if (value <= 0x7fffffffUL)
				return IntOrUInt.GetThisInstance();
			else if (value <= 0xffffffffUL)
				return UInt.GetThisInstance();
			else if (value <= 0x7fffffffffffffffUL)
				return LongOrULong.GetThisInstance();
			else
				return SymbolDefinition.builtInTypes_ulong.GetThisInstance();
		}
	}
}

public class InstanceDefinition : SymbolDefinition
{
	public TypeReference type;
	private bool _resolvingTypeOf = false;

	public override SymbolDefinition TypeOf()
	{
		if (_resolvingTypeOf)
		{
			//Debug.Log("Resolving type of " + GetName());
			return unknownType;
		}
		_resolvingTypeOf = true;
		
		if (type == null || type.IsNullOrInvalidated())
		//	type = null;
		
		//if (type == null)
		{
			//type = new TypeReference();

		//	var parentDefinition = parentScope as SymbolDefinition;
		//	if (parentDefinition.declarations.Count > 0)
			{
				SymbolDeclaration decl = declarations;
				if (decl != null && decl.parseTreeNode != null && decl.parseTreeNode.parent != null)
				{
					ParseTree.BaseNode typeNode = null;
					switch (decl.kind)
					{
						case SymbolKind.Parameter:
							if (decl.parseTreeNode.RuleName == "implicitAnonymousFunctionParameter")
							{
								type = TypeOfImplicitParameter(decl);
							}
							else
							{
								typeNode = decl.parseTreeNode.FindChildByName("type");
								type = typeNode != null ? TypeReference.To(typeNode) : null;//"System.Object" };
							}
							break;

						case SymbolKind.Field:
							typeNode = decl.parseTreeNode.parent.parent.parent.FindChildByName("type");
							type = typeNode != null ? TypeReference.To(typeNode) : null;//"System.Object" };
							break;

						case SymbolKind.EnumMember:
							type = TypeReference.To(parentSymbol);
							break;

						case SymbolKind.ConstantField:
						case SymbolKind.LocalConstant:
							//typeNode = decl.parseTreeNode.parent.parent.ChildAt(1);
							//break;
							switch (decl.parseTreeNode.parent.parent.RuleName)
							{
								case "constantDeclaration":
								case "localConstantDeclaration":
									typeNode = decl.parseTreeNode.parent.parent.ChildAt(1);
									break;

								default:
									typeNode = decl.parseTreeNode.parent.parent.parent.FindChildByName("IDENTIFIER");
									break;
							}
							type = typeNode != null ? TypeReference.To(typeNode) : null;
							break;

						case SymbolKind.Property:
						case SymbolKind.Indexer:
							typeNode = decl.parseTreeNode.parent.FindChildByName("type");
							type = typeNode != null ? TypeReference.To(typeNode) : null;
							break;

						case SymbolKind.Event:
							if (decl.parseTreeNode.RuleName == "interfaceEventDeclarator")
								typeNode = decl.parseTreeNode.parent.parent.ChildAt(1);
							else
								typeNode = decl.parseTreeNode.FindParentByName("eventDeclaration").ChildAt(1);
							type = typeNode != null ? TypeReference.To(typeNode) : null;
							break;
						
						case SymbolKind.Variable:
							if (decl.parseTreeNode.parent.parent != null)
								typeNode = decl.parseTreeNode.parent.parent.FindChildByName("localVariableType");
							type = typeNode != null ? TypeReference.To(typeNode) : null;
							break;
							
						case SymbolKind.TupleDeconstructVariable:
							if (decl.parseTreeNode.parent != null)
							{
								var isImplicitDeconstruction = decl.parseTreeNode.RuleName == "implicitDeconstructVariableDeclarator";
								
								var deconstructListNode = isImplicitDeconstruction
									? decl.parseTreeNode.FindParentByName("implicitDeconstructList")
									: decl.parseTreeNode.FindParentByName("explicitDeconstructList");
								if (deconstructListNode == null)
									break;
								
								var deconstructNode = deconstructListNode.parent;
								if (deconstructNode == null)
									break;

								if (!isImplicitDeconstruction)
								{
									while (deconstructNode.RuleName == "explicitDeconstructTarget")
									{
										deconstructListNode = deconstructNode.parent;
										deconstructNode = deconstructListNode.parent;
									}
								}
								
								if (deconstructNode.RuleName == "foreachStatement")
								{
									if (isImplicitDeconstruction)
									{
										var varNode = deconstructNode.NodeAt(2);
										if (varNode != null)
										{
											var varLeaf = varNode.LeafAt(0);
											if (varLeaf != null)
											{
												var tupleType = varLeaf.resolvedSymbol as TupleTypeDefinition;
												if (tupleType == null)
													break;

												var targetNode = decl.parseTreeNode.parent;
												var elementType = tupleType.TypeOfDeconstructNode(targetNode);
								
												if (elementType != null)
													type = TypeReference.To(elementType);
											}
										}
									}
									else
									{
										typeNode = decl.parseTreeNode.parent.NodeAt(0);
										type = typeNode != null ? TypeReference.To(typeNode) : null;
									}
								}
								else
								{
									deconstructNode = isImplicitDeconstruction
										? deconstructListNode.FindParentByName("implicitDeconstructDeclaration")
										: deconstructListNode.FindParentByName("explicitDeconstructDeclaration");

									if (deconstructNode == null)
										break;
									
									if (deconstructNode.resolvedSymbol == null || deconstructNode.resolvedSymbol.kind == SymbolKind.Error)
										ResolveNode(deconstructNode, null, null, 0, false);
									
									var tupleType = deconstructNode.resolvedSymbol as TupleTypeDefinition;
									if (tupleType == null)
										break;

									var targetNode = decl.parseTreeNode.parent;
									if (!isImplicitDeconstruction)
										targetNode = targetNode.parent;
									TypeDefinitionBase elementType = tupleType.TypeOfDeconstructNode(targetNode);
									
									if (elementType != null)
										type = TypeReference.To(elementType);
								}
							}
							break;

						case SymbolKind.OutVariable:
							if (decl.parseTreeNode.parent != null)
								typeNode = decl.parseTreeNode.parent.NodeAt(0);
							type = typeNode != null ? TypeReference.To(typeNode) : null;
							break;

						case SymbolKind.IsVariable:
							if (decl.parseTreeNode.parent != null)
								typeNode = decl.parseTreeNode.parent.NodeAt(decl.parseTreeNode.childIndex - 1);
							type = typeNode != null ? TypeReference.To(typeNode) : null;
							if (!(type.definition is TypeDefinitionBase))
							{
								type = TypeReference.To(unknownType);
								decl.parseTreeNode.firstChild.semanticError = "unknown type";
							}
							break;

						case SymbolKind.CaseVariable:
							typeNode = decl.parseTreeNode.parent.FindChildByName("localVariableType");
							type = typeNode != null ? TypeReference.To(typeNode) : null;
							break;

						case SymbolKind.ForEachVariable:
							typeNode = decl.parseTreeNode.FindChildByName("localVariableType");
							type = typeNode != null ? TypeReference.To(typeNode) : null;
							break;

						case SymbolKind.FromClauseVariable:
							typeNode = decl.parseTreeNode.FindChildByName("type");
							type = typeNode != null
								? TypeReference.To(typeNode)
								: TypeReference.To(EnumerableElementType(decl.parseTreeNode.NodeAt(-1)));
							break;

						case SymbolKind.CatchParameter:
							typeNode = decl.parseTreeNode.parent.FindChildByName("exceptionClassType");
							type = typeNode != null ? TypeReference.To(typeNode) : null;
							break;

						default:
							Debug.LogError(decl.kind);
							break;
					}
				}
			}
		}

		var result = type != null ? type.definition : unknownType;
		_resolvingTypeOf = false;
		return result;
	}

	private TypeReference TypeOfImplicitParameter(SymbolDeclaration declaration)
	{
		int index = 0;
		var node = declaration.parseTreeNode;
		if (node.parent.RuleName == "implicitAnonymousFunctionParameterList")
		{
			index = node.childIndex / 2;
			node = node.parent;
		}
		node = node.parent; // anonymousFunctionSignature
		node = node.parent; // lambdaExpression
		node = node.parent; // nonAssignmentExpression
		node = node.parent; // elementInitializer or expression or keyValueInitializer

		bool dictInit = false;
		if (node.RuleName == "expression" && node.parent.RuleName == "expressionList" && node.parent.parent.RuleName == "elementInitializer")
		{
			node = node.parent.parent;
			dictInit = true;
		}
		else if (node.RuleName == "keyValueInitializer")
		{
			dictInit = true;
		}

		if (dictInit || node.RuleName == "elementInitializer")
		{
			node = node.parent // elementInitializerList
				.parent // collectionInitializer
				.parent // objectOrCollectionInitializer
				.parent // objectCreationExpression
				.parent; // primaryExpression
			if (node.RuleName != "primaryExpression")
				return null;

			node = node.NodeAt(1);
			if (node == null || node.RuleName != "nonArrayType")
				return null;

			var collectionType = ResolveNode(node.ChildAt(0)).TypeOf() as TypeDefinitionBase;
			if (collectionType != null && collectionType.kind != SymbolKind.Error)
			{
				var enumerableType = collectionType.ConvertTo(builtInTypes_IEnumerable_1) as ConstructedTypeDefinition;

				var targetTypeReference = enumerableType == null || enumerableType.typeArguments == null ? null : enumerableType.typeArguments.FirstOrDefault();
				var targetType = targetTypeReference == null ? null : targetTypeReference.definition;

				if (dictInit && targetType.NumTypeParameters == 2)
				{
					targetType = targetType.GetTypeArgument(1);
				}

				if (targetType != null && targetType.GetGenericSymbol() == builtInTypes_Expression_1)
				{
					targetType = targetType.GetTypeArgument(0);
				}
				
				if (targetType != null && targetType.kind == SymbolKind.Delegate)
				{
					var delegateParameters = targetType.GetParameters();
					if (delegateParameters != null && index < delegateParameters.Count)
					{
						var type = delegateParameters[index].TypeOf();
						type = type.SubstituteTypeParameters(targetType);
						return TypeReference.To(type);
					}
				}
			}
		}
		else if (node.RuleName == "expression" && node.parent.RuleName == "assignment")
		{
			var targetSymbol = node.parent.ChildAt(0).resolvedSymbol ?? ResolveNode(node.parent.ChildAt(0));
			if (targetSymbol != null && targetSymbol.kind != SymbolKind.Error)
			{
				var targetType = targetSymbol;
				if (targetType != null && targetType.GetGenericSymbol() == builtInTypes_Expression_1)
				{
					targetType = targetType.GetTypeArgument(0);
				}
				
				targetType = targetType != null && targetType.kind == SymbolKind.Delegate ? targetType : targetSymbol.TypeOf();
				if (targetType != null && targetType.kind == SymbolKind.Delegate)
				{
					var delegateParameters = targetType.GetParameters();
					if (delegateParameters != null && index < delegateParameters.Count)
					{
						var type = delegateParameters[index].TypeOf();
						type = type.SubstituteTypeParameters(targetType);
						return TypeReference.To(type);
					}
				}
			}
		}
		else if (node.RuleName == "expression" && (node.parent.RuleName == "localVariableInitializer" || node.parent.RuleName == "variableInitializer"))
		{
			node = node.parent.parent;
			if (node.RuleName == "variableInitializerList")
			{
				node = node.parent.parent.parent.NodeAt(1);
				if (node == null || node.RuleName != "nonArrayType")
					return null;
			}
			else if (node.RuleName != "localVariableDeclarator" && node.RuleName != "variableDeclarator")
			{
				return null;
			}

			var targetSymbol = node.ChildAt(0).resolvedSymbol ?? ResolveNode(node.ChildAt(0));
			if (targetSymbol != null && targetSymbol.kind != SymbolKind.Error)
			{
				var targetType = targetSymbol;
				if (targetType != null && targetType.GetGenericSymbol() == builtInTypes_Expression_1)
				{
					targetType = targetType.GetTypeArgument(0);
				}
				
				targetType = targetType != null && targetType.kind == SymbolKind.Delegate ? targetType : targetSymbol.TypeOf();
				if (targetType != null && targetType.kind == SymbolKind.Delegate)
				{
					var delegateParameters = targetType.GetParameters();
					if (delegateParameters != null && index < delegateParameters.Count)
					{
						var type = delegateParameters[index].TypeOf();
						type = type.SubstituteTypeParameters(targetType);
						return TypeReference.To(type);
					}
				}
			}
		}
		else if (node.RuleName == "expression" && node.parent.RuleName == "argumentValue")
		{
			CharSpan paramName = new CharSpan();
			int argumentIndex = 0;

			node = node.parent; // argumentValue
			if (node.childIndex == 0)
			{
				node = node.parent; // argument
				argumentIndex = node.childIndex / 2;
			}
			else
			{
				node = node.parent; // argument
				paramName = node.NodeAt(0).firstChild.Print();
			}

			ParseTree.Leaf methodId = null;

			node = node.parent; // argumentList
			node = node.parent; // arguments
			node = node.parent; // constructorInitializer or attribute or primaryExpressionPart or objectCreationExpression or targetTypedNewExpression
			if (node.RuleName == "primaryExpressionPart")
			{
				node = node.parent.NodeAt(node.childIndex - 1); // primaryExpressionStart or primaryExpressionPart
				if (node.RuleName == "primaryExpressionStart")
				{
					methodId = node.LeafAt(0);
				}
				else // node.RuleName == "primaryExpressionPart"
				{
					node = node.NodeAt(-1);
					if (node.RuleName == "accessIdentifier")
					{
						methodId = node.LeafAt(1);
					}
				}
			}
			else if (node.RuleName == "objectCreationExpression")
			{
				ParseTree.Node nonArrayTypeNode = node.parent.NodeAt(1);
				var typeNameNode = nonArrayTypeNode.NodeAt(0);
				if (typeNameNode != null && typeNameNode.RuleName == "typeName")
				{
					var lastTypeOrGenericNode = typeNameNode.NodeAt(0).NodeAt(-1);
					if (lastTypeOrGenericNode != null && lastTypeOrGenericNode.RuleName == "typeOrGeneric")
						methodId = lastTypeOrGenericNode.LeafAt(0);
				}
			}
			else if (node.RuleName == "targetTypedNewExpression")
			{
				methodId = node.parent.LeafAt(0);
			}
			else if (node.RuleName == "constructorInitializer")
			{
				methodId = node.LeafAt(1);
			}

			if (methodId != null && (methodId.token.tokenKind == SyntaxToken.Kind.Identifier || node.RuleName == "constructorInitializer"))
			{
				//Debug.Log("Resolving implicit types in " + methodId.token.text + " - " + methodId.resolvedSymbol);

				if (methodId.resolvedSymbol == null ||
					methodId.resolvedSymbol.kind == SymbolKind.MethodGroup ||
					methodId.resolvedSymbol.kind == SymbolKind.Error)
				{
					FGResolver.GetResolvedSymbol(methodId);
				}

				TypeDefinitionBase extendedType = null;

				var method = methodId.resolvedSymbol as MethodDefinition;
				var constructedSymbol = methodId.resolvedSymbol as ConstructedSymbolReference;
						
				if (constructedSymbol != null && (constructedSymbol.kind == SymbolKind.Method || constructedSymbol.kind == SymbolKind.Constructor))
					method = constructedSymbol.referencedSymbol as MethodDefinition;

				if (method == null)
					return null;

				var parameters = method.GetParameters();
				if (parameters == null)
					return null;

				if (!paramName.IsEmpty)
				{
					for (argumentIndex = parameters.Count; argumentIndex --> 0; )
						if (parameters[argumentIndex].name == paramName)
							break;

					if (argumentIndex < 0)
						return null;
				}

				if (method.IsExtensionMethod)
				{
					var nodeLeft = methodId.parent;
					if (nodeLeft != null && nodeLeft.RuleName == "accessIdentifier")
					{
						var baseNodeLeft = nodeLeft.FindPreviousNode();
						if (baseNodeLeft is ParseTree.Leaf)
							nodeLeft = baseNodeLeft.FindPreviousNode() as ParseTree.Node;
						else
							nodeLeft = baseNodeLeft as ParseTree.Node;
						if (nodeLeft != null)
						{
							if (nodeLeft.RuleName == "primaryExpressionPart" || nodeLeft.RuleName == "primaryExpressionStart")
							{
								var symbolLeft = FGResolver.GetResolvedSymbol(nodeLeft);
								if (symbolLeft != null && symbolLeft.kind != SymbolKind.Error && !(symbolLeft is TypeDefinitionBase))
								{
									if (paramName.IsEmpty)
										++argumentIndex;
									extendedType = symbolLeft.TypeOf() as TypeDefinitionBase;
									if (extendedType != null && extendedType.kind == SymbolKind.Error)
										extendedType = null;
								}
							}
							else
							{
								if (paramName.IsEmpty)
									++argumentIndex;
							}
						}
					}
				}

				if (argumentIndex < parameters.Count)
				{
					var parameter = parameters[argumentIndex];
					var parameterType = parameter.TypeOf();
					if (constructedSymbol != null)
						parameterType = parameterType.SubstituteTypeParameters(constructedSymbol);
					else
						parameterType = parameterType.SubstituteTypeParameters(method);
					
					if (parameterType.GetGenericSymbol() == builtInTypes_Expression_1)
					{
						parameterType = parameterType.GetTypeArgument(0) ?? parameterType;
					}

					if (parameterType.kind == SymbolKind.Delegate)
					{
						var delegateParameters = parameterType.GetParameters();
						if (delegateParameters != null && index < delegateParameters.Count)
						{
							var type = delegateParameters[index].TypeOf();
							type = type.SubstituteTypeParameters(parameterType);
							if (constructedSymbol != null)
								type = type.SubstituteTypeParameters(constructedSymbol);
							else
								type = type.SubstituteTypeParameters(method);
							if (extendedType != null)
								type = type.SubstituteTypeParameters(extendedType);
							return TypeReference.To(type);
						}
					}
				}
			}
		}
		return null;
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		TypeOf();

		if (type == null)
		{
			leaf.resolvedSymbol = null;
			return;
		}
		var typeDef = type.definition as TypeDefinitionBase;
		if (typeDef == null || typeDef.kind == SymbolKind.Error)
		{
			leaf.resolvedSymbol = null;
			return;
		}
		typeDef.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);

		var resolved = leaf.resolvedSymbol;
		var isType = resolved is TypeDefinitionBase;
		if (resolved != null && (isType ? true : resolved.IsStatic))
		{
			if (IsVariableReference && name == typeDef.name)
				return;

			leaf.semanticError = "Member '" + resolved.name + "' cannot be accessed with an instance reference; qualify it with a type name instead";
		}
	}

	public bool IsVariableReference
	{
		get
		{
			switch (kind)
			{
				case SymbolKind.Parameter:
				case SymbolKind.CaseVariable:
				case SymbolKind.ForEachVariable:
				case SymbolKind.FromClauseVariable:
				case SymbolKind.Variable:
				case SymbolKind.TupleDeconstructVariable:
				case SymbolKind.OutVariable:
				case SymbolKind.IsVariable:
				case SymbolKind.Field:
				case SymbolKind.ConstantField:
				case SymbolKind.LocalConstant:
				case SymbolKind.Property:
				//case SymbolKind.Event:
				case SymbolKind.CatchParameter:
				case SymbolKind.EnumMember:
				//case SymbolKind.Indexer:
					return true;

				default:
					return false;
			}
		}
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		var instanceType = TypeOf();
		if (instanceType != null)
			instanceType.GetMembersCompletionData(data, BindingFlags.Instance, mask, context);
	}

	//public override bool IsGeneric
	//{
	//	get
	//	{
	//		return TypeOf().IsGeneric;
	//	}
	//}
}

public class IndexerDefinition : InstanceDefinition
{
	public List<ParameterDefinition> parameters;

	public SymbolDefinition AddParameter(SymbolDeclaration symbol)
	{
		var symbolName = symbol.Name;
		var parameter = (ParameterDefinition) Create(symbol);
		parameter.type = TypeReference.To(symbol.parseTreeNode.FindChildByName("type"));
		parameter.parentSymbol = this;
		if (!string.IsNullOrEmpty(symbolName))
		{
			if (parameters == null)
				parameters = new List<ParameterDefinition>();
			parameters.Add(parameter);
		}
		return parameter;
	}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter)
		{
			SymbolDefinition definition = AddParameter(symbol);
			symbol.definition = definition;
			return definition;
		}

		return base.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter && parameters != null)
			parameters.Remove((ParameterDefinition) symbol.definition);
		else
			base.RemoveDeclaration(symbol);
	}

	public override List<ParameterDefinition> GetParameters()
	{
		return parameters ?? _emptyParameterList;
	}
	
	public override SymbolDefinition FindName(CharSpan memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName.DecodeId();
		
		if (!asTypeOnly && parameters != null)
		{
			var definition = parameters.LastByName(memberName);
			if (definition != null)
				return definition;
		}
		return base.FindName(memberName, numTypeParameters, asTypeOnly);
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		if (!asTypeOnly && parameters != null)
		{
			var leafText = DecodeId(leaf.token.text);
			var definition = parameters.LastByName(leafText);
			if (definition != null)
			{
				leaf.resolvedSymbol = definition;
				return;
			}
		}
		base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
	}
	
	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		if (parameters != null)
		{
			for (var i = parameters.Count; i --> 0; )
			{
				var p = parameters[i];
				if (!data.ContainsKey(p.name))
					data.Add(p.name, p);
			}
		}
	}
}

public class ThisReference : InstanceDefinition
{
	private DiscardVariable discardVariable;
	
	public ThisReference(TypeDefinitionBase type)
	{
		this.type = TypeReference.To(type);//.SubstituteTypeParameters(type ?? unknownType));
		kind = SymbolKind.Instance;
	}

	public override string GetTooltipText(bool fullText = true)
	{
		return type.definition.GetTooltipText(fullText);
	}

	public string GetDiscardTooltipText(bool fullText = true)
	{
		return base.GetTooltipText(fullText);
	}

	new public bool IsValid()
	{
		return type != null && type.definition != null && type.definition.IsValid();
	}
	
	public DiscardVariable GetDiscardVariable()
	{
		if (discardVariable == null)
			discardVariable = new DiscardVariable(this);
		return discardVariable;
	}
}

public class DiscardVariable : ThisReference
{
	public DiscardVariable(ThisReference thisReference)
	: base(thisReference.type.definition as TypeDefinitionBase)
	{
		name = "_";
		kind = SymbolKind.Instance;
	}

	public override string GetTooltipText(bool fullText = true)
	{
		return GetDiscardTooltipText();
	}
}

public class ValueParameter : ParameterDefinition {}

public class NullLiteral : InstanceDefinition
{
	public static readonly NullTypeDefinition nullTypeDefinition = new NullTypeDefinition() { name = "<null>" };

	public override SymbolDefinition TypeOf()
	{
		return nullTypeDefinition;
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
	}
}

public class DefaultValue : InstanceDefinition
{
	public static readonly DefaultTypeDefinition defaultTypeDefinition = new DefaultTypeDefinition() { name = "<default>" };

	public override SymbolDefinition TypeOf()
	{
		return defaultTypeDefinition;
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
	}
}

public class DefaultTypeDefinition : TypeDefinitionBase
{
	public override bool CanConvertTo(TypeDefinitionBase otherType)
	{
		return true;
	}

	public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
	{
		return otherType;
	}
}

public class DynamicTypeDefinition : TypeDefinition
{
	public static readonly DynamicTypeDefinition dynamicTypeDefinition = new DynamicTypeDefinition()
	{
		name = "dynamic",
		kind = SymbolKind.Class,
		modifiers = Modifiers.Public | Modifiers.Sealed,
	};
	
	public override SymbolDefinition FindName(CharSpan memberName, int numTypeParameters, bool asTypeOnly)
	{
		if (asTypeOnly || memberName.IsEmpty || memberName[0] == '.')
			return null;
		return dynamicTypeDefinition.GetThisInstance();
	}
	
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return true;
	}
	
	public override bool CanConvertTo(TypeDefinitionBase otherType)
	{
		return true;
	}
	
	public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
	{
		return otherType;
	}
	
	public override SymbolDefinition TypeOf()
	{
		return dynamicTypeDefinition;
	}
	
	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		if (asTypeOnly)
			leaf.resolvedSymbol = null;
		else
			leaf.resolvedSymbol = dynamicTypeDefinition.GetThisInstance();
	}
	
	public override SymbolDefinition ResolveMethodOverloads(ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		var thisInstance = dynamicTypeDefinition.GetThisInstance();
		if (invokedLeaf != null)
			invokedLeaf.resolvedSymbol = thisInstance;
		return thisInstance;
	}
	
	public override SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
	{
		return dynamicTypeDefinition.GetThisInstance();
	}
	
	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
	}
}

public class NullTypeDefinition : TypeDefinitionBase
{
	public override bool CanConvertTo(TypeDefinitionBase otherType)
	{
		if (this == otherType)
			return true;
		if (otherType.IsReferenceType)
			return true;
		if (otherType is PointerTypeDefinition)
			return true;
		return otherType.GetGenericSymbol() == builtInTypes_Nullable;
	}

	public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
	{
		if (otherType == null)
			return null;

		if (this == otherType || otherType is TypeParameterDefinition)
			return this;

		if (otherType.kind == SymbolKind.Class || otherType.kind == SymbolKind.Interface || otherType.kind == SymbolKind.Delegate)
			return otherType;

		if (otherType is PointerTypeDefinition)
			return otherType;

		return null;
	}
}

public class ParameterDefinition : InstanceDefinition
{
	public bool IsThisParameter { get { return (modifiers & Modifiers.This) == Modifiers.This; } }

	public bool IsRef { get { return (modifiers & Modifiers.Ref) == Modifiers.Ref; } }
	public bool IsOut { get { return modifiers == Modifiers.Out; } }
	public bool IsIn { get { return (modifiers & Modifiers.In) == Modifiers.In; } }
	public bool IsParametersArray { get { return modifiers == Modifiers.Params; } }

	public bool IsOptional { get { return defaultValue != null || IsParametersArray; } }
	public string defaultValue;
}

public abstract class TypeDefinitionBase : SymbolDefinition
{
	private SymbolDefinition thisReferenceCache;
	
	public int numExtensionMethods;

	protected bool convertingToBase; // Prevents infinite recursion
	
	public bool IsReferenceType
	{
		get
		{
			var asTypeParam = this as TypeParameterDefinition;
			if (asTypeParam != null)
			{
				// Must call BaseType() to initialize constraints.
				var baseType = asTypeParam.BaseType();

				if (asTypeParam.structConstraint)
					return false;
				if (baseType != null)
					return baseType.IsReferenceType;

				return asTypeParam.IsReferenceType;
			}

			return kind == SymbolKind.Class || kind == SymbolKind.Interface || kind == SymbolKind.Delegate
				|| this is NullTypeDefinition || this.GetGenericSymbol() == builtInTypes_Nullable || this is DynamicTypeDefinition;
		}
	}
	
	public bool IsNullable
	{
		get
		{
			return this == builtInTypes_Nullable || GetGenericSymbol() == builtInTypes_Nullable;
		}
	}
	
	public bool IsManagedType
	{
		get
		{
			if (kind == SymbolKind.Enum)
				return false;
			if (kind != SymbolKind.Struct)
				return true;
			// sbyte, byte, short, ushort, int, uint, long, ulong, nint, nuint, char, float, double, decimal, or bool:
			if (this == builtInTypes_sbyte || this == builtInTypes_byte || this == builtInTypes_short ||
				this == builtInTypes_int || this == builtInTypes_uint || this == builtInTypes_long ||
				this == builtInTypes_ulong || this == builtInTypes_char || this == builtInTypes_float ||
				this == builtInTypes_double || this == builtInTypes_decimal || this == builtInTypes_bool)
			{
				return false;
			}
			for (int i = members.Count; i --> 0; )
			{
				var m = members[i];
				if (m == null || m.kind != SymbolKind.Field || m.IsStatic)
					continue;
				var fieldType = m.TypeOf() as TypeDefinitionBase;
				if (fieldType == null || fieldType.IsManagedType)
					return true;
			}
			return false;
		}
	}

	protected static bool InvalidSymbolReference(TypeReference x)
	{
		return x.IsError() || !x.IsValid();
	}
	protected static Predicate<TypeReference> delegateToInvalidSymbolReference = InvalidSymbolReference;
	
	public override Type GetRuntimeType()
	{
		if (Assembly == null || Assembly.assembly == null)
			return null;
		
		if (parentSymbol is TypeDefinitionBase)
		{
			Type parentType = parentSymbol.GetRuntimeType();
			if (parentType == null)
				return null;
			
			var result = parentType.GetNestedType(ReflectionName, BindingFlags.NonPublic | BindingFlags.Public);
			return result;
		}
		
		return Assembly.assembly.GetType(FullReflectionName);
	}
	
	public override SymbolDefinition TypeOf()
	{
		return this;
	}
	
	public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
	{
		return this;
	}
	
	public virtual void InvalidateBaseType() {}
	
	public virtual List<TypeReference> Interfaces()
	{
		return _emptyInterfaceList;
	}

	public virtual TypeDefinitionBase BaseType()
	{
		return this == builtInTypes_object || kind == SymbolKind.Error ? null : builtInTypes_object;
	}

	public virtual string RankString()
	{
		return string.Empty;
	}
	
	public static readonly MethodDefinition cannotCreateInstanceOfInterface = new MethodDefinition { name = "Cannot create an instance of the abstract class or interface", kind = SymbolKind.Error };
	public static readonly MethodDefinition methodNameExpected = new MethodDefinition { name = "Method name expected", kind = SymbolKind.Error };

	protected MethodDefinition defaultConstructor;
	public virtual MethodDefinition GetDefaultConstructor()
	{
		if (kind == SymbolKind.Interface || kind == SymbolKind.Class && IsAbstract)
			return cannotCreateInstanceOfInterface;
		
		if (kind == SymbolKind.Delegate)
			return methodNameExpected;
		
		var constructorGroup = FindName(".ctor", 0, false) as MethodGroupDefinition;
		if (defaultConstructor == null && (kind != SymbolKind.Class || constructorGroup == null))
		{
			if (constructorGroup == null)
			{
				constructorGroup = Create(SymbolKind.MethodGroup, ".ctor") as MethodGroupDefinition;
				constructorGroup.parentSymbol = this;
				members[".ctor", 0] = constructorGroup;
			}
			defaultConstructor = new MethodDefinition
			{
				kind = SymbolKind.Constructor,
				parentSymbol = constructorGroup,
				name = ".ctor",
				accessLevel = AccessLevel.Public,
				modifiers = Modifiers.Public,
			};
			constructorGroup.AddMethod(defaultConstructor);
		}
		else if (defaultConstructor == null && constructorGroup != null)
		{
			defaultConstructor = constructorGroup.methods.Find(method => !method.IsStatic && method.NumParameters == 0);
		}
		return defaultConstructor;
	}

	private Dictionary<int, ArrayTypeDefinition> createdArrayTypes;
	public ArrayTypeDefinition MakeArrayType(int arrayRank)
	{
		ArrayTypeDefinition arrayType;
		if (createdArrayTypes == null)
			createdArrayTypes = new Dictionary<int, ArrayTypeDefinition>();
		if (!createdArrayTypes.TryGetValue(arrayRank, out arrayType))
			createdArrayTypes[arrayRank] = arrayType = new ArrayTypeDefinition(this, arrayRank);
		return arrayType;
	}

	private PointerTypeDefinition createdPointerType;
	public PointerTypeDefinition MakePointerType()
	{
		if (createdPointerType == null)
			createdPointerType = new PointerTypeDefinition(this);
		return createdPointerType;
	}

	private RefTypeDefinition createdRefType;
	public RefTypeDefinition MakeRefType()
	{
		if (createdRefType == null)
			createdRefType = new RefTypeDefinition(this);
		return createdRefType;
	}

	private TypeDefinitionBase createdNullableType;
	public TypeDefinitionBase MakeNullableType()
	{
		if (createdNullableType == null)
		{
			if (IsReferenceType)
				createdNullableType = this;
			else
				createdNullableType = builtInTypes_Nullable.ConstructType(new []{ TypeReference.To(this) });
		}
		return createdNullableType;
	}

	static public ConstructedTypeDefinition MakeValueTupleType(List<TypeReference> types)
	{
		TypeReference[] typesArray = null;
		if (types.Count <= 7)
		{
			typesArray = types.ToArray();
		}
		else
		{
			typesArray = new [] { types[0], types[1], types[2], types[3], types[4], types[5], types[6], null };
			var rest = types.GetRange(7, types.Count - 7);
			var restType = MakeValueTupleType(rest);
			typesArray[7] = TypeReference.To(restType);
		}
		
		var valueTupleType =
			typesArray.Length == 1 ? builtInTypes_ValueTuple_1.ConstructType(typesArray) :
			typesArray.Length == 2 ? builtInTypes_ValueTuple_2.ConstructType(typesArray) :
			typesArray.Length == 3 ? builtInTypes_ValueTuple_3.ConstructType(typesArray) :
			typesArray.Length == 4 ? builtInTypes_ValueTuple_4.ConstructType(typesArray) :
			typesArray.Length == 5 ? builtInTypes_ValueTuple_5.ConstructType(typesArray) :
			typesArray.Length == 6 ? builtInTypes_ValueTuple_6.ConstructType(typesArray) :
			typesArray.Length == 7 ? builtInTypes_ValueTuple_7.ConstructType(typesArray) :
			typesArray.Length == 8 ? builtInTypes_ValueTuple_8.ConstructType(typesArray) :
			null;

		return valueTupleType;
	}

	static public TupleTypeDefinition MakeTupleType(List<TypeReference> fieldTypes)
	{
		var valueTupleType = MakeValueTupleType(fieldTypes);
		var tupleType = new TupleTypeDefinition(valueTupleType, fieldTypes);
		return tupleType;
	}

	public SymbolDefinition GetThisInstance()
	{
		var asThisReference = thisReferenceCache as ThisReference;
		if (asThisReference == null || !asThisReference.IsValid())
		{
			if (IsStatic)
				return thisReferenceCache = unknownSymbol;
			thisReferenceCache = new ThisReference(this);
		}
		return thisReferenceCache;
	}
	
	public DiscardVariable GetDiscardVariable()
	{
		var thisReference = GetThisInstance() as ThisReference;
		if (thisReference == null)
			return null;
		return thisReference.GetDiscardVariable();
	}
	
	private bool resolvingInBase = false;
	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		if (kind == SymbolKind.Error)
			return;

		base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);

		if (!resolvingInBase && leaf.resolvedSymbol == null)
		{
			resolvingInBase = true;
			
			var baseType = BaseType();
			var interfaces = Interfaces();
			
			if (!asTypeOnly && interfaces != null && (kind == SymbolKind.Interface || kind == SymbolKind.TypeParameter))
			{
				for (var i = interfaces.Count; i --> 0; )
				{
					var element = interfaces[i];
					element.definition.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
					if (leaf.resolvedSymbol != null)
					{
						resolvingInBase = false;
						return;
					}
				}
			}

			SymbolDefinition resolved = null;
			if (baseType != null && baseType != this)
			{
				baseType.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
				resolved = leaf.resolvedSymbol;
			}
			
			resolvingInBase = false;

			if (resolved != null)
			{
				if (resolved.IsPrivate && resolved.kind != SymbolKind.MethodGroup)
					leaf.resolvedSymbol = null;
			}
		}
	}
	
	public virtual bool DerivesFrom(TypeDefinitionBase otherType)
	{
		if (otherType == null)
			return false;
		
		return DerivesFromRef(ref otherType);
	}

	public virtual bool DerivesFromRef(ref TypeDefinitionBase otherType)
	{
		if (otherType == null)
			return false;
		
		var otherTypeAsConstructed = otherType as ConstructedTypeDefinition;
		if (otherTypeAsConstructed != null)
			otherType = otherTypeAsConstructed.genericTypeDefinition;

		if (this == otherType)
			return true;

		var baseType = BaseType();
		if (baseType != null)
			return baseType.DerivesFromRef(ref otherType);

		return false;
	}
	
	public override SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
	{
		var indexers = GetAllIndexers();
		
		// TODO: Resolve overloads
		
		return indexers != null ? indexers[0] : null;
	}

	public virtual List<SymbolDefinition> GetAllIndexers()
	{
		List<SymbolDefinition> indexers = null;
		for (var i = 0; i < members.Count; ++i)
		{
			var m = members[i];
			if (m.kind == SymbolKind.Indexer)
			{
				if (indexers == null)
					indexers = new List<SymbolDefinition>();
				indexers.Add(m);
			}
		}
		if (indexers == null)
		{
			var baseType = BaseType();
			if (baseType != null && baseType != builtInTypes_object)
				return baseType.GetAllIndexers();
		}
		return indexers;
	}
	
	public void ListOverrideCandidates(List<MethodDefinition> methods, AssemblyDefinition context)
	{
		var accessLevelMask = AccessLevelMask.Public | AccessLevelMask.Protected;
		if (Assembly.InternalsVisibleTo(context))
			accessLevelMask |= AccessLevelMask.Internal;
		
		for (var i = members.Count; i --> 0; )
		{
			var member = members[i];
			if (member.kind == SymbolKind.MethodGroup)
			{
				var asMethodGroup = member as MethodGroupDefinition;
				if (asMethodGroup != null)
				{
					var mgMethods = asMethodGroup.methods;
					for (var j = mgMethods.Count; j --> 0; )
					{
						var method = mgMethods[j];
						if ((method.IsOverride || method.IsVirtual || method.IsAbstract) && method.IsAccessible(accessLevelMask))
						{
							methods.Add(method);
						}
					}
				}
			}
		}

		if (completionsFromBase)
			return;
		completionsFromBase = true;
		
		var baseType = BaseType();
		if (baseType != null && (baseType.kind == SymbolKind.Class || baseType.kind == SymbolKind.Struct))
			baseType.ListOverrideCandidates(methods, context);
		
		completionsFromBase = false;
	}
	
	public void ListOverrideCandidates(Dictionary<string, InstanceDefinition> properties, AssemblyDefinition context)
	{
		var accessLevelMask = AccessLevelMask.Public | AccessLevelMask.Protected;
		if (Assembly.InternalsVisibleTo(context))
			accessLevelMask |= AccessLevelMask.Internal;
		
		for (var i = members.Count; i --> 0; )
		{
			var member = members[i] as InstanceDefinition;
			if (member == null)
				continue;
			
			if (member.kind == SymbolKind.Property || member.kind == SymbolKind.Event)
			{
				if ((member.IsOverride || member.IsVirtual || member.IsAbstract) && member.IsAccessible(accessLevelMask))
				{
					if (!properties.ContainsKey(member.name))
					{
						properties.Add(member.name.GetString(), member);
					}
				}
			}
		}

		if (completionsFromBase)
			return;
		completionsFromBase = true;
		
		var baseType = BaseType();
		if (baseType != null && (baseType.kind == SymbolKind.Class || baseType.kind == SymbolKind.Struct))
			baseType.ListOverrideCandidates(properties, context);
		
		completionsFromBase = false;
	}
	
	public void GetCompletionDataFromImportedType(Dictionary<string, SymbolDefinition> data, AccessLevelMask mask, ResolveContext context)
	{
		completionsFromBase = true;
		
		GetCompletionData(data, context);
		
		completionsFromBase = false;
	}
	
	private bool completionsFromBase = false;
	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		if (kind == SymbolKind.Error)
			return;

		base.GetMembersCompletionData(data, flags, mask, context);
		
		if (completionsFromBase)
			return;
		completionsFromBase = true;
		
		var baseType = BaseType();
		var interfaces = Interfaces();
		if (flags != BindingFlags.Static && (kind == SymbolKind.Interface || kind == SymbolKind.TypeParameter))
			foreach (var i in interfaces)
				i.definition.GetMembersCompletionData(data, flags, mask & ~AccessLevelMask.Private, context);
		if (baseType != null && (kind != SymbolKind.Enum || flags != BindingFlags.Static) &&
			(baseType.kind != SymbolKind.Interface || kind == SymbolKind.Interface || kind == SymbolKind.TypeParameter))
		{
			baseType.GetMembersCompletionData(data, flags, mask & ~AccessLevelMask.Private, context);
		}
		
		completionsFromBase = false;
	}
	
	internal virtual TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
	{
		return null;
	}

	public virtual bool CanConvertTo(TypeDefinitionBase otherType)
	{
		if (this == otherType)
			return true;

		if (DefaultValue.defaultTypeDefinition == otherType)
			return true;

		if (ConvertTo(otherType) != null)
			return true;

		var hasImplicitConversion = CollectImplicitConversionOperators(this, otherType);
		
		return hasImplicitConversion;
		
		//if (HasImplicitConversionOperatorTo(otherType))
		//	return true;
		
		//if (otherType.HasImplicitConversionOperatorFrom(this))
		//	return true;
		
		//return false;
	}

	public virtual TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
	{
		if (otherType == null)
			return null;

		if (this == otherType || otherType == this)
			return this;

		if (this == DefaultValue.defaultTypeDefinition)
			return this;

		if (otherType is TypeParameterDefinition)
			return this;

		if (otherType == builtInTypes_object)
			return otherType;

		if (otherType.GetGenericSymbol() == builtInTypes_Nullable)
			return CanConvertTo(otherType.GetTypeArgument(0)) ? otherType : null;

		if (this == builtInTypes_int && (
			otherType == builtInTypes_long ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_uint && (
			otherType == builtInTypes_long ||
			otherType == builtInTypes_ulong ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_byte && (
			otherType == builtInTypes_short ||
			otherType == builtInTypes_ushort ||
			otherType == builtInTypes_int ||
			otherType == builtInTypes_uint ||
			otherType == builtInTypes_long ||
			otherType == builtInTypes_ulong ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_sbyte && (
			otherType == builtInTypes_short ||
			otherType == builtInTypes_int ||
			otherType == builtInTypes_long ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_short && (
			otherType == builtInTypes_int ||
			otherType == builtInTypes_long ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_ushort && (
			otherType == builtInTypes_int ||
			otherType == builtInTypes_uint ||
			otherType == builtInTypes_long ||
			otherType == builtInTypes_ulong ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if ((this == builtInTypes_long || this == builtInTypes_ulong) &&
			(otherType == builtInTypes_float || otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_float &&
			otherType == builtInTypes_double)
			return otherType;
		if (this == builtInTypes_char && (
			otherType == builtInTypes_ushort ||
			otherType == builtInTypes_int ||
			otherType == builtInTypes_uint ||
			otherType == builtInTypes_long ||
			otherType == builtInTypes_ulong ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;

		if (DerivesFromRef(ref otherType))
			return otherType;

		return null;
	}
	
	protected static List<MethodDefinition> conversionOperators = new List<MethodDefinition>(16);
	protected static List<TypeDefinitionBase> conversionFromTypes = new List<TypeDefinitionBase>(16);
	protected static List<TypeDefinitionBase> conversionToTypes = new List<TypeDefinitionBase>(16);
	
	private static bool collectingImplicitConversionOperators = false;
	public static bool CollectImplicitConversionOperators(SymbolDefinition fromSymbol, TypeDefinitionBase toType)
	{
		if (collectingImplicitConversionOperators)
			return false;
		collectingImplicitConversionOperators = true;
		var result = _CollectImplicitConversionOperators(fromSymbol, toType);
		collectingImplicitConversionOperators = false;
		return result;
	}

	public static bool _CollectImplicitConversionOperators(SymbolDefinition fromSymbol, TypeDefinitionBase toType)
	{
		HashSet<TypeDefinitionBase> conversionOperatorTypes = new HashSet<TypeDefinitionBase>(/*64*/);
		
		conversionOperators.Clear();
		conversionFromTypes.Clear();
		conversionToTypes.Clear();
		
		var sourceType = fromSymbol.TypeOf() as TypeDefinitionBase;
		if (sourceType == null || sourceType.kind == SymbolKind.Interface || sourceType == builtInTypes_object)
			return false;
		
		var targetType = toType;
		if (targetType == null || targetType.kind == SymbolKind.Interface || targetType == builtInTypes_object)
			return false;
		
		if (sourceType.kind == SymbolKind.Error || targetType.kind == SymbolKind.Error)
			return false;

		var currentType = sourceType;
		{
			if (sourceType.GetGenericSymbol() == builtInTypes_Nullable)
				currentType = sourceType.GetTypeArgument(0);
			if (currentType == null || currentType.kind == SymbolKind.Error)
				return false;

			if (currentType.kind == SymbolKind.TypeParameter)
			{
				currentType = currentType.BaseType();
			}
		
			if (currentType.kind == SymbolKind.Struct)
			{
				conversionOperatorTypes.Add(currentType);
			}
			else
			{
				while (currentType != null && currentType.kind == SymbolKind.Class)
				{
					conversionOperatorTypes.Add(currentType);
					currentType = currentType.BaseType();
				}
			}
		}
		
		currentType = targetType;
		{
			if (targetType.GetGenericSymbol() == builtInTypes_Nullable)
				currentType = toType.GetTypeArgument(0);
			if (currentType == null || currentType.kind == SymbolKind.Error)
				return false;

			if (currentType.kind == SymbolKind.TypeParameter)
			{
				currentType = currentType.BaseType();
			}
			
			if (currentType.kind == SymbolKind.Struct || currentType.kind == SymbolKind.Class)
			{
				conversionOperatorTypes.Add(currentType);
			}
		}
		
		TypeDefinitionBase sx = null;
		TypeDefinitionBase tx = null;

		foreach (var type in conversionOperatorTypes)
		{
			var mg = type.FindName("op_Implicit", 0, false) as MethodGroupDefinition;
			if (mg == null)
				continue;
		
			var methods = mg.methods;
			var numMethods = methods.Count;
			for (var i = numMethods; i --> 0; )
			{
				var method = methods[i];
				
				var convertsToType = method.ReturnType();
				if (convertsToType == null || convertsToType.kind == SymbolKind.Error || convertsToType.kind == SymbolKind.Interface)
					continue;
				convertsToType = convertsToType.SubstituteTypeParameters(type);
				if (convertsToType.ConvertTo(targetType) == null)
					continue;
				
				var parameters = method.GetParameters();
				if (parameters == null || parameters.Count == 0)
					continue;
				
				var convertsFromType = parameters[0].TypeOf() as TypeDefinitionBase;
				if (convertsFromType == null || convertsFromType.kind == SymbolKind.Error || convertsFromType.kind == SymbolKind.Interface)
					continue;
				convertsFromType = convertsFromType.SubstituteTypeParameters(type);
				if (sourceType.ConvertTo(convertsFromType) == null)
					continue;
				
				conversionOperators.Add(method);
				conversionFromTypes.Add(convertsFromType);
				conversionToTypes.Add(convertsToType);
				
				var isSameSource = sourceType.IsSameType(convertsFromType);
				var isSameTarget = targetType.IsSameType(convertsToType);
				
				if (isSameSource)
					sx = sourceType;
				if (isSameTarget)
					tx = targetType;
				
				if (isSameSource && isSameTarget)
				{
					if (conversionOperators.Count > 1)
					{
						conversionOperators.Clear();
						conversionOperators.Add(method);
					}
					return true;
				}
			}
		}
		
		var count = conversionOperators.Count;
		
		if (count <= 1)
			return count == 1;
		
		if (sx == null)
		{
			for (var i = count; i --> 0; )
			{
				currentType = conversionFromTypes[i];

				var convertsToAllOtherTypes = true;
				for (var j = count; j --> 0; )
				{
					if (i == j)
						continue;
					
					var otherType = conversionFromTypes[j];
					
					if (currentType.ConvertTo(otherType) == null)
					{
						convertsToAllOtherTypes = false;
						break;
					}
				}
				if (!convertsToAllOtherTypes)
					continue;
				
				if (sx == null)
				{
					sx = currentType;
				}
				else
				{
					conversionOperators.Clear();
					return false;
				}
			}
		}
		if (sx == null)
		{
			conversionOperators.Clear();
			return false;
		}
		
		if (tx == null)
		{
			for (var i = count; i --> 0; )
			{
				currentType = conversionToTypes[i];

				var convertsFromAllOtherTypes = true;
				for (var j = count; j --> 0; )
				{
					if (i == j)
						continue;
					
					var otherType = conversionFromTypes[j];
					
					if (otherType.ConvertTo(currentType) == null)
					{
						convertsFromAllOtherTypes = false;
						break;
					}
				}
				if (!convertsFromAllOtherTypes)
					continue;
				
				if (tx == null)
				{
					tx = currentType;
				}
				else
				{
					conversionOperators.Clear();
					return false;
				}
			}
		}
		if (tx == null)
		{
			conversionOperators.Clear();
			return false;
		}
		
		for (var i = count; i --> 0; )
			if (sx != conversionFromTypes[i] || tx != conversionToTypes[i])
				conversionOperators.RemoveAt(i);
		
		if (conversionOperators.Count > 1)
			conversionOperators.Clear();
		
		return conversionOperators.Count == 1;
	}
	
	public bool HasImplicitConversionOperatorTo(TypeDefinitionBase otherType)
	{
		var mg = FindName("op_Implicit", 0, false) as MethodGroupDefinition;
		if (mg == null)
			return false;
		
		var candidates = mg.methods;
		var numCandidates = candidates.Count;
		if (numCandidates > 0)
		{
			for (var i = numCandidates; i --> 0; )
			{
				var candidate = candidates[i];
				var retType = candidate.ReturnType();
				if (retType != null && retType.IsSameType(otherType))
					return true;
			}
		}
		
		return false;
	}
	
	public bool HasImplicitConversionOperatorFrom(TypeDefinitionBase otherType)
	{
		var mg = FindName("op_Implicit", 0, false) as MethodGroupDefinition;
		if (mg == null)
			return false;
		
		var candidates = mg.methods;
		var numCandidates = candidates.Count;
		if (numCandidates > 0)
		{
			for (var i = numCandidates; i --> 0; )
			{
				var candidate = candidates[i];
				var parameters = candidate.GetParameters();
				if (parameters.Count != 1)
					continue;
				
				var parameterType = parameters[0].TypeOf();
				if (parameterType != null && parameterType.IsSameType(otherType))
					return true;
			}
		}
		
		return false;
	}

	public MethodDefinition FindMethod(string name, int numTypeParams, int numParams, bool onlyNonStatic)
	{
		//TODO: Also try to find an extension method.
		
		MethodDefinition method = null;
		
		for (var typeDef = this; typeDef != null; typeDef = typeDef.BaseType())
		{
			SymbolDefinition member = typeDef.FindName(name, numTypeParams, false);
			if (member == null)
				continue;
			if (member.kind != SymbolKind.MethodGroup)
				continue;
			
			List<MethodDefinition> methods;
			var methodGroup = member as MethodGroupDefinition;
			if (methodGroup != null)
			{
				methods = methodGroup.methods;
			}
			else
			{
				var constructedMethodGroup = member as ConstructedMethodGroupDefinition;
				if (constructedMethodGroup == null)
					continue;

				methods = constructedMethodGroup.methods;
			}
			
			if (methods == null)
				continue;
			
			method = methods.Find(x => x.NumParameters == numParams);
			if (method != null)
				break;
		}
		
		return method;
	}

	public SymbolDefinition FindProperty(string name, bool onlyNonStatic)
	{
		for (var typeDef = this; typeDef != null; typeDef = typeDef.BaseType())
		{
			var property = typeDef.FindName(name, 0, false);
			if (property == null)
				continue;
			if (property.kind != SymbolKind.Property)
				continue;

			if (property.IsStatic && onlyNonStatic)
				continue;
			
			return property;
		}
		
		return null;
	}

	public MethodDefinition FindDeconstructMethod(int numElements)
	{
		//TODO: Also try to find an extension method.
		//TODO: Also try to find the method in base types.
		
		SymbolDefinition member = FindName("Deconstruct", -1, false);
		if (member == null)
			return null;
		if (member.kind != SymbolKind.MethodGroup)
			return null;
		
		List<MethodDefinition> methods;
		var methodGroup = member as MethodGroupDefinition;
		if (methodGroup != null)
		{
			if (methodGroup.IsStatic)
				return null;

			methods = methodGroup.methods;
		}
		else
		{
			var constructedMethodGroup = member as ConstructedMethodGroupDefinition;
			if (constructedMethodGroup == null)
				return null;
				
			if (constructedMethodGroup.IsStatic)
				return null;

			methods = constructedMethodGroup.methods;
		}

		for (var i = methods.Count; i --> 0; )
		{
			var method = methods[i];
			
			if (method.NumParameters != numElements)
				continue;
			
			foreach(var parameter in method.parameters)
			{
				if (parameter.modifiers != Modifiers.Out)
				{
					method = null;
					break;
				}
			}
			
			if (method != null)
				return method;
		}
		
		return null;
	}
}

//TODO: Finish this
public class LambdaExpressionDefinition : TypeDefinitionBase
{
	private List<ParameterDefinition> parameters;

	public SymbolDefinition AddParameter(SymbolDeclaration symbol)
	{
		var symbolName = symbol.Name;
		var parameter = (ParameterDefinition)Create(symbol);
		var typeNode = symbol.parseTreeNode.FindChildByName("type");
		parameter.type = typeNode != null ? TypeReference.To(typeNode) : null;
		parameter.parentSymbol = this;
		if (!string.IsNullOrEmpty(symbolName))
		{
			if (parameters == null)
				parameters = new List<ParameterDefinition>();
			parameters.Add(parameter);

			var nameNode = symbol.NameNode();
			if (nameNode != null)
				nameNode.SetDeclaredSymbol(parameter);
		}
		return parameter;
	}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter)
		{
			SymbolDefinition definition = AddParameter(symbol);
			//	if (!members.TryGetValue(symbolName, out definition) || definition is ReflectedMember || definition is ReflectedType)
			//		definition = AddMember(symbol);

			symbol.definition = definition;
			return definition;
		}

		return base.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter && parameters != null)
			parameters.Remove((ParameterDefinition)symbol.definition);
		else
			base.RemoveDeclaration(symbol);
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		if (!asTypeOnly && parameters != null && numTypeArgs <= 0)
		{
			var leafText = DecodeId(leaf.token.text);
			var definition = parameters.LastByName(leafText);
			if (definition != null)
			{
				leaf.resolvedSymbol = definition;
				return;
			}
		}
		base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
	}

	public override List<ParameterDefinition> GetParameters()
	{
		return parameters ?? _emptyParameterList;
	}

	public override bool IsSameType(TypeDefinitionBase type)
	{
		if (type == this)
			return true;

		//if (type.kind != SymbolKind.Delegate)
			return false;

		//var numParams = GetParameters().Count;
		//var numDelegateParams = type.NumParameters;
		//if (numParams != numDelegateParams)
		//	return false;

		////var returnType = TypeOf();
		////var delegateReturnType = type.TypeOf();
		////if (returnType != delegateReturnType)
		////	return false;

		//var delegateParams = type.GetParameters();
		//for (int i = numParams; i --> 0; )
		//{
		//	var paramType = parameters[i].TypeOf();
		//	var delegateParamType = delegateParams[i].TypeOf();
		//	if (paramType != delegateParamType)
		//		return false;
		//}

		//return true;
	}

	public override bool CanConvertTo(TypeDefinitionBase otherType)
	{
		if (this == otherType || ConvertTo(otherType) != null)
			return true;
		return false;
	}

	public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
	{
		if (this == otherType)
			return this;

		if (otherType == null)
			return null;

		//var expressionTree = false;
		if (otherType.GetGenericSymbol() == builtInTypes_Expression_1)
		{
			//expressionTree = true;
			otherType = otherType.GetTypeArgument(0);
		}

		if (otherType.kind != SymbolKind.Delegate)
			return null;

		var declaration = declarations;
		if (declaration == null)
			return null;

		if (declaration.parseTreeNode.numValidNodes == 0)
			return null;

		var delegateParameters = otherType.GetParameters();
		var numDelegateParameters = delegateParameters != null ? delegateParameters.Count : 0;
		
		if (declaration.parseTreeNode.RuleName == "anonymousMethodExpression")
		{
			var signatureNode = declaration.parseTreeNode.FindChildByName("explicitAnonymousFunctionSignature") as ParseTree.Node;
			if (signatureNode == null)
				return otherType;
			var parametersNode = signatureNode.FindChildByName("explicitAnonymousFunctionParameterList") as ParseTree.Node;
			var numParameters = parametersNode == null ? 0 : (parametersNode.numValidNodes + 1) / 2;
			if (numDelegateParameters == numParameters)
				return otherType;
			
			return null;
		}

		bool hasImplicitParams;

		var anonymousFunctionSignatureNode = declaration.parseTreeNode.NodeAt(0);
		if (anonymousFunctionSignatureNode.numValidNodes == 1 && anonymousFunctionSignatureNode.NodeAt(0) != null)
		{
			// there is one implicitly typed parameter
			if (numDelegateParameters != 1)
				return null;

			hasImplicitParams = true;
		}
		else
		{
			var parameterListNode =
				(anonymousFunctionSignatureNode.FindChildByName("implicitAnonymousFunctionParameterList") ??
				anonymousFunctionSignatureNode.FindChildByName("explicitAnonymousFunctionParameterList")) as ParseTree.Node;
			var numLambdaParameters = parameterListNode == null ? 0 : (parameterListNode.numValidNodes + 1) / 2;
			if (numDelegateParameters != numLambdaParameters)
				return null;

			hasImplicitParams = numLambdaParameters > 0 && parameterListNode.RuleName == "implicitAnonymousFunctionParameterList";
			if (numLambdaParameters > 0 && !hasImplicitParams)
			{
				for (int i = 0; i < numLambdaParameters; i += 2)
				{
					var lambdaParamType = parameters[i].TypeOf() as TypeDefinitionBase;
					if (lambdaParamType == null)
						continue;
					var delegateParamType = delegateParameters[i].TypeOf();
					if (delegateParamType != null)
					{
						var constructedType = delegateParamType.SubstituteTypeParameters(otherType);
						if (constructedType != null)
							delegateParamType = constructedType;
					}
					if (!delegateParamType.IsSameType(lambdaParamType) && !(lambdaParamType.kind == SymbolKind.Error || delegateParamType.kind == SymbolKind.Error))
					{
						return null;
					}
				}
			}
		}

		var returnType = ReturnType(otherType); // hasImplicitParams ? ReturnType(otherType) : TypeOf() as TypeDefinitionBase;
		bool returnsVoid = returnType == builtInTypes_void;

		var delegateReturnType = otherType.TypeOf() as TypeDefinitionBase;
		bool delegateReturnsVoid = delegateReturnType == builtInTypes_void;
		bool delegateReturnsTask = delegateReturnType == builtInTypes_Task;
		bool delegateReturnsGenericTask = delegateReturnType == builtInTypes_Task_1;

		var bodyNode = declaration.parseTreeNode.NodeAt(2);
		if (bodyNode == null)
			return otherType;
		
		bool bodyIsStatementBlock = bodyNode != null && bodyNode.numValidNodes > 1;
		var isAsync = declaration.parseTreeNode.childIndex == 1;

		if (bodyIsStatementBlock)
		{
			if (delegateReturnsVoid || delegateReturnsTask && isAsync)
			{
				if (!returnsVoid)
					return null;
			}
			else if (!isAsync && !delegateReturnsVoid)
			{
				if (returnType == null)
					return null;
				if (!returnType.CanConvertTo(delegateReturnType)) // Should check each return statement.
					return null;
			}
			else if (isAsync && delegateReturnsGenericTask)
			{
				if (returnType == null)
					return null;
				if (!returnType.CanConvertTo(delegateReturnType.GetTypeArgument(0))) // Should check each return statement.
					return null;
			}
		}
		else
		{
			// Body is expression:
			if (delegateReturnsVoid || delegateReturnsTask && isAsync)
			{
				// Only check if the expression of the body is permitted as statement-expression.
				//if (!returnsVoid)
				//	return null;
				var expressionNode = bodyNode.NodeAt(0);
				if (expressionNode == null)
					return null;
				var assignmentNode = expressionNode.NodeAt(0);
				if (assignmentNode == null)
					return null;
				if (assignmentNode.RuleName != "assignment")
				{
					// nonAssignmentExpression
					var conditionalExpressionNode = assignmentNode.NodeAt(0);
					if (conditionalExpressionNode == null || conditionalExpressionNode.RuleName != "conditionalExpression")
						return null;
					if (conditionalExpressionNode.numValidNodes != 1)
						return null;
					if (CsParser.isCSharp4)
					{
						var nullCoalescingExpressionNode = conditionalExpressionNode.NodeAt(0);
						if (nullCoalescingExpressionNode == null)
							return null;
						// etc...
					}
					else
					{
						var flatExpressionNode = conditionalExpressionNode.NodeAt(0);
						if (flatExpressionNode == null || flatExpressionNode.numValidNodes != 1)
							return null;
						var unaryExpressionNode = flatExpressionNode.NodeAt(0);
						if (unaryExpressionNode == null)
							return null;
						var childNode = unaryExpressionNode.NodeAt(0);
						if (childNode == null || childNode.RuleName == "castExpression")
							return null;
						if (childNode.RuleName == "primaryExpression")
						{
							var firstChild = childNode.firstChild;
							if (firstChild == null)
								return null;
							if (firstChild.IsLit("new"))
							{
								var nonArrayTypeNode = childNode.NodeAt(1);
								if (nonArrayTypeNode == null || nonArrayTypeNode.RuleName != "nonArrayType")
									return null;
								var objectCreationExpessionNode = nonArrayTypeNode.nextSibling as ParseTree.Node;
								if (objectCreationExpessionNode == null || objectCreationExpessionNode.RuleName != "objectCreationExpression")
									return null;
							}
							else
							{
								var primaryExpressionPart = childNode.NodeAt(-1);
								if (primaryExpressionPart == null || primaryExpressionPart.RuleName != "primaryExpressionPart")
									return null;
								var argumentsNode = primaryExpressionPart.NodeAt(0);
								if (argumentsNode == null || argumentsNode.RuleName != "arguments")
									return null;
							}
						}
					}
				}
			}
			// If the body of F is an expression, and either F is non-async and D has a non-void return type T, or F is async
			// and D has a return type Task<T>, then when each parameter of F is given the type of the corresponding parameter
			// in D, the body of F is a valid expression (wrt §7) that is implicitly convertible to T.
			else if (!isAsync && !delegateReturnsVoid)
			{
				if (returnType == null)
					return null;
				if (!returnType.CanConvertTo(delegateReturnType))
					return null;
			}
			else if (isAsync && delegateReturnsGenericTask)
			{
				if (returnType == null)
					return null;
				if (!returnType.CanConvertTo(delegateReturnType.GetTypeArgument(0)))
					return null;
			}
		}
	
		return otherType;
	}

	public TypeDefinitionBase ReturnType(TypeDefinitionBase delegateType)
	{
		var declaration = declarations;
		if (declaration == null)
			return unknownType;
		if (declaration.parseTreeNode.numValidNodes != 3)
			return unknownType;
		var lambdaExpressionBodyNode = declaration.parseTreeNode.NodeAt(2);
		if (lambdaExpressionBodyNode == null)
			return null;
		
		List<ParameterDefinition> delegateParameters = delegateType.GetParameters();
		var numParams = delegateType.NumParameters;

		var numLambdaParams = NumParameters;
		if (numLambdaParams != numParams)
			return null;
		
		if (numLambdaParams == 0)
		{
			var resolvedBody = ResolveNode(lambdaExpressionBodyNode);
			var bodyType = resolvedBody == null ? null : resolvedBody.TypeOf() as TypeDefinitionBase;
			return bodyType ?? unknownType;
		}

		//UnResolveNode(lambdaExpressionBodyNode);

		var savedParameterTypes = TypeReference.AllocArray(numParams);
		for (int i = numParams; i --> 0; )
		{
			savedParameterTypes[i] = parameters[i].type;
			var lambdaParamType = parameters[i].type == null ? null : parameters[i].type.definition;

			var delegateParameterType = delegateParameters[i].type.definition;
			if (delegateParameterType != null)
			{
				var constructedType = delegateParameterType.SubstituteTypeParameters(delegateType);
				if (lambdaParamType != null)
					constructedType = constructedType.SubstituteTypeParameters(lambdaParamType);
				if (constructedType != lambdaParamType)
					parameters[i].type = TypeReference.To(constructedType);
				//else
				//	parameters[i].type = delegateParameters[i].type;
			}
		}

		ParseTree.resolverVersion++;
		var resolvedExpression = ResolveNode(lambdaExpressionBodyNode);
		var returnType = resolvedExpression == null ? null : resolvedExpression.TypeOf() as TypeDefinitionBase;
		ParseTree.resolverVersion--;

		for (int i = numParams; i --> 0; )
		{
			parameters[i].type = savedParameterTypes[i];
		}
		TypeReference.ReleaseArray(savedParameterTypes);

		//UnResolveNode(lambdaExpressionBodyNode);

		return returnType ?? unknownType;
	}

	static void UnResolveNode(ParseTree.BaseNode rootNode)
	{
		var leaf = rootNode as ParseTree.Leaf;
		if (leaf != null)
		{
			leaf.UnResolve();
			return;
		}

		rootNode.UnResolve();

		var next = ((ParseTree.Node)rootNode).firstChild;
		while (next != null)
		{
			UnResolveNode(next);
			next = next.nextSibling;
		}
	}
	
	private new SymbolDefinition TypeOf()
	{
		var declaration = declarations;
		if (declaration == null)
			return unknownType;
		if (declaration.parseTreeNode.numValidNodes != 3)
			return unknownType;
		var lambdaExpressionBodyNode = declaration.parseTreeNode.NodeAt(2);
		if (lambdaExpressionBodyNode == null)
			return null;
		var resolvedExpression = ResolveNode(lambdaExpressionBodyNode);
		var returnType = resolvedExpression == null ? unknownType : resolvedExpression.TypeOf() as TypeDefinitionBase;
		return returnType ?? unknownType;
	}
	
	internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
	{
		var returnType = TypeOf() as TypeDefinitionBase;
		if (returnType != null && returnType.kind != SymbolKind.Error)
		{
			var boundReturnType = argumentType.BindTypeArgument(typeArgument, returnType);
			return boundReturnType;
		}
		return null;
	}

	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		foreach (var parameter in GetParameters())
		{
			var parameterName = parameter.GetName();
			if (!data.ContainsKey(parameterName))
				data.Add(parameterName, parameter);
		}

		base.GetCompletionData(data, context);
	}
}

public class EnumTypeDefinition : TypeDefinition
{
	private TypeReference underlyingType;
	
	public TypeReference UnderlyingType {
		get {
			if (underlyingType == null)
				underlyingType = TypeReference.To(builtInTypes_int);
			return underlyingType;
		}
		
		set {
			if (underlyingType == value)
				return;
			addedPredefinedOperators = false;
			underlyingType = value;
		}
	}

	public bool addedPredefinedOperators;
	
	public override SymbolDefinition FindName(CharSpan memberName, int numTypeParameters, bool asTypeOnly)
	{
		var result = base.FindName(memberName, numTypeParameters, asTypeOnly);
		if (result != null)
			return result;

		if (addedPredefinedOperators || !memberName.FastStartsWith("op_"))
			return null;
		AddPredefinedOperators(this);

		return base.FindName(memberName, numTypeParameters, asTypeOnly);
	}

	public static void AddPredefinedOperators(TypeDefinitionBase targetType)
	{
		TypeReference underlyingType = null;
		TypeReference targetTypeRef = null;
		TypeReference boolTypeRef = TypeReference.To(typeof(bool));

		var enumType = targetType as EnumTypeDefinition;
		if (enumType != null)
		{
			enumType.addedPredefinedOperators = true;
			underlyingType = enumType.underlyingType;
			targetTypeRef = TypeReference.To(enumType);
		}
		else
		{
			var reflectedEnumType = targetType as ReflectedEnumType;
			if (reflectedEnumType == null)
				return;

			underlyingType = reflectedEnumType.UnderlyingType;
			targetTypeRef = TypeReference.To(reflectedEnumType);
		}

		var methodGroup = new MethodGroupDefinition
		{
			kind = SymbolKind.MethodGroup,
			name = "op_Addition",
			modifiers = Modifiers.None,
			parentSymbol = targetType,
		};
		targetType.members[methodGroup.name, 0] = methodGroup;
		methodGroup.AddMethod(MethodDefinition.CreateOperator(methodGroup.name, targetTypeRef, targetTypeRef, underlyingType));
		methodGroup.AddMethod(MethodDefinition.CreateOperator(methodGroup.name, targetTypeRef, underlyingType, targetTypeRef));

		methodGroup = new MethodGroupDefinition
		{
			kind = SymbolKind.MethodGroup,
			name = "op_Subtraction",
			modifiers = Modifiers.None,
			parentSymbol = targetType,
		};
		targetType.members[methodGroup.name, 0] = methodGroup;
		methodGroup.AddMethod(MethodDefinition.CreateOperator(methodGroup.name, underlyingType, targetTypeRef, targetTypeRef));
		methodGroup.AddMethod(MethodDefinition.CreateOperator(methodGroup.name, targetTypeRef, targetTypeRef, underlyingType));

		for (var opID = Operator.ID.FirstComparisonOperator; opID <= Operator.ID.LastComparisonOperator; ++opID)
		{
			var operatorName = Operator.dotNetName[(int)opID];

			methodGroup = new MethodGroupDefinition
			{
				kind = SymbolKind.MethodGroup,
				name = operatorName,
				modifiers = Modifiers.None,
				parentSymbol = targetType,
			};
			targetType.members[methodGroup.name, 0] = methodGroup;
			methodGroup.AddMethod(MethodDefinition.CreateOperator(methodGroup.name, boolTypeRef, targetTypeRef, targetTypeRef));
		}

		for (var opID = Operator.ID.op_BitwiseOr; opID <= Operator.ID.op_ExclusiveOr; ++opID)
		{
			var operatorName = Operator.dotNetName[(int)opID];

			methodGroup = new MethodGroupDefinition
			{
				kind = SymbolKind.MethodGroup,
				name = operatorName,
				modifiers = Modifiers.None,
				parentSymbol = targetType,
			};
			targetType.members[methodGroup.name, 0] = methodGroup;
			methodGroup.AddMethod(MethodDefinition.CreateOperator(methodGroup.name, targetTypeRef, targetTypeRef, targetTypeRef));
		}
	}

	public override TypeDefinitionBase BaseType()
	{
		return builtInTypes_Enum;
	}
}

public class DelegateTypeDefinition : TypeDefinition
{
	public TypeReference returnType;
	public List<ParameterDefinition> parameters = new List<ParameterDefinition>();
	
	private MethodDefinition invokeMethod;
	
	public DelegateTypeDefinition()
	{
		CreateMembers();
	}
	
	public class InvokeMethodDefinition : MethodDefinition
	{
		protected DelegateTypeDefinition owner;
		public InvokeMethodDefinition(DelegateTypeDefinition owner)
		{
			this.owner = owner;
			parameters = owner.parameters;
			
			kind = SymbolKind.Method;
			name = "Invoke";
			accessLevel = AccessLevel.Public;
			modifiers = Modifiers.Public;
			
			var methodGroup = Create(SymbolKind.MethodGroup, name) as MethodGroupDefinition;
			methodGroup.parentSymbol = owner;
			owner.members[name, 0] = methodGroup;
			parentSymbol = methodGroup;
			methodGroup.AddMethod(this);
		}
		
		public override TypeDefinitionBase ReturnType()
		{
			_returnType = owner.returnType;
			return base.ReturnType();
		}
		
		public override SymbolDefinition TypeOf()
		{
			_returnType = owner.returnType;
			return base.TypeOf();
		}
	}
	
	protected void CreateMembers()
	{
		var methodGroup = Create(SymbolKind.MethodGroup, ".ctor") as MethodGroupDefinition;
		methodGroup.parentSymbol = this;
		members[methodGroup.name, 0] = methodGroup;
		
		defaultConstructor = new MethodDefinition
		{
			kind = SymbolKind.Constructor,
			parentSymbol = methodGroup,
			name = methodGroup.name,
			accessLevel = AccessLevel.Public,
			modifiers = Modifiers.Public,
		};

		defaultConstructor.parameters = new List<ParameterDefinition>();
		var parameter = (ParameterDefinition) Create(SymbolKind.Parameter, "method");
		parameter.type = TypeReference.To(this);
		parameter.parentSymbol = defaultConstructor;
		defaultConstructor.parameters.Add(parameter);
		
		methodGroup.AddMethod(defaultConstructor);
		
		// Invoke
		invokeMethod = new InvokeMethodDefinition(this);

		// Predefined operators
		methodGroup = Create(SymbolKind.MethodGroup, "op_Addition") as MethodGroupDefinition;
		methodGroup.parentSymbol = this;
		members[methodGroup.name, 0] = methodGroup;
		var method = MethodDefinition.CreateOperator(methodGroup.name, this, this, this);
		methodGroup.AddMethod(method);

		methodGroup = Create(SymbolKind.MethodGroup, "op_Subtraction") as MethodGroupDefinition;
		methodGroup.parentSymbol = this;
		members[methodGroup.name, 0] = methodGroup;
		method = MethodDefinition.CreateOperator(methodGroup.name, this, this, this);
		methodGroup.AddMethod(method);
	}

	public override TypeDefinitionBase BaseType()
	{
		if (baseType == null)
			baseType = TypeReference.To(typeof(MulticastDelegate));
		return baseType.definition as TypeDefinitionBase;
	}
	
	public override List<TypeReference> Interfaces()
	{
		if (interfaces == null)
			interfaces = BaseType().Interfaces();
		return interfaces;
	}

	public override SymbolDefinition TypeOf()
	{
		return returnType == null ? builtInTypes_void : returnType.definition.IsValid() ? returnType.definition : unknownType;
	}

	public SymbolDefinition AddParameter(SymbolDeclaration symbol)
	{
		var symbolName = symbol.Name;
		var parameter = (ParameterDefinition) Create(symbol);
		parameter.type = TypeReference.To(symbol.parseTreeNode.FindChildByName("type"));
		parameter.parentSymbol = this;
		if (!string.IsNullOrEmpty(symbolName))
		{
			if (parameters == null)
				parameters = new List<ParameterDefinition>();
			parameters.Add(parameter);
			
			var nameNode = symbol.NameNode();
			if (nameNode != null)
				nameNode.SetDeclaredSymbol(parameter);
		}
		return parameter;
	}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter)
		{
			SymbolDefinition definition = AddParameter(symbol);
			//	if (!members.TryGetValue(symbolName, out definition) || definition is ReflectedMember || definition is ReflectedType)
			//		definition = AddMember(symbol);

			symbol.definition = definition;
			return definition;
		}

		return base.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter && parameters != null)
			parameters.Remove((ParameterDefinition) symbol.definition);
		else
			base.RemoveDeclaration(symbol);
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		if (!asTypeOnly && parameters != null)
		{
			var leafText = DecodeId(leaf.token.text);
			var definition = parameters.LastByName(leafText);
			if (definition != null)
			{
				leaf.resolvedSymbol = definition;
				return;
			}
		}
		base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
	}

	public override List<ParameterDefinition> GetParameters()
	{
		return parameters ?? _emptyParameterList;
	}

	//public override List<TypeParameterDefinition> GetTypeParameters()
	//{
	//	return null;// typeParameters ?? new List<TypeParameterDefinition>();
	//}

	private string delegateInfoText;
	public override string GetDelegateInfoText()
	{
		if (delegateInfoText == null)
		{
			delegateInfoText = TypeOf().GetName() + " " + GetName() + (parameters != null && parameters.Count == 1 ? "( " : "(");
			delegateInfoText += PrintParameters(parameters) + (parameters != null && parameters.Count == 1 ? " )" : ")");
		}
		return delegateInfoText;
	}
}

public class TypeParameterDefinition : TypeDefinitionBase
{
	public TypeReference baseTypeConstraint;
	public List<TypeReference> interfacesConstraint;
	public bool classConstraint;
	public bool structConstraint;
	public bool newConstraint;

	public override string GetTooltipText(bool fullText = true)
	{
		//if (tooltipText == null)
		{
			tooltipText = name + " in " + parentSymbol.GetName();
			if (fullText && baseTypeConstraint != null && baseTypeConstraint.definition != null)
				tooltipText += " where " + name + " : " + baseTypeConstraint.definition.GetName();
		}
		return tooltipText;
	}

	public override CharSpan GetName()
	{
		//var definingType = parentSymbol as TypeDefinition;
		//if (definingType != null && definingType.tempTypeArguments != null)
		//{
		//    var index = definingType.typeParameters.IndexOf(this);
		//    return definingType.tempTypeArguments[index].definition.GetName();
		//}
		return name;
	}

	public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
	{
		return context.TypeOfTypeParameter(this);
	}
	
	private bool resolvingBaseType;
	public override TypeDefinitionBase BaseType()
	{
		if (resolvingBaseType)
			return null;
		resolvingBaseType = true;
		
		if (baseTypeConstraint != null && (baseTypeConstraint.definition == null || !baseTypeConstraint.definition.IsValid()) ||
			interfacesConstraint != null && interfacesConstraint.Exists(InvalidSymbolReference))
		{
			baseTypeConstraint = null;
			interfacesConstraint = null;
		}
		
		if (baseTypeConstraint == null && interfacesConstraint == null)
		{
			interfacesConstraint = new List<TypeReference>();
			
			ParseTree.Node clauseNode = null;
			if (declarations != null)
			{
				for (var d = declarations; d != null; d = d.next)
				{
					if (d.IsValid())
					{
						ParseTree.Node constraintsNode = null;
						var typeParameterListNode = d.parseTreeNode.parent;
						var parentRuleName = typeParameterListNode.parent.RuleName;
						if (parentRuleName == "structDeclaration" ||
							parentRuleName == "classDeclaration" ||
							parentRuleName == "interfaceDeclaration" ||
							parentRuleName == "delegateDeclaration" ||
							parentRuleName == "interfaceMethodDeclaration")
						{
							constraintsNode = typeParameterListNode.parent.FindChildByName("typeParameterConstraintsClauses") as ParseTree.Node;
						}
						else if (parentRuleName == "qidStart" || parentRuleName == "qidPart")
						{
							constraintsNode = typeParameterListNode.parent
								.parent // qid
								.parent // memberName
								.parent // methodHeader
								.FindChildByName("typeParameterConstraintsClauses") as ParseTree.Node;
						}
						
						if (constraintsNode != null)
						{
							for (var j = 0; j < constraintsNode.numValidNodes; j++)
							{
								clauseNode = constraintsNode.NodeAt(j);
								if (clauseNode != null && clauseNode.numValidNodes == 4)
								{
									var c = clauseNode.NodeAt(1);
									if (c != null && c.numValidNodes == 1)
									{
										var id = DecodeId(c.LeafAt(0).token.text);
										if (id == name)
											break;
									}
								}
								clauseNode = null;
							}
						}
						
						// Declaration found
						break;
					}
				}
			}
			
			if (clauseNode != null)
			{
				structConstraint = false;
				classConstraint = false;
				
				var constrantListNode = clauseNode.NodeAt(3);
				if (constrantListNode != null)
				{
					var firstLeaf = constrantListNode.LeafAt(0);
					if (firstLeaf != null)
					{
						structConstraint = firstLeaf.IsLit("struct");
						classConstraint = firstLeaf.IsLit("class");
					}
					
					var secondaryList = constrantListNode.NodeAt(-1);
					if (secondaryList != null && secondaryList.RuleName == "secondaryConstraintList")
					{
						for (int i = 0; i < secondaryList.numValidNodes; i += 2)
						{
							var constraintNode = secondaryList.NodeAt(i);
							if (constraintNode != null)
							{
								var typeNameNode = constraintNode.NodeAt(0);
								if (typeNameNode != null)
								{
									if (baseTypeConstraint == null && interfacesConstraint.Count == 0)
									{
										var resolvedType = ResolveNode(typeNameNode, null, null, 0, true) as TypeDefinitionBase;
										if (resolvedType != null && resolvedType.kind != SymbolKind.Error)
										{
											if (resolvedType.kind == SymbolKind.Interface)
												interfacesConstraint.Add(TypeReference.To(typeNameNode));
											else
												baseTypeConstraint = TypeReference.To(typeNameNode);
										}
									}
									else
									{
										interfacesConstraint.Add(TypeReference.To(typeNameNode));
									}
								}
							}
						}
					}
				}
			}
		}
		
		var result = baseTypeConstraint != null ? baseTypeConstraint.definition as TypeDefinitionBase : base.BaseType();
		if (result == this)
		{
			baseTypeConstraint = TypeReference.To(circularBaseType);
			result = circularBaseType;
		}
		resolvingBaseType = false;
		return result;
	}

	public override List<TypeReference> Interfaces()
	{
		if (interfacesConstraint == null)
			BaseType();
		return interfacesConstraint ?? _emptyInterfaceList;
	}
	
	private bool checkingDerivesFromBase;
	public override bool DerivesFromRef(ref TypeDefinitionBase otherType)
	{
		if (otherType == null)
			return false;
		
		var otherTypeAsConstructed = otherType as ConstructedTypeDefinition;
		if (otherTypeAsConstructed != null)
			otherType = otherTypeAsConstructed.genericTypeDefinition;

		if (this == otherType)
			return true;

		if (checkingDerivesFromBase)
			return false;
		checkingDerivesFromBase = true;
		
		if (interfacesConstraint == null)
			BaseType();
		
		if (interfacesConstraint != null)
			for (var i = 0; i < interfacesConstraint.Count; ++i)
			{
				var typeDefinition = interfacesConstraint[i].definition as TypeDefinitionBase;
				if (typeDefinition != null && typeDefinition.DerivesFromRef(ref otherType))
				{
					checkingDerivesFromBase = false;
					return true;
				}
			}

		if (BaseType() != null)
		{
			var result = BaseType().DerivesFromRef(ref otherType);
			checkingDerivesFromBase = false;
			return result;
		}
		
		checkingDerivesFromBase = false;
		return false;
	}

	internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
	{
		if (this == typeArgument)
			return argumentType is NullTypeDefinition ? null : argumentType;

		return null;
	}
	
	
	public override void XMLDocsID(StringBuilder sb, List<SymbolDefinition> parentTypesTypeParams, List<TypeParameterDefinition> methodTypeParams)
	{
		var index = parentTypesTypeParams.IndexOf(this);
		if (index >= 0)
		{
			sb.Append('`');
			sb.Append(index);
			return;
		}

		if (methodTypeParams != null)
		{
			index = methodTypeParams.IndexOf(this as TypeParameterDefinition);
			if (index >= 0)
			{
				sb.Append("``");
				sb.Append(index);
				return;
			}
		}

#if SI3_WARNINGS
		Debug.LogWarning("TypeParameter " + name + " not found in parentTypesTypeParams or methodTypeParams!");
#endif
		sb.Append(name);
	}
}

public class ConstructedTypeDefinition : TypeDefinition
{
	private TypeDefinition _genericTypeDefinition;
	public TypeDefinition genericTypeDefinition
	{
		get { return _genericTypeDefinition; }
		private set { _genericTypeDefinition = value.GetGenericSymbol() as TypeDefinition; }
	}
	
	public readonly TypeReference[] typeArguments;

	public ConstructedTypeDefinition(TypeDefinition definition, TypeReference[] arguments)
	{
		name = definition.name;
		kind = definition.kind;
		modifiers = definition.modifiers;
		parentSymbol = definition.parentSymbol != null ? definition.parentSymbol.GetGenericSymbol() : null;
		genericTypeDefinition = definition;

		if (definition.typeParameters != null && arguments != null)
		{
			typeParameters = definition.typeParameters;
			typeArguments = new TypeReference[typeParameters.Count];
			for (var i = 0; i < typeArguments.Length && i < arguments.Length; ++i)
			{
				var argType = arguments[i];
				if (argType.definition != builtInTypes_void)
				{
					typeArguments[i] = argType;
				}
				else
				{
					typeArguments[i] = TypeReference.To(unknownType);
				}
			}
		}
	}

	public override TypeDefinitionBase GetTypeArgument(int index)
	{
		if (typeArguments == null)
			return null;
		
		if (index < 0 || index >= typeArguments.Length)
			return null;
		
		return typeArguments[index].definition as TypeDefinitionBase;
	}

	//public override ConstructedTypeDefinition ConstructType(TypeReference[] typeArgs)
	//{
	//	var result = genericTypeDefinition.ConstructType(typeArgs);
	//	result.parentSymbol = parentSymbol;
	//	return result;
	//}

	private static List<SymbolDefinition> reboundOldKeysStack = new List<SymbolDefinition>();
	private static List<SymbolDefinition> reboundNewKeysStack = new List<SymbolDefinition>();
	private static List<SymbolDefinition> reboundValuesStack = new List<SymbolDefinition>();

	private ConstructedTypeDefinition rebindingResult = null;

	public override SymbolDefinition Rebind()
	{
		if (rebindingResult != null)
			return rebindingResult;

		rebindingResult = this;
		if (parentSymbol == null)
		{
			var constructedType = base.Rebind() as ConstructedTypeDefinition;
			if (constructedType == null || constructedType == this)
			{
				rebindingResult = null;
				return this;
			}
			rebindingResult = constructedType.ConstructType(typeArguments);
			rebindingResult.genericTypeDefinition = rebindingResult.genericTypeDefinition.Rebind() as TypeDefinition;
			rebindingResult = constructedType;
		}
		else
		{
			genericTypeDefinition = genericTypeDefinition.Rebind() as TypeDefinition;
			for (var i = typeParameters.Count; i --> 0; )
				if (typeParameters[i] != null)
					typeParameters[i] = typeParameters[i].Rebind() as TypeParameterDefinition;
		}

		//if (!rebinding)
		//{
		//	rebinding = true;
		//	var baseIndex = reboundOldKeysStack.Count;
		//	foreach (var kv in rebindingResult.constructedMembers)
		//	{
		//		var reboundKey = kv.Key.Rebind();
		//		var reboundValue = kv.Value.Rebind();
		//		if (reboundKey != null && reboundValue != null && (reboundKey != kv.Key || reboundValue != kv.Value))
		//		{
		//			reboundOldKeysStack.Add(kv.Key);
		//			reboundNewKeysStack.Add(reboundKey);
		//			reboundValuesStack.Add(reboundValue);
		//		}
		//	}
		//	if (reboundOldKeysStack.Count > baseIndex)
		//	{
		//		for (var i = baseIndex; i < reboundOldKeysStack.Count; ++i)
		//		{
		//			var oldKey = reboundOldKeysStack[i];
		//			var newKey = reboundNewKeysStack[i];
		//			if (oldKey != newKey)
		//				rebindingResult.constructedMembers.Remove(oldKey);
		//		}
		//		for (var i = baseIndex; i < reboundOldKeysStack.Count; ++i)
		//		{
		//			var newKey = reboundNewKeysStack[i];
		//			rebindingResult.constructedMembers[newKey] = reboundValuesStack[i];
		//		}
		//		reboundOldKeysStack.RemoveRange(baseIndex, reboundOldKeysStack.Count - baseIndex);
		//		reboundNewKeysStack.RemoveRange(baseIndex, reboundNewKeysStack.Count - baseIndex);
		//		reboundValuesStack.RemoveRange(baseIndex, reboundValuesStack.Count - baseIndex);
		//	}
		//	rebinding = false;
		//}

		var result = rebindingResult;
		rebindingResult = null;
		return result;
	}

	public override SymbolDefinition TypeOf()
	{
		if (kind != SymbolKind.Delegate)
			return base.TypeOf();
		
		var result = genericTypeDefinition.TypeOf() as TypeDefinitionBase;
		result = result.SubstituteTypeParameters(this);
		return result;
	}
	
	public override SymbolDefinition GetGenericSymbol()
	{
		return genericTypeDefinition.GetGenericSymbol();
	}

	public override TypeDefinitionBase TypeOfTypeParameter(TypeParameterDefinition tp)
	{
		if (typeParameters != null)
		{
			var index = typeParameters.IndexOf(tp);
			if (index >= 0)
			{
				if (typeArguments[index] != null)
				{
					var result = typeArguments[index].definition as TypeDefinitionBase;
					if (result != null)
						return result;
				}
			}

			var tpParent = tp.parentSymbol;
			if (tpParent != null && tpParent != this)
			{
				var constructedBase = tpParent as TypeDefinitionBase;
				if (constructedBase != null && DerivesFromRef(ref constructedBase))
				{
					tpParent = constructedBase;
				}
				if (tpParent != null && tpParent != this)
				{
					var result = tpParent.TypeOfTypeParameter(tp);
					if (result != null && result.kind != SymbolKind.TypeParameter)
						return result;
				}
			}
		}
		return base.TypeOfTypeParameter(tp);
	}

	public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
	{
		var target = this;
		var parentType = parentSymbol as TypeDefinitionBase;
		if (parentType != null)
		{
			parentType = parentType.SubstituteTypeParameters(context);
			var constructedParent = parentType as ConstructedTypeDefinition;
			if (constructedParent != null)
				target = constructedParent.GetConstructedMember(this.genericTypeDefinition) as ConstructedTypeDefinition;
		}

		if (typeArguments == null)
			return target;

		var constructNew = false;
		var newArguments = TypeReference.AllocArray(typeArguments.Length);
		for (var i = 0; i < newArguments.Length; ++i)
		{
			newArguments[i] = typeArguments[i];
			var original = typeArguments[i] != null ? typeArguments[i].definition as TypeDefinitionBase : null;
			var genericType = original;
			if (genericType == null)
			{
				genericType = typeParameters[i];
				if (genericType == null)
					continue;
			}
			var substitute = genericType.SubstituteTypeParameters(context);
			if (substitute != original)
			{
				newArguments[i] = TypeReference.To(substitute);
				constructNew = true;
			}
		}
		if (!constructNew)
		{
			TypeReference.ReleaseArray(newArguments);
			return target;
		}

		var result = target.ConstructType(newArguments, parentSymbol as TypeDefinition);
		TypeReference.ReleaseArray(newArguments);
		return result;
	}

	internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
	{
		if (argumentType.kind == SymbolKind.LambdaExpression)
		{
			TypeDefinitionBase boundType = null;

			var lambdaParams = argumentType.GetParameters();
			if (lambdaParams != null)
			{
				var numLambdaParams = lambdaParams.Count;

				var parameters = GetParameters();
				if (parameters != null)
				{
					var numParams = parameters.Count;

					for (int i = UnityEngine.Mathf.Min(numLambdaParams, numParams); i --> 0; )
					{
						var lambdaParam = lambdaParams[i];
						if (lambdaParam == null)
							continue;
						var lambdaParamType = lambdaParam.TypeOf() as TypeDefinitionBase;
						if (lambdaParam == null || lambdaParam.kind == SymbolKind.Error)
							continue;

						var parameter = parameters[i];
						if (parameter == null)
							continue;
						var paramType = parameter.TypeOf() as TypeDefinitionBase;
						if (paramType == null || paramType.kind == SymbolKind.Error)
							continue;
						paramType = paramType.SubstituteTypeParameters(this);
						if (paramType == null || paramType.kind == SymbolKind.Error)
							continue;

						boundType = paramType.BindTypeArgument(typeArgument, lambdaParamType);
						if (boundType != null)
							return boundType;
					}
				}
			}

			var asLambda = argumentType as LambdaExpressionDefinition;
			if (asLambda != null)
			{
				var returnType = asLambda.ReturnType(this);
				if (returnType != null && returnType != builtInTypes_void)
				{
					var thisReturnType = TypeOf() as TypeDefinitionBase;
					if (thisReturnType != null)
						boundType = thisReturnType.BindTypeArgument(typeArgument, returnType);
					if (boundType != null)
						return boundType;
				}
			}

			boundType = argumentType.BindTypeArgument(typeArgument, TypeOf() as TypeDefinitionBase);
			return boundType;
		}

		var convertedArgument = argumentType.ConvertTo(this);

		//TypeDefinitionBase convertedArgument = this;
		//if (!argumentType.DerivesFromRef(ref convertedArgument))
		//	return base.BindTypeArgument(typeArgument, argumentType);
			
		var argumentAsConstructedType = convertedArgument as ConstructedTypeDefinition;
		if (argumentAsConstructedType != null && GetGenericSymbol() == argumentAsConstructedType.GetGenericSymbol())
		{
			TypeDefinitionBase inferedType = null;
			for (int i = 0; i < NumTypeParameters; ++i)
			{
				var fromConstructedType = argumentAsConstructedType.typeArguments[i].definition as TypeDefinitionBase;
				if (fromConstructedType != null)
				{
					var bindTarget = typeArguments[i].definition as TypeDefinitionBase;
					var boundTypeArgument = bindTarget.BindTypeArgument(typeArgument, fromConstructedType);
					if (boundTypeArgument != null)
					{
						if (inferedType == null || inferedType.CanConvertTo(boundTypeArgument))
							inferedType = boundTypeArgument;
						else if (!boundTypeArgument.CanConvertTo(inferedType))
							return null;
					}
				}
			}
			
			if (inferedType != null)
				return inferedType;
		}
		return base.BindTypeArgument(typeArgument, argumentType);
	}

	public override List<TypeReference> Interfaces()
	{
		if (interfaces == null)
			BaseType();
		return interfaces;
	}

	public override TypeDefinitionBase BaseType()
	{
		var rebuildInterfaces = interfaces == null;

		if (baseType != null && (baseType.definition == null || !baseType.definition.IsValid()) ||
			interfaces != null && interfaces.Exists(InvalidSymbolReference))
		{
			baseType = null;
			rebuildInterfaces = true;
		}

		if (rebuildInterfaces)
		{
			var genericTypeDef = genericTypeDefinition;
			var baseTypeDef = genericTypeDef.BaseType();
			baseType = baseTypeDef != null ? TypeReference.To(baseTypeDef.SubstituteTypeParameters(this)) : null;

			var genericInterfaces = genericTypeDef.Interfaces();
			if (interfaces == null)
			{
				if (genericInterfaces == null)
					interfaces = new List<TypeReference>();
				else
					interfaces = new List<TypeReference>(genericInterfaces);
			}
			else
			{
				interfaces.Clear();
				if (genericInterfaces != null)
					interfaces.AddRange(genericInterfaces);
			}

			for (var i = 0; i < interfaces.Count; ++i)
			{
				var interfaceDefinition = interfaces[i].definition as TypeDefinitionBase;
				if (interfaceDefinition != null)
					interfaces[i] = TypeReference.To(interfaceDefinition.SubstituteTypeParameters(this));
			}
		}
		return baseType != null ? baseType.definition as TypeDefinitionBase : base.BaseType();
	}

	public override List<ParameterDefinition> GetParameters()
	{
		return genericTypeDefinition.GetParameters();
	}

	public override bool CanConvertTo(TypeDefinitionBase otherType)
	{
		if (this == otherType)
			return true;

		if (ConvertTo(otherType) != null)
			return true;

		var hasImplicitConversion = CollectImplicitConversionOperators(this, otherType);
		
		return hasImplicitConversion;
	}

	public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
	{
		if (otherType == null)
			return null;

		if (this == otherType || otherType is TypeParameterDefinition)
			return this;

		if (genericTypeDefinition == otherType)
			return this;

		var otherGenericType = otherType.GetGenericSymbol() as TypeDefinitionBase;
		if (genericTypeDefinition == otherGenericType)
		{
			var otherConstructedType = otherType as ConstructedTypeDefinition;
			var otherTypeTypeArgs = otherConstructedType.typeArguments;
			
			var numTypeArgs = typeArguments != null ? typeArguments.Length : 0;

			var convertedTypeArgs = TypeReference.AllocArray(numTypeArgs);
			for (var i = 0; i < numTypeArgs; i++)
			{
				var typeArgument = typeArguments[i].definition as TypeDefinitionBase;
				if (typeArgument == null)
					typeArgument = otherTypeTypeArgs[i].definition as TypeDefinitionBase;
				else
					typeArgument = typeArgument.ConvertTo(otherTypeTypeArgs[i].definition as TypeDefinitionBase);
				if (typeArgument == null)
				{
					TypeReference.ReleaseArray(convertedTypeArgs);
					convertedTypeArgs = null;
					break;
				}
				
				var typeReference = TypeReference.To(typeArgument);
				convertedTypeArgs[i] = typeReference;
			}

			if (convertedTypeArgs != null)
			{
				var convertedType = genericTypeDefinition.ConstructType(convertedTypeArgs, parentSymbol as TypeDefinition);
				TypeReference.ReleaseArray(convertedTypeArgs);
				return convertedType;
			}
		}

		if (convertingToBase)
			return null;
		convertingToBase = true;
		
		var baseTypeDefinition = BaseType();

		if (otherType.kind == SymbolKind.Interface)
		{
			for (int i = 0; i < interfaces.Count; i++)
			{
				var interfaceType = (TypeDefinitionBase) interfaces[i].definition;
				if (interfaceType == null)
					continue;
				var convertedToInterface = interfaceType.ConvertTo(otherType);
				if (convertedToInterface != null)
				{
					convertingToBase = false;
					return convertedToInterface;
				}
			}
		}

		if (baseTypeDefinition != null)
		{
			var converted = baseTypeDefinition.ConvertTo(otherType);
			if (converted != null)
			{
				convertingToBase = false;
				return converted;
			}
		}
		
		convertingToBase = false;
		
		return null;
	}

	public override bool DerivesFromRef(ref TypeDefinitionBase otherType)
	{
		if (otherType == null)
			return false;
		
		if (genericTypeDefinition == otherType)
		{
			otherType = this;
			return true;
		}
		
		var otherGenericType = otherType.GetGenericSymbol() as TypeDefinitionBase;
		if (genericTypeDefinition == otherGenericType)
		{
			var otherConstructedType = otherType as ConstructedTypeDefinition;
			var otherTypeTypeArgs = otherConstructedType.typeArguments;
			
			var numTypeArgs = typeArguments != null ? typeArguments.Length : 0;

			var convertedTypeArgs = TypeReference.AllocArray(numTypeArgs);
			for (var i = 0; i < numTypeArgs; i++)
			{
				var typeArgument = typeArguments[i].definition as TypeDefinitionBase;
				if (typeArgument == null)
					typeArgument = otherTypeTypeArgs[i].definition as TypeDefinitionBase;
				else
				{
					var otherTypeTypeArg = otherTypeTypeArgs[i].definition as TypeDefinitionBase;
					//if (typeArgument.DerivesFromRef(ref otherTypeTypeArg))
					//	typeArgument = otherTypeTypeArg;
					if (otherTypeTypeArg != null)
						typeArgument = otherTypeTypeArg.SubstituteTypeParameters(this);
				}
				if (typeArgument == null)
				{
					TypeReference.ReleaseArray(convertedTypeArgs);
					convertedTypeArgs = null;
					break;
				}
				
				var typeReference = TypeReference.To(typeArgument);
				convertedTypeArgs[i] = typeReference;
			}

			if (convertedTypeArgs != null)
			{
				otherType = genericTypeDefinition.ConstructType(convertedTypeArgs, parentSymbol as TypeDefinition);
				TypeReference.ReleaseArray(convertedTypeArgs);
				return true;
			}
		}

		var baseType = BaseType();

		if (otherType.kind == SymbolKind.Interface || otherType.kind == SymbolKind.TypeParameter)
		{
			for (var i = interfaces.Count; i --> 0; )
			{
				var element = interfaces[i];
				var elementDefinition = element.definition;
				if (elementDefinition == null)
					continue;
				if (((TypeDefinitionBase) element.definition).DerivesFromRef(ref otherType))
				{
					otherType = otherType.SubstituteTypeParameters(this);
					return true;
				}
			}
		}

		if (baseType != null && baseType.DerivesFromRef(ref otherType))
		{
			otherType = otherType.SubstituteTypeParameters(this);
			return true;
		}

		return false;
	}

	public override bool DerivesFrom(TypeDefinitionBase otherType)
	{
		if (otherType == null)
			return false;
		
		return genericTypeDefinition.DerivesFrom(otherType);
	}

	public override CharSpan GetName()
	{
		if (typeArguments == null || typeArguments.Length == 0)
			return name;
		
		if (_genericTypeDefinition == builtInTypes_Nullable && typeArguments.Length == 1)
		{
			var nullableType = typeArguments[0];
			if (nullableType != null)
				return nullableType.definition.GetName() + '?';
		}

		var sb = StringBuilders.Alloc();
		
		sb.Append(name);
		var comma = "<";
		for (var i = 0; i < typeArguments.Length; ++i)
		{
			sb.Append(comma);
			if (typeArguments[i] != null)
				sb.Append(typeArguments[i].definition.GetName());
			comma = ", ";
		}
		sb.Append('>');
		var result = sb.ToString();
		
		StringBuilders.Release(sb);
		
		return result;
	}
	
	//public override string GetDelegateInfoText()
	//{
	//	var result = genericTypeDefinition.GetTooltipText();
	//	return result;
	//}

//	public override string GetTooltipText()
//	{
//		return base.GetTooltipText();

////		if (tooltipText != null)
////			return tooltipText;

//		if (parentSymbol != null && !string.IsNullOrEmpty(parentSymbol.GetName()))
//			tooltipText = kind.ToString().ToLowerInvariant() + " " + parentSymbol.GetName() + ".";// + name;
//		else
//			tooltipText = kind.ToString().ToLowerInvariant() + " ";// +name;

//		tooltipText += GetName();
//		//tooltipText += "<" + (typeArguments[0] != null ? typeArguments[0].definition : genericTypeDefinition.typeParameters[0]).GetName();
//		//for (var i = 1; i < typeArguments.Length; ++i)
//		//    tooltipText += ", " + (typeArguments[i] != null ? typeArguments[i].definition : genericTypeDefinition.typeParameters[i]).GetName();
//		//tooltipText += '>';

//		var xmlDocs = GetXmlDocs();
//		if (!string.IsNullOrEmpty(xmlDocs))
//		{
//		    tooltipText += "\n\n" + xmlDocs;
//		}

//		return tooltipText;
//	}

	public override SymbolDefinition FindName(CharSpan memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName.DecodeId();
		
		return genericTypeDefinition.FindName(memberName, numTypeParameters, asTypeOnly);
	}

	public Dictionary<SymbolDefinition, SymbolDefinition> constructedMembers;

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		genericTypeDefinition.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
		
		var genericMember = leaf.resolvedSymbol;
		if (genericMember == null)// || genericMember is MethodGroupDefinition)// !genericMember.IsGeneric)
			return;

		SymbolDefinition constructed;
		if (constructedMembers != null && constructedMembers.TryGetValue(genericMember, out constructed))
			leaf.resolvedSymbol = constructed;
		else if (genericMember.parentSymbol == genericTypeDefinition)
			leaf.resolvedSymbol = GetConstructedMember(genericMember);
		else
		{
			TypeDefinitionBase baseType = genericMember.parentSymbol as TypeDefinitionBase;
			if (DerivesFromRef(ref baseType))
			{
				var constructedBaseType = baseType as ConstructedTypeDefinition;
				if (constructedBaseType != null)
					leaf.resolvedSymbol = constructedBaseType.GetConstructedMember(genericMember);
			}
		}

		if (asTypeOnly && !(leaf.resolvedSymbol is TypeDefinitionBase))
			leaf.resolvedSymbol = null;
	}
	
	public override MethodDefinition GetDefaultConstructor()
	{
		if (defaultConstructor == null)
		{
			var genericConstructor = base.GetDefaultConstructor();
			defaultConstructor = genericConstructor;
		}
		return defaultConstructor;
	}

	public SymbolDefinition GetConstructedMember(SymbolDefinition member)
	{
		if (member == null)
			return null;
		
		var parent = member.parentSymbol;
		if (parent is MethodGroupDefinition)
			parent = parent.parentSymbol;
		
		if (parent == this)
			return member;
		
		//if (genericTypeDefinition != parent.GetGenericSymbol())
		{
#if SI3_WARNINGS
			//UnityEngine.Debug.Log(member.GetType() + " " + member.GetTooltipText() + " is not member of " + genericTypeDefinition.GetType() + " " + genericTypeDefinition.GetTooltipText() + " but " + parent.GetType() + " " + parent.GetTooltipText() + "\n" + member.FullReflectionName + " " + genericTypeDefinition.FullReflectionName);
#endif
		//	return member;
		}

		//if (!member.IsGeneric)
		//    return member;

		SymbolDefinition constructed;
		if (constructedMembers == null)
			constructedMembers = new Dictionary<SymbolDefinition, SymbolDefinition>();
		else if (constructedMembers.TryGetValue(member, out constructed))
			return constructed;

		constructed = ConstructMember(member);
		constructedMembers[member] = constructed;
		return constructed;
	}
	
	public static readonly TypeReference[] emptySymbolReferenceArray = new TypeReference[]{};
	private SymbolDefinition ConstructMember(SymbolDefinition member)
	{
		SymbolDefinition symbol;
		//if (member is InstanceDefinition)
		//{
		//	symbol = new ConstructedInstanceDefinition(member as InstanceDefinition);
		//}
		var nestedType = member as TypeDefinition;
		if (nestedType != null)
		{
			var nestedTypeParams = nestedType.typeParameters;
			if (nestedTypeParams == null || nestedTypeParams.Count == 0)
			{
				symbol = nestedType.ConstructType(emptySymbolReferenceArray, this);
			}
			else
			{
				var numTypeParams = nestedTypeParams.Count;
				TypeReference[] typeParams = TypeReference.AllocArray(numTypeParams);
				for (var i = 0; i < numTypeParams; ++i)
					typeParams[i] = TypeReference.To(nestedTypeParams[i]);

				symbol = nestedType.ConstructType(typeParams, this);
				TypeReference.ReleaseArray(typeParams);
			}
		}
		else
		{
			symbol = new ConstructedSymbolReference(member);
		}
		symbol.parentSymbol = this;
		return symbol;
	}

	public override bool IsSameType(TypeDefinitionBase type)
	{
		if (type == this)
			return true;
		
		var constructedType = type as ConstructedTypeDefinition;
		if (constructedType == null)
			return false;
		
		if (genericTypeDefinition != constructedType.genericTypeDefinition)
			return false;
		
		for (var i = 0; i < typeArguments.Length; ++i)
			if (!typeArguments[i].definition.IsSameType(constructedType.typeArguments[i].definition as TypeDefinitionBase))
				return false;
		
		return true;
	}

	public override SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
	{
		var indexers = GetAllIndexers();

		// TODO: Resolve overloads

		return indexers != null ? indexers[indexers.Count - 1] : null;
	}

	public override List<SymbolDefinition> GetAllIndexers()
	{
		List<SymbolDefinition> indexers = genericTypeDefinition.GetAllIndexers();
		if (indexers != null)
		{
			for (var i = 0; i < indexers.Count; ++i)
			{
				var member = indexers[i];
				member = GetConstructedMember(member);
				indexers[i] = member;
			}
		}
		return indexers;
	}

	//public override bool IsGeneric
	//{
	//	get
	//	{
	//		return false;
	//	}
	//}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		var dataFromDefinition = new Dictionary<string,SymbolDefinition>();
		genericTypeDefinition.GetMembersCompletionData(dataFromDefinition, flags, mask, context);
		foreach (var entry in dataFromDefinition)
		{
			if (!data.ContainsKey(entry.Key))
			{
				var member = GetConstructedMember(entry.Value);
				data.Add(entry.Key, member);
			}
		}

	//	base.GetMembersCompletionData(data, flags, mask, assembly);

		// TODO: Is this really needed?
	//	if (BaseType() != null && (kind != SymbolKind.Enum || flags != BindingFlags.Static))
	//		BaseType().GetMembersCompletionData(data, flags, mask & ~AccessLevelMask.Private, assembly);
	}
	
	public override void XMLDocsID(StringBuilder sb, List<SymbolDefinition> parentTypesTypeParams, List<TypeParameterDefinition> methodTypeParams)
	{
		parentSymbol.XMLDocsID(sb, parentTypesTypeParams, methodTypeParams);
		
		if (sb.Length > 0)
			sb.Append('.');
		
		sb.Append(name);
		
		if (typeArguments == null || typeArguments.Length == 0)
			return;
		
		sb.Append('{');
		for (var i = 0; i < typeArguments.Length; ++i)
		{
			if (i > 0)
				sb.Append(',');
			
			typeArguments[i].definition.XMLDocsID(sb, parentTypesTypeParams, methodTypeParams);
		}
		sb.Append('}');
	}

	public TupleTypeDefinition MakeTupleType()
	{
		var fromType = this;
		var generic = genericTypeDefinition;
		if (generic == null || generic.kind != SymbolKind.Struct)
			return null;

		List<TypeReference> fieldTypes = null;
		while (generic != null)
		{
			if (generic == builtInTypes_ValueTuple_1 || generic == builtInTypes_ValueTuple_2 || generic == builtInTypes_ValueTuple_3 || generic == builtInTypes_ValueTuple_4 ||
				generic == builtInTypes_ValueTuple_5 || generic == builtInTypes_ValueTuple_6 || generic == builtInTypes_ValueTuple_7 || generic == builtInTypes_ValueTuple_8)
			{
				if (fieldTypes == null)
					fieldTypes = new List<TypeReference>();
				for (int i = 0; i < 7 && i < fromType.typeArguments.Length; ++i)
					fieldTypes.Add(TypeReference.To(fromType.typeArguments[i].definition as TypeDefinitionBase));

				if (generic == builtInTypes_ValueTuple_8)
				{
					fromType = fromType.typeArguments[7].definition as ConstructedTypeDefinition;
					generic = fromType == null ? null : fromType.genericTypeDefinition;
				}
				else
				{
					break;
				}
			}
			else
			{
				break;
			}
		}

		if (fieldTypes == null)
			return null;

		var tupleType = new TupleTypeDefinition(this, fieldTypes);
		return tupleType;
	}
}

public class TupleField : InstanceDefinition
{
	public CharSpan aliasName;
	
	public override CharSpan GetName()
	{
		if (aliasName.IsEmpty)
			return name;
		else
			return aliasName;
	}
}

public class TupleTypeDefinition : TypeDefinition
{
	private TupleField[] fields;
	
	public TupleTypeDefinition(TypeDefinition baseType, List<TypeReference> elementTypes)
	{
		this.baseType = TypeReference.To(baseType);
		parentSymbol = baseType.parentSymbol;

		fields = new TupleField[elementTypes.Count];
		
		for (var i = 0; i < elementTypes.Count; ++i)
		{
			var elementType = elementTypes[i];
			
			var field = new TupleField();
			fields[i] = field;
			
			field.name = GetDefaultFieldName(i);
			field.kind = SymbolKind.Field;
			field.accessLevel = AccessLevel.Public;
			field.modifiers = Modifiers.Public;
			field.parentSymbol = this;
			field.type = elementType;
			
			if (elementType != null && elementType.Node != null)
			{
				var nameLeaf = elementType.Node.nextSibling as ParseTree.Leaf;
				if (nameLeaf != null)
				{
					var name = nameLeaf.token.text;
					field.aliasName = name;
					nameLeaf.SetDeclaredSymbol(field);
				}
			}
		}
	}
	
	public int Arity
	{
		get
		{
			return fields.Length;
		}
	}
	
	public SymbolDefinition SetElementAliasName(int index, string value)
	{
		if (index >= fields.Length)
		{
			#if SI3_WARNINGS
			Debug.Log("index " + index + " >= fields.Length " + fields.Length);
			#endif
			return null;
		}
		var field = fields[index];
		field.aliasName = value;
		return field;
	}

	public string GetElementName(int index)
	{
		return fields[index].aliasName.IsEmpty ? fields[index].name : fields[index].aliasName;
	}
	
	string _GetElementAliasName(int index)
	{
		return fields[index].aliasName;
	}
	
	public TypeDefinitionBase TypeOfElement(int index)
	{
		return fields[index].TypeOf() as TypeDefinitionBase;
	}
	
	public TypeDefinitionBase TypeOfDeconstructNode(ParseTree.Node node)
	{
		if (node == null || node.parent == null || node.parent.parent == null)
			return null;
		
		var index = node.childIndex / 2;
		node = node.parent.parent;
		var ruleName = node.RuleName;
		if (ruleName == "implicitDeconstructDeclaration" || ruleName == "explicitDeconstructDeclaration" || ruleName == "foreachStatement")
			return TypeOfElement(index);
		
		var nestedTupleType = TypeOfDeconstructNode(node) as TupleTypeDefinition;
		if (nestedTupleType == null)
			return null;
		
		return nestedTupleType.TypeOfElement(index);
	}
	
	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		var id = DecodeId(leaf.token.text);
		
		leaf.resolvedSymbol = FindName(id, 0, false);
		if (leaf.resolvedSymbol != null)
			leaf.semanticError = null;
	}
	
	public override SymbolDefinition FindName(CharSpan memberName, int numTypeParameters, bool asTypeOnly)
	{
		for (var i = Arity; i --> 0; )
		{
			var field = fields[i];
			if (memberName == field.name || memberName == field.aliasName)
				return field;
		}
		
		var constructedBase = baseType.definition as ConstructedTypeDefinition;
		if (constructedBase == null)
			return null;

		var member = constructedBase.FindName(memberName, numTypeParameters, asTypeOnly);
		if (member == null)
			return null;
		
		return constructedBase.GetConstructedMember(member);
	}
	
	static string[] defaultFieldNames =
	{
		"Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Item8", "Item9", "Item10",
		"Item11", "Item12", "Item13", "Item14", "Item15", "Item16", "Item17", "Item18", "Item19", "Item20",
		"Item21", "Item22", "Item23", "Item24", "Item25", "Item26", "Item27", "Item28", "Item29", "Item30",
	};
	
	static string GetDefaultFieldName(int index)
	{
		if (index > defaultFieldNames.Length)
			return "Item" + (index + 1);
		else
			return defaultFieldNames[index];
	}
	
	public override CharSpan GetName()
	{
		if (!name.IsEmpty)
			return name;

		var sb = StringBuilders.Alloc();
		
		var comma = "(";
		var arity = Arity;
		for (var i = 0; i < arity; ++i)
		{
			var field = fields[i];
			
			sb.Append(comma);
			var elementType = field.TypeOf();
			if (elementType != null)
				sb.Append(elementType.GetName());
			else
				sb.Append('?');
			var aliasName = field.aliasName;
			if (!aliasName.IsEmpty)
			{
				sb.Append(' ');
				sb.Append(aliasName);
			}
			comma = ", ";
		}
		sb.Append(')');
		name = sb.ToString();
		
		StringBuilders.Release(sb);
		
		return name;
	}
	
	public override string GetTooltipText(bool fullText = true)
	{
		var result = GetName();
		return result;
	}

	public override bool IsSameType(TypeDefinitionBase type)
	{
		var asTuple = type as TupleTypeDefinition;
		if (asTuple == null)
			return false;

		if (asTuple.Arity != Arity)
			return false;

		for (int i = 0; i < Arity; ++i)
		{
			var thisFieldType = fields[i].type.definition as TypeDefinitionBase;
			if (thisFieldType == null || thisFieldType.kind == SymbolKind.Error)
				return false;
			
			var otherFieldType = asTuple.fields[i].type.definition as TypeDefinitionBase;
			if (otherFieldType == null || otherFieldType.kind == SymbolKind.Error)
				return false;
			
			if (!thisFieldType.IsSameType(otherFieldType))
				return false;
		}

		return true;
	}

	public override bool CanConvertTo(TypeDefinitionBase otherType)
	{
		var asTuple = otherType as TupleTypeDefinition;
		if (asTuple == null)
			return base.CanConvertTo(otherType);

		if (asTuple.Arity != Arity)
			return false;

		for (int i = 0; i < Arity; ++i)
		{
			var thisFieldType = fields[i].type.definition as TypeDefinitionBase;
			if (thisFieldType == null || thisFieldType.kind == SymbolKind.Error)
				return false;
			
			var otherFieldType = asTuple.fields[i].type.definition as TypeDefinitionBase;
			if (otherFieldType == null || otherFieldType.kind == SymbolKind.Error)
				return false;
			
			if (!thisFieldType.CanConvertTo(otherFieldType))
				return false;
		}

		return true;
	}

	public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
	{
		var asTuple = otherType as TupleTypeDefinition;
		if (asTuple == null)
			return base.ConvertTo(otherType);

		if (asTuple.Arity != Arity)
			return unknownType;

		var elementTypes = new List<TypeReference>(Arity);
		for (int i = 0; i < Arity; ++i)
		{
			var thisFieldType = fields[i].type.definition as TypeDefinitionBase;
			if (thisFieldType == null || thisFieldType.kind == SymbolKind.Error)
				return unknownType;
			
			var otherFieldType = asTuple.fields[i].type.definition as TypeDefinitionBase;
			if (otherFieldType == null || otherFieldType.kind == SymbolKind.Error)
				return unknownType;
			
			var converted = thisFieldType.ConvertTo(otherFieldType);
			if (converted == null || converted.kind == SymbolKind.Error)
				return unknownType;

			elementTypes.Add(TypeReference.To(converted));
		}

		return MakeTupleType(elementTypes);
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		base.GetMembersCompletionData(data, flags, mask, context);
		
		var arity = Arity;
		for (int i = 0; i < arity; ++i)
		{
			var field = fields[i];
			if (!field.aliasName.IsEmpty)
			{
				var aliasField = new IndexerDefinition();
				
				aliasField.name = field.aliasName;
				aliasField.kind = field.kind;
				aliasField.modifiers = field.modifiers;
				aliasField.accessLevel = field.accessLevel;
				aliasField.parentSymbol = this;
				aliasField.type = field.type;
				
				data.Remove(field.name);
				data[field.aliasName] = aliasField;
			}
			else
			{
				data[field.name] = field;
			}
		}
	}

	internal bool AreAllFieldsValid()
	{
		for (var i = Arity; i --> 0; )
			if (!fields[i].type.IsValid())
				return false;
		return true;
	}
}

public class ConstructedSymbolReference : SymbolDefinition
{
	public SymbolDefinition referencedSymbol { get; private set; }

	public ConstructedSymbolReference(SymbolDefinition referencedSymbolDefinition)
	{
		referencedSymbol = referencedSymbolDefinition;
		kind = referencedSymbol.kind;
		modifiers = referencedSymbol.modifiers;
		accessLevel = referencedSymbol.accessLevel;
		name = referencedSymbol.name;
		//parentSymbol = referencedSymbol.parentSymbol;
	}

	public override CharSpan GetName()
	{
		return referencedSymbol.GetName();
	}

	//public static implicit operator MethodGroupDefinition(ConstructedSymbolReference reference)
	//{
	//	if (reference.kind != SymbolKind.MethodGroup)
	//		return null;
	//	var referencedSymbol = reference.referencedSymbol as MethodGroupDefinition;
	//	return referencedSymbol;
	//}

		public override SymbolDefinition Rebind()
	{
		referencedSymbol = referencedSymbol.Rebind();
		return base.Rebind();
	}

	public override bool IsExtensionMethod
	{
		get { return referencedSymbol.IsExtensionMethod; }
	}

	public override TypeDefinitionBase TypeOfTypeParameter(TypeParameterDefinition tp)
	{
		var fromReferencedSymbol = referencedSymbol.TypeOfTypeParameter(tp);
		var asTypeParameter = fromReferencedSymbol as TypeParameterDefinition;
		if (asTypeParameter != null)
			return base.TypeOfTypeParameter(tp);
		else
			return fromReferencedSymbol;
	}

	public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
	{
		return base.SubstituteTypeParameters(context);
	}

	public override SymbolDefinition TypeOf()
	{
		var result = referencedSymbol.GetGenericSymbol().TypeOf() as TypeDefinitionBase;
		
		var ctx = parentSymbol as ConstructedTypeDefinition;
		if (ctx != null && result != null)
			result = result.SubstituteTypeParameters(ctx);

		if (referencedSymbol != referencedSymbol.GetGenericSymbol())
			result = result.SubstituteTypeParameters(referencedSymbol);

		return result;
	}
	
	public override SymbolDefinition GetGenericSymbol()
	{
		return referencedSymbol.GetGenericSymbol();
	}

	public override List<ParameterDefinition> GetParameters()
	{
		return referencedSymbol.GetParameters();
	}

	public override List<TypeParameterDefinition> GetTypeParameters()
	{
		return referencedSymbol.GetTypeParameters();
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		if (asTypeOnly)
			return;

		var symbolType = TypeOf() as TypeDefinitionBase;
		if (symbolType != null)
			symbolType.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
	}

	public override SymbolDefinition ResolveMethodOverloads(ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		if (kind != SymbolKind.MethodGroup)
			return null;
		if (referencedSymbol.parentSymbol == null && referencedSymbol.savedParentSymbol != null)
			referencedSymbol = referencedSymbol.Rebind();
		var symbolName = (GetType() ?? referencedSymbol.GetType()).Name;
		var genericMethod = referencedSymbol.ResolveMethodOverloads(argumentListNode, typeArgs, scope, invokedLeaf);
		if (genericMethod == null || genericMethod.kind != SymbolKind.Method || 0x44 < symbolName[0])
			return null;
		return ((ConstructedTypeDefinition) parentSymbol).GetConstructedMember(genericMethod);
	}
	
	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		var symbolType = TypeOf();
		if (symbolType != null)
			symbolType.GetMembersCompletionData(data, BindingFlags.Instance, mask, context);
	}
}

public class PointerTypeDefinition : TypeDefinitionBase
{
	public readonly TypeReference referentType;
	
	public PointerTypeDefinition(TypeDefinitionBase referentType)
	{
		kind = referentType.kind;
		this.referentType = TypeReference.To(referentType);
		name = referentType.GetName();
	}
	
	public override CharSpan GetName()
	{
		return referentType.definition.GetName() + "*";
	}
	
	public override TypeDefinitionBase BaseType()
	{
		return (referentType.definition as TypeDefinitionBase ?? unknownType).BaseType();
	}
	
	internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
	{
		return (referentType.definition as TypeDefinitionBase ?? unknownType).BindTypeArgument(typeArgument, argumentType);
	}
	
	public override bool CanConvertTo(TypeDefinitionBase otherType)
	{
		if (otherType == builtInTypes_void.MakePointerType())
			return true;
		
		// ???
		return (referentType.definition as TypeDefinitionBase ?? unknownType).CanConvertTo(otherType);
	}
	
	public override string CompletionDisplayString(string styledName)
	{
		return referentType.definition.CompletionDisplayString(styledName);
	}
	
	public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
	{
		if (otherType == builtInTypes_void.MakePointerType())
			return otherType;
		
		// ???
		return (referentType.definition as TypeDefinitionBase ?? unknownType).ConvertTo(otherType);
	}
	
	public override bool DerivesFrom(TypeDefinitionBase otherType)
	{
		// ???
		return (referentType.definition as TypeDefinitionBase ?? unknownType).DerivesFrom(otherType);
	}
	
	public override bool DerivesFromRef(ref TypeDefinitionBase otherType)
	{
		// ???
		return (referentType.definition as TypeDefinitionBase ?? unknownType).DerivesFromRef(ref otherType);
	}
	
	public override SymbolDefinition FindName(CharSpan memberName, int numTypeParameters, bool asTypeOnly)
	{
		return referentType.definition.FindName(memberName, numTypeParameters, asTypeOnly);
	}
	
	//public override List<SymbolDefinition> GetAllIndexers()
	//{
	//	return (referentType.definition as TypeDefinitionBase ?? unknownType).GetAllIndexers();
	//}
	
	//public override string GetDelegateInfoText()
	//{
	//	return (referentType.definition as TypeDefinitionBase ?? unknownType).GetDelegateInfoText();
	//}
	
	public override SymbolDefinition GetGenericSymbol()
	{
		return (referentType.definition.GetGenericSymbol() as TypeDefinitionBase ?? unknownType).MakePointerType();
	}
	
	//public override SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
	//{
	//	return referentType.definition.GetIndexer(argumentTypes);
	//}
	
	public override List<ParameterDefinition> GetParameters()
	{
		return referentType.definition.GetParameters();
	}
	
	public override Type GetRuntimeType()
	{
		return referentType.definition.GetRuntimeType().MakePointerType();
	}
	
	public override string GetTooltipText(bool fullText = true)
	{
		return referentType.definition.GetTooltipText(fullText);
	}
	
	public override TypeDefinitionBase GetTypeArgument(int index)
	{
		return referentType.definition.GetTypeArgument(index);
	}
	
	public override List<TypeParameterDefinition> GetTypeParameters()
	{
		return referentType.definition.GetTypeParameters();
	}
	
	public override List<TypeReference> Interfaces()
	{
		return (referentType.definition as TypeDefinitionBase ?? unknownType).Interfaces();
	}
	
	public override bool IsAccessible(AccessLevelMask accessLevelMask)
	{
		return referentType.definition.IsAccessible(accessLevelMask);
	}
	
	public override bool IsSameType(TypeDefinitionBase type)
	{
		var asPointer = type as PointerTypeDefinition;
		if (asPointer == null)
			return false;
		return referentType.definition.IsSameType(asPointer.referentType.definition as TypeDefinitionBase);
	}
	
	public override SymbolDefinition Rebind()
	{
		var rebound = (referentType.definition.Rebind() ?? referentType.definition) as TypeDefinitionBase;
		return rebound.MakePointerType();
	}
	
	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		referentType.definition.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
	}
	
	//public override string RankString()
	//{
	//	return (referentType.definition as TypeDefinitionBase ?? unknownType).RankString();
	//}
	
	public override SymbolDefinition ResolveMethodOverloads(ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		return referentType.definition.ResolveMethodOverloads(argumentListNode, typeArgs, scope, invokedLeaf);
	}
	
	public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
	{
		return referentType.definition.SubstituteTypeParameters(context).MakePointerType();
	}
	
	public override string ToString()
	{
		return referentType.definition.ToString() + "*";
	}
	
	public override void XMLDocsID(StringBuilder sb, List<SymbolDefinition> parentTypesTypeParams, List<TypeParameterDefinition> methodTypeParams)
	{
		referentType.definition.XMLDocsID(sb, parentTypesTypeParams, methodTypeParams);
	}
	
	public override TypeDefinitionBase TypeOfTypeParameter(TypeParameterDefinition tp)
	{
		return referentType.definition.TypeOfTypeParameter(tp);
	}
	
	public override SymbolDefinition TypeOf()
	{
		return referentType.definition.TypeOf();
	}
	
	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		referentType.definition.GetMembersCompletionData(data, flags, mask, context);
	}
}

public class RefTypeDefinition : TypeDefinitionBase
{
	public readonly TypeReference referencedType;
	
	public RefTypeDefinition(TypeDefinitionBase referencedType)
	{
		kind = referencedType.kind;
		this.referencedType = TypeReference.To(referencedType);
		name = referencedType.GetName();
	}
	
	public override CharSpan GetName()
	{
		return "ref " + referencedType.definition.GetName();
	}
	
	public override TypeDefinitionBase BaseType()
	{
		return (referencedType.definition as TypeDefinitionBase ?? unknownType).BaseType();
	}
	
	internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
	{
		return (referencedType.definition as TypeDefinitionBase ?? unknownType).BindTypeArgument(typeArgument, argumentType);
	}
	
	public override bool CanConvertTo(TypeDefinitionBase otherType)
	{
		return (referencedType.definition as TypeDefinitionBase ?? unknownType).CanConvertTo(otherType);
	}
	
	public override string CompletionDisplayString(string styledName)
	{
		return referencedType.definition.CompletionDisplayString(styledName);
	}
	
	public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
	{
		return (referencedType.definition as TypeDefinitionBase ?? unknownType).ConvertTo(otherType);
	}
	
	public override bool DerivesFrom(TypeDefinitionBase otherType)
	{
		return (referencedType.definition as TypeDefinitionBase ?? unknownType).DerivesFrom(otherType);
	}
	
	public override bool DerivesFromRef(ref TypeDefinitionBase otherType)
	{
		return (referencedType.definition as TypeDefinitionBase ?? unknownType).DerivesFromRef(ref otherType);
	}
	
	public override SymbolDefinition FindName(CharSpan memberName, int numTypeParameters, bool asTypeOnly)
	{
		return referencedType.definition.FindName(memberName, numTypeParameters, asTypeOnly);
	}
	
	public override List<SymbolDefinition> GetAllIndexers()
	{
		return (referencedType.definition as TypeDefinitionBase ?? unknownType).GetAllIndexers();
	}
	
	public override string GetDelegateInfoText()
	{
		return (referencedType.definition as TypeDefinitionBase ?? unknownType).GetDelegateInfoText();
	}
	
	public override SymbolDefinition GetGenericSymbol()
	{
		return referencedType.definition.GetGenericSymbol();
	}
	
	public override SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
	{
		return referencedType.definition.GetIndexer(argumentTypes);
	}
	
	public override List<ParameterDefinition> GetParameters()
	{
		return referencedType.definition.GetParameters();
	}
	
	public override Type GetRuntimeType()
	{
		return referencedType.definition.GetRuntimeType();
	}
	
	public override string GetTooltipText(bool fullText = true)
	{
		return referencedType.definition.GetTooltipText(fullText);
	}
	
	public override TypeDefinitionBase GetTypeArgument(int index)
	{
		return referencedType.definition.GetTypeArgument(index);
	}
	
	public override List<TypeParameterDefinition> GetTypeParameters()
	{
		return referencedType.definition.GetTypeParameters();
	}
	
	public override List<TypeReference> Interfaces()
	{
		return (referencedType.definition as TypeDefinitionBase ?? unknownType).Interfaces();
	}
	
	public override bool IsAccessible(AccessLevelMask accessLevelMask)
	{
		return referencedType.definition.IsAccessible(accessLevelMask);
	}
	
	public override bool IsSameType(TypeDefinitionBase type)
	{
		return referencedType.definition.IsSameType(type);
	}
	
	public override SymbolDefinition Rebind()
	{
		var rebound = (referencedType.definition.Rebind() ?? referencedType.definition) as TypeDefinitionBase;
		return rebound.MakeRefType();
	}
	
	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		referencedType.definition.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
	}
	
	public override string RankString()
	{
		return (referencedType.definition as TypeDefinitionBase ?? unknownType).RankString();
	}
	
	public override SymbolDefinition ResolveMethodOverloads(ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		return referencedType.definition.ResolveMethodOverloads(argumentListNode, typeArgs, scope, invokedLeaf);
	}
	
	public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
	{
		return referencedType.definition.SubstituteTypeParameters(context);
	}
	
	public override string ToString()
	{
		return referencedType.definition.ToString();
	}
	
	public override void XMLDocsID(StringBuilder sb, List<SymbolDefinition> parentTypesTypeParams, List<TypeParameterDefinition> methodTypeParams)
	{
		referencedType.definition.XMLDocsID(sb, parentTypesTypeParams, methodTypeParams);
	}
	
	public override TypeDefinitionBase TypeOfTypeParameter(TypeParameterDefinition tp)
	{
		return referencedType.definition.TypeOfTypeParameter(tp);
	}
	
	public override SymbolDefinition TypeOf()
	{
		return referencedType.definition.TypeOf();
	}
	
	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		referencedType.definition.GetMembersCompletionData(data, flags, mask, context);
	}
}

public class ArrayTypeDefinition : TypeDefinition
{
	public readonly TypeReference elementType;
	public readonly int rank;

	private List<TypeReference> arrayGenericInterfaces;

	public ArrayTypeDefinition(TypeDefinitionBase elementType, int rank)
	{
		kind = SymbolKind.Class;
		this.elementType = TypeReference.To(elementType);
		this.rank = rank;
		name = elementType.GetName();// + RankString();
	}

	public override CharSpan GetName()
	{
		return name + RankString();
	}

	public override TypeDefinitionBase BaseType()
	{
		if (arrayGenericInterfaces == null && rank == 1)
			Interfaces();
		return builtInTypes_Array;
	}

	public override List<TypeReference> Interfaces()
	{
		if (arrayGenericInterfaces == null && rank == 1)
		{
			arrayGenericInterfaces = new List<TypeReference> {
				TypeReference.To(typeof(IEnumerable<>)),
				TypeReference.To(typeof(IList<>)),
				TypeReference.To(typeof(ICollection<>)),
				TypeReference.To(typeof(IReadOnlyList<>)),
			};

			var typeArguments = new []{ elementType };
			for (var i = 0; i < arrayGenericInterfaces.Count; ++i)
			{
				var genericInterface = arrayGenericInterfaces[i].definition as TypeDefinition;
				genericInterface = genericInterface.ConstructType(typeArguments);
				arrayGenericInterfaces[i] = TypeReference.To(genericInterface);
			}
		}
		interfaces = arrayGenericInterfaces ?? base.Interfaces();
		return interfaces;
	}

	public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
	{
		var constructedElement = elementType.definition.SubstituteTypeParameters(context);
		if (constructedElement != elementType.definition)
			return constructedElement.MakeArrayType(rank);

		return base.SubstituteTypeParameters(context);
	}

	private readonly string[] cachedRankStrings = {"[]", "[,]", "[,,]", "[,,,]", "[,,,,]", "[,,,,,]", "[,,,,,,]"};
	public override string RankString()
	{
		if (rank < 8)
			return cachedRankStrings[rank - 1];
		return "[" + new string(',', rank - 1) + "]";
	}

	public override SymbolDefinition FindName(CharSpan symbolName, int numTypeParameters, bool asTypeOnly)
	{
		//symbolName.DecodeId();
		
		var result = base.FindName(symbolName, numTypeParameters, asTypeOnly);
//		if (result == null && BaseType() != null)
//		{
//			//	Debug.Log("Symbol lookup '" + symbolName +"' in base " + baseType.definition);
//			result = BaseType().FindName(symbolName, numTypeParameters, asTypeOnly);
//		}
		return result;
	}

	public override string GetTooltipText(bool fullText = true)
	{
//		if (tooltipText != null)
//			return tooltipText;

		if (elementType == null || elementType.definition == null)
			return "array of unknown type";

		if (parentSymbol != null && !string.IsNullOrEmpty(parentSymbol.GetName()))
			tooltipText = parentSymbol.GetName() + "." + elementType.definition.GetName() + RankString();
		else
			tooltipText = elementType.definition.GetName() + RankString();

		if (fullText)
		{
			var xmlDocs = GetXmlDocs();
			if (!string.IsNullOrEmpty(xmlDocs))
			{
				tooltipText += "\n\n" + xmlDocs;
			}
		}

		return tooltipText;
	}

	public override bool CanConvertTo(TypeDefinitionBase otherType)
	{
		if (this == otherType)
			return true;

		var asArrayType = otherType as ArrayTypeDefinition;
		if (asArrayType != null)
		{
			if (rank != asArrayType.rank)
				return false;
			return (elementType.definition as TypeDefinitionBase ?? unknownType).CanConvertTo(asArrayType.elementType.definition as TypeDefinitionBase ?? unknownType);
		}

		if (rank == 1 && (otherType.kind == SymbolKind.Interface || otherType.kind == SymbolKind.TypeParameter))
		{
			var genericInterfaces = Interfaces();
			for (var i = 0; i < genericInterfaces.Count; ++i)
			{
				var type = genericInterfaces[i].definition as TypeDefinitionBase;
				if (type != null && type.CanConvertTo(otherType))
					return true;
			}
		}

		return base.CanConvertTo(otherType);
	}

	public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
	{
		if (otherType == null)
			return null;

		if (this == otherType || otherType is TypeParameterDefinition)
			return this;

		var asArrayType = otherType as ArrayTypeDefinition;
		if (asArrayType != null)
		{
			if (rank != asArrayType.rank)
				return null;

			var convertedElementType = (elementType.definition as TypeDefinitionBase ?? unknownType).ConvertTo(asArrayType.elementType.definition as TypeDefinitionBase ?? unknownType);
			if (convertedElementType == null)
				return null;

			if (convertedElementType == elementType.definition)
				return this;

			return convertedElementType.MakeArrayType(rank);
		}

		if (rank == 1 && otherType.kind == SymbolKind.Interface)
		{
			var genericInterfaces = Interfaces();
			for (var i = 0; i < genericInterfaces.Count; ++i)
			{
				var interfaceType = genericInterfaces[i].definition as TypeDefinitionBase;
				var constructedInterface = interfaceType.ConvertTo(otherType);
				if (constructedInterface != null)
					return constructedInterface;
			}
		}

		return base.ConvertTo(otherType);
	}

	internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
	{
		var argumentAsArray = argumentType as ArrayTypeDefinition;
		if (argumentAsArray != null && argumentAsArray.rank == rank)
		{
			var boundElementType = (elementType.definition as TypeDefinitionBase ?? unknownType).BindTypeArgument(typeArgument, argumentAsArray.elementType.definition as TypeDefinitionBase);
			if (boundElementType != null)
				return boundElementType;
		}
		return base.BindTypeArgument(typeArgument, argumentType);
	}
	
	public override void XMLDocsID(StringBuilder sb, List<SymbolDefinition> parentTypesTypeParams, List<TypeParameterDefinition> methodTypeParams)
	{
		var elementTypeDef = elementType.definition;
		elementTypeDef.XMLDocsID(sb, parentTypesTypeParams, methodTypeParams);

		if (rank == 1)
		{
			sb.Append("[]");
		}
		else
		{
			sb.Append("[0:");
			for (var j = 1; j < rank; ++j)
				sb.Append(",0:");
			sb.Append("]");
		}
	}
}

internal class StringBuilders
{
	private static StringBuilders instance = new StringBuilders();
	
	public static StringBuilder Alloc()
	{
		return instance._Alloc();
	}
	
	public static void Release(StringBuilder sb)
	{
		instance._Release(sb);
	}
	
	private Stack<StringBuilder> pool = new Stack<StringBuilder>(16);
	
	private StringBuilder _Alloc()
	{
		if (pool.Count == 0)
			return new StringBuilder();
		else
			return pool.Pop();
	}
	
	private void _Release(StringBuilder sb)
	{
		sb.Length = 0;
		pool.Push(sb);
	}
}

public class TypeDefinition : TypeDefinitionBase
{
	protected TypeReference baseType;
	protected List<TypeReference> interfaces;
	
	public List<TypeParameterDefinition> typeParameters;
	//public TypeReference[] tempTypeArguments;

	//public new SymbolDefinition parentSymbol
	//{
	//	get { return base.parentSymbol; }
	//	set {
	//		if (parentSymbol != null && (parentSymbol.kind == SymbolKind.Class || parentSymbol.kind == SymbolKind.Struct))
	//			Debug.Log("Setting parent " + value + " of " + this);
	//		base.parentSymbol = value;
	//	}
	//}

	private static List<string> reboundKeysStack = new List<string>();
	private static List<ConstructedTypeDefinition> reboundValuesStack = new List<ConstructedTypeDefinition>();

	//protected bool rebinding;
	private TypeDefinition rebindingResult;
	public override SymbolDefinition Rebind()
	{
		if (rebindingResult != null)
			return rebindingResult;
		
		rebindingResult = this;
		
		//Debug.Log("Will rebind TypeDefinition " + FullReflectionName);
		rebindingResult = base.Rebind() as TypeDefinition;
		if (rebindingResult == null)
		{
			#if SI3_WARNINGS
			Debug.LogWarning("Couldn't rebind TypeDefinition " + FullReflectionName);
			#endif
			rebindingResult = null;
			return this;
		}
		if (rebindingResult == this)
		{
			rebindingResult = null;
			return this;
		}
		
		//if (rebindingResult.typeParameters != null)
		//{
		//	for (var i = rebindingResult.typeParameters.Count; i --> 0; )
		//		rebindingResult.typeParameters[i] = rebindingResult.typeParameters[i].Rebind() as TypeParameterDefinition;
		//}
		
		//if (constructedTypes != null && constructedTypes.Count > 0)
		//{
		//	var baseIndex = reboundKeysStack.Count;
		//	foreach (var kv in constructedTypes)
		//	{
		//		var rebound = kv.Value.Rebind() as ConstructedTypeDefinition;
		//		if (rebound != null && rebound != kv.Value)
		//		{
		//			reboundKeysStack.Add(kv.Key);
		//			reboundValuesStack.Add(rebound);
		//		}
		//	}
		//	if (reboundKeysStack.Count > baseIndex)
		//	{
		//		for (var i = baseIndex; i < reboundKeysStack.Count; ++i)
		//		{
		//			rebindingResult.constructedTypes[reboundKeysStack[i]] = reboundValuesStack[i];
		//		}
		//		reboundKeysStack.RemoveRange(baseIndex, reboundKeysStack.Count - baseIndex);
		//		reboundValuesStack.RemoveRange(baseIndex, reboundValuesStack.Count - baseIndex);
		//	}
		//}

		var result = rebindingResult;
		rebindingResult = null;
		return result;
	}

	private Dictionary<string, ConstructedTypeDefinition> constructedTypes;
	public ConstructedTypeDefinition ConstructType(TypeReference[] typeArgs, TypeDefinition parentType = null)
	{
		var thisAsConstructedType = this as ConstructedTypeDefinition;
		if (thisAsConstructedType != null)
		{
			var result3 = thisAsConstructedType.genericTypeDefinition.ConstructType(typeArgs, parentType);
			return result3;
		}

		if (typeArgs != null && typeArgs.Length == 0)
			typeArgs = null;

		var sb = StringBuilders.Alloc();

		var delimiter = string.Empty;
		sb.Length = 0;
		var constructedParentType = parentType as ConstructedTypeDefinition;
		if (constructedParentType != null)
		{
			var parentTypeArgs = constructedParentType.typeArguments;
			if (parentTypeArgs != null)
			{
				foreach (var arg in parentTypeArgs)
				{
					sb.Append(delimiter);
					sb.Append(arg.ToString());
					delimiter = ", ";
				}
			}
		}
		if (typeArgs != null)
		{
			foreach (var arg in typeArgs)
			{
				sb.Append(delimiter);
				sb.Append(arg.ToString());
				delimiter = ", ";
			}
		}
		var sig = sb.ToString();
		
		StringBuilders.Release(sb);

		if (constructedTypes == null)
			constructedTypes = new Dictionary<string, ConstructedTypeDefinition>();
 
		ConstructedTypeDefinition result;
		if (constructedTypes.TryGetValue(sig, out result))
		{
			if (result.IsValid())
			{
				var allEqual = true;
				if (result.typeArguments == null)
				{
					allEqual = typeArgs == null;
				}
				else
				{
					if (typeArgs == null || result.typeArguments.Length != typeArgs.Length)
					{
						allEqual = false;
					}
					else
					{
						for (var i = result.typeArguments.Length; i --> 0; )
						{
							var ta = typeArgs[i];
							var rta = result.typeArguments[i];
							if (/*ta != rta &&*/ ta.definition != rta.definition)
							{
								allEqual = false;
								break;
							}
						}
					}
				}
				if (allEqual)
				{
					result.defaultConstructor = null;
					return result;
				}
			}
		}

		var result2 = new ConstructedTypeDefinition(this, typeArgs);
		if (constructedParentType != null)
			result2.parentSymbol = constructedParentType;
		constructedTypes[sig] = result2;
		return result2;
	}

	public override SymbolDefinition TypeOf()
	{
		return this;
	}
	
	public override void InvalidateBaseType()
	{
		baseType = null;
		interfaces = null;
		++ParseTree.resolverVersion;
		if (ParseTree.resolverVersion == 0)
			++ParseTree.resolverVersion;
	}

	public override List<TypeReference> Interfaces()
	{
		if (interfaces == null)
			BaseType();
		return interfaces;
	}
	
	protected bool resolvingBaseType = false;
	public override TypeDefinitionBase BaseType()
	{
		if (resolvingBaseType || kind == SymbolKind.Error)
			return null;
		resolvingBaseType = true;

		var rebuildInterfaces = interfaces == null;
		
		if (baseType != null && (baseType.definition == null || !baseType.definition.IsValid()) ||
			interfaces != null && interfaces.Exists(delegateToInvalidSymbolReference))
		{
			baseType = null;
			rebuildInterfaces = true;
		}

		if (baseType == null && rebuildInterfaces)
		{
			interfaces = interfaces ?? new List<TypeReference>();
			interfaces.Clear();
			
			for (var currentDeclaration = declarations; currentDeclaration != null; currentDeclaration = currentDeclaration.next)
			{
				var baseNode = (ParseTree.Node) currentDeclaration.parseTreeNode.FindChildByName(
					currentDeclaration.kind == SymbolKind.Class ? "classBase" :
					currentDeclaration.kind == SymbolKind.Struct ? "structInterfaces" :
					"interfaceBase");
				var interfaceListNode = baseNode != null ? baseNode.NodeAt(1) : null;
				if (interfaceListNode == null || interfaceListNode.numValidNodes == 0)
					continue;
					
				if (kind == SymbolKind.Class)
				{
					var typeRef = TypeReference.To(interfaceListNode.ChildAt(0));
					var kind = typeRef.definition.kind;
					if (kind == SymbolKind.Interface)
					{
						interfaces.Add(typeRef);
					}
					else if (kind == SymbolKind.Class || kind == SymbolKind.TypeParameter)
					{
						baseType = typeRef;
					}
					else
					{
						var typeNameNode = interfaceListNode.NodeAt(0);
						if (typeNameNode != null)
						{
							var namespaceOrTypeNode = typeNameNode.NodeAt(0);
							if (namespaceOrTypeNode != null)
							{
								var typeOrGenericNode = namespaceOrTypeNode.NodeAt(-1);
								if (typeOrGenericNode != null)
								{
									var idLeaf = typeOrGenericNode.LeafAt(0);
									if (idLeaf != null && idLeaf.syntaxError == null && idLeaf.semanticError == null)
										idLeaf.semanticError = "invalid base type";
								}
							}
						}
					}
	
					for (var i = 2; i < interfaceListNode.numValidNodes; i += 2)
						interfaces.Add(TypeReference.To(interfaceListNode.ChildAt(i)));
				}
				else if (kind == SymbolKind.Struct || kind == SymbolKind.Interface)
				{
					for (var i = 0; i < interfaceListNode.numValidNodes; i += 2)
						interfaces.Add(TypeReference.To(interfaceListNode.ChildAt(i)));
				}
				else
				{
					break;
				}
			}
			//Debug.Log("BaseType() of " + this + " is " + (baseType != null ? baseType.definition.ToString() : "null"));
		}

		if (baseType == null)
		{
			if (kind == SymbolKind.Struct)
			{
				baseType = TypeReference.To(typeof(ValueType));
			}
			else if (kind == SymbolKind.Interface)
			{
				baseType = null;//TypeReference.To(typeof(object));
			}
			else if (kind == SymbolKind.Enum)
			{
				baseType = TypeReference.To(typeof(Enum));
			}
			else if (kind == SymbolKind.Delegate)
			{
				baseType = TypeReference.To(typeof(MulticastDelegate));
			}
		}
		
		var result = baseType != null ? baseType.definition as TypeDefinitionBase : base.BaseType();
		if (result == this)
		{
			baseType = TypeReference.To(circularBaseType);
			result = circularBaseType;
		}
		resolvingBaseType = false;
		return result;
	}

	public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
	{
		if (otherType == null)
			return null;

		if (this == otherType || otherType is TypeParameterDefinition)
			return this;

		if (otherType == builtInTypes_object)
			return otherType;

		if (otherType.GetGenericSymbol() == builtInTypes_Nullable)
			return CanConvertTo(otherType.GetTypeArgument(0)) ? otherType : null;

		if (this == builtInTypes_int && (
			otherType == builtInTypes_long ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_uint && (
			otherType == builtInTypes_long ||
			otherType == builtInTypes_ulong ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_byte && (
			otherType == builtInTypes_short ||
			otherType == builtInTypes_ushort ||
			otherType == builtInTypes_int ||
			otherType == builtInTypes_uint ||
			otherType == builtInTypes_long ||
			otherType == builtInTypes_ulong ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_sbyte && (
			otherType == builtInTypes_short ||
			otherType == builtInTypes_int ||
			otherType == builtInTypes_long ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_short && (
			otherType == builtInTypes_int ||
			otherType == builtInTypes_long ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_ushort && (
			otherType == builtInTypes_int ||
			otherType == builtInTypes_uint ||
			otherType == builtInTypes_long ||
			otherType == builtInTypes_ulong ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		if ((this == builtInTypes_long || this == builtInTypes_ulong) &&
			(otherType == builtInTypes_float || otherType == builtInTypes_double))
			return otherType;
		if (this == builtInTypes_float &&
			otherType == builtInTypes_double)
			return otherType;
		if (this == builtInTypes_char && (
			otherType == builtInTypes_ushort ||
			otherType == builtInTypes_int ||
			otherType == builtInTypes_uint ||
			otherType == builtInTypes_long ||
			otherType == builtInTypes_ulong ||
			otherType == builtInTypes_float ||
			otherType == builtInTypes_double))
			return otherType;
		
		//var otherTypeAsConstructed = otherType as ConstructedTypeDefinition;
		//if (otherTypeAsConstructed != null)
		//	otherType = otherTypeAsConstructed.genericTypeDefinition;

		if (this == otherType.GetGenericSymbol())
			return otherType;

		if (convertingToBase)
			return null;
		convertingToBase = true;

		var baseTypeDefinition = BaseType();

		if (interfaces != null && (otherType.kind == SymbolKind.Interface || otherType.kind == SymbolKind.TypeParameter))
		{
			for (var i = 0; i < interfaces.Count; ++i)
			{
				var interfaceDefinition = interfaces[i].definition as TypeDefinitionBase;
				if (interfaceDefinition != null)
				{
					var convertedInterface = interfaceDefinition.ConvertTo(otherType);
					if (convertedInterface != null)
					{
						convertingToBase = false;
						return convertedInterface;
					}
				}
			}
		}

		if (baseTypeDefinition != null)
		{
			var convertedBase = baseTypeDefinition.ConvertTo(otherType);
			convertingToBase = false;
			return convertedBase;
		}

		convertingToBase = false;
		return null;
	}
	
	private bool checkingDerivesFromBase;
	public override bool DerivesFromRef(ref TypeDefinitionBase otherType)
	{
		if (otherType == null)
			return false;
		
		var otherTypeAsConstructed = otherType as ConstructedTypeDefinition;
		if (otherTypeAsConstructed != null)
			otherType = otherTypeAsConstructed.genericTypeDefinition;

		if (this == otherType)
			return true;

		if (interfaces == null)
			BaseType();
		
		if (checkingDerivesFromBase)
			return false;
		checkingDerivesFromBase = true;
		
		if (interfaces != null)
			for (var i = 0; i < interfaces.Count; ++i)
			{
				var typeDefinition = interfaces[i].definition as TypeDefinitionBase;
				if (typeDefinition != null && typeDefinition.DerivesFromRef(ref otherType))
				{
					checkingDerivesFromBase = false;
					return true;
				}
			}

		if (BaseType() != null)
		{
			var result = BaseType().DerivesFromRef(ref otherType);
			checkingDerivesFromBase = false;
			return result;
		}
		
		checkingDerivesFromBase = false;
		return false;
	}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind != SymbolKind.TypeParameter)
			return base.AddDeclaration(symbol);

		var symbolName = symbol.ReflectionName;// symbol.Name;
		if (typeParameters == null)
			typeParameters = new List<TypeParameterDefinition>();
		var definition = typeParameters.FirstByName(symbolName);
		if (definition == null)
		{
			definition = (TypeParameterDefinition) Create(symbol);
			definition.parentSymbol = this;
			typeParameters.Add(definition);
		}

		symbol.definition = definition;

		var nameNode = symbol.NameNode();
		if (nameNode != null)
		{
			var leaf = nameNode as ParseTree.Leaf;
			if (leaf != null)
				leaf.SetDeclaredSymbol(definition);
			else
			{
				// TODO: Remove this block?
				var lastLeaf = ((ParseTree.Node) nameNode).GetLastLeaf();
				if (lastLeaf != null)
				{
					if (lastLeaf.parent.RuleName == "typeParameterList")
						lastLeaf = lastLeaf.parent.parent.LeafAt(0);
					lastLeaf.SetDeclaredSymbol(definition);
				}
			}
		}
		
		//// this.ReflectionName has changed
		//parentSymbol.members.Remove(this);
		//parentSymbol.members[ReflectionName] = this;

		return definition;
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.TypeParameter && typeParameters != null)
		{
			if (typeParameters.Remove(symbol.definition as TypeParameterDefinition))
			{
				//// this.ReflectionName has changed
				//parentSymbol.members.Remove(this);
				//parentSymbol.members[ReflectionName] = this;
			}
		}

		base.RemoveDeclaration(symbol);
	}

	public override SymbolDefinition FindName(CharSpan memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName.DecodeId();
		
		if (numTypeParameters == 0 && typeParameters != null)
		{
			for (var i = typeParameters.Count; i --> 0; )
				if (typeParameters[i].name == memberName)
					return typeParameters[i];
		}
		
		var member = base.FindName(memberName, numTypeParameters, asTypeOnly);
		return member;
	}

	public override List<TypeParameterDefinition> GetTypeParameters()
	{
		return typeParameters;
	}

	public override string GetTooltipText(bool fullText = true)
	{
		if (kind == SymbolKind.Delegate)
			return base.GetTooltipText(fullText);

	//	if (tooltipText != null)
	//		return tooltipText;

		var parentSD = parentSymbol;
		if (parentSD != null && !string.IsNullOrEmpty(parentSD.GetName()))
			tooltipText = kind.ToString().ToLowerInvariant() + " " + parentSD.GetName() + "." + name;
		else
			tooltipText = kind.ToString().ToLowerInvariant() + " " + name;

		if (typeParameters != null)
		{
			tooltipText += "<" + TypeOfTypeParameter(typeParameters[0]).GetName();
			for (var i = 1; i < typeParameters.Count; ++i)
				tooltipText += ", " + TypeOfTypeParameter(typeParameters[i]).GetName();
			tooltipText += ">";
		}

		if (fullText)
		{
			var xmlDocs = GetXmlDocs();
			if (!string.IsNullOrEmpty(xmlDocs))
			{
				tooltipText += "\n\n" + xmlDocs;
			}
			
			//tooltipText += "\n\n" + FullName;
		}

		return tooltipText;
	}

	public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
	{
		if (typeParameters == null)
		{
			if (parentSymbol == null || context.GetGenericSymbol() != parentSymbol.GetGenericSymbol())
				return base.SubstituteTypeParameters(context);

			var constructedParent = context as ConstructedTypeDefinition;
			if (constructedParent == null)
				return base.SubstituteTypeParameters(context);

			var result2 = constructedParent.GetConstructedMember(GetGenericSymbol()) as TypeDefinitionBase;
			if (result2 != null)
				return result2;

			return base.SubstituteTypeParameters(context);
		}
		
		var constructType = false;
		var typeArguments = TypeReference.AllocArray(typeParameters.Count);
		for (var i = 0; i < typeArguments.Length; ++i)
		{
			var original = typeParameters[i];
			if (original == null)
			{
				typeArguments[i] = TypeReference.To(typeParameters[i]);
				continue;
			}
			var substitute = original.SubstituteTypeParameters(context);
			if (substitute != original)
			{
				typeArguments[i] = TypeReference.To(substitute);
				constructType = true;
			}
			else
			{
				typeArguments[i] = TypeReference.To(typeParameters[i]);
			}
		}
		if (!constructType)
		{
			TypeReference.ReleaseArray(typeArguments);
			return this;
		}

		var result = ConstructType(typeArguments);
		TypeReference.ReleaseArray(typeArguments);
		return result;
	}

	internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
	{
		if (NumTypeParameters == 0)
			return base.BindTypeArgument(typeArgument, argumentType);
		
		if (argumentType.kind == SymbolKind.LambdaExpression)
			return argumentType.BindTypeArgument(typeArgument, TypeOf() as TypeDefinitionBase);
		
		TypeDefinitionBase convertedArgument = this;
		if (!argumentType.DerivesFromRef(ref convertedArgument))
			return base.BindTypeArgument(typeArgument, argumentType);
		
		var argumentAsConstructedType = convertedArgument as ConstructedTypeDefinition;
		if (argumentAsConstructedType != null && GetGenericSymbol() == argumentAsConstructedType.GetGenericSymbol())
		{
			TypeDefinitionBase inferedType = null;
			for (int i = 0; i < NumTypeParameters; ++i)
			{
				var fromConstructedType = argumentAsConstructedType.typeArguments[i].definition as TypeDefinitionBase;
				if (fromConstructedType != null)
				{
					var boundTypeArgument = typeParameters[i].BindTypeArgument(typeArgument, fromConstructedType);
					if (boundTypeArgument != null)
					{
						if (inferedType == null || inferedType.CanConvertTo(boundTypeArgument))
							inferedType = boundTypeArgument;
						else if (!boundTypeArgument.CanConvertTo(inferedType))
							return null;
					}
				}
			}
			
			if (inferedType != null)
				return inferedType;
		}
		return base.BindTypeArgument(typeArgument, argumentType);
	}
	
	//public override bool IsGeneric
	//{
	//	get
	//	{
	//		return typeParameters != null;
	//	}
	//}
}

//public struct ArgumentsStackEntry
//{
//	public Modifiers modifiers;
//	public TypeDefinitionBase type;
//	public SymbolDefinition resolvedSymbol;
//	public string namedArgument;
//	public ParseTree.Node node;
//}

public class MethodGroupExpressionType : TypeDefinitionBase
{
}

public class MethodGroupDefinition : SymbolDefinition
{
	public static List<Modifiers> modifiersStack = new List<Modifiers>(256);
	public static List<TypeDefinitionBase> argumentTypesStack = new List<TypeDefinitionBase>(256);
	public static List<SymbolDefinition> resolvedArgumentsStack = new List<SymbolDefinition>(256);
	public static List<string> namedArgumentsStack = new List<string>(256);
	public static List<ParseTree.Node> argumentNodesStack = new List<ParseTree.Node>(256);
	
	public static MethodGroupExpressionType MGType = new MethodGroupExpressionType { kind = SymbolKind.MethodGroup, name = "<method group>" };

	public static readonly MethodDefinition ambiguousMethodOverload = new MethodDefinition { kind = SymbolKind.Error, name = "ambiguous method overload" };
	public static readonly MethodDefinition unresolvedMethodOverload = new MethodDefinition { kind = SymbolKind.Error, name = "unresolved method overload" };
	public static readonly MethodDefinition invalidUseOfNamedArguments = new MethodDefinition { kind = SymbolKind.Error, name = "invalid use of named arguments" };

	public readonly List<MethodDefinition> methods = new List<MethodDefinition>();

	public virtual void AddMethod(MethodDefinition method)
	{
		for (var i = methods.Count; i --> 0; )
			if (!methods[i].IsValid())
				methods.RemoveAt(i);
		
		if (method.declarations != null)
		{
			var d = method.declarations;
			for (var i = methods.Count; i --> 0;)
			{
				if (methods[i].ContainsDeclaration(d))
				{
					methods.RemoveAt(i);
					break;
				}
			}
		}
		
		methods.Add(method);
		method.parentSymbol = this;
	}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		return base.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		for (var i = methods.Count; i --> 0; )
		{
			if (methods[i].ContainsDeclaration(symbol))
			{
				methods.RemoveAt(i);
				break;
			}
		}
	}

	public override SymbolDefinition TypeOf()
	{
		if (kind == SymbolKind.Constructor)
			return parentSymbol;
		else
			return MGType;
	}

	public SymbolDefinition ResolveParameterName(ParseTree.Leaf leaf)
	{
		var methods = this.methods;
		if (methods.Count == 0)
		{
			var genericSymbol = GetGenericSymbol() as MethodGroupDefinition;
			if (genericSymbol != null)
				methods = genericSymbol.methods;
		}

		var leafText = DecodeId(leaf.token.text);
		for (var i = methods.Count; i --> 0; )
		{
			var m = methods[i];
			var p = m.GetParameters();
			for (var j = p.Count; j --> 0; )
			{
				var pd = p[j];
				if (pd.name == leafText)
					return leaf.resolvedSymbol = pd;
			}
		}
		return leaf.resolvedSymbol = unknownParameterName;
	}
	
	public override SymbolDefinition Rebind()
	{
		if (parentSymbol == null && savedParentSymbol == null)
			return this;
		
		var newParent = (parentSymbol ?? savedParentSymbol).Rebind();
		if (newParent == null)
		{
			#if SI3_WARNINGS
			Debug.LogWarning("Couldn't rebind parent of " + GetTooltipText());
			#endif
			return null;
		}
		
		if (newParent == parentSymbol)
			return this;
		
		SymbolDefinition newSymbol = newParent.FindName(name, -1, false);
#if SI3_WARNINGS
		if (newSymbol == null)
		{
			Debug.LogWarning(GetTooltipText() + " not found in " + newParent.GetTooltipText());
			return null;
		}
#endif
		return newSymbol;
	}
	
	public static int ProcessArgumentListNode(ParseTree.Node argumentListNode, TypeDefinitionBase extendedType)
	{
		var thisOffest = 0;
		var numArguments = argumentListNode == null ? 0 : (argumentListNode.numValidNodes + 1) / 2;
		if (extendedType != null)
		{
			thisOffest = 1;
			++numArguments;
			
			modifiersStack.Add(Modifiers.This);
			argumentTypesStack.Add(extendedType);
			resolvedArgumentsStack.Add(null);//extendedType.GetThisInstance());
			namedArgumentsStack.Add(null);
			argumentNodesStack.Add(null);
		}
		
		for (var i = thisOffest; i < numArguments; ++i)
		{
			var argumentNode = argumentListNode.NodeAt((i - thisOffest) * 2);
			if (argumentNode != null)
			{
				var argumentValueNode = argumentNode.FindChildByName("argumentValue") as ParseTree.Node;
				if (argumentValueNode != null)
				{
					var resolvedArg = ResolveNode(argumentValueNode);
					resolvedArgumentsStack.Add(resolvedArg);
					argumentTypesStack.Add(unknownType);
					modifiersStack.Add(Modifiers.None);
					namedArgumentsStack.Add(null);
					argumentNodesStack.Add(argumentNode);
					
					if (resolvedArg != null)
						argumentTypesStack[argumentTypesStack.Count - 1] = resolvedArg.TypeOf() as TypeDefinitionBase ?? unknownType;
					
					var modifierLeaf = argumentValueNode.LeafAt(0);
					if (modifierLeaf != null)
					{
						if (modifierLeaf.IsLit("ref"))
							modifiersStack[modifiersStack.Count - 1] = Modifiers.Ref;
						else if (modifierLeaf.IsLit("out"))
							modifiersStack[modifiersStack.Count - 1] = Modifiers.Out;
						else if (modifierLeaf.IsLit("in"))
							modifiersStack[modifiersStack.Count - 1] = Modifiers.In;
					}

					var argumentNameNode = argumentNode.NodeAt(0);
					if (argumentNameNode.RuleName == "argumentName")
					{
						var argumentNameLeaf = argumentNameNode.LeafAt(0);
						if (argumentNameLeaf != null && argumentNameLeaf.token.tokenKind == SyntaxToken.Kind.Identifier)
						{
							namedArgumentsStack[namedArgumentsStack.Count - 1] = argumentNameLeaf.token.text;
						}
					}
					
					continue;
				}
			}
			
			numArguments = i;
			break;
		}
		
		return numArguments;
	}
	
	public static MethodDefinition CheckNamedArguments(int numArguments)
	{
		//int baseIndex = namedArgumentsStack.Count - numArguments;
		//for (var i = 0; i < numArguments; ++i)
		//{
		//	if (namedArgumentsStack[baseIndex + i] == null)
		//		continue;
			
		//	for (var j = i; j < numArguments; ++j)
		//	{
		//		var argName = namedArgumentsStack[baseIndex + j];
		//		if (string.IsNullOrEmpty(argName))
		//			return invalidUseOfNamedArguments;
		//		for (var k = j + 1; k < numArguments; ++k)
		//			if (argName == namedArgumentsStack[baseIndex + k])
		//				return invalidUseOfNamedArguments;
		//	}
		//	break;
		//}
		return null;
	}

	public override SymbolDefinition ResolveMethodOverloads(ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		if (invokedLeaf != null && !invokedLeaf.HasErrors() && invokedLeaf.resolvedSymbol is MethodDefinition)
			return invokedLeaf.resolvedSymbol;

		int baseIndex = modifiersStack.Count;
		
		int numArguments = ProcessArgumentListNode(argumentListNode, null);
		var namedArgsError = CheckNamedArguments(numArguments);
		var resolved = namedArgsError ?? ResolveMethodOverloads(numArguments, scope, invokedLeaf);

		for (var i = numArguments; i --> 0; )
		{
			var resolvedArg = resolvedArgumentsStack[i + baseIndex] as MethodDefinition;
			if (resolvedArg == null)
				continue;
			var argNode = argumentNodesStack[i + baseIndex];
			if (argNode == null)
				continue;
			var firstLeaf = argNode.GetFirstLeaf();
			if (firstLeaf == null)
				continue;
			var primaryExprNode = firstLeaf.FindParentByName("primaryExpression");
			if (primaryExprNode == null)
				continue;
			if (!primaryExprNode.FirstNonTrivialParent().IsAncestorOf(argNode))
				continue;
			if (primaryExprNode.numValidNodes > 1)
			{
				var lastPartNode = primaryExprNode.NodeAt(-1);
				if (lastPartNode == null)
					continue;
				firstLeaf = lastPartNode.GetFirstLeaf();
				if (firstLeaf == null)
					continue;
				if (lastPartNode.childIndex > 0)
					firstLeaf = firstLeaf.FindNextLeaf();
				if (firstLeaf == null)
					continue;
			}
			var resolverLeaf = firstLeaf.resolvedSymbol;
			if (resolverLeaf == null || resolverLeaf.kind != SymbolKind.MethodGroup)
				continue;
			firstLeaf.resolvedSymbol = resolvedArg;
		}
		
		modifiersStack.RemoveRange(baseIndex, numArguments);
		argumentTypesStack.RemoveRange(baseIndex, numArguments);
		resolvedArgumentsStack.RemoveRange(baseIndex, numArguments);
		namedArgumentsStack.RemoveRange(baseIndex, numArguments);
		argumentNodesStack.RemoveRange(baseIndex, numArguments);
		
		return resolved;
	}
	
	public static List<MethodDefinition> methodCandidatesStack = new List<MethodDefinition>();

	public virtual int CollectCandidates(int numArguments, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		var self = this;
		if (parentSymbol == null && savedParentSymbol != null)
			self = Rebind() as MethodGroupDefinition ?? self;
		
		return _CollectCandidates(self, numArguments, scope, invokedLeaf);
	}
	
	private static int _CollectCandidates(MethodGroupDefinition self, int numArguments, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		if (self.parentSymbol == null)
			return 0;
		if (self.name == ".cctor")
			return 0;

		var accessLevelMask = AccessLevelMask.Public;
		var parentSymbol = self.parentSymbol;
		while (parentSymbol is MethodDefinition)
			parentSymbol = parentSymbol.parentSymbol;
		var parentType = parentSymbol as TypeDefinitionBase ?? parentSymbol.parentSymbol as TypeDefinitionBase;
		var contextType = scope == null ? null : scope.EnclosingType();
		if (contextType != null && parentType != null)
		{
			if (contextType == parentType)
			{
				accessLevelMask |= AccessLevelMask.Any;
			}
			else
			{
				var parentTypeAssembly = parentType.Assembly;
				if (parentTypeAssembly != null && parentTypeAssembly.InternalsVisibleTo(contextType.Assembly))
					accessLevelMask |= AccessLevelMask.Internal;

				if (parentType.IsSameOrParentOf(contextType))
					accessLevelMask |= AccessLevelMask.Public | AccessLevelMask.Protected | AccessLevelMask.Private;
				else if (contextType.DerivesFrom(parentType))
					accessLevelMask |= AccessLevelMask.Public | AccessLevelMask.Protected;
			}
		}
		
		var baseIndex = methodCandidatesStack.Count;
		for (var i = 0; i < self.methods.Count; ++i)
		{
			var method = self.methods[i];
			if (!method.IsOverride && method.IsAccessible(accessLevelMask) && method.explicitInterfaceImplementation == null &&
				(numArguments == -1 || method.CanCallWith(numArguments, false)) &&
				(method.kind != SymbolKind.Constructor || !method.IsStatic))
			{
				methodCandidatesStack.Add(method);
			}
		}
		var numCandidates = methodCandidatesStack.Count - baseIndex;
		
		var thisAsConstructedMG = self as ConstructedMethodGroupDefinition;
		for (var i = numCandidates; i --> 0; )
		{
			var candidate = methodCandidatesStack[baseIndex + i];
			if (thisAsConstructedMG == null)
			{
				if (invokedLeaf == null)
					continue;

				if (candidate.kind == SymbolKind.Constructor)
				{
					break;
				}
				else
				{
					if (candidate.NumTypeParameters == 0 || numArguments == -1)
						continue;

					candidate = InferMethodTypeArguments(candidate, numArguments, invokedLeaf);
				}

				if (candidate == null)
					methodCandidatesStack.RemoveAt(baseIndex + i);
				else
					methodCandidatesStack[baseIndex + i] = candidate;
			}
			else
			{
				// TODO: Verify this!!!
				var constructedMethod = candidate;//.ConstructMethod(thisAsConstructedMG.typeArguments);
				if (constructedMethod != null)
					methodCandidatesStack[baseIndex + i] = constructedMethod;
			}
		}
		numCandidates = methodCandidatesStack.Count - baseIndex;
		
		if (numCandidates != 0 && numArguments != -1 || self.name == ".ctor")
			return numCandidates;

		if (parentType == null)
			return numCandidates;

		var baseType = (TypeDefinitionBase) parentType;
		while ((baseType = baseType.BaseType()) != null)
		{
			var baseSymbol = baseType.FindName(self.name, 0, false) as MethodGroupDefinition;
			if (baseSymbol != null)
				return numCandidates + baseSymbol.CollectCandidates(numArguments, scope, invokedLeaf);
		}
		return numCandidates;
	}

	private static Stack<List<int>> rangeListsPool = new Stack<List<int>>();

	private static List<int> GenerateRangeList(int to)
	{
		var list = rangeListsPool.Count > 0 ? rangeListsPool.Pop() : new List<int>(to);
		for (var i = 0; i < to; ++i)
			list.Add(i);
		return list;
	}

	private static void ReleaseRangeList(List<int> list)
	{
		list.Clear();
		rangeListsPool.Push(list);
	}

	public static MethodDefinition InferMethodTypeArguments(MethodDefinition method, int numArguments, ParseTree.Leaf invokedLeaf)
	{
		int baseIndex = argumentTypesStack.Count - numArguments;

		var oldResolvedLeaf = invokedLeaf == null ? null : invokedLeaf.resolvedSymbol;
		
		var numTypeParameters = method.NumTypeParameters;
		List<TypeDefinitionBase> typeArgs = CreateTypeList();
		var typeParams = method.typeParameters;
		for (var i = 0; i < typeParams.Count; i++)
		{
			var item = typeParams[i];
			typeArgs.Add(item.SubstituteTypeParameters(method));
		}
		
		//var typeArgsUpper = new TypeDefinitionBase[numTypeParameters];
		//var typeArgsLower = new TypeDefinitionBase[numTypeParameters];
		
		var parameters = method.GetParameters();
		var numParameters = Math.Min(parameters.Count, numArguments);
		
		var openTypeArguments = GenerateRangeList(numTypeParameters);
		
		var stayInLoop = true;
		var bindToLambdaExpressions = false;
		var hasLambdaExpressions = false;
		while (stayInLoop)
		{
			stayInLoop = false;
			for (var i = openTypeArguments.Count; i --> 0; )
			{
				var typeArgIndex = openTypeArguments[i];
				var typeArgument = typeArgs[typeArgIndex];

				for (var j = numParameters; j --> 0; )
				{
					var argumentType = argumentTypesStack[baseIndex + j];
					if (argumentType == null)
						continue;

					if (argumentType.kind == SymbolKind.LambdaExpression || argumentType.kind == SymbolKind.MethodGroup)
					{
						if (!bindToLambdaExpressions)
						{
							hasLambdaExpressions = true;
							continue;
						}
					}
					
					var parameter = parameters[j]; //TODO: Consider expanded params parameter and all arguments
					var parameterType = parameter.TypeOf() as TypeDefinitionBase;

					if (parameterType.GetGenericSymbol() == builtInTypes_Expression_1)
					{
						parameterType = parameterType.GetTypeArgument(0);
					}

					parameterType = parameterType.SubstituteTypeParameters(method);
					
					if (parameterType != null && parameterType.IsValid())
					{
						TypeDefinitionBase boundType = null;
						if (argumentType.kind == SymbolKind.MethodGroup && parameterType.kind == SymbolKind.Delegate)
						{
							var methodGroupNode = argumentNodesStack[baseIndex + j];
							if (methodGroupNode != null)
							{
								var methodGroupLeaf = methodGroupNode.GetLastLeaf();
								if (methodGroupLeaf != null)
								{
									if (methodGroupLeaf.IsLit(">"))
									{
										methodGroupLeaf = methodGroupLeaf.parent.FindPreviousLeaf();
									}
									var resolvedMethod = methodGroupLeaf.resolvedSymbol as MethodDefinition;
									var methodGroup = (resolvedMethod != null ? resolvedMethod.parentSymbol : methodGroupLeaf.resolvedSymbol) as MethodGroupDefinition;
									if (methodGroup != null)
									{
										var matchingMethod = methodGroup.FindMatchingMethod(parameterType);
										if (matchingMethod != null)
										{
											methodGroupLeaf.resolvedSymbol = matchingMethod;

											var returnType = matchingMethod.ReturnType();
											if (returnType != null && returnType.kind != SymbolKind.Error)
											{
												var delegateReturnType = parameterType.TypeOf() as TypeDefinitionBase;
												if (delegateReturnType != null && delegateReturnType.kind != SymbolKind.Error)
													boundType = delegateReturnType.BindTypeArgument(typeArgument, returnType);
											}
										}
									}
								}
							}
						}
						else
						{
							boundType = parameterType.BindTypeArgument(typeArgument, argumentType);
						}

						if (boundType != null && boundType != typeArgument && boundType.kind != SymbolKind.Error)
						{
							typeArgs[typeArgIndex] = boundType;
							openTypeArguments.RemoveAt(i);
							stayInLoop = openTypeArguments.Count > 0;

							if (stayInLoop)
							{
								var newTypeArguments = TypeReference.AllocArray(typeArgs.Count);
								for (var k = typeArgs.Count; k --> 0; )
									newTypeArguments[k] = TypeReference.To(typeArgs[k]);
								var constructedMethod = method.ConstructMethod(newTypeArguments);
								TypeReference.ReleaseArray(newTypeArguments);
								if (constructedMethod != null)
								{
									method = constructedMethod;
									if (invokedLeaf != null)
										invokedLeaf.resolvedSymbol = method;
								}
							}

							//TODO: Should actually use the lower and upper bounds
							break;
						}
					}
				}
			}

			if (!stayInLoop && hasLambdaExpressions)
			{
				stayInLoop = true;
				hasLambdaExpressions = false;
				bindToLambdaExpressions = true;
			}
		}

		var allInfered = openTypeArguments.Count == 0;

		ReleaseRangeList(openTypeArguments);
		
		if (invokedLeaf != null)
			invokedLeaf.resolvedSymbol = oldResolvedLeaf;

		//if (!allInfered)
		//{
		//	ReleaseTypeList(typeArgs);
		//	return null;
		//}

		var typeArgRefs = TypeReference.AllocArray(numTypeParameters);
		for (var i = 0; i < numTypeParameters; ++i)
			typeArgRefs[i] = TypeReference.To(typeArgs[i] ?? builtInTypes_object);

		var finalConstructedMethod = method.ConstructMethod(typeArgRefs);
		TypeReference.ReleaseArray(typeArgRefs);

		if (finalConstructedMethod != null)
			method = finalConstructedMethod;

		ReleaseTypeList(typeArgs);
		return method;
	}

	public virtual MethodDefinition ResolveMethodOverloads(int numArguments, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		var baseIndex = methodCandidatesStack.Count;
		var numCandidates = CollectCandidates(numArguments, scope, invokedLeaf);
		if (numCandidates == 0)
			return unresolvedMethodOverload;

	//	if (candidates.Count == 1)
	//		return candidates[0];
		/*
		ConstructedTypeDefinition genericContext = null;
		if (name == ".ctor")
			genericContext = invokedLeaf.resolvedSymbol as ConstructedTypeDefinition;
		if (genericContext == null)
		{
			genericContext = parentSymbol as ConstructedTypeDefinition;
		}
		*/

		invokedLeaf.semanticError = null;
		
		var result = ResolveMethodOverloads(numArguments, numCandidates, /*genericContext*/invokedLeaf.resolvedSymbol);
		
		if (methodCandidatesStack.Count > baseIndex)
			methodCandidatesStack.RemoveRange(baseIndex, methodCandidatesStack.Count - baseIndex);
		
		return result;
	}
	
	public static MethodDefinition ResolveMethodOverloads(int numArguments, int numCandidates, SymbolDefinition genericContext = null)
	{
		List<TypeDefinitionBase> bestParams = null;
		List<TypeDefinitionBase> bestRawParams = null;
		List<TypeDefinitionBase> currentParams = null;
		List<TypeDefinitionBase> currentRawParams = null;

		int argsBaseIndex = argumentTypesStack.Count - numArguments;
		int baseIndex = methodCandidatesStack.Count - numCandidates;
		
		var hasNamedArgs = false;
		var firstNamedArgIndex = int.MaxValue;
		for (var i = 0; i < numArguments; ++i)
		{
			var argName = namedArgumentsStack[argsBaseIndex + i];
			if (argName != null)
			{
				hasNamedArgs = true;
				firstNamedArgIndex = i;
				break;
			}
		}
		
		// find best match
		MethodDefinition bestMatch = null;
		bool bestIsExpanded = false;
		var bestExactMatches = -1;
		var numMatchingMethods = 0;
		for (var candidateIndex = numCandidates; candidateIndex --> 0; )
		{
			var method = methodCandidatesStack[baseIndex + candidateIndex];
			
			var parameters = method.GetParameters();
			var expandParams = true;

		tryNotExpanding:
			
			if (currentParams == null)
			{
				currentParams = CreateTypeList();
				currentRawParams = CreateTypeList();
			}
			else
			{
				currentParams.Clear();
				currentRawParams.Clear();
			}

			var exactMatches = 0;
			ParameterDefinition paramsArray = null;
			var numParameters = UnityEngine.Mathf.Min(numArguments, parameters.Count);
			if (numParameters == 0 && numArguments > 0)
			{
				// Remove this candidate
				methodCandidatesStack.RemoveAt(baseIndex + candidateIndex);
				--numCandidates;
				continue;
			}
			
			var outOfOrderArgs = false;
			for (var i = 0; i < numParameters; ++i)
			{
				var parameterIndex = i;
				if (parameterIndex >= firstNamedArgIndex)
				{
					var argName = namedArgumentsStack[argsBaseIndex + i];
					if (outOfOrderArgs && argName == null)
					{
						// Remove this candidate
						exactMatches = -1;
						break;
					}
					
					if (argName != null)
					{
						parameterIndex = firstNamedArgIndex;
						while (parameterIndex < parameters.Count)
						{
							if (argName == parameters[parameterIndex].name)
								break;
							++parameterIndex;
						}
						if (parameterIndex != i)
							outOfOrderArgs = true;
						if (parameterIndex == parameters.Count)
						{
#if SI3_WARNINGS
							Debug.LogError("Error finding parameter name " + argName + " in " + method.GetTooltipText());
#endif
							exactMatches = -1;
							break;
						}
					}
				}
				
				if (expandParams && paramsArray == null && parameters[parameterIndex].IsParametersArray)
					paramsArray = parameters[parameterIndex];
					
				TypeDefinitionBase parameterType = null;
				if (paramsArray != null)
				{
					var arrayType = paramsArray.TypeOf() as ArrayTypeDefinition;
					if (arrayType != null)
						parameterType = arrayType.elementType.definition as TypeDefinitionBase;
				}
				else
				{
					if (parameterIndex >= parameters.Count)
					{
						exactMatches = -1;
						break;
					}
					parameterType = parameters[parameterIndex].TypeOf() as TypeDefinitionBase;
				}
				var genericParamType = parameterType;
				parameterType = parameterType == null ? unknownType : parameterType.SubstituteTypeParameters(method);

				if (genericContext != null)
					parameterType = parameterType.SubstituteTypeParameters(genericContext);
				
				if (parameterType.kind == SymbolKind.Delegate || parameterType.GetGenericSymbol() == builtInTypes_Expression_1)
				{
					var delegateType = parameterType.kind == SymbolKind.Class ? parameterType.GetTypeArgument(0) : parameterType;

					var resolvedArgument = resolvedArgumentsStack[argsBaseIndex + i];
					if (resolvedArgument != null && resolvedArgument.kind == SymbolKind.MethodGroup)
					{
						var methodGroup = resolvedArgument as MethodGroupDefinition;
						if (methodGroup == null)
						{
							var asMGReference = resolvedArgument as ConstructedSymbolReference;
							if (asMGReference != null)
							{
								methodGroup = asMGReference.referencedSymbol as MethodGroupDefinition;
							}
						}
						if (methodGroup != null)
						{
							var matchingMethod = methodGroup.FindMatchingMethod(delegateType);
							if (matchingMethod != null)
							{
								++exactMatches;
								continue;
							}

							exactMatches = -1;
							break;
						}
					}
				}

				var argumentType = argumentTypesStack[argsBaseIndex + i];
				//if (argumentType == null || argumentType == unknownType)
				//{
				//	exactMatches = -1;
				//	break;
				//}
				
				currentRawParams.Add(genericParamType);
				currentParams.Add(parameterType);

				bool isSameType = argumentType == null || argumentType == unknownType || argumentType.IsSameType(parameterType);
				if (isSameType)
				{
					if (CsParser.isCSharp4)
					{
						++exactMatches;
						continue;
					}

					bool isInParameter = parameters[parameterIndex].IsIn;
					bool isInArgument = modifiersStack[argsBaseIndex + i] == Modifiers.In;
					if (isInParameter == isInArgument)
					{
						++exactMatches;
						continue;
					}
				}
				var isRefOrOut = (modifiersStack[argsBaseIndex + i] & (Modifiers.Ref | Modifiers.Out)) != Modifiers.None;
				if (!isRefOrOut && !isSameType && !argumentType.CanConvertTo(parameterType))
				{
					if (numCandidates == 1 && argumentType.kind == SymbolKind.TypeParameter)
					{
						// HACK! FIXME!
						++exactMatches;
						continue;
					}
					
					exactMatches = -1;
					break;
				}
				//else if (parameterType.kind == SymbolKind.Delegate)
				//{
				//	++exactMatches;
				//	continue;
				//}
				else if (exactMatches + (numParameters - i - 1) < bestExactMatches)
				{
					break;
				}
			}

			var currentIsExpanded = paramsArray != null;
			if (!currentIsExpanded && expandParams)
			{
				currentIsExpanded = parameters != null && parameters.Count > 0 && parameters[parameters.Count - 1].IsParametersArray;
			}

			if (exactMatches < 0)
			{
				if (!currentIsExpanded)
				{
					// Remove this candidate
					methodCandidatesStack.RemoveAt(baseIndex + candidateIndex);
					--numCandidates;
					continue;
				}
				
				expandParams = false;
				paramsArray = null;
				goto tryNotExpanding;
			}
			if (exactMatches > bestExactMatches)
			{
				bestExactMatches = exactMatches;
				bestIsExpanded = currentIsExpanded;
				bestMatch = method;
				numMatchingMethods = 1;
				
				var temp = bestParams;
				bestParams = currentParams;
				currentParams = temp;

				temp = bestRawParams;
				bestRawParams = currentRawParams;
				currentRawParams = temp;
				
				// Remove all previously checked candidate
				var numPreviouslyChecked = numCandidates - (baseIndex + candidateIndex + 1);
				if (numPreviouslyChecked > 0)
				{
					methodCandidatesStack.RemoveRange(baseIndex + candidateIndex + 1, numPreviouslyChecked);
					numCandidates -= numPreviouslyChecked;
				}
				continue;
			}
			else if (exactMatches == bestExactMatches)
			{
				// Check better match
				var currentIsBetter = false;
				var otherIsBetter = false;
				
				for (var argIndex = 0; argIndex < numArguments; ++argIndex)
				{
					if (currentIsBetter && otherIsBetter)
						break;
					
					var paramIndex = UnityEngine.Mathf.Min(argIndex, currentParams.Count - 1);
					var otherParamIndex = UnityEngine.Mathf.Min(argIndex, bestParams.Count - 1);
					if (paramIndex < 0)
					{	
						if (otherParamIndex >= 0)
							otherIsBetter = true;
						break;
					}
					else if (otherParamIndex < 0)
					{	
						currentIsBetter = true;
						break;
					}

					var currentParamType = currentParams[paramIndex];
					var currentRawParamType = currentRawParams[paramIndex];
					
					var otherParamType = bestParams[otherParamIndex];
					var otherRawParamType = bestRawParams[otherParamIndex];

				checkTaskType:
					
					if (currentRawParamType == otherRawParamType)
						continue;
					
					var argType = argumentTypesStack[argsBaseIndex + argIndex];
					var isSameAsCurrentType = argType.IsSameType(currentParamType);
					var isSameAsOtherType = argType.IsSameType(otherParamType);
					
					if (isSameAsCurrentType && !isSameAsOtherType)
					{
						currentIsBetter = true;
						continue;
					}
					if (!isSameAsCurrentType && isSameAsOtherType)
					{
						otherIsBetter = true;
						continue;
					}

					// Find better conversion target
					var canConvertCurrentToOther = currentRawParamType.CanConvertTo(otherRawParamType);
					var canConvertOtherToCurrent = otherRawParamType.CanConvertTo(currentRawParamType);
					if (canConvertCurrentToOther && !canConvertOtherToCurrent)
					{
						currentIsBetter = true;
						continue;
					}
					if (!canConvertCurrentToOther && canConvertOtherToCurrent)
					{
						otherIsBetter = true;
						continue;
					}

					if (argType.kind == SymbolKind.LambdaExpression)
					{
						var currentDelegate = currentParamType.GetGenericSymbol() == builtInTypes_Expression_1 ? currentParamType.GetTypeArgument(0) : currentParamType;
						var otherDelegate = otherParamType.GetGenericSymbol() == builtInTypes_Expression_1 ? otherParamType.GetTypeArgument(0) : otherParamType;
						if (currentDelegate.kind == SymbolKind.Delegate && otherDelegate.kind == SymbolKind.Delegate)
						{
							var currentReturnType = currentDelegate.TypeOf();
							var otherReturnType = otherDelegate.TypeOf();

							var currentIsVoid = currentReturnType == builtInTypes_void;
							var otherIsVoid = otherReturnType == builtInTypes_void;
							if (!currentIsVoid || !otherIsVoid)
							{
								if (currentIsVoid)
								{
									otherIsBetter = true;
									continue;
								}
								if (otherIsVoid)
								{
									currentIsBetter = true;
									continue;
								}

								if (currentDelegate.GetGenericSymbol() == builtInTypes_Task_1 &&
									otherDelegate.GetGenericSymbol() == builtInTypes_Task_1)
								{
									currentParamType = currentDelegate.GetTypeArgument(0) ?? currentDelegate.GetTypeParameters()[0];
									otherParamType = otherDelegate.GetTypeArgument(0) ?? otherDelegate.GetTypeParameters()[0];
									goto checkTaskType;
								}
							}
						}
					}
					
					if (currentParamType.GetGenericSymbol() == builtInTypes_Nullable)
						currentParamType = currentParamType.GetTypeArgument(0);
					if (otherParamType.GetGenericSymbol() == builtInTypes_Nullable)
						otherParamType = otherParamType.GetTypeArgument(0);
					
					var currentIsSigned =
						currentParamType == builtInTypes_sbyte ||
						currentParamType == builtInTypes_short ||
						currentParamType == builtInTypes_int ||
						currentParamType == builtInTypes_long;
					var currentIsUnsigned =
						currentParamType == builtInTypes_byte ||
						currentParamType == builtInTypes_ushort ||
						currentParamType == builtInTypes_uint ||
						currentParamType == builtInTypes_ulong;
					var otherIsSigned =
						otherParamType == builtInTypes_sbyte ||
						otherParamType == builtInTypes_short ||
						otherParamType == builtInTypes_int ||
						otherParamType == builtInTypes_long;
					var otherIsUnsigned =
						otherParamType == builtInTypes_byte ||
						otherParamType == builtInTypes_ushort ||
						otherParamType == builtInTypes_uint ||
						otherParamType == builtInTypes_ulong;

					if (currentIsSigned && otherIsUnsigned)
					{
						currentIsBetter = true;
						continue;
					}
					if (currentIsUnsigned && otherIsSigned)
					{
						otherIsBetter = true;
						continue;
					}

					if (currentParamType.kind == SymbolKind.Enum && (otherIsSigned || otherIsUnsigned))
					{
						currentIsBetter = true;
						continue;
					}
					if (otherParamType.kind == SymbolKind.Enum && (currentIsSigned || currentIsUnsigned))
					{
						otherIsBetter = true;
						continue;
					}
				}

				if (currentIsBetter == otherIsBetter)
				{
					currentIsBetter = false;
					otherIsBetter = false;
					
					// Tie-breaking rules
					if (method.NumTypeParameters == 0 && bestMatch.NumTypeParameters > 0)
					{
						currentIsBetter = true;
					}
					else if (method.NumTypeParameters > 0 && bestMatch.NumTypeParameters == 0)
					{
						otherIsBetter = true;
					}
					else
					{
						if (!currentIsExpanded && bestIsExpanded)
							currentIsBetter = true;
						else if (currentIsExpanded && !bestIsExpanded)
							otherIsBetter = true;
						else if (currentIsExpanded && bestIsExpanded)
						{
							if (method.NumParameters > bestMatch.NumParameters)
								currentIsBetter = true;
							else if (method.NumParameters < bestMatch.NumParameters)
								otherIsBetter = true;
						}
						
						if (!currentIsBetter && !otherIsBetter)
						{
							if (numArguments == method.NumParameters && numArguments < bestMatch.NumParameters)
								currentIsBetter = true;
							else if (numArguments < method.NumParameters && numArguments == bestMatch.NumParameters)
								otherIsBetter = true;
						}
					}

					if (!currentIsBetter && !otherIsBetter)
					{
						if (bestMatch.isLiftedOperator && !method.isLiftedOperator)
							currentIsBetter = true;
						else if (!bestMatch.isLiftedOperator && method.isLiftedOperator)
							currentIsBetter = true;
					}
				}
				
				if (!currentIsBetter && otherIsBetter)
				{
					// Remove this candidate
					methodCandidatesStack.RemoveAt(baseIndex + candidateIndex);
					--numCandidates;
					continue;
				}
				if (currentIsBetter && !otherIsBetter)
				{
					// This method is better
					bestMatch = method;
					bestIsExpanded = currentIsExpanded;
					numMatchingMethods = 1;
				
					var temp = bestParams;
					bestParams = currentParams;
					currentParams = temp;
				
					temp = bestRawParams;
					bestRawParams = currentRawParams;
					currentRawParams = temp;
				
					// Remove all previously checked candidate
					var numPreviouslyChecked = numCandidates - (baseIndex + candidateIndex + 1);
					if (numPreviouslyChecked > 0)
					{
						methodCandidatesStack.RemoveRange(baseIndex + candidateIndex + 1, numPreviouslyChecked);
						numCandidates -= numPreviouslyChecked;
					}
					continue;
				}
				
				++numMatchingMethods;
			}
			else if (bestExactMatches >= 0)
			{
				// Remove this candidate
				methodCandidatesStack.RemoveAt(baseIndex + candidateIndex);
				--numCandidates;
				continue;
			}
		}

		if (bestMatch != null)
		{
			var parameters = bestMatch.GetParameters();
			for (var i = numArguments; i --> 0; )
			{
				var parameterIndex = -1;
				
				var argModifier = modifiersStack[argsBaseIndex + i];
				if (argModifier == Modifiers.Out && argumentNodesStack[argsBaseIndex + i] != null)
				{
					var argumentValueNode = argumentNodesStack[argsBaseIndex + i].NodeAt(-1);
					if (argumentValueNode != null)
					{
						var outVariableDeclarationNode = argumentValueNode.NodeAt(1);
						if (outVariableDeclarationNode != null)
						{
							var localVariableTypeNode = outVariableDeclarationNode.NodeAt(0);
							if (localVariableTypeNode != null)
							{
								var varNode = localVariableTypeNode.NodeAt(0);
								if (varNode != null && (varNode.RuleName == "VAR" || varNode.RuleName == "nonAssignmentExpression"))
								{
									var firstLeaf = varNode.LeafAt(0) ?? varNode.GetFirstLeaf();
									var varLeaf = firstLeaf != null && firstLeaf.token.text == "var" ? firstLeaf : null;
									var discardLeaf = firstLeaf != null && firstLeaf.token.text == "_" ? firstLeaf : null;

									if (parameterIndex == -1)
									{
										parameterIndex = i;
										if (parameterIndex >= firstNamedArgIndex)
										{
											var argName = namedArgumentsStack[argsBaseIndex + i];
											if (argName != null)
											{
												parameterIndex = firstNamedArgIndex;
												while (parameterIndex < parameters.Count)
												{
													if (argName == parameters[parameterIndex].name)
														break;
													++parameterIndex;
												}
												if (parameterIndex == parameters.Count)
												{
	#if SI3_WARNINGS
													Debug.LogError("Error finding parameter name " + argName + " in " + bestMatch.GetTooltipText());
	#endif
												}
											}
										}
									}
									
									if (parameterIndex < parameters.Count)
									{
										var argumentType = parameters[parameterIndex].TypeOf() as TypeDefinitionBase;
										if (argumentType != null)
											argumentType = argumentType.SubstituteTypeParameters(bestMatch);

										if (varLeaf != null)
										{
											varLeaf.resolvedSymbol = argumentType;
										}
										else if (discardLeaf != null && (discardLeaf.resolvedSymbol == null || discardLeaf.resolvedSymbol == unknownSymbol))
										{
											var thisInstance = argumentType.GetThisInstance() as ThisReference;
											if (thisInstance != null)
											{
												discardLeaf.token.tokenKind = SyntaxToken.Kind.ContextualKeyword;
												discardLeaf.resolvedSymbol = thisInstance.GetDiscardVariable();
												discardLeaf.semanticError = null;
											}
										}

										resolvedArgumentsStack[argsBaseIndex + i] = argumentType;
									}
								}
							}
						}
					}
				}
				
				var r = resolvedArgumentsStack[argsBaseIndex + i] as MethodGroupDefinition;
				if (r == null)
				{
					continue;
				}
				
				if (r.kind == SymbolKind.MethodGroup)
				{
					if (parameterIndex == -1)
					{
						parameterIndex = i;
						if (parameterIndex >= firstNamedArgIndex)
						{
							var argName = namedArgumentsStack[argsBaseIndex + i];
					
							parameterIndex = firstNamedArgIndex;
							while (parameterIndex < parameters.Count)
							{
								if (argName == parameters[parameterIndex].name)
									break;
								++parameterIndex;
							}
							if (parameterIndex == parameters.Count)
							{
#if SI3_WARNINGS
								Debug.LogError("Error finding parameter name " + argName + " in " + bestMatch.GetTooltipText());
#endif
							}
						}
					}
					
					if (parameterIndex < parameters.Count)
					{
						var parameterType = parameters[parameterIndex].TypeOf() as TypeDefinitionBase;
						if (parameterType == null)
							continue;

						parameterType = parameterType.SubstituteTypeParameters(bestMatch);
						if (parameterType == null)
							continue;

						if (genericContext != null)
							parameterType = parameterType.SubstituteTypeParameters(genericContext);
						if (parameterType == null)
							continue;

						var matchingMethod = r.FindMatchingMethod(parameterType);
						if (matchingMethod == null)
							continue;

						resolvedArgumentsStack[argsBaseIndex + i] = matchingMethod;
					}
				}
			}

			if (hasNamedArgs)
			{
				for (var i = numArguments; i --> 0; )
				{
					var argName = namedArgumentsStack[argsBaseIndex + i];
					if (argName == null)
						continue;

					var parameterIndex = i;
					if (argName != parameters[i].name)
					{
						parameterIndex = 0;
						while (parameterIndex < parameters.Count)
						{
							if (argName == parameters[parameterIndex].name)
								break;
							++parameterIndex;
						}
					}

					if (parameterIndex < parameters.Count)
					{
						var argNode = argumentNodesStack[argsBaseIndex + i];
						var argNameNode = argNode.firstChild as ParseTree.Node;
						if (argNameNode == null)
							continue;
						var nameLeaf = argNameNode.firstChild as ParseTree.Leaf;
						if (nameLeaf == null)
							continue;
						nameLeaf.resolvedSymbol = parameters[parameterIndex];
					}
				}
			}

			ReleaseTypeList(currentParams);
			ReleaseTypeList(currentRawParams);
			ReleaseTypeList(bestParams);
			ReleaseTypeList(bestRawParams);
			
			if (numMatchingMethods > 1)
			{
				return ambiguousMethodOverload;
			}
			else
			{
				return bestMatch;
			}
		}
		
		ReleaseTypeList(currentParams);
		ReleaseTypeList(currentRawParams);
		ReleaseTypeList(bestParams);
		ReleaseTypeList(bestRawParams);
		
		if (numCandidates <= 1)
			return unresolvedMethodOverload;
		return numMatchingMethods > 0 ? ambiguousMethodOverload : unresolvedMethodOverload;
	}

	public MethodDefinition FindMatchingMethod(TypeDefinitionBase delegateType)
	{
		var parameters = delegateType.GetParameters() ?? _emptyParameterList;
		var returnType = delegateType.TypeOf() as TypeDefinitionBase;
		
		var parameterTypes = new TypeDefinitionBase[parameters.Count];
		for (var i = parameters.Count; i --> 0; )
			parameterTypes[i] = (parameters[i].TypeOf() as TypeDefinitionBase).SubstituteTypeParameters(delegateType); // TODO: Verify this!

		for (var j = methods.Count; j --> 0; )
		{
			var m = methods[j];
			var p = m.GetParameters() ?? _emptyParameterList;
			if (p.Count != parameters.Count)
				continue;
			for (var i = p.Count; i --> 0; )
			{
				var p1 = p[i];
				var p2 = parameters[i];
				var p1Mods = p1.modifiers & Modifiers.RefOutOrIn;
				var p2Mods = p2.modifiers & Modifiers.RefOutOrIn;
				if (p1Mods != p2Mods)
					goto nextMethod;

				var p1Type = p1.TypeOf() as TypeDefinitionBase;
				p1Type = p1Type.SubstituteTypeParameters(delegateType); // TODO: Fix this!
				var p2Type = parameterTypes[i];
				p2Type = p2Type.SubstituteTypeParameters(p1Type);

				if (p2Mods == 0 && p2Type.IsReferenceType)
				{
					if (!p2Type.CanConvertTo(p1Type))
						goto nextMethod;
				}
				else
				{
					if (!p2Type.IsSameType(p1Type))
						goto nextMethod;
				}
			}

			var ret1Type = m.ReturnType();
			ret1Type = ret1Type.SubstituteTypeParameters(delegateType); // TODO: Fix this!
			var ret2Type = returnType.SubstituteTypeParameters(ret1Type);
			if (!ret1Type.CanConvertTo(ret2Type))
				continue;

			return m;
		nextMethod:
			continue;
		}

		return null;
	}

	private bool CanConvertTo(DelegateTypeDefinition delegateType)
	{
		throw new NotImplementedException();
	}

	public override bool IsAccessible(AccessLevelMask accessLevelMask)
	{
		for (var i = methods.Count; i --> 0; )
			if (methods[i].IsAccessible(accessLevelMask))
				return true;
		return false;
	}

	private Dictionary<string, ConstructedMethodGroupDefinition> constructedMethodGroups;
	public ConstructedMethodGroupDefinition ConstructMethodGroup(TypeReference[] typeArgs)
	{
		var sb = StringBuilders.Alloc();
		
		var delimiter = string.Empty;
		if (typeArgs != null)
		{
			foreach (var arg in typeArgs)
			{
				sb.Append(delimiter);
				sb.Append(arg.ToString());
				delimiter = ", ";
			}
		}
		var sig = sb.ToString();
		
		StringBuilders.Release(sb);

		if (constructedMethodGroups == null)
			constructedMethodGroups = new Dictionary<string, ConstructedMethodGroupDefinition>();

		ConstructedMethodGroupDefinition result;
		if (constructedMethodGroups.TryGetValue(sig, out result))
		{
			if (result.IsValid() && result.typeArguments != null && result.methods.Count == methods.Count)
			{
				if (result.typeArguments.All(x => x.definition != null && x.definition.kind != SymbolKind.Error && x.definition.IsValid()))
				{
					for (var i = result.methods.Count; i --> 0; )
						if (!result.methods[i].IsValid())
							result.methods.RemoveAt(i);
					
					for (var i = result.methods.Count; i --> 0; )
						if (!methods.Contains(((ConstructedMethodDefinition) result.methods[i]).genericMethodDefinition))
							result.methods.RemoveAt(i);
					
					if (methods.Count == result.methods.Count)
						return result;
				}
			}
		}

		if (result != null)
		{
			for (var i = result.methods.Count; i --> 0; )
			{
				var method = result.methods[i];
				method.savedParentSymbol = method.parentSymbol;
				method.parentSymbol = null;
			}
			result.savedParentSymbol = parentSymbol;
			result.parentSymbol = null;
		}

		var newTypeArgs = new TypeReference[typeArgs.Length];
		for (var i = typeArgs.Length; i --> 0; )
				newTypeArgs[i] = typeArgs[i];

		result = new ConstructedMethodGroupDefinition(this, newTypeArgs);
		constructedMethodGroups[sig] = result;
		return result;
	}
}

public class ConstructedMethodGroupDefinition : MethodGroupDefinition
{
	public readonly MethodGroupDefinition genericMethodGroupDefinition;
	public readonly TypeReference[] typeArguments;

	public override SymbolDefinition GetGenericSymbol()
	{
		return genericMethodGroupDefinition;
	}

	public ConstructedMethodGroupDefinition(MethodGroupDefinition definition, TypeReference[] arguments)
	{
		name = definition.name;
		kind = definition.kind;
		parentSymbol = definition.parentSymbol;
		genericMethodGroupDefinition = definition;
		modifiers = definition.modifiers;
		//numTypeParameters = definition.numTypeParameters;
		
		typeArguments = arguments;
		//if (arguments != null)
		//{
		//	typeArguments = new TypeReference[arguments.Length];
		//	for (var i = 0; i < typeArguments.Length; ++i)
		//		typeArguments[i] = TypeReference.To(arguments[i].definition);
		//}

		UpdateMethods();
	}

	private void UpdateMethods()
	{
		var genericMethods = genericMethodGroupDefinition.methods;
		
		for (var i = methods.Count; i --> 0; )
			if (!genericMethods.Contains(methods[i].GetGenericSymbol() as MethodDefinition))
				methods.RemoveAt(i);
		
		for (var i = genericMethods.Count; i --> 0; )
		{
			var m = genericMethods[i];
			if (m.NumTypeParameters == typeArguments.Length)
			{
				bool ok = false;
				for (var j = methods.Count; j --> 0; )
					if (methods[j].GetGenericSymbol() == m)
					{
						ok = true;
						break;
					}
				if (!ok)
				{
					var constructedMethod = m.ConstructMethod(typeArguments);
					if (constructedMethod != null)
					{
						constructedMethod.parentSymbol = this;
						methods.Add(constructedMethod);
					}
				}
			}
		}
	}

	public override SymbolDefinition ResolveMethodOverloads(ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		UpdateMethods();
		var genericMethod = /*genericMethodGroupDefinition.*/base.ResolveMethodOverloads(argumentListNode, typeArgs, scope, invokedLeaf);
		//if (genericMethod == null || genericMethod.kind != SymbolKind.Method)
		//	return null;
		return genericMethod;
	}

	public override MethodDefinition ResolveMethodOverloads(int numArguments, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		UpdateMethods();
		return base.ResolveMethodOverloads(numArguments, scope, invokedLeaf);
	}

	public override int CollectCandidates(int numArguments, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		UpdateMethods();
		return base.CollectCandidates(numArguments, scope, invokedLeaf);
	}

	public override void AddMethod(MethodDefinition method)
	{
		Debug.LogError("AddMethod on ConstructedMethodGroupDefinition: " + method);
	}
}

public class ConstructedMethodDefinition : MethodDefinition
{
	public readonly MethodDefinition genericMethodDefinition;
	public readonly TypeReference[] typeArguments;
	
	public override bool IsExtensionMethod {
		get { return genericMethodDefinition.IsExtensionMethod; }
	}

	public override SymbolDefinition GetGenericSymbol()
	{
		return genericMethodDefinition;
	}

	public ConstructedMethodDefinition(MethodDefinition definition, TypeReference[] arguments)
	{
		name = definition.name;
		kind = definition.kind;
		parentSymbol = definition.parentSymbol;
		genericMethodDefinition = definition;
		_parameters = genericMethodDefinition.parameters;
		modifiers = genericMethodDefinition.modifiers;

		if (definition.typeParameters != null && arguments != null)
		{
			_typeParameters = definition.typeParameters;
			typeArguments = new TypeReference[_typeParameters.Count];
			for (var i = 0; i < typeArguments.Length; ++i)
				typeArguments[i] = i < arguments.Length ? arguments[i] : TypeReference.To(unknownType);
		}
	}

	public override TypeDefinitionBase TypeOfTypeParameter(TypeParameterDefinition tp)
	{
		if (typeParameters != null)
		{
			var index = typeParameters.IndexOf(tp);
			if (index >= 0)
				return typeArguments[index].definition as TypeDefinitionBase ?? tp;
		}
		return base.TypeOfTypeParameter(tp);
	}

	public override TypeDefinitionBase ReturnType()
	{
		var result = genericMethodDefinition.ReturnType();
		result = result.SubstituteTypeParameters(this);
		return result;
	}

	public override CharSpan GetName()
	{
		var typeParameters = GetTypeParameters();
		if (typeParameters == null || typeParameters.Count == 0)
			return name;

		var sb = StringBuilders.Alloc();
		
		sb.Append(name);
		sb.Append('<');
		sb.Append(TypeOfTypeParameter(typeParameters[0]).GetName());
		for (var i = 1; i < typeParameters.Count; ++i)
		{
			sb.Append(", ");
			sb.Append(TypeOfTypeParameter(typeParameters[i]).GetName());
		}
		sb.Append('>');
		var result = sb.ToString();
		
		StringBuilders.Release(sb);
		
		return result;
	}

	public override ConstructedMethodDefinition ConstructMethod(TypeReference[] typeArgs)
	{
		return genericMethodDefinition.ConstructMethod(typeArgs);
	}
}

public abstract class InvokeableSymbolDefinition : SymbolDefinition
{
	public abstract TypeDefinitionBase ReturnType();
	
	protected bool initialized = false;

	protected TypeReference _returnType;
	protected TypeReference returnType
	{
		get
		{
			if (!initialized)
				Initialize();
			return _returnType;
		}
	}
	
	protected List<ParameterDefinition> _parameters;
	public List<ParameterDefinition> parameters
	{
		get
		{
			if (!initialized)
				Initialize();
			return _parameters;
		}
		
		set
		{
			_parameters = value;
		}
	}
	
	public List<TypeParameterDefinition> _typeParameters;
	public List<TypeParameterDefinition> typeParameters
	{
		get
		{
			if (!initialized)
				Initialize();
			return _typeParameters;
		}
	}
	
	protected virtual void Initialize()
	{
		initialized = true;
	}
	
	public SymbolDefinition AddTypeParameter(SymbolDeclaration symbol)
	{
		var symbolName = symbol.Name;
		if (_typeParameters == null)
			_typeParameters = new List<TypeParameterDefinition>();
		var definition = _typeParameters.FirstByName(symbolName);
		if (definition == null)
		{
			definition = (TypeParameterDefinition)Create(symbol);
			definition.parentSymbol = this;
			_typeParameters.Add(definition);
		}
		
		symbol.definition = definition;
		
		var nameNode = symbol.NameNode();
		if (nameNode != null)
		{
			var leaf = nameNode as ParseTree.Leaf;
			if (leaf != null)
				leaf.SetDeclaredSymbol(definition);
			else
			{
				var lastLeaf = ((ParseTree.Node)nameNode).GetLastLeaf();
				if (lastLeaf != null)
				{
					if (lastLeaf.parent.RuleName == "typeParameterList")
						lastLeaf = lastLeaf.parent.parent.LeafAt(0);
					lastLeaf.SetDeclaredSymbol(definition);
				}
			}
		}
		
		return definition;
	}
	
	public bool CanCallWith(int numArguments, bool asExtensionMethod)
	{
		var modifiers = MethodGroupDefinition.modifiersStack;
		int baseIndex = modifiers.Count - numArguments;
		
		var minArgs = asExtensionMethod ? 1 : 0;
		var maxArgs = minArgs;
		var parameters = this.parameters ?? _emptyParameterList;
		for (var i = 0; i < parameters.Count; ++i)
		{
			var param = parameters[i];

			if (i < numArguments)
			{
				if (!asExtensionMethod || !param.IsThisParameter)
				{
					var passedWithOut = modifiers[baseIndex + i] == Modifiers.Out;
					var passedWithRef = modifiers[baseIndex + i] == Modifiers.Ref;
					var passedWithIn = modifiers[baseIndex + i] == Modifiers.In;
					if (param.IsOut != passedWithOut || param.IsRef != passedWithRef || passedWithIn && !param.IsIn)
						return false;
				}
			}
			else if (!param.IsOptional)
			{
				return false;
			}

			if (!asExtensionMethod || !param.IsThisParameter)
			{
				if (param.IsParametersArray)
					maxArgs = 100000;
				else if (!param.IsOptional)
					++minArgs;
				++maxArgs;
			}
		}
		if (numArguments < minArgs || numArguments > maxArgs)
			return false;

		var numOptionalNamedArgs = 0;
		var namedArgs = MethodGroupDefinition.namedArgumentsStack;
		for (var numFixed = asExtensionMethod ? 1 : 0; numFixed < numArguments; ++numFixed)
		{
			var argName = namedArgs[baseIndex + numFixed];
			if (argName == null)
				continue;
			if (numFixed >= parameters.Count)
				return false;
			if (argName == parameters[numFixed].name)
				continue;

			for (var j = numFixed; j < numArguments; ++j)
			{
				argName = namedArgs[baseIndex + j];
				if (argName == null)
				{
					// Positional argument after out-of-order named argument.
					return false;
				}

				var found = false;
				for (var k = numFixed; k < parameters.Count; ++k)
				{
					var p = parameters[k];
					if (p.name == argName)
					{
						if (p.IsOptional)
							++numOptionalNamedArgs;
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}
			break;
		}
		if (numArguments - numOptionalNamedArgs < minArgs)
			return false;
		return true;
	}

	public override SymbolDefinition TypeOf()
	{
		return ReturnType();
	}
	
	public override List<ParameterDefinition> GetParameters()
	{
		return parameters ?? _emptyParameterList;
	}

	public override List<TypeParameterDefinition> GetTypeParameters()
	{
		return typeParameters;
	}

	public SymbolDefinition AddParameter(SymbolDeclaration symbol)
	{
		var symbolName = symbol.Name;
		var parameter = (ParameterDefinition) Create(symbol);
		parameter.type = TypeReference.To(symbol.parseTreeNode.FindChildByName("type"));
		parameter.parentSymbol = this;
		var lastNode = symbol.parseTreeNode.NodeAt(-1);
		if (lastNode != null && lastNode.RuleName == "defaultArgument")
		{
			var defaultValueNode = lastNode.NodeAt(1);
			if (defaultValueNode != null)
				parameter.defaultValue = defaultValueNode.Print();
		}
		if (!string.IsNullOrEmpty(symbolName))
		{
			if (_parameters == null)
				_parameters = new List<ParameterDefinition>();
			_parameters.Add(parameter);
			
			if (IsOperator && parameters.Count > 1)
			{
				if (_name == "op_UnaryPlus")
				{
					SetNewName("op_Addition");
				}
				else if (_name == "op_UnaryNegation")
				{
					SetNewName("op_Subtraction");
				}
			}
		}
		return parameter;
	}
	
	private void RemoveParameter(ParameterDefinition parameter)
	{
		parameters.Remove(parameter);
		
		if (IsOperator && parameters.Count == 1)
		{
			if (_name == "op_Addition")
			{
				SetNewName("op_UnaryPlus");
			}
			else if (_name == "op_Subtraction")
			{
				SetNewName("op_UnaryNegation");
			}
		}
	}
	
	private void SetNewName(CharSpan newName)
	{
		var thisMethod = this as MethodDefinition;
		if (thisMethod == null)
		{
			Debug.LogError("this is not a MethodDefiniton: " + GetTooltipText());
			name = newName;
			return;
		}
		
		var mg = parentSymbol as MethodGroupDefinition;
		if (mg == null)
		{
			Debug.LogError("parentSymbol is not a MethodGroupDefinition for: " + GetTooltipText());
			name = newName;
			return;
		}
		
		var type = mg.parentSymbol as TypeDefinitionBase;
		if (type == null)
		{
			Debug.LogError("type is null for: " + GetTooltipText());
			name = newName;
			return;
		}
		
		var declaration = declarations;
		if (declarations == null)
		{
			Debug.LogError("No declaration for: " + GetTooltipText());
			name = newName;
			return;
		}
		
		mg.RemoveDeclaration(declaration);
		
		var newMG = type.FindName(newName, 0, false) as MethodGroupDefinition;
		if (newMG == null)
		{
			newMG = new MethodGroupDefinition
			{
				kind = SymbolKind.MethodGroup,
				name = newName,
				modifiers = Modifiers.None, //Modifiers.Public | Modifiers.Static,
				parentSymbol = type,
			};
			type.AddMember(newMG);
		}
		
		newMG.AddMethod(thisMethod);
	}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter)
		{
			SymbolDefinition definition = AddParameter(symbol);
			//	if (!members.TryGetValue(symbolName, out definition) || definition is ReflectedMember || definition is ReflectedType)
			//		definition = AddMember(symbol);

			symbol.definition = definition;
			return definition;
		}
		else if (symbol.kind == SymbolKind.TypeParameter)
		{
			SymbolDefinition definition = AddTypeParameter(symbol);
			return definition;
		}

		return base.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter && parameters != null)
			parameters.Remove((ParameterDefinition) symbol.definition);
		else if (symbol.kind == SymbolKind.TypeParameter && typeParameters != null)
			typeParameters.Remove((TypeParameterDefinition) symbol.definition);
		else
			base.RemoveDeclaration(symbol);
	}

	public override SymbolDefinition FindName(CharSpan memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName.DecodeId();

		if (!asTypeOnly && numTypeParameters == 0 && parameters != null)
		{
			var definition = parameters.FirstByName(memberName);
			if (definition != null)
				return definition;
		}
		else
		{
			if (typeParameters != null)
			{
				var definition = typeParameters.FirstByName(memberName);
				if (definition != null)
					return definition;
			}
		}
		return base.FindName(memberName, numTypeParameters, asTypeOnly);
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		if (asTypeOnly)
			return;

		if (numTypeArgs == 0)
		{
			var leafText = DecodeId(leaf.token.text);

			if (parameters != null)
			{
				for (var i = parameters.Count; i --> 0; )
				{
					if (parameters[i].name == leafText)
					{
						leaf.resolvedSymbol = parameters[i];
						return;
					}
				}
			}
			
			if (typeParameters != null)
			{
				for (var i = typeParameters.Count; i --> 0; )
				{
					if (typeParameters[i].name == leafText)
					{
						leaf.resolvedSymbol = typeParameters[i];
						return;
					}
				}
			}
		}
		
		base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
	}

	public SymbolDefinition ResolveParameterName(ParseTree.Leaf leaf)
	{
		var leafText = DecodeId(leaf.token.text);
		var p = GetParameters();
		for (var j = p.Count; j --> 0; )
		{
			var pd = p[j];
			if (pd.name == leafText)
				return leaf.resolvedSymbol = pd;
		}
		return leaf.resolvedSymbol = unknownParameterName;
	}
	
	//public override string GetTooltipText()
	//{
	//    if (tooltipText != null)
	//        return tooltipText;

	//    var parentSD = parentSymbol;
	//    if (parentSD != null && !string.IsNullOrEmpty(parentSD.GetName()))
	//        tooltipText = kind.ToString().ToLowerInvariant() + " " + parentSD.GetName() + "." + name;
	//    else
	//        tooltipText = kind.ToString().ToLowerInvariant() + " " + name;

	//    var typeOf = TypeOf();
	//    var typeName = "";
	//    if (typeOf != null && kind != SymbolKind.Constructor && kind != SymbolKind.Destructor)
	//    {
	//        //var tp = typeOf as TypeParameterDefinition;
	//        //if (tp != null)
	//        //    typeOf = TypeOfTypeParameter(tp);
	//        var ctx = parentSymbol as ConstructedTypeDefinition;
	//        if (ctx != null)
	//            typeOf = ((TypeDefinitionBase) typeOf).SubstituteTypeParameters(ctx);
	//        typeName = typeOf.GetName() + " ";

	//        if (typeOf.kind != SymbolKind.TypeParameter)
	//            for (var parentType = typeOf.parentSymbol as TypeDefinitionBase; parentType != null; parentType = parentType.parentSymbol as TypeDefinitionBase)
	//                typeName = parentType.GetName() + '.' + typeName;
	//    }

	//    var parentText = string.Empty;
	//    var parent = parentSymbol is MethodGroupDefinition ? parentSymbol.parentSymbol : parentSymbol;
	//    if ((parent is TypeDefinitionBase && parent.kind != SymbolKind.Delegate && kind != SymbolKind.TypeParameter)
	//        || parent is NamespaceDefinition
	//        )//|| kind == SymbolKind.Accessor)
	//    {
	//        var parentName = parent.GetName();
	//        if (kind == SymbolKind.Constructor)
	//        {
	//            var typeParent = parent.parentSymbol as TypeDefinitionBase;
	//            parentName = typeParent != null ? typeParent.GetName() : null;
	//        }
	//        if (!string.IsNullOrEmpty(parentName))
	//            parentText = parentName + ".";
	//    }

	//    var nameText = name;

	//    List<ParameterDefinition> parameters = GetParameters();
	//    var parametersText = string.Empty;
	//    string parametersEnd = null;

	//    if (kind == SymbolKind.Method)
	//    {
	//        nameText += '(';
	//        //parameters = ((MethodDefinition) this).parameters;
	//        parametersEnd = ")";
	//    }
	//    else if (kind == SymbolKind.Constructor)
	//    {
	//        nameText = parent.name + '(';
	//        //parameters = ((MethodDefinition) this).parameters;
	//        parametersEnd = ")";
	//    }
	//    else if (kind == SymbolKind.Destructor)
	//    {
	//        nameText = "~" + parent.name + "()";
	//    }
	//    else if (kind == SymbolKind.Indexer)
	//    {
	//        nameText = "this[";
	//        //parameters = ((IndexerDefinition) this).parameters;
	//        parametersEnd = "]";
	//    }
	//    else if (kind == SymbolKind.Delegate)
	//    {
	//        nameText += '(';
	//        //parameters = ((DelegateTypeDefinition) this).parameters;
	//        parametersEnd = ")";
	//    }

	//    if (parameters != null)
	//    {
	//        parametersText = PrintParameters(parameters);
	//    }

	//    tooltipText = kindText + typeName + parentText + nameText + parametersText + parametersEnd;

	//    if (typeOf != null && typeOf.kind == SymbolKind.Delegate)
	//    {
	//        tooltipText += "\n\nDelegate info\n";
	//        tooltipText += typeOf.GetDelegateInfoText();
	//    }

	//    return tooltipText;
	//}

	//public override bool IsGeneric
	//{
	//	get
	//	{
	//		if (ReturnType().IsGeneric)
	//			return true;
	//		var numParams = parameters == null ? 0 : parameters.Count;
	//		for (var i = 0; i < numParams; ++i)
	//			if (parameters[i].TypeOf().IsGeneric)
	//				return true;
	//		return false;
	//	}
	//}
}

public class MethodDefinition : InvokeableSymbolDefinition
{
	protected bool isExtensionMethod;
	public override bool IsExtensionMethod {
		get { if (!initialized) Initialize(); return isExtensionMethod; }
	}

	public bool isOperator;
	public bool isLiftedOperator;
	public override bool IsOperator {
		get { return isOperator; }
	}

	public TypeReference explicitInterfaceImplementation;

	public MethodDefinition()
	{
		kind = SymbolKind.Method;
	}
	
	public static MethodDefinition CreateOperator(
		string operatorName,
		TypeReference returnType,
		TypeReference lhsOperandType,
		TypeReference rhsOperandType)
	{
		var method = new MethodDefinition();
		method.name = operatorName;
		method.isOperator = true;
		method.modifiers = Modifiers.Public | Modifiers.Static;
		method._returnType = returnType;
		method._parameters = new List<ParameterDefinition>{
			new ParameterDefinition { name = "a", type = lhsOperandType },
			new ParameterDefinition { name = "b", type = rhsOperandType }
		};
		return method;
	}

	public static MethodDefinition CreateOperator(
		string operatorName,
		TypeDefinitionBase returnType,
		TypeDefinitionBase lhsOperandType,
		TypeDefinitionBase rhsOperandType)
	{
		var method = new MethodDefinition();
		method.name = operatorName;
		method.isOperator = true;
		method.modifiers = Modifiers.Public | Modifiers.Static;
		method._returnType = TypeReference.To(returnType);
		method._parameters = new List<ParameterDefinition>{
			new ParameterDefinition { name = "a", type = TypeReference.To(lhsOperandType) },
			new ParameterDefinition { name = "b", type = TypeReference.To(rhsOperandType) }
		};
		return method;
	}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		var result = base.AddDeclaration(symbol);
		
		if (IsStatic && result.kind == SymbolKind.Parameter && (result.modifiers & Modifiers.This) != 0 &&
			symbol.parseTreeNode != null && symbol.parseTreeNode.parent != null && symbol.parseTreeNode.parent.childIndex == 0)
		{
			var parentType = (parentSymbol.kind == SymbolKind.MethodGroup ? parentSymbol.parentSymbol : parentSymbol) as TypeDefinitionBase;
			if (parentType.kind == SymbolKind.Class && parentType.NumTypeParameters == 0)
			{
				var namespaceDefinition = parentType.parentSymbol;
				if (namespaceDefinition is NamespaceDefinition)
				{
					isExtensionMethod = true;
					++parentType.numExtensionMethods;
				}
			}
		}
		
		return result;
	}
	
	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (IsExtensionMethod && symbol.kind == SymbolKind.Parameter && (symbol.definition.modifiers & Modifiers.This) != 0 &&
			(symbol.parseTreeNode == null || symbol.parseTreeNode.parent == null || symbol.parseTreeNode.parent.childIndex == 0))
		{
			isExtensionMethod = false;
			
			var parentType = (parentSymbol.kind == SymbolKind.MethodGroup ? parentSymbol.parentSymbol : parentSymbol) as TypeDefinitionBase;
			
			var namespaceDefinition = parentType != null ? parentType.parentSymbol : null;
			if (namespaceDefinition is NamespaceDefinition)
				--parentType.numExtensionMethods;
		}
		base.RemoveDeclaration(symbol);
	}

	public override TypeDefinitionBase ReturnType()
	{
		if (returnType == null)
		{
			if (kind == SymbolKind.Constructor)
			{
				var result = parentSymbol as TypeDefinitionBase ?? parentSymbol.parentSymbol as TypeDefinitionBase;
				_returnType = result != null ? TypeReference.To(result) : null;
				return result ?? unknownType;
			}

			if (declarations != null)
			{
				ParseTree.BaseNode refNode = null;
				switch (declarations.parseTreeNode.RuleName)
				{
					case "methodDeclaration":
					case "interfaceMethodDeclaration":
						refNode = declarations.parseTreeNode.FindPreviousNode();
						break;
					case "conversionOperatorDeclarator":
					case "localFunctionDeclaration":
						refNode = declarations.parseTreeNode.ChildAt(2);
						break;
					default:
						refNode = declarations.parseTreeNode.parent.parent.ChildAt(declarations.parseTreeNode.parent.childIndex - 1);
						break;
				}
#if SI3_WARNINGS
				if (refNode == null)
					Debug.LogError("Could not find method return type from node: " + declarations.parseTreeNode);
#endif
				_returnType = refNode != null ? TypeReference.To(refNode) : null;
			}
		}
		
		return _returnType == null ? unknownType : _returnType.definition as TypeDefinitionBase ?? unknownType;
	}
	
	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		foreach (var parameter in GetParameters())
		{
			var parameterName = parameter.GetName();
			if (!data.ContainsKey(parameterName))
				data.Add(parameterName, parameter);
		}
		if ((flags & (BindingFlags.Instance | BindingFlags.Static)) != BindingFlags.Instance)
		{
			if (typeParameters != null)
			{
				foreach (var parameter in typeParameters)
				{
					var parameterName = parameter.name;
					if (!data.ContainsKey(parameterName))
						data.Add(parameterName, parameter);
				}
			}
		}

		for (var i = 0; i < members.Count; ++i)
		{
			var methodGroup = members[i] as MethodGroupDefinition;
			if (methodGroup != null)
			{
				foreach (var method in methodGroup.methods)
				{
					var reflectionName = method.ReflectionName;
					if (!data.ContainsKey(reflectionName))
						data.Add(reflectionName, method);
				}
			}
		}
	}
	
	public override SymbolDefinition Rebind()
	{
		if (parentSymbol == null && savedParentSymbol == null)
			return this;
		
		var newParent = (parentSymbol ?? savedParentSymbol).Rebind() as MethodGroupDefinition;
		if (newParent == null)
			return null;
		
		if (newParent == parentSymbol)
			return this;
		
		var tp = GetTypeParameters();
		var numTypeParams = tp != null ? tp.Count : 0;
		
		MethodDefinition newSymbol = null;
		var ownParams = GetParameters();
		foreach (var m in newParent.methods)
		{
			if (m.IsStatic != IsStatic)
				continue;

			if (m.NumTypeParameters != numTypeParams)
				continue;
				
			var otherParams = m.GetParameters();
			if (otherParams == null || ownParams.Count != otherParams.Count)
				continue;
			
			var allEqual = true;
			for (var i = ownParams.Count; i --> 0; )
			{
				var own = ownParams[i];
				var other = otherParams[i];
				if (own.IsOut != other.IsOut || own.IsRef != other.IsRef || own.IsIn != other.IsIn || own.name != other.name)
				{
					allEqual = false;
					break;
				}
				var ownParamType = own.TypeOf().Rebind();
				if (!ownParamType.IsSameType(other.TypeOf() as TypeDefinitionBase))
				{
					allEqual = false;
					break;
				}
			}
			if (allEqual)
			{
				newSymbol = m;
				break;
			}
		}
		
#if SI3_WARNINGS
		if (newSymbol == null)
		{
			Debug.LogWarning(GetTooltipText() + " not found in " + newParent.GetTooltipText());
			return null;
		}
#endif
		
		if (newSymbol == null || newSymbol == this)
			return this;
		
		//if (newSymbol.typeParameters != null)
		//{
		//	for (var i = newSymbol.typeParameters.Count; i --> 0; )
		//		newSymbol.typeParameters[i] = newSymbol.typeParameters[i].Rebind() as TypeParameterDefinition;
		//}
		
		//if (constructedMethods != null && constructedMethods.Count > 0)
		//{
		//	var newCache = new Dictionary<int, ConstructedMethodDefinition>();
		//	foreach (var kv in constructedMethods)
		//	{
		//		var newMethod = kv.Value.Rebind() as ConstructedMethodDefinition;
		//		if (newMethod == null)
		//			continue;
				
		//		if (newMethod == kv.Value)
		//		{
		//			newCache[kv.Key] = newMethod;
		//			continue;
		//		}
				
		//		var typeArgs = newMethod.typeArguments;
		//		var numTypeArgs = typeArgs != null ? typeArgs.Length : 0;
				
		//		var hash = 0;
		//		if (typeArgs != null)
		//		{
		//			unchecked // ignore overflow
		//			{
		//				hash = (int)2166136261;
		//				for (var i = 0; i < numTypeParams; ++i)
		//					hash = hash * 16777619 ^ (i < numTypeArgs ? typeArgs[i].definition : unknownType).GetHashCode();
		//			}
		//		}
		//		newCache[hash] = newMethod;
		//	}
			
		//	newSymbol.constructedMethods = newCache;
		//}
		
		return newSymbol;
	}
	
	private Dictionary<int, ConstructedMethodDefinition> constructedMethods;
	public virtual ConstructedMethodDefinition ConstructMethod(TypeReference[] typeArgs)
	{
		var numTypeParams = typeParameters != null ? typeParameters.Count : 0;
		if (numTypeParams == 0)
			return null;

		var numTypeArgs = typeArgs != null ? typeArgs.Length : 0;
		if (numTypeArgs == 0)
		{
#if SI3_WARNINGS
			Debug.LogWarning("Calling ConstructMethod without type arguments!");
#endif
			return null;
		}

		var hash = 0;
		if (typeArgs != null)
		{
			unchecked // ignore overflow
			{
				hash = (int)2166136261;
				for (var i = 0; i < numTypeArgs; ++i)
					hash = hash * 16777619 ^ (i < numTypeArgs ? typeArgs[i].definition : unknownType).GetHashCode();
			}
		}
		
		if (constructedMethods == null)
			constructedMethods = new Dictionary<int, ConstructedMethodDefinition>();
		
		ConstructedMethodDefinition result;
		if (constructedMethods.TryGetValue(hash, out result))
		{
			if (result.IsValid() && result.typeArguments != null)
			{
				var validCachedMethod = true;
				var resultTypeArgs = result.typeArguments;
				for (var i = 0; i < numTypeParams; ++i)
				{
					var definition = resultTypeArgs[i].definition;
					var typeArg = i < numTypeArgs ? typeArgs[i].definition : unknownType;
					if (definition == null || !definition.IsValid() || definition != typeArg)
					{
						validCachedMethod = false;
						break;
					}
				}
				if (validCachedMethod)
					return result;
			}
		}
			
		result = new ConstructedMethodDefinition(this, typeArgs);
		constructedMethods[hash] = result;
		return result;
	}
	
	public TypeDefinitionBase GetParentType()
	{
		if (parentSymbol == null)
			return null;
#if SI3_WARNINGS
		if (parentSymbol.kind != SymbolKind.MethodGroup)
			Debug.LogWarning("Expected method group - " + parentSymbol.GetTooltipText());
#endif
		var parentType = parentSymbol.parentSymbol as TypeDefinitionBase;
		return parentType;
	}
}

public class NamespaceName : IEquatable<NamespaceName>
{
	public readonly string name;
	public readonly int hashID;
	
	static Dictionary<int, NamespaceName> all = new Dictionary<int, NamespaceName>(1000);
	
	public List<NamespaceDefinition> allNamespaces = new List<NamespaceDefinition>();
	
	public static NamespaceName Get(string name)
	{
		var hashID = SymbolDefinition.GetHashID(name);
		NamespaceName nn;
		if (all.TryGetValue(hashID, out nn))
			return nn;
		
		nn = new NamespaceName(name, hashID);
		all[hashID] = nn;
		return nn;
	}
	
	NamespaceName(string name, int hashID)
	{
		this.name = name;
		this.hashID = hashID;
	}
	
	public override string ToString()
	{
		return name;
	}
	
	public bool Equals(NamespaceName other)
	{
		return hashID == other.hashID;
	}
	
	public override bool Equals(object obj)
	{
		var asNN = obj as NamespaceName;
		return asNN != null && hashID == asNN.hashID;
	}
	
	public override int GetHashCode()
	{
		return hashID;
	}
}

public class NamespaceDefinition : SymbolDefinition
{
	//public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	//{
	//	if (symbol.Name == "CsGrammar")
	//		Debug.Log("Adding " + symbol + " to namespace " + name);
	//	return base.AddDeclaration(symbol);
	//}
	
	//public override void RemoveDeclaration(SymbolDeclaration symbol)
	//{
	//	if (symbol.Name == "CsGrammar")
	//		Debug.Log("Removing " + symbol + " from namespace " + name);
	//	base.RemoveDeclaration(symbol);
	//}
	
	//public override SymbolDefinition FindName(string memberName)
	//{
	//    var result = base.FindName(memberName);
	//    if (result == null)
	//    {
	//        UnityEngine.Debug.Log(memberName + " not found in " + GetTooltipText());
	//    }
	//    return result;
	//}
	
	public override SymbolDefinition Rebind()
	{
		SymbolDefinition assembly = this;
		while (assembly != null)
		{
			var result = assembly as AssemblyDefinition;
			if (result != null)
				return result.FindSameNamespace(this);
			assembly = assembly.parentSymbol ?? assembly.savedParentSymbol;
		}
		return null;
	}
	
	public void CollectExtensionMethods(
		CharSpan id,
		TypeReference[] typeArgs,
		TypeDefinitionBase extendedType,
		HashSet<MethodDefinition> extensionsMethods,
		Scope context)
	{
		var numTypeArguments = typeArgs == null ? -1 : typeArgs.Length;
		
		var thisAssembly = Assembly;
		var contextAssembly = context.GetAssembly();

		var accessLevelMask = AccessLevelMask.Public;
		if (thisAssembly != null && thisAssembly.InternalsVisibleTo(contextAssembly))
			accessLevelMask |= AccessLevelMask.Internal;

		var contextType = context.EnclosingType();

		for (var i = members.Count; i --> 0; )
		{
			var typeDefinition = members[i];
			if (typeDefinition.kind != SymbolKind.Class || !typeDefinition.IsValid() || (typeDefinition as TypeDefinitionBase).numExtensionMethods == 0 || !typeDefinition.IsStatic || typeDefinition.NumTypeParameters > 0)
				continue;

			var currentAccessLevelMask = accessLevelMask;
			if (contextType == typeDefinition)
			{
				currentAccessLevelMask = AccessLevelMask.Any;
			}
			else
			{
				if (contextType != null && contextType.DerivesFrom(typeDefinition as TypeDefinitionBase))
					currentAccessLevelMask |= AccessLevelMask.Protected;
				// TODO: Add else here?
				if (!typeDefinition.IsAccessible(accessLevelMask))
					continue;
			}
			
			//if (contextType == parentType || parentType.IsSameOrParentOf(contextType))
			//	accessLevelMask |= AccessLevelMask.Public | AccessLevelMask.Protected | AccessLevelMask.Private;
			//else if (contextType.DerivesFrom(parentType))
			//	accessLevelMask |= AccessLevelMask.Public | AccessLevelMask.Protected;
			
			SymbolDefinition member;
			if (typeDefinition.members.TryGetValue(id, numTypeArguments, out member))
			{
				if (member.kind == SymbolKind.MethodGroup)
				{
					var methodGroup = member as MethodGroupDefinition;
					if (methodGroup != null)
					{
						foreach (var method in methodGroup.methods)
						{
							if (method.IsExtensionMethod && method.IsAccessible(currentAccessLevelMask))
							{
								if (numTypeArguments >= 0 && numTypeArguments != method.NumTypeParameters)
									continue;
								
								var extendsType = method.parameters[0].TypeOf() as TypeDefinitionBase;
								if ((extendsType is TypeParameterDefinition) || extendedType.DerivesFromRef(ref extendsType))
								{
									if (numTypeArguments > 0)
									{
										var constructedMethod = method.ConstructMethod(typeArgs);
										if (constructedMethod != null)
											extensionsMethods.Add(constructedMethod);
										else
											extensionsMethods.Add(method);
									}
									else
									{
										extensionsMethods.Add(method);
									}
								}
							}
						}
					}
					else
					{
						#if SI3_WARNINGS
						Debug.LogError("Expected a method group: " + member.GetTooltipText());
						#endif
					}
				}
			}
		}
	}

	private bool resolvingMember = false;
	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		if (resolvingMember)
			return;
		resolvingMember = true;
		
		leaf.resolvedSymbol = null;
		//if (declarations != null)
		//{
		//	foreach (var declaration in declarations)
		//	{
		//		declaration.scope.Resolve(leaf, numTypeArgs);
		//		if (leaf.resolvedSymbol != null)
		//		{
		//			resolvingMember = false;
		//			return;
		//		}
		//	}
		//}

		var assemblyDefinition = context != null ? context.GetAssembly() : null;
		if (assemblyDefinition == null || assemblyDefinition == Assembly)
			base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
		
		resolvingMember = false;
		
		if (leaf.resolvedSymbol == null)
		{
			if (assemblyDefinition != null)
			{
				assemblyDefinition.ResolveInReferencedAssemblies(leaf, this, numTypeArgs, asTypeOnly);
			}
		}
	}

	public override void ResolveAttributeMember(ParseTree.Leaf leaf, Scope context)
	{
		if (resolvingMember)
			return;
		resolvingMember = true;

		leaf.resolvedSymbol = null;
		leaf.semanticError = null;
		base.ResolveAttributeMember(leaf, context);

		resolvingMember = false;

		if (leaf.resolvedSymbol == null)
		{
			var assemblyDefinition = context.GetAssembly();
			assemblyDefinition.ResolveAttributeInReferencedAssemblies(leaf, this);
		}
	}
	
	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		GetMembersCompletionData(data, context.fromInstance ? 0 : BindingFlags.Static, AccessLevelMask.Any, context);
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		base.GetMembersCompletionData(data, flags, mask, context);

		var assemblyDefinition = context.assembly;
		assemblyDefinition.GetMembersCompletionDataFromReferencedAssemblies(data, this, context);
	}

	public void GetTypesOnlyCompletionData(Dictionary<string, SymbolDefinition> data, AccessLevelMask mask, AssemblyDefinition assembly)
	{
		if ((mask & AccessLevelMask.Public) != 0)
		{
			var thisAssembly = Assembly;
			if (thisAssembly != null)
			{
				if (thisAssembly.InternalsVisibleTo(assembly))
					mask |= AccessLevelMask.Internal;
				else
					mask &= ~AccessLevelMask.Internal;
			}
		}
		
		for (var i = 0; i < members.Count; ++i)
		{
			var m = members[i];
			
			if (m.kind == SymbolKind.Namespace)
				continue;
			
			if (m.kind != SymbolKind.MethodGroup)
			{
				if (m.IsAccessible(mask) && !data.ContainsKey(m.ReflectionName))
				{
					data.Add(m.ReflectionName, m);
				}
			}
		}
		
		if (assembly != null)
			assembly.GetTypesOnlyCompletionDataFromReferencedAssemblies(data, this);
	}

	//public override bool IsPublic
	//{
	//	get
	//	{
	//		return true;
	//	}
	//}

	public override TypeDefinitionBase TypeOfTypeParameter(TypeParameterDefinition tp)
	{
		return tp;
	}

	public override string GetTooltipText(bool fullText = true)
	{
		return name == string.Empty ? "global namespace" : base.GetTooltipText(fullText);
	}

	public void GetExtensionMethodsCompletionData(TypeDefinitionBase targetType, Dictionary<string, SymbolDefinition> data, AccessLevelMask accessLevelMask, TypeDefinitionBase contextType)
	{
		for (var i = 0; i < members.Count; ++i)
		{
			var t = members[i];
			
			if (t.kind == SymbolKind.Class && t.IsStatic && t.NumTypeParameters == 0 &&
				(t as TypeDefinitionBase).numExtensionMethods > 0)
			{
				AccessLevelMask mask =
					t == contextType || t.IsSameOrParentOf(contextType) ? AccessLevelMask.Any :
					contextType != null && contextType.DerivesFrom(t as TypeDefinitionBase) ? AccessLevelMask.Protected : 0;
				mask |= accessLevelMask;
				if (!t.IsAccessible(mask))
					continue;

				var classMembers = t.members;
				for (var j = 0; j < classMembers.Count; ++j)
				{
					var cm = classMembers[j];
					
					if (cm.kind == SymbolKind.MethodGroup)
					{
						var mg = cm as MethodGroupDefinition;
						if (mg == null)
							continue;
						if (data.ContainsKey(mg.name))
							continue; // TODO: Check if data contains the same overload?
						foreach (var m in mg.methods)
						{
							if (m.kind != SymbolKind.Method)
								continue;
							if (!m.IsExtensionMethod)
								continue;
							if (!m.IsAccessible(mask))
								continue;
							
							var parameters = m.GetParameters();
							if (parameters == null || parameters.Count == 0)
								continue;
							var extendsType = parameters[0].TypeOf() as TypeDefinitionBase;
							if (!(extendsType is TypeParameterDefinition) && !targetType.CanConvertTo(extendsType))
								continue;
							
							data.Add(m.name, m);
							break;
						}
					}
					//else if (cm.kind == SymbolKind.Method)
					//{
					//	var m = cm as MethodDefinition;
					//	if (m == null)
					//		continue;
					//	if (!m.IsExtensionMethod)
					//		continue;
					//	//Debug.Log(m.GetTooltipText() + " in " + m.NamespaceOfExtensionMethod);
					//}
				}
			}
		}
	}
	
	public IEnumerable<TypeDefinitionBase> EnumTypes(string name)
	{
		for (var i = members.Count; i --> 0; )
		{
			var member = members[i];
			
			switch (member.kind)
			{
			case SymbolKind.Class:
			case SymbolKind.Delegate:
			case SymbolKind.Enum:
			case SymbolKind.Interface:
			case SymbolKind.Struct:
				if (member.name == name)
					yield return member as TypeDefinitionBase;
				break;
				
			case SymbolKind.Namespace:
				var nsDef = member as NamespaceDefinition;
				foreach (var type in nsDef.EnumTypes(name))
					yield return type;
				break;
			}
		}
	}
	
	public IEnumerable<SymbolDefinition> EnumTypesAndNamespaces(string name)
	{
		for (var i = members.Count; i --> 0; )
		{
			var member = members[i];
			
			switch (member.kind)
			{
			case SymbolKind.Class:
			case SymbolKind.Delegate:
			case SymbolKind.Enum:
			case SymbolKind.Interface:
			case SymbolKind.Struct:
				if (member.name == name)
					yield return member;
				break;
				
			case SymbolKind.Namespace:
				var nsDef = member as NamespaceDefinition;
				if (nsDef.name == name)
					yield return nsDef;
				
				foreach (var symbol in nsDef.EnumTypesAndNamespaces(name))
					yield return symbol;
				break;
			}
		}
	}
}

public class CompilationUnitDefinition : NamespaceDefinition
{
	//public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	//{
	//	return base.AddDeclaration(symbol);
	//}

	//public override void RemoveDeclaration(SymbolDeclaration symbol)
	//{
	//	base.RemoveDeclaration(symbol);
	//}
}

public class SymbolDeclarationScope : Scope
{
	public SymbolDeclaration declaration;
	
	public SymbolDeclarationScope(ParseTree.Node node) : base(node) {}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
	//	if (symbol.kind == SymbolKind.Method)// || symbol.kind == SymbolKind.LambdaExpression)
	//	{
	//		declaration = symbol;
	//		return parentScope.AddDeclaration(symbol);
	//	}
		if (symbol.scope == null)
			symbol.scope = this;
		if (declaration == null)
		{
			Debug.LogWarning("Missing declaration in SymbolDeclarationScope! Can't add " + symbol + "\nfor node: " + parseTreeNode);
			return null;
		}
		return declaration.definition.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if ((symbol.kind == SymbolKind.Method /*|| symbol.kind == SymbolKind.LambdaExpression*/) && declaration == symbol)
		{
			declaration = null;
			parentScope.RemoveDeclaration(symbol);
		}
		else if (declaration != null && declaration.definition != null)
		{
			declaration.definition.RemoveDeclaration(symbol);
		}
	}

	public override SymbolDefinition FindName(CharSpan symbolName, int numTypeParameters)
	{
		throw new NotImplementedException();
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs, bool asTypeOnly)
	{
		if (declaration != null && declaration.definition != null)
		{
			if (declaration.definition is TypeDefinitionBase)
			{
				declaration.definition.ResolveMember(leaf, this, numTypeArgs, asTypeOnly);
			}
			else if (!asTypeOnly && numTypeArgs <= 0)
			{
				var parameters = declaration.definition.GetParameters();
				if (parameters != null)
				{
					var id = SymbolDefinition.DecodeId(leaf.token.text);
					for (int i = parameters.Count; i --> 0; )
					{
						if (parameters[i].GetName() == id)
						{
							leaf.resolvedSymbol = parameters[i];
							return;
						}
					}
				}
			}

			if (numTypeArgs == 0 && leaf.resolvedSymbol == null)
			{
				var typeParams = declaration.definition.GetTypeParameters();
				if (typeParams != null)
				{
					var id = SymbolDefinition.DecodeId(leaf.token.text);
					for (int i = typeParams.Count; i --> 0; )
					{
						if (typeParams[i].GetName() == id)
						{
							leaf.resolvedSymbol = typeParams[i];
							return;
						}
					}
				}
			}
		}

		if (leaf.resolvedSymbol == null)
			base.Resolve(leaf, numTypeArgs, asTypeOnly);
	}

	public override void ResolveAttribute(ParseTree.Leaf leaf)
	{
		if (declaration != null && declaration.definition != null)
			declaration.definition.ResolveAttributeMember(leaf, this);

		if (leaf.resolvedSymbol == null)
			base.ResolveAttribute(leaf);
	}

	public override TypeDefinition EnclosingType()
	{
		if (declaration != null)
		{
			switch (declaration.kind)
			{
				case SymbolKind.Class:
				case SymbolKind.Struct:
				case SymbolKind.Interface:
					var type = declaration.definition as TypeDefinition;
					if (type != null)
						return type;
					break;
			}
		}
		return parentScope != null ? parentScope.EnclosingType() : null;
	}

	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		if (declaration != null && declaration.definition != null)
		{
			var typeParameters = declaration.definition.GetTypeParameters();
			if (typeParameters != null)
			{
				for (var i = typeParameters.Count; i --> 0; )
				{
					var tp = typeParameters[i];
					if (!data.ContainsKey(tp.name))
						data.Add(tp.name, tp);
				}
			}
		}
		base.GetCompletionData(data, context);
	}
}

public class TypeBaseScope : Scope
{
	public TypeDefinitionBase definition;
	
	public TypeBaseScope(ParseTree.Node node) : base(node) {}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		//Debug.Log("Adding base types list: " + symbol);
		//if (definition != null)
		//    definition.baseType = new TypeReference { identifier = symbol.Name };
		//Debug.Log("baseType: " + definition.baseType.definition);
		return null;
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
	}

	public override SymbolDefinition FindName(CharSpan symbolName, int numTypeParameters)
	{
		return parentScope.FindName(symbolName, numTypeParameters);
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs, bool asTypeOnly)
	{
		if (parentScope != null)
			parentScope.Resolve(leaf, numTypeArgs, asTypeOnly);
	}
}

public class BodyScope : LocalScope
{
	public SymbolDefinition definition;
	
	public BodyScope(ParseTree.Node node) : base(node) {}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (definition == null)
			return null;
		
		symbol.scope = this;
	//	Debug.Log("Adding declaration " + symbol + " to " + definition);

		switch (symbol.kind)
		{
		case SymbolKind.ConstantField:
		case SymbolKind.LocalConstant:
			if (!(definition is TypeDefinitionBase))
				return base.AddDeclaration(symbol);
			break;
		case SymbolKind.Variable:
		case SymbolKind.CaseVariable:
		case SymbolKind.ForEachVariable:
		case SymbolKind.FromClauseVariable:
		case SymbolKind.TupleDeconstructVariable:
		case SymbolKind.OutVariable:
		case SymbolKind.IsVariable:
			return base.AddDeclaration(symbol);
		}

		if (definition is TypeDefinitionBase || symbol.kind != SymbolKind.Method)
			return definition.AddDeclaration(symbol);
		else
			return base.AddDeclaration(symbol); // add local function
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		switch (symbol.kind)
		{
		case SymbolKind.LocalConstant:
		case SymbolKind.Variable:
		case SymbolKind.TupleDeconstructVariable:
		case SymbolKind.OutVariable:
		case SymbolKind.IsVariable:
		case SymbolKind.CaseVariable:
		case SymbolKind.ForEachVariable:
		case SymbolKind.FromClauseVariable:
			base.RemoveDeclaration(symbol);
			return;
		}

		if (definition != null)
			definition.RemoveDeclaration(symbol);
		base.RemoveDeclaration(symbol);
	}

	//public virtual SymbolDefinition ImportReflectedType(Type type)
	//{
	//    throw new InvalidOperationException();
	//}

	public override SymbolDefinition FindName(CharSpan symbolName, int numTypeParameters)
	{
		return definition.FindName(symbolName, numTypeParameters, false);
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs, bool asTypeOnly)
	{
		leaf.resolvedSymbol = null;

		if (definition != null)
		{
			definition.ResolveMember(leaf, this, numTypeArgs, asTypeOnly);
			
			if (leaf.resolvedSymbol != null)
				return;
						
			if (numTypeArgs == 0 && leaf.resolvedSymbol == null)
			{
				var typeParams = definition.GetTypeParameters();
				if (typeParams != null)
				{
					var id = SymbolDefinition.DecodeId(leaf.token.text);
					for (var i = typeParams.Count; i --> 0; )
					{
						if (typeParams[i].GetName() == id)
						{
							leaf.resolvedSymbol = typeParams[i];
							return;
						}
					}
				}
			}
		}

		base.Resolve(leaf, numTypeArgs, asTypeOnly);
	}

	public override void ResolveAttribute(ParseTree.Leaf leaf)
	{
		leaf.resolvedSymbol = null;
		leaf.semanticError = null;
		if (definition != null)
			definition.ResolveAttributeMember(leaf, this);

		if (leaf.resolvedSymbol == null)
			base.ResolveAttribute(leaf);
	}

	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		if (definition != null)
			definition.GetCompletionData(data, context);
		
		bool oldFromInstance = context.fromInstance;
		Scope scope = this;
		while (context.fromInstance && scope != null)
		{
			var asBodyScope = scope as BodyScope;
			if (asBodyScope != null)
			{
				var symbol = asBodyScope.definition;
				if (symbol != null && symbol.kind != SymbolKind.LambdaExpression)
				{
					if (!symbol.IsInstanceMember)
						context.fromInstance = false;
					break;
				}
			}
			scope = scope.parentScope;
		}
		base.GetCompletionData(data, context);
		context.fromInstance = oldFromInstance;
	}
}

public class UsingAliasDefinition : SymbolDefinition
{
	public TypeReference type;
	
	public override SymbolDefinition TypeOf()
	{
		return type.definition;
	}
	
	public override string GetTooltipText(bool fullText = true)
	{
		if (!fullText)
			return name.ToString();

		if (type.definition.kind == SymbolKind.Namespace)
			return tooltipText = "(namespace alias) " + name + " = " + type.definition.GetTooltipText(true);
		else
			return tooltipText = "(type alias) " + name + " = " + type.definition.GetTooltipText(true);
	}
}

public class NamespaceScope : Scope
{
	public NamespaceDeclaration declaration;
	public NamespaceDefinition definition;

	public List<SymbolDeclaration> typeDeclarations;

	public NamespaceScope(ParseTree.Node node) : base(node) {}
	
	public override IEnumerable<NamespaceDefinition> VisibleNamespacesInScope()
	{
		yield return definition;

		foreach (var nsRef in declaration.importedNamespaces)
		{
			var ns = nsRef.definition as NamespaceDefinition;
			if (ns != null)
				yield return ns;
		}

		if (parentScope != null)
			foreach (var ns in parentScope.VisibleNamespacesInScope())
				yield return ns;
	}

	//public override SymbolDefinition AddDeclaration(SymbolKind symbolKind, ParseTree.Node definitionNode)
	//{
	//    SymbolDefinition result;

	//    if (symbolKind != SymbolKind.Namespace)
	//    {
	//        result = base.AddDeclaration(symbolKind, definitionNode);
	//    }
	//    else
	//    {
	//        var symbol = new NamespaceDeclaration { kind = symbolKind, parseTreeNode = definitionNode };
	//        result = AddDeclaration(symbol);
	//    }

	//    result.parentSymbol = definition;
	//    return result;
	//}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (definition == null)
			return null;

		symbol.scope = this;
		
		if (symbol.kind == SymbolKind.Class ||
			symbol.kind == SymbolKind.Struct ||
			symbol.kind == SymbolKind.Interface ||
			symbol.kind == SymbolKind.Enum ||
			symbol.kind == SymbolKind.Delegate)
		{
			if (typeDeclarations == null)
				typeDeclarations = new List<SymbolDeclaration>();
			typeDeclarations.Add(symbol);
			//symbol.modifiers = (symbol.modifiers & Modifiers.Public) != 0 ? Modifiers.Public : Modifiers.Internal;
		}

		if (symbol.kind == SymbolKind.ImportedNamespace)
		{
			declaration.importedNamespaces.Add(TypeReference.To(symbol.parseTreeNode.ChildAt(0)));
			return null;
		}
		else if (symbol.kind == SymbolKind.ImportedStaticType)
		{
			if (symbol.parseTreeNode.numValidNodes >= 2)
				declaration.importedStaticTypes.Add(TypeReference.To(symbol.parseTreeNode.ChildAt(1)));
			return null;
		}
		else if (symbol.kind == SymbolKind.UsingAlias)
		{
			var usingAliasDefinition = SymbolDefinition.Create(symbol) as UsingAliasDefinition;
			declaration.usingAliases.Add(usingAliasDefinition);
			usingAliasDefinition.type = TypeReference.To(symbol.parseTreeNode.ChildAt(2));
			return null;
		}

		return definition.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (typeDeclarations != null)
			typeDeclarations.Remove(symbol);

		if (symbol.kind == SymbolKind.ImportedNamespace)
		{
			var node = symbol.parseTreeNode;
			for (var i = declaration.importedNamespaces.Count; i --> 0;)
			{
				var x = declaration.importedNamespaces[i].Node;
				if (x == null || x.parent == null || x.parent == node)
				{
					declaration.importedNamespaces.RemoveAt(i);
					return;
				}
				//else
				//{
				//	Debug.LogWarning("No parent: " + x);
				//}
			}
			return;
		}
		else if (symbol.kind == SymbolKind.ImportedStaticType)
		{
			var node = symbol.parseTreeNode;
			for (var i = declaration.importedStaticTypes.Count; i --> 0;)
			{
				var x = declaration.importedStaticTypes[i].Node;
				if (x == null || x.parent == null || x.parent == node)
				{
					declaration.importedStaticTypes.RemoveAt(i);
					return;
				}
			}
			return;
		}
		else if (symbol.kind == SymbolKind.UsingAlias)
		{
			for (var i = declaration.usingAliases.Count; i --> 0; )
			{
				var x = declaration.usingAliases[i];
				if (x.ListRemoveDeclaration(symbol))
				{
					if (x.declarations == null)
						declaration.usingAliases.RemoveAt(i);
					return;
				}
			}
			return;
		}

		if (definition != null)
			definition.RemoveDeclaration(symbol);
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs, bool asTypeOnly)
	{
		leaf.resolvedSymbol = null;
		
		if (declaration == null)
			return;
		
		var id = SymbolDefinition.DecodeId(leaf.token.text);
		
		if (numTypeArgs == 0)
		{
			for (int i = declaration.usingAliases.Count; i --> 0; )
			{
				if (declaration.usingAliases[i].name == id)
				{
					if (declaration.usingAliases[i].type != null)
					{
						leaf.resolvedSymbol = declaration.usingAliases[i].type.definition;
						return;
					}
					else
					{
						break;
					}
				}
			}
		}

		if (!asTypeOnly && leaf.resolvedSymbol == null)
		{
			if (leaf.parent.RuleName == "primaryExpressionStart")
			{
				var primaryExpressionPartNode = leaf.parent.nextSibling as ParseTree.Node;
				if (primaryExpressionPartNode != null && primaryExpressionPartNode.RuleName == "primaryExpressionPart")
				{
					var argumentListNode = primaryExpressionPartNode.FindChildByName("arguments", "argumentList") as ParseTree.Node;
					if (argumentListNode != null)
					{
						TypeReference[] typeArgs = null;
						ParseTree.Node typeArgumentListNode = leaf.parent.NodeAt(1);
						if (typeArgumentListNode != null && typeArgumentListNode.RuleName == "typeArgumentList")
						{
							var numTypeArguments = typeArgumentListNode.numValidNodes / 2;
							typeArgs = TypeReference.AllocArray(numTypeArguments);
							for (int i = 0; i < numTypeArguments; ++i)
								typeArgs[i] = TypeReference.To(typeArgumentListNode.ChildAt(1 + 2 * i));
						}
						//ResolveAsImportedStaticMethod(leaf, null, argumentListNode, typeArgs, this);
						ResolveAsImportedStaticMethod(id, argumentListNode, typeArgs, this, leaf);
						TypeReference.ReleaseArray(typeArgs);
					}
				}
			}
		}
		
		if (leaf.resolvedSymbol == null && definition != null)
		{
			definition.ResolveMember(leaf, this, numTypeArgs, asTypeOnly);
		}

		var parseTreeNode = this.parseTreeNode ?? declaration.parseTreeNode;
		if (leaf.resolvedSymbol == null && declaration.importedNamespaces.Count > 0 && parseTreeNode != null)
		{
			var firstMemberDeclNode = parseTreeNode.FindChildByName("namespaceMemberDeclaration");
			var firstMemberDeclLeaf = firstMemberDeclNode == null ? null : firstMemberDeclNode.GetFirstLeaf();
			var line = firstMemberDeclLeaf == null ? int.MaxValue : firstMemberDeclLeaf.line;
			var leafLine = leaf.line;
			if (line <= leafLine || line == leafLine && firstMemberDeclLeaf.tokenIndex <= leaf.tokenIndex)
			{
				for (var i = declaration.importedNamespaces.Count; i --> 0; )
				{
					var importedNamespaceDefinition = declaration.importedNamespaces[i].definition as NamespaceDefinition;
					if (importedNamespaceDefinition != null)
					{
						importedNamespaceDefinition.ResolveMember(leaf, this, numTypeArgs, true);
						if (leaf.resolvedSymbol != null)
						{
							if (leaf.resolvedSymbol.kind == SymbolKind.Namespace)
								leaf.resolvedSymbol = null;
							else
								break;
						}
					}
					else
					{
						var importedNamespacePathNode = declaration.importedNamespaces[i].Node as ParseTree.Node;
						if (importedNamespacePathNode != null)
						{
							importedNamespacePathNode = importedNamespacePathNode.NodeAt(0);
							if (importedNamespacePathNode != null)
							{
								var childNsDefinition = definition.Assembly.FindNamespace(importedNamespacePathNode, true, definition as NamespaceDefinition);
								if (childNsDefinition != null)
								{
									childNsDefinition.ResolveMember(leaf, this, numTypeArgs, true);
									if (leaf.resolvedSymbol != null)
									{
										if (leaf.resolvedSymbol.kind == SymbolKind.Namespace)
											leaf.resolvedSymbol = null;
										else
											break;
									}
								}

								var nsDefinition = definition.Assembly.FindNamespace(importedNamespacePathNode, true);
								if (nsDefinition != null)
								{
									nsDefinition.ResolveMember(leaf, this, numTypeArgs, true);
									if (leaf.resolvedSymbol != null)
									{
										if (leaf.resolvedSymbol.kind == SymbolKind.Namespace)
											leaf.resolvedSymbol = null;
										else
											break;
									}
								}
							}
						}
					}
				}
			}
		}
		
		if (leaf.resolvedSymbol != null)
			return;
		
		for (var i = declaration.importedStaticTypes.Count; i --> 0; )
		{
			var stRef = declaration.importedStaticTypes[i];
			var importedType = stRef.definition as TypeDefinitionBase;
			if (importedType == null)
				continue;
					
			importedType.ResolveMember(leaf, this, numTypeArgs, asTypeOnly);
			if (leaf.resolvedSymbol == null)
				continue;
					
			if (leaf.resolvedSymbol.parentSymbol == importedType)
			{
				if (!leaf.resolvedSymbol.IsInstanceMember)
					break;
			}
			leaf.resolvedSymbol = null;
		}

		if (leaf.resolvedSymbol != null)
			return;
		
		if (definition != null)
		{
			var parentScopeDef = parentScope != null ? ((NamespaceScope) parentScope).definition : null;
			for (var nsDef = definition.parentSymbol as NamespaceDefinition;
				leaf.resolvedSymbol == null && nsDef != null && nsDef != parentScopeDef;
				nsDef = nsDef.parentSymbol as NamespaceDefinition)
			{
				nsDef.ResolveMember(leaf, this, numTypeArgs, asTypeOnly);
			}
		}

		if (leaf.resolvedSymbol == null && parentScope != null)
			parentScope.Resolve(leaf, numTypeArgs, asTypeOnly);
	}

	public override void ResolveAttribute(ParseTree.Leaf leaf)
	{
		leaf.resolvedSymbol = null;
		leaf.semanticError = null;

		var id = SymbolDefinition.DecodeId(leaf.token.text);
		
		for (int i = declaration == null ? 0 : declaration.usingAliases.Count; i --> 0; )
		{
			if (declaration.usingAliases[i].name == id)
			{
				if (declaration.usingAliases[i].type != null)
				{
					leaf.resolvedSymbol = declaration.usingAliases[i].type.definition;
					return;
				}
				else
				{
					break;
				}
			}
		}
		
		var parentScopeDef = parentScope != null ? ((NamespaceScope) parentScope).definition : null;
		for (var nsDef = definition;
			leaf.resolvedSymbol == null && nsDef != null && nsDef != parentScopeDef;
			nsDef = nsDef.parentSymbol as NamespaceDefinition)
		{
			nsDef.ResolveAttributeMember(leaf, this);
		}
		
		if (leaf.resolvedSymbol == null && declaration != null)
		{
			foreach (var nsRef in declaration.importedNamespaces)
			{
				if (nsRef.definition != null)
				{
					nsRef.definition.ResolveAttributeMember(leaf, this);
					if (leaf.resolvedSymbol != null)
						break;
				}
			}
		}

		if (leaf.resolvedSymbol == null && parentScope != null)
			parentScope.ResolveAttribute(leaf);
	}
	
	public void CollectImportedStaticMethods(
		string id,
		TypeReference[] typeArgs,
		Scope context,
		HashSet<MethodDefinition> methods)
	{
		var numTypeArguments = typeArgs == null ? -1 : typeArgs.Length;
		
		var contextAssembly = context.GetAssembly();
		
		for (var i = declaration.importedStaticTypes.Count; i --> 0; )
		{
			var typeDefinition = declaration.importedStaticTypes[i].definition;
			if (typeDefinition == null
				|| (typeDefinition.kind != SymbolKind.Class && typeDefinition.kind != SymbolKind.Struct)
				|| !typeDefinition.IsValid())
				continue;
			
			var accessLevelMask = AccessLevelMask.Public;
			if (typeDefinition.Assembly != null && typeDefinition.Assembly.InternalsVisibleTo(contextAssembly))
				accessLevelMask |= AccessLevelMask.Internal;
			
			if (!typeDefinition.IsAccessible(accessLevelMask))
				continue;
			
			//if (contextType == parentType || parentType.IsSameOrParentOf(contextType))
			//	accessLevelMask |= AccessLevelMask.Public | AccessLevelMask.Protected | AccessLevelMask.Private;
			//else if (contextType.DerivesFrom(parentType))
			//	accessLevelMask |= AccessLevelMask.Public | AccessLevelMask.Protected;

			var asReflectedType = typeDefinition as ReflectedType;
			if (asReflectedType != null)
			{
				asReflectedType.ReflectAllMembers(accessLevelMask != AccessLevelMask.Public ? BindingFlags.Public | BindingFlags.NonPublic : BindingFlags.Public);
			}
			
			SymbolDefinition member;
			if (!typeDefinition.members.TryGetValue(id, numTypeArguments, out member))
				continue;
			if (member.kind != SymbolKind.MethodGroup)
				continue;
			var methodGroup = member as MethodGroupDefinition;
			if (methodGroup == null)
			{
				#if SI3_WARNINGS
				Debug.LogError("Expected a method group: " + member.GetTooltipText());
				#endif
				continue;
			}

			foreach (var method in methodGroup.methods)
			{
				if (method.IsExtensionMethod || !method.IsStatic || !method.IsAccessible(accessLevelMask))
					continue;

				if (numTypeArguments > 0)
				{
					var constructedMethod = method.ConstructMethod(typeArgs);
					methods.Add(constructedMethod ?? method);
				}
				else
				{
					methods.Add(method);
				}
			}
		}
	}

	public SymbolDefinition ResolveAsImportedStaticMethod(ParseTree.Leaf invokedLeaf, SymbolDefinition invokedSymbol, ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope context)
	{
		if (invokedLeaf == null && (invokedSymbol == null || invokedSymbol.kind == SymbolKind.Error))
			return null;
		
		CharSpan id = invokedSymbol != null && invokedSymbol.kind != SymbolKind.Error ? invokedSymbol.name : invokedLeaf != null ? SymbolDefinition.DecodeId(invokedLeaf.token.text) : CharSpan.Empty;
		
		return ResolveAsImportedStaticMethod(id, argumentListNode, typeArgs, context, invokedLeaf);
	}
	
	public SymbolDefinition ResolveAsImportedStaticMethod(string id, ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope context, ParseTree.Leaf invokedLeaf = null)
	{
		if (invokedLeaf != null && invokedLeaf.resolvedSymbol == null)
			invokedLeaf.resolvedSymbol = SymbolDefinition.unknownSymbol;
		
		MethodDefinition firstAccessibleMethod = null;
		
		var importedStaticMethods = new HashSet<MethodDefinition>();
		
		MethodDefinition namedArgsError = null;
		int numArguments = 0;
		int argsBaseIndex = MethodGroupDefinition.modifiersStack.Count;

		for (var nsScope = this; nsScope != null; nsScope = nsScope.parentScope as NamespaceScope)
		{
			nsScope.CollectImportedStaticMethods(id, typeArgs, context, importedStaticMethods);
			if (importedStaticMethods.Count == 0)
				continue;

			firstAccessibleMethod = importedStaticMethods.First();
			
			if (numArguments == 0)
			{
				numArguments = MethodGroupDefinition.ProcessArgumentListNode(argumentListNode, null);
				namedArgsError = MethodGroupDefinition.CheckNamedArguments(numArguments);
			}
			
			MethodDefinition resolved = namedArgsError;
			if (resolved == null)
			{
				var candidatesStack = MethodGroupDefinition.methodCandidatesStack;
				int baseIndex = candidatesStack.Count;
				foreach (var method in importedStaticMethods)
					if (numArguments == 0 || method.CanCallWith(numArguments, false))
						candidatesStack.Add(method);
				int numCandidates = candidatesStack.Count - baseIndex;

				if (typeArgs == null)
				{
					for (var i = numCandidates; i --> 0;)
					{
						var candidate = candidatesStack[baseIndex + i];
						if (candidate.NumTypeParameters == 0 || numArguments == 0)
							continue;

						candidate = MethodGroupDefinition.InferMethodTypeArguments(candidate, numArguments, invokedLeaf);
						if (candidate == null)
							candidatesStack.RemoveAt(baseIndex + i);
						else
							candidatesStack[baseIndex + i] = candidate;
					}
				}
				else
				{
					for (var i = numCandidates; i-- > 0; )
					{
						var candidate = candidatesStack[baseIndex + i];
						if (candidate.NumTypeParameters != 0)
							continue;

						candidatesStack.RemoveAt(baseIndex + i);
					}
				}
				numCandidates = candidatesStack.Count - baseIndex;
			
				resolved = MethodGroupDefinition.ResolveMethodOverloads(numArguments, numCandidates);
			
				if (candidatesStack.Count > baseIndex)
					candidatesStack.RemoveRange(baseIndex, candidatesStack.Count - baseIndex);
			}
			
			if (resolved != null && resolved.kind != SymbolKind.Error)
			{
				MethodGroupDefinition.modifiersStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.argumentTypesStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.resolvedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.argumentNodesStack.RemoveRange(argsBaseIndex, numArguments);
				
				if (invokedLeaf != null)
					invokedLeaf.resolvedSymbol = resolved;
				
				return resolved;
			}

			importedStaticMethods.Clear();
		}

		MethodGroupDefinition.modifiersStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.argumentTypesStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.resolvedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.argumentNodesStack.RemoveRange(argsBaseIndex, numArguments);
				
		if (firstAccessibleMethod != null && invokedLeaf != null)
		{
			invokedLeaf.resolvedSymbol = firstAccessibleMethod;
			invokedLeaf.semanticError = MethodGroupDefinition.unresolvedMethodOverload.name;
		}
		return null;
	}

	public override MethodDefinition FindDeconstructExtensionMethod(TypeDefinitionBase memberOf, int numOutParameters, Scope context)
	{
		MethodDefinition firstAccessibleMethod = null;
		
		var thisAssembly = GetAssembly();
		
		var extensionsMethods = new HashSet<MethodDefinition>();
		
		int numArguments = 0;
		int argsBaseIndex = MethodGroupDefinition.modifiersStack.Count;

		var parentNSScope = parentScope as NamespaceScope;
		var parentNSDef = parentNSScope != null ? parentNSScope.definition : null;
		for (var nsDef = definition;
			nsDef != null && nsDef != parentNSDef;
			nsDef = nsDef.parentSymbol as NamespaceDefinition)
		{
			thisAssembly.CollectExtensionMethods(nsDef, "Deconstruct", null, memberOf, extensionsMethods, context);
			if (extensionsMethods.Count > 0)
			{
				firstAccessibleMethod = extensionsMethods.First();
				
				if (numArguments == 0)
				{
					MethodGroupDefinition.modifiersStack.Add(Modifiers.This);
					MethodGroupDefinition.argumentTypesStack.Add(memberOf);
					MethodGroupDefinition.resolvedArgumentsStack.Add(null);//extendedType.GetThisInstance());
					MethodGroupDefinition.namedArgumentsStack.Add(null);
					MethodGroupDefinition.argumentNodesStack.Add(null);

					numArguments = numOutParameters + 1;
					
					for (var i = 0; i < numOutParameters; ++i)
					{
						MethodGroupDefinition.modifiersStack.Add(Modifiers.Out);
						MethodGroupDefinition.argumentTypesStack.Add(null);
						MethodGroupDefinition.resolvedArgumentsStack.Add(null);//extendedType.GetThisInstance());
						MethodGroupDefinition.namedArgumentsStack.Add(null);
						MethodGroupDefinition.argumentNodesStack.Add(null);
					}
				}
				
				MethodDefinition resolved = null;
				{
					var candidatesStack = MethodGroupDefinition.methodCandidatesStack;
					int baseIndex = candidatesStack.Count;
					foreach (var method in extensionsMethods)
					{
						if (method.ReturnType() == SymbolDefinition.builtInTypes_void
							&& method.CanCallWith(numArguments, true))
						{
							candidatesStack.Add(method);
						}
					}
					
					int numCandidates = candidatesStack.Count - baseIndex;
	
					for (var i = numCandidates; i --> 0;)
					{
						var candidate = candidatesStack[baseIndex + i];
						if (candidate.NumTypeParameters == 0)
							continue;

						candidate = MethodGroupDefinition.InferMethodTypeArguments(candidate, numArguments, null);
						if (candidate == null)
							candidatesStack.RemoveAt(baseIndex + i);
						else
							candidatesStack[baseIndex + i] = candidate;
					}
					numCandidates = candidatesStack.Count - baseIndex;
				
					resolved = MethodGroupDefinition.ResolveMethodOverloads(numArguments, numCandidates);
				
					if (candidatesStack.Count > baseIndex)
						candidatesStack.RemoveRange(baseIndex, candidatesStack.Count - baseIndex);
				}
				
				if (resolved != null && resolved.kind != SymbolKind.Error)
				{
					MethodGroupDefinition.modifiersStack.RemoveRange(argsBaseIndex, numArguments);
					MethodGroupDefinition.argumentTypesStack.RemoveRange(argsBaseIndex, numArguments);
					MethodGroupDefinition.resolvedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
					MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
					MethodGroupDefinition.argumentNodesStack.RemoveRange(argsBaseIndex, numArguments);
					
					return resolved;
				}
			}

			extensionsMethods.Clear();
		}

		var importedNamespaces = declaration.importedNamespaces;
		for (var i = importedNamespaces.Count; i --> 0; )
		{
			var nsDef = importedNamespaces[i].definition as NamespaceDefinition;
			if (nsDef != null)
				thisAssembly.CollectExtensionMethods(nsDef, "Deconstruct", null, memberOf, extensionsMethods, context);
		}
		if (extensionsMethods.Count > 0)
		{
			if (firstAccessibleMethod == null)
				firstAccessibleMethod = extensionsMethods.First();

			if (numArguments == 0)
			{
				MethodGroupDefinition.modifiersStack.Add(Modifiers.This);
				MethodGroupDefinition.argumentTypesStack.Add(memberOf);
				MethodGroupDefinition.resolvedArgumentsStack.Add(null);//extendedType.GetThisInstance());
				MethodGroupDefinition.namedArgumentsStack.Add(null);
				MethodGroupDefinition.argumentNodesStack.Add(null);

				numArguments = numOutParameters + 1;
					
				for (var i = 0; i < numOutParameters; ++i)
				{
					MethodGroupDefinition.modifiersStack.Add(Modifiers.Out);
					MethodGroupDefinition.argumentTypesStack.Add(null);
					MethodGroupDefinition.resolvedArgumentsStack.Add(null);//extendedType.GetThisInstance());
					MethodGroupDefinition.namedArgumentsStack.Add(null);
					MethodGroupDefinition.argumentNodesStack.Add(null);
				}
			}
				
			MethodDefinition resolved = null;
			{
				var candidatesStack = MethodGroupDefinition.methodCandidatesStack;
				int baseIndex = candidatesStack.Count;
				foreach (var method in extensionsMethods)
					if (method.CanCallWith(numArguments, true))
						candidatesStack.Add(method);
				int numCandidates = candidatesStack.Count - baseIndex;
	
				for (var i = numCandidates; i --> 0; )
				{
					var candidate = candidatesStack[baseIndex + i];
					if (candidate.NumTypeParameters == 0 || numArguments == 0)
						continue;

					candidate = MethodGroupDefinition.InferMethodTypeArguments(candidate, numArguments, null);
					if (candidate == null)
						candidatesStack.RemoveAt(baseIndex + i);
					else
						candidatesStack[baseIndex + i] = candidate;
				}
				numCandidates = candidatesStack.Count - baseIndex;
				
				resolved = MethodGroupDefinition.ResolveMethodOverloads(numArguments, numCandidates);
				
				if (candidatesStack.Count > baseIndex)
					candidatesStack.RemoveRange(baseIndex, candidatesStack.Count - baseIndex);
			}
			
			if (resolved != null && resolved.kind != SymbolKind.Error)
			{
				MethodGroupDefinition.modifiersStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.argumentTypesStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.resolvedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.argumentNodesStack.RemoveRange(argsBaseIndex, numArguments);
				
				return resolved;
			}
		}
		
		MethodGroupDefinition.modifiersStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.argumentTypesStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.resolvedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.argumentNodesStack.RemoveRange(argsBaseIndex, numArguments);
		
		return base.FindDeconstructExtensionMethod(memberOf, numOutParameters, context);
	}

	public override SymbolDefinition ResolveAsExtensionMethod(CharSpan id, TypeDefinitionBase memberOf, ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope context, ParseTree.Leaf invokedLeaf = null)
	{
		var thisAssembly = GetAssembly();
		if (thisAssembly == null)
			return null;
		
		var extensionsMethods = new HashSet<MethodDefinition>();
		
		MethodDefinition namedArgsError = null;
		int numArguments = 0;
		int argsBaseIndex = MethodGroupDefinition.modifiersStack.Count;

		var parentNSScope = parentScope as NamespaceScope;
		var parentNSDef = parentNSScope != null ? parentNSScope.definition : null;
		for (var nsDef = definition;
			nsDef != null && nsDef != parentNSDef;
			nsDef = nsDef.parentSymbol as NamespaceDefinition)
		{
			thisAssembly.CollectExtensionMethods(nsDef, id, typeArgs, memberOf, extensionsMethods, context);
			if (extensionsMethods.Count > 0)
			{
				foreach (var method in extensionsMethods)
				{
					if (firstAccessibleMethod == null)
					{
						firstAccessibleMethod = method;
						continue;
					}
					
					var targetOfFirst = firstAccessibleMethod.GetParameters()[0].TypeOf() as TypeDefinitionBase;
					if (targetOfFirst == memberOf)
						break;
					
					var targetOfCurrent = method.GetParameters()[0].TypeOf() as TypeDefinitionBase;
					if (targetOfCurrent == null)
						continue;
					
					if (targetOfCurrent == memberOf || targetOfFirst == null)
					{
						firstAccessibleMethod = method;
						break;
					}
					
					if (targetOfCurrent.DerivesFrom(targetOfFirst))
						firstAccessibleMethod = method;
				}
				
				if (numArguments == 0)
				{
					numArguments = MethodGroupDefinition.ProcessArgumentListNode(argumentListNode, memberOf);
					namedArgsError = MethodGroupDefinition.CheckNamedArguments(numArguments);
				}
				
				MethodDefinition resolved = namedArgsError;
				if (resolved == null)
				{
					var candidatesStack = MethodGroupDefinition.methodCandidatesStack;
					int baseIndex = candidatesStack.Count;
					foreach (var method in extensionsMethods)
						if (numArguments == 0 || method.CanCallWith(numArguments, true))
							candidatesStack.Add(method);
					int numCandidates = candidatesStack.Count - baseIndex;
	
					if (typeArgs == null)
					{
						for (var i = numCandidates; i --> 0;)
						{
							var candidate = candidatesStack[baseIndex + i];
							if (candidate.NumTypeParameters == 0 || numArguments == 0)
								continue;
	
							candidate = MethodGroupDefinition.InferMethodTypeArguments(candidate, numArguments, invokedLeaf);
							if (candidate == null)
								candidatesStack.RemoveAt(baseIndex + i);
							else
								candidatesStack[baseIndex + i] = candidate;
						}
					}
					else
					{
						for (var i = numCandidates; i-- > 0; )
						{
							var candidate = candidatesStack[baseIndex + i];
							if (candidate.NumTypeParameters != 0)
								continue;

							candidatesStack.RemoveAt(baseIndex + i);
						}
					}
					numCandidates = candidatesStack.Count - baseIndex;
				
					resolved = MethodGroupDefinition.ResolveMethodOverloads(numArguments, numCandidates);
				
					if (candidatesStack.Count > baseIndex)
						candidatesStack.RemoveRange(baseIndex, candidatesStack.Count - baseIndex);
				}
				
				if (resolved != null && resolved.kind != SymbolKind.Error)
				{
					MethodGroupDefinition.modifiersStack.RemoveRange(argsBaseIndex, numArguments);
					MethodGroupDefinition.argumentTypesStack.RemoveRange(argsBaseIndex, numArguments);
					MethodGroupDefinition.resolvedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
					MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
					MethodGroupDefinition.argumentNodesStack.RemoveRange(argsBaseIndex, numArguments);
					
					return resolved;
				}
			}

			extensionsMethods.Clear();
		}

		var importedNamespaces = declaration.importedNamespaces;
		for (var i = importedNamespaces.Count; i --> 0; )
		{
			var nsDef = importedNamespaces[i].definition as NamespaceDefinition;
			if (nsDef != null)
				thisAssembly.CollectExtensionMethods(nsDef, id, typeArgs, memberOf, extensionsMethods, context);
		}
		if (extensionsMethods.Count > 0)
		{
			foreach (var method in extensionsMethods)
			{
				if (firstAccessibleMethod == null)
				{
					firstAccessibleMethod = method;
					continue;
				}
					
				var targetOfFirst = firstAccessibleMethod.GetParameters()[0].TypeOf() as TypeDefinitionBase;
				if (targetOfFirst == memberOf)
					break;
					
				var targetOfCurrent = method.GetParameters()[0].TypeOf() as TypeDefinitionBase;
				if (targetOfCurrent == null)
					continue;
					
				if (targetOfCurrent == memberOf || targetOfFirst == null)
				{
					firstAccessibleMethod = method;
					break;
				}
					
				if (targetOfCurrent.DerivesFrom(targetOfFirst))
					firstAccessibleMethod = method;
			}
			
			if (numArguments == 0)
			{
				numArguments = MethodGroupDefinition.ProcessArgumentListNode(argumentListNode, memberOf);
				namedArgsError = MethodGroupDefinition.CheckNamedArguments(numArguments);
			}
				
			MethodDefinition resolved = namedArgsError;
			if (resolved == null)
			{
				var candidatesStack = MethodGroupDefinition.methodCandidatesStack;
				int baseIndex = candidatesStack.Count;
				foreach (var method in extensionsMethods)
					if (numArguments == 0 || method.CanCallWith(numArguments, true))
						candidatesStack.Add(method);
				int numCandidates = candidatesStack.Count - baseIndex;
	
				if (typeArgs == null)
				{
					for (var i = numCandidates; i --> 0; )
					{
						var candidate = candidatesStack[baseIndex + i];
						if (candidate.NumTypeParameters == 0 || numArguments == 0)
							continue;
	
						candidate = MethodGroupDefinition.InferMethodTypeArguments(candidate, numArguments, invokedLeaf);
						if (candidate == null)
							candidatesStack.RemoveAt(baseIndex + i);
						else
							candidatesStack[baseIndex + i] = candidate;
					}
				}
				else
				{
					for (var i = numCandidates; i-- > 0; )
					{
						var candidate = candidatesStack[baseIndex + i];
						if (candidate.NumTypeParameters != 0)
							continue;

						candidatesStack.RemoveAt(baseIndex + i);
					}
				}
				numCandidates = candidatesStack.Count - baseIndex;
				
				resolved = MethodGroupDefinition.ResolveMethodOverloads(numArguments, numCandidates);
				
				if (candidatesStack.Count > baseIndex)
					candidatesStack.RemoveRange(baseIndex, candidatesStack.Count - baseIndex);
			}
			
			if (resolved != null && resolved.kind != SymbolKind.Error)
			{
				MethodGroupDefinition.modifiersStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.argumentTypesStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.resolvedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
				MethodGroupDefinition.argumentNodesStack.RemoveRange(argsBaseIndex, numArguments);
				
				return resolved;
			}
		}
		
		MethodGroupDefinition.modifiersStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.argumentTypesStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.resolvedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, numArguments);
		MethodGroupDefinition.argumentNodesStack.RemoveRange(argsBaseIndex, numArguments);
		
		if (parentScope != null)
		{
			var resolved = parentScope.ResolveAsExtensionMethod(id, memberOf, argumentListNode, typeArgs, context, invokedLeaf);
			if (resolved != null)
				return resolved;
		}
		
		return null;
	}
	
	public override SymbolDefinition FindName(CharSpan symbolName, int numTypeParameters)
	{
		return definition.FindName(symbolName, numTypeParameters, true);
	}

	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		if (definition != null)
			definition.GetMembersCompletionData(data, BindingFlags.NonPublic, AccessLevelMask.Any, context);
		
		if (!context.fromInstance || context.completionNode == null || context.completionNode.parent == null
			|| context.completionNode.parent.RuleName != "usingDirective")
		{
			foreach (var ta in declaration.usingAliases)
				if (!data.ContainsKey(ta.name))
					data.Add(ta.name, ta);

			foreach (var i in declaration.importedNamespaces)
			{
				var nsDef = i.definition as NamespaceDefinition;
				if (nsDef != null)
					nsDef.GetTypesOnlyCompletionData(data, AccessLevelMask.Any, context.assembly);
			}
		
			foreach (var i in declaration.importedStaticTypes)
			{
				var typeDef = i.definition as TypeDefinitionBase;
				if (typeDef == null)
					continue;
				typeDef.GetCompletionDataFromImportedType(data, AccessLevelMask.Any, context);
			}
		}
		
		if (parentScope != null && definition != null)
		{
			var parentScopeDef = ((NamespaceScope) parentScope).definition;
			if (parentScopeDef != null)
			{
				for (var nsDef = definition.parentSymbol;
					nsDef != null && nsDef != parentScopeDef;
					nsDef = nsDef.parentSymbol as NamespaceDefinition)
				{
					nsDef.GetCompletionData(data, context);
				}
			}
		}
		
		bool oldFromInstance = context.fromInstance;
		context.fromInstance = false;
		base.GetCompletionData(data, context);
		context.fromInstance = oldFromInstance;
	}

	public override TypeDefinition EnclosingType()
	{
		return null;
	}

	public override void GetExtensionMethodsCompletionData(TypeDefinitionBase forType, Dictionary<string, SymbolDefinition> data, TypeDefinitionBase contextType)
	{
//	Debug.Log("Extensions for " + forType.GetTooltipText());
		var assembly = this.GetAssembly();
		
		assembly.GetExtensionMethodsCompletionData(forType, definition, data, contextType);
		foreach (var nsRef in declaration.importedNamespaces)
		{
			var ns = nsRef.definition as NamespaceDefinition;
			if (ns != null)
				assembly.GetExtensionMethodsCompletionData(forType, ns, data, contextType);
		}
		
		if (parentScope != null)
		{
			var parentScopeDef = ((NamespaceScope) parentScope).definition;
			for (var nsDef = definition.parentSymbol as NamespaceDefinition;
				nsDef != null && nsDef != parentScopeDef;
				nsDef = nsDef.parentSymbol as NamespaceDefinition)
			{
				assembly.GetExtensionMethodsCompletionData(forType, nsDef, data, contextType);
			}

			parentScope.GetExtensionMethodsCompletionData(forType, data, contextType);
		}
	}
}

public class AttributesScope : Scope
{
	public AttributesScope(ParseTree.Node node) : base(node) {}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		Debug.LogException(new InvalidOperationException());
		return null;
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		Debug.LogException(new InvalidOperationException());
	}

	public override SymbolDefinition FindName(CharSpan symbolName, int numTypeParameters)
	{
		var result = parentScope.FindName(symbolName, numTypeParameters);
		return result;
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs, bool asTypeOnly)
	{
		leaf.resolvedSymbol = null;
		base.Resolve(leaf, numTypeArgs, asTypeOnly);

		if (leaf.resolvedSymbol == null || leaf.resolvedSymbol == SymbolDefinition.unknownSymbol)
		{
			if (leaf.parent.RuleName == "typeOrGeneric" && leaf.parent.parent.parent.parent.RuleName == "attribute" &&
				leaf.parent.childIndex == leaf.parent.parent.numValidNodes - 1)
			{
				var old = leaf.token.text;
				leaf.token.SetText(old + "Attribute");
				leaf.resolvedSymbol = null;
				base.Resolve(leaf, numTypeArgs, true);
				leaf.token.SetText(old);
			}
		}

		//if (leaf.resolvedSymbol == SymbolDefinition.unknownSymbol)
		//	Debug.LogError(leaf);
	}
}

public class MemberInitializerScope : Scope
{
	public MemberInitializerScope(ParseTree.Node node) : base(node)	{}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs, bool asTypeOnly)
	{
		leaf.resolvedSymbol = null;
		if (numTypeArgs == 0 && !asTypeOnly)
		{
			ParseTree.BaseNode target = null;
	
			if (leaf.childIndex == 0 && leaf.parent != null && leaf.parent.parent == parseTreeNode)
			{
				var node = parseTreeNode // memberInitializerList
					.parent // objectInitializer
					.parent // objectOrCollectionInitializer
					.parent;
				if (node.RuleName == "objectCreationExpression")
				{
					target = node.parent.NodeAt(1); // nonArrayType in a primaryExpression node
				}
				else if (node.RuleName == "targetTypedNewExpression")
				{
					target = node.parent.LeafAt(0); // new(...) in a primaryExpression node
				}
				else // node is a memberInitializer node
				{
					target = node.LeafAt(0); // IDENTIFIER in a memberInitializer node
				}
	
				if (target != null)
				{
					var targetSymbol = target.resolvedSymbol;
					if (targetSymbol == null)
						targetSymbol = SymbolDefinition.ResolveNode(target, parentScope);
					if (targetSymbol != null)
						targetSymbol = targetSymbol.TypeOf();

					if (targetSymbol != null)
						targetSymbol.ResolveMember(leaf, parentScope, 0, false);
					return;
				}
			}
		}
		
		base.Resolve(leaf, numTypeArgs, asTypeOnly);
	}

	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		var baseNode = context.completionNode;

		if (baseNode.parent != null && (baseNode.parent == parseTreeNode || baseNode.childIndex == 0 && baseNode.parent.parent == parseTreeNode))
		{
			SymbolDefinition target = null;
			ParseTree.BaseNode targetNode = null;

			var node = parseTreeNode // memberInitializerList
				.parent // objectInitializer
				.parent // objectOrCollectionInitializer
				.parent;
			if (node.RuleName == "objectCreationExpression")
			{
				targetNode = node.parent;
				target = SymbolDefinition.ResolveNode(targetNode); // nonArrayType in a primaryExpression node
				var targetAsType = target as TypeDefinitionBase;
				if (targetAsType != null)
					target = targetAsType.GetThisInstance();
			}
			else if (node.RuleName == "targetTypedNewExpression")
			{
				targetNode = node.parent;
				target = SymbolDefinition.ResolveNode(targetNode); // new(...) in a primaryExpression node
				var targetAsType = target as TypeDefinitionBase;
				if (targetAsType != null)
					target = targetAsType.GetThisInstance();
			}
			else // parent is a memberInitializer node
			{
				targetNode = node.parent.LeafAt(0);
				target = SymbolDefinition.ResolveNode(node.parent.LeafAt(0)); // IDENTIFIER in a memberInitializer node
			}

			if (target != null)
			{
				HashSet<SymbolDefinition> completions = new HashSet<SymbolDefinition>();
				FGResolver.GetCompletions(IdentifierCompletionsType.Member, targetNode, completions, completionAssetPath);
				foreach (var symbol in completions)
					data.Add(symbol.name, symbol);
			}
		}
		else
		{
			base.GetCompletionData(data, context);
		}
	}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		return parentScope.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		parentScope.RemoveDeclaration(symbol);
	}

	public override SymbolDefinition FindName(CharSpan symbolName, int numTypeParameters)
	{
		throw new InvalidOperationException("Calling FindName on MemberInitializerScope is not allowed!");
	}
}

public class BaseScope : Scope
{
	public BaseScope(ParseTree.Node node) : base(node) {}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		return parentScope.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		parentScope.RemoveDeclaration(symbol);
	}

	public override SymbolDefinition FindName(CharSpan symbolName, int numTypeParameters)
	{
		return parentScope.FindName(symbolName, numTypeParameters);
	}
}

public class LocalScope : Scope
{
	protected List<SymbolDefinition> localSymbols;

	public LocalScope(ParseTree.Node node) : base(node) {}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		symbol.scope = this;
		if (localSymbols == null)
			localSymbols = new List<SymbolDefinition>();

		//var name = symbol.Name;

	//	Debug.Log("Adding localSymbol " + name);
		var definition = SymbolDefinition.Create(symbol);
	//	var oldDefinition = (from ls in localSymbols where ls.Value.declarations[0].parseTreeNode.parent == symbol.parseTreeNode.parent select ls.Key).FirstOrDefault();
	//	if (oldDefinition != null)
	//		Debug.LogWarning(oldDefinition);
		localSymbols.Add(definition);

		return definition;
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (localSymbols != null)
		{
			for (var i = localSymbols.Count; i --> 0;)
			{
				var x = localSymbols[i];
				if (!x.ListRemoveDeclaration(symbol))
					continue;
				if (x.declarations == null)
					localSymbols.RemoveAt(i);
			}
		}
		symbol.definition = null;
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs, bool asTypeOnly)
	{
		leaf.resolvedSymbol = null;

		if (!asTypeOnly && localSymbols != null)
		{
			var id = SymbolDefinition.DecodeId(leaf.token.text);
			for (var i = localSymbols.Count; i --> 0; )
			{
				if (localSymbols[i].name == id)
				{
					leaf.resolvedSymbol = localSymbols[i];
					if (leaf.resolvedSymbol.kind == SymbolKind.Method && leaf.parent.RuleName == "primaryExpressionStart" && leaf.parent.nextSibling == null)
					{
						// Temporary method group for this local method.
						var methodGroup = new MethodGroupDefinition { kind = SymbolKind.MethodGroup, name = leaf.resolvedSymbol.name };
						methodGroup.methods.Add(leaf.resolvedSymbol as MethodDefinition);
						leaf.resolvedSymbol = methodGroup;
					}
					return;
				}
			}
		}

		base.Resolve(leaf, numTypeArgs, asTypeOnly);
	}

	public override SymbolDefinition FindName(CharSpan symbolName, int numTypeParameters)
	{
		symbolName.DecodeId();
		var hashID = symbolName.GetHashCode();
		
		if (numTypeParameters == 0 && localSymbols != null)
		{
			for (var i = localSymbols.Count; i --> 0; )
				if (localSymbols[i].hashID == hashID)
					return localSymbols[i];
		}
		return null;
	}

	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		if (localSymbols != null)
		{
			foreach (var ls in localSymbols)
			{
				SymbolDeclaration declaration = ls.declarations;
				ParseTree.Node declarationNode = declaration != null ? declaration.parseTreeNode : null;
				if (declarationNode == null)
					continue;
				var firstLeaf = declarationNode.GetFirstLeaf();
				if (firstLeaf != null &&
					(firstLeaf.line > context.completionAtLine ||
					firstLeaf.line == context.completionAtLine && firstLeaf.tokenIndex >= context.completionAtTokenIndex))
						continue;
				if (!data.ContainsKey(ls.name))
					data.Add(ls.name, ls);
			}
		}
		base.GetCompletionData(data, context);
	}
}

public class SwitchSectionScope : LocalScope
{
	public SwitchSectionScope(ParseTree.Node node) : base(node) {}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.CaseVariable)
			return base.AddDeclaration(symbol);
		else
			return parentScope.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.CaseVariable)
			base.RemoveDeclaration(symbol);
		else
			parentScope.RemoveDeclaration(symbol);
	}
}

public class AttributeArgumentsScope : LocalScope
{
	public AttributeArgumentsScope(ParseTree.Node node) : base(node) {}
	
	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		var attributeTypeLeaf = parseTreeNode.parent.parent.NodeAt(0).GetLastLeaf();
		if (attributeTypeLeaf != null)
		{
			var attributeType = attributeTypeLeaf.resolvedSymbol as TypeDefinitionBase;
			if (attributeType != null)
			{
				var tempData = new Dictionary<string, SymbolDefinition>();
				attributeType.GetMembersCompletionData(tempData, BindingFlags.Instance, AccessLevelMask.Public | AccessLevelMask.Internal, context);
				foreach (var kv in tempData)
				{
					var symbolKind = kv.Value.kind;
					if (symbolKind == SymbolKind.Field || symbolKind == SymbolKind.Property)
						if (!data.ContainsKey(kv.Key))
							data[kv.Key] = kv.Value;
				}
			}
		}
		base.GetCompletionData(data, context);
	}
}

public class AccessorBodyScope : BodyScope
{
	private ValueParameter _value;
	private ValueParameter Value {
		get {
			if (_value == null || !_value.IsValid())
			{
				/*var valueType =*/ definition.parentSymbol.TypeOf();
				_value = new ValueParameter
				{
					name = "value",
					kind = SymbolKind.Parameter,
					parentSymbol = definition,
					type = ((InstanceDefinition) definition.parentSymbol).type,
				};
			}
			return _value;
		}
	}
	
	public AccessorBodyScope(ParseTree.Node node) : base(node) {}

	public override SymbolDefinition FindName(CharSpan symbolName, int numTypeParameters)
	{
		if (numTypeParameters == 0 && symbolName == "value" && definition.name != "get")
		{
			return Value;
		}

		return base.FindName(symbolName, numTypeParameters);
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs, bool asTypeOnly)
	{
		if (!asTypeOnly && numTypeArgs == 0 && leaf.token.text == "value" && definition.name != "get")
		{
			leaf.resolvedSymbol = Value;
			return;
		}

		base.Resolve(leaf, numTypeArgs, asTypeOnly);
	}

	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		if (definition.name != "get")
			data["value"] = Value;
		definition.parentSymbol.GetCompletionData(data, context);
		base.GetCompletionData(data, context);
	}
}

internal static class TypeInference
{
	static List<ParseTree.BaseNode> expressionNodes = new List<ParseTree.BaseNode>(64);
	static List<SymbolDefinition> resolvedExpressions = new List<SymbolDefinition>(64);
	static List<TypeDefinitionBase> expressionTypes = new List<TypeDefinitionBase>(64);

	public struct BestCommonTypeResolver
	{
		Scope scope;
		
		int nodesBaseIndex;
		
		public BestCommonTypeResolver(Scope scope)
		{
			this.scope = scope;
			
			nodesBaseIndex = expressionNodes.Count;
		}
		
		public void AddExpressionNode(ParseTree.BaseNode baseNode)
		{
			var resolved = SymbolDefinition.ResolveNode(baseNode, scope);
			var type = resolved != null ? resolved.TypeOf() as TypeDefinitionBase : null;
			
			expressionNodes.Add(baseNode);
			resolvedExpressions.Add(resolved);	
			expressionTypes.Add(type ?? SymbolDefinition.unknownType);
		}
		
		public TypeDefinitionBase Resolve()
		{
			var nodesCount = expressionNodes.Count - nodesBaseIndex;
			if (nodesCount == 0)
				return SymbolDefinition.builtInTypes_void;
			
			TypeDefinitionBase result = null;
			
			for (var i = 0; i < nodesCount; ++i)
			{
				var typeUe = expressionTypes[nodesBaseIndex + i];
				for (var j = 0; j < nodesCount; ++j)
				{
					if (i == j)
						continue;
					var typeU = expressionTypes[nodesBaseIndex + j];
					//if (typeU == SymbolDefinition.unknownType)
					//{
					//	var node = expressionNodes[nodesBaseIndex + j] as ParseTree.Node;
					//	if (node != null)
					//	{
					//		var firstLeaf = node.GetFirstLeaf(true);
					//		if (firstLeaf != null && firstLeaf.IsLit("default") && firstLeaf.parent.numValidNodes == 1)
					//		{
					//			continue;
					//		}
					//	}
					//}
					if (!typeU.IsSameType(typeUe))
					{
						if (!typeU.CanConvertTo(typeUe))
						{
							typeUe = null;
							break;
						}
					}
					else if (typeU == SymbolDefinition.builtInTypes_dynamic && typeU != null)
					{
						typeUe = SymbolDefinition.builtInTypes_dynamic;
						break;
					}
				}
				if (typeUe == null)
					continue;
				if (result == null)
				{
					result = typeUe;
				}
				else if (!typeUe.TypeOf().IsSameType(result.TypeOf() as TypeDefinitionBase)) // Handles integer literals
				{
					result = SymbolDefinition.unknownType;
					break;
				}
			}
			
			//inferedTypes.Add(SymbolDefinition.unknownType);
			
			expressionNodes.RemoveRange(nodesBaseIndex, nodesCount);
			resolvedExpressions.RemoveRange(nodesBaseIndex, nodesCount);
			expressionTypes.RemoveRange(nodesBaseIndex, nodesCount);
			
			//var result = inferedTypes[typesBaseIndex];
			//inferedTypes.RemoveAt(typesBaseIndex);
			
			return result;
		}
		
		public SymbolDefinition ResolveReturnTypeOfStatementList(ParseTree.Node statementListNode)
		{
			ReturnTypeOfStatementList(statementListNode);
			
			var returnType = Resolve() as TypeDefinitionBase;
			if (returnType != null)
				return returnType.GetThisInstance();
			return null;
		}
		
		private void ReturnTypeOfStatement(ParseTree.Node statementNode)
		{
			var childNode = statementNode.firstChild as ParseTree.Node;
			if (childNode == null)
				return;
			
			if (childNode.RuleName == "embeddedStatement")
			{
				ReturnTypeOfEmbeddedStatement(childNode);
			}
			else if (childNode.RuleName == "labeledStatement")
			{
				statementNode = childNode.NodeAt(2);
				if (statementNode != null)
					ReturnTypeOfStatement(statementNode);
			}
		}
	
		private void ReturnTypeOfStatementList(ParseTree.Node statementListNode)
		{
			var statementNode = statementListNode != null ? statementListNode.firstChild as ParseTree.Node : null;
			for ( ; statementNode != null; statementNode = statementNode.nextSibling as ParseTree.Node)
			{
				ReturnTypeOfStatement(statementNode);
			}
		}

		private void ReturnTypeOfEmbeddedStatement(ParseTree.Node embeddedStatementNode)
		{
			var firstNode = embeddedStatementNode.firstChild as ParseTree.Node;
			if (firstNode == null)
			{
				return;
			}
			
			if (firstNode.RuleName == "lockStatement" ||
				firstNode.RuleName == "usingStatement" ||
				firstNode.RuleName == "fixedStatement")
			{
				embeddedStatementNode = firstNode.NodeAt(4);
				if (embeddedStatementNode != null)
				{
					ReturnTypeOfEmbeddedStatement(embeddedStatementNode);
				}
				return;
			}
			
			if (firstNode.RuleName == "jumpStatement")
			{
				var returnStatementNode = firstNode.firstChild as ParseTree.Node;
				if (returnStatementNode != null && returnStatementNode.RuleName == "returnStatement")
				{
					var expressionNode = returnStatementNode.NodeAt(1) ?? returnStatementNode.NodeAt(2);
					if (expressionNode != null)
					{
						AddExpressionNode(expressionNode);
					}
				}
				return;
			}
			
			if (firstNode.RuleName == "unsafeStatement" ||
				firstNode.RuleName == "uncheckedStatement" ||
				firstNode.RuleName == "checkedStatement")
			{
				firstNode = firstNode.NodeAt(1); // block
				if (firstNode == null)
				{
					return;
				}
				// Fall through
			}
			
			if (firstNode.RuleName == "block")
			{
				var statementListNode = firstNode.NodeAt(1);
				if (statementListNode != null)
				{
					ReturnTypeOfStatementList(statementListNode);
				}
				return;
			}
			
			if (firstNode.RuleName == "tryStatement")
			{
				var blockNode = firstNode.NodeAt(1);
				if (blockNode != null)
				{
					var statementListNode = firstNode.NodeAt(1);
					if (statementListNode != null)
					{
						ReturnTypeOfStatementList(statementListNode);
					}
					
					var nextNode = blockNode.nextSibling as ParseTree.Node;
					if (nextNode != null && nextNode.RuleName == "catchClauses")
					{
						nextNode = nextNode.nextSibling as ParseTree.Node;
					}
					if (nextNode != null) // finallyClause
					{
						blockNode = nextNode.NodeAt(1);
						if (blockNode != null)
						{
							statementListNode = firstNode.NodeAt(1);
							if (statementListNode != null)
							{
								ReturnTypeOfStatementList(statementListNode);
							}
						}
					}
				}
				return;
			}
			
			if (firstNode.RuleName == "iterationStatement")
			{
				var loopStatementNode = firstNode.firstChild as ParseTree.Node;
				if (loopStatementNode == null)
					return;
				
				embeddedStatementNode = loopStatementNode.FindChildByName("embeddedStatement") as ParseTree.Node;
				if (embeddedStatementNode != null)
				{
					ReturnTypeOfEmbeddedStatement(embeddedStatementNode);
				}
				return;
			}
			
			if (firstNode.RuleName == "selectionStatement")
			{
				var ifOrSwitchStatementNode = firstNode.firstChild as ParseTree.Node;
				if (ifOrSwitchStatementNode == null)
					return;
				
				if (ifOrSwitchStatementNode.RuleName == "ifStatement")
				{
					embeddedStatementNode = ifOrSwitchStatementNode.NodeAt(4);
					if (embeddedStatementNode != null)
					{
						ReturnTypeOfEmbeddedStatement(embeddedStatementNode);
						
						var elseStatementNode = embeddedStatementNode.nextSibling as ParseTree.Node;
						if (elseStatementNode != null)
						{
							embeddedStatementNode = elseStatementNode.NodeAt(1);
							if (embeddedStatementNode != null)
							{
								ReturnTypeOfEmbeddedStatement(embeddedStatementNode);
							}
						}
					}
					return;
				}

				// switchStatement
				var switchBlockNode = ifOrSwitchStatementNode.NodeAt(4);
				if (switchBlockNode == null)
					return;
				var switchSectionNode = switchBlockNode.NodeAt(1);
				for ( ; switchSectionNode != null; switchSectionNode = switchSectionNode.nextSibling as ParseTree.Node)
				{
					var statementNode = switchSectionNode.FindChildByName("statement") as ParseTree.Node;
					if (statementNode == null)
						continue;
					ReturnTypeOfStatement(statementNode);
					
					var statementListNode = statementNode.nextSibling as ParseTree.Node;
					if (statementListNode == null)
						continue;
					ReturnTypeOfStatementList(statementListNode);
				}
				return;
			}
			
			return;
		}
	}
}

internal struct FlatExpressionResolver
{
	ParseTree.BaseNode node;
	Scope scope;
	int numNodesLeft;
	int minPrecedence;
	//int baseOperatorsIndex;
	//int baseOperandsIndex;
	
	//static Stack<int> operators = new Stack<int>(256);
	//static Stack<SymbolDefinition> operands = new Stack<SymbolDefinition>(256);
	
	public FlatExpressionResolver(ParseTree.Node flatExpressionNode, Scope scope)
	{
		this.node = flatExpressionNode.firstChild;
		this.scope = scope;
		numNodesLeft = flatExpressionNode.numValidNodes;
		minPrecedence = 0;
		//baseOperatorsIndex = operators.Count;
		//baseOperandsIndex = operands.Count;
	}
	
	//private void CleanUp()
	//{
	//	while (baseOperatorsIndex > operators.Count)
	//		operators.Pop();
	//	while (baseOperandsIndex > operands.Count)
	//		operands.Pop();
	//}
	
	public SymbolDefinition Resolve()
	{
		var lhs = ResolvePrimary();
		if (lhs == null || lhs.kind == SymbolKind.Error || node == null)
			return lhs;
		
		var result = ResolvePart(lhs, minPrecedence);
		
		//CleanUp();
		
		return result;
	}
	
	private SymbolDefinition ResolvePart(SymbolDefinition lhs, int minPrecedence)
	{
		var savedNode = node;
		var savedNumValidNodes = numNodesLeft;

		Operator.ID operatorID;
		int precedence;
		bool isLeftAssociativity;
		while ((operatorID = ParseOperator(out precedence, out isLeftAssociativity)) != Operator.ID.None)
		{
			var operatorNode = savedNode;
			
			if (precedence < minPrecedence)
			{
				node = savedNode;
				numNodesLeft = savedNumValidNodes;
				break;
			}

			var rhs = operatorID == Operator.ID.op_Is && !CsParser.isCSharp9 || operatorID == Operator.ID.op_As ? ResolveType() : ResolvePrimary();
			if (rhs == null)
			{
				if (operatorID == Operator.ID.op_Range)
					rhs = SymbolDefinition.builtInTypes_Index.GetThisInstance();
				else
					return lhs;
			}
			else if (rhs.kind == SymbolKind.Error)
			{
				return rhs;
			}

			savedNode = node;
			savedNumValidNodes = numNodesLeft;

			int nextPrecedence;
			bool nextIsLeftAssociativity;
			while ((ParseOperator(out nextPrecedence, out nextIsLeftAssociativity)) != Operator.ID.None)
			{
				node = savedNode;
				numNodesLeft = savedNumValidNodes;

				if (nextPrecedence < precedence || nextIsLeftAssociativity && nextPrecedence == precedence)
					break;

				rhs = ResolvePart(rhs, precedence + (nextPrecedence > precedence ? 1 : 0));
				if (rhs == null || rhs.kind == SymbolKind.Error)
					break;

				savedNode = node;
				savedNumValidNodes = numNodesLeft;
			}

			if (rhs == null || rhs.kind == SymbolKind.Error)
			{
				lhs = rhs;
				break;
			}
			
			if (operatorID == Operator.ID.op_Is)
			{
				lhs = SymbolDefinition.builtInTypes_bool.GetThisInstance();
			}
			else if (operatorID == Operator.ID.op_As)
			{
				var asType = rhs as TypeDefinitionBase;
				if (asType == null)
					break;
				
				lhs = asType.GetThisInstance();
			}
			else if (operatorID == Operator.ID.op_NullCoalescing)
			{
				var lhsType = lhs.TypeOf() as TypeDefinitionBase;
				if (lhsType == null || lhs.kind == SymbolKind.Error)
					break;
				
				var rhsType = rhs.TypeOf() as TypeDefinitionBase;
				if (rhsType == null || rhs.kind == SymbolKind.Error)
				{
					lhs = rhs;
					break;
				}
				
				var asNullable = lhsType.GetGenericSymbol() == SymbolDefinition.builtInTypes_Nullable ? lhsType as ConstructedTypeDefinition : null;
				var underlyingType = asNullable != null ? asNullable.GetTypeArgument(0) : null;
				
				if (underlyingType == null && !lhsType.IsReferenceType)
				{
					operatorNode.semanticError = "Operator '??' cannot be applied to operands of type '" + lhsType.GetName() + "' and '" + rhsType.GetName() + "'";
					lhs = SymbolDefinition.unknownType;
					break;
				}

				if (SymbolDefinition.builtInTypes_dynamic != null &&
					(lhsType == SymbolDefinition.builtInTypes_dynamic || rhsType == SymbolDefinition.builtInTypes_dynamic))
				{
					lhs = SymbolDefinition.builtInTypes_dynamic.GetThisInstance();
				}
				else if (underlyingType != null && rhsType.CanConvertTo(underlyingType))
				{
					lhs = underlyingType.GetThisInstance();
				}
				else if (!(lhsType is NullTypeDefinition) && rhsType.CanConvertTo(lhsType))
				{
					// lhs = lhs;
				}
				else if (underlyingType != null && underlyingType.CanConvertTo(rhsType))
				{
					lhs = rhs;
				}
				else if (!(rhsType is NullTypeDefinition) && lhsType.CanConvertTo(rhsType))
				{
					lhs = rhs;
				}
				else
				{
					operatorNode.semanticError = "Operator '??' cannot be applied to operands of type '" + lhsType.GetName() + "' and '" + rhsType.GetName() + "'";
					lhs = SymbolDefinition.unknownType;
					break;
				}
			}
			else if (operatorID == Operator.ID.op_SwitchExpression)
			{
				lhs = rhs;
				if (lhs == null || lhs.kind == SymbolKind.Error)
					break;
			}
			else if (operatorID == Operator.ID.op_Range)
			{
				lhs = SymbolDefinition.builtInTypes_Range.GetThisInstance();
			}
			else
			{
				if (SymbolDefinition.builtInTypes_dynamic != null &&
					(lhs.TypeOf() == SymbolDefinition.builtInTypes_dynamic || rhs.TypeOf() == SymbolDefinition.builtInTypes_dynamic))
				{
					lhs = SymbolDefinition.builtInTypes_dynamic.GetThisInstance();
				}
				else
				{
					lhs = SymbolDefinition.ResolveBinaryOperation(operatorID, lhs, rhs, scope);
				}
				if (lhs == null || lhs.kind == SymbolKind.Error)
					break;
			}

			savedNode = node;
			savedNumValidNodes = numNodesLeft;
		}

		return lhs;
	}
	
	private SymbolDefinition ResolvePrimary()
	{
		var primaryNode = Advance();
		if (primaryNode == null)
			return null;
		if (primaryNode.IsLit(".") && primaryNode.nextSibling != null && primaryNode.nextSibling.IsLit("."))
		{
			var exprNode = node.parent.NodeAt(2);
			if (exprNode != null)
				SymbolDefinition.ResolveNode(exprNode, scope);
			return SymbolDefinition.builtInTypes_Range.GetThisInstance();
		}
		var result = SymbolDefinition.ResolveNode(primaryNode, scope);
		return result;
	}
	
	private SymbolDefinition ResolveType()
	{
		var typeNode = Advance();
		if (typeNode == null)
			return null;
		var asNode = typeNode as ParseTree.Node;
		if (asNode != null && asNode.RuleName == "isVariableDeclaration")
			typeNode = asNode.firstChild;
		var result = SymbolDefinition.ResolveNode(typeNode, scope, null, 0, true);
		return result;
	}
	
	private ParseTree.BaseNode Advance()
	{
		if (numNodesLeft == 0)
			return null;
		--numNodesLeft;
		var result = node;
		node = node.nextSibling;
		return result;
	}

	private Operator.ID ParseOperator(out int precedence, out bool leftAssociativity)
	{
		precedence = 0;
		leftAssociativity = true;
	
		if (node == null || numNodesLeft <= 0)
			return Operator.ID.None;
	
		var leaf = node as ParseTree.Leaf;
		if (leaf == null)
			return Operator.ID.None;
	
		var token = leaf.token;
		if (token == null)
			return Operator.ID.None;
	
		var text = token.text;
	
		Operator.ID operatorID = Operator.ID.None;
	
		if (text.length == 1)
		{
			var c = text[0];
			switch (c)
			{
			case '>':
				if (numNodesLeft < 2 || !node.nextSibling.IsLit(">"))
				{
					operatorID = Operator.ID.op_GreaterThan;
					precedence = 7;
					break;
				}
			
				node = node.nextSibling;
				--numNodesLeft;
			
				operatorID = Operator.ID.op_RightShift;
				precedence = 8;
				break;
			
			case '+':
				operatorID = Operator.ID.op_Addition;
				precedence = 9;
				break;
			
			case '-':
				operatorID = Operator.ID.op_Subtraction;
				precedence = 9;
				break;
			
			case '*':
				operatorID = Operator.ID.op_Multiply;
				precedence = 10;
				break;
			
			case '/':
				operatorID = Operator.ID.op_Division;
				precedence = 10;
				break;
			
			case '%':
				operatorID = Operator.ID.op_Modulus;
				precedence = 10;
				break;

			case '<':
				operatorID = Operator.ID.op_LessThan;
				precedence = 7;
				break;

			case '&':
				operatorID = Operator.ID.op_BitwiseAnd;
				precedence = 5;
				break;

			case '^':
				operatorID = Operator.ID.op_ExclusiveOr;
				precedence = 4;
				break;

			case '|':
				operatorID = Operator.ID.op_BitwiseOr;
				precedence = 3;
				break;
				
			default:
				return Operator.ID.None;
			}
		}
		else if (text.length == 2)
		{
			var c0 = text[0];
			var c1 = text[1];
			switch (c0)
			{
			case '&':
				if (c1 != '&')
					return Operator.ID.None;
				operatorID = Operator.ID.op_LogicalAnd;
				precedence = 2;
				break;
				
			case '|':
				if (c1 != '|')
					return Operator.ID.None;
				operatorID = Operator.ID.op_LogicalOr;
				precedence = 1;
				break;
				
			case '?':
				if (c1 != '?')
					return Operator.ID.None;
				operatorID = Operator.ID.op_NullCoalescing;
				precedence = 0;
				leftAssociativity = false;
				break;
				
			case '<':
				if (c1 == '=')
				{
					operatorID = Operator.ID.op_LessThanOrEqual;
					precedence = 7;
					break;
				}
				
				if (c1 != '<')
					return Operator.ID.None;
				operatorID = Operator.ID.op_LeftShift;
				precedence = 8;
				break;

			case '=':
				if (c1 != '=')
					return Operator.ID.None;
				operatorID = Operator.ID.op_Equality;
				precedence = 6;
				break;
				
			case '!':
				if (c1 != '=')
					return Operator.ID.None;
				operatorID = Operator.ID.op_Inequality;
				precedence = 6;
				break;
				
			case '>':
				if (c1 != '=')
					return Operator.ID.None;
				operatorID = Operator.ID.op_GreaterThanOrEqual;
				precedence = 7;
				break;
				
			case 'a':
				if (c1 != 's')
					return Operator.ID.None;
				operatorID = Operator.ID.op_As;
				precedence = 7;
				break;
				
			case 'i':
				if (c1 != 's')
					return Operator.ID.None;
				operatorID = Operator.ID.op_Is;
				precedence = 7;
				break;
				
			case '.':
				if (c1 != '.')
					return Operator.ID.None;
				operatorID = Operator.ID.op_Range;
				precedence = 12;
				break;
			
			default:
				return Operator.ID.None;
			}
		}
		else if (text == "switch")
		{
			operatorID = Operator.ID.op_SwitchExpression;
			precedence = 11;
		}
		
		node = node.nextSibling;
		--numNodesLeft;
		
		if (numNodesLeft == 0)
			node = null;
	
		return operatorID;
	}
}


public class SymbolDefinition
{
	public static readonly bool supportsDynamicType = Type.GetType("Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo,Microsoft.CSharp", false) != null;
	
	public static readonly SymbolDefinition resolvedChildren = new SymbolDefinition { kind = SymbolKind.None };
	public static readonly SymbolDefinition nullLiteral = new NullLiteral { kind = SymbolKind.Null };
	public static readonly SymbolDefinition contextualKeyword = new SymbolDefinition { kind = SymbolKind.Null };
	public static readonly TypeDefinition unknownType = new TypeDefinition { name = "unknown type", kind = SymbolKind.Error };
	public static readonly TypeDefinition nullVarType = new TypeDefinition { name = "Cannot assign null to an implicitly-typed variable", kind = SymbolKind.Error };
	public static readonly TypeDefinition voidVarType = new TypeDefinition { name = "Cannot assign void to an implicitly-typed variable", kind = SymbolKind.Error };
	public static readonly TypeDefinition circularBaseType = new TypeDefinition { name = "circular base type", kind = SymbolKind.Error };
	public static readonly SymbolDefinition unknownSymbol = new SymbolDefinition { name = "unknown symbol", kind = SymbolKind.Error };
	public static readonly SymbolDefinition unknownParameterName = new SymbolDefinition { name = "unknown parameter name", kind = SymbolKind.Error };
	public static readonly SymbolDefinition thisInStaticMember = new SymbolDefinition { name = "cannot use 'this' in static member", kind = SymbolKind.Error };
	public static readonly SymbolDefinition baseInStaticMember = new SymbolDefinition { name = "cannot use 'base' in static member", kind = SymbolKind.Error };
	public static readonly TypeDefinition cannotInvokeSymbol = new TypeDefinition { name = "Cannot invoke symbol", kind = SymbolKind.Error };

	protected static readonly List<ParameterDefinition> _emptyParameterList = new List<ParameterDefinition>();
	protected static readonly List<TypeReference> _emptyInterfaceList = new List<TypeReference>();
	
	private static Stack<List<TypeDefinitionBase>> typeListsPool = new Stack<List<TypeDefinitionBase>>();

	protected static List<TypeDefinitionBase> CreateTypeList()
	{
		return typeListsPool.Count > 0 ? typeListsPool.Pop() : new List<TypeDefinitionBase>(16);
	}

	protected static void ReleaseTypeList(List<TypeDefinitionBase> list)
	{
		if (list != null)
		{
			list.Clear();
			typeListsPool.Push(list);
		}
	}

	public SymbolKind kind;
	public CharSpan _name;
	public CharSpan name {
		get { return _name; }
		set
        {
			_name = value;
			hashID = value.GetHashCode();
		}
	}
	//public void SetName(CharSpan value)
	//{
	//	_name = value;
	//	hashID = value.GetHashCode();
	//}
	public int hashID;

	public UnityEngine.Texture2D cachedIcon;

	public SymbolDefinition parentSymbol;
	public SymbolDefinition savedParentSymbol;

	public Modifiers modifiers;
	public AccessLevel accessLevel;

	/// <summary>
	/// Zero, one, or more declarations defining this symbol
	/// </summary>
	/// <remarks>Check for null!!!</remarks>
	//public List<SymbolDeclaration> declarations;
	public SymbolDeclaration declarations;
	
	public SymbolDeclaration FindFirstValidDeclaration()
	{
		SymbolDeclaration d;
		do
		{
			d = declarations;
			if (d != null && d.IsValid())
				return d;
		} while (declarations != d);
		
		if (declarations != null)
		{
			Debug.LogError("declarations is not null!!!");
			
			d = declarations.next;
			while (d != null)
			{
				var next = d.next;
				if (d != null && d.IsValid())
					return d;
				d = next;
			}
		}
		
		return null;
	}

	public static int GetHashID(string s)
	{
		unchecked
		{
			int hash1 = 5381;
			int hash2 = hash1;
			if (s != null)
			{
				int length = s.Length;
				for (int i = 0; i < length; )
				{
					hash1 = ((hash1 << 5) + hash1) ^ s[i++];
					hash2 = i < length ? ((hash2 << 5) + hash2) ^ s[i++] : hash2;
				}
			}
			return hash1 + (hash2 * 1566083941);
		}
	}

	public static int GetHashID(string s, int startAt, int length)
	{
		unchecked
		{
			int hash1 = 5381;
			int hash2 = hash1;
			if (s != null)
			{
				length += startAt;
				for (int i = startAt; i < length; )
				{
					hash1 = ((hash1 << 5) + hash1) ^ s[i++];
					hash2 = i < length ? ((hash2 << 5) + hash2) ^ s[i++] : hash2;
				}
			}
			return hash1 + (hash2 * 1566083941);
		}
	}

	public static int GetHashIDIgnoreCase(string s, int start, int length)
	{
		unchecked
		{
			int hash1 = 5381;
			int hash2 = hash1;
			if (s != null)
			{
				int endEven = start + (length & ~1);
				for (int i = start; i < endEven; )
				{
					hash1 = ((hash1 << 5) + hash1) ^ char.ToLowerInvariant(s[i++]);
					hash2 = ((hash2 << 5) + hash2) ^ char.ToLowerInvariant(s[i++]);
				}
				if ((length & 1) != 0)
					hash1 = ((hash1 << 5) + hash1) ^ char.ToLowerInvariant(s[endEven]);
			}
			return hash1 + (hash2 * 1566083941);
		}
	}

	public bool ContainsDeclaration(SymbolDeclaration symbol)
	{
		for (var d = declarations; d != null; d = d.next )
			if (ReferenceEquals(d, symbol))
				return true;
		return false;
	}

	public struct SymbolList
	{
		private List<SymbolDefinition> storage;
		
		private MiniBloom1 bloomFilter;
		
		public int Count {
			get {
				return storage == null ? 0 : storage.Count;
			}
		}
		
		public void RemoveAt(int index)
		{
			//if (storage == null)
			//	throw new IndexOutOfRangeException();
			storage.RemoveAt(index);
		}
		
		private int BinarySearch(int hashID, int numTypeParameters)
		{
			//var hashID = name.GetHashCode();
			
			int lo = 0;
			int hi = storage.Count - 1;
			while (lo <= hi)
			{
				int i = lo + ((hi - lo) >> 1);
				var current = storage[i];
				//int order = string.CompareOrdinal(current.name, name);
				int order = current.hashID < hashID ? -1 : current.hashID == hashID ? 0 : 1;
				
				if (order == 0)
				{
					while (i > 0 && storage[i-1].hashID == hashID)
					{
						--i;
						current = storage[i];
					}
					if (numTypeParameters < 0 ||
						numTypeParameters == current.NumTypeParameters ||
						current.kind == SymbolKind.MethodGroup)
					{
						return i;
					}
					
					while (++i < storage.Count)
					{
						current = storage[i];
						if (current.hashID != hashID)
						{
							return ~i;
						}
						var numTypeParamsCurrent = current.NumTypeParameters;
						if (numTypeParamsCurrent == numTypeParameters)
						{
							return i;
						}
						if (numTypeParamsCurrent > numTypeParameters)
						{
							return ~i;
						}
					}
					return ~i;
				}
				if (order < 0)
				{
					lo = i + 1;
				}
				else
				{
					hi = i - 1;
				}
			}
			
			return ~lo;
		}
		
		//private int BinarySearch(string name, int numTypeParameters)
		//{
		//	var hashID = GetHashID(name);
			
		//	int lo = 0;
		//	int hi = storage.Count - 1;
		//	while (lo <= hi)
		//	{
		//		int i = lo + ((hi - lo) >> 1);
		//		var current = storage[i];
		//		//int order = string.CompareOrdinal(current.name, name);
		//		int order = current.hashID.CompareTo(hashID);
				
		//		if (order == 0)
		//		{
		//			while (i > 0 && storage[i-1].name == name)
		//			{
		//				--i;
		//			}
		//			current = storage[i];
		//			if (numTypeParameters < 0 ||
		//				numTypeParameters == current.NumTypeParameters ||
		//				current.kind == SymbolKind.MethodGroup)
		//			{
		//				return i;
		//			}
					
		//			while (++i < storage.Count)
		//			{
		//				current = storage[i];
		//				if (current.name != name)
		//				{
		//					return ~i;
		//				}
		//				if (current.NumTypeParameters == numTypeParameters)
		//				{
		//					return i;
		//				}
		//				if (current.NumTypeParameters > numTypeParameters)
		//				{
		//					return ~i;
		//				}
		//			}
		//			return ~i;
		//		}
		//		if (order < 0)
		//		{
		//			lo = i + 1;
		//		}
		//		else
		//		{
		//			hi = i - 1;
		//		}
		//	}
			
		//	return ~lo;
		//}
		
		//private int BinarySearch(string name, int startAt, int length, int numTypeParameters)
		//{
		//	var hashID = GetHashID(name, startAt, length);
			
		//	int lo = 0;
		//	int hi = storage.Count - 1;
		//	while (lo <= hi)
		//	{
		//		int i = lo + ((hi - lo) >> 1);
		//		var current = storage[i];
		//		//int order = string.CompareOrdinal(current.name, 0, name, startAt, length);
		//		int order = current.hashID.CompareTo(hashID);
				
		//		if (order == 0)
		//		{
		//			//if (current.name.Length > length)
		//			//{
		//			//	hi = i - 1;
		//			//	continue;
		//			//}
					
		//			while (i > 0 && storage[i-1].name == name)
		//			{
		//				--i;
		//			}
		//			current = storage[i];
		//			if (numTypeParameters < 0 ||
		//				numTypeParameters == current.NumTypeParameters ||
		//				current.kind == SymbolKind.MethodGroup)
		//			{
		//				return i;
		//			}
					
		//			while (++i < storage.Count)
		//			{
		//				current = storage[i];
		//				if (current.name != name)
		//				{
		//					return ~i;
		//				}
		//				if (current.NumTypeParameters == numTypeParameters)
		//				{
		//					return i;
		//				}
		//				if (current.NumTypeParameters > numTypeParameters)
		//				{
		//					return ~i;
		//				}
		//			}
		//			return ~i;
		//		}
		//		if (order < 0)
		//		{
		//			lo = i + 1;
		//		}
		//		else
		//		{
		//			hi = i - 1;
		//		}
		//	}
			
		//	return ~lo;
		//}
		
		public bool TryGetValue(string name, int startAt, int length, int numTypeParameters, out SymbolDefinition value)
		{
			if (storage == null)
			{
				value = null;
				return false;
			}
			
			var hashID = GetHashID(name, startAt, length);
			
			if (!bloomFilter.Contains(hashID))
			{
				value = null;
				return false;
			}
			
			var index = BinarySearch(hashID, numTypeParameters);
			if (index < 0)
			{
				value = null;
				return false;
			}
			
			value = storage[index];
			return true;
		}
		
		public bool TryGetValue(CharSpan name, int numTypeParameters, out SymbolDefinition value)
		{
			if (storage == null)
			{
				value = null;
				return false;
			}
			
			var hashID = name.GetHashCode();
			
			if (!bloomFilter.Contains(hashID))
			{
				value = null;
				return false;
			}
			
			var index = BinarySearch(hashID, numTypeParameters);
			if (index < 0)
			{
				value = null;
				return false;
			}
			
			value = storage[index];
			return true;
		}

		//public bool Remove(CharSpan name, int numTypeParameters)
		//{
		//	if (storage == null)
		//		return false;
			
		//	var index = BinarySearch(name, numTypeParameters);
		//	if (index >= 0)
		//	{
		//		storage.RemoveAt(index);
		//		return true;
		//	}
			
		//	return false;
		//}
		
		public bool Contains(CharSpan name, int numTypeParameters)
		{
			var hashID = name.GetHashCode();
			
			if (!bloomFilter.Contains(hashID))
			{
				return false;
			}
			
			return BinarySearch(hashID, numTypeParameters) >= 0;
		}
		
		public SymbolDefinition this[int index]
		{
			get {
				return storage[index];
			}
		}
		
		public SymbolDefinition this[CharSpan name, int numTypeParameters]
		{
			//get
			//{
			//	SymbolDefinition value;
			//	if (!TryGetValue(name, numTypeParameters, out value))
			//		return null;//throw new KeyNotFoundException(name);
			//	return value;
			//}

			set
			{
				bloomFilter.Add(value.hashID);
					
				if (storage == null)
				{
					storage = new List<SymbolDefinition>();
					storage.Add(value);
					return;
				}
				
				var index = BinarySearch(value.hashID, numTypeParameters);
				if (index >= 0)
				{
					var count = storage.Count;
					while (index < count)
					{
						var old = storage[index];
						if (object.ReferenceEquals(value, old))
							return;

						if (old.declarations == null)
						{
							if (value.declarations == null)
								break;

							storage.RemoveAt(index);
							--count;
						}
						else
						{
							var allValid = true;
							for (var d = old.declarations; d != null; d = d.next)
							{
								if (!d.IsValid())
								{
									allValid = false;
									break;
								}
							}
						
							if (!allValid)
							{
								storage.RemoveAt(index);
								--count;
							}
							else
							{
								++index;
							}
						}
						
						if (index < count)
						{
							old = storage[index];
							if (old.name != name ||
								old.kind != SymbolKind.MethodGroup && old.NumTypeParameters != numTypeParameters)
							{
								break;
							}
						}
					}
				}
				else
				{
					index = ~index;
				}
				storage.Insert(index, value);
			}
		}
	}
	
	public SymbolList members = new SymbolList();

	public static AccessLevel AccessLevelFromModifiers(Modifiers modifiers)
	{
		if ((modifiers & Modifiers.Public) != 0)
			return AccessLevel.Public;
		if ((modifiers & Modifiers.Protected) != 0)
		{
			if ((modifiers & Modifiers.Internal) != 0)
				return AccessLevel.ProtectedOrInternal;
			else if ((modifiers & Modifiers.Private) != 0)
				return AccessLevel.ProtectedAndInternal;
			return AccessLevel.Protected;
		}
		if ((modifiers & Modifiers.Internal) != 0)
			return AccessLevel.Internal;
		if ((modifiers & Modifiers.Private) != 0)
			return AccessLevel.Private;
		return AccessLevel.None;
	}
	
	public static CharSpan DecodeId(CharSpan name)
	{
		if (!name.IsEmpty && name[0] == '@')
			return name.Substring(1);
		return name;
	}
	
	public static bool IsOperatorName(CharSpan methodName)
	{
		if (!methodName.FastStartsWith("op_"))
			return false;
		
		switch (methodName)
		{
		case "op_Implicit":
		case "op_Explicit":
		case "op_Addition":
		case "op_Subtraction":
		case "op_Multiply":
		case "op_Division":
		case "op_Modulus":
		case "op_ExclusiveOr":
		case "op_BitwiseAnd":
		case "op_BitwiseOr":
		case "op_LogicalAnd":
		case "op_LogicalOr":
		case "op_Assign":
		case "op_LeftShift":
		case "op_RightShift":
		case "op_SignedRightShift":
		case "op_UnsignedRightShift":
		case "op_Equality":
		case "op_GreaterThan":
		case "op_LessThan":
		case "op_Inequality":
		case "op_GreaterThanOrEqual":
		case "op_LessThanOrEqual":
		case "op_MultiplicationAssignment":
		case "op_SubtractionAssignment":
		case "op_ExclusiveOrAssignment":
		case "op_LeftShiftAssignment":
		case "op_ModulusAssignment":
		case "op_AdditionAssignment":
		case "op_BitwiseAndAssignment":
		case "op_BitwiseOrAssignment":
		case "op_Comma":
		case "op_DivisionAssignment":
		case "op_Decrement":
		case "op_Increment":
		case "op_UnaryNegation":
		case "op_UnaryPlus":
		case "op_OnesComplement":
		case "op_UnsignedRightShiftAssignment":
		case "op_RightShiftAssignment":
		case "op_MemberSelection":
		case "op_PointerToMemberSelection":
		case "op_LogicalNot":
		case "op_True":
		case "op_False":
		case "op_AddressOf":
		case "op_PointerDereference":
			return true;
		}
		
		return false;
	}

	public bool IsValid()
	{
		if (declarations == null)
		{
			if (this is NullLiteral || this is NullTypeDefinition)
				return true;

			if (this is DefaultValue || this is DefaultTypeDefinition)
				return true;

			var asArrayType = this as ArrayTypeDefinition;
			if (asArrayType != null)
			{
				var elementType = asArrayType.elementType;
				return elementType.IsValid();
			}

			var asRefType = this as RefTypeDefinition;
			if (asRefType != null)
			{
				var refType = asRefType.referencedType;
				return refType.IsValid();
			}

			var asPointerType = this as PointerTypeDefinition;
			if (asPointerType != null)
			{
				var referentType = asPointerType.referentType;
				return referentType.IsValid();
			}

			var asConstructedType = this as ConstructedTypeDefinition;
			if (asConstructedType != null)
			{
				var typeArgs = asConstructedType.typeArguments;
				if (typeArgs != null)
					for (var i = typeArgs.Length; i-- > 0;)
						if (!typeArgs[i].IsValid())
							return false;
			}

			var asTupleType = this as TupleTypeDefinition;
			if (asTupleType != null)
			{
				return asTupleType.AreAllFieldsValid();
			}

			var genericSymbol = GetGenericSymbol();
			if (genericSymbol != null)
			{
				if (genericSymbol is ReflectedType || genericSymbol is ReflectedMethod ||
					genericSymbol is ReflectedConstructor || genericSymbol is ReflectedMember)
				{
					var result = genericSymbol.Assembly != null;
					if (result)
						return true;
					else
						return false;
				}
			}

			if (kind == SymbolKind.Error)
				return true;
			
			if (this is ReflectedType || this is ReflectedMethod || this is ReflectedConstructor || this is ReflectedMember)
			{
				var result = Assembly != null;
				if (result)
					return true;
				else
					return false;
			}

			{
				var result = !(this is TypeDefinitionBase) || (parentSymbol != null && parentSymbol.IsValid()); // kind != SymbolKind.Error;
				if (result)
					return true;
				else
					return false;
			}
		}

		if (kind == SymbolKind.MethodGroup)
			return true;
		
		SymbolDeclaration next;
		for (var declaration = declarations; declaration != null; declaration = next)
		{
			next = declaration.next;
			
			if (!declaration.IsValid())
			{
				ListRemoveDeclaration(declaration);
				if (declaration.scope != null)
				{
					declaration.scope.RemoveDeclaration(declaration);
					declaration.scope = null;
					++ParseTree.resolverVersion;
					if (ParseTree.resolverVersion == 0)
						++ParseTree.resolverVersion;
				}
			}
		}

		return declarations != null || kind == SymbolKind.Namespace && members.Count > 0;
	}
	
	public virtual SymbolDefinition Rebind()
	{
		if (kind == SymbolKind.Namespace)
			return Assembly.FindSameNamespace(this as NamespaceDefinition);
		
		if (parentSymbol == null && savedParentSymbol == null)
			return this;
		
		var newParent = (parentSymbol ?? savedParentSymbol).Rebind();
		if (newParent == null)
			return null;
		
		if (newParent == parentSymbol)
			return this;
		
		var tp = GetTypeParameters();
		var numTypeParams = tp != null ? tp.Count : 0;
		var symbolIsType = this is TypeDefinitionBase;
		SymbolDefinition newSymbol = newParent.FindName(name, numTypeParams, symbolIsType);
#if SI3_WARNINGS
		if (newSymbol == null)
			Debug.LogWarning(GetTooltipText() + " not found in " + newParent.GetTooltipText());
#endif
		return newSymbol;
	}
	
	public virtual Type GetRuntimeType()
	{
		if (parentSymbol == null)
			return null;
		return parentSymbol.GetRuntimeType();
	}

	public static SymbolDefinition Create(SymbolDeclaration declaration)
	{
		var symbolName = declaration.Name;
		if (!symbolName.IsEmpty)
			symbolName.DecodeId();
		
		var definition = Create(declaration.kind, symbolName);
		if (declaration.kind == SymbolKind.Constructor && (declaration.modifiers & Modifiers.Static) != 0)
			definition.name = ".cctor";

		declaration.definition = definition;

		if (declaration.parseTreeNode != null)
		{
			definition.modifiers = declaration.modifiers;
			definition.accessLevel = AccessLevelFromModifiers(declaration.modifiers);

			declaration.next = definition.declarations;
			definition.declarations = declaration;
		}

		var nameNode = declaration.NameNode();
		if (nameNode is ParseTree.Leaf)
			nameNode.SetDeclaredSymbol(definition);

		return definition;
	}

	public static SymbolDefinition Create(SymbolKind kind, CharSpan name)
	{
		SymbolDefinition definition;

		switch (kind)
		{
			case SymbolKind.LambdaExpression:
				definition = new LambdaExpressionDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Parameter:
				definition = new ParameterDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.CaseVariable:
			case SymbolKind.ForEachVariable:
			case SymbolKind.FromClauseVariable:
			case SymbolKind.Variable:
			case SymbolKind.TupleDeconstructVariable:
			case SymbolKind.OutVariable:
			case SymbolKind.IsVariable:
			case SymbolKind.Field:
			case SymbolKind.ConstantField:
			case SymbolKind.LocalConstant:
			case SymbolKind.Property:
			case SymbolKind.Event:
			case SymbolKind.CatchParameter:
			case SymbolKind.EnumMember:
				definition = new InstanceDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Indexer:
				definition = new IndexerDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Struct:
			case SymbolKind.Class:
			case SymbolKind.Interface:
				definition = new TypeDefinition
				{
					name = name,
				};
				break;
			
			case SymbolKind.Enum:
				definition = new EnumTypeDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Delegate:
				definition = new DelegateTypeDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Namespace:
				definition = new NamespaceDefinition
				{
					name = name,
					accessLevel = AccessLevel.Public,
					modifiers = Modifiers.Public,
				};
				break;

			case SymbolKind.Method:
				definition = new MethodDefinition
				{
					name = name,
					explicitInterfaceImplementation = TypeReference.To(SymbolDeclaration.ExplicitInterfaceNode),
				};
				break;

			case SymbolKind.Operator:
				kind = SymbolKind.Method;
				definition = new MethodDefinition
				{
					name = name,
					isOperator = true,
				};
				break;

			case SymbolKind.Constructor:
				definition = new MethodDefinition
				{
					name = ".ctor",
				};
				break;

			case SymbolKind.MethodGroup:
				definition = new MethodGroupDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.TypeParameter:
				definition = new TypeParameterDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.UsingAlias:
				definition = new UsingAliasDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Accessor:
				definition = new SymbolDefinition
				{
					name = name,
				};
				break;

			default:
				definition = new SymbolDefinition
				{
					name = name,
				};
				break;
		}

		definition.kind = kind;

		return definition;
	}

	public virtual CharSpan GetName()
	{
		var typeParameters = GetTypeParameters();
		if (typeParameters == null || typeParameters.Count == 0)
			return name;

		var sb = StringBuilders.Alloc();
		
		sb.Append(name);
		sb.Append('<');
		sb.Append(typeParameters[0].GetName());
		for (var i = 1; i < typeParameters.Count; ++i)
		{
			sb.Append(", ");
			sb.Append(typeParameters[i].GetName());
		}
		sb.Append('>');
		var result = sb.ToString();
		
		StringBuilders.Release(sb);
		
		return result;
	}

	public string ReflectionName
	{
		get {
			var tp = GetTypeParameters();
			if (tp == null || tp.Count == 0)
				return name;
			if (kind == SymbolKind.Method)
				return name + "``" + tp.Count;
			return name + "`" + tp.Count;
		}
	}

	public virtual SymbolDefinition TypeOf()
	{
		return this;
	}
	
	public virtual SymbolDefinition GetGenericSymbol()
	{
		return this;
	}
	
	public virtual TypeDefinitionBase GetTypeArgument(int index)
	{
		return null;
	}
	
	public virtual TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
	{
		Debug.LogWarning("Not a type! Can't substitute type of: " + GetTooltipText());
		return null;
	}

	//public static Dictionary<Type, ReflectedType> reflectedTypes = new Dictionary<Type, ReflectedType>(16000);

	public TypeDefinitionBase ImportReflectedType(Type type, string typeNamespace = null)
	{
		var typeName = type.Name;
		
		if (typeName.FastStartsWith("<"))
			return null;
		
		//if (type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null)
		//{
		//	Debug.LogError(type);
		//	return null;
		//}

		ReflectedType reflectedType;
		//if (reflectedTypes.TryGetValue(type, out reflectedType))
		//{
		//	//#if SI3_WARNINGS
		//	Debug.LogError("Found imported type: " + reflectedType.ReflectionName);
		//	//#endif
		//	return reflectedType;
		//}
		
		if (type.IsArray)
		{
			var elementType = TypeReference.To(type.GetElementType());
			var arrayType = ((TypeDefinitionBase)elementType.definition).MakeArrayType(type.GetArrayRank());
			return arrayType;
		}

		if ((type.IsGenericType || type.ContainsGenericParameters) && !type.IsGenericTypeDefinition)
		{
			var arguments = type.GetGenericArguments();
			var numGenericArgs = arguments.Length;
			var declaringType = type.DeclaringType;
			TypeDefinition parentType = null;
			if (declaringType != null && declaringType.IsGenericType)
			{
				var parentArgs = declaringType.GetGenericArguments();
				numGenericArgs -= parentArgs.Length;

				parentType = TypeReference.To(declaringType).definition as TypeDefinition;
			}

			var argumentRefs = TypeReference.AllocArray(numGenericArgs);
			var startFrom = arguments.Length - numGenericArgs;
			for (var i = startFrom; i < arguments.Length; ++i)
				argumentRefs[i - startFrom] = TypeReference.To(arguments[i]);

			var typeDefinitionRef = TypeReference.To(type.GetGenericTypeDefinition());
			var typeDefinition = typeDefinitionRef.definition as TypeDefinition;
			var constructedType = typeDefinition.ConstructType(argumentRefs, parentType);
			TypeReference.ReleaseArray(argumentRefs);
			return constructedType;
		}

		#if SI3_WARNINGS
		if (type.IsGenericParameter)
		{
			UnityEngine.Debug.LogError("Importing reflected generic type parameter " + type.FullName);
		}
		#endif

		if (type.IsEnum)
			reflectedType = new ReflectedEnumType(type, typeName, typeNamespace);
		else
			reflectedType = new ReflectedType(type, typeName, typeNamespace);
		members[reflectedType.name, reflectedType.NumTypeParameters] = reflectedType;
		reflectedType.parentSymbol = this;
		
		if (type.IsPointer)
			return reflectedType.MakePointerType();
		else
			return reflectedType;
	}

	public void AddMember(SymbolDefinition symbol)
	{
		symbol.parentSymbol = this;
		if (!symbol.name.IsEmpty)
		{
			var declaration = symbol.declarations != null && symbol.declarations.next == null ? symbol.declarations : null;
			if (declaration != null && declaration.numTypeParameters > 0)
				members[declaration.Name, declaration.numTypeParameters] = symbol;
			else
				members[symbol.name, symbol.NumTypeParameters] = symbol;
		}
	}

	public SymbolDefinition AddMember(SymbolDeclaration symbol)
	{
		var member = Create(symbol);
		var symbolName = member.name;
		if (member.kind == SymbolKind.Method || member.kind == SymbolKind.Constructor || member.kind == SymbolKind.Operator)
		{
			SymbolDefinition methodGroup = null;
			if (!members.TryGetValue(symbolName, 0, out methodGroup) || !(methodGroup is MethodGroupDefinition))
			{
				methodGroup = AddMember(new SymbolDeclaration(symbolName)
				{
					kind = SymbolKind.MethodGroup,
					modifiers = Modifiers.None, // symbol.modifiers,
					parseTreeNode = symbol.parseTreeNode,
					scope = symbol.scope,
				//	numTypeParameters = symbol.numTypeParameters,
				});
			}
			var asMethodGroup = methodGroup as MethodGroupDefinition;
			if (asMethodGroup != null)
			{
				asMethodGroup.AddMethod((MethodDefinition) member);
			//	member = methodGroup;
			}
			//else
			//	UnityEngine.Debug.LogError(methodGroup);
		}
		else
		{
			if (member.kind == SymbolKind.Delegate)
			{
				var memberAsDelegate = (DelegateTypeDefinition) member;
				var typeNode = symbol.parseTreeNode.FindChildByName("type");
				if (typeNode == null)
					memberAsDelegate.returnType = null;
				else
					memberAsDelegate.returnType = TypeReference.To(typeNode);
			}
			else if (member.kind == SymbolKind.Enum)
			{
				var memberAsEnum = (EnumTypeDefinition) member;
				var enumBaseNode = symbol.parseTreeNode.FindChildByName("enumBase") as ParseTree.Node;
				if (enumBaseNode != null)
					enumBaseNode = enumBaseNode.NodeAt(1);
				memberAsEnum.UnderlyingType = enumBaseNode == null ?
					TypeReference.To(builtInTypes_int) :
					TypeReference.To(enumBaseNode);
			}
			else if (member.kind == SymbolKind.Accessor)
			{
				if (member.accessLevel == AccessLevel.None)
				{
					member.modifiers = modifiers & (Modifiers.Internal | Modifiers.Protected | Modifiers.Private);
					member.accessLevel = accessLevel;
				}
			}
			//else if (member.kind == SymbolKind.MethodGroup)
			//{
			//	((MethodGroupDefinition) member).numTypeParameters = symbol.numTypeParameters;
			//}

			AddMember(member);
			
			if (member.kind == SymbolKind.Namespace)
			{
				var nn = NamespaceName.Get(member.FullName);
				nn.allNamespaces.Add(member as NamespaceDefinition);
			}
		}
		
		if (member.IsPartial)
		{
			if (member is TypeDefinitionBase)
			{
				FGTextBufferManager.FindOtherTypeDeclarationParts(symbol);
				FGTextBufferManager.ParseAllAsyncBuffers();
			}
		}

		return member;
	}

	public virtual SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		var parentNamespace = this as NamespaceDefinition;

		SymbolDefinition definition;
		if (parentNamespace != null && symbol is NamespaceDeclaration)
		{
			var qnNode = symbol.parseTreeNode.NodeAt(1);
			if (qnNode == null)
				return null;

			for (var i = 0; i < qnNode.numValidNodes - 2; i += 2)
			{
				var ns = qnNode.ChildAt(i).Print();
				var childNS = parentNamespace.FindName(ns, 0, false);
				if (childNS == null)
				{
					childNS = new NamespaceDefinition {
						kind = SymbolKind.Namespace,
						name = ns,
						accessLevel = AccessLevel.Public,
						modifiers = Modifiers.Public,
					};
					parentNamespace.AddMember(childNS);
					
					var nn = NamespaceName.Get(childNS.FullName);
					nn.allNamespaces.Add(childNS as NamespaceDefinition);
				}
				parentNamespace = childNS as NamespaceDefinition;
				if (parentNamespace == null)
					break;
			}
		}

		var addToSymbol = parentNamespace ?? this;
		if (!addToSymbol.members.TryGetValue(symbol.Name, symbol.kind == SymbolKind.Method ? 0 : symbol.numTypeParameters, out definition) ||
			(symbol.kind == SymbolKind.Operator || symbol.kind == SymbolKind.Method || symbol.kind == SymbolKind.Constructor) && definition is MethodGroupDefinition ||
			definition is ReflectedMember || definition is ReflectedType ||
			definition is ReflectedMethod || definition is ReflectedConstructor ||
			!definition.IsValid())
		{
			if (definition != null &&
				(definition is ReflectedMember || definition is ReflectedType ||
					definition is ReflectedMethod || definition is ReflectedConstructor)
				&& definition != symbol.definition)
			{
				definition.Invalidate();
				definition = null;
			}
			if (definition == null || definition.kind != SymbolKind.Namespace || symbol.kind != SymbolKind.Namespace)
				definition = addToSymbol.AddMember(symbol);
		}
		else
		{
			if (definition.kind == SymbolKind.Namespace && symbol.kind == SymbolKind.Namespace)
			{
				symbol.next = definition.declarations;
				definition.declarations = symbol;
			}
			else if (symbol.IsPartial && definition.declarations != null)
			{
				var definitionAsType = definition as TypeDefinitionBase;
				if (definitionAsType != null)
				{
					definitionAsType.InvalidateBaseType();
				}
				symbol.next = definition.declarations;
				definition.declarations = symbol;
				
				definition.modifiers |=
					symbol.modifiers &
					(Modifiers.Abstract | Modifiers.New | Modifiers.Sealed | Modifiers.Static | Modifiers.AccessMask);
			}
			else
			{
				definition = addToSymbol.AddMember(symbol);
			}
		}

		symbol.definition = definition;

		var nameNode = symbol.NameNode();
		if (nameNode != null)
		{
			var leaf = nameNode as ParseTree.Leaf;
			if (leaf == null)
			{
				var node = (ParseTree.Node) nameNode;
				if (node.RuleName == "memberName")
				{
					node = node.NodeAt(0); // qid
					if (node != null)
					{
						node = node.NodeAt(-1); // the last child node, qidPart or qidStart
						if (node != null)
						{
							if (node.RuleName == "qidStart")
							{
								if (node.numValidNodes < 3)
									leaf = node.LeafAt(0);
								else
									leaf = node.LeafAt(2);
							}
							else // node is qidPart
							{
								node = node.NodeAt(0); // accessIdentifier
								if (node != null)
									leaf = node.LeafAt(1);
							}
						}
					}
				}
			}
			if (leaf != null)
			{
				leaf.SetDeclaredSymbol(definition);
				if (definition.kind == SymbolKind.Destructor)
				{
					var id = DecodeId(leaf.token.text);
					if (id != addToSymbol.name)
						leaf.semanticError = "Name of destructor must match name of class";
				}
				else if (definition.kind == SymbolKind.Constructor)
				{
					var id = DecodeId(leaf.token.text);
					if (id != addToSymbol.name)
						leaf.semanticError = "Methods must have return type";
				}
			}
		}

		return definition;
	}

	private void Invalidate()
	{
		if (savedParentSymbol == null)
			savedParentSymbol = parentSymbol;
		parentSymbol = null;
		for (var i = members.Count; i --> 0; )
			members[i].Invalidate();
		
		var typeParams = GetTypeParameters();
		if (typeParams != null)
			for (int i = typeParams.Count; i --> 0; )
				typeParams[i].Invalidate();
	}
	
	public bool ListRemoveDeclaration(SymbolDeclaration symbol)
	{
		if (declarations == null)
			return false;
		
		if (symbol == declarations)
		{
			declarations = symbol.next;
			symbol.next = null;
			return true;
		}
		
		var d = declarations;
		var n = d.next;
		while (n != null)
		{
			if (n == symbol)
			{
				d.next = n.next;
				n.next = null;
				return true;
			}
			d = n;
			n = n.next;
		}
		return false;
	}

	public virtual void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Method || symbol.kind == SymbolKind.Operator || symbol.kind == SymbolKind.Constructor)
		{
			for (var i = members.Count; i --> 0; )
			{
				var x = members[i];
				if (x.kind != SymbolKind.MethodGroup || x.declarations == null)
					continue;

				var mg = x as MethodGroupDefinition;
				if (mg.methods.Count > 0)
				{
					mg.RemoveDeclaration(symbol);
					if (mg.methods.Count == 0)
					{
						while (mg.declarations != null)
						{
							var d = mg.declarations;
							mg.declarations = d.next;
							d.next = null;
						}
						mg.savedParentSymbol = mg.parentSymbol;
						mg.parentSymbol = null;

						members.RemoveAt(i);
						break;
					}
				}
			}
		}
		else
		{
			var index = members.Count;
			while (index --> 0)
			{
				var member = members[index];
				if (member.kind == SymbolKind.MethodGroup || member.declarations == null)
					continue;

				if (!member.ListRemoveDeclaration(symbol))
					continue;

				if (member.declarations == null)
				{
					if (member.kind != SymbolKind.Namespace || member.members.Count == 0)
						{ members.RemoveAt(index); break; }
				}
				else
				{
					var firstDeclarationKind = member.declarations.kind;
					if (member.kind != firstDeclarationKind)
					{
						if ((firstDeclarationKind == SymbolKind.Class ||
							 firstDeclarationKind == SymbolKind.Struct ||
							 firstDeclarationKind == SymbolKind.Interface) &&
							member.IsPartial && member is TypeDefinitionBase)
						{
							member.kind = firstDeclarationKind;
						}
					}
				}
			}
		}
	}

	public override string ToString()
	{
		return kind + " " + name;
	}
	
	public virtual string CompletionDisplayString(string styledName)
	{
		return styledName;
	}
	
	public virtual string GetDelegateInfoText() { return GetTooltipText(); }

	public string PrintParameters(List<ParameterDefinition> parameters, bool singleLine = false)
	{
		if (parameters == null || tooltipAsExtensionMethod && parameters.Count == 1)
			return "";

		var parametersText = "";
		var comma = !singleLine && parameters.Count > (tooltipAsExtensionMethod ? 2 : 1) ? "\n    " : "";
		var nextComma = !singleLine && parameters.Count > (tooltipAsExtensionMethod ? 2 : 1) ? ",\n    " : ", ";
		for (var i = (tooltipAsExtensionMethod ? 1 : 0); i < parameters.Count; ++i)
		{
			var param = parameters[i];
			
			if (param == null)
				continue;
			var typeOfP = param.TypeOf() as TypeDefinitionBase;
			if (typeOfP == null)
				continue;

			//var ctx = (kind == SymbolKind.Delegate ? this : parentSymbol) as ConstructedTypeDefinition;
			//if (ctx != null)
			//	typeOfP = typeOfP.SubstituteTypeParameters(ctx);
			//if (kind == SymbolKind.Method || kind == SymbolKind.MethodGroup)
				typeOfP = typeOfP.SubstituteTypeParameters(this);

			if (typeOfP == null)
				continue;
			parametersText += comma;
			if (param.IsThisParameter)
				parametersText += "this ";
			else if (param.IsRef)
				parametersText += "ref ";
			else if (param.IsOut)
				parametersText += "out ";
			else if (param.IsIn)
				parametersText += "in ";
			else if (param.IsParametersArray)
				parametersText += "params ";
			parametersText += typeOfP.GetName() + " " + param.name;
			if (param.defaultValue != null)
				parametersText += " = " + param.defaultValue;
			comma = nextComma;
		}
		if (!singleLine && parameters.Count > (tooltipAsExtensionMethod ? 2 : 1))
			parametersText += "\n";
		return parametersText;
	}
	
	public virtual bool IsExtensionMethod {
		get { return false; }
	}

	public virtual bool IsOperator {
		get { return false; }
	}
	
	public bool IsOverride
	{
		get { return (modifiers & Modifiers.Override) != 0; }
	}
	
	public bool IsVirtual
	{
		get { return (modifiers & Modifiers.Virtual) != 0; }
	}
	
	protected string tooltipText;
	private bool tooltipAsExtensionMethod;
	
	public string GetTooltipTextAsExtensionMethod()
	{
		string result = "";
		try
		{
			tooltipAsExtensionMethod = true;
			result = GetTooltipText();
		}
		finally
		{
			tooltipAsExtensionMethod = false;
		}
		return result;
	}

	public virtual string GetTooltipText(bool fullText = true)
	{
		if (kind == SymbolKind.Null)
			return null;

//		if (tooltipText != null)
//			return tooltipText;

		if (kind == SymbolKind.Error)
			return name;

		var kindText = string.Empty;
		if (fullText)
		{
			switch (kind)
			{
			case SymbolKind.Namespace: return tooltipText = "namespace " + FullName;
			case SymbolKind.Constructor: kindText = "(constructor) "; break;
			case SymbolKind.Destructor: kindText = "(destructor) "; break;
			case SymbolKind.ConstantField:
			case SymbolKind.LocalConstant: kindText = "(constant) "; break;
			case SymbolKind.Property: kindText = "(property) "; break;
			case SymbolKind.Field: kindText = "(field) "; break;
			case SymbolKind.Event: kindText = "(event) "; break;
			case SymbolKind.Variable:
			case SymbolKind.TupleDeconstructVariable:
			case SymbolKind.OutVariable:
			case SymbolKind.IsVariable:
			case SymbolKind.CaseVariable:
			case SymbolKind.ForEachVariable:
			case SymbolKind.FromClauseVariable:
			case SymbolKind.CatchParameter: kindText = "(local variable) "; break;
			case SymbolKind.Parameter: kindText = "(parameter) "; break;
			case SymbolKind.Delegate: kindText = "delegate "; break;
			case SymbolKind.MethodGroup: kindText = "(method group) "; break;
			case SymbolKind.Accessor: kindText = "(accessor) "; break;
			case SymbolKind.Label: return tooltipText = "(label) " + name;
			case SymbolKind.Method: kindText = IsExtensionMethod ? "(extension) " : ""; break;
			}
		}
		
		var typeOf = kind == SymbolKind.Accessor || kind == SymbolKind.MethodGroup ? null : TypeOf();

		var typeName = string.Empty;
		if (typeOf != null && kind != SymbolKind.Namespace && kind != SymbolKind.Constructor && kind != SymbolKind.Destructor)
		{
			var ctx = (typeOf.kind == SymbolKind.Delegate ? typeOf : parentSymbol) as ConstructedTypeDefinition;
			if (ctx != null)
				typeOf = ((TypeDefinitionBase) typeOf).SubstituteTypeParameters(ctx);
			typeName = typeOf.GetName() + " ";

			if (typeOf.kind != SymbolKind.TypeParameter)
				for (var parentType = typeOf.parentSymbol as TypeDefinitionBase; parentType != null; parentType = parentType.parentSymbol as TypeDefinitionBase)
					typeName = parentType.GetName() + "." + typeName;
		}

		var parameters = GetParameters();
		var numParams = parameters != null ? parameters.Count : 0;
		
		var parentText = string.Empty;
		var parent = parentSymbol is MethodGroupDefinition ? parentSymbol.parentSymbol : parentSymbol;
		if ((parent is TypeDefinitionBase &&
				parent.kind != SymbolKind.Delegate && kind != SymbolKind.TypeParameter && parent.kind != SymbolKind.LambdaExpression)
			|| parent is NamespaceDefinition)
		{
			var parentName = parent.GetName();
			if (kind == SymbolKind.Constructor)
			{
				var typeParent = parent.parentSymbol as TypeDefinitionBase;
				parentName = typeParent != null ? typeParent.GetName() : null;

				if (parent.NumTypeParameters > 0)
				{
					if (!string.IsNullOrEmpty(parentName))
						parentText = parentName + ".";
					parentName += parent.GetName();
				}
			}
			else if (kind == SymbolKind.Method && tooltipAsExtensionMethod)
			{
				var typeOfThisParameter = parameters[0].TypeOf();
				if (typeOfThisParameter != null)
					typeOfThisParameter = typeOfThisParameter.SubstituteTypeParameters(this);
				parentName = typeOfThisParameter != null ? typeOfThisParameter.GetName() : null;
			}
			if (!string.IsNullOrEmpty(parentName))
				parentText = parentName + ".";
		}

		var nameText = GetName();

		var parametersText = string.Empty;
		string parametersEnd = null;
		
		if (kind == SymbolKind.Method)
		{
			nameText += (numParams == (tooltipAsExtensionMethod ? 2 : 1) ? "( " : "(");
			parametersEnd = (numParams == (tooltipAsExtensionMethod ? 2 : 1) ? " )" : ")");
		}
		else if (kind == SymbolKind.Constructor)
		{
			nameText = parent.name + (numParams == 1 ? "( " : "(");
			parametersEnd = (numParams == 1 ? " )" : ")");
		}
		else if (kind == SymbolKind.Destructor)
		{
			nameText = "~" + parent.name + "()";
		}
		else if (kind == SymbolKind.Indexer)
		{
			nameText = (numParams == 1 ? "this[ " : "this[");
			parametersEnd = (numParams == 1 ? " ]" : "]");
		}
		else if (kind == SymbolKind.Delegate)
		{
			nameText += (numParams == 1 ? "( " : "(");
			parametersEnd = (numParams == 1 ? " )" : ")");
		}
		else if (kind == SymbolKind.Instance && this is DiscardVariable)
		{
			kindText = "(discard) ";
		}

		if (parameters != null)
		{
			parametersText = PrintParameters(parameters, !fullText);
		}

		tooltipText = kindText + typeName + parentText + nameText + parametersText + parametersEnd;

		if (fullText)
		{
			tooltipText += DebugValue();
	
			if (typeOf != null && typeOf.kind == SymbolKind.Delegate)
			{
				tooltipText += "\n\nDelegate info\n";
				tooltipText += typeOf.GetDelegateInfoText();
			}
	
			var xmlDocs = GetXmlDocs();
			if (!string.IsNullOrEmpty(xmlDocs))
			{
				tooltipText += "\n\n" + xmlDocs;
			}
			
			//tooltipText += "\n\n" + FullName;
		}

		return tooltipText;
	}
	
	protected string DebugValue()
	{
		if (kind == SymbolKind.ConstantField || //kind == SymbolKind.LocalConstant ||
			kind == SymbolKind.Field || (kind == SymbolKind.Property && SISettings.inspectPropertyValues))
		{
			if (!(parentSymbol is TypeDefinitionBase))
				return "";
			
			var runtimeType = parentSymbol.GetRuntimeType();
			if (runtimeType == null)
				return "";
			
			if (runtimeType.ContainsGenericParameters)
				return "";
			
			var typeOf = TypeOf() as TypeDefinitionBase;
			
			object value;
			
			if (IsInstanceMember)
			{
				var isScriptableObject = typeof(UnityEngine.ScriptableObject).IsAssignableFrom(runtimeType);
				var isComponent = typeof(UnityEngine.Component).IsAssignableFrom(runtimeType);
				if (isScriptableObject || isComponent)
				{
					const BindingFlags instanceMember = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
					UnityEngine.Object[] allInstances = null;
					string result = "";
					if (isComponent)
					{
						if (name == "material" || name == "mesh")
							return "";
						
						allInstances = UnityEditor.Selection.GetFiltered(runtimeType, UnityEditor.SelectionMode.ExcludePrefab);
						if (allInstances.Length > 0)
						{
							result = "\n    in " + allInstances.Length + " selected scene objects";
						}
						else
						{
							#if UNITY_2023_1_OR_NEWER
							allInstances = UnityEngine.Object.FindObjectsByType(runtimeType, UnityEngine.FindObjectsSortMode.None);
							#else
							allInstances = UnityEngine.Object.FindObjectsOfType(runtimeType);
							#endif
							if (allInstances.Length > 0)
								result = "\n    in " + allInstances.Length + " active scene objects";
						}
					}
					if (allInstances == null || allInstances.Length == 0)
					{
						allInstances = UnityEngine.Resources.FindObjectsOfTypeAll(runtimeType);
						result = "\n    in " + allInstances.Length + " instances";
					}
					
					var fieldInfo = kind == SymbolKind.Field ? runtimeType.GetField(name, instanceMember) : null;
					var propertyInfo = kind == SymbolKind.Property ? runtimeType.GetProperty(name, instanceMember) : null;
					if (fieldInfo == null && propertyInfo == null)
						return result;
					if (propertyInfo != null && propertyInfo.GetGetMethod(true) == null)
						return result;
					try
					{
						if (!IsDebuggerBrowsable(fieldInfo as MemberInfo ?? propertyInfo))
							return result;
						for (var i = 0; i < Math.Min(allInstances.Length, 10); ++i)
						{
							value = fieldInfo != null
								? fieldInfo.GetValue(allInstances[i])
								: propertyInfo.GetValue(allInstances[i], null);
							result += DebugPrintValue(typeOf, value, "\n    " + (
								allInstances[i].name == ""
								? allInstances[i].ToString()
								: "\"" + allInstances[i].name + "\" (" + allInstances[i].GetHashCode() + ")") + ": ");
						}
					}
#if SI3_WARNINGS
					catch (Exception e)
					{
						Debug.LogException(e);
					}
#else
					catch {}
#endif
					return result;
				}
				return "";
			}
			
			const BindingFlags staticMember = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			if (kind == SymbolKind.Field || kind == SymbolKind.ConstantField)
			{
				var fieldInfo = runtimeType.GetField(name, staticMember);
				if (fieldInfo == null)
					return "";
				try {
					if (!IsDebuggerBrowsable(fieldInfo))
						return "";
					value = fieldInfo.GetValue(null);
				} catch {
					return "";
				}
			}
			else if (kind == SymbolKind.Property)
			{
				var propertyInfo = runtimeType.GetProperty(name, staticMember);
				if (propertyInfo == null)
					return "";
				try {
					if (!IsDebuggerBrowsable(propertyInfo))
						return "";
					value = propertyInfo.GetValue(null, null);
				} catch {
					return "";
				}
			}
			else
			{
				return "";
			}
			return DebugPrintValue(typeOf, value, "\n    = ");
		}
		return "";
	}
	
	bool IsDebuggerBrowsable(MemberInfo memberInfo)
	{
		var dbAttribute = Attribute.GetCustomAttribute(memberInfo, typeof(DebuggerBrowsableAttribute), true) as DebuggerBrowsableAttribute;
		return dbAttribute == null || dbAttribute.State != DebuggerBrowsableState.Never;
	}
	
	protected string DebugPrintValue(TypeDefinitionBase typeOf, object value, string header)
	{
		if (value == null)
			return header + "null;";
		
		if (typeOf == builtInTypes_bool)
			return header + ((bool) value ? "true;" : "false;");
		if (typeOf == builtInTypes_int ||
			typeOf == builtInTypes_short ||
			typeOf == builtInTypes_sbyte)
			return header + value + ";";
		if (typeOf == builtInTypes_uint ||
			typeOf == builtInTypes_ushort ||
			typeOf == builtInTypes_byte)
			return header + value + "u;";
		if (typeOf == builtInTypes_long)
			return header + value + "L;";
		if (typeOf == builtInTypes_ulong)
			return header + value + "UL;";
		if (typeOf == builtInTypes_float)
			return header + value + "f;";
		if (typeOf == builtInTypes_char)
			return header + "'" + EscapeString(value.ToString()) + "';";
		if (typeOf == builtInTypes_string)
		{
			string s = "";
			try {
				s = value as string;
			} catch {}
			if (s.Length > 100)
				s = s.Substring(0, 100) + "...";
			var nl = s.IndexOfAny(new []{'\r', '\n'});
			if (nl >= 0)
				s = s.Substring(0, nl) + "...";
			s = EscapeString(s);
			return header + "\"" + s + "\";";
		}
		
		var asEnumerable = value as System.Collections.IEnumerable;
		if (asEnumerable != null)
		{
			var asArray = value as Array;
			if (asArray != null)
				return header + "{ Length = " + asArray.Length + " }";
			var asCollection = value as System.Collections.ICollection;
			if (asCollection != null)
				return header + "{ Count = " + asCollection.Count + " }";
			var countProperty = value.GetType().GetProperty("Count");
			if (countProperty != null)
			{
				var count = countProperty.GetValue(value, null);
				return header + "{ Count = " + count + " }";
			}
		}
		
		var str = value.ToString();
		if (str.Length > 100)
			str = str.Substring(0, 100) + "...";
		var newLine = str.IndexOfAny(new []{'\r', '\n'});
		if (newLine >= 0)
			str = str.Substring(0, newLine) + "...";
		return header + "{ " + str + " }";
	}

	private static string EscapeString(string input)
	{
		return System.Text.RegularExpressions.Regex.Replace(input, @"([""\\\0\a\b\f\n\r\t\v])|[\x00-\x1F]",
			m => m.Groups[1].Success
				? @"\" + "\"\\0abfnrtv"["\"\\0\a\b\f\n\r\t\v".IndexOf(m.Groups[1].Value[0])] 
				: $"\\u{(int)m.Value[0]:x4}");
	}

	public virtual List<ParameterDefinition> GetParameters()
	{
		return null;
	}

	public virtual List<TypeParameterDefinition> GetTypeParameters()
	{
		return null;
	}

	protected string GetXmlDocs()
	{
#if UNITY_WEBPLAYER && !UNITY_5_0
		return null;
#else
		string result = null;
		
		var unityName = UnityHelpName;
		if (unityName != null)
		{
			if (UnitySymbols.summaries.TryGetValue(unityName, out result))
				return result;
			//Debug.Log(unityName);
			return null;
		}
		
		return result;
#endif
		
		//var xml = new System.Xml.XmlDocument();
		//xml.Load(UnityEngine.Application.dataPath + "/FlipbookGames/ScriptInspector2/Editor/EditorResources/XmlDocs/UnityEngine.xml");
		//var summary = xml.SelectSingleNode("/doc/members/member[@name = 'T:" + FullName + "']/summary");
		//if (summary != null)
		//    return summary.InnerText;
		//return null;
	}

	private string unityHelpName;
	public string UnityHelpName
	{
		get
		{
			if (unityHelpName != null)
			{
				if (unityHelpName == "")
					return null;
				return unityHelpName;
			}
			unityHelpName = "";

			if (kind == SymbolKind.TypeParameter)
				return null;
			
			var result = FullName;
			if (result == null)
				return null;
			if (result.FastStartsWith("UnityEngine."))
				result = result.Substring("UnityEngine.".Length);
			else if (result.FastStartsWith("UnityEditor."))
				result = result.Substring("UnityEditor.".Length);
			else
				return null;
			
			if (kind == SymbolKind.Indexer)
				result = result.Substring(0, result.LastIndexOf(".", StringComparison.Ordinal) + 1) + "Index_operator";
			else if (kind == SymbolKind.Constructor)
				result = result.Substring(0, result.LastIndexOf(".", StringComparison.Ordinal)) + "-ctor";
			else if ((kind == SymbolKind.Field || kind == SymbolKind.Property || kind == SymbolKind.Event) && parentSymbol.kind != SymbolKind.Enum)
				result = result.Substring(0, result.LastIndexOf(".", StringComparison.Ordinal)) + "-" + name;
			
			if ((kind == SymbolKind.Class || kind == SymbolKind.Delegate) && NumTypeParameters > 0)
				result += "_" + NumTypeParameters;

			unityHelpName = result;
			return result;
		}
	}
	
	protected int IndexOfTypeParameter(TypeParameterDefinition tp)
	{
		var typeParams = GetTypeParameters();
		var index = typeParams != null ? typeParams.IndexOf(tp) : -1;
		if (index < 0)
			return parentSymbol != null ? parentSymbol.IndexOfTypeParameter(tp) : -1;
		for (var parent = parentSymbol; parent != null; parent = parent.parentSymbol)
		{
			typeParams = parent.GetTypeParameters();
			if (typeParams != null)
				index += typeParams.Count;
		}
		return index;
	}
	
	public string XmlDocsName
	{
		get
		{
			List<TypeParameterDefinition> tp = null;
			List<SymbolDefinition> parentTypesTypeParams = null;

			var sb = StringBuilders.Alloc();

			switch (kind)
			{
				case SymbolKind.Namespace:
					sb.Append("N:");
					sb.Append(FullName);
					break;
				case SymbolKind.Class:
				case SymbolKind.Struct:
				case SymbolKind.Interface:
				case SymbolKind.Enum:
				case SymbolKind.Delegate:
					sb.Append("T:");
					sb.Append(FullReflectionName);
					break;
				case SymbolKind.Field:
				case SymbolKind.ConstantField:
					sb.Append("F:");
					sb.Append(FullReflectionName);
					break;
				case SymbolKind.Property:
					sb.Append("P:");
					sb.Append(FullReflectionName);
					break;
				case SymbolKind.Indexer:
					sb.Append("P:");
					sb.Append(parentSymbol.FullReflectionName);
					sb.Append(".Item");
					break;
				case SymbolKind.Method:
				case SymbolKind.Operator:
					sb.Append("M:");
					if (parentSymbol.kind == SymbolKind.MethodGroup)
						sb.Append(parentSymbol.parentSymbol.FullReflectionName);
					else
						sb.Append(parentSymbol.FullReflectionName);
					sb.Append('.');
					sb.Append(name);
					tp = GetTypeParameters();
					if (tp != null && tp.Count > 0)
					{
						sb.Append("``");
						sb.Append(tp.Count);
					}
					break;
				case SymbolKind.Constructor:
					sb.Append("M:");
					if (parentSymbol.kind == SymbolKind.MethodGroup)
						sb.Append(parentSymbol.parentSymbol.FullReflectionName);
					else
						sb.Append(parentSymbol.FullReflectionName);
					if (IsStatic)
						sb.Append(".cctor");
					else
						sb.Append(".ctor");
					break;
				case SymbolKind.Destructor:
					sb.Append("M:");
					sb.Append(parentSymbol.FullReflectionName);
					sb.Append(".Finalize");
					break;
				case SymbolKind.Event:
					sb.Append("E:");
					sb.Append(FullReflectionName);
					break;
				default:
					return null;
			}
			var parameters = GetParameters();
			if (kind != SymbolKind.Delegate && parameters != null && parameters.Count > 0)
			{
				parentTypesTypeParams = new List<SymbolDefinition>();
				for (var ps = parentSymbol; ps is TypeDefinitionBase || ps is MethodGroupDefinition; ps = ps.parentSymbol)
				{
					var parentType = ps as TypeDefinitionBase;
					if (parentType == null)
						continue;

					var typeParams = parentType.GetTypeParameters();
					if (typeParams == null)
						continue;

					parentTypesTypeParams.InsertRange(0, typeParams);
				}

				sb.Append("(");
				for (var i = 0; i < parameters.Count; ++i)
				{
					var p = parameters[i];
					if (i > 0)
						sb.Append(",");
					var t = p.TypeOf();
					//if (t.kind == SymbolKind.TypeParameter)
					//{
					//	sb.Append("``");
					//	var tpDef = t as TypeParameterDefinition;
					//	var tpIndex = tpDef.parentSymbol.IndexOfTypeParameter(tpDef);
					//	sb.Append(tpIndex);
					//}
					//else
					{
						t.XMLDocsID(sb, parentTypesTypeParams, tp);
					}
					if (p.IsRef || p.IsOut)
						sb.Append("@");
					if (p.IsOptional)
						sb.Append("!");
				}
				sb.Append(")");
			}
			var result = sb.ToString();
			
			StringBuilders.Release(sb);

			//Debug.Log(result);
			return result;
		}
	}
	
	public string RelativeName(Scope context)
	{
		if (context == null)
			return FullName;
		
		foreach (var kv in builtInTypes)
			if (kv.Value == this)
				return kv.Key;
		
		var thisPath = new List<SymbolDefinition>();
		if (this is TupleTypeDefinition)
		{
			thisPath.Add(this);
		}
		else
		{
			for (var parent = this; parent != null && !(parent is AssemblyDefinition); parent = parent.parentSymbol)
			{
				if (parent is MethodGroupDefinition)
					parent = parent.parentSymbol;
				if (!string.IsNullOrEmpty(parent.name))
					thisPath.Add(parent);
			}
		}
		
		var contextPath = new List<SymbolDefinition>();
		var contextScope = context;
		while (contextScope != null)
		{
			var asNamespaceScope = contextScope as NamespaceScope;
			if (asNamespaceScope != null)
			{
				var nsDefinition = asNamespaceScope.definition;
				while (nsDefinition != null && !string.IsNullOrEmpty(nsDefinition.name))
				{
					contextPath.Add(nsDefinition);
					nsDefinition = nsDefinition.parentSymbol as NamespaceDefinition;
				}
				break;
			}
			else
			{
				var asBodyScope = contextScope as BodyScope;
				if (asBodyScope != null)
				{
					var scopeDefinition = asBodyScope.definition;
					switch (scopeDefinition.kind)
					{
					case SymbolKind.Class:
					case SymbolKind.Struct:
					case SymbolKind.Interface:
						contextPath.Add(scopeDefinition);
						break;
					}
				}
			}
			
			contextScope = contextScope.parentScope;
		}
		
		while (contextPath.Count > 0 && thisPath.Count > 0 && contextPath[contextPath.Count - 1] == thisPath[thisPath.Count - 1])
		{
			contextPath.RemoveAt(contextPath.Count - 1);
			thisPath.RemoveAt(thisPath.Count - 1);
		}
		
		if (thisPath.Count <= 1)
			return name;
		
		NamespaceDefinition thisNamespace = null;
		var index = thisPath.Count;
		while (index --> 0)
		{
			var namespaceDefinition = thisPath[index] as NamespaceDefinition;
			if (namespaceDefinition == null)
				break;
			thisNamespace = namespaceDefinition;
		}
		if (index >= 0 && thisNamespace != null && thisNamespace.parentSymbol != null)
		{
			++index;
			var thisNamespaceName = thisNamespace.FullName;
			
			var contextNamespaceScope = context.EnclosingNamespaceScope();
			while (contextNamespaceScope != null)
			{
				var importedNamespaces = contextNamespaceScope.declaration.importedNamespaces;
				for (var i = importedNamespaces.Count; i --> 0; )
				{
					if (importedNamespaces[i].definition.FullName == thisNamespaceName)
					{
						thisPath.RemoveRange(index, thisPath.Count - index);
						goto namespaceIsImported;
					}
				}
				contextNamespaceScope = contextNamespaceScope.parentScope as NamespaceScope;
			}
		}
		
	namespaceIsImported:
		
		var sb = StringBuilders.Alloc();
		
		for (var i = thisPath.Count; i --> 0; )
		{
			sb.Append(thisPath[i].name);
			var asConstructedType = thisPath[i] as ConstructedTypeDefinition;
			if (asConstructedType != null)
			{
				var typeArguments = asConstructedType.typeArguments;
				if (typeArguments != null && typeArguments.Length > 0)
				{
					var comma = "<";
					for (var j = 0; j < typeArguments.Length; ++j)
					{
						sb.Append(comma);
						if (typeArguments[j] != null)
							sb.Append(typeArguments[j].definition.RelativeName(context));
						comma = ", ";
					}
					sb.Append('>');
				}
			}
			if (i > 0)
				sb.Append('.');
		}
		var result = sb.ToString();
		
		StringBuilders.Release(sb);
		
		return result;
	}

	private string fullName;
	public string FullName
	{
		get
		{
			if (fullName == null)
			{
				if (parentSymbol != null)
				{
					if (parentSymbol is AssemblyDefinition)
						return name;
				
					var parentFullName = (parentSymbol is MethodGroupDefinition)
						? (parentSymbol.parentSymbol ?? unknownSymbol).FullName
						: parentSymbol.FullName;
					if (string.IsNullOrEmpty(name))
						fullName = parentFullName;
					else if (string.IsNullOrEmpty(parentFullName))
						fullName = name;
					else if (name[0] == '.')
						fullName = parentFullName + name;
					else
						fullName = parentFullName + "." + name;
				}
				else
				{
					fullName = name;
				}
			}
			return fullName;
		}
	}

	public virtual void XMLDocsID(StringBuilder sb, List<SymbolDefinition> parentTypesTypeParams, List<TypeParameterDefinition> methodTypeParams)
	{
		if (string.IsNullOrEmpty(name))
			return;
		
		sb.Append(FullReflectionName);
	}

	public string FullReflectionName
	{
		get
		{
			if (parentSymbol != null)
			{
				if (parentSymbol is AssemblyDefinition)
					return ReflectionName;
				
				var parentFullName = (parentSymbol is MethodGroupDefinition)
					? (parentSymbol.parentSymbol ?? unknownSymbol).FullReflectionName
					: parentSymbol.FullReflectionName;
				if (string.IsNullOrEmpty(ReflectionName))
					return parentFullName;
				if (string.IsNullOrEmpty(parentFullName))
					return ReflectionName;
				return parentFullName + "." + ReflectionName;
			}
			return ReflectionName;
		}
	}

	public string Dump()
	{
		var sb = StringBuilders.Alloc();
		Dump(sb, string.Empty);
		var result = sb.ToString();
		StringBuilders.Release(sb);
		return result;
	}

	protected virtual void Dump(StringBuilder sb, string indent)
	{
		sb.AppendLine(indent + kind + " " + name + " (" + GetType() + ")");

		for (var i = 0; i < members.Count; ++i)
			members[i].Dump(sb, indent + "  ");
	}

	public virtual void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs, bool asTypeOnly)
	{
		leaf.resolvedSymbol = null;

		var id = DecodeId(leaf.token.text);

		SymbolDefinition definition;
		if (!members.TryGetValue(id, numTypeArgs, out definition))
		{
			return;
			//if (numTypeArgs > 0)
			//	members.TryGetValue(id, out definition);
		}
		if (definition != null && definition.kind != SymbolKind.Namespace && !(definition is TypeDefinitionBase))
		{
			if (asTypeOnly)
				return;
			if (leaf.parent != null && leaf.parent.RuleName == "typeOrGeneric")
				leaf.semanticError = "Type expected";
		}

		leaf.resolvedSymbol = definition;
	}

	public virtual void ResolveAttributeMember(ParseTree.Leaf leaf, Scope context)
	{
		leaf.resolvedSymbol = null;
		leaf.semanticError = null;

		var id = leaf.token.text;
		leaf.resolvedSymbol = FindName(id + "Attribute", 0, true) ?? FindName(id, 0, true);
	}
	
	public virtual SymbolDefinition ResolveMethodOverloads(ParseTree.Node argumentListNode, TypeReference[] typeArgs, Scope scope, ParseTree.Leaf invokedLeaf)
	{
		throw new InvalidOperationException();
	}
	
	private static Dictionary<Operator.ID, List<MethodDefinition>> _predefinedOperators;
	internal static Dictionary<Operator.ID, List<MethodDefinition>> PredefinedOperators {
		get {
			if (_predefinedOperators == null)
			{
				_predefinedOperators = new Dictionary<Operator.ID, List<MethodDefinition>>();
				
				_predefinedOperators[Operator.ID.op_Addition] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_Addition", builtInTypes_int, builtInTypes_int, builtInTypes_int),
					MethodDefinition.CreateOperator("op_Addition", builtInTypes_uint, builtInTypes_uint, builtInTypes_uint),
					MethodDefinition.CreateOperator("op_Addition", builtInTypes_long, builtInTypes_long, builtInTypes_long),
					MethodDefinition.CreateOperator("op_Addition", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_ulong),
					MethodDefinition.CreateOperator("op_Addition", builtInTypes_float, builtInTypes_float, builtInTypes_float),
					MethodDefinition.CreateOperator("op_Addition", builtInTypes_double, builtInTypes_double, builtInTypes_double),
					MethodDefinition.CreateOperator("op_Addition", builtInTypes_decimal, builtInTypes_decimal, builtInTypes_decimal),
					MethodDefinition.CreateOperator("op_Addition", builtInTypes_string, builtInTypes_string, builtInTypes_string),
					MethodDefinition.CreateOperator("op_Addition", builtInTypes_string, builtInTypes_string, builtInTypes_object),
					MethodDefinition.CreateOperator("op_Addition", builtInTypes_string, builtInTypes_object, builtInTypes_string),
				};
				
				_predefinedOperators[Operator.ID.op_Subtraction] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_Subtraction", builtInTypes_int, builtInTypes_int, builtInTypes_int),
					MethodDefinition.CreateOperator("op_Subtraction", builtInTypes_uint, builtInTypes_uint, builtInTypes_uint),
					MethodDefinition.CreateOperator("op_Subtraction", builtInTypes_long, builtInTypes_long, builtInTypes_long),
					MethodDefinition.CreateOperator("op_Subtraction", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_ulong),
					MethodDefinition.CreateOperator("op_Subtraction", builtInTypes_float, builtInTypes_float, builtInTypes_float),
					MethodDefinition.CreateOperator("op_Subtraction", builtInTypes_double, builtInTypes_double, builtInTypes_double),
					MethodDefinition.CreateOperator("op_Subtraction", builtInTypes_decimal, builtInTypes_decimal, builtInTypes_decimal),
				};
				
				_predefinedOperators[Operator.ID.op_Multiply] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_Multiply", builtInTypes_int, builtInTypes_int, builtInTypes_int),
					MethodDefinition.CreateOperator("op_Multiply", builtInTypes_uint, builtInTypes_uint, builtInTypes_uint),
					MethodDefinition.CreateOperator("op_Multiply", builtInTypes_long, builtInTypes_long, builtInTypes_long),
					MethodDefinition.CreateOperator("op_Multiply", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_ulong),
					MethodDefinition.CreateOperator("op_Multiply", builtInTypes_float, builtInTypes_float, builtInTypes_float),
					MethodDefinition.CreateOperator("op_Multiply", builtInTypes_double, builtInTypes_double, builtInTypes_double),
					MethodDefinition.CreateOperator("op_Multiply", builtInTypes_decimal, builtInTypes_decimal, builtInTypes_decimal),
				};
				
				_predefinedOperators[Operator.ID.op_Division] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_Division", builtInTypes_int, builtInTypes_int, builtInTypes_int),
					MethodDefinition.CreateOperator("op_Division", builtInTypes_uint, builtInTypes_uint, builtInTypes_uint),
					MethodDefinition.CreateOperator("op_Division", builtInTypes_long, builtInTypes_long, builtInTypes_long),
					MethodDefinition.CreateOperator("op_Division", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_ulong),
					MethodDefinition.CreateOperator("op_Division", builtInTypes_float, builtInTypes_float, builtInTypes_float),
					MethodDefinition.CreateOperator("op_Division", builtInTypes_double, builtInTypes_double, builtInTypes_double),
					MethodDefinition.CreateOperator("op_Division", builtInTypes_decimal, builtInTypes_decimal, builtInTypes_decimal),
				};
				
				_predefinedOperators[Operator.ID.op_Modulus] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_Modulus", builtInTypes_int, builtInTypes_int, builtInTypes_int),
					MethodDefinition.CreateOperator("op_Modulus", builtInTypes_uint, builtInTypes_uint, builtInTypes_uint),
					MethodDefinition.CreateOperator("op_Modulus", builtInTypes_long, builtInTypes_long, builtInTypes_long),
					MethodDefinition.CreateOperator("op_Modulus", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_ulong),
					MethodDefinition.CreateOperator("op_Modulus", builtInTypes_float, builtInTypes_float, builtInTypes_float),
					MethodDefinition.CreateOperator("op_Modulus", builtInTypes_double, builtInTypes_double, builtInTypes_double),
					MethodDefinition.CreateOperator("op_Modulus", builtInTypes_decimal, builtInTypes_decimal, builtInTypes_decimal),
				};
				
				_predefinedOperators[Operator.ID.op_LogicalAnd] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_LogicalAnd", builtInTypes_bool, builtInTypes_bool, builtInTypes_bool),
				};
				
				_predefinedOperators[Operator.ID.op_LogicalOr] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_LogicalOr", builtInTypes_bool, builtInTypes_bool, builtInTypes_bool),
				};
				
				_predefinedOperators[Operator.ID.op_BitwiseAnd] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_BitwiseAnd", builtInTypes_bool, builtInTypes_bool, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_BitwiseAnd", builtInTypes_int, builtInTypes_int, builtInTypes_int),
					MethodDefinition.CreateOperator("op_BitwiseAnd", builtInTypes_uint, builtInTypes_uint, builtInTypes_uint),
					MethodDefinition.CreateOperator("op_BitwiseAnd", builtInTypes_long, builtInTypes_long, builtInTypes_long),
					MethodDefinition.CreateOperator("op_BitwiseAnd", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_ulong),
				};
				
				_predefinedOperators[Operator.ID.op_BitwiseOr] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_BitwiseOr", builtInTypes_bool, builtInTypes_bool, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_BitwiseOr", builtInTypes_int, builtInTypes_int, builtInTypes_int),
					MethodDefinition.CreateOperator("op_BitwiseOr", builtInTypes_uint, builtInTypes_uint, builtInTypes_uint),
					MethodDefinition.CreateOperator("op_BitwiseOr", builtInTypes_long, builtInTypes_long, builtInTypes_long),
					MethodDefinition.CreateOperator("op_BitwiseOr", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_ulong),
				};
				
				_predefinedOperators[Operator.ID.op_ExclusiveOr] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_ExclusiveOr", builtInTypes_bool, builtInTypes_bool, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_ExclusiveOr", builtInTypes_int, builtInTypes_int, builtInTypes_int),
					MethodDefinition.CreateOperator("op_ExclusiveOr", builtInTypes_uint, builtInTypes_uint, builtInTypes_uint),
					MethodDefinition.CreateOperator("op_ExclusiveOr", builtInTypes_long, builtInTypes_long, builtInTypes_long),
					MethodDefinition.CreateOperator("op_ExclusiveOr", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_ulong),
				};
				
				_predefinedOperators[Operator.ID.op_LeftShift] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_LeftShift", builtInTypes_int, builtInTypes_int, builtInTypes_int),
					MethodDefinition.CreateOperator("op_LeftShift", builtInTypes_uint, builtInTypes_uint, builtInTypes_int),
					MethodDefinition.CreateOperator("op_LeftShift", builtInTypes_long, builtInTypes_long, builtInTypes_int),
					MethodDefinition.CreateOperator("op_LeftShift", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_int),
				};
				
				_predefinedOperators[Operator.ID.op_RightShift] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_RightShift", builtInTypes_int, builtInTypes_int, builtInTypes_int),
					MethodDefinition.CreateOperator("op_RightShift", builtInTypes_uint, builtInTypes_uint, builtInTypes_int),
					MethodDefinition.CreateOperator("op_RightShift", builtInTypes_long, builtInTypes_long, builtInTypes_int),
					MethodDefinition.CreateOperator("op_RightShift", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_int),
				};
				
				_predefinedOperators[Operator.ID.op_LessThan] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_LessThan", builtInTypes_int, builtInTypes_int, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThan", builtInTypes_uint, builtInTypes_uint, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThan", builtInTypes_long, builtInTypes_long, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThan", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThan", builtInTypes_float, builtInTypes_float, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThan", builtInTypes_double, builtInTypes_double, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThan", builtInTypes_decimal, builtInTypes_decimal, builtInTypes_bool),
				};
				
				_predefinedOperators[Operator.ID.op_LessThanOrEqual] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_LessThanOrEqual", builtInTypes_int, builtInTypes_int, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThanOrEqual", builtInTypes_uint, builtInTypes_uint, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThanOrEqual", builtInTypes_long, builtInTypes_long, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThanOrEqual", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThanOrEqual", builtInTypes_float, builtInTypes_float, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThanOrEqual", builtInTypes_double, builtInTypes_double, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_LessThanOrEqual", builtInTypes_decimal, builtInTypes_decimal, builtInTypes_bool),
				};
				
				_predefinedOperators[Operator.ID.op_GreaterThan] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_GreaterThan", builtInTypes_int, builtInTypes_int, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThan", builtInTypes_uint, builtInTypes_uint, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThan", builtInTypes_long, builtInTypes_long, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThan", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThan", builtInTypes_float, builtInTypes_float, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThan", builtInTypes_double, builtInTypes_double, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThan", builtInTypes_decimal, builtInTypes_decimal, builtInTypes_bool),
				};
				
				_predefinedOperators[Operator.ID.op_GreaterThanOrEqual] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_GreaterThanOrEqual", builtInTypes_int, builtInTypes_int, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThanOrEqual", builtInTypes_uint, builtInTypes_uint, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThanOrEqual", builtInTypes_long, builtInTypes_long, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThanOrEqual", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThanOrEqual", builtInTypes_float, builtInTypes_float, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThanOrEqual", builtInTypes_double, builtInTypes_double, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_GreaterThanOrEqual", builtInTypes_decimal, builtInTypes_decimal, builtInTypes_bool),
				};
				
				_predefinedOperators[Operator.ID.op_Equality] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_Equality", builtInTypes_bool, builtInTypes_bool, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Equality", builtInTypes_string, builtInTypes_string, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Equality", builtInTypes_int, builtInTypes_int, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Equality", builtInTypes_uint, builtInTypes_uint, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Equality", builtInTypes_long, builtInTypes_long, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Equality", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Equality", builtInTypes_float, builtInTypes_float, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Equality", builtInTypes_double, builtInTypes_double, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Equality", builtInTypes_decimal, builtInTypes_decimal, builtInTypes_bool),
				};
				
				_predefinedOperators[Operator.ID.op_Inequality] = new List<MethodDefinition> {
					MethodDefinition.CreateOperator("op_Inequality", builtInTypes_bool, builtInTypes_bool, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Inequality", builtInTypes_string, builtInTypes_string, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Inequality", builtInTypes_int, builtInTypes_int, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Inequality", builtInTypes_uint, builtInTypes_uint, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Inequality", builtInTypes_long, builtInTypes_long, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Inequality", builtInTypes_ulong, builtInTypes_ulong, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Inequality", builtInTypes_float, builtInTypes_float, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Inequality", builtInTypes_double, builtInTypes_double, builtInTypes_bool),
					MethodDefinition.CreateOperator("op_Inequality", builtInTypes_decimal, builtInTypes_decimal, builtInTypes_bool),
				};
			}
			
			return _predefinedOperators;
		}
	}
	
	private static List<TypeDefinitionBase> _processedTypes = new List<TypeDefinitionBase>(16);

	internal static SymbolDefinition ResolveBinaryOperation(Operator.ID operatorID, SymbolDefinition lhs, SymbolDefinition rhs, Scope scope)
	{
		var result = _ResolveBinaryOperation(operatorID, lhs, rhs, scope);
		//if (result != null && result.TypeOf() != unknownType)
			return result;
		
		//TypeDefinitionBase lhsType = lhs.TypeOf() as ConstructedTypeDefinition;
		//TypeDefinitionBase rhsType = rhs.TypeOf() as ConstructedTypeDefinition;
		//if ((lhsType == null || lhsType.GetGenericSymbol() != builtInTypes_Nullable) &&
		//	(rhsType == null || rhsType.GetGenericSymbol() != builtInTypes_Nullable))
		//{
		//	return null;
		//}
		
		//if (lhsType != null && lhsType.GetGenericSymbol() == builtInTypes_Nullable)
		//{
		//	lhsType = lhsType.GetTypeArgument(0);
		//	lhs = lhsType.GetThisInstance();
		//}
		//if (rhsType != null && rhsType.GetGenericSymbol() == builtInTypes_Nullable)
		//{
		//	rhsType = rhsType.GetTypeArgument(0);
		//	rhs = rhsType.GetThisInstance();
		//}
		
		//result = _ResolveBinaryOperation(operatorID, lhs, rhs, scope);
		//if (result == null)
		//	return null;
		
		//var resultType = result.TypeOf() as TypeDefinitionBase;
		//if (resultType == null || resultType.kind == SymbolKind.Error || resultType.GetGenericSymbol() == builtInTypes_Nullable)
		//	return null;

		//if (operatorID <= Operator.ID.LastNonComparisonOperator)
		//	resultType = resultType.MakeNullableType();
		//else if (resultType != builtInTypes_bool)
		//	return null;

		//result = resultType.GetThisInstance();
		//return result;
	}

	private static SymbolDefinition _ResolveBinaryOperation(Operator.ID operatorID, SymbolDefinition lhs, SymbolDefinition rhs, Scope scope)
	{
		var lhsType = lhs.TypeOf() as TypeDefinitionBase;
		var rhsType = rhs.TypeOf() as TypeDefinitionBase;
		var argsBaseIndex = MethodGroupDefinition.resolvedArgumentsStack.Count;
		
		var argumentTypesStack = MethodGroupDefinition.argumentTypesStack;
		var resolvedArgumentsStack = MethodGroupDefinition.resolvedArgumentsStack;
		var modifiersStack = MethodGroupDefinition.modifiersStack;
		var argumentsStack = MethodGroupDefinition.argumentNodesStack;
		
		argumentTypesStack.Add(lhsType);
		argumentTypesStack.Add(rhsType);
		resolvedArgumentsStack.Add(lhs);
		resolvedArgumentsStack.Add(rhs);
		modifiersStack.Add(Modifiers.None);
		modifiersStack.Add(Modifiers.None);
		MethodGroupDefinition.namedArgumentsStack.Add(null);
		MethodGroupDefinition.namedArgumentsStack.Add(null);
		argumentsStack.Add(null);
		argumentsStack.Add(null);
		
		var candidatesStack = MethodGroupDefinition.methodCandidatesStack;
		int lhsBaseIndex = candidatesStack.Count;
		int numLhsCandidates = 0;
		
		_processedTypes.Clear();
		
		var operatorName = Operator.dotNetName[(int)operatorID];

		bool collectEnumPredefinedOperators = false;

	CollectFromEnumTypes:

		var type = lhsType;
		if (type != null && collectEnumPredefinedOperators == (type.kind == SymbolKind.Enum))
		{
			while (type != null && type != builtInTypes_object)
			{
				_processedTypes.Add(type);

				var methodGroup = type.FindName(operatorName, 0, false) as MethodGroupDefinition;
				if (methodGroup != null)
				{
					numLhsCandidates = methodGroup.CollectCandidates(2, scope, null);
					if (numLhsCandidates > 0)
					{
						for (var i = numLhsCandidates; i --> 0; )
						{
							var candidate = candidatesStack[lhsBaseIndex + i];

							var leftType = candidate.parameters[0].TypeOf() as TypeDefinitionBase;
							var rightType = candidate.parameters[1].TypeOf() as TypeDefinitionBase;
							var retType = candidate.ReturnType();
							if (!leftType.IsReferenceType && !rightType.IsReferenceType && !retType.IsReferenceType)
							{
								var nullableLeftType = leftType.MakeNullableType();
								var nullableRightType = rightType.MakeNullableType();
								if (lhsType.CanConvertTo(nullableLeftType) && rhsType.CanConvertTo(nullableRightType))
								{
									if (operatorID < Operator.ID.FirstComparisonOperator)
										retType = retType.MakeNullableType();

									var liftedOperator = MethodDefinition.CreateOperator(candidate.name, retType, nullableLeftType, nullableRightType);
									liftedOperator.isLiftedOperator = true;
									liftedOperator.parentSymbol = candidate.parentSymbol;
									candidatesStack.Add(liftedOperator);
								}
							}

							if (!lhsType.CanConvertTo(leftType) || !rhsType.CanConvertTo(rightType))
							{
								candidatesStack.RemoveAt(lhsBaseIndex + i);
							}
						}
						numLhsCandidates = candidatesStack.Count - lhsBaseIndex;
					}
				}

				if (numLhsCandidates != 0)
					break;

				if (collectEnumPredefinedOperators)
					break;
				else
					type = type.BaseType();
			};
		}
		
		int rhsBaseIndex = candidatesStack.Count;
		int numRhsCandidates = 0;
		
		type = rhsType;
		if (type != null && collectEnumPredefinedOperators == (type.kind == SymbolKind.Enum))
		{
			while (type != null && type != builtInTypes_object)
			{
				if (_processedTypes.Contains(type))
					break;
			
				var methodGroup = type.FindName(operatorName, 0, false) as MethodGroupDefinition;
				if (methodGroup != null)
				{
					numRhsCandidates = methodGroup.CollectCandidates(2, scope, null);
					if (numRhsCandidates != 0)
					{
						for (var i = numRhsCandidates; i --> 0; )
						{
							var candidate = candidatesStack[rhsBaseIndex + i];

							var leftType = candidate.parameters[0].TypeOf() as TypeDefinitionBase;
							var rightType = candidate.parameters[1].TypeOf() as TypeDefinitionBase;
							var retType = candidate.ReturnType();
							if (!leftType.IsReferenceType && !rightType.IsReferenceType && !retType.IsReferenceType)
							{
								var nullableLeftType = leftType.MakeNullableType();
								var nullableRightType = rightType.MakeNullableType();
								if (lhsType.CanConvertTo(nullableLeftType) && rhsType.CanConvertTo(nullableRightType))
								{
									if (operatorID < Operator.ID.FirstComparisonOperator)
										retType = retType.MakeNullableType();

									var liftedOperator = MethodDefinition.CreateOperator(candidate.name, retType, nullableLeftType, nullableRightType);
									liftedOperator.isLiftedOperator = true;
									liftedOperator.parentSymbol = candidate.parentSymbol;
									candidatesStack.Add(liftedOperator);
								}
							}

							if (!lhsType.CanConvertTo(leftType) || !rhsType.CanConvertTo(rightType))
							{
								candidatesStack.RemoveAt(rhsBaseIndex + i);
							}
						}
						numRhsCandidates = candidatesStack.Count - rhsBaseIndex;
					}
				}
			
				if (numRhsCandidates != 0)
					break;

				if (collectEnumPredefinedOperators)
					break;
				else
					type = type.BaseType();
			};
		}
		
		numLhsCandidates += numRhsCandidates;
		
		if (numLhsCandidates == 0 && !collectEnumPredefinedOperators)
		{
			collectEnumPredefinedOperators = true;
			goto CollectFromEnumTypes;
		}

		if (numLhsCandidates == 0 && collectEnumPredefinedOperators)
		{
			List<MethodDefinition> predefinedOps;
			if (!PredefinedOperators.TryGetValue(operatorID, out predefinedOps))
			{
				Debug.LogError("Unknown predefined operator name: " + operatorID);
				
				var result = argumentTypesStack[0].GetThisInstance();
				
				modifiersStack.RemoveRange(argsBaseIndex, 2);
				argumentTypesStack.RemoveRange(argsBaseIndex, 2);
				resolvedArgumentsStack.RemoveRange(argsBaseIndex, 2);
				MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, 2);
				argumentsStack.RemoveRange(argsBaseIndex, 2);
				
				return result;
			}

			if (lhsType == null || rhsType == null)
			{
				#if SI3_WARNINGS
				Debug.LogError("lhsType: " + lhsType + "  rhsType: " + rhsType);
				#endif

				modifiersStack.RemoveRange(argsBaseIndex, 2);
				argumentTypesStack.RemoveRange(argsBaseIndex, 2);
				resolvedArgumentsStack.RemoveRange(argsBaseIndex, 2);
				MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, 2);
				argumentsStack.RemoveRange(argsBaseIndex, 2);
				
				return null;
			}

			SymbolDefinition resultType = null;
			
			TypeDefinitionBase enumType = null;
			TypeDefinitionBase underlyingEnumType = null;
			
			if (operatorID == Operator.ID.op_Equality || operatorID == Operator.ID.op_Inequality)
			{
				if (lhsType.IsReferenceType && rhsType.IsReferenceType)
				{
					if (lhsType.CanConvertTo(rhsType) || rhsType.CanConvertTo(lhsType))
					{
						resultType = builtInTypes_bool;
					}
					
					modifiersStack.RemoveRange(argsBaseIndex, 2);
					argumentTypesStack.RemoveRange(argsBaseIndex, 2);
					resolvedArgumentsStack.RemoveRange(argsBaseIndex, 2);
					MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, 2);
					argumentsStack.RemoveRange(argsBaseIndex, 2);
					
					return resultType;
				}
			}
			
			//if (lhsType.kind == SymbolKind.Enum || rhsType.kind == SymbolKind.Enum)
			//{
			//	if (lhsType is IntegerLiteralTypeZero)
			//	{
			//		lhsType = rhsType;
			//	}
			//	else if (rhsType is IntegerLiteralTypeZero)
			//	{
			//		rhsType = lhsType;
			//	}
				
			//	if (lhsType == rhsType)
			//	{
			//		var reflectedEnumType = lhsType as ReflectedEnumType;
			//		if (reflectedEnumType != null)
			//			underlyingEnumType = reflectedEnumType.UnderlyingType.definition as TypeDefinitionBase;
			//		else if (!(lhsType is EnumTypeDefinition))
			//		{
			//			#if SI3_WARNINGS
			//			Debug.LogWarning("lhsType: " + lhsType);
			//			#endif
			//		}
			//		else
			//			underlyingEnumType = ((EnumTypeDefinition)lhsType).UnderlyingType.definition as TypeDefinitionBase;
			//	}
			//	else
			//	{
			//		modifiersStack.RemoveRange(argsBaseIndex, 2);
			//		argumentTypesStack.RemoveRange(argsBaseIndex, 2);
			//		resolvedArgumentsStack.RemoveRange(argsBaseIndex, 2);
			//		MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, 2);
			//		argumentsStack.RemoveRange(argsBaseIndex, 2);
				
			//		return null;
			//	}
				
			//	if (underlyingEnumType != null)
			//	{
			//		enumType = lhsType;
					
			//		lhsType = underlyingEnumType;
			//		rhsType = underlyingEnumType;
			//	}
			//}
			
			// Shortcuts for binary numeric promotion
			if (operatorID != Operator.ID.op_LeftShift && operatorID != Operator.ID.op_RightShift
				//&& predefinedOps.Count == 0
				/*&& !(lhsType is IntegerLiteralType) && !(rhsType is IntegerLiteralType)*/)
			{
				var lhsPromotedType = lhsType.TypeOf() as TypeDefinitionBase ?? unknownType;
				var rhsPromotedType = rhsType.TypeOf() as TypeDefinitionBase ?? unknownType;
			
				if (lhsPromotedType == builtInTypes_decimal)
				{
					rhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_decimal);
					if (rhsPromotedType != null && rhsPromotedType.kind != SymbolKind.Error)
						resultType = builtInTypes_decimal;
				}
				else if (rhsPromotedType == builtInTypes_decimal)
				{
					lhsPromotedType = lhsPromotedType.ConvertTo(builtInTypes_decimal);
					if (lhsPromotedType != null && lhsPromotedType.kind != SymbolKind.Error)
						resultType = builtInTypes_decimal;
				}
				else if (lhsPromotedType == builtInTypes_double)
				{
					rhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_double);
					if (rhsPromotedType != null && rhsPromotedType.kind != SymbolKind.Error)
						resultType = builtInTypes_double;
				}
				else if (rhsPromotedType == builtInTypes_double)
				{
					lhsPromotedType = lhsPromotedType.ConvertTo(builtInTypes_double);
					if (lhsPromotedType != null && lhsPromotedType.kind != SymbolKind.Error)
						resultType = builtInTypes_double;
				}
				else if (lhsPromotedType == builtInTypes_float)
				{
					rhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_float);
					if (rhsPromotedType != null && rhsPromotedType.kind != SymbolKind.Error)
						resultType = builtInTypes_float;
				}
				else if (rhsPromotedType == builtInTypes_float)
				{
					lhsPromotedType = lhsPromotedType.ConvertTo(builtInTypes_float);
					if (lhsPromotedType != null && lhsPromotedType.kind != SymbolKind.Error)
						resultType = builtInTypes_float;
				}
				//else if (lhsPromotedType == builtInTypes_ulong)
				//{
				//	if (rhsPromotedType == builtInTypes_int ||
				//		rhsPromotedType == builtInTypes_short ||
				//		rhsPromotedType == builtInTypes_sbyte ||
				//		rhsPromotedType == builtInTypes_long)
				//	{
				//		resultType = MethodGroupDefinition.ambiguousMethodOverload;
				//	}
				//	else
				//	{
				//		rhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_ulong);
				//		if (rhsPromotedType != null && rhsPromotedType.kind != SymbolKind.Error)
				//			resultType = builtInTypes_ulong;
				//	}
				//}
				//else if (rhsPromotedType == builtInTypes_ulong)
				//{
				//	if (lhsPromotedType == builtInTypes_int ||
				//		lhsPromotedType == builtInTypes_short ||
				//		lhsPromotedType == builtInTypes_sbyte ||
				//		lhsPromotedType == builtInTypes_long)
				//	{
				//		resultType = MethodGroupDefinition.ambiguousMethodOverload;
				//	}
				//	else
				//	{
				//		lhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_ulong);
				//		if (rhsPromotedType != null && rhsPromotedType.kind != SymbolKind.Error)
				//			resultType = builtInTypes_ulong;
				//	}
				//}
				//else if (lhsPromotedType == builtInTypes_long)
				//{
				//	rhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_long);
				//	if (rhsPromotedType != null && rhsPromotedType.kind != SymbolKind.Error)
				//		resultType = builtInTypes_long;
				//}
				//else if (rhsPromotedType == builtInTypes_long)
				//{
				//	lhsPromotedType = lhsPromotedType.ConvertTo(builtInTypes_long);
				//	if (lhsPromotedType != null && lhsPromotedType.kind != SymbolKind.Error)
				//		resultType = builtInTypes_long;
				//}
				//else if (lhsPromotedType == builtInTypes_uint)
				//{
				//	if (rhsPromotedType == builtInTypes_int ||
				//		rhsPromotedType == builtInTypes_short ||
				//		rhsPromotedType == builtInTypes_sbyte)
				//	{
				//		lhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_long);
				//		rhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_long);
				//		if (lhsPromotedType == builtInTypes_long && rhsPromotedType == builtInTypes_long)
				//			resultType = builtInTypes_long;
				//	}
				//	else
				//	{
				//		rhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_uint);
				//		if (rhsPromotedType == builtInTypes_uint)
				//			resultType = builtInTypes_uint;
				//	}
				//}
				//else if (rhsPromotedType == builtInTypes_uint)
				//{
				//	if (lhsPromotedType == builtInTypes_int ||
				//		lhsPromotedType == builtInTypes_short ||
				//		lhsPromotedType == builtInTypes_sbyte)
				//	{
				//		lhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_long);
				//		rhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_long);
				//		if (lhsPromotedType == builtInTypes_long && rhsPromotedType == builtInTypes_long)
				//			resultType = builtInTypes_long;
				//	}
				//	else
				//	{
				//		lhsPromotedType = lhsPromotedType.ConvertTo(builtInTypes_uint);
				//		if (lhsPromotedType == builtInTypes_uint)
				//			resultType = builtInTypes_uint;
				//	}
				//}
				//else
				//{
				//	lhsPromotedType = lhsPromotedType.ConvertTo(builtInTypes_int);
				//	if (lhsPromotedType == builtInTypes_int)
				//	{
				//		rhsPromotedType = rhsPromotedType.ConvertTo(builtInTypes_int);
				//		if (rhsPromotedType == builtInTypes_int)
				//			resultType = builtInTypes_int;
				//	}
				//}
			
				switch (operatorID)
				{
					case Operator.ID.op_Equality:
					case Operator.ID.op_GreaterThan:
					case Operator.ID.op_LessThan:
					case Operator.ID.op_Inequality:
					case Operator.ID.op_GreaterThanOrEqual:
					case Operator.ID.op_LessThanOrEqual:
					case Operator.ID.op_LogicalAnd:
					case Operator.ID.op_LogicalOr:
						resultType = builtInTypes_bool;
						break;
				}

				if (resultType != null && resultType.kind != SymbolKind.Error)
				{
					switch (operatorID)
					{
					case Operator.ID.op_Addition:
					case Operator.ID.op_Subtraction:
					case Operator.ID.op_BitwiseAnd:
					case Operator.ID.op_BitwiseOr:
					case Operator.ID.op_ExclusiveOr:
						if (underlyingEnumType != null)
							resultType = enumType;
						break;
					case Operator.ID.op_Multiply:
					case Operator.ID.op_Division:
					case Operator.ID.op_Modulus:
						break;
					case Operator.ID.op_Equality:
					case Operator.ID.op_GreaterThan:
					case Operator.ID.op_LessThan:
					case Operator.ID.op_Inequality:
					case Operator.ID.op_GreaterThanOrEqual:
					case Operator.ID.op_LessThanOrEqual:
					case Operator.ID.op_LogicalAnd:
					case Operator.ID.op_LogicalOr:
						//if (resultType.kind != SymbolKind.Error)
						resultType = builtInTypes_bool;
						break;
					default:
						//if (resultType.kind != SymbolKind.Error)
						resultType = null;
						break;
					}
				
					if (resultType != null)
					{
						modifiersStack.RemoveRange(argsBaseIndex, 2);
						argumentTypesStack.RemoveRange(argsBaseIndex, 2);
						resolvedArgumentsStack.RemoveRange(argsBaseIndex, 2);
						MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, 2);
						argumentsStack.RemoveRange(argsBaseIndex, 2);
				
						var asType = resultType as TypeDefinitionBase;
						if (asType != null)
							return asType.GetThisInstance();
						else
							return resultType;
					}
				}
			}
			
			numLhsCandidates += predefinedOps.Count;
			candidatesStack.AddRange(predefinedOps);

			//for (var i = numLhsCandidates; i --> 0; )
			//{
			//	var candidate = candidatesStack[lhsBaseIndex + i];
			//	if (!lhsType.CanConvertTo(candidate.parameters[0].TypeOf() as TypeDefinitionBase) ||
			//		!rhsType.CanConvertTo(candidate.parameters[1].TypeOf() as TypeDefinitionBase) ||
			//		lhsType.TypeOf() != candidate.parameters[0].TypeOf() && rhsType.TypeOf() != candidate.parameters[1].TypeOf())
			//	{
			//		candidatesStack.RemoveAt(lhsBaseIndex + i);
			//	}
			//}
		}
		
		MethodDefinition resolvedOverload = null;
		numLhsCandidates = candidatesStack.Count - lhsBaseIndex;
		if (numLhsCandidates != 0)
		{
			//// Add lifted operators for +, -, *, /, %, &, |, ^, <<, and >>.
			//if (operatorID == Operator.ID.op_Addition)
			//{
			//	for (var i = numLhsCandidates; i --> 0; )
			//	{
			//		var candidate = candidatesStack[lhsBaseIndex + i];
			//		var leftType = candidate.parameters[0].TypeOf() as TypeDefinitionBase;
			//		var rightType = candidate.parameters[1].TypeOf() as TypeDefinitionBase;
			//		var retType = candidate.ReturnType();
			//		if (leftType.IsReferenceType || rightType.IsReferenceType || retType.IsReferenceType)
			//		{
			//			continue;
			//		}

			//		leftType = leftType.MakeNullableType();
			//		rightType = rightType.MakeNullableType();
			//		retType = retType.MakeNullableType();

			//		var liftedOperator = MethodDefinition.CreateOperator(candidate.name, retType, leftType, rightType);
			//		liftedOperator.parentSymbol = candidate.parentSymbol;

			//		candidatesStack.Add(liftedOperator);
			//		++numLhsCandidates;
			//	}
			//}

			resolvedOverload = MethodGroupDefinition.ResolveMethodOverloads(2, numLhsCandidates);
			candidatesStack.RemoveRange(lhsBaseIndex, candidatesStack.Count - lhsBaseIndex);
		}
			
		modifiersStack.RemoveRange(argsBaseIndex, 2);
		argumentTypesStack.RemoveRange(argsBaseIndex, 2);
		resolvedArgumentsStack.RemoveRange(argsBaseIndex, 2);
		MethodGroupDefinition.namedArgumentsStack.RemoveRange(argsBaseIndex, 2);
		argumentsStack.RemoveRange(argsBaseIndex, 2);
		
		if (resolvedOverload == null)
			return null;

		var returnType = resolvedOverload.ReturnType();
		return returnType == null ? null : returnType.GetThisInstance();
	}
	
	public static Dictionary<CharSpan, TypeDefinitionBase> builtInTypes;
	
	public static TypeDefinition builtInTypes_int;
	public static TypeDefinition builtInTypes_uint;
	public static TypeDefinition builtInTypes_byte;
	public static TypeDefinition builtInTypes_sbyte;
	public static TypeDefinition builtInTypes_short;
	public static TypeDefinition builtInTypes_ushort;
	public static TypeDefinition builtInTypes_long;
	public static TypeDefinition builtInTypes_ulong;
	public static TypeDefinition builtInTypes_float;
	public static TypeDefinition builtInTypes_double;
	public static TypeDefinition builtInTypes_decimal;
	public static TypeDefinition builtInTypes_char;
	public static TypeDefinition builtInTypes_string;
	public static TypeDefinition builtInTypes_bool;
	public static TypeDefinition builtInTypes_object;
	public static TypeDefinition builtInTypes_void;
	
	public static TypeDefinition builtInTypes_Array;
	public static TypeDefinition builtInTypes_Nullable;
	public static TypeDefinition builtInTypes_IEnumerable;
	public static TypeDefinition builtInTypes_IEnumerable_1;
	public static TypeDefinition builtInTypes_Exception;
	public static TypeDefinition builtInTypes_Enum;
	public static TypeDefinition builtInTypes_Expression_1;
	
	public static TypeDefinitionBase builtInTypes_Type;
	public static TypeDefinitionBase builtInTypes_dynamic;
	
	public static TypeDefinition builtInTypes_Index;
	public static TypeDefinition builtInTypes_Range;

	public static TypeDefinition builtInTypes_Task;
	public static TypeDefinition builtInTypes_Task_1;
	public static TypeDefinition builtInTypes_INotifyCompletion;
	
	public static TypeDefinition builtInTypes_ValueTuple_1;
	public static TypeDefinition builtInTypes_ValueTuple_2;
	public static TypeDefinition builtInTypes_ValueTuple_3;
	public static TypeDefinition builtInTypes_ValueTuple_4;
	public static TypeDefinition builtInTypes_ValueTuple_5;
	public static TypeDefinition builtInTypes_ValueTuple_6;
	public static TypeDefinition builtInTypes_ValueTuple_7;
	public static TypeDefinition builtInTypes_ValueTuple_8;

	//public static HashSet<string> missingResolveNodePaths = new HashSet<string>();
	
	public static SymbolDefinition ResolveNodeAsConstructor(ParseTree.BaseNode oceNode, Scope scope, SymbolDefinition asMemberOf)
	{
		if (asMemberOf == null)
			return null;

		var node = oceNode as ParseTree.Node;
		if (node == null || node.numValidNodes == 0)
			return null;

		var node1 = node.RuleName == "arguments" ? node : node.NodeAt(0);
		if (node1 == null)
			return null;

		var constructor = asMemberOf.FindName(".ctor", 0, false);
		var asConstructedType = asMemberOf as ConstructedTypeDefinition;
		if (asConstructedType != null && constructor != null)
		{
			constructor = asConstructedType.GetConstructedMember(constructor);
		}

		if (constructor == null || constructor.parentSymbol != asMemberOf)
		{
			var type = asMemberOf as TypeDefinitionBase;
			constructor = type != null ? type.GetDefaultConstructor() : null;
		}
		if (constructor.GetGenericSymbol() is MethodGroupDefinition)
		{
			if (node1.RuleName == "arguments")
			{
				constructor = ResolveNode(node1, scope, constructor);
			}
			else
			{
				var type = asMemberOf as TypeDefinitionBase;
				constructor = type != null ? type.GetDefaultConstructor() : null;
			}
			if (constructor == null || constructor.kind == SymbolKind.Error)
				return constructor;
		}
		else if (node1.RuleName == "arguments")
		{
			for (var i = 1; i < node1.numValidNodes - 1; ++i)
				ResolveNode(node1.ChildAt(i), scope, constructor);
		}

		if (asConstructedType != null)
		{
			constructor = asConstructedType.GetConstructedMember(constructor);
		}

		if (node1.RuleName == "arguments" && constructor != null)
		{
			var prevNode = node.FindPreviousNode();
			var constructorLeaf = prevNode as ParseTree.Leaf;
			var typeNameNode = prevNode as ParseTree.Node;
			if (typeNameNode != null)
				typeNameNode = typeNameNode.NodeAt(0);
			if (typeNameNode != null && typeNameNode.RuleName == "typeName")
			{
				var lastTypeOrGenericNode = typeNameNode.NodeAt(0).NodeAt(-1);
				if (lastTypeOrGenericNode != null && lastTypeOrGenericNode.RuleName == "typeOrGeneric")
					constructorLeaf = lastTypeOrGenericNode.LeafAt(0);
			}

			if (constructorLeaf != null)
			{
				constructorLeaf.resolvedSymbol = constructor;

				var argumentListNode = node1.numValidNodes > 2 ? node1.NodeAt(1) : null;
				if (argumentListNode != null)
					ReResolveImplicitlyTypedArguments(argumentListNode, constructor);
			}
		}

		if (node.RuleName != "arguments" && node.numValidNodes == 2)
			ResolveNode(node.ChildAt(1));
		
		return constructor;
	}

	public static SymbolDefinition EnumerableElementType(ParseTree.Node node)
	{
		var enumerableExpr = ResolveNode(node);
		if (enumerableExpr != null)
		{
			var arrayType = enumerableExpr.TypeOf() as ArrayTypeDefinition;
			if (arrayType != null)
			{
				if (arrayType.rank > 0 && arrayType.elementType != null)
					return arrayType.elementType.definition;
			}
			else
			{
				var enumerableType = enumerableExpr.TypeOf() as TypeDefinitionBase;
				if (enumerableType != null)
				{
					TypeDefinitionBase iEnumerableGenericTypeDef = builtInTypes_IEnumerable_1;
					if (enumerableType.DerivesFromRef(ref iEnumerableGenericTypeDef))
					{
						var asGenericEnumerable = iEnumerableGenericTypeDef as ConstructedTypeDefinition;
						if (asGenericEnumerable != null)
							return asGenericEnumerable.typeArguments[0].definition;
					}

					var iEnumerableTypeDef = builtInTypes_IEnumerable;
					if (enumerableType.DerivesFrom(iEnumerableTypeDef))
						return builtInTypes_object;
					
					var getEnumeratorMethod = enumerableType.FindMethod("GetEnumerator", 0, 0, true);
					if (getEnumeratorMethod != null && getEnumeratorMethod.IsPublic)
					{
						var enumeratorType = getEnumeratorMethod.ReturnType();
						if (enumeratorType != null)
						{
							var moveNextMethod = enumeratorType.FindMethod("MoveNext", 0, 0, true);
							if (moveNextMethod != null && moveNextMethod.IsPublic && moveNextMethod.ReturnType() == builtInTypes_bool)
							{
								var currentProperty = enumeratorType.FindProperty("Current", true);
								if (currentProperty != null && currentProperty.IsPublic)
								{
									var enumeratedType = currentProperty.TypeOf() as TypeDefinitionBase;
									if (enumeratedType != null)
										enumeratedType = enumeratedType.SubstituteTypeParameters(enumerableType); // Check this again.
									return enumeratedType ?? unknownType;
								}
							}
						}
					}
				}
			}
		}
		return unknownType;
	}
	
	private static SymbolDefinition ResolveArgumentsNode(ParseTree.Node argumentsNode, Scope scope, ParseTree.Leaf invokedLeaf, SymbolDefinition invokedSymbol, SymbolDefinition memberOf)
	{
		SymbolDefinition result = null;
		
		invokedSymbol = invokedSymbol ?? invokedLeaf.resolvedSymbol;
		
		if (builtInTypes_dynamic != null && invokedSymbol is InstanceDefinition && invokedSymbol.TypeOf() == builtInTypes_dynamic)
			return builtInTypes_dynamic.GetThisInstance();

		var argumentListNode = argumentsNode != null && argumentsNode.numValidNodes >= 2 ? argumentsNode.NodeAt(1) : null;
		//if (argumentListNode != null)
		//	ResolveNode(argumentListNode, scope);

		TypeReference[] typeArgs = null;
		
		if (invokedLeaf != null && invokedLeaf.resolvedSymbol != null &&
			invokedLeaf.resolvedSymbol.kind == SymbolKind.Method && invokedLeaf.resolvedSymbol.parentSymbol == null)
		{
			// Local function.
			// TODO: Check type arguments and arguments nodes.
			return invokedLeaf.resolvedSymbol;
		}

		if (invokedSymbol.kind == SymbolKind.MethodGroup)
		{
			if (invokedLeaf != null)
			{
				var parentNode = invokedLeaf.parent;
				if (parentNode != null)
				{
					ParseTree.Node typeArgumentListNode = null;
					if (parentNode.RuleName == "accessIdentifier")
						typeArgumentListNode = parentNode.NodeAt(2);
					else if (parentNode.RuleName == "primaryExpressionStart")
						typeArgumentListNode = parentNode.NodeAt(1);
					if (typeArgumentListNode != null && typeArgumentListNode.RuleName == "typeArgumentList")
					{
						var numTypeArguments = typeArgumentListNode.numValidNodes / 2;
						typeArgs = TypeReference.AllocArray(numTypeArguments);
						for (int i = 0; i < numTypeArguments; ++i)
							typeArgs[i] = TypeReference.To(typeArgumentListNode.ChildAt(1 + 2 * i));
					}
				}
			}
			
			result = invokedSymbol.ResolveMethodOverloads(argumentListNode, typeArgs, scope, invokedLeaf);
			var targetType = invokedSymbol;
			while (targetType != null && !(targetType is TypeDefinitionBase))
				targetType = targetType.parentSymbol;
			while (result == MethodGroupDefinition.unresolvedMethodOverload && targetType != null)
			{
				targetType = (targetType as TypeDefinitionBase).BaseType();
				if (targetType != null)
				{
					var inBase = targetType.FindName(invokedSymbol.name, 0, false);
					if (inBase != null && inBase.kind == SymbolKind.MethodGroup)
						result = inBase.ResolveMethodOverloads(argumentListNode, typeArgs, scope, invokedLeaf);
				}
			}
			
			if (result != null && result.kind == SymbolKind.Method && !(result is MethodDefinition))
				result = result as ConstructedSymbolReference;
			
			if (result != null && result.kind != SymbolKind.Error)
			{
				var prevNode = argumentsNode != null ? argumentsNode.parent.FindPreviousNode() as ParseTree.Node : null;
				var idLeaf = invokedLeaf;
				if (invokedLeaf == null && prevNode != null)
                {
					if (prevNode.RuleName == "primaryExpressionPart")
						idLeaf = prevNode.NodeAt(-1).LeafAt(1);
					else if (prevNode.numValidNodes == 1)
						idLeaf = prevNode.LeafAt(0);
                }
				if (result.kind == SymbolKind.Error)
				{
					idLeaf.resolvedSymbol = invokedSymbol as MethodGroupDefinition;
					idLeaf.semanticError = result.name;
				}
				else if (idLeaf.resolvedSymbol != result)
				{
					idLeaf.resolvedSymbol = result;
					idLeaf.semanticError = null;
					
					if (argumentListNode != null)
					{
						ReResolveImplicitlyTypedArguments(argumentListNode, result);
					}
				}

				TypeReference.ReleaseArray(typeArgs);
				return result;
			}
		}
		
		if (//(invokedSymbol.kind == SymbolKind.MethodGroup || invokedSymbol.kind == SymbolKind.Error) &&
			memberOf != null && !(memberOf is TypeDefinitionBase))
		{
			if (invokedLeaf != null && typeArgs == null)
			{
				var parentNode = invokedLeaf.parent;
				if (parentNode != null)
				{
					ParseTree.Node typeArgumentListNode = null;
					if (parentNode.RuleName == "accessIdentifier")
						typeArgumentListNode = parentNode.NodeAt(2);
					else if (parentNode.RuleName == "primaryExpressionStart")
						typeArgumentListNode = parentNode.NodeAt(1);
					if (typeArgumentListNode != null && typeArgumentListNode.RuleName == "typeArgumentList")
						if (typeArgumentListNode != null)
					{
						var numTypeArguments = typeArgumentListNode.numValidNodes / 2;
						typeArgs = TypeReference.AllocArray(numTypeArguments);
						for (int i = 0; i < numTypeArguments; ++i)
							typeArgs[i] = TypeReference.To(typeArgumentListNode.ChildAt(1 + 2 * i));
					}
				}
			}
			
			var memberOfType = memberOf.TypeOf() as TypeDefinitionBase ?? scope.EnclosingType();
			result = scope.ResolveAsExtensionMethod(invokedLeaf, invokedSymbol, memberOfType, argumentListNode, typeArgs, scope);
			//Debug.Log("ResolveAsExtensionMethod: " + (invokedLeaf != null ? invokedLeaf.token.text : invokedSymbol.ToString()) + " on " + memberOf.ToString());
			
			if (result != null && result.kind == SymbolKind.Method && !(result is MethodDefinition))
				result = result as ConstructedSymbolReference;
			
			if (result != null)
			{
				if (result.kind == SymbolKind.Error)
				{
					invokedLeaf.resolvedSymbol = result;
					invokedLeaf.semanticError = result.name;
				}
				else if (invokedLeaf.resolvedSymbol != result)
				{
					invokedLeaf.resolvedSymbol = result;
					invokedLeaf.semanticError = null;

					if (argumentListNode != null)
					{
						ReResolveImplicitlyTypedArguments(argumentListNode, result);
					}
				}

				invokedSymbol = result;
			}
		}

		TypeReference.ReleaseArray(typeArgs);

		if (invokedSymbol.kind != SymbolKind.Method && invokedSymbol.kind != SymbolKind.Constructor && invokedSymbol.kind != SymbolKind.Error)
		{
			var typeOf = invokedSymbol.TypeOf() as TypeDefinitionBase;
			if (typeOf == null || typeOf.kind == SymbolKind.Error)
				return unknownType;
			
			var returnType = invokedSymbol.kind == SymbolKind.Delegate ? typeOf :
				typeOf.kind == SymbolKind.Delegate ? typeOf.TypeOf() as TypeDefinitionBase :
				invokedSymbol.kind != SymbolKind.MethodGroup ? cannotInvokeSymbol : null;
			if (returnType != null && returnType.kind == SymbolKind.Error)
				result = returnType;
			else if (returnType != null)
				return returnType.GetThisInstance();

			if (invokedLeaf != null)
			{
				if (result != null && result.kind == SymbolKind.Error)
					invokedLeaf.semanticError = result.name;
				else
					invokedLeaf.semanticError = "Cannot invoke symbol";
			}
		}
		
		return result;
	}
	
	private static void ReResolveImplicitlyTypedArguments(ParseTree.Node argumentListNode, SymbolDefinition context)
	{
		for (var i = 0; i < argumentListNode.numValidNodes; i += 2)
		{
			var argumentNode = argumentListNode.NodeAt(i);
			if (argumentNode == null)
				continue;
			var argumentValueNode = argumentNode.NodeAt(-1);
			if (argumentValueNode == null)
				continue;
			if (argumentValueNode.numValidNodes > 1)
			{
				if (argumentValueNode.ChildAt(0).IsLit("out"))
				{
					var nextLeaf = argumentValueNode.ChildAt(1).GetFirstLeaf();
					if (nextLeaf != null && nextLeaf.token.tokenKind == SyntaxToken.Kind.ContextualKeyword && nextLeaf.token.text == "_")
					{
						if (nextLeaf.resolvedSymbol != null)
						{
							var argumentType = nextLeaf.resolvedSymbol.TypeOf() as TypeDefinitionBase;
							if (argumentType != null)
							{
								argumentType = argumentType.SubstituteTypeParameters(context);
								if (argumentType != null)
								{
									var thisInstance = argumentType.GetThisInstance() as ThisReference;
									if (thisInstance != null)
									{
										nextLeaf.resolvedSymbol = thisInstance.GetDiscardVariable();
									}
								}
							}
						}
					}
				}
				continue;
			}
			var expressionNode = argumentValueNode.NodeAt(0);
			if (expressionNode == null || expressionNode.RuleName != "expression")
				continue;
			var nonAssignmentExpressionNode = expressionNode.NodeAt(0);
			if (nonAssignmentExpressionNode == null || nonAssignmentExpressionNode.RuleName != "nonAssignmentExpression")
				continue;
			var lambdaExpressionNode = nonAssignmentExpressionNode.NodeAt(0);
			if (lambdaExpressionNode == null || lambdaExpressionNode.RuleName != "lambdaExpression")
				continue;
			var anonymousFunctionSignatureNode = lambdaExpressionNode.NodeAt(0);
			if (anonymousFunctionSignatureNode == null)
				continue;
			var implicitAnonymousFunctionParameterNode = anonymousFunctionSignatureNode.NodeAt(0);
			if (implicitAnonymousFunctionParameterNode != null)
			{
				var identifierLeaf = implicitAnonymousFunctionParameterNode.LeafAt(0);
				if (identifierLeaf == null)
					continue;
				var instanceDefinition = identifierLeaf.resolvedSymbol as InstanceDefinition;
				if (instanceDefinition == null)
					continue;
				
				instanceDefinition.type = null;
				//instanceDefinition.TypeOf();
			}
			else
			{
				var implicitAnonymousFunctionParameterListNode = anonymousFunctionSignatureNode.NodeAt(1);
				if (implicitAnonymousFunctionParameterListNode == null)
					continue;
				for (var j = 0; j < implicitAnonymousFunctionParameterListNode.numValidNodes; j += 2)
				{
					implicitAnonymousFunctionParameterNode = implicitAnonymousFunctionParameterListNode.NodeAt(j);
					if (implicitAnonymousFunctionParameterNode == null)
						continue;
					var identifierLeaf = implicitAnonymousFunctionParameterNode.LeafAt(0);
					if (identifierLeaf == null)
						continue;
					var instanceDefinition = identifierLeaf.resolvedSymbol as InstanceDefinition;
					if (instanceDefinition == null)
						continue;
					instanceDefinition.type = null;
					//instanceDefinition.TypeOf();
				}
			}
		}
	}
	
	private static readonly string[] lambdaBodyRulePath = new [] {
		"argumentValue",
		"expression",
		"nonAssignmentExpression",
		"lambdaExpression",
		"lambdaExpressionBody"
	};

	private static ParseTree.Leaf FindMethodLeafFromArgument(ParseTree.Node node)
	{
		ParseTree.Leaf methodLeaf = null;

		var argumentsNode = node.FindParentByName("arguments");
		var varDeclsNode = argumentsNode.parent.FindPreviousNode() as ParseTree.Node;
		if (varDeclsNode.RuleName == "primaryExpressionStart")
		{
			methodLeaf = varDeclsNode.GetFirstLeaf(true);
		}
		else
		{
			var accessIdentifierNode = varDeclsNode.NodeAt(0);
			if (accessIdentifierNode != null && accessIdentifierNode.RuleName == "accessIdentifier")
			{
				methodLeaf = accessIdentifierNode.LeafAt(1);
			}
			else
			{
#if SI3_WARNINGS
				Debug.LogError(varDeclsNode);
#endif
			}
		}

#if SI3_WARNINGS
		if (methodLeaf == null)
			Debug.LogError("No method leaf for: " + varDeclsNode);
#endif
		return methodLeaf;
	}
	
	static TypeDefinitionBase ResolveDeconstructList(ParseTree.Node listNode, TypeDefinitionBase expressionType, Scope scope)
	{
		//var isImplicitDeconstruction = listNode.RuleName == "implicitDeconstructList";
		
		var elementTypes = new List<TypeReference>(16);
		var tupleType = expressionType as TupleTypeDefinition;
		if (tupleType == null)
		{
			var asValueTuple = expressionType as ConstructedTypeDefinition;
			if (asValueTuple != null)
				tupleType = asValueTuple.MakeTupleType();
		}
		var tupleTypeArity = tupleType != null ? tupleType.Arity : 0;
		
		var arity = listNode.numValidNodes / 2;
		
		var extensionOffset = 0;
		MethodDefinition deconstructMethod = null;
		if (tupleType == null)
		{
			deconstructMethod = expressionType.FindDeconstructMethod(arity);
			if (deconstructMethod == null)
			{
				deconstructMethod = scope.FindDeconstructExtensionMethod(expressionType, arity, scope);
				extensionOffset = 1;
			}
		}
		var constructedMethod = deconstructMethod as ConstructedMethodDefinition;
		
		for (var i = 0; i < arity; ++i)
		{
			var targetNode = listNode.NodeAt(i*2 + 1);
			var elementType = tupleType != null && i < tupleTypeArity
				? tupleType.TypeOfElement(i)
				: deconstructMethod != null && i < deconstructMethod.NumParameters
				? deconstructMethod.parameters[i + extensionOffset].TypeOf() as TypeDefinitionBase
				: unknownType;
			if (constructedMethod != null)
				elementType = elementType.SubstituteTypeParameters(constructedMethod);
			elementType = elementType.SubstituteTypeParameters(expressionType);
			var targetNodeChild0 = targetNode != null ? targetNode.NodeAt(0) : null;
			if (targetNode == null || targetNodeChild0 == null)
			{
				elementTypes.Add(TypeReference.To(elementType));
			}
			else
			{
				switch (targetNodeChild0.RuleName)
				{
				case "DISCARD":
					elementTypes.Add(TypeReference.To(elementType));
					var discardLeaf = targetNodeChild0.LeafAt(0);
					discardLeaf.resolvedSymbol = elementType.GetDiscardVariable();
					discardLeaf.semanticError = null;
					break;
				case "implicitDeconstructVariableDeclarator":
					elementTypes.Add(TypeReference.To(elementType));
					break;
				case "explicitDeconstructVariableDeclaration":
					if (elementType != null)
					{
						var node0 = targetNodeChild0.NodeAt(0);
						if (node0 != null)
						{
							var node0Child = node0.NodeAt(0);
							if (node0Child.RuleName == "VAR")
							{
								node0Child.LeafAt(0).resolvedSymbol = elementType;
							}
							else
							{
								var explicitType = ResolveNode(node0, scope, null, 0, true) as TypeDefinitionBase;
								if (explicitType != null)
								{
									var canConvert = elementType.CanConvertTo(explicitType);
									if (!canConvert)
									{
										node0.GetFirstLeaf().semanticError = "Cannot implicitly convert " + elementType.GetName() + " to " + explicitType.GetName();
									}
									elementType = explicitType;
								}
							}
						}
						var node1 = targetNodeChild0.NodeAt(1);
						if (node1 != null)
						{
							var node1Leaf = node1.GetFirstLeaf();
							if (node1Leaf != null && node1Leaf.token.text == "_")
							{
								node1Leaf.semanticError = null;
								node1Leaf.resolvedSymbol = elementType.GetDiscardVariable();
							}
						}
					}
					elementTypes.Add(TypeReference.To(elementType));
					//targetNodeChild0.resolvedSymbol = elementType;
					break;
				case "implicitDeconstructList":
				case "explicitDeconstructList":
					elementTypes.Add(TypeReference.To(ResolveDeconstructList(targetNodeChild0, elementType, scope)));
					break;
				}
			}
		}
		
		return TypeDefinitionBase.MakeTupleType(elementTypes);
	}
		
	public struct TupleElementNestingInfo
	{
		public int index;
		public int arity;
	}

	public static void CheckAssignment(ParseTree.Node assignmentNode, SymbolDefinition resolvedExpression)
	{
		if (resolvedExpression == null)
			return;

		var expressionNode = assignmentNode.NodeAt(2);
		
		var destinationLeaf = expressionNode.parent.ChildAt(0) as ParseTree.Leaf;
		if (destinationLeaf == null)
		{
			//Debug.Log(expressionNode.parent.ChildAt(0));
			return;
		}
		
		var destination = destinationLeaf.resolvedSymbol;
		if (destination == null)
			return;

		var destinationType = destination.TypeOf() as TypeDefinitionBase;
		if (destinationType == null || destinationType.kind == SymbolKind.Error)
			return;

		if (resolvedExpression.kind != SymbolKind.MethodGroup)
		{
			var expressionType = resolvedExpression.TypeOf() as TypeDefinitionBase;
			if (expressionType != null && !expressionType.CanConvertTo(destinationType))
			{
				var leaf = expressionNode.parent.LeafAt(1);
				if (leaf != null)
					leaf.semanticError = "Cannot convert type '" + expressionType.GetName() + "' to type '" + destinationType.GetName() + "'";
			}
			return;
		}

		if (destinationType.kind != SymbolKind.Delegate)
		{
			var leaf = expressionNode.parent.LeafAt(1);
			if (leaf != null)
				leaf.semanticError = "Cannot convert method group '" + resolvedExpression.name + "' to type '" + destinationType.GetName() + "'";
			return;
		}

		var asMethodGroup = resolvedExpression as MethodGroupDefinition;
		if (asMethodGroup == null)
			return;

		var methodGroupLeaf = expressionNode.GetFirstLeaf();
		//var primaryExpressionNode = methodGroupLeaf.FindParentByName("primaryExpression");
		//var lastPartNode = primaryExpressionNode.NodeAt(-1);
						
		var matchingMethod = asMethodGroup.FindMatchingMethod(destinationType);
		if (matchingMethod == null)
			return;

		methodGroupLeaf.resolvedSymbol = matchingMethod;
	}

	public static SymbolDefinition ResolveNode(ParseTree.BaseNode baseNode, Scope scope = null, SymbolDefinition asMemberOf = null, int numTypeArguments = 0, bool asTypeOnly = false)
#if MEASURE_RESOLVENODE_DEPTH
	{
		//var oldResolved = baseNode.resolvedSymbol;
		//if (oldResolved != null)
		//{
		//	return oldResolved;
		//}
		
		++resolveNodeDepth;
		if (resolveNodeDepth > resolveNodeDepthMax)
		{
			resolveNodeDepth = 0;
			throw new InvalidOperationException();
		}

		var result = _ResolveNode(baseNode, scope, asMemberOf, numTypeArguments, asTypeOnly);
		//baseNode.CacheResolvedSymbol(result);

//#if SI3_WARNINGS
		//if (oldResolved == result)
		//{
		//	if (oldResolved == null)
		//	{
		//		var sb = StringBuilders.Alloc();
		//		baseNode.Dump(sb, 0);
		//		Debug.LogWarning("Cannot resolve node: " + sb.ToString());
		//		StringBuilders.Release(sb);
		//	}
		//}
//#endif
		
		--resolveNodeDepth;
		return result;
	}
	
	private static int resolveNodeDepth = 0;
	private static int resolveNodeDepthMax = 100;
	
	private static SymbolDefinition _ResolveNode(ParseTree.BaseNode baseNode, Scope scope = null, SymbolDefinition asMemberOf = null, int numTypeArguments = 0, bool asTypeOnly = false)
#endif
	{
		ParseTree.Node node;

	// A goto label to avoid recursion when possible:
	reresolve:
		
		node = baseNode as ParseTree.Node;

		if (asMemberOf == null && node != null && node.RuleName == "type")
		{
			var nextNode = node;
			while (nextNode.parent != null && nextNode.parent.RuleName == "typeArgumentList")
				nextNode = nextNode.parent.parent.parent.parent.parent;
			if (nextNode != null && nextNode.childIndex >= 2 && nextNode.RuleName == "type")
			{
				nextNode = nextNode.nextSibling as ParseTree.Node;
				if (nextNode != null &&
					(nextNode.RuleName == "methodDeclaration" || nextNode.RuleName == "interfaceMethodDeclaration"))
				{
					scope = nextNode.scope;
				}
			}
		}

		if (scope == null)
		{
			var scopeNode = CsGrammar.EnclosingSemanticNode(baseNode, SemanticFlags.ScopesMask);
			while (scopeNode != null && scopeNode.scope == null && scopeNode.parent != null)
				scopeNode = CsGrammar.EnclosingSemanticNode(scopeNode.parent, SemanticFlags.ScopesMask);
			if (scopeNode != null)
				scope = scopeNode.scope;
		}

		var leaf = baseNode as ParseTree.Leaf;
		if (leaf != null)
		{
			if ((leaf.resolvedSymbol == null || leaf.semanticError != null ||
				//leaf.resolvedSymbol.kind == SymbolKind.Method ||
				!leaf.resolvedSymbol.IsValid()) && leaf.token != null)
			{
				var prevResolvedSymbol = leaf.resolvedSymbol;

				leaf.resolvedSymbol = null;
				leaf.semanticError = null;
				
				if (leaf.token.tokenKind == SyntaxToken.Kind.ContextualKeyword)
				{
					if (!CsParser.isCSharp4 && leaf.token.text == "_")
						leaf.token.tokenKind = SyntaxToken.Kind.Identifier;
					else if (builtInTypes_dynamic != null && leaf.token.text == "dynamic")
						leaf.token.tokenKind = SyntaxToken.Kind.Identifier;
				}

				switch (leaf.token.tokenKind)
				{
					case SyntaxToken.Kind.Identifier:
						if (asMemberOf != null)
						{
							asMemberOf.ResolveMember(leaf, scope, numTypeArguments, asTypeOnly);
							if (asTypeOnly && leaf.resolvedSymbol == null)
							{
								asMemberOf.ResolveMember(leaf, scope, numTypeArguments, false);
								if (leaf.resolvedSymbol != null && leaf.resolvedSymbol.kind != SymbolKind.Error)
								{
									leaf.semanticError = "Type expected!";
								}
							}
							//	UnityEngine.Debug.LogWarning("Could not resolve member '" + leaf + "' of " + asMemberOf + "[" + asMemberOf.GetType() + "], line " + (1+leaf.line));
						}
						else if (scope != null)
						{
							if (leaf.token.text == "global")
							{
								var nextLeaf = leaf.FindNextLeaf();
								if (nextLeaf != null && nextLeaf.IsLit("::"))
								{
									var assembly = scope.GetAssembly();
									if (assembly != null)
									{
										leaf.resolvedSymbol = scope.GetAssembly().GlobalNamespace;
//										nextLeaf = nextLeaf.FindNextLeaf();
//										if (nextLeaf != null && nextLeaf.token.tokenKind == SyntaxToken.Kind.Identifier)
//										{
//											nextLeaf.resolvedSymbol = assembly.FindNamespace(nextLeaf.token.text);
//										}
										return leaf.resolvedSymbol;
									}
								}
							}
							scope.Resolve(leaf, numTypeArguments, asTypeOnly);
							if (leaf.resolvedSymbol == null)
							{
								if (asTypeOnly)
								{
									scope.Resolve(leaf, numTypeArguments, false);
									if (leaf.resolvedSymbol != null && leaf.resolvedSymbol.kind != SymbolKind.Error)
									{
										leaf.semanticError = "Type expected!";
									}
								}
								else if (!CsParser.isCSharp4 && leaf.token.text == "nameof" && leaf.parent != null)
								{
									var nextNode = leaf.parent.nextSibling as ParseTree.Node;
									if (nextNode != null)
									{
										var firstChildNode = nextNode.firstChild as ParseTree.Node;
										if (firstChildNode != null && firstChildNode.RuleName == "arguments")
										{
											leaf.token.tokenKind = SyntaxToken.Kind.Keyword;
											leaf.resolvedSymbol = builtInTypes_string.GetThisInstance();
											return leaf.resolvedSymbol;
										}
									}
								}
								else if (!CsParser.isCSharp4 && leaf.token.text == "_" && leaf.parent != null)
								{
									var peStartNode = leaf.parent;
									if (peStartNode.RuleName == "primaryExpressionStart" && peStartNode.parent != null)
									{
										var unaryExprNode = peStartNode.parent.parent;
										if (unaryExprNode != null)
										{
											List<TupleElementNestingInfo> nesting = null;
											var assignmentNode = unaryExprNode.parent;
											int childIndex = unaryExprNode.childIndex;
											while (assignmentNode != null && assignmentNode.RuleName != "assignment")
											{
												assignmentNode = assignmentNode.FirstNonTrivialParent(out childIndex);
												if (assignmentNode == null || assignmentNode.RuleName != "parenOrTupleExpression")
													break;
												if (nesting == null)
													nesting = new List<TupleElementNestingInfo>(8);
												nesting.Add(new TupleElementNestingInfo
												{
													index = childIndex / 2,
													arity = assignmentNode.numValidNodes / 2
												});
											}
											if (assignmentNode != null && assignmentNode.RuleName == "assignment" && (childIndex == 0 || nesting != null))
											{
												assignmentNode = assignmentNode.NodeAt(1);
												var nextLeaf = assignmentNode == null ? null : assignmentNode.GetFirstLeaf();
												if (nextLeaf != null && nextLeaf.token.text == "=")
												{
													leaf.token.tokenKind = SyntaxToken.Kind.ContextualKeyword;
													var expressionNode = assignmentNode.nextSibling as ParseTree.Node;
													if (expressionNode != null)
													{
														var resolvedExpression = ResolveNode(assignmentNode.nextSibling, scope, null, 0, false);
														if (resolvedExpression != null)
														{
															var type = resolvedExpression.TypeOf() as TypeDefinitionBase;
															if (nesting != null)
															{
																for (var i = nesting.Count; type != null && i --> 0; )
																{
																	var tupleType = type as TupleTypeDefinition;
																	if (tupleType != null)
																	{
																		type = tupleType.TypeOfElement(nesting[i].index);
																		continue;
																	}
																
																	var deconstructMethod = type.FindDeconstructMethod(nesting[i].arity);
																	if (deconstructMethod == null)
																	{
																		deconstructMethod = scope.FindDeconstructExtensionMethod(type, nesting[i].arity, scope);
																	}
																	//TODO: Check is the Deconstruct() method accessible...
																	if (deconstructMethod == null)
																		break;
																
																	var parameters = deconstructMethod.GetParameters();
																	var paramIndex = nesting[i].index;
																	if (parameters.Count >= 1 && parameters[0].IsThisParameter)
																		++paramIndex;
																	if (paramIndex >= parameters.Count)
																		break;
																	var paramType = parameters[paramIndex].TypeOf() as TypeDefinitionBase;
																	if (paramType == null && type.kind == SymbolKind.Error)
																		break;
																	paramType = paramType.SubstituteTypeParameters(deconstructMethod);
																	if (paramType == null && paramType.kind == SymbolKind.Error)
																		break;
																	type = paramType.SubstituteTypeParameters(type);
																}
															}
															resolvedExpression = type != null ? type.GetThisInstance() : null;
														}
														leaf.resolvedSymbol = resolvedExpression;
														var asThisReference = leaf.resolvedSymbol as ThisReference;
														if (asThisReference != null)
															leaf.resolvedSymbol = asThisReference.GetDiscardVariable();
													}
												}
											}
										}
									}
								}
							}
						}
						if (leaf.resolvedSymbol == null)
						{
							if (asMemberOf != null)
								asMemberOf.ResolveMember(leaf, scope, -1, asTypeOnly);
							else if (scope != null)
								scope.Resolve(leaf, -1, asTypeOnly);
						}
						if (leaf.resolvedSymbol != null &&
							leaf.resolvedSymbol.NumTypeParameters != numTypeArguments &&
							leaf.resolvedSymbol.kind != SymbolKind.Error)
						{
							if (leaf.resolvedSymbol is TypeDefinitionBase)
							{
								if (!(leaf.resolvedSymbol is ConstructedTypeDefinition))
									leaf.semanticError = string.Format("Type '{0}' does not take {1} type argument{2}",
										leaf.resolvedSymbol.GetName(), numTypeArguments, numTypeArguments == 1 ? "" : "s");
							}
							else if (numTypeArguments > 0 &&
								(leaf.resolvedSymbol.kind == SymbolKind.Method))// || leaf.resolvedSymbol.kind == SymbolKind.MethodGroup))
							{
								leaf.semanticError = string.Format("Method '{0}' does not take {1} type argument{2}",
									leaf.token.text, numTypeArguments, numTypeArguments == 1 ? "" : "s");
							}
						}
						
						if (leaf.resolvedSymbol == null && asMemberOf == null && builtInTypes_dynamic != null && leaf.token.text == "dynamic")
						{
							leaf.token.tokenKind = SyntaxToken.Kind.ContextualKeyword;
							leaf.resolvedSymbol = builtInTypes_dynamic;
							return builtInTypes_dynamic;
						}

						break;

					case SyntaxToken.Kind.Keyword:
						if (leaf.token.text == "this" || leaf.token.text == "base")
						{
							var scopeNode = CsGrammar.EnclosingScopeNode(leaf.parent,
								SemanticFlags.MethodBodyScope,
								SemanticFlags.AccessorBodyScope);//,
								//SemanticFlags.LambdaExpressionBodyScope,
								//SemanticFlags.AnonymousMethodBodyScope);
							if (scopeNode == null)
							{
								if (leaf.childIndex == 1 && leaf.parent.RuleName == "constructorInitializer")
								{
									var bodyScope = scope.parentScope.parentScope as BodyScope;
									if (bodyScope == null)
										break;
									
									asMemberOf = bodyScope.definition;
									if (asMemberOf.kind != SymbolKind.Class && asMemberOf.kind != SymbolKind.Struct)
										break;
									
									if (leaf.token.text == "base")
									{
										if (asMemberOf.kind == SymbolKind.Struct)
											break; // CS0522: Struct constructors cannot call base constructors
										
										asMemberOf = ((TypeDefinitionBase) asMemberOf).BaseType();
									}
									
									leaf.resolvedSymbol = asMemberOf;
									leaf.resolvedSymbol = ResolveNodeAsConstructor(leaf.parent.NodeAt(2), scope, asMemberOf);
								}
								break;
							}

							var memberScope = scopeNode.scope as BodyScope;
							if (memberScope != null && memberScope.definition.IsStatic)
							{
								if (leaf.token.text == "base")
									leaf.resolvedSymbol = baseInStaticMember;
								else
									leaf.resolvedSymbol = thisInStaticMember;
								break;
							}

							scopeNode = CsGrammar.EnclosingScopeNode(scopeNode, SemanticFlags.TypeDeclarationScope);
							if (scopeNode == null)
							{
								leaf.resolvedSymbol = unknownSymbol;
								break;
							}

							var thisType = ((SymbolDeclarationScope) scopeNode.scope).declaration.definition as TypeDefinitionBase;
							if (thisType != null && leaf.token.text == "base")
								thisType = thisType.BaseType();
							if (thisType != null && (thisType.kind == SymbolKind.Struct || thisType.kind == SymbolKind.Class))
								leaf.resolvedSymbol = thisType.GetThisInstance();
							else
								leaf.resolvedSymbol = unknownSymbol;
							break;
						}
						else
						{
							TypeDefinitionBase type;
							if (builtInTypes.TryGetValue(leaf.token.text, out type))
								leaf.resolvedSymbol = type;
						}
						break;

					case SyntaxToken.Kind.CharLiteral:
						leaf.resolvedSymbol = builtInTypes_char.GetThisInstance();
						break;

					case SyntaxToken.Kind.IntegerLiteral:
						var endsWith = leaf.token.text[leaf.token.text.length - 1];
						var unsignedDecimal = endsWith == 'u' || endsWith == 'U';
						var longDecimal = endsWith == 'l' || endsWith == 'L';
						if (unsignedDecimal)
						{
							endsWith = leaf.token.text[leaf.token.text.length - 2];
							longDecimal = endsWith == 'l' || endsWith == 'L';
						}
						else if (longDecimal)
						{
							endsWith = leaf.token.text[leaf.token.text.length - 2];
							unsignedDecimal = endsWith == 'u' || endsWith == 'U';
						}
						if (longDecimal || unsignedDecimal)
						{
							leaf.resolvedSymbol =
							(
								longDecimal
								? unsignedDecimal ? builtInTypes_ulong : builtInTypes_long
								: builtInTypes_uint
							).GetThisInstance();
						}
						else
						{
							leaf.resolvedSymbol = IntegerLiteral.FromText(leaf.token.text);
							if (leaf.resolvedSymbol == null)
							{
								leaf.resolvedSymbol = builtInTypes_int.GetThisInstance();
								leaf.syntaxError = FGGrammar.IntegerConstantIsTooLargeErrorMessage.Instance;
							}
						}
						break;

					case SyntaxToken.Kind.RealLiteral:
						endsWith = leaf.token.text[leaf.token.text.length - 1];
						leaf.resolvedSymbol =
							endsWith == 'f' || endsWith == 'F' ? builtInTypes_float.GetThisInstance() :
							endsWith == 'm' || endsWith == 'M' ? builtInTypes_decimal.GetThisInstance() :
							builtInTypes_double.GetThisInstance();
						break;

					case SyntaxToken.Kind.StringLiteral:
					case SyntaxToken.Kind.VerbatimStringBegin:
					case SyntaxToken.Kind.VerbatimStringLiteral:
					case SyntaxToken.Kind.InterpolatedStringWholeLiteral:
					case SyntaxToken.Kind.InterpolatedStringEndLiteral:
						leaf.resolvedSymbol = builtInTypes_string.GetThisInstance();
						break;
						
					case SyntaxToken.Kind.InterpolatedStringStartLiteral:
					case SyntaxToken.Kind.InterpolatedStringMidLiteral:
					case SyntaxToken.Kind.InterpolatedStringFormatLiteral:
						leaf.resolvedSymbol = builtInTypes_string.GetThisInstance();
						break;

					case SyntaxToken.Kind.BuiltInLiteral:
						leaf.resolvedSymbol = leaf.token.text == "null" ? nullLiteral : builtInTypes_bool.GetThisInstance();
						break;
					
					case SyntaxToken.Kind.Missing:
						return null;
					
					case SyntaxToken.Kind.ContextualKeyword:
						return null;
					
					case SyntaxToken.Kind.Punctuator:
						return null;

					default:
						Debug.LogWarning(leaf.ToString());
						return null;
				}

				if (leaf.resolvedSymbol == null)
					leaf.resolvedSymbol = unknownSymbol;
				if (leaf.semanticError == null && leaf.resolvedSymbol.kind == SymbolKind.Error)
					leaf.semanticError = leaf.resolvedSymbol.name;
			}
			return leaf.resolvedSymbol;
		}

		if (node == null || node.numValidNodes == 0 || node.missing)
			return null;

		int rank;
		SymbolDefinition part = null, dummy = null; // used as non-null return value for explicitly resolving child nodes

//		Debug.Log("Resolving node: " + node);
		switch (node.RuleName)
		{
			case "interpolatedStringLiteral":
				{
					var child = node.firstChild;
					while (child != null)
					{
						ResolveNode(child, scope);
						child = child.nextSibling;
					}
				}
				return builtInTypes_string.GetThisInstance();
			
			case "localVariableType":
				if (node.numValidNodes == 1)
				{
					baseNode = node.ChildAt(0);
					//scope = scope;
					//asMemberOf = asMemberOf;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				break;

			case "GET":
			case "SET":
			case "ADD":
			case "REMOVE":
				SymbolDeclaration declaration = null;
				for (var tempNode = node; declaration == null && tempNode != null; tempNode = tempNode.parent)
					declaration = tempNode.declaration;
				if (declaration == null)
					return node.ChildAt(0).resolvedSymbol = unknownSymbol;
				return node.ChildAt(0).resolvedSymbol = declaration.definition;

			case "YIELD":
			case "FROM":
			case "SELECT":
			case "WHERE":
			case "GROUP":
			case "INTO":
			case "ORDERBY":
			case "JOIN":
			case "LET":
			case "ON":
			case "EQUALS":
			case "BY":
			case "ASCENDING_OR_DESCENDING":
			case "ATTRIBUTETARGET":
			case "UNMANAGED":
				node.ChildAt(0).resolvedSymbol = contextualKeyword;
				return contextualKeyword;

			case "memberName":
				declaration = null;
				while (declaration == null && node != null)
				{
					declaration = node.declaration;
					node = node.parent;
				}
				if (declaration == null)
					return unknownSymbol;
				return declaration.definition;

			case "DISCARD":
				var discardLeaf = node.LeafAt(0);
				discardLeaf.token.tokenKind = SyntaxToken.Kind.ContextualKeyword;
				discardLeaf.resolvedSymbol = ((ThisReference)builtInTypes_void.GetThisInstance()).GetDiscardVariable();
				discardLeaf.semanticError = null;
				return nullLiteral;
				
			case "explicitDeconstructDeclaration":
				if (node.resolvedSymbol != null && node.resolvedSymbol.kind != SymbolKind.Error)
					return node.resolvedSymbol;
				if (node.numValidNodes == 3)
				{
					var expressionNode = node.NodeAt(2);
					if (expressionNode != null)
					{
						var resolvedExpression = ResolveNode(expressionNode, scope, null, 0, false);
						if (resolvedExpression != null && resolvedExpression.kind != SymbolKind.Error)
						{
							var expressionType = resolvedExpression.TypeOf() as TypeDefinitionBase;
							node.resolvedSymbol = ResolveDeconstructList(node.NodeAt(0), expressionType, scope);
						}
					}
				}
				return node.resolvedSymbol;
				
			case "implicitDeconstructDeclaration":
				if (node.resolvedSymbol != null && node.resolvedSymbol.kind != SymbolKind.Error)
					return node.resolvedSymbol;
				if (node.numValidNodes == 4)
				{
					var expressionNode = node.NodeAt(3);
					if (expressionNode != null)
					{
						var resolvedExpression = ResolveNode(expressionNode, scope, null, 0, false);
						if (resolvedExpression != null && resolvedExpression.kind != SymbolKind.Error)
						{
							var expressionType = resolvedExpression.TypeOf() as TypeDefinitionBase;
							node.resolvedSymbol = ResolveDeconstructList(node.NodeAt(1), expressionType, scope);
						}
					}
				}
				return node.resolvedSymbol;
				
			case "switchExpressionBody":
			{
				var bestCommonType = new TypeInference.BestCommonTypeResolver();
				
				var switchExpressionArmNode = node.ChildAt(1);
				while (switchExpressionArmNode != null)
				{
					var asNode = switchExpressionArmNode as ParseTree.Node;
					if (asNode != null)
					{
						var expressionNode = asNode.FindChildByName("=>");
						if (expressionNode != null)
							expressionNode = expressionNode.nextSibling;
						asNode = expressionNode as ParseTree.Node;
						if (asNode != null && asNode.RuleName != "throwExpression")
							bestCommonType.AddExpressionNode(asNode);
					}
					
					switchExpressionArmNode = switchExpressionArmNode.nextSibling;
					if (switchExpressionArmNode != null)
						switchExpressionArmNode = switchExpressionArmNode.nextSibling;
				}
				
				var inferedType = bestCommonType.Resolve();
				return (inferedType ?? unknownType).GetThisInstance();
			}

			case "VAR":
				ParseTree.Node varDeclsNode = null;
				if (node.parent.parent.RuleName == "foreachStatement" && node.parent.parent.numValidNodes >= 6)
				{
					varDeclsNode = node.parent.parent.NodeAt(5);
					if (varDeclsNode != null && varDeclsNode.numValidNodes == 1)
					{
						var elementType = EnumerableElementType(varDeclsNode);
						node.ChildAt(0).resolvedSymbol = elementType;
					}
				}
				else if (node.parent.parent.RuleName == "caseVariableDeclaration")
				{
					varDeclsNode = node.FindParentByName("switchExpressionArm");
					if (varDeclsNode != null)
					{
						//varDeclsNode = node.FindParentByName("switchExpressionBody");
						//varDeclsNode = varDeclsNode != null ? varDeclsNode.NodeAt(2) : null;
						//if (varDeclsNode != null && varDeclsNode.numValidNodes == 1)
						//{
						//	var initExpr = ResolveNode(varDeclsNode);
						//	var varLeaf = node.ChildAt(0);
						//	varLeaf.semanticError = null;
						//	if (initExpr != null && initExpr.kind != SymbolKind.Error)
						//		varLeaf.resolvedSymbol = initExpr.TypeOf();
						//	else
						//		varLeaf.resolvedSymbol = unknownType;
						//}
					}
					else
					{
						varDeclsNode = node.FindParentByName("switchStatement");
						varDeclsNode = varDeclsNode != null ? varDeclsNode.NodeAt(2) : null;
						if (varDeclsNode != null && varDeclsNode.numValidNodes == 1)
						{
							var initExpr = ResolveNode(varDeclsNode);
							var varLeaf = node.ChildAt(0);
							varLeaf.semanticError = null;
							if (initExpr != null && initExpr.kind != SymbolKind.Error)
								varLeaf.resolvedSymbol = initExpr.TypeOf();
							else
								varLeaf.resolvedSymbol = unknownType;
						}
					}
				}
				else if (node.parent.parent.RuleName == "isVariableDeclaration")
				{
					var isVarDeclNode = node.parent.parent;
					var varType = builtInTypes_bool;
					if (isVarDeclNode.childIndex == 2)
					{
						var relExprNode = node.parent.parent.parent;
						if (relExprNode != null)
						{
							var expression = ResolveNode(relExprNode.NodeAt(0), scope, null, 0, false);
							if (expression != null)
								varType = expression.TypeOf() as TypeDefinition ?? unknownType;
						}
					}

					var varLeaf = node.parent.parent.GetFirstLeaf();
					if (varLeaf != null)
						varLeaf.resolvedSymbol = varType;
				}
				else if (node.parent.parent.RuleName == "outVariableDeclaration")
				{
					ParseTree.Leaf methodLeaf = FindMethodLeafFromArgument(node);
					
					var varLeaf = node.ChildAt(0);
					if (varLeaf.resolvedSymbol == null && methodLeaf != null && methodLeaf.resolvedSymbol == null)
					{
						FGResolver.ResolveNode(methodLeaf.parent);
					}
					
					if (methodLeaf == null || methodLeaf.resolvedSymbol == null && methodLeaf.semanticError != null)
					{
						varLeaf.resolvedSymbol = unknownType;
					}
					else if (varLeaf.resolvedSymbol != null)
					{
						varLeaf.resolvedSymbol = varLeaf.resolvedSymbol.SubstituteTypeParameters(methodLeaf.resolvedSymbol);
					}
				}
				else if (node.parent.RuleName == "implicitDeconstructDeclaration")
				{
					var varNode = node.ChildAt(0);
					varNode.resolvedSymbol = null;
					
					if (node.parent.resolvedSymbol == null)
						ResolveNode(node.parent, scope, null, 0, false);
					
					varNode.resolvedSymbol = node.parent.resolvedSymbol;
				}
				else if (node.parent.RuleName == "foreachStatement" && (node.nextSibling is ParseTree.Node) && (node.nextSibling as ParseTree.Node).RuleName == "implicitDeconstructList")
				{
					var varNode = node.ChildAt(0);
					
					var elementType = EnumerableElementType(node.parent.NodeAt(5)) as TypeDefinitionBase;
					if (elementType != null && elementType.kind != SymbolKind.Error)
					{
						varNode.resolvedSymbol = ResolveDeconstructList(node.nextSibling as ParseTree.Node, elementType, scope);
					}
					else
					{
						varNode.resolvedSymbol = elementType;
					}
				}
				else if (node.parent.parent.RuleName == "explicitDeconstructVariableDeclaration")
				{
					var varNode = node.ChildAt(0);
					
					var deconstructNode = node.parent.parent.parent.FindParentByName("explicitDeconstructDeclaration")
						?? node.parent.parent.parent.parent.FindParentByName("foreachStatement")
						?? node.parent.parent;
					if (deconstructNode.resolvedSymbol == null)
					{
						varNode.resolvedSymbol = null;
						if (deconstructNode.RuleName == "foreachStatement")
						{
							if (deconstructNode.resolvedSymbol == null || deconstructNode.resolvedSymbol.kind == SymbolKind.Error)
							{
								if (deconstructNode.numValidNodes >= 5)
								{
									var elementType = EnumerableElementType(deconstructNode.NodeAt(4)) as TypeDefinitionBase;
									if (elementType != null && elementType.kind != SymbolKind.Error)
									{
										deconstructNode.resolvedSymbol =
											ResolveDeconstructList(deconstructNode.NodeAt(2) as ParseTree.Node, elementType, scope);
									}
								}
							}
						}
						else
						{
							ResolveNode(deconstructNode, scope, null, 0, false);
						}
					}

					if (deconstructNode.RuleName != "foreachStatement")
					{
						var nextLeaf = varNode.FindNextLeaf();
						if (nextLeaf != null && nextLeaf.resolvedSymbol != null)
							varNode.resolvedSymbol = nextLeaf.resolvedSymbol.TypeOf();
					}
				}
				else if (node.parent.parent.numValidNodes >= 2)
				{
					varDeclsNode = node.parent.parent.NodeAt(1);
					if (varDeclsNode != null && varDeclsNode.numValidNodes == 1)
					{
						var declNode = varDeclsNode.NodeAt(0);
						if (declNode != null && declNode.numValidNodes >= 3)
						{
							var initExpr = ResolveNode(declNode.ChildAt(-1));
							var varLeaf = node.ChildAt(0);
							varLeaf.semanticError = null;
							if (initExpr != null && initExpr.kind != SymbolKind.Error)
							{
								initExpr = initExpr.TypeOf() ?? initExpr;
								if (initExpr is IntegerLiteralType)
									initExpr = initExpr.TypeOf() ?? initExpr;
								varLeaf.resolvedSymbol = initExpr;
							}
							else
							{
								varLeaf.resolvedSymbol = unknownType;

								//initExpr = ResolveNode(declNode.ChildAt(-1));
								//initExpr = null;
							}
						}
						else
							node.ChildAt(0).resolvedSymbol = unknownType;
					}
				}
				else
				{
#if SI3_WARNINGS
					Debug.Log(node.parent);
#endif
					node.ChildAt(0).resolvedSymbol = unknownType;
				}
				
				var resolvedType = node.ChildAt(0).resolvedSymbol;
				if (resolvedType is NullTypeDefinition)
				{
					node.ChildAt(0).resolvedSymbol = nullVarType;
					resolvedType = unknownType;
				}
				else if (resolvedType == builtInTypes_void)
				{
					node.ChildAt(0).resolvedSymbol = voidVarType;
					resolvedType = unknownType;
				}
				return resolvedType;

			case "type": case "type2":
				var resolvedTypeNode = ResolveNode(node.ChildAt(0), scope, asMemberOf, numTypeArguments, true);
				var typeNodeType = resolvedTypeNode as TypeDefinitionBase;
				if (typeNodeType != null)
				{
					if (node.numValidNodes > 1)
					{
						ParseTree.BaseNode nullableShorthandOrPointer = node.LeafAt(1);
						if (nullableShorthandOrPointer != null)
						{
							if (nullableShorthandOrPointer.IsLit("?"))
							{
								typeNodeType = typeNodeType.MakeNullableType();
							}
							else if (nullableShorthandOrPointer.IsLit("*"))
							{
								typeNodeType = typeNodeType.MakePointerType();
								while ((nullableShorthandOrPointer = nullableShorthandOrPointer.nextSibling) != null
									&& nullableShorthandOrPointer.IsLit("*"))
								{
									typeNodeType = typeNodeType.MakePointerType();
								}
							}
						}

						var rankNode = node.NodeAt(-1);
						if (rankNode != null && rankNode.numValidNodes != 0)
						{
							for (var i = 1; i < rankNode.numValidNodes; i += 2)
							{
								rank = 1;
								while (i < rankNode.numValidNodes && rankNode.ChildAt(i).IsLit(","))
								{
									++rank;
									++i;
								}
								typeNodeType = typeNodeType.MakeArrayType(rank);
							}
						}
					}
					return typeNodeType;
				}
				//else if (resolvedTypeNode != null && resolvedTypeNode.kind != SymbolKind.Error)
				//{
				//	var firstLeaf = node.LeafAt(0) ?? node.NodeAt(0).GetFirstLeaf();
				//	if (firstLeaf != null)
				//		firstLeaf.semanticError = "Type expected";
				//}

				break;

			case "attribute":
				var attributeTypeName = ResolveNode(node.ChildAt(0), scope, null, numTypeArguments, true);
				//if (attributeTypeName == null || attributeTypeName == unknownSymbol || attributeTypeName == unknownType)
				//{
				//    var lastLeaf = ((ParseTree.Node) node.nodes[0]).GetLastLeaf();
				//    var oldText = lastLeaf.token.text;
				//    lastLeaf.token.text += "Attribute";
				//    lastLeaf.resolvedSymbol = null;
				//    attributeTypeName = ResolveNode(node.nodes[0], scope);
				//    lastLeaf.token.text = oldText;
				//}
				if (node.numValidNodes == 2)
					ResolveNode(node.ChildAt(1), null);
				return attributeTypeName;

			case "integralType":
			case "simpleType":
			case "numericType":
			case "floatingPointType":
			case "predefinedType":
			case "basicType":
			case "typeName":
			case "exceptionClassType":
				resolvedType = ResolveNode(node.ChildAt(0), scope, asMemberOf, numTypeArguments, true);
				if (resolvedType != null && resolvedType.kind != SymbolKind.Error && resolvedType.kind != SymbolKind.Constructor && !(resolvedType is TypeDefinitionBase))
					node.GetFirstLeaf().semanticError = "Type expected";
				return resolvedType;
			
			case "globalNamespace":
				baseNode = node.ChildAt(0);
				//scope = scope;
				asMemberOf = null;
				numTypeArguments = 0;
				asTypeOnly = false;
				goto reresolve;

			case "nonArrayType":
				var nonArrayTypeSymbol = ResolveNode(node.ChildAt(0), scope, asMemberOf, 0, true);
				if (nonArrayTypeSymbol != null && nonArrayTypeSymbol.kind == SymbolKind.Constructor)
					return nonArrayTypeSymbol;
				var nonArrayType = nonArrayTypeSymbol as TypeDefinitionBase;
				if (nonArrayType != null && node.numValidNodes == 2)
					return nonArrayType.MakeNullableType();
				return nonArrayType;

			//case "typeParameterList":
			//    return null;

			case "typeParameter":
				return ResolveNode(node.ChildAt(0), scope, asMemberOf, 0, true) as TypeDefinitionBase;

			case "typeVariableName":
				//asMemberOf = ((SymbolDeclarationScope) scope).declaration.definition;
				return ResolveNode(node.ChildAt(0), scope) as TypeParameterDefinition;
			
			case "tupleType":
				var typeRefs = new List<TypeReference>(node.numValidNodes / 2);
				for (var i = 1; i < node.numValidNodes; i += 2)
				{
					var tupleElementNode = node.NodeAt(i);
					if (tupleElementNode != null)
					{
						tupleElementNode = tupleElementNode.NodeAt(0);
					}
					
					if (tupleElementNode == null)
					{
						typeRefs.Add(TypeReference.To(unknownType));
					}
					else
					{
						typeRefs.Add(TypeReference.To(tupleElementNode));
					}
				}
				var tupleType = TypeDefinitionBase.MakeTupleType(typeRefs);
				return tupleType;

			case "outVariableDeclaration":
				baseNode = node.ChildAt(0);
				//scope = scope;
				asMemberOf = null;
				numTypeArguments = 0;
				asTypeOnly = true;
				goto reresolve;

			case "typeOrGeneric":
				if (asMemberOf == null && node.childIndex > 0 && node.parent != null)
				{
					if (asMemberOf != null && asMemberOf.kind != SymbolKind.Error && !(asMemberOf.kind == SymbolKind.Namespace || asMemberOf is TypeDefinitionBase))
						asMemberOf = ResolveNode(node.parent.ChildAt(node.childIndex - 2), scope, null, 0, true);
					if (asMemberOf != null && asMemberOf.kind != SymbolKind.Error && !(asMemberOf.kind == SymbolKind.Namespace || asMemberOf is TypeDefinitionBase))
						node.ChildAt(node.childIndex - 2).semanticError = "Namespace name or type expected";
				}
				if (node.numValidNodes >= 2)
				{
					var typeArgsListNode = node.NodeAt(1);
					if (typeArgsListNode != null && typeArgsListNode.numValidNodes > 0)
					{
						bool isUnboundType = typeArgsListNode.RuleName == "unboundTypeRank";
						var numTypeArgs = isUnboundType ? typeArgsListNode.numValidNodes - 1 : typeArgsListNode.numValidNodes / 2;
						var typeDefinition = ResolveNode(node.ChildAt(0), scope, asMemberOf != null ? asMemberOf.GetGenericSymbol() : null, numTypeArgs, true) as TypeDefinition;
						if (typeDefinition == null)
							return node.ChildAt(0).resolvedSymbol;

						if (!isUnboundType && !(typeDefinition is ConstructedTypeDefinition))
						{
							var typeArgs = TypeReference.AllocArray(numTypeArgs);
							for (var i = 0; i < numTypeArgs; ++i)
								typeArgs[i] = TypeReference.To(typeArgsListNode.ChildAt(1 + 2 * i));
							if (typeDefinition.typeParameters != null && typeDefinition.typeParameters.Count == numTypeArgs)
							{
								var constructedType = typeDefinition.ConstructType(typeArgs, asMemberOf as TypeDefinition);
								node.ChildAt(0).resolvedSymbol = constructedType;
								TypeReference.ReleaseArray(typeArgs);
								return constructedType;
							}
							TypeReference.ReleaseArray(typeArgs);
						}

						return typeDefinition;
					}
				}
				else if (scope is AttributesScope && node.childIndex == node.parent.numValidNodes - 1 && node.parent.parent.parent.RuleName == "attribute")
				{
					var lastLeaf = node.LeafAt(0);
					if (asMemberOf != null)
						asMemberOf.ResolveAttributeMember(lastLeaf, scope);
					else
						scope.ResolveAttribute(lastLeaf);

					if (lastLeaf.resolvedSymbol == null)
					{
						lastLeaf.resolvedSymbol = unknownSymbol;
						lastLeaf.semanticError = unknownSymbol.name;
					}
					return lastLeaf.resolvedSymbol;
				}
				//part = ResolveNode(node.ChildAt(0), scope, asMemberOf, 0, asMemberOf != null && asMemberOf.kind != SymbolKind.Namespace || (node.childIndex == node.parent.numValidNodes - 1));
				baseNode = node.ChildAt(0);
				//scope = scope;
				//asMemberOf = asMemberOf;
				numTypeArguments = 0;
				asTypeOnly = asTypeOnly ||
					asMemberOf != null && asMemberOf.kind != SymbolKind.Namespace ||
					node.parent != null && node.parent.parent != null && node.childIndex == node.parent.numValidNodes - 1 &&
					node.parent.parent.RuleName != "namespaceName" && node.parent.parent.RuleName != "usingAliasDirective";
				goto reresolve;

			case "namespaceName":
				var resolvedSymbol = ResolveNode(node.ChildAt(0), scope, asMemberOf, 0, false);
				if (resolvedSymbol != null && resolvedSymbol.kind != SymbolKind.Error && !(resolvedSymbol is NamespaceDefinition))
					node.ChildAt(0).semanticError = "Namespace name expected";
				return resolvedSymbol;

			case "namespaceOrTypeName":
				asTypeOnly = node.numValidNodes == 1 && numTypeArguments > 0;
				part = ResolveNode(node.firstChild, scope, null, node.numValidNodes == 1 ? numTypeArguments : 0, asTypeOnly);
				if (part != null && part.kind != SymbolKind.Error && !(part.kind == SymbolKind.Constructor || part.kind == SymbolKind.Namespace || part is TypeDefinitionBase))
				{
					if (node.NodeAt(0).firstChild != null)
						node.NodeAt(0).firstChild.semanticError = "Namespace name or type expected";
					if (!asTypeOnly)
					{
						asTypeOnly = true;
						part = ResolveNode(node.firstChild, scope, null, node.numValidNodes == 1 ? numTypeArguments : 0, asTypeOnly);
						if (part != null && part.kind != SymbolKind.Error && !(part.kind == SymbolKind.Namespace || part is TypeDefinitionBase))
							if (node.NodeAt(0).firstChild != null)
								node.NodeAt(0).firstChild.semanticError = "Namespace name or type expected";
					}
				}
				for (var i = 2; i < node.numValidNodes; i += 2)
				{
					if (!asTypeOnly && (numTypeArguments > 0 && i == node.numValidNodes - 1 || part is TypeDefinitionBase))
						asTypeOnly = true;
					part = ResolveNode(node.ChildAt(i), scope, part, i == node.numValidNodes - 1 ? numTypeArguments : 0, asTypeOnly);
					if (part != null && part.kind != SymbolKind.Error && !(part.kind == SymbolKind.Constructor || part.kind == SymbolKind.Namespace || part is TypeDefinitionBase))
					{
						if (node.NodeAt(i).firstChild != null)
							node.NodeAt(i).firstChild.semanticError = "Namespace name or type expected";
						if (!asTypeOnly)
						{
							asTypeOnly = true;
							part = ResolveNode(node.ChildAt(i), scope, part, i == node.numValidNodes - 1 ? numTypeArguments : 0, asTypeOnly);
							if (part != null && part.kind != SymbolKind.Error && !(part.kind == SymbolKind.Namespace || part is TypeDefinitionBase))
								if (node.NodeAt(i).firstChild != null)
									node.NodeAt(i).firstChild.semanticError = "Namespace name or type expected";
						}
					}
				}
				return part;

			case "usingAliasDirective":
				var usingAliasNode = node.NodeAt(2);
				if (usingAliasNode != null)
					ResolveNode(usingAliasNode, scope, null, 0, true);
				baseNode = node.firstChild;
				//scope = scope;
				asMemberOf = null;
				numTypeArguments = 0;
				asTypeOnly = false;
				goto reresolve;

			case "qualifiedIdentifier":
				part = ResolveNode(node.firstChild, scope) as NamespaceDefinition;
				for (var i = 2; part != null && i < node.numValidNodes; i += 2)
				{
					part = ResolveNode(node.ChildAt(i), scope, part);
					var idNode = node.NodeAt(i);
					if (idNode != null && idNode.numValidNodes == 1)
						idNode.firstChild.resolvedSymbol = part;
				}
				return part;

			case "destructorDeclarator":
				return builtInTypes_void;

			case "memberInitializer":
				ResolveNode(node.firstChild, scope);
				if (node.numValidNodes == 3)
					ResolveNode(node.ChildAt(2), scope);
				return null;

			case "primaryExpression":
				var invokeTarget = part;
				var returnNullable = false;
				ParseTree.Leaf invokeTargetLeaf = null;
				for (var i = 0; i < node.numValidNodes; ++i)
				{
					var child = node.ChildAt(i);
					var childAsLeaf = child as ParseTree.Leaf;
					if (childAsLeaf != null && childAsLeaf.missing)
						return part;

					var methodNameNode = child as ParseTree.Node;
					SymbolDefinition nextPart = null;
					
					if (i == 0 && childAsLeaf != null && childAsLeaf.token != null && childAsLeaf.token.text == "new")
					{
						methodNameNode = node.NodeAt(1);
						if (methodNameNode != null && methodNameNode.numValidNodes > 0)
						{
							var nonArrayTypeNode = methodNameNode.RuleName == "nonArrayType" ? methodNameNode : null;
							if (nonArrayTypeNode != null)
							{
								asMemberOf = ResolveNode(nonArrayTypeNode, scope);
								if (asMemberOf == null)
								{
									asMemberOf = ResolveNode(nonArrayTypeNode, scope);
									//asMemberOf = null;
								}

								var node3 = node.NodeAt(2);
								if (node3 != null && node3.RuleName == "objectCreationExpression")
								{
									i += 2;
									nextPart = asMemberOf != null && asMemberOf.kind == SymbolKind.Constructor ? asMemberOf : ResolveNodeAsConstructor(node3, scope, asMemberOf);
									if (nextPart != null && nextPart.kind == SymbolKind.Constructor)
									{
										var asMemberOfAsConstructedType = asMemberOf as ConstructedTypeDefinition;
										if (asMemberOfAsConstructedType != null)
											nextPart = asMemberOfAsConstructedType.GetConstructedMember(nextPart);
									}

									ParseTree.Leaf constructorLeaf = null;
									var typeNameNode = nonArrayTypeNode.NodeAt(0);
									if (typeNameNode != null && typeNameNode.RuleName == "typeName")
									{
										var lastTypeOrGenericNode = typeNameNode.NodeAt(0).NodeAt(-1);
										if (lastTypeOrGenericNode != null && lastTypeOrGenericNode.RuleName == "typeOrGeneric")
											constructorLeaf = lastTypeOrGenericNode.LeafAt(0);
									}
									else
									{
										constructorLeaf = nonArrayTypeNode.GetFirstLeaf();
									}
									
									if (constructorLeaf != null)
									{
										if (nextPart != null && nextPart.kind != SymbolKind.Error)
											constructorLeaf.resolvedSymbol = nextPart;

										if (asMemberOf is TypeDefinitionBase)
										{
											if (asMemberOf.kind == SymbolKind.Class)
											{
												if (asMemberOf.IsAbstract)
													constructorLeaf.semanticError = "Cannot create an instance of the abstract class";
												else if (asMemberOf.IsStatic)
													constructorLeaf.semanticError = "Cannot create an instance of the static class";
												else if (asMemberOf == builtInTypes_dynamic)
													constructorLeaf.semanticError = "Cannot create an instance of the dynamic type";
											}
											else if (asMemberOf.kind == SymbolKind.Interface)
												constructorLeaf.semanticError = "Cannot create an instance of the interface";
											else if (asMemberOf == builtInTypes_void)
												constructorLeaf.semanticError = "Cannot create an instance of the System.Void type";
										}
									}
								}
								else if (node3 != null && node3.RuleName == "arrayCreationExpression")
								{
									i += 2;
									nextPart = ResolveNode(node.ChildAt(i), scope, asMemberOf);
								}
								else
								{
									i += 2;
									var type = asMemberOf as TypeDefinitionBase ?? unknownType;
									nextPart = type.GetThisInstance();
								}

								methodNameNode = null;
							}
							else
							{
								// methodNameNode is implicitArrayCreationExpression, anonymousObjectCreationExpression, or targetTypedNewExpression
								nextPart = ResolveNode(methodNameNode, scope);
							}
						}
					}
					else
					{
						// child is primaryExpressionStart, primaryExpressionPart, or anonymousMethodExpression
						
						var primaryExpressionPartNode = i != 0 ? child as ParseTree.Node : null;
						var argumentsNode = primaryExpressionPartNode != null ? primaryExpressionPartNode.NodeAt(-1) : null;
						if (argumentsNode != null && argumentsNode.RuleName == "arguments")
						{
							nextPart = ResolveArgumentsNode(argumentsNode, scope, invokeTargetLeaf, part, asMemberOf ?? part.parentSymbol);
							
							//var parameters = nextPart != null ? nextPart.GetParameters() : null;
							//if (parameters != null)
							//{
							//	var argumentListNode2 = argumentsNode != null && argumentsNode.numValidNodes >= 2 ? argumentsNode.NodeAt(1) : null;
							//	if (argumentListNode2 != null)
							//	{
							//		for (var j = 0; j < argumentListNode2.numValidNodes; j += 2)
							//		{
							//			var argumentNode = argumentListNode2.NodeAt(j);
							//			if (argumentNode == null)
							//				continue;
										
							//			var lambdaExpressionBodyNode = argumentNode.FindChildByName(lambdaBodyRulePath);
							//			if (lambdaExpressionBodyNode != null)
							//				ResolveNode(lambdaExpressionBodyNode);
							//		}
							//	}
							//}
						}
						else
						{
							nextPart = ResolveNode(child, scope, part);
						}
					}
					
					asMemberOf = part;
					
					if (nextPart != null && nextPart.kind != SymbolKind.Error)
					{
						SymbolDefinition method = nextPart.kind == SymbolKind.Method || nextPart.kind == SymbolKind.Constructor ? nextPart : null;
						if (nextPart.kind == SymbolKind.MethodGroup)
						{
							if (methodNameNode.numValidNodes == 2 && !(nextPart is ConstructedMethodGroupDefinition))
							{
								nextPart = ResolveNode(methodNameNode.NodeAt(1), scope, nextPart);
							}
						}
						//if (part.kind == SymbolKind.MethodGroup && ++i < node.numValidNodes)
						//{
						//	methodNameNode = node.NodeAt(i - 1);
						//	child = node.ChildAt(i);
						//	part = ResolveNode(child, scope, part);
						//	if (part != null)
						//		method = part.kind == SymbolKind.Method ? part : null;
						//}
						
						if (method != null)
						{
	//						var asMemberOfConstructedType = asMemberOf as ConstructedTypeDefinition;
	//						if (asMemberOfConstructedType != null)
	//							part = asMemberOfConstructedType.GetConstructedMember(method);
	
							if (methodNameNode != null)
							{
	//							if (methodNameNode.RuleName == "nonArrayType")
	//							{
	//								methodNameNode = methodNameNode.NodeAt(0);
	//							}
	
								if (methodNameNode.RuleName == "primaryExpressionStart")
								{
									var methodNameLeaf = methodNameNode.LeafAt(methodNameNode.numValidNodes < 3 ? 0 : 2);
									if (methodNameLeaf != null)
										methodNameLeaf.resolvedSymbol = nextPart;
								}
								else if (methodNameNode.RuleName == "primaryExpressionPart")
								{
									var accessIdentifierNode = methodNameNode.NodeAt(-1);
									if (accessIdentifierNode != null && accessIdentifierNode.RuleName == "accessIdentifier")
									{
										var methodNameLeaf = accessIdentifierNode.LeafAt(1);
										if (methodNameLeaf != null)
											methodNameLeaf.resolvedSymbol = nextPart;
									}
								}
	//							else if (methodNameNode.RuleName == "nonArrayType")
	//							{
	//								var nameNode = methodNameNode.ChildAt(0);
	//								while (nameNode is ParseTree.Node)
	//								{
	//									var nameNodeAsNode = nameNode as ParseTree.Node;
	//									if (nameNodeAsNode.RuleName == "namespaceOrTypeName")
	//										nameNode = nameNodeAsNode.ChildAt(-1);
	//									else
	//										nameNode = nameNodeAsNode.ChildAt(0);
	//								}
	//								nameNode.resolvedSymbol = method;
	//							}
							}
							else
							{
								node.ChildAt(i).resolvedSymbol = method;
							}
						}
					}
					
					var childNode = child as ParseTree.Node;
					if (childNode != null)
					{
						if (childNode.RuleName == "primaryExpressionPart")
							childNode = childNode.NodeAt(-1);
						else
							childNode = childNode.NodeAt(0);
					}
					if (childNode != null)// && childNode.RuleName == "accessIdentifier")
					{
						if (nextPart != null && invokeTarget != null && invokeTargetLeaf != null && !(invokeTarget is TypeDefinitionBase) &&
							(nextPart is TypeDefinitionBase || nextPart.IsStatic))
						{
							switch (invokeTarget.kind)
							{
							case SymbolKind.ConstantField:
							case SymbolKind.Field:
							case SymbolKind.Property:
							case SymbolKind.Indexer:
							case SymbolKind.Event:
							case SymbolKind.LocalConstant:
							case SymbolKind.Variable:
							case SymbolKind.TupleDeconstructVariable:
							case SymbolKind.OutVariable:
							case SymbolKind.IsVariable:
							case SymbolKind.CaseVariable:
							case SymbolKind.ForEachVariable:
							case SymbolKind.FromClauseVariable:
							case SymbolKind.Parameter:
							case SymbolKind.CatchParameter:
							case SymbolKind.Instance:
								var parentType = nextPart.parentSymbol;
								while (parentType != null && !(parentType is TypeDefinitionBase))
									parentType = parentType.parentSymbol;
								if (parentType != null && parentType.NumTypeParameters == 0 &&
									invokeTarget.NumTypeParameters == 0 && invokeTarget.name == parentType.name)
								{
									if (childNode.RuleName == "arguments")
									{
										invokeTargetLeaf = child.parent.NodeAt(child.childIndex - 2).firstChild as ParseTree.Leaf;
										if (invokeTargetLeaf != null)
											invokeTargetLeaf.resolvedSymbol = parentType;
									}
									else
									{
										invokeTargetLeaf.resolvedSymbol = parentType;
									}
									//Debug.Log(invokeTarget.GetTooltipText() + " ## " + (nextPart != null ? nextPart.GetTooltipText() : "null"));
								}
								break;
							}
						}
					}
					part = nextPart;
					if (part == null)// || part.kind == SymbolKind.Error)
						break;

					if (part.kind == SymbolKind.Method)
					{
						var currentNode = child as ParseTree.Node;
						if (currentNode != null)
							currentNode = currentNode.RuleName == "primaryExpressionPart" ? currentNode.NodeAt(-1) : null;
						if (currentNode == null || currentNode.RuleName != "arguments")
						{
							var constructedSymbolRef = part as ConstructedSymbolReference;
							if (constructedSymbolRef != null && constructedSymbolRef.kind == SymbolKind.Method)
							{
								var constructedType = constructedSymbolRef.parentSymbol as ConstructedTypeDefinition;
								part = constructedType.GetConstructedMember(part.GetGenericSymbol().parentSymbol);
							}
							else if (part.parentSymbol != null && part.parentSymbol.kind == SymbolKind.MethodGroup)
							{
								part = part.parentSymbol;
							}
						}
					}

					if (part == null)
						break;

					if (part.kind == SymbolKind.Method)
					{
						var returnType = (part = part.TypeOf()) as TypeDefinitionBase;
						if (returnType != null)
							part = returnType.GetThisInstance();
					}
					else if (part.kind == SymbolKind.Constructor)
					{
						var type = part.parentSymbol as TypeDefinitionBase ?? part.parentSymbol.parentSymbol as TypeDefinitionBase;
						part = type.GetThisInstance();
					}
					
					if (part == null)// || part.kind == SymbolKind.Error)
						break;
					
					if (part.kind != SymbolKind.MethodGroup)
					{
						invokeTarget = part;
					}
					
					var partNode = child as ParseTree.Node;
					if (partNode != null)
					{
						if (partNode.RuleName == "primaryExpressionPart")
						{
							var accessIdentifierNode = partNode.NodeAt(-1);
							if (accessIdentifierNode != null && accessIdentifierNode.RuleName == "accessIdentifier")
							{
								invokeTargetLeaf = accessIdentifierNode.LeafAt(1);
								
								if (accessIdentifierNode.childIndex == 1)
								{
									returnNullable = true;
								}
								
								var prevPartType = asMemberOf.TypeOf() as TypeDefinitionBase;
								if (prevPartType != null)
								{
									if (prevPartType == builtInTypes_void)
									{
										var accessLeaf = accessIdentifierNode.LeafAt(0);
										accessLeaf.semanticError =
											returnNullable
											? "Operator '?.' cannot be applied to operand of type void"
											: "Operator '.' cannot be applied to operand of type void";
										return unknownType;
									}
									else if (returnNullable)
									{
										if (prevPartType.kind == SymbolKind.Enum ||
											prevPartType.kind == SymbolKind.Struct && prevPartType.GetGenericSymbol() != builtInTypes_Nullable)
										{
											var accessLeaf = partNode.LeafAt(0);
											accessLeaf.semanticError = "Operator '?.' cannot be applied to operand of a value type";
											return unknownType;
										}
									}
								}
							}
							else
							{
								invokeTargetLeaf = null;
							}
						}
						else if (partNode.RuleName == "primaryExpressionStart")
						{
							var identifierLeaf = partNode.LeafAt(0);
							if (identifierLeaf != null && identifierLeaf.token.tokenKind == SyntaxToken.Kind.Identifier)
								invokeTargetLeaf = partNode.LeafAt(partNode.numValidNodes == 3 ? 2 : 0);
						}
					}
				}
				if (part == null)
					break;
				if (returnNullable)
				{
					var partType = part.TypeOf() as TypeDefinitionBase;
					if (partType != null && (partType.kind == SymbolKind.Struct || partType.kind == SymbolKind.Enum))
						part = partType.MakeNullableType().GetThisInstance();
				}
				if (part == null)
					break;

				//int childIndex;
				//var firstNonTrivialParent = node.FirstNonTrivialParent(out childIndex);
				//while (firstNonTrivialParent != null && childIndex > 0 && firstNonTrivialParent.RuleName == "conditionalExpression")
				//	firstNonTrivialParent = firstNonTrivialParent.FirstNonTrivialParent(out childIndex);
				//if (firstNonTrivialParent != null && childIndex == 2)
				//{
				//	if (firstNonTrivialParent.RuleName == "assignment" || firstNonTrivialParent.RuleName == "localVariableDeclarator")
				//	{
				//		CheckAssignment(firstNonTrivialParent, part);
				//	}
				//}
				
				return part ?? unknownSymbol;

			case "primaryExpressionStart":
				if (node.numValidNodes == 1)
				{
					baseNode = node.ChildAt(0);
					//scope = scope;
					asMemberOf = null;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				if (node.numValidNodes == 2)
				{
					var typeArgsNode = node.NodeAt(1);
					if (typeArgsNode != null && typeArgsNode.RuleName == "typeArgumentList")
						numTypeArguments = typeArgsNode.numValidNodes / 2;
					asMemberOf = ResolveNode(node.ChildAt(0), scope, null, numTypeArguments);
					if (asMemberOf is TypeDefinitionBase)
					{
						baseNode = typeArgsNode;
						//scope = scope;
						//asMemberOf = asMemberOf;
						numTypeArguments = 0;
						asTypeOnly = false;
						goto reresolve;
					}
					else
						return asMemberOf;
					//return ResolveNode(node.ChildAt(0), scope, null, numTypeArguments);
				}
				if (node.numValidNodes == 3)
				{
					part = ResolveNode(node.ChildAt(0), scope, null);
					
					baseNode = node.ChildAt(2);
					//scope = scope;
					asMemberOf = part;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				break;

			case "primaryExpressionPart":
				if (asMemberOf == null)
				{
					asMemberOf = ResolveNode(node.FindPreviousNode(), scope);
					if (asMemberOf != null && asMemberOf.kind == SymbolKind.Method)
						asMemberOf = asMemberOf.TypeOf();
				}
				if (asMemberOf != null)
				{
					baseNode = node.ChildAt(-1);
					//scope = scope;
					//asMemberOf = asMemberOf;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				break;

			case "nullForgivingOperator":
				if (asMemberOf == null)
				{
					baseNode = node.FindPreviousNode();
					//scope = scope;
					//asMemberOf = asMemberOf;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				return asMemberOf;

			case "brackets":
				if (asMemberOf == null)
				{
					var prevNode = node.FindPreviousNode();
					if (prevNode != null && prevNode.IsLit("?"))
						prevNode = prevNode.FindPreviousNode();
					asMemberOf = ResolveNode(prevNode, scope);
					if (asMemberOf != null && asMemberOf.kind == SymbolKind.Method)
						asMemberOf = asMemberOf.TypeOf();
				}
				if (asMemberOf != null)
				{
				//	Debug.LogWarning("Resolving brackets on " + asMemberOf.GetTooltipText());
					var arrayType = asMemberOf.TypeOf() as ArrayTypeDefinition;
					if (arrayType != null && arrayType.elementType != null)
					{
					//	UnityEngine.Debug.Log("    elementType " + arrayType.elementType.TypeOf());
						return (arrayType.elementType.definition as TypeDefinitionBase ?? unknownType).GetThisInstance();
					}
					if (node.numValidNodes == 3)
					{
						var expressionListNode = node.NodeAt(1);
						if (expressionListNode != null && expressionListNode.numValidNodes >= 1)
						{
							var argumentTypes = new TypeDefinitionBase[(expressionListNode.numValidNodes + 1) / 2];
							for (var i = 0; i < argumentTypes.Length; ++i)
							{
								var expression = ResolveNode(expressionListNode.ChildAt(i*2), scope);
								if (expression == null)
									goto default;
								argumentTypes[i] = expression.TypeOf() as TypeDefinitionBase;
							}
							var typeOf = asMemberOf.TypeOf() as TypeDefinitionBase;
							if (typeOf == null)
								return null;
							
							var asPointerType = typeOf as PointerTypeDefinition;
							if (asPointerType != null)
							{
								var referentType = asPointerType.referentType.definition as TypeDefinitionBase ?? unknownType;
								return referentType.GetThisInstance();
							}

							var indexer = typeOf == null ? null : typeOf.GetIndexer(argumentTypes);
							if (indexer != null)
							{
								typeOf = indexer.TypeOf() as TypeDefinitionBase;
								return typeOf == null ? null : typeOf.GetThisInstance();
							}
							else
							{
								return unknownSymbol;
							}
						}
					}
				}
				break;

			case "accessIdentifier":
				if (asMemberOf == null)
				{
					var prevNode = node.FindPreviousNode();
					if (prevNode != null && prevNode.IsLit("?"))
						prevNode = prevNode.FindPreviousNode();
					asMemberOf = ResolveNode(prevNode, scope);
					if (asMemberOf != null && asMemberOf.kind == SymbolKind.Method)
						asMemberOf = asMemberOf.TypeOf();
				}
				{
					var leaf1 = node.LeafAt(1);
					if (leaf1 != null && leaf1.token.text == ".")
					{
						var rangeToExpressionNode = node.NodeAt(2);
						if (rangeToExpressionNode != null)
						{
							//var rangeToExpression =
							ResolveNode(rangeToExpressionNode, scope);
						}
						return SymbolDefinition.builtInTypes_Range.GetThisInstance();
					}
				}
				if (node.numValidNodes == 2)
				{
					var node1 = node.ChildAt(1);
					if (!node1.missing)
					{
						baseNode = node1;
						//scope = scope;
						//asMemberOf = asMemberOf;
						numTypeArguments = 0;
						asTypeOnly = false;
						goto reresolve;
					}
				}
				else if (node.numValidNodes == 3)
				{
					var typeArgsNode = node.NodeAt(2);
					if (typeArgsNode != null && typeArgsNode.RuleName == "typeArgumentList")
						numTypeArguments = typeArgsNode.numValidNodes / 2;
					asMemberOf = ResolveNode(node.ChildAt(1), scope, asMemberOf, numTypeArguments);
					
					baseNode = typeArgsNode;
					//scope = scope;
					//asMemberOf = asMemberOf;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				return asMemberOf;

			case "typeArgumentList":
				if (asMemberOf == null)
				{
					//Debug.Log("asMemberOf is null / resolving " + node);
					asMemberOf = ResolveNode(node.FindPreviousNode(), scope);
				}
				numTypeArguments = node.numValidNodes / 2;
				var genericMethodGroup = asMemberOf as MethodGroupDefinition;
				var constructedSymbolReference = asMemberOf as ConstructedSymbolReference;
				if (constructedSymbolReference != null)
					genericMethodGroup = constructedSymbolReference.referencedSymbol as MethodGroupDefinition;
				if (genericMethodGroup != null)
				{
					var typeArgs = TypeReference.AllocArray(numTypeArguments);
					for (var i = 0; i < numTypeArguments; ++i)
						typeArgs[i] = TypeReference.To(node.ChildAt(2 * i + 1));
					genericMethodGroup = genericMethodGroup.ConstructMethodGroup(typeArgs);
					TypeReference.ReleaseArray(typeArgs);

					var methodGroupLeaf = node.FindPreviousLeaf();

					if (constructedSymbolReference == null)
					{
						if (methodGroupLeaf != null)
							methodGroupLeaf.resolvedSymbol = genericMethodGroup;
						return genericMethodGroup;
					}

					var constructedType = constructedSymbolReference.parentSymbol as ConstructedTypeDefinition;
					if (constructedType == null)
					{
						if (methodGroupLeaf != null)
							methodGroupLeaf.resolvedSymbol = genericMethodGroup;
						return genericMethodGroup;
					}

					constructedSymbolReference = constructedType.GetConstructedMember(genericMethodGroup) as ConstructedSymbolReference;
					asMemberOf = constructedSymbolReference ?? (SymbolDefinition) genericMethodGroup;
					if (methodGroupLeaf != null)
						methodGroupLeaf.resolvedSymbol = asMemberOf;
					return asMemberOf;
				}

				var genericType = asMemberOf as TypeDefinition;
				if (genericType != null)
				{
					var typeArgs = TypeReference.AllocArray(numTypeArguments);
					for (var i = 0; i < numTypeArguments; ++i)
						typeArgs[i] = TypeReference.To(node.ChildAt(2 * i + 1));
					if (genericType.typeParameters != null && genericType.typeParameters.Count == numTypeArguments)
					{
						var constructedType = genericType.ConstructType(typeArgs); // TODO: Fix this, add parentType argument.
						if (constructedType != null)
						{
							var prevNode = node.FindPreviousNode() as ParseTree.Leaf;
							if (prevNode != null)
								prevNode.resolvedSymbol = constructedType;
							TypeReference.ReleaseArray(typeArgs);
							return constructedType;
						}
					}
					TypeReference.ReleaseArray(typeArgs);
				}
				return asMemberOf;
			
			case "attributeArguments":
				if (asMemberOf == null)
				{
					var prevNode = node.FindPreviousNode();
					asMemberOf = ResolveNode(prevNode, scope);
				}
				var attributeArgumentListNode = node.numValidNodes >= 2 ? node.NodeAt(1) : null;
				if (attributeArgumentListNode != null)
					ResolveNode(attributeArgumentListNode, scope, asMemberOf);
				return resolvedChildren;
			
			case "parameterModifier":
				return dummy;
			
			case "gotoStatement":
				if (node.numValidNodes >= 2)
					return node.ChildAt(1).resolvedSymbol;
				else
					return dummy;
			
			case "arguments":
				if (asMemberOf == null)
				{
					var prevBaseNode = node.FindPreviousNode();
					asMemberOf = ResolveNode(prevBaseNode, scope);
					if (asMemberOf == null)
					{
						return null;
					}
				}

				var argumentListNode = node.NodeAt(1);
				if (argumentListNode != null && argumentListNode.numValidNodes > 0)
					ResolveNode(argumentListNode, scope);
				
				if (node.parent.RuleName == "attribute")
					return unknownSymbol;

				var baseNodeLeft = node.FindPreviousNode();
				var nodeLeftOfArguments = baseNodeLeft as ParseTree.Node;
				var idLeaf = baseNodeLeft as ParseTree.Leaf;
				if (nodeLeftOfArguments != null)
				{
					baseNodeLeft = nodeLeftOfArguments.firstChild;
					idLeaf = baseNodeLeft as ParseTree.Leaf;
					if (idLeaf == null && nodeLeftOfArguments.RuleName == "nonArrayType")
					{
						nodeLeftOfArguments = baseNodeLeft as ParseTree.Node;
						if (nodeLeftOfArguments != null)
						{
							nodeLeftOfArguments = nodeLeftOfArguments.firstChild as ParseTree.Node;
							if (nodeLeftOfArguments != null)
							{
								nodeLeftOfArguments = nodeLeftOfArguments.NodeAt(-1);
								if (nodeLeftOfArguments != null)
									idLeaf = nodeLeftOfArguments.GetFirstLeaf();
							}
						}
					}
					else if (idLeaf == null)
					{
						nodeLeftOfArguments = baseNodeLeft as ParseTree.Node;
						if (nodeLeftOfArguments != null)
							idLeaf = nodeLeftOfArguments.LeafAt(1);
					}
				}
				
#if SI3_WARNINGS
				if (idLeaf == null)
				{
					Debug.Log(node.FindPreviousNode());
				}
#endif

				var asMemberOfSymbolReference = asMemberOf as ConstructedSymbolReference;
				if (asMemberOfSymbolReference != null)
					asMemberOf = asMemberOfSymbolReference.referencedSymbol;
				var methodGroup = asMemberOf as MethodGroupDefinition;
				if (methodGroup != null)
				{
					asMemberOf = methodGroup.ResolveMethodOverloads(argumentListNode, null, scope, idLeaf);
					SymbolDefinition method = asMemberOf as MethodDefinition;
					if (method == null && asMemberOf != null && asMemberOf.kind == SymbolKind.Method)
						method = asMemberOf as ConstructedSymbolReference;
					if (method != null)
					{
						if (method.kind == SymbolKind.Error)
						{
							var type = methodGroup.parentSymbol as TypeDefinitionBase;
							if (type != null && type.kind == SymbolKind.Struct && (argumentListNode == null || argumentListNode.numValidNodes == 0))
							{
								method = type.GetDefaultConstructor();
								idLeaf.resolvedSymbol = method;
								idLeaf.semanticError = null;
							}
							else
							{
								idLeaf.resolvedSymbol = methodGroup;
								idLeaf.semanticError = method.name;
							}
						}
						else if (idLeaf.resolvedSymbol != method)
						{
							idLeaf.resolvedSymbol = method;
						}
						
						return method;
					}
				}
				else if (asMemberOf.kind == SymbolKind.MethodGroup)
				{
					var constructedMethodGroup = asMemberOf as ConstructedSymbolReference;
					if (constructedMethodGroup != null)
						asMemberOf = constructedMethodGroup.ResolveMethodOverloads(argumentListNode, null, scope, idLeaf);
					SymbolDefinition method = asMemberOf as MethodDefinition;
					if (method == null && asMemberOf != null && asMemberOf.kind == SymbolKind.Method)
						method = asMemberOf as ConstructedSymbolReference;
					if (method != null)
					{
						if (method.kind == SymbolKind.Error)
						{
							idLeaf.resolvedSymbol = methodGroup;
							idLeaf.semanticError = method.name;
						}
						else if (idLeaf.resolvedSymbol != method)
						{
							idLeaf.resolvedSymbol = method;
						}
						
						return method;
					}
				}
				else if (asMemberOf.kind != SymbolKind.Method && asMemberOf.kind != SymbolKind.Constructor && asMemberOf.kind != SymbolKind.Error)
				{
					var typeOf = asMemberOf.TypeOf() as TypeDefinitionBase;
					if (typeOf == null || typeOf.kind == SymbolKind.Error)
						return unknownType;

					var returnType = asMemberOf.kind == SymbolKind.Delegate ? typeOf :
						typeOf.kind == SymbolKind.Delegate ? typeOf.TypeOf() as TypeDefinitionBase : null;
					if (returnType != null)
						return returnType.GetThisInstance();
					
					//Debug.Log(">> " + asMemberOf.GetTooltipText());
					//Debug.Log(node);
//					if (asMemberOf.kind != SymbolKind.Event)
					if (node.RuleName != "arguments")
						node.LeafAt(0).semanticError = "Cannot invoke symbol";
				}
				
				return asMemberOf;

			case "argument":
				if (node.numValidNodes >= 1)
				{
					if (node.numValidNodes == 1)
					{
						baseNode = node.ChildAt(0);
						//scope = scope;
						asMemberOf = null;
						numTypeArguments = 0;
						asTypeOnly = false;
						goto reresolve;
					}
					else
						ResolveNode(node.ChildAt(0), scope);
				}
				if (node.numValidNodes == 3)
				{
					baseNode = node.ChildAt(2);
					//scope = scope;
					asMemberOf = null;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				return resolvedChildren;

			case "attributeArgument":
				if (node.numValidNodes >= 1)
				{
					if (node.numValidNodes == 1)
					{
						baseNode = node.ChildAt(0);
						//scope = scope;
						asMemberOf = null;
						numTypeArguments = 0;
						asTypeOnly = false;
						goto reresolve;
					}
					else
						ResolveNode(node.ChildAt(0), scope, asMemberOf);
				}
				if (node.numValidNodes == 3)
				{
					baseNode = node.ChildAt(2);
					//scope = scope;
					asMemberOf = null;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				return resolvedChildren;
			
			case "argumentList":
				for (var i = 0; i < node.numValidNodes; i += 2)
					dummy = ResolveNode(node.ChildAt(i), scope);
				return dummy;

			case "attributeArgumentList":
				for (var i = 0; i < node.numValidNodes; i += 2)
					dummy = ResolveNode(node.ChildAt(i), scope, asMemberOf);
				return resolvedChildren;

			case "argumentValue":
				baseNode = node.ChildAt(-1);
				//scope = scope;
				asMemberOf = null;
				numTypeArguments = 0;
				asTypeOnly = false;
				goto reresolve;

			case "argumentName":
				//return ResolveNode(node.ChildAt(0), asMemberOf: asMemberOf);
				// arguments
				// argumentList
				// argument
				var parameterNameLeaf = node.LeafAt(0);
				if (parameterNameLeaf == null)
					return unknownSymbol;
				var arguments = node.parent.parent.parent;
				var invokedNode = arguments.FindPreviousNode() as ParseTree.Leaf;
				if (invokedNode == null)
					return unknownSymbol;
				var invokedSymbol = invokedNode.resolvedSymbol;
				methodGroup = invokedSymbol as MethodGroupDefinition;
				if (methodGroup != null)
					return parameterNameLeaf.resolvedSymbol = methodGroup.ResolveParameterName(parameterNameLeaf);
				var invokableSymbol = invokedSymbol as InvokeableSymbolDefinition;
				if (invokableSymbol != null)
					return parameterNameLeaf.resolvedSymbol = invokableSymbol.ResolveParameterName(parameterNameLeaf);
				return parameterNameLeaf.resolvedSymbol = unknownSymbol;
			
			case "attributeMemberName":
				var asType = asMemberOf as TypeDefinitionBase;
				if (asType != null)
				{
					baseNode = node.ChildAt(0);
					//scope = scope;
					asMemberOf = asType.GetThisInstance();
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				return unknownSymbol;

			case "castExpression":
				if (node.numValidNodes == 4)
					ResolveNode(node.ChildAt(3), scope);
				
				var castType = ResolveNode(node.ChildAt(1), scope) as TypeDefinitionBase;
				if (castType != null)
					return castType.GetThisInstance();
				break;

			case "typeofExpression":
				if (node.numValidNodes >= 3)
				{
					var resolved = ResolveNode(node.ChildAt(2), scope);
					var type = resolved == null ? null : resolved.TypeOf();
					if (type != null && type == builtInTypes_dynamic)
						node.firstChild.semanticError = "Cannot get type of the dynamic type";
				}
				return builtInTypes_Type.GetThisInstance();

			case "defaultValueExpression":
				if (node.numValidNodes >= 3)
				{
					var typeNode = ResolveNode(node.ChildAt(2), scope) as TypeDefinitionBase;
					if (typeNode != null)
						return typeNode.GetThisInstance();
				}
				return DefaultValue.defaultTypeDefinition.GetThisInstance();

			case "sizeofExpression":
				if (node.numValidNodes >= 3)
				{
					//var sizeofType =
					ResolveNode(node.ChildAt(2), scope);
					//	as TypeDefinitionBase;
					//if (sizeofType != null && sizeofType.IsManagedType)
					//	node.firstChild.semanticError = "Cannot get size of the managed type";
				}
				return builtInTypes_int.GetThisInstance();

			case "nameofExpression":
				if (node.numValidNodes >= 3)
					ResolveNode(node.ChildAt(2), scope);
				return builtInTypes_string.GetThisInstance();

			case "checkedExpression":
			case "uncheckedExpression":
				if (node.numValidNodes >= 3)
				{
					baseNode = node.ChildAt(2);
					//scope = scope;
					asMemberOf = null;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				return unknownSymbol;

			case "localVariableDeclarator":
				//Debug.Log("=> : " + node);
				return null;

			case "assignment":
			{
				var destination = ResolveNode(node.ChildAt(0), scope);
				if (node.numValidNodes >= 3)
				{
					var exprNode = node.ChildAt(2);
					var resolvedExpression = ResolveNode(exprNode, scope);
					
					if (destination != null && resolvedExpression != null && resolvedExpression.kind == SymbolKind.MethodGroup)
					{
						var delegateType = destination.TypeOf() as TypeDefinitionBase;
						var asMethodGroup = resolvedExpression as MethodGroupDefinition;
						if (delegateType != null && delegateType.kind == SymbolKind.Delegate && asMethodGroup != null)
						{
							var methodGroupLeaf = exprNode.GetFirstLeaf();
							//var primaryExpressionNode = methodGroupLeaf.FindParentByName("primaryExpression");
							//var lastPartNode = primaryExpressionNode.NodeAt(-1);
							
							var matchingMethod = asMethodGroup.FindMatchingMethod(delegateType);
							if (matchingMethod != null)
							{
								methodGroupLeaf.resolvedSymbol = matchingMethod;
							}
						}
					}
				}
				return destination;
			}
			
			case "expression":
				if (node.parent.RuleName != "assignment")
				{
					baseNode = node.ChildAt(0);
					//scope = scope;
					asMemberOf = null;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				else
				{
					var result = ResolveNode(node.ChildAt(0), scope);
					if (result != null && result.kind == SymbolKind.MethodGroup)
					{
						var destination = ResolveNode(node.parent.ChildAt(0), scope);
						if (destination != null)
						{
							var delegateType = destination.TypeOf() as TypeDefinitionBase;
							var asMethodGroup = result as MethodGroupDefinition;
							if (delegateType != null && delegateType.kind == SymbolKind.Delegate && asMethodGroup != null)
							{
								var methodGroupLeaf = node.GetFirstLeaf();
								//var primaryExpressionNode = methodGroupLeaf.FindParentByName("primaryExpression");
								//var lastPartNode = primaryExpressionNode.NodeAt(-1);
						
								var matchingMethod = asMethodGroup.FindMatchingMethod(delegateType);
								if (matchingMethod != null)
								{
									methodGroupLeaf.resolvedSymbol = matchingMethod;
								}
							}
						}
					}
					return result;
				}
				
			case "localVariableInitializer":
			case "variableReference":
			case "constantExpression":
			case "nonAssignmentExpression":
				baseNode = node.ChildAt(0);
				//scope = scope;
				asMemberOf = null;
				numTypeArguments = 0;
				asTypeOnly = false;
				goto reresolve;

			case "tupleExpressionElement":
				baseNode = node.numValidNodes >= 3 ? node.ChildAt(2) : node.ChildAt(0);
				//scope = scope;
				asMemberOf = null;
				numTypeArguments = 0;
				asTypeOnly = false;
				goto reresolve;

			case "parenOrTupleExpression":
				leaf = node.LeafAt(2);
				if (leaf == null || !leaf.IsLit(","))
				{
					baseNode = node.NodeAt(1);
					if (baseNode == null)
						return null;
					leaf = ((ParseTree.Node)baseNode).LeafAt(1);
					if (leaf == null || !leaf.IsLit(":"))
					{
						//scope = scope;
						asMemberOf = null;
						numTypeArguments = 0;
						asTypeOnly = false;
						goto reresolve;
					}
				}
				// tupleExpression
				typeRefs = new List<TypeReference>(node.numValidNodes / 2);
				for (var i = 1; i < node.numValidNodes; i += 2)
				{
					var tupleElementNode = node.NodeAt(i);
					if (tupleElementNode != null)
					{
						tupleElementNode = tupleElementNode.NodeAt(-1);
					}
					
					if (tupleElementNode == null)
					{
						typeRefs.Add(TypeReference.To(unknownType));
					}
					else
					{
						var element = ResolveNode(tupleElementNode, scope, null, 0, false);
						typeRefs.Add(TypeReference.To(element == null ? unknownType : element.TypeOf()));
					}
				}
				tupleType = TypeDefinitionBase.MakeTupleType(typeRefs);
				for (var i = 1; i < node.numValidNodes; i += 2)
				{
					var tupleElementNode = node.NodeAt(i);
					if (tupleElementNode == null)
						continue;
					if (tupleElementNode.numValidNodes > 1)
					{
						var nameLeaf = tupleElementNode.GetFirstLeaf();
						if (nameLeaf != null)
						{
							var tupleField = tupleType.SetElementAliasName(i / 2, nameLeaf.token.text);
							if (tupleField != null)
								nameLeaf.SetDeclaredSymbol(tupleField);
						}
					}
					else
					{
						// Inferred tuple field names, a.k.a. tuple projection initializers
						
						var nameLeaf = tupleElementNode.GetLastLeaf();
						if (nameLeaf != null && nameLeaf.token.tokenKind == SyntaxToken.Kind.Identifier)
						{
							var firstLeaf = tupleElementNode.GetFirstLeaf();
							if (firstLeaf != nameLeaf)
							{
								var primaryExpressionPartNode = nameLeaf.FindParentByName("primaryExpressionPart");
								if (primaryExpressionPartNode == null)
								{
									nameLeaf = null;
								}
								else
								{
									var primaryExpressionNode = firstLeaf.FindParentByName("primaryExpression");
									if (primaryExpressionNode != primaryExpressionPartNode.parent)
									{
										nameLeaf = null;
									}
									else
									{
										var nonTrivialParent = primaryExpressionNode.FirstNonTrivialParent();
										if (nonTrivialParent != tupleElementNode.parent)
										{
											//Debug.Log(nonTrivialParent);
											//Debug.Log(tupleElementNode.parent);
											nameLeaf = null;
										}
									}
								}
							}
							
							if (nameLeaf != null)
								tupleType.SetElementAliasName(i / 2, nameLeaf.token.text);
						}
					}
				}
				return tupleType.GetThisInstance();

			case "nullCoalescingExpression":
				for (var i = 2; i < node.numValidNodes; i += 2)
					ResolveNode(node.ChildAt(i), scope);
				var lhs = ResolveNode(node.ChildAt(0), scope);
				if (node.numValidNodes >= 2 && lhs != null && (lhs.TypeOf() ?? unknownType).GetGenericSymbol() == builtInTypes_Nullable)
				{
					var constructedType = lhs.TypeOf() as ConstructedTypeDefinition;
					if (constructedType != null)
					{
						var nullableType = constructedType.typeArguments[0].definition as TypeDefinitionBase;
						if (nullableType != null)
							return nullableType.GetThisInstance();
					}
				}
				return lhs;

			case "conditionalExpression":
				if (node.numValidNodes >= 3)
				{
					ResolveNode(node.ChildAt(0), scope);
					
					var bestCommonType = new TypeInference.BestCommonTypeResolver(scope);
					bestCommonType.AddExpressionNode(node.FindChildByName("expression"));
					if (node.numValidNodes >= 5)
						bestCommonType.AddExpressionNode(node.ChildAt(-1));
					var commonType = bestCommonType.Resolve();
					if (commonType != null)
						return commonType.GetThisInstance();
					else
						return unknownType;
				}
				else
				{
					baseNode = node.ChildAt(0);
					//scope = scope;
					//asMemberOf = asMemberOf;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}

			case "unaryExpression":
				if (node.numValidNodes == 1)
				{
					baseNode = node.ChildAt(0);
					//scope = scope;
					asMemberOf = null;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				if (node.ChildAt(0) is ParseTree.Node)
				{
					baseNode = node.ChildAt(0);
					//scope = scope;
					asMemberOf = null;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				if (node.firstChild.IsLit("^"))
				{
					ResolveNode(node.ChildAt(1), scope);
					return builtInTypes_Index.GetThisInstance();
				}
				if (node.firstChild.IsLit("!"))
				{
					ResolveNode(node.ChildAt(1), scope);
					return builtInTypes_bool.GetThisInstance();
				}
				if (node.firstChild.IsLit("*"))
				{
					var resolved = ResolveNode(node.ChildAt(1), scope) as InstanceDefinition;
					if (resolved == null)
						return unknownType.GetThisInstance();
					var asPointer = resolved.TypeOf() as PointerTypeDefinition;
					if (asPointer == null)
						return unknownType.GetThisInstance();
					return (asPointer.referentType.definition as TypeDefinitionBase ?? unknownType).GetThisInstance();
				}
				if (node.firstChild.IsLit("&"))
				{
					var resolved = ResolveNode(node.ChildAt(1), scope);
					if (resolved == null)
						return unknownType.MakePointerType().GetThisInstance();
					var resolvedKind = resolved.kind;
					switch (resolvedKind)
					{
						case SymbolKind.Field:
						case SymbolKind.Parameter:
						case SymbolKind.CatchParameter:
						case SymbolKind.Variable:
						case SymbolKind.ForEachVariable:
						case SymbolKind.FromClauseVariable:
						case SymbolKind.TupleDeconstructVariable:
						case SymbolKind.CaseVariable:
						case SymbolKind.OutVariable:
						case SymbolKind.IsVariable:
						case SymbolKind.Instance: // TODO: Check!
							var typeOf = resolved.TypeOf();
							var asArray = typeOf as ArrayTypeDefinition;
							if (asArray != null)
							{
								var type = asArray.elementType.definition as TypeDefinitionBase ?? unknownType;
								return type.MakePointerType().GetThisInstance();
							}
							else
							{
								var type = resolved.TypeOf() as TypeDefinitionBase ?? unknownType;
								return type.MakePointerType().GetThisInstance();
							}
					}
					return unknownType.MakePointerType().GetThisInstance();
				}
				baseNode = node.ChildAt(1);
				//scope = scope;
				asMemberOf = null;
				numTypeArguments = 0;
				asTypeOnly = false;
				goto reresolve;
				
			case "flatExpression":
			{
				FlatExpressionResolver fer = new FlatExpressionResolver(node, scope);
				var result = fer.Resolve();
				if (result == null)
				{
					node.semanticError = "Cannot resolve expression";
				}
				else if (result.kind == SymbolKind.Error)
				{
					node.semanticError = result.name;
				}
				else
				{
					node.semanticError = null;
				}
				return result;
			}
			
			case "awaitExpression":
				if (node.numValidNodes < 2)
					return unknownType;
				var awaitOperand = ResolveNode(node.ChildAt(1), scope, null) ?? unknownType;
				var typeOfAwaitOperand = awaitOperand.TypeOf() as TypeDefinition ?? unknownType;
				if (typeOfAwaitOperand.kind == SymbolKind.Error)
					return typeOfAwaitOperand;
				
				if (typeOfAwaitOperand == builtInTypes_Task)
					return builtInTypes_void.GetThisInstance();
				
				var taskType = typeOfAwaitOperand.ConvertTo(builtInTypes_Task_1) as ConstructedTypeDefinition;
				if (taskType != null)
				{
					var returnTypeReference = taskType.typeArguments == null ? null : taskType.typeArguments.FirstOrDefault();
					if (returnTypeReference == null)
						return null;
					var returnType = returnTypeReference.definition as TypeDefinition;
					return returnType == null ? null : returnType.GetThisInstance();
				}
				
				var getAwaiterMethod = typeOfAwaitOperand.FindMethod("GetAwaiter", 0, 0, true);
				if (getAwaiterMethod == null)
				{
					getAwaiterMethod = scope.ResolveAsExtensionMethod("GetAwaiter", typeOfAwaitOperand, null, null, scope, null) as MethodDefinition;
				}
				if (getAwaiterMethod == null)
					return null;
				
				//TODO: Check is the GetAwaiter() method accessibie...
				var awaiterType = getAwaiterMethod.ReturnType();
				if (awaiterType == null || !awaiterType.DerivesFrom(builtInTypes_INotifyCompletion))
					return null;
				
				var getResultMethod = awaiterType.FindMethod("GetResult", 0, 0, true);
				if (getResultMethod == null)
					return null;
					
				var getResultReturnType = getResultMethod.ReturnType();
				getResultReturnType = getResultReturnType.SubstituteTypeParameters(getResultMethod);
				getResultReturnType = getResultReturnType.SubstituteTypeParameters(awaiterType);
				getResultReturnType = getResultReturnType.SubstituteTypeParameters(getAwaiterMethod);
				getResultReturnType = getResultReturnType.SubstituteTypeParameters(typeOfAwaitOperand);
				
				return getResultReturnType.GetThisInstance();
			
			case "preIncrementExpression":
			case "preDecrementExpression":
				if (node.numValidNodes == 2)
				{
					baseNode = node.ChildAt(1);
					//scope = scope;
					asMemberOf = null;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				return builtInTypes_int.GetThisInstance();

			case "inclusiveOrExpression":
			case "exclusiveOrExpression":
			case "andExpression":
			case "shiftExpression":
			case "multiplicativeExpression":
				for (var i = 2; i < node.numValidNodes; i += 2)
					ResolveNode(node.ChildAt(i), scope);
				baseNode = node.ChildAt(0);
				//scope = scope;
				asMemberOf = null;
				numTypeArguments = 0;
				asTypeOnly = false;
				goto reresolve; // HACK

			case "additiveExpression":
				part = ResolveNode(node.ChildAt(0), scope);
				for (var i = 2; i < node.numValidNodes; i += 2)
				{
					var rhs = ResolveNode(node.ChildAt(i), scope);
					if (part is InstanceDefinition && rhs is InstanceDefinition)
					{
						if (node.ChildAt(i - 1).IsLit("+"))
							part = ResolveBinaryOperation(Operator.ID.op_Addition, part, rhs, scope);
						else
							part = ResolveBinaryOperation(Operator.ID.op_Subtraction, part, rhs, scope);
					}
				}
				return part;

			case "arrayCreationExpression":
				if (asMemberOf == null)
					asMemberOf = ResolveNode(node.FindPreviousNode());
				var resultType = asMemberOf as TypeDefinitionBase;
				if (resultType == null)
					return unknownType.MakeArrayType(1);

				var rankSpecifiersNode = node.FindChildByName("rankSpecifiers") as ParseTree.Node;
				if (rankSpecifiersNode == null || rankSpecifiersNode.childIndex > 0)
				{
					var expressionListNode = node.NodeAt(1);
					if (expressionListNode != null && expressionListNode.RuleName == "expressionList")
						resultType = resultType.MakeArrayType(1 + expressionListNode.numValidNodes / 2);
				}
				if (rankSpecifiersNode != null && rankSpecifiersNode.numValidNodes != 0)
				{
					for (var i = 1; i < rankSpecifiersNode.numValidNodes; i += 2)
					{
						rank = 1;
						while (i < rankSpecifiersNode.numValidNodes && rankSpecifiersNode.ChildAt(i).IsLit(","))
						{
							++rank;
							++i;
						}
						resultType = resultType.MakeArrayType(rank);
					}
				}

				var initializerNode = node.NodeAt(-1);
				if (initializerNode != null && initializerNode.RuleName == "arrayInitializer")
					ResolveNode(initializerNode);

				return (resultType ?? unknownType).GetThisInstance();

			case "implicitArrayCreationExpression":
				resultType = null;

				var rankSpecifierNode = node.NodeAt(0);
				rank = rankSpecifierNode != null && rankSpecifierNode.numValidNodes > 0 ? rankSpecifierNode.numValidNodes - 1 : 1;

				initializerNode = node.NodeAt(1);
				var elements = initializerNode != null ? ResolveNode(initializerNode) : null;
				if (elements != null)
					resultType = (elements.TypeOf() as TypeDefinitionBase ?? unknownType).MakeArrayType(rank);

				return (resultType ?? unknownType).GetThisInstance();

			case "arrayInitializer":
				if (node.numValidNodes >= 2)
					if (!node.ChildAt(1).IsLit("}"))
					{
						baseNode = node.ChildAt(1);
						//scope = scope;
						asMemberOf = null;
						numTypeArguments = 0;
						asTypeOnly = false;
						goto reresolve;
					}
				return unknownType;

			case "variableInitializerList":
			{
				var bestCommonType = new TypeInference.BestCommonTypeResolver(scope);
				for (var i = 0; i < node.numValidNodes; i += 2)
					bestCommonType.AddExpressionNode(node.ChildAt(i));
				TypeDefinitionBase commonType = bestCommonType.Resolve();
				return commonType;
			}

			case "variableInitializer":
				baseNode = node.ChildAt(0);
				//scope = scope;
				asMemberOf = null;
				numTypeArguments = 0;
				asTypeOnly = false;
				goto reresolve;

			case "conditionalOrExpression":
				if (node.numValidNodes == 1)
				{
					node = node.NodeAt(0);
					goto case "conditionalAndExpression";
				}
				for (var i = 0; i < node.numValidNodes; i += 2)
					ResolveNode(node.ChildAt(i), scope);
				return builtInTypes_bool;

			case "conditionalAndExpression":
				if (node.numValidNodes == 1)
				{
					node = node.NodeAt(0);
					goto case "flatExpression";
					//goto case "inclusiveOrExpression";
				}
				for (var i = 0; i < node.numValidNodes; i += 2)
					ResolveNode(node.ChildAt(i), scope);
				return builtInTypes_bool;

			case "equalityExpression":
				if (node.numValidNodes == 1)
				{
					node = node.NodeAt(0);
					goto case "flatExpression";
				}
				for (var i = 0; i < node.numValidNodes; i += 2 )
					ResolveNode(node.ChildAt(i), scope);
				return builtInTypes_bool;

			case "relationalExpression":
				if (node.numValidNodes == 1)
				{
					node = node.NodeAt(0);
					goto case "flatExpression";
					//goto case "shiftExpression";
				}
				part = ResolveNode(node.ChildAt(0), scope);
				for (var i = 2; i < node.numValidNodes; i += 2)
				{
					if (node.ChildAt(i - 1).IsLit("as"))
					{
						part = ResolveNode(node.ChildAt(i), scope);
						if (part is TypeDefinitionBase)
							part = (part as TypeDefinitionBase).GetThisInstance();
					}
					else
					{
						ResolveNode(node.ChildAt(i), scope);
						part = builtInTypes_bool.GetThisInstance();
					}
				}
				return part;

			case "booleanExpression":
				ResolveNode(node.ChildAt(0), scope);
				return builtInTypes_bool;

			case "anonymousMethodExpression":
				if (node.numValidNodes > 1)
					ResolveNode(node.ChildAt(1), scope);
				if (node.numValidNodes == 3)
					ResolveNode(node.ChildAt(2), scope);
				var nodeScope = node.scope as SymbolDeclarationScope;
				if (nodeScope != null && nodeScope.declaration != null)
					return nodeScope.declaration.definition;
				return unknownSymbol;
			
			case "lambdaExpression":
				ResolveNode(node.ChildAt(0), scope);
			//	if (node.numValidNodes == 3)
			//		ResolveNode(node.ChildAt(2), scope);
				nodeScope = node.scope as SymbolDeclarationScope;
				if (nodeScope != null && nodeScope.declaration != null)
					return nodeScope.declaration.definition;
				return unknownSymbol;

			case "lambdaExpressionBody":
				{
					var firstLeaf = node.LeafAt(0);
					if (firstLeaf == null || firstLeaf.IsLit("ref"))
					{
						var expressionNode = node.NodeAt(0) ?? node.NodeAt(1);
						if (expressionNode != null)
						{
							baseNode = expressionNode;
							scope = null;
							asMemberOf = null;
							numTypeArguments = 0;
							asTypeOnly = false;
							goto reresolve;
						}
					}
					else if (firstLeaf.IsLit("{"))
					{
						var bestCommonType = new TypeInference.BestCommonTypeResolver();
						var returnedInstance = bestCommonType.ResolveReturnTypeOfStatementList(node.NodeAt(1));
						return returnedInstance;
					}
					return null;
				}

			case "objectCreationExpression":
				var objectType = (ResolveNode(node.FindPreviousNode(), scope) ?? unknownType).TypeOf() as TypeDefinitionBase;
				return objectType != null ? objectType.GetThisInstance() : null;

			case "targetTypedNewExpression":
				//var newLeaf = node.parent.LeafAt(0);
				var parentNode = node.parent.parent.parent.parent.parent.parent.parent.parent;
				switch (parentNode.RuleName)
				{
					case "argumentValue":
						break;
					case "localVariableDeclarator":
						break;
					default:
						break;
				}
				#if SI3_WARNINGS
				Debug.LogWarning("Implement resolver for targetTypedNewExpression node: " + parentNode);
				#endif
				return null;

			case "queryExpression":
				var queryBodyNode = node.NodeAt(1);
				if (queryBodyNode != null)
				{
					var selectClauseNode = queryBodyNode.FindChildByName("selectClause") as ParseTree.Node;
					if (selectClauseNode != null)
					{
						var selectExpressionNode = selectClauseNode.NodeAt(1);
						if (selectExpressionNode != null)
						{
							var element = ResolveNode(selectExpressionNode);
							if (element != null)
							{
								var elementType = element.TypeOf() as TypeDefinitionBase;
								if (elementType != null)
								{
									var constructedType = builtInTypes_IEnumerable_1.ConstructType(new[]{ TypeReference.To(elementType) });
									return constructedType.GetThisInstance();
								}
							}
						}
					}
				}
				return unknownSymbol;

			case "qid":
				for (var i = 0; i < node.numValidNodes - 1; i++)
				{
					asMemberOf = ResolveNode(node.ChildAt(i), scope, asMemberOf);
					if (asMemberOf == null || asMemberOf.kind == SymbolKind.Error)
						break;
				}
				if (node.numValidNodes == 1 && node.NodeAt(0).numValidNodes == 3)
					asMemberOf = ResolveNode(node.NodeAt(0).ChildAt(0), scope);
				return asMemberOf ?? unknownSymbol;

			case "qidStart":
				if (node.numValidNodes == 1)
				{
					baseNode = node.ChildAt(0);
					//scope = scope;
					asMemberOf = null;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				if (node.numValidNodes == 2 && node.NodeAt(1) != null)
				{
					ResolveNode(node.ChildAt(1), scope);

					baseNode = node.ChildAt(0);
					//scope = scope;
					asMemberOf = null;
					numTypeArguments = node.NodeAt(1).numValidNodes / 2;
					asTypeOnly = true;
					goto reresolve;
				}
				asMemberOf = ResolveNode(node.ChildAt(0), scope);
				if (asMemberOf != null && asMemberOf.kind != SymbolKind.Error && node.numValidNodes == 3)
				{
					baseNode = node.ChildAt(2);
					//scope = scope;
					//asMemberOf = asMemberOf;
					numTypeArguments = 0;
					asTypeOnly = false;
					goto reresolve;
				}
				return unknownSymbol;

			case "qidPart":
				baseNode = node.ChildAt(0);
				//scope = scope;
				//asMemberOf = asMemberOf;
				numTypeArguments = 0;
				asTypeOnly = false;
				goto reresolve;
				
			case "pattern":
				baseNode = node.ChildAt(0);
				//scope = scope;
				//asMemberOf = asMemberOf;
				numTypeArguments = 0;
				//asTypeOnly = false;
				goto reresolve;
				
			case "classMemberDeclaration":
				return null;

			case "implicitAnonymousFunctionParameterList":
			case "implicitAnonymousFunctionParameter":
			case "explicitAnonymousFunctionSignature":
			case "explicitAnonymousFunctionParameterList":
			case "explicitAnonymousFunctionParameter":
			case "anonymousFunctionSignature":
			case "typeParameterList":
			case "constructorInitializer":
			case "interfaceMemberDeclaration":
			case "collectionInitializer":
			case "elementInitializerList":
			case "elementInitializer":
			case "methodHeader":
				return null;

			default:
		//		if (missingResolveNodePaths.Add(node.RuleName))
		//			UnityEngine.Debug.Log("TODO: Add ResolveNode path for " + node.RuleName);
				return null;
		}

	//	Debug.Log("TODO: Canceled ResolveNode for " + node.RuleName);
		return null;
	}

	public virtual SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
	{
		return null;
	}

	public virtual SymbolDefinition FindName(CharSpan memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName.DecodeId();
		
		SymbolDefinition definition;
		if (!members.TryGetValue(memberName, numTypeParameters, out definition))
			return null;
		
		if (asTypeOnly && definition != null /*&& definition.kind == SymbolKind.Namespace)//*/ && !(definition is TypeDefinitionBase))
			return null;
		return definition;
	}

	public NamespaceDefinition FindNamespace(string namespaceName, int startAt, int length)
	{
		SymbolDefinition definition;
		members.TryGetValue(namespaceName, startAt, length, 0, out definition);
		return definition as NamespaceDefinition;
	}

	public NamespaceDefinition FindNamespace(CharSpan namespaceName)
	{
		SymbolDefinition definition;
		members.TryGetValue(namespaceName, 0, out definition);
		return definition as NamespaceDefinition;
	}

	public virtual void GetCompletionData(Dictionary<string, SymbolDefinition> data, ResolveContext context)
	{
		var tp = GetTypeParameters();
		if (tp != null)
		{
			for (var i = 0; i < tp.Count; ++i)
			{
				TypeParameterDefinition p = tp[i];
				if (!data.ContainsKey(p.name))
					data.Add(p.name, p);
			}
		}

		GetMembersCompletionData(data, context.fromInstance ? 0 : BindingFlags.Static, AccessLevelMask.Any, context);
	//	base.GetCompletionData(data, assembly);
	}

	public virtual void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, ResolveContext context)
	{
		if (kind == SymbolKind.Error)
			return;

		var myAssembly = Assembly;

		if ((mask & AccessLevelMask.Public) != 0 && myAssembly != null)
		{
			if (myAssembly.InternalsVisibleTo(context.assembly))
				mask |= AccessLevelMask.Internal;
			else
				mask &= ~AccessLevelMask.Internal;
		}
		
		flags = flags & (BindingFlags.Static | BindingFlags.Instance);
		bool onlyStatic = flags == BindingFlags.Static;
		bool onlyInstance = flags == BindingFlags.Instance;

		for (var i = 0; i < members.Count; ++i)
		{
			var m = members[i];
			
			if (m.kind == SymbolKind.Namespace)
			{
				if (!data.ContainsKey(m.ReflectionName))
					data.Add(m.ReflectionName, m);
			}
			else if (m.kind != SymbolKind.MethodGroup)
			{
				if ((onlyStatic ? !m.IsInstanceMember : onlyInstance ? m.IsInstanceMember : true)
					&& m.IsAccessible(mask)
					&& m.kind != SymbolKind.Constructor && m.kind != SymbolKind.Destructor && m.kind != SymbolKind.Indexer
					&& !data.ContainsKey(m.ReflectionName))
				{
					data.Add(m.ReflectionName, m);
				}
			}
			else
			{
				var methodGroup = m as MethodGroupDefinition;
				foreach (var method in methodGroup.methods)
					if ((onlyStatic ? method.IsStatic : onlyInstance ? !method.IsStatic : true)
						&& method.IsAccessible(mask)
						&& method.kind != SymbolKind.Constructor && method.kind != SymbolKind.Destructor && method.kind != SymbolKind.Indexer
						&& !data.ContainsKey(m.ReflectionName))
					{
						data.Add(m.ReflectionName, method);
					}
			}
		}
	}
	
	public bool IsInstanceMember
	{
		get
		{
			return !IsStatic && kind != SymbolKind.ConstantField && !(this is TypeDefinitionBase);
		}
	}
	
	public bool IsSealed
	{
		get
		{
			return (modifiers & Modifiers.Sealed) != 0;
		}
	}

	public virtual bool IsStatic
	{
		get
		{
			return (modifiers & Modifiers.Static) != 0;
		}
		set
		{
			if (value)
				modifiers |= Modifiers.Static;
			else
				modifiers &= ~Modifiers.Static;
		}
	}

	public bool IsPublic
	{
		get
		{
			return (modifiers & Modifiers.Public) != 0 ||
				(kind == SymbolKind.Namespace) ||
				parentSymbol != null && (
					parentSymbol.parentSymbol != null
					&& (kind == SymbolKind.Method || kind == SymbolKind.Indexer)
					&& (parentSymbol.parentSymbol.kind == SymbolKind.Interface)
					||
					(kind == SymbolKind.Property || kind == SymbolKind.Event)
					&& (parentSymbol.kind == SymbolKind.Interface)
				);
		}
		set
		{
			if (value)
				modifiers |= Modifiers.Public;
			else
				modifiers &= ~Modifiers.Public;
		}
	}

	public bool IsInternal
	{
		get
		{
			return (modifiers & Modifiers.Internal) != 0 ||
				kind != SymbolKind.Namespace && (modifiers & Modifiers.Public) == 0 && parentSymbol != null && parentSymbol.kind == SymbolKind.Namespace;
		}
		set
		{
			if (value)
				modifiers |= Modifiers.Internal;
			else
				modifiers &= ~Modifiers.Internal;
		}
	}

	public bool IsProtected
	{
		get
		{
			return (modifiers & Modifiers.Protected) != 0;
		}
		set
		{
			if (value)
				modifiers |= Modifiers.Protected;
			else
				modifiers &= ~Modifiers.Protected;
		}
	}
	
	public bool IsPrivate
	{
		get
		{
			return (modifiers & Modifiers.ProtectedOrInternalOrPublic) == 0;
		}
	}
	
	public bool IsProtectedInternal
	{
		get
		{
			return (modifiers & Modifiers.AccessMask) == Modifiers.InternalOrProtected;
		}
	}
	
	public bool IsPrivateProtected
	{
		get
		{
			return (modifiers & Modifiers.AccessMask) == Modifiers.ProtectedOrPrivate;
		}
	}
	
	public bool IsAbstract
	{
		get
		{
			return (modifiers & Modifiers.Abstract) != 0;
		}
		set
		{
			if (value)
				modifiers |= Modifiers.Abstract;
			else
				modifiers &= ~Modifiers.Abstract;
		}
	}
	
	public bool IsPartial
	{
		get { return (modifiers & Modifiers.Partial) != 0; }
	}
	
	//public virtual bool IsGeneric
	//{
	//	get
	//	{
	//		return false;
	//	}
	//}

	public AssemblyDefinition Assembly
	{
		get
		{
			var assembly = this;
			while (assembly != null)
			{
				if (assembly.kind == SymbolKind.None)
				{
					var result = assembly as AssemblyDefinition;
					if (result != null)
					{
						return result;
					}
				}
				assembly = assembly.parentSymbol;
			}
			return null;
		}
	}

	public virtual bool IsSameType(TypeDefinitionBase type)
	{
		return type == this;
	}

	public bool IsSameOrParentOf(TypeDefinitionBase type)
	{
		var constructedType = this as ConstructedTypeDefinition;
		var thisType = constructedType != null ? constructedType.genericTypeDefinition : this;
		while (type != null)
		{
			if (type == thisType)
				return true;
			constructedType = type as ConstructedTypeDefinition;
			type = (constructedType != null ? constructedType.genericTypeDefinition : type).parentSymbol as TypeDefinitionBase;
		}
		return false;
	}

	public virtual TypeDefinitionBase TypeOfTypeParameter(TypeParameterDefinition tp)
	{
		if (parentSymbol != null)
			return parentSymbol.TypeOfTypeParameter(tp);
		return tp;
	}

	public virtual bool IsAccessible(AccessLevelMask accessLevelMask)
	{
		if (accessLevelMask == AccessLevelMask.None)
			return false;
		if (IsPublic)
			return true;
		if (IsProtected && (accessLevelMask & AccessLevelMask.Protected) != 0)
			return true;
		if (IsInternal && (accessLevelMask & AccessLevelMask.Internal) != 0)
			return true;

		return (accessLevelMask & AccessLevelMask.Private) != 0;
	}

	public int NumTypeParameters {
		get {
			var typeParameters = GetTypeParameters();
			return typeParameters != null ? typeParameters.Count : 0;
		}
	}

	public int NumParameters {
		get {
			var parameters = GetParameters();
			return parameters != null ? parameters.Count : 0;
		}
	}
}

static class DictExtensions
{
	public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
	{
		return "{" + string.Join(",", dictionary.Select(kv => kv.Key.ToString() + "=" + kv.Value.ToString()).ToArray()) + "}";
	}
}

public class SymbolDeclaration //: IVisitableTreeNode<SymbolDeclaration, SymbolDeclaration>
{
	public SymbolDeclaration next;
	
	public SymbolDefinition definition;
	public Scope scope;

	public SymbolKind kind;

	public ParseTree.Node parseTreeNode;
	public Modifiers modifiers;
	public int numTypeParameters;

	protected CharSpan name;

	//public SymbolDeclaration parentDeclaration;
	//public List<SymbolDeclaration> nestedDeclarations = new List<SymbolDeclaration>();

	public SymbolDeclaration() {}

	public SymbolDeclaration(CharSpan name)
	{
		this.name = name;
	}

	public bool IsValid()
	{
		if (scope == null)
			return false;
		
		var node = parseTreeNode;
		if (node != null)
		{
			if (node.declaration == this)
			{
				while (node.parent != null)
					node = node.parent;
				if (node.RuleName == "compilationUnit")
					return true;
			}
			else if (kind == SymbolKind.MethodGroup && definition != null)
			{
				var mg = definition as MethodGroupDefinition;
				for (var i = mg.methods.Count; i --> 0; )
				{
					if (mg.methods[i].ContainsDeclaration(node.declaration))
					{
						while (node.parent != null)
							node = node.parent;
						if (node.RuleName == "compilationUnit")
							return true;
					}
					
					mg.RemoveDeclaration(node.declaration);
				}
				
				if (mg.methods.Count == 0 && mg.parentSymbol != null)
				{
					definition.parentSymbol.RemoveDeclaration(this);
					return false;
				}
				
				return true;
			}
		}

		if (scope != null)
		{
			scope.RemoveDeclaration(this);
			++ParseTree.resolverVersion;
			if (ParseTree.resolverVersion == 0)
				++ParseTree.resolverVersion;
		}
		else if (definition != null)
		{
			if (definition.parentSymbol != null)
				definition.parentSymbol.RemoveDeclaration(this);
		}
		scope = null;
		return false;
	}
	
	public bool IsPartial
	{
		get { return (modifiers & Modifiers.Partial) != 0; }
	}
	
	public bool IsAsync
	{
		get { return (modifiers & Modifiers.Async) != 0; }
	}

	public ParseTree.BaseNode NameNode()
	{
		if (parseTreeNode == null || parseTreeNode.numValidNodes == 0)
			return null;

		ParseTree.BaseNode nameNode = null;
		switch (parseTreeNode.RuleName)
		{
			case "namespaceDeclaration":
				nameNode = parseTreeNode.ChildAt(1);
				var nameNodeAsNode = nameNode as ParseTree.Node;
				if (nameNodeAsNode != null && nameNodeAsNode.numValidNodes != 0)
					nameNode = nameNodeAsNode.ChildAt(-1) ?? nameNode;
				break;

			case "usingAliasDirective":
				nameNode = parseTreeNode.ChildAt(0);
				break;

			case "interfaceDeclaration":
			case "structDeclaration":
			case "classDeclaration":
			case "enumDeclaration":
				nameNode = parseTreeNode.ChildAt(1);
				break;

			case "delegateDeclaration":
				nameNode = parseTreeNode.FindChildByName("NAME");
				break;

			case "eventDeclarator":
			case "eventWithAccessorsDeclaration":
			case "propertyDeclaration":
			case "interfacePropertyDeclaration":
			case "variableDeclarator":
			case "localVariableDeclarator":
			case "outVariableDeclarator":
			case "isVariableDeclarator":
			case "caseVariableDeclarator":
			case "constantDeclarator":
			case "interfaceMethodDeclaration":
			case "catchExceptionIdentifier":
			case "implicitDeconstructVariableDeclarator":
			case "explicitDeconstructVariableDeclarator":
				nameNode = parseTreeNode.ChildAt(0);
				break;

			case "methodDeclaration":
			case "constructorDeclaration":
				var methodHeaderNode = parseTreeNode.NodeAt(0);
				if (methodHeaderNode != null)
					nameNode = methodHeaderNode.ChildAt(0);
				break;

			case "localFunctionDeclaration":
				nameNode = parseTreeNode.LeafAt(3);
				break;

			case "methodHeader":
			case "constructorDeclarator":
				nameNode = parseTreeNode.ChildAt(0);
				break;

			case "destructorDeclaration":
				var destructorDeclaratorNode = parseTreeNode.NodeAt(0);
				if (destructorDeclaratorNode != null)
					nameNode = destructorDeclaratorNode.FindChildByName("IDENTIFIER");
				break;

			case "fixedParameter":
			case "operatorParameter":
			case "parameterArray":
			case "explicitAnonymousFunctionParameter":
				nameNode = parseTreeNode.FindChildByName("NAME");
				break;

			case "implicitAnonymousFunctionParameter":
				nameNode = parseTreeNode.ChildAt(0);
				break;

			case "typeParameter":
				nameNode = parseTreeNode.ChildAt(0);
				break;

			case "enumMemberDeclaration":
				if (parseTreeNode.ChildAt(0) is ParseTree.Node)
					nameNode = parseTreeNode.ChildAt(1);
				else
					nameNode = parseTreeNode.ChildAt(0);
				break;

			case "statementList":
				return null;

			case "lambdaExpression":
			case "anonymousMethodExpression":
				return parseTreeNode;

			case "interfaceTypeList":
				nameNode = parseTreeNode.ChildAt(0);
				break;

			case "interfaceEventDeclarator":
				nameNode = parseTreeNode;
				break;

			case "foreachStatement":
			case "fromClause":
				nameNode = parseTreeNode.FindChildByName("NAME");
				break;

			case "getAccessorDeclaration":
			case "interfaceGetAccessorDeclaration":
			case "setAccessorDeclaration":
			case "interfaceSetAccessorDeclaration":
			case "addAccessorDeclaration":
			case "removeAccessorDeclaration":
			case "readonlyAccessorDeclaration":
				nameNode = parseTreeNode.FindChildByName("IDENTIFIER");
				break;

			case "indexerDeclaration":
			case "interfaceIndexerDeclaration":
			case "labeledStatement":
				return parseTreeNode.ChildAt(0);

			case "conversionOperatorDeclarator":
			case "operatorDeclarator":
			case "usingNamespaceDirective":
			case "typeParameterConstraintsClause":
				return null;

			default:
				Debug.LogWarning("Don't know how to extract symbol name from: " + parseTreeNode);
				return null;
		}
		return nameNode;
	}

	public CharSpan Name
	{
		get
		{
			if (!name.IsEmpty)
				return name;

			if (definition != null)
				return name = definition.name;

			if (kind == SymbolKind.Constructor)
				return (modifiers & Modifiers.Static) != 0 ? name = ".cctor" : name = ".ctor";
			if (kind == SymbolKind.Destructor)
				return name = "Finalize";
			if (kind == SymbolKind.Indexer)
				return name = "Item";
			if (kind == SymbolKind.LambdaExpression)
			{
				var cuNode = parseTreeNode;
				while (cuNode != null && !(cuNode.scope is CompilationUnitScope))
					cuNode = cuNode.parent;
				name = cuNode != null ? cuNode.scope.CreateAnonymousName() : scope.CreateAnonymousName();
				return name;
			}
			if (kind == SymbolKind.Accessor)
			{
				switch (parseTreeNode.RuleName)
				{
					case "getAccessorDeclaration":
					case "interfaceGetAccessorDeclaration":
					case "readonlyAccessorDeclaration":
						return "get";
					case "setAccessorDeclaration":
					case "interfaceSetAccessorDeclaration":
						return "set";
					case "addAccessorDeclaration":
						return "add";
					case "removeAccessorDeclaration":
						return "remove";
				}
			}
			if (kind == SymbolKind.Operator)
			{
				switch (parseTreeNode.RuleName)
				{
				case "conversionOperatorDeclarator":
					return parseTreeNode.ChildAt(0).IsLit("implicit") ? "op_Implicit" : "op_Explicit";
					
				default:
					var operatorNode = parseTreeNode.NodeAt(1);
					if (operatorNode == null)
						return "UNKNOWN";

					var op = operatorNode.ChildAt(0);
					if (op == null)
						return "UNKNOWN";

					if (operatorNode.RuleName == "overloadableUnaryOperator")
					{
						if (op.IsLit("+"))
							return "op_UnaryPlus";
						if (op.IsLit("-"))
							return "op_UnaryNegation";
					}
					else
					{
						if (op.IsLit("+"))
							return "op_Addition";
						if (op.IsLit("-"))
							return "op_Subtraction";
					}

					if (op.IsLit("*"))
						return "op_Multiply";
					else if (op.IsLit("/"))
						return "op_Division";
					else if (op.IsLit("%"))
						return "op_Modulus";
					else if (op.IsLit("^"))
						return "op_ExclusiveOr";
					else if (op.IsLit("&"))
						return "op_BitwiseAnd";
					else if (op.IsLit("|"))
						return "op_BitwiseOr";
					//return "op_LogicalAnd";
					//return "op_LogicalOr";
					//return "op_Assign";
					else if (op.IsLit("<<"))
						return "op_LeftShift";
					else if (op.IsLit(">"))
					{
						var opNext = op.nextSibling;
						return opNext != null && opNext.IsLit(">") ? "op_RightShift" : "op_GreaterThan";
					}
					//return "op_SignedRightShift";
					//return "op_UnsignedRightShift";
					else if (op.IsLit("=="))
						return "op_Equality";
					else if (op.IsLit("<"))
						return "op_LessThan";
					else if (op.IsLit("!="))
						return "op_Inequality";
					else if (op.IsLit(">="))
						return "op_GreaterThanOrEqual";
					else if (op.IsLit("<="))
						return "op_LessThanOrEqual";
					//return "op_MultiplicationAssignment";
					//return "op_SubtractionAssignment";
					//return "op_ExclusiveOrAssignment";
					//return "op_LeftShiftAssignment";
					//return "op_ModulusAssignment";
					//return "op_AdditionAssignment";
					//return "op_BitwiseAndAssignment";
					//return "op_BitwiseOrAssignment";
					//return "op_Comma";
					//return "op_DivisionAssignment";
					else if (op.IsLit("--"))
						return "op_Decrement";
					else if (op.IsLit("++"))
						return "op_Increment";
					else if (op.IsLit("~"))
						return "op_OnesComplement";
					else if (op.IsLit("!"))
						return "op_LogicalNot";
					else if (op.IsLit("true"))
						return "op_True";
					else if (op.IsLit("false"))
						return "op_False";
					//return "op_UnsignedRightShiftAssignment";
					//return "op_RightShiftAssignment";
					//return "op_MemberSelection";
					//return "op_PointerToMemberSelection";
					//return "op_AddressOf";
					//return "op_PointerDereference";
					
					return "UNKNOWN";
				}
			}

			explicitInterfaceNode = null;

			var nameNode = NameNode();
			var asNode = nameNode as ParseTree.Node;
			if (asNode != null && asNode.RuleName == "memberName")
			{
				asNode = asNode.NodeAt(0);
				if (asNode != null && asNode.RuleName == "qid")
				{
					explicitInterfaceNode = asNode.NodeAt(-2);

					asNode = asNode.NodeAt(-1);
					if (asNode != null && asNode.numValidNodes != 0)
					{
						if (asNode.RuleName == "qidStart")
						{
							nameNode = asNode.ChildAt(0);
						}
						else
						{
							asNode = asNode.NodeAt(0);
							if (asNode != null && asNode.numValidNodes != 0)
							{
								nameNode = asNode.ChildAt(1);
							}
						}
					}
				}
			}
			var asLeaf = nameNode as ParseTree.Leaf;
			if (asLeaf != null && asLeaf.token != null && asLeaf.token.tokenKind != SyntaxToken.Kind.Identifier)
				nameNode = null;
			if (nameNode == null)
				name = "UNKNOWN";
			else
				name = nameNode.Print();
			return name;
		}
	}

	static private ParseTree.Node explicitInterfaceNode;
	static public ParseTree.Node ExplicitInterfaceNode
		{
			get
			{
				var result = explicitInterfaceNode;
				explicitInterfaceNode = null;
				return result;
			}
		}
	
	public string ReflectionName {
		get {
			if (numTypeParameters == 0)
				return Name;
			return Name + "`" + numTypeParameters;
		}
	}

	//public bool Accept(IHierarchicalVisitor<SymbolDeclaration, SymbolDeclaration> visitor)
	//{
	//    if (nestedDeclarations.Count == 0)
	//        return visitor.Visit(this);
		
	//    if (visitor.VisitEnter(this))
	//    {
	//        foreach (var nested in nestedDeclarations)
	//            if (!nested.Accept(visitor))
	//                break;
	//    }
	//    return visitor.VisitLeave(this);
	//}

	public override string ToString()
	{
		var sb = StringBuilders.Alloc();
		Dump(sb, string.Empty);
		var result = sb.ToString();
		StringBuilders.Release(sb);
		return result;
	}

	protected virtual void Dump(StringBuilder sb, string indent)
	{
		sb.AppendLine(indent + kind + " " + ReflectionName + " (" + GetType() + ")");
		
		//foreach (var nested in nestedDeclarations)
		//    nested.Dump(sb, indent + "  ");
	}

	public bool HasAllModifiers(Modifiers mods)
	{
		return (modifiers & mods) == mods;
	}

	public bool HasAnyModifierOf(Modifiers mods)
	{
		return (modifiers & mods) != 0;
	}
}

public class NamespaceDeclaration : SymbolDeclaration
{
	public List<TypeReference> importedNamespaces = new List<TypeReference>();
	public List<TypeReference> importedStaticTypes = new List<TypeReference>();
	public List<UsingAliasDefinition> usingAliases = new List<UsingAliasDefinition>();

	public NamespaceDeclaration(string nsName)
		: base(nsName)
	{}

	public NamespaceDeclaration() {}

	public void ImportNamespace(string namespaceToImport, ParseTree.BaseNode declaringNode)
	{
		throw new NotImplementedException ();
	}

	protected override void Dump(StringBuilder sb, string indent)
	{
		base.Dump(sb, indent);

		sb.AppendLine(indent + "Imports:");
		var indent2 = indent + "  ";
		foreach (var ns in importedNamespaces)
			sb.AppendLine(indent2 + ns);

		sb.AppendLine("  Aliases:");
		foreach (var ta in usingAliases)
			sb.AppendLine(indent2 + ta.name);

		sb.AppendLine("  Static imports:");
		foreach (var ta in importedStaticTypes)
			sb.AppendLine(indent2 + ta.definition.name);
	}
}

public class CompilationUnitScope : NamespaceScope
{
	public string path;

	public AssemblyDefinition assembly;

	private int numAnonymousSymbols;
	
	public CompilationUnitScope() : base(null) {}

	public override string CreateAnonymousName()
	{
		return ".Anonymous_" + numAnonymousSymbols++;
	}
}

public class AssemblyDefinition : SymbolDefinition
{
	public enum UnityAssembly
	{
		None,
		DllFirstPass,
		CSharpFirstPass,
		UnityScriptFirstPass,
		BooFirstPass,
		DllEditorFirstPass,
		CSharpEditorFirstPass,
		UnityScriptEditorFirstPass,
		BooEditorFirstPass,
		Dll,
		CSharp,
		UnityScript,
		Boo,
		DllEditor,
		CSharpEditor,
		UnityScriptEditor,
		BooEditor,

		Last = BooEditor
	}

	public readonly Assembly assembly;
	public readonly UnityAssembly assemblyId;
	public readonly bool isScriptAssembly;
	public readonly bool fromCsScripts;
#if UNITY_2017_3_OR_NEWER
	public readonly UnityEditor.Compilation.Assembly scriptAssembly;
#endif
	
	static class MonoIslandsHelper
	{
		static System.Collections.IEnumerable monoIslands;
		static FieldInfo outputField;
		static FieldInfo referencesField;
		static Type monoIslandType = System.Type.GetType("UnityEditor.Scripting.MonoIsland,UnityEditor.dll");
		static MethodInfo getMonoIslandsMethod = typeof(UnityEditorInternal.InternalEditorUtility).GetMethod("GetMonoIslands", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
		
		static MonoIslandsHelper()
		{
			if (getMonoIslandsMethod == null)
				return;
			
			if (monoIslandType == null)
				return;
			
			if (outputField == null)
			{
				outputField = monoIslandType.GetField("_output", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				referencesField = monoIslandType.GetField("_references", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (outputField == null || referencesField == null)
				return;
			
			monoIslands = getMonoIslandsMethod.Invoke(null, null) as System.Collections.IEnumerable;
		}
		
#if !UNITY_2017_3_OR_NEWER
		public static string[] GetReferencedAssembliesFor(string assemblyName)
		{
			if (monoIslands == null)
				return null;
			
			assemblyName += ".dll";
			foreach (var island in monoIslands)
			{
				var output = outputField.GetValue(island) as string;
				if (!output.EndsWith(assemblyName, StringComparison.InvariantCultureIgnoreCase))
					continue;
				
				var references = referencesField.GetValue(island) as string[];
				if (references == null)
					return null;
				
				for (var i = references.Length; i --> 0; )
				{
					var reference = references[i];
					references[i] = System.IO.Path.GetFullPath(reference);
				}
				return references;
			}
			
#if SI3_WARNINGS
			Debug.LogWarning(assemblyName + " not found");
#endif
			return null;
		}
#endif
	}
	
	public static string GetAssemblyNameLowercase(Assembly assembly)
	{
		var location = assembly.Location;
		var start = UnityEngine.Mathf.Max(location.LastIndexOf('/'), location.LastIndexOf('\\')) + 1;
		var end = location.LastIndexOf('.');

		var sb = StringBuilders.Alloc();
		for (int i = start; i < end; ++i)
			sb.Append(location[i].ToLowerAsciiInvariant());
		var name = sb.ToString();
		StringBuilders.Release(sb);
		
		return name;
	}
	
	private static int GetAssemblyNameLowercaseHashID(Assembly assembly)
	{
		var location = assembly.Location;
		var start = UnityEngine.Mathf.Max(location.LastIndexOf('/'), location.LastIndexOf('\\')) + 1;
		var end = location.LastIndexOf('.');
		var hash = SymbolDefinition.GetHashIDIgnoreCase(location, start, end - start);
		return hash;
	}
	
	private static bool IsManagedAssembly(string assemblyFile)
	{
#if UNITY_5_2 || UNITY_5_3 || UNITY_5_4_OR_NEWER
		var dllType = UnityEditorInternal.InternalEditorUtility.DetectDotNetDll(assemblyFile);
		return dllType != UnityEditorInternal.DllType.Unknown && dllType != UnityEditorInternal.DllType.Native;
#else
		return true;
#endif
	}
	
	private string[] referencedAssemblyNames;
	internal string[] ReferencedAssemblyNames {
		get {
			if (referencedAssemblyNames == null)
			{
				if (assembly != null)
				{
					var referencedAssemblies = assembly.GetReferencedAssemblies();
					referencedAssemblyNames = new string[referencedAssemblies.Length];
					for (var i = referencedAssemblies.Length; i --> 0; )
						referencedAssemblyNames[i] = referencedAssemblies[i].Name;
				}
			}
			return referencedAssemblyNames;
		}
	}
	
	private HashSet<int> referencedAssemblyIDs = new HashSet<int>();
	private AssemblyDefinition[] _referencedAssemblies;
	public AssemblyDefinition[] referencedAssemblies
	{
		get {
			if (_referencedAssemblies == null)
			{
				var raSetCompiled = new HashSet<AssemblyDefinition>();
				var raSetDefault = new HashSet<AssemblyDefinition>();
				var raSet = new HashSet<AssemblyDefinition>();
				
				if (assembly != null)
				{
#if UNITY_2017_3_OR_NEWER
					if (scriptAssembly != null)
					{
						var reflectionReferencedAssemblies = this.assembly.GetReferencedAssemblies();
				
						foreach (var ra in reflectionReferencedAssemblies)
						{
							var assemblyName = ra.Name.ToLowerAscii();
							var assemblyDefinition = FromName(assemblyName);
							if (assemblyDefinition != null)
								raSetCompiled.Add(assemblyDefinition);
						}
						
						raSetDefault.Add(AssemblyDefinition.FromName("mscorlib"));
						raSetDefault.Add(AssemblyDefinition.FromName("system"));
						raSetDefault.Add(AssemblyDefinition.FromName("system.core"));
						//raSetDefault.Add(AssemblyDefinition.FromName("system.data"));

						foreach (var ra in scriptAssembly.allReferences)
						{
							var assemblyDefinition = FromPath(ra);
							if (assemblyDefinition != null)
								raSet.Add(assemblyDefinition);
						}
						
						foreach (var ra in raSetDefault)
						{
							raSet.Remove(ra);
							raSetCompiled.Remove(ra);
						}
						foreach (var ra in raSetCompiled)
						{
							raSet.Remove(ra);
						}
					}
					else
					{
						#if SI3_WARNINGS
						Debug.LogError("Assembly '" + name + "' is not a script assembly!");
						#endif
					}
#else
					foreach (var ra in ReferencedAssemblyNames)
					{
						var assemblyDefinition = FromName(ra);
						if (assemblyDefinition != null)
							raSet.Add(assemblyDefinition);
					}
#endif
				}
				
#if !UNITY_2017_3_OR_NEWER
				var assemblyName = unityAssemblyNames[(int) assemblyId];
				if (assemblyName == null && assembly != null)
					assemblyName = AssemblyName;
				if (assemblyName != null)
				{
					var raPaths = MonoIslandsHelper.GetReferencedAssembliesFor(assemblyName);
					if (raPaths != null)
					{
						for (var i = 0; i < raPaths.Length; i++)
						{
							if (!IsManagedAssembly(raPaths[i]))
								continue;
							
							var ra = AssemblyDefinition.FromPath(raPaths[i]);
							if (ra != null)
								raSet.Add(ra);
							else
								Debug.LogWarning("Can't load " + raPaths[i]);
						}
					}
				}
				
				raSet.Add(AssemblyDefinition.FromName("mscorlib"));
				raSet.Add(AssemblyDefinition.FromName("nunit.framework"));
				raSet.Add(AssemblyDefinition.FromName("System"));
				raSet.Add(AssemblyDefinition.FromName("System.Core"));
				raSet.Add(AssemblyDefinition.FromName("System.Runtime.Serialization"));
				raSet.Add(AssemblyDefinition.FromName("System.XML"));
				raSet.Add(AssemblyDefinition.FromName("System.Xml.Linq"));

				raSet.Remove(null);
#endif
				
				_referencedAssemblies = new AssemblyDefinition[raSetCompiled.Count + raSetDefault.Count + raSet.Count];
				raSetDefault.CopyTo(_referencedAssemblies);
				raSetCompiled.CopyTo(_referencedAssemblies, raSetDefault.Count);
				raSet.CopyTo(_referencedAssemblies, raSetCompiled.Count + raSetDefault.Count);
				
				foreach (var ra in _referencedAssemblies)
				{
					referencedAssemblyIDs.Add(ra.hashID);
				}
			}

			return _referencedAssemblies;
		}
	}

	public Dictionary<string, CompilationUnitScope> compilationUnits;

	struct HashCode : IEquatable<HashCode>
	{
		public int value;

		public HashCode(int i)
		{
			value = i;
		}
				
		public bool Equals(HashCode other) { return value == other.value; }

		public override int GetHashCode()
		{
			return value;
		}

		static public implicit operator int(HashCode hash)
		{
			return hash.value;
		}

		static public implicit operator HashCode(int i)
		{
			return new HashCode(i);
		}
	}

	private static readonly Dictionary<HashCode, AssemblyDefinition> allAssemblyDefinitions = new Dictionary<HashCode, AssemblyDefinition>(512);
	public static AssemblyDefinition FromAssembly(Assembly assembly)
	{
		var key = GetAssemblyNameLowercaseHashID(assembly);
		//var key = assembly.GetHashCode();

		AssemblyDefinition definition;
		if (!allAssemblyDefinitions.TryGetValue(key, out definition))
		{
			definition = new AssemblyDefinition(assembly);
			allAssemblyDefinitions[key] = definition;

			//if (key == 0x559de7dc)
			//{
			//	definition.FindNamespace(CharSpan.Empty);
			//}
		}
		return definition;
	}

	private static readonly string[] unityAssemblyNames = new[]
	{
		null,
		null,
		"assembly-csharp-firstpass",
		"assembly-unityscript-firstpass",
		"assembly-boo-firstpass",
		null,
		"assembly-csharp-editor-firstpass",
		"assembly-unityscript-editor-firstpass",
		"assembly-boo-editor-firstpass",
		null,
		"assembly-csharp",
		"assembly-unityscript",
		"assembly-boo",
		null,
		"assembly-csharp-editor",
		"assembly-unityscript-editor",
		"assembly-boo-editor"
	};
	
	public static bool IsScriptAssemblyName(string name)
	{
		return Array.IndexOf<string>(unityAssemblyNames, name.ToLowerAscii()) >= 0;
	}
	
	private static Dictionary<string, Assembly> _domainAssemblies;
	private static Dictionary<string, Assembly> domainAssemblies {
		get {
			if (_domainAssemblies != null)
				return _domainAssemblies;

			_domainAssemblies = new Dictionary<string, Assembly>(512);
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly is System.Reflection.Emit.AssemblyBuilder)
					continue;
				_domainAssemblies[GetAssemblyNameLowercase(assembly)] = assembly;
			}
			AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoadEventHandler;
			return _domainAssemblies;
		}
	}
	
	private static void AssemblyLoadEventHandler(object sender, AssemblyLoadEventArgs args)
	{
		var assembly = args.LoadedAssembly;
		if (assembly is System.Reflection.Emit.AssemblyBuilder)
			return;
		_domainAssemblies[GetAssemblyNameLowercase(assembly)] = assembly;
	}
	
	private static Dictionary<string, AssemblyDefinition> reflectionOnlyAssemblies = new Dictionary<string, AssemblyDefinition>();
	
	private static AssemblyDefinition FromPath(string assemblyPath)
	{
		var assemblyName = System.IO.Path.GetFileNameWithoutExtension(assemblyPath).ToLowerAscii();
		System.Reflection.Assembly assembly;
		if (domainAssemblies.TryGetValue(assemblyName, out assembly))
			return FromAssembly(assembly);
		
		AssemblyDefinition assemblyDefinition;
		if (!reflectionOnlyAssemblies.TryGetValue(assemblyPath, out assemblyDefinition))
		{
			try
			{
				if (!IsManagedAssembly(assemblyPath))
					return null;
				
				var loadedAssembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(assemblyPath);
				assemblyDefinition = FromAssembly(loadedAssembly);
				if (assemblyDefinition != null)
					reflectionOnlyAssemblies[assemblyPath] = assemblyDefinition;
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}
		return assemblyDefinition;
	}
	
	public static AssemblyDefinition FromName(string assemblyName)
	{
		System.Reflection.Assembly assembly;
		if (domainAssemblies.TryGetValue(assemblyName, out assembly))
			return FromAssembly(assembly);
#if SI3_WARNINGS
		//Debug.LogWarning("No assembly for name: " + assemblyName);
#endif
		return null;
	}

	private static readonly AssemblyDefinition[] unityAssemblies = new AssemblyDefinition[1 + (int) UnityAssembly.Last];
	private static AssemblyDefinition FromId(UnityAssembly assemblyId)
	{
		if (assemblyId == UnityAssembly.None)
			return null;
		
		var index = (int) assemblyId;
		if (unityAssemblies[index] == null)
		{
			var assemblyName = unityAssemblyNames[index];
			unityAssemblies[index] = FromName(assemblyName) ?? new AssemblyDefinition(assemblyId);
		}
		return unityAssemblies[index];
	}
	
	public static bool IsIgnoredScript(string assetPath)
	{
		return
			assetPath.StartsWithIgnoreCase("assets/webplayertemplates/") ||
			assetPath.StartsWithIgnoreCase("assets/webgltemplates/") ||
			assetPath.StartsWithIgnoreCase("assets/streamingassets/");
	}
	
	private static UnityAssembly AssemblyIdFromAssetPath(string pathName)
	{
		var ext = (System.IO.Path.GetExtension(pathName) ?? string.Empty).ToLowerAscii();
		var isCSharp = ext == ".cs";
		var isUnityScript = ext == ".js";
		var isBoo = ext == ".boo";
		var isDll = ext == ".dll";
		if (!isCSharp && !isUnityScript && !isBoo && !isDll)
			return UnityAssembly.None;

		var path = (System.IO.Path.GetDirectoryName(pathName) ?? string.Empty).ToLowerAscii().Replace('\\', '/') + "/";

		if (!pathName.FastStartsWith("assets/"))
			return UnityAssembly.None;

		if (IsIgnoredScript(path))
			return UnityAssembly.None;
		
		bool isUnity_5_2_1p4_orNewer = true;
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
		isUnity_5_2_1p4_orNewer =
		UnityEngine.Application.unityVersion.FastStartsWith("5.2.1p") &&
			int.Parse(UnityEngine.Application.unityVersion.Substring("5.2.1p".Length)) >= 4;
#endif
		
		var isPlugins = path.FastStartsWith("assets/plugins/");
		var isStandardAssets = path.FastStartsWith("assets/standard assets/") ||
			path.FastStartsWith("assets/pro standard assets/");
		var isFirstPass = isPlugins || isStandardAssets;
		bool isEditor;
		if (isFirstPass && !isUnity_5_2_1p4_orNewer)
		{
			isEditor =
				isPlugins && path.FastStartsWith("assets/plugins/editor/") ||
				isStandardAssets && path.FastStartsWith("assets/pro standard assets/editor/") ||
				isStandardAssets && path.FastStartsWith("assets/standard assets/editor/");
		}
		else
		{
			isEditor = path.Contains("/editor/");
		}

		UnityAssembly assemblyId;
		if (isFirstPass && isEditor)
			assemblyId = isCSharp ? UnityAssembly.CSharpEditorFirstPass : isBoo ? UnityAssembly.BooEditorFirstPass : isUnityScript ? UnityAssembly.UnityScriptEditorFirstPass : UnityAssembly.DllEditorFirstPass;
		else if (isEditor)
			assemblyId = isCSharp ? UnityAssembly.CSharpEditor : isBoo ? UnityAssembly.BooEditor : isUnityScript ? UnityAssembly.UnityScriptEditor : UnityAssembly.DllEditor;
		else if (isFirstPass)
			assemblyId = isCSharp ? UnityAssembly.CSharpFirstPass : isBoo ? UnityAssembly.BooFirstPass : isUnityScript ? UnityAssembly.UnityScriptFirstPass : UnityAssembly.DllFirstPass;
		else
			assemblyId = isCSharp ? UnityAssembly.CSharp : isBoo ? UnityAssembly.Boo : isUnityScript ? UnityAssembly.UnityScript : UnityAssembly.Dll;
		
		return assemblyId;
	}
	
	public static AssemblyDefinition FromAssetPath(string pathName)
	{
#if UNITY_2017_3_OR_NEWER
		var asmName = UnityEditor.Compilation.CompilationPipeline.GetAssemblyNameFromScriptPath(pathName);
		asmName = asmName.Substring(0, asmName.Length - ".dll".Length).ToLowerAscii();
		var asmFromName = FromName(asmName);
		if (asmFromName != null)
			return asmFromName;
#endif
		return FromId(AssemblyIdFromAssetPath(pathName));
	}

	private AssemblyDefinition(UnityAssembly id)
	{
		assemblyId = id;
		isScriptAssembly = id != UnityAssembly.None
			&& id != UnityAssembly.Dll && id != UnityAssembly.DllFirstPass
			&& id != UnityAssembly.DllEditor && id != UnityAssembly.DllEditorFirstPass;
		fromCsScripts = id == UnityAssembly.CSharp || id == UnityAssembly.CSharpFirstPass
			|| id == UnityAssembly.CSharpEditor || id == UnityAssembly.CSharpEditorFirstPass;
	}
	
#if UNITY_2017_3_OR_NEWER
	private static Dictionary<int, UnityEditor.Compilation.Assembly> allScriptAssemblies;
#endif
	
	public static bool IsScriptAssembly(string name, Assembly assembly)
	{
#if !UNITY_2017_3_OR_NEWER
		return IsScriptAssemblyName(assemblyName);
#else
		if (IsScriptAssemblyName(name))
			return true;
		var scriptAssembly = FindScriptAssembly(assembly);
		return scriptAssembly != null;
#endif
	}
	
#if UNITY_2017_3_OR_NEWER
	public static UnityEditor.Compilation.Assembly FindScriptAssembly(Assembly assembly)
	{
		var assemblyLocation = assembly.Location;
		
		//if (string.IsNullOrEmpty(assemblyLocation))
		//	return null;
				
		//var location = System.IO.Path.GetDirectoryName(assemblyLocation);
		//if (!location.StartsWith(System.IO.Directory.GetCurrentDirectory(), StringComparison.InvariantCultureIgnoreCase))
		//{
		//	return null;
		//}
		
		if (allScriptAssemblies == null)
		{
			var scriptAssemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies();
			allScriptAssemblies = new Dictionary<int, UnityEditor.Compilation.Assembly>(scriptAssemblies.Length);
			foreach (var item in scriptAssemblies)
			{
				if (item.sourceFiles.Length == 0)// || item.sourceFiles[0].FastStartsWith("packages/"))
				{
					continue;
				}
				var fullPath = System.IO.Path.GetFullPath(item.outputPath);
				allScriptAssemblies.Add(fullPath.GetHashCode(), item);
				
				//Debug.Log(item.outputPath + " " + item.flags + ": " + string.Join(", ", item.sourceFiles));
			}
		}

		int key = assemblyLocation.GetHashCode();
		UnityEditor.Compilation.Assembly output = null;
		allScriptAssemblies.TryGetValue(key, out output);
		return output;
		
		//foreach (var asm in allScriptAssemblies)
		//{
		//	//if (assemblyLocation.EndsWith(asm.outputPath, StringComparison.InvariantCultureIgnoreCase))
		//	if (0 == string.Compare(System.IO.Path.GetFullPath(asm.outputPath), assemblyLocation, StringComparison.InvariantCultureIgnoreCase))
		//	{
		//		return asm;
		//	}
		//}
		
		////Debug.Log("Non-script Assembly: " + assembly.Location);
		//return null;
	}
#endif

	//public static AssemblyDefinition[] GetAllCSharpAssemblyDefinitions()
	//{
	//	var result = new List<AssemblyDefinition>();
	//	foreach (var kv in domainAssemblies)
	//	{
	//		if (!IsScriptAssembly(kv.Key, kv.Value))
	//			continue;
	//		var asmDef = FromAssembly(kv.Value);
	//		if (!asmDef.fromCsScripts)
	//			continue;
	//		result.Add(asmDef);
	//	}
	//	return result.ToArray();
	//}
	
	private AssemblyDefinition(Assembly assembly)
	{
		this.assembly = assembly;
		
#if !UNITY_2017_3_OR_NEWER
		isScriptAssembly = IsScriptAssemblyName(GetAssemblyNameLowercase(assembly));
#else
		scriptAssembly = FindScriptAssembly(assembly);
		isScriptAssembly = scriptAssembly != null;
#endif

		string nameAsString = GetAssemblyNameLowercase(assembly);
		name = nameAsString;
		switch (name)
		{
			case "assembly-csharp-firstpass":
				assemblyId = UnityAssembly.CSharpFirstPass;
				fromCsScripts = true;
				break;
			case "assembly-unityscript-firstpass":
				assemblyId = UnityAssembly.UnityScriptFirstPass;
				fromCsScripts = false;
				break;
			case "assembly-boo-firstpass":
				assemblyId = UnityAssembly.BooFirstPass;
				fromCsScripts = false;
				break;
			case "assembly-csharp-editor-firstpass":
				assemblyId = UnityAssembly.CSharpEditorFirstPass;
				fromCsScripts = true;
				break;
			case "assembly-unityscript-editor-firstpass":
				assemblyId = UnityAssembly.UnityScriptEditorFirstPass;
				fromCsScripts = false;
				break;
			case "assembly-boo-editor-firstpass":
				assemblyId = UnityAssembly.BooEditorFirstPass;
				fromCsScripts = false;
				break;
			case "assembly-csharp":
				assemblyId = UnityAssembly.CSharp;
				fromCsScripts = true;
				break;
			case "assembly-unityscript":
				assemblyId = UnityAssembly.UnityScript;
				fromCsScripts = false;
				break;
			case "assembly-boo":
				assemblyId = UnityAssembly.Boo;
				fromCsScripts = false;
				break;
			case "assembly-csharp-editor":
				assemblyId = UnityAssembly.CSharpEditor;
				fromCsScripts = true;
				break;
			case "assembly-unityscript-editor":
				assemblyId = UnityAssembly.UnityScriptEditor;
				fromCsScripts = false;
				break;
			case "assembly-boo-editor":
				assemblyId = UnityAssembly.BooEditor;
				fromCsScripts = false;
				break;
			default:
				assemblyId = UnityAssembly.None;
				fromCsScripts = false;
				break;
		}
		
		//hashID = GetHashID();
		if (this.isScriptAssembly)
		{
			fromCsScripts = true;
		}
	}

	private string assemblyName;
	public string AssemblyName
	{
		get
		{
			if (assemblyName != null)
				return assemblyName;
			if (assembly != null)
				assemblyName = GetAssemblyNameLowercase(assembly);
			else
				assemblyName = unityAssemblyNames[(int) assemblyId] ?? "<Unknown-Assembly>";
			return assemblyName;
		}
	}

	private HashSet<string> internalsVisibleTo;
	
	public bool InternalsVisibleTo(AssemblyDefinition referencingAssembly)
	{
		if (referencingAssembly == this)
			return true;

		if (assembly == null)
			return false;
		
		if (internalsVisibleTo == null)
		{
			internalsVisibleTo = new HashSet<string>();

			var attributes = assembly.GetCustomAttributes<System.Runtime.CompilerServices.InternalsVisibleToAttribute>();
			foreach (var attr in attributes)
			{
				internalsVisibleTo.Add(attr.AssemblyName.ToLowerAscii());
			}
		}

		if (referencingAssembly == null)
			return false;
		else
			return internalsVisibleTo.Contains(referencingAssembly.AssemblyName);
	}

	public static CompilationUnitScope GetCompilationUnitScope(string assetPath, bool forceCreateNew = false)
	{
		if (assetPath == null)
			return null;
		
		assetPath = assetPath.ToLowerAscii();

		var assembly = FromAssetPath(assetPath);
		if (assembly == null)
		{
#if SI3_WARNINGS
			Debug.LogWarning("No assembly for path: " + assetPath);
#endif
			return null;
		}

		if (assembly.compilationUnits == null)
			assembly.compilationUnits = new Dictionary<string, CompilationUnitScope>();

		CompilationUnitScope scope;
		if (!assembly.compilationUnits.TryGetValue(assetPath, out scope) || forceCreateNew)
		{
			if (forceCreateNew)
			{
				if (scope != null && scope.typeDeclarations != null)
				{
					var newResolverVersion = false;
					var scopeTypes = scope.typeDeclarations;
					for (var i = scopeTypes.Count; i --> 0; )
					{
						var typeDeclaration = scopeTypes[i];
						scope.RemoveDeclaration(typeDeclaration);
						newResolverVersion = true;
					}
					if (newResolverVersion)
					{
						++ParseTree.resolverVersion;
						if (ParseTree.resolverVersion == 0)
							++ParseTree.resolverVersion;
					}
				}
				assembly.compilationUnits.Remove(assetPath);
			}

			scope = new CompilationUnitScope
			{
				assembly = assembly,
				path = assetPath,
			};
			assembly.compilationUnits[assetPath] = scope;

			//var cuDefinition = new CompilationUnitDefinition
			//{
			//    kind = SymbolKind.None,
			//    parentSymbol = assembly,
			//};

			scope.declaration = new NamespaceDeclaration
			{
				kind = SymbolKind.Namespace,
				definition = assembly.GlobalNamespace,
			};
			scope.definition = assembly.GlobalNamespace;
		}
		return scope;
	}
	
	public MiniBloom1 typesFilter;
	public MiniBloom1 namespacesFilter;

	private NamespaceDefinition _globalNamespace;
	public NamespaceDefinition GlobalNamespace
	{
		get { return _globalNamespace ?? InitializeGlobalNamespace(); }
		set { _globalNamespace = value; }
	}
	
	private static string projectPath = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.Length - "Assets".Length).ToLower().Replace('\\', '/');
	private static string unityDataPath = UnityEditor.EditorApplication.applicationContentsPath.ToLower().Replace('\\', '/');

	private NamespaceDefinition InitializeGlobalNamespace()
	{
	//	var timer = new Stopwatch();
		//	timer.Start();

		_globalNamespace = new NamespaceDefinition
		{
			name = "",
			kind = SymbolKind.Namespace,
			parentSymbol = this,
			accessLevel = AccessLevel.Public,
			modifiers = Modifiers.Public,
		};

		if (assembly != null)
		{
			System.Type[] types = null;
			//var types = /*isScriptAssembly ? (IEnumerable<Type>)*/ assembly.DefinedTypes /*: assembly.ExportedTypes*/;
			try
			{
				if (isScriptAssembly)
				{
					types = assembly.GetTypes();
				}
				else
				{
					var importAllTypes = false;
					
					var location = assembly.Location.Replace('\\', '/');
					
					if (location.StartsWithIgnoreCase(unityDataPath) && assembly.GetName().Name.StartsWithIgnoreCase("unity") ||
						location.StartsWithIgnoreCase(projectPath))
					{
						InternalsVisibleTo(null);
						importAllTypes = internalsVisibleTo.Count > 0;
					}

					types = importAllTypes ? assembly.GetTypes() : assembly.GetExportedTypes();
				}
			}
			catch
			{
#if SI3_WARNINGS
				if (types != null)
					foreach (var item in types)
						if (item != null)
							Debug.LogWarning(item);
#endif
				return GlobalNamespace;
			}
			
			//if (!isScriptAssembly)
			//	bloomFilter = new HashSet<int>(types.Length * 2);
			
			var namespaces = new Dictionary<int, NamespaceDefinition>();
			
			try
			{
				foreach (var t in types)
				{
					if (t == null)
						continue;
					
					//if (!isScriptAssembly && !t.IsPublic)
					//	continue;

					if (t.IsNested)
						continue;

					//ReflectedType reflectedType;
					//if (reflectedTypes.TryGetValue(t, out reflectedType))
					//{
					//	Debug.LogError("Skipping already imported type: " + reflectedType.ReflectionName);
					//	continue;
					//}
				
					NamespaceDefinition current = _globalNamespace;
				
					var typeNamespace = t.Namespace;
					if (!string.IsNullOrEmpty(typeNamespace))
					{
						var typeNamespaceHash = GetHashID(typeNamespace);
						
						NamespaceDefinition nsd;
						if (namespaces.TryGetValue(typeNamespaceHash, out nsd))
						{
							current = nsd;
						}
						else
						{
							var ns = typeNamespace.Split('.');
							for (var i = 0; i < ns.Length; ++i)
							{
								var nsName = ns[i];
								var definition = current.FindNamespace(nsName, 0, nsName.Length);
								if (definition != null)
								{
									current = definition;
								}
								else
								{
									nsd = new NamespaceDefinition
									{
										kind = SymbolKind.Namespace,
										name = nsName,
										parentSymbol = current,
										accessLevel = AccessLevel.Public,
										modifiers = Modifiers.Public,
									};
									current.AddMember(nsd);
									current = nsd;
								
									if (!isScriptAssembly)
										namespacesFilter.Add(GetHashID(nsName));
									
									var nn = NamespaceName.Get(nsd.FullName);
									nn.allNamespaces.Add(nsd);
								}
							}
							namespaces[typeNamespaceHash] = current;
						}
					}
	
					var importedType = current.ImportReflectedType(t);
				
					if (!isScriptAssembly && importedType != null)
						typesFilter.Add(importedType.hashID);
				}
			}
#if SI3_WARNINGS
			catch (System.Exception e)
			{
				Debug.LogException(e);
				Debug.Log("... for assembly " + name);
			}
#else
			catch {}
#endif
		}
		
		//if (!isScriptAssembly)
		//	Debug.Log(namespacesFilter.ToString() + " " + typesFilter.ToString() + " " + name + " " + _globalNamespace.members.Count);

		if (builtInTypes == null)
		{
			builtInTypes = new Dictionary<CharSpan, TypeDefinitionBase>(16);

			builtInTypes.Add("int", builtInTypes_int = DefineBuiltInType(typeof(int)));
			builtInTypes.Add("uint", builtInTypes_uint = DefineBuiltInType(typeof(uint)));
			builtInTypes.Add("byte", builtInTypes_byte = DefineBuiltInType(typeof(byte)));
			builtInTypes.Add("sbyte", builtInTypes_sbyte = DefineBuiltInType(typeof(sbyte)));
			builtInTypes.Add("short", builtInTypes_short = DefineBuiltInType(typeof(short)));
			builtInTypes.Add("ushort", builtInTypes_ushort = DefineBuiltInType(typeof(ushort)));
			builtInTypes.Add("long", builtInTypes_long = DefineBuiltInType(typeof(long)));
			builtInTypes.Add("ulong", builtInTypes_ulong = DefineBuiltInType(typeof(ulong)));
			builtInTypes.Add("float", builtInTypes_float = DefineBuiltInType(typeof(float)));
			builtInTypes.Add("double", builtInTypes_double = DefineBuiltInType(typeof(double)));
			builtInTypes.Add("decimal", builtInTypes_decimal = DefineBuiltInType(typeof(decimal)));
			builtInTypes.Add("char", builtInTypes_char = DefineBuiltInType(typeof(char)));
			builtInTypes.Add("string", builtInTypes_string = DefineBuiltInType(typeof(string)));
			builtInTypes.Add("bool", builtInTypes_bool = DefineBuiltInType(typeof(bool)));
			builtInTypes.Add("object", builtInTypes_object = DefineBuiltInType(typeof(object)));
			builtInTypes.Add("void", builtInTypes_void = DefineBuiltInType(typeof(void)));
			
			builtInTypes_Array = DefineBuiltInType(typeof(System.Array));
			builtInTypes_Nullable = DefineBuiltInType(typeof(System.Nullable<>));
			builtInTypes_IEnumerable = DefineBuiltInType(typeof(System.Collections.IEnumerable));
			builtInTypes_IEnumerable_1 = DefineBuiltInType(typeof(System.Collections.Generic.IEnumerable<>));
			builtInTypes_Exception = DefineBuiltInType(typeof(System.Exception));
			builtInTypes_Enum = DefineBuiltInType(typeof(System.Enum));
			builtInTypes_Expression_1 = DefineBuiltInType(typeof(System.Linq.Expressions.Expression<>));
			
			if (supportsDynamicType)
			{
				builtInTypes.Add("dynamic", builtInTypes_dynamic = DynamicTypeDefinition.dynamicTypeDefinition);
				builtInTypes_dynamic.parentSymbol = builtInTypes_object.parentSymbol.parentSymbol;
			}
			
			builtInTypes_Type = DefineBuiltInType(typeof(System.Type));

			var typeIndex = Type.GetType("System.Index,mscorlib");
			builtInTypes_Index = DefineBuiltInType(typeIndex) ?? unknownType;
			var typeRange = Type.GetType("System.Range,mscorlib");
			builtInTypes_Range = DefineBuiltInType(typeRange) ?? unknownType;

			var typeTask = Type.GetType("System.Threading.Tasks.Task,mscorlib");
			builtInTypes_Task = DefineBuiltInType(typeTask);
			var typeTask1 = Type.GetType("System.Threading.Tasks.Task`1,mscorlib");
			builtInTypes_Task_1 = DefineBuiltInType(typeTask1);
			var typeINotifyCompletion = Type.GetType("System.Runtime.CompilerServices.INotifyCompletion,mscorlib");
			builtInTypes_INotifyCompletion = DefineBuiltInType(typeINotifyCompletion);
			
			var typeValueTuple = Type.GetType("System.ValueTuple`1,mscorlib");
			builtInTypes_ValueTuple_1 = DefineBuiltInType(typeValueTuple);
			typeValueTuple = Type.GetType("System.ValueTuple`2,mscorlib");
			builtInTypes_ValueTuple_2 = DefineBuiltInType(typeValueTuple);
			typeValueTuple = Type.GetType("System.ValueTuple`3,mscorlib");
			builtInTypes_ValueTuple_3 = DefineBuiltInType(typeValueTuple);
			typeValueTuple = Type.GetType("System.ValueTuple`4,mscorlib");
			builtInTypes_ValueTuple_4 = DefineBuiltInType(typeValueTuple);
			typeValueTuple = Type.GetType("System.ValueTuple`5,mscorlib");
			builtInTypes_ValueTuple_5 = DefineBuiltInType(typeValueTuple);
			typeValueTuple = Type.GetType("System.ValueTuple`6,mscorlib");
			builtInTypes_ValueTuple_6 = DefineBuiltInType(typeValueTuple);
			typeValueTuple = Type.GetType("System.ValueTuple`7,mscorlib");
			builtInTypes_ValueTuple_7 = DefineBuiltInType(typeValueTuple);
			typeValueTuple = Type.GetType("System.ValueTuple`8,mscorlib");
			builtInTypes_ValueTuple_8 = DefineBuiltInType(typeValueTuple);
		}

		//	timer.Stop();
		//	UnityEngine.Debug.Log(timer.ElapsedMilliseconds + " ms\n" + string.Join(", ", _globalNamespace.members.Keys.ToArray()));
		//	Debug.Log(_globalNamespace.Dump());

		return _globalNamespace;
	}

	public static TypeDefinition DefineBuiltInType(Type type)
	{
		if (type == null)
			return null;
		var assembly = FromAssembly(type.Assembly);
		var namespaceDef = assembly.FindNamespace(type.Namespace);
		var name = type.Name;
		var index = name.IndexOf('`');
		if (index > 0)
			name = name.Substring(0, index);
		var definition = namespaceDef.FindName(name, type.GetGenericArguments().Length, true);
		return definition as TypeDefinition;
	}

	public SymbolDefinition FindNamespace(ParseTree.Node nsPathNode, bool tryReferencedAssemblies, NamespaceDefinition asChildOf = null)
	{
		var lastNode = nsPathNode.NodeAt(-1);
		var lastLeaf = lastNode == null ? null : lastNode.LeafAt(0);
		SymbolDefinition result = lastNode == null ? null : lastLeaf.resolvedSymbol;
		if (result != null && result.kind != SymbolKind.Error)
		{
			if (asChildOf == null || result.parentSymbol.name == asChildOf.name)
				return result;
			else
				return null;
		}
		
		result = asChildOf ?? GlobalNamespace;
		for (var i = 0; i < nsPathNode.numValidNodes; i += 2)
		{
			var node = nsPathNode.NodeAt(i);
			if (node == null)
				break;
			var leaf = node.LeafAt(0);
			result.members.TryGetValue(leaf.Print(), 0, out result);
			leaf.resolvedSymbol = result;
			result = result as NamespaceDefinition;
			if (result == null)
				break;
		}

		if (result != null)
			return result;

		if (!tryReferencedAssemblies)
			return null;
		
		var array = referencedAssemblies;
		var length = array.Length;
		for (var i = 0; i < length; ++i)
		{
			var ra = array[i];
			var asChildOfInRefAsm = asChildOf != null ? ra.FindSameNamespace(asChildOf) : null;
			if (asChildOf == null || asChildOfInRefAsm != null)
			{
				result = ra.FindNamespace(nsPathNode, false, asChildOf);

				if (result != null)
				{
					//if (i > 0)
					//{
					//	array[i] = array[i - 1];
					//	array[i - 1] = ra;
					//}
				
					if (lastLeaf != null && !lastLeaf.IsLit(".") && !lastLeaf.IsLit("::"))
						lastLeaf.resolvedSymbol = result;
				
					return result;
				}
			}
		}

		return null;
	}

	public SymbolDefinition FindNamespace(string namespaceName)
	{
		SymbolDefinition result = GlobalNamespace;
		if (string.IsNullOrEmpty(namespaceName))
			return result;
		var start = 0;
		while (start < namespaceName.Length)
		{
			var dotPos = namespaceName.IndexOf('.', start);
			
			var length = dotPos == -1 ? namespaceName.Length - start : dotPos - start;
			result = result.FindNamespace(namespaceName, start, length);
			if (result == null)
				return unknownSymbol;
			start += length + 1;
		}
		return result ?? unknownSymbol;
	}
	
	public NamespaceDefinition FindSameNamespace(NamespaceDefinition namespaceDefinition)
	{
		if (namespaceDefinition.name.IsEmpty)
			return GlobalNamespace;
		var parentNamespace = (namespaceDefinition.parentSymbol ?? namespaceDefinition.savedParentSymbol) as NamespaceDefinition;
		parentNamespace = FindSameNamespace(parentNamespace) as NamespaceDefinition;
		if (parentNamespace == null)
			return null;
		return parentNamespace.FindName(namespaceDefinition.name, 0, false) as NamespaceDefinition;
	}
	
	private bool areReferencedAssembliesReflected = false;

	public void ResolveInReferencedAssemblies(ParseTree.Leaf leaf, NamespaceDefinition namespaceDefinition, int numTypeArgs, bool asTypeOnly)
	{
		NamespaceDefinition nsDef;
		
		var leafText = DecodeId(leaf.token.text);
		var hashCode = leafText.GetHashCode();

		var array = referencedAssemblies;
		var length = array.Length;
		
		if (!areReferencedAssembliesReflected)
		{
			for (var i = 0; i < length; ++i)
			{
				var ra = array[i];
				nsDef = ra.GlobalNamespace;
			}
			areReferencedAssembliesReflected = true;
		}
		
		var fullName = namespaceDefinition.FullName;
		var namespaceName = fullName != "" ? NamespaceName.Get(fullName) : null;
		var definitions = namespaceName != null ? namespaceName.allNamespaces : null;
		
		if (definitions != null)
		{
			//if (definitions.Count <= 1)
			//	return;
			
			var numDefs = definitions.Count;
			for (var i = 0; i < numDefs; ++i)
			{
				nsDef = definitions[i];
				var ra = nsDef.Assembly;
				if (ra == this)
					continue;
				
				if (!ra.isScriptAssembly)
				{
					if (!ra.typesFilter.Contains(hashCode) && !ra.namespacesFilter.Contains(hashCode))
						continue;
				}
				
				if (!referencedAssemblyIDs.Contains(ra.hashID))
					continue;
				
				var result = nsDef.FindName(leafText, numTypeArgs, asTypeOnly);
				if (result != null)
				{
					if (!result.IsPublic)
					{
						if (!result.IsInternal)
						{
							continue;
						}

						var internalsVisible = ra.InternalsVisibleTo(this);
						if (!internalsVisible)
						{
							continue;
						}
					}
					
					leaf.resolvedSymbol = result;
					
					if (i > 0)
					{
						definitions[i] = definitions[i - 1];
						definitions[i - 1] = nsDef;
					}
					return;
				}
			}
			return;
		}

		//length = ReferencedAssemblyNames.Length;
		
		for (var i = 0; i < length; ++i)
		{
			var ra = array[i];
			nsDef = ra.GlobalNamespace;
			
			if (!ra.isScriptAssembly)
			{
				if (!ra.typesFilter.Contains(hashCode) && !ra.namespacesFilter.Contains(hashCode))
					continue;
			}

			var result = nsDef.FindName(leafText, numTypeArgs, asTypeOnly);
			if (result != null)
			{
				if (!result.IsPublic)
				{
					if (!result.IsInternal)
					{
						//Debug.Log(result);
						continue;
					}

					var internalsVisible = ra.InternalsVisibleTo(this);
					if (!internalsVisible)
					{
						//Debug.Log(result);
						continue;
					}
				}
				
				leaf.resolvedSymbol = result;
				
				//if (i > 0)
				//{
				//	array[i] = array[i - 1];
				//	array[i - 1] = ra;
				//}
				return;
			}
		}
	}

	public void ResolveAttributeInReferencedAssemblies(ParseTree.Leaf leaf, NamespaceDefinition namespaceDefinition)
	{
		var leafText = DecodeId(leaf.token.text);
		
		foreach (var ra in referencedAssemblies)
		{
			var nsDef = ra.FindSameNamespace(namespaceDefinition);
			if (nsDef != null)
			{
				leaf.resolvedSymbol = nsDef.FindName(leafText + "Attribute", 0, true);
				if (leaf.resolvedSymbol != null)
					return;

				leaf.resolvedSymbol = nsDef.FindName(leafText, 0, true);
				if (leaf.resolvedSymbol != null)
					return;
			}
		}
	}

	private static bool dontReEnter = false;

	public void GetMembersCompletionDataFromReferencedAssemblies(Dictionary<string, SymbolDefinition> data, NamespaceDefinition namespaceDefinition, ResolveContext context)
	{
		if (dontReEnter)
			return;

		foreach (var ra in referencedAssemblies)
		{
			var nsDef = ra.FindSameNamespace(namespaceDefinition);
			if (nsDef != null)
			{
				dontReEnter = true;
				var accessLevelMask = ra.InternalsVisibleTo(this) ? AccessLevelMask.Public | AccessLevelMask.Internal : AccessLevelMask.Public;
				nsDef.GetMembersCompletionData(data, 0, accessLevelMask, context);
				dontReEnter = false;
			}
		}
	}

	public void GetTypesOnlyCompletionDataFromReferencedAssemblies(Dictionary<string, SymbolDefinition> data, NamespaceDefinition namespaceDefinition)
	{
		if (dontReEnter)
			return;

		foreach (var ra in referencedAssemblies)
		{
			var nsDef = ra.FindSameNamespace(namespaceDefinition);
			if (nsDef != null)
			{
				dontReEnter = true;
				var accessLevelMask = ra.InternalsVisibleTo(this) ? AccessLevelMask.Public | AccessLevelMask.Internal : AccessLevelMask.Public;
				nsDef.GetTypesOnlyCompletionData(data, accessLevelMask, this);
				dontReEnter = false;
			}
		}
	}
	
	public void CollectExtensionMethods(
		NamespaceDefinition namespaceDefinition,
		CharSpan id,
		TypeReference[] typeArgs,
		TypeDefinitionBase extendedType,
		HashSet<MethodDefinition> extensionsMethods,
		Scope context)
	{
		namespaceDefinition.CollectExtensionMethods(id, typeArgs, extendedType, extensionsMethods, context);
		
		foreach (var ra in referencedAssemblies)
		{
			var nsDef = ra.FindSameNamespace(namespaceDefinition);
			if (nsDef != null)
				nsDef.CollectExtensionMethods(id, typeArgs, extendedType, extensionsMethods, context);
		}
	}
	
	public void GetExtensionMethodsCompletionData(TypeDefinitionBase targetType, NamespaceDefinition namespaceDefinition, Dictionary<string, SymbolDefinition> data, TypeDefinitionBase contextType)
	{
		namespaceDefinition.GetExtensionMethodsCompletionData(targetType, data, AccessLevelMask.Public | AccessLevelMask.Internal, contextType);

		foreach (var ra in referencedAssemblies)
		{
			var nsDef = ra.FindSameNamespace(namespaceDefinition);
			if (nsDef != null)
				nsDef.GetExtensionMethodsCompletionData(targetType, data, AccessLevelMask.Public | (ra.InternalsVisibleTo(this) ? AccessLevelMask.Internal : 0), contextType);
		}
	}
	
	public IEnumerable<TypeDefinitionBase> EnumAssignableTypesFor(TypeDefinitionBase type)
	{
		yield return type;
		//foreach (var derived in Assembly.EnumDerivedTypes(this))
		//	yield return derived;
	}
	
	public IEnumerable<TypeDefinitionBase> EnumTypes(string name)
	{
		foreach (var type in GlobalNamespace.EnumTypes(name))
			yield return type;
		for (var i = referencedAssemblies.Length; i --> 0; )
			foreach (var type in referencedAssemblies[i].GlobalNamespace.EnumTypes(name))
				yield return type;
	}
	
	public IEnumerable<SymbolDefinition> EnumTypesAndNamespaces(string name)
	{
		foreach (var symbol in GlobalNamespace.EnumTypesAndNamespaces(name))
			yield return symbol;
		for (var i = referencedAssemblies.Length; i --> 0; )
			foreach (var symbol in referencedAssemblies[i].GlobalNamespace.EnumTypesAndNamespaces(name))
				yield return symbol;
	}
}

public struct CodeIssue
{
	public enum Kind
	{
		None,
		UnknownSymbol,
		UnknownMember,
	}
	
	public Kind kind;
	
	public CodeIssue(Kind issueKind)
	{
		kind = issueKind;
	}
}

public interface IIssueProvider
{
	CodeIssue Check(FGTextBuffer textBuffer, SyntaxToken token);
}

[UnityEditor.InitializeOnLoad]
public class UnknownSymbolIssueProvider : IIssueProvider
{
	static UnknownSymbolIssueProvider()
	{
		FGResolver.RegisterIssueProvider(new UnknownSymbolIssueProvider());
	}
	
	public CodeIssue Check(FGTextBuffer textBuffer, SyntaxToken token)
	{
		if (token.parent != null &&
			token.parent.resolvedSymbol != null &&
			token.parent.semanticError == "unknown symbol")
		{
			return new CodeIssue(CodeIssue.Kind.UnknownSymbol);
		}
		
		return new CodeIssue(CodeIssue.Kind.None);
	}
}

public interface ICodeFixProvider
{
	bool CanFix(CodeIssue issue, FGTextBuffer textBuffer, SyntaxToken token);
	IEnumerable<ICodeFix> EnumFixes(CodeIssue issue, FGTextBuffer textBuffer, SyntaxToken token);
}

public interface ICodeFix
{
	string GetTitle(SyntaxToken token);
	void Apply(FGTextEditor editor, SyntaxToken token);
}
	
[UnityEditor.InitializeOnLoad]
public class ResolveAsTypeFixProvider : ICodeFixProvider
{
	static ResolveAsTypeFixProvider()
	{
		FGResolver.RegisterCodeFixProvider(new ResolveAsTypeFixProvider());
	}
	
#region ICodeFixProvider
	
	public bool CanFix(CodeIssue issue, FGTextBuffer textBuffer, SyntaxToken token)
	{
		return issue.kind == CodeIssue.Kind.UnknownSymbol;
	}
	
	public IEnumerable<ICodeFix> EnumFixes(CodeIssue issue, FGTextBuffer textBuffer, SyntaxToken token)
	{
		if (token.parent.parent != null &&
			(	token.parent.parent.RuleName == "primaryExpressionStart" ||
				token.parent.parent.RuleName == "typeOrGeneric"))
		{
			int lineIndex = token.Line;
			int tokenIndex = token.TokenIndex;
			var prevToken = textBuffer.GetTokenLeftOf(ref lineIndex, ref tokenIndex);
			if (prevToken != null && prevToken.tokenKind == SyntaxToken.Kind.Missing)
				yield break;

			var tokenScopeNode = token.parent.parent;
			while (tokenScopeNode != null && tokenScopeNode.scope == null)
				tokenScopeNode = tokenScopeNode.parent;
			if (tokenScopeNode == null)
				yield break;
			
			var enclosingNamespaceScopeNode = tokenScopeNode;
			while (enclosingNamespaceScopeNode != null && !(enclosingNamespaceScopeNode.scope is NamespaceScope))
				enclosingNamespaceScopeNode = enclosingNamespaceScopeNode.parent;
			if (enclosingNamespaceScopeNode == null)
				yield break;
			
			var namespaceScope = enclosingNamespaceScopeNode.scope as NamespaceScope;
			var allTypesAndNamespaces = namespaceScope.GetAssembly().EnumTypesAndNamespaces(token.text);
			foreach (var typeOrNamespace in allTypesAndNamespaces)
			{
				yield return new AddUsingDirectiveFix(typeOrNamespace);
				yield return new AddNamespaceQualifierFix(typeOrNamespace);
			}
			
			if (token.text.length > "Attribute".Length &&
				!token.text.FastEndsWith("Attribute") &&
				token.parent.parent.RuleName == "typeOrGeneric" &&
				token.parent.parent.parent != null &&
				token.parent.parent.parent.parent != null &&
				token.parent.parent.parent.parent.parent != null &&
				token.parent.parent.parent.parent.parent.RuleName == "attribute" /*&&
				token.parent.parent.childIndex == token.parent.parent.parent.numValidNodes - 1*/)
			{
				var allTypes = namespaceScope.GetAssembly().EnumTypes(token.text + "Attribute");
				foreach (var type in allTypes)
				{
					yield return new AddUsingDirectiveFix(type);
					yield return new AddNamespaceQualifierFix(type);
				}
			}
		}
#if SI3_WARNINGS
		else if (token.parent.parent != null)
		{
			Debug.LogWarning(token.parent.parent.RuleName);
		}
#endif
	}
	
#endregion ICodeFixProvider
}

public class UnknownSymbolFix
{
	protected SymbolDefinition symbol;
		
	protected ParseTree.Node EnclosingNamespaceScopeNode(SyntaxToken token)
	{
		if (token.parent == null)
			return null;
		
		var enclosingNamespaceScopeNode = token.parent.parent;
		while (enclosingNamespaceScopeNode != null && !(enclosingNamespaceScopeNode.scope is NamespaceScope))
			enclosingNamespaceScopeNode = enclosingNamespaceScopeNode.parent;
		
		return enclosingNamespaceScopeNode;
	}
}

public class AddUsingDirectiveFix : UnknownSymbolFix, ICodeFix
{
	public AddUsingDirectiveFix(SymbolDefinition symbol)
	{
		this.symbol = symbol;
	}
	
	public string GetTitle(SyntaxToken token)
	{
		return "using " + symbol.parentSymbol.FullName + ";";
	}
	
	public void Apply(FGTextEditor editor, SyntaxToken token)
	{
		var enclosingNamespaceScopeNode = EnclosingNamespaceScopeNode(token);
		if (enclosingNamespaceScopeNode == null)
			return;
		
		var firstMemberNode = enclosingNamespaceScopeNode.FindChildByName("namespaceMemberDeclaration");
		if (firstMemberNode == null)
			return;
		
		var textBuffer = editor.TextBuffer;
		TextPosition insertPos;
		
		var previousLeaf = firstMemberNode.FindPreviousLeaf();
		if (previousLeaf != null)
		{
			insertPos = editor.TextBuffer.GetTokenSpan(previousLeaf).EndPosition;
		}
		else
		{
			insertPos = new TextPosition(firstMemberNode.GetFirstLeaf().line, 0);
			while (insertPos.line > 0)
			{
				SyntaxToken nonWSToken, nonTriviaToken;
				textBuffer.GetFirstTokens(insertPos.line - 1, out nonWSToken, out nonTriviaToken);
				if (nonTriviaToken != null)
					break;
				if (nonWSToken != null && nonWSToken.text != "//")
					break;
				if (nonWSToken != null)
				{
					var tokens = textBuffer.formattedLines[insertPos.line - 1].tokens;
					if (tokens.Count <= nonWSToken.TokenIndex + 1 ||
						!tokens[nonWSToken.TokenIndex + 1].text.FastStartsWith("/"))
					{
						break;
					}
				}
				insertPos.line--;
			}
		}
		
		editor.SetCursorPosition(insertPos.line, insertPos.index);
		if (previousLeaf != null)
			editor.TextBuffer.InsertText(editor.caretPosition, "\nusing " + symbol.parentSymbol.FullName + ";");
		else
			editor.TextBuffer.InsertText(editor.caretPosition, "using " + symbol.parentSymbol.FullName + ";\n");
		editor.TextBuffer.UpdateHighlighting(insertPos.line, insertPos.line + 1);
		editor.ReindentLines(insertPos.line, insertPos.line + 1);
	}
}

public class AddNamespaceQualifierFix : UnknownSymbolFix, ICodeFix
{
	public AddNamespaceQualifierFix(SymbolDefinition symbol)
	{
		this.symbol = symbol;
	}
	
	public string GetTitle(SyntaxToken token)
	{
		return symbol.parentSymbol.FullName + "." + token.text;
	}
	
	public void Apply(FGTextEditor editor, SyntaxToken token)
	{
		var textSpan = editor.TextBuffer.GetTokenSpan(token.parent);
		editor.SetCursorPosition(textSpan.line, textSpan.index);
		editor.TextBuffer.InsertText(editor.caretPosition, symbol.parentSymbol.FullName + ".");
		editor.TextBuffer.UpdateHighlighting(textSpan.line, textSpan.line);
	}
}

public static class FGResolver
{
	private static List<IIssueProvider> issueProviders = new List<IIssueProvider>();
	private static List<ICodeFixProvider> codeFixesProviders = new List<ICodeFixProvider>();
	
	public static void RegisterIssueProvider(IIssueProvider provider)
	{
		issueProviders.Add(provider);
	}
	
	public static void RegisterCodeFixProvider(ICodeFixProvider fixProvider)
	{
		codeFixesProviders.Add(fixProvider);
	}
	
	public static List<ICodeFix> GetFixes(FGTextBuffer textBuffer, SyntaxToken token)
	{
		var fixes = new List<ICodeFix>();
		foreach (var issueProvider in issueProviders)
		{
			var issue = issueProvider.Check(textBuffer, token);
			if (issue.kind == CodeIssue.Kind.None)
				continue;
			
			foreach (var fixProvider in codeFixesProviders)
				if (fixProvider.CanFix(issue, textBuffer, token))
					fixes.AddRange(fixProvider.EnumFixes(issue, textBuffer, token));
		}
		return fixes;
	}
	
	public static void GetCompletions(IdentifierCompletionsType completionTypes, ParseTree.BaseNode parseTreeNode, HashSet<SymbolDefinition> completionSymbols, string assetPath)
	{
#if false
		var stopwatch = new Stopwatch();
		stopwatch.Start();
		GetCompletions_Profiled(completionTypes, parseTreeNode, completionSymbols, assetPath);
		stopwatch.Stop();
		Debug.Log("GetCompletions: " + stopwatch.ElapsedMilliseconds + "ms");
	}
	
	public static void GetCompletions_Profiled(IdentifierCompletionsType completionTypes, ParseTree.BaseNode parseTreeNode, HashSet<SymbolDefinition> completionSymbols, string assetPath)
	{
#endif
		try
		{
			var d = new Dictionary<string, SymbolDefinition>();
			var assemblyDefinition = AssemblyDefinition.FromAssetPath(assetPath);
			
			if ((completionTypes & IdentifierCompletionsType.MemberName) != 0)
			{
				ParseTree.BaseNode targetNode = null;

				var node = parseTreeNode.parent; // memberInitializerList or objectInitializer or objectCreationExpression or targetTypedNewExpression
				if (node.RuleName != "objectOrCollectionInitializer")
				{
					if (node.RuleName != "objectInitializer")
					{
						if (node.RuleName == "memberInitializerList")
							node = node.parent; // objectInitializer
					}
					node = node.parent; // objectOrCollectionInitializer
				}
				node = node.parent;
				if (node.RuleName == "objectCreationExpression")
				{
					targetNode = node.parent;
				}
				else if (node.RuleName == "memberInitializer")
				{
					targetNode = node.LeafAt(0);
				}
				else // node is targetTypedNewExpression
				{
					return;
				}
				
				var targetDef = targetNode != null ? SymbolDefinition.ResolveNode(targetNode) : null;
				if (targetDef != null)
				{
					GetMemberCompletions(targetDef, parseTreeNode, assemblyDefinition, d, false);

					var filteredData = new Dictionary<string, SymbolDefinition>();
					foreach (var kv in d)
					{
						var symbol = kv.Value;
						if (symbol.kind == SymbolKind.Field && (symbol.modifiers & Modifiers.ReadOnly) == 0 ||
							symbol.kind == SymbolKind.Property && symbol.FindName("set", 0, false) != null)
						{
							filteredData[kv.Key] = symbol;
						}
					}
					d = filteredData;
				}
				
				var targetType = targetDef != null ? targetDef.TypeOf() as TypeDefinitionBase : null;
				if (targetType == null || !targetType.DerivesFrom(SymbolDefinition.builtInTypes_IEnumerable))
				{
					completionSymbols.Clear();
					//completionSymbols.EnsureCapacity(d.Count + 100);
					completionSymbols.UnionWith(d.Values);
					return;
				}
			}

			if ((completionTypes & IdentifierCompletionsType.Member) != 0)
			{
				var target = parseTreeNode.FindPreviousNode();

				var prevNode = parseTreeNode.FindPreviousNode();
				if (target != null && target.IsLit("?"))
					target = target.FindPreviousNode();

				if (target != null)
				{
					var targetAsNode = target as ParseTree.Node;
					if (targetAsNode != null && targetAsNode.RuleName == "primaryExpressionPart")
					{
						var node0 = targetAsNode.NodeAt(-1);
						if (node0 != null && node0.RuleName == "arguments")
						{
							target = target.FindPreviousNode();
							targetAsNode = target as ParseTree.Node;
						}
					}
					//Debug.Log(targetAsNode ?? target.parent);
					ResolveNode(targetAsNode ?? target.parent);
					var targetDef = GetResolvedSymbol(targetAsNode ?? target.parent);

					GetMemberCompletions(targetDef, parseTreeNode, assemblyDefinition, d, true);
				}
			}
			else if (parseTreeNode == null)
			{
#if SI3_WARNINGS
				Debug.LogWarning(completionTypes);
#endif
			}
			else
			{
				Scope.completionNode = parseTreeNode;
				Scope.completionAssetPath = assetPath;

				if (parseTreeNode.IsLit("=>"))
				{
					parseTreeNode = parseTreeNode.parent.NodeAt(parseTreeNode.childIndex + 1) ?? parseTreeNode;
				}
				if (parseTreeNode.IsLit("]") && parseTreeNode.parent.RuleName == "attributes")
				{
					parseTreeNode = parseTreeNode.parent.parent.NodeAt(parseTreeNode.parent.childIndex + 1);
				}

				var enclosingScopeNode = parseTreeNode as ParseTree.Node ?? parseTreeNode.parent;
				if (enclosingScopeNode != null && (enclosingScopeNode.scope is SymbolDeclarationScope) &&
					(parseTreeNode.IsLit(";") || parseTreeNode.IsLit("}")) &&
					enclosingScopeNode.GetLastLeaf() == parseTreeNode)
				{
					enclosingScopeNode = enclosingScopeNode.parent;
				}
				while (enclosingScopeNode != null && enclosingScopeNode.scope == null)
					enclosingScopeNode = enclosingScopeNode.parent;
				if (enclosingScopeNode != null)
				{
					var lastLeaf = parseTreeNode as ParseTree.Leaf ??
						((ParseTree.Node) parseTreeNode).GetLastLeaf() ??
						((ParseTree.Node) parseTreeNode).FindPreviousLeaf();
					Scope.completionAtLine = lastLeaf != null ? lastLeaf.line : 0;
					Scope.completionAtTokenIndex = lastLeaf != null ? lastLeaf.tokenIndex : 0;
					
					ResolveContext context = new ResolveContext();
					context.scope = enclosingScopeNode.scope;
					context.completionNode = parseTreeNode;
					context.completionAssetPath = assetPath;
					context.completionAtLine = Scope.completionAtLine;
					context.completionAtTokenIndex = Scope.completionAtTokenIndex;
					context.assembly = assemblyDefinition;
					context.type = enclosingScopeNode.scope.EnclosingType();
					context.fromInstance = true;
					
					enclosingScopeNode.scope.GetCompletionData(d, context);
				}
			}
			
			//if ((completionTypes & ~IdentifierCompletionsType.Member) == IdentifierCompletionsType.TypeName)
			//{
			//	var allDefinitions = d;
			//	d = new Dictionary<string, SymbolDefinition>();
			//	foreach (var kv in allDefinitions)
			//	{
			//		var kind = kv.Value.kind;
			//		if (kv.Value is TypeDefinitionBase || kind == SymbolKind.Namespace)
			//		{
			//			d[kv.Key] = kv.Value;
			//		}
			//	}
			//}
	
			//completionSymbols.EnsureCapacity(d.Count + 100);
			completionSymbols.UnionWith(d.Values);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	public static SymbolDefinition GetResolvedSymbol(ParseTree.BaseNode baseNode)
	{
#if false
		var stopwatch = new Stopwatch();
		stopwatch.Start();
		var result = GetResolvedSymbol_Internal(baseNode);
		stopwatch.Stop();
		Debug.Log("GetResolvedSymbol: " + stopwatch.ElapsedMilliseconds + "ms");
		return result;
	}
	
	public static SymbolDefinition GetResolvedSymbol_Internal(ParseTree.BaseNode baseNode)
	{
#endif
	avoidRecursion:
	
		var leaf = baseNode as ParseTree.Leaf;
		if (leaf != null)
		{
			if ((leaf.resolvedSymbol == null || leaf.resolvedSymbol.kind == SymbolKind.Error) && leaf.parent != null)
				ResolveNodeInternal(leaf.parent);
			return leaf.resolvedSymbol;
		}

		var node = baseNode as ParseTree.Node;
		if (node == null || node.numValidNodes == 0)
			return null;

		switch (node.RuleName)
		{
			case "primaryExpressionStart":
				if (node.numValidNodes < 3)
				{
					baseNode = node.ChildAt(0);
					goto avoidRecursion;
				}
				leaf = node.LeafAt(2);
				return leaf != null ? leaf.resolvedSymbol : null;
			case "primaryExpressionPart":
				baseNode = node.NodeAt(-1);
				goto avoidRecursion;
			case "arguments":
			case "attributeArguments":
				baseNode = node.FindPreviousNode() as ParseTree.Node;
				if (baseNode is ParseTree.Leaf)
					baseNode = node.parent as ParseTree.Node; // targetTypedNewExpression
				goto avoidRecursion;
			case "targetTypedNewExpression":
				leaf = node.parent.LeafAt(0);
				var newType = leaf.resolvedSymbol;
				if (newType == null || newType.kind == SymbolKind.Error)
					newType = SymbolDefinition.builtInTypes_object;
				var typeOfNewType = (TypeDefinitionBase) newType.TypeOf();
				return typeOfNewType.GetThisInstance();
			case "objectCreationExpression":
				newType = GetResolvedSymbol(node.FindPreviousNode() as ParseTree.Node);
				if (newType == null || newType.kind == SymbolKind.Error)
					newType = SymbolDefinition.builtInTypes_object;
				typeOfNewType = (TypeDefinitionBase) newType.TypeOf();
				return typeOfNewType.GetThisInstance();
			case "arrayCreationExpression":
				var elementType = GetResolvedSymbol(node.FindPreviousNode() as ParseTree.Node);
				var arrayInstance = SymbolDefinition.ResolveNode(node, null, elementType);
				return arrayInstance ?? SymbolDefinition.builtInTypes_Array.GetThisInstance();
			case "nonArrayType":
				var typeNameTypeOrConstructor = GetResolvedSymbol(node.NodeAt(0));
				if (typeNameTypeOrConstructor != null && typeNameTypeOrConstructor.kind == SymbolKind.Constructor)
					return typeNameTypeOrConstructor;
				var typeNameType = typeNameTypeOrConstructor as TypeDefinitionBase;
				if (typeNameType == null || typeNameType.kind == SymbolKind.Error)
					typeNameType = SymbolDefinition.builtInTypes_object;
				return node.numValidNodes == 1 ? typeNameType : typeNameType.MakeNullableType();
			case "typeName":
				baseNode = node.NodeAt(0);
				goto avoidRecursion;
			case "namespaceOrTypeName":
				baseNode = node.NodeAt(node.numValidNodes & ~1);
				goto avoidRecursion;
			case "accessIdentifier":
				leaf = node.numValidNodes < 2 ? null : node.LeafAt(1);
				if (leaf != null && leaf.resolvedSymbol == null)
					FGResolver.ResolveNodeInternal(node);
				return leaf != null ? leaf.resolvedSymbol : null;
			case "predefinedType":
			case "typeOrGeneric":
				return node.LeafAt(0).resolvedSymbol;
			case "typeofExpression":
				return ((TypeDefinitionBase) TypeReference.To(typeof(Type)).definition).GetThisInstance();
			case "sizeofExpression":
				return SymbolDefinition.builtInTypes_int.GetThisInstance();
			case "localVariableType":
			case "brackets":
			case "expression":
			case "unaryExpression":
			case "parenOrTupleExpression":
			case "checkedExpression":
			case "uncheckedExpression":
			case "defaultValueExpression":
			case "relationalExpression":
			case "inclusiveOrExpression":
			case "exclusiveOrExpression":
			case "andExpression":
			case "equalityExpression":
			case "shiftExpression":
			case "flatExpression":
			case "primaryExpression":
			case "type":
			case "globalNamespace":
			case "nameofExpression":
			case "nullForgivingOperator":
				return SymbolDefinition.ResolveNode(node, null, null, 0);
			default:
#if SI3_WARNINGS
				Debug.LogWarning(node.RuleName + "\n" + node.Print());
#endif
				return SymbolDefinition.ResolveNode(node, null, null, 0);
		}
	}

	private static void GetMemberCompletions(
		SymbolDefinition targetDef,
		ParseTree.BaseNode parseTreeNode,
		AssemblyDefinition assemblyDefinition,
		Dictionary<string, SymbolDefinition> d,
		bool includeExtensionMethods)
	{
		if (targetDef == null)
			return;

		//Debug.Log(targetDef.GetTooltipText());
		var typeOf = targetDef.TypeOf();
		if (typeOf == null)
			return;
		//UnityEngine.Debug.Log(typeOf);

		var flags = BindingFlags.Instance | BindingFlags.Static;
		switch (targetDef.kind)
		{
			case SymbolKind.None:
			case SymbolKind.Error:
				break;
			case SymbolKind.Namespace:
			case SymbolKind.Interface:
			case SymbolKind.Struct:
			case SymbolKind.Class:
			case SymbolKind.TypeParameter:
			case SymbolKind.Delegate:
			case SymbolKind.Enum:
			case SymbolKind.BaseTypesList:
			case SymbolKind.TypeParameterConstraintList:
				flags = BindingFlags.Static;
				break;
			case SymbolKind.Field:
			case SymbolKind.ConstantField:
			case SymbolKind.LocalConstant:
			case SymbolKind.Property:
			case SymbolKind.Event:
			case SymbolKind.Indexer:
			case SymbolKind.Method:
			case SymbolKind.MethodGroup:
			case SymbolKind.Constructor:
			case SymbolKind.Destructor:
			case SymbolKind.Operator:
			case SymbolKind.Accessor:
			case SymbolKind.Parameter:
			case SymbolKind.CatchParameter:
			case SymbolKind.Variable:
			case SymbolKind.TupleDeconstructVariable:
			case SymbolKind.OutVariable:
			case SymbolKind.IsVariable:
			case SymbolKind.CaseVariable:
			case SymbolKind.ForEachVariable:
			case SymbolKind.FromClauseVariable:
			case SymbolKind.EnumMember:
			case SymbolKind.Instance:
			case SymbolKind.LambdaExpression:
				flags = BindingFlags.Instance;
				break;
			case SymbolKind.Null:
			case SymbolKind.Label:
			case SymbolKind.TupleType:
				return;
			case SymbolKind.ImportedNamespace:
			case SymbolKind.UsingAlias:
			case SymbolKind.ImportedStaticType:
			default:
				throw new ArgumentOutOfRangeException();
		}
		//targetDef.kind = targetDef is TypeDefinitionBase && targetDef.kind != SymbolKind.Enum ? BindingFlags.Static : targetDef is InstanceDefinition ? BindingFlags.Instance : 0;

		TypeDefinitionBase contextType = null;
		for (var n = parseTreeNode as ParseTree.Node ?? parseTreeNode.parent; n != null; n = n.parent)
		{
			var s = n.scope as SymbolDeclarationScope;
			if (s != null)
			{
				contextType = s.declaration.definition as TypeDefinitionBase;
				if (contextType != null)
					break;
			}
		}

		AccessLevelMask mask =
			typeOf == contextType || typeOf.IsSameOrParentOf(contextType) ? AccessLevelMask.Any :
			contextType != null && contextType.DerivesFrom(typeOf as TypeDefinitionBase) ? AccessLevelMask.Protected | AccessLevelMask.Internal | AccessLevelMask.Public :
			AccessLevelMask.Internal | AccessLevelMask.Public;

		var typeOfAssembly = typeOf.Assembly;
		if (typeOfAssembly == null || !typeOfAssembly.InternalsVisibleTo(assemblyDefinition))
			mask &= ~AccessLevelMask.Internal;

		//					var enclosingScopeNode = parseTreeNode as ParseTree.Node ?? parseTreeNode.parent;
		//					while (enclosingScopeNode != null && enclosingScopeNode.scope == null)
		//						enclosingScopeNode = enclosingScopeNode.parent;
		//					var enclosingScope = enclosingScopeNode != null ? enclosingScopeNode.scope : null;

		//UnityEngine.Debug.Log(flags + "\n" + mask);
		
		var resolveContext = new ResolveContext {
			assembly = assemblyDefinition,
		};
		
		typeOf.GetMembersCompletionData(d, flags, mask, resolveContext);

		if (includeExtensionMethods && flags == BindingFlags.Instance &&
			(typeOf.kind == SymbolKind.Class || typeOf.kind == SymbolKind.Struct || typeOf.kind == SymbolKind.Interface || typeOf.kind == SymbolKind.Enum || typeOf.kind == SymbolKind.TypeParameter))
		{
			var enclosingScopeNode = parseTreeNode as ParseTree.Node ?? parseTreeNode.parent;
			while (enclosingScopeNode != null && enclosingScopeNode.scope == null)
				enclosingScopeNode = enclosingScopeNode.parent;
			var enclosingScope = enclosingScopeNode != null ? enclosingScopeNode.scope : null;
			
			if (enclosingScope != null)
				enclosingScope.GetExtensionMethodsCompletionData(typeOf as TypeDefinitionBase, d, contextType);
		}
	}

	public static ParseTree.Node ResolveNode(ParseTree.Node node)
	{
		var initialCount = MethodGroupDefinition.argumentTypesStack.Count;
		var initialCandidatesCount = MethodGroupDefinition.methodCandidatesStack.Count;
		
#if SI3_WARNINGS
		if (MethodGroupDefinition.argumentTypesStack.Count > 50)
		{
			Debug.LogError(node);
			return null;
		}
#endif
		
		var result = ResolveNodeInternal(node);
		
		if (MethodGroupDefinition.argumentTypesStack.Count != initialCount)
			Debug.LogError("argumentTypesStack.Count == " + MethodGroupDefinition.argumentTypesStack.Count);
		if (MethodGroupDefinition.resolvedArgumentsStack.Count != initialCount)
			Debug.LogError("resolvedArgumentsStack.Count == " + MethodGroupDefinition.resolvedArgumentsStack.Count);
		if (MethodGroupDefinition.modifiersStack.Count != initialCount)
			Debug.LogError("modifiersStack.Count == " + MethodGroupDefinition.modifiersStack.Count);
		if (MethodGroupDefinition.namedArgumentsStack.Count != initialCount)
			Debug.LogError("namedArguments.Count == " + MethodGroupDefinition.namedArgumentsStack.Count);
		if (MethodGroupDefinition.argumentNodesStack.Count != initialCount)
			Debug.LogError("argumentNodesStack.Count == " + MethodGroupDefinition.argumentNodesStack.Count);
		
		if (MethodGroupDefinition.methodCandidatesStack.Count != initialCandidatesCount)
			Debug.LogError("methodCandidatesStack.Count == " + MethodGroupDefinition.methodCandidatesStack.Count);
		
		return result;
	}
	
	public static ParseTree.Node ResolveNodeInternal(ParseTree.Node node)
	{
		if (node == null)
			return null;
		
	//	UnityEngine.Debug.Log(node.RuleName);
		while (node.parent != null)
		{
			switch (node.RuleName)
			{
				//case "primaryExpression":
				case "primaryExpressionStart":
				case "primaryExpressionPart":
				case "objectCreationExpression":
				case "objectOrCollectionInitializer":
				case "typeOrGeneric":
				case "namespaceOrTypeName":
				case "typeName":
				case "nonArrayType":
				//case "attribute":
				case "accessIdentifier":
				case "brackets":
				case "argumentList":
				case "attributeArgumentList":
				case "argumentName":
				case "attributeMemberName":
				case "argument":
				case "attributeArgument":
				case "attributeArguments":
				// case "VAR":
//				case "localVariableType":
				case "localVariableDeclaration":
				case "caseVariableDeclarator":
				case "implicitDeconstructVariableDeclarator":
				case "explicitDeconstructVariableDeclarator":
				case "outVariableDeclarator":
				case "isVariableDeclarator":
				case "arrayCreationExpression":
				case "implicitArrayCreationExpression":
				case "arrayInitializer":
				case "arrayInitializerList":
//				case "qid":
				case "qidStart":
				case "qidPart":
				case "memberInitializer":
//				case "memberName":
			//	case "unaryExpression":
			//	case "modifiers":
				case "globalNamespace":
				case "tupleElement":
				case "tupleExpressionElement":
					node = node.parent;
				//	UnityEngine.Debug.Log("--> " + node.RuleName);
					continue;
			}
			break;
		}
		
		if (node.parent == null && node.RuleName != "compilationUnit")
			return null;
		
		try
		{
			//var numTypeArgs = 0;
			//var parent = node.parent;
			//if (parent != null)
			//{
			//	var nextNode = node.NodeAt(node.childIndex + 1);
			//	if (nextNode != null)
			//	{
			//		if (nextNode.RuleName == "typeArgumentList")
			//			numTypeArgs = (nextNode.numValidNodes + 1) / 2;
			//		else if (nextNode.RuleName == "typeParameterList")
			//			numTypeArgs = (nextNode.numValidNodes + 2) / 3;
			//		else if (nextNode.RuleName == "unboundTypeRank")
			//			numTypeArgs = nextNode.numValidNodes - 1;
			//	}
			//}
			var result = SymbolDefinition.ResolveNode(node, null, null, 0);//numTypeArgs);
			if (result == null)
				ResolveChildren(node);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
			MethodGroupDefinition.argumentTypesStack.Clear();
			MethodGroupDefinition.resolvedArgumentsStack.Clear();
			MethodGroupDefinition.modifiersStack.Clear();
			MethodGroupDefinition.namedArgumentsStack.Clear();
			MethodGroupDefinition.argumentNodesStack.Clear();
			
			MethodGroupDefinition.methodCandidatesStack.Clear();
			
			return null;
		}
		
		return node;
	}

	static void ResolveChildren(ParseTree.Node node)
	{
		if (node == null)
			return;
		if (node.numValidNodes != 0)
		{
			for (var i = 0; i < node.numValidNodes; ++i)
			{
				var child = node.ChildAt(i);
				
				var leaf = child as ParseTree.Leaf;
				if (leaf == null ||
					leaf.token != null &&
					leaf.token.tokenKind != SyntaxToken.Kind.Punctuator &&
					(leaf.token.tokenKind != SyntaxToken.Kind.Keyword || SymbolDefinition.builtInTypes.ContainsKey(leaf.token.text)))
				{
					if (leaf == null && child != null)
					{
						switch (((ParseTree.Node) child).RuleName)
						{
							case "modifiers":
							case "methodBody":
								continue;
						}
					}
					var numTypeArgs = 0;
					//var nextNode = node.NodeAt(i + 1);
					//if (nextNode != null)
					//{
					//	if (nextNode.RuleName == "typeArgumentList")
					//		numTypeArgs = (nextNode.numValidNodes + 1) / 2;
					//	else if (nextNode.RuleName == "typeParameterList")
					//		numTypeArgs = (nextNode.numValidNodes + 2) / 3;
					//	else if (nextNode.RuleName == "unboundTypeRank")
					//		numTypeArgs = nextNode.numValidNodes - 1;
					//}
					if (SymbolDefinition.ResolveNode(child, null, null, numTypeArgs) == null)
					{
						var childAsNode = child as ParseTree.Node;
						if (childAsNode != null)
							ResolveChildren(childAsNode);
					}
				}
			}
		}
	}
	
	public static bool IsWriteReference(SyntaxToken token)
	{
		if (!(token.parent.resolvedSymbol is InstanceDefinition))
			return false;
		
		if (token.parent == null || token.parent.resolvedSymbol == null)
			return false;
		
		var parent = token.parent.parent;
		if (parent == null || parent.parent == null)
			return false;
		
		var parentRule = parent.RuleName;
		switch (token.parent.resolvedSymbol.kind)
		{
		case SymbolKind.Field:
		case SymbolKind.Property:
		case SymbolKind.Parameter:
		case SymbolKind.CaseVariable:
		case SymbolKind.ForEachVariable:
		case SymbolKind.FromClauseVariable:
		case SymbolKind.Variable:
		case SymbolKind.TupleDeconstructVariable:
		case SymbolKind.OutVariable:
		case SymbolKind.IsVariable:
		case SymbolKind.LocalConstant:
		case SymbolKind.ConstantField:
		case SymbolKind.Event:
		case SymbolKind.CatchParameter:
			if (parentRule == "localVariableDeclarator")
			{
				if (parent.numValidNodes == 1)
					break;
			}
			else if (parentRule == "variableDeclarator" || parentRule == "eventDeclarator")
			{
				// fields are always initialized
			}
			else if (parentRule == "foreachStatement")
			{
				// always initialized
			}
			else if (parentRule == "memberInitializer")
			{
				// always initialized
			}
			else if (parentRule == "fixedParameter" || parentRule == "parameterArray")
			{
				// parameters are always initialized
			}
			else if (parentRule == "constantDeclarator")
			{
				// always initialized
			}
			else if (parentRule == "interfaceEventDeclarator")
			{
				// always initialized
			}
			else if (parentRule == "catchExceptionIdentifier")
			{
				// always initialized
			}
			else if (parentRule == "caseVariableDeclarator")
			{
				// always initialized
			}
			else if (parentRule == "implicitDeconstructVariableDeclarator")
			{
				// always initialized
			}
			else if (parentRule == "explicitDeconstructVariableDeclarator")
			{
				// always initialized
			}
			else if (parentRule == "outVariableDeclarator")
			{
				// always initialized
			}
			else if (parentRule == "isVariableDeclarator")
			{
				// always initialized
			}
			else if (parentRule == "qidStart")
			{
				if (parent.childIndex < parent.parent.numValidNodes - 1)
					break;
				if (parent.numValidNodes == 3 && token.parent.childIndex != 2)
					break;
				// only the last token in a qid sequence is always initialized
			}
			else if (parentRule == "accessIdentifier" && parent.parent.RuleName == "qidPart")
			{
				if (parent.parent.childIndex < parent.parent.parent.numValidNodes - 1)
					break;
				// only the last token in a qid sequence is always initialized
			}
			else if (parentRule == "primaryExpressionStart" && parent.parent.numValidNodes == 1 ||
				parentRule == "accessIdentifier" && parent.parent.RuleName == "primaryExpressionPart" && parent.parent.childIndex == parent.parent.parent.numValidNodes - 1)
			{
				var primaryExpressionNode = parentRule == "accessIdentifier" ? parent.parent.parent : parent.parent;
				var incrementExpressionNode = primaryExpressionNode.parent.parent;
				parentRule = incrementExpressionNode.RuleName;
				if (parentRule != "preIncrementExpression" && parentRule != "preDecrementExpression")
				{
					var nextLeaf = primaryExpressionNode.parent.LeafAt(1);
					if (nextLeaf == null || !nextLeaf.IsLit("++") && !nextLeaf.IsLit("--"))
					{
						if (parentRule != "assignment" || primaryExpressionNode.parent.childIndex != 0)
						{
							while (incrementExpressionNode != null && incrementExpressionNode.RuleName != "expression")
								incrementExpressionNode = incrementExpressionNode.parent;
							if (incrementExpressionNode == null || incrementExpressionNode.parent.RuleName != "variableReference")
								break;
						}
					}
				}
			}
			else
			{
				var prevLeaf = token.parent.FindPreviousLeaf();
				if (prevLeaf == null || !prevLeaf.IsLit("ref") && !prevLeaf.IsLit("out"))
				{
					var nextLeaf = token.parent.FindNextLeaf();
					if (nextLeaf == null || nextLeaf.parent.RuleName != "assignmentOperator")
					{
						break;
					}
				}
			}
			
			return true;
		}
		
		return false;
	}
}

}
