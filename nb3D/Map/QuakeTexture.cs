using OpenTK.Graphics.OpenGL4;
using GL = OpenTK.Graphics.OpenGL4.GL;

namespace nb3D.Map;

public class QuakeTexture : IMeshTexture
{
    private readonly int m_handle;
    
    public string Name { get; }
    public int Width { get; }
    public int Height { get; }

    public QuakeTexture(string name, int width, int height, byte[] rawTextureData, QuakePalette palette)
    {
        Name = name;
        Width = width;
        Height = height;
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
            BuildTexturePixels(rawTextureData, palette));

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

    private byte[] BuildTexturePixels(byte[] rawTextureData, QuakePalette palette)
    {
        var pixels = new byte[Width * Height * 3];

        for (var i = 0; i < rawTextureData.Length; i++)
        {
            var color = palette.GetColor(rawTextureData[i]);

            pixels[i * 3] = color.R;
            pixels[i * 3 + 1] = color.G;
            pixels[i * 3 + 2] = color.B;
        }

        return pixels;
    }
}