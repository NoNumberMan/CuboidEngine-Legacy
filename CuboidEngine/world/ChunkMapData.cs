using System.Runtime.InteropServices;

namespace CuboidEngine {
	[StructLayout( LayoutKind.Explicit, Pack = 8, Size = 16 )]
	public readonly struct ChunkMapData {
		internal static unsafe int ByteSize => sizeof( ChunkMapData );

		[FieldOffset( 0 )] public readonly ulong position;
		[FieldOffset( 8 )] public readonly ulong index;

		public ChunkMapData( ulong position, ulong index ) {
			this.position = position;
			this.index    = index;
		}
	}
}