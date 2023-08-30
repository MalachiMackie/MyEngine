#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTextCoords;

// pass the model matrix in 4 attributes because the max size for an attribute is 4 values
layout (location = 2) in vec4 aModelRow1;
layout (location = 3) in vec4 aModelRow2;
layout (location = 4) in vec4 aModelRow3;
layout (location = 5) in vec4 aModelRow4;

uniform mat4 uView;
uniform mat4 uProjection;

out vec2 frag_texCoords;

void main()
{
    mat4 aModel = mat4(aModelRow1, aModelRow2, aModelRow3, aModelRow4);
    gl_Position = uProjection * uView * aModel * vec4(aPosition, 1.0);
    frag_texCoords = aTextCoords;
}