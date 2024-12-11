using System.Text;
using OpenTK.Mathematics;

namespace nb3D;

public class MeshDef
{
    public readonly struct VertexDef(Vector3 position, Vector2 textureCoord, int textureIndex, uint elementIndex)
    {
        public Vector3 Position { get; } = position;
        public Vector2 TextureCoord { get; } = textureCoord;
        public int TextureIndex { get; } = textureIndex; // index in the array texture
        public uint ElementIndex { get; } = elementIndex;

        public static long BuildVertexDefId(Vector3 position, Vector2 textureCoord, int textureIndex)
        {
            return HashCode.Combine(position, textureCoord, textureIndex);
        }

        public override string ToString()
        {
            return $"[Coord: {Position}, UV: {TextureCoord}]";
        }
    }

    public readonly struct FaceDef(VertexDef vertex1, VertexDef vertex2, VertexDef vertex3)
    {
        public readonly VertexDef Vertex1 = vertex1;
        public readonly VertexDef Vertex2 = vertex2;
        public readonly VertexDef Vertex3 = vertex3;
        public readonly Vector3 Normal = Vector3.Zero;

        public string ToString(MeshDef meshDef)
        {
            return $"FaceDef ({Vertex1.ElementIndex} - {Vertex1}, {Vertex2.ElementIndex} - " +
                   $"{Vertex2}, {Vertex3.ElementIndex} - {Vertex3})";
        }
    }

    private readonly Dictionary<long, VertexDef> m_vertices = new();
    private readonly List<FaceDef> m_faces = new();
    private readonly Dictionary<string, MaterialDef> m_materials = new();

    public IReadOnlyCollection<VertexDef> Vertices => m_vertices.Values;
    public IReadOnlyCollection<FaceDef> Faces => m_faces;
    public IReadOnlyCollection<MaterialDef> Materials => m_materials.Values;

    public void AddVertex(long vertexDefId, VertexDef vertexDef) => m_vertices.Add(vertexDefId, vertexDef);

    public bool TryGetVertex(long vertexDefId, out VertexDef vertexDef) =>
        m_vertices.TryGetValue(vertexDefId, out vertexDef);

    public void AddFace(FaceDef faceDef) => m_faces.Add(faceDef);

    public void AddMaterial(string name, MaterialDef matDef) => m_materials.Add(name, matDef);

    public int GetMaterialIndex(string name) => m_materials[name].Index;

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Vertices:");

        foreach (var vertexDef in Vertices)
        {
            sb.AppendLine(vertexDef.ToString());
        }

        sb.AppendLine();
        sb.AppendLine("Faces:");
        
        foreach (var faceDef in Faces)
        {
            sb.AppendLine(faceDef.ToString(this));
        }

        return sb.ToString();
    }
}