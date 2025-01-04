using System.Collections.ObjectModel;
using System.Data;
using System.Runtime.InteropServices;

namespace nb3D.Map;

public class WAD3
{
    [StructLayout(LayoutKind.Explicit, Pack = 0)]
    private unsafe struct Header
    {
        [FieldOffset(0)] public fixed char Magic[4]; // File format magic number
        [FieldOffset(4)] public int DirCount;        // Number of Directory entries
        [FieldOffset(8)] public int DirOffset;       // Offset from the WAD3 data's beginning for first Directory entry
    }

    [StructLayout(LayoutKind.Explicit, Pack = 0)]
    private unsafe struct Entry
    {
        [FieldOffset(0)] public int EntryOffset;                // Offset from the beginning of the WAD3 data
        [FieldOffset(4)] public int DiskSize;                   // The entry's size in the archive in bytes
        [FieldOffset(8)] public int EntrySize;                  // The entry's uncompressed size
        [FieldOffset(12)] public char FileType;                 // File type of the entry
        [FieldOffset(13)] public bool Compressed;               // Whether the file was compressed
        [FieldOffset(14)] public short Padding;                 // Not used
        [FieldOffset(16)] private fixed byte TextureName[16];   // Null-terminated texture name
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

    public static WAD3 Load(string path)
    {
        var data = File.ReadAllBytes(path);

        return new WAD3(data);
    }

    private const int MipTextureType = 0x43;

    private readonly byte[] m_data;
    private readonly Dictionary<string, QuakeTexture> m_textures = new();

    public ReadOnlyDictionary<string, QuakeTexture> Textures { get; }

    private WAD3(byte[] data)
    {
        m_data = data;
        Textures = m_textures.AsReadOnly();

        ReadEntries();
    }
    
    private unsafe void ReadEntries()
    {
        fixed (byte* dataPtr = m_data)
        {
            var magicStr = Marshal.PtrToStringAnsi((IntPtr)dataPtr, 4)!;

            if (magicStr != "WAD3")
            {
                throw new DataException($"Unsupported WAD format: {magicStr}");
            }

            var header = *(Header *)dataPtr;
            var entrySize = sizeof(Entry);

            for (var i = 0; i < header.DirCount; i++)
            {
                var entryOffset = entrySize * i;
                var entry = *(Entry*)(dataPtr + header.DirOffset + entryOffset);

                if (entry.Compressed)
                {
                    throw new DataException("Compressed texture entries are not supported");
                }
                
                if (entry.FileType == MipTextureType)
                {
                    LoadTexture(dataPtr + entry.EntryOffset);
                }
            }
        }
    }

    private unsafe void LoadTexture(byte *texturePtr)
    {
        var texture = *(MipTexture *)texturePtr;
        var textureName = Marshal.PtrToStringAnsi((IntPtr)texture.Name)!;

        Console.WriteLine($"WAD: {textureName} {texture.Width}x{texture.Height}");

        // the palette data is located after the fourth mipmap texture data
        // each mipmap texture size is the size of the previous one divided by 2
        // there are two dummy bytes between the last byte of the fourth mipmap and the palette data
        var fourthMipmapLength = (texture.Width / 8) * (texture.Height / 8);
        var paletteDataPtr = texturePtr + texture.Offsets[3] + fourthMipmapLength + 2;
        var paletteData = new byte[256 * 3];
        var rawTextureData = new byte[texture.Width * texture.Height];
        var textureDataPtr = texturePtr + texture.Offsets[0];

        Marshal.Copy((IntPtr)paletteDataPtr, paletteData, 0, paletteData.Length);
        Marshal.Copy((IntPtr)textureDataPtr, rawTextureData, 0, rawTextureData.Length);

        var palette = new QuakePalette(paletteData);
        m_textures[textureName] = new QuakeTexture(textureName, (int)texture.Width, (int)texture.Height, rawTextureData, palette);
    }
}