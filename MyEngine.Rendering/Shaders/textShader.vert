#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTextCoords;
layout (location = 2) in vec2 aModelPosition;

uniform mat4 uProjection;

out vec2 frag_texCoords;

void main()
{
    gl_Position = uProjection * (vec4(aModelPosition, 0.0, 0.0) + vec4(aPosition, 1.0));
    frag_texCoords = aTextCoords;
}