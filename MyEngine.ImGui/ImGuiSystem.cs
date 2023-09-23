using MyEngine.Core.Ecs.Systems;

namespace MyEngine.ImGui;

public class ImGuiSystem : ISystem
{

    private readonly ImGuiResource _resource;

    public ImGuiSystem(ImGuiResource resource)
    {
        _resource = resource;
    }

    public void Run(double deltaTime)
    {
        var controller = _resource.Controller;
        controller.Update((float)deltaTime);
        ImGuiNET.ImGui.ShowDemoWindow();
        controller.Render();
    }
}
