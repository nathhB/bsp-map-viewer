#version 330

out vec4 outputColor;
in vec3 texCoord;

uniform samplerCube texture0;

void main() {
    outputColor = texture(texture0, texCoord);
}
