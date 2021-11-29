using System.Runtime.InteropServices;

namespace CuboidEngine {
	[StructLayout( LayoutKind.Sequential, Pack = 1, Size = 2 )]
	public readonly struct Voxel {
		public readonly byte color; //3r, 3g, 2b 8bit color
		public readonly byte allum; //1 alpha or luminance, 7 either alpha or lum

		public bool Empty => allum == 0;

		internal static unsafe int ByteSize => sizeof( Voxel );

		public Voxel( byte color, byte allum ) {
			this.color = color;
			this.allum = allum;
		}

		public Voxel( byte color, bool isAlpha, byte allum ) {
			this.color =  color;
			this.allum =  ( byte ) ( ( allum << 1 ) & 255 );
			this.allum |= ( byte ) ( isAlpha ? 0b0 : 0b1 );
		}
	}
}