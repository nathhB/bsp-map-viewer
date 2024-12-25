#version 330

out vec4 outputColor;
in vec2 texCoord;
in vec2 lightmapCoord;
uniform sampler2D texture0;
uniform sampler2D texture1;

void main() {
    vec4 t0 = texture(texture0, texCoord);
    vec4 t1 = texture(texture1, lightmapCoord);
    float lightmapIntensity = clamp(t1.r + 0.8, 0.0, 1.0);
    vec4 ligthmapColor = vec4(vec3(lightmapIntensity), 1.0);

    outputColor = t0 * ligthmapColor;
    // outputColor = ligthmapColor;
    // outputColor = t0;
}
