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

		public static void UploadChunk( Chunk chunk, int index ) {
			int  pos    = 0;
			int  lod    = index < OpenCLObjects.Lod0ChunkNumber ? 0 : 1;
			long offset = lod == 0 ? index * Chunk.Lod0VoxelCount : OpenCLObjects.Lod0ChunkNumber * Chunk.Lod0VoxelCount + ( index - OpenCLObjects.Lod0ChunkNumber ) * Chunk.Lod1VoxelCount;

			for ( int i = 0; i <= Chunk.ChunkLengthBits - lod; ++i ) {
				CEngine.CLEnqueueWriteBuffer( OpenCLObjects.VoxelBuffer, Voxel.ByteSize * ( ( int ) offset + pos ), chunk.Vol( i ) );
				pos += ( 1 << i ) * ( 1 << i ) * ( 1 << i );
			}
		}

		public static void RenderRayMarcher() {
			CEngine.CLSetKernelArg( OpenCLObjects.RayMarcherKernel, 0, OpenCLObjects.PixelBuffer );
			CEngine.CLSetKernelArg( OpenCLObjects.RayMarcherKernel, 1, OpenCLObjects.CameraBuffer );
			CEngine.CLSetKernelArg( OpenCLObjects.RayMarcherKernel, 2, OpenCLObjects.MapBuffer );
			CEngine.CLSetKernelArg( OpenCLObjects.RayMarcherKernel, 3, OpenCLObjects.VoxelBuffer );
			CEngine.CLSetKernelArg( OpenCLObjects.RayMarcherKernel, 4, OpenCLObjects.DistanceBuffer );
			CEngine.CLSetKernelArg( OpenCLObjects.RayMarcherKernel, 5, OpenCLObjects.RngBuffer );

			CEngine.CLEnqueueAquireGLObjects( OpenCLObjects.PixelBuffer );
			CEngine.CLRunKernel( OpenCLObjects.RayMarcherKernel, 1, new[] {1920 * 1080}, new[] {32} );
			CEngine.CLEnqueueReleaseGLObjects( OpenCLObjects.PixelBuffer );
			CEngine.CLWaitForFinish();

			RenderRayMarcherResult();
		}

		public static void RequestChunks() {
			CEngine.CLSetKernelArg( OpenCLObjects.RequestChunkKernel, 0, OpenCLObjects.CameraBuffer );
			CEngine.CLSetKernelArg( OpenCLObjects.RequestChunkKernel, 1, OpenCLObjects.MapBuffer );
			CEngine.CLSetKernelArg( OpenCLObjects.RequestChunkKernel, 2, OpenCLObjects.VoxelBuffer );
			CEngine.CLSetKernelArg( OpenCLObjects.RequestChunkKernel, 3, OpenCLObjects.RequestChunkBuffer );
			CEngine.CLSetKernelArg( OpenCLObjects.RequestChunkKernel, 4, OpenCLObjects.RngBuffer );

			CEngine.CLRunKernel( OpenCLObjects.RequestChunkKernel, 1, new[] {OpenCLObjects.RequestChunkBufferLength}, new[] {32} );
			CEngine.CLWaitForFinish();
		}

		public static void RenderRayMarcherResult() {
			int vao       = GL.GenVertexArray();
			int vertices  = GL.GenBuffer();
			int texcoords = GL.GenBuffer();
			int indices   = GL.GenBuffer();

			float[] vertexArray   = {0.0f, 0.0f, 1280.0f, 0.0f, 1280.0f, 720.0f, 0.0f, 720.0f};
			float[] texcoordArray = {0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f};
			byte[]  indexArray    = {0, 1, 2, 2, 3, 0};

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

			Matrix4 proj = Matrix4.CreateOrthographicOffCenter( 0.0f, 1280f, 0.0f, 720f, -1.0f, 1.0f );
			CEngine.UseShaderProgram( Shaders.ScreenShaderId );
			CEngine.SetShaderProgramUniformMatrix( Shaders.ScreenShaderId, ref proj, Uniforms.ProjectionMatrix );

			int id = TextureManager.GetTextureId( OpenCLObjects.TextureID );
			GL.BindTexture( TextureTarget.Texture2D, id );
			GL.DrawElements( PrimitiveType.Triangles, 6, DrawElementsType.UnsignedByte, 0 );

			GL.BindTexture( TextureTarget.Texture2D, 0 );
			GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
			GL.BindBuffer( BufferTarget.ElementArrayBuffer, 0 );
			GL.BindVertexArray( 0 );
			GL.DeleteBuffer( vertices );
			GL.DeleteBuffer( texcoords );
			GL.DeleteBuffer( indices );
			GL.DeleteVertexArray( vao );
		}

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


		//DEPRECATED============================================================


		[Obsolete]
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static float Intersect( Vector3 pos, Vector3 dir, Vector3 dirInv, float size ) {
			float tMaxX = dir.X >= 0.0f ? ( size - pos.X ) * dirInv.X : -pos.X * dirInv.X;
			float tMaxY = dir.Y >= 0.0f ? ( size - pos.Y ) * dirInv.Y : -pos.Y * dirInv.Y;
			float tMaxZ = dir.Z >= 0.0f ? ( size - pos.Z ) * dirInv.Z : -pos.Z * dirInv.Z;

			if ( tMaxX < tMaxY ) return tMaxX < tMaxZ ? tMaxX : tMaxZ;
			return tMaxY < tMaxZ ? tMaxY : tMaxZ;
		}

		[Obsolete]
		public static void RayTracer( World world, Camera camera, ID shaderId ) {
			//1. create rays 1920x1080
			Vector3 camdir = camera.GetDirection();

			Vector3 x = Vector3.Cross( Vector3.UnitY, camdir );
			Vector3 y = 9.0f / 16.0f * Vector3.Cross( camdir, x );

			byte[] bmp = new byte[3 * 1920 * 1080];
			Parallel.For( 0, 1080, new ParallelOptions() {MaxDegreeOfParallelism = 6}, ( int j ) => {
				//for( int j = 0; j < 1080; ++j ) {
				Vector3 orig = camera.Pos + ( j / 1080.0f - 0.5f ) * y - 0.5f * x;
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