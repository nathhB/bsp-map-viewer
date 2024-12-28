using Flecs.NET.Core;
using nb3D.Components;
using nb3D.Map;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace nb3D;

public class SceneRenderer
{
    public readonly struct Options
    {
        public static readonly Options Default = new Options(true);

        public readonly bool EnableLightmaps;

        public Options(bool enableLightmaps)
        {
            EnableLightmaps = enableLightmaps;
        }
    }

    private readonly Query<TransformComponent,MeshComponent,ShaderComponent> m_renderableEntitiesQuery;
    private readonly World m_world;
    private readonly QuakeMap m_map;
    private readonly Shader m_mapShader;
    private readonly Options m_options;
    private Matrix4 m_viewMatrix;
    private Matrix4 m_projectionMatrix;

    public SceneRenderer(World world, QuakeMap map, Shader mapShader, Options options)
    {
        m_world = world;
        m_map = map;
        m_mapShader = mapShader;
        m_options = options;
        m_renderableEntitiesQuery = world.QueryBuilder<TransformComponent, MeshComponent, ShaderComponent>()
            .Cached()
            .Build();
    }
    
    public void Render()
    {
        var camera = m_world.Lookup("Camera");
        
        if (!camera.IsValid()) return;
        
        var cameraTransform = camera.Get<TransformComponent>();
        var cameraComponent = camera.Get<CameraComponent>();
        m_viewMatrix = ComputeViewMatrix(cameraTransform, cameraComponent);
        m_projectionMatrix = ComputeProjectionMatrix(cameraComponent);

        RenderMap(camera);
        m_renderableEntitiesQuery.Each(RenderEntity);
    }

    private void RenderEntity(
        Entity entity,
        ref TransformComponent transformComp,
        ref MeshComponent meshComp,
        ref ShaderComponent shaderComp)
    {
        shaderComp.Shader.Use();
        shaderComp.Shader.SetUniform("modelMatrix", transformComp.ComputeMatrix());
        shaderComp.Shader.SetUniform("viewMatrix", m_viewMatrix);
        shaderComp.Shader.SetUniform("projectionMatrix", m_projectionMatrix);
            
        meshComp.Mesh.Bind();
        GL.DrawElements(PrimitiveType.Triangles, meshComp.Mesh.VertexCount, DrawElementsType.UnsignedInt, 0);
    }
    
    private void RenderMap(Entity camera)
    {
        var cameraTransform = camera.Get<TransformComponent>();
    
        m_mapShader.Use();
        m_mapShader.SetUniform("texture0", 0);
        m_mapShader.SetUniform("texture1", 1); // used for lightmaps
        m_mapShader.SetUniform("modelMatrix", Matrix4.Identity);
        m_mapShader.SetUniform("viewMatrix", m_viewMatrix);
        m_mapShader.SetUniform("projectionMatrix", m_projectionMatrix);
    
        // for (var h = 0; h < m_map.HullCount; h++)
        for (var h = 0; h < 1; h++)
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
                RenderCompleteHull(h);
            }
            else
            {
                RenderLeafVisibilityList(leafId, hull.VisLeafCount);
            }
        }
    }
    
    private void RenderLeafVisibilityList(int mainLeafId, int leafCount)
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
                        RenderLeaf(leafId);
                    }
                }
            }
        }
    }
    
    private void RenderCompleteHull(int hullId)
    {
        var hull = m_map.GetHull(hullId);
        var leafId = m_map.GetHullFirstLeafId(hullId);
    
        for (var l = leafId; l < leafId + hull.VisLeafCount; l++)
        {
            RenderLeaf(l);
        }
    }
    
    private void RenderLeaf(int leafId)
    {
        var surfaceMeshes = m_map.GetLeafMeshes(leafId);
    
        foreach (var surfaceMesh in surfaceMeshes)
        {
            var lightmap = m_options.EnableLightmaps ? surfaceMesh.Lightmap : m_map.FullLitLightmap;
    
            surfaceMesh.Mesh.Bind();
            lightmap.Use(TextureUnit.Texture1);
            GL.DrawElements(PrimitiveType.TriangleFan, surfaceMesh.Mesh.VertexCount, DrawElementsType.UnsignedInt, 0);
        }
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
        // convert to OpenGL's coordinate system
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