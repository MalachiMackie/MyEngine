using System;
using System.Collections.Generic;
using System.Linq;

namespace MyEngine.SourceGenerator
{
    public sealed class StartupSystemConstructorParameterDto
    {
        public string Name { get; set; }
    }

    public sealed class StartupSystemConstructorDto
    {
        public IReadOnlyCollection<StartupSystemConstructorParameterDto> Parameters { get; set; } = Array.Empty<StartupSystemConstructorParameterDto>();
    }

    public sealed class StartupSystemClassDto
    {
        public string FullyQualifiedName { get; set; }
        public StartupSystemConstructorDto Constructor { get; set; }

        public StartupSystemClassDto()
        {

        }

        public StartupSystemClassDto(StartupSystemClass startupSystemClass)
        {
            FullyQualifiedName = startupSystemClass.FullyQualifiedName;
            Constructor = new StartupSystemConstructorDto
            {
                Parameters = startupSystemClass.Constructor.Parameters.Select(x => new StartupSystemConstructorParameterDto { Name = x.Name }).ToArray()
            };
        }
    }

}
