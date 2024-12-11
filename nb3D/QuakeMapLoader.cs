using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Mathematics;

namespace nb3D;

/**
 * References:
 *      https://www.gamers.org/dEngine/quake/spec/quake-spec34/qkspec_4.htm
 *      https://github.com/demoth/jake2/blob/main/info/BSP.md
 * 
 * IMPORTANT: long are converted to integers !!
 */
public class QuakeMapLoader
{
    public struct Vec3F
    {
        public float X;
        public float Y;
        public float Z;

        public static explicit operator Vector3(Vec3F v) => new Vector3(v.X, v.Y, v.Z);
    }
        
    public struct Vec3S
    {
        public short X;
        public short Y;
        public short Z;
    }
    
    public struct BoundingBox
    {
        public Vec3F Min;
        public Vec3F Max;
    }
        
    public struct BoundingBoxS
    {
        public Vec3S Min;
        public Vec3S Max;
    }

    public struct BspNode
    {
        public int PlaneId;         // Id of the plane that splits the node (intersecting plane), must be in [0,numplanes[ 
        public short Front;         // Index to (front >= 0) ? front child node : leaf[~front]
        public short Back;          // Index to (back >= 0) ? back child node : leaf[~back]
        public BoundingBoxS Box;    // Bounding box
        public ushort SurfaceId;    // Id of the first surface
        public ushort SurfaceCount; // Number of surfaces
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
        public Vec3F Origin;            // Always (0,0,0)
        public int NodeId0;             // Index of first BSP node
        public int NodeId1;             // Index of the first bound node
        public int NodeId2;             // Index of the second bound node 
        public int NodeId3;             // Usually zero
        public int VisLeafCount;        // Number of visible BSP leaves
        public int SurfaceListIndex;    // Index of the first surface in the list of surfaces
        public int SurfaceCount;        // Number of surfaces
    }
    
    public struct Plane
    {
        public Vec3F Normal;	// Vector orthogonal to plane (Nx,Ny,Nz) with Nx2+Ny2+Nz2 = 1
        public float Dist;		// Offset to plane, along the normal vector
        public int Type;		// Plane type
    }
    
    public struct Surface
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
        public int Lightmap;         // Offset to the lightmap or -1
    }

    public struct Edge
    {
        public ushort Vertex0;
        public ushort Vertex1;
    }

    public struct Vertex
    {
        public float X;
        public float Y;
        public float Z;

        public override string ToString() => $"({X}, {Y}, {Z})";
    }

    [StructLayout(LayoutKind.Explicit, Pack = 0)]
    public unsafe struct MipTexture
    {
        [FieldOffset(0)] public fixed char Name[16];        // Name of the texture.
        [FieldOffset(16)] public uint Width;                // width of picture, must be a multiple of 8
        [FieldOffset(20)] public uint Height;               // height of picture, must be a multiple of 8
        [FieldOffset(24)] public uint Offset1;              // offset to u_char Pix[width   * height]
        [FieldOffset(28)] public uint Offset2;              // offset to u_char Pix[width/2 * height/2]
        [FieldOffset(32)] public uint Offset4;              // offset to u_char Pix[width/4 * height/4]
        [FieldOffset(36)] public uint Offset8;              // offset to u_char Pix[width/8 * height/8]
    }

    public struct TextureInfo
    {
        public Vec3F Snrm;      // S projection
        public float Soff;		// S offset
        public Vec3F Tnrm;      // T projection
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

    private const int Version = 29;

    public class Map
    {
        private readonly Header m_header;
        private readonly byte[] m_data;
        private readonly Dictionary<int, Mesh[]> m_meshes;
        private readonly QuakeTexture[] m_textures;

        public unsafe int PlaneCount => m_header.Planes.Size / sizeof(Plane);
        public unsafe int NodeCount => m_header.Nodes.Size / sizeof(BspNode);
        public unsafe int LeafCount => m_header.Leaves.Size / sizeof(BspLeaf);
        public unsafe int SurfaceCount => m_header.Surfaces.Size / sizeof(Surface);
        public unsafe int HullCount => m_header.Hulls.Size / sizeof(Hull);
        public unsafe int TextureInfoCount => m_header.Texinfo.Size / sizeof(TextureInfo);
        public unsafe int LightMapCount => m_header.Lightmaps.Size / sizeof(TextureInfo);

        public Map(byte[] data, QuakePalette palette)
        {
            var pData = GCHandle.Alloc(data, GCHandleType.Pinned);
            var header = (Header)Marshal.PtrToStructure(pData.AddrOfPinnedObject(), typeof(Header))!;
            pData.Free();
         
            if (header.Version != Version)
            {
                throw new DataException($"Invalid version: {header.Version}");
            }

            m_header = header;
            m_data = data;
            m_meshes = new Dictionary<int, Mesh[]>();

            m_textures = LoadTextures(palette);
            CreateMeshes();
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
                var normal = (Vector3)plane.Normal;
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

        public Hull GetHull(int id) => GetEntry<Hull>(id, m_header.Hulls.Offset);

        public BspNode GetNode(int id) => GetEntry<BspNode>(id, m_header.Nodes.Offset);

        public BspLeaf GetLeaf(int id) => GetEntry<BspLeaf>(id, m_header.Leaves.Offset);

        public Plane GetPlane(int id) => GetEntry<Plane>(id, m_header.Planes.Offset);

        public Surface GetSurface(int id) => GetEntry<Surface>(id, m_header.Surfaces.Offset);

        public Edge GetEdge(int id) => GetEntry<Edge>(id, m_header.Edges.Offset);

        public Vertex GetVertex(int id) => GetEntry<Vertex>(id, m_header.Vertices.Offset);
        
        public Span<byte> GetVisibilityList(int id) => m_data.AsSpan(m_header.Visilist.Offset + id);

        public TextureInfo GetTextureInfo(int id) => GetEntry<TextureInfo>(id, m_header.Texinfo.Offset);

        public unsafe int GetSurfaceId(int listIndex)
        {
            fixed (byte* dataPtr = m_data)
            {
                var listPtr = (ushort*)(dataPtr + m_header.SurfaceList.Offset);

                return *(listPtr + listIndex);
            }
        }

        public unsafe int GetEdgeId(int listIndex)
        {
            fixed (byte* dataPtr = m_data)
            {
                var listPtr = (int*)(dataPtr + m_header.EdgeList.Offset);
        
                return *(listPtr + listIndex);
            }
        }

        public Mesh[] GetLeafMeshes(int leafId) => m_meshes[leafId];

        public int GetHullFirstLeafId(int hullId)
        {
            var leafId = 0;

            for (var h = 0; h < hullId; h++)
            {
                leafId += GetHull(h).VisLeafCount;
            }

            return leafId;
        }

        public string GetSurfaceDebugStr(Surface surface)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Surface (edge count: {surface.EdgeCount})");

            for (var e = 0; e < surface.EdgeCount; e++)
            {
                var edgeListIndex = surface.EdgeListIndex + e;
                var edgeId = GetEdgeId(edgeListIndex);
                        
                // edgeId can be negative
                // if edgeId < 0, vertices are in order vertex1 - vertex0
                // otherwise, they are in order vertex0 - vertex1
                var edge = GetEdge(edgeId > 0 ? edgeId : -edgeId);
                var startVertex = GetVertex(edgeId > 0 ? edge.Vertex0 : edge.Vertex1);
                var endVertex = GetVertex(edgeId > 0 ? edge.Vertex1 : edge.Vertex0);

                sb.AppendLine($"Edge {e} | {startVertex} -> {endVertex}");
            }

            return sb.ToString();
        }

        private unsafe QuakeTexture[] LoadTextures(QuakePalette palette)
        {
            fixed (byte* dataPtr = m_data)
            {
                var mipHeaderPtr = dataPtr + m_header.Miptex.Offset;
                var textureCount = *(int*)mipHeaderPtr;
                var offsets = (int*)(mipHeaderPtr + 4);
                var textures = new QuakeTexture[textureCount];

                Console.WriteLine($"Texture count: {textureCount}");

                for (var t = 0; t < textureCount; t++)
                {
                    var texturePtr = mipHeaderPtr + offsets[t];
                    var texture = *(MipTexture*)texturePtr;
                    var textureDataPtr = texturePtr + texture.Offset1;
                    var textureName = Marshal.PtrToStringAnsi((IntPtr)texture.Name)!;
                    var rawTextureData = new byte[texture.Width * texture.Height];

                    Marshal.Copy((IntPtr)textureDataPtr, rawTextureData, 0, rawTextureData.Length);
                    Console.WriteLine($"{textureName} - {texture.Width}x{texture.Height}");
                    textures[t] = new QuakeTexture(textureName, (int)texture.Width, (int)texture.Height, rawTextureData, palette);
                }

                return textures;
            }
        }

        private void CreateMeshes()
        {
            for (var l = 0; l < LeafCount; l++)
            {
                var leaf = GetLeaf(l);
                var leafMeshes = new List<Mesh>();
                
                for (
                    var sIndex = leaf.SurfaceListIndex;
                    sIndex < leaf.SurfaceListIndex + leaf.SurfaceCount;
                    sIndex++)
                {
                    var mesh = CreateSurfaceMesh(GetSurfaceId(sIndex));

                    leafMeshes.Add(mesh);
                }

                m_meshes[l] = leafMeshes.ToArray();
            }
        }
        
        private Mesh CreateSurfaceMesh(int surfaceId)
        {
            var surface = GetSurface(surfaceId);
            var vertexCount = surface.EdgeCount;
            var vertices = new Vector3[vertexCount];
            var textureInfo = GetTextureInfo(surface.TexinfoId);
            var texture = m_textures[textureInfo.TexId];
            var planeType = GetPlane(surface.PlaneId).Type;
            
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

            BuildSurfaceVertexData(vertices, textureInfo, surface.Lightmap, planeType, out var vertexData, out var vertexIndices);

            return new Mesh(vertexData, vertexIndices, texture);
        }
        
        // fan triangulation from surface convex polygon
        private void BuildSurfaceVertexData(
            Vector3[] vertices,
            TextureInfo textureInfo,
            int lightmapId,
            int planeType,
            out float[] vertexData,
            out uint[] vertexIndices)
        {
            const int vertexDataLength = 5; // 3 bytes for vertices, 2 bytes of texture coordinates

            if (lightmapId != -1)
            {
                unsafe
                {
                    GetLightMap(lightmapId, planeType);
                }
            }
            
            var texture = m_textures[textureInfo.TexId];
            var triangleCount = vertices.Length - 2;
            var triangleIndex = 0;
            vertexData = new float[vertices.Length * vertexDataLength];
            vertexIndices = new uint[triangleCount * 3];

            for (var v = 0; v < vertices.Length; v++)
            {
                var vertex = vertices[v];
                var dataIndex = v * vertexDataLength;
        
                // vertex
                vertexData[dataIndex] = vertex.X;
                vertexData[dataIndex + 1] = vertex.Y;
                vertexData[dataIndex + 2] = vertex.Z;
                
                // texture coordinate
                var tU = (Vector3.Dot((Vector3)textureInfo.Snrm, vertex) + textureInfo.Soff) / texture.Width;
                var tV = (Vector3.Dot((Vector3)textureInfo.Tnrm, vertex) + textureInfo.Toff) / texture.Height;

                vertexData[dataIndex + 3] = tU;
                vertexData[dataIndex + 4] = tV;
            }
                        
            for (uint v = 1; v < vertices.Length - 1; v++, triangleIndex += 3)
            {
                vertexIndices[triangleIndex] = 0;
                vertexIndices[triangleIndex + 1] = v;
                vertexIndices[triangleIndex + 2] = v + 1;
            }
        }

        private unsafe byte* GetLightMap(int lightmapId, int planeType)
        {
            return null;
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
    }

    public static Map Load(string mapPath, string palettePath)
    {
        using var stream = File.OpenRead(mapPath);
        var buffer = new byte[stream.Length];
        var bytesToRead = stream.Length;
        var totalReadBytes = 0;

        while (bytesToRead > 0)
        {
            var readBytes = stream.Read(buffer, totalReadBytes, (int)bytesToRead);

            if (readBytes == 0)
            {
                break;
            }
            
            bytesToRead -= readBytes;
            totalReadBytes += readBytes;
        }

        if (totalReadBytes != stream.Length)
        {
            throw new IOException($"Failed to read map file: {mapPath}");
        }

        var palette = QuakePalette.Load(palettePath);
        var map = new Map(buffer, palette);
        
        Console.WriteLine(map.HullCount);
        Console.WriteLine(map.PlaneCount);
        Console.WriteLine(map.SurfaceCount);
        Console.WriteLine(map.NodeCount);
        Console.WriteLine(map.LeafCount);
        Console.WriteLine(map.TextureInfoCount);

        return map;
    }
}