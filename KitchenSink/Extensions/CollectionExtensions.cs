﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static KitchenSink.Operators;

namespace KitchenSink.Extensions
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Returns true if item is in sequence.
        /// </summary>
        public static bool IsIn<A>(this A val, params A[] vals)
        {
            return IsIn(val, (IEnumerable<A>) vals);
        }

        /// <summary>
        /// Returns true if item is in sequence.
        /// </summary>
        public static bool IsIn<A>(this A val, IEnumerable<A> seq)
        {
            return seq.Any(Apply<A, A, bool>(Eq, val));
        }

        /// <summary>
        /// Returns true if item is not in sequence.
        /// </summary>
        public static bool IsNotIn<A>(this A val, params A[] vals)
        {
            return !IsIn(val, (IEnumerable<A>) vals);
        }

        /// <summary>
        /// Returns true if item is not in sequence.
        /// </summary>
        public static bool IsNotIn<A>(this A val, IEnumerable<A> seq)
        {
            return !IsIn(val, seq);
        }

        /// <summary>
        /// Adapter for specialized collections that do not implement IEnumerable&lt;A&gt;.
        /// Eagerly reads enumerator results into list.
        /// Result can be enumerated multiple times.
        /// </summary>
        public static IEnumerable AsEnumerable(this IEnumerator e)
        {
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }

        /// <summary>
        /// Adapter for specialized collections that do not implement IEnumerable&lt;A&gt;.
        /// </summary>
        public static IEnumerable<A> AsEnumerable<A>(this IEnumerator<A> e)
        {
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }

        /// <summary>
        /// Adapter for specialized collections that do not implement IEnumerable&lt;A&gt;.
        /// Lazily reads enumerator results and returns them.
        /// Result can be enumerated only once.
        /// </summary>
        public static IEnumerable<A> AsEnumerableNonRepeatable<A>(this IEnumerator e)
        {
            while (e.MoveNext())
            {
                yield return (A) e.Current;
            }
        }

        /// <summary>
        /// Sorts elements by their natural order.
        /// </summary>
        public static IEnumerable<A> Sort<A>(this IEnumerable<A> seq) where A : IComparable
        {
            return seq.OrderBy(x => x);
        }

        /// <summary>
        /// Sorts elements descending by their natural order.
        /// </summary>
        public static IEnumerable<A> SortDescending<A>(this IEnumerable<A> seq) where A : IComparable
        {
            return seq.OrderByDescending(x => x);
        }

        /// <summary>
        /// Sorts elements according to given comparer.
        /// </summary>
        public static IEnumerable<A> Sort<A>(this IEnumerable<A> seq, IComparer<A> comp)
        {
            return seq.OrderBy(x => x, comp);
        }

        /// <summary>
        /// Sorts elements descending according to given comparer.
        /// </summary>
        public static IEnumerable<A> SortDescending<A>(this IEnumerable<A> seq, IComparer<A> comp)
        {
            return seq.OrderByDescending(x => x, comp);
        }

        /// <summary>
        /// Sorts elements according to given comparer.
        /// </summary>
        public static IEnumerable<A> Sort<A>(this IEnumerable<A> seq, Func<A, A, Comparison> f)
        {
            return seq.OrderBy(x => x, new ComparisonComparer<A>(f));
        }

        /// <summary>
        /// Sorts elements descending according to given comparer.
        /// </summary>
        public static IEnumerable<A> SortDescending<A>(this IEnumerable<A> seq, Func<A, A, Comparison> f)
        {
            return seq.OrderByDescending(x => x, new ComparisonComparer<A>(f));
        }

        private class ComparisonComparer<A> : IComparer<A>
        {
            private readonly Func<A, A, Comparison> f;

            public ComparisonComparer(Func<A, A, Comparison> f)
            {
                this.f = f;
            }

            public int Compare(A x, A y)
            {
                switch (f(x, y))
                {
                    case Comparison.GT: return 1;
                    case Comparison.LT: return -1;
                    case Comparison.EQ: return 0;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Returns elements in given sequence as sub-sequences of given size.
        /// Example: [1 2 3 4 5 6 7 8] 3 => [[1 2 3] [4 5 6] [7 8]]
        /// </summary>
        public static IEnumerable<IEnumerable<A>> Partition<A>(this IEnumerable<A> seq, int count)
        {
            var segment = new A[count];
            var i = 0;

            foreach (var item in seq)
            {
                segment[i] = item;
                i++;

                if (i == count)
                {
                    yield return segment;
                    segment = new A[count];
                    i = 0;
                }
            }

            if (i > 0)
            {
                yield return segment.Take(i);
            }
        }

        /// <summary>
        /// Returns sequence of overlapping pairs of elements in given sequence.
        /// Example: [1 2 3 4] => [[1 2] [2 3] [3 4]]
        /// </summary>
        public static IEnumerable<Tuple<A, A>> OverlappingPairs<A>(this IEnumerable<A> seq)
        {
            var array = seq.ToArray();

            if (array.Length < 2)
            {
                throw new ArgumentException("too few elements");
            }

            return Enumerable.Range(0, array.Length - 1).Select(i => Tuple.Create(array[i], array[i + 1]));
        }

        /// <summary>
        /// Creates a <see cref="HashSet&lt;A&gt;"/> from an <see cref="IEnumerable&lt;A&gt;"/>.
        /// </summary>
        public static HashSet<A> ToSet<A>(this IEnumerable<A> seq)
        {
            return new HashSet<A>(seq);
        }

        /// <summary>
        /// Forces entire sequence to be enumerated immediately.
        /// </summary>
        public static IEnumerable<A> Force<A>(this IEnumerable<A> seq)
        {
            return seq.ToArray();
        }

        /// <summary>
        /// Optimized version of Concat for Arrays.
        /// </summary>
        public static A[] Concat<A>(this A[] array, params A[] vals)
        {
            var result = new A[array.Length + vals.Length];
            array.CopyTo(result, 0);
            vals.CopyTo(result, array.Length);
            return result;
        }

        /// <summary>
        /// Optimized version of Concat for Lists.
        /// </summary>
        public static List<A> Concat<A>(this List<A> xs, List<A> ys)
        {
            var result = new List<A>(xs.Count + ys.Count);
            result.AddRange(xs);
            result.AddRange(ys);
            return result;
        }

        /// <summary>
        /// Returns a sequence of items paired with their index in the original sequence.
        /// </summary>
        public static IEnumerable<Tuple<A, int>> ZipWithIndicies<A>(this IEnumerable<A> seq)
        {
            var i = 0;

            foreach (var item in seq)
            {
                yield return Tuple.Create(item, i);
                i++;
            }
        }

        /// <summary>
        /// Returns sequence, excluding elements at given indicies.
        /// </summary>
        public static IEnumerable<A> ExceptAt<A>(this IEnumerable<A> seq, params int[] indicies)
        {
            return seq.Where((_, i) => i.IsNotIn(indicies));
        }

        /// <summary>
        /// Randomizes elements in sequence. This will enumerate the entire sequence.
        /// </summary>
        public static IEnumerable<A> Shuffle<A>(this IEnumerable<A> seq)
        {
            var rand = new Random();
            var temp = seq.ToArray();

            foreach (var i in Enumerable.Range(0, temp.Length))
            {
                var j = rand.Next(i, temp.Length);
                yield return temp[j];
                temp[j] = temp[i];
            }
        }

        /// <summary>
        /// Sets every value in array to a particular value.
        /// </summary>
        public static A[] Fill<A>(this A[] array, A value)
        {
            for (var i = 0; i < array.Length; ++i)
            {
                array[i] = value;
            }

            return array;
        }
    }
}
