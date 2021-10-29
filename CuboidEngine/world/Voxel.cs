namespace CuboidEngine
{
	internal struct Voxel
	{
		public byte color; //2r,2g,2b,2a

		public int Alpha => color & 3;

		public Voxel( byte color ) {
			this.color = color;
		}
	}
}