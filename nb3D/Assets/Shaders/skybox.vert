#version 330

layout(location = 0) in vec3 aPosition;

uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

out vec3 texCoord;

void main() {
    texCoord = aPosition;
    gl_Position = vec4(aPosition, 1.0) * viewMatrix * projectionMatrix;
}
