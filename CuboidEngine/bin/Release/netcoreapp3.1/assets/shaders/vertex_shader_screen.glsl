#version 330

layout(location=0) in vec2 vVertices;
layout(location=1) in vec2 vTexcoords;

out vec2 frag_texcoords;

uniform mat4 proj	= mat4(1);
uniform mat4 cam	= mat4(1);
uniform mat4 trans	= mat4(1);

void main() {
  frag_texcoords = vTexcoords;
  gl_Position = proj * vec4( vVertices.xy, 0.5, 1.0 );
} 