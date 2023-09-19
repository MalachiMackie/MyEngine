namespace MyEngine.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AppEntrypointAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Class, AllowMultiple =false, Inherited = false)]
public class AppEntrypointInfoAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class AppEntrypointInfoFullyQualifiedNameAttribute : Attribute
{

}

public interface IAppEntrypoint
{
    void BuildApp(AppBuilder builder);
}
