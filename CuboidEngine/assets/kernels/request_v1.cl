

void trace_path( const int index, const Ray* camray, __constant uint* mapBuffer, __constant byte* voxelBuffer, __global uint* requestBuffer, const uint rng ) {
	Ray ray = *camray;

	int3 ccPos = (int3)((int)floor(ray.pos.x / CHUNK_LENGTH_F), (int)floor(ray.pos.y / CHUNK_LENGTH_F), (int)floor(ray.pos.z / CHUNK_LENGTH_F));
	IntersectResult intersect;
	float t_total = 0.0f;

	for ( int i = 0; i < 2; ++i ) { //bounce once
		extend_ray( &intersect, &ccPos, &t_total, ray, mapBuffer, voxelBuffer );

		if ( intersect.hit == HIT_RESULT_MISS ) {
			ray.pos += ray.dir * intersect.t;
			int3 cPos = (int3)((int)floor(ray.pos.x / CHUNK_LENGTH_F), (int)floor(ray.pos.y / CHUNK_LENGTH_F), (int)floor(ray.pos.z / CHUNK_LENGTH_F));
			
			//printf( "%i \n", (cPos.z - ccPos.z) );
			
			uint chunkIndex = (uint)((cPos.x - ccPos.x) + WORLD_OFFSET) + WORLD_SIZE * (uint)((cPos.y - ccPos.y) + WORLD_OFFSET) + WORLD_SIZE * WORLD_SIZE * (uint)((cPos.z - ccPos.z) + WORLD_OFFSET);
			requestBuffer[index] = ( chunkIndex | 0xff000000 );
			return;
		}
		
		if ( intersect.hit == HIT_RESULT_SKYBOX ) return;

		ray.pos += ray.dir * intersect.t + 0.005f * normals[intersect.face_id];
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
	}
}


__kernel void request_chunks( __constant float* camBuffer, __constant uint* mapBuffer, __constant byte* voxelBuffer, __global uint* requestBuffer, __global uint* rngBuffer ) {
	const int index = get_global_id(0);
	
	const uint rand = next( rngBuffer, index );
	
	const float r = ((rand >> 0) & 65535) * 0.0167849241f;
	const float theta = ((rand >> 16) & 65535) * 0.0000958738f;
	
	const int px = 960 + (int) ( r * cos(theta) );
	const int py = 540 + (int) ( r * sin(theta) );
	
	if (!(px < 0 || px >= 1920 || py < 0 || py >= 1080)) {
		Camera cam;
		cam.pos = (float3)(camBuffer[0], camBuffer[1], camBuffer[2]);
		cam.dir = (float3)(camBuffer[3], camBuffer[4], camBuffer[5]);
		
		const float3 up = (float3)(0.0f, 1.0f, 0.0f);
		const float3 camSpaceX = normalize(cross(up, cam.dir));
		const float3 camSpaceY = ( 1080.0f / 1920.0f ) * cross( cam.dir, camSpaceX );
		
		Ray camRay;
		camRay.pos = cam.pos;
		camRay.dir = ( py / 1080.0f - 0.5f ) * camSpaceY + ( px / 1920.0f - 0.5f ) * camSpaceX + 0.62f * cam.dir;
		camRay.dirInv = 1.0f / camRay.dir;

		trace_path( index, &camRay, mapBuffer, voxelBuffer, requestBuffer, rand );
	}
}