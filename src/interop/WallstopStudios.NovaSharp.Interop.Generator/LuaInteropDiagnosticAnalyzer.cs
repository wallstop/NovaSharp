namespace WallstopStudios.NovaSharp.Interop.Generator
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    /// <summary>
    /// Validates the public generated interop attribute contract before source generation runs.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuaInteropDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticDescriptors =
            ImmutableArray.Create(
                LuaInteropDiagnostics.LuaObjectMustBePartial,
                LuaInteropDiagnostics.UnsupportedType,
                LuaInteropDiagnostics.UnsupportedSignatureShape,
                LuaInteropDiagnostics.NameCollision,
                LuaInteropDiagnostics.AsyncReturnRequiresAdapter,
                LuaInteropDiagnostics.MemberRequiresLuaObject
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return SupportedDiagnosticDescriptors; }
        }

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
            context.RegisterSyntaxNodeAction(
                AnalyzeAttributedMemberDeclaration,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.FieldDeclaration,
                SyntaxKind.IndexerDeclaration,
                SyntaxKind.MethodDeclaration,
                SyntaxKind.PropertyDeclaration
            );
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
            if (!HasAttribute(type, AttributeNames.LuaObject))
            {
                return;
            }

            if (!IsPartial(type, context.CancellationToken))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        LuaInteropDiagnostics.LuaObjectMustBePartial,
                        GetLocation(type),
                        type.Name
                    )
                );
            }

            if (type.TypeParameters.Length > 0)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        LuaInteropDiagnostics.UnsupportedSignatureShape,
                        GetLocation(type),
                        type.Name,
                        "an open generic LuaObject type"
                    )
                );
            }

            AnalyzeNameCollisions(context, type);
            AnalyzeLuaObjectMembers(context, type);
        }

        private static void AnalyzeAttributedMemberDeclaration(SyntaxNodeAnalysisContext context)
        {
            MemberDeclarationSyntax declaration = (MemberDeclarationSyntax)context.Node;
            if (declaration.AttributeLists.Count == 0)
            {
                return;
            }

            if (!HasLuaInteropAttributeSyntax(declaration))
            {
                return;
            }

            FieldDeclarationSyntax fieldDeclaration = declaration as FieldDeclarationSyntax;
            if (fieldDeclaration != null)
            {
                foreach (
                    VariableDeclaratorSyntax variable in fieldDeclaration.Declaration.Variables
                )
                {
                    AnalyzeMemberRequiresLuaObject(
                        context,
                        context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken)
                    );
                }

                return;
            }

            AnalyzeMemberRequiresLuaObject(context, GetDeclaredMemberSymbol(context, declaration));
        }

        private static bool HasLuaInteropAttributeSyntax(MemberDeclarationSyntax declaration)
        {
            foreach (AttributeListSyntax attributeList in declaration.AttributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    if (IsLuaInteropAttributeSyntaxName(attribute.Name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsLuaInteropAttributeSyntaxName(NameSyntax name)
        {
            string unqualifiedName = GetUnqualifiedAttributeSyntaxName(name);
            switch (unqualifiedName)
            {
                case "LuaMember":
                case "LuaMemberAttribute":
                case "LuaMetamethod":
                case "LuaMetamethodAttribute":
                case "LuaIgnore":
                case "LuaIgnoreAttribute":
                    return true;
                default:
                    return false;
            }
        }

        private static string GetUnqualifiedAttributeSyntaxName(NameSyntax name)
        {
            SimpleNameSyntax simpleName = name as SimpleNameSyntax;
            if (simpleName != null)
            {
                return simpleName.Identifier.ValueText;
            }

            QualifiedNameSyntax qualifiedName = name as QualifiedNameSyntax;
            if (qualifiedName != null)
            {
                return qualifiedName.Right.Identifier.ValueText;
            }

            AliasQualifiedNameSyntax aliasQualifiedName = name as AliasQualifiedNameSyntax;
            if (aliasQualifiedName != null)
            {
                return aliasQualifiedName.Name.Identifier.ValueText;
            }

            return null;
        }

        private static ISymbol GetDeclaredMemberSymbol(
            SyntaxNodeAnalysisContext context,
            MemberDeclarationSyntax declaration
        )
        {
            MethodDeclarationSyntax methodDeclaration = declaration as MethodDeclarationSyntax;
            if (methodDeclaration != null)
            {
                return context.SemanticModel.GetDeclaredSymbol(
                    methodDeclaration,
                    context.CancellationToken
                );
            }

            PropertyDeclarationSyntax propertyDeclaration =
                declaration as PropertyDeclarationSyntax;
            if (propertyDeclaration != null)
            {
                return context.SemanticModel.GetDeclaredSymbol(
                    propertyDeclaration,
                    context.CancellationToken
                );
            }

            IndexerDeclarationSyntax indexerDeclaration = declaration as IndexerDeclarationSyntax;
            if (indexerDeclaration != null)
            {
                return context.SemanticModel.GetDeclaredSymbol(
                    indexerDeclaration,
                    context.CancellationToken
                );
            }

            ConstructorDeclarationSyntax constructorDeclaration =
                declaration as ConstructorDeclarationSyntax;
            if (constructorDeclaration != null)
            {
                return context.SemanticModel.GetDeclaredSymbol(
                    constructorDeclaration,
                    context.CancellationToken
                );
            }

            return null;
        }

        private static void AnalyzeMemberRequiresLuaObject(
            SyntaxNodeAnalysisContext context,
            ISymbol member
        )
        {
            if (member == null)
            {
                return;
            }

            AttributeSet attributes = AttributeSet.Create(member);
            if (!attributes.HasLuaInteropAttribute)
            {
                return;
            }

            INamedTypeSymbol containingType = member.ContainingType;
            if (containingType == null || !HasAttribute(containingType, AttributeNames.LuaObject))
            {
                string containingName = containingType == null ? "<unknown>" : containingType.Name;
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        LuaInteropDiagnostics.MemberRequiresLuaObject,
                        GetLocation(member),
                        member.Name,
                        containingName
                    )
                );
                return;
            }
        }

        private static void AnalyzeLuaObjectMembers(
            SymbolAnalysisContext context,
            INamedTypeSymbol type
        )
        {
            foreach (ISymbol member in type.GetMembers())
            {
                if (member.IsImplicitlyDeclared)
                {
                    continue;
                }

                AttributeSet attributes = AttributeSet.Create(member);
                if (!attributes.HasLuaInteropAttribute)
                {
                    continue;
                }

                if (attributes.HasLuaIgnore)
                {
                    continue;
                }

                AnalyzeMemberSignature(context, member, GetLuaBindingName(member, attributes));
            }
        }

        private static void AnalyzeNameCollisions(
            SymbolAnalysisContext context,
            INamedTypeSymbol type
        )
        {
            Dictionary<string, ISymbol> exposedNames = new Dictionary<string, ISymbol>(
                StringComparer.Ordinal
            );
            foreach (ISymbol member in type.GetMembers())
            {
                if (member.IsImplicitlyDeclared)
                {
                    continue;
                }

                AttributeSet attributes = AttributeSet.Create(member);
                if (attributes.HasLuaIgnore)
                {
                    continue;
                }

                foreach (AttributeData attribute in attributes.Attributes)
                {
                    string luaName = GetLuaName(member, attribute);
                    if (luaName == null)
                    {
                        continue;
                    }

                    if (exposedNames.ContainsKey(luaName))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                LuaInteropDiagnostics.NameCollision,
                                GetLocation(member),
                                type.Name,
                                luaName
                            )
                        );
                        continue;
                    }

                    exposedNames.Add(luaName, member);
                }
            }
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

        private static string GetLuaBindingName(ISymbol member, AttributeSet attributes)
        {
            foreach (AttributeData attribute in attributes.Attributes)
            {
                string luaName = GetLuaName(member, attribute);
                if (luaName != null)
                {
                    return luaName;
                }
            }

            return member.Name;
        }

        private static void AnalyzeMemberSignature(
            SymbolAnalysisContext context,
            ISymbol member,
            string bindingName
        )
        {
            IMethodSymbol method = member as IMethodSymbol;
            if (method != null)
            {
                AnalyzeMethodSignature(context, method, bindingName);
                return;
            }

            IPropertySymbol property = member as IPropertySymbol;
            if (property != null)
            {
                AnalyzePropertySignature(context, property, bindingName);
                return;
            }

            IFieldSymbol field = member as IFieldSymbol;
            if (field != null)
            {
                AnalyzeReturnType(context, field, field.Type, bindingName);
            }
        }

        private static void AnalyzePropertySignature(
            SymbolAnalysisContext context,
            IPropertySymbol property,
            string bindingName
        )
        {
            if (property.RefKind != RefKind.None)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        LuaInteropDiagnostics.UnsupportedSignatureShape,
                        GetLocation(property),
                        bindingName,
                        property.RefKind == RefKind.RefReadOnly
                            ? "a ref readonly return"
                            : "a ref return"
                    )
                );
                return;
            }

            AnalyzeReturnType(context, property, property.Type, bindingName);

            foreach (IParameterSymbol parameter in property.Parameters)
            {
                if (parameter.RefKind != RefKind.None)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            LuaInteropDiagnostics.UnsupportedSignatureShape,
                            GetLocation(parameter),
                            bindingName,
                            "a ref, out, or in parameter"
                        )
                    );
                    continue;
                }

                AnalyzeType(context, parameter, parameter.Type, bindingName);
            }
        }

        private static void AnalyzeMethodSignature(
            SymbolAnalysisContext context,
            IMethodSymbol method,
            string bindingName
        )
        {
            if (method.MethodKind == MethodKind.Constructor)
            {
                return;
            }

            if (method.IsGenericMethod)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        LuaInteropDiagnostics.UnsupportedSignatureShape,
                        GetLocation(method),
                        bindingName,
                        "an open generic method"
                    )
                );
            }

            if (method.RefKind != RefKind.None)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        LuaInteropDiagnostics.UnsupportedSignatureShape,
                        GetLocation(method),
                        bindingName,
                        method.RefKind == RefKind.RefReadOnly
                            ? "a ref readonly return"
                            : "a ref return"
                    )
                );
                return;
            }

            if (method.ReturnsVoid)
            {
                // Void is a valid Lua nil return.
            }
            else
            {
                AnalyzeReturnType(context, method, method.ReturnType, bindingName);
            }

            foreach (IParameterSymbol parameter in method.Parameters)
            {
                if (parameter.RefKind != RefKind.None)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            LuaInteropDiagnostics.UnsupportedSignatureShape,
                            GetLocation(parameter),
                            bindingName,
                            "a ref, out, or in parameter"
                        )
                    );
                    continue;
                }

                AnalyzeType(context, parameter, parameter.Type, bindingName);
            }
        }

        private static void AnalyzeReturnType(
            SymbolAnalysisContext context,
            ISymbol symbol,
            ITypeSymbol type,
            string bindingName
        )
        {
            if (IsAsyncReturnType(type))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        LuaInteropDiagnostics.AsyncReturnRequiresAdapter,
                        GetLocation(symbol),
                        bindingName,
                        type.ToDisplayString()
                    )
                );
            }
            else
            {
                AnalyzeType(context, symbol, type, bindingName);
            }
        }

        private static void AnalyzeType(
            SymbolAnalysisContext context,
            ISymbol symbol,
            ITypeSymbol type,
            string bindingName
        )
        {
            if (IsPointerType(type))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        LuaInteropDiagnostics.UnsupportedSignatureShape,
                        GetLocation(symbol),
                        bindingName,
                        "a pointer type"
                    )
                );
                return;
            }

            if (ContainsOpenGenericType(type))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        LuaInteropDiagnostics.UnsupportedSignatureShape,
                        GetLocation(symbol),
                        bindingName,
                        "an open generic type"
                    )
                );
                return;
            }

            if (!IsSupportedType(type))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        LuaInteropDiagnostics.UnsupportedType,
                        GetLocation(symbol),
                        bindingName,
                        type.ToDisplayString()
                    )
                );
            }
        }

        private static bool IsPartial(INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
            {
                SyntaxNode node = reference.GetSyntax(cancellationToken);
                TypeDeclarationSyntax typeDeclaration = node as TypeDeclarationSyntax;
                if (
                    typeDeclaration != null
                    && typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword)
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSupportedType(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_Double:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                case SpecialType.System_String:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    return true;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                return true;
            }

            INamedTypeSymbol namedType = type as INamedTypeSymbol;
            if (namedType == null)
            {
                return false;
            }

            if (HasAttribute(namedType, AttributeNames.LuaObject))
            {
                return true;
            }

            return IsNamedType(namedType, "NovaSharp", "LuaValue")
                || IsNamedType(namedType, "NovaSharp", "LuaTable")
                || IsNamedType(namedType, "NovaSharp", "LuaFunction")
                || IsNamedType(namedType, "NovaSharp", "LuaCoroutine");
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

        private static bool ContainsOpenGenericType(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.TypeKind == TypeKind.TypeParameter)
            {
                return true;
            }

            IArrayTypeSymbol arrayType = type as IArrayTypeSymbol;
            if (arrayType != null)
            {
                return ContainsOpenGenericType(arrayType.ElementType);
            }

            INamedTypeSymbol namedType = type as INamedTypeSymbol;
            if (namedType == null)
            {
                return false;
            }

            foreach (ITypeSymbol typeArgument in namedType.TypeArguments)
            {
                if (ContainsOpenGenericType(typeArgument))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPointerType(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.TypeKind == TypeKind.Pointer)
            {
                return true;
            }

            IArrayTypeSymbol arrayType = type as IArrayTypeSymbol;
            if (arrayType != null)
            {
                return IsPointerType(arrayType.ElementType);
            }

            return false;
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

        private static string GetNamedMemberName(AttributeData attribute, string fallback)
        {
            if (attribute.ConstructorArguments.Length == 1)
            {
                object value = attribute.ConstructorArguments[0].Value;
                string name = value as string;
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
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
            if (customName != null)
            {
                return customName;
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

        private static bool IsNamedType(
            INamedTypeSymbol type,
            string namespaceName,
            string typeName
        )
        {
            return type.Name == typeName
                && type.ContainingNamespace.ToDisplayString() == namespaceName;
        }

        private static Location GetLocation(ISymbol symbol)
        {
            if (symbol.Locations.Length == 0)
            {
                return Location.None;
            }

            return symbol.Locations[0];
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

        private readonly struct AttributeSet
        {
            private AttributeSet(ImmutableArray<AttributeData> attributes)
            {
                Attributes = attributes;
                HasLuaMember = false;
                HasLuaMetamethod = false;
                HasLuaIgnore = false;

                foreach (AttributeData attribute in attributes)
                {
                    if (IsAttribute(attribute, AttributeNames.LuaMember))
                    {
                        HasLuaMember = true;
                    }
                    else if (IsAttribute(attribute, AttributeNames.LuaMetamethod))
                    {
                        HasLuaMetamethod = true;
                    }
                    else if (IsAttribute(attribute, AttributeNames.LuaIgnore))
                    {
                        HasLuaIgnore = true;
                    }
                }
            }

            /// <summary>
            /// Gets all attributes declared on the member.
            /// </summary>
            public ImmutableArray<AttributeData> Attributes { get; }

            /// <summary>
            /// Gets whether the member has <c>LuaMemberAttribute</c>.
            /// </summary>
            public bool HasLuaMember { get; }

            /// <summary>
            /// Gets whether the member has <c>LuaMetamethodAttribute</c>.
            /// </summary>
            public bool HasLuaMetamethod { get; }

            /// <summary>
            /// Gets whether the member has <c>LuaIgnoreAttribute</c>.
            /// </summary>
            public bool HasLuaIgnore { get; }

            public bool HasLuaInteropAttribute
            {
                get { return HasLuaMember || HasLuaMetamethod || HasLuaIgnore; }
            }

            /// <summary>
            /// Creates an attribute summary for a symbol.
            /// </summary>
            public static AttributeSet Create(ISymbol symbol)
            {
                return new AttributeSet(symbol.GetAttributes());
            }
        }
    }
}
