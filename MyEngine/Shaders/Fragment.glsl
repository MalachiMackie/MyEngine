#version 330 core

in vec2 frag_texCoords;

out vec4 out_color;

// Now we define a uniform value!
// A uniform in OpenGL is a value that can be changed outside of the shader by modifying its value.
// A sampler2D contains both a texture and information on how to sample it.
// Sampling a texture is basically calculating the color of a pixel on a texture at any given point.
uniform sampler2D uTexture;

void main()
{
    out_color = texture(uTexture, frag_texCoords);
}