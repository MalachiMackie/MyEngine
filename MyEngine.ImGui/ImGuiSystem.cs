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

        ImGuiNET.ImGui.Begin("Debug Info");
        ImGuiNET.ImGui.LabelText("DeltaTime", $"{deltaTime}");
        ImGuiNET.ImGui.LabelText("FPS", $"{1 / deltaTime}");
        ImGuiNET.ImGui.End();

        controller.Render();
    }
}
