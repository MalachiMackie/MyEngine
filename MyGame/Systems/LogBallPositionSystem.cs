using MyEngine.Core.Ecs;
using MyEngine.Core.Ecs.Components;
using MyEngine.Core.Ecs.Systems;
using MyGame.Components;

namespace MyGame.Systems;

public class LogBallPositionSystem : ISystem
{
    private readonly IQuery<LogPositionComponent, TransformComponent> _query;

    public LogBallPositionSystem(IQuery<LogPositionComponent, TransformComponent> query)
    {
        _query = query;
    }

    public void Run(double deltaTime)
    {
        foreach (var components in _query)
        {
            var (log, transform) = components;

            // Console.WriteLine("Position of {0}: {1}", log.Name, transform.LocalTransform.position);
        }
    }
}
