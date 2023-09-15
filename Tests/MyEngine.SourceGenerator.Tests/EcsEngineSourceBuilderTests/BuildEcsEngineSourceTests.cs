﻿using FluentAssertions;

namespace MyEngine.SourceGenerator.Tests.EcsEngineSourceBuilderTests;

public class BuildEcsEngineSourceTests
{
    [Fact]
    public void Should_BuildFullEcsEngineSource()
    {

        var appEntrypointFullyQualifiedName = "MyAppEntrypoint";
        var systemClasses = new[]
        {
            new SystemClassDto()
            {
                FullyQualifiedName = "MySystem1",
                Constructor = new SystemConstructorDto
                {
                    ResourceParameters = new []
                    {
                        new SystemConstructorResourceParameterDto
                        {
                            Name = "Resource1",
                            ParameterIndex = 0
                        },
                        new SystemConstructorResourceParameterDto
                        {
                            Name = "Resource2",
                            ParameterIndex = 1
                        }
                    }
                }
            },
            new SystemClassDto()
            {
                FullyQualifiedName = "MySystem2",
                Constructor = new SystemConstructorDto()
            }
        };

        var startupSystemClasses = new[]
        {
            new StartupSystemClassDto
            {
                FullyQualifiedName = "MyStartupSystemClass1",
                Constructor = new StartupSystemConstructorDto
                {
                    Parameters = new []
                    {
                        new StartupSystemConstructorParameterDto
                        {
                            Name = "Resource1"
                        },
                        new StartupSystemConstructorParameterDto
                        {
                            Name = "Resource2"
                        }
                    }
                }
            },
            new StartupSystemClassDto
            {
                FullyQualifiedName = "MyStartupSystemClass2",
                Constructor = new StartupSystemConstructorDto()
            }
        };

        var (fileName, result) = EcsEngineSourceBuilder.BuildEcsEngineSource(
            startupSystemClasses,
            systemClasses,
            appEntrypointFullyQualifiedName);

        result.Should().Be(
            """
            // <auto-generated />
            #nullable enable
            #pragma warning disable CS0162 // Unreachable code detected

            namespace MyEngine.Runtime
            {
                internal partial class EcsEngine
                {
                    private static partial global::MyEngine.Core.IAppEntrypoint GetAppEntrypoint() => new MyAppEntrypoint();

                    private partial void AddStartupSystemInstantiations()
                    {
                        _startupSystemInstantiations.Add(typeof(MyStartupSystemClass1), () =>
                        {
                            if (_resourceContainer.TryGetResource<Resource1>(out var resource1)
                                && _resourceContainer.TryGetResource<Resource2>(out var resource2))
                            {
                                return new MyStartupSystemClass1(resource1, resource2);
                            }
                            return null;
                        });
                        _startupSystemInstantiations.Add(typeof(MyStartupSystemClass2), () =>
                        {
                            if (true)
                            {
                                return new MyStartupSystemClass2();
                            }
                            return null;
                        });
                    }


                    private partial void AddSystemInstantiations()
                    {
                        _systemInstantiations.Add(typeof(MySystem1), () =>
                        {
                            
                            if (_resourceContainer.TryGetResource<Resource1>(out var resource0)
                                && _resourceContainer.TryGetResource<Resource2>(out var resource1))
                            {
                                return new MySystem1(
                                    resource0,
                                    resource1);
                            }
                            return null;
                        });
                        _systemInstantiations.Add(typeof(MySystem2), () =>
                        {
                            
                            if (true)
                            {
                                return new MySystem2(
                                    );
                            }
                            return null;
                        });
                    }

                    private static partial IReadOnlyCollection<Type> GetAllStartupSystemTypes() =>
                        new Type[]
                        {
                            typeof(MyStartupSystemClass1),
                            typeof(MyStartupSystemClass2)
                        };

                    private static partial IReadOnlyCollection<Type> GetAllSystemTypes() =>
                        new Type[]
                        {
                            typeof(MySystem1),
                            typeof(MySystem2)
                        };

                    private static partial Dictionary<System.Type, System.Type[]> GetUninstantiatedStartupSystems() =>
                        new ()
                        {
                            { typeof(MyStartupSystemClass1), new Type[] { typeof(Resource1), typeof(Resource2) } },
                            { typeof(MyStartupSystemClass2), Array.Empty<Type>() }
                        };

                    private static partial Dictionary<System.Type, System.Type[]> GetUninstantiatedSystems() =>
                        new ()
                        {
                            { typeof(MySystem1), new Type[] { typeof(Resource1), typeof(Resource2) } },
                            { typeof(MySystem2), Array.Empty<Type>() }
                        };
                }
            }

            #pragma warning restore CS0162 // Unreachable code detected
            #nullable restore

            """);
    }
}