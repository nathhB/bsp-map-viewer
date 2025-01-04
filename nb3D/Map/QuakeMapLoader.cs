namespace nb3D.Map;

public static class QuakeMapLoader
{
    public static QuakeMap Load(string mapPath, string palettePath, params string[] wadPaths)
    {
        var data = File.ReadAllBytes(mapPath);
        var palette = QuakePalette.Load(palettePath);
        var wads = wadPaths.Select(WAD3.Load).ToArray();
        var map = new QuakeMap(data, palette, wads);

        Console.WriteLine(map.HullCount);
        Console.WriteLine(map.PlaneCount);
        Console.WriteLine(map.SurfaceCount);
        Console.WriteLine(map.NodeCount);
        Console.WriteLine(map.LeafCount);
        Console.WriteLine(map.TextureInfoCount);

        return map;
    }
}