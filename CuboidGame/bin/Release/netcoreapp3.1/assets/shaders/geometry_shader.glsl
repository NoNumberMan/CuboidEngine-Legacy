#version 330

layout (points) in;
layout (triangle_strip, max_vertices = 24) out;

in vec4 geom_color[];
in uint geom_occlusion[];
in mat4 transform[];

out vec4 frag_color;
out vec3 frag_norm;

uniform vec3 cam_dir = vec3(1);

const vec3 north = vec3( 0.0, 0.0, 1.0 );
const vec3 east = vec3( 1.0, 0.0, 0.0 );
const vec3 south = vec3( 0.0, 0.0, -1.0 );
const vec3 west = vec3( -1.0, 0.0, 0.0 );
const vec3 top = vec3( 0.0, 1.0, 0.0 );
const vec3 bottom = vec3( 0.0, -1.0, 0.0 );

void main() {
	vec4 center = gl_in[0].gl_Position;
    mat4 trans = transform[0];

    if ( ( geom_occlusion[0] & 16u ) == 0u ) { //n
        frag_color = geom_color[0];
        frag_norm = north;
        gl_Position = trans * ( center + vec4(0.0, 0.0, 1.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = north;
        gl_Position = trans * ( center + vec4(1.0, 0.0, 1.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = north;
        gl_Position = trans * ( center + vec4(0.0, 1.0, 1.0, 0.0) );
        EmitVertex();


        frag_color = geom_color[0];
        frag_norm = north;
        gl_Position = trans * ( center + vec4(1.0, 1.0, 1.0, 0.0) );
        EmitVertex();
        EndPrimitive();
    }

    if ( ( geom_occlusion[0] & 1u ) == 0u ) { //e
        frag_color = geom_color[0];
        frag_norm = east;
        gl_Position = trans * ( center + vec4(1.0, 0.0, 0.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = east;
        gl_Position = trans * ( center + vec4(1.0, 0.0, 1.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = east;
        gl_Position = trans * ( center + vec4(1.0, 1.0, 0.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = east;
        gl_Position = trans * ( center + vec4(1.0, 1.0, 1.0, 0.0) );
        EmitVertex();
        EndPrimitive();
    }

    if ( ( geom_occlusion[0] & 32u ) == 0u ) { //s
        frag_color = geom_color[0];
        frag_norm = south;
        gl_Position = trans * ( center + vec4(0.0, 0.0, 0.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = south;
        gl_Position = trans * ( center + vec4(1.0, 0.0, 0.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = south;
        gl_Position = trans * ( center + vec4(0.0, 1.0, 0.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = south;
        gl_Position = trans * ( center + vec4(1.0, 1.0, 0.0, 0.0) );
        EmitVertex();
        EndPrimitive();
    }

    if ( ( geom_occlusion[0] & 2u ) == 0u ) { //w
        frag_color = geom_color[0];
        frag_norm = west;
        gl_Position = trans * ( center + vec4(0.0, 0.0, 0.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = west;
        gl_Position = trans * ( center + vec4(0.0, 1.0, 0.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = west;
        gl_Position = trans * ( center + vec4(0.0, 0.0, 1.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = west;
        gl_Position = trans * ( center + vec4(0.0, 1.0, 1.0, 0.0) );
        EmitVertex();
        EndPrimitive();
    }

    if ( ( geom_occlusion[0] & 4u ) == 0u ) { //t
        frag_color = geom_color[0];
        frag_norm = top;
        gl_Position = trans * ( center + vec4(0.0, 1.0, 0.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = top;
        gl_Position = trans * ( center + vec4(1.0, 1.0, 0.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = top;
        gl_Position = trans * ( center + vec4(0.0, 1.0, 1.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = top;
        gl_Position = trans * ( center + vec4(1.0, 1.0, 1.0, 0.0) );
        EmitVertex();
        EndPrimitive();
    }

    if ( ( geom_occlusion[0] & 8u ) == 0u ) { //b 
        frag_color = geom_color[0];
        frag_norm = bottom;
        gl_Position = trans * ( center + vec4(0.0, 0.0, 0.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = bottom;
        gl_Position = trans * ( center + vec4(0.0, 0.0, 1.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = bottom;
        gl_Position = trans * ( center + vec4(1.0, 0.0, 0.0, 0.0) );
        EmitVertex();

        frag_color = geom_color[0];
        frag_norm = bottom;
        gl_Position = trans * ( center + vec4(1.0, 0.0, 1.0, 0.0) );
        EmitVertex();
        EndPrimitive();
    }
}