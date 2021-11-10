using System.Drawing;
using OpenTK.Compute.OpenCL;

namespace CuboidEngine {
	public static class OpenCLObjects {
		public static ID RayMarcherKernel { get; private set; } = new ID( -1 );
		public static ID PixelBuffer      { get; private set; } = new ID( -1 );
		public static ID VoxelBuffer      { get; private set; } = new ID( -1 );
		public static ID CameraBuffer     { get; private set; } = new ID( -1 );
		public static ID Map4Buffer       { get; private set; } = new ID( -1 );
		public static ID Map3Buffer       { get; private set; } = new ID( -1 );
		public static ID Map2Buffer       { get; private set; } = new ID( -1 );
		public static ID Map1Buffer       { get; private set; } = new ID( -1 );
		public static ID MapBuffer        { get; private set; } = new ID( -1 );

		public static ID TextureID { get; private set; } = new ID( -1 );

		public static void LoadOpenCLObjects() {
			RayMarcherKernel = CEngine.LoadKernelFromFiles( "marchRays", new[] {"../../../../CuboidEngine/assets/kernels/raymarcher.cl"} );
			VoxelBuffer      = CEngine.CreateBuffer( Chunk.ChunkSize, MemoryFlags.ReadOnly );
			MapBuffer        = CEngine.CreateBuffer( 1 + 8 + 64 + 512 + 4096, MemoryFlags.ReadOnly );


			TextureID    = TextureManager.CreateEmptyTexture();
			PixelBuffer  = ComputingManager.CreateTextureBuffer( TextureID );
			CameraBuffer = CEngine.CreateBuffer( 8 * sizeof( float ), MemoryFlags.ReadOnly );
		}
	}
}