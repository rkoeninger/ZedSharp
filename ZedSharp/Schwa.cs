﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ZedSharp
{
    public static class Schwa
    {
        public static Expression Parse(String source)
        {
            var syntax = Syntax.Read(source);
            var env = new SymbolEnvironment();
            return syntax.Parse(env);
        }

        public static A Eval<A>(String source)
        {
            var expr = Parse(source);
            var lambda = Expression.Lambda<Func<A>>(expr);
            return lambda.Compile().Invoke();
        }
    }

    public class Syntax
    {
        public static Token Read(String str)
        {
            using (var reader = new StringReader(str))
                return new Syntax(reader).ReadToken();
        }

        public static List<Token> ReadAll(String str)
        {
            using (var reader = new StringReader(str))
                return new Syntax(reader).ReadAllTokens();
        }

        public static List<Token> ReadFile(String path)
        {
            using (var reader = File.OpenText(path))
                return new Syntax(reader, path).ReadAllTokens();
        }

        private readonly String _sourceUnit;
        private int _line = 1;
        private int _column = 1;
        private readonly TextReader _reader;
        private readonly char[] _unicodeHex = new char[8];

        public Syntax(TextReader reader, String sourceUnit = "Unknown")
        {
            _reader = reader;
            _sourceUnit = sourceUnit;
        }

        public List<Token> ReadAllTokens()
        {
            var tokens = new List<Token>();

            for (; ; )
            {
                SkipWhiteSpace();

                if (_reader.Peek() == -1)
                    break;

                tokens.Add(ReadToken());
            }

            return tokens;
        }

        public Token ReadToken()
        {
            SkipWhiteSpace();
            var location = CurrentLocation;
            var peekByte = _reader.Peek();

            switch (peekByte)
            {
                case -1:
                    Fail("Unexpected end of file");
                    return null;
                case '(':
                    Skip(); // Pull '(' off the reader
                    return new Combo(ReadUntilComboEnd(), location);
                case '[':
                    Skip(); // Pull '[' off the reader
                    return new SquareCombo(ReadUntilComboEnd(), location);
                case '{':
                    Skip(); // Pull '{' off the reader
                    return new CurlyCombo(ReadUntilComboEnd(), location);
                case ')':
                case ']':
                case '}':
                    Skip(); // Pull ')' or ']' or '}' off the reader
                    return new ComboEnd(location);
                case '\"':
                case '\'':
                    return new Atom(ReadStringLiteral((char) peekByte), location);
                case '«':
                    return new Atom(ReadNestedStringLiteral(), location);
                case '»':
                    throw new SchwaLexException(location, "Closing nested string character unexpected");
                default:
                    return new Atom(ReadLiteral(), location);
            }
        }

        private class ComboEnd : Token
        {
            internal ComboEnd(Location location) : base(location)
            {
            }

            public override Expression Parse(SymbolEnvironment env)
            {
                throw new InvalidOperationException("ComboEnd cannot be parsed");
            }
        }

        private void Skip()
        {
            _reader.Read();
            _column++;
        }

        private void SkipWhiteSpace()
        {
            for (; ; )
            {
                var b = _reader.Peek();

                if (b == -1)
                    break;

                var ch = (char)b;

                if (!Char.IsWhiteSpace(ch))
                    break;

                if (ch == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else if (ch != '\r')
                {
                    _column++;
                }

                _reader.Read();
            }
        }

        private String ReadLiteral()
        {
            var builder = new StringBuilder();

            for (;;)
            {
                var b = _reader.Peek();

                if (b == -1)
                    break;

                var ch = (char)b;

                if (ch == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else if (ch != '\r')
                {
                    _column++;
                }

                if (Char.IsWhiteSpace(ch) || ch.IsIn(')', '(', ']', '[', '}', '{', '\"', '\'', '«', '»'))
                    break;

                builder.Append(ch);
                _reader.Read();
            }

            return builder.ToString();
        }

        private String ReadStringLiteral(char quoteChar)
        {
            var builder = new StringBuilder();
            builder.Append((char)_reader.Read()); // pull the opening qoute

            for (;;)
            {
                var b = _reader.Read();

                if (b == -1)
                    Fail("Incomplete string literal");

                var ch = (char)b;

                if (ch == '\\')
                {
                    builder.Append(ReadEscapeSequence());
                    continue;
                }
                
                if (ch == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else if (ch != '\r')
                {
                    _column++;
                }

                builder.Append(ch);

                if (ch == quoteChar)
                    break;
            }

            return builder.ToString();
        }

        private String ReadNestedStringLiteral()
        {
            var builder = new StringBuilder();
            builder.Append((char)_reader.Read()); // pull the opening qoute
            var nestDepth = 1;

            for (;;)
            {
                var b = _reader.Read();

                if (b == -1)
                    Fail("Incomplete string literal");

                var ch = (char)b;

                if (ch == '\\')
                {
                    builder.Append(ReadEscapeSequence());
                    continue;
                }
                
                if (ch == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else if (ch != '\r')
                {
                    _column++;
                }

                builder.Append(ch);

                if (ch == '«')
                {
                    nestDepth++;
                }
                else if (ch == '»')
                {
                    nestDepth--;

                    if (nestDepth == 0)
                        break;
                }
            }

            return builder.ToString();
        }

        private String ReadEscapeSequence()
        {
            // Backslash that starts the escape sequence has already been read at this point
            // Full escape sequence should be read off of the reader when this method returns
            var escapeChar = (char)_reader.Read();

            switch (escapeChar)
            {
                case '\\': return "\\";
                case '0':  return "\0";
                case '\'': return "\'";
                case '\"': return "\"";
                case 'n':  return "\n";
                case 'r':  return "\r";
                case 't':  return "\t";
                case 'v':  return "\v";
                case 'b':  return "\b";
                case 'f':  return "\f";
                case 'a':  return "\a";
                case 'u':  return new String(ReadUnicode16BitEscapeSequence(), 1);
                case 'U':  return ReadUnicode32BitEscapeSequence();
                case 'x':  return new String(ReadUnicodeVariableLengthEscapeSequence(), 1);
            }

            Fail("Invalid character escape sequence: \"\\" + escapeChar + "\"");
            return null;
        }

        private char ReadUnicode16BitEscapeSequence()
        {
            if (_reader.Read(_unicodeHex, 0, 4) != 4)
                Fail("Incomplete 16-bit unicode code point escape sequence");

            var utf16Bits = Int16.Parse(new String(_unicodeHex, 0, 4), NumberStyles.HexNumber);
            return (char)utf16Bits;
        }

        private String ReadUnicode32BitEscapeSequence()
        {
            if (_reader.Read(_unicodeHex, 0, 8) != 8)
                Fail("Incomplete 32-bit unicode code point escape sequence");

            var utf32Bits = Int32.Parse(new String(_unicodeHex, 0, 8), NumberStyles.HexNumber);
            return Char.ConvertFromUtf32(utf32Bits);
        }

        private char ReadUnicodeVariableLengthEscapeSequence()
        {
            _unicodeHex.Fill((char) 0);
            var len = 0;

            while (len < 4)
            {
                var digitByte = _reader.Peek();

                if (digitByte == -1)
                    Fail("Incomplete unicode code point escape sequence");

                var digitChar = (char)digitByte;

                if (!IsHexDigit(digitChar))
                    break;

                // Pull char off reader now that we're sure we need it
                _unicodeHex[len++] = (char)_reader.Read();
            }

            if (len == 0)
                Fail("No hex chars following \\x escape sequence");

            var utf16Bits = Int16.Parse(new String(_unicodeHex, 0, len), NumberStyles.HexNumber);
            return (char)utf16Bits;
        }

        private static bool IsHexDigit(char ch)
        {
            return Char.IsDigit(ch)
                   || (ch >= 'A' && ch <= 'F')
                   || (ch >= 'a' && ch <= 'f');
        }

        private List<Token> ReadUntilComboEnd()
        {
            var tokens = new List<Token>();

            for (var token = ReadToken(); !(token is ComboEnd); token = ReadToken())
            {
                if (token == null)
                    Fail("Unexpected end of combo");

                tokens.Add(token);
            }

            return tokens;
        }

        private Location CurrentLocation
        {
            get { return new Location(_sourceUnit, _line, _column); }
        }

        private void Fail(String message)
        {
            throw new SchwaLexException(CurrentLocation, message);
        }
    }

    public class Location
    {
        public Location(String file, int line, int column)
        {
            File = file;
            Line = line;
            Column = column;
        }

        public String File { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        public override string ToString()
        {
            return String.Format("Line {0}, Column {1} in {2}", Line, Column, File);
        }
    }

    public abstract class Token
    {
        protected Token(Location location)
        {
            Location = location;
        }

        public Location Location { get; private set; }

        public abstract Expression Parse(SymbolEnvironment env);

        internal static readonly ConstantExpression Zero = Expression.Constant(0);
        internal static readonly ConstantExpression Null = Expression.Constant(null);
        internal static readonly ConstantExpression False = Expression.Constant(false);
        internal static readonly ConstantExpression True = Expression.Constant(true);

    }

    internal class Atom : Token
    {
        public Atom(String atom, Location location) : base(location)
        {
            Literal = atom;
        }

        public String Literal { get; private set; }

        public override string ToString()
        {
            return Literal;
        }

        private static readonly Regex IntRegex = new Regex(@"^[-+]?[0-9]+u?l?$");
        private static readonly Regex FloatRegex = new Regex(@"^[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?[fm]?$");

        private static String ButLast(String str, int len)
        {
            return str.Substring(0, str.Length - len);
        }

        public override Expression Parse(SymbolEnvironment env)
        {
            if (Literal == "null")           return Null;
            if (Literal == "true")           return True;
            if (Literal == "false")          return False;
            if (Literal[0].IsIn('\"', '\'')) return Expression.Constant(StripQuotes(Literal, false));
            if (Literal[0] == '«')           return Expression.Constant(StripQuotes(Literal, true));

            if (IntRegex.IsMatch(Literal))
            {
                if (Literal.EndsWith("ul")) return Expression.Constant(UInt64.Parse(ButLast(Literal, 2)));
                if (Literal.EndsWith("u"))  return Expression.Constant(UInt32.Parse(ButLast(Literal, 1)));
                if (Literal.EndsWith("l"))  return Expression.Constant(Int64.Parse(ButLast(Literal, 1)));

                return Expression.Constant(Int32.Parse(Literal));
            }

            if (FloatRegex.IsMatch(Literal))
            {
                if (Literal.EndsWith("f")) return Expression.Constant(Single.Parse(ButLast(Literal, 1)));
                if (Literal.EndsWith("m")) return Expression.Constant(Decimal.Parse(ButLast(Literal, 1)));

                return Expression.Constant(Double.Parse(Literal));
            }

            if (Literal.Contains("."))
            {
                var parts = Literal.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);

                // TODO: identify parts[0] as variable or class
                // identify each item in the chain as property, field or method
            }

            return env.Get(Literal).OrElseThrow("Variable not defined");
        }

        private static String StripQuotes(String str, bool stripNestedQuotes)
        {
            str = str.Substring(1, str.Length - 2);

            if (stripNestedQuotes)
                str = str.Replace('«', '\"').Replace('»', '\"');

            return String.Intern(str);
        }
    }

    internal class CurlyCombo : Token
    {
        public CurlyCombo(List<Token> tokens, Location location) : base(location)
        {
            Tokens = tokens;
        }

        public List<Token> Tokens { get; private set; }

        public override string ToString()
        {
            return "{" + String.Join(" ", Tokens) + "}";
        }

        public override Expression Parse(SymbolEnvironment env)
        {
            var elementExprs = Tokens.Select(x => x.Parse(env)).Partition(2).Select(x => x.ToArray()).ToArray();
            var keyType = Combo.GetCommonBaseType(elementExprs.Select(x => x.ElementAt(0).Type).ToArray());
            var valType = Combo.GetCommonBaseType(elementExprs.Select(x => x.ElementAt(1).Type).ToArray());
            var dictType = typeof(Dictionary<object, object>).GetGenericTypeDefinition().MakeGenericType(keyType, valType);
            var ctor = dictType.GetConstructor(new Type[0]);
            if (ctor == null) throw new Exception("Dictionary doesn't have a no-arg constructor somehow");
            var addMethod = dictType.GetMethod("Add");
            if (addMethod == null) throw new Exception("Dictionary doesn't have an Add method somehow");
            return Expression.ListInit(
                Expression.New(ctor),
                elementExprs.Select(x => Expression.ElementInit(addMethod, x[0], x[1])));
        }
    }

    internal class SquareCombo : Token
    {
        public SquareCombo(List<Token> tokens, Location location) : base(location)
        {
            Tokens = tokens;
        }

        public List<Token> Tokens { get; private set; }

        public override string ToString()
        {
            return "[" + String.Join(" ", Tokens) + "]";
        }

        public override Expression Parse(SymbolEnvironment env)
        {
            var elementExprs = Tokens.Select(x => x.Parse(env)).ToArray();
            return BuildListExpression(elementExprs);
        }

        public static Expression BuildListExpression(IEnumerable<Expression> exprs)
        {
            var exprArray = exprs.ToArray();
            var elementType = Combo.GetCommonBaseType(exprArray.Select(x => x.Type).ToArray());
            var listType = typeof(List<>).MakeGenericType(elementType);
            var ctor = listType.GetConstructor(new Type[0]);
            if (ctor == null) throw new Exception("List doesn't have no-arg constructor somehow");
            return Expression.ListInit(Expression.New(ctor), exprArray);
        }
    }

    internal class Combo : Token
    {
        public Combo(List<Token> tokens, Location location) : base(location)
        {
            Tokens = tokens;
        }

        public List<Token> Tokens { get; private set; }

        public override string ToString()
        {
            return "(" + String.Join(" ", Tokens) + ")";
        }

        public override Expression Parse(SymbolEnvironment env)
        {
            if (Tokens.Count == 0) throw new SchwaParseException(Location, "Empty parens don't mean anything");

            if (Tokens[0] is Atom)
            {
                return ParseOp((Atom) Tokens[0], env);
            }

            throw new NotImplementedException();
        }

        private Expression ParseOp(Atom atom, SymbolEnvironment env)
        {
            switch (atom.Literal)
            {
                case "!":  return UnaryOp(env, Tokens, Expression.Not);
                case "~":  return UnaryOp(env, Tokens, Expression.OnesComplement);
                case "++": return UnaryOp(env, Tokens, Expression.Increment);
                case "--": return UnaryOp(env, Tokens, Expression.Decrement);
                case "<":  return CompareOp(env, Tokens, Expression.LessThan);
                case "<=": return CompareOp(env, Tokens, Expression.LessThanOrEqual);
                case ">":  return CompareOp(env, Tokens, Expression.GreaterThan);
                case ">=": return CompareOp(env, Tokens, Expression.GreaterThanOrEqual);
                case "<<": return BinaryOp(env, Tokens, Expression.LeftShift);
                case ">>": return BinaryOp(env, Tokens, Expression.RightShift);
                case "??": return BinaryOp(env, Tokens, Expression.Coalesce);
                case "==": return EqualityOp(env, Tokens);
                case "!=": return InequalityOp(env, Tokens);
                case "if":
                case "?:": return TernaryOp(env, Tokens, Expression.Condition);
                case "&":  return NaryOp(env, Tokens, Expression.And);
                case "|":  return NaryOp(env, Tokens, Expression.Or);
                case "and":
                case "&&": return NaryOp(env, Tokens, Expression.AndAlso);
                case "or":
                case "||": return NaryOp(env, Tokens, Expression.OrElse);
                case "xor":
                case "^":  return NaryOp(env, Tokens, Expression.ExclusiveOr);
                case "+":  return NaryOp(env, Tokens, Expression.Add);
                case "-":  return NaryOp(env, Tokens, Expression.Subtract);
                case "*":  return NaryOp(env, Tokens, Expression.Multiply);
                case "/":  return NaryOp(env, Tokens, Expression.Divide);
                case "%":  return NaryOp(env, Tokens, Expression.Modulo);
                case "#":  // use for indexer access (get only, set not supported)
                {
                    var target = Tokens[1].Parse(env);
                    var args = Tokens.Skip(2).Select(x => x.Parse(env));
                    var type = target.Type;
                    var indexer = type.GetProperties()
                        .Where(x => x.GetIndexParameters().Any())
                        .FirstMaybe(x => ParamsMatch(x.GetIndexParameters(), args))
                        .OrElseThrow("No indexer found that matches those arguments");
                    return Expression.Call(target, indexer.GetMethod, args);
                }
                case "is":
                {
                    var typeAtom = (Atom)Tokens[1];
                    var type = ParseType(typeAtom.Literal);
                    return Expression.TypeIs(Tokens[2].Parse(env), type);
                }
                case "as":
                {
                    var typeAtom = (Atom)Tokens[1];
                    var type = ParseType(typeAtom.Literal);
                    return Expression.TypeAs(Tokens[2].Parse(env), type);
                }
                case "cast":
                {
                    var typeAtom = (Atom)Tokens[1];
                    var type = ParseType(typeAtom.Literal);
                    return Expression.Convert(Tokens[2].Parse(env), type);
                }
                case "typeof":
                {
                    var typeAtom = (Atom)Tokens[1];
                    var type = ParseType(typeAtom.Literal);
                    return Expression.Constant(type);
                }
                case "default":
                {
                    var typeAtom = (Atom)Tokens[1];
                    var type = ParseType(typeAtom.Literal);
                    return Expression.Default(type);
                }
                case "new":
                {
                    var typeAtom = (Atom)Tokens[1];
                    var type = ParseType(typeAtom.Literal);
                    var args = Tokens.Skip(2).Select(x => x.Parse(env)).ToArray();
                    var ctor = type.GetConstructor(args.Select(x => x.Type).ToArray());
                    if (ctor == null) throw new Exception("Type \"" + type + "\" does not have that constructor");
                    return Expression.New(ctor, args);
                }
                case "=>":
                {
                    var paramsToken = (Combo)Tokens[1];
                    var paramsExprs = paramsToken.Tokens
                        .Select(x => ((Combo) x).With(y =>
                            env.Define(
                                ((Atom)y.Tokens[1]).Literal,
                                ParseType(((Atom)y.Tokens[0]).Literal))))
                        .ToArray();
                    var bodyExpr = Tokens[2].Parse(env);
                    return Expression.Lambda(bodyExpr, paramsExprs);
                }
                case ".":
                {
                    throw new NotImplementedException();
                }
                // TODO: select, where, orderby, groupby, distinct
                // TODO: static methods
                // TODO: instance methods
                // TODO: property access chains
                // TODO: void instance methods return target object
            }

            throw new NotImplementedException();
        }

        private static bool ParamsMatch(IEnumerable<ParameterInfo> paramz, IEnumerable<Expression> args)
        {
            var paramArray = paramz.ToArray();
            var argArray = args.ToArray();

            return paramArray.Zip(argArray, Tuple.Create)
                .All(x => x.Item2.Type.IsAssignableTo(x.Item1.ParameterType));
        }

        private static readonly Type Nullable = typeof (Nullable<>);

        public static Type ParseType(String str)
        {
            if (str.EndsWith("?"))
            {
                str = str.Substring(0, str.Length - 1);
                var type = ParseType(str);
                return Nullable.MakeGenericType(type);
            }

            return TypeKeywords.GetMaybe(str).OrElse(null);
        }

        public static readonly IReadOnlyDictionary<string, Type> TypeKeywords = Dictionary.Of(
            "bool",    typeof(bool),
            "char",    typeof(char),
            "byte",    typeof(byte),
            "sbyte",   typeof(sbyte),
            "short",   typeof(short),
            "ushort",  typeof(ushort),
            "int",     typeof(int),
            "uint",    typeof(uint),
            "long",    typeof(long),
            "ulong",   typeof(ulong),
            "float",   typeof(float),
            "double",  typeof(double),
            "decimal", typeof(decimal),
            "string",  typeof(string),
            "object",  typeof(object));

        private static bool AllEqual(IEnumerable<object> vals)
        {
            var array = vals.ToArray();

            if (array.Length < 2)
                throw new Exception();

            var set = new HashSet<object> { array.First() };
            return array.Skip(1).All(val => !set.Add(val));
        }

        private static bool AllInequal(IEnumerable<object> vals)
        {
            var array = vals.ToArray();

            if (array.Length < 2)
                throw new Exception();

            var set = new HashSet<object>();
            return array.All(set.Add);
        }

        public static Expression BuildListExpression(IEnumerable<Expression> exprs)
        {
            var exprArray = exprs.ToArray();
            var elementType = Combo.GetCommonBaseType(exprArray.Select(x => x.Type).ToArray());
            var listType = typeof(List<>).MakeGenericType(elementType);
            var ctor = listType.GetConstructor(new Type[0]);
            if (ctor == null) throw new Exception("List doesn't have no-arg constructor somehow");
            var listExpr = Expression.ListInit(Expression.New(ctor), exprArray);
            var castMethod = typeof (Enumerable).GetMethod("Cast", new[] {typeof (IEnumerable<>)}).MakeGenericMethod(typeof(object));
            var asEnumerableMethod = typeof (Enumerable).GetMethod("AsEnumerable").MakeGenericMethod(elementType);
            return Expression.Call(castMethod, listExpr);
        }

        private static Expression EqualityOp(SymbolEnvironment env, IEnumerable<Token> tokens)
        {
            Expression<Func<IEnumerable<object>, bool>> f = xs => AllEqual(xs);
            var exprs = tokens.Skip(1).Select(x => x.Parse(env));
            return Expression.Invoke(f, Seq.Of(BuildListExpression(exprs)));
        }

        private static Expression InequalityOp(SymbolEnvironment env, IEnumerable<Token> tokens)
        {
            Expression<Func<IEnumerable<object>, bool>> f = xs => AllInequal(xs);
            var exprs = tokens.Skip(1).Select(x => x.Parse(env));
            return Expression.Invoke(f, Seq.Of(BuildListExpression(exprs)));
        }

        private static Expression CompareOp(SymbolEnvironment env, IEnumerable<Token> tokens, Func<Expression, Expression, Expression> f)
        {
            var exprs = tokens.Skip(1).Select(x => x.Parse(env));
            return exprs.OverlappingPartition2().Select(x => f(x.Item1, x.Item2)).Aggregate(Expression.AndAlso);
        }

        private static Expression NaryOp(SymbolEnvironment env, IEnumerable<Token> tokens, Func<Expression, Expression, Expression> f)
        {
            return tokens.Skip(1).Select(x => x.Parse(env)).Aggregate(f);
        }

        private Expression TernaryOp(SymbolEnvironment env, IEnumerable<Token> tokens, Func<Expression, Expression, Expression, Expression> f)
        {
            var array = tokens.Skip(1).ToArray();

            if (array.Length != 3)
                throw new SchwaParseException(Location, "Ternary operator requires exactly 3 arguments");

            return f(array[0].Parse(env), array[1].Parse(env), array[2].Parse(env));
        }

        private Expression BinaryOp(SymbolEnvironment env, IEnumerable<Token> tokens, Func<Expression, Expression, Expression> f)
        {
            var array = tokens.Skip(1).ToArray();

            if (array.Length != 2)
                throw new SchwaParseException(Location, "Binary operator requires exactly 2 arguments");

            return f(array[0].Parse(env), array[1].Parse(env));
        }

        private Expression UnaryOp(SymbolEnvironment env, IEnumerable<Token> tokens, Func<Expression, Expression> f)
        {
            var array = tokens.Skip(1).ToArray();

            if (array.Length != 1)
                throw new SchwaParseException(Location, "Unary operator requires exactly 1 arguments");

            return f(array[0].Parse(env));
        }

        public static Type GetCommonBaseType(Type[] types)
        {
            if (types.Length == 0)
                throw new Exception("Explicit type declaration required");

            if (types.Length == 1)
                return types[0];

            return types.Aggregate(GetCommonBaseType);
        }

        public static Type GetCommonBaseType(Type x, Type y)
        {
            if (x == y || y.IsAssignableTo(x))
                return x;

            if (x.IsAssignableTo(y))
                return y;

            return GetCommonBaseType(y, x.BaseType);
        }
    }

    public class SymbolEnvironment
    {
        private readonly ConcurrentDictionary<String, ParameterExpression> Symbols = new ConcurrentDictionary<String, ParameterExpression>();
        private readonly Maybe<SymbolEnvironment> ContainingScope;

        public SymbolEnvironment()
        {
            ContainingScope = Maybe<SymbolEnvironment>.None;
        }

        public SymbolEnvironment(SymbolEnvironment parent)
        {
            ContainingScope = Maybe.Some(parent);
        }

        public Maybe<ParameterExpression> Get(String name)
        {
            return Symbols.GetMaybe(name).OrEval(() => ContainingScope.Select(x => x.Get(name)).Flatten());
        }

        public ParameterExpression Define(String name, Type type)
        {
            // TODO: what if it's already defined?
            //       already defined in this scope? parent scope?
            return Symbols.GetOrAdd(name, _ => Expression.Variable(type, name));
        }

        public bool IsDefined(String name)
        {
            return Symbols.GetMaybe(name)
                .OrEval(() => ContainingScope.Select(x => x.Symbols.GetMaybe(name)).Flatten())
                .HasValue;
        }
    }

    public class SchwaLexException : ApplicationException
    {
        public SchwaLexException(Location location, String message) : base(message)
        {
            Location = location;
        }

        public Location Location { get; private set; }
    }

    public class SchwaParseException : ApplicationException
    {
        public SchwaParseException(Location location, String message) : base(message)
        {
            Location = location;
        }

        public Location Location { get; private set; }
    }
}
