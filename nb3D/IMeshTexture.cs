using OpenTK.Graphics.OpenGL4;

namespace nb3D;

public interface IMeshTexture : IDisposable
{
    public void Use(TextureUnit targetUnit);
}