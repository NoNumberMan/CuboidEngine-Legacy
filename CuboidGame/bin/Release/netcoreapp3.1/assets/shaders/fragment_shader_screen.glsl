#version 330

in vec2 frag_texcoords;

out vec4 color_out;

uniform sampler2D txture;

void main() {
	//color_out = vec4( 0.0, 1.0, 0.0, 1.0 );
	color_out = vec4( texture( txture, frag_texcoords ).rgb, 1.0 );
}