﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZedSharp
{
    /// <summary>Simple IoC container that makes use of DefaultImplementation and DefaultImplementationOf attributes.</summary>
    public class Needs
    {
        public static Needs Of(params Object[] vals)
        {
            var needs = new Needs();

            foreach (var val in vals)
                needs.Tree.Set(val.GetType(), val);

            return needs;
        }

        private readonly TypeTree<Object> Tree = new TypeTree<Object>();

        public Needs Set<T>(Object impl)
        {
            Tree.Set<T>(impl);
            return this;
        }

        public Maybe<T> Get<T>() where T : class
        {
            var t = typeof(T);
            return Tree.Get(t)
                .OrEvalMany(t, GetDefaultImpl)
                .Cast<T>();
        }

        private static Maybe<Object> GetDefaultImpl(Type @interface)
        {
            return GetDeclaredImplementingClass(@interface)
                .OrEvalMany(@interface, FindDeclaringImplementingClass)
                .Select(Activator.CreateInstance);
        }

        private static Maybe<Type> GetDeclaredImplementingClass(Type @interface)
        {
            return @interface.GetAttribute<DefaultImplementationAttribute>()
                .Select(x => x.ImplementingClass);
        }

        private static Maybe<Type> FindDeclaringImplementingClass(Type @interface)
        {
            var impls = Types.All(t => t.HasAttribute<DefaultImplementationOfAttribute>(a => a.ImplementedInterface == @interface)).ToList();

            if (impls.Count > 1)
                throw new Exception("Multiple implementations found: " + impls.Concat(", "));

            return impls.SingleMaybe();
        }
    }

    /// <summary>Declares the default implementation of this interface.</summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class DefaultImplementationAttribute : Attribute
    {
        public DefaultImplementationAttribute(Type @class)
        {
            if ((!(@class.IsClass || @class.IsValueType)) || @class.IsAbstract)
                throw new ArgumentException(@class + " is not a concrete class or struct type");

            ImplementingClass = @class;
        }

        public Type ImplementingClass { get; private set; }

        public bool IsProperlyDefinedOn(Type implementedInterface)
        {
            return ImplementingClass.IsAssignableTo(implementedInterface);
        }
    }

    /// <summary>Declares that this class or struct type is the default implementation of the specified interface type.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public class DefaultImplementationOfAttribute : Attribute
    {
        public DefaultImplementationOfAttribute(Type @interface)
        {
            if (!@interface.IsInterface)
                throw new ArgumentException(@interface + " is not an interface type");

            ImplementedInterface = @interface;
        }

        public Type ImplementedInterface { get; private set; }

        public bool IsProperlyDefinedOn(Type implementingClass)
        {
            return implementingClass.IsAssignableTo(ImplementedInterface);
        }
    }

    public static class Inject
    {
        public static readonly IConsole LiveConsole = new LiveConsole();
        public static readonly IFileSystem LiveFileSystem = new LiveFileSystem();
        public static readonly IClock LiveClock = new LiveClock();

        public static IClock StoppedClock(ZonedDateTime time)
        {
            return new StoppedClock(time);
        }

        public static IFileSystem VirtualFileSystem()
        {
            return new VirtualFileSystem();
        }

        public static IConsole ScriptedConsole(StringReader input, StringWriter output = null)
        {
            return new ScriptedConsole(input, output);
        }

        public static readonly Needs StandardNeeds = Needs.Of(
            LiveClock,
            LiveConsole,
            LiveFileSystem);
    }

    [DefaultImplementation(typeof(LiveConsole))]
    public interface IConsole
    {
        String ReadLine();
        void WriteLine(Object s);
        void WriteLine(String format, params Object[] args);
    }

    internal class LiveConsole : IConsole
    {
        public String ReadLine()
        {
            return Console.ReadLine();
        }

        public void WriteLine(Object s)
        {
            Console.WriteLine(s);
        }

        public void WriteLine(String format, params Object[] args)
        {
            Console.WriteLine(format, args);
        }
    }

    internal class ScriptedConsole : IConsole
    {
        public ScriptedConsole(StringReader input, StringWriter output)
        {
            Input = input;
            Output = output;
        }

        private readonly StringReader Input;
        private readonly StringWriter Output;

        public String ReadLine()
        {
            return Input.ReadLine();
        }

        public void WriteLine(Object s)
        {
            Output.WriteLine(s);
        }

        public void WriteLine(String format, params Object[] args)
        {
            Output.WriteLine(format, args);
        }
    }

    [DefaultImplementation(typeof(LiveFileSystem))]
    public interface IFileSystem
    {
        String ReadAllText(String path);
        void WriteAllText(String path, String contents);
    }

    internal class LiveFileSystem : IFileSystem
    {
        public String ReadAllText(String path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllText(String path, String contents)
        {
            File.WriteAllText(path, contents);
        }
    }

    internal class VirtualFileSystem : IFileSystem
    {
        private readonly Dictionary<String, String> Files = new Dictionary<String, String>();

        public String ReadAllText(String path)
        {
            try
            {
                return Files[path];
            }
            catch (KeyNotFoundException)
            {
                throw new FileNotFoundException(path);
            }
        }

        public void WriteAllText(String path, String contents)
        {
            Files[path] = contents;
        }
    }

    [DefaultImplementation(typeof(LiveClock))]
    public interface IClock
    {
        ZonedDateTime Now { get; }
    }

    internal class LiveClock : IClock
    {
        public ZonedDateTime Now
        {
            get { return new ZonedDateTime(DateTime.UtcNow, TimeZoneInfo.Utc); }
        }
    }

    internal class StoppedClock : IClock
    {
        public StoppedClock(ZonedDateTime time)
        {
            Time = time;
        }

        private readonly ZonedDateTime Time;

        public ZonedDateTime Now
        {
            get { return Time; }
        }
    }
}
