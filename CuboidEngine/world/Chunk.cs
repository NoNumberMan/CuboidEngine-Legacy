using System;
using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.ES11;

namespace CuboidEngine {
	public class Chunk {
		public const int ChunkLength      = 32;
		public const int ChunkLengthShift = 5;
		public const int DistMapLength    = ChunkLength >> 1;
		public const int ChunkSize        = ChunkLength * ChunkLength * ChunkLength;

		public bool IsDirty { get; set; }

		private readonly Voxel[] _voxels;
		private readonly Voxel[] _volume0;
		private readonly Voxel[] _volume1;
		private readonly Voxel[] _volume2;
		private readonly Voxel[] _volume3;
		private readonly Voxel[] _volume4;

		internal Span<Voxel> Voxels => _voxels.AsSpan();
		internal Span<Voxel> Vol0   => _volume0.AsSpan();
		internal Span<Voxel> Vol1   => _volume1.AsSpan();
		internal Span<Voxel> Vol2   => _volume2.AsSpan();
		internal Span<Voxel> Vol3   => _volume3.AsSpan();
		internal Span<Voxel> Vol4   => _volume4.AsSpan();

		internal Chunk() {
			_voxels  = new Voxel[ChunkSize]; //32x32x32
			_volume0 = new Voxel[1];         //1x1x1
			_volume1 = new Voxel[8];         //2x2x2
			_volume2 = new Voxel[64];        //4x4x4
			_volume3 = new Voxel[512];       //8x8x8
			_volume4 = new Voxel[4096];      //16x16x16
		}

		public Voxel this[ int x, int y, int z ] {
			get => _voxels[x + ChunkLength * y + ChunkLength * ChunkLength * z];
			set {
				_voxels[x + ( y << 5 ) + ( z << 10 )] = value;
				IsDirty                               = true;
			}
		}

		public void UpdateVolumes() { //TODO parallel
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

		private Voxel GetColor( int lvl, int x, int y, int z ) {
			return lvl switch {
				0 => _volume0[x + y + z],
				1 => _volume1[x + ( y << 1 ) + ( z << 2 )],
				2 => _volume2[x + ( y << 2 ) + ( z << 4 )],
				3 => _volume3[x + ( y << 3 ) + ( z << 6 )],
				4 => _volume4[x + ( y << 4 ) + ( z << 8 )],
				5 => _voxels[x + ( y << 5 ) + ( z << 10 )],
				_ => throw new IndexOutOfRangeException()
			};
		}

		private Voxel GetAverageColor( int lvl, int x, int y, int z ) {
			int avgr = 0;
			int avgg = 0;
			int avgb = 0;
			int avga = 0;
			int avgl = 0;
			int aorl = 0;
			int t    = 0;
			for ( int sz = 2 * z; sz < 2 * z + 2; ++sz )
			for ( int sy = 2 * y; sy < 2 * y + 2; ++sy )
			for ( int sx = 2 * x; sx < 2 * x + 2; ++sx ) {
				Voxel v = GetColor( lvl + 1, sx, sy, sz );
				if ( v.Empty ) continue;
				avgr += ( v.color >> 5 ) & 7; //11 -> 3 + 3 + 3 / 8 -> 1 * 0.333f -> 0.99998 -> 0 -> 
				avgg += ( v.color >> 2 ) & 7;
				avgb += ( v.color >> 0 ) & 3;

				bool alpha = ( v.allum & 1 ) == 0;
				avga += alpha ? ( v.allum >> 1 ) & 127 : 0; //11+11+11.. / 8 -> 11 = 3
				avgl += alpha ? 0 : ( v.allum >> 1 ) & 127; //11+11+11.. / 8 -> 11 = 3
				aorl += alpha ? -1 : +1;
				++t;
			}

			return t == 0
				? new Voxel()
				: new Voxel( ( byte ) ( ( ( avgr / t ) << 5 ) + ( ( avgg / t ) << 2 ) + ( ( avgb / t ) << 0 ) ), ( byte ) ( aorl > 0 ? 1 + ( ( avgl / t ) << 1 ) : 0 + ( ( avga / t ) << 1 ) ) );
		}

		public bool IsEmpty( int lvl, int x, int y, int z ) {
			return lvl switch {
				0 => _volume0[x + y + z].Empty,
				1 => _volume1[x + ( y << 1 ) + ( z << 2 )].Empty,
				2 => _volume2[x + ( y << 2 ) + ( z << 4 )].Empty,
				3 => _volume3[x + ( y << 3 ) + ( z << 6 )].Empty,
				4 => _volume4[x + ( y << 4 ) + ( z << 8 )].Empty,
				5 => _voxels[x + ( y << 5 ) + ( z << 10 )].Empty,
				_ => false
			};
		}
	}
}