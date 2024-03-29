using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyEngine.SourceGenerator.Generators
{
    public class SourceGeneratorHelpers
    {
        public IEnumerable<ITypeSymbol> GetAllNamespaceTypes(INamespaceSymbol namespaceSymbol)
        {
            // todo: nested types
            var types = new List<ITypeSymbol>(namespaceSymbol.GetTypeMembers());

            foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                types.AddRange(GetAllNamespaceTypes(childNamespace));
            }

            return types;
        }

        public bool DoesAssemblyGiveEngineRuntimeAccessToInternals(IAssemblySymbol assembly)
        {
            return assembly.GetAttributes()
                .Where(x => x.AttributeClass != null)
                .Where(x => x.AttributeClass!.ToDisplayString() == typeof(InternalsVisibleToAttribute).FullName)
                .Where(x => x.ConstructorArguments.Length == 1
                    && x.ConstructorArguments[0].Value is string str
                    && str == "MyEngine.Runtime")
                .Any();
        }

        public bool IsClassConcreteAndAccessible(ISymbol symbol)
        {
            if (!(symbol is INamedTypeSymbol classSymbol))
            {
                return false;
            }

            if (classSymbol.IsAbstract)
            {
                return false;
            }

            if (classSymbol.DeclaredAccessibility == Accessibility.Public)
            {
                return true;
            }

            // NotApplicable when no accessibility is declared, so internal is the default for classes
            if (classSymbol.DeclaredAccessibility == Accessibility.Internal || classSymbol.DeclaredAccessibility == Accessibility.NotApplicable)
            {
                var assembly = classSymbol.ContainingAssembly;

                return DoesAssemblyGiveEngineRuntimeAccessToInternals(assembly);
            }

            return false;
        }

        public IEnumerable<IMethodSymbol> GetAccessibleConstructors(
            INamedTypeSymbol classSymbol)
        {
            var internalsAccessible = DoesAssemblyGiveEngineRuntimeAccessToInternals(classSymbol.ContainingAssembly);

            return classSymbol.InstanceConstructors
                .Where(x =>
                {
                    return x.DeclaredAccessibility == Accessibility.Public || (x.DeclaredAccessibility == Accessibility.Internal && internalsAccessible);
                });
        }

        public bool DoesClassNodeImplementInterface(INamedTypeSymbol classSymbol, string interfaceFullyQualifiedName)
        {
            return classSymbol.AllInterfaces.Any(x => x.ToDisplayString() == interfaceFullyQualifiedName);
        }

        public bool DoesClassNodeImplementInterface(SemanticModel semanticModel, ClassDeclarationSyntax classNode, string interfaceFullyQualifiedName)
        {
            if (classNode.BaseList is null)
            {
                return false;
            }

            foreach (var type in classNode.BaseList.Types)
            {
                var typeInfo = semanticModel.GetTypeInfo(type.Type);
                if (typeInfo.Type != null && DoesTypeSymbolImplementInterface(typeInfo.Type, interfaceFullyQualifiedName))
                {
                    return true;
                }
            }

            return false;
        }

        public bool DoesTypeSymbolImplementInterface(ITypeSymbol typeInfo, string interfaceFullyQualifiedName)
        {
            return typeInfo.ToDisplayString() == interfaceFullyQualifiedName
                || typeInfo.AllInterfaces.Any(x => x.ToDisplayString() == interfaceFullyQualifiedName);
        }

        public bool DoesClassHaveAccessibleEmptyConstructor(INamedTypeSymbol classSymbol)
        {
            return classSymbol.InstanceConstructors.Any(x => x.Parameters.Length == 0 && x.DeclaredAccessibility == Accessibility.Public);
        }

        public bool DoesClassHaveAccessibleEmptyConstructor(ClassDeclarationSyntax classNode)
        {
            var constructorDeclarations = classNode.ChildNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .ToArray();

            // no constructor means public constructor
            if (constructorDeclarations.Length == 0)
            {
                return true;
            }

            var publicConstructors = constructorDeclarations
                .Where(x => x.ChildTokens().Any(y => y.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
                .ToArray();

            // todo: add analyzer that reports these warnings
            if (publicConstructors.Length == 0)
            {
                return false;
            }

            var emptyConstructor = publicConstructors
                .FirstOrDefault(x =>
                {
                    var parameterList = x.ChildNodes().OfType<ParameterListSyntax>().First();
                    return parameterList.Parameters.Count == 0;
                });

            if (emptyConstructor is null)
            {
                return false;
            }

            // found a public empty constructor 
            return true;
        }

        private const string CoreNamespace = "MyEngine.Core";

        public static readonly Dictionary<EngineAttribute, EngineAttributeInfo> AttributeNames = new[]
        {
            new EngineAttributeInfo(EngineAttribute.AppEntrypoint, $"{CoreNamespace}.AppEntrypointAttribute", $"[global::{CoreNamespace}.AppEntrypoint]"),
        }.ToDictionary(x => x.Attribute);

        public bool DoesAttributeMatch(AttributeData attributeData, EngineAttribute expectedAttribute)
        {
            if (attributeData.AttributeClass is null)
            {
                return false;
            }

            return attributeData.AttributeClass.ToDisplayString() == AttributeNames[expectedAttribute].FullyQualifiedName;
        }
    }

    public class EngineAttributeInfo
    {
        public EngineAttributeInfo(EngineAttribute attribute, string fullyQualifiedName, string codeUsage)
        {
            Attribute = attribute;
            FullyQualifiedName = fullyQualifiedName;
            CodeUsage = codeUsage;
        }

        public EngineAttribute Attribute { get; }
        public string FullyQualifiedName { get; }
        public string CodeUsage { get; }
    }
}
