using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK.Mathematics;

namespace CuboidEngine
{
	internal static class WorldManager
	{
		private static readonly AssetManager<World> _worlds       = new AssetManager<World>();
		private static readonly List<ID>            _activeWorlds = new List<ID>();

		public static ID GenerateWorld( IWorldGenerator generator ) {
			return _worlds.AddAsset( new World() );
		}

		public static void RemoveWorld( ID id ) {
			_worlds.RemoveAsset( id );
		}

		public static void SetWorldActive( ID id ) {
			_activeWorlds.Add( id );
		}

		public static void SetWorldInactive( ID id ) {
			Debug.Assert( _activeWorlds.Contains( id ), $"World with id {id} was not active!" );
			_activeWorlds.Remove( id );
		}

		public static void UpdateActiveWorlds() {
			for ( int i = 0; i < _activeWorlds.Count; ++i ) _worlds[_activeWorlds[i]].Update();
		}

		public static void RenderActiveWorlds() {
			for ( int i = 0; i < _activeWorlds.Count; ++i ) _worlds[_activeWorlds[i]].Render();
		}



		//camera
		public static void GetCameraMatrix( ID id, out Matrix4 matrix ) {
			matrix = _worlds[id].Camera.GetMatrix();
		}

		public static void GetCameraDirection( ID id, out System.Numerics.Vector3 dir ) {
			( float x, float y, float z ) = _worlds[id].Camera.GetDirection();
			dir                           = new System.Numerics.Vector3( x, y, z );
		}

		public static void SetCameraPosition( ID id, float x, float y, float z ) {
			_worlds[id].Camera.SetPosition( x, y, z );
		}

		public static void SetCameraVelocity( ID id, float dx, float dy, float dz ) {
			_worlds[id].Camera.SetVelocity( dx, dy, dz );
		}

		public static void SetCameraAngle( ID id, float pitch, float yaw, float roll ) {
			_worlds[id].Camera.SetAngle( pitch, yaw, roll );
		}

		public static void SetCameraAngularVelocity( ID id, float dpitch, float dyaw, float droll ) {
			_worlds[id].Camera.SetAngularVelocity( dpitch, dyaw, droll );
		}

		public static void AccCameraDir( ID id, float intensity, System.Numerics.Vector3 dir ) {
			_worlds[id].Camera.AccDir( intensity, new Vector3( dir.X, dir.Y, dir.Z ) );
		}

		public static void AccCameraAngle( ID id, float ddpitch, float ddyaw, float ddroll ) {
			_worlds[id].Camera.AccAngle( ddpitch, ddyaw, ddroll );
		}
	}
}