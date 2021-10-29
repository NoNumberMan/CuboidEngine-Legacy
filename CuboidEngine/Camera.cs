using System;
using OpenTK.Mathematics;

namespace CuboidEngine
{
	internal class Camera
	{
		private Vector3 _pos;
		private Vector3 _vel;

		private Vector3 _ang;
		private Vector3 _angvel;

		private float _velmul    = 0.024f;
		private float _angvelmul = 0.024f;
		private float _drag      = 0.02f;


		public Camera( float x, float y, float z, float pitch, float yaw, float roll ) {
			_pos    = new Vector3( x, y, z );
			_ang    = new Vector3( pitch, yaw, roll );
			_vel    = Vector3.Zero;
			_angvel = Vector3.Zero;
		}

		public Camera() : this( 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f ) { }


		public void Update() {
			_pos += _velmul * _vel;
			_ang += _angvelmul * _angvel;

			_vel    *= ( 1.0f - _drag );
			_angvel *= ( 1.0f - _drag );
		}

		public Matrix4 GetMatrix() {
			return ( Matrix4.CreateTranslation( _pos ) * Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( _ang ) ) );
		}

		public Vector3 GetDirection() {
			return ( Vector4.UnitZ * GetMatrix().Inverted() ).Xyz;
		}

		public Vector3 GetPosition() {
			return _pos;
		}

		public void SetPosition( float x, float y, float z ) {
			_pos = new Vector3( x, y, z );
		}

		public void SetVelocity( float dx, float dy, float dz ) {
			_vel = new Vector3( dx, dy, dz );
		}

		public void SetAngle( float pitch, float yaw, float roll ) {
			_ang = new Vector3( pitch, yaw, roll );
		}

		public void SetAngularVelocity( float dpitch, float dyaw, float droll ) {
			_angvel = new Vector3( dpitch, dyaw, droll );
		}

		public void AccDir( float intensity, Vector3 dir ) {
			_vel += intensity * dir;
		}

		public void AccAngle( float ddpitch, float ddyaw, float ddroll ) {
			_angvel += new Vector3( ddpitch, ddyaw, ddroll );
		}
	}
}