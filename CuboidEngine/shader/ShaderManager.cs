using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace CuboidEngine
{
	internal static class ShaderManager
	{
		private static readonly ShaderType[] _loadShaderTypes = new[] { ShaderType.VertexShader, ShaderType.FragmentShader, ShaderType.GeometryShader };
		private static readonly AssetManager<int> _assetManager = new AssetManager<int>();

		public static ID LoadShaderProgramFromFile( string[] shaderFiles ) {
			Debug.Assert( shaderFiles.Length == 2 || shaderFiles.Length == 3, $"Need at least 2 shaders and at most 3" );

			string[] shaderSources = new string[shaderFiles.Length];
			for ( int i = 0; i < shaderFiles.Length; ++i ) {
				Debug.Assert( File.Exists( shaderFiles[i] ), $"Shader file '{shaderFiles[i]}' does not exist!" );
				shaderSources[i] = File.ReadAllText( shaderFiles[i] );
			}
			
			return LoadShaderProgramFromSource( shaderSources );
		}

		public static ID LoadShaderProgramFromSource( string[] shaderSources ) {
			Debug.Assert( shaderSources.Length == 2 || shaderSources.Length == 3, $"Need at least 2 shaders and at most 3" );
			int shaderProgramId = LoadShaderProgram( shaderSources );
			return _assetManager.AddAsset( shaderProgramId );
		}

		public static void UnloadShaderProgram( ID id ) {
			int shaderProgramId = _assetManager[id];
			GL.DeleteProgram( shaderProgramId );
			_assetManager.RemoveAsset( id );
		}

		public static void UseShaderProgram( ID id ) {
			int shaderProgramId = _assetManager[id];
			GL.UseProgram( shaderProgramId );
		}

		public static void SetShaderProgramUniformMatrix( ID id, ref Matrix4 matrix, Uniforms uniformType ) {
			int shaderProgramId = _assetManager[id];
			GL.UseProgram( shaderProgramId );
			int uniform = GL.GetUniformLocation( shaderProgramId, UniformHelper.GetUniformName( uniformType ) );
			GL.UniformMatrix4( uniform, false, ref matrix );
		}

		public static void SetShaderProgramUniformVector( ID id, Vector3 vector, Uniforms uniformType ) {
			int shaderProgramId = _assetManager[id];
			GL.UseProgram( shaderProgramId );
			int uniform = GL.GetUniformLocation( shaderProgramId, UniformHelper.GetUniformName( uniformType ) );
			GL.Uniform3( uniform, vector );
		}

		

		private static int LoadShader( string shaderSource, ShaderType type ) {
			int shaderId = GL.CreateShader( type );
			GL.ShaderSource( shaderId, shaderSource );
			GL.CompileShader( shaderId );
			ValidateShader( shaderId );
			return shaderId;
		}

		private static int LoadShaderProgram( string[] shaderSources ) {
			int shaderProgramId = GL.CreateProgram();

			int[] shaderIds = new int[shaderSources.Length];
			for ( int i = 0; i < shaderSources.Length; ++i ) {
				shaderIds[i] = LoadShader( shaderSources[i], _loadShaderTypes[i] );
				GL.AttachShader( shaderProgramId, shaderIds[i] );
			}

			GL.LinkProgram( shaderProgramId );

			for ( int i = 0; i < shaderSources.Length; ++i ) {
				GL.DetachShader( shaderProgramId, shaderIds[i] );
				GL.DeleteShader( shaderIds[i] );
			}

			ValidateShaderProgram( shaderProgramId );

			return shaderProgramId;
		}


		[System.Diagnostics.Conditional( "DEBUG" )]
		private static void ValidateShader( int shaderId ) {
			string infoLog = GL.GetShaderInfoLog( shaderId );
			Debug.Assert( string.IsNullOrEmpty( infoLog ), infoLog );
		}

		[System.Diagnostics.Conditional( "DEBUG" )]
		private static void ValidateShaderProgram( int shaderProgramId ) {
			string infoLog = GL.GetProgramInfoLog( shaderProgramId );
			Debug.Assert( string.IsNullOrEmpty( infoLog ), infoLog );
		}
	}
}