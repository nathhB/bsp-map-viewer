namespace nb3D.Map;

public class SkyboxMesh : Mesh
{
    public SkyboxMesh(SkyboxTexture texture) : base(new SkyboxMeshDataProvider(), texture)
    {
    }
}