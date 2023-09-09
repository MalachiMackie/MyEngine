namespace MyEngine.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AppEntrypointAttribute : Attribute
{

}

public interface IAppEntrypoint
{
    void BuildApp(AppBuilder builder);
}
