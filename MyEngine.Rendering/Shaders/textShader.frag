#version 330 core

in vec2 frag_texCoords;
in float frag_transparency;
in float frag_textureIndex;

out vec4 out_color;

uniform sampler2DArray uTextures;

void main()
{
    vec4 colour = texture(uTextures, vec3(frag_texCoords, frag_textureIndex));

    colour.a *= frag_transparency;
    out_color = colour;
}