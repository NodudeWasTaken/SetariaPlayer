using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
	class VibrationConvert
	{
		private List<(long, double)> pos = new List<(long, double)>();
		private double dist { 
			get {
				return Config.cfg.vibrationBufferDuration;
			}
		}
		private double def = 0.10;
		public VibrationConvert() { }
		private void FilterDist((long, double) timestep) {
			pos = pos.Where((pos) => pos.Item1 + this.dist >= timestep.Item1).ToList();
		}
		/*private void FilterRange((long, double) timestep) {
			if (pos.Count > 5) {
				pos = pos.GetRange(pos.Count - 5, 5);
			}
		}
		private void FilterDiff((long, double) timestep) {
			for (int i = pos.Count; i > 0; i--) {
				if (Math.Abs(timestep.Item2 / pos[i].Item2) > this.dist) {
					pos = pos.GetRange(i, pos.Count - i);
					break;
				}
			}
		}*/
		public void Update((long, double) timestep) {
			//TODO: Dont need to do this every time
			//Even if it should be relatively inexpensive
			FilterDist(timestep);
			pos.Add(timestep);
		}
		public double Get()
		{
			if (pos.Count < 2) {
				return def;
			}

			List<double> result = new List<double>();

			//Convert to speed
			//Enables us to tell the intensity
			for (int i=1; i<pos.Count; i++) {
				var o = pos[i-1];
				var c = pos[i];
				//Distance over 20cm (0.2m, approx height of handy)
				double d1 = 0.2 * o.Item2;
				double d2 = 0.2 * c.Item2;
				//Speed in m pr second
				double m = Math.Abs(d1 - d2);
				double s = Math.Abs(o.Item1 - c.Item1) / 1000.0;
				//TODO: Fix divide by zero
				double ms = m / s;
				result.Add(ms);
			}

			//Filter if too different
			//Enables us to react to local changes
			double lastElem = result.Last();
			for (int i = result.Count-1; i > 0; i--) {
				if (Utilities.diff(lastElem, result[i]) > Config.cfg.vibrationDiff) {
					result = result.GetRange(i, result.Count - i);
					break;
				}
			}

			//Gets the local average (local intensity)
			//Assume max 2m/s
			double speed = result.Average() / Config.cfg.vibrationMaxSpeed;
			//Limit the max
			speed = Math.Min(speed, 1.0);
			//TODO: Fix divide by zero
			if (Double.IsNaN(speed)) {
				return def;
			}
			return speed;
		}
		public void Clear() {
			pos.Clear();
		}
	}
}
