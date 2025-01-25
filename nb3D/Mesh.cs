using OpenTK.Graphics.OpenGL4;

namespace nb3D;

public class Mesh : IDisposable
{
    private readonly int m_vao;
    private readonly int m_vbo;
    private readonly int m_ebo;
    private readonly IMeshTexture? m_texture;

    public int VertexCount { get; private set; }
    public int VertexIndexCount { get; private set; }
    public bool UseElementBufferObject { get; }

    public Mesh(IMeshDataProvider dataProvider, IMeshTexture? texture = null)
    {
        m_texture = texture;
        VertexCount = dataProvider.VertexData.Length / 3;
        UseElementBufferObject = dataProvider.UseElementBufferObject;

        // Vertex buffer
        m_vbo = GL.GenBuffer();
                    
        GL.BindBuffer(BufferTarget.ArrayBuffer, m_vbo);
        dataProvider.BuildBufferData();
                            
        // Vertex array
        m_vao = GL.GenVertexArray();
                    
        GL.BindVertexArray(m_vao);
        dataProvider.BuildVertexData();

        if (dataProvider.UseElementBufferObject)
        {
            if (dataProvider.VertexIndices == null)
            {
                throw new NullReferenceException($"{nameof(dataProvider.VertexIndices)} is null");
            }

            m_ebo = GL.GenBuffer();
            VertexIndexCount = dataProvider.VertexIndices.Length;

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_ebo);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                dataProvider.VertexIndices.Length * sizeof(uint),
                dataProvider.VertexIndices,
                BufferUsageHint.StaticDraw);
        }
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