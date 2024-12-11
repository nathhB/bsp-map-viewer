using Flecs.NET.Core;
using nb3D.Components;

namespace nb3D.Systems;

public class RotateSystem
{
    public RotateSystem(World world)
    {
        world.System<TransformComponent, RotateComponent>().Each(ProcessEntity);
    }

    private void ProcessEntity(
        Iter it, int i, ref TransformComponent transformComponent, ref RotateComponent rotateComponent)
    {
        transformComponent.EulerAngles += rotateComponent.Axes * rotateComponent.Angle * it.DeltaTime();
    }
}