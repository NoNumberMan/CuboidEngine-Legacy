package com.nonumberstudios.cuboid.debug;

import com.nonumberstudios.cuboid.CuboidEngine;

import java.util.function.Function;

public final class Debug {
	private static final boolean DISABLE_ON_DEBUG = false;
	private static final boolean ENABLE_ON_RELEASE = false;

	public static final boolean ENABLED = !DISABLE_ON_DEBUG && ( ENABLE_ON_RELEASE ||
			java.lang.management.ManagementFactory.getRuntimeMXBean().getInputArguments().toString().contains( "-agentlib:jdwp" ) );


	private Debug() {
	}


	public static void doAssert( boolean expression ) {
		if ( ENABLED ) {
			if ( !expression ) {
				CuboidEngine.LOGGER.fatal( "Assertion failed!" );
				throw new AssertionError();
			}
		}
	}

	public static void doAssert( boolean expression, String msg ) {
		if ( ENABLED ) {
			if ( !expression ) {
				CuboidEngine.LOGGER.fatal( msg );
				throw new AssertionError( msg );
			}
		}
	}

	public static void doAssert( AssertExpression expression ) {
		if ( ENABLED ) {
			if ( !expression.eval() ) {
				CuboidEngine.LOGGER.fatal( "Assertion failed!" );
				throw new AssertionError();
			}
		}
	}

	public static void doAssert( AssertExpression expression, String msg ) {
		if ( ENABLED ) {
			if ( !expression.eval() ) {
				CuboidEngine.LOGGER.fatal( msg );
				throw new AssertionError( msg );
			}
		}
	}


	public static void run( RunExpression expression ) {
		if ( ENABLED ) {
			expression.run();
		}
	}


	@FunctionalInterface
	public static interface AssertExpression {
		boolean eval();
	}

	@FunctionalInterface
	public static interface RunExpression {
		void run();
	}
}
