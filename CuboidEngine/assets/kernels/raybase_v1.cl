

#define HIT_RESULT_MISS 0
#define HIT_RESULT_SKYBOX 1
#define HIT_RESULT_INTERSECT 2

typedef uchar byte;

__constant float3 normal_x = (float3)( 1.0f, 0.0f, 0.0f );
__constant float3 normal_y = (float3)( 0.0f, 1.0f, 0.0f );
__constant float3 normal_z = (float3)( 0.0f, 0.0f, 1.0f );
__constant float4 normals[6] = { (float4)( -1.0f, 0.0f, 0.0f, 0.0f ), (float4)( 0.0f, -1.0f, 0.0f, 0.0f ), 
	(float4)( 0.0f, 0.0f, -1.0f, 0.0f ), (float4)( 1.0f, 0.0f, 0.0f, 0.0f ), (float4)( 0.0f, 1.0f, 0.0f, 0.0f ), (float4)( 0.0f, 0.0f, 1.0f, 0.0f ) };


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


__inline uint next( __global uint* buffer, int idx ) {
	uint x = buffer[idx];
	buffer[idx] = (x * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);
	return x;
}

__inline float3 new_dir_x( uint rng ) {
	uint u0 = ( ( rng >> 0 ) & 65535 );
	uint u1 = ( ( rng >> 16 ) & 65535 );

	double theta = 0.0000958738 * ( ( double ) u0 );
	double r = sqrt( 0.0000152588 * ( ( double ) u1 ) );
	return (float3)(sqrt(1.0 - r * r), r * cos(theta), r * sin(theta));
}

__inline float3 new_dir_y( uint rng ) {
	uint u0 = ( ( rng >> 0 ) & 65535 );
	uint u1 = ( ( rng >> 16 ) & 65535 );

	double theta = 0.0000958738 * ( ( double ) u0 );
	double r = sqrt( 0.0000152588 * ( ( double ) u1 ) );

	return (float3)(r * cos(theta), sqrt(1.0 - r * r), r * sin(theta));
}

__inline float3 new_dir_z( uint rng ) {
	uint u0 = ( ( rng >> 0 ) & 65535 );
	uint u1 = ( ( rng >> 16 ) & 65535 );

	double theta = 0.0000958738 * ( ( double ) u0 );
	double r = sqrt( 0.0000152588 * ( ( double ) u1 ) );
	return (float3)(r * cos(theta), r * sin(theta), sqrt(1.0 - r * r));
}

__inline float intersect( const float3* pos, const float3* dir, const float3* dirInv, const float size ) {
	float tMaxX = dir->x >= 0.0f ? (size - pos->x) * dirInv->x : -pos->x * dirInv->x;
	float tMaxY = dir->y >= 0.0f ? (size - pos->y) * dirInv->y : -pos->y * dirInv->y;
	float tMaxZ = dir->z >= 0.0f ? (size - pos->z) * dirInv->z : -pos->z * dirInv->z;

	if (tMaxX < tMaxY) return tMaxX < tMaxZ ? tMaxX : tMaxZ;
	return tMaxY < tMaxZ ? tMaxY : tMaxZ;
}

__inline float3 normal( const float3 pos, const float size ) {
	float3 normal = -normal_x;
	float dist = pos.x;

	if ( pos.y < dist ) dist = pos.y, normal = -normal_y;
	if ( pos.z < dist ) dist = pos.z, normal = -normal_z;
	if ( (size - pos.x) < dist ) dist = (size - pos.x), normal = normal_x;
	if ( (size - pos.y) < dist ) dist = (size - pos.y), normal = normal_y;
	if ( (size - pos.z) < dist ) dist = (size - pos.z), normal = normal_z;

	return normal;
}

__inline int get_face_idx( const float3 pos, const float size ) {
	int face_idx = 0;
	float dist = fabs(pos.x);

	if ( pos.y < dist ) dist = fabs(pos.y), face_idx = 1;
	if ( pos.z < dist ) dist = fabs(pos.z), face_idx = 2;
	if ( (size - pos.x) < dist ) dist = fabs(size - pos.x), face_idx = 3;
	if ( (size - pos.y) < dist ) dist = fabs(size - pos.y), face_idx = 4;
	if ( (size - pos.z) < dist ) dist = fabs(size - pos.z), face_idx = 5;

	return face_idx;
}

//TODO split into getChunk and getVoxel function
VoxelFetchResult get_voxel( const int3* ccPos, __constant Voxel* voxelBuffer, __constant uint* mapBuffer, int3* cPos, int3* vPos, int lvl) {
	uint chunkIndex = (uint)((cPos->x - ccPos->x) + WORLD_OFFSET) + WORLD_SIZE * (uint)((cPos->y - ccPos->y) + WORLD_OFFSET) + WORLD_SIZE * WORLD_SIZE * (uint)((cPos->z - ccPos->z) + WORLD_OFFSET);
	int mapIdx = mapBuffer[chunkIndex];

	if ( mapIdx == -1 ) {
		return (VoxelFetchResult){(Voxel){0,0},0};
	}

	int r = CHUNK_LENGTH_BITS - lvl;
	int offset = MAP_OFFSET[lvl] + mapIdx * CHUNK_VOXEL_COUNT;

	return (VoxelFetchResult){voxelBuffer[offset + ((vPos->x - (CHUNK_LENGTH * cPos->x)) >> r) + (((vPos->y - (CHUNK_LENGTH * cPos->y)) >> r) << lvl) + (((vPos->z - (CHUNK_LENGTH * cPos->z)) >> r) << (2 * lvl))], 1};
}

void extend_ray( IntersectResult* intersectResult, const bool allowMissingChunk, const int3* ccPos, const float* tTotal, Ray ray, __constant uint* mapBuffer, __constant byte* voxelBuffer ) {
	float t = 0.0f;
	int lvl = 0;
	int steps = 0;
	while ( steps++ < 256 && t < 4096.0f ) { //distance based cutoff, may not work TODOTODOTODOTODO
		float size = (float)(CHUNK_LENGTH >> lvl);
		
		int3 vPos = (int3)((int)floor(ray.pos.x), (int)floor(ray.pos.y), (int)floor(ray.pos.z)); //voxel pos in voxelBuffer coordinates
		int3 cPos = (int3)((int)floor(ray.pos.x / CHUNK_LENGTH_F), (int)floor(ray.pos.y / CHUNK_LENGTH_F), (int)floor(ray.pos.z / CHUNK_LENGTH_F));
		float3 rPos = ray.pos - size * floor(ray.pos / size);

		if ( abs(cPos.x - ccPos->x) >= WORLD_OFFSET-1 || abs(cPos.y - ccPos->y) >= WORLD_OFFSET-1 || abs(cPos.z - ccPos->z) >= WORLD_OFFSET-1) {
			break;
		}

		VoxelFetchResult fetchResult = get_voxel( ccPos, (__constant Voxel*)voxelBuffer, mapBuffer, &cPos, &vPos, lvl );

		if ( !fetchResult.success && !allowMissingChunk ) {
			intersectResult->hit = HIT_RESULT_MISS; //TODO maybe pointing up??????
			intersectResult->t = t;
			return;
		}

		Voxel voxel = fetchResult.voxel;

		if ( voxel.allum > 0 ) {
			if ( lvl < CHUNK_LENGTH_BITS ) {
				++lvl;
				continue;
			}

			intersectResult->hit = HIT_RESULT_INTERSECT;
			intersectResult->voxel = voxel;
			intersectResult->t = t;
			intersectResult->face_id = get_face_idx(rPos, size);
			return;
		}

		float dt = max((intersect(&rPos, &ray.dir, &ray.dirInv, size) + 0.00001f), 0.001f);
		ray.pos += dt * ray.dir;
		t += dt;

		if ( ray.pos.y > 512.0f ) { //TODO make height limit
			intersectResult->hit = HIT_RESULT_SKYBOX;
			intersectResult->t = t;
			return;
		}

		//TODO kind of a mess
		int r = CHUNK_LENGTH_BITS - lvl;
		int3 vPosN = (int3)((int)floor(ray.pos.x), (int)floor(ray.pos.y), (int)floor(ray.pos.z));
		int3 d = ((vPos >> r) & 1) - ((vPosN >> r) & 1);
		lvl = ( lvl > 0 && ( d.x > 0 || d.y > 0 || d.z > 0 ) ) ? lvl - 1 : lvl;
	}

	intersectResult->hit = dot(ray.dir, (float3)(0.0f, 1.0f, 0.0f)) > 0.0f ? HIT_RESULT_SKYBOX : HIT_RESULT_MISS;
	intersectResult->t = t;
}
