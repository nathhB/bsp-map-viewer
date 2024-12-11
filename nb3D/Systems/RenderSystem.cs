using Flecs.NET.Core;
using nb3D.Components;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace nb3D.Systems;

public class RenderSystem
{
    private readonly System<TransformComponent, MeshComponent,ShaderComponent> m_system;
    private Matrix4 m_viewMatrix;
    private Matrix4 m_projectionMatrix;
    private readonly QuakeMapLoader.Map m_map;

    public RenderSystem(World world, QuakeMapLoader.Map map, Shader mapShader)
    {
        m_system = world
            .System<TransformComponent, MeshComponent, ShaderComponent>()
            .Kind<Game.RenderKind>()
            .Run((Iter it, Action<Iter> callback) =>
            {
                var camera = world.Lookup("Camera");

                if (!camera.IsValid()) return;

                var cameraTransform = camera.Get<TransformComponent>();
                var cameraComponent = camera.Get<CameraComponent>();
                m_viewMatrix = ComputeViewMatrix(cameraTransform, cameraComponent);
                m_projectionMatrix = ComputeProjectionMatrix(cameraComponent);

                while (it.Next())
                {
                    callback(it);
                }
            })
            .Each(RenderEntity);
        m_map = map;
    }

    public void RenderMap(Entity camera, Shader shader)
    {
        var cameraTransform = camera.Get<TransformComponent>();
        var cameraComponent = camera.Get<CameraComponent>();

        m_viewMatrix = ComputeViewMatrix(cameraTransform, cameraComponent);
        m_projectionMatrix = ComputeProjectionMatrix(cameraComponent);

        const bool ignoreVisibilityList = false;

        if (ignoreVisibilityList)
        {
            for (var h = 0; h < m_map.HullCount; h++)
            {
                RenderCompleteHull(h, shader);
            }
        }
        else
        {
            for (var h = 0; h < m_map.HullCount; h++)
            {
                var hull = m_map.GetHull(h);

                if (!m_map.TryFindLeafAt(cameraTransform.Position, h, out var leafId))
                {
                    Console.WriteLine("Could not find leaf !");
                    continue;
                }

                // Console.WriteLine($"Hull {h} ({hull.VisLeafCount}) => {m_map.GetLeaf(leafId).VisList}");

                var leaf = m_map.GetLeaf(leafId);

                if (leaf.VisList == -1)
                {
                    RenderCompleteHull(h, shader);
                }
                else
                {
                    RenderLeafVisibilityList(leafId, hull.VisLeafCount, shader);
                }
            }
        }
    }

    private void RenderLeafVisibilityList(int mainLeafId, int leafCount, Shader shader)
    {
        var mainLeaf = m_map.GetLeaf(mainLeafId);
        var visList = m_map.GetVisibilityList(mainLeaf.VisList);
        var listOffset = 0;
        
        for (var leafId = 1; leafId < leafCount; listOffset++)
        {
            // run-length encoding, see: https://www.gamers.org/dEngine/quake/spec/quake-spec34/qkspec_4.htm#BL4
            if (visList[listOffset] == 0)
            {
                leafId += visList[listOffset + 1] * 8;
                listOffset++;
            }
            else
            {
                for (byte bit = 1; bit != 0; bit *= 2, leafId++)
                {
                    var visMask = visList[listOffset];

                    if ((visMask & bit) > 0)
                    {
                        RenderLeaf(leafId, shader);
                    }
                }
            }
        }
    }

    private void RenderCompleteHull(int hullId, Shader shader)
    {
        var hull = m_map.GetHull(hullId);
        var leafId = m_map.GetHullFirstLeafId(hullId);

        for (var l = leafId; l < leafId + hull.VisLeafCount; l++)
        {
            RenderLeaf(l, shader);
        }
    }

    private void RenderLeaf(int leafId, Shader shader)
    {
        var meshes = m_map.GetLeafMeshes(leafId);

        foreach (var mesh in meshes)
        {
            mesh.Bind();

            shader.Use();
            shader.SetUniform("modelMatrix", Matrix4.Identity);
            shader.SetUniform("viewMatrix", m_viewMatrix);
            shader.SetUniform("projectionMatrix", m_projectionMatrix);
            
            GL.DrawElements(PrimitiveType.TriangleFan, mesh.VertexCount, DrawElementsType.UnsignedInt, 0);
        }
    }

    private void RenderEntity(
        Entity entity,
        ref TransformComponent transformComponent,
        ref MeshComponent meshComponent,
        ref ShaderComponent shaderComponent)
    {
        shaderComponent.Shader.Use();
        shaderComponent.Shader.SetUniform("modelMatrix", transformComponent.ComputeMatrix());
        shaderComponent.Shader.SetUniform("viewMatrix", m_viewMatrix);
        shaderComponent.Shader.SetUniform("projectionMatrix", m_projectionMatrix);

        meshComponent.Mesh.Bind();
        GL.DrawElements(PrimitiveType.TriangleFan, meshComponent.Mesh.VertexCount, DrawElementsType.UnsignedInt, 0);
    }

    private Matrix4 ComputeViewMatrix(TransformComponent transformComponent, CameraComponent cameraComponent)
    {
        var dir = Vector3.Normalize(cameraComponent.Target - transformComponent.Position);
        var right = Vector3.Normalize(Vector3.Cross(dir, new Vector3(0, 0, 1)));
        var up = Vector3.Normalize(Vector3.Cross(right, dir));
        var dotX = Vector3.Dot(transformComponent.Position, right);
        var dotY = Vector3.Dot(transformComponent.Position, dir);
        var dotZ = Vector3.Dot(transformComponent.Position, up);
        var lookAtMatrix = new Matrix4(
            new Vector4(right.X, dir.X, up.X, 0),
            new Vector4(right.Y, dir.Y, up.Y, 0),
            new Vector4(right.Z, dir.Z, up.Z, 0),
            new Vector4(-dotX, -dotY, -dotZ, 1)
        );

        // the world uses Quake's coordinate system
        var worldToViewMatrix = new Matrix4(
            1, 0, 0, 0,
            0, 0, -1, 0,
            0, 1, 0, 0,
            0, 0, 0, 1);

        return lookAtMatrix * worldToViewMatrix;
    }
    
    private Matrix4 ComputeProjectionMatrix(CameraComponent cameraComponent)
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(cameraComponent.Fov),
            cameraComponent.AspectRatio,
            cameraComponent.Near,
            cameraComponent.Far);
    }
}