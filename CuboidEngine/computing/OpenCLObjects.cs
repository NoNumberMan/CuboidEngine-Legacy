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
		public const int MapSizeMultiplier        = 4;
		public const int RequestChunkBufferLength = 256;

		public static long Lod0ChunkNumber;
		public static long Lod1ChunkNumber;
		public static long TotalChunkNumber;
		public static long TotalMapChunkNumber;
		public static long Lod0ChunkSize;
		public static long Lod1ChunkSize;

		public static ID RayMarcherKernel   { get; private set; } = new ID( -1 );
		public static ID RequestChunkKernel { get; private set; } = new ID( -1 );
		public static ID PixelBuffer        { get; private set; } = new ID( -1 );
		public static ID CameraBuffer       { get; private set; } = new ID( -1 );
		public static ID MapBuffer          { get; private set; } = new ID( -1 );
		public static ID VoxelBuffer        { get; private set; } = new ID( -1 );
		public static ID RngBuffer          { get; private set; } = new ID( -1 );
		public static ID RequestChunkBuffer { get; private set; } = new ID( -1 );
		public static ID DistanceBuffer     { get; private set; } = new ID( -1 );

		public static ID TextureID { get; private set; } = new ID( -1 );

		public static void LoadOpenCLObjects() {
			long voxelBufferMaxSpace = ComputingManager.MemorySizeValue / 2;
			Lod0ChunkNumber     = 6 * voxelBufferMaxSpace / ( 10 * Chunk.Lod0VoxelCount * Voxel.ByteSize ) / 100;
			Lod1ChunkNumber     = 4 * voxelBufferMaxSpace / ( 10 * Chunk.Lod1VoxelCount * Voxel.ByteSize );
			Lod0ChunkSize       = Lod0ChunkNumber * Chunk.Lod0VoxelCount * Voxel.ByteSize;
			Lod1ChunkSize       = Lod1ChunkNumber * Chunk.Lod1VoxelCount * Voxel.ByteSize;
			TotalChunkNumber    = Lod0ChunkNumber + Lod1ChunkNumber;
			TotalMapChunkNumber = TotalChunkNumber * MapSizeMultiplier;
			long totalChunkSize = Lod0ChunkSize + Lod1ChunkSize;

			RayMarcherKernel   = CEngine.CLLoadKernelFromSources( "render", new[] {LoadKernel( "../../../../CuboidEngine/assets/kernels/raymarcher_v3.cl" )} );
			RequestChunkKernel = CEngine.CLLoadKernelFromSources( "request_chunks", new[] {LoadKernel( "../../../../CuboidEngine/assets/kernels/request_v1.cl" )} );
			VoxelBuffer        = CEngine.CLCreateBuffer( ( int ) totalChunkSize, MemoryFlags.ReadOnly );
			DistanceBuffer     = CEngine.CLCreateBuffer( ( int ) TotalMapChunkNumber, MemoryFlags.ReadWrite );
			RequestChunkBuffer = CEngine.CLCreateBuffer( RequestChunkBufferLength * sizeof( ulong ), MemoryFlags.ReadWrite );

			MapBuffer    = CEngine.CLCreateBuffer( ( int ) ( TotalMapChunkNumber * ChunkMapData.ByteSize ), MemoryFlags.ReadOnly );
			CameraBuffer = CEngine.CLCreateBuffer( 8 * sizeof( float ), MemoryFlags.ReadOnly );
			RngBuffer    = CEngine.CLCreateBuffer( 1920 * 1080 * sizeof( uint ), MemoryFlags.ReadWrite );
			TextureID    = TextureManager.CreateEmptyTexture();
			PixelBuffer  = ComputingManager.CreateTextureBuffer( TextureID );

			CEngine.CLEnqueueFillBuffer( VoxelBuffer, 0, ( int ) totalChunkSize, ( byte ) 0 );
			CEngine.CLEnqueueFillBuffer( DistanceBuffer, 0, ( int ) TotalMapChunkNumber, ( byte ) 255 );
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

		private static string LoadKernel( string file ) {
			Debug.Assert( File.Exists( file ), $"File {file} does not exist!" );
			string        source  = File.ReadAllText( file );
			StringBuilder builder = new StringBuilder();
			builder.AppendLine( "#pragma OPENCL EXTENSION cl_khr_int64_base_atomics: enable" );
			builder.AppendLine( "#pragma OPENCL EXTENSION cl_khr_int64_extended_atomics: enable" );
			builder.AppendLine( $"#define CHUNK_LENGTH {Chunk.ChunkLength}" );
			builder.AppendLine( $"#define LOD0_CHUNK_LENGTH {Chunk.ChunkLength}" );
			builder.AppendLine( $"#define LOD1_CHUNK_LENGTH {Chunk.ChunkLength >> 1}" );
			builder.AppendLine( $"#define CHUNK_LENGTH_F {Chunk.ChunkLength}.0f" );
			builder.AppendLine( $"#define CHUNK_LENGTH_BITS {Chunk.ChunkLengthBits}" );
			builder.AppendLine( $"#define CHUNK_VOXEL_COUNT {Chunk.ChunkVoxelCount}" );
			builder.AppendLine( $"#define LOD0_VOXEL_COUNT {Chunk.Lod0VoxelCount}" );
			builder.AppendLine( $"#define LOD1_VOXEL_COUNT {Chunk.Lod1VoxelCount}" );
			builder.AppendLine( $"#define LOD0_CHUNK_NUMBER {Lod0ChunkNumber}" );
			builder.AppendLine( $"#define LOD1_CHUNK_NUMBER {Lod1ChunkNumber}" );
			builder.AppendLine( $"#define TOTAL_CHUNK_NUMBER {TotalChunkNumber}" );
			builder.AppendLine( $"#define TOTAL_MAP_CHUNK_NUMBER {TotalMapChunkNumber}" );
			builder.AppendLine( $"#define VOXEL_SIZE {Voxel.ByteSize}" );
			builder.AppendLine( $"#define REQUEST_CHUNK_BUFFER_LENGTH {RequestChunkBufferLength}" );
			builder.AppendLine( $"#define WORLD_CHUNK_SIZE {World.WorldSize}ul" );
			builder.AppendLine( $"#define WORLD_CHUNK_OFFSET {World.WorldCenterOffset}ul" );
			builder.Append( $"__constant int MAP_OFFSET[{Chunk.ChunkLengthBits + 1}] = {{" );

			int[] offsetArray = new int[Chunk.ChunkLengthBits + 1];
			int   pos         = 0;
			for ( int i = 0; i < Chunk.ChunkLengthBits + 1; ++i ) {
				offsetArray[i] =  pos;
				pos            += ( 1 << i ) * ( 1 << i ) * ( 1 << i );
			}

			builder.Append( string.Join( ',', offsetArray ) );
			builder.AppendLine( "};" );
			builder.Append( source );

			string str = builder.ToString();
			Console.WriteLine( str );

			return str;
		}
	}
}