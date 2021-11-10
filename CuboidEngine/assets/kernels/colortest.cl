
__kernel void make_colors(__write_only image2d_t pixels) {
	int px = get_global_id(0);
	int py = get_global_id(1);
	
	write_imagef(pixels, (int2)(px, py), (float4)((float)(px) / 1920.0, (float)(py) / 1080.0, (float)(px * py) / ( 1920.0 * 1080.0 ), 1.0) );
}