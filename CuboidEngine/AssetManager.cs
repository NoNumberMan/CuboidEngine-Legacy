using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CuboidEngine
{
	internal class AssetManager<T>
	{
		private readonly List<T>    _assets = new List<T>();
		private readonly Stack<int> _nextId = new Stack<int>();

		public ID AddAsset( T t ) {
			if ( _nextId.TryPop( out int id ) ) {
				_assets[id] = t;
				return new ID( id );
			}
			else {
				_assets.Add( t );
				return new ID( _assets.Count - 1 );
			}
		}

		public void RemoveAsset( ID id ) {
			_nextId.Push( id.Value );
			id.SetInvalid();
		}

		public T this[ID id] => _assets[id.Value];
	}
}