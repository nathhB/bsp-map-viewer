using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace nb3D;

public class Shader(string vertexData, string fragmentData)
{
    private int m_handle;
    private Dictionary<string, int> m_uniformLocations = new();

    public int Handle => m_handle;

    public static Shader CompileFromFiles(string vertexPath, string fragmentPath)
    {
        var vertexData = File.ReadAllText(vertexPath);
        var fragmentData = File.ReadAllText(fragmentPath);
        var shader = new Shader(vertexData, fragmentData);

        shader.Compile();

        return shader;
    }

    public void Use()
    {
        GL.UseProgram(m_handle);
    }

    public void SetUniform<T>(string name, T value)
    {
        if (!m_uniformLocations.TryGetValue(name, out var location))
        {
            location = GL.GetUniformLocation(m_handle, name);
            m_uniformLocations[name] = location;
        }

        switch (value)
        {
            case int i:
                SetUniform(location, i);
                break;
            case Vector4 vec4:
                SetUniform(location, vec4);
                break;
            case Color4 color4:
                SetUniform(location, color4);
                break;
            case Matrix3 mat3:
                SetUniform(location, mat3);
                break;
            case Matrix4 mat4:
                SetUniform(location, mat4);
                break;

            default:
                throw new InvalidOperationException($"Unsupported uniform type: {typeof(T)} (uniform: {name})");
        }
    }

    private void SetUniform(int location, int value)
    {
        GL.Uniform1(location, value);
    }

    private void SetUniform(int location, Color4 color)
    {
        GL.Uniform4(location, color);
    }

    private void SetUniform(int location, Vector4 vec4)
    {
        GL.Uniform4(location, vec4);
    }

    private void SetUniform(int location, Matrix3 mat3)
    {
        GL.UniformMatrix3(location, true, ref mat3);
    }

    private void SetUniform(int location, Matrix4 mat4)
    {
        GL.UniformMatrix4(location, true, ref mat4);
    }

    private void Compile()
    {
        var vertexShader = Compile(ShaderType.VertexShader, vertexData);
        var fragmentShader = Compile(ShaderType.FragmentShader, fragmentData);

        m_handle = GL.CreateProgram();
        
        GL.AttachShader(m_handle, vertexShader);
        GL.AttachShader(m_handle, fragmentShader);
        
        GL.LinkProgram(m_handle);

        GL.GetProgram(m_handle, GetProgramParameterName.LinkStatus, out var success);

        if (success == 0)
        {
            throw new Exception($"Failed to link program: {GL.GetProgramInfoLog(m_handle)}");
        }
        
        GL.DetachShader(m_handle, vertexShader);
        GL.DetachShader(m_handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    private int Compile(ShaderType shaderType, string data)
    {
        var shader = GL.CreateShader(shaderType);

        GL.ShaderSource(shader, data);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out var success);

        if (success == 0)
        {
            throw new Exception($"Failed to compile shader of type {shaderType}: {GL.GetShaderInfoLog(shader)}");
        }

        return shader;
    }
}