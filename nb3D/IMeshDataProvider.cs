namespace nb3D;

public interface IMeshDataProvider
{
    public uint[] VertexIndices { get; }

    public void BuildBufferData();
    public void BuildVertexData();
}