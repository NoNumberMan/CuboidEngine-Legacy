using System;
using System.Linq;
using CuboidEngine.Libs;

namespace CuboidEngine {
	public static class WorldGenerators {
		public static IWorldGenerator FlatWorld        => new FlatWorldGenerator();
		public static IWorldGenerator RandomWorld      => new RandomWorldGenerator();
		public static IWorldGenerator TinyIslandsWorld => new TinyIslandsWorldGenerator();
		public static IWorldGenerator RealisticWorld   => new RealisticWorldGenerator();

		private sealed class FlatWorldGenerator : IWorldGenerator {
			private readonly Random _random;

			public FlatWorldGenerator() {
				_random = new Random();
			}

			public void Generate( Chunk chunk, int cx, int cy, int cz ) {
				for ( int z = 0; z < Chunk.ChunkLength; ++z )
				for ( int x = 0; x < Chunk.ChunkLength; ++x ) {
					chunk[x, 0, z] = new Voxel( 0b10010010, true, 127 );
					chunk[x, 1, z] = new Voxel( 0b01101000, true, 127 );
					chunk[x, 2, z] = new Voxel( 0b01101000, true, 127 );
					chunk[x, 3, z] = new Voxel( 0b01101000, true, 127 );

					int green = _random.Next( 3, 7 );

					chunk[x, 4, z] = new Voxel( ( byte ) ( green << 2 ), true, 127 );
				}
			}
		}

		private sealed class RandomWorldGenerator : IWorldGenerator {
			private readonly Random _random;

			public RandomWorldGenerator() {
				_random = new Random();
			}

			public void Generate( Chunk chunk, int cx, int cy, int cz ) {
				for ( byte z = 0; z < Chunk.ChunkLength; ++z )
				for ( byte x = 0; x < Chunk.ChunkLength; ++x )
				for ( byte y = 0; y < _random.Next( 1, Chunk.ChunkLength ); ++y ) {
					byte color = ( byte ) _random.Next( 0, 255 );
					chunk[x, y, z] = new Voxel( color, true, 127 );
				}
			}
		}

		private sealed class TinyIslandsWorldGenerator : IWorldGenerator {
			private readonly        Random        _random;
			private readonly        FastNoiseLite _noise;
			private static readonly int[]         _octaves       = new int[] {1, 4, 8, 32};
			private static readonly float[]       _strengths     = new float[] {0.0f, 0.0f, 0.0f, 1.0f};
			private static readonly float         _totalStrength = _strengths.Sum();

			private static readonly byte[] _stone = new byte[] {0b10010010};
			private static readonly byte[] _water = new byte[] {0b00101011};
			private static readonly byte[] _dirt  = new byte[] {0b01101000};
			private static readonly byte[] _grass = new byte[] {0b00111001};
			private static readonly byte[] _snow  = new byte[] {0b11111111};

			public TinyIslandsWorldGenerator() {
				_random = new Random();
				_noise  = new FastNoiseLite( 0 );
				_noise.SetNoiseType( FastNoiseLite.NoiseType.Perlin );
				_noise.SetFractalType( FastNoiseLite.FractalType.PingPong );
			}

			public void Generate( Chunk chunk, int cx, int cy, int cz ) {
				const int waterLevel = 64;

				for ( int z = 0; z < Chunk.ChunkLength; ++z )
				for ( int x = 0; x < Chunk.ChunkLength; ++x ) {
					int height = ( int ) ( 16.0f + 128.0f * ( 0.5f + 0.5f * _noise.GetNoise( x + Chunk.ChunkLength * cx, z + Chunk.ChunkLength * cz ) ) );
					for ( int y = 0; y < Chunk.ChunkLength; ++y ) {
						int ry = y + cy * Chunk.ChunkLength;

						byte color;
						if ( ry > height ) {
							if ( ry > waterLevel ) break;
							color = _water[_random.Next( 0, _water.Length )];
						}
						else {
							if ( ry == height )
								color = height < 96 ? _grass[_random.Next( 0, _grass.Length )] : _snow[_random.Next( 0, _snow.Length )];
							else if ( ry > height - 5 )
								color = _dirt[_random.Next( 0, _dirt.Length )];
							else
								color = _stone[_random.Next( 0, _stone.Length )];
						}

						chunk[x, y, z] = new Voxel( color, true, 127 );
					}
				}
			}
		}

		private sealed class RealisticWorldGenerator : IWorldGenerator {
			private readonly        Random        _random;
			private readonly        FastNoiseLite _noise;
			private static readonly int[]         _octaves       = new int[] {1, 4, 8, 32};
			private static readonly float[]       _strengths     = new float[] {0.0f, 0.0f, 0.0f, 1.0f};
			private static readonly float         _totalStrength = _strengths.Sum();

			private static readonly byte[] _stone = new byte[] {0b10010010};
			private static readonly byte[] _water = new byte[] {0b00101011};
			private static readonly byte[] _dirt  = new byte[] {0b01101000};
			private static readonly byte[] _grass = new byte[] {0b00111001};
			private static readonly byte[] _snow  = new byte[] {0b11111111};

			public RealisticWorldGenerator() {
				_random = new Random();
				_noise  = new FastNoiseLite( 0 );
				_noise.SetNoiseType( FastNoiseLite.NoiseType.Perlin );
				_noise.SetFractalType( FastNoiseLite.FractalType.FBm );
			}

			public void Generate( Chunk chunk, int cx, int cy, int cz ) {
				const int waterLevel = 2 * Chunk.ChunkLength;
				const int avgHeight  = 4 * Chunk.ChunkLength;

				System.Threading.Tasks.Parallel.For( 0, Chunk.ChunkLength, ( int z ) => {
					for ( int x = 0; x < Chunk.ChunkLength; ++x ) {
						int height = ( int ) ( avgHeight * ( 0.5f + 0.5f * _noise.GetNoise( x + Chunk.ChunkLength * cx, z + Chunk.ChunkLength * cz ) ) );
						for ( int y = 0; y < Chunk.ChunkLength; ++y ) {
							int ry = y + cy * Chunk.ChunkLength;

							byte color;
							if ( ry > height ) {
								if ( ry > waterLevel ) break;
								color = _water[_random.Next( 0, _water.Length )];
							}
							else {
								if ( ry == height )
									color = height < 3 * Chunk.ChunkLength ? _grass[_random.Next( 0, _grass.Length )] : _snow[_random.Next( 0, _snow.Length )];
								else if ( ry > height - 5 )
									color = _dirt[_random.Next( 0, _dirt.Length )];
								else
									color = _stone[_random.Next( 0, _stone.Length )];
							}

							chunk[x, y, z] = new Voxel( color, true, 127 );
						}
					}

					chunk[0, 80, 0] = new Voxel( 255, false, 127 );
					chunk[1, 80, 0] = new Voxel( 255, false, 127 );
					chunk[1, 80, 1] = new Voxel( 255, false, 127 );
					chunk[0, 80, 1] = new Voxel( 255, false, 127 );
				} );
			}
		}
	}
}