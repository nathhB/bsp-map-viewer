using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using StbImageSharp;

namespace nb3D.Map;

/**
 * References:
 *      https://www.gamers.org/dEngine/quake/spec/quake-spec34/qkspec_4.htm
 *      https://github.com/demoth/jake2/blob/main/info/BSP.md
 *      https://developer.valvesoftware.com/wiki/BSP_(GoldSrc)
 *
 * Support BSP29 and BSP30 (GoldSrc)
 *
 * IMPORTANT: long are converted to integers !!
 */
public partial class QuakeMap
{
    public struct Vec3S
    {
        public short X;
        public short Y;
        public short Z;
    }
        
    public struct BoundingBox
    {
        public Vector3 Min;
        public Vector3 Max;
    }
            
    public struct BoundingBoxS
    {
        public Vec3S Min;
        public Vec3S Max;
    }
    
    public struct BspLeaf
    {
        public int Type;                // Special type of leaf
        public int VisList;             // Beginning of visibility lists; must be -1 or in [0, numvislist[
        public BoundingBoxS Box;        // Bounding box of the leaf
        public short SurfaceListIndex;  // Index of the first surface in the list of surface IDs, must be in [0, SurfaceCount[
        // This is NOT the ID of the surface
        public short SurfaceCount;      // Number of surfaces
            
        // Ambient sound volumes, ff = max
        public byte Sndwater;          
        public byte Sndsky;            
        public byte Sndslime;          
        public byte Sndlava;
    }
        
    public struct Hull
    {
        public BoundingBox BoundingBox; // The bounding box of the hull
        public Vector3 Origin;          // Always (0,0,0)
        public int NodeId0;             // Index of first BSP node
        public int NodeId1;             // Index of the first bound node
        public int NodeId2;             // Index of the second bound node 
        public int NodeId3;             // Usually zero
        public int VisLeafCount;        // Number of visible BSP leaves
        public int SurfaceListIndex;    // Index of the first surface in the list of surfaces
        public int SurfaceCount;        // Number of surfaces
    }
    
    private struct BspNode
    {
        public int PlaneId;         // Id of the plane that splits the node (intersecting plane), must be in [0,numplanes[ 
        public short Front;         // Index to (front >= 0) ? front child node : leaf[~front]
        public short Back;          // Index to (back >= 0) ? back child node : leaf[~back]
        public BoundingBoxS Box;    // Bounding box
        public ushort SurfaceId;    // Id of the first surface
        public ushort SurfaceCount; // Number of surfaces
    }
    
    private struct Plane
    {
        public Vector3 Normal;  // Vector orthogonal to plane (Nx,Ny,Nz) with Nx2+Ny2+Nz2 = 1
        public float Dist;		// Offset to plane, along the normal vector
        public int Type;		// Plane type
    }
        
    private struct Surface
    {
        public ushort PlaneId;      // The plane in which the surface lies
        public ushort Side;		    // 0 if in front of the plane, 1 if behind the plane
        public int EdgeListIndex;   // First edge in the list of edges
        public ushort EdgeCount;    // Index of the first edge in the list of edges
        // This is NOT the ID of the edge
        public ushort TexinfoId;    // Index to the texture info which contains an index to the mipmap texture
            
        // Type of light or 0xFF for no light map
        public byte Lightstyle0;
        public byte Lightstyle1;
        public byte Lightstyle2;
        public byte Lightstyle3;
        public int LightmapOffset;  // Offset to the lightmap or -1
    }
    
    private struct Edge
    {
        public ushort Vertex0;
        public ushort Vertex1;
    }
    
    private struct Vertex
    {
        public float X;
        public float Y;
        public float Z;
    
        public override string ToString() => $"({X}, {Y}, {Z})";
    }
    
    [StructLayout(LayoutKind.Explicit, Pack = 0)]
    private unsafe struct MipTexture
    {
        [FieldOffset(0)] public fixed char Name[16];        // Name of the texture.
        [FieldOffset(16)] public uint Width;                // width of picture, must be a multiple of 8
        [FieldOffset(20)] public uint Height;               // height of picture, must be a multiple of 8
        [FieldOffset(24)] public uint Offset1;              // offset to u_char Pix[width   * height]
        [FieldOffset(28)] public uint Offset2;              // offset to u_char Pix[width/2 * height/2]
        [FieldOffset(32)] public uint Offset4;              // offset to u_char Pix[width/4 * height/4]
        [FieldOffset(36)] public uint Offset8;              // offset to u_char Pix[width/8 * height/8]
    }
    
    private struct TextureInfo
    {
        public Vector3 Snrm;    // S projection
        public float Soff;      // S offset
        public Vector3 Tnrm;    // T projection
        public float Toff;		// T offset
        public int TexId;		// Id to the mipmap texture
        public int Flag;		// Flag for sky/water texture
    }
        
    private struct Entry
    {
        public int Offset;  // Offset to entry, in bytes, from start of file
        public int Size;    // Size of entry in file, in bytes
    }
        
    private struct Header
    {
        public int Version;
        public Entry Entities;
        public Entry Planes;
        public Entry Miptex;
        public Entry Vertices;
        public Entry Visilist;
        public Entry Nodes;
        public Entry Texinfo;
        public Entry Surfaces;
        public Entry Lightmaps;
        public Entry BoundNodes;
        public Entry Leaves;
        public Entry SurfaceList;
        public Entry Edges;
        public Entry EdgeList;
        public Entry Hulls;
    }

    private const int BSP29 = 29;
    private const int BSP30 = 30;
    private static readonly int[] SupportedVersions = { BSP29, BSP30 };
    private readonly Header m_header;
    private readonly byte[] m_data;
    private readonly Dictionary<int, QuakeMapMesh[]> m_meshes = new();
    private readonly Dictionary<int, QuakeTexture> m_textures = new();
    private readonly QuakePalette m_palette;
    private readonly WAD3[] m_wads;
    private int m_lightmapCount;

    public unsafe int PlaneCount => m_header.Planes.Size / sizeof(Plane);
    public unsafe int NodeCount => m_header.Nodes.Size / sizeof(BspNode);
    public unsafe int LeafCount => m_header.Leaves.Size / sizeof(BspLeaf);
    public unsafe int SurfaceCount => m_header.Surfaces.Size / sizeof(Surface);
    public unsafe int HullCount => m_header.Hulls.Size / sizeof(Hull);
    public unsafe int TextureInfoCount => m_header.Texinfo.Size / sizeof(TextureInfo);
    public unsafe int LightMapCount => m_header.Lightmaps.Size / sizeof(TextureInfo);
    public QuakeLightmap FullLitLightmap { get; }

    public QuakeMap(byte[] data, QuakePalette palette, WAD3[] wads)
    {
        var pData = GCHandle.Alloc(data, GCHandleType.Pinned);
        var header = (Header)Marshal.PtrToStructure(pData.AddrOfPinnedObject(), typeof(Header))!;
        pData.Free();
         
        if (!SupportedVersions.Contains(header.Version))
        {
            throw new DataException($"Unsupported version: {header.Version}");
        }

        m_header = header;
        m_data = data;
        m_palette = palette;
        m_wads = wads;
        FullLitLightmap = BuildFullLitLightmap();

        LoadTextures();
        LoadMeshes();
        CreateEntities();
    }

    public bool TryFindLeafAt(Vector3 position, int hullId, out int leafId)
    {
        var currentNode = GetNode(GetHull(hullId).NodeId0);
        var foundLeaf = false;

        leafId = -1;

        while (!foundLeaf)
        {
            var plane = GetPlane(currentNode.PlaneId);
            var normal = plane.Normal;
            // plane equation to determine if we are in front or behind the plane
            var planeEquality = (normal.X * position.X + normal.Y * position.Y + normal.Z * position.Z) - plane.Dist;
            var nextNodeId = planeEquality >= 0 ? currentNode.Front : currentNode.Back;

            if (nextNodeId >= 0)
            {
                currentNode = GetNode(nextNodeId);
            }
            else
            {
                leafId = -(nextNodeId + 1);
                foundLeaf = true;
            }
        }

        return foundLeaf;
    }

    public Span<byte> GetVisibilityList(int id) => m_data.AsSpan(m_header.Visilist.Offset + id);

    public QuakeMapMesh[] GetLeafMeshes(int leafId) => m_meshes[leafId];
        
    public int GetHullFirstLeafId(int hullId)
    {
        var leafId = 0;
        
        for (var h = 0; h < hullId; h++)
        {
            leafId += GetHull(h).VisLeafCount;
        }
        
        return leafId;
    }

    public Hull GetHull(int id) => GetEntry<Hull>(id, m_header.Hulls.Offset);

    public BspLeaf GetLeaf(int id) => GetEntry<BspLeaf>(id, m_header.Leaves.Offset);
        
    private BspNode GetNode(int id) => GetEntry<BspNode>(id, m_header.Nodes.Offset);

    private Plane GetPlane(int id) => GetEntry<Plane>(id, m_header.Planes.Offset);

    private Surface GetSurface(int id) => GetEntry<Surface>(id, m_header.Surfaces.Offset);

    private Edge GetEdge(int id) => GetEntry<Edge>(id, m_header.Edges.Offset);

    private Vertex GetVertex(int id) => GetEntry<Vertex>(id, m_header.Vertices.Offset);

    private TextureInfo GetTextureInfo(int id) => GetEntry<TextureInfo>(id, m_header.Texinfo.Offset);

    private unsafe int GetSurfaceId(int listIndex)
    {
        fixed (byte* dataPtr = m_data)
        {
            var listPtr = (ushort*)(dataPtr + m_header.SurfaceList.Offset);

            return *(listPtr + listIndex);
        }
    }

    private unsafe int GetEdgeId(int listIndex)
    {
        fixed (byte* dataPtr = m_data)
        {
            var listPtr = (int*)(dataPtr + m_header.EdgeList.Offset);
        
            return *(listPtr + listIndex);
        }
    }

    private unsafe void LoadTextures()
    {
        fixed (byte* dataPtr = m_data)
        {
            var mipHeaderPtr = dataPtr + m_header.Miptex.Offset;
            var textureCount = *(int*)mipHeaderPtr;
            var offsets = (int*)(mipHeaderPtr + 4);

            Console.WriteLine($"Texture count: {textureCount}");

            for (var t = 0; t < textureCount; t++)
            {
                var texturePtr = mipHeaderPtr + offsets[t];
                var texture = *(MipTexture*)texturePtr;
                var textureName = Marshal.PtrToStringAnsi((IntPtr)texture.Name)!;

                Console.WriteLine($"{textureName} - {texture.Width}x{texture.Height}");

                QuakeTexture? quakeTexture;

                if (texture.Offset1 == 0)
                {
                    if (!TryFindTextureInWADs(textureName, out quakeTexture))
                    {
                        Console.WriteLine($"Failed to find texture '{textureName}' in data, missing a WAD file?");
                    }
                }
                else
                {
                    var textureDataPtr = texturePtr + texture.Offset1;
                    var rawTextureData = new byte[texture.Width * texture.Height];

                    Marshal.Copy((IntPtr)textureDataPtr, rawTextureData, 0, rawTextureData.Length);    
                    quakeTexture = new QuakeTexture(textureName, (int)texture.Width, (int)texture.Height, rawTextureData, m_palette);
                }

                if (quakeTexture != null)
                {
                    m_textures[t] = quakeTexture;
                }
            }
        }
    }

    private void LoadMeshes()
    {
        for (var l = 0; l < LeafCount; l++)
        {
            var leaf = GetLeaf(l);
            var leafMeshes = new List<QuakeMapMesh>();
                
            for (
                var sIndex = leaf.SurfaceListIndex;
                sIndex < leaf.SurfaceListIndex + leaf.SurfaceCount;
                sIndex++)
            {
                if (TryCreateSurfaceMesh(GetSurfaceId(sIndex), out var mapMesh))
                {
                    leafMeshes.Add(mapMesh!);
                }

            }

            m_meshes[l] = leafMeshes.ToArray();
        }
    }
        
    private bool TryCreateSurfaceMesh(int surfaceId, out QuakeMapMesh? mapMesh)
    {
        var surface = GetSurface(surfaceId);
        var vertexCount = surface.EdgeCount;
        var vertices = new Vector3[vertexCount];
        var textureInfo = GetTextureInfo(surface.TexinfoId);

        if (!m_textures.TryGetValue(textureInfo.TexId, out var texture))
        {
            mapMesh = null;
            return false;
        }

        for (var e = 0; e < surface.EdgeCount; e++)
        {
            // var edgeListIndex = surface.EdgeListIndex + (surface.EdgeCount - 1 - e);
            var edgeListIndex = surface.EdgeListIndex + e;
            var edgeId = GetEdgeId(edgeListIndex);

            // edgeId can be negative
            // if edgeId < 0, vertices are in order vertex1 - vertex0
            // otherwise, they are in order vertex0 - vertex1
            var edge = GetEdge(edgeId > 0 ? edgeId : -edgeId);
            var vertex = GetVertex(edgeId > 0 ? edge.Vertex0 : edge.Vertex1);

            vertices[e] = new Vector3(vertex.X, vertex.Y, vertex.Z);
        }

        QuakeLightmap? lightmap;
        Vector2[] lightmapUvs;

        if (surface.LightmapOffset != -1 /*&& GetPlane(surface.PlaneId).Type == 1 && ++m_lightmapCount <= 7 && surfaceId == 42*/)
        {
            var rgb = m_header.Version > BSP29;

            lightmap = BuildLightmap(vertices, textureInfo, surface.LightmapOffset, rgb, out lightmapUvs);
        }
        else
        {
            lightmap = FullLitLightmap;
            lightmapUvs = new Vector2[vertices.Length];
        }

        BuildSurfaceVertexData(vertices, lightmapUvs, textureInfo, out var vertexData, out var vertexIndices);

        var mesh = new Mesh(new QuakeSurfaceMeshDataProvider(vertexData, vertexIndices), texture);
        mapMesh = new QuakeMapMesh(mesh, lightmap);

        return true;
    }
        
    // fan triangulation from surface convex polygon
    private void BuildSurfaceVertexData(
        Vector3[] vertices,
        Vector2[] lightmapUvs,
        TextureInfo textureInfo,
        out float[] vertexData,
        out uint[] vertexIndices)
    {
        // 3 bytes for vertices, 2 bytes of texture coordinates, 2 bytes for lightmap coodinates
        const int vertexDataLength = 7;
            
        var texture = m_textures[textureInfo.TexId];
        var triangleCount = vertices.Length - 2;
        var triangleIndex = 0;
        vertexData = new float[vertices.Length * vertexDataLength];
        vertexIndices = new uint[triangleCount * 3];

        for (var v = 0; v < vertices.Length; v++)
        {
            var vertex = vertices[v];
            var lightmapUv = lightmapUvs[v];
            var dataIndex = v * vertexDataLength;
        
            // vertex
            vertexData[dataIndex] = vertex.X;
            vertexData[dataIndex + 1] = vertex.Y;
            vertexData[dataIndex + 2] = vertex.Z;

            // texture coordinate
            var tU = (Vector3.Dot(textureInfo.Snrm, vertex) + textureInfo.Soff) / texture.Width;
            var tV = (Vector3.Dot(textureInfo.Tnrm, vertex) + textureInfo.Toff) / texture.Height;

            vertexData[dataIndex + 3] = tU;
            vertexData[dataIndex + 4] = tV;

            // lightmap coordinate
            vertexData[dataIndex + 5] = lightmapUv.X;
            vertexData[dataIndex + 6] = lightmapUv.Y;
        }

        for (uint v = 1; v < vertices.Length - 1; v++, triangleIndex += 3)
        {
            vertexIndices[triangleIndex] = 0;
            vertexIndices[triangleIndex + 1] = v;
            vertexIndices[triangleIndex + 2] = v + 1;
        }
    }

    private QuakeLightmap BuildLightmap(
        Vector3[] vertices,
        TextureInfo textureInfo,
        int lightmapOffset,
        bool rgb,
        out Vector2[] uvs)
    {
        unsafe
        {
            uvs = vertices
                .Select(v => new Vector2(
                    Vector3.Dot(textureInfo.Snrm, v) + textureInfo.Soff,
                    Vector3.Dot(textureInfo.Tnrm, v) + textureInfo.Toff)
                ).ToArray();
            var minU = uvs.Select(v => v.X).Min();
            var maxU = uvs.Select(v => v.X).Max();
            var minV = uvs.Select(v => v.Y).Min();
            var maxV = uvs.Select(v => v.Y).Max();
            var lightmapWidth = (int)Math.Ceiling((maxU - minU) / QuakeLightmap.Size);
            var lightmapHeight = (int)Math.Ceiling((maxV - minV) / QuakeLightmap.Size);

            if (lightmapWidth > QuakeLightmap.Size || lightmapHeight > QuakeLightmap.Size)
            {
                throw new DataException($"Invalid lightmap size: {lightmapWidth}x{lightmapHeight}");
            }

            var uRatio = (float)lightmapWidth / QuakeLightmap.Size;
            var vRatio = (float)lightmapHeight / QuakeLightmap.Size;

            uvs = uvs
                .Select(uv => new Vector2(
                    ((uv.X - minU) / (maxU - minU)) * uRatio,
                    ((uv.Y - minV) / (maxV - minV)) * vRatio))
                .ToArray();

            var data = new byte[QuakeLightmap.Size * QuakeLightmap.Size * 3];
            var channelCount = rgb ? 3 : 1;

            Array.Fill(data, (byte)255);

            fixed (byte* dataPtr = m_data)
            {
                var lightmapPtr = dataPtr + m_header.Lightmaps.Offset + lightmapOffset;

                for (var i = 0; i < lightmapWidth * lightmapHeight * channelCount; i++)
                {
                    var x = i % (lightmapWidth * channelCount);
                    var y = i / (lightmapWidth * channelCount);

                    if (rgb)
                    {
                        var p = (y * (QuakeLightmap.Size * 3)) + x;

                        data[p] = lightmapPtr[i];
                    }
                    else
                    {
                        var p = (y * (QuakeLightmap.Size * 3)) + (x * 3);

                        data[p] = lightmapPtr[i];
                        data[p + 1] = lightmapPtr[i];
                        data[p + 2] = lightmapPtr[i];
                    }
                }
            }

            return new QuakeLightmap(data);
        }
    }

    private QuakeLightmap BuildFullLitLightmap()
    {
        var fullLitData = new byte[16 * 16 * 3];
                        
        Array.Fill(fullLitData, (byte)255);
        return new QuakeLightmap(fullLitData);
    }
    
    private QuakeLightmap BuildGrayLightmap()
    {
        var data = new byte[16 * 16 * 3];

        for (int i = 0; i < 16 * 16; i++)
        {
            var val = (byte)(200 * ((float)i / (16 * 16)));

            data[i * 3] = val;
            data[i * 3 + 1] = val;
            data[i * 3 + 2] = val;
        }

        return new QuakeLightmap(data);
    }

    private unsafe void CreateEntities()
    {
        fixed (byte *dataPtr = m_data)
        {
            var entitiesPtr = (char *)(dataPtr + m_header.Entities.Offset);
            var entitiesStr = Marshal.PtrToStringAnsi((IntPtr)entitiesPtr)!;
                
            // TODO
        }
    }

    private unsafe T GetEntry<T>(int id, int entryOffset)
    {
        fixed (byte* dataPtr = m_data)
        {
            var offset = entryOffset + (id * sizeof(T));

            return *(T *)(dataPtr + offset);
        }
    }

    private bool TryFindTextureInWADs(string name, out QuakeTexture? texture)
    {
        foreach (var wad in m_wads)
        {
            if (wad.Textures.TryGetValue(name, out texture))
            {
                return true;
            }
        }

        texture = null;
        return false;
    }
}