﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ZedSharp
{
    public static class Maybe
    {
        public static Maybe<A> Of<A>(Nullable<A> nullable) where A : struct
        {
            return nullable.HasValue ? new Maybe<A>(nullable.Value) : new Maybe<A>();
        }

        public static Maybe<A> Of<A>(A val)
        {
            return new Maybe<A>(val);
        }

        public static Maybe<A> Some<A>(A val)
        {
            if (val == null)
                throw new ArgumentNullException("Can't create Maybe.Some<" + typeof(A) + "> with null value");

            return new Maybe<A>(val);
        }

        public static Maybe<A> None<A>()
        {
            return new Maybe<A>();
        }

        public static Maybe<A> Try<A>(Func<A> f)
        {
            try
            {
                return Of(f());
            }
            catch
            {
                return None<A>();
            }
        }

        public static Maybe<A> If<A>(A val, Func<A, bool> f)
        {
            return If(val, f, Z.Id);
        }

        public static Maybe<B> If<A, B>(A val, Func<A, bool> f, Func<A, B> convert)
        {
            return f(val) ? Of(convert(val)) : None<B>();
        }

        public static Lazy<Maybe<B>> LazyIf<A, B>(A val, Func<A, bool> f, Func<A, B> selector)
        {
            return new Lazy<Maybe<B>>(() => Maybe.If(val, f, selector));
        }

        public static Maybe<A> Flatten<A>(this Maybe<Maybe<A>> maybe)
        {
            return maybe.HasValue ? maybe.Value : None<A>();
        }

        public static Maybe<Int32> ToInt(this String s)
        {
            int i;
            return Int32.TryParse(s, out i) ? Maybe.Of(i) : Maybe.None<Int32>();
        }

        public static Maybe<Double> ToDouble(this String s)
        {
            double d;
            return Double.TryParse(s, out d) ? Maybe.Of(d) : Maybe.None<Double>();
        }

        public static Maybe<XDocument> ToXml(this String s)
        {
            return Try(() => XDocument.Parse(s));
        }

        public static Maybe<A> Get<A>(this IList<A> list, int index)
        {
            return Try(() => list[index]);
        }

        public static Maybe<B> Get<A, B>(this IDictionary<A, B> dict, A key)
        {
            return Try(() => dict[key]);
        }

        public static Maybe<A> MaybeFirst<A>(this IEnumerable<A> seq)
        {
            return Try(() => seq.First());
        }

        public static Maybe<A> MaybeLast<A>(this IEnumerable<A> seq)
        {
            return Try(() => seq.Last());
        }

        public static Maybe<A> MaybeSingle<A>(this IEnumerable<A> seq)
        {
            return Try(() => seq.Single());
        }

        public static Maybe<A> MaybeElementAt<A>(this IEnumerable<A> seq, int index)
        {
            return Try(() => seq.ElementAt(index));
        }

        public static IEnumerable<A> WhereSome<A>(this IEnumerable<Maybe<A>> seq)
        {
            foreach (var x in seq)
                if (x.HasValue)
                    yield return x.Value;
        }

        public static Maybe<IEnumerable<A>> Sequence<A>(this IEnumerable<Maybe<A>> seq)
        {
            var list = new List<A>();

            foreach (var x in seq)
                if (x.HasValue)
                    list.Add(x.Value);
                else
                    return Maybe.None<IEnumerable<A>>();

            return Maybe.Of(list).Cast<IEnumerable<A>>();
        }

        public static Maybe<IEnumerable<A>> NonEmpty<A>(Maybe<IEnumerable<A>> maybe)
        {
            return maybe.Where(x => x.Any());
        }

        public static Maybe<List<A>> NonEmpty<A>(Maybe<List<A>> maybe)
        {
            return maybe.Where(x => x.Count > 0);
        }

        public static Maybe<String> NonEmpy(Maybe<String> maybe)
        {
            return maybe.Where(x => !String.IsNullOrEmpty(x));
        }

        public static Maybe<String> NonWhitespace(Maybe<String> maybe)
        {
            return maybe.Where(x => !String.IsNullOrWhiteSpace(x));
        }

        public static Maybe<int> NonNegative(Maybe<int> maybe)
        {
            return maybe.Where(x => x >= 0);
        }

        public static Maybe<int> Positive(Maybe<int> maybe)
        {
            return maybe.Where(x => x > 0);
        }
    }
    
    /// <summary>
    /// A null-encapsulating wrapper.
    /// A Maybe might not have a value, not a reference to an Maybe can not be null.
    /// </summary>
    /// <typeparam name="A"></typeparam>
    public struct Maybe<A>
    {
        public static implicit operator Maybe<A>(A val)
        {
            return Maybe.Of(val);
        }

        internal Maybe(A val) : this()
        {
            Value = val;
            HasValue = val != null;
        }

        internal A Value { get; set; }
        public bool HasValue { get; private set; }

        public Type InnerType { get { return typeof(A); } }

        public Maybe<B> Select<B>(Func<A, B> f)
        {
            var val = Value;
            return HasValue ? Maybe.Try(() => f(val)) : Maybe.None<B>();
        }

        public Maybe<B> SelectMany<B>(Func<A, Maybe<B>> f)
        {
            return Select(f).Flatten();
        }

        public Maybe<A> Where(Func<A, bool> f)
        {
            return HasValue ? Maybe.If(Value, f) : this;
        }

        public Maybe<C> Join<B, C>(Maybe<B> that, Func<A, B, C> f)
        {
            var val = Value;
            return HasValue ? that.Select(x => f(val, x)) : Maybe.None<C>();
        }

        /// <summary>Attempts cast, returning Some or None.</summary>
        public Maybe<B> Cast<B>()
        {
            var val = Value;
            return Maybe.Try(() => (B) (Object) val);
        }

        public A OrElse(A other)
        {
            return HasValue ? Value : other;
        }

        public Maybe<A> OrEval(Func<A> f)
        {
            return HasValue ? this : Maybe.Try(f);
        }

        public Maybe<A> OrEval(Func<Maybe<A>> f)
        {
            return HasValue ? this : Maybe.Try(f).Flatten();
        }

        public Maybe<A> Or(Maybe<A> maybe)
        {
            return HasValue ? this : maybe;
        }

        public Maybe<A> OrReverse(Maybe<A> maybe)
        {
            return HasValue ? maybe : this;
        }

        public A OrThrow(String message)
        {
            if (! HasValue)
                throw new Exception(message);

            return Value;
        }

        public A OrThrow(Exception e)
        {
            if (!HasValue)
                throw e;

            return Value;
        }
        
        public Maybe<A> ForEach(Action<A> f)
        {
            if (HasValue)
                f(Value);

            return this;
        }

        public List<A> ToList()
        {
            return HasValue ? Z.List(Value) : new List<A>();
        }

        public A[] ToArray()
        {
            return HasValue ? Z.Array(Value) : new A[0];
        }

        public override string ToString()
        {
            return HasValue ? Value.ToString() : "None";
        }

        public override int GetHashCode()
        {
            return HasValue ? Value.GetHashCode() ^ (int)0x0a5a5a5a : 1;
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(other, this))
                return true;

            if (other == null || !(other is Maybe<A>))
                return false;

            var that = (Maybe<A>) other;
 
            return (this.HasValue && that.HasValue && Object.Equals(this.Value, that.Value))
                || (!this.HasValue && !that.HasValue);
        }
    }
}
