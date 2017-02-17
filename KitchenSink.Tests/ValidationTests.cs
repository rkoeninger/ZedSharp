﻿using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace KitchenSink.Tests
{
    [TestFixture]
    public class ValidationTests
    {
        [Test]
        public void ValidationIses()
        {
            Assert.IsTrue(Verify.That(PersonA)
                .Is(x => x != null)
                .Cut()
                .Is(x => x.Address != null)
                .Cut()
                .Is(x => x.Address.City != null)
                .HasErrors);

            Expect.Error(() => Verify.That(PersonA)
                .Is(x => x != null)
                .Is(x => x.Address != null)
                .Is(x => x.Address.City != null));

            Assert.IsFalse(Verify.That(PersonD)
                .Is(x => x != null)
                .Is(x => x.Address != null)
                .Is(x => x.Address.City != null)
                .HasErrors);
        }

        [Test]
        public void ValidationPropertyChain()
        {
            // Some of these verifications are purposely redundant to confirm that the expression destructuring process works
            // ReSharper disable EqualExpressionComparison
            Assert.IsFalse(Verify.That(() => PersonA.Address.City));
            Assert.IsFalse(Verify.That(() => PersonB.Address.City));
            Assert.IsTrue(Verify.That(() => PersonC.Address.City));
            Assert.IsFalse(Verify.That(() => PersonC.Address.City == "springfield"));
            Assert.IsTrue(Verify.That(() => PersonC.Address.City == PersonC.Address.City));
            Assert.IsFalse(Verify.That(() => PersonB.Address.City == PersonB.Address.City));
            Assert.IsFalse(Verify.That(() => PersonA.Address.City == PersonA.Address.City));
            Assert.IsTrue(Verify.That(() => PersonB.Address.City.SkipVerify()));
            Assert.IsTrue(Verify.That(() => Person0.Address.City.NoVerify()));
            // ReSharper restore EqualExpressionComparison
        }

        private static readonly Person Person0 = null;
        private static readonly Person PersonA = new Person("John", "Smith", null);
        private static readonly Person PersonB = new Person("John", "Smith", new Address("123", null));
        private static readonly Person PersonC = new Person("John", "Smith", new Address("123", "qwerty"));
        private static readonly Person PersonD = new Person("John", "Smith", new Address("123", "qwerty"), new List<Person> { PersonA, PersonB, PersonC });

        public class Person
        {
            public Person(String firstName, String lastName, Address address, List<Person> friends = null)
            {
                FirstName = firstName;
                LastName = lastName;
                Address = address;
                Friends = friends ?? new List<Person>();
            }

            public String FirstName { get; private set; }
            public String LastName { get; private set; }
            public Address Address { get; private set; }
            public List<Person> Friends { get; private set; }
        }

        public class Address
        {
            public Address(String street, String city)
            {
                Street = street;
                City = city;
            }

            public String Street { get; private set; }
            public String City { get; private set; }
        }
    }
}