#version 330 core

in vec2 frag_texCoords;
in float frag_transparency;
flat in int frag_textureIndex;
flat in int frag_instanceId;

out vec4 out_color;

uniform sampler2D uTextures[32];

void main()
{
    vec4 colour;

    switch (frag_textureIndex)
    {
        case 0: colour = texture(uTextures[0], frag_texCoords); break;
        case 1: colour = texture(uTextures[1], frag_texCoords); break;
        case 2: colour = texture(uTextures[2], frag_texCoords); break;
        case 3: colour = texture(uTextures[3], frag_texCoords); break;
        case 4: colour = texture(uTextures[4], frag_texCoords); break;
        case 5: colour = texture(uTextures[5], frag_texCoords); break;
        case 6: colour = texture(uTextures[6], frag_texCoords); break;
        case 7: colour = texture(uTextures[7], frag_texCoords); break;
        case 8: colour = texture(uTextures[8], frag_texCoords); break;
        case 9: colour = texture(uTextures[9], frag_texCoords); break;
        case 10: colour = texture(uTextures[10], frag_texCoords); break;
        case 11: colour = texture(uTextures[11], frag_texCoords); break;
        case 12: colour = texture(uTextures[12], frag_texCoords); break;
        case 13: colour = texture(uTextures[13], frag_texCoords); break;
        case 14: colour = texture(uTextures[14], frag_texCoords); break;
        case 15: colour = texture(uTextures[15], frag_texCoords); break;
        case 16: colour = texture(uTextures[16], frag_texCoords); break;
        case 17: colour = texture(uTextures[17], frag_texCoords); break;
        case 18: colour = texture(uTextures[18], frag_texCoords); break;
        case 19: colour = texture(uTextures[19], frag_texCoords); break;
        case 20: colour = texture(uTextures[20], frag_texCoords); break;
        case 21: colour = texture(uTextures[21], frag_texCoords); break;
        case 22: colour = texture(uTextures[22], frag_texCoords); break;
        case 23: colour = texture(uTextures[23], frag_texCoords); break;
        case 24: colour = texture(uTextures[24], frag_texCoords); break;
        case 25: colour = texture(uTextures[25], frag_texCoords); break;
        case 26: colour = texture(uTextures[26], frag_texCoords); break;
        case 27: colour = texture(uTextures[27], frag_texCoords); break;
        case 28: colour = texture(uTextures[28], frag_texCoords); break;
        case 29: colour = texture(uTextures[29], frag_texCoords); break;
        case 30: colour = texture(uTextures[30], frag_texCoords); break;
        case 31: colour = texture(uTextures[31], frag_texCoords); break;
        default: colour = texture(uTextures[0], frag_texCoords); break;
    }

    colour.a *= frag_transparency;

    out_color = colour;
}