using OpenTK.Mathematics;

namespace nb3D;

public static class MeshBuilder
{
    public static Mesh Build(MeshDef meshDef)
    {
        var hasTextures = meshDef.Materials.Count > 0;
        var stride = hasTextures ? 6 : 3;
        var vertices = new float[meshDef.Vertices.Count * stride];

        foreach (var vertexDef in meshDef.Vertices)
        {
            var bufferIndex = vertexDef.ElementIndex * stride;
            
            vertices[bufferIndex] = vertexDef.Position[0];
            vertices[bufferIndex + 1] = vertexDef.Position[1];
            vertices[bufferIndex + 2] = vertexDef.Position[2];

            if (hasTextures)
            {
                vertices[bufferIndex + 3] = vertexDef.TextureCoord[0];
                vertices[bufferIndex + 4] = vertexDef.TextureCoord[1];
                vertices[bufferIndex + 5] = vertexDef.TextureIndex;
            }
        }

        var faceVertexIndices = new uint[meshDef.Faces.Count * 3];
        var faces = meshDef.Faces.ToArray();

        for (var i = 0; i < faces.Length; i++)
        {
            var faceDef = faces[i];
            var bufferIndex = i * 3;

            faceVertexIndices[bufferIndex] = faceDef.Vertex1.ElementIndex;
            faceVertexIndices[bufferIndex + 1] = faceDef.Vertex2.ElementIndex;
            faceVertexIndices[bufferIndex + 2] = faceDef.Vertex3.ElementIndex;
        }

        var arrayTexture = meshDef.Materials.Count > 0 ? BuildArrayTexture(meshDef) : null;

        return new Mesh(vertices, faceVertexIndices, arrayTexture);
    }

    private static ArrayTexture? BuildArrayTexture(MeshDef meshDef)
    {
        List<string> texturesPath = new List<string>(meshDef.Materials.Count);

        foreach (var matDef in meshDef.Materials)
        {
            texturesPath.Add(matDef.TexturePath);
        }

        return ArrayTexture.CreateFromFiles(texturesPath.ToArray());
    }
}