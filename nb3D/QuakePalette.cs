namespace nb3D;

public class QuakePalette
{
    public record struct QuakeColor(byte R, byte G, byte B);

    public static QuakePalette Load(string path)
    {
        using var stream = File.OpenRead(path);
        var buffer = new byte[stream.Length];
        var bytesToRead = stream.Length;
        var totalReadBytes = 0;
        
        while (bytesToRead > 0)
        {
            var readBytes = stream.Read(buffer, totalReadBytes, (int)bytesToRead);
        
            if (readBytes == 0)
            {
                break;
            }
                    
            bytesToRead -= readBytes;
            totalReadBytes += readBytes;
        }
        
        if (totalReadBytes != stream.Length)
        {
            throw new IOException($"Failed to read palette file: {path}");
        }

        return new QuakePalette(buffer);
    }

    private readonly QuakeColor[] m_colors = new QuakeColor[256];

    private QuakePalette(byte[] data)
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