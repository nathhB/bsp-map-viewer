namespace nb3D.Map;

public static class QuakeMapLoader
{
    public static QuakeMap Load(string mapPath, string palettePath)
    {
        using var stream = File.OpenRead(mapPath);
        var data = new byte[stream.Length];
        var bytesToRead = stream.Length;
        var totalReadBytes = 0;

        while (bytesToRead > 0)
        {
            var readBytes = stream.Read(data, totalReadBytes, (int)bytesToRead);

            if (readBytes == 0)
            {
                break;
            }
            
            bytesToRead -= readBytes;
            totalReadBytes += readBytes;
        }

        if (totalReadBytes != stream.Length)
        {
            throw new IOException($"Failed to read map file: {mapPath}");
        }

        var palette = QuakePalette.Load(palettePath);
        var map = new QuakeMap(data, palette);
        
        Console.WriteLine(map.HullCount);
        Console.WriteLine(map.PlaneCount);
        Console.WriteLine(map.SurfaceCount);
        Console.WriteLine(map.NodeCount);
        Console.WriteLine(map.LeafCount);
        Console.WriteLine(map.TextureInfoCount);
        Console.WriteLine(map.LightMapCount);

        return map;
    }
}