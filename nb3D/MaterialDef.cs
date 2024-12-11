namespace nb3D;

public class MaterialDef
{
    public string Name { get; }
    public string TexturePath { get; }
    public int Index { get; } // index in the material library

    public MaterialDef(string name, string texturePath, int index)
    {
        Name = name;
        TexturePath = texturePath;
        Index = index;
    }
}