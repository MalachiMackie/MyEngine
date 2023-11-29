using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyEngine.Rendering;

namespace MyEngine.ImGui;

public class ImGuiSystem : ISystem
{
    private readonly ImGuiResource _resource;
    private readonly RenderStats _renderStats;
    private readonly IQuery<TransformComponent> _entities;

    public ImGuiSystem(ImGuiResource resource, RenderStats renderStats, IQuery<TransformComponent> entities)
    {
        _resource = resource;
        _renderStats = renderStats;
        _entities = entities;
    }

    public void Run(double deltaTime)
    {
        var controller = _resource.Controller;
        controller.Update((float)deltaTime);

        ImGuiNET.ImGui.Begin("Debug Info");
        ImGuiNET.ImGui.LabelText("DeltaTime", $"{deltaTime}");
        ImGuiNET.ImGui.LabelText("FPS", $"{1 / deltaTime}");
        ImGuiNET.ImGui.LabelText("Draw Calls", _renderStats.DrawCalls.ToString());
        ImGuiNET.ImGui.LabelText("Entity Count", _entities.Count().ToString());
        ImGuiNET.ImGui.End();

        controller.Render();
    }
}
