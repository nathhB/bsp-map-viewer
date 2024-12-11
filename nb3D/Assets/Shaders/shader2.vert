#version 330

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

out vec2 texCoord;

void main() {
    texCoord = aTexCoord;
    gl_Position = vec4(aPosition, 1.0) * modelMatrix * viewMatrix * projectionMatrix;
}
