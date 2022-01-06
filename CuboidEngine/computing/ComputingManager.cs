using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.OpenGL4;

namespace CuboidEngine {
	internal static class ComputingManager {
		private static          CLDevice?              _device;
		private static          CLContext?             _context;
		private static          CLCommandQueue?        _queue;
		private static readonly AssetManager<CLKernel> _kernels = new AssetManager<CLKernel>();
		private static readonly AssetManager<CLBuffer> _buffers = new AssetManager<CLBuffer>();

		public static long MemorySizeValue;
		public static long MaximumWorkGroupSizeValue;
		public static long MaximumWorkItemSize0;
		public static long MaximumWorkItemSize1;
		public static long MaximumWorkItemSize2;
		public static long MaximumWorkItemDimensionsValue;

		public static void Init( IntPtr glContext, IntPtr glPlatform ) {
			CLResultCode platformResult = CL.GetPlatformIds( out CLPlatform[] platforms );
			if ( platformResult != CLResultCode.Success ) throw new Exception( "Could not find OpenCL platform!" );

			SortedList<int, Tuple<CLDevice, long[]>> validDevices = new SortedList<int, Tuple<CLDevice, long[]>>();
			for ( int i = 0; i < platforms.Length; ++i ) {
				CLResultCode deviceResult = CL.GetDeviceIds( platforms[i], DeviceType.Gpu, out CLDevice[] devices );
				if ( deviceResult == CLResultCode.Success )
					for ( int j = 0; j < devices.Length; ++j ) {
						CL.GetDeviceInfo( devices[j], DeviceInfo.Vendor, out byte[] vendor );
						CL.GetDeviceInfo( devices[j], DeviceInfo.Available, out byte[] available );
						CL.GetDeviceInfo( devices[j], DeviceInfo.CompilerAvailable, out byte[] compilerAvailable );
						CL.GetDeviceInfo( devices[j], DeviceInfo.Extensions, out byte[] extensions );
						CL.GetDeviceInfo( devices[j], DeviceInfo.MaximumMemoryAllocationSize, out byte[] memorySize );
						CL.GetDeviceInfo( devices[j], DeviceInfo.ImageMaximumBufferSize, out byte[] imageMaximumBufferSize );
						CL.GetDeviceInfo( devices[j], DeviceInfo.ImageSupport, out byte[] imageSupport );
						CL.GetDeviceInfo( devices[j], DeviceInfo.MaximumClockFrequency, out byte[] maximumClockFrequency );
						CL.GetDeviceInfo( devices[j], DeviceInfo.MaximumWorkGroupSize, out byte[] maximumWorkGroupSize );
						CL.GetDeviceInfo( devices[j], DeviceInfo.MaximumWorkItemDimensions, out byte[] maximumWorkItemDimensions );
						CL.GetDeviceInfo( devices[j], DeviceInfo.MaximumWorkItemSizes, out byte[] maximumWorkItemSizes );

						long memorySizeValue                = BitConverter.ToInt64( memorySize );
						long maximumWorkGroupSizeValue      = BitConverter.ToInt64( maximumWorkGroupSize );
						long maximumWorkItemSize0           = BitConverter.ToInt64( maximumWorkItemSizes, 0 );
						long maximumWorkItemSize1           = BitConverter.ToInt64( maximumWorkItemSizes, 8 );
						long maximumWorkItemSize2           = BitConverter.ToInt64( maximumWorkItemSizes, 16 ); //TODO
						int  maximumClockFrequencyValue     = BitConverter.ToInt32( maximumClockFrequency );
						int  maximumWorkItemDimensionsValue = BitConverter.ToInt32( maximumWorkItemDimensions );

						int req = available[0] * compilerAvailable[0] * imageSupport[0] * ( maximumWorkItemDimensionsValue >= 2 ? 1 : 0 )
								* ( Encoding.Default.GetString( extensions ).Contains( "cl_khr_gl_sharing" ) ? 1 : 0 ) * ( memorySizeValue >= 500000000 ? 1 : 0 );
						if ( req == 0 ) continue;

						int h = 0;
						h += memorySizeValue >= 1000000000L ? memorySizeValue >= 2000000000L ? 10 : 5 : 0;
						h += maximumClockFrequencyValue >= 1000 ? maximumClockFrequencyValue >= 2000 ? 10 : 5 : 0;
						h += maximumWorkGroupSizeValue >= 512 ? maximumWorkGroupSizeValue >= 1024L ? 10 : 5 : 0;
						validDevices.TryAdd( h,
							new Tuple<CLDevice, long[]>( devices[j], new[] {memorySizeValue, maximumWorkGroupSizeValue, maximumWorkItemSize0, maximumWorkItemSize1, maximumWorkItemSize2, maximumWorkItemDimensionsValue} ) );
					}
			}

			if ( validDevices.Count == 0 ) throw new Exception( "Could not find valid OpenCL device!" );

			long[] values = validDevices[validDevices.Keys.Last()].Item2;
			_device                        = validDevices[validDevices.Keys.Last()].Item1;
			MemorySizeValue                = values[0];
			MaximumWorkGroupSizeValue      = values[1];
			MaximumWorkItemSize0           = values[2];
			MaximumWorkItemSize1           = values[3];
			MaximumWorkItemSize2           = values[4];
			MaximumWorkItemDimensionsValue = values[5];

			CL.GetDeviceInfo( _device!.Value, DeviceInfo.Vendor, out byte[] v2 );
			_context = CL.CreateContext(
				new[] {( IntPtr ) CLGL.ContextProperties.GlContextKHR, glContext, ( IntPtr ) CLGL.ContextProperties.WglHDCKHR, glPlatform, ( IntPtr ) ContextProperties.ContextPlatform, platforms[0].Handle, IntPtr.Zero},
				new CLDevice[] {_device!.Value}, IntPtr.Zero, IntPtr.Zero, out CLResultCode contextResult );

			if ( contextResult != CLResultCode.Success ) throw new Exception( "Could not create OpenCL context!" );
			_queue = CL.CreateCommandQueueWithProperties( _context.Value, _device!.Value.Handle, IntPtr.Zero, out CLResultCode queueResult );
			if ( queueResult != CLResultCode.Success ) throw new Exception( "Could not create OpenCL command queue!" );
		}

		public static void Terminate() {
			CL.ReleaseContext( _context!.Value );
			_context = null;
		}

		public static ID LoadKernelFromFiles( string kernelName, string[] kernelFiles ) {
			Debug.Assert( kernelFiles.Length > 0, "Kernel requires at least 1 source!" );
			string[] kernelSources = new string[kernelFiles.Length];
			for ( int i = 0; i < kernelFiles.Length; ++i ) {
				Debug.Assert( File.Exists( kernelFiles[i] ), $"File {kernelFiles[i]} does not exist!" );
				kernelSources[i] = File.ReadAllText( kernelFiles[i] );
			}

			return LoadKernelFromSources( kernelName, kernelSources );
		}

		public static ID LoadKernelFromSources( string kernelName, string[] kernelSources ) {
			IntPtr[] strPtr = new IntPtr[kernelSources.Length];
			uint[]   strLen = new uint[kernelSources.Length];
			for ( int i = 0; i < kernelSources.Length; ++i ) {
				strPtr[i] = Marshal.StringToHGlobalAuto( kernelSources[i] );
				strLen[i] = ( uint ) kernelSources[i].Length;
			}

			CLProgram program = CL.CreateProgramWithSource( _context!.Value, kernelSources[0], out CLResultCode programResult );
			Debug.Assert( programResult == CLResultCode.Success, $"Failed to create program!" );
			CL.BuildProgram( program, new CLDevice[] {_device!.Value}, string.Empty, ( evt, data ) => { } );
			ValidateProgram( program );

			CLKernel kernel = CL.CreateKernel( program, kernelName, out CLResultCode kernelResult );
			Debug.Assert( kernelResult == CLResultCode.Success, $"Failed to create kernel!" );

			ID id = _kernels.AddAsset( kernel );
			return id;
		}

		public static void UnloadKernel( ID id ) {
			CLKernel kernel = _kernels[id];
			CL.ReleaseKernel( kernel );
			_kernels.RemoveAsset( id );
		}

		public static void SetKernelArg<T>( ID id, uint index, T arg ) where T : unmanaged {
			CLKernel kernel = _kernels[id];
			CL.SetKernelArg( kernel, index, in arg );
		}

		public static void SetKernelArg( ID id, uint index, ID bufferId ) {
			CLKernel kernel = _kernels[id];
			CLBuffer buffer = _buffers[bufferId];
			CL.SetKernelArg( kernel, index, in buffer );
		}

		public static void RunKernel( ID id, int dim, int[] globalWorkSize, int[] localWorkSize ) {
			CLKernel kernel = _kernels[id];

			UIntPtr[] globalWorkSizeOffset = new UIntPtr[dim];
			UIntPtr[] globalWorkSizePtr    = new UIntPtr[dim];
			UIntPtr[] localWorkSizePtr     = new UIntPtr[dim];
			for ( int i = 0; i < dim; ++i ) {
				globalWorkSizeOffset[i] = UIntPtr.Zero;
				globalWorkSizePtr[i]    = ( UIntPtr ) globalWorkSize[i];
				localWorkSizePtr[i]     = ( UIntPtr ) localWorkSize[i];
			}

			CLResultCode result = CL.EnqueueNDRangeKernel( _queue!.Value, kernel, ( uint ) dim, globalWorkSizeOffset, globalWorkSizePtr, localWorkSizePtr, 0, null, out CLEvent evnt );
			CL.ReleaseEvent( evnt );

			Debug.Assert( result == CLResultCode.Success, "Failed to enqueue kernel!" );
		}

		public static ID CreateBuffer<T>( T[] data ) where T : unmanaged {
			Debug.Assert( _context != null, "OpenCL context does not exist!" );
			CLBuffer buffer = CL.CreateBuffer( _context!.Value, MemoryFlags.ReadOnly, data, out CLResultCode result );
			return _buffers.AddAsset( buffer );
		}

		public static ID CreateBuffer<T>( Span<T> data ) where T : unmanaged {
			Debug.Assert( _context != null, "OpenCL context does not exist!" );
			CLBuffer buffer = CL.CreateBuffer( _context!.Value, MemoryFlags.ReadOnly, data, out CLResultCode result ); //Does this upload the data?
			return _buffers.AddAsset( buffer );
		}

		public static ID CreateBuffer( int size, MemoryFlags flags = 0 ) {
			Debug.Assert( _context != null, "OpenCL context does not exist!" );
			CLBuffer buffer = CL.CreateBuffer( _context!.Value, flags, ( UIntPtr ) size, IntPtr.Zero, out CLResultCode result );
			return _buffers.AddAsset( buffer );
		}

		public static void EnqueueWriteBuffer<T>( ID id, int offset, Span<T> data ) where T : unmanaged {
			Debug.Assert( _context != null, "OpenCL context does not exist!" );
			CLBuffer     buffer = _buffers[id];
			CLResultCode result = CL.EnqueueWriteBuffer( _queue!.Value, buffer, false, ( UIntPtr ) offset, data, null, out CLEvent evnt );
			CL.ReleaseEvent( evnt );
			HandleCLResultCode( result );
		}

		public static unsafe void EnqueueWriteBuffer<T>( ID id, int offset, T[] data ) where T : unmanaged {
			Debug.Assert( _context != null, "OpenCL context does not exist!" );
			CLBuffer     buffer = _buffers[id];
			CLResultCode result = CL.EnqueueWriteBuffer( _queue!.Value, buffer, false, ( UIntPtr ) offset, data, null, out CLEvent evnt );
			CL.ReleaseEvent( evnt );
			HandleCLResultCode( result );
		}

		public static void EnqueueReadBuffer<T>( ID id, T[] data ) where T : unmanaged {
			Debug.Assert( _context != null, "OpenCL context does not exist!" );
			CLBuffer     buffer = _buffers[id];
			CLResultCode result = EnqueueReadBuffer( _queue!.Value, buffer, false, UIntPtr.Zero, data, null, out CLEvent evnt );
			CL.ReleaseEvent( evnt );
			HandleCLResultCode( result );
		}

		public static void EnqueueAquireGLObjects( ID id ) {
			Debug.Assert( _context != null, "OpenCL context does not exist!" );
			CLBuffer     buffer = _buffers[id];
			CLResultCode result = CLGL.EnqueueAcquireGLObjects( _queue!.Value, 1, new[] {buffer}, 0, null, out CLEvent evnt );
			CL.ReleaseEvent( evnt );
			HandleCLResultCode( result );
		}

		public static void EnqueueReleaseGLObjects( ID id ) {
			Debug.Assert( _context != null, "OpenCL context does not exist!" );
			CLBuffer     buffer = _buffers[id];
			CLResultCode result = CLGL.EnqueueReleaseGLObjects( _queue!.Value, 1, new[] {buffer}, 0, null, out CLEvent evnt );
			CL.ReleaseEvent( evnt );
			HandleCLResultCode( result );
		}

		public static void EnqueueFillBuffer<T>( ID id, int offset, int size, T fill ) where T : unmanaged {
			Debug.Assert( _context != null, "OpenCL context does not exist!" );
			CLBuffer     buffer = _buffers[id];
			CLResultCode result = CL.EnqueueFillBuffer( _queue!.Value, buffer, new T[] {fill}, ( UIntPtr ) offset, ( UIntPtr ) size, null, out CLEvent evnt );
			CL.ReleaseEvent( evnt );
			HandleCLResultCode( result );
		}

		public static ID CreateTextureBuffer( ID textureId ) {
			Debug.Assert( _context != null, "OpenCL context does not exist!" );
			CLBuffer textureBuffer = CLGL.CreateFromGLTexture( _context!.Value, MemoryFlags.ReadWrite, ( int ) TextureTarget.Texture2D, 0,
				TextureManager.GetTextureId( textureId ), out CLResultCode result );
			HandleCLResultCode( result );
			return _buffers.AddAsset( textureBuffer );
		}

		private static unsafe CLResultCode EnqueueReadBuffer<T>( //fix for opencl code
			CLCommandQueue commandQueue,
			CLBuffer buffer,
			bool blockingRead,
			UIntPtr offset,
			T[] array,
			CLEvent[] eventWaitList,
			out CLEvent eventHandle )
			where T : unmanaged {
			fixed ( T* objPtr = array ) {
				return CL.EnqueueReadBuffer( commandQueue, buffer, blockingRead, offset, ( UIntPtr ) ( ulong ) ( array.Length * sizeof( T ) ), ( IntPtr ) ( void* ) objPtr, eventWaitList != null ? ( uint ) eventWaitList.Length : 0U,
					eventWaitList,
					out eventHandle );
			}
		}

		public static void WaitForFinish() {
			CL.Finish( _queue!.Value );
		}

		public static void Flush() {
			CL.Flush( _queue!.Value );
		}


		[Conditional( "DEBUG" )]
		private static void ValidateProgram( CLProgram program ) {
			CLResultCode buildStatusInfoResult = CL.GetProgramBuildInfo( program, _device!.Value, ProgramBuildInfo.Log, out byte[] log );
			Debug.Assert( buildStatusInfoResult == CLResultCode.Success, "Failed to get build error log!" );

			if ( log.Length > 2 ) throw new Exception( Encoding.Default.GetString( log ) );
		}

		[Conditional( "DEBUG" )]
		private static void HandleCLResultCode( CLResultCode result ) {
			Debug.Assert( result == CLResultCode.Success, "OpenCL returned an error!" );
		}
	}
}