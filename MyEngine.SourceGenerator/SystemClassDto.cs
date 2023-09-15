using System.Collections.Generic;
using System.Linq;

namespace MyEngine.SourceGenerator
{
    public sealed class SystemClassDto
    {
        public SystemClassDto()
        {

        }

        public SystemClassDto(SystemClass systemClass)
        {
            FullyQualifiedName = systemClass.FullyQualifiedName;
            Constructor = new SystemConstructorDto
            {
                ResourceParameters = systemClass.Constructor.ResourceParameters.Select(x => new SystemConstructorResourceParameterDto
                {
                    Name = x.Value.Name,
                    ParameterIndex = x.Key
                }).ToArray(),
                QueryParameters = systemClass.Constructor.QueryParameters.Select(x => new SystemConstructorQueryParameterDto
                {
                    TypeParameters = x.Value.TypeParameters.Select(y =>
                        new QueryComponentTypeParameterDto
                        {
                            ComponentTypeName = y.ComponentTypeName,
                            MetaComponentType = y.MetaComponentType
                        }).ToArray(),
                    ParameterIndex = x.Key
                }).ToArray()
            };
        }
        public string FullyQualifiedName { get; set; }
        public SystemConstructorDto Constructor { get; set; }
    }

    public sealed class SystemConstructorDto
    {
        public int TotalParameters => QueryParameters.Count + ResourceParameters.Count;
        public IReadOnlyCollection<SystemConstructorQueryParameterDto> QueryParameters { get; set; }
        public IReadOnlyCollection<SystemConstructorResourceParameterDto> ResourceParameters { get; set; }
    }

    public sealed class SystemConstructorQueryParameterDto
    {
        public IReadOnlyList<QueryComponentTypeParameterDto> TypeParameters { get; set; }
        public uint ParameterIndex { get; set; }
    }

    public sealed class SystemConstructorResourceParameterDto
    {
        public string Name { get; set; }
        public uint ParameterIndex { get; set; }
    }


    public sealed class QueryComponentTypeParameterDto
    {
        public MetaComponentType? MetaComponentType { get; set; }

        public string ComponentTypeName { get; set; }
    }
}
