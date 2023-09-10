﻿using System;
using System.Collections.Generic;

namespace MyEngine.SourceGenerator
{
    public sealed class StartupSystemConstructorParameter
    {
        public string Name { get; set; }
    }

    public sealed class StartupSystemConstructor
    {
        public static readonly StartupSystemConstructor NoConstructor = new StartupSystemConstructor(Array.Empty<StartupSystemConstructorParameter>());
        public IReadOnlyCollection<StartupSystemConstructorParameter> Parameters { get; }

        public StartupSystemConstructor(IReadOnlyCollection<StartupSystemConstructorParameter> parameters)
        {
            Parameters = parameters;
        }
    }

    public sealed class StartupSystemClass
    {
        public string FullyQualifiedName { get; }
        public StartupSystemConstructor Constructor { get; }

        public StartupSystemClass(string fullyQualifiedName, StartupSystemConstructor constructor)
        {
            FullyQualifiedName = fullyQualifiedName;
            Constructor = constructor;
        }
    }
}
