﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CompilingGenerator
{
    [Generator]
    public sealed partial class BindingCodeGenerator : ISourceGenerator
    {
        static readonly string mBindPropAttrName = "EngineNS.UI.Bind.BindPropertyAttribute";
        static readonly string mAttachedPropAttrName = "EngineNS.UI.Bind.AttachedPropertyAttribute";
        static readonly string mBindObjectAttrName = "EngineNS.UI.Bind.BindableObjectAttribute";
        sealed class BindingSyntaxReceiver : ISyntaxContextReceiver
        {
            public List<FieldDeclarationSyntax> CandidateFields = new List<FieldDeclarationSyntax>();
            public List<PropertyDeclarationSyntax> CandidateProperties = new List<PropertyDeclarationSyntax>();
            public List<MethodDeclarationSyntax> CandidateMethods = new List<MethodDeclarationSyntax>();
            public List<ClassDeclarationSyntax> CandidateClasses = new List<ClassDeclarationSyntax>();
            //static bool IsValidFieldDecSyntax(FieldDeclarationSyntax fieldDecSyntax, GeneratorSyntaxContext context)
            //{
            //    foreach(var attributeListSyntax in fieldDecSyntax.AttributeLists)
            //    {
            //        foreach(var attributeSyntax in attributeListSyntax.Attributes)
            //        {
            //            var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
            //            if(symbol != null && symbol.ContainingType.ToDisplayString() == BindingCodeGenerator.mBindPropAttrName)
            //                return true;
            //        }
            //    }
            //    return false;
            //}
            static bool IsValidPropertyDecSyntax(PropertyDeclarationSyntax propertyDecSyntax, GeneratorSyntaxContext context)
            {
                foreach (var attributeListSyntax in propertyDecSyntax.AttributeLists)
                {
                    foreach (var attributeSyntax in attributeListSyntax.Attributes)
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                        if (symbol != null && symbol.ContainingType.ToDisplayString() == BindingCodeGenerator.mBindPropAttrName)
                            return true;
                    }
                }
                return false;
            }
            static bool IsValidMethodDecSyntax(MethodDeclarationSyntax methodDecSyntax, GeneratorSyntaxContext context)
            {
                foreach(var attributeListSyntax in methodDecSyntax.AttributeLists)
                {
                    foreach(var attributeSyntax in attributeListSyntax.Attributes)
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                        if (symbol != null && symbol.ContainingType.ToDisplayString() == BindingCodeGenerator.mAttachedPropAttrName)
                            return true;
                    }
                }
                return false;
            }
            static bool IsValidClassDecSyntax(ClassDeclarationSyntax methodDecSyntax, GeneratorSyntaxContext context)
            {
                foreach(var attributeListSyntax in methodDecSyntax.AttributeLists)
                {
                    foreach(var attributeSyntax in attributeListSyntax.Attributes)
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                        if (symbol != null && symbol.ContainingType.ToDisplayString() == BindingCodeGenerator.mBindObjectAttrName)
                            return true;
                    }
                }
                return false;
            }
            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                var syntaxNode = context.Node;
                if(syntaxNode is PropertyDeclarationSyntax propertyDecSyntax && propertyDecSyntax.AttributeLists.Count > 0)
                {
                    if(IsValidPropertyDecSyntax(propertyDecSyntax, context))
                        CandidateProperties.Add(propertyDecSyntax);
                }
                //else if (syntaxNode is FieldDeclarationSyntax fieldDecSyntax && fieldDecSyntax.AttributeLists.Count > 0)
                //{
                //    if(IsValidFieldDecSyntax(fieldDecSyntax, context))
                //        CandidateFields.Add(fieldDecSyntax);
                //}
                else if(syntaxNode is MethodDeclarationSyntax methodDecSyntax && methodDecSyntax.AttributeLists.Count > 0)
                {
                    if(IsValidMethodDecSyntax(methodDecSyntax, context))
                        CandidateMethods.Add(methodDecSyntax);
                }
                else if(syntaxNode is ClassDeclarationSyntax classDecSyntax && classDecSyntax.AttributeLists.Count > 0)
                {
                    if(IsValidClassDecSyntax(classDecSyntax, context))
                        CandidateClasses.Add(classDecSyntax);
                }
            }
        }

        string ProcessClass(INamedTypeSymbol? classSymbol, List<ISymbol> symbols, ISymbol? bindPropertySymbol, ISymbol? attachedPropertySymbol, ISymbol? bindObjectSymbol, GeneratorExecutionContext context)
        {
            if (classSymbol == null || bindPropertySymbol == null || attachedPropertySymbol == null)
                return "";
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;
            bool baseHasBindObjectInterface = false;
            var baseType = classSymbol.BaseType;
            while((baseType != null))
            {
                var baseTypeDisplayString = baseType.ToDisplayString();
                if (baseTypeDisplayString == "object")
                    break;
                if(baseTypeDisplayString == "EngineNS.UI.Bind.TtBindableObject")
                {
                    baseHasBindObjectInterface = true;
                    break;
                }
                foreach(var ifac in baseType.AllInterfaces)
                {
                    if(ifac.ToDisplayString() == "EngineNS.UI.Bind.IBindableObject")
                    {
                        baseHasBindObjectInterface = true;
                        break;
                    }
                }
                var baseTypeAttributes = baseType.GetAttributes();
                if(baseTypeAttributes.Length > 0)
                {
                    var attData = baseTypeAttributes.SingleOrDefault(ad => (ad.AttributeClass == null) ? false : ad.AttributeClass.Equals(bindObjectSymbol, SymbolEqualityComparer.Default));
                    if (attData != null)
                        baseHasBindObjectInterface = true;
                }
                if (baseHasBindObjectInterface)
                    break;
                baseType = baseType.BaseType;
            }
            string bindImpSource = "";
            string setValueWithPropertyName = $@"
#nullable enable
        public{(baseHasBindObjectInterface ? " override" : " virtual")} void SetValue<T>(in T value, [CallerMemberName] string? propertyName = null)
#nullable disable
        {{";
            bool hasSetValueWithPropertyNameSwitch = false;
            string setValueWithPropertyNameSwitch = $@"
            if(string.IsNullOrEmpty(propertyName))
                return;
            var propertyNameHash = Standart.Hash.xxHash.xxHash64.ComputeHash(propertyName);
            switch(propertyNameHash)
            {{";

            string getValueWithPropertyName = $@"
#nullable enable
        public{(baseHasBindObjectInterface ? " override" : " virtual")} T GetValue<T>([CallerMemberName] string? propertyName = null)
#nullable disable
        {{";
            bool hasGetValueWithPropertyNameSwitch = false;
            string getValueWithPropertyNameSwitch = $@"
            if(string.IsNullOrEmpty(propertyName))
                return default(T);
            var propertyNameHash = Standart.Hash.xxHash.xxHash64.ComputeHash(propertyName);
            switch(propertyNameHash)
            {{";

            string createBindingExpressionMethod = $@"
        public{(baseHasBindObjectInterface ? " override" : " virtual")} EngineNS.UI.Bind.TtBindingExpressionBase CreateBindingExpression<TProperty>(string propertyName, EngineNS.UI.Bind.TtBindingBase binding, EngineNS.UI.Bind.TtBindingExpressionBase parent)
        {{";
            bool hasCreateBindingExpressionMethodSwitch = false;
            string createBindingExpressionMethodSwitch = $@"
            if(string.IsNullOrEmpty(propertyName))
                return null;
            var propertyNameHash = Standart.Hash.xxHash.xxHash64.ComputeHash(propertyName);
            switch(propertyNameHash)
            {{";

            var source = $@"
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace {namespaceName}
{{
    public partial class {className} : EngineNS.UI.Bind.IBindableObject, EngineNS.EGui.Controls.PropertyGrid.IPropertyCustomization
    {{
";
            var bindExprDicName = $"mBindExprDic";
            var setAttachedPropertiesStr = "";
            if(!baseHasBindObjectInterface)
            {
                source += @$"
            protected Dictionary<EngineNS.UI.Bind.TtBindableProperty, EngineNS.UI.Bind.TtBindablePropertyValueBase> {bindExprDicName} = new Dictionary<EngineNS.UI.Bind.TtBindableProperty, EngineNS.UI.Bind.TtBindablePropertyValueBase>();";
             }
            foreach(var symbol in symbols)
            {
                if(symbol is IFieldSymbol)
                {

                }
                else if(symbol is IPropertySymbol)
                {
                    var propSymbol = symbol as IPropertySymbol;
                    if (propSymbol != null)
                    {
                        var propName = propSymbol.Name;
                        var propTypeDisplayName = propSymbol.Type.ToDisplayString();

                        var attData = propSymbol.GetAttributes().Single(ad => (ad.AttributeClass == null) ? false : ad.AttributeClass.Equals(bindPropertySymbol, SymbolEqualityComparer.Default));
                        var defaultValOpt = attData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "DefaultValue").Value;
                        var defValStr = $"default({propTypeDisplayName})";
                        if (!defaultValOpt.IsNull)
                        {
                            defValStr = (defaultValOpt.Value == null) ? "null" : ((defaultValOpt.Value is bool) ? defaultValOpt.Value.ToString().ToLower() : defaultValOpt.Value.ToString());
                        }
                        var valEditorAttOpt = propSymbol.GetAttributes().SingleOrDefault(ad =>
                        {
                            var cls = ad.AttributeClass;
                            while(cls != null)
                            {
                                if (cls.ToDisplayString() == "EngineNS.EGui.Controls.PropertyGrid.PGCustomValueEditorAttribute")
                                    return true;
                                cls = cls.BaseType;
                            }
                            return false;
                        });
                        string valEditorStr = "null";
                        if(valEditorAttOpt != null && valEditorAttOpt.AttributeClass != null)
                        {
                            var editorName = valEditorAttOpt.AttributeClass.ToDisplayString();
                            valEditorStr = @$"typeof({namespaceName}.{className}).GetProperty(""{propName}"").GetCustomAttributes(typeof({editorName}), false)[0] as {editorName}";
                        }
                        var displayNameAttOpt = propSymbol.GetAttributes().SingleOrDefault(ad =>
                        {
                            var cls = ad.AttributeClass;
                            while (cls != null)
                            {
                                if (cls.ToDisplayString() == "EngineNS.UI.Bind.BindPropertyDisplayNameAttribute")
                                    return true;
                                cls = cls.BaseType;
                            }
                            return false;
                        });
                        string displayNameStr = "null";
                        if (displayNameAttOpt != null && displayNameAttOpt.AttributeClass != null)
                        {
                            var attName = displayNameAttOpt.AttributeClass.ToDisplayString();
                            displayNameStr = @$"typeof({namespaceName}.{className}).GetProperty(""{propName}"").GetCustomAttributes(typeof({attName}), false)[0] as {attName}";
                        }
                        var valCategoryAttData = propSymbol.GetAttributes().SingleOrDefault(ad => (ad.AttributeClass == null) ? false : (ad.AttributeClass.ToDisplayString() == "System.ComponentModel.CategoryAttribute"));
                        string categoryValue = "";
                        if (valCategoryAttData != null && valCategoryAttData.ConstructorArguments.Length > 0)
                            categoryValue = System.Convert.ToString(valCategoryAttData.ConstructorArguments[0].Value);
                        var bindPropName = $"{propName}Property";
                        if(!classSymbol.MemberNames.Any(name => bindPropName == name))
                        {
                            source += @$"
        static EngineNS.UI.Bind.TtBindableProperty {bindPropName} = EngineNS.UEngine.Instance.UIBindManager.Register<{propTypeDisplayName}, {className}>(""{propName}"",{(string.IsNullOrEmpty(categoryValue)?"":@$" ""{categoryValue}"",")} {defValStr}, null, {valEditorStr}, {displayNameStr});";
                        }

                        var bindingImpName = $"{className}_BindingImp_{propName}";
                        var bindingExprImpName = $"{className}_BindingExprImp_{propName}";

                        hasCreateBindingExpressionMethodSwitch = true;
                        createBindingExpressionMethodSwitch += $@"
                case {Standart.Hash.xxHash.xxHash64.ComputeHash(propName)}: //{propName}
                    {{
                        var beImp = new {bindingExprImpName}(binding, {propName}, parent);
                        beImp.TargetObject = this;
                        beImp.TargetProperty = (EngineNS.UI.Bind.TtBindableProperty<{propTypeDisplayName}>){bindPropName};
                        return beImp;
                    }}";
                        hasSetValueWithPropertyNameSwitch = true;
                        setValueWithPropertyNameSwitch += $@"
                case {Standart.Hash.xxHash.xxHash64.ComputeHash(propName)}: //{propName}
                    SetValue<T>(value, {bindPropName});
                    break;";
                        hasGetValueWithPropertyNameSwitch = true;
                        getValueWithPropertyNameSwitch += $@"
                case {Standart.Hash.xxHash.xxHash64.ComputeHash(propName)}: //{propName}
                    return GetValue<T>({bindPropName});";

                        bindImpSource += $@"
    public class {bindingExprImpName} : EngineNS.UI.Bind.TtBindingExpression<{propTypeDisplayName}>
    {{
        public {bindingExprImpName}(EngineNS.UI.Bind.TtBindingBase binding, EngineNS.UI.Bind.TtBindingExpressionBase parent)
            : base(binding, parent)
        {{
        }}
        public {bindingExprImpName}(EngineNS.UI.Bind.TtBindingBase binding, {propTypeDisplayName} val, EngineNS.UI.Bind.TtBindingExpressionBase parent)
            : base(binding, parent)
        {{
            mValueStore = val;
        }}
        public override void UpdateSource()
        {{
            if ((Mode == EngineNS.UI.Bind.EBindingMode.OneTime) && (mSetValueTime > 0))
                return;
            mSetValueTime++;
            mFinalValue = mValueStore;
            if(mParentExp != null)
            {{
                mParentExp.UpdateSource();
            }}
            else
            {{
                TargetProperty?.OnValueChanged?.Invoke(TargetObject, TargetProperty, mFinalValue);
                (({namespaceName}.{className})TargetObject).{propName} = mValueStore;
            }}
        }}
    }}
";
                    }
                }
                else if(symbol is IMethodSymbol)
                {
                    var methodSymbol = symbol as IMethodSymbol;
                    if(methodSymbol != null)
                    {
                        if(!methodSymbol.IsStatic)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "BindingGeneric",
                                    "Method is not static",
                                    "Attached property method {0} must be static",
                                    "Error",
                                    DiagnosticSeverity.Error,
                                    true), symbol.Locations.FirstOrDefault(),
                                    symbol.Name));

                        }
                        if(methodSymbol.Parameters.Length != 3)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "BindingGeneric",
                                    "Method need three parameters",
                                    $"Method parameters must be {className}, EngineNS.UI.Bind.TtBindableProperty, [property type]",
                                    "Error",
                                    DiagnosticSeverity.Error,
                                    true), symbol.Locations.FirstOrDefault(),
                                    symbol.Name));
                        }
                        if(methodSymbol.Parameters[0].Type.ToDisplayString() != $"EngineNS.UI.Bind.IBindableObject")
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "BindingGeneric",
                                    "Method first parameter type error",
                                    $"Method first parameter type must be EngineNS.UI.Bind.IBindableObject",
                                    "Error",
                                    DiagnosticSeverity.Error,
                                    true), symbol.Locations.FirstOrDefault(),
                                    symbol.Name));
                        }
                        if (methodSymbol.Parameters[1].Type.ToDisplayString() != "EngineNS.UI.Bind.TtBindableProperty")
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "BindingGeneric",
                                    "Method second parameter type error",
                                    $"Method second parameter type must be EngineNS.UI.Bind.TtBindableProperty",
                                    "Error",
                                    DiagnosticSeverity.Error,
                                    true), symbol.Locations.FirstOrDefault(),
                                    symbol.Name));
                        }
                        var propTypeDisplayName = methodSymbol.Parameters[2].Type.ToDisplayString();
                        var methodName = methodSymbol.Name;
                        var attData = methodSymbol.GetAttributes().Single(ad => (ad.AttributeClass == null) ? false : ad.AttributeClass.Equals(attachedPropertySymbol, SymbolEqualityComparer.Default));
                        var nameOpt = attData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "Name").Value;
                        string nameVal = "";
                        if(nameOpt.Value == null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "BindingGeneric",
                                    "Attached property name not set",
                                    "Must set AttachedPropertyAttribute Name field, like Name = \"your property name\"",
                                    "Error",
                                    DiagnosticSeverity.Error,
                                    true), symbol.Locations.FirstOrDefault(),
                                    symbol.Name));
                        }
                        else
                            nameVal = nameOpt.Value.ToString();
                        var bindPropName = $"{nameVal}Property";
                        var defValOpt = attData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "DefaultValue").Value;
                        var defValStr = $"default({propTypeDisplayName})";
                        if(!defValOpt.IsNull)
                        {
                            defValStr = (defValOpt.Value == null) ? "null" : defValOpt.Value.ToString();
                        }
                        var valCategoryOpt = attData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "Category").Value;
                        string categoryVal = "";
                        if(valCategoryOpt.Value != null)
                            categoryVal = valCategoryOpt.Value.ToString();
                        setAttachedPropertiesStr += $@"
            target.SetValue<{propTypeDisplayName}>({defValStr}, {bindPropName});";
                        var valEditorAttOpt = methodSymbol.GetAttributes().SingleOrDefault(ad =>
                        {
                            var cls = ad.AttributeClass;
                            while(cls != null)
                            {
                                if (cls.ToDisplayString() == "EngineNS.EGui.Controls.PropertyGrid.PGCustomValueEditorAttribute")
                                    return true;
                                cls = cls.BaseType;
                            }
                            return false;
                        });
                        string valEditorStr = "null";
                        if(valEditorAttOpt != null && valEditorAttOpt.AttributeClass != null)
                        {
                            var editorName = valEditorAttOpt.AttributeClass.ToDisplayString();
                            valEditorStr = @$"typeof({namespaceName}.{className}).GetMethod(""{methodName}"", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new Type[] {{ typeof({
                                methodSymbol.Parameters[0].Type.ToDisplayString()}), typeof({
                                methodSymbol.Parameters[1].Type.ToDisplayString()}), typeof({
                                methodSymbol.Parameters[2].Type.ToDisplayString()}) }}, null).GetCustomAttributes(typeof({editorName}), false)[0] as {editorName}";
                        }
                        var displayNameAttOpt = methodSymbol.GetAttributes().SingleOrDefault(ad =>
                        {
                            var cls = ad.AttributeClass;
                            while(cls != null)
                            {
                                if (cls.ToDisplayString() == "EngineNS.UI.Bind.BindPropertyDisplayNameAttribute")
                                    return true;
                                cls = cls.BaseType;
                            }
                            return false;
                        });
                        string displayNameStr = "null";
                        if(displayNameAttOpt != null && displayNameAttOpt.AttributeClass != null)
                        {
                            var attName = displayNameAttOpt.AttributeClass.ToDisplayString();
                            displayNameStr = @$"typeof({namespaceName}.{className}).GetMethod(""{methodName}"", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new Type[] {{ typeof({
                                methodSymbol.Parameters[0].Type.ToDisplayString()}), typeof({
                                methodSymbol.Parameters[1].Type.ToDisplayString()}), typeof({
                                methodSymbol.Parameters[2].Type.ToDisplayString()}) }}, null).GetCustomAttributes(typeof({attName}), false)[0] as {attName}";
                        }
                        if (!classSymbol.MemberNames.Any(name => bindPropName == name))
                        {
                            source += $@"
        static EngineNS.UI.Bind.TtBindableProperty {bindPropName} = EngineNS.UEngine.Instance.UIBindManager.RegisterAttached<{propTypeDisplayName}, {className}>(""{nameVal}"",{(string.IsNullOrEmpty(categoryVal) ? "" : @$" ""{categoryVal}"",")} {defValStr}, {methodName}, {valEditorStr}, {displayNameStr});";
                        }

                        if(!classSymbol.MemberNames.Any(name => $"Get{nameVal}" == name))
                        {
                            source += $@"
        public static {propTypeDisplayName} Get{nameVal}(EngineNS.UI.Bind.IBindableObject target)
        {{
            if(target == null)
                return {defValStr};
            return target.GetValue<{propTypeDisplayName}>({bindPropName});
        }}";
                        }
                        if(!classSymbol.MemberNames.Any(name => $"Set{nameVal}" == name))
                        {
                            source += $@"
        public static void Set{nameVal}(EngineNS.UI.Bind.IBindableObject target, {propTypeDisplayName} value)
        {{
            target?.SetValue<{propTypeDisplayName}>(value, {bindPropName});
        }}";
                        }
                    }
                }
            }

            //            createBindingMethod += $@"
            //            }}
            //            return null;
            //        }}
            //";
            //            source += createBindingMethod;

            createBindingExpressionMethodSwitch += $@"
            }}";
            if (hasCreateBindingExpressionMethodSwitch)
                createBindingExpressionMethod += createBindingExpressionMethodSwitch;
            createBindingExpressionMethod += $@"
            return null;
        }}
";
            source += createBindingExpressionMethod;

            setValueWithPropertyNameSwitch += $@"
            }}";
            if (hasSetValueWithPropertyNameSwitch)
                setValueWithPropertyName += setValueWithPropertyNameSwitch;
            setValueWithPropertyName += $@"
        }}
";
            source += setValueWithPropertyName;
            source += $@"
        public{(baseHasBindObjectInterface ? " override" : " virtual")} void SetValue<T>(in T value, EngineNS.UI.Bind.TtBindableProperty bp)
        {{
            EngineNS.UI.Bind.TtBindablePropertyValueBase bpVal = null;
            lock ({bindExprDicName})
            {{
                if (!{bindExprDicName}.TryGetValue(bp, out bpVal))
                {{
                    bpVal = new EngineNS.UI.Bind.TtAttachedValue<{className}, T>(this);
                    {bindExprDicName}[bp] = bpVal;
                }}
            }}
            bpVal.SetValue<T>(this, bp, in value);
        }}
";
            getValueWithPropertyNameSwitch += $@"
            }}";
            if (hasGetValueWithPropertyNameSwitch)
                getValueWithPropertyName += getValueWithPropertyNameSwitch;
            getValueWithPropertyName += $@"
            return default(T);
        }}
";
            source += getValueWithPropertyName;
            source += $@"
        public{(baseHasBindObjectInterface ? " override" : " virtual")} T GetValue<T>(EngineNS.UI.Bind.TtBindableProperty bp)
        {{
            if (bp == null)
                return default(T);
            EngineNS.UI.Bind.TtBindablePropertyValueBase bpVal = null;
            lock ({bindExprDicName})
            {{
                if (!{bindExprDicName}.TryGetValue(bp, out bpVal))
                    return default(T);
            }}
            return bpVal.GetValue<T>(bp);
        }}
";

            if (!classSymbol.MemberNames.Any( name => "SetBindExpression" == name ))
            {
                source += $@"
        public{(baseHasBindObjectInterface ? " override" : " virtual")} void SetBindExpression(EngineNS.UI.Bind.TtBindableProperty bp, EngineNS.UI.Bind.TtBindingExpressionBase expr)
        {{
            EngineNS.UI.Bind.TtBindablePropertyValueBase bpVal = null;
            lock({bindExprDicName})
            {{
                if(!{bindExprDicName}.TryGetValue(bp, out bpVal))
                {{
                    bpVal = new EngineNS.UI.Bind.TtExpressionValues();
                    {bindExprDicName}[bp] = bpVal;
                }}
            }}
            ((EngineNS.UI.Bind.TtExpressionValues)bpVal).Expressions.Add(expr);
        }}
";
            }

            if(!classSymbol.MemberNames.Any(name => "OnValueChange" == name))
            {
                source += $@"
#nullable enable
        public{(baseHasBindObjectInterface ? " override" : " virtual")} void OnValueChange<T>(T value, [CallerMemberName] string? propertyName = null)
#nullable disable
        {{
            EngineNS.UI.Bind.TtBindableProperty bp = null;";
                var switchCode = $@"
            if(string.IsNullOrEmpty(propertyName))
                return;
            var propertyNameHash = Standart.Hash.xxHash.xxHash64.ComputeHash(propertyName);
            switch(propertyNameHash)
            {{";
                bool hasSwitchCode = false;
                foreach (var symbol in symbols)
                {
                    if (symbol is IFieldSymbol)
                    {

                    }
                    else if (symbol is IPropertySymbol)
                    {
                        hasSwitchCode = true;
                        switchCode += $@"
                case {Standart.Hash.xxHash.xxHash64.ComputeHash(symbol.Name)}: // {symbol.Name}
                    bp = {symbol.Name}Property;
                    break;";
                    }
                }
                switchCode += $@"
            }}";
                if (hasSwitchCode)
                    source += switchCode;
                source += $@"
            if (bp == null)
                return;
            EngineNS.UI.Bind.TtBindablePropertyValueBase bpVal = null;
            lock ({bindExprDicName})
            {{
                if (!{bindExprDicName}.TryGetValue(bp, out bpVal))
                    return;
            }}
            if (bpVal == null)
                return;
            bpVal.SetValue<T>(this, bp, value);
        }}";
            }
            if(!classSymbol.MemberNames.Any(name => "ClearBindExpression" == name))
            {
                source += $@"
        public{(baseHasBindObjectInterface ? " override" : " virtual")} void ClearBindExpression(EngineNS.UI.Bind.TtBindableProperty bp)
        {{
            lock({bindExprDicName})
            {{
                {bindExprDicName}.Remove(bp);
            }}
        }}";
            }
            if (!classSymbol.MemberNames.Any(name => "RemoveAttachedProperties" == name))
            {
                source += $@"
        public{(baseHasBindObjectInterface ? " override" : " virtual")} void RemoveAttachedProperties(System.Type propertiesHostType)
        {{
            var removePros = new System.Collections.Generic.HashSet<EngineNS.UI.Bind.TtBindableProperty>();
            foreach (var data in {bindExprDicName})
            {{
                if(data.Key.HostType.IsEqual(propertiesHostType))
                {{
                    removePros.Add(data.Key);
                }}
            }}
            foreach(var key in removePros)
            {{
                RemoveAttachedProperty(key);
            }}
        }}";
            }
            if (!classSymbol.MemberNames.Any(name => "RemoveAttachedProperty" == name))
            {
                source += $@"
        public{(baseHasBindObjectInterface ? " override" : " virtual")} void RemoveAttachedProperty(EngineNS.UI.Bind.TtBindableProperty property)
        {{
            {bindExprDicName}.Remove(property);
        }}";
            }
            if (!classSymbol.MemberNames.Any(name => "SetAttachedProperties" == name))
            {
                source += $@"
        public{(baseHasBindObjectInterface ? " override" : " virtual")} void SetAttachedProperties(EngineNS.UI.Bind.IBindableObject target)
        {{
                {(baseHasBindObjectInterface ? "base.SetAttachedProperties(target);" : "")}";
                source += setAttachedPropertiesStr;
                source += $@"
        }}";
            }
            if (!classSymbol.MemberNames.Any(name => "IsPropertyVisibleDirty" == name))
            {
                source += $@"
        [System.ComponentModel.Browsable(false)]
        public{(baseHasBindObjectInterface ? " override" : " virtual")} bool IsPropertyVisibleDirty
        {{
            get;
            set;
        }} = false;";
            }
            if(!classSymbol.MemberNames.Any(name => "GetProperties" == name))
            {
                source += $@"
        public{(baseHasBindObjectInterface ? " override" : " virtual")} void GetProperties(ref EngineNS.EGui.Controls.PropertyGrid.CustomPropertyDescriptorCollection collection, bool parentIsValueType)
        {{
            var pros = System.ComponentModel.TypeDescriptor.GetProperties(this);
            collection.InitValue(this, EngineNS.Rtti.UTypeDesc.TypeOf(this.GetType()), pros, parentIsValueType);

            // attached properties
            foreach(var bindData in {bindExprDicName})
            {{
                if(bindData.Value.Type == EngineNS.UI.Bind.TtBindablePropertyValueBase.EType.AttachedValue)
                {{
                    var proDesc = EngineNS.EGui.Controls.PropertyGrid.PropertyCollection.PropertyDescPool.QueryObjectSync();
                    proDesc.Name = bindData.Key.Name;
                    if(bindData.Key.DisplayNameAtt != null)
                        proDesc.DisplayName = bindData.Key.DisplayNameAtt.GetDisplayName(this);
                    proDesc.PropertyType = bindData.Key.PropertyType;
                    proDesc.Category = bindData.Key.Category;
                    proDesc.CustomValueEditor = bindData.Key.CustomValueEditor;
                    collection.Add(proDesc);
                }}
            }}
        }}";
            }
            if(!classSymbol.MemberNames.Any(name => "GetPropertyValue" == name))
            {
                source += $@"
        public{(baseHasBindObjectInterface ? " override" : " virtual")} object GetPropertyValue(string propertyName)
        {{
            var pro = this.GetType().GetProperty(propertyName);
            if (pro != null)
                return pro.GetValue(this);

            foreach(var bindData in {bindExprDicName})
            {{
                if(bindData.Key.Name == propertyName)
                {{
                    return bindData.Value.GetValue<object>(bindData.Key);
                }}
            }}

            return null;
        }}";
            }
            if(!classSymbol.MemberNames.Any(name => "SetPropertyValue" == name))
            {
                source += $@"
        public{(baseHasBindObjectInterface ? " override" : " virtual")} void SetPropertyValue(string propertyName, object value)
        {{
            var pro = this.GetType().GetProperty(propertyName);
            if (pro != null)
                pro.SetValue(this, value);
            else
            {{
                foreach(var bindData in {bindExprDicName})
                {{
                    if(bindData.Key.Name == propertyName)
                    {{
                        bindData.Value.SetValue<object>(this, bindData.Key, in value);
                        break;
                    }}
                }}
            }}
        }}
";
            }
            source += "\r\n    }\r\n";
            source += bindImpSource;
            source += "}\r\n";
            return source;
        }
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not BindingSyntaxReceiver receiver)
                return;

            var compilation = context.Compilation;
            var bindPropertySymbol = compilation.GetTypeByMetadataName(mBindPropAttrName);
            var attachedPropertySymbol = compilation.GetTypeByMetadataName(mAttachedPropAttrName);
            var bindObjectSymbol = compilation.GetTypeByMetadataName(mBindObjectAttrName);
            List<ISymbol> symbols = new List<ISymbol>();
            foreach(var prop in receiver.CandidateProperties)
            {
                var model = compilation.GetSemanticModel(prop.SyntaxTree);
                var propertySymbol = model.GetDeclaredSymbol(prop) as IPropertySymbol;
                if (propertySymbol == null)
                    continue;

                var attData = propertySymbol.GetAttributes().Single(ad => (ad.AttributeClass == null) ? false : ad.AttributeClass.Equals(bindPropertySymbol, SymbolEqualityComparer.Default));
                var isAutoGenOpt = attData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "IsAutoGen").Value;
                bool isAutoGen = true;
                if (!isAutoGenOpt.IsNull)
                {
                    isAutoGen = System.Convert.ToBoolean(isAutoGenOpt.Value);
                }
                if(isAutoGen)
                {
                    symbols.Add(propertySymbol);
                }
            }
            foreach (var field in receiver.CandidateFields)
            {
                var model = compilation.GetSemanticModel(field.SyntaxTree);
                foreach(var variable in field.Declaration.Variables)
                {
                    var fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (fieldSymbol == null) continue;
                    symbols.Add(fieldSymbol);
                }
            }
            foreach(var method in receiver.CandidateMethods)
            {
                var model = compilation.GetSemanticModel(method.SyntaxTree);
                var methodSymbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                if(methodSymbol == null) 
                    continue;
                symbols.Add(methodSymbol);
            }
            foreach(var cls in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(cls.SyntaxTree);
                var clsSymbol = model.GetDeclaredSymbol(cls) as INamedTypeSymbol;
                if (clsSymbol == null) 
                    continue;
                symbols.Add(clsSymbol);
            }
            var groups = symbols.GroupBy(f => (f.Kind == SymbolKind.NamedType)? f : f.ContainingType, SymbolEqualityComparer.Default);
            foreach(var group in groups)
            {
                var key = group.Key;
                if(key == null) continue;
                var classSource = ProcessClass(key as INamedTypeSymbol, group.ToList(), bindPropertySymbol, attachedPropertySymbol, bindObjectSymbol, context);
                context.AddSource($"{key.ToDisplayString()}_bind.g.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new BindingSyntaxReceiver());
        }
    }
}