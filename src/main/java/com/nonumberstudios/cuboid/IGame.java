package com.nonumberstudios.cuboid;

public interface IGame {
	String getTitle();
	void onLoad();
	void onMouseMove( float x, float y, float dx, float dy );
	void onMouseButton();
	void onMouseScroll();
	void onKeyStroke( int key, int state, boolean isRepeat );
	void onRenderTick( double dt );
	void onUpdateTick( double dt );
	void onWindowResize( float width, float height );
}
