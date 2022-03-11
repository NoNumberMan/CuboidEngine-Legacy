package com.nonumberstudios;

import com.nonumberstudios.cuboid.CuboidEngine;
import com.nonumberstudios.cuboid.IGame;

public class TestGame implements IGame {
	public static void main( String[] args ) {
		CuboidEngine.init( new TestGame(), 1280, 720, "Cool Test Game" );

		while(true);
		/*
		while ( !glfwWindowShouldClose(windowHandle) ) {
			glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
			glfwSwapBuffers(windowHandle);
			glfwPollEvents();
			logger.info( "Hello World!" );
		}

		glfwFreeCallbacks(windowHandle);
		glfwDestroyWindow(windowHandle);

		glfwTerminate();
		glfwSetErrorCallback(null).free();*/
	}

	@Override
	public void onLoad() {

	}

	@Override
	public void onMouseMoveEvent( float x, float y, float dx, float dy ) {

	}

	@Override
	public void onMouseButtonEvent( int button, int action, int mods ) {

	}

	@Override
	public void onMouseScrollEvent( float dy ) {

	}

	@Override
	public void onKeyEvent( int key, int action, int mods ) {

	}

	@Override
	public void onCharEvent( int chr ) {

	}

	@Override
	public void onWindowResizeEvent( int width, int height ) {

	}

	@Override
	public void onRenderTickEvent( double dt ) {

	}

	@Override
	public void onUpdateTickEvent( double dt ) {

	}
}
