using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;

namespace CuboidEngine {
	internal sealed class World {
		private readonly List<Chunk>     _chunks = new List<Chunk>();
		private readonly IWorldGenerator _worldGenerator;

		public           Camera                   Camera { get; } = new Camera();
		private readonly SortedList<ulong, ulong> _map = new SortedList<ulong, ulong>();

		public World( IWorldGenerator worldGenerator ) {
			_worldGenerator = worldGenerator;

			int idx = 0;
			for ( int z = -6; z <= 6; ++z )
			for ( int y = 0; y < 1; ++y )
			for ( int x = -6; x <= 6; ++x, ++idx ) {
				Chunk chunk = new Chunk();
				_worldGenerator.Generate( chunk, x, y, z );
				_chunks.Add( chunk );
				_map.Add( ( ulong ) idx, ( ulong ) ( x + 32768 ) + 65536ul * ( ulong ) ( y + 32768 ) + 65536ul * 65536ul * ( ulong ) ( z + 32768 ) );
			}
		}

		public void Update() {
			Camera.Update();
		}

		public void Prepare() {
			//RenderManager.RayTracer( this, Camera, Shaders.ScreenShaderId );
			for ( int i = 0; i < _chunks.Count; ++i )
				if ( _chunks[i].IsDirty ) {
					_chunks[i].UpdateVolumes();
					RenderManager.PrepareChunk( _chunks[i], 37449 * i );
					CEngine.EnqueueWriteBuffer( OpenCLObjects.MapBuffer, 2 * sizeof( ulong ) * ( i + 1 ), new ulong[] {_map[( ulong ) i], ( ulong ) i}.AsSpan() );
					_chunks[i].IsDirty = false;
				}

			CEngine.EnqueueWriteBuffer( OpenCLObjects.MapBuffer, 0, new ulong[] {( ulong ) _chunks.Count, 0ul}.AsSpan() );

			float[] cam = new float[] {
				Camera.GetPosition().X, Camera.GetPosition().Y, Camera.GetPosition().Z, Camera.GetDirection().X, Camera.GetDirection().Y, Camera.GetDirection().Z, 1920.0f, 1080.0f //TODO store screen size in camera
			};

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