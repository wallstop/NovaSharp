namespace NovaSharp.Hardwire.Generators
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Linq;
    using Interpreter;
    using Interpreter.Interop;
    using Interpreter.Interop.BasicDescriptors;
    using Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using Utils;

    internal class MethodMemberDescriptorGenerator : IHardwireGenerator
    {
        private readonly string _prefix;

        public MethodMemberDescriptorGenerator()
            : this("MTHD") { }

        public MethodMemberDescriptorGenerator(string prefix)
        {
            _prefix = prefix;
        }

        public string ManagedType
        {
            get { return "NovaSharp.Interpreter.Interop.MethodMemberDescriptor"; }
        }

        public CodeExpression[] Generate(
            Table table,
            HardwireCodeGenerationContext generator,
            CodeTypeMemberCollection members
        )
        {
            bool isArray = table.Get("arraytype").IsNotNil();
            string memberName = table.Get("name").String;

            // Ignore arrays weird special members
            if (isArray)
            {
                if ((memberName == "Get") || (memberName == "Set") || (memberName == "Address"))
                {
                    return null;
                }
            }

            // Create the descriptor class
            string className = _prefix + "_" + Guid.NewGuid().ToString("N");

            CodeTypeDeclaration classCode = new(className)
            {
                TypeAttributes =
                    System.Reflection.TypeAttributes.NestedPrivate
                    | System.Reflection.TypeAttributes.Sealed,
            };
            classCode.BaseTypes.Add(typeof(HardwiredMethodMemberDescriptor));

            // Create the class constructor
            CodeConstructor ctor = new() { Attributes = MemberAttributes.Assembly };
            classCode.Members.Add(ctor);

            // Create the parameters
            List<HardwireParameterDescriptor> paramDescs =
                HardwireParameterDescriptor.LoadDescriptorsFromTable(table.Get("params").Table);

            int paramNum = paramDescs.Count;
            int optionalNum = paramDescs.Where(p => p.HasDefaultValue).Count();

            // Add initialize call to ctor
            List<CodeExpression> initParams = new();

            initParams.Add(new CodePrimitiveExpression(memberName));
            initParams.Add(
                new CodePrimitiveExpression(
                    table.Get("static").Boolean || table.Get("ctor").Boolean
                )
            );

            initParams.Add(
                new CodeArrayCreateExpression(
                    typeof(ParameterDescriptor),
                    paramDescs.Select(e => e.Expression).ToArray()
                )
            );

            initParams.Add(new CodePrimitiveExpression(table.Get("extension").Boolean));

            ctor.Statements.Add(
                new CodeMethodInvokeExpression(
                    new CodeThisReferenceExpression(),
                    "Initialize",
                    initParams.ToArray()
                )
            );

            // Create the Invoke method : protected override object Invoke(Script script, object obj, object[] pars, int argscount);

            CodeMemberMethod m = new()
            {
                Name = "Invoke",
                Attributes = MemberAttributes.Override | MemberAttributes.Family,
                ReturnType = new CodeTypeReference(typeof(object)),
            };
            m.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Script), "script"));
            m.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "obj"));
            m.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object[]), "pars"));
            m.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "argscount"));

            // get some meta about the method
            bool isVoid = table.Get("ret").String == "System.Void";
            bool isCtor = table.Get("ctor").Boolean;
            bool isStatic = table.Get("static").Boolean;
            bool isExtension = table.Get("extension").Boolean;
            bool specialName = table.Get("special").Boolean;

            string declType = table.Get("decltype").String;
            CodeVariableReferenceExpression paramArray = new("pars");
            CodeExpression paramThis = isStatic
                ? (CodeExpression)(new CodeTypeReferenceExpression(declType))
                : (CodeExpression)(
                    new CodeCastExpression(declType, new CodeVariableReferenceExpression("obj"))
                );

            // Build a list of arguments to the call
            int refparCount = 0;
            List<CodeExpression> paramExps = new();
            for (int i = 0; i < paramDescs.Count; i++)
            {
                HardwireParameterDescriptor p = paramDescs[i];

                CodeExpression paramExp = new CodeCastExpression(
                    paramDescs[i].ParamType,
                    new CodeArrayIndexerExpression(paramArray, new CodePrimitiveExpression(i))
                );

                if (p.IsOut)
                {
                    string varName = GenerateRefParamVariable(refparCount++);
                    CodeVariableDeclarationStatement vd = new(p.ParamType, varName);
                    m.Statements.Add(vd);
                    paramExp = new CodeDirectionExpression(
                        FieldDirection.Out,
                        new CodeVariableReferenceExpression(varName)
                    );
                }
                else if (p.IsRef)
                {
                    string varName = GenerateRefParamVariable(refparCount++);
                    CodeVariableDeclarationStatement vd = new(p.ParamType, varName, paramExp);
                    m.Statements.Add(vd);
                    paramExp = new CodeDirectionExpression(
                        FieldDirection.Ref,
                        new CodeVariableReferenceExpression(varName)
                    );
                }

                paramExps.Add(paramExp);
            }

            // build a list of possible dispatching to default params
            List<CodeExpression[]> calls = new();
            CodeVariableReferenceExpression paramArgsCount = new("argscount");

            for (int callidx = paramNum - optionalNum; callidx <= paramNum; callidx++)
            {
                List<CodeExpression> pars = new();

                // Build the array of parameters expressions
                for (int i = 0; i < callidx; i++)
                {
                    pars.Add(paramExps[i]);
                }

                calls.Add(pars.ToArray());
            }

            // foreach "overload" of default pars, dispatch a call
            for (int i = 0; i < calls.Count - 1; i++)
            {
                int argcnt = calls[i].Length;

                CodeExpression condition = new CodeBinaryOperatorExpression(
                    paramArgsCount,
                    CodeBinaryOperatorType.LessThanOrEqual,
                    new CodePrimitiveExpression(argcnt)
                );

                CodeConditionStatement ifs = new(
                    condition,
                    GenerateCall(
                            table,
                            generator,
                            isVoid,
                            isCtor,
                            isStatic,
                            isExtension,
                            calls[i],
                            paramThis,
                            declType,
                            specialName,
                            refparCount
                        )
                        .OfType<CodeStatement>()
                        .ToArray()
                );

                m.Statements.Add(ifs);
            }

            m.Statements.AddRange(
                GenerateCall(
                    table,
                    generator,
                    isVoid,
                    isCtor,
                    isStatic,
                    isExtension,
                    calls[^1],
                    paramThis,
                    declType,
                    specialName,
                    refparCount
                )
            );

            // close
            classCode.Members.Add(m);
            members.Add(classCode);
            return new CodeExpression[] { new CodeObjectCreateExpression(className) };
        }

        private string GenerateRefParamVariable(int refparIdx)
        {
            return $"refp_{refparIdx}";
        }

        private CodeStatementCollection GenerateCall(
            Table table,
            HardwireCodeGenerationContext generator,
            bool isVoid,
            bool isCtor,
            bool isStatic,
            bool isExtension,
            CodeExpression[] arguments,
            CodeExpression paramThis,
            string declaringType,
            bool specialName,
            int refparCount
        )
        {
            string arrayCtorType = table.Get("arraytype").IsNil()
                ? null
                : table.Get("arraytype").String;

            CodeStatementCollection coll = new();
            CodeExpression retVal = null;

            if (isCtor)
            {
                if (arrayCtorType != null)
                {
                    CodeExpression exp = generator.TargetLanguage.CreateMultidimensionalArray(
                        arrayCtorType,
                        arguments
                    );

                    retVal = new CodeArrayCreateExpression(arrayCtorType, arguments);
                }
                else
                {
                    retVal = new CodeObjectCreateExpression(table.Get("ret").String, arguments);
                }
            }
            else if (specialName)
            {
                GenerateSpecialNameCall(
                    table,
                    generator,
                    isVoid,
                    isCtor,
                    isStatic,
                    isExtension,
                    arguments,
                    paramThis,
                    declaringType,
                    table.Get("name").String,
                    coll
                );
            }
            else
            {
                retVal = new CodeMethodInvokeExpression(
                    paramThis,
                    table.Get("name").String,
                    arguments
                );
            }

            if (retVal != null)
            {
                if (isVoid)
                {
                    coll.Add(new CodeExpressionStatement(retVal));
                    retVal = new CodePropertyReferenceExpression(
                        new CodeTypeReferenceExpression(typeof(DynValue)),
                        (refparCount == 0) ? "Void" : "Nil"
                    );
                }

                if (refparCount == 0)
                {
                    coll.Add(new CodeMethodReturnStatement(retVal));
                }
                else
                {
                    coll.Add(new CodeVariableDeclarationStatement(typeof(object), "retv", retVal));

                    List<CodeExpression> retVals = new();

                    retVals.Add(WrapFromObject(new CodeVariableReferenceExpression("retv")));

                    for (int i = 0; i < refparCount; i++)
                    {
                        retVals.Add(
                            WrapFromObject(
                                new CodeVariableReferenceExpression(GenerateRefParamVariable(i))
                            )
                        );
                    }

                    CodeArrayCreateExpression arrayExp = new(typeof(DynValue), retVals.ToArray());

                    CodeMethodInvokeExpression tupleExp = new(
                        new CodeTypeReferenceExpression(typeof(DynValue)),
                        "NewTuple",
                        arrayExp
                    );

                    coll.Add(new CodeMethodReturnStatement(tupleExp));
                }
            }

            return coll;
        }

        private CodeExpression WrapFromObject(CodeExpression retVal)
        {
            CodeVariableReferenceExpression script = new("script");
            return new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(typeof(DynValue)),
                "FromObject",
                script,
                retVal
            );
        }

        private void GenerateSpecialNameCall(
            Table table,
            HardwireCodeGenerationContext generator,
            bool isVoid,
            bool isCtor,
            bool isStatic,
            bool isExtension,
            CodeExpression[] arguments,
            CodeExpression paramThis,
            string declaringType,
            string specialName,
            CodeStatementCollection coll
        )
        {
            ReflectionSpecialName special = new(specialName);
            CodeExpression exp = null;
            CodeStatement stat = null;

            switch (special.Type)
            {
                case ReflectionSpecialNameType.IndexGetter:
                    if (isStatic)
                    {
                        EmitInvalid(
                            generator,
                            coll,
                            "Static indexers are not supported by hardwired descriptors."
                        );
                    }
                    else
                    {
                        exp = new CodeIndexerExpression(paramThis, arguments);
                    }

                    break;
                case ReflectionSpecialNameType.IndexSetter:
                    if (isStatic)
                    {
                        EmitInvalid(
                            generator,
                            coll,
                            "Static indexers are not supported by hardwired descriptors."
                        );
                    }
                    else
                    {
                        coll.Add(
                            new CodeVariableDeclarationStatement(declaringType, "tmp", paramThis)
                        );

                        stat = new CodeAssignStatement(
                            new CodeIndexerExpression(
                                new CodeVariableReferenceExpression("tmp"),
                                arguments.Take(arguments.Length - 1).ToArray()
                            ),
                            arguments.Last()
                        );
                    }
                    break;
                case ReflectionSpecialNameType.ImplicitCast:
                case ReflectionSpecialNameType.ExplicitCast:
                    exp = paramThis;
                    break;
                case ReflectionSpecialNameType.OperatorTrue:
                    GenerateBooleanOperator(paramThis, coll, true);
                    break;
                case ReflectionSpecialNameType.OperatorFalse:
                    generator.Minor("'false' operator is implemented in terms of 'true' operator.");
                    GenerateBooleanOperator(paramThis, coll, false);
                    break;
                case ReflectionSpecialNameType.PropertyGetter:
                    exp = new CodePropertyReferenceExpression(paramThis, special.Argument);
                    break;
                case ReflectionSpecialNameType.PropertySetter:
                    {
                        if (isStatic)
                        {
                            CodePropertyReferenceExpression memberExp = new(
                                new CodeTypeReferenceExpression(declaringType),
                                special.Argument
                            );

                            coll.Add(new CodeAssignStatement(memberExp, arguments[0]));
                        }
                        else
                        {
                            coll.Add(
                                new CodeVariableDeclarationStatement(
                                    declaringType,
                                    "tmp",
                                    paramThis
                                )
                            );

                            CodePropertyReferenceExpression memberExp = new(
                                new CodeVariableReferenceExpression("tmp"),
                                special.Argument
                            );

                            coll.Add(new CodeAssignStatement(memberExp, arguments[0]));
                        }
                        coll.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
                    }
                    break;
                case ReflectionSpecialNameType.OperatorAdd:
                    exp = BinaryOperator(CodeBinaryOperatorType.Add, paramThis, arguments);
                    break;
                case ReflectionSpecialNameType.OperatorAnd:
                    exp = BinaryOperator(CodeBinaryOperatorType.BitwiseAnd, paramThis, arguments);
                    break;
                case ReflectionSpecialNameType.OperatorOr:
                    exp = BinaryOperator(CodeBinaryOperatorType.BitwiseOr, paramThis, arguments);
                    break;
                case ReflectionSpecialNameType.OperatorDec:
                    exp = generator.TargetLanguage.UnaryDecrement(arguments[0]);
                    if (exp == null)
                    {
                        EmitInvalid(
                            generator,
                            coll,
                            $"Language {generator.TargetLanguage.Name} does not support decrement operators."
                        );
                    }

                    break;
                case ReflectionSpecialNameType.OperatorDiv:
                    exp = BinaryOperator(CodeBinaryOperatorType.Divide, paramThis, arguments);
                    break;
                case ReflectionSpecialNameType.OperatorEq:
                    exp = BinaryOperator(
                        CodeBinaryOperatorType.ValueEquality,
                        paramThis,
                        arguments
                    );
                    break;
                case ReflectionSpecialNameType.OperatorXor:
                    exp = generator.TargetLanguage.BinaryXor(arguments[0], arguments[1]);
                    if (exp == null)
                    {
                        EmitInvalid(
                            generator,
                            coll,
                            $"Language {generator.TargetLanguage.Name} does not support XOR operators."
                        );
                    }

                    break;
                case ReflectionSpecialNameType.OperatorGt:
                    exp = BinaryOperator(CodeBinaryOperatorType.GreaterThan, paramThis, arguments);
                    break;
                case ReflectionSpecialNameType.OperatorGte:
                    exp = BinaryOperator(
                        CodeBinaryOperatorType.GreaterThanOrEqual,
                        paramThis,
                        arguments
                    );
                    break;
                case ReflectionSpecialNameType.OperatorInc:
                    exp = generator.TargetLanguage.UnaryIncrement(arguments[0]);
                    if (exp == null)
                    {
                        EmitInvalid(
                            generator,
                            coll,
                            $"Language {generator.TargetLanguage.Name} does not support increment operators."
                        );
                    }

                    break;
                case ReflectionSpecialNameType.OperatorNeq:
                    exp = BinaryOperator(
                        CodeBinaryOperatorType.IdentityInequality,
                        paramThis,
                        arguments
                    );
                    break;
                case ReflectionSpecialNameType.OperatorLt:
                    exp = BinaryOperator(CodeBinaryOperatorType.LessThan, paramThis, arguments);
                    break;
                case ReflectionSpecialNameType.OperatorLte:
                    exp = BinaryOperator(
                        CodeBinaryOperatorType.LessThanOrEqual,
                        paramThis,
                        arguments
                    );
                    break;
                case ReflectionSpecialNameType.OperatorNot:
                    exp = generator.TargetLanguage.UnaryLogicalNot(arguments[0]);
                    if (exp == null)
                    {
                        EmitInvalid(
                            generator,
                            coll,
                            $"Language {generator.TargetLanguage.Name} does not support logical NOT operators."
                        );
                    }

                    break;
                case ReflectionSpecialNameType.OperatorMod:
                    exp = BinaryOperator(CodeBinaryOperatorType.Modulus, paramThis, arguments);
                    break;
                case ReflectionSpecialNameType.OperatorMul:
                    exp = BinaryOperator(CodeBinaryOperatorType.Multiply, paramThis, arguments);
                    break;
                case ReflectionSpecialNameType.OperatorCompl:
                    exp = generator.TargetLanguage.UnaryOneComplement(arguments[0]);
                    if (exp == null)
                    {
                        EmitInvalid(
                            generator,
                            coll,
                            $"Language {generator.TargetLanguage.Name} does not support bitwise NOT operators."
                        );
                    }

                    break;
                case ReflectionSpecialNameType.OperatorSub:
                    exp = BinaryOperator(CodeBinaryOperatorType.Subtract, paramThis, arguments);
                    break;
                case ReflectionSpecialNameType.OperatorNeg:
                    exp = generator.TargetLanguage.UnaryNegation(arguments[0]);
                    if (exp == null)
                    {
                        EmitInvalid(
                            generator,
                            coll,
                            $"Language {generator.TargetLanguage.Name} does not support negation operators."
                        );
                    }

                    break;
                case ReflectionSpecialNameType.OperatorUnaryPlus:
                    exp = generator.TargetLanguage.UnaryPlus(arguments[0]);
                    if (exp == null)
                    {
                        EmitInvalid(
                            generator,
                            coll,
                            $"Language {generator.TargetLanguage.Name} does not support unary + operators."
                        );
                    }

                    break;
                case ReflectionSpecialNameType.AddEvent:
                case ReflectionSpecialNameType.RemoveEvent:
                    coll.Add(
                        new CodeThrowExceptionStatement(
                            new CodeObjectCreateExpression(
                                typeof(InvalidOperationException),
                                new CodePrimitiveExpression(
                                    "Access to event special methods is not supported by hardwired decriptors."
                                )
                            )
                        )
                    );
                    break;
                default:
                    break;
            }

            if (stat != null)
            {
                coll.Add(stat);
                exp = exp ?? new CodePrimitiveExpression(null);
            }

            if (exp != null)
            {
                coll.Add(new CodeMethodReturnStatement(exp));
            }
        }

        private CodeExpression BinaryOperator(
            CodeBinaryOperatorType codeBinaryOperatorType,
            CodeExpression paramThis,
            CodeExpression[] arguments
        )
        {
            return new CodeBinaryOperatorExpression(
                arguments[0],
                codeBinaryOperatorType,
                arguments[1]
            );
        }

        private void GenerateBooleanOperator(
            CodeExpression paramThis,
            CodeStatementCollection coll,
            bool boolOp
        )
        {
            coll.Add(
                new CodeConditionStatement(
                    paramThis,
                    new CodeStatement[]
                    {
                        new CodeMethodReturnStatement(new CodePrimitiveExpression(boolOp)),
                    },
                    new CodeStatement[]
                    {
                        new CodeMethodReturnStatement(new CodePrimitiveExpression(!boolOp)),
                    }
                )
            );
        }

        private void EmitInvalid(
            HardwireCodeGenerationContext generator,
            CodeStatementCollection coll,
            string message
        )
        {
            generator.Warning(message);
            coll.Add(
                new CodeThrowExceptionStatement(
                    new CodeObjectCreateExpression(
                        typeof(InvalidOperationException),
                        new CodePrimitiveExpression(message)
                    )
                )
            );
        }
    }
}
