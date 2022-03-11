package com.nonumberstudios;

import com.nonumberstudios.cuboid.CuboidEngine;
import org.lwjgl.glfw.*;
import org.lwjgl.opengl.*;
import org.lwjgl.system.*;
import java.nio.*;
import static org.lwjgl.glfw.Callbacks.*; //like using namespace x
import static org.lwjgl.glfw.GLFW.*;
import static org.lwjgl.opengl.GL11.*;
import static org.lwjgl.system.MemoryStack.*;
import static org.lwjgl.system.MemoryUtil.*;

public class TestGame {
	public static void main( String[] args ) {
		CuboidEngine.init( null );
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
}
