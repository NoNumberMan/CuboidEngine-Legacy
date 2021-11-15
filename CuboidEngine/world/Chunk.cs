using System;
using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.ES11;

namespace CuboidEngine {
	internal class Chunk {
		public const int ChunkLength      = 32;
		public const int ChunkLengthShift = 5;
		public const int DistMapLength    = ChunkLength >> 1;
		public const int ChunkSize        = ChunkLength * ChunkLength * ChunkLength;

		public bool IsDirty { get; set; } = true;

		private readonly Voxel[] _voxels;
		private readonly byte[]  _volume0;
		private readonly byte[]  _volume1;
		private readonly byte[]  _volume2;
		private readonly byte[]  _volume3;
		private readonly byte[]  _volume4;

		public Span<Voxel> Voxels => _voxels.AsSpan();
		public Span<byte>  Vol0   => _volume0.AsSpan();
		public Span<byte>  Vol1   => _volume1.AsSpan();
		public Span<byte>  Vol2   => _volume2.AsSpan();
		public Span<byte>  Vol3   => _volume3.AsSpan();
		public Span<byte>  Vol4   => _volume4.AsSpan();

		public Chunk() {
			_voxels  = new Voxel[ChunkSize]; //32x32x32
			_volume0 = new byte[1];          //1x1x1
			_volume1 = new byte[8];          //2x2x2
			_volume2 = new byte[64];         //4x4x4
			_volume3 = new byte[512];        //8x8x8
			_volume4 = new byte[4096];       //16x16x16
		}

		public Chunk( byte[] voxels ) {
			_voxels  = new Voxel[ChunkSize]; //32x32x32
			_volume0 = new byte[1];          //1x1x1
			_volume1 = new byte[8];          //2x2x2
			_volume2 = new byte[64];         //4x4x4
			_volume3 = new byte[512];        //8x8x8
			_volume4 = new byte[4096];       //16x16x16

			for ( int i = 0; i < ChunkSize; ++i ) _voxels[i] = new Voxel( voxels[i] );

			for ( int iz = 0; iz < 16; ++iz )
			for ( int iy = 0; iy < 16; ++iy )
			for ( int ix = 0; ix < 16; ++ix )
				_volume4[ix + 16 * iy + 16 * 16 * iz] = GetAverageColor( 4, ix, iy, iz );

			for ( int iz = 0; iz < 8; ++iz )
			for ( int iy = 0; iy < 8; ++iy )
			for ( int ix = 0; ix < 8; ++ix )
				_volume3[ix + 8 * iy + 8 * 8 * iz] = GetAverageColor( 3, ix, iy, iz );

			for ( int iz = 0; iz < 4; ++iz )
			for ( int iy = 0; iy < 4; ++iy )
			for ( int ix = 0; ix < 4; ++ix )
				_volume2[ix + 4 * iy + 4 * 4 * iz] = GetAverageColor( 2, ix, iy, iz );

			for ( int iz = 0; iz < 2; ++iz )
			for ( int iy = 0; iy < 2; ++iy )
			for ( int ix = 0; ix < 2; ++ix )
				_volume1[ix + 2 * iy + 4 * iz] = GetAverageColor( 1, ix, iy, iz );

			_volume0[0] = GetAverageColor( 0, 0, 0, 0 );
		}

		private byte GetColor( int lvl, int x, int y, int z ) {
			return lvl switch {
				0 => _volume0[x + y + z],
				1 => _volume1[x + ( y << 1 ) + ( z << 2 )],
				2 => _volume2[x + ( y << 2 ) + ( z << 4 )],
				3 => _volume3[x + ( y << 3 ) + ( z << 6 )],
				4 => _volume4[x + ( y << 4 ) + ( z << 8 )],
				5 => _voxels[x + ( y << 5 ) + ( z << 10 )].color,
				_ => throw new IndexOutOfRangeException()
			};
		}

		private byte GetAverageColor( int lvl, int x, int y, int z ) {
			byte avgr = 0;
			byte avgg = 0;
			byte avgb = 0;
			byte avga = 0;
			byte t    = 0;
			for ( int sz = 2 * z; sz < 2 * z + 2; ++sz )
			for ( int sy = 2 * y; sy < 2 * y + 2; ++sy )
			for ( int sx = 2 * x; sx < 2 * x + 2; ++sx ) {
				byte c = GetColor( lvl + 1, sx, sy, sz );
				avgr += ( byte ) ( ( c >> 6 ) & 3 ); //11 -> 3 + 3 + 3 / 8 -> 1 * 0.333f -> 0.99998 -> 0 -> 
				avgg += ( byte ) ( ( c >> 4 ) & 3 );
				avgb += ( byte ) ( ( c >> 2 ) & 3 );
				avga += ( byte ) ( ( c >> 0 ) & 3 ); //11+11+11.. / 8 -> 11 = 3
				if ( c > 0 ) ++t;
			}

			return t == 0 ? ( byte ) 0 : ( byte ) ( ( ( avgr / t ) << 6 ) + ( ( avgg / t ) << 4 ) + ( ( avgb / t ) << 2 ) + ( ( avga / t ) << 0 ) );
		}

		public Voxel this[ int x, int y, int z ] {
			get => _voxels[x + ChunkLength * y + ChunkLength * ChunkLength * z];
			set {
				if ( value.color != 0 ) {
					int tx = x >> 5;

					_voxels[x + ( y << 5 ) + ( z << 10 )]                            = value;
					_volume4[( x >> 1 ) + ( ( y >> 1 ) << 4 ) + ( ( z >> 1 ) << 8 )] = GetAverageColor( 4, x >> 1, y >> 1, z >> 1 );
					_volume3[( x >> 2 ) + ( ( y >> 2 ) << 3 ) + ( ( z >> 2 ) << 6 )] = GetAverageColor( 3, x >> 2, y >> 2, z >> 2 );
					_volume2[( x >> 3 ) + ( ( y >> 3 ) << 2 ) + ( ( z >> 3 ) << 4 )] = GetAverageColor( 2, x >> 3, y >> 3, z >> 3 );
					_volume1[( x >> 4 ) + ( ( y >> 4 ) << 1 ) + ( ( z >> 4 ) << 2 )] = GetAverageColor( 1, x >> 4, y >> 4, z >> 4 );
					_volume0[0]                                                      = GetAverageColor( 0, x >> 5, y >> 5, z >> 5 );
				}

				/*
				if ( value.color == 0 ) {
					_distMap0[0] = 0;
					_distMap1[0] &= ( byte ) ~( 1 << ( ( ( x >> 4 ) & 1 ) + ( ( ( y >> 4 ) & 1 ) << 1 ) + ( ( ( z >> 4 ) & 1 ) << 2 ) ) );
					_distMap2[( ( ( x >> 4 ) & 1 ) + ( ( y >> 3 ) & 2 ) + ( ( z >> 2 ) & 4 ) )] &= ( byte ) ~( 1 << ( ( ( x >> 3 ) & 1 ) + ( ( ( y >> 3 ) & 1 ) << 1 ) + ( ( ( z >> 3 ) & 1 ) << 2 ) ) );
					_distMap3[( ( ( x >> 3 ) & 2 ) + ( ( y >> 1 ) & 8 ) + ( ( z << 1 ) & 32 ) )] &= ( byte ) ~( 1 << ( ( ( x >> 2 ) & 1 ) + ( ( ( y >> 2 ) & 1 ) << 1 ) + ( ( ( z >> 2 ) & 1 ) << 2 ) ) );
					_distMap4[( ( ( x >> 2 ) & 4 ) + ( ( y << 1 ) & 32 ) + ( ( z << 4 ) & 128 ) )] &= ( byte ) ~( 1 << ( ( ( x >> 1 ) & 1 ) + ( ( ( y >> 1 ) & 1 ) << 1 ) + ( ( ( z >> 1 ) & 1 ) << 2 ) ) );
				}*/
			}
		}

		public bool IsEmpty( int x, int y, int z, int lvl ) {
			return lvl switch {
				0 => ( _volume0[0] & 1 ) == 0,
				1 => ( _volume1[0] & ( 1 << ( ( ( x >> 4 ) & 1 ) + ( ( ( y >> 4 ) & 1 ) << 1 ) + ( ( ( z >> 4 ) & 1 ) << 2 ) ) ) ) == 0,
				2 => ( _volume2[( ( x >> 4 ) & 1 ) + ( ( y >> 3 ) & 2 ) + ( ( z >> 2 ) & 4 )] & ( byte ) ( 1 << ( ( ( x >> 3 ) & 1 ) + ( ( ( y >> 3 ) & 1 ) << 1 ) + ( ( ( z >> 3 ) & 1 ) << 2 ) ) ) ) == 0,
				3 => ( _volume3[( ( x >> 3 ) & 3 ) + ( ( y >> 1 ) & 12 ) + ( ( z << 1 ) & 48 )] & ( byte ) ( 1 << ( ( ( x >> 2 ) & 1 ) + ( ( ( y >> 2 ) & 1 ) << 1 ) + ( ( ( z >> 2 ) & 1 ) << 2 ) ) ) ) == 0,
				4 => ( _volume4[( ( x >> 2 ) & 7 ) + ( ( y << 1 ) & 56 ) + ( ( z << 4 ) & 448 )] & ( byte ) ( 1 << ( ( ( x >> 1 ) & 1 ) + ( ( ( y >> 1 ) & 1 ) << 1 ) + ( ( ( z >> 1 ) & 1 ) << 2 ) ) ) ) == 0,
				5 => _voxels[x + ( y << ChunkLengthShift ) + ( z << ( ChunkLengthShift + ChunkLengthShift ) )].color == 0,
				_ => false
			};
		}

		public Voxel GetVoxel( int x, int y, int z ) { //inline
			return _voxels[x + ChunkLength * y + ChunkLength * ChunkLength * z];
		}

		public void SetVoxel( int x, int y, int z, byte color ) { //inline
			this[x, y, z] = new Voxel( color );
		}

		public void SetVoxelInit( int x, int y, int z, byte color ) { //inline
			this[x, y, z] = new Voxel( color );
		}
	}
}