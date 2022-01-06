

__constant float3 light_dir = (float3)( 0.0f, -1.0f, -0.0f );
__constant float3 normal_x = (float3)( 1.0f, 0.0f, 0.0f );
__constant float3 normal_y = (float3)( 0.0f, 1.0f, 0.0f );
__constant float3 normal_z = (float3)( 0.0f, 0.0f, 1.0f );

typedef uchar byte;

typedef struct __attribute__((packed)) _Camera {
	float3 pos;
	float3 dir;
	float2 size;
} Camera;

typedef struct _Ray { //64 bytes max
	float3 pos;
	float3 dir;
	float3 dirInv;
} Ray;

typedef struct _Voxel {
	byte color;
	byte allum;
} Voxel;

typedef struct _RayStack {
	Ray stack[16];
	int ptr;
} RayStack;

void add_to_stack( Ray* ray, RayStack* stack ){
	stack->stack[stack->ptr++] = *ray;
}

void get_from_stack( Ray* ray, RayStack* stack ) {
	*ray = stack->stack[stack->ptr--];
}

int bin_search(__constant ulong* mapBuffer, ulong key) {
	int first = 0;
	int last = (int) mapBuffer[0] - 1;
	int mid = (int) last / 2;

	while ( first <= last ) {
		ulong val = mapBuffer[2 + 2 * mid];
		if (val < key) {
			first = mid + 1;
		}
		else if (val == key) {
			return mid;
		}
		else {
			last = mid - 1;
		}

		mid = ( first + last ) / 2;
	}

	return -1;
}

__inline Voxel getVolumeColor( __constant Voxel* voxelBuffer, __constant ulong* mapBuffer, volatile __global ulong* requestBuffer, __global uint* distanceBuffer, int3* cPos, int3* vPos, int lvl, int* lod, int cd ) {
	ulong index = (ulong)(cPos->x + WORLD_CHUNK_OFFSET) + WORLD_CHUNK_SIZE * (ulong)(cPos->y + WORLD_CHUNK_OFFSET) + WORLD_CHUNK_SIZE * WORLD_CHUNK_SIZE * (ulong)(cPos->z + WORLD_CHUNK_OFFSET);
	int mapIdx = bin_search( mapBuffer, index );

	if ( mapIdx == -1 ) {
		mapIdx = 0;

		if (atomic_min(requestBuffer, (ulong)cd) > cd) {
			atomic_xchg(requestBuffer + 1, index);
		}
	}
	else {
		atomic_min(distanceBuffer + mapIdx, cd);
	}
	
	int voxelBufferIdx = mapBuffer[3 + 2 * mapIdx];
	*lod = voxelBufferIdx < LOD0_CHUNK_NUMBER ? 0 : 1;	
	int r = CHUNK_LENGTH_BITS - lvl;
	int offset = MAP_OFFSET[lvl] + ( *lod == 0 ? voxelBufferIdx * LOD0_VOXEL_COUNT : (LOD0_CHUNK_NUMBER * LOD0_VOXEL_COUNT + (voxelBufferIdx - LOD0_CHUNK_NUMBER) * LOD1_VOXEL_COUNT));

	return voxelBuffer[offset + ((vPos->x - (CHUNK_LENGTH * cPos->x)) >> r) + (((vPos->y - (CHUNK_LENGTH * cPos->y)) >> r) << lvl) + (((vPos->z - (CHUNK_LENGTH * cPos->z)) >> r) << (2 * lvl))];
}

__inline float intersect( float3* pos, float3* dir, float3* dirInv, float size ) {
	float tMaxX = dir->x >= 0.0f ? (size - pos->x) * dirInv->x : -pos->x * dirInv->x;
	float tMaxY = dir->y >= 0.0f ? (size - pos->y) * dirInv->y : -pos->y * dirInv->y;
	float tMaxZ = dir->z >= 0.0f ? (size - pos->z) * dirInv->z : -pos->z * dirInv->z;

	if (tMaxX < tMaxY) return tMaxX < tMaxZ ? tMaxX : tMaxZ;
	return tMaxY < tMaxZ ? tMaxY : tMaxZ;
}

__inline float3 normal( float3 pos, float size ) {
	float3 normal = -normal_x;
	float dist = pos.x;

	if ( pos.y < dist ) dist = pos.y, normal = -normal_y;
	if ( pos.z < dist ) dist = pos.z, normal = -normal_z;
	if ( (size - pos.x) < dist ) dist = (size - pos.x), normal = normal_x;
	if ( (size - pos.y) < dist ) dist = (size - pos.y), normal = normal_y;
	if ( (size - pos.z) < dist ) dist = (size - pos.z), normal = normal_z;

	return normal;
}



//TODO
//1x. update chunk structure / more chunks
//2x. LOD
//3x. realistic voxelBuffer generation -> simplex/perlin/conventional noise pls
//4x. cleanup/tweaking
//5x. mutiple pointers, point to same chunk
//6x. dynamic chunk fetching
//7. shadow rays
//8. adding lights
//9. path tracer (tm)
__kernel void marchRays(__write_only image2d_t pixelBuffer, __constant float* camBuffer, __constant ulong* mapBuffer, __constant byte* voxelBuffer, __global uint* distanceBuffer, volatile __global ulong* requestBuffer) {
	int px = get_global_id(0);
	int py = get_global_id(1);

	RayStack stack;
	Camera cam;
	cam.pos = (float3)(camBuffer[0], camBuffer[1], camBuffer[2]);
	cam.dir = (float3)(camBuffer[3], camBuffer[4], camBuffer[5]);
	cam.size = (float2)(camBuffer[6], camBuffer[7]);
	
	float3 up = (float3)(0.0f, 1.0f, 0.0f);
	float3 camSpaceX = normalize(cross(up, cam.dir));
	float3 camSpaceY = ( cam.size.y / cam.size.x ) * cross( cam.dir, camSpaceX );
	float3 offset = ( py / 1080.0f - 0.5f ) * camSpaceY + ( px / 1920.0f - 0.5f ) * camSpaceX + 0.62f * cam.dir;
	int3 cPosCam = (int3)((int)floor(cam.pos.x / 32.0f), (int)floor(cam.pos.y / 32.0f), (int)floor(cam.pos.z / 32.0f));
	//Ray startRay;
	//startRay.pos = voxelBuffer;
	//startRay.dir = voxelBuffer - cam.pos;
	//startRay.dirInv = 1.0f / startRay.dir;
	//stack.stack[0] = startRay;
	//add_to_stack( &startRay, &stack );
	stack.ptr = 1;

	//write_imagef(pixelBuffer, (int2)(px, py), (float4)(length(camSpaceY), 0.0f, 0.0f, 1.0f));
	//return;
	
	//float3 last_norm = -cam.dir;
	float3 color = (float3)(0.0f);
	int steps = 0;
	while( stack.ptr > 0 ) {
		Ray ray;
		ray.pos = cam.pos;
		ray.dir = offset;
		ray.dirInv = 1.0f / ray.dir;
		stack.ptr = 0;
		//get_from_stack( &ray, &stack );
		//ray = stack.stack[stack.ptr--];

		//start marching
		int lvl = 0;
		float dist_traveled = 0.0f;
		while ( steps++ < 128 ) {
			float size = (float)(CHUNK_LENGTH >> lvl);
			
			int3 vPos = (int3)((int)floor(ray.pos.x), (int)floor(ray.pos.y), (int)floor(ray.pos.z)); //voxel pos in voxelBuffer coordinates
			int3 cPos = (int3)((int)floor(ray.pos.x / CHUNK_LENGTH_F), (int)floor(ray.pos.y / CHUNK_LENGTH_F), (int)floor(ray.pos.z / CHUNK_LENGTH_F));
			float3 rPos = ray.pos - size * floor(ray.pos / size);
			float3 n = normal(rPos, size);
			int cd = ( cPos.x - cPosCam.x ) * (cPos.x - cPosCam.x) + (cPos.y - cPosCam.y) * (cPos.y - cPosCam.y) + (cPos.z - cPosCam.z) * (cPos.z - cPosCam.z);

			int lod = 0;
			Voxel voxel = getVolumeColor( (__constant Voxel*)voxelBuffer, mapBuffer, requestBuffer, distanceBuffer, &cPos, &vPos, lvl, &lod, cd );
			
			if ( voxel.allum > 0 ) {
				if ( lvl < CHUNK_LENGTH_BITS - lod ) {
					++lvl;
					continue;
				}

				//another bounce
				//make new ray and push
				int r = ( ( voxel.color >> 5 ) & 7 );
				int g = ( ( voxel.color >> 2 ) & 7 );
				int b = ( ( voxel.color >> 0 ) & 3 );
				float rf = (float)r / 7.0f;
				float gf = (float)g / 7.0f;
				float bf = (float)b / 3.0f;
				color = ((float3)( rf, gf, bf ));

				color = color * max( 0.1f, dot( -light_dir, n ) );

				break;
			}

			float t = max((intersect(&rPos, &ray.dir, &ray.dirInv, size) + 0.00001f), 0.001f);
			ray.pos += t * ray.dir;
			dist_traveled += t;

			int r = CHUNK_LENGTH_BITS - lvl;
			int3 vPosN = (int3)((int)floor(ray.pos.x), (int)floor(ray.pos.y), (int)floor(ray.pos.z));
			int3 d = ((vPos >> r) & 1) - ((vPosN >> r) & 1);
			//int3 dPos = vPosN - vPos;
			lvl = ( lvl > 0 && ( d.x > 0 || d.y > 0 || d.z > 0 ) ) ? lvl - 1 : lvl;

			//if ( !(dPos.x == 0 && dPos.y == 0 && dPos.z == 0) )
			//	last_norm = normalize((float3)(dPos.x, dPos.y, dPos.z));
		}

		//color = mix(color, (float3)(0.5, 0.8, 1.0), dist_traveled / 512.0f );
	}

	write_imagef(pixelBuffer, (int2)(px, py), (float4)(color, 1.0f));
}