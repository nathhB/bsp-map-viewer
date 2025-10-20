using System.Data;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;

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

    [StructLayout(LayoutKind.Explicit, Pack = 0)]
    private unsafe struct Surface
    {
        [FieldOffset(0)] public ushort PlaneId;      // The plane in which the surface lies
        [FieldOffset(2)] public ushort Side;	     // 0 if in front of the plane, 1 if behind the plane
        [FieldOffset(4)] public int EdgeListIndex;   // First edge in the list of edges
        [FieldOffset(8)] public ushort EdgeCount;    // Index of the first edge in the list of edges
        // This is NOT the ID of the edge
        [FieldOffset(10)] public ushort TexinfoId;    // Index to the texture info which contains an index to the mipmap texture

        // Type of light or 0xFF for no light map
        [FieldOffset(12)] public fixed byte Lightstyles[4];
        [FieldOffset(16)] public int LightmapOffset;  // Offset to the lightmap or -1
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

        public static explicit operator Vector3(Vertex v) => new Vector3(v.X, v.Y, v.Z);
    }

    [StructLayout(LayoutKind.Explicit, Pack = 0)]
    private unsafe struct MipTexture
    {
        [FieldOffset(0)] public fixed char Name[16];        // Name of the texture.
        [FieldOffset(16)] public uint Width;                // width of picture, must be a multiple of 8
        [FieldOffset(20)] public uint Height;               // height of picture, must be a multiple of 8
        // offset to u_char Pix[width   * height]
        // offset to u_char Pix[width/2 * height/2]
        // offset to u_char Pix[width/4 * height/4]
        // offset to u_char Pix[width/8 * height/8]
        [FieldOffset(24)] public fixed uint Offsets[4];
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

    private record struct SurfaceExtents(int MinU, int MinV, int Width, int Height);

    private const int BSP29 = 29;
    private const int BSP30 = 30;
    private static readonly int[] SupportedVersions = { BSP29, BSP30 };
    private readonly Header m_header;
    private readonly byte[] m_data;
    private readonly Dictionary<int, QuakeMapMesh[]> m_meshes = new();
    private readonly Dictionary<int, QuakeTexture> m_textures = new();
    private readonly QuakePalette m_palette;
    private readonly WAD3[] m_wads;
    private readonly ILogger m_logger;

    public unsafe int PlaneCount => m_header.Planes.Size / sizeof(Plane);
    public unsafe int NodeCount => m_header.Nodes.Size / sizeof(BspNode);
    public unsafe int LeafCount => m_header.Leaves.Size / sizeof(BspLeaf);
    public unsafe int SurfaceCount => m_header.Surfaces.Size / sizeof(Surface);
    public unsafe int HullCount => m_header.Hulls.Size / sizeof(Hull);
    public unsafe int TextureInfoCount => m_header.Texinfo.Size / sizeof(TextureInfo);
    public QuakeLightmap FullLitLightmap { get; }

    public QuakeMap(byte[] data, QuakePalette palette, WAD3[] wads, ILogger logger)
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
        m_logger = logger;
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

            for (var t = 0; t < textureCount; t++)
            {
                var texturePtr = mipHeaderPtr + offsets[t];
                var texture = *(MipTexture*)texturePtr;
                var textureName = Marshal.PtrToStringAnsi((IntPtr)texture.Name)!;
                var loadFromWAD = texture.Offsets[0] == 0;

                QuakeTexture? quakeTexture;

                if (loadFromWAD)
                {
                    if (!TryFindTextureInWADs(textureName, out quakeTexture))
                    {
                        m_logger.Warning($"Could not find texture {textureName}, missing a WAD?");
                    }
                }
                else
                {
                    if (m_header.Version == BSP29)
                    {
                        var textureDataPtr = texturePtr + texture.Offsets[0];
                        var rawTextureData = new byte[texture.Width * texture.Height];

                        Marshal.Copy((IntPtr)textureDataPtr, rawTextureData, 0, rawTextureData.Length);
                        quakeTexture = new QuakeTexture(
                            textureName, (int)texture.Width, (int)texture.Height, rawTextureData, m_palette, false);
                    }
                    else
                    {
                        quakeTexture = WAD3.LoadTexture(texturePtr);
                    }
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

    private unsafe bool TryCreateSurfaceMesh(int surfaceId, out QuakeMapMesh? mapMesh)
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

            vertices[e] = (Vector3)GetVertex(edgeId > 0 ? edge.Vertex0 : edge.Vertex1);
        }

        QuakeLightmap? lightmap;
        Vector2[] lightmapUvs;
        var hasLightmap = surface.Lightstyles[0] != 0xFF && surface.LightmapOffset != -1;

        if (hasLightmap)
        {
            var rgb = m_header.Version > BSP29;
            var extents = ComputeSurfaceExtents(surface);

            lightmap = BuildLightmap(vertices, textureInfo, extents, surface.LightmapOffset, rgb, out lightmapUvs);
        }
        else
        {
            /*lightmap = FullLitLightmap;
            lightmapUvs = new Vector2[vertices.Length];*/
            mapMesh = null;
            return false;
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
        out uint[]? vertexIndices)
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

    private QuakeLightmap BuildLightmap(Vector3[] vertices,
        TextureInfo textureInfo,
        SurfaceExtents surfaceExtents,
        int lightmapOffset,
        bool rgb,
        out Vector2[] uvs)
    {
        unsafe
        {
            var lightmapWidth = (surfaceExtents.Width / 16) + 1;
            var lightmapHeight = (surfaceExtents.Height / 16) + 1;
            var uRatio = (lightmapWidth - 1) / 32f;
            var vRatio = (lightmapHeight - 1) / 32f;

            uvs = new Vector2[vertices.Length];

            for (var i = 0; i < uvs.Length; i++)
            {
                var vertex = vertices[i];
                var u = Vector3.Dot(textureInfo.Snrm, vertex) + textureInfo.Soff;
                var v = Vector3.Dot(textureInfo.Tnrm, vertex) + textureInfo.Toff;

                u = ((u - surfaceExtents.MinU) / surfaceExtents.Width) * uRatio;
                v = ((v - surfaceExtents.MinV) / surfaceExtents.Height) * vRatio;

                u = Math.Clamp(u, 1 / 32f, uRatio);
                v = Math.Clamp(v, 1 / 32f, vRatio);

                uvs[i] = new Vector2(u, v);
            }

            var lightmapTextureData = new byte[32 * 32 * 3];

            fixed (byte* dataPtr = m_data)
            {
                var lightmapPtr = dataPtr + m_header.Lightmaps.Offset + lightmapOffset;

                if (rgb)
                {
                    for (var i = 0; i < lightmapWidth * lightmapHeight * 3; i++)
                    {
                        var x = i % (lightmapWidth * 3);
                        var y = i / (lightmapWidth * 3);
                        var p = y * (32 * 3) + x;

                        lightmapTextureData[p] = lightmapPtr[i];
                    }
                }
                else
                {
                    for (var i = 0; i < lightmapWidth * lightmapHeight; i++)
                    {
                        var x = i % lightmapWidth;
                        var y = i / lightmapWidth;
                        var p = y * (32 * 3) + (x * 3);

                        lightmapTextureData[p] = lightmapPtr[i];
                        lightmapTextureData[p + 1] = lightmapPtr[i];
                        lightmapTextureData[p + 2] = lightmapPtr[i];
                    }
                }
            }

            return new QuakeLightmap(32, 32, lightmapTextureData);
        }
    }

    private QuakeLightmap BuildFullLitLightmap()
    {
        var fullLitData = new byte[16 * 16 * 3];

        Array.Fill(fullLitData, (byte)0);
        return new QuakeLightmap(16, 16, fullLitData);
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

    private SurfaceExtents ComputeSurfaceExtents(Surface surface)
    {
        var minU = float.MaxValue;
        var minV = float.MaxValue;
        var maxU = -float.MaxValue;
        var maxV = -float.MaxValue;
        var texInfo = GetTextureInfo(surface.TexinfoId);

        for (var e = 0; e < surface.EdgeCount; e++)
        {
            var edgeListIndex = surface.EdgeListIndex + e;
            var edgeId = GetEdgeId(edgeListIndex);

            // edgeId can be negative
            // if edgeId < 0, vertices are in order vertex1 - vertex0
            // otherwise, they are in order vertex0 - vertex1
            var edge = GetEdge(edgeId > 0 ? edgeId : -edgeId);
            var vertex = GetVertex(edgeId > 0 ? edge.Vertex0 : edge.Vertex1);

            // use doubles to avoid floating point precision issues
            double vX = vertex.X;
            double vY = vertex.Y;
            double vZ = vertex.Z;
            var u = (vX * texInfo.Snrm.X + vY * texInfo.Snrm.Y + vZ * texInfo.Snrm.Z) + texInfo.Soff;
            var v = (vX * texInfo.Tnrm.X + vY * texInfo.Tnrm.Y + vZ * texInfo.Tnrm.Z) + texInfo.Toff;

            minU = (float)Math.Min(u, minU);
            minV = (float)Math.Min(v, minV);
            maxU = (float)Math.Max(u, maxU);
            maxV = (float)Math.Max(v, maxV);
        }

        minU = 16 * (int)Math.Floor(minU / 16);
        maxU = 16 * (int)Math.Ceiling(maxU / 16);
        minV = 16 * (int)Math.Floor(minV / 16);
        maxV = 16 * (int)Math.Ceiling(maxV / 16);

        return new SurfaceExtents((int)minU, (int)minV, (int)(maxU - minU), (int)(maxV - minV));
    }
}