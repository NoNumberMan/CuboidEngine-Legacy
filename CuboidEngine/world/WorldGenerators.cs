namespace CuboidEngine
{
	public class WorldGenerators
	{
		public static IWorldGenerator FlatWorld => new FlatWorldGenerator();

		private class FlatWorldGenerator : IWorldGenerator {
			//implement
			/// <inheritdoc />
			public void Generate( Voxel[] chunk, int x, int y ) { }
		}
	}
}