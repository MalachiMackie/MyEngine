#version 330 core

in vec2 frag_texCoords;
in float frag_transparency;

out vec4 out_color;

uniform sampler2D uTexture;

void main()
{
    vec4 colour = texture(uTexture, frag_texCoords);
    colour.a *= frag_transparency;
    out_color = colour;
}