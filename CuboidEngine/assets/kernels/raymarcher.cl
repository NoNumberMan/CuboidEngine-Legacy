
__constant int CHUNK_OFFSET = 32768;
__constant float3 light_dir = (float3)( 0.2f, -0.8f, -0.4f );

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

int bin_search(__constant ulong* map, ulong key) {
	int first = 0;
	int last = (int) map[0] - 1;
	int mid = (int) last / 2;

	while ( first <= last ) {
		ulong val = map[2 * mid];
		if (val < key) {
			first = mid + 1;
		}
		else if (val == key) {
			return map[2 * mid + 1];
		}
		else {
			last = mid - 1;
		}

		mid = ( first + last ) / 2;
	}

	return -1;
}

__inline Voxel getVolumeColor( __constant Voxel* world, __constant ulong* map, int3* cPos, int3* vPos, int lvl ) {	
	ulong index = (ulong)(cPos->x + CHUNK_OFFSET) + 65536ul * (ulong)(cPos->y + CHUNK_OFFSET) + 65536ul * 65536ul * (ulong)(cPos->z + CHUNK_OFFSET);
	int worldIdx = bin_search( map, index );

	if ( worldIdx == -1 ) return (Voxel){ 0, 0 };

	int offset = MAP_OFFSET[lvl];
	int r = CHUNK_LENGTH_BITS - lvl;
	return world[worldIdx * CHUNK_VOXEL_COUNT + offset + ((vPos->x - CHUNK_LENGTH * cPos->x) >> r) + (((vPos->y - CHUNK_LENGTH * cPos->y) >> r) << lvl) + (((vPos->z - CHUNK_LENGTH * cPos->z) >> r) << (2 * lvl))];
}

float intersect( float3* pos, float3* dir, float3* dirInv, float size ) {
	float tMaxX = dir->x >= 0.0f ? (size - pos->x) * dirInv->x : -pos->x * dirInv->x;
	float tMaxY = dir->y >= 0.0f ? (size - pos->y) * dirInv->y : -pos->y * dirInv->y;
	float tMaxZ = dir->z >= 0.0f ? (size - pos->z) * dirInv->z : -pos->z * dirInv->z;

	if (tMaxX < tMaxY) return tMaxX < tMaxZ ? tMaxX : tMaxZ;
	return tMaxY < tMaxZ ? tMaxY : tMaxZ;
}


//TODO
//1x. update chunk structure / more chunks
//2x. LOD
//3x. realistic world generation -> simplex/perlin/conventional noise pls
//4x. cleanup/tweaking
//5. mutiple pointers, point to same chunk
//6. dynamic chunk fetching
//7. shadow rays
//8. adding lights
//9. path tracer (tm)
__kernel void marchRays(__write_only image2d_t pixels, __constant ulong* map, __constant float* camArray, __constant byte* lod0Voxels, __constant byte* lod0Distances, __global long* request ) {
	int px = get_global_id(0);
	int py = get_global_id(1);

	RayStack stack;
	Camera cam;
	cam.pos = (float3)(camArray[0], camArray[1], camArray[2]);
	cam.dir = (float3)(camArray[3], camArray[4], camArray[5]);
	cam.size = (float2)(camArray[6], camArray[7]);
	
	float3 up = (float3)(0.0f, 1.0f, 0.0f);
	float3 camSpaceX = normalize(cross(up, cam.dir));
	float3 camSpaceY = ( cam.size.y / cam.size.x ) * cross( cam.dir, camSpaceX );
	float3 offset = ( py / 1080.0f - 0.5f ) * camSpaceY + ( px / 1920.0f - 0.5f ) * camSpaceX + 0.62f * cam.dir;
	int3 cPosCam = (int3)((int)floor(cam.pos.x / 32.0f), (int)floor(cam.pos.y / 32.0f), (int)floor(cam.pos.z / 32.0f));
	//Ray startRay;
	//startRay.pos = world;
	//startRay.dir = world - cam.pos;
	//startRay.dirInv = 1.0f / startRay.dir;
	//stack.stack[0] = startRay;
	//add_to_stack( &startRay, &stack );
	stack.ptr = 1;

	//write_imagef(pixels, (int2)(px, py), (float4)(length(camSpaceY), 0.0f, 0.0f, 1.0f));
	//return;

	
	float3 last_norm = -cam.dir;
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
			int3 vPos = (int3)((int)floor(ray.pos.x), (int)floor(ray.pos.y), (int)floor(ray.pos.z)); //voxel pos in world coordinates
			int3 cPos = (int3)((int)floor(ray.pos.x / CHUNK_LENGTH_F), (int)floor(ray.pos.y / CHUNK_LENGTH_F), (int)floor(ray.pos.z / CHUNK_LENGTH_F));
			int cd = ( cPos.x - cPosCam.x ) * (cPos.x - cPosCam.x) + (cPos.y - cPosCam.y) * (cPos.y - cPosCam.y) + (cPos.z - cPosCam.z) * (cPos.z - cPosCam.z);

			Voxel voxel = getVolumeColor( (__constant Voxel*)world, map, &cPos, &vPos, lvl );

			if ( voxel.allum > 0 ) {
				if (lvl < CHUNK_LENGTH_BITS ) {
				//if (lvl < 5 - ( int ) ( cd / 16 ) ) {
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

				color = color * max( 0.1f, dot( light_dir, last_norm ) );

				break;
			}

			float size = (float)(CHUNK_LENGTH >> lvl);
			float3 rPos = ray.pos - size * floor(ray.pos / size);
			float t = intersect(&rPos, &ray.dir, &ray.dirInv, size);
			ray.pos += max((t + 0.00001f), 0.001f) * ray.dir;
			dist_traveled += max((t + 0.00001f), 0.001f);

			int r = CHUNK_LENGTH_BITS - lvl;
			int3 vPosN = (int3)((int)floor(ray.pos.x), (int)floor(ray.pos.y), (int)floor(ray.pos.z));
			int3 d = ((vPos >> r) & 1) - ((vPosN >> r) & 1);
			lvl = ( lvl > 0 && ( d.x > 0 || d.y > 0 || d.z > 0 ) ) ? lvl - 1 : lvl;

			last_norm = (float3)( -d.x, -d.y, -d.z ) / ( d.x * d.x + d.y * d.y + d.z * d.z );
		}

		//color = mix(color, (float3)(0.5, 0.8, 1.0), dist_traveled / 512.0f );
	}

	write_imagef(pixels, (int2)(px, py), (float4)(color, 1.0f));
}