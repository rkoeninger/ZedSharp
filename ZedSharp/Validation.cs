﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZedSharp
{
    public static class Validation
    {
        public static Validation<A> Of<A>(A value)
        {
            return new Validation<A>(value);
        }
    }

    public struct Validation<A>
    {
        internal Validation(A value) : this(value, Enumerable.Empty<Exception>())
        {
        }

        private Validation(A value, IEnumerable<Exception> errors) : this()
        {
            Value = value;
            ErrorList = errors.ToList();
        }

        private A Value { get; set; }
        private List<Exception> ErrorList { get; set; }

        public IEnumerable<Exception> Errors
        {
            get { return ErrorList; }
        }

        public Validation<A> Check(Func<A, bool> f, String message = null)
        {
            return new Validation<A>(Value, f(Value) ? ErrorList : ErrorList.Concat(new[] { new ApplicationException(message ?? "") }));
        }

        public Validation<A> Check(Action<A> f)
        {
            try
            {
                f(Value);
                return new Validation<A>(Value, ErrorList);
            }
            catch (Exception exc)
            {
                return new Validation<A>(Value, ErrorList.Concat(new[] { exc }));
            }
        }

        public Unsure<A> ToUnsure()
        {
            var errors = ErrorList;
            return Unsure.If(Value, _ => errors.Count == 0);
        }
    }
}
