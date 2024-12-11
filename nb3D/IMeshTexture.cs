using OpenTK.Graphics.OpenGL4;

namespace nb3D;

public interface IMeshTexture : IDisposable
{
    public int DataLength { get; }

    public void SetVertexAttributes(int vertexDataLength);
    public void Use(TextureUnit targetUnit);
}