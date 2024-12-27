namespace nb3D.Map;

public class QuakePalette
{
    public record struct QuakeColor(byte R, byte G, byte B);

    public static QuakePalette Load(string path)
    {
        var data = File.ReadAllBytes(path);

        return new QuakePalette(data);
    }

    private readonly QuakeColor[] m_colors = new QuakeColor[256];

    public QuakePalette(byte[] data)
    {
        for (var i = 0; i < 256; i++)
        {
            var r = data[i * 3 + 0];
            var g = data[i * 3 + 1];
            var b = data[i * 3 + 2];

            m_colors[i] = new QuakeColor(r, g, b);
        }
    }

    public QuakeColor GetColor(byte paletteIndex) => m_colors[paletteIndex];
}