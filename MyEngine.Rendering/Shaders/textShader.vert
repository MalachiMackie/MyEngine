#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTextCoords;
layout (location = 2) in vec2 aModelPosition;
layout (location = 3) in float aTransparency;

uniform mat4 uProjection;

out vec2 frag_texCoords;
out float frag_transparency;

void main()
{
    gl_Position = uProjection * (vec4(aModelPosition, 0.0, 0.0) + vec4(aPosition, 1.0));
    frag_texCoords = aTextCoords;
    frag_transparency = aTransparency;
}