#version 330

layout(location=0) in uint vData;

out vec4 geom_color;
out uint geom_occlusion;
out mat4 transform;

uniform mat4 proj	= mat4(1);
uniform mat4 cam	= mat4(1);
uniform mat4 trans	= mat4(1);

void main() {
  uint dx = ( vData >> 24 );
  uint dy = ( vData >> 16 );
  uint dz = ( vData >> 8 );

  float x = float( ( dx & 31u ) );
  float y = float( ( dy & 31u ) );
  float z = float( ( dz & 31u ) );
  float r = float( ( ( vData >> 0 ) & 192u ) ) / 192.0;
  float g = float( ( ( vData >> 0 ) & 48u ) ) / 48.0;
  float b = float( ( ( vData >> 0 ) & 12u ) ) / 12.0;
  float a = float( ( ( vData >> 0 ) & 3u ) ) / 3.0;

  geom_occlusion = 0u;
  geom_occlusion += ( ( ( dx >> 6 ) & 1u ) << 0 ); //e
  geom_occlusion += ( ( ( dx >> 7 ) & 1u ) << 1 ); //w
  geom_occlusion += ( ( ( dy >> 6 ) & 1u ) << 2 ); //t
  geom_occlusion += ( ( ( dy >> 7 ) & 1u ) << 3 ); //b
  geom_occlusion += ( ( ( dz >> 6 ) & 1u ) << 4 ); //n
  geom_occlusion += ( ( ( dz >> 7 ) & 1u ) << 5 ); //s

  geom_color = vec4( r, g, b, a );
  transform = proj * cam * trans;

  gl_Position = vec4( x, y, z, 1.0 );
} 