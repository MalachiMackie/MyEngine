using MyEngine.Core.Ecs.Systems;
using MyEngine.Rendering;

namespace MyEngine.ImGui;

public class ImGuiSystem : ISystem
{
    private readonly ImGuiResource _resource;
    private readonly RenderStats _renderStats; 

    public ImGuiSystem(ImGuiResource resource, RenderStats renderStats)
    {
        _resource = resource;
        _renderStats = renderStats;
    }

    public void Run(double deltaTime)
    {
        var controller = _resource.Controller;
        controller.Update((float)deltaTime);

        ImGuiNET.ImGui.Begin("Debug Info");
        ImGuiNET.ImGui.LabelText("DeltaTime", $"{deltaTime}");
        ImGuiNET.ImGui.LabelText("FPS", $"{1 / deltaTime}");
        ImGuiNET.ImGui.LabelText("Draw Calls", _renderStats.DrawCalls.ToString());
        ImGuiNET.ImGui.End();

        controller.Render();
    }
}
