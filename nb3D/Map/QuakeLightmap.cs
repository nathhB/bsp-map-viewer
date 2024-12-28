using System.Data;
using OpenTK.Graphics.OpenGL4;
using GL = OpenTK.Graphics.OpenGL4.GL;

namespace nb3D.Map;

public class QuakeLightmap : IMeshTexture
{
    public const int Size = 16;

    private readonly int m_handle;

    public QuakeLightmap(byte[] rawLightmapData)
    {
        if (rawLightmapData.Length != Size * Size * 3)
        {
            throw new DataException($"Lightmaps must be {Size}x{Size}");
        }

        m_handle = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, m_handle);
        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgb,
            Size,
            Size,
            0,
            PixelFormat.Rgb,
            PixelType.UnsignedByte,
            rawLightmapData);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
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