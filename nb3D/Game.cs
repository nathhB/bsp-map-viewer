using System.Diagnostics;
using Flecs.NET.Core;
using nb3D.Components;
using nb3D.Systems;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using StbImageSharp;

namespace nb3D;

public class Game : GameWindow
{
    public struct RenderKind;

    private World m_world = World.Create();
    private readonly Pipeline m_defaultPipeline;
    private readonly Pipeline m_renderPipeline;
    private Shader m_shader = null!;
    private Shader m_shader2 = null!;
    private Shader m_shader3 = null!;
    private Mesh m_planeMesh = null!;
    private Mesh m_cubeMesh = null!;
    private RenderSystem m_renderSystem;
    private readonly FreeFlyCameraSystem m_freeFlyCameraSystem;
    private readonly RotateSystem m_rotateSystem;
    private readonly QuakeMapLoader.Map m_map;
    private Stopwatch m_frameTimeStopwatch = new Stopwatch();

    public Game(int width, int height, string title) :
        base(GameWindowSettings.Default,
            new NativeWindowSettings { ClientSize = (width, height), Title = title })
    {
        m_defaultPipeline = m_world.Pipeline().With(Ecs.System).Build();
        m_renderPipeline = m_world.Pipeline().With(Ecs.System).With<RenderKind>().Build();
        m_map = QuakeMapLoader.Load("Assets/Maps/start.bsp", "Assets/Maps/palette.lmp");
        m_freeFlyCameraSystem = new FreeFlyCameraSystem(m_world, KeyboardState);
        m_rotateSystem = new RotateSystem(m_world);
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        
        CreateShader();
        LoadMeshes();
        CreateCamera();
        // CreateMeshEntity(new Vector3(0, 0, 0), m_planeMesh, m_shader, rotate: false);
        // CreateMeshEntity(new Vector3(1, 2520, 1), m_cubeMesh, m_shader3, false);
        /*CreateCube(new Vector3(0, 10, 6));
        CreateCube(new Vector3(3, 10, 0));
        CreateCube(new Vector3(-3, 10, 0));*/

        m_renderSystem = new RenderSystem(m_world, m_map, m_shader2);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        m_world.SetPipeline(m_defaultPipeline);
        m_world.Progress((float)args.Time);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        m_frameTimeStopwatch.Restart();
        base.OnRenderFrame(args);
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        m_renderSystem.RenderMap(m_world.Lookup("Camera"), m_shader2);
        m_world.SetPipeline(m_renderPipeline);
        m_world.Progress();

        SwapBuffers();

        var frameTimeMs = m_frameTimeStopwatch.ElapsedMilliseconds;
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    private void CreateCamera()
    {
        var entity = m_world.Entity("Camera");

        // entity.Set(new TransformComponent {Position = new Vector3(-704, 2450, 0)});
        entity.Set(new TransformComponent {Position = new Vector3(544, 288, 32)});
        entity.Set(new CameraComponent
        {
            Fov = 90f,
            Near = 0.1f,
            Far = 5000f,
            AspectRatio = (float)Size.X / Size.Y
        });
        entity.Set(new FreeFlyComponent(500, 5));
    }

    private void CreateMeshEntity(Vector3 position, Mesh mesh, Shader shader, bool rotate = false)
    {
        var entity = m_world.Entity();
    
        entity.Set(new TransformComponent {Position = position, EulerAngles = new Vector3(0, 0, 0)});
        entity.Set(new MeshComponent(mesh));
        entity.Set(new ShaderComponent(shader));

        if (rotate)
        {
            entity.Set(new RotateComponent(new Vector3(0, 0, 1), 20));
        }
    }

    private void CreateShader()
    {
        m_shader = Shader.CompileFromFiles("Assets/Shaders/shader.vert", "Assets/Shaders/shader.frag");
        m_shader2 = Shader.CompileFromFiles("Assets/Shaders/shader2.vert", "Assets/Shaders/shader2.frag");
        m_shader3 = Shader.CompileFromFiles("Assets/Shaders/shader3.vert", "Assets/Shaders/shader3.frag");
    }
   
    private void LoadMeshes()
    {
        var cubeDef = WavefrontImporter.Import("Assets/Models/cube3.obj", "Assets/Models/cube3.mtl");
        // var planeDef = WavefrontImporter.Import("Assets/Models/plane.obj", "Assets/Models/plane.mtl");

        m_cubeMesh = MeshBuilder.Build(cubeDef);
        /*m_planeMesh = MeshBuilder.BuildFromConvexPolygon(new[]
        {
            new Vector3(608, 0, 176),
            new Vector3(592, 0, 176),
            new Vector3(592, 0, 16),
            new Vector3(608, 0, 16),
        });*/
        /*m_planeMesh = MeshBuilder.BuildFromConvexPolygon(new[]
        {
            new Vector3(8, 0, 24),
            new Vector3(-8, 0, 24),
            new Vector3(-8, 0, 16),
            new Vector3(8, 0, 16),
        });*/
        /*m_planeMesh = MeshBuilder.BuildFromConvexPolygon(new[]
        {
            new Vector3(8, 0, -9 + 24),
            new Vector3(8, 0, -9 + 8),
            new Vector3(-8, 0, -9 + 8),
            new Vector3(-8, 0, -9 + 24),
        });*/
    }
}