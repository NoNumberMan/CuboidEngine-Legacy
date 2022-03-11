package com.nonumberstudios.cuboid;

public interface IGame {
	void onLoad();
	void onMouseMoveEvent( float x, float y, float dx, float dy );
	void onMouseButtonEvent( final int button, final int action, final int mods );
	void onMouseScrollEvent( final float dy );
	void onKeyEvent( final int key, final int action, final int mods );
	void onCharEvent( final int chr );
	void onWindowResizeEvent( int width, int height );
	void onRenderTickEvent( double dt );
	void onUpdateTickEvent( double dt );
}
