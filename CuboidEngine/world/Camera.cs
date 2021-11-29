using System;
using OpenTK.Mathematics;

namespace CuboidEngine {
	internal class Camera {
		public Vector3 Pos { get; set; }
		public Vector3 Vel { get; set; }

		public Vector3 Ang    { get; set; }
		public Vector3 Angvel { get; set; }

		public Vector2 Size { get; set; }


		private float Velmul    = 0.024f;
		private float Angvelmul = 0.024f;
		private float _drag     = 0.02f;


		public Camera( float x, float y, float z, float pitch, float yaw, float roll ) {
			Pos    = new Vector3( x, y, z );
			Ang    = new Vector3( pitch, yaw, roll );
			Vel    = Vector3.Zero;
			Angvel = Vector3.Zero;
			Size   = new Vector2( 256, 256 );
		}

		public Camera() : this( 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f ) { }


		public void Update() {
			Pos += Velmul * Vel;
			Ang += Angvelmul * Angvel;

			Vel    *= 1.0f - _drag;
			Angvel *= 1.0f - _drag;
		}

		public Matrix4 GetMatrix() {
			return Matrix4.CreateTranslation( Pos ) * Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( Ang ) );
		}

		public Vector3 GetDirection() {
			return ( Vector4.UnitZ * GetMatrix().Inverted() ).Xyz;
		}

		public void SetPosition( float x, float y, float z ) {
			Pos = new Vector3( x, y, z );
		}

		public void SetVelocity( float dx, float dy, float dz ) {
			Vel = new Vector3( dx, dy, dz );
		}

		public void SetSize( float width, float height ) {
			Size = new Vector2( width, height );
		}

		public void SetAngle( float pitch, float yaw, float roll ) {
			Ang = new Vector3( pitch, yaw, roll );
		}

		public void SetAngularVelocity( float dpitch, float dyaw, float droll ) {
			Angvel = new Vector3( dpitch, dyaw, droll );
		}

		public void AccDir( float intensity, Vector3 dir ) {
			Vel += intensity * dir;
		}

		public void AccAngle( float ddpitch, float ddyaw, float ddroll ) {
			Angvel += new Vector3( ddpitch, ddyaw, ddroll );
		}
	}
}