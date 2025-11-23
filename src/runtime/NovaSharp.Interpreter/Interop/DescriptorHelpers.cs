namespace NovaSharp.Interpreter.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.Attributes;

    /// <summary>
    /// Helper extension methods used to simplify some parts of userdata descriptor implementations
    /// </summary>
    public static class DescriptorHelpers
    {
        /// <summary>
        /// Determines whether a
        /// <see cref="NovaSharpVisibleAttribute" /> or a <see cref="NovaSharpHiddenAttribute" />  is changing visibility of a member
        /// to scripts.
        /// </summary>
        /// <param name="mi">The member to check.</param>
        /// <returns>
        /// <c>true</c> if visibility is forced visible,
        /// <c>false</c> if visibility is forced hidden or the specified MemberInfo is null,
        /// <c>if no attribute was found</c>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">If both NovaSharpHiddenAttribute and NovaSharpVisibleAttribute are specified and they convey different messages.</exception>
        public static bool? GetVisibilityFromAttributes(this MemberInfo mi)
        {
            if (mi == null)
            {
                return false;
            }

            NovaSharpVisibleAttribute va = mi.GetCustomAttributes(true)
                .OfType<NovaSharpVisibleAttribute>()
                .SingleOrDefault();
            NovaSharpHiddenAttribute ha = mi.GetCustomAttributes(true)
                .OfType<NovaSharpHiddenAttribute>()
                .SingleOrDefault();

            if (va != null && ha != null && va.Visible)
            {
                throw new InvalidOperationException(
                    $"A member ('{mi.Name}') can't have discording NovaSharpHiddenAttribute and NovaSharpVisibleAttribute."
                );
            }
            else if (ha != null)
            {
                return false;
            }
            else if (va != null)
            {
                return va.Visible;
            }
            else
            {
                return null;
            }
        }

        public static bool IsDelegateType(this Type t)
        {
            return Framework.Do.IsAssignableFrom(typeof(Delegate), t);
        }

        /// <summary>
        /// Gets the visibility of the type as a string
        /// </summary>
        public static string GetClrVisibility(this Type type)
        {
#if NETFX_CORE
            var t = type.GetTypeInfo();
#else
            Type t = type;
#endif
            if (t.IsPublic || t.IsNestedPublic)
            {
                return "public";
            }

            if ((t.IsNotPublic && (!t.IsNested)) || (t.IsNestedAssembly))
            {
                return "internal";
            }

            if (t.IsNestedFamORAssem)
            {
                return "protected-internal";
            }

            if (t.IsNestedFamANDAssem || t.IsNestedFamily)
            {
                return "protected";
            }

            if (t.IsNestedPrivate)
            {
                return "private";
            }

            return "unknown";
        }

        /// <summary>
        /// Gets a string representing visibility of the given member type
        /// </summary>
        public static string GetClrVisibility(this FieldInfo info)
        {
            if (info.IsPublic)
            {
                return "public";
            }

            if (info.IsAssembly)
            {
                return "internal";
            }

            if (info.IsFamilyOrAssembly)
            {
                return "protected-internal";
            }

            if (info.IsFamilyAndAssembly || info.IsFamily)
            {
                return "protected";
            }

            if (info.IsPrivate)
            {
                return "private";
            }

            return "unknown";
        }

        /// <summary>
        /// Gets a string representing visibility of the given member type
        /// </summary>
        public static string GetClrVisibility(this PropertyInfo info)
        {
            MethodInfo gm = Framework.Do.GetGetMethod(info);
            MethodInfo sm = Framework.Do.GetSetMethod(info);

            string gv = (gm != null) ? GetClrVisibility(gm) : "private";
            string sv = (sm != null) ? GetClrVisibility(sm) : "private";

            if (gv == "public" || sv == "public")
            {
                return "public";
            }
            else if (gv == "internal" || sv == "internal")
            {
                return "internal";
            }
            else
            {
                return gv;
            }
        }

        /// <summary>
        /// Gets a string representing visibility of the given member type
        /// </summary>
        public static string GetClrVisibility(this MethodBase info)
        {
            if (info.IsPublic)
            {
                return "public";
            }

            if (info.IsAssembly)
            {
                return "internal";
            }

            if (info.IsFamilyOrAssembly)
            {
                return "protected-internal";
            }

            if (info.IsFamilyAndAssembly || info.IsFamily)
            {
                return "protected";
            }

            if (info.IsPrivate)
            {
                return "private";
            }

            return "unknown";
        }

        /// <summary>
        /// Determines whether the specified PropertyInfo is visible publicly (either the getter or the setter is public).
        /// </summary>
        /// <param name="pi">The PropertyInfo.</param>
        /// <returns></returns>
        public static bool IsPropertyInfoPublic(this PropertyInfo pi)
        {
            MethodInfo getter = Framework.Do.GetGetMethod(pi);
            MethodInfo setter = Framework.Do.GetSetMethod(pi);

            return (getter != null && getter.IsPublic) || (setter != null && setter.IsPublic);
        }

        /// <summary>
        /// Gets the list of metamethod names from attributes - in practice the list of metamethods declared through
        /// <see cref="NovaSharpUserDataMetamethodAttribute" /> .
        /// </summary>
        /// <param name="mi">The mi.</param>
        /// <returns></returns>
        public static List<string> GetMetaNamesFromAttributes(this MethodInfo mi)
        {
            return mi.GetCustomAttributes(typeof(NovaSharpUserDataMetamethodAttribute), true)
                .OfType<NovaSharpUserDataMetamethodAttribute>()
                .Select(a => a.Name)
                .ToList();
        }

        /// <summary>
        /// Gets the Types implemented in the assembly, catching the ReflectionTypeLoadException just in case..
        /// </summary>
        /// <param name="asm">The assebly</param>
        /// <returns></returns>
        public static Type[] SafeGetTypes(this Assembly asm)
        {
            try
            {
                return Framework.Do.GetAssemblyTypes(asm);
            }
            catch (ReflectionTypeLoadException)
            {
                return Array.Empty<Type>();
            }
        }

        /// <summary>
        /// Gets the name of a conversion method to be exposed to Lua scripts
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static string GetConversionMethodName(this Type type)
        {
            StringBuilder sb = new(type.Name);

            for (int i = 0; i < sb.Length; i++)
            {
                if (!char.IsLetterOrDigit(sb[i]))
                {
                    sb[i] = '_';
                }
            }

            return "__to" + sb.ToString();
        }

        /// <summary>
        /// Gets all implemented types by a given type
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllImplementedTypes(this Type t)
        {
            for (Type ot = t; ot != null; ot = Framework.Do.GetBaseType(ot))
            {
                yield return ot;
            }

            foreach (Type it in Framework.Do.GetInterfaces(t))
            {
                yield return it;
            }
        }

        /// <summary>
        /// Determines whether the string is a valid simple identifier (starts with letter or underscore
        /// and contains only letters, digits and underscores).
        /// </summary>
        public static bool IsValidSimpleIdentifier(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            if (str[0] != '_' && !char.IsLetter(str[0]))
            {
                return false;
            }

            for (int i = 1; i < str.Length; i++)
            {
                if (str[i] != '_' && !char.IsLetterOrDigit(str[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Converts the string to a valid simple identifier (starts with letter or underscore
        /// and contains only letters, digits and underscores).
        /// </summary>
        public static string ToValidSimpleIdentifier(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "_";
            }

            if (str[0] != '_' && !char.IsLetter(str[0]))
            {
                str = "_" + str;
            }

            StringBuilder sb = new(str);

            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] != '_' && !char.IsLetterOrDigit(sb[i]))
                {
                    sb[i] = '_';
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts the specified name from underscore_case to camelCase.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static string Camelify(string name)
        {
            StringBuilder sb = new(name.Length);

            bool first = true;
            bool lastWasUnderscore = false;
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];

                if (ch == '_')
                {
                    if (!first)
                    {
                        lastWasUnderscore = true;
                    }
                    continue;
                }

                if (first)
                {
                    sb.Append(char.ToLowerInvariant(ch));
                    first = false;
                    lastWasUnderscore = false;
                    continue;
                }

                if (lastWasUnderscore)
                {
                    sb.Append(char.ToUpperInvariant(ch));
                }
                else
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }

                lastWasUnderscore = false;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts the specified name from camelCase/PascalCase to SNAKE_CASE.
        /// </summary>
        public static string ToUpperUnderscore(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            StringBuilder sb = new(name.Length * 2);

            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];

                if (char.IsLetterOrDigit(ch))
                {
                    if (sb.Length > 0)
                    {
                        char prev = name[i - 1];

                        bool prevIsLetter = char.IsLetter(prev);
                        bool prevIsLowerOrDigit = char.IsLower(prev) || char.IsDigit(prev);
                        bool prevIsDigit = char.IsDigit(prev);
                        bool prevIsUpper = char.IsUpper(prev);
                        bool curIsUpper = char.IsUpper(ch);
                        bool curIsLetter = char.IsLetter(ch);
                        bool curIsDigit = char.IsDigit(ch);
                        bool nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                        bool nextIsLetter = i + 1 < name.Length && char.IsLetter(name[i + 1]);

                        if (
                            (curIsUpper && prevIsLowerOrDigit)
                            || (curIsUpper && prevIsUpper && nextIsLower)
                            || (curIsLetter && prevIsDigit)
                            || (curIsDigit && prevIsLetter && HasLetterAfterDigits(name, i))
                        )
                        {
                            sb.Append('_');
                        }
                    }

                    sb.Append(char.ToUpperInvariant(ch));
                }
                else
                {
                    if (sb.Length > 0 && sb[^1] != '_')
                    {
                        sb.Append('_');
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts the specified name to one with an uppercase first letter (something to Something).
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static string UpperFirstLetter(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                return char.ToUpperInvariant(name[0]) + name.Substring(1);
            }

            return name;
        }

        /// <summary>
        /// Normalizes consecutive uppercase runs so that only the first character remains uppercase.
        /// </summary>
        public static string NormalizeUppercaseRuns(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            string snake = ToUpperUnderscore(name);
            if (string.IsNullOrEmpty(snake))
            {
                return snake;
            }

            string[] parts = snake.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new(name.Length);

            foreach (string part in parts)
            {
                if (part.Length == 0)
                {
                    continue;
                }

                sb.Append(char.ToUpperInvariant(part[0]));

                for (int i = 1; i < part.Length; i++)
                {
                    sb.Append(char.ToLowerInvariant(part[i]));
                }
            }

            return sb.ToString();
        }

        private static bool HasLetterAfterDigits(string name, int index)
        {
            for (int i = index; i < name.Length; i++)
            {
                char ch = name[i];

                if (char.IsLetter(ch))
                {
                    return true;
                }

                if (!char.IsDigit(ch))
                {
                    break;
                }
            }

            return false;
        }
    }
}
