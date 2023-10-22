#version 330 core

layout (location = 0) in vec3 aVertexPosition;
layout (location = 1) in float aTransparency;
layout (location = 2) in float aTextureIndex;
layout (location = 3) in vec2 aPosition;
layout (location = 4) in vec2 aScale;
layout (location = 5) in vec2 aTexCoord1;
layout (location = 6) in vec2 aTexCoord2;
layout (location = 7) in vec2 aTexCoord3;
layout (location = 8) in vec2 aTexCoord4;

uniform mat4 uProjection;

out vec2 frag_texCoords;
out float frag_transparency;
out float frag_textureIndex;

void main()
{
    mat4 scaleMatrix = mat4(
        vec4(aScale.x, 0, 0, 0),
        vec4(0, aScale.y, 0, 0),
        vec4(0, 0, 1, 0),
        vec4(0, 0, 0, 1)
    );
    mat4 translationMatrix = mat4(
        vec4(1, 0, 0, 0),
        vec4(0, 1, 0, 0),
        vec4(0, 0, 1, 0),
        vec4(aPosition.x, aPosition.y, 0, 1)
    );

    mat4 modelMatrix = translationMatrix * scaleMatrix;

    gl_Position = uProjection * modelMatrix * vec4(aVertexPosition, 1.0);
    switch (gl_VertexID)
    {
        case 0: frag_texCoords = aTexCoord1; break;
        case 1: frag_texCoords = aTexCoord2; break;
        case 2: frag_texCoords = aTexCoord3; break;
        case 3: frag_texCoords = aTexCoord4; break;
    }
    frag_transparency = aTransparency;
    frag_textureIndex = aTextureIndex;
}