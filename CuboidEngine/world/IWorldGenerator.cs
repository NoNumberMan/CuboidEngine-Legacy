namespace CuboidEngine {
	public interface IWorldGenerator {
		public void Generate( Chunk chunk, int cx, int cy, int cz );
	}
}