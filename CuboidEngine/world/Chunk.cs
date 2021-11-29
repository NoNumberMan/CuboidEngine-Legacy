using System;

namespace CuboidEngine {
	public class Chunk {
		public const int ChunkLengthBits = 5;
		public const int ChunkLength     = 1 << ChunkLengthBits;
		public const int ChunkSize       = ChunkLength * ChunkLength * ChunkLength;

		public static readonly int ChunkVoxelCount;

		static Chunk() {
			for ( int i = 0; i < ChunkLengthBits + 1; ++i )
				ChunkVoxelCount += ( 1 << i ) * ( 1 << i ) * ( 1 << i );
		}

		public bool IsDirty { get; set; }

		private readonly Voxel[]   _voxels;
		private readonly Voxel[][] _superVoxels;

		internal Span<Voxel> Voxels => _voxels.AsSpan();

		internal Span<Voxel> Vol( int idx ) {
			return _superVoxels[idx];
		}

		internal Chunk() {
			_voxels      = new Voxel[ChunkSize];
			_superVoxels = new Voxel[ChunkLengthBits][];
			for ( int i = 0; i < ChunkLengthBits; ++i ) _superVoxels[i] = new Voxel[( 1 << i ) * ( 1 << i ) * ( 1 << i )];
		}

		public Voxel this[ int x, int y, int z ] {
			get => _voxels[x + ChunkLength * y + ChunkLength * ChunkLength * z];
			set {
				_voxels[x + ( y << ChunkLengthBits ) + ( z << ( 2 * ChunkLengthBits ) )] = value;
				IsDirty                                                                  = true;
			}
		}

		public void UpdateVolumes() {
			for ( int i = ChunkLengthBits - 1; i >= 0; --i ) {
				int superIdx = i;
				int size     = 1 << i;
				System.Threading.Tasks.Parallel.For( 0, size, ( int iz ) => {
					for ( int iy = 0; iy < size; ++iy )
					for ( int ix = 0; ix < size; ++ix )
						_superVoxels[superIdx][ix + size * iy + size * size * iz] = GetAverageColor( superIdx, ix, iy, iz );
				} );
			}
		}

		private Voxel GetColor( int lvl, int x, int y, int z ) {
			return lvl == ChunkLengthBits ? _voxels[x + ( y << ChunkLengthBits ) + ( z << ( 2 * ChunkLengthBits ) )] : _superVoxels[lvl][x + ( y << lvl ) + ( z << ( 2 * lvl ) )];
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
			return lvl == ChunkLengthBits ? _voxels[x + ( y << ChunkLengthBits ) + ( z << ( 2 * ChunkLengthBits ) )].Empty : _superVoxels[lvl][x + ( y << lvl ) + ( z << ( 2 * lvl ) )].Empty;
		}
	}
}