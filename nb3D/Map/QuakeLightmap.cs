using OpenTK.Graphics.OpenGL4;
using GL = OpenTK.Graphics.OpenGL4.GL;

namespace nb3D.Map;

public class QuakeLightmap : IMeshTexture
{
    private readonly int m_handle;
    private readonly byte[] m_rawLightmapData;

    public int Width { get; }
    public int Height { get; }

    public QuakeLightmap(int width, int height, byte[] rawLightmapData)
    {
        Width = width;
        Height = height;
        m_handle = GL.GenTexture();
        m_rawLightmapData = rawLightmapData;

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
            BuildTexturePixels(rawLightmapData));

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

    public bool ContainsFullDark() => m_rawLightmapData.Any(b => b == 0);

    private byte[] BuildTexturePixels(byte[] rawLightmapData)
    {
        var pixels = new byte[Width * Height * 3];
    
        for (var i = 0; i < rawLightmapData.Length; i++)
        {
            pixels[i * 3] = rawLightmapData[i];
            pixels[i * 3 + 1] = rawLightmapData[i];
            pixels[i * 3 + 2] = rawLightmapData[i];
        }
    
        return pixels;
    }
}