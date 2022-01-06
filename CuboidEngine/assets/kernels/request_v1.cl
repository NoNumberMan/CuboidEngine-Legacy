

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

typedef struct _VoxelFetchResult {
	Voxel voxel;
	byte success;
} VoxelFetchResult;

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

__inline float3 new_dir_x( uint rng ) {
	uint u0 = ( ( rng >> 0 ) & 65535 );
	uint u1 = ( ( rng >> 16 ) & 65535 );

	float theta = 0.0000958738f * ( ( float ) u0 );
	float r = sqrt( 0.0000152588f * ( ( float ) u1 ) );
	return (float3)(sqrt(1.0f - r * r), r * cos(theta), r * sin(theta));
}

__inline float3 new_dir_y( uint rng ) {
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

VoxelFetchResult getVoxel( const int index, __constant Voxel* voxelBuffer, __constant ulong* mapBuffer, __global ulong* requestBuffer, int3* cPos, int3* vPos, int lvl, int* lod, int cd ) {
	ulong chunk_index = (ulong)(cPos->x + WORLD_CHUNK_OFFSET) + WORLD_CHUNK_SIZE * (ulong)(cPos->y + WORLD_CHUNK_OFFSET) + WORLD_CHUNK_SIZE * WORLD_CHUNK_SIZE * (ulong)(cPos->z + WORLD_CHUNK_OFFSET);
	int mapIdx = bin_search( mapBuffer, chunk_index );

	if ( mapIdx == -1 ) {
		requestBuffer[index] = chunk_index;
		return (VoxelFetchResult){(Voxel){0, 0}, 0};
	}
	
	int voxelBufferIdx = mapBuffer[3 + 2 * mapIdx];
	*lod = voxelBufferIdx < LOD0_CHUNK_NUMBER ? 0 : 1;	
	int r = CHUNK_LENGTH_BITS - lvl;
	int offset = MAP_OFFSET[lvl] + ( *lod == 0 ? voxelBufferIdx * LOD0_VOXEL_COUNT : (LOD0_CHUNK_NUMBER * LOD0_VOXEL_COUNT + (voxelBufferIdx - LOD0_CHUNK_NUMBER) * LOD1_VOXEL_COUNT));

	return (VoxelFetchResult){voxelBuffer[offset + ((vPos->x - (CHUNK_LENGTH * cPos->x)) >> r) + (((vPos->y - (CHUNK_LENGTH * cPos->y)) >> r) << lvl) + (((vPos->z - (CHUNK_LENGTH * cPos->z)) >> r) << (2 * lvl))], 1};
}

void extendRay( IntersectResult* intersect_result, const int index, const float* t_total, Ray ray, __constant ulong* mapBuffer, __constant byte* voxelBuffer, __global ulong* requestBuffer ) {
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
		VoxelFetchResult fetchResult = getVoxel( index, (__constant Voxel*)voxelBuffer, mapBuffer, requestBuffer, &cPos, &vPos, lvl, &lod, cd );

		if ( !fetchResult.success ) break;
		
		Voxel voxel = fetchResult.voxel;

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

void tracePath( const int index, const Ray* camray, __constant ulong* mapBuffer, __constant byte* voxelBuffer, __global ulong* requestBuffer, const uint rng ) {
	Ray ray = *camray;

	IntersectResult intersect;
	float t_total = 0.0f;

	for ( int i = 0; i < 2; ++i ) { //bounce once
		extendRay( &intersect, index, &t_total, ray, mapBuffer, voxelBuffer, requestBuffer );

		if ( intersect.hit != HIT_RESULT_INTERSECT || ~requestBuffer[index] != 0 ) {
			return;
		}

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


__kernel void request_chunks( __constant float* camBuffer, __constant ulong* mapBuffer, __constant byte* voxelBuffer, __global ulong* requestBuffer, __global uint* rngBuffer ) {
	const int index = get_global_id(0);
	
	const uint rand = next( rngBuffer, index );
	
	const float r = ((rand >> 0) & 65535) * 0.016784668f;
	const float theta = ((rand >> 16) & 65535) * 0.0000958738f;
	
	const int px = 540 + (int) ( r * cos(theta) );
	const int py = 960 + (int) ( r * sin(theta) );
	requestBuffer[index] = ~0ul;
	
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

		tracePath( index, &camRay, mapBuffer, voxelBuffer, requestBuffer, rand );
	}
}