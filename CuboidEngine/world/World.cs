using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;

namespace CuboidEngine {
	internal sealed class World {
		private readonly Chunk[] _chunks = new Chunk[1 * 1 * 1];

		public Camera Camera { get; } = new Camera();

		public World() {
			for ( int i = 0; i < 1 * 1 * 1; ++i )
				_chunks[i] = new Chunk();

			for ( byte k = 0; k < Chunk.ChunkLength; ++k )
			for ( byte i = 0; i < Chunk.ChunkLength; ++i )
			for ( byte j = 0; j < new Random().Next( 0, Chunk.ChunkLength ); ++j ) {
				//for ( byte j = 0; j < Chunk.ChunkLength; ++j ) {
				byte color = 3;
				color += ( byte ) ( ( uint ) new Random().Next( 0, 63 ) << 2 );
				_chunks[0].SetVoxel( i, j, k, color );
			}
		}


		public void Update() {
			Camera.Update();
		}

		public void Prepare() {
			//RenderManager.RayTracer( this, Camera, Shaders.ScreenShaderId );
			for ( int i = 0; i < _chunks.Length; ++i ) //if chunk is dirty (has changed)
				RenderManager.PrepareChunk( _chunks[i] );

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