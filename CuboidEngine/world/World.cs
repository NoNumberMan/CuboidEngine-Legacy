﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using OpenTK.Mathematics;

namespace CuboidEngine {
	internal sealed class World {
		public const int WorldSize         = 16;
		public const int WorldCenterOffset = ( int ) WorldSize >> 1;

		private          int     _currentChunkNumber = 0;
		private readonly Chunk[] _chunks             = new Chunk[OpenCLObjects.ChunkNumber];
		private readonly int[]   _chunksArrayBuffer0 = new int[WorldSize * WorldSize * WorldSize];
		private readonly int[]   _chunksArrayBuffer1 = new int[WorldSize * WorldSize * WorldSize];
		private          int[]   _chunksArray;

		private readonly Stack<int> _replacableChunks = new Stack<int>();

		private int[] _chunksArrayNext;

		private readonly IWorldGenerator _worldGenerator;

		public Camera   Camera          { get; } = new Camera();
		public Vector3i LastCamChunkPos { get; private set; }

		public World( IWorldGenerator worldGenerator ) {
			_worldGenerator = worldGenerator;

			Chunk chunk = new Chunk(); //zero chunk
			_chunks[_currentChunkNumber++] = chunk;
			Array.Fill( _chunksArrayBuffer0, -1 );
			Array.Fill( _chunksArrayBuffer1, -1 );
			_chunksArray     = _chunksArrayBuffer0;
			_chunksArrayNext = _chunksArrayBuffer1;

			Camera.SetPosition( 0.0f, 128.0f, 0.0f );
			LastCamChunkPos = new Vector3i( 0, 4, 0 );
		}

		public void Update() {
			Camera.Update();
		}

		public void Prepare() {
			uint[] requestBuffer = new uint[OpenCLObjects.RequestChunkBufferLength];
			CEngine.CLWaitForFinish();

			float[] cam = new float[] {Camera.Pos.X, Camera.Pos.Y, Camera.Pos.Z, Camera.GetDirection().X, Camera.GetDirection().Y, Camera.GetDirection().Z, Camera.Size.X, Camera.Size.Y};
			CEngine.CLEnqueueWriteBuffer( OpenCLObjects.CameraBuffer, 0, cam );
			RenderManager.RequestChunks();

			CEngine.CLEnqueueReadBuffer( OpenCLObjects.RequestChunkBuffer, requestBuffer );
			CEngine.CLWaitForFinish();

			int      ccx            = ( int ) MathF.Floor( Camera.Pos.X / 32.0f ); //TODO, store cam pos using chunk + offset
			int      ccy            = ( int ) MathF.Floor( Camera.Pos.Y / 32.0f );
			int      ccz            = ( int ) MathF.Floor( Camera.Pos.Z / 32.0f );
			Vector3i newCamChunkPos = new Vector3i( ccx, ccy, ccz );
			Vector3i diff           = newCamChunkPos - LastCamChunkPos;

			if ( diff.ManhattanLength > 0 ) { //overlap
				int a = 0;

				for ( int z = Math.Max( 0, diff.Z ); z < Math.Min( WorldSize, WorldSize + diff.Z ); ++z )
				for ( int y = Math.Max( 0, diff.Y ); y < Math.Min( WorldSize, WorldSize + diff.Y ); ++y )
				for ( int x = Math.Max( 0, diff.X ); x < Math.Min( WorldSize, WorldSize + diff.X ); ++x ) {
					int i = x + WorldSize * y + WorldSize * WorldSize * z;
					int j = x - diff.X + WorldSize * ( y - diff.Y ) + WorldSize * WorldSize * ( z - diff.Z );
					_chunksArrayNext[j] = _chunksArray[i];
				}

				//create new area
				if ( diff.X != 0 ) {
					int x = diff.X > 0 ? WorldSize - 1 : 0;
					for ( int z = 0; z < WorldSize; ++z )
					for ( int y = 0; y < WorldSize; ++y ) {
						int j = x + WorldSize * y + WorldSize * WorldSize * z;
						_chunksArrayNext[j] = -1;
					}
				}

				if ( diff.Y != 0 ) {
					int y = diff.Y > 0 ? WorldSize - 1 : 0;
					for ( int z = 0; z < WorldSize; ++z )
					for ( int x = 0; x < WorldSize; ++x ) {
						int j = x + WorldSize * y + WorldSize * WorldSize * z;
						_chunksArrayNext[j] = -1;
					}
				}

				if ( diff.Z != 0 ) {
					int z = diff.Z > 0 ? WorldSize - 1 : 0;
					for ( int y = 0; y < WorldSize; ++y )
					for ( int x = 0; x < WorldSize; ++x ) {
						int j = x + WorldSize * y + WorldSize * WorldSize * z;
						_chunksArrayNext[j] = -1;
					}
				}

				//remove old area
				if ( diff.X != 0 ) {
					int x = diff.X > 0 ? 0 : WorldSize - 1;
					for ( int z = 0; z < WorldSize; ++z )
					for ( int y = 0; y < WorldSize; ++y ) {
						int i = x + WorldSize * y + WorldSize * WorldSize * z;
						if ( _chunksArray[i] > 0 ) _replacableChunks.Push( _chunksArray[i] );
					}
				}

				if ( diff.Y != 0 ) {
					int y = diff.Y > 0 ? 0 : WorldSize - 1;
					for ( int z = 0; z < WorldSize; ++z )
					for ( int x = 0; x < WorldSize; ++x ) {
						int i = x + WorldSize * y + WorldSize * WorldSize * z;
						if ( _chunksArray[i] > 0 ) _replacableChunks.Push( _chunksArray[i] );
					}
				}

				if ( diff.Z != 0 ) {
					int z = diff.Z > 0 ? 0 : WorldSize - 1;
					for ( int y = 0; y < WorldSize; ++y )
					for ( int x = 0; x < WorldSize; ++x ) {
						int i = x + WorldSize * y + WorldSize * WorldSize * z;
						if ( _chunksArray[i] > 0 ) _replacableChunks.Push( _chunksArray[i] );
					}
				}

				( _chunksArray, _chunksArrayNext ) = ( _chunksArrayNext, _chunksArray ); //swap
			}

			for ( int r = 0; r < OpenCLObjects.RequestChunkBufferLength; ++r ) {
				uint position = requestBuffer[r] & 0xffffff;
				int  rcx      = ( int ) ( position % WorldSize ) - WorldCenterOffset;
				int  rcy      = ( int ) ( position / WorldSize % WorldSize ) - WorldCenterOffset;
				int  rcz      = ( int ) ( position / ( WorldSize * WorldSize ) ) - WorldCenterOffset;

				if ( ( ( requestBuffer[r] >> 24 ) & 255 ) != 0 && _chunksArray[position] == -1 ) {
					Chunk chunk = new Chunk();
					_worldGenerator.Generate( chunk, ccx + rcx, ccy + rcy, ccz + rcz );
					chunk.UpdateVolumes();

					if ( chunk.IsEmpty( 0, 0, 0, 0 ) ) {
						_chunksArray[position] = 0;
					}
					else {
						int chunkIdx;
						if ( !_replacableChunks.TryPop( out chunkIdx ) ) chunkIdx = _currentChunkNumber++;
						_chunksArray[position] = chunkIdx;
						_chunks[chunkIdx]      = chunk;
						RenderManager.UploadChunk( chunk, chunkIdx );
					}
				}
			}

			CEngine.CLEnqueueWriteBuffer( OpenCLObjects.MapBuffer, 0, _chunksArray );
			LastCamChunkPos = newCamChunkPos;
		}

		public bool IsInRange( int cx, int cy, int cz ) { //TODO
			return cx >= 0 && cx < 1 && cy >= 0 && cy < 1 && cz >= 1 && cz < 2;
		}

		public Chunk? GetChunk( int cx, int cy, int cz ) { //TODO FIX PLZ LOL
			return IsInRange( cx, cy, cz ) ? _chunks[cx + 1 * cy + 1 * 1 * ( cz - 1 )] : null;
		}
	}
}