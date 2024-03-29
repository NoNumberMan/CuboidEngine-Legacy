﻿using System;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.ES11;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace CuboidEngine {
	public static class CEngine {
		private static GameWindow? _window;
		private static IGame?      _game;

		public static bool IsInitialized => _window != null;


		public static void HelloWorld() {
			Console.WriteLine( "Hello World" );
		}

		private static unsafe IntPtr GetContext() {
			return ( IntPtr ) OpenTK.Windowing.GraphicsLibraryFramework.GLFW.GetWGLContext( _window!.WindowPtr );
		}

		public static void Init( IGame game ) {
			_game = game;

			GameWindowSettings   gws = GameWindowSettings.Default;
			NativeWindowSettings nws = NativeWindowSettings.Default;
			gws.IsMultiThreaded = false;
			gws.RenderFrequency = 6000;
			gws.UpdateFrequency = 6000;
			nws.Profile         = ContextProfile.Compatability;

			nws.APIVersion = Version.Parse( "3.2.0" );
			nws.Size       = new Vector2i( 1280, 720 );
			nws.Title      = game.GetTitle();

			_window = new GameWindow( gws, nws );

			_window.VSync       =  VSyncMode.Off;
			_window.Load        += OnWindowLoad;
			_window.MouseMove   += OnWindowMouseMove;
			_window.MouseUp     += OnWindowMouseButton;
			_window.MouseDown   += OnWindowMouseButton;
			_window.MouseWheel  += OnWindowMouseScroll;
			_window.KeyUp       += OnWindowKeyUp;
			_window.KeyDown     += OnWindowKeyDown;
			_window.RenderFrame += OnWindowRenderTick;
			_window.UpdateFrame += OnWindowUpdateTick;
			_window.Resize      += OnWindowResize;
			_window.Closed      += OnWindowClose;
			_window.Run();
		}

		//WINDOW
		public static bool IsMouseButtonDown( MouseButton button ) {
			return _window!.MouseState.IsButtonDown( ( OpenTK.Windowing.GraphicsLibraryFramework.MouseButton ) button );
		}


		//BASIC OPENGL
		public static ID LoadShaderProgramFromFile( string[] shaderFiles ) {
			return ShaderManager.LoadShaderProgramFromFile( shaderFiles );
		}

		public static ID LoadShaderProgramFromSource( string[] shaderSources ) {
			return ShaderManager.LoadShaderProgramFromSource( shaderSources );
		}

		public static void UnloadShaderProgram( ID id ) {
			ShaderManager.UnloadShaderProgram( id );
		}

		public static void UseShaderProgram( ID id ) {
			ShaderManager.UseShaderProgram( id );
		}

		public static void SetShaderProgramUniformMatrix( ID id, ref Matrix4 matrix, Uniforms uniformType ) {
			ShaderManager.SetShaderProgramUniformMatrix( id, ref matrix, uniformType );
		}

		public static void SetShaderProgramUniformVector( ID id, Vector3 vector, Uniforms uniformType ) {
			ShaderManager.SetShaderProgramUniformVector( id, vector, uniformType );
		}


		//BASIC OPENCL
		public static ID CLLoadKernelFromFiles( string kernelName, string[] kernelFiles ) {
			return ComputingManager.LoadKernelFromFiles( kernelName, kernelFiles );
		}

		public static ID CLLoadKernelFromSources( string kernelName, string[] kernelSources ) {
			return ComputingManager.LoadKernelFromSources( kernelName, kernelSources );
		}

		public static void CLUnloadKernel( ID id ) {
			ComputingManager.UnloadKernel( id );
		}

		public static void CLSetKernelArg<T>( ID id, uint index, T arg ) where T : unmanaged {
			ComputingManager.SetKernelArg<T>( id, index, arg );
		}

		public static void CLSetKernelArg( ID id, uint index, ID bufferId ) {
			ComputingManager.SetKernelArg( id, index, bufferId );
		}

		public static void CLRunKernel( ID id, int dim, int[] globalWorkSize, int[] localWorkSize ) {
			ComputingManager.RunKernel( id, dim, globalWorkSize, localWorkSize );
		}

		public static ID CLCreateBuffer<T>( T[] data ) where T : unmanaged {
			return ComputingManager.CreateBuffer( data );
		}

		public static ID CLCreateBuffer<T>( Span<T> data ) where T : unmanaged {
			return ComputingManager.CreateBuffer( data );
		}

		public static void CLEnqueueWriteBuffer<T>( ID id, int offset, Span<T> data ) where T : unmanaged {
			ComputingManager.EnqueueWriteBuffer( id, offset, data );
		}

		public static void CLEnqueueWriteBuffer<T>( ID id, int offset, T[] data ) where T : unmanaged {
			ComputingManager.EnqueueWriteBuffer( id, offset, data );
		}

		public static void CLEnqueueFillBuffer<T>( ID id, int offset, int size, T fill ) where T : unmanaged {
			ComputingManager.EnqueueFillBuffer( id, offset, size, fill );
		}

		public static void CLWaitForFinish() {
			ComputingManager.WaitForFinish();
		}

		public static ID CLCreateBuffer( int size, MemoryFlags flags = 0 ) {
			return ComputingManager.CreateBuffer( size, flags );
		}

		public static void CLEnqueueReadBuffer<T>( ID id, T[] data ) where T : unmanaged {
			ComputingManager.EnqueueReadBuffer( id, data );
		}

		public static void CLEnqueueAquireGLObjects( ID id ) {
			ComputingManager.EnqueueAquireGLObjects( id );
		}

		public static void CLEnqueueReleaseGLObjects( ID id ) {
			ComputingManager.EnqueueReleaseGLObjects( id );
		}

		public static void CLFlush() {
			ComputingManager.Flush();
		}


		//WORLD
		public static ID GenerateWorld( IWorldGenerator generator ) {
			return WorldManager.GenerateWorld( generator );
		}

		public static void DeleteWorld( ID id ) {
			WorldManager.RemoveWorld( id );
		}

		public static void SetWorldActive( ID id ) {
			WorldManager.SetWorldActive( id );
		}

		public static void SetWorldInactive( ID id ) {
			WorldManager.SetWorldInactive( id );
		}

		public static void GetCameraMatrix( ID id, out Matrix4 matrix ) {
			WorldManager.GetCameraMatrix( id, out matrix );
		}

		public static void GetCameraDirection( ID id, out System.Numerics.Vector3 dir ) {
			WorldManager.GetCameraDirection( id, out dir );
		}

		public static void SetCameraSize( ID id, float width, float height ) {
			WorldManager.SetCameraSize( id, width, height );
		}

		public static void SetCameraPosition( ID id, float x, float y, float z ) {
			WorldManager.SetCameraPosition( id, x, y, z );
		}

		public static void SetCameraVelocity( ID id, float dx, float dy, float dz ) {
			WorldManager.SetCameraVelocity( id, dx, dy, dz );
		}

		public static void SetCameraAngle( ID id, float pitch, float yaw, float roll ) {
			WorldManager.SetCameraAngle( id, pitch, yaw, roll );
		}

		public static void SetCameraAngularVelocity( ID id, float dpitch, float dyaw, float droll ) {
			WorldManager.SetCameraAngularVelocity( id, dpitch, dyaw, droll );
		}

		public static void AccCameraDir( ID id, float intensity, System.Numerics.Vector3 dir ) {
			WorldManager.AccCameraDir( id, intensity, dir );
		}

		public static void AccCameraAngle( ID id, float ddpitch, float ddyaw, float ddroll ) {
			WorldManager.AccCameraAngle( id, ddpitch, ddyaw, ddroll );
		}


		//RENDERING
		//internal static ID CreateRenderObject( Chunk chunk, Chunk? chunkNorth, Chunk? chunkSouth, Chunk? chunkEast, Chunk? chunkWest, Chunk? chunkTop, Chunk? chunkBottom, int xOffset, int yOffset, int zOffset ) =>
		//	RenderManager.CreateRenderObject( chunk, chunkNorth, chunkSouth, chunkEast, chunkWest, chunkTop, chunkBottom, xOffset, yOffset, zOffset );

		[Obsolete]
		internal static void DeleteRenderObject( ID id ) {
			RenderManager.DeleteRenderObject( id );
		}

		[Obsolete]
		internal static void RenderRenderObject( ID id ) {
			RenderManager.RenderRenderObject( id, Shaders.DefaultShaderId );
		}


		//CALLBACKS
		private static void OnWindowLoad() {
			Shaders.LoadShaders();
			ComputingManager.Init( GetContext(), _window!.Context.WindowPtr );
			OpenCLObjects.LoadOpenCLObjects();

			ComputingManager.EnqueueAquireGLObjects( OpenCLObjects.PixelBuffer );


			GL.Enable( EnableCap.DepthTest );
			GL.ClearColor( 0.1f, 0.3f, 0.8f, 1.0f );

			_game!.OnLoad();
		}

		private static void OnWindowMouseMove( MouseMoveEventArgs args ) {
			_game!.OnMouseMove( args.X, args.Y, args.DeltaX, args.DeltaY );
		}

		private static void OnWindowMouseButton( MouseButtonEventArgs args ) { }

		private static void OnWindowMouseScroll( MouseWheelEventArgs args ) { }

		private static void OnWindowKeyUp( KeyboardKeyEventArgs args ) {
			_game!.OnKeyStroke( ( Keys ) args.Key, KeyState.Up, args.IsRepeat );
		}

		private static void OnWindowKeyDown( KeyboardKeyEventArgs args ) {
			_game!.OnKeyStroke( ( Keys ) args.Key, KeyState.Down, args.IsRepeat );
		}

		private static readonly Timer _timer = new Timer();

		private static void OnWindowRenderTick( FrameEventArgs args ) {
#if DEBUG
			if ( _timer.Count > 1 ) {
				Console.WriteLine( $"Elapsed Time = {_timer.MeanTime}ms" );
				_timer.Restart();
			}
			else {
				_timer.AddSample( args.Time * 1000.0 );
			}
#endif

			GL.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit );

			WorldManager.RenderActiveWorlds();

			_game!.OnRenderTick( args.Time ); //Does the game need this?

			_window!.SwapBuffers();
		}

		private static void OnWindowUpdateTick( FrameEventArgs args ) {
			WorldManager.UpdateActiveWorlds();
			_game!.OnUpdateTick( args.Time );
		}

		private static void OnWindowResize( ResizeEventArgs args ) {
			GL.Viewport( 0, 0, args.Width, args.Height );
			_game!.OnWindowResize( args.Width, args.Height );
		}

		private static void OnWindowClose() {
			ComputingManager.Terminate();
		}
	}
}