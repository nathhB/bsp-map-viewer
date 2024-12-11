using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace nb3D;

public class ArrayTexture : IMeshTexture
{
    private readonly int m_handle;

    public int DataLength => 3;

    public static ArrayTexture CreateFromFiles(params string[] paths)
    {
        var images = new List<ImageResult>(paths.Length);
        var firstImage = true;
        var width = 0;
        var height = 0;

        foreach (var path in paths)
        {
            using Stream stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            if (firstImage)
            {
                width = image.Width;
                height = image.Height;
                firstImage = false;
            }

            images.Add(image);
        }

        return new ArrayTexture(images.ToArray(), width, height);
    }

    private ArrayTexture(ImageResult[] images, int width, int height)
    {
        m_handle = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2DArray, m_handle);
        GL.TexStorage3D(
            TextureTarget3d.Texture2DArray,
            1,
            SizedInternalFormat.Rgba8,
            width,
            height,
            images.Length);

        for (var i = 0; i < images.Length; i++)
        {
            var image = images[i];

            if (image.Width != width || image.Height != height)
            {
                throw new InvalidOperationException($"All textures must be of size {width}x{height}");
            }

            GL.TexSubImage3D(
                TextureTarget.Texture2DArray,
                0, 0, 0, i, width, height, 1,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                image.Data);
        }

        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (float)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToEdge);
    }
    
    public void SetVertexAttributes(int vertexDataLength)
    {
        var stride = (vertexDataLength + DataLength) * sizeof(float);
        var offset = vertexDataLength * sizeof(float);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, offset);
        GL.EnableVertexAttribArray(1);

        offset += 2 * sizeof(float);
        
        GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, offset);
        GL.EnableVertexAttribArray(2);
    }

    public void Use(TextureUnit targetUnit)
    {
        GL.ActiveTexture(targetUnit);
        GL.BindTexture(TextureTarget.Texture2DArray, m_handle);       
    }

    public void Dispose()
    {
        GL.DeleteTexture(m_handle);
    }
}