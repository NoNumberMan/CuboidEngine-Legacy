using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CuboidEngine
{
#if DEBUG
	public class ID
	{
		private int _id;

		internal int Value {
			get {
				Debug.Assert( _id >= 0 );
				return _id;
			}
		}

		internal ID( int id ) {
			_id = id;
		}

		internal void SetInvalid() => _id = -1;
	}
#else
	[StructLayout( LayoutKind.Sequential, Pack = 4 )]
	public struct ID
	{
		private int _id;

		internal int Value => _id;

		internal ID( int id ) {
			_id = id;
		}

		[Conditional("DEBUG")]
		internal void SetInvalid() {
			int a = 0;
		}
	}
#endif
}