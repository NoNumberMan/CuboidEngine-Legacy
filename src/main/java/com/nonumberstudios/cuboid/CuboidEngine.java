package com.nonumberstudios.cuboid;

import com.nonumberstudios.cuboid.debug.Debug;
import com.nonumberstudios.cuboid.debug.ExceptionHelper;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.io.IoBuilder;
import org.lwjgl.glfw.GLFWErrorCallback;
import org.lwjgl.glfw.GLFWVidMode;
import org.lwjgl.opengl.GL;
import org.lwjgl.system.MemoryStack;

import java.nio.IntBuffer;

import static org.lwjgl.glfw.GLFW.*;
import static org.lwjgl.opengl.GL11.glClearColor;
import static org.lwjgl.opengl.GL11.glViewport;
import static org.lwjgl.system.MemoryStack.stackPush;
import static org.lwjgl.system.MemoryUtil.NULL;

public final class CuboidEngine {
	public static final Logger LOGGER = LogManager.getLogger( "CUBOID" );

	private static CuboidEngine _instance = null;

	private final IGame _game;
	private final long _window;
	private float _cursorLastX;
	private float _cursorLastY;


	private CuboidEngine( IGame game, long window ) {
		_game = game;
		_window = window;
	}


	public static CuboidEngine getInstance() {
		Debug.doAssert( _instance != null, "Cuboid Engine was not initialized!" );
		return _instance;
	}

	public static void init( IGame game ) {
		_instance = new CuboidEngine( game, createWindow() );

		glfwSetCursorPosCallback( _instance._window, CuboidEngine::onMouseMoveEvent );
		glfwSetMouseButtonCallback( _instance._window, CuboidEngine::onMouseButtonEvent );
		glfwSetScrollCallback( _instance._window, CuboidEngine::onMouseScrollEvent );
		glfwSetKeyCallback( _instance._window, CuboidEngine::onKeyEvent );
		glfwSetCharCallback( _instance._window, CuboidEngine::onCharEvent );
		glfwSetWindowSizeCallback( _instance._window, CuboidEngine::onWindowResizeEvent );

		_instance._game.onLoad();
	}

	public static void init( IGame game, final int windowWidth, final int windowHeight, final String windowTitle ) {
		init( game );
		setWindowSize( windowWidth, windowHeight );
		setWindowTitle( windowTitle );
		showWindow();
	}

	public static void setWindowTitle( final String windowTitle ) {
		CuboidEngine engine = getInstance();
		glfwSetWindowTitle( engine._window, windowTitle );
	}

	public static void setWindowSize( final int windowWidth, final int windowHeight ) {
		CuboidEngine engine = getInstance();
		glfwSetWindowSize( engine._window, windowWidth, windowHeight );
	}

	public static void showWindow() {
		CuboidEngine engine = getInstance();
		glfwShowWindow( engine._window );
	}

	public static void setVSync( boolean enable ) {
		if ( enable ) glfwSwapInterval( 1 );
		else glfwSwapInterval( 0 );
	}


	private static long createWindow() {
		Debug.run( () -> GLFWErrorCallback.createPrint( IoBuilder.forLogger( LOGGER ).buildPrintStream() ).set() );
		LOGGER.debug( "Creating window..." );

		ExceptionHelper.HardAssert( glfwInit(), "Initialized glfw", "Unable to initialize GLFW!", IllegalStateException.class );

		glfwDefaultWindowHints();
		glfwWindowHint( GLFW_VISIBLE, GLFW_FALSE );
		glfwWindowHint( GLFW_RESIZABLE, GLFW_TRUE );
		Debug.run( () -> glfwWindowHint( GLFW_OPENGL_DEBUG_CONTEXT, GLFW_TRUE ) );

		final long windowHandle = glfwCreateWindow( 1, 1, "Cuboid Engine", NULL, NULL );
		ExceptionHelper.HardAssert( windowHandle != NULL, "Created window", "Failed to create the GLFW window!", RuntimeException.class );

		try ( MemoryStack stack = stackPush() ) {
			IntBuffer pWidth = stack.mallocInt( 1 );
			IntBuffer pHeight = stack.mallocInt( 1 );

			glfwGetWindowSize( windowHandle, pWidth, pHeight );

			GLFWVidMode vidmode = glfwGetVideoMode( glfwGetPrimaryMonitor() );

			glfwSetWindowPos(
					windowHandle,
					( vidmode.width() - pWidth.get( 0 ) ) / 2,
					( vidmode.height() - pHeight.get( 0 ) ) / 2
			);
		}

		glfwMakeContextCurrent( windowHandle );
		glfwSwapInterval( 1 );

		GL.createCapabilities(); //similar to glewInit();
		glClearColor( 1.0f, 0.0f, 0.0f, 0.0f );
		return windowHandle;
	}

	private static void onKeyEvent( final long window, final int key, final int scancode, final int action, final int mods ) {
		CuboidEngine engine = getInstance();
		//TODO add Nuklear
		engine._game.onKeyEvent( key, action, mods );
	}

	private static void onMouseMoveEvent( final long window, final double x, final double y ) {
		CuboidEngine engine = getInstance();
		final float xf = ( float ) x;
		final float yf = ( float ) y;
		final float dx = xf - engine._cursorLastX;
		final float dy = yf - engine._cursorLastY;
		engine._cursorLastX = xf;
		engine._cursorLastY = yf;
		engine._game.onMouseMoveEvent( xf, yf, dx, dy );
	}

	private static void onMouseButtonEvent( final long window, final int button, final int action, final int mods ) {
		CuboidEngine engine = getInstance();
		//TODO add Nuklear
		engine._game.onMouseButtonEvent( button, action, mods );
	}

	private static void onMouseScrollEvent( final long window, final double dx, final double dy ) {
		CuboidEngine engine = getInstance();
		//TODO add Nuklear
		engine._game.onMouseScrollEvent( ( float ) dy );
	}

	private static void onCharEvent( final long window, final int chr ) {
		CuboidEngine engine = getInstance();
		//TODO add Nuklear
		engine._game.onCharEvent( chr );
	}

	private static void onWindowResizeEvent( final long window, final int width, final int height ) {
		CuboidEngine engine = getInstance();
		glViewport( 0, 0, width, height );
		engine._game.onWindowResizeEvent( width, height );
	}
}
