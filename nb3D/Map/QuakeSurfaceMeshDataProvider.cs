using OpenTK.Graphics.OpenGL4;

namespace nb3D.Map;

public class QuakeSurfaceMeshDataProvider(float[] vertexData, uint[] vertexIndices) : IMeshDataProvider
{
    private float[] m_vertexData = vertexData;

    public uint[] VertexIndices { get; } = vertexIndices;

    public void BuildBufferData()
    {
        GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);
    }

    public void BuildVertexData()
    {
        const int vertexDataLength = 3;
        const int textureDataLength = 2;
        const int lightmapDataLength = 2;
        const int textureDataOffset = vertexDataLength * sizeof(float);
        const int lightmapDataOffset = (vertexDataLength + textureDataLength) * sizeof(float);
        
        // vertices data
        var stride = (vertexDataLength + textureDataLength + lightmapDataLength) * sizeof(float);
        
        GL.VertexAttribPointer(0, vertexDataLength, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, textureDataLength, VertexAttribPointerType.Float, false, stride, textureDataOffset);
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribPointer(2, lightmapDataLength, VertexAttribPointerType.Float, false, stride, lightmapDataOffset);
        GL.EnableVertexAttribArray(2);
    }
}