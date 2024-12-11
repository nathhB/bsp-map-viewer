#version 330

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in float aTexIndex;

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

out vec2 texCoord;
out float texIndex;

void main() {
    texCoord = aTexCoord;
    texIndex = aTexIndex;
    gl_Position = vec4(aPosition, 1.0) * modelMatrix * viewMatrix * projectionMatrix;
}
