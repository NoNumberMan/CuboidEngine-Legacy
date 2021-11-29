namespace CuboidEngine {
	public interface IGame {
		public string GetTitle();


		public void OnLoad();
		public void OnMouseMove( float x, float y, float dx, float dy );
		public void OnMouseButton();
		public void OnMouseScroll();
		public void OnKeyStroke( Keys key, KeyState state, bool isRepeat );
		public void OnRenderTick( double dt );
		public void OnUpdateTick( double dt );
		public void OnWindowResize( float width, float height );
	}
}