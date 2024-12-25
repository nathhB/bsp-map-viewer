using OpenTK.Graphics.OpenGL4;

namespace nb3D;

public class Mesh : IDisposable
{
    private readonly int m_vao;
    private readonly int m_vbo;
    private readonly int m_ebo;
    private readonly IMeshTexture? m_texture;
    private readonly uint[] m_vertexIndices;

    public int VertexCount => m_vertexIndices.Length;

    public Mesh(IMeshDataProvider dataProvider, IMeshTexture? texture = null)
    {
        m_vertexIndices = dataProvider.VertexIndices;
        m_texture = texture;

        // Vertex buffer
        m_vbo = GL.GenBuffer();
                    
        GL.BindBuffer(BufferTarget.ArrayBuffer, m_vbo);
        dataProvider.BuildBufferData();
                            
        // Vertex array
        m_vao = GL.GenVertexArray();
                    
        GL.BindVertexArray(m_vao);
        dataProvider.BuildVertexData();

        // Element buffer object
        m_ebo = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, m_vertexIndices.Length * sizeof(uint), m_vertexIndices, BufferUsageHint.StaticDraw);
    }

    public void Bind()
    {
        GL.BindVertexArray(m_vao);
        m_texture?.Use(TextureUnit.Texture0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(m_vbo);
        GL.DeleteBuffer(m_ebo);
        GL.DeleteVertexArray(m_vao);
    }
}