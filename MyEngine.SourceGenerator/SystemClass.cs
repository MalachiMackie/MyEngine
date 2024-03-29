using System.Collections.Generic;
using System.Linq;

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
        public uint TotalParameters { get; private set; }
        public Dictionary<uint, SystemConstructorQueryParameter> QueryParameters { get; } = new Dictionary<uint, SystemConstructorQueryParameter>();
        public Dictionary<uint, SystemConstructorResourceParameter> ResourceParameters { get; } = new Dictionary<uint, SystemConstructorResourceParameter>();

        public SystemConstructor()
        {
        }

        public void AddParameter(SystemConstructorQueryParameter parameter)
        {
            QueryParameters[TotalParameters] = parameter;
            TotalParameters++;
        }

        public SystemConstructor WithParameter(SystemConstructorQueryParameter parameter)
        {
            AddParameter(parameter);
            return this;
        }

        public void AddParameter(SystemConstructorResourceParameter parameter)
        {
            ResourceParameters[TotalParameters] = parameter;
            TotalParameters++;
        }

        public SystemConstructor WithParameter(SystemConstructorResourceParameter parameter)
        {
            AddParameter(parameter);
            return this;
        }
    }

    public sealed class SystemConstructorQueryParameter
    {
        public SystemConstructorQueryParameter(QueryComponentTypeParameter firstTypeParameter, IEnumerable<QueryComponentTypeParameter> restTypeParameters)
        {
            TypeParameters = restTypeParameters.Prepend(firstTypeParameter).ToArray();
        }

        public IReadOnlyList<QueryComponentTypeParameter> TypeParameters { get; }
    }

    public sealed class SystemConstructorResourceParameter
    {
        public SystemConstructorResourceParameter(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }


    public sealed class QueryComponentTypeParameter
    {
        public QueryComponentTypeParameter(string componentTypeName, MetaComponentType? metaComponentType)
        {
            ComponentTypeName = componentTypeName;
            MetaComponentType = metaComponentType;
        }

        public MetaComponentType? MetaComponentType { get; }

        public string ComponentTypeName { get; }
    }

    public enum MetaComponentType
    {
        OptionalComponent
    }
}
