﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace KitchenSink.Collections
{
    public static class BitmappedTrie
    {
        public static IBitmappedTrie<A> Empty<A>() => new BitmappedTrieLeaf<A>(0, new A[16]);
        public static IBitmappedTrie<A> ToBitmappedTrie<A>(this IEnumerable<A> seq) =>
            seq.Aggregate(Empty<A>(), (acc, x) => acc.Suffix(x));
    }

    public interface IBitmappedTrie<A>
    {
        int Count { get; }
        A this[int index] { get; }
        IBitmappedTrie<A> Suffix(A value);
    }

    internal class BitmappedTrieBranch<A> : IBitmappedTrie<A>
    {
        private readonly int depth;
        private readonly IBitmappedTrie<A>[] array;

        internal BitmappedTrieBranch(int count, int depth, IBitmappedTrie<A>[] array)
        {
            Count = count;
            this.depth = depth;
            this.array = array;
        }

        public int Count { get; }

        public A this[int index]
        {
            get
            {
                if (index < 0 && index >= Count)
                {
                    throw new IndexOutOfRangeException(index.ToString());
                }

                var offset = depth * 4;
                var child = (index >> offset) & 15;
                return array[child][index & ~(-1 << offset)];
            }
        }

        public IBitmappedTrie<A> Suffix(A value)
        {
            if (Count < (16 << (depth * 4)))
            {
                var offset = depth * 4;
                var child = (Count >> offset) & 15;
                var newChildren = array.ToArray();
                newChildren[child] = newChildren[child].Suffix(value);
                return new BitmappedTrieBranch<A>(Count + 1, depth, newChildren);
            }

            // TODO: this is wrong? create all necessary levels
            var sibiling = new A[16];
            sibiling[0] = value;
            var parent = new IBitmappedTrie<A>[16];
            parent[0] = this;
            parent[1] = new BitmappedTrieLeaf<A>(1, sibiling);
            return new BitmappedTrieBranch<A>(Count + 1, depth + 1, parent);
        }
    }

    internal class BitmappedTrieLeaf<A> : IBitmappedTrie<A>
    {
        private readonly A[] array;

        internal BitmappedTrieLeaf(int count, A[] array)
        {
            Count = count;
            this.array = array;
        }

        public int Count { get; }

        public A this[int index] =>
            index >= 0 && index < Count
                ? array[index]
                : throw new IndexOutOfRangeException(index.ToString());

        public IBitmappedTrie<A> Suffix(A value)
        {
            if (Count < 16)
            {
                var newArray = array.ToArray();
                newArray[Count] = value;
                return new BitmappedTrieLeaf<A>(Count + 1, newArray);
            }

            var sibiling = new A[16];
            sibiling[0] = value;
            var parent = new IBitmappedTrie<A>[16];
            parent[0] = this;
            parent[1] = new BitmappedTrieLeaf<A>(1, sibiling);
            return new BitmappedTrieBranch<A>(Count + 1, 1, parent);
        }
    }
}
