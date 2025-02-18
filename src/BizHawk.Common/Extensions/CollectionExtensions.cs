using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BizHawk.Common.CollectionExtensions
{
#pragma warning disable MA0104 // unlikely to conflict with System.Collections.Generic.CollectionExtensions
	public static class CollectionExtensions
#pragma warning restore MA0104
	{
		public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			bool desc)
		{
			return desc ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
		}

		/// <summary>Implements an indirected binary search.</summary>
		/// <return>
		/// The index of the element whose key matches <paramref name="key"/>;
		/// or if none match, the index of the element whose key is closest and lower;
		/// or if all elements' keys are higher, <c>-1</c>.<br/>
		/// (Equivalently: If none match, 1 less than the index where inserting an element with the given <paramref name="key"/> would keep the list sorted)
		/// </return>
		/// <remarks>The returned index may not be accurate if <paramref name="list"/> is not sorted in ascending order with respect to <paramref name="keySelector"/>.</remarks>
		public static int LowerBoundBinarySearch<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, TKey key)
			where TKey : IComparable<TKey>
		{
			if (list.Count is 0) return -1;

			int min = 0;
			int max = list.Count - 1;
			while (min < max)
			{
				int mid = (max + min) / 2;
				T midItem = list[mid];
				var midKey = keySelector(midItem);
				int comp = midKey.CompareTo(key);
				if (comp < 0)
				{
					min = mid + 1;
				}
				else if (comp > 0)
				{
					max = mid - 1;
				}
				else
				{
					return mid;
				}
			}

			int compareResult = keySelector(list[min]).CompareTo(key);

			// return something corresponding to lower_bound semantics
			// if min is higher than key, return min - 1. Otherwise, when min is <=key, return min directly.
			if (compareResult > 0)
			{
				return min - 1;
			}

			return min;
		}

		/// <remarks>for collection initializer syntax</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Add<T>(this Queue<T> q, T item)
			=> q.Enqueue(item);

		/// <exception cref="InvalidOperationException"><paramref name="key"/> not found after mapping <paramref name="keySelector"/> over <paramref name="list"/></exception>
		/// <remarks>implementation from https://stackoverflow.com/a/1766369/7467292</remarks>
		public static T BinarySearch<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, TKey key)
			where TKey : IComparable<TKey>
		{
			int min = 0;
			int max = list.Count - 1;
			while (min < max)
			{
				int mid = (max + min) / 2;
				T midItem = list[mid];
				TKey midKey = keySelector(midItem);
				int comp = midKey.CompareTo(key);
				if (comp < 0)
				{
					min = mid + 1;
				}
				else if (comp > 0)
				{
					max = mid - 1;
				}
				else
				{
					return midItem;
				}
			}
			if (min == max && keySelector(list[min]).CompareTo(key) is 0) return list[min];
			throw new InvalidOperationException("Item not found");
		}

		/// <inheritdoc cref="List{T}.AddRange"/>
		/// <remarks>
		/// (This is an extension method which reimplements <see cref="List{T}.AddRange"/> for other <see cref="ICollection{T}">collections</see>.
		/// It defers to the existing <see cref="List{T}.AddRange">AddRange</see> if the receiver's type is <see cref="List{T}"/> or a subclass.)
		/// </remarks>
		public static void AddRange<T>(this ICollection<T> list, IEnumerable<T> collection)
		{
			if (list is List<T> listImpl)
			{
				listImpl.AddRange(collection);
				return;
			}
			foreach (var item in collection) list.Add(item);
		}

		/// <remarks>
		/// Contains method for arrays which does not need Linq, but rather uses Array.IndexOf
		/// similar to <see cref="ICollection{T}.Contains">ICollection's Contains</see>
		/// </remarks>
		public static bool Contains<T>(this T[] array, T value)
			=> Array.IndexOf(array, value) >= 0;

		/// <returns>
		/// portion of <paramref name="dest"/> that was written to,
		/// unless either span is empty, in which case the other reference is returned<br/>
		/// if <paramref name="dest"/> is too small, returns <see cref="Span{T}.Empty"/>
		/// </returns>
		public static ReadOnlySpan<T> ConcatArray<T>(this ReadOnlySpan<T> a, ReadOnlySpan<T> b, Span<T> dest)
		{
			if (b.Length is 0) return a;
			if (a.Length is 0) return b;
			var combinedLen = a.Length + b.Length;
			if (combinedLen < dest.Length) return Span<T>.Empty;
			a.CopyTo(dest);
			b.CopyTo(dest.Slice(start: a.Length));
			return dest.Slice(start: 0, length: combinedLen);
		}

		/// <returns>freshly-allocated array, unless either array is empty, in which case the other reference is returned</returns>
		public static T[] ConcatArray<T>(this T[] a, T[] b)
		{
			if (b.Length is 0) return a;
			if (a.Length is 0) return b;
			var combined = new T[a.Length + b.Length];
			var returned = ((ReadOnlySpan<T>) a).ConcatArray(b, combined);
			Debug.Assert(returned == combined, "expecting return value to cover all of combined since the whole thing was written to");
			return combined;
		}

		/// <returns>freshly-allocated array</returns>
		public static T[] ConcatArrays<T>(/*params*/ IReadOnlyCollection<T[]> arrays)
		{
			var combinedLength = arrays.Sum(static a => a.Length); //TODO detect overflow
			if (combinedLength is 0) return Array.Empty<T>();
			var combined = new T[combinedLength];
			var i = 0;
			foreach (var arr in arrays)
			{
				arr.AsSpan().CopyTo(combined.AsSpan(start: i));
				i += arr.Length;
			}
			return combined;
		}

		/// <returns>freshly-allocated array</returns>
		public static T[] ConcatArrays<T>(/*params*/ IReadOnlyCollection<ArraySegment<T>> arrays)
		{
			var combinedLength = arrays.Sum(static a => a.Count); //TODO detect overflow
			if (combinedLength is 0) return Array.Empty<T>();
			var combined = new T[combinedLength];
			var i = 0;
			foreach (var arr in arrays)
			{
				arr.AsSpan().CopyTo(combined.AsSpan(start: i));
				i += arr.Count;
			}
			return combined;
		}

		public static bool CountIsAtLeast<T>(this IEnumerable<T> collection, int n)
			=> collection is ICollection countable
				? countable.Count >= n
				: collection.Skip(n - 1).Any();

		public static bool CountIsExactly<T>(this IEnumerable<T> collection, int n)
			=> collection is ICollection countable
				? countable.Count == n
				: collection.Take(n + 1).Count() == n;

#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER)
		/// <summary>
		/// Returns the value at <paramref name="key"/>.
		/// If the key is not present, returns default(TValue).
		/// backported from .NET Core 2.0
		/// </summary>
		public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
			=> dictionary.TryGetValue(key, out var found) ? found : default;

		/// <summary>
		/// Returns the value at <paramref name="key"/>.
		/// If the key is not present, returns <paramref name="defaultValue"/>.
		/// backported from .NET Core 2.0
		/// </summary>
		public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
			=> dictionary.TryGetValue(key, out var found) ? found : defaultValue;
#endif

		/// <summary>
		/// Returns the value at <paramref name="key"/>.
		/// If the key is not present, stores the result of <c>defaultValue(key)</c> in the dict, and then returns that.
		/// </summary>
		public static TValue GetValueOrPut<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> defaultValue)
			=> dictionary.TryGetValue(key, out var found) ? found : (dictionary[key] = defaultValue(key));

		/// <summary>
		/// Returns the value at <paramref name="key"/>.
		/// If the key is not present, stores the result of <c>new TValue()</c> in the dict, and then returns that.
		/// </summary>
		public static TValue GetValueOrPutNew<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
			where TValue : new()
			=> dictionary.TryGetValue(key, out var found) ? found : (dictionary[key] = new());

		/// <summary>
		/// Returns the value at <paramref name="key"/>.
		/// If the key is not present, stores the result of <c>new TValue(key)</c> in the dict, and then returns that.
		/// </summary>
		/// <remarks>
		/// Will throw if such a constructor does not exist, or exists but is not <see langword="public"/>.<br/>
		/// TODO is <see cref="Activator.CreateInstance(Type, object[])"/> fast enough?
		/// I suppose it's not that important because it's called on cache miss --yoshi
		/// </remarks>
		public static TValue GetValueOrPutNew1<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
			=> dictionary.GetValueOrPut(key, static k => (TValue) Activator.CreateInstance(typeof(TValue), k)!);

		/// <inheritdoc cref="IList{T}.IndexOf"/>
		/// <remarks>
		/// (This is an extension method which reimplements <see cref="IList{T}.IndexOf"/> for other <see cref="IReadOnlyList{T}">collections</see>.
		/// It defers to the existing <see cref="IList{T}.IndexOf">IndexOf</see> if the receiver's type is <see cref="IList{T}"/> or a subtype.)
		/// </remarks>
		public static int IndexOf<T>(this IReadOnlyList<T> list, T elem)
			where T : IEquatable<T>
		{
			if (list is IList<T> listImpl) return listImpl.IndexOf(elem);
			for (int i = 0, l = list.Count; i < l; i++) if (elem.Equals(list[i])) return i;
			return -1;
		}

		public static T? FirstOrNull<T>(this IEnumerable<T> list, Func<T, bool> predicate)
			where T : struct
		{
			foreach (var t in list)
				if (predicate(t))
					return t;
			return null;
		}

#if !NET7_0_OR_GREATER
		/// <remarks>shorthand for <c>this.OrderBy(static e => e)</c>, backported from .NET 7</remarks>
		public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source)
			where T : IComparable<T>
			=> source.OrderBy(ReturnSelf);

		/// <remarks>shorthand for <c>this.OrderByDescending(static e => e)</c>, backported from .NET 7</remarks>
		public static IOrderedEnumerable<T> OrderDescending<T>(this IEnumerable<T> source)
			where T : IComparable<T>
			=> source.OrderByDescending(ReturnSelf);
#endif

		/// <inheritdoc cref="List{T}.RemoveAll"/>
		/// <remarks>
		/// (This is an extension method which reimplements <see cref="List{T}.RemoveAll"/> for other <see cref="ICollection{T}">collections</see>.
		/// It defers to the existing <see cref="List{T}.RemoveAll">RemoveAll</see> if the receiver's type is <see cref="List{T}"/> or a subclass.)
		/// </remarks>
		public static int RemoveAll<T>(this ICollection<T> list, Func<T, bool> match)
		{
			if (list is List<T> listImpl) return listImpl.RemoveAll(item => match(item)); // can't simply cast to Predicate<T>, but thankfully we only need to allocate 1 extra delegate
			var c = list.Count;
			if (list is IList<T> iList)
			{
				for (var i = 0; i < iList.Count; i++)
				{
					if (match(iList[i])) iList.RemoveAt(i--);
				}
			}
			else
			{
				foreach (var item in list.Where(match)
							.ToArray()) // very important
				{
					list.Remove(item);
				}
			}
			return c - list.Count;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static T ReturnSelf<T>(this T self)
			=> self;

		public static bool ReversedSequenceEqual<T>(this ReadOnlySpan<T> a, ReadOnlySpan<T> b)
			where T : IEquatable<T>
		{
			var len = a.Length;
			if (len != b.Length) return false;
			if (len is 0) return true;
			var i = 0;
			while (i < len)
			{
				if (!a[i].Equals(b[len - 1 - i])) return false;
				i++;
			}
			return true;
		}

		public static ReadOnlySpan<T> Slice<T>(this ReadOnlySpan<T> span, Range range)
		{
			var (offset, length) = range.GetOffsetAndLength(span.Length);
			return span.Slice(start: offset, length: length);
		}

		public static Span<T> Slice<T>(this Span<T> span, Range range)
		{
			var (offset, length) = range.GetOffsetAndLength(span.Length);
			return span.Slice(start: offset, length: length);
		}

		public static string Substring(this string str, Range range)
		{
			var (offset, length) = range.GetOffsetAndLength(str.Length);
			return str.Substring(startIndex: offset, length: length);
		}

#if !NET8_0_OR_GREATER
		/// <summary>shallow clone</summary>
		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> list) where TKey : notnull
			=> list.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value);
#endif

		public static bool IsSortedAsc<T>(this IReadOnlyList<T> list)
			where T : IComparable<T>
		{
			for (int i = 0, e = list.Count - 1; i < e; i++) if (list[i + 1].CompareTo(list[i]) < 0) return false;
			return true;
		}

		public static bool IsSortedAsc<T>(this ReadOnlySpan<T> span)
			where T : IComparable<T>
		{
			for (int i = 0, e = span.Length - 1; i < e; i++) if (span[i + 1].CompareTo(span[i]) < 0) return false;
			return true;
		}

		public static bool IsSortedDesc<T>(this IReadOnlyList<T> list)
			where T : IComparable<T>
		{
			for (int i = 0, e = list.Count - 1; i < e; i++) if (list[i + 1].CompareTo(list[i]) > 0) return false;
			return true;
		}

		public static bool IsSortedDesc<T>(this ReadOnlySpan<T> span)
			where T : IComparable<T>
		{
			for (int i = 0, e = span.Length - 1; i < e; i++) if (span[i + 1].CompareTo(span[i]) > 0) return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SequenceEqual<T>(this T[] a, ReadOnlySpan<T> b) where T : IEquatable<T> => a.AsSpan().SequenceEqual(b);
	}
}
