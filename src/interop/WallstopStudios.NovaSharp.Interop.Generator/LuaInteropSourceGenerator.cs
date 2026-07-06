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

            SortedDictionary<string, MemberModel> members = new SortedDictionary<
                string,
                MemberModel
            >(StringComparer.Ordinal);
            SortedDictionary<string, EnumModel> enums = new SortedDictionary<string, EnumModel>(
                StringComparer.Ordinal
            );
            bool isReferenceType = type.TypeKind == TypeKind.Class;
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
                    MemberModel candidate = CreateMemberModel(member, luaName, isReferenceType);
                    MemberModel existing;
                    if (
                        !members.TryGetValue(luaName, out existing)
                        || candidate.BindingPriority > existing.BindingPriority
                    )
                    {
                        members[luaName] = candidate;
                    }
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
                isReferenceType,
                ToArray(members),
                ToArray(enums, members.Keys)
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

        private static MemberModel CreateMemberModel(
            ISymbol member,
            string luaName,
            bool isReferenceType
        )
        {
            IMethodSymbol method = member as IMethodSymbol;
            if (method != null)
            {
                return CreateMethodMemberModel(method, luaName, isReferenceType);
            }

            IPropertySymbol property = member as IPropertySymbol;
            if (property != null)
            {
                return CreatePropertyMemberModel(property, luaName, isReferenceType);
            }

            IFieldSymbol field = member as IFieldSymbol;
            if (field != null)
            {
                return CreateFieldMemberModel(field, luaName, isReferenceType);
            }

            return MemberModel.Unsupported(luaName);
        }

        private static MemberModel CreateMethodMemberModel(
            IMethodSymbol method,
            string luaName,
            bool isReferenceType
        )
        {
            if (!CanEmitMethod(method, isReferenceType))
            {
                return MemberModel.Unsupported(luaName);
            }

            ParameterModel[] parameters = new ParameterModel[method.Parameters.Length];
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                TypeModel parameterType = CreateTypeModel(method.Parameters[i].Type);
                if (!parameterType.IsSupported)
                {
                    return MemberModel.Unsupported(luaName);
                }

                parameters[i] = new ParameterModel(parameterType);
            }

            TypeModel returnType = method.ReturnsVoid
                ? TypeModel.Unsupported
                : CreateTypeModel(method.ReturnType);
            if (!method.ReturnsVoid && !returnType.IsSupportedReturn)
            {
                return MemberModel.Unsupported(luaName);
            }

            return new MemberModel(
                luaName,
                MemberBindingKind.Method,
                isDispatchable: true,
                EscapeIdentifier(method.Name),
                parameters,
                method.ReturnsVoid,
                returnType,
                canRead: false,
                canWrite: false
            );
        }

        private static MemberModel CreatePropertyMemberModel(
            IPropertySymbol property,
            string luaName,
            bool isReferenceType
        )
        {
            if (
                property.IsStatic
                || property.Parameters.Length != 0
                || property.RefKind != RefKind.None
            )
            {
                return MemberModel.Unsupported(luaName);
            }

            TypeModel propertyType = CreateTypeModel(property.Type);
            if (!propertyType.IsSupportedReturn)
            {
                return MemberModel.Unsupported(luaName);
            }

            bool canRead = property.GetMethod != null;
            bool canWrite =
                isReferenceType && property.SetMethod != null && !property.SetMethod.IsInitOnly;
            if (!canRead && !canWrite)
            {
                return MemberModel.Unsupported(luaName);
            }

            return new MemberModel(
                luaName,
                MemberBindingKind.Property,
                isDispatchable: true,
                EscapeIdentifier(property.Name),
                Array.Empty<ParameterModel>(),
                returnsVoid: false,
                propertyType,
                canRead,
                canWrite
            );
        }

        private static MemberModel CreateFieldMemberModel(
            IFieldSymbol field,
            string luaName,
            bool isReferenceType
        )
        {
            if (field.IsStatic || field.IsConst)
            {
                return MemberModel.Unsupported(luaName);
            }

            TypeModel fieldType = CreateTypeModel(field.Type);
            if (!fieldType.IsSupportedReturn)
            {
                return MemberModel.Unsupported(luaName);
            }

            return new MemberModel(
                luaName,
                MemberBindingKind.Field,
                isDispatchable: true,
                EscapeIdentifier(field.Name),
                Array.Empty<ParameterModel>(),
                returnsVoid: false,
                fieldType,
                canRead: true,
                canWrite: isReferenceType && !field.IsReadOnly
            );
        }

        private static bool CanEmitMethod(IMethodSymbol method, bool isReferenceType)
        {
            if (
                method.MethodKind != MethodKind.Ordinary
                || method.IsStatic
                || method.IsGenericMethod
            )
            {
                return false;
            }

            if (method.RefKind != RefKind.None)
            {
                return false;
            }

            if (!isReferenceType && !method.IsReadOnly)
            {
                return false;
            }

            if (IsAsyncReturnType(method.ReturnType))
            {
                return false;
            }

            foreach (IParameterSymbol parameter in method.Parameters)
            {
                if (parameter.RefKind != RefKind.None)
                {
                    return false;
                }
            }

            // Struct instances are captured by value, so non-readonly methods would mutate
            // a copy and changes would not persist across calls. Only dispatch readonly methods.
            INamedTypeSymbol containingType = method.ContainingType;
            if (
                containingType != null
                && containingType.TypeKind == TypeKind.Struct
                && !method.IsReadOnly
            )
            {
                return false;
            }

            return true;
        }

        private static TypeModel CreateTypeModel(ITypeSymbol type)
        {
            if (type == null)
            {
                return TypeModel.Unsupported;
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return new TypeModel(LuaInteropTypeKind.Boolean, GetTypeSyntax(type));
                case SpecialType.System_Byte:
                    return new TypeModel(LuaInteropTypeKind.Byte, GetTypeSyntax(type));
                case SpecialType.System_Double:
                    return new TypeModel(LuaInteropTypeKind.Double, GetTypeSyntax(type));
                case SpecialType.System_Int16:
                    return new TypeModel(LuaInteropTypeKind.Int16, GetTypeSyntax(type));
                case SpecialType.System_Int32:
                    return new TypeModel(LuaInteropTypeKind.Int32, GetTypeSyntax(type));
                case SpecialType.System_Int64:
                    return new TypeModel(LuaInteropTypeKind.Int64, GetTypeSyntax(type));
                case SpecialType.System_SByte:
                    return new TypeModel(LuaInteropTypeKind.SByte, GetTypeSyntax(type));
                case SpecialType.System_Single:
                    return new TypeModel(LuaInteropTypeKind.Single, GetTypeSyntax(type));
                case SpecialType.System_String:
                    return new TypeModel(LuaInteropTypeKind.String, GetTypeSyntax(type));
                case SpecialType.System_UInt16:
                    return new TypeModel(LuaInteropTypeKind.UInt16, GetTypeSyntax(type));
                case SpecialType.System_UInt32:
                    return new TypeModel(LuaInteropTypeKind.UInt32, GetTypeSyntax(type));
                case SpecialType.System_UInt64:
                    return new TypeModel(LuaInteropTypeKind.UInt64, GetTypeSyntax(type));
            }

            INamedTypeSymbol namedType = type as INamedTypeSymbol;
            if (namedType == null)
            {
                return TypeModel.Unsupported;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                ITypeSymbol underlyingType = namedType.EnumUnderlyingType;
                return new TypeModel(
                    LuaInteropTypeKind.Enum,
                    GetTypeSyntax(type),
                    IsUnsignedEnumUnderlyingType(underlyingType),
                    GetTypeSyntax(underlyingType)
                );
            }

            if (IsNamedType(namedType, "NovaSharp", "LuaValue"))
            {
                return new TypeModel(LuaInteropTypeKind.LuaValue, GetTypeSyntax(type));
            }

            if (IsNamedType(namedType, "NovaSharp", "LuaTable"))
            {
                return new TypeModel(LuaInteropTypeKind.LuaTable, GetTypeSyntax(type));
            }

            if (IsNamedType(namedType, "NovaSharp", "LuaFunction"))
            {
                return new TypeModel(LuaInteropTypeKind.LuaFunction, GetTypeSyntax(type));
            }

            if (IsNamedType(namedType, "NovaSharp", "LuaCoroutine"))
            {
                return new TypeModel(LuaInteropTypeKind.LuaCoroutine, GetTypeSyntax(type));
            }

            return TypeModel.Unsupported;
        }

        private static bool IsNamedType(
            INamedTypeSymbol type,
            string namespaceName,
            string typeName
        )
        {
            return type.Name == typeName
                && type.ContainingNamespace.ToDisplayString() == namespaceName;
        }

        private static bool IsAsyncReturnType(ITypeSymbol type)
        {
            INamedTypeSymbol namedType = type as INamedTypeSymbol;
            if (namedType == null)
            {
                return false;
            }

            INamedTypeSymbol originalDefinition = namedType.OriginalDefinition;
            return IsNamedType(originalDefinition, "System.Threading.Tasks", "Task")
                || IsNamedType(originalDefinition, "System.Threading.Tasks", "ValueTask");
        }

        private static string GetTypeSyntax(ITypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        private static string EscapeIdentifier(string name)
        {
            if (
                SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None
                || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
            )
            {
                return string.Concat("@", name);
            }

            return name;
        }

        private static MemberModel[] ToArray(SortedDictionary<string, MemberModel> values)
        {
            MemberModel[] result = new MemberModel[values.Count];
            int index = 0;
            foreach (KeyValuePair<string, MemberModel> pair in values)
            {
                result[index++] = pair.Value;
            }

            return result;
        }

        private static EnumModel[] ToArray(
            SortedDictionary<string, EnumModel> values,
            ICollection<string> reservedNames
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
            AppendGeneratedRegistrationMethod(builder, model);
            builder.AppendLine();
            AppendGeneratedCodeAttribute(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            builder.AppendLine(
                "private static global::NovaSharp.LuaValue __NovaSharpGeneratedDispatch("
                    + model.TypeName
                    + " instance, global::System.ReadOnlySpan<global::NovaSharp.LuaValue> args, string memberName)"
            );
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            builder.AppendLine("{");
            if (model.RequiresInstanceNullCheck)
            {
                AppendNullCheck(builder, model.NamespaceName.Length > 0 ? 3 : 2, "instance");
            }

            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("_ = args;");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("switch (memberName)");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("{");
            foreach (MemberModel member in model.Members)
            {
                if (!member.IsDispatchable || member.BindingKind != MemberBindingKind.Method)
                {
                    continue;
                }

                AppendIndent(builder, model.NamespaceName.Length > 0 ? 4 : 3);
                builder.Append("case ");
                AppendStringLiteral(builder, member.LuaName);
                builder.AppendLine(":");
                AppendDispatchCase(builder, model, member);
            }

            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("}");
            builder.AppendLine();
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 3 : 2);
            builder.AppendLine("return global::NovaSharp.LuaValue.Nil;");
            AppendIndent(builder, model.NamespaceName.Length > 0 ? 2 : 1);
            builder.AppendLine("}");
            builder.AppendLine();
            if (HasAccessorMembers(model))
            {
                AppendGeneratedIndexMethod(builder, model);
                AppendGeneratedNewIndexMethod(builder, model);
            }

            if (model.ReferencedEnums.Length > 0)
            {
                AppendEnumRegistrationMethod(builder, model);
            }

            if (NeedsUnsignedReturnHelper(model))
            {
                AppendUnsignedReturnHelper(builder, model);
            }

            if (NeedsUnsignedArgumentHelper(model))
            {
                AppendUnsignedArgumentHelper(builder, model);
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

        private static bool NeedsUnsignedReturnHelper(LuaObjectModel model)
        {
            foreach (MemberModel member in model.Members)
            {
                if (
                    member.IsDispatchable
                    && !member.ReturnsVoid
                    && (
                        member.ReturnType.Kind == LuaInteropTypeKind.UInt64
                        || (
                            member.ReturnType.Kind == LuaInteropTypeKind.Enum
                            && member.ReturnType.IsUnsigned
                        )
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool NeedsUnsignedArgumentHelper(LuaObjectModel model)
        {
            foreach (MemberModel member in model.Members)
            {
                if (!member.IsDispatchable)
                {
                    continue;
                }

                if (member.BindingKind != MemberBindingKind.Method)
                {
                    if (member.CanWrite && NeedsUnsignedArgumentRead(member.ReturnType))
                    {
                        return true;
                    }

                    continue;
                }

                foreach (ParameterModel parameter in member.Parameters)
                {
                    if (NeedsUnsignedArgumentRead(parameter.Type))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool NeedsUnsignedArgumentRead(TypeModel type)
        {
            return type.Kind == LuaInteropTypeKind.UInt64
                || (type.Kind == LuaInteropTypeKind.Enum && type.IsUnsigned);
        }

        private static void AppendUnsignedReturnHelper(StringBuilder builder, LuaObjectModel model)
        {
            int memberIndent = model.NamespaceName.Length > 0 ? 2 : 1;
            int bodyIndent = model.NamespaceName.Length > 0 ? 3 : 2;

            AppendGeneratedCodeAttribute(builder, memberIndent);
            AppendIndent(builder, memberIndent);
            builder.AppendLine(
                "private static global::NovaSharp.LuaValue __NovaSharpGeneratedToLuaValue(ulong value)"
            );
            AppendIndent(builder, memberIndent);
            builder.AppendLine("{");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("if (value <= (ulong)global::System.Int64.MaxValue)");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("{");
            AppendIndent(builder, bodyIndent + 1);
            builder.AppendLine("return global::NovaSharp.LuaValue.FromInteger((long)value);");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("}");
            builder.AppendLine();
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("return global::NovaSharp.LuaValue.FromNumber((double)value);");
            AppendIndent(builder, memberIndent);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        private static void AppendUnsignedArgumentHelper(
            StringBuilder builder,
            LuaObjectModel model
        )
        {
            int memberIndent = model.NamespaceName.Length > 0 ? 2 : 1;
            int bodyIndent = model.NamespaceName.Length > 0 ? 3 : 2;

            AppendGeneratedCodeAttribute(builder, memberIndent);
            AppendIndent(builder, memberIndent);
            builder.AppendLine(
                "private static ulong __NovaSharpGeneratedReadUInt64(global::NovaSharp.LuaValue value)"
            );
            AppendIndent(builder, memberIndent);
            builder.AppendLine("{");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("if (value.Kind == global::NovaSharp.LuaKind.Integer)");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("{");
            AppendIndent(builder, bodyIndent + 1);
            builder.AppendLine("return checked((ulong)value.AsInteger());");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("}");
            builder.AppendLine();
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("double number = value.AsNumber();");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine(
                "if (number < 0d || number >= 18446744073709551616d || number != global::System.Math.Truncate(number))"
            );
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("{");
            AppendIndent(builder, bodyIndent + 1);
            builder.AppendLine(
                "throw new global::System.OverflowException(@\"Lua value is outside the range of System.UInt64.\");"
            );
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("}");
            builder.AppendLine();
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("return checked((ulong)number);");
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
            foreach (MemberModel member in model.Members)
            {
                if (!member.IsDispatchable)
                {
                    continue;
                }

                builder.Append("|member:");
                builder.Append(member.LuaName);
            }

            foreach (EnumModel enumModel in model.ReferencedEnums)
            {
                builder.Append("|enum:");
                builder.Append(enumModel.DisplayName);
            }

            return builder.ToString();
        }

        private static void AppendGeneratedRegistrationMethod(
            StringBuilder builder,
            LuaObjectModel model
        )
        {
            int memberIndent = model.NamespaceName.Length > 0 ? 2 : 1;
            int bodyIndent = model.NamespaceName.Length > 0 ? 3 : 2;
            int tableCapacity = model.ReferencedEnums.Length;
            foreach (MemberModel member in model.Members)
            {
                if (member.IsDispatchable && member.BindingKind == MemberBindingKind.Method)
                {
                    tableCapacity++;
                }
            }

            AppendGeneratedCodeAttribute(builder, memberIndent);
            AppendIndent(builder, memberIndent);
            builder.Append(
                "public static void __NovaSharpGeneratedRegister(global::NovaSharp.LuaEngine engine, global::NovaSharp.LuaTable destination, "
            );
            builder.Append(model.TypeName);
            builder.AppendLine(" instance)");
            AppendIndent(builder, memberIndent);
            builder.AppendLine("{");
            AppendNullCheck(builder, bodyIndent, "engine");
            AppendNullCheck(builder, bodyIndent, "destination");
            if (model.RequiresInstanceNullCheck)
            {
                AppendNullCheck(builder, bodyIndent, "instance");
            }

            builder.AppendLine();
            AppendIndent(builder, bodyIndent);
            builder.Append("global::NovaSharp.LuaTable objectTable = engine.CreateTable(0, ");
            builder.Append(tableCapacity.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine(");");
            if (model.ReferencedEnums.Length > 0)
            {
                AppendIndent(builder, bodyIndent);
                builder.AppendLine("__NovaSharpGeneratedRegisterEnumTables(engine, objectTable);");
            }

            bool emittedCallback = false;
            foreach (MemberModel member in model.Members)
            {
                if (!member.IsDispatchable || member.BindingKind != MemberBindingKind.Method)
                {
                    continue;
                }

                AppendIndent(builder, bodyIndent);
                builder.Append("objectTable.Set(");
                AppendStringLiteral(builder, member.LuaName);
                builder.Append(
                    ", engine.CreateCallback((_, args) => __NovaSharpGeneratedDispatch(instance, args, "
                );
                AppendStringLiteral(builder, member.LuaName);
                builder.Append("), ");
                AppendStringLiteral(builder, string.Concat(model.LuaName, ".", member.LuaName));
                builder.AppendLine("));");
                emittedCallback = true;
            }

            if (HasAccessorMembers(model))
            {
                AppendIndent(builder, bodyIndent);
                builder.AppendLine(
                    "global::NovaSharp.LuaTable metatable = engine.CreateTable(0, 2);"
                );
                AppendIndent(builder, bodyIndent);
                builder.Append(
                    "metatable.Set(@\"__index\", engine.CreateCallback((_, args) => __NovaSharpGeneratedIndex(instance, args), "
                );
                AppendStringLiteral(builder, string.Concat(model.LuaName, ".__index"));
                builder.AppendLine("));");
                AppendIndent(builder, bodyIndent);
                builder.Append(
                    "metatable.Set(@\"__newindex\", engine.CreateCallback((_, args) => __NovaSharpGeneratedNewIndex(instance, args), "
                );
                AppendStringLiteral(builder, string.Concat(model.LuaName, ".__newindex"));
                builder.AppendLine("));");
                AppendIndent(builder, bodyIndent);
                builder.AppendLine("objectTable.SetMetatable(metatable);");
            }

            if (model.ReferencedEnums.Length > 0 || emittedCallback || HasAccessorMembers(model))
            {
                builder.AppendLine();
            }

            AppendIndent(builder, bodyIndent);
            builder.Append("destination.Set(");
            AppendStringLiteral(builder, model.LuaName);
            builder.AppendLine(", objectTable.ToValue());");
            AppendIndent(builder, memberIndent);
            builder.AppendLine("}");
        }

        private static void AppendDispatchCase(
            StringBuilder builder,
            LuaObjectModel model,
            MemberModel member
        )
        {
            int bodyIndent = model.NamespaceName.Length > 0 ? 5 : 4;
            AppendIndent(builder, bodyIndent);
            builder.Append("if (args.Length != ");
            builder.Append(member.Parameters.Length.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine(")");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("{");
            AppendIndent(builder, bodyIndent + 1);
            builder.Append(
                "throw new global::WallstopStudios.NovaSharp.Interpreter.Errors.ScriptRuntimeException("
            );
            AppendStringLiteral(
                builder,
                string.Concat(
                    "Lua member '",
                    member.LuaName,
                    "' expects ",
                    member.Parameters.Length.ToString(CultureInfo.InvariantCulture),
                    " argument(s)."
                )
            );
            builder.AppendLine(");");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("}");

            AppendIndent(builder, bodyIndent);
            if (member.ReturnsVoid)
            {
                builder.Append("instance.");
                builder.Append(member.ClrName);
                AppendArgumentList(builder, member);
                builder.AppendLine(";");
                AppendIndent(builder, bodyIndent);
                builder.AppendLine("return global::NovaSharp.LuaValue.Nil;");
                return;
            }

            builder.Append("return ");
            AppendReturnValue(
                builder,
                member.ReturnType,
                CreateCallExpression("instance." + member.ClrName, member)
            );
            builder.AppendLine(";");
        }

        private static bool HasAccessorMembers(LuaObjectModel model)
        {
            foreach (MemberModel member in model.Members)
            {
                if (member.IsDispatchable && member.BindingKind != MemberBindingKind.Method)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AppendGeneratedIndexMethod(StringBuilder builder, LuaObjectModel model)
        {
            int memberIndent = model.NamespaceName.Length > 0 ? 2 : 1;
            int bodyIndent = model.NamespaceName.Length > 0 ? 3 : 2;
            int switchIndent = model.NamespaceName.Length > 0 ? 4 : 3;

            AppendGeneratedCodeAttribute(builder, memberIndent);
            AppendIndent(builder, memberIndent);
            builder.AppendLine(
                "private static global::NovaSharp.LuaValue __NovaSharpGeneratedIndex("
                    + model.TypeName
                    + " instance, global::System.ReadOnlySpan<global::NovaSharp.LuaValue> args)"
            );
            AppendIndent(builder, memberIndent);
            builder.AppendLine("{");
            if (model.RequiresInstanceNullCheck)
            {
                AppendNullCheck(builder, bodyIndent, "instance");
            }

            AppendMetamethodArityCheck(builder, bodyIndent, "__index", 2);
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("if (args[1].Kind != global::NovaSharp.LuaKind.String)");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("{");
            AppendIndent(builder, bodyIndent + 1);
            builder.AppendLine("return global::NovaSharp.LuaValue.Nil;");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("}");
            builder.AppendLine();
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("string key = args[1].AsString();");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("switch (key)");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("{");
            foreach (MemberModel member in model.Members)
            {
                if (
                    !member.IsDispatchable
                    || member.BindingKind == MemberBindingKind.Method
                    || !member.CanRead
                )
                {
                    continue;
                }

                AppendIndent(builder, switchIndent);
                builder.Append("case ");
                AppendStringLiteral(builder, member.LuaName);
                builder.AppendLine(":");
                AppendIndent(builder, switchIndent + 1);
                builder.Append("return ");
                AppendReturnValue(builder, member.ReturnType, "instance." + member.ClrName);
                builder.AppendLine(";");
            }

            AppendIndent(builder, bodyIndent);
            builder.AppendLine("}");
            builder.AppendLine();
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("return global::NovaSharp.LuaValue.Nil;");
            AppendIndent(builder, memberIndent);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        private static void AppendGeneratedNewIndexMethod(
            StringBuilder builder,
            LuaObjectModel model
        )
        {
            int memberIndent = model.NamespaceName.Length > 0 ? 2 : 1;
            int bodyIndent = model.NamespaceName.Length > 0 ? 3 : 2;
            int switchIndent = model.NamespaceName.Length > 0 ? 4 : 3;

            AppendGeneratedCodeAttribute(builder, memberIndent);
            AppendIndent(builder, memberIndent);
            builder.AppendLine(
                "private static global::NovaSharp.LuaValue __NovaSharpGeneratedNewIndex("
                    + model.TypeName
                    + " instance, global::System.ReadOnlySpan<global::NovaSharp.LuaValue> args)"
            );
            AppendIndent(builder, memberIndent);
            builder.AppendLine("{");
            if (model.RequiresInstanceNullCheck)
            {
                AppendNullCheck(builder, bodyIndent, "instance");
            }

            AppendMetamethodArityCheck(builder, bodyIndent, "__newindex", 3);
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("if (args[1].Kind != global::NovaSharp.LuaKind.String)");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("{");
            AppendIndent(builder, bodyIndent + 1);
            builder.Append(
                "throw new global::WallstopStudios.NovaSharp.Interpreter.Errors.ScriptRuntimeException("
            );
            AppendStringLiteral(builder, "Lua member keys must be strings.");
            builder.AppendLine(");");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("}");
            builder.AppendLine();
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("string key = args[1].AsString();");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("switch (key)");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("{");
            foreach (MemberModel member in model.Members)
            {
                if (!member.IsDispatchable || member.BindingKind == MemberBindingKind.Method)
                {
                    continue;
                }

                AppendIndent(builder, switchIndent);
                builder.Append("case ");
                AppendStringLiteral(builder, member.LuaName);
                builder.AppendLine(":");
                if (member.CanWrite)
                {
                    AppendIndent(builder, switchIndent + 1);
                    builder.Append("instance.");
                    builder.Append(member.ClrName);
                    builder.Append(" = ");
                    AppendArgumentRead(builder, member.ReturnType, 2);
                    builder.AppendLine(";");
                    AppendIndent(builder, switchIndent + 1);
                    builder.AppendLine("return global::NovaSharp.LuaValue.Nil;");
                    continue;
                }

                AppendIndent(builder, switchIndent + 1);
                builder.Append(
                    "throw new global::WallstopStudios.NovaSharp.Interpreter.Errors.ScriptRuntimeException("
                );
                AppendStringLiteral(
                    builder,
                    string.Concat("Lua member '", member.LuaName, "' is read-only.")
                );
                builder.AppendLine(");");
            }

            AppendIndent(builder, bodyIndent);
            builder.AppendLine("}");
            builder.AppendLine();
            AppendIndent(builder, bodyIndent);
            builder.Append(
                "throw new global::WallstopStudios.NovaSharp.Interpreter.Errors.ScriptRuntimeException(global::System.String.Concat(@\"Lua member '\", key, @\"' is not writable.\"));"
            );
            builder.AppendLine();
            AppendIndent(builder, memberIndent);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        private static void AppendMetamethodArityCheck(
            StringBuilder builder,
            int bodyIndent,
            string metamethodName,
            int expectedLength
        )
        {
            AppendIndent(builder, bodyIndent);
            builder.Append("if (args.Length != ");
            builder.Append(expectedLength.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine(")");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("{");
            AppendIndent(builder, bodyIndent + 1);
            builder.Append(
                "throw new global::WallstopStudios.NovaSharp.Interpreter.Errors.ScriptRuntimeException("
            );
            AppendStringLiteral(
                builder,
                string.Concat(
                    "Lua metamethod '",
                    metamethodName,
                    "' expects ",
                    expectedLength.ToString(CultureInfo.InvariantCulture),
                    " argument(s)."
                )
            );
            builder.AppendLine(");");
            AppendIndent(builder, bodyIndent);
            builder.AppendLine("}");
        }

        private static void AppendArgumentList(StringBuilder builder, MemberModel member)
        {
            builder.Append('(');
            for (int i = 0; i < member.Parameters.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                AppendArgumentRead(builder, member.Parameters[i].Type, i);
            }

            builder.Append(')');
        }

        private static void AppendReturnValue(
            StringBuilder builder,
            TypeModel returnType,
            string valueExpression
        )
        {
            switch (returnType.Kind)
            {
                case LuaInteropTypeKind.Boolean:
                    builder.Append("global::NovaSharp.LuaValue.FromBoolean(");
                    builder.Append(valueExpression);
                    builder.Append(')');
                    return;
                case LuaInteropTypeKind.Byte:
                case LuaInteropTypeKind.Int16:
                case LuaInteropTypeKind.Int32:
                case LuaInteropTypeKind.Int64:
                case LuaInteropTypeKind.SByte:
                case LuaInteropTypeKind.UInt16:
                case LuaInteropTypeKind.UInt32:
                    builder.Append("global::NovaSharp.LuaValue.FromInteger((long)");
                    builder.Append(valueExpression);
                    builder.Append(')');
                    return;
                case LuaInteropTypeKind.UInt64:
                    builder.Append("__NovaSharpGeneratedToLuaValue((ulong)");
                    builder.Append(valueExpression);
                    builder.Append(')');
                    return;
                case LuaInteropTypeKind.Double:
                case LuaInteropTypeKind.Single:
                    builder.Append("global::NovaSharp.LuaValue.FromNumber((double)");
                    builder.Append(valueExpression);
                    builder.Append(')');
                    return;
                case LuaInteropTypeKind.String:
                    builder.Append("global::NovaSharp.LuaValue.FromString(");
                    builder.Append(valueExpression);
                    builder.Append(')');
                    return;
                case LuaInteropTypeKind.LuaValue:
                    builder.Append(valueExpression);
                    return;
                case LuaInteropTypeKind.LuaTable:
                case LuaInteropTypeKind.LuaFunction:
                case LuaInteropTypeKind.LuaCoroutine:
                    builder.Append(valueExpression);
                    builder.Append(".ToValue()");
                    return;
                case LuaInteropTypeKind.Enum:
                    if (returnType.IsUnsigned)
                    {
                        builder.Append("__NovaSharpGeneratedToLuaValue((ulong)");
                    }
                    else
                    {
                        builder.Append("global::NovaSharp.LuaValue.FromInteger((long)");
                    }

                    builder.Append(valueExpression);
                    builder.Append(')');
                    return;
                default:
                    builder.Append("global::NovaSharp.LuaValue.Nil");
                    return;
            }
        }

        private static string CreateCallExpression(string callTarget, MemberModel member)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(callTarget);
            AppendArgumentList(builder, member);
            return builder.ToString();
        }

        private static void AppendArgumentRead(StringBuilder builder, TypeModel type, int index)
        {
            string argument = string.Concat(
                "args[",
                index.ToString(CultureInfo.InvariantCulture),
                "]"
            );
            switch (type.Kind)
            {
                case LuaInteropTypeKind.Boolean:
                    builder.Append(argument);
                    builder.Append(".AsBoolean()");
                    return;
                case LuaInteropTypeKind.Byte:
                case LuaInteropTypeKind.Int16:
                case LuaInteropTypeKind.Int32:
                case LuaInteropTypeKind.Int64:
                case LuaInteropTypeKind.SByte:
                case LuaInteropTypeKind.UInt16:
                case LuaInteropTypeKind.UInt32:
                    builder.Append("checked((");
                    builder.Append(type.CodeType);
                    builder.Append(')');
                    builder.Append(argument);
                    builder.Append(".AsInteger())");
                    return;
                case LuaInteropTypeKind.UInt64:
                    builder.Append("__NovaSharpGeneratedReadUInt64(");
                    builder.Append(argument);
                    builder.Append(')');
                    return;
                case LuaInteropTypeKind.Double:
                    builder.Append(argument);
                    builder.Append(".AsNumber()");
                    return;
                case LuaInteropTypeKind.Single:
                    builder.Append("(float)");
                    builder.Append(argument);
                    builder.Append(".AsNumber()");
                    return;
                case LuaInteropTypeKind.String:
                    builder.Append(argument);
                    builder.Append(".AsString()");
                    return;
                case LuaInteropTypeKind.LuaValue:
                    builder.Append(argument);
                    return;
                case LuaInteropTypeKind.LuaTable:
                    builder.Append(argument);
                    builder.Append(".AsTable()");
                    return;
                case LuaInteropTypeKind.LuaFunction:
                    builder.Append(argument);
                    builder.Append(".AsFunction()");
                    return;
                case LuaInteropTypeKind.LuaCoroutine:
                    builder.Append(argument);
                    builder.Append(".AsCoroutine()");
                    return;
                case LuaInteropTypeKind.Enum:
                    builder.Append('(');
                    builder.Append(type.CodeType);
                    builder.Append(')');
                    if (type.IsUnsigned)
                    {
                        builder.Append("checked((");
                        builder.Append(type.EnumUnderlyingCodeType);
                        builder.Append(')');
                        builder.Append("__NovaSharpGeneratedReadUInt64(");
                        builder.Append(argument);
                        builder.Append("))");
                        return;
                    }

                    builder.Append("checked((");
                    builder.Append(type.EnumUnderlyingCodeType);
                    builder.Append(')');
                    builder.Append(argument);
                    builder.Append(".AsInteger()");
                    builder.Append(')');
                    return;
                default:
                    builder.Append("default(");
                    builder.Append(type.CodeType);
                    builder.Append(')');
                    return;
            }
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
                bool requiresInstanceNullCheck,
                MemberModel[] members,
                EnumModel[] referencedEnums
            )
            {
                NamespaceName = namespaceName;
                TypeKeyword = typeKeyword;
                TypeName = typeName;
                LuaName = luaName;
                TypeDisplayName = typeDisplayName;
                RequiresInstanceNullCheck = requiresInstanceNullCheck;
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
            /// Gets whether the generated dispatch must reject null instances.
            /// </summary>
            public bool RequiresInstanceNullCheck { get; }

            /// <summary>
            /// Gets the sorted Lua-visible member models.
            /// </summary>
            public MemberModel[] Members { get; }

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

        private sealed class MemberModel
        {
            public MemberModel(
                string luaName,
                MemberBindingKind bindingKind,
                bool isDispatchable,
                string clrName,
                ParameterModel[] parameters,
                bool returnsVoid,
                TypeModel returnType,
                bool canRead,
                bool canWrite
            )
            {
                LuaName = luaName;
                BindingKind = bindingKind;
                IsDispatchable = isDispatchable;
                ClrName = clrName;
                Parameters = parameters == null ? Array.Empty<ParameterModel>() : parameters;
                ReturnsVoid = returnsVoid;
                ReturnType = returnType;
                CanRead = canRead;
                CanWrite = canWrite;
            }

            /// <summary>
            /// Creates an unsupported member placeholder that can still reserve a Lua-visible name.
            /// </summary>
            public static MemberModel Unsupported(string luaName)
            {
                return new MemberModel(
                    luaName,
                    MemberBindingKind.Unsupported,
                    isDispatchable: false,
                    null,
                    null,
                    false,
                    TypeModel.Unsupported,
                    canRead: false,
                    canWrite: false
                );
            }

            /// <summary>
            /// Gets the Lua-visible member name.
            /// </summary>
            public string LuaName { get; }

            /// <summary>
            /// Gets the generated binding kind.
            /// </summary>
            public MemberBindingKind BindingKind { get; }

            /// <summary>
            /// Gets the priority used when duplicate Lua names are present.
            /// </summary>
            public int BindingPriority
            {
                get
                {
                    switch (BindingKind)
                    {
                        case MemberBindingKind.Method:
                            return 2;
                        case MemberBindingKind.Property:
                        case MemberBindingKind.Field:
                            return 1;
                        default:
                            return 0;
                    }
                }
            }

            /// <summary>
            /// Gets whether this member can be emitted as a generated callback.
            /// </summary>
            public bool IsDispatchable { get; }

            /// <summary>
            /// Gets the CLR member name.
            /// </summary>
            public string ClrName { get; }

            /// <summary>
            /// Gets the ordered parameter models.
            /// </summary>
            public ParameterModel[] Parameters { get; }

            /// <summary>
            /// Gets whether the method returns void.
            /// </summary>
            public bool ReturnsVoid { get; }

            /// <summary>
            /// Gets the return type model when the method returns a value.
            /// </summary>
            public TypeModel ReturnType { get; }

            /// <summary>
            /// Gets whether this generated accessor supports reads.
            /// </summary>
            public bool CanRead { get; }

            /// <summary>
            /// Gets whether this generated accessor supports writes.
            /// </summary>
            public bool CanWrite { get; }
        }

        private sealed class ParameterModel
        {
            public ParameterModel(TypeModel type)
            {
                Type = type;
            }

            /// <summary>
            /// Gets the parameter type model.
            /// </summary>
            public TypeModel Type { get; }
        }

        private readonly struct TypeModel
        {
            public static readonly TypeModel Unsupported = new TypeModel(
                LuaInteropTypeKind.Unsupported,
                "object"
            );

            public TypeModel(
                LuaInteropTypeKind kind,
                string codeType,
                bool isUnsigned = false,
                string enumUnderlyingCodeType = ""
            )
            {
                Kind = kind;
                CodeType = codeType;
                IsUnsigned = isUnsigned;
                EnumUnderlyingCodeType = enumUnderlyingCodeType;
            }

            /// <summary>
            /// Gets the generated interop type kind.
            /// </summary>
            public LuaInteropTypeKind Kind { get; }

            /// <summary>
            /// Gets the C# type syntax used in generated source.
            /// </summary>
            public string CodeType { get; }

            /// <summary>
            /// Gets whether this type is represented with an unsigned runtime value.
            /// </summary>
            public bool IsUnsigned { get; }

            /// <summary>
            /// Gets the generated C# syntax for this enum type's underlying integral type.
            /// </summary>
            public string EnumUnderlyingCodeType { get; }

            /// <summary>
            /// Gets whether this type is supported for generated argument unpacking.
            /// </summary>
            public bool IsSupported
            {
                get { return Kind != LuaInteropTypeKind.Unsupported; }
            }

            /// <summary>
            /// Gets whether this type is supported for generated return wrapping.
            /// </summary>
            public bool IsSupportedReturn
            {
                get { return IsSupported; }
            }
        }

        private enum LuaInteropTypeKind
        {
            Unsupported,
            Boolean,
            Byte,
            Double,
            Enum,
            Int16,
            Int32,
            Int64,
            LuaCoroutine,
            LuaFunction,
            LuaTable,
            LuaValue,
            SByte,
            Single,
            String,
            UInt16,
            UInt32,
            UInt64,
        }

        private enum MemberBindingKind
        {
            Unsupported,
            Method,
            Property,
            Field,
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
