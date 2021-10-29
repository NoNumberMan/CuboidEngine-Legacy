using System;
using System.Numerics;
using CuboidEngine;

namespace CuboidGame
{
	public class Program : IGame
	{
		public static void Main( string[] args ) {
			CEngine.Init( new Program() );
		}

		public string GetTitle() {
			return "Test Program";
		}

		private ID _world;
		public void OnLoad() {
			_world = CEngine.GenerateWorld( WorldGenerators.FlatWorld );
			CEngine.SetWorldActive( _world );
			//load world
		}

		public void OnMouseMove( float x, float y, float dx, float dy ) {
			if ( CEngine.IsMouseButtonDown( MouseButton.Left ) )
				CEngine.AccCameraAngle( _world!, -0.005f * dy, -0.005f * dx, 0.0f );
		}

		public void OnMouseButton() { }

		public void OnMouseScroll() { }

		public void OnKeyStroke( Keys key, KeyState state, bool isRepeat ) {
			if ( key == Keys.W && state == KeyState.Down ) {
				CEngine.GetCameraDirection( _world!, out Vector3 dir );
				CEngine.AccCameraDir( _world!, 0.5f, dir );
			}

			if ( key == Keys.S && state == KeyState.Down ) {
				CEngine.GetCameraDirection( _world!, out Vector3 dir );
				CEngine.AccCameraDir( _world!, 0.5f, -dir );
			}

			if ( key == Keys.A && state == KeyState.Down ) {
				CEngine.GetCameraDirection( _world!, out Vector3 forward );
				Vector3 dir = Vector3.Cross( forward, Vector3.UnitY );
				CEngine.AccCameraDir( _world!, 0.5f, dir );
			}

			if ( key == Keys.D && state == KeyState.Down ) {
				CEngine.GetCameraDirection( _world!, out Vector3 forward );
				Vector3 dir = Vector3.Cross( forward, Vector3.UnitY );
				CEngine.AccCameraDir( _world!, 0.5f, -dir );
			}

			if ( key == Keys.Space && state == KeyState.Down ) {
				CEngine.AccCameraDir( _world!, 0.5f, -Vector3.UnitY );
			}

			if ( key == Keys.LeftShift && state == KeyState.Down ) {
				CEngine.AccCameraDir( _world!, 0.5f, Vector3.UnitY );
			}
		}

		public void OnRenderTick( double dt ) {
			//render world
		}

		public void OnUpdateTick( double dt ) {
			//do nothing
			//CEngine.DeleteWorld( world );
			//world = CEngine.GenerateWorld( WorldGenerators.FlatWorld );
		}
	}
}