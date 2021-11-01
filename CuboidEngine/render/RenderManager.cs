using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace CuboidEngine {
	internal static class RenderManager {
		private static readonly AssetManager<RenderObject> _renderObjects = new AssetManager<RenderObject>();


		public static void PrepareChunk( Chunk chunk ) {
			CEngine.EnqueueWriteBuffer( OpenCLObjects.VoxelBuffer, 0, chunk.Voxels );
		}

		public static void RenderRayMarcherResult() {
			float[] pixels = new float[1920 * 1080 * 3];
			CEngine.EnqueueReadBuffer( OpenCLObjects.PixelBuffer, pixels );
			CEngine.WaitForFinish();

			int textureId = GL.GenTexture();
			GL.BindTexture( TextureTarget.Texture2D, textureId );

			//These are temporary. Will set the tex params properly when needed
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, ( int ) TextureWrapMode.Repeat );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, ( int ) TextureWrapMode.Repeat );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ( int ) TextureMinFilter.Linear );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ( int ) TextureMagFilter.Linear );

			GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 1920, 1080,
				0, PixelFormat.Rgb, PixelType.UnsignedByte, pixels );

			//6. create full screen quad
			int vao       = GL.GenVertexArray();
			int vertices  = GL.GenBuffer();
			int texcoords = GL.GenBuffer();
			int indices   = GL.GenBuffer();

			float[] vertexArray   = new[] {0.0f, 0.0f, 1280.0f, 0.0f, 1280.0f, 720.0f, 0.0f, 720.0f};
			float[] texcoordArray = new[] {0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f};
			byte[]  indexArray    = new byte[] {0, 1, 2, 2, 3, 0};

			GL.BindVertexArray( vao );
			GL.BindBuffer( BufferTarget.ArrayBuffer, vertices );
			GL.BufferData( BufferTarget.ArrayBuffer, vertexArray.Length * sizeof( float ), vertexArray, BufferUsageHint.StaticCopy );
			GL.EnableVertexAttribArray( 0 );
			GL.VertexAttribPointer( 0, 2, VertexAttribPointerType.Float, false, 0, 0 );

			GL.BindBuffer( BufferTarget.ArrayBuffer, texcoords );
			GL.BufferData( BufferTarget.ArrayBuffer, texcoordArray.Length * sizeof( float ), texcoordArray, BufferUsageHint.StaticCopy );
			GL.EnableVertexAttribArray( 1 );
			GL.VertexAttribPointer( 1, 2, VertexAttribPointerType.Float, false, 0, 0 );

			GL.BindBuffer( BufferTarget.ElementArrayBuffer, indices );
			GL.BufferData( BufferTarget.ElementArrayBuffer, indexArray.Length, indexArray, BufferUsageHint.StaticCopy );


			//7. render said quad using the bmp texture
			Matrix4 proj = Matrix4.CreateOrthographicOffCenter( 0.0f, 1280f, 0.0f, 720f, -1.0f, 1.0f );
			CEngine.UseShaderProgram( Shaders.ScreenShaderId );
			CEngine.SetShaderProgramUniformMatrix( Shaders.ScreenShaderId, ref proj, Uniforms.ProjectionMatrix );

			GL.DrawElements( PrimitiveType.Triangles, 6, DrawElementsType.UnsignedByte, 0 );

			//8. clean up
			GL.BindTexture( TextureTarget.Texture2D, 0 );
			GL.DeleteTexture( textureId );
			GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
			GL.BindBuffer( BufferTarget.ElementArrayBuffer, 0 );
			GL.BindVertexArray( 0 );
			GL.DeleteBuffer( vertices );
			GL.DeleteBuffer( texcoords );
			GL.DeleteBuffer( indices );
			GL.DeleteVertexArray( vao );
		}

		//public static ID CreateRenderObject( Chunk chunk, Chunk? chunkNorth, Chunk? chunkSouth, Chunk? chunkEast, Chunk? chunkWest, Chunk? chunkTop, Chunk? chunkBottom, int xOffset, int yOffset, int zOffset ) {
		//	List<uint> data = new List<uint>( Chunk.ChunkSize );
		//
		//	for ( byte k = 0; k < Chunk.ChunkLength; ++k )
		//	for ( byte j = 0; j < Chunk.ChunkLength; ++j )
		//	for ( byte i = 0; i < Chunk.ChunkLength; ++i ) {
		//		uint x        = i;
		//		uint y        = j;
		//		uint z        = k;
		//		uint occluded = 0;
		//
		//		if ( ( i < Chunk.ChunkLength - 1 && chunk.GetVoxel( i + 1, j, k ).Alpha == 3 ) || ( chunkEast.HasValue && chunkEast.Value.GetVoxel( 0, j, k ).Alpha == 3 ) ) {
		//			x += 1 << 6;
		//			++occluded;
		//		} //e
		//
		//		if ( ( i > 0 && chunk.GetVoxel( i - 1, j, k ).Alpha == 3 ) || ( chunkWest.HasValue && chunkWest.Value.GetVoxel( 0, j, k ).Alpha == 3 ) ) {
		//			x += 1 << 7;
		//			++occluded;
		//		} //w
		//
		//		if ( ( j < Chunk.ChunkLength - 1 && chunk.GetVoxel( i, j + 1, k ).Alpha == 3 ) || ( chunkTop.HasValue && chunkTop.Value.GetVoxel( 0, j, k ).Alpha == 3 ) ) {
		//			y += 1 << 6;
		//			++occluded;
		//		} //t
		//
		//		if ( ( j > 0 && chunk.GetVoxel( i, j - 1, k ).Alpha == 3 ) || ( chunkBottom.HasValue && chunkBottom.Value.GetVoxel( 0, j, k ).Alpha == 3 ) ) {
		//			y += 1 << 7;
		//			++occluded;
		//		} //b
		//
		//		if ( ( k < Chunk.ChunkLength - 1 && chunk.GetVoxel( i, j, k + 1 ).Alpha == 3 ) || ( chunkNorth.HasValue && chunkNorth.Value.GetVoxel( 0, j, k ).Alpha == 3 ) ) {
		//			z += 1 << 6;
		//			++occluded;
		//		} //n
		//
		//		if ( ( k > 0 && chunk.GetVoxel( i, j, k - 1 ).Alpha == 3 ) || ( chunkSouth.HasValue && chunkSouth.Value.GetVoxel( 0, j, k ).Alpha == 3 ) ) {
		//			z += 1 << 7;
		//			++occluded;
		//		} //s
		//
		//		if ( occluded < 6 ) {
		//			uint c = chunk.GetVoxel( i, j, k ).color;
		//			uint d = 0;
		//			d += x << 24;
		//			d += y << 16;
		//			d += z << 8;
		//			d += c << 0;
		//			data.Add( d );
		//		}
		//	}
		//
		//	int vao = GL.GenVertexArray();
		//	int vbo = GL.GenBuffer();
		//
		//	GL.BindVertexArray( vao );
		//	GL.BindBuffer( BufferTarget.ArrayBuffer, vbo );
		//	GL.BufferData( BufferTarget.ArrayBuffer, data.Count * sizeof ( uint ), data.ToArray(), BufferUsageHint.StaticCopy );
		//	GL.EnableVertexAttribArray( 0 );
		//	GL.VertexAttribIPointer( 0, 1, VertexAttribIntegerType.UnsignedInt, 0, IntPtr.Zero );
		//
		//	GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
		//	GL.BindVertexArray( 0 );
		//	GL.DeleteBuffer( vbo );
		//
		//	RenderObject ro = new RenderObject() {
		//		offset = new OpenTK.Mathematics.Vector3( xOffset, yOffset, zOffset ),
		//		size   = data.Count,
		//		vao    = vao
		//	};
		//
		//	return _renderObjects.AddAsset( ro );
		//}

		public static void DeleteRenderObject( ID id ) {
			RenderObject ro = _renderObjects[id];
			GL.DeleteVertexArray( ro.vao );
			_renderObjects.RemoveAsset( id );
		}

		public static void RenderRenderObject( ID id, ID shaderId ) {
			RenderObject ro              = _renderObjects[id];
			Matrix4      transformMatrix = Matrix4.CreateTranslation( ro.offset );

			CEngine.UseShaderProgram( shaderId );
			CEngine.SetShaderProgramUniformMatrix( shaderId, ref transformMatrix, Uniforms.TransformationMatrix );

			GL.BindVertexArray( ro.vao );
			GL.DrawArrays( PrimitiveType.Points, 0, ro.size );
			GL.BindVertexArray( 0 );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Vector3i Intersect( Vector3 pos, Vector3 dir, Vector3 dirInv, float size, out float t ) {
			float[] tMin = new[] {dir.X >= 0.0f ? ( size - pos.X ) * dirInv.X : -pos.X * dirInv.X, dir.Y >= 0.0f ? ( size - pos.Y ) * dirInv.Y : -pos.Y * dirInv.Y, dir.Z >= 0.0f ? ( size - pos.Z ) * dirInv.Z : -pos.Z * dirInv.Z};

			//compare z and y if z smaller -> switch z, y
			bool c0 = tMin[2] < tMin[1];
			if ( c0 ) {
				tMin[1] += tMin[2];
				tMin[2] =  tMin[1] - tMin[2];
				tMin[1] -= tMin[2];
			}

			//compare x and y, if y smaller -> switch x, y
			bool c1 = tMin[1] < tMin[0];
			if ( c1 ) {
				tMin[0] += tMin[1];
				tMin[1] =  tMin[0] - tMin[1];
				tMin[0] -= tMin[1];
			}

			//compare z and y, if z smaller -> switch y,z
			bool c2 = tMin[2] < tMin[1];
			if ( c2 ) {
				tMin[1] += tMin[2];
				tMin[2] =  tMin[1] - tMin[2];
				tMin[1] -= tMin[2];
			}

			t = tMin[0] + 0.001f;

			float d0 = tMin[1] - tMin[0];
			float d1 = tMin[2] - tMin[0];
			if ( d0 < 0.001f && d1 < 0.001f ) {
				int x = dir.X >= 0.0f ? 1 : -1;
				int y = dir.Y >= 0.0f ? 1 : -1;
				int z = dir.Z >= 0.0f ? 1 : -1;
				return new Vector3i( x, y, z );
			}
			else if ( d0 < 0.001f ) {
				int x = !c1 || !c2 ? dir.X >= 0.0f ? 1 : -1 : 0;
				int y = c1 && !c0 || c0 && c2 ? dir.Y >= 0.0f ? 1 : -1 : 0;
				int z = c0 && c1 || !c0 && c2 || c0 && !c2 ? dir.Z >= 0.0f ? 1 : -1 : 0;
				return new Vector3i( x, y, z );
			}
			else {
				int x = !c1 ? dir.X >= 0.0f ? 1 : -1 : 0;
				int y = c1 && !c0 ? dir.Y >= 0.0f ? 1 : -1 : 0;
				int z = c0 && c1 ? dir.Z >= 0.0f ? 1 : -1 : 0;
				return new Vector3i( x, y, z );
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Vector3i Intersect2( Vector3 pos, Vector3 dir, Vector3 dirInv, float size, out float t ) {
			float[] tMax = new[] {dir.X >= 0.0f ? ( size - pos.X ) * dirInv.X : -pos.X * dirInv.X, dir.Y >= 0.0f ? ( size - pos.Y ) * dirInv.Y : -pos.Y * dirInv.Y, dir.Z >= 0.0f ? ( size - pos.Z ) * dirInv.Z : -pos.Z * dirInv.Z};

			int[] indices = new[] {0, 1, 2}; //0, 2, 1

			void Swap( int i, int j ) {
				//tMax[i] += tMax[j];
				//tMax[j] = tMax[i] - tMax[j];
				//tMax[i] -= tMax[j];

				int temp = indices[i];
				indices[i] = indices[j];
				indices[j] = temp;

				//indices[i] += indices[j];
				//indices[j] = indices[i] - indices[j];
				//indices[i] -= indices[j];
			}

			if ( tMax[indices[1]] < tMax[indices[0]] ) Swap( 0, 1 );

			if ( tMax[indices[2]] < tMax[indices[0]] ) Swap( 0, 2 );

			if ( tMax[indices[2]] < tMax[indices[1]] ) Swap( 1, 2 );


			t = tMax[indices[0]] + 0.001f;
			float d0 = tMax[indices[1]] - tMax[indices[0]];
			float d1 = tMax[indices[2]] - tMax[indices[0]];
			if ( d0 <= 0.001f && d1 <= 0.001f ) {
				int x = dir.X >= 0.0f ? 1 : -1;
				int y = dir.Y >= 0.0f ? 1 : -1;
				int z = dir.Z >= 0.0f ? 1 : -1;
				return new Vector3i( x, y, z );
			}

			if ( d0 <= 0.001f ) {
				int x = indices[0] == 0 || indices[1] == 0 ? dir.X >= 0.0f ? 1 : -1 : 0;
				int y = indices[0] == 1 || indices[1] == 1 ? dir.Y >= 0.0f ? 1 : -1 : 0;
				int z = indices[0] == 2 || indices[1] == 2 ? dir.Z >= 0.0f ? 1 : -1 : 0;
				return new Vector3i( x, y, z );
			}
			else {
				int x = indices[0] == 0 ? dir.X >= 0.0f ? 1 : -1 : 0;
				int y = indices[0] == 1 ? dir.Y >= 0.0f ? 1 : -1 : 0;
				int z = indices[0] == 2 ? dir.Z >= 0.0f ? 1 : -1 : 0;
				return new Vector3i( x, y, z );
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static float Intersect( Vector3 pos, Vector3 dir, Vector3 dirInv, float size ) {
			//int stepX = dir.X < 0.0f ? -1 : 1;
			//int stepY = dir.Y < 0.0f ? -1 : 1;
			//int stepZ = dir.Z < 0.0f ? -1 : 1;

			float tMaxX = dir.X >= 0.0f ? ( size - pos.X ) * dirInv.X : -pos.X * dirInv.X;
			float tMaxY = dir.Y >= 0.0f ? ( size - pos.Y ) * dirInv.Y : -pos.Y * dirInv.Y;
			float tMaxZ = dir.Z >= 0.0f ? ( size - pos.Z ) * dirInv.Z : -pos.Z * dirInv.Z;

			if ( tMaxX < tMaxY ) return tMaxX < tMaxZ ? tMaxX : tMaxZ;
			return tMaxY < tMaxZ ? tMaxY : tMaxZ;
		}


		public static void RayTracer( World world, Camera camera, ID shaderId ) {
			//1. create rays 1920x1080
			Vector3 camdir = camera.GetDirection();

			Vector3 x = Vector3.Cross( Vector3.UnitY, camdir );
			Vector3 y = 9.0f / 16.0f * Vector3.Cross( camdir, x );

			byte[] bmp = new byte[3 * 1920 * 1080];
			Parallel.For( 0, 1080, new ParallelOptions() {MaxDegreeOfParallelism = 6}, ( int j ) => {
				//for( int j = 0; j < 1080; ++j ) {
				Vector3 orig = camera.GetPosition() + ( j / 1080.0f - 0.5f ) * y - 0.5f * x;
				Vector3 dir  = 0.5f * camdir + ( j / 1080.0f - 0.5f ) * y - 0.5f * x;

				for ( int i = 0; i < 1920; ++i ) {
					orig += 0.0005208f * x;
					dir  += 0.0005208f * x;
					Vector3 dirInv = new Vector3( 1.0f / dir.X, 1.0f / dir.Y, 1.0f / dir.Z );

					Vector3  pos      = orig;
					Vector3i chunkPos = new Vector3i( ( int ) MathF.Floor( orig.X / 32.0f ), ( int ) MathF.Floor( orig.Y / 32.0f ), ( int ) MathF.Floor( orig.Z / 32.0f ) );
					byte     color    = 0;
					float    total    = 0.0f;

					outer:
					while ( color == 0 && total < 120.0f ) {
						Chunk? chunk = world.GetChunk( chunkPos.X, chunkPos.Y, chunkPos.Z );

						if ( chunk == null ) {
							Vector3 rPos = new Vector3( pos.X - 32.0f * MathF.Floor( pos.X / 32.0f ),
								pos.Y - 32.0f * MathF.Floor( pos.Y / 32.0f ),
								pos.Z - 32.0f * MathF.Floor( pos.Z / 32.0f ) );

							float t = Intersect( rPos, dir, dirInv, 32.0f );

							pos.X += ( t + 0.001f ) * dir.X;
							pos.Y += ( t + 0.001f ) * dir.Y;
							pos.Z += ( t + 0.001f ) * dir.Z;
							total += t;

							float f0 = MathF.Floor( 0.03125f * pos.X );
							float f1 = MathF.Floor( 0.03125f * pos.Y );
							float f2 = MathF.Floor( 0.03125f * pos.Z );
							chunkPos = new Vector3i( ( int ) f0, ( int ) f1, ( int ) f2 );
						}
						else {
							int lvl = 0;
							while ( true ) {
								Vector3i vPos = new Vector3i( ( int ) MathF.Floor( pos.X - chunkPos.X * 32 ), ( int ) MathF.Floor( pos.Y - chunkPos.Y * 32 ), ( int ) MathF.Floor( pos.Z - chunkPos.Z * 32 ) );

								a:
								if ( !chunk.IsEmpty( vPos.X, vPos.Y, vPos.Z, lvl ) ) {
									if ( lvl == 5 ) {
										color = chunk[vPos.X, vPos.Y, vPos.Z].color;
										if ( color != 0 ) break;
									}
									else {
										++lvl;
										goto a;
									}
								}

								int size = 32 >> lvl; //or just size
								Vector3 rPos = new Vector3( pos.X - size * MathF.Floor( pos.X / size ),
									pos.Y - size * MathF.Floor( pos.Y / size ),
									pos.Z - size * MathF.Floor( pos.Z / size ) );

								float t = Intersect( rPos, dir, dirInv, size );
								pos   += ( t + 0.001f ) * dir;
								total += t;

								Vector3i vPos2 = new Vector3i( ( int ) MathF.Floor( pos.X - chunkPos.X * 32 ), ( int ) MathF.Floor( pos.Y - chunkPos.Y * 32 ), ( int ) MathF.Floor( pos.Z - chunkPos.Z * 32 ) );

								while ( vPos.X / size - ( int ) Math.Floor( vPos2.X / ( float ) size ) != 0 ) {
									if ( lvl == 0 ) {
										chunkPos = new Vector3i( ( int ) MathF.Floor( pos.X / 32.0f ),
											( int ) MathF.Floor( pos.Y / 32.0f ), ( int ) MathF.Floor( pos.Z / 32.0f ) );
										goto outer;
									}

									--lvl;
									size <<= 1;
								}

								while ( vPos.Y / size - ( int ) Math.Floor( vPos2.Y / ( float ) size ) != 0 ) {
									if ( lvl == 0 ) {
										chunkPos = new Vector3i( ( int ) MathF.Floor( pos.X / 32.0f ),
											( int ) MathF.Floor( pos.Y / 32.0f ), ( int ) MathF.Floor( pos.Z / 32.0f ) );
										goto outer;
									}

									--lvl;
									size <<= 1;
								}

								while ( vPos.Z / size - ( int ) Math.Floor( vPos2.Z / ( float ) size ) != 0 ) {
									if ( lvl == 0 ) {
										chunkPos = new Vector3i( ( int ) MathF.Floor( pos.X / 32.0f ),
											( int ) MathF.Floor( pos.Y / 32.0f ), ( int ) MathF.Floor( pos.Z / 32.0f ) );
										goto outer;
									}

									--lvl;
									size <<= 1;
								}
							}
						}
					}

					//3. cast 1 shadow ray per primary ray

					//4. write result to bmp
					bmp[3 * ( i + 1920 * j ) + 0] = ( byte ) ( ( ( color >> 6 ) & 3 ) * 85 );
					bmp[3 * ( i + 1920 * j ) + 1] = ( byte ) ( ( ( color >> 4 ) & 3 ) * 85 );
					bmp[3 * ( i + 1920 * j ) + 2] = ( byte ) ( ( ( color >> 2 ) & 3 ) * 85 );
				}
			} );

			/*
			for ( int j = 0; j < 1080; ++j )
			for ( int i = 0; i < 1920; ++i ) {
				//2. launch (primary) rays into chunk
				Vector3 offset = ( ( i / 1920.0f ) - 0.5f ) * x + ( ( j / 1080.0f ) - 0.5f ) * y;
				Vector3 orig   = camera.GetPosition() + offset;
				Vector3 dir    = ( offset + 0.5f * camdir );

				Vector3 dirInv = new Vector3( 1.0f / dir.X, 1.0f / dir.Y, 1.0f / dir.Z );

				Vector3  pos      = orig;
				Vector3i chunkPos = new Vector3i( ( int ) MathF.Floor( orig.X / 32.0f ), ( int ) MathF.Floor( orig.Y / 32.0f ), ( int ) MathF.Floor( orig.Z / 32.0f ) );
				byte     color    = 0;
				float    total    = 0.0f;

				while ( color == 0 && total < 120.0f ) {
					Chunk? chunk = world.GetChunk( chunkPos.X, chunkPos.Y, chunkPos.Z );

					if ( chunk == null ) {
						Vector3 rPos = new Vector3( pos.X - 32.0f * MathF.Floor( pos.X / 32.0f ),
							pos.Y                         - 32.0f * MathF.Floor( pos.Y / 32.0f ),
							pos.Z                         - 32.0f * MathF.Floor( pos.Z / 32.0f ) );

						float t = Intersect( rPos, dir, dirInv, 32.0f );
						pos   += ( t + 0.001f ) * dir;
						total += t;

						float f0 = MathF.Floor( 0.03125f * pos.X );
						float f1 = MathF.Floor( 0.03125f * pos.Y );
						float f2 = MathF.Floor( 0.03125f * pos.Z );
						chunkPos = new Vector3i( ( int ) f0, ( int ) f1, ( int ) f2 );
					}
					else {
						int lvl = 0;
						while ( true ) {
							Vector3i vPos = new Vector3i( ( int ) ( pos.X - chunkPos.X * 32 ), ( int ) ( pos.Y - chunkPos.Y * 32 ), ( int ) ( pos.Z - chunkPos.Z * 32 ) );

							a:
							if ( !chunk.IsEmpty( vPos.X, vPos.Y, vPos.Z, lvl ) ) {
								if ( lvl == 5 ) {
									color = chunk[vPos.X, vPos.Y, vPos.Z].color;
									if ( color != 0 ) break;
								}
								else {
									++lvl;
									goto a;
								}
							}

							/*
							int mull = 32 >> 5; //or just size
							Vector3 rPos = new Vector3( pos.X - mull * ( int ) MathF.Floor( pos.X / mull ),
								pos.Y                  - mull * ( int ) MathF.Floor( pos.Y / mull ),
								pos.Z                  - mull * ( int ) MathF.Floor( pos.Z / mull ) );
							
							Vector3i dp = Vector3i.UnitY; //TODO
							float t = Intersect( rPos, dir, dirInv, mull );
							pos  += ( t + 0.001f ) * dir;
							total += t;

							if ( ( dp.X != 0 && ( lvl <= 4 || vPos.X / 2 != ( vPos.X + dp.X * mull ) / 2 ) ) ) {
								if ( lvl <= 3 || vPos.X / 4 != ( vPos.X + dp.X * mull ) / 4 ) {
									if ( lvl <= 2 || vPos.X / 8 != ( vPos.X + dp.X * mull ) / 8 ) {
										if ( lvl <= 1 || vPos.X / 16 != ( vPos.X + dp.X * mull ) / 16 ) {
											if ( lvl == 0 || vPos.X / 32 != ( vPos.X + dp.X * mull ) / 32 ) {
												chunkPos = new Vector3i( ( int ) MathF.Floor( pos.X / 32.0f ), ( int ) MathF.Floor( pos.Y / 32.0f ), ( int ) MathF.Floor( pos.Z / 32.0f ) );
												break;
											}
											else lvl = 1;
										}
										else lvl = 2;
									}
									else lvl = 3;
								}
								else lvl = 4;
							}

							if ( ( dp.Y != 0 && ( lvl <= 4 || vPos.Y / 2 != ( vPos.Y + dp.Y * mull ) / 2 ) ) ) {
								if ( lvl <= 3 || vPos.Y / 4 != ( vPos.Y + dp.Y * mull ) / 4 ) {
									if ( lvl <= 2 || vPos.Y / 8 != ( vPos.Y + dp.Y * mull ) / 8 ) {
										if ( lvl <= 1 || vPos.Y / 16 != ( vPos.Y + dp.Y * mull ) / 16 ) {
											if ( lvl == 0 || vPos.Y / 32 != ( vPos.Y + dp.Y * mull ) / 32 ) {
												chunkPos = new Vector3i( ( int ) MathF.Floor( pos.X / 32.0f ), ( int ) MathF.Floor( pos.Y / 32.0f ), ( int ) MathF.Floor( pos.Z / 32.0f ) );
												break;
											}
											else lvl = 1;
										}
										else lvl = 2;
									}
									else lvl = 3;
								}
								else lvl = 4;
							}

							if ( ( dp.Z != 0 && ( lvl <= 4 || vPos.Z / 2 != ( vPos.Z + dp.Z * mull ) / 2 ) ) ) {
								if ( lvl <= 3 || vPos.Z / 4 != ( vPos.Z + dp.Z * mull ) / 4 ) {
									if ( lvl <= 2 || vPos.Z / 8 != ( vPos.Z + dp.Z * mull ) / 8 ) {
										if ( lvl <= 1 || vPos.Z / 16 != ( vPos.Z + dp.Z * mull ) / 16 ) {
											if ( lvl == 0 || vPos.Z / 32 != ( vPos.Z + dp.Z * mull ) / 32 ) {
												chunkPos = new Vector3i( ( int ) MathF.Floor( pos.X / 32.0f ), ( int ) MathF.Floor( pos.Y / 32.0f ), ( int ) MathF.Floor( pos.Z / 32.0f ) );
												break;
											}
											else lvl = 1;
										}
										else lvl = 2;
									}
									else lvl = 3;
								}
								else lvl = 4;
							}
						}
					}
				}

				//3. cast 1 shadow ray per primary ray

				//4. write result to bmp
				bmp[3 * ( i + 1920 * j ) + 0] = ( byte ) ( ( ( color >> 6 ) & 3 ) * 85 );
				bmp[3 * ( i + 1920 * j ) + 1] = ( byte ) ( ( ( color >> 4 ) & 3 ) * 85 );
				bmp[3 * ( i + 1920 * j ) + 2] = ( byte ) ( ( ( color >> 2 ) & 3 ) * 85 );
			}*/

			//5. create texture using bmp

			int textureId = GL.GenTexture();
			GL.BindTexture( TextureTarget.Texture2D, textureId );

			//These are temporary. Will set the tex params properly when needed
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, ( int ) TextureWrapMode.Repeat );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, ( int ) TextureWrapMode.Repeat );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ( int ) TextureMinFilter.Linear );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ( int ) TextureMagFilter.Linear );

			GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 1920, 1080,
				0, PixelFormat.Rgb, PixelType.UnsignedByte, bmp );

			//6. create full screen quad
			int vao       = GL.GenVertexArray();
			int vertices  = GL.GenBuffer();
			int texcoords = GL.GenBuffer();
			int indices   = GL.GenBuffer();

			float[] vertexArray   = new[] {0.0f, 0.0f, 1280.0f, 0.0f, 1280.0f, 720.0f, 0.0f, 720.0f};
			float[] texcoordArray = new[] {0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f};
			byte[]  indexArray    = new byte[] {0, 1, 2, 2, 3, 0};

			GL.BindVertexArray( vao );
			GL.BindBuffer( BufferTarget.ArrayBuffer, vertices );
			GL.BufferData( BufferTarget.ArrayBuffer, vertexArray.Length * sizeof( float ), vertexArray, BufferUsageHint.StaticCopy );
			GL.EnableVertexAttribArray( 0 );
			GL.VertexAttribPointer( 0, 2, VertexAttribPointerType.Float, false, 0, 0 );

			GL.BindBuffer( BufferTarget.ArrayBuffer, texcoords );
			GL.BufferData( BufferTarget.ArrayBuffer, texcoordArray.Length * sizeof( float ), texcoordArray, BufferUsageHint.StaticCopy );
			GL.EnableVertexAttribArray( 1 );
			GL.VertexAttribPointer( 1, 2, VertexAttribPointerType.Float, false, 0, 0 );

			GL.BindBuffer( BufferTarget.ElementArrayBuffer, indices );
			GL.BufferData( BufferTarget.ElementArrayBuffer, indexArray.Length, indexArray, BufferUsageHint.StaticCopy );


			//7. render said quad using the bmp texture
			Matrix4 proj = Matrix4.CreateOrthographicOffCenter( 0.0f, 1280f, 0.0f, 720f, -1.0f, 1.0f );
			CEngine.UseShaderProgram( shaderId );
			CEngine.SetShaderProgramUniformMatrix( shaderId, ref proj, Uniforms.ProjectionMatrix );

			GL.DrawElements( PrimitiveType.Triangles, 6, DrawElementsType.UnsignedByte, 0 );

			//8. clean up
			GL.BindTexture( TextureTarget.Texture2D, 0 );
			GL.DeleteTexture( textureId );
			GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
			GL.BindBuffer( BufferTarget.ElementArrayBuffer, 0 );
			GL.BindVertexArray( 0 );
			GL.DeleteBuffer( vertices );
			GL.DeleteBuffer( texcoords );
			GL.DeleteBuffer( indices );
			GL.DeleteVertexArray( vao );
		}
	}
}