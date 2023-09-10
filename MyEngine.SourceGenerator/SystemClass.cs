using System;
using System.Collections.Generic;

namespace MyEngine.SourceGenerator
{
    public sealed class SystemClass
    {
        public string FullyQualifiedName { get; }
        public SystemConstructor Constructor { get; }

        public SystemClass(string fullyQualifiedName, SystemConstructor constructor)
        {
            FullyQualifiedName = fullyQualifiedName;
            Constructor = constructor;
        }
    }

    public sealed class SystemConstructor
    {
        public static readonly SystemConstructor NoConstructor = new SystemConstructor(Array.Empty<SystemConstructorParameter>());
        public IReadOnlyCollection<SystemConstructorParameter> Parameters { get; }

        public SystemConstructor(IReadOnlyCollection<SystemConstructorParameter> parameters)
        {
            Parameters = parameters;
        }
    }

    public sealed class SystemConstructorParameter
    {
        public string Name { get; set; }

        public bool IsResource { get; set; }

        public IReadOnlyList<QueryComponentTypeParameter> QueryComponentTypeParameters { get; set; }
    }


    public sealed class QueryComponentTypeParameter
    {
        public MetaComponentType? MetaComponentType { get; set; }

        public string ComponentTypeName { get; set; }
    } 

    public enum MetaComponentType
    {
        OptionalComponent
    }
}
