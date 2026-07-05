namespace WallstopStudios.NovaSharp.Interop.Generator
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    /// <summary>
    /// Emits the first deterministic generated interop shape for valid LuaObject types.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class LuaInteropSourceGenerator : IIncrementalGenerator
    {
        private const string LuaObjectMetadataName = "NovaSharp.LuaObjectAttribute";
        private const string GeneratedCodeTool = "WallstopStudios.NovaSharp.Interop.Generator";
        private const string GeneratedCodeVersion = "3.0.0";

        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<LuaObjectModel> luaObjects = context
                .SyntaxProvider.ForAttributeWithMetadataName(
                    LuaObjectMetadataName,
                    static (node, _) => node is TypeDeclarationSyntax,
                    static (attributeContext, cancellationToken) =>
                        CreateModel(attributeContext, cancellationToken)
                )
                .Where(static model => model != null);

            context.RegisterSourceOutput(
                luaObjects,
                static (sourceContext, model) =>
                    sourceContext.AddSource(
                        model.HintName,
                        SourceText.From(EmitSource(model), Encoding.UTF8)
                    )
            );
        }

        private static LuaObjectModel CreateModel(
            GeneratorAttributeSyntaxContext context,
            System.Threading.CancellationToken cancellationToken
        )
        {
            INamedTypeSymbol type = context.TargetSymbol as INamedTypeSymbol;
            if (type == null || type.TypeParameters.Length > 0 || type.ContainingType != null)
            {
                return null;
            }

            TypeDeclarationSyntax declaration = null;
            foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
            {
                SyntaxNode syntax = reference.GetSyntax(cancellationToken);
                TypeDeclarationSyntax candidate = syntax as TypeDeclarationSyntax;
                if (candidate != null)
                {
                    declaration = candidate;
                    break;
                }
            }

            if (!CanGenerateCompanionPartial(type, declaration))
            {
                return null;
            }

            SortedSet<string> members = new SortedSet<string>(StringComparer.Ordinal);
            SortedDictionary<string, EnumModel> enums = new SortedDictionary<string, EnumModel>(
                StringComparer.Ordinal
            );
            foreach (ISymbol member in type.GetMembers())
            {
                if (member.IsImplicitlyDeclared || HasAttribute(member, AttributeNames.LuaIgnore))
                {
                    continue;
                }

                bool exposed = false;
                foreach (AttributeData attribute in member.GetAttributes())
                {
                    string luaName = GetLuaName(member, attribute);
                    if (luaName == null)
                    {
                        continue;
                    }

                    exposed = true;
                    members.Add(luaName);
                }

                if (exposed)
                {
                    AddReferencedEnums(member, enums);
                }
            }

            string namespaceName = type.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : type.ContainingNamespace.ToDisplayString();
            return new LuaObjectModel(
                namespaceName,
                GetTypeKeyword(type),
                type.Name,
                GetLuaObjectName(type),
                type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                ToArray(members),
                ToArray(enums, members)
            );
        }

        private static bool CanGenerateCompanionPartial(
            INamedTypeSymbol type,
            TypeDeclarationSyntax declaration
        )
        {
            if (declaration == null)
            {
                return false;
            }

            if (
                !(declaration is ClassDeclarationSyntax)
                && !(declaration is StructDeclarationSyntax)
                && !(declaration is RecordDeclarationSyntax)
            )
            {
                return false;
            }

            if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            return declaration.Modifiers.Any(SyntaxKind.PartialKeyword);
        }

        private static string GetTypeKeyword(INamedTypeSymbol type)
        {
            if (type.IsRecord)
            {
                return type.TypeKind == TypeKind.Struct ? "record struct" : "record";
            }

            return type.TypeKind == TypeKind.Struct ? "struct" : "class";
        }

        private static void AddReferencedEnums(
            ISymbol member,
            SortedDictionary<string, EnumModel> enums
        )
        {
            IFieldSymbol field = member as IFieldSymbol;
            if (field != null)
            {
                AddEnumType(field.Type, enums);
                return;
            }

            IPropertySymbol property = member as IPropertySymbol;
            if (property != null)
            {
                AddEnumType(property.Type, enums);
                foreach (IParameterSymbol parameter in property.Parameters)
                {
                    AddEnumType(parameter.Type, enums);
                }

                return;
            }

            IMethodSymbol method = member as IMethodSymbol;
            if (method != null)
            {
                if (!method.ReturnsVoid)
                {
                    AddEnumType(method.ReturnType, enums);
                }

                foreach (IParameterSymbol parameter in method.Parameters)
                {
                    AddEnumType(parameter.Type, enums);
                }
            }
        }

        private static void AddEnumType(ITypeSymbol type, SortedDictionary<string, EnumModel> enums)
        {
            if (type == null || type.TypeKind != TypeKind.Enum)
            {
                return;
            }

            INamedTypeSymbol enumType = type as INamedTypeSymbol;
            if (enumType == null || HasAttribute(enumType, AttributeNames.LuaIgnore))
            {
                return;
            }

            string displayName = enumType.ToDisplayString(
                SymbolDisplayFormat.CSharpErrorMessageFormat
            );
            if (enums.ContainsKey(displayName))
            {
                return;
            }

            List<EnumMemberModel> members = new List<EnumMemberModel>();
            foreach (ISymbol member in enumType.GetMembers())
            {
                IFieldSymbol field = member as IFieldSymbol;
                if (
                    field == null
                    || field.IsImplicitlyDeclared
                    || !field.HasConstantValue
                    || HasAttribute(field, AttributeNames.LuaIgnore)
                )
                {
                    continue;
                }

                members.Add(new EnumMemberModel(field.Name, CreateLuaValueExpression(field)));
            }

            enums.Add(displayName, new EnumModel(displayName, enumType.Name, members.ToArray()));
        }

        private static string[] ToArray(SortedSet<string> values)
        {
            string[] result = new string[values.Count];
            values.CopyTo(result);
            return result;
        }

        private static EnumModel[] ToArray(
            SortedDictionary<string, EnumModel> values,
            SortedSet<string> reservedNames
        )
        {
            Dictionary<string, int> tableNameCounts = new Dictionary<string, int>(
                StringComparer.Ordinal
            );
            HashSet<string> usedNames = new HashSet<string>(reservedNames, StringComparer.Ordinal);
            foreach (KeyValuePair<string, EnumModel> pair in values)
            {
                IncrementCount(tableNameCounts, pair.Value.TableName);
            }

            EnumModel[] result = new EnumModel[values.Count];
            int index = 0;
            foreach (KeyValuePair<string, EnumModel> pair in values)
            {
                EnumModel value = pair.Value;
                string tableName = CreateUniqueEnumTableName(value, tableNameCounts, usedNames);
                result[index++] = new EnumModel(value.DisplayName, tableName, value.Members);
            }

            return result;
        }

        private static string CreateUniqueEnumTableName(
            EnumModel value,
            Dictionary<string, int> tableNameCounts,
            HashSet<string> usedNames
        )
        {
            string tableName =
                tableNameCounts[value.TableName] > 1 || usedNames.Contains(value.TableName)
                    ? value.DisplayName
                    : value.TableName;
            if (usedNames.Add(tableName))
            {
                return tableName;
            }

            string prefix = string.Concat("enum:", value.DisplayName);
            tableName = prefix;
            int suffix = 2;
            while (!usedNames.Add(tableName))
            {
                tableName = string.Concat(
                    prefix,
                    "#",
                    suffix.ToString(CultureInfo.InvariantCulture)
                );
                suffix++;
            }

            return tableName;
        }

        private static void IncrementCount(Dictionary<string, int> values, string key)
        {
            if (values.TryGetValue(key, out int count))
            {
                values[key] = count + 1;
                return;
            }

            values.Add(key, 1);
        }

        private static string CreateLuaValueExpression(IFieldSymbol field)
        {
            INamedTypeSymbol enumType = field.ContainingType;
            if (enumType == null || enumType.EnumUnderlyingType == null)
            {
                return "global::NovaSharp.LuaValue.Nil";
            }

            object value = field.ConstantValue;
            if (IsUnsignedEnumUnderlyingType(enumType.EnumUnderlyingType))
            {
                ulong unsignedValue = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                if (unsignedValue <= long.MaxValue)
                {
                    return string.Concat(
                        "global::NovaSharp.LuaValue.FromInteger(",
                        unsignedValue.ToString(CultureInfo.InvariantCulture),
                        "L)"
                    );
                }

                return string.Concat(
                    "global::NovaSharp.LuaValue.FromNumber(",
                    unsignedValue.ToString(CultureInfo.InvariantCulture),
                    "d)"
                );
            }

            long signedValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            if (signedValue == long.MinValue)
            {
                return "global::NovaSharp.LuaValue.FromInteger(global::System.Int64.MinValue)";
            }

            return string.Concat(
                "global::NovaSharp.LuaValue.FromInteger(",
                signedValue.ToString(CultureInfo.InvariantCulture),
                "L)"
            );
        }

        private static bool IsUnsignedEnumUnderlyingType(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private static string EmitSource(LuaObjectModel model)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            if (model.NamespaceName.Length > 0)
            {
                builder.Append("namespace ");
                builder.AppendLine(model.NamespaceName);
                builder.AppendLine("{");
                builder.Append("    ");
            }

            builder.Append("partial ");
            builder.Append(model.TypeKeyword);
            builder.Append(' ');
            builder.AppendLine(model.TypeName);
            builder.Append(model.NamespaceName.Length > 0 ? "    {" : "{");
            builder.AppendLine();
            AppendGeneratedCodeAttribute(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            builder.AppendLine(
                "private static global::NovaSharp.LuaValue __NovaSharpGeneratedDispatch(global::System.ReadOnlySpan<global::NovaSharp.LuaValue> args, string memberName)"
            );
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            builder.AppendLine("{");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("_ = args;");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("switch (memberName)");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("{");
            foreach (string member in model.Members)
            {
                AppendIndent(builder, model.NamespaceName.Length > 0 ? 4 : 3);
                builder.Append("case ");
                AppendStringLiteral(builder, member);
                builder.AppendLine(":");
            }

            if (model.Members.Length > 0)
            {
                AppendIndent(builder, model.NamespaceName.Length > 0 ? 5 : 4);
                builder.AppendLine("break;");
            }

            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("}");
            builder.AppendLine();
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("return global::NovaSharp.LuaValue.Nil;");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            builder.AppendLine("}");
            builder.AppendLine();
            if (model.ReferencedEnums.Length > 0)
            {
                AppendEnumRegistrationMethod(builder, model);
            }

            AppendGeneratedCodeAttribute(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            builder.AppendLine("private static string __NovaSharpGeneratedManifest");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            builder.AppendLine("{");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("get");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("{");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 4 : 3);
            builder.Append("return ");
            AppendStringLiteral(builder, CreateManifest(model));
            builder.AppendLine(";");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("}");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            builder.AppendLine("}");
            builder.Append(model.NamespaceName.Length > 0 ? "    }" : "}");
            builder.AppendLine();
            if (model.NamespaceName.Length > 0)
            {
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        private static void AppendEnumRegistrationMethod(
            StringBuilder builder,
            LuaObjectModel model
        )
        {
            int memberIndent = model.NamespaceName.Length > 0 ? 2 : 1;
            int bodyIndent = model.NamespaceName.Length > 0 ? 3 : 2;

            AppendGeneratedCodeAttribute(builder, memberIndent);
            AppendIndent(builder, memberIndent);
            builder.AppendLine(
                "private static void __NovaSharpGeneratedRegisterEnumTables(global::NovaSharp.LuaEngine engine, global::NovaSharp.LuaTable destination)"
            );
            AppendIndent(builder, memberIndent);
            builder.AppendLine("{");
            AppendNullCheck(builder, bodyIndent, "engine");
            AppendNullCheck(builder, bodyIndent, "destination");
            builder.AppendLine();

            for (int i = 0; i < model.ReferencedEnums.Length; i++)
            {
                EnumModel enumModel = model.ReferencedEnums[i];
                string tableVariable = string.Concat(
                    "enumTable",
                    i.ToString(CultureInfo.InvariantCulture)
                );
                AppendIndent(builder, bodyIndent);
                builder.Append("global::NovaSharp.LuaTable ");
                builder.Append(tableVariable);
                builder.Append(" = engine.CreateTable(0, ");
                builder.Append(enumModel.Members.Length.ToString(CultureInfo.InvariantCulture));
                builder.AppendLine(");");
                foreach (EnumMemberModel member in enumModel.Members)
                {
                    AppendIndent(builder, bodyIndent);
                    builder.Append(tableVariable);
                    builder.Append(".Set(");
                    AppendStringLiteral(builder, member.Name);
                    builder.Append(", ");
                    builder.Append(member.LuaValueExpression);
                    builder.AppendLine(");");
                }

                AppendIndent(builder, bodyIndent);
                builder.Append("destination.Set(");
                AppendStringLiteral(builder, enumModel.TableName);
                builder.Append(", ");
                builder.Append(tableVariable);
                builder.AppendLine(".ToValue());");
                if (i + 1 < model.ReferencedEnums.Length)
                {
                    builder.AppendLine();
                }
            }

            AppendIndent(builder, memberIndent);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        private static void AppendNullCheck(StringBuilder builder, int indent, string parameterName)
        {
            AppendIndent(builder, indent);
            builder.Append("if (");
            builder.Append(parameterName);
            builder.AppendLine(" == null)");
            AppendIndent(builder, indent);
            builder.AppendLine("{");
            AppendIndent(builder, indent + 1);
            builder.Append("throw new global::System.ArgumentNullException(nameof(");
            builder.Append(parameterName);
            builder.AppendLine("));");
            AppendIndent(builder, indent);
            builder.AppendLine("}");
        }

        private static string CreateManifest(LuaObjectModel model)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("object:");
            builder.Append(model.LuaName);
            builder.Append("|type:");
            builder.Append(model.TypeDisplayName);
            foreach (string member in model.Members)
            {
                builder.Append("|member:");
                builder.Append(member);
            }

            foreach (EnumModel enumModel in model.ReferencedEnums)
            {
                builder.Append("|enum:");
                builder.Append(enumModel.DisplayName);
            }

            return builder.ToString();
        }

        private static void AppendGeneratedCodeAttribute(StringBuilder builder, int indent)
        {
            AppendIndent(builder, indent);
            builder.Append("[global::System.CodeDom.Compiler.GeneratedCode(");
            AppendStringLiteral(builder, GeneratedCodeTool);
            builder.Append(", ");
            AppendStringLiteral(builder, GeneratedCodeVersion);
            builder.AppendLine(")]");
        }

        private static void AppendIndent(StringBuilder builder, int count)
        {
            for (int i = 0; i < count; i++)
            {
                builder.Append("    ");
            }
        }

        private static void AppendStringLiteral(StringBuilder builder, string value)
        {
            builder.Append('@');
            builder.Append('"');
            foreach (char character in value)
            {
                builder.Append(character);
                if (character == '"')
                {
                    builder.Append('"');
                }
            }

            builder.Append('"');
        }

        private static string GetLuaObjectName(INamedTypeSymbol type)
        {
            foreach (AttributeData attribute in type.GetAttributes())
            {
                if (!IsAttribute(attribute, AttributeNames.LuaObject))
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Length == 1)
                {
                    object value = attribute.ConstructorArguments[0].Value;
                    if (value == null || value is string)
                    {
                        string name = value as string;
                        if (IsValidLuaName(name))
                        {
                            return name;
                        }
                    }
                }
            }

            return type.Name;
        }

        private static string GetLuaName(ISymbol member, AttributeData attribute)
        {
            if (IsAttribute(attribute, AttributeNames.LuaMember))
            {
                return GetNamedMemberName(attribute, member.Name);
            }

            return IsAttribute(attribute, AttributeNames.LuaMetamethod)
                ? GetMetamethodName(attribute)
                : null;
        }

        private static string GetNamedMemberName(AttributeData attribute, string fallback)
        {
            if (attribute.ConstructorArguments.Length == 1)
            {
                object value = attribute.ConstructorArguments[0].Value;
                if (value == null || value is string)
                {
                    string name = value as string;
                    return IsValidLuaName(name) ? name : null;
                }
            }

            return fallback;
        }

        private static string GetMetamethodName(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length != 1)
            {
                return null;
            }

            object value = attribute.ConstructorArguments[0].Value;
            string customName = value as string;
            if (customName != null || value == null)
            {
                return IsValidLuaName(customName) ? customName : null;
            }

            if (!(value is int))
            {
                return null;
            }

            int kind = (int)value;
            switch (kind)
            {
                case 1:
                    return "__add";
                case 2:
                    return "__sub";
                case 3:
                    return "__mul";
                case 4:
                    return "__mod";
                case 5:
                    return "__pow";
                case 6:
                    return "__div";
                case 7:
                    return "__idiv";
                case 8:
                    return "__band";
                case 9:
                    return "__bor";
                case 10:
                    return "__bxor";
                case 11:
                    return "__bnot";
                case 12:
                    return "__shl";
                case 13:
                    return "__shr";
                case 14:
                    return "__unm";
                case 15:
                    return "__concat";
                case 16:
                    return "__len";
                case 17:
                    return "__eq";
                case 18:
                    return "__lt";
                case 19:
                    return "__le";
                case 20:
                    return "__index";
                case 21:
                    return "__newindex";
                case 22:
                    return "__call";
                case 23:
                    return "__close";
                case 24:
                    return "__gc";
                case 25:
                    return "__mode";
                case 26:
                    return "__name";
                case 27:
                    return "__pairs";
                case 28:
                    return "__tostring";
                default:
                    return null;
            }
        }

        private static bool IsValidLuaName(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool HasAttribute(ISymbol symbol, AttributeName name)
        {
            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (IsAttribute(attribute, name))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAttribute(AttributeData attribute, AttributeName name)
        {
            INamedTypeSymbol attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
            {
                return false;
            }

            return attributeClass.Name == name.TypeName
                && attributeClass.ContainingNamespace.ToDisplayString() == name.Namespace;
        }

        private readonly struct AttributeName
        {
            public AttributeName(string namespaceName, string typeName)
            {
                Namespace = namespaceName;
                TypeName = typeName;
            }

            /// <summary>
            /// Gets the namespace that contains the attribute type.
            /// </summary>
            public string Namespace { get; }

            /// <summary>
            /// Gets the CLR attribute type name.
            /// </summary>
            public string TypeName { get; }
        }

        private static class AttributeNames
        {
            public static readonly AttributeName LuaObject = new AttributeName(
                "NovaSharp",
                "LuaObjectAttribute"
            );

            public static readonly AttributeName LuaMember = new AttributeName(
                "NovaSharp",
                "LuaMemberAttribute"
            );

            public static readonly AttributeName LuaMetamethod = new AttributeName(
                "NovaSharp",
                "LuaMetamethodAttribute"
            );

            public static readonly AttributeName LuaIgnore = new AttributeName(
                "NovaSharp",
                "LuaIgnoreAttribute"
            );
        }

        private sealed class LuaObjectModel
        {
            public LuaObjectModel(
                string namespaceName,
                string typeKeyword,
                string typeName,
                string luaName,
                string typeDisplayName,
                string[] members,
                EnumModel[] referencedEnums
            )
            {
                NamespaceName = namespaceName;
                TypeKeyword = typeKeyword;
                TypeName = typeName;
                LuaName = luaName;
                TypeDisplayName = typeDisplayName;
                Members = members;
                ReferencedEnums = referencedEnums;
                HintName = CreateHintName(namespaceName, typeName);
            }

            /// <summary>
            /// Gets the namespace that contains the generated companion partial type.
            /// </summary>
            public string NamespaceName { get; }

            /// <summary>
            /// Gets the source keyword used for the companion partial type.
            /// </summary>
            public string TypeKeyword { get; }

            /// <summary>
            /// Gets the CLR type name for the companion partial type.
            /// </summary>
            public string TypeName { get; }

            /// <summary>
            /// Gets the Lua-visible object name.
            /// </summary>
            public string LuaName { get; }

            /// <summary>
            /// Gets the diagnostic CLR display name for the generated manifest.
            /// </summary>
            public string TypeDisplayName { get; }

            /// <summary>
            /// Gets the sorted Lua-visible member names.
            /// </summary>
            public string[] Members { get; }

            /// <summary>
            /// Gets the sorted enum types referenced by exposed members.
            /// </summary>
            public EnumModel[] ReferencedEnums { get; }

            /// <summary>
            /// Gets the generated-source hint name.
            /// </summary>
            public string HintName { get; }

            private static string CreateHintName(string namespaceName, string typeName)
            {
                if (namespaceName.Length == 0)
                {
                    return string.Concat(typeName, ".NovaSharpLuaInterop.g.cs");
                }

                return string.Concat(namespaceName, ".", typeName, ".NovaSharpLuaInterop.g.cs");
            }
        }

        private sealed class EnumModel
        {
            public EnumModel(string displayName, string tableName, EnumMemberModel[] members)
            {
                DisplayName = displayName;
                TableName = tableName;
                Members = members;
            }

            /// <summary>
            /// Gets the diagnostic CLR display name for the enum.
            /// </summary>
            public string DisplayName { get; }

            /// <summary>
            /// Gets the Lua table key used to expose this enum.
            /// </summary>
            public string TableName { get; }

            /// <summary>
            /// Gets the enum members exposed as string-keyed table entries.
            /// </summary>
            public EnumMemberModel[] Members { get; }
        }

        private sealed class EnumMemberModel
        {
            public EnumMemberModel(string name, string luaValueExpression)
            {
                Name = name;
                LuaValueExpression = luaValueExpression;
            }

            /// <summary>
            /// Gets the CLR enum member name.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets the generated LuaValue expression for the enum member value.
            /// </summary>
            public string LuaValueExpression { get; }
        }
    }
}
