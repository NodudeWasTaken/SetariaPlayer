using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetariaPlayer
{
	internal class Config
	{
		public class ConfigInt {
			public int vibrationBufferDuration = 5000;
			public double vibrationUpdateDiff = 0.2;
			public double vibrationCalcDiff = 0.15;
			public double vibrationMaxSpeed = 2;
			public ConfigInt() {
				//TODO: Load or create config file
			}
		}
		public static ConfigInt cfg = new ConfigInt();
	}
}
