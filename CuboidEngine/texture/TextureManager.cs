using System;
using System.Diagnostics;
using System.IO;
using OpenTK.Graphics.OpenGL4;

namespace CuboidEngine {
	internal static class TextureManager {
		private static readonly AssetManager<int> _textures = new AssetManager<int>();

		public static ID CreateEmptyTexture() {
			int textureId = GL.GenTexture();
			GL.BindTexture( TextureTarget.Texture2D, textureId );

			//These are temporary. Will set the tex params properly when needed
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, ( int ) TextureWrapMode.Repeat );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, ( int ) TextureWrapMode.Repeat );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ( int ) TextureMinFilter.Linear );
			GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ( int ) TextureMagFilter.Linear );

			GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1920, 1080,
				0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero );

			return _textures.AddAsset( textureId );
		}

		public static int GetTextureId( ID id ) {
			return _textures[id];
		}
	}
}