//HintName: AppSystemsInfo.g.cs
namespace MyEngine.SourceGenerator.Tests.Generated
{
    [global::MyEngine.Core.AppSystemsInfo]
    public static class AppSystemsInfo
    {
        [global::MyEngine.Core.SystemClasses]
        public const string SystemClasses = "[{\"FullyQualifiedName\":\"global::MyNamespace.MySystem\",\"Constructor\":{\"TotalParameters\":1,\"QueryParameters\":[],\"ResourceParameters\":[{\"Name\":\"MyNamespace.MyResource\",\"ParameterIndex\":0}]}}]";

        [global::MyEngine.Core.StartupSystemClasses]
        public const string StartupSystemClasses = "[{\"FullyQualifiedName\":\"global::MyNamespace.MyStartupSystem\",\"Constructor\":{\"Parameters\":[{\"Name\":\"MyNamespace.MyResource\"}]}}]";
    }
}
