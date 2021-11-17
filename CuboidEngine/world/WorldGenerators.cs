using System;

namespace CuboidEngine {
	public static class WorldGenerators {
		public static IWorldGenerator FlatWorld      => new FlatWorldGenerator();
		public static IWorldGenerator RandomWorld    => new RandomWorldGenerator();
		public static IWorldGenerator RealisticWorld => new FlatWorldGenerator();

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
	}
}