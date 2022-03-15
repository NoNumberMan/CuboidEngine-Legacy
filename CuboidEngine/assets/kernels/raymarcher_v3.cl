
float3 trace_path( const int pixel_idx, const Ray* camray, __constant uint* mapBuffer, __constant byte* voxelBuffer, const uint rng ) {
	Ray ray = *camray;

	float3 color = (float3)(0.0f);
	float3 mask = (float3)(1.0f);
	IntersectResult intersect;
	float t_total = 0.0f;

	int3 ccPos = (int3)((int)floor(ray.pos.x / CHUNK_LENGTH_F), (int)floor(ray.pos.y / CHUNK_LENGTH_F), (int)floor(ray.pos.z / CHUNK_LENGTH_F));

	for ( int i = 0; i < 8; ++i ) { //bounce once
		extend_ray( &intersect, true, &ccPos, &t_total, ray, mapBuffer, voxelBuffer );

		if ( intersect.hit == HIT_RESULT_MISS ) {
			return color;
		}
		
		if ( intersect.hit == HIT_RESULT_SKYBOX ) {
			return color + mask * (float3)(1.0f); //TODO change skybox color
		}

		ray.pos += ray.dir * intersect.t + 0.05f * normals[intersect.face_id].xyz;
		t_total += intersect.t;

		switch(intersect.face_id) {
			case 0: ray.dir = -new_dir_x( rng ); break;
			case 1: ray.dir = -new_dir_y( rng ); break;
			case 2: ray.dir = -new_dir_z( rng ); break;
			case 3: ray.dir = new_dir_x( rng ); break;
			case 4: ray.dir = new_dir_y( rng ); break;
			case 5: ray.dir = new_dir_z( rng ); break;
		}

		ray.dirInv = 1.0f / ray.dir;
		
		int r = ( ( intersect.voxel.color >> 5 ) & 7 );
		int g = ( ( intersect.voxel.color >> 2 ) & 7 );
		int b = ( ( intersect.voxel.color >> 0 ) & 3 );
		int l = ( ( intersect.voxel.allum & 1 ) == 1 ) ? ( intersect.voxel.allum >> 1 ) : 0;
		
		color += mask * ( ((float) l * 0.0078740157f ) * (float3)((float)r, (float)g, (float)b * 2.33333333f) * 0.1428571429f );
		mask *= (float3)((float)r, (float)g, (float)b * 2.33333333f) * 0.1428571429f;
	}

	return color;
}

__kernel void render( __read_write image2d_t pixelBuffer, __constant float* camBuffer, __constant uint* mapBuffer, __constant byte* voxelBuffer, __global uint* rngBuffer ) {
	const int pixel_idx = get_global_id(0);
	const int px = pixel_idx % 1920;
	const int py = pixel_idx / 1920;

	Camera cam;
	cam.pos = (float3)(camBuffer[0], camBuffer[1], camBuffer[2]);
	cam.dir = (float3)(camBuffer[3], camBuffer[4], camBuffer[5]);
	
	const float3 up = (float3)(0.0f, 1.0f, 0.0f);
	const float3 camSpaceX = normalize(cross(up, cam.dir));
	const float3 camSpaceY = (1080.0f / 1920.0f) * cross( cam.dir, camSpaceX );
	
	Ray camRay;
	float3 color = (float3)(0.0f);
	for( int i = 0; i < 10; ++i ) {
		const uint rand = next( rngBuffer, pixel_idx );
		const float dpx = ( ( ( rand >> 0 ) & 65535 ) * 0.0000152588f ) - 0.5f;
		const float dpy = ( ( ( rand >> 16 ) & 65535 ) * 0.0000152588f ) - 0.5f;

		camRay.pos = cam.pos;
		camRay.dir = ( (py + dpy) / 1080.0f - 0.5f ) * camSpaceY + ( (px + dpx) / 1920.0f - 0.5f ) * camSpaceX + 0.62f * cam.dir;
		camRay.dirInv = 1.0f / camRay.dir;

		color += 0.1f * trace_path( pixel_idx, &camRay, mapBuffer, voxelBuffer, rand );
	}

	write_imagef(pixelBuffer, (int2)(px, py), (float4)(color, 1.0f));
}