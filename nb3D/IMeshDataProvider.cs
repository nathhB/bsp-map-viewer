namespace nb3D;

public interface IMeshDataProvider
{
    public uint[]? VertexIndices { get; }
    public float[] VertexData { get; }
    public bool UseElementBufferObject { get; }

    public void BuildBufferData();
    public void BuildVertexData();
}