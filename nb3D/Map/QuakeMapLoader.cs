namespace nb3D.Map;

public static class QuakeMapLoader
{
    public static QuakeMap Load(ILogger logger, string mapPath, string palettePath, params string[] wadPaths)
    {
        var data = File.ReadAllBytes(mapPath);
        var palette = QuakePalette.Load(palettePath);
        var wads = wadPaths.Select(WAD3.Load).ToArray();
        var map = new QuakeMap(data, palette, wads, logger);

        logger.Info($"Map loaded: {mapPath}\n" +
                    $"Hull count: {map.HullCount}\n" +
                    $"Plane count: {map.PlaneCount}\n" +
                    $"Surface count: {map.SurfaceCount}\n" +
                    $"Node count: {map.NodeCount}\n" +
                    $"Texture count: {map.TextureInfoCount}");

        return map;
    }
}