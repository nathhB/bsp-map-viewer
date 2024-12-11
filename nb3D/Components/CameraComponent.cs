using OpenTK.Mathematics;

namespace nb3D.Components;

public struct CameraComponent
{
    public float Fov { get; set; }
    public float AspectRatio { get; set; }
    public float Near { get; set; }
    public float Far { get; set; }
    public Vector3 Target { get; set; }
}