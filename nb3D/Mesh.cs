using OpenTK.Graphics.OpenGL4;

namespace nb3D;

public class Mesh : IDisposable
{
    private readonly int m_vao;
    private readonly int m_vbo;
    private readonly int m_ebo;
    private readonly IMeshTexture? m_texture;

    public int VertexCount { get; }

    public Mesh(float[] vertices, uint[] faceVertexIndices, IMeshTexture? texture = null)
    {
        VertexCount = faceVertexIndices.Length;
        m_texture = texture;

        // Vertex buffer
        m_vbo = GL.GenBuffer();
                    
        GL.BindBuffer(BufferTarget.ArrayBuffer, m_vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
                            
        // Vertex array
        m_vao = GL.GenVertexArray();
                    
        GL.BindVertexArray(m_vao);

        const int vertexDataLength = 3;
        var textureDataLength = m_texture?.DataLength ?? 0;

        // vertices data
        var stride = (vertexDataLength + textureDataLength) * sizeof(float);

        GL.VertexAttribPointer(0, vertexDataLength, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        
        m_texture?.SetVertexAttributes(vertexDataLength);

        // Element buffer object
        m_ebo = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, faceVertexIndices.Length * sizeof(uint), faceVertexIndices, BufferUsageHint.StaticDraw);
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