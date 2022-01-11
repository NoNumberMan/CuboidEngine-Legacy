using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Compute.OpenCL;

namespace CuboidEngine {
	public static class OpenCLObjects {
		public const int RequestChunkBufferLength = 256;

		public static long ChunkNumber;
		public static long ChunkSize;

		public static ID RayMarcherKernel   { get; private set; } = new ID( -1 );
		public static ID RequestChunkKernel { get; private set; } = new ID( -1 );
		public static ID PixelBuffer        { get; private set; } = new ID( -1 );
		public static ID CameraBuffer       { get; private set; } = new ID( -1 );
		public static ID MapBuffer          { get; private set; } = new ID( -1 );
		public static ID VoxelBuffer        { get; private set; } = new ID( -1 );
		public static ID RngBuffer          { get; private set; } = new ID( -1 );
		public static ID RequestChunkBuffer { get; private set; } = new ID( -1 );

		public static ID TextureID { get; private set; } = new ID( -1 );

		public static void LoadOpenCLObjects() {
			long voxelBufferMaxSpace = ComputingManager.MemorySizeValue / 2;
			ChunkNumber = voxelBufferMaxSpace / ( Chunk.VoxelCount * Voxel.ByteSize );
			ChunkSize   = ChunkNumber * Chunk.VoxelCount * Voxel.ByteSize;

			RayMarcherKernel   = CEngine.CLLoadKernelFromSources( "render", new[] {LoadKernel( "../../../../CuboidEngine/assets/kernels/raybase_v1.cl", "../../../../CuboidEngine/assets/kernels/raymarcher_v3.cl" )} );
			RequestChunkKernel = CEngine.CLLoadKernelFromSources( "request_chunks", new[] {LoadKernel( "../../../../CuboidEngine/assets/kernels/raybase_v1.cl", "../../../../CuboidEngine/assets/kernels/request_v1.cl" )} );
			VoxelBuffer        = CEngine.CLCreateBuffer( ( int ) ChunkSize, MemoryFlags.ReadOnly );
			RequestChunkBuffer = CEngine.CLCreateBuffer( RequestChunkBufferLength * sizeof( uint ), MemoryFlags.ReadWrite );

			MapBuffer    = CEngine.CLCreateBuffer( ( int ) ( World.WorldSize * World.WorldSize * World.WorldSize * sizeof( int ) ), MemoryFlags.ReadOnly );
			CameraBuffer = CEngine.CLCreateBuffer( 8 * sizeof( float ), MemoryFlags.ReadOnly );
			RngBuffer    = CEngine.CLCreateBuffer( 1920 * 1080 * sizeof( uint ), MemoryFlags.ReadWrite );
			TextureID    = TextureManager.CreateEmptyTexture();
			PixelBuffer  = ComputingManager.CreateTextureBuffer( TextureID );

			CEngine.CLEnqueueFillBuffer( VoxelBuffer, 0, ( int ) ChunkSize, ( byte ) 0 );
			CEngine.CLEnqueueFillBuffer( RequestChunkBuffer, 0, ( int ) RequestChunkBufferLength * sizeof( uint ), ( byte ) 0 );
			CEngine.CLEnqueueWriteBuffer( RngBuffer, 0, GenerateRng() );
		}

		public static uint[] GenerateRng() {
			uint[] results = new uint[1920 * 1080];
			Random r0      = new Random();
			Random r1      = new Random();
			Random r2      = new Random();
			Random r3      = new Random();
			Task t0 = Task.Run( () => {
				for ( int i = 0; i < 518400; ++i ) results[4 * i + 0] = ( uint ) Math.Abs( r0.Next() );
			} );
			Task t1 = Task.Run( () => {
				for ( int i = 0; i < 518400; ++i ) results[4 * i + 1] = ( uint ) Math.Abs( r1.Next() );
			} );
			Task t2 = Task.Run( () => {
				for ( int i = 0; i < 518400; ++i ) results[4 * i + 2] = ( uint ) Math.Abs( r2.Next() );
			} );
			Task t3 = Task.Run( () => {
				for ( int i = 0; i < 518400; ++i ) results[4 * i + 3] = ( uint ) Math.Abs( r3.Next() );
			} );

			Task.WaitAll( t0, t1, t2, t3 );

			return results;
		}

		private static string LoadKernel( params string[] files ) {
			Debug.Assert( files.Length > 0, $"Must have at least 1 file!" );
			for ( int i = 0; i < files.Length; ++i ) Debug.Assert( File.Exists( files[i] ), $"File {files[i]} does not exist!" );

			StringBuilder builder = new StringBuilder();
			builder.AppendLine( "#pragma OPENCL EXTENSION cl_khr_int64_base_atomics: enable" );
			builder.AppendLine( "#pragma OPENCL EXTENSION cl_khr_int64_extended_atomics: enable" );
			builder.AppendLine( $"#define CHUNK_LENGTH {Chunk.ChunkLength}" );
			builder.AppendLine( $"#define CHUNK_LENGTH_F {Chunk.ChunkLength}.0f" );
			builder.AppendLine( $"#define CHUNK_LENGTH_BITS {Chunk.ChunkLengthBits}" );
			builder.AppendLine( $"#define CHUNK_VOXEL_COUNT {Chunk.VoxelCount}" );
			builder.AppendLine( $"#define CHUNK_NUMBER {ChunkNumber}" );
			//builder.AppendLine( $"#define TOTAL_MAP_CHUNK_NUMBER {TotalMapChunkNumber}" );
			builder.AppendLine( $"#define VOXEL_SIZE {Voxel.ByteSize}" );
			builder.AppendLine( $"#define REQUEST_CHUNK_BUFFER_LENGTH {RequestChunkBufferLength}" );
			builder.AppendLine( $"#define WORLD_SIZE {World.WorldSize}ul" );
			builder.AppendLine( $"#define WORLD_OFFSET {World.WorldCenterOffset}ul" );
			builder.Append( $"__constant int MAP_OFFSET[{Chunk.ChunkLengthBits + 1}] = {{" );

			int[] offsetArray = new int[Chunk.ChunkLengthBits + 1];
			int   pos         = 0;
			for ( int i = 0; i < Chunk.ChunkLengthBits + 1; ++i ) {
				offsetArray[i] =  pos;
				pos            += ( 1 << i ) * ( 1 << i ) * ( 1 << i );
			}

			builder.Append( string.Join( ',', offsetArray ) );
			builder.AppendLine( "};" );

			for ( int i = 0; i < files.Length; ++i )
				builder.AppendLine( File.ReadAllText( files[i] ) );

			return builder.ToString();
		}
	}
}