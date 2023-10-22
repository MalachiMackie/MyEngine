#version 330 core

layout (location = 0) in vec3 aPosition;

layout (location = 1) in vec2 aTexCoordsA;
layout (location = 2) in vec2 aTexCoordsB;
layout (location = 3) in vec2 aTexCoordsC;
layout (location = 4) in vec2 aTexCoordsD;

// todo: just pass in the required parts. could save 4 bytes here
layout (location = 5) in vec4 aModelRow1;
layout (location = 6) in vec4 aModelRow2;
layout (location = 7) in vec4 aModelRow3;
layout (location = 8) in vec4 aModelRow4;

layout (location = 9) in float aTransparency;
layout (location = 10) in float aTextureIndex;

uniform mat4 uViewProjection;

out vec2 frag_texCoords;
out float frag_transparency;
out float frag_textureIndex;

void main()
{
    mat4 aModel = mat4(aModelRow1, aModelRow2, aModelRow3, aModelRow4);
    gl_Position = uViewProjection * aModel * vec4(aPosition, 1.0);
    switch (gl_VertexID)
    {
        case 0: frag_texCoords = aTexCoordsA; break;
        case 1: frag_texCoords = aTexCoordsB; break;
        case 2: frag_texCoords = aTexCoordsC; break;
        case 3: frag_texCoords = aTexCoordsD; break;
    }
    frag_transparency = aTransparency;
    frag_textureIndex = aTextureIndex;
}