package com.nonumberstudios.cuboid.debug;

import com.nonumberstudios.cuboid.CuboidEngine;

import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;

public final class ExceptionHelper {

	private ExceptionHelper() { }

	public static <T extends RuntimeException> void HardAssert( final boolean expression, final String success, final String failure, final Class<T> exception ) {
		if ( expression ) {
			CuboidEngine.LOGGER.debug( success );
		}
		else {
			CuboidEngine.LOGGER.fatal( failure );

			try {
				Constructor<T> constructor = exception.getConstructor( String.class );
				throw constructor.newInstance( failure );
			} catch( NoSuchMethodException | InvocationTargetException | InstantiationException | IllegalAccessException e ) {
				throw new RuntimeException( "Failed to throw exception for failure: " + failure );
			}
		}
	}
}
