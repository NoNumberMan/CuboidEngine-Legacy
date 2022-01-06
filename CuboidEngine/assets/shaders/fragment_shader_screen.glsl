#version 330

in vec2 frag_texcoords;

out vec4 color_out;

uniform sampler2D txture;

void main() {
	int px = clamp(int(frag_texcoords.x * 1920.0f), 0, 1920);
	int py = clamp(int(frag_texcoords.y * 1080.0f), 0, 1080);

	vec3 color = texture( txture, frag_texcoords ).rgb;
	
	//color += 0.6065306597f * texture( txture, vec2(min(px+1, 1920) * 0.0005208333f, py * 0.0009259259f) ).rgb;
	//color += 0.6065306597f * texture( txture, vec2(px * 0.0005208333f, min(py+1, 1080) * 0.0009259259f) ).rgb;
	//color += 0.6065306597f * texture( txture, vec2(px * 0.0005208333f, max(py-1, 0) * 0.0009259259f) ).rgb;
	//color += 0.6065306597f * texture( txture, vec2(max(px-1, 0) * 0.0005208333f, py * 0.0009259259f) ).rgb;
	//color += 0.3678794412f * texture( txture, vec2(min(px+1, 1920) * 0.0005208333f, min(py+1, 1080) * 0.0009259259f) ).rgb;
	//color += 0.3678794412f * texture( txture, vec2(max(px-1, 0) * 0.0005208333f, min(py+1, 1080) * 0.0009259259f) ).rgb;
	//color += 0.3678794412f * texture( txture, vec2(max(px-1, 0) * 0.0005208333f, max(py-1, 0) * 0.0009259259f) ).rgb;
	//color += 0.3678794412f * texture( txture, vec2(min(px+1, 1920) * 0.0005208333f, max(py-1, 0) * 0.0009259259f) ).rgb;
	//color *= 0.2520501906;

	color_out = vec4( color, 1.0 );
}