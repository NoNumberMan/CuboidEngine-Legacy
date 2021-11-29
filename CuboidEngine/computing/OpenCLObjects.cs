using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using OpenTK.Compute.OpenCL;

namespace CuboidEngine {
	public static class OpenCLObjects {
		public static ID RayMarcherKernel { get; private set; } = new ID( -1 );
		public static ID PixelBuffer      { get; private set; } = new ID( -1 );
		public static ID VoxelBuffer      { get; private set; } = new ID( -1 );
		public static ID CameraBuffer     { get; private set; } = new ID( -1 );
		public static ID MetaBuffer       { get; private set; } = new ID( -1 );
		public static ID MapBuffer        { get; private set; } = new ID( -1 );

		public static ID TextureID { get; private set; } = new ID( -1 );

		public static void LoadOpenCLObjects() {
			long voxelBufferMaxSpace = ComputingManager.memorySizeValue / 2;
			long voxelChunkNumber    = voxelBufferMaxSpace / ( Chunk.ChunkVoxelCount * Voxel.ByteSize );

			RayMarcherKernel = CEngine.LoadKernelFromSources( "marchRays", new[] {LoadRayMarchingKernel( "../../../../CuboidEngine/assets/kernels/raymarcher.cl" )} );
			VoxelBuffer      = CEngine.CreateBuffer( ( int ) ( voxelChunkNumber * Chunk.ChunkVoxelCount ), MemoryFlags.ReadOnly );
			MapBuffer        = CEngine.CreateBuffer( ( int ) ( voxelChunkNumber * ChunkMapData.ByteSize ), MemoryFlags.ReadOnly );
			MetaBuffer       = CEngine.CreateBuffer( 2 * sizeof( int ), MemoryFlags.ReadOnly );
			CameraBuffer     = CEngine.CreateBuffer( 8 * sizeof( float ), MemoryFlags.ReadOnly );

			TextureID   = TextureManager.CreateEmptyTexture();
			PixelBuffer = ComputingManager.CreateTextureBuffer( TextureID );
		}

		private static string LoadRayMarchingKernel( string file ) {
			Debug.Assert( File.Exists( file ), $"File {file} does not exist!" );
			string        source  = File.ReadAllText( file );
			StringBuilder builder = new StringBuilder();
			builder.AppendLine( $"#define CHUNK_LENGTH {Chunk.ChunkLength}" );
			builder.AppendLine( $"#define CHUNK_LENGTH_F {Chunk.ChunkLength}.0f" );
			builder.AppendLine( $"#define CHUNK_LENGTH_BITS {Chunk.ChunkLengthBits}" );
			builder.AppendLine( $"#define CHUNK_VOXEL_COUNT {Chunk.ChunkVoxelCount}" );
			builder.AppendLine( $"#define VOXEL_SIZE {Voxel.ByteSize}" );
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