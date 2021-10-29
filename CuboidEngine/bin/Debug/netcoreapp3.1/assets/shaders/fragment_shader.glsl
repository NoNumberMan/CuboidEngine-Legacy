#version 330

in vec4 frag_color;
in vec3 frag_norm;

out vec4 color_out;

const vec3 light_dir = vec3(0.298142, -0.745356, 0.596285);

void main() {
	float light = max( 0.0, -1.0 * dot(frag_norm, light_dir));

	//color_out = vec4(1.0, 0.0, 0.0, 1.0);
	color_out = frag_color * ( 0.2 + 0.8 * light );
}