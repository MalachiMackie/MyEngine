namespace MyEngine.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AppSystemsInfoAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class SystemClassesAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class StartupSystemClassesAttribute : Attribute
{

}
