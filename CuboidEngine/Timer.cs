using System.Collections.Generic;
using System.Linq;

namespace CuboidEngine
{
	public class Timer
	{
		private readonly List<double> _samples = new List<double>();

		public long Count => _samples.Count;
		public double MeanTime => _samples.Average();


		public void AddSample( double dt ) {
			_samples.Add( dt );
		}

		public void Restart() {
			_samples.Clear();
		}
	}
}