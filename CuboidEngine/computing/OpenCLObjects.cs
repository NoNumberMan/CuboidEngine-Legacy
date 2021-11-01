using System.Drawing;
using OpenTK.Compute.OpenCL;

namespace CuboidEngine {
	public static class OpenCLObjects {
		public static ID RayMarcherKernel { get; private set; } = new ID( -1 );
		public static ID PixelBuffer      { get; private set; } = new ID( -1 );
		public static ID VoxelBuffer      { get; private set; } = new ID( -1 );
		public static ID Map4Buffer       { get; private set; } = new ID( -1 );
		public static ID Map3Buffer       { get; private set; } = new ID( -1 );
		public static ID Map2Buffer       { get; private set; } = new ID( -1 );
		public static ID Map1Buffer       { get; private set; } = new ID( -1 );
		public static ID Map0Buffer       { get; private set; } = new ID( -1 );

		public static void LoadOpenCLObjects() {
			RayMarcherKernel = CEngine.LoadKernelFromFiles( "unknown", new[] {"raymarcher.cl"} );
			PixelBuffer      = CEngine.CreateBuffer( 1920 * 1080 * 3 * sizeof( float ), MemoryFlags.WriteOnly );
			VoxelBuffer      = CEngine.CreateBuffer( Chunk.ChunkSize, MemoryFlags.ReadOnly );
			Map4Buffer       = CEngine.CreateBuffer( 4096, MemoryFlags.ReadOnly );
			Map3Buffer       = CEngine.CreateBuffer( 512, MemoryFlags.ReadOnly );
			Map2Buffer       = CEngine.CreateBuffer( 64, MemoryFlags.ReadOnly );
			Map1Buffer       = CEngine.CreateBuffer( 8, MemoryFlags.ReadOnly );
			Map0Buffer       = CEngine.CreateBuffer( 1, MemoryFlags.ReadOnly );
		}
	}
}