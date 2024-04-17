using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            return Math.Clamp(value, min, max);
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
		public static (long, int) LimitSpeed((long, int) currentAction, (long, int) previousAction, int maxSpeed) {
			// Calculate the speed
			double d = Math.Abs(currentAction.Item2 - previousAction.Item2);
			double deltaTime = currentAction.Item1 - previousAction.Item1;
			double speed = (d / deltaTime) * 1000;

			// If speed exceeds the maximum allowed speed
			if (speed > maxSpeed) {
				// Adjust position to reduce speed
				// Calculate the maximum allowed distance for the given time to limit speed
				double maxDistance = maxSpeed * (deltaTime / 1000); // Convert deltaTime to seconds

				// Calculate the direction of movement
				int direction = currentAction.Item2 > previousAction.Item2 ? 1 : -1;

				// Calculate the new position to ensure the speed limit is not exceeded
				int newPos = previousAction.Item2 + (int)(maxDistance * direction);

				// Update the position
				currentAction.Item2 = newPos;

				// Recalculate the speed with the adjusted position
				d = Math.Abs(currentAction.Item2 - previousAction.Item2);
				speed = (d / deltaTime) * 1000;
			}

			// Update the speed in the current action
			// currentAction.Speed = speed;
			return currentAction;
		}
	}
}
