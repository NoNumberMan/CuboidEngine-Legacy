using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Mathematics;

namespace CuboidEngine {
	internal sealed class World {
		public const int WorldSize         = 65536;
		public const int WorldCenterOffset = ( int ) WorldSize / 2;

		private readonly List<Chunk>     _chunks = new List<Chunk>();
		private readonly IWorldGenerator _worldGenerator;

		public           Camera                          Camera { get; } = new Camera();
		private readonly SortedList<ulong, ChunkMapData> _map = new SortedList<ulong, ChunkMapData>();

		public World( IWorldGenerator worldGenerator ) {
			_worldGenerator = worldGenerator;

			int idx = 0;
			for ( int x = -8; x <= 8; ++x )
			for ( int y = 0; y < 8; ++y )
			for ( int z = -8; z <= 8; ++z, ++idx ) {
				ulong position = ( ulong ) ( x + WorldCenterOffset ) + ( ulong ) WorldSize * ( ulong ) ( y + WorldCenterOffset ) + ( ulong ) WorldSize * ( ulong ) WorldSize * ( ulong ) ( z + WorldCenterOffset );

				Chunk chunk = new Chunk();
				_worldGenerator.Generate( chunk, x, y, z );
				_chunks.Add( chunk );
				_map.Add( position, new ChunkMapData( position, ( ulong ) idx ) );
			}

			Camera.SetPosition( 0.0f, 128.0f, 0.0f );
		}

		public void Update() {
			Camera.Update();
		}

		public void Prepare() {
			bool anyDirty = false;
			for ( int i = 0; i < _chunks.Count; ++i )
				if ( _chunks[i].IsDirty ) {
					anyDirty = true;
					_chunks[i].UpdateVolumes();
					RenderManager.PrepareChunk( _chunks[i], Chunk.ChunkVoxelCount * i );
					_chunks[i].IsDirty = false;
				}

			if ( anyDirty ) {
				ulong[] data = new ulong[_map.Values.Count * 2];
				for ( int i = 0; i < _map.Keys.Count; ++i ) {
					data[2 * i + 0] = _map[_map.Keys[i]].position;
					data[2 * i + 1] = _map[_map.Keys[i]].index;
				}

				CEngine.EnqueueWriteBuffer( OpenCLObjects.MapBuffer, 16, data );
				CEngine.EnqueueWriteBuffer( OpenCLObjects.MapBuffer, 0, new[] {( ulong ) _chunks.Count, ( ulong ) Chunk.ChunkLengthBits} );
			}

			float[] cam = new float[] {Camera.Pos.X, Camera.Pos.Y, Camera.Pos.Z, Camera.GetDirection().X, Camera.GetDirection().Y, Camera.GetDirection().Z, Camera.Size.X, Camera.Size.Y};

			CEngine.EnqueueWriteBuffer<float>( OpenCLObjects.CameraBuffer, 0, cam );
		}

		public bool IsInRange( int cx, int cy, int cz ) {
			return cx >= 0 && cx < 1 && cy >= 0 && cy < 1 && cz >= 1 && cz < 2;
		}

		public Chunk? GetChunk( int cx, int cy, int cz ) {
			return IsInRange( cx, cy, cz ) ? _chunks[cx + 1 * cy + 1 * 1 * ( cz - 1 )] : null;
		}
	}
}