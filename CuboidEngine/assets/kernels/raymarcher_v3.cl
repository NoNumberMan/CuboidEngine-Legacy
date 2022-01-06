

#define HIT_RESULT_MISS 0
#define HIT_RESULT_SKYBOX 1
#define HIT_RESULT_INTERSECT 2

__constant float3 light_dir = (float3)( 0.0f, -1.0f, -0.0f );
__constant float3 normal_x = (float3)( 1.0f, 0.0f, 0.0f );
__constant float3 normal_y = (float3)( 0.0f, 1.0f, 0.0f );
__constant float3 normal_z = (float3)( 0.0f, 0.0f, 1.0f );
__constant float3 normals[6] = { (float3)( -1.0f, 0.0f, 0.0f ), (float3)( 0.0f, -1.0f, 0.0f ), 
	(float3)( 0.0f, 0.0f, -1.0f ), (float3)( 1.0f, 0.0f, 0.0f ), (float3)( 0.0f, 1.0f, 0.0f ), (float3)( 0.0f, 0.0f, 1.0f ) };


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
	Ray stack[4];
	int ptr;
} RayStack;

typedef struct _IntersectResult {
	Voxel voxel;
	float t;
	int face_id;
	int hit;
} IntersectResult;


uint next( __global uint* buffer, int idx ) {
	uint x = buffer[idx];
	buffer[idx] = (x * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);
	return x;
}

void stack_push( Ray ray, RayStack* stack ) {
	stack->stack[stack->ptr++] = ray;
}

Ray stack_pop( RayStack* stack ) {
	return stack->stack[stack->ptr--];
}

__inline float3 new_dir_x( uint rng ) {
	uint u0 = ( ( rng >> 0 ) & 65535 );
	uint u1 = ( ( rng >> 16 ) & 65535 );

	float theta = 0.0000958738f * ( ( float ) u0 );
	float r = sqrt( 0.0000152588f * ( ( float ) u1 ) );
	return (float3)(sqrt(1.0f - r * r), r * cos(theta), r * sin(theta));
}

__inline float3 new_dir_y( const uint asdf, uint rng ) {
	uint u0 = ( ( rng >> 0 ) & 65535 );
	uint u1 = ( ( rng >> 16 ) & 65535 );

	float theta = 0.0000958738f * ( ( float ) u0 );
	float r = sqrt( 0.0000152588f * ( ( float ) u1 ) );

	return (float3)(r * cos(theta), sqrt(1.0f - r * r), r * sin(theta));
}

__inline float3 new_dir_z( uint rng ) {
	uint u0 = ( ( rng >> 0 ) & 65535 );
	uint u1 = ( ( rng >> 16 ) & 65535 );

	float theta = 0.0000958738f * ( ( float ) u0 );
	float r = sqrt( 0.0000152588f * ( ( float ) u1 ) );
	return (float3)(r * cos(theta), r * sin(theta), sqrt(1.0f - r * r));
}


__inline int bin_search(__constant ulong* mapBuffer, ulong key) {
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

__inline void push_request_chunk( __global ulong* requestBuffer, int index ) {
	for ( int i = 0; i < requestBuffer[0]; ++i )
}

Voxel getVoxel( __constant Voxel* voxelBuffer, __constant ulong* mapBuffer, __global ulong* requestBuffer, __global uint* distanceBuffer, int3* cPos, int3* vPos, int lvl, int* lod, int cd ) {
	ulong index = (ulong)(cPos->x + WORLD_CHUNK_OFFSET) + WORLD_CHUNK_SIZE * (ulong)(cPos->y + WORLD_CHUNK_OFFSET) + WORLD_CHUNK_SIZE * WORLD_CHUNK_SIZE * (ulong)(cPos->z + WORLD_CHUNK_OFFSET);
	int mapIdx = bin_search( mapBuffer, index );

	if ( mapIdx == -1 ) {
		mapIdx = 0;

		if(*requestBuffer > cd) {
			atomic_store(requestBuffer, cd);
		}

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

__inline int get_face_idx( float3 pos, float size ) {
	int face_idx = 0;
	float dist = pos.x;

	if ( pos.y < dist ) dist = pos.y, face_idx = 1;
	if ( pos.z < dist ) dist = pos.z, face_idx = 2;
	if ( (size - pos.x) < dist ) dist = (size - pos.x), face_idx = 3;
	if ( (size - pos.y) < dist ) dist = (size - pos.y), face_idx = 4;
	if ( (size - pos.z) < dist ) dist = (size - pos.z), face_idx = 5;

	return face_idx;
}

void extendRay( IntersectResult* intersect_result, const float* t_total, Ray ray, __constant ulong* mapBuffer, __constant byte* voxelBuffer, __global uint* distanceBuffer, __global ulong* requestBuffer ) {
	float t = 0.0f;
	int lvl = 0;
	int steps = 0;
	while ( steps++ < 128 ) { //distance based cutoff, may not work
		float size = (float)(CHUNK_LENGTH >> lvl);
		
		int3 vPos = (int3)((int)floor(ray.pos.x), (int)floor(ray.pos.y), (int)floor(ray.pos.z)); //voxel pos in voxelBuffer coordinates
		int3 cPos = (int3)((int)floor(ray.pos.x / CHUNK_LENGTH_F), (int)floor(ray.pos.y / CHUNK_LENGTH_F), (int)floor(ray.pos.z / CHUNK_LENGTH_F));
		int cd = (((uint)(*t_total + t)) >> 5);
		float3 rPos = ray.pos - size * floor(ray.pos / size);

		int lod = 0;
		Voxel voxel = getVoxel( (__constant Voxel*)voxelBuffer, mapBuffer, requestBuffer, distanceBuffer, &cPos, &vPos, lvl, &lod, cd );
		//voxel.color = 255;
		//voxel.allum = 255;

		if ( voxel.allum > 0 ) {
			if ( lvl < CHUNK_LENGTH_BITS - lod ) {
				++lvl;
				continue;
			}

			intersect_result->hit = HIT_RESULT_INTERSECT;
			intersect_result->voxel = voxel;
			intersect_result->t = t;
			intersect_result->face_id = get_face_idx(rPos, size);
			return;
		}

		float dt = max((intersect(&rPos, &ray.dir, &ray.dirInv, size) + 0.00001f), 0.001f);
		ray.pos += dt * ray.dir;
		t += dt;

		if ( ray.pos.y > 1024.0f ) { //TODO make height limit
			intersect_result->hit = HIT_RESULT_SKYBOX;
			return;
		}

		int r = CHUNK_LENGTH_BITS - lvl;
		int3 vPosN = (int3)((int)floor(ray.pos.x), (int)floor(ray.pos.y), (int)floor(ray.pos.z));
		int3 d = ((vPos >> r) & 1) - ((vPosN >> r) & 1);
		lvl = ( lvl > 0 && ( d.x > 0 || d.y > 0 || d.z > 0 ) ) ? lvl - 1 : lvl;
	}

	intersect_result->hit = HIT_RESULT_MISS;
}

float3 tracePath( const int doOut, const Ray* camray, __constant ulong* mapBuffer, __constant byte* voxelBuffer, __global uint* distanceBuffer, __global ulong* requestBuffer, const uint rng ) {
	Ray ray = *camray;

	float3 color = (float3)(0.0f);
	float3 mask = (float3)(1.0f);
	IntersectResult intersect;
	float t_total = 0.0f;

	for ( int i = 0; i < 3; ++i ) { //bounce once
		extendRay( &intersect, &t_total, ray, mapBuffer, voxelBuffer, distanceBuffer, requestBuffer );

		if ( intersect.hit == HIT_RESULT_MISS ) {
			return color;
		}
		
		if ( intersect.hit == HIT_RESULT_SKYBOX ) {
			return color + mask * (float3)(0.0f); //TODO change skybox color
		}

		//otherwise intersect.hit == intersect

		ray.pos += ray.dir * intersect.t + 0.005f * normals[intersect.face_id];
		t_total += intersect.t;

		switch(intersect.face_id) {
			case 0: ray.dir = -new_dir_x( rng ); break;
			case 1: ray.dir = -new_dir_y( doOut, rng ); break;
			case 2: ray.dir = -new_dir_z( rng ); break;
			case 3: ray.dir = new_dir_x( rng ); break;
			case 4: ray.dir = new_dir_y( doOut, rng ); break;
			case 5: ray.dir = new_dir_z( rng ); break;
		}

		ray.dirInv = 1.0f / ray.dir;
		
		int r = ( ( intersect.voxel.color >> 5 ) & 7 );
		int g = ( ( intersect.voxel.color >> 2 ) & 7 );
		int b = ( ( intersect.voxel.color >> 0 ) & 3 );
		int l = ( ( intersect.voxel.allum & 1 ) == 1 ) ? ( intersect.voxel.allum >> 1 ) : 0;
		
		color += mask * ( (float) l * 0.0078740157f * (float3)((float)r, (float)g, (float)b * 2.33333333f) * 0.1428571429f );
		mask *= (float3)((float)r, (float)g, (float)b * 2.33333333f) * 0.1428571429f;
	}

	return color; //return black
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
__kernel void render(__read_write image2d_t pixelBuffer, __constant float* camBuffer, __constant ulong* mapBuffer, __constant byte* voxelBuffer, __global uint* distanceBuffer, __global ulong* requestBuffer, __global uint* rngBuffer ) {
	const int pixel_idx = get_global_id(0);
	const int px = pixel_idx % 1920;
	const int py = pixel_idx / 1920;

	Camera cam;
	cam.pos = (float3)(camBuffer[0], camBuffer[1], camBuffer[2]);
	cam.dir = (float3)(camBuffer[3], camBuffer[4], camBuffer[5]);
	
	const float3 up = (float3)(0.0f, 1.0f, 0.0f);
	const float3 camSpaceX = normalize(cross(up, cam.dir));
	const float3 camSpaceY = ( 1080.0f / 1920.0f ) * cross( cam.dir, camSpaceX );
	
	Ray camRay;
	float3 color = (float3)(0.0f);
	for( int i = 0; i < 1; ++i ) {
		const uint rand = next( rngBuffer, pixel_idx );
		const float dpx = ( ( ( rand >> 0 ) & 65535 ) * 0.0000152588f ) - 0.5f;
		const float dpy = ( ( ( rand >> 16 ) & 65535 ) * 0.0000152588f ) - 0.5f;

		camRay.pos = cam.pos;
		camRay.dir = ( (py + dpy) / 1080.0f - 0.5f ) * camSpaceY + ( (px + dpx) / 1920.0f - 0.5f ) * camSpaceX + 0.62f * cam.dir;
		camRay.dirInv = 1.0f / camRay.dir;

		color += 1.0f * tracePath( (px == 0 && py == 0), &camRay, mapBuffer, voxelBuffer, distanceBuffer, requestBuffer, rand );
	}

	write_imagef(pixelBuffer, (int2)(px, py), (float4)(color, 1.0f));
}