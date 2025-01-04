using System.Diagnostics;
using Flecs.NET.Core;
using nb3D.Components;
using nb3D.Map;
using nb3D.Systems;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace nb3D;

public class Game : GameWindow
{
    public struct RenderKind;

    private readonly FreeFlyCameraSystem m_freeFlyCameraSystem;
    private readonly QuakeMap m_map;
    private readonly Stopwatch m_frameTimeStopwatch = new Stopwatch();
    private World m_world = World.Create();
    private Shader m_mapShader;
    private SceneRenderer m_sceneRenderer;

    public Game(int width, int height, string title) :
        base(GameWindowSettings.Default,
            new NativeWindowSettings { ClientSize = (width, height), Title = title })
    {
        m_map = QuakeMapLoader.Load(
            "Assets/Maps/de_dust2.bsp",
            "Assets/Maps/palette.lmp",
            "Assets/WADs/de_aztec.wad",
            "Assets/WADs/cs_dust.wad");
        m_freeFlyCameraSystem = new FreeFlyCameraSystem(m_world, KeyboardState);
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        
        LoadShaders();
        CreateCamera();

        m_sceneRenderer = new SceneRenderer(m_world, m_map, m_mapShader, SceneRenderer.Options.Default);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        m_world.Progress((float)args.Time);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        m_frameTimeStopwatch.Restart();
        base.OnRenderFrame(args);
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        m_sceneRenderer.Render();
        SwapBuffers();
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
        entity.Set(new TransformComponent {Position = new Vector3(1766, 3888, 288)});
        entity.Set(new CameraComponent
        {
            Fov = 90f,
            Near = 0.1f,
            Far = 5000f,
            AspectRatio = (float)Size.X / Size.Y
        });
        entity.Set(new FreeFlyComponent(500, 5));
    }

    private void LoadShaders()
    {
        Shader.CompileFromFiles("Assets/Shaders/shader.vert", "Assets/Shaders/shader.frag");
        Shader.CompileFromFiles("Assets/Shaders/shader2.vert", "Assets/Shaders/shader2.frag");
        Shader.CompileFromFiles("Assets/Shaders/shader3.vert", "Assets/Shaders/shader3.frag");
        m_mapShader = Shader.CompileFromFiles("Assets/Shaders/mapShader.vert", "Assets/Shaders/mapShader.frag");
    }
}