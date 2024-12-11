using System.Globalization;
using OpenTK.Mathematics;

namespace nb3D;

public static class WavefrontImporter
{
    public static MeshDef Import(string objPath, string mtlPath)
    {
        var meshDef = new MeshDef();
        using var mtlStream = File.OpenRead(mtlPath);
        using var objStream = File.OpenRead(objPath);

        ParseMtl(mtlStream, meshDef);
        ParseObj(objStream, meshDef);

        return meshDef;
    }

    private static void ParseMtl(Stream stream, MeshDef meshDef)
    {
        using var reader = new StreamReader(stream);
        string? line;
        var matDefIndex = 0;

        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("newmtl"))
            {
                var matName = line.Split(' ')[1];
                var matDef = ParseMaterial(reader, matName, matDefIndex);

                meshDef.AddMaterial(matName, matDef);
                matDefIndex++;
            }
        }
    }

    private static MaterialDef ParseMaterial(StreamReader reader, string matName, int matDefIndex)
    {
        string? line;
        string? texturePath = null;

        while ((line = reader.ReadLine()) != string.Empty)
        {
            if (line == null)
            {
                break;
            }

            if (line.StartsWith("map_Kd"))
            {
                texturePath = line.Split(' ')[1];
            }
        }

        if (texturePath == null)
        {
            throw new InvalidDataException("Failed to parse MTL: could not find texture (missing map_Kd)");
        }

        return new MaterialDef(matName, texturePath, matDefIndex);
    }

    private static void ParseObj(Stream stream, MeshDef meshDef)
    {
        using var reader = new StreamReader(stream);
        var vertices = new List<Vector3>();
        var textureCoordinates = new List<Vector2>();
        var activeMaterialIndex = -1;
        uint elementArrayIndex = 0;
        string? line;
        
        while ((line = reader.ReadLine()) != null)
        {
            var parts = line.Split(' ', 2);
            var type = parts[0];
            var data = parts[1];
        
            switch (type)
            {
                case "v":
                    vertices.Add(ParseVertex(meshDef, data));
                    break;
                        
                case "vt":
                    textureCoordinates.Add(ParseTextureCoordinate(data));
                    break;
                        
                case "vn":
                    // ignore normals for now
                    break;
                        
                case "f":
                    ParseFace(meshDef, vertices, textureCoordinates, activeMaterialIndex, data, ref elementArrayIndex);
                    break;
                
                case "usemtl":
                    SelectMaterial(meshDef, data, out activeMaterialIndex);
                    break;
            }
        }
    }

    private static Vector3 ParseVertex(MeshDef meshDef, string data)
    {
        var parts = data.Split(' ');

        return new Vector3(
            ParseFloat(parts[0]),
            ParseFloat(parts[1]),
            ParseFloat(parts[2]));
    }

    private static Vector2 ParseTextureCoordinate(string data)
    {
        var parts = data.Split(' ');

        return new Vector2(ParseFloat(parts[0]), ParseFloat(parts[1]));
    }

    private static void ParseFace(MeshDef meshDef,
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<Vector2> textureCoordinates,
        int activeMaterialIndex,
        string data,
        ref uint elementArrayIndex)
    {
        if (activeMaterialIndex < 0 || activeMaterialIndex >= meshDef.Materials.Count)
        {
            throw new ArgumentException($"{nameof(activeMaterialIndex)} is out of range");
        }

        var parts = data.Split(' ');

        if (parts.Length != 3)
        {
            throw new InvalidOperationException("Faces must be triangles");
        }

        var faceVertices = new MeshDef.VertexDef[3];

        for (var i = 0; i < 3; i++)
        {
            var subParts = parts[i].Split('/');

            if (subParts.Length != 3)
            {
                throw new InvalidOperationException("Faces components must contain vertex, uv and normal");
            }
            
            var vertexIndex = uint.Parse(subParts[0]) - 1;
            var textureCoordIndex = int.Parse(subParts[1]) - 1;
            var vertex = vertices[(int)vertexIndex];
            var textureCoord = textureCoordinates[textureCoordIndex];
            var vertexDefId = MeshDef.VertexDef.BuildVertexDefId(vertex, textureCoord, activeMaterialIndex);

            if (!meshDef.TryGetVertex(vertexDefId, out var vertexDef))
            {
                vertexDef = new MeshDef.VertexDef(vertex, textureCoord, activeMaterialIndex, elementArrayIndex);
                meshDef.AddVertex(vertexDefId, vertexDef);
                elementArrayIndex++;
            }

            faceVertices[i] = vertexDef;
        }

        meshDef.AddFace(new MeshDef.FaceDef(faceVertices[0], faceVertices[1], faceVertices[2]));
    }

    private static void SelectMaterial(MeshDef meshDef, string materialName, out int textureIndex)
    {
        textureIndex = meshDef.GetMaterialIndex(materialName);
    }

    private static float ParseFloat(string str) => float.Parse(str, CultureInfo.InvariantCulture.NumberFormat);
}