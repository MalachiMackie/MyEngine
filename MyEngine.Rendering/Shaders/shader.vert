#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTextCoords;

// pass the model matrix in 4 attributes because the max size for an attribute is 4 values
layout (location = 2) in vec4 aModelRow1;
layout (location = 3) in vec4 aModelRow2;
layout (location = 4) in vec4 aModelRow3;
layout (location = 5) in vec4 aModelRow4;

layout (location = 6) in float aTransparency;
layout (location = 7) in float aTextureIndex;

uniform mat4 uViewProjection;

out vec2 frag_texCoords;
out float frag_transparency;
out float frag_textureIndex;

void main()
{
    mat4 aModel = mat4(aModelRow1, aModelRow2, aModelRow3, aModelRow4);
    gl_Position = uViewProjection * aModel * vec4(aPosition, 1.0);
    frag_texCoords = aTextCoords;
    frag_transparency = aTransparency;
    frag_textureIndex = aTextureIndex;
}