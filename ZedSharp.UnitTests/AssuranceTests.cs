﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZedSharp;
using ZedSharp.Test;

namespace ZedSharp.UnitTests
{
    [TestClass]
    public class AssuranceTests
    {
        [TestMethod]
        public void SureThrowsOnNull()
        {
            RequiresSureString("");
            Expect.Error(() => { RequiresSureString(null); });
            Expect.Error(() => { String ns = null; RequiresSureString(ns); });

            Expect.CompileFail(Common.Wrap(@"Action<Sure<String>> a = x => {}; String s; a(s)"), Common.ZedDll);
            Expect.CompileFail(Common.Wrap(@"Action<Sure<String>> a = x => {}; Sure<String> ss; a(ss)"), Common.ZedDll);
        }

        public static void RequiresSureString(Sure<String> ss) {}

        [TestMethod]
        public void UnsureWrappers()
        {
            String ns = null;
            String s = "";
            Exception ne = null;
            Exception e = new Exception();
            AssertIsNone(Unsure.Of(ns));
            AssertIsSome(Unsure.Of(s));
            AssertIsNone(Unsure.None<String>());
            AssertIsNone(Unsure.Error<String>(ne));
            AssertIsError(Unsure.Error<String>(e));
        }

        [TestMethod]
        public void UnsureParsing()
        {
            AssertIsSome("123".ToInt());
            AssertIsNone("!@#".ToInt());
            AssertIsNone("123.123".ToInt());
            AssertIsSome("123".ToDouble());
            AssertIsNone("!@#".ToDouble());
            AssertIsSome("123.123".ToDouble());
        }

        [TestMethod]
        public void UnsureJoining()
        {
            Assert.AreEqual(Unsure.Of(3), Unsure.Of(1).Join(Unsure.Of(2), (x, y) => x + y));
            Assert.AreEqual(Unsure.None<int>(), Unsure.None<int>().Join(Unsure.Of(2), (x, y) => x + y));
            Assert.AreEqual(Unsure.None<int>(), Unsure.Of(1).Join(Unsure.None<int>(), (x, y) => x + y));
            Assert.AreEqual(Unsure.None<int>(), Unsure.None<int>().Join(Unsure.None<int>(), (x, y) => x + y));
        }

        [TestMethod]
        public void UnsureCasting()
        {
            Assert.IsTrue(Unsure.Of("").Cast<string>().HasValue);
            Assert.IsTrue(Unsure.Of("").Cast<object>().HasValue);
            Assert.IsFalse(Unsure.Of("").Cast<int>().HasValue);
        }

        [TestMethod]
        public void UnsureEnumerableExtensions()
        {
            AssertIsSome(new [] {0}.UnsureFirst());
            AssertIsError(new int[0].UnsureFirst());
            AssertIsSome(new [] {0}.UnsureLast());
            AssertIsError(new int[0].UnsureLast());
            AssertIsSome(new [] {0}.UnsureSingle());
            AssertIsError(new int[0].UnsureSingle());
            AssertIsSome(new [] {0,0,0,0}.UnsureElementAt(2));
            AssertIsError(new [] {0,0,0,0}.UnsureElementAt(5));
            AssertIsError(new [] {0,0,0,0}.UnsureElementAt(-1));
            Assert.AreEqual(5, new [] {"#", "3", "2", "1", "e", "3", "r", "3"}.Select(x => x.ToInt()).WhereSure().Count());
            AssertIsNone(new [] {"#", "3", "2", "1", "e", "3", "r", "3"}.Select(x => x.ToInt()).Sequence());
            AssertIsSome(new [] {"9", "3", "2", "1", "6", "3", "5", "3"}.Select(x => x.ToInt()).Sequence());
        }

        [TestMethod]
        public void ValidationTests()
        {
            var v = Ensure.Of("abcdefg")
                .Check(x => x.StartsWith("abc"))
                .Check(x => x.EndsWith("abc"))
                .Check(x => { if (x.Length < 10) throw new Exception("asdfasdfa"); });
            Assert.AreEqual(2, v.Errors.Count());
        }

        public void AssertIsSome<A>(Unsure<A> unsure)
        {
            Assert.IsTrue(unsure.HasValue);
            Assert.IsFalse(unsure.HasError);
        }

        public void AssertIsNone<A>(Unsure<A> unsure)
        {
            Assert.IsFalse(unsure.HasValue);
            Assert.IsFalse(unsure.HasError);
        }

        public void AssertIsError<A>(Unsure<A> unsure)
        {
            Assert.IsFalse(unsure.HasValue);
            Assert.IsTrue(unsure.HasError);
        }
    }
}