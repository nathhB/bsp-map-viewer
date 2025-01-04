using OpenTK.Graphics.OpenGL4;
using GL = OpenTK.Graphics.OpenGL4.GL;

namespace nb3D.Map;

public class QuakeLightmap : IMeshTexture
{
    private readonly int m_handle;

    public QuakeLightmap(int width, int height, byte[] rawLightmapData)
    {
        m_handle = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, m_handle);
        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgb,
            width,
            height,
            0,
            PixelFormat.Rgb,
            PixelType.UnsignedByte,
            rawLightmapData);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public void Use(TextureUnit targetUnit)
    {
        GL.ActiveTexture(targetUnit);
        GL.BindTexture(TextureTarget.Texture2D, m_handle);
    }

    public void Dispose()
    {
        GL.DeleteTexture(m_handle);
    }
}