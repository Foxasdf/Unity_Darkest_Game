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

#define TREE_WITH_PARENT_POINTERS

namespace ScriptInspector
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;

	[Serializable]
	public struct TextInterval
	{
		#region Properties
		public TextPosition Start;
		public TextPosition End;
		#endregion

		#region Constructor
		public TextInterval(TextPosition start, TextPosition end)
			: this()
		{
			if (start >= end)
			{
				#if SI3_WARNINGS
				throw new ArgumentException("the start value of the interval must be smaller than the end value. null interval are not allowed");
				#else
				end = start;
				end.index++;
				#endif
			}

			this.Start = start;
			this.End = end;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Determines if two intervals overlap (i.e. if this interval starts before the other ends and it finishes after the other starts)
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns>
		///   <c>true</c> if the specified other is overlapping; otherwise, <c>false</c>.
		/// </returns>
		public bool OverlapsWith(TextInterval other)
		{
			if (Start.line > other.End.line || Start.line == other.End.line && Start.index >= other.End.index)
				return false;
			return (End.line > other.Start.line || End.line == other.Start.line && End.index > other.Start.index);
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}]", this.Start.ToString(), this.End.ToString());
		}

		#endregion
	}

	/// <summary>
	/// Interval Tree class
	/// </summary>
	/// <typeparam name="TyoeValue"></typeparam>
	public class TextIntervalTree<TypeValue>
	{
		#region Fields

		public int Count;
		private IntervalNode Root;
		private IComparer<TextPosition> comparer;
		private KeyValueComparer<TextPosition, TypeValue> keyvalueComparer;

		#endregion

		#region Ctor

		/// <summary>
		/// Initializes a new instance of the <see cref="IntervalTree&lt;T, TypeValue&gt;"/> class.
		/// </summary>
		public TextIntervalTree()
			: this(null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IntervalTree&lt;T, TypeValue&gt;"/> class.
		/// </summary>
		/// <param name="elems">The elems.</param>
		public TextIntervalTree(IEnumerable<KeyValuePair<TextInterval, TypeValue>> elems)
		{
			if (elems != null)
			{
				foreach (var elem in elems)
				{
					Add(elem.Key, elem.Value);
				}
			}
			this.comparer = ComparerUtil.comparer;
			this.keyvalueComparer = new KeyValueComparer<TextPosition, TypeValue>(this.comparer);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Adds the specified interval.
		/// If there is more than one interval starting at the same time/value, the intervalnode.Interval stores the start time and the maximum end time of all intervals starting at the same value.
		/// All end values (except the maximum end time/value which is stored in the interval node itself) are stored in the Range list in decreasing order.
		/// Note: this is okay for problems where intervals starting at the same time /value is not a frequent occurrence, however you can use other data structure for better performance depending on your problem needs
		/// </summary>
		/// <param name="arg">The arg.</param>
		public void Add(TextPosition x, TextPosition y, TypeValue value)
		{
			Add(new TextInterval(x, y), value);
		}

		/// <summary>
		/// Adds the specified interval.
		/// If there is more than one interval starting at the same time/value, the intervalnode.Interval stores the start time and the maximum end time of all intervals starting at the same value.
		/// All end values (except the maximum end time/value which is stored in the interval node itself) are stored in the Range list in decreasing order.
		/// Note: this is okay for problems where intervals starting at the same time /value is not a frequent occurrence, however you can use other data structure for better performance depending on your problem needs
		/// </summary>
		/// <param name="arg">The arg.</param>
		public bool Add(TextInterval interval, TypeValue value)
		{
			bool wasAdded = false;
			bool wasSuccessful = false;

			this.Root = IntervalNode.Add(this.Root, interval, value, ref wasAdded, ref wasSuccessful);
			if (this.Root != null)
			{
				IntervalNode.ComputeMax(this.Root);
			}

			if (wasSuccessful)
			{
				this.Count++;
			}

			return wasSuccessful;
		}
		
#if TREE_WITH_PARENT_POINTERS
		//public void InOrderTraversal()
		//{
		//	IntervalNode current = Root;
		//	IntervalNode prev = null;
        
		//	while (current != null)
		//	{
		//		if (prev == current.Parent)
		//		{
		//			// Going down the tree
		//			if (current.Left != null)
		//			{
		//				prev = current;
		//				current = current.Left;
		//				continue;
		//			}
					
		//			UnityEngine.Debug.Log(current.Value);
					
		//			prev = current;
		//			current = (current.Right != null) ? current.Right : current.Parent;
		//		}
		//		else if (prev == current.Left)
		//		{
		//			// Coming up from left child
		//			UnityEngine.Debug.Log(current.Value);
					
		//			prev = current;
		//			current = (current.Right != null) ? current.Right : current.Parent;
		//		}
		//		else if (prev == current.Right)
		//		{
		//			// Coming up from right child
		//			prev = current;
		//			current = current.Parent;
		//		}
		//	}
		//}

		public void OnInsertedText(TextPosition from, TextPosition to)
		{
			IntervalNode current = Root;
			IntervalNode prev = null;
        
			while (current != null)
			{
				if (prev == current.Parent)
				{
					// Going down the tree.
					while (current.Left != null)
					{
						prev = current;
						current = current.Left;
					}
				}
				else if (prev == current.Right)
				{
					// Coming up from right child.
					prev = current;
					current = current.Parent;
					continue;
				}

				// Coming up from left child or down to leftmost child.
				
				// Process the current node.
				current.Interval.Start.OnInsertedText(from, to, true);
				current.Interval.End.OnInsertedText(from, to, false);
				current.Max.OnInsertedText(from, to, false);
				
				var range = current.Range;
				if (range != null)
				{
					for (int i = range.Count; i --> 0; )
					{
						#if SI3_WARNINGS
						UnityEngine.Debug.LogWarning(range[i].Key);
						#endif
						
						range[i].Key.OnInsertedText(from, to, false);
					}
				}

				prev = current;
				current = (current.Right != null) ? current.Right : current.Parent;
			}
		}

#endif

		/// <summary>
		/// Deletes the specified interval.
		/// If the interval tree is used with unique intervals, this method removes the interval specified as an argument.
		/// If multiple identical intervals (starting at the same time and also ending at the same time) are allowed, this function will delete one of them( see procedure DeleteIntervalFromNodeWithRange for details)
		/// In this case, it is easy enough to either specify the (interval, value) pair to be deleted or enforce uniqueness by changing the Add procedure.
		/// </summary>
		/// <param name="arg">The arg.</param>
		public bool Delete(TextInterval arg)
		{
			if (this.Root != null)
			{
				bool wasDeleted = false;
				bool wasSuccessful = false;

				this.Root = IntervalNode.Delete(this.Root, arg, ref wasDeleted, ref wasSuccessful);
				if (this.Root != null)
				{
					IntervalNode.ComputeMax(this.Root);
				}

				if (wasSuccessful)
				{
					this.Count--;
				}
				return wasSuccessful;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Searches for all intervals overlapping the one specified.
		/// If multiple intervals starting at the same time/value are found to overlap the specified interval, they are returned in decreasing order of their End values.
		/// </summary>
		/// <param name="toFind">To find.</param>
		/// <param name="list">The list.</param>
		public void GetIntervalsOverlappingWith(TextInterval toFind, List<TypeValue> list)
		{
			if (this.Root != null)
			{
				this.Root.GetIntervalsOverlappingWith(toFind, list);
			}
		}

		/// <summary>
		/// Searches for all intervals overlapping the one specified.
		/// If multiple intervals starting at the same time/value are found to overlap the specified interval, they are returned in decreasing order of their End values.
		/// </summary>
		/// <param name="toFind">To find.</param>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<TextInterval, TypeValue>> GetIntervalsOverlappingWith(TextInterval toFind)
		{
			return (this.Root != null) ? this.Root.GetIntervalsOverlappingWith(toFind) : null;
		}

		/// <summary>
		/// Returns all intervals beginning at the specified start value.
		/// The multiple intervals start at the specified value, they are sorted based on their End value (i.e. returned in ascending order of their End values)
		/// </summary>
		/// <param name="arg">The arg.</param>
		/// <returns></returns>
		public List<KeyValuePair<TextInterval, TypeValue>> GetIntervalsStartingAt(TextPosition arg)
		{
			return IntervalNode.GetIntervalsStartingAt(this.Root, arg);
		}

#if TREE_WITH_PARENT_POINTERS

		/// <summary>
		/// Gets the collection of intervals (in ascending order of their Start values).
		/// Those intervals starting at the same time/value are sorted further based on their End value (i.e. returned in ascending order of their End values)
		/// </summary>
		public IEnumerable<TextInterval> Intervals
		{
			get
			{
				if (this.Root == null)
				{
					yield break;
				}

				var p = IntervalNode.FindMin(this.Root);
				while (p != null)
				{
					foreach (var rangeNode in p.GetRangeReverse())
					{
						yield return rangeNode.Key;
					}

					yield return p.Interval;
					p = p.Successor();
				}
			}
		}

		/// <summary>
		/// Gets the collection of values (ascending order)
		/// Those intervals starting at the same time/value are sorted further based on their End value (i.e. returned in ascending order of their End values)
		/// </summary>
		public IEnumerable<TypeValue> Values
		{
			get
			{
				if (this.Root == null)
				{
					yield break;
				}

				var p = IntervalNode.FindMin(this.Root);
				while (p != null)
				{
					foreach (var rangeNode in p.GetRangeReverse())
					{
						yield return rangeNode.Value;
					}

					yield return p.Value;
					p = p.Successor();
				}
			}
		}

		/// <summary>
		/// Gets the interval value pairs.
		/// Those intervals starting at the same time/value are sorted further based on their End value (i.e. returned in ascending order of their End values)
		/// </summary>
		public IEnumerable<KeyValuePair<TextInterval, TypeValue>> IntervalValuePairs
		{
			get
			{
				if (this.Root == null)
				{
					yield break;
				}

				var p = IntervalNode.FindMin(this.Root);
				while (p != null)
				{
					foreach (var rangeNode in p.GetRangeReverse())
					{
						yield return rangeNode;
					}

					yield return new KeyValuePair<TextInterval, TypeValue>(p.Interval, p.Value);
					p = p.Successor();
				}
			}
		}

#endif

		/// <summary>
		/// Tries to the get the value associated with the interval.
		/// </summary>
		/// <param name="subtree">The subtree.</param>
		/// <param name="data">The data.</param>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public bool TryGetInterval(TextInterval data, out TypeValue value)
		{
			return this.TryGetIntervalImpl(this.Root, data, out value);
		}

		/// <summary>
		/// Clears this instance.
		/// </summary>
		public void Clear()
		{
			this.Root = null;
			this.Count = 0;
		}

		/// <summary>
		/// Searches for interval starting at.
		/// </summary>
		/// <param name="subtree">The subtree.</param>
		/// <param name="data">The data.</param>
		/// <returns></returns>
		private bool TryGetIntervalImpl(IntervalNode subtree, TextInterval data, out TypeValue value)
		{
			var current = subtree;
			while (current != null)
			{
				int compareResult = data.Start.CompareTo(current.Interval.Start);

				if (compareResult < 0)
				{
					current = current.Left;
				}
				else if (compareResult > 0)
				{
					current = current.Right;
				}
				else
				{
					if (data.End.CompareTo(current.Interval.End) == 0)
					{
						value = current.Value;
						return true;
					}
					else if (current.Range != null)
					{
						int kthIndex = current.Range.BinarySearch(
							new KeyValuePair<TextPosition, TypeValue>(data.End, default(TypeValue)), 
							this.keyvalueComparer);
						if (kthIndex >= 0)
						{
							value = current.Range[kthIndex].Value;
							return true;
						}
					}
					break;
				}
			}
			value = default(TypeValue);
			return false;
		}

		#endregion

		#region Nested Classes

		/// <summary>
		/// IntervalNode class.
		/// </summary>
		/// <typeparam name="TElem">The type of the elem.</typeparam>
		private class IntervalNode
		{
			#region Fields

#if TREE_WITH_PARENT_POINTERS
			public IntervalNode Parent;
#endif
			#endregion

			#region Properties

			public int Balance;// { get; private set; }
			public IntervalNode Left;//  { get; private set; }
			public IntervalNode Right;// { get; private set; }
			public TextInterval Interval;// { get; private set; }
			public TypeValue Value;// { get; private set; }
			public List<KeyValuePair<TextPosition, TypeValue>> Range;// { get; private set; }
			public TextPosition Max;// { get;  private set; }

			#endregion

			#region C'tor

			public IntervalNode(TextInterval interval, TypeValue value)
			{
				this.Left = null;
				this.Right = null;
				this.Balance = 0;
				this.Interval = interval;
				this.Value = value;
				this.Max = interval.End;
			}

			#endregion

			#region Methods

			/// <summary>
			/// Adds the specified elem.
			/// </summary>
			/// <param name="elem">The elem.</param>
			/// <param name="data">The data.</param>
			/// <returns></returns>
			public static IntervalNode Add(IntervalNode elem, TextInterval interval, TypeValue value, ref bool wasAdded, ref bool wasSuccessful)
			{
				if (elem == null)
				{
					elem = new IntervalNode(interval, value);
					wasAdded = true;
					wasSuccessful = true;
				}
				else
				{
					int compareResult = interval.Start.CompareTo(elem.Interval.Start);
					IntervalNode newChild = null;
					if (compareResult < 0)
					{
						newChild = Add(elem.Left, interval, value, ref wasAdded, ref wasSuccessful);
						if (elem.Left != newChild)
						{
							elem.Left = newChild;
#if TREE_WITH_PARENT_POINTERS
							newChild.Parent = elem;
#endif
						}

						if (wasAdded)
						{
							elem.Balance--;

							if (elem.Balance == 0)
							{
								wasAdded = false;
							}
							else if (elem.Balance == -2)
							{
								if (elem.Left.Balance == 1)
								{
									int elemLeftRightBalance = elem.Left.Right.Balance;

									elem.Left = RotateLeft(elem.Left);
									elem = RotateRight(elem);

									elem.Balance = 0;
									elem.Left.Balance = elemLeftRightBalance == 1 ? -1 : 0;
									elem.Right.Balance = elemLeftRightBalance == -1 ? 1 : 0;
								}
								else if (elem.Left.Balance == -1)
								{
									elem = RotateRight(elem);
									elem.Balance = 0;
									elem.Right.Balance = 0;
								}
								wasAdded = false;
							}
						}
					}
					else if (compareResult > 0)
					{
						newChild = Add(elem.Right, interval, value, ref wasAdded, ref wasSuccessful);
						if (elem.Right != newChild)
						{
							elem.Right = newChild;
#if TREE_WITH_PARENT_POINTERS
							newChild.Parent = elem;
#endif
						}

						if (wasAdded)
						{
							elem.Balance++;
							if (elem.Balance == 0)
							{
								wasAdded = false;
							}
							else if (elem.Balance == 2)
							{
								if (elem.Right.Balance == -1)
								{
									int elemRightLeftBalance = elem.Right.Left.Balance;

									elem.Right = RotateRight(elem.Right);
									elem = RotateLeft(elem);

									elem.Balance = 0;
									elem.Left.Balance = elemRightLeftBalance == 1 ? -1 : 0;
									elem.Right.Balance = elemRightLeftBalance == -1 ? 1 : 0;
								}

								else if (elem.Right.Balance == 1)
								{
									elem = RotateLeft(elem);

									elem.Balance = 0;
									elem.Left.Balance = 0;
								}
								wasAdded = false;
							}
						}
					}
					else
					{
						//// if there are more than one interval starting at the same time/value, the intervalnode.Interval stores the start time and the maximum end time of all intervals starting at the same value.
						//// all end values (except the maximum end time/value which is stored in the interval node itself) are stored in the Range list in decreasing order.
						//// note: this is ok for problems where intervals starting at the same time /value is not a frequent occurrence, however you can use other data structure for better performance depending on your problem needs

						elem.AddIntervalValuePair(interval, value);

						wasSuccessful = true;
					}
					ComputeMax(elem);
				}

				return elem;
			}

			/// <summary>
			/// Computes the max.
			/// </summary>
			/// <param name="node">The node.</param>
			public static void ComputeMax(IntervalNode node)
			{
				TextPosition maxRange = node.Interval.End;

				if (node.Left == null && node.Right == null)
				{
					node.Max = maxRange;
				}
				else if (node.Left == null)
				{
					node.Max = (maxRange.CompareTo(node.Right.Max) >= 0) ? maxRange : node.Right.Max;
				}
				else if (node.Right == null)
				{
					node.Max = (maxRange.CompareTo(node.Left.Max) >= 0) ? maxRange : node.Left.Max;
				}
				else
				{
					TextPosition leftMax = node.Left.Max;
					TextPosition rightMax = node.Right.Max;

					if (leftMax.CompareTo(rightMax) >= 0)
					{
						node.Max = maxRange.CompareTo(leftMax) >= 0 ? maxRange : leftMax;
					}
					else
					{
						node.Max = maxRange.CompareTo(rightMax) >= 0 ? maxRange : rightMax;
					}
				}
			}

			/// <summary>
			/// Finds the min.
			/// </summary>
			/// <param name="node">The node.</param>
			/// <returns></returns>
			public static IntervalNode FindMin(IntervalNode node)
			{
				while (node != null && node.Left != null)
				{
					node = node.Left;
				}
				return node;
			}

			/// <summary>
			/// Finds the max.
			/// </summary>
			/// <param name="node">The node.</param>
			/// <returns></returns>
			public static IntervalNode FindMax(IntervalNode node)
			{
				while (node != null && node.Right != null)
				{
					node = node.Right;
				}
				return node;
			}

			/// <summary>
			/// Gets the range of intervals stored in this.Range (i.e. starting at the same value 'this.Interval.Start' as the interval stored in the node itself)
			/// The range intervals are sorted in the descending order of their End interval values
			/// </summary>
			/// <returns></returns>
			public IEnumerable<KeyValuePair<TextInterval, TypeValue>> GetRange()
			{
				if (this.Range != null)
				{
					foreach (var value in this.Range)
					{
						var kth = new TextInterval(this.Interval.Start, value.Key);
						yield return new KeyValuePair<TextInterval, TypeValue>(kth, value.Value);
					}
				}
				else
				{
					yield break;
				}
			}

			/// <summary>
			/// Gets the range of intervals stored in this.Range (i.e. starting at the same value 'this.Interval.Start' as the interval stored in the node itself).
			/// The range intervals are sorted in the ascending order of their End interval values
			/// </summary>
			/// <returns></returns>
			public IEnumerable<KeyValuePair<TextInterval, TypeValue>> GetRangeReverse()
			{
				if (this.Range != null && this.Range.Count > 0)
				{
					int rangeCount = this.Range.Count;
					for (int k = rangeCount - 1; k >= 0; k--)
					{
						var kth = new TextInterval(this.Interval.Start, this.Range[k].Key);
						yield return new KeyValuePair<TextInterval, TypeValue>(kth, this.Range[k].Value);
					}
				}
				else
				{
					yield break;
				}
			}

#if TREE_WITH_PARENT_POINTERS

			/// <summary>
			/// Succeeds this instance.
			/// </summary>
			/// <returns></returns>
			public IntervalNode Successor()
			{
				if (this.Right != null)
					return FindMin(this.Right);
				else
				{
					var p = this;
					while (p.Parent != null && p.Parent.Right == p)
					{
						p = p.Parent;
					}
					return p.Parent;
				}
			}

			/// <summary>
			/// Precedes this instance.
			/// </summary>
			/// <returns></returns>
			public IntervalNode Predecesor()
			{
				if (this.Left != null)
				{
					return FindMax(this.Left);
				}
				else
				{
					var p = this;
					while (p.Parent != null && p.Parent.Left == p)
					{
						p = p.Parent;
					}
					return p.Parent;
				}
			}
#endif

			/// <summary>
			/// Deletes the specified node.
			/// </summary>
			/// <param name="node">The node.</param>
			/// <param name="arg">The arg.</param>
			/// <returns></returns>
			public static IntervalNode Delete(IntervalNode node, TextInterval arg, ref bool wasDeleted, ref bool wasSuccessful)
			{
				int cmp = arg.Start.CompareTo(node.Interval.Start);
				IntervalNode newChild = null;

				if (cmp < 0)
				{
					if (node.Left != null)
					{
						newChild = Delete(node.Left, arg, ref wasDeleted, ref wasSuccessful);
						if (node.Left != newChild)
						{
							node.Left = newChild;
						}

						if (wasDeleted)
						{
							node.Balance++;
						}
					}
				}
				else if (cmp == 0)
				{
					if (arg.End.CompareTo(node.Interval.End) == 0 && node.Range == null)
					{
						if (node.Left != null && node.Right != null)
						{
							var min = FindMin(node.Right);

							var interval = node.Interval;
							node.Swap(min);

							wasDeleted = false;

							newChild = Delete(node.Right, interval, ref wasDeleted, ref wasSuccessful);
							if (node.Right != newChild)
							{
								node.Right = newChild;
							}

							if (wasDeleted)
							{
								node.Balance--;
							}
						}
						else if (node.Left == null)
						{
							wasDeleted = true;
							wasSuccessful = true;

#if TREE_WITH_PARENT_POINTERS
							if (node.Right != null)
							{
								node.Right.Parent = node.Parent;
							}
#endif
							return node.Right;
						}
						else
						{
							wasDeleted = true;
							wasSuccessful = true;
#if TREE_WITH_PARENT_POINTERS
							if (node.Left != null)
							{
								node.Left.Parent = node.Parent;
							}
#endif
							return node.Left;
						}
					}
					else
					{
						wasSuccessful = node.DeleteIntervalFromNodeWithRange(arg);
					}
				}
				else
				{
					if (node.Right != null)
					{
						newChild = Delete(node.Right, arg, ref wasDeleted, ref wasSuccessful);
						if (node.Right != newChild)
						{
							node.Right = newChild;
						}

						if (wasDeleted)
						{
							node.Balance--;
						}
					}
				}

				ComputeMax(node);

				if (wasDeleted)
				{
					if (node.Balance == 1 || node.Balance == -1)
					{
						wasDeleted = false;
						return node;
					}
					else if (node.Balance == -2)
					{
						if (node.Left.Balance == 1)
						{
							int leftRightBalance = node.Left.Right.Balance;

							node.Left = RotateLeft(node.Left);
							node = RotateRight(node);

							node.Balance = 0;
							node.Left.Balance = (leftRightBalance == 1) ? -1 : 0;
							node.Right.Balance = (leftRightBalance == -1) ? 1 : 0;
						}
						else if (node.Left.Balance == -1)
						{
							node = RotateRight(node);
							node.Balance = 0;
							node.Right.Balance = 0;
						}
						else if (node.Left.Balance == 0)
						{
							node = RotateRight(node);
							node.Balance = 1;
							node.Right.Balance = -1;

							wasDeleted = false;
						}
					}
					else if (node.Balance == 2)
					{
						if (node.Right.Balance == -1)
						{
							int rightLeftBalance = node.Right.Left.Balance;

							node.Right = RotateRight(node.Right);
							node = RotateLeft(node);

							node.Balance = 0;
							node.Left.Balance = (rightLeftBalance == 1) ? -1 : 0;
							node.Right.Balance = (rightLeftBalance == -1) ? 1 : 0;
						}
						else if (node.Right.Balance == 1)
						{
							node = RotateLeft(node);
							node.Balance = 0;
							node.Left.Balance = 0;
						}
						else if (node.Right.Balance == 0)
						{
							node = RotateLeft(node);
							node.Balance = -1;
							node.Left.Balance = 1;

							wasDeleted = false;
						}
					}
				}

				return node;
			}

			/// <summary>
			/// Returns all intervals beginning at the specified start value. The intervals are sorted based on their End value (i.e. returned in ascending order of their End values)
			/// </summary>
			/// <param name="subtree">The subtree.</param>
			/// <param name="data">The data.</param>
			/// <returns></returns>
			public static List<KeyValuePair<TextInterval, TypeValue>> GetIntervalsStartingAt(IntervalNode subtree, TextPosition start)
			{
				var current = subtree;
				while (current != null)
				{
					int compareResult = start.CompareTo(current.Interval.Start);
					if (compareResult < 0)
					{
						current = current.Left;
					}
					else if (compareResult > 0)
					{
						current = current.Right;
					}
					else
					{
						var result = new List<KeyValuePair<TextInterval, TypeValue>>();
						if (current.Range != null)
						{
							foreach (var kvp in current.GetRangeReverse())
							{
								result.Add(kvp);
							}
						}
						result.Add(new KeyValuePair<TextInterval, TypeValue>(current.Interval, current.Value));
						return result;
					}
				}
				return null;
			}

			/// <summary>
			/// Searches for all intervals in this subtree that are overlapping the argument interval.
			/// If multiple intervals starting at the same time/value are found to overlap, they are returned in decreasing order of their End values.
			/// </summary>
			/// <param name="toFind">To find.</param>
			/// <param name="list">The list.</param>
			public void GetIntervalsOverlappingWith(TextInterval toFind, List<TypeValue> list)
			{
				var toFindStart = toFind.Start;
				var toFindEnd = toFind.End;
				
				var current = this;
				IntervalNode previous = null;
				
				while (current != null)
				{
					var currentMax = current.Max;
					if (toFindStart.line > currentMax.line || toFindStart.line == currentMax.line && toFindStart.index >= currentMax.index)
					{
						// toFind begins after the subtree.Max ends, prune the whole subtree and continue with the parent node.
						previous = current;
						current = current.Parent;
						continue;
					}

					if (previous == current.Parent)
					{
						// Coming down from the parent node - search the left subtree first.
						if (current.Left != null)
						{
							previous = current;
							current = current.Left;
							continue;
						}

						// No left child - search the current node and the right subtree.
						previous = null;
					}
					
					if (previous == current.Left)
					{
						// Coming up from the left child or no left child - search the current node.
						
						var currentStart = current.Interval.Start;
						if (toFindEnd.line < currentStart.line || toFindEnd.line == currentStart.line && toFindEnd.index <= currentStart.index)
						{
							// toFind ends before the this node's beginning, prune this node with its right subtree and continue with the parent node.
							previous = current;
							current = current.Parent;
							continue;
						}
						
						// Check overlapping...
						var currentEnd = current.Interval.End;
						if (currentEnd.line > toFindStart.line || currentEnd.line == toFindStart.line && currentEnd.index > toFindStart.index)
						//if (current.Interval.OverlapsWith(toFind))
						{
							list.Add(current.Value);
						
							// The max value is stored in the node, if the node doesn't overlap then neither are the nodes in its range.
							var range = current.Range;
							if (range != null && range.Count > 0)
							{
								var currentInterval = current.Interval;
								foreach (var value in range)
								{
									currentInterval.End = value.Key;
									if (value.Key.line > toFindStart.line || value.Key.line == toFindStart.line && value.Key.index > toFindStart.index)
									{
										list.Add(value.Value);
									}
									else
									{
										break;
									}
								}
							}
						}
						
						// Continue with the right subtree.
						previous = current;
						if (current.Right != null)
						{
							current = current.Right;
							continue;
						}
					}
					
					// Note that here previous == current.Right...

					// Coming up from the right child or no right child - continue with the parent node.
					previous = current;
					current = current.Parent;
				}
			}
			
			static Stack<IntervalNode> stack = new Stack<IntervalNode>();
			
			/// <summary>
			/// Searches for all intervals in this subtree that are overlapping the argument interval.
			/// If multiple intervals starting at the same time/value are found to overlap, they are returned in decreasing order of their End values.
			/// </summary>
			/// <param name="toFind">To find.</param>
			/// <param name="list">The list.</param>
			public void Old_GetIntervalsOverlappingWith(TextInterval toFind, List<TypeValue> list)
			{
				var toFindStart = toFind.Start;
				var toFindEnd = toFind.End;
				
				var current = this;
				while (current != null || stack.Count > 0)
				{
					while (current != null)
					{
						var currentStart = current.Interval.Start;
						if (toFindEnd.line < currentStart.line || toFindEnd.line == currentStart.line && toFindEnd.index <= currentStart.index)
						{
							////toFind ends before subtree.Data begins, prune the right subtree
							current = current.Left;
							continue;
						}
						
						var currentMax = current.Max;
						if (toFindStart.line > currentMax.line || toFindStart.line == currentMax.line && toFindStart.index >= currentMax.index)
						{
							////toFind begins after the subtree.Max ends, prune the whole subtree
							current = null;
							break;
						}

						//// search the left subtree
						stack.Push(current);
						current = current.Left;
					}
					
					if (stack.Count == 0)
					{
						break;
					}
					
					current = stack.Pop();
					
					var currentInterval = current.Interval;
					if (currentInterval.OverlapsWith(toFind))
					{
						list.Add(current.Value);
						
						////the max value is stored in the node, if the node doesn't overlap then neither are the nodes in its range
						var range = current.Range;
						if (range != null && range.Count > 0)
						{
							foreach (var value in range)
							{
								currentInterval.End = value.Key;
								if (value.Key.line > toFindStart.line || value.Key.line == toFindStart.line && value.Key.index > toFindStart.index)
								{
									list.Add(value.Value);
								}
								else
								{
									break;
								}
							}
						}
					}

					//// search the right subtree
					current = current.Right;
				}
			}

			/// <summary>
			/// Gets all intervals in this subtree that are overlapping the argument interval.
			/// If multiple intervals starting at the same time/value are found to overlap, they are returned in decreasing order of their End values.
			/// </summary>
			/// <param name="toFind">To find.</param>
			/// <returns></returns>
			public IEnumerable<KeyValuePair<TextInterval, TypeValue>> GetIntervalsOverlappingWith(TextInterval toFind)
			{
				if (toFind.End.CompareTo(this.Interval.Start) <= 0)
				{
					////toFind ends before subtree.Data begins, prune the right subtree
					if (this.Left != null)
					{
						foreach (var value in this.Left.GetIntervalsOverlappingWith(toFind))
						{
							yield return value;
						}
					}
				}
				else if (toFind.Start.CompareTo(this.Max) >= 0)
				{
					////toFind begins after the subtree.Max ends, prune the left subtree
					if (this.Right != null)
					{
						foreach (var value in this.Right.GetIntervalsOverlappingWith(toFind))
						{
							yield return value;
						}
					}
				}
				else
				{
					if (this.Left != null)
					{
						foreach (var value in this.Left.GetIntervalsOverlappingWith(toFind))
						{
							yield return value;
						}
					}

					if (this.Interval.OverlapsWith(toFind))
					{
						yield return new KeyValuePair<TextInterval, TypeValue>(this.Interval, this.Value);

						if (this.Range != null && this.Range.Count > 0)
						{
							foreach (var kvp in this.GetRange())
							{
								if (kvp.Key.OverlapsWith(toFind))
								{
									yield return kvp;
								}
								else
								{
									break;
								}
							}
						}
					}

					if (this.Right != null)
					{
						foreach (var value in this.Right.GetIntervalsOverlappingWith(toFind))
						{
							yield return value;
						}
					}
				}
			}

			/// <summary>
			/// Rotates lefts this instance.
			/// Assumes that this.Right != null
			/// </summary>
			/// <returns></returns>
			private static IntervalNode RotateLeft(IntervalNode node)
			{
				var right = node.Right;
				Debug.Assert(node.Right != null);

				var rightLeft = right.Left;

				node.Right = rightLeft;
				ComputeMax(node);

#if TREE_WITH_PARENT_POINTERS
				var parent = node.Parent;
				if (rightLeft != null)
				{
					rightLeft.Parent = node;
				}
#endif
				right.Left = node;
				ComputeMax(right);

#if TREE_WITH_PARENT_POINTERS
				node.Parent = right;
				if (parent != null)
				{
					if (parent.Left == node)
					{
						parent.Left = right;
					}
					else
					{
						parent.Right = right;
					}
				}
				right.Parent = parent;
#endif
				return right;
			}

			/// <summary>
			/// Rotates right this instance.
			/// Assumes that (this.Left != null)
			/// </summary>
			/// <returns></returns>
			private static IntervalNode RotateRight(IntervalNode node)
			{
				var left = node.Left;
				Debug.Assert(node.Left != null);

				var leftRight = left.Right;
				node.Left = leftRight;
				ComputeMax(node);

#if TREE_WITH_PARENT_POINTERS
				var parent = node.Parent;
				if (leftRight != null)
				{
					leftRight.Parent = node;
				}
#endif
				left.Right = node;
				ComputeMax(left);

#if TREE_WITH_PARENT_POINTERS
				node.Parent = left;
				if (parent != null)
				{
					if (parent.Left == node)
					{
						parent.Left = left;
					}
					else
					{
						parent.Right = left;
					}
				}
				left.Parent = parent;
#endif
				return left;
			}

			/// <summary>
			/// Deletes the specified interval from this node.
			/// If the interval tree is used with unique intervals, this method removes the interval specified as an argument.
			/// If multiple identical intervals (starting at the same time and also ending at the same time) are allowed, this function will delete one of them.
			/// In this case, it is easy enough to either specify the (interval, value) pair to be deleted or enforce uniqueness by changing the Add procedure.
			/// </summary>
			/// <param name="interval">The interval to be deleted.</param>
			/// <returns></returns>
			private bool DeleteIntervalFromNodeWithRange(TextInterval interval)
			{
				if (this.Range != null && this.Range.Count > 0)
				{
					int rangeCount = this.Range.Count;
					int intervalPosition = -1;

					// find the exact interval to delete based on its End value.
					if (interval.End.CompareTo(this.Interval.End) == 0)
					{
						intervalPosition = 0;
					}
					else if (rangeCount > 12)
					{
						var keyvalueComparer = new KeyValueComparer<TextPosition, TypeValue>(ComparerUtil.comparer);
						int k = this.Range.BinarySearch(new KeyValuePair<TextPosition, TypeValue>(interval.End, default(TypeValue)), keyvalueComparer);
						if (k >= 0)
						{
							intervalPosition = k + 1;
						}
					}
					else
					{
						for (int k = 0; k < rangeCount; k++)
						{
							if (interval.End.CompareTo(this.Range[k].Key) == 0)
							{
								intervalPosition = k + 1;
								break;
							}
						}
					}

					if (intervalPosition < 0)
					{
						return false;
					}
					else if (intervalPosition == 0)
					{
						this.Interval = new TextInterval(this.Interval.Start, this.Range[0].Key);
						this.Value = this.Range[0].Value;
						this.Range.RemoveAt(0);
					}
					else if (intervalPosition > 0)
					{
						this.Range.RemoveAt(intervalPosition - 1);
					}

					if (this.Range.Count == 0)
					{
						this.Range = null;
					}

					return true;
				}
				else
				{
					////if interval end was not found in the range (or the node itself) or if the node doesnt have a range, return false
					return false;
				}
			}

			private void Swap(IntervalNode node)
			{
				var dataInterval = this.Interval;
				var dataValue = this.Value;
				var dataRange = this.Range;

				this.Interval = node.Interval;
				this.Value = node.Value;
				this.Range = node.Range;

				node.Interval = dataInterval;
				node.Value = dataValue;
				node.Range = dataRange;
			}

			private void AddIntervalValuePair(TextInterval interval, TypeValue value)
			{
				if (this.Range == null)
				{
					this.Range = new List<KeyValuePair<TextPosition, TypeValue>>();
				}

				////always store the max End value in the node.Data itself .. store the Range list in decreasing order
				if (interval.End.CompareTo(this.Interval.End) > 0)
				{
					this.Range.Insert(0, new KeyValuePair<TextPosition, TypeValue>(this.Interval.End, this.Value));
					this.Interval = interval;
					this.Value = value;
				}
				else
				{
					bool wasAdded = false;
					for (int i = 0; i < this.Range.Count; i++)
					{
						if (interval.End.CompareTo(this.Range[i].Key) >= 0)
						{
							this.Range.Insert(i, new KeyValuePair<TextPosition, TypeValue>(interval.End, value));
							wasAdded = true;
							break;
						}
					}
					if (!wasAdded)
					{
						this.Range.Add(new KeyValuePair<TextPosition, TypeValue>(interval.End, value));
					}
				}
			}

			#endregion
		}

		private class KeyValueComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>>
		{
			private IComparer<TKey> keyComparer;

			/// <summary>
			/// Initializes a new instance of the <see cref="IntervalTree&lt;T, TypeValue&gt;.KeyValueComparer&lt;TKey, TValue&gt;"/> class.
			/// </summary>
			/// <param name="keyComparer">The key comparer.</param>
			public KeyValueComparer(IComparer<TKey> keyComparer)
			{
				this.keyComparer = keyComparer;
			}

			/// <summary>
			/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
			/// </summary>
			/// <param name="x">The first object to compare.</param>
			/// <param name="y">The second object to compare.</param>
			/// <returns>
			/// Value Condition Less than zero is less than y.Zerox equals y.Greater than zero is greater than y.
			/// </returns>
			public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
			{
				return (-1) * this.keyComparer.Compare(x.Key, y.Key);
			}

			/// <summary>
			/// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
			/// </summary>
			/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
			/// <returns>
			///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
			/// </returns>
			public override bool Equals(object obj)
			{
				if (obj is KeyValueComparer<TKey, TValue>)
				{
					return object.Equals(this.keyComparer, ((KeyValueComparer<TKey, TValue>)obj).keyComparer);
				}
				else
				{
					return false;
				}
			}

			/// <summary>
			/// Returns a hash code for this instance.
			/// </summary>
			/// <returns>
			/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
			/// </returns>
			public override int GetHashCode()
			{
				return this.keyComparer.GetHashCode();
			}
		}

		public static class ComparerUtil
		{
			public static IComparer<TextPosition> comparer = Comparer<TextPosition>.Default;
		}

		#endregion
	}
	
}
