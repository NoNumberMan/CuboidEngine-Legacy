using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using OpenTK.Compute.OpenCL;

namespace CuboidEngine
{
	internal static class ComputingManager
	{
		private static CLDevice? _device;
		private static CLContext? _context;
		private static CLCommandQueue? _queue;
		private static readonly AssetManager<CLKernel> _kernels = new AssetManager<CLKernel>();
		private static readonly Dictionary<ID, Queue<CLEvent>> _events = new Dictionary<ID, Queue<CLEvent>>();

		public static void Init() {
			CLResultCode platformResult = CL.GetPlatformIds( out CLPlatform[] platforms );
			if ( platformResult != CLResultCode.Success ) throw new Exception( "Could not find OpenCL platform!" );

			SortedList<int, CLDevice> validDevices = new SortedList<int, CLDevice>();
			for ( int i = 0; i < platforms.Length; ++i ) {
				CLResultCode deviceResult = CL.GetDeviceIds( platforms[i], DeviceType.Gpu, out CLDevice[] devices );
				if ( deviceResult == CLResultCode.Success ) {
					for ( int j = 0; j < devices.Length; ++j ) {
						//CL.GetDeviceInfo( devices[j], DeviceInfo.Name, out byte[] paramValue ); TODO
						CL.GetDeviceInfo( devices[j], DeviceInfo.Vendor, out byte[] vendor );

						int h = 0;
						Console.WriteLine( System.Text.Encoding.Default.GetString( vendor ) );
						if ( System.Text.Encoding.Default.GetString( vendor ) == "NVIDIA Corporation" ) h += 5; //TODO
						validDevices.Add( h, devices[j] );
					}
				}
			}

			if ( validDevices.Count == 0 ) throw new Exception( "Could not find valid OpenCL device!" );
			
			_device = validDevices[validDevices.Keys.Last()];
			_context = CL.CreateContext( IntPtr.Zero, new CLDevice[] { _device!.Value }, IntPtr.Zero, IntPtr.Zero, out CLResultCode contextResult );
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
			uint[] strLen = new uint[kernelSources.Length];
			for ( int i = 0; i < kernelSources.Length; ++i ) {
				strPtr[i] = Marshal.StringToHGlobalAuto( kernelSources[i] );
				strLen[i] = ( uint ) kernelSources[i].Length;
			}

			CLProgram program = CL.CreateProgramWithSource( _context!.Value, ( uint ) kernelSources.Length, strPtr, strLen, out CLResultCode programResult );
			Debug.Assert( programResult == CLResultCode.Success, $"Failed to create program!" );
			CL.BuildProgram( program, new CLDevice[] { _device!.Value }, String.Empty, null );
			ValidateProgram( program );

			CLKernel kernel = CL.CreateKernel( program, kernelName, out CLResultCode kernelResult );
			Debug.Assert( kernelResult == CLResultCode.Success, $"Failed to create kernel!" );

			ID id = _kernels.AddAsset( kernel );
			_events.Add( id, new Queue<CLEvent>() );
			return id;
		}

		public static void UnloadKernel( ID id ) {
			CLKernel kernel = _kernels[id];
			CL.ReleaseKernel( kernel );
			_kernels.RemoveAsset( id );
			_events.Remove( id );
		}

		public static void SetKernelArg<T>( ID id, uint index, T arg ) where T : unmanaged {
			CLKernel kernel = _kernels[id];
			CL.SetKernelArg( kernel, index, in arg );
		}

		public static void RunKernel( ID id, int dim, int[] globalWorkSize, int[] localWorkSize) {
			CLKernel kernel = _kernels[id];

			UIntPtr[] globalWorkSizeOffset = new UIntPtr[dim];
			UIntPtr[] globalWorkSizePtr = new UIntPtr[dim];
			UIntPtr[] localWorkSizePtr = new UIntPtr[dim];
			for ( int i = 0; i < dim; ++i ) {
				globalWorkSizePtr[i] = ( UIntPtr ) globalWorkSize[i];
				localWorkSizePtr[i] = ( UIntPtr ) localWorkSize[i];
			}

			CLResultCode result = CL.EnqueueNDRangeKernel( _queue!.Value, kernel, ( uint ) dim, globalWorkSizeOffset, globalWorkSizePtr, localWorkSizePtr, 0, null, out CLEvent evnt );
			Debug.Assert( result == CLResultCode.Success, "Failed to enqueue kernel!" );
			_events[id].Enqueue( evnt );
		}

		public static void WaitForEvents( ID id ) {
			while ( _events[id].TryDequeue( out CLEvent evnt ) ) CL.WaitForEvents( 1, new[] { evnt } );
		}

		[System.Diagnostics.Conditional( "DEBUG" )]
		private static void ValidateProgram( CLProgram program ) {
			CLResultCode buildStatusResult = CL.GetProgramBuildInfo( program, _device!.Value, ProgramBuildInfo.Status, out byte[] code );
			if ( buildStatusResult != CLResultCode.Success ) {
				CLResultCode buildStatusInfoResult = CL.GetProgramBuildInfo( program, _device!.Value, ProgramBuildInfo.Log, out byte[] log );
				Debug.Assert( buildStatusInfoResult == CLResultCode.Success, "Failed to get build error log!" );
				throw new Exception( System.Text.Encoding.Default.GetString( log ) );
			}
		}
	}
}