using System.Text.Json;
using System.Text.Json.Serialization;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace nb3D.Map;

public class SkyboxTexture : IMeshTexture
{
    private class SkyboxDefinition
    {
        [JsonPropertyName("width")] public int Width { get; set; }
        [JsonPropertyName("height")] public int Height { get; set; }
        [JsonPropertyName("right")] public required string RightPath { get; set; }
        [JsonPropertyName("left")] public required string LeftPath { get; set; }
        [JsonPropertyName("top")] public required string TopPath { get; set; }
        [JsonPropertyName("bottom")] public required string BottomPath { get; set; }
        [JsonPropertyName("back")] public required string BackPath { get; set; }
        [JsonPropertyName("front")] public required string FrontPath { get; set; }
    }

    private readonly int m_handle;

    public static SkyboxTexture Load(string path)
    {
        var skyDefStr = File.ReadAllText(path);
        var skyDef = JsonSerializer.Deserialize<SkyboxDefinition>(skyDefStr)!;
        var facePaths = new []
        {
            skyDef.RightPath,
            skyDef.LeftPath,
            skyDef.TopPath,
            skyDef.BottomPath,
            skyDef.FrontPath,
            skyDef.BackPath,
        };
        var data = new byte[6][];

        for (var i = 0; i < 6; i++)
        {
            var facePath = facePaths[i];
            var image = ImageResult.FromMemory(File.ReadAllBytes(facePath), ColorComponents.RedGreenBlue);

            if (image.Width != skyDef.Width || image.Height != skyDef.Height)
            {
                throw new InvalidDataException(
                    $"Skybox face {i}'s dimensions are not {skyDef.Width}x{skyDef.Height}");
            }

            data[i] = image.Data;
        }

        return new SkyboxTexture(skyDef.Width, skyDef.Height, data);
    }

    public SkyboxTexture(int width, int height, byte[][] data)
    {
        if (data.Length != 6)
        {
            throw new InvalidDataException("Skybox must be created from 6 textures");
        }

        m_handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureCubeMap, m_handle);

        for (var i = 0; i < 6; i++)
        {
            GL.TexImage2D(
                TextureTarget.TextureCubeMapPositiveX + i,
                0,
                PixelInternalFormat.Rgb,
                width,
                height,
                0,
                PixelFormat.Rgb,
                PixelType.UnsignedByte,
                data[i]);
        }
        
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

        GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);
    }

    public void Dispose()
    {
        GL.DeleteTexture(m_handle);
    }

    public void Use(TextureUnit targetUnit)
    {
        GL.ActiveTexture(targetUnit);
        GL.BindTexture(TextureTarget.TextureCubeMap, m_handle);
    }
}