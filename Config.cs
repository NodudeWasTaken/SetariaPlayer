using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
	internal class Config
	{
		public class ConfigInt {
			public int vibrationBufferDuration = 5000;
			public double vibrationDiff = 0.2;
			public double vibrationMaxSpeed = 2;
			public ConfigInt() {
				//TODO: Load or create config file
			}
		}
		public static ConfigInt cfg = new ConfigInt();
	}
}
