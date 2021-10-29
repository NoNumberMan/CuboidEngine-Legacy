namespace CuboidEngine
{
	internal static class Shaders
	{
		public static ID DefaultShaderId { get; private set; } = new ID( -1 );
		public static ID ScreenShaderId { get; private set; } = new ID( -1 );

		public static void LoadShaders() {
			DefaultShaderId = CEngine.LoadShaderProgramFromFile( new string[] {
				"../../../../CuboidEngine/assets/shaders/vertex_shader.glsl",
				"../../../../CuboidEngine/assets/shaders/fragment_shader.glsl", 
				"../../../../CuboidEngine/assets/shaders/geometry_shader.glsl" } );
			ScreenShaderId = CEngine.LoadShaderProgramFromFile( new string[] {
				"../../../../CuboidEngine/assets/shaders/vertex_shader_screen.glsl",
				"../../../../CuboidEngine/assets/shaders/fragment_shader_screen.glsl"
			} );
		}
	}
}