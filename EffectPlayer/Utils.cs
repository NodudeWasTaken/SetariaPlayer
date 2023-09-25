using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetariaPlayer.EffectPlayer
{
    class Utils
    {
        public static long UnixTimeMS()
        {
            return ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
        }
        public static double diff(double b, double n)
        {
            return Math.Abs(n - b) / b;
        }
        public static double NormalizedApproach(double value, double max, double coefficient = 12.0)
        {
            double sigmoidInput = value / max;
            double result = 1.0 / (1.0 + Math.Exp(-sigmoidInput * coefficient)); // Adjust coefficient for desired curve
            return result;
        }
        public static double Limit(double value, double min, double max)
        {
            return Math.Max(Math.Min(value, max), min);
        }
		public static int InterpolateHeight((long, int) point1, (long, int) point2, long duration) {
			long t1 = point1.Item1;
			long t2 = point2.Item1;
			int h1 = point1.Item2;
			int h2 = point2.Item2;

			double alpha = (double)(duration - t1) / (t2 - t1);
			int interpolatedHeight = (int)(h1 + alpha * (h2 - h1));
			return interpolatedHeight;
		}

	}
}
