using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
	class Utilities
	{
		public static long curtime()
		{
			return ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
		}
		public static double diff(double b, double n)
		{
			return Math.Abs(n - b) / b;
		}
	}
}
