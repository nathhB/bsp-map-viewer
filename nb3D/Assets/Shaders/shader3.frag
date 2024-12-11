#version 330

out vec4 outputColor;
in vec2 texCoord;
in float texIndex;
uniform sampler2DArray texture0;

void main() {
    outputColor = texture(texture0, vec3(texCoord.x, texCoord.y, texIndex));
}