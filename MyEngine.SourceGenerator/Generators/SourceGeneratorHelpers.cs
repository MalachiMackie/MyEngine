using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyEngine.SourceGenerator.Generators
{
    public interface ISourceGeneratorHelpers
    {
        IEnumerable<ITypeSymbol> GetAllNamespaceTypes(INamespaceSymbol namespaceSymbol);

        string GetFullyQualifiedName(ClassDeclarationSyntax classNode);

        bool IsClassConcreteAndAccessible(SemanticModel semanticModel, ClassDeclarationSyntax classNode);

        bool DoesClassNodeImplementInterface(SemanticModel semanticModel, ClassDeclarationSyntax classNode, string interfaceFullyQualifiedName);

        bool DoesTypeInfoImplementInterface(TypeInfo typeInfo, string interfaceFullyQualifiedName);

        bool DoesClassHaveAccessibleConstructor(ClassDeclarationSyntax classNode);
    }

    public class SourceGeneratorHelpers : ISourceGeneratorHelpers
    {
        public IEnumerable<ITypeSymbol> GetAllNamespaceTypes(INamespaceSymbol namespaceSymbol)
        {
            var types = new List<ITypeSymbol>(namespaceSymbol.GetTypeMembers());

            foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                types.AddRange(GetAllNamespaceTypes(childNamespace));
            }

            return types;
        }

        public string GetFullyQualifiedName(ClassDeclarationSyntax classNode)
        {
            // todo: figure out if there's a better way to do this
            var className = classNode.Identifier.ToString();
            var classNamespaceNode = classNode.Parent;
            string classNamespace;
            if (classNamespaceNode is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                classNamespace = $"global::{namespaceDeclaration.Name}";
            }
            else if (classNamespaceNode is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclarationSyntax)
            {
                classNamespace = $"global::{fileScopedNamespaceDeclarationSyntax.Name}";
            }
            else
            {
                // we assumed a class declaration syntax's parent is always a namespace
                throw new Exception($"ClassNode's parent is not a namespace declaration. It was a {classNamespaceNode.GetType().Name}");
            }

            var fullyQualifiedName = classNamespace.Length == 0
                ? className
                : $"{classNamespace}.{className}";

            return fullyQualifiedName;
        }

        public bool IsClassConcreteAndAccessible(SemanticModel semanticModel, ClassDeclarationSyntax classNode)
        {
            var childTokens = classNode.ChildTokens().ToArray();
            if (childTokens.Any(x => x.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AbstractKeyword)))
            {
                // class is abstract, so can't be constructed

                // todo: error
                return false;
            }

            if (!childTokens.Any(x => x.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
            {
                if (childTokens.Any(x => x.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.InternalKeyword))
                    && (semanticModel.Compilation.AssemblyName == "MyEngine.Core"
                    || semanticModel.Compilation.AssemblyName == "MyEngine.Input"))
                {
                    // this is one of our projects, and the system is internal, so we can access it from the runtime
                    return true;
                }
                // class is not public, so can't be accessed

                // todo: error
                return false;
            }

            return true;
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
                if (DoesTypeInfoImplementInterface(typeInfo, interfaceFullyQualifiedName))
                {
                    return true;
                }
            }

            return false;
        }

        public bool DoesTypeInfoImplementInterface(TypeInfo typeInfo, string interfaceFullyQualifiedName)
        {
            if (typeInfo.Type is null)
            {
                return false;
            }

            return typeInfo.Type.ToDisplayString() == interfaceFullyQualifiedName
                || typeInfo.Type.AllInterfaces.Any(x => x.ToDisplayString() == interfaceFullyQualifiedName);
        }

        public bool DoesClassHaveAccessibleConstructor(ClassDeclarationSyntax classNode)
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
    }
}
