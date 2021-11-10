#define MAP0_OFFSET 0
#define MAP1_OFFSET 1
#define MAP2_OFFSET 9
#define MAP3_OFFSET 73
#define MAP4_OFFSET 585

__constant int MAP_OFFSET[5] = { MAP0_OFFSET, MAP1_OFFSET, MAP2_OFFSET, MAP3_OFFSET, MAP4_OFFSET };


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

byte getVolumeColor( __constant byte* voxels, __constant byte* map, int3* vPos, int lvl ) {
	//we know, we only have a chunk at 0,0,1
	if ( vPos->x < 0 || vPos->x >= 32 || vPos->y < 0 || vPos->y >= 32 || vPos->z < 32 || vPos->z >= 64 ) return 0;

	if ( lvl < 5 ) {
		//do something with map
		int offset = MAP_OFFSET[lvl];
		int r = 5 - lvl;
		return map[offset + (vPos->x >> r) + ((vPos->y >> r) << lvl) + (((vPos->z-32) >> r) << (2 * lvl))];
	}
	else return voxels[ vPos->x + 32 * vPos->y + 32 * 32 * ( vPos->z - 32 ) ]; //TODO figure something out with chunk offsets or something
}

float intersect( float3* pos, float3* dir, float3* dirInv, float size ) {
	float tMaxX = dir->x >= 0.0f ? (size - pos->x) * dirInv->x : -pos->x * dirInv->x;
	float tMaxY = dir->y >= 0.0f ? (size - pos->y) * dirInv->y : -pos->y * dirInv->y;
	float tMaxZ = dir->z >= 0.0f ? (size - pos->z) * dirInv->z : -pos->z * dirInv->z;

	if (tMaxX < tMaxY) return tMaxX < tMaxZ ? tMaxX : tMaxZ;
	return tMaxY < tMaxZ ? tMaxY : tMaxZ;
}


//TODO
//1. more chunks / update chunk structure
//2. LOD
//3. shadow rays
//4. dynamic chunk fetching
//5. path tracer (tm)
__kernel void marchRays(__write_only image2d_t pixels, __constant byte* voxels, __constant byte* map, __constant float* camArray ) {
	int px = get_global_id(0);
	int py = get_global_id(1);

	RayStack stack;
	Camera cam;
	cam.pos = (float3)(camArray[0], camArray[1], camArray[2]);
	cam.dir = (float3)(camArray[3], camArray[4], camArray[5]);
	cam.size = (float2)(camArray[6], camArray[7]);
	
	float3 up = (float3)(0.0f, 1.0f, 0.0f);
	float3 camSpaceX = normalize(cross(up, cam.dir)); //TODO 
	float3 camSpaceY = ( cam.size.y / cam.size.x ) * cross( cam.dir, camSpaceX );
	float3 offset = ( py / cam.size.y - 0.5f ) * camSpaceY + ( px / cam.size.x - 0.5f ) * camSpaceX + 0.62f * cam.dir;

	//Ray startRay;
	//startRay.pos = world;
	//startRay.dir = world - cam.pos;
	//startRay.dirInv = 1.0f / startRay.dir;
	//stack.stack[0] = startRay;
	//add_to_stack( &startRay, &stack );
	stack.ptr = 1;

	//write_imagef(pixels, (int2)(px, py), (float4)(length(camSpaceY), 0.0f, 0.0f, 1.0f));
	//return;

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
		while ( steps++ < 64 ) {
			int3 vPos = (int3)((int)floor(ray.pos.x), (int)floor(ray.pos.y), (int)floor(ray.pos.z)); //voxel pos in world coordinates

			byte vc = getVolumeColor( voxels, map, &vPos, lvl );
			if ( vc > 0 ) {
				if (lvl < 5 ) {
					++lvl;
					continue;
				}

				//another bounce
				//make new ray and push
				int r = ( ( vc >> 6 ) & 3 );
				int g = ( ( vc >> 4 ) & 3 );
				int b = ( ( vc >> 2 ) & 3 );
				float rf = (float)r / 3.0f;
				float gf = (float)g / 3.0f;
				float bf = (float)b / 3.0f;
				color = ((float3)( rf, gf, bf ));
				break;
			}

			

			float size = (float)(32 >> lvl);
			float3 rPos = ray.pos - size * floor(ray.pos / size);
			float t = intersect(&rPos, &ray.dir, &ray.dirInv, size);
			ray.pos += max((t + 0.00001f), 0.001f) * ray.dir;

			int r = 5 - lvl;
			int3 vPosN = (int3)((int)floor(ray.pos.x), (int)floor(ray.pos.y), (int)floor(ray.pos.z));
			int3 d = ((vPos >> r) & 1) - ((vPosN >> r) & 1);
			lvl = ( lvl > 0 && ( d.x > 0 || d.y > 0 || d.z > 0 ) ) ? lvl - 1 : lvl;
		}
	}

	write_imagef(pixels, (int2)(px, py), (float4)(color, 1.0f));
}