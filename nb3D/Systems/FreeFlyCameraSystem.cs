using Flecs.NET.Core;
using nb3D.Components;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace nb3D.Systems;

public class FreeFlyCameraSystem
{
    private readonly System<TransformComponent, CameraComponent, FreeFlyComponent> m_system;
    private readonly KeyboardState m_keyboardState;

    public FreeFlyCameraSystem(World world, KeyboardState keyboardState)
    {
        m_keyboardState = keyboardState;

        m_system = world
            .System<TransformComponent, CameraComponent, FreeFlyComponent>()
            .Each(ProcessEntity);
    }

    private void ProcessEntity(Iter it, int i, ref TransformComponent transformComponent,
        ref CameraComponent cameraComponent, ref FreeFlyComponent freeFlyComponent)
    {
        var dt = it.DeltaTime();
        var eulerAngles = transformComponent.EulerAngles;

        if (m_keyboardState.IsKeyDown(Keys.Right))
        {
            eulerAngles.Z += freeFlyComponent.RotateAngle * dt;
        }
        else if (m_keyboardState.IsKeyDown(Keys.Left))
        {
            eulerAngles.Z -= freeFlyComponent.RotateAngle * dt;
        }

        transformComponent.EulerAngles = eulerAngles;

        var forwardVector = new Vector3(0, 1, 0);
        var upVector = new Vector3(0, 0, 1);
        var moveVector = Vector3.Zero;

        forwardVector *= Matrix3.CreateRotationZ(-transformComponent.EulerAngles.Z);

        var rightVector = Vector3.Cross(forwardVector, new Vector3(0, 0, 1));
        
        if (m_keyboardState.IsKeyDown(Keys.W))
        {
            moveVector += forwardVector;
        }
        if (m_keyboardState.IsKeyDown(Keys.S))
        {
            moveVector -= forwardVector;
        }
        if (m_keyboardState.IsKeyDown(Keys.D))
        {
            moveVector += rightVector;
        }
        if (m_keyboardState.IsKeyDown(Keys.A))
        {
            moveVector -= rightVector;
        }
        if (m_keyboardState.IsKeyDown(Keys.Up))
        {
            moveVector += upVector;
        }
        if (m_keyboardState.IsKeyDown(Keys.Down))
        {
            moveVector -= upVector;
        }

        transformComponent.Position += moveVector * freeFlyComponent.MoveSpeed * dt;
        cameraComponent.Target = transformComponent.Position + forwardVector;
    }
}