using System;

namespace CuboidEngine
{
	public enum Uniforms
	{
		ProjectionMatrix,
		CameraMatrix,
		TransformationMatrix,
		CameraDirectionVector
	}

	internal static class UniformHelper
	{
		public static string GetUniformName( Uniforms matrixType ) {
			return matrixType switch {
				Uniforms.ProjectionMatrix     => "proj",
				Uniforms.CameraMatrix         => "cam",
				Uniforms.TransformationMatrix => "trans",
				Uniforms.CameraDirectionVector => "cam_dir",
				_                                    => throw new ArgumentOutOfRangeException( nameof ( matrixType ), matrixType, null )
			};
		}
	}
}