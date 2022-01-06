using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using OpenTK.Mathematics;

namespace CuboidEngine {
	internal sealed class World {
		public const int WorldSize         = 65536;
		public const int WorldCenterOffset = ( int ) WorldSize >> 1;

		private readonly List<Chunk>     _chunks;
		private readonly IWorldGenerator _worldGenerator;

		public           Camera                 Camera { get; } = new Camera();
		private readonly SortedList<ulong, int> _map = new SortedList<ulong, int>();

		public World( IWorldGenerator worldGenerator ) {
			_worldGenerator = worldGenerator;
			_chunks         = new List<Chunk>( ( int ) OpenCLObjects.TotalChunkNumber );

			Chunk chunk = new Chunk(); //zero chunk
			_chunks.Add( chunk );
			_map.Add( 0, 0 );

			Camera.SetPosition( 0.0f, 128.0f, 0.0f );
		}

		public void Update() {
			Camera.Update();
		}

		public void Prepare() {
			ulong[] requestBuffer  = new ulong[OpenCLObjects.RequestChunkBufferLength];
			uint[]  distanceBuffer = new uint[_map.Count];
			CEngine.CLWaitForFinish();
			CEngine.CLEnqueueReadBuffer( OpenCLObjects.RequestChunkBuffer, requestBuffer );
			CEngine.CLEnqueueReadBuffer( OpenCLObjects.DistanceBuffer, distanceBuffer );
			CEngine.CLWaitForFinish();
			CEngine.CLEnqueueFillBuffer( OpenCLObjects.RequestChunkBuffer, 0, OpenCLObjects.RequestChunkBufferLength * sizeof( ulong ), ( byte ) 255 );
			CEngine.CLEnqueueFillBuffer( OpenCLObjects.DistanceBuffer, 0, ( int ) OpenCLObjects.TotalMapChunkNumber * sizeof( uint ), ( byte ) 255 );
			//CEngine.CLEnqueueWriteBuffer( OpenCLObjects.RngBuffer, 0, OpenCLObjects.GenerateRng() );


			bool anyDirty = false;

			if ( requestBuffer[1] < ulong.MaxValue ) { //TODO expand utilization of requestbuffer to all 255 slots
				ulong position = ( ulong ) requestBuffer[1];
				if ( !_map.ContainsKey( position ) ) {
					int cx = ( int ) ( position % WorldSize ) - WorldCenterOffset;
					int cy = ( int ) ( position / WorldSize % WorldSize ) - WorldCenterOffset;
					int cz = ( int ) ( position / ( ( ulong ) WorldSize * ( ulong ) WorldSize ) ) - WorldCenterOffset;

					Chunk chunk      = new Chunk();
					int   chunkIndex = _chunks.Count;
					_worldGenerator.Generate( chunk, cx, cy, cz );
					chunk.UpdateVolumes();

					if ( _map.Count >= OpenCLObjects.TotalMapChunkNumber && CleanMap( distanceBuffer ) == 0 ) goto done;

					if ( chunk.IsEmpty( 0, 0, 0, 0 ) ) {
						_map.Add( position, 0 );
					}
					else {
						if ( chunkIndex >= OpenCLObjects.Lod0ChunkNumber ) { //cannot overflow map
							int empty = 0;
							for ( int i = 0; i < _map.Keys.Count; ++i )
								if ( distanceBuffer[i] == uint.MaxValue ) {
									if ( _map.Values[i] > 0 ) {
										chunkIndex = _map.Values[i];
										_map.RemoveAt( i );
										_map.Add( position, chunkIndex );
										_chunks[chunkIndex] = chunk;

										RenderManager.UploadChunk( chunk, chunkIndex );
										goto done;
									}
									else {
										++empty;
									}
								}

							if ( empty > 16 ) CleanMap( distanceBuffer );
						}

						_map.Add( position, chunkIndex );
						_chunks.Add( chunk );
						RenderManager.UploadChunk( chunk, chunkIndex );
					}

					done:
					anyDirty = true;
				}
			}

			if ( anyDirty ) {
				ulong[] data = new ulong[_map.Keys.Count * 2];
				for ( int i = 0; i < _map.Keys.Count; ++i ) {
					data[2 * i + 0] = _map.Keys[i];
					data[2 * i + 1] = ( ulong ) _map.Values[i]; // _map[_map.Keys[i]];
				}

				CEngine.CLEnqueueWriteBuffer( OpenCLObjects.MapBuffer, 16, data );
				CEngine.CLEnqueueWriteBuffer( OpenCLObjects.MapBuffer, 0, new[] {( ulong ) _map.Keys.Count, ( ulong ) Chunk.ChunkLengthBits} );
			}

			float[] cam = new float[] {Camera.Pos.X, Camera.Pos.Y, Camera.Pos.Z, Camera.GetDirection().X, Camera.GetDirection().Y, Camera.GetDirection().Z, Camera.Size.X, Camera.Size.Y};
			CEngine.CLEnqueueWriteBuffer<float>( OpenCLObjects.CameraBuffer, 0, cam );
		}

		private int CleanMap( uint[] distanceBuffer ) {
			int removed = 0;
			for ( int i = distanceBuffer.Length - 1; i >= 0; --i )
				if ( distanceBuffer[i] == uint.MaxValue ) {
					_map.RemoveAt( i );
					++removed;
				}

			return removed;
		}

		public bool IsInRange( int cx, int cy, int cz ) { //TODO
			return cx >= 0 && cx < 1 && cy >= 0 && cy < 1 && cz >= 1 && cz < 2;
		}

		public Chunk? GetChunk( int cx, int cy, int cz ) { //TODO FIX PLZ LOL
			return IsInRange( cx, cy, cz ) ? _chunks[cx + 1 * cy + 1 * 1 * ( cz - 1 )] : null;
		}
	}
}