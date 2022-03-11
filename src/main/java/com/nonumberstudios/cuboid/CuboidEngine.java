package com.nonumberstudios.cuboid;

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
import static org.lwjgl.system.MemoryStack.stackPush;
import static org.lwjgl.system.MemoryUtil.NULL;

public final class CuboidEngine {
	public static final Logger LOGGER = LogManager.getLogger( "CUBOID" );
	public static final boolean DEBUG = java.lang.management.ManagementFactory.getRuntimeMXBean().getInputArguments().toString().contains("-agentlib:jdwp");
	private static IGame _game;

	private CuboidEngine() {
	}

	private static long createWindow( final int width, final int height, final String title ) {
		if ( DEBUG ) GLFWErrorCallback.createPrint( IoBuilder.forLogger( LOGGER ).buildPrintStream() ).set();

		if ( !glfwInit() ) throw new IllegalStateException( "Unable to initialize GLFW" );

		glfwDefaultWindowHints();
		glfwWindowHint( GLFW_VISIBLE, GLFW_FALSE );
		glfwWindowHint( GLFW_RESIZABLE, GLFW_TRUE );
		if ( DEBUG ) glfwWindowHint( GLFW_OPENGL_DEBUG_CONTEXT, GLFW_TRUE );

		long windowHandle = glfwCreateWindow( 1280, 720, "Hello World!", NULL, NULL );
		if ( windowHandle == NULL ) throw new RuntimeException( "Failed to create the GLFW window" );

		glfwSetKeyCallback( windowHandle, ( window, key, scancode, action, mods ) -> {
			if ( key == GLFW_KEY_ESCAPE && action == GLFW_RELEASE )
				glfwSetWindowShouldClose( window, true );
		} );

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
		glfwShowWindow( windowHandle );

		GL.createCapabilities(); //similar to glewInit();
		glClearColor( 1.0f, 0.0f, 0.0f, 0.0f );
		return windowHandle;
	}

	public static void init( IGame game ) {
		_game = game;

		LOGGER.info( "Hello World!" );
		LOGGER.error( "Hello World!" );
		createWindow( 10, 10, "1" );

		/*_window = new GameWindow( gws, nws );

		_window.VSync       =  VSyncMode.Off;
		_window.Load        += OnWindowLoad;
		_window.MouseMove   += OnWindowMouseMove;
		_window.MouseUp     += OnWindowMouseButton;
		_window.MouseDown   += OnWindowMouseButton;
		_window.MouseWheel  += OnWindowMouseScroll;
		_window.KeyUp       += OnWindowKeyUp;
		_window.KeyDown     += OnWindowKeyDown;
		_window.RenderFrame += OnWindowRenderTick;
		_window.UpdateFrame += OnWindowUpdateTick;
		_window.Resize      += OnWindowResize;
		_window.Closed      += OnWindowClose;
		_window.Run();*/
	}
}
