using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace SetariaPlayer
{
	class Utilities
	{
		public static long curtime() {
			return ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
		}
		public static double diff(double b, double n) {
			return Math.Abs(n - b) / b;
		}
		public static double NormalizedApproach(double value, double max, double coefficient=12.0) {
			double sigmoidInput = value / max;
			double result = 1.0 / (1.0 + Math.Exp(-sigmoidInput * coefficient)); // Adjust coefficient for desired curve
			return result;
		}
		public static double Limit(double value, double min, double max) {
			return Math.Max(Math.Min(value, max), min);
		}
	}
}
