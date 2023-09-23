using MyEngine.Core;

namespace MyEngine.ImGui;

// todo: remove this when I can say to run ImGuiSystem before RenderSystem and after PostUpdate
public class ImGuiSystemStage : ISystemStage
{
    public static ImGuiSystemStage Instance { get; } = new();
    private ImGuiSystemStage()
    {

    }

    public bool Equals(ISystemStage? other)
    {
        return other is ImGuiSystemStage;
    }
}

public class ImGuiPlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder
            .AddSystemStage(ImGuiSystemStage.Instance, 6)
            .AddSystem<InitailizeImGuiSystem>(PreUpdateSystemStage.Instance)
            .AddSystem<ImGuiSystem>(ImGuiSystemStage.Instance);
    }
}
