using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SMPro.Inspector.EditorTools
{
    public readonly struct Getter<T>
    {
        readonly Func<object, object, T> read;

        Getter(Func<object, object, T> read, string error)
        {
            this.read = read;
            Error = error;
        }

        internal static Getter<T> From(Func<object, object, T> read) => new(read, null);
        internal static Getter<T> Fail(string error) => new(null, error);

        public bool IsValid => read != null;
        public string Error { get; }

        public T Get(object owner) => read != null ? read(owner, null) : default;
        public T Get(object owner, object argument) => read != null ? read(owner, argument) : default;
    }

    public static class MemberResolver
    {
        const BindingFlags DeclaredMembers = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        static readonly MethodInfo ObjectEquals = typeof(object).GetMethod(nameof(Equals), BindingFlags.Public | BindingFlags.Static);
        static readonly Dictionary<(Type owner, string path, Type result), object> values = new();
        static readonly Dictionary<(Type owner, string key), Getter<bool>> conditions = new();

        public static Getter<T> Value<T>(Type owner, string path)
        {
            var key = (owner, path, typeof(T));
            if (values.TryGetValue(key, out object cached)) return (Getter<T>)cached;
            Getter<T> getter = BuildValue<T>(owner, path);
            values[key] = getter;
            return getter;
        }

        public static Getter<bool> Condition(Type owner, string path, params object[] compareTo)
        {
            if (compareTo == null || compareTo.Length == 0) return Value<bool>(owner, path);
            var key = (owner, ConditionKey(path, compareTo));
            if (!conditions.TryGetValue(key, out Getter<bool> getter))
            {
                getter = BuildCondition(owner, path, compareTo);
                conditions[key] = getter;
            }
            return getter;
        }

        static Getter<T> BuildValue<T>(Type owner, string path)
        {
            if (!TryAccess(owner, path, out Access access, out string error)) return Getter<T>.Fail(error);
            Expression body = access.Body;
            if (access.Negate)
            {
                if (body.Type != typeof(bool)) return Getter<T>.Fail($"'!{access.Path}' needs a bool, but '{access.Path}' is {Describe(body.Type)}");
                body = Expression.Not(body);
            }
            if (!TryConvert(body, typeof(T), out Expression converted))
                return Getter<T>.Fail($"'{access.Path}' is {Describe(body.Type)}, expected {Describe(typeof(T))}");
            return Compile<T>(converted, access);
        }

        static Getter<bool> BuildCondition(Type owner, string path, object[] compareTo)
        {
            if (!TryAccess(owner, path, out Access access, out string error)) return Getter<bool>.Fail(error);
            if (!TryCompare(access.Body, access.Path, compareTo, out Expression test, out error)) return Getter<bool>.Fail(error);
            if (access.Negate) test = Expression.Not(test);
            return Compile<bool>(test, access);
        }

        static Getter<T> Compile<T>(Expression body, Access access)
        {
            Expression<Func<object, object, T>> lambda = Expression.Lambda<Func<object, object, T>>(body, access.Owner, access.Argument);
            try
            {
                return Getter<T>.From(lambda.Compile());
            }
            catch (Exception)
            {
                return Getter<T>.From(lambda.Compile(true));
            }
        }

        readonly struct Access
        {
            public Access(ParameterExpression owner, ParameterExpression argument, Expression body, string path, bool negate)
            {
                Owner = owner;
                Argument = argument;
                Body = body;
                Path = path;
                Negate = negate;
            }

            public ParameterExpression Owner { get; }
            public ParameterExpression Argument { get; }
            public Expression Body { get; }
            public string Path { get; }
            public bool Negate { get; }
        }

        static bool TryAccess(Type owner, string path, out Access access, out string error)
        {
            access = default;
            error = null;
            if (owner == null)
            {
                error = "No owner type";
                return false;
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                error = $"Empty member name on {owner.Name}";
                return false;
            }

            string trimmed = path.Trim();
            bool negate = trimmed[0] == '!';
            if (negate) trimmed = trimmed.Substring(1).Trim();
            string[] segments = trimmed.Split('.');

            ParameterExpression ownerParameter = Expression.Parameter(typeof(object), "owner");
            ParameterExpression argumentParameter = Expression.Parameter(typeof(object), "argument");
            var nullChecks = new List<Expression>();
            Type currentType = owner;
            Expression current = null;

            for (int i = 0; i < segments.Length; i++)
            {
                string name = segments[i].Trim();
                bool last = i == segments.Length - 1;
                MemberInfo member = name.Length > 0 ? FindMember(currentType, name) : null;
                if (member == null)
                {
                    error = $"No member '{name}' on {Describe(currentType)}";
                    return false;
                }

                Expression instance = null;
                if (!IsStatic(member))
                {
                    if (current == null) instance = Expression.Convert(ownerParameter, owner);
                    else
                    {
                        instance = current;
                        if (!currentType.IsValueType) nullChecks.Add(current);
                    }
                }

                switch (member)
                {
                    case FieldInfo field:
                        current = Expression.Field(instance, field);
                        break;
                    case PropertyInfo property:
                        if (property.GetGetMethod(true) == null)
                        {
                            error = $"'{name}' has no getter";
                            return false;
                        }
                        current = Expression.Property(instance, property);
                        break;
                    case MethodInfo method:
                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length == 0)
                        {
                            current = Expression.Call(instance, method);
                            break;
                        }
                        if (parameters.Length > 1)
                        {
                            error = $"'{name}' takes {parameters.Length} parameters; at most 1 is supported";
                            return false;
                        }
                        if (!last)
                        {
                            error = $"'{name}' takes a parameter, so it must be the last part of '{trimmed}'";
                            return false;
                        }
                        Type parameterType = parameters[0].ParameterType;
                        if (parameterType.IsByRef)
                        {
                            error = $"'{name}' has a ref or out parameter";
                            return false;
                        }
                        current = Expression.Call(instance, method, ArgumentAs(argumentParameter, parameterType));
                        break;
                }
                currentType = current.Type;
            }

            Expression body = current;
            for (int i = nullChecks.Count - 1; i >= 0; i--)
            {
                Expression link = nullChecks[i];
                body = Expression.Condition(Expression.Equal(link, Expression.Constant(null, link.Type)), Expression.Default(body.Type), body);
            }

            access = new Access(ownerParameter, argumentParameter, body, trimmed, negate);
            return true;
        }

        static Expression ArgumentAs(ParameterExpression argument, Type type) =>
            Expression.Condition(Expression.Equal(argument, Expression.Constant(null)), Expression.Default(type), Expression.Convert(argument, type));

        internal static MethodInfo FindMethod(Type type, string name)
        {
            if (type == null || string.IsNullOrWhiteSpace(name)) return null;
            return FindMember(type, name.Trim()) as MethodInfo;
        }

        static MemberInfo FindMember(Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, DeclaredMembers);
                if (field != null) return field;

                foreach (PropertyInfo property in current.GetProperties(DeclaredMembers))
                    if (property.Name == name && property.GetIndexParameters().Length == 0) return property;

                MethodInfo best = null;
                int bestCount = int.MaxValue;
                foreach (MethodInfo method in current.GetMethods(DeclaredMembers))
                {
                    if (method.Name != name || method.IsGenericMethodDefinition) continue;
                    int count = method.GetParameters().Length;
                    if (count < bestCount)
                    {
                        best = method;
                        bestCount = count;
                    }
                }
                if (best != null) return best;
            }
            return null;
        }

        static bool IsStatic(MemberInfo member) => member switch
        {
            FieldInfo field => field.IsStatic,
            PropertyInfo property => property.GetGetMethod(true)?.IsStatic ?? false,
            MethodInfo method => method.IsStatic,
            _ => false
        };

        static bool TryConvert(Expression body, Type target, out Expression result)
        {
            result = null;
            if (body.Type == target)
            {
                result = body;
                return true;
            }
            if (body.Type == typeof(void)) return false;
            if (target.IsAssignableFrom(body.Type) || (IsNumeric(body.Type) && IsNumeric(target)))
            {
                result = Expression.Convert(body, target);
                return true;
            }
            return false;
        }

        static bool TryCompare(Expression body, string path, object[] compareTo, out Expression test, out string error)
        {
            test = null;
            error = null;
            Type type = body.Type;

            if (type.IsEnum)
            {
                long mask = 0;
                Expression raw = Expression.Convert(body, typeof(long));
                foreach (object value in compareTo)
                {
                    if (value == null || value.GetType() != type)
                    {
                        error = CannotCompare(path, type, value);
                        return false;
                    }
                    long number = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                    mask |= number;
                    Expression equals = Expression.Equal(raw, Expression.Constant(number));
                    test = test == null ? equals : Expression.OrElse(test, equals);
                }
                if (type.IsDefined(typeof(FlagsAttribute), false) && mask != 0)
                    test = Expression.NotEqual(Expression.And(raw, Expression.Constant(mask)), Expression.Constant(0L));
                return true;
            }

            foreach (object value in compareTo)
            {
                if (!TryComparable(value, type, out object comparable))
                {
                    error = CannotCompare(path, type, value);
                    return false;
                }
                Expression equals = EqualTo(body, comparable);
                test = test == null ? equals : Expression.OrElse(test, equals);
            }
            return true;
        }

        static bool TryComparable(object value, Type type, out object comparable)
        {
            comparable = null;
            if (value == null) return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
            Type valueType = value.GetType();
            if (type.IsAssignableFrom(valueType))
            {
                comparable = value;
                return true;
            }
            if (IsNumeric(type) && IsNumeric(valueType))
            {
                comparable = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }

        static Expression EqualTo(Expression body, object value)
        {
            Type type = body.Type;
            MethodInfo equality = FindEqualityOperator(type);
            if (equality != null)
            {
                Type operand = equality.GetParameters()[0].ParameterType;
                return Expression.Equal(Expression.Convert(body, operand), Expression.Constant(value, operand), false, equality);
            }
            if (type.IsPrimitive) return Expression.Equal(body, Expression.Constant(value, type));
            return Expression.Call(ObjectEquals, Expression.Convert(body, typeof(object)), Expression.Constant(value, typeof(object)));
        }

        static MethodInfo FindEqualityOperator(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                MethodInfo method = current.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new[] { current, current }, null);
                if (method != null) return method;
            }
            return null;
        }

        static bool IsNumeric(Type type)
        {
            if (type.IsEnum) return false;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        static string ConditionKey(string path, object[] compareTo)
        {
            var key = new StringBuilder(path);
            foreach (object value in compareTo)
                key.Append('|').Append(value?.GetType().FullName).Append(':').Append(Convert.ToString(value, CultureInfo.InvariantCulture));
            return key.ToString();
        }

        static string CannotCompare(string path, Type type, object value) =>
            $"Can't compare '{path}' ({Describe(type)}) with {DescribeValue(value)}";

        static string DescribeValue(object value) => value switch
        {
            null => "null",
            string text => $"\"{text}\" (string)",
            _ => $"{Convert.ToString(value, CultureInfo.InvariantCulture)} ({Describe(value.GetType())})"
        };

        static string Describe(Type type)
        {
            if (type == typeof(void)) return "void";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(string)) return "string";
            if (type == typeof(object)) return "object";
            return type.Name;
        }
    }
}
