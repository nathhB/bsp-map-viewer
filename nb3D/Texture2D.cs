using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace nb3D;

public class Texture2D : IDisposable
{
    private readonly int m_handle;

    public static Texture2D LoadFromFile(string path)
    {
        using Stream stream = File.OpenRead(path);
        return new Texture2D(ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha));
    }

    private Texture2D(ImageResult image)
    {
        m_handle = GL.GenTexture();

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, m_handle);
        GL.TexImage2D(TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba,
            image.Width,
            image.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            image.Data);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public void Use(TextureUnit textureUnit)
    {
        GL.ActiveTexture(textureUnit);
        GL.BindTexture(TextureTarget.Texture2D, m_handle);
    }

    public void Dispose()
    {
        GL.DeleteTexture(m_handle);
    }
}