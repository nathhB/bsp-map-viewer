using OpenTK.Mathematics;

namespace nb3D.Components;

public struct TransformComponent()
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 EulerAngles { get; set; } = Vector3.Zero;

    public Matrix4 ComputeMatrix()
    {
        var translation = Matrix4.CreateTranslation(Position);
        // var rotationX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(EulerAngles.X));
        // var rotationY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(EulerAngles.Y));
        // var mat = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(EulerAngles.Z));

        /*mat.Row0.W = Position.X;
        mat.Row1.W = Position.Y;
        mat.Row2.W = Position.Z;
        mat.Row3 = new Vector4(0, 0, 0, 1);*/

        return Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(EulerAngles.Z)) * translation;
    }
}