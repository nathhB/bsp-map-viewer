namespace nb3D.Map;

public partial class QuakeMap
{
    public class QuakeMapMesh(Mesh mesh, QuakeLightmap lightmap)
    {
        public Mesh Mesh { get; } = mesh;
        public QuakeLightmap Lightmap { get; } = lightmap;
    }
}