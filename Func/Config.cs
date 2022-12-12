using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace SetariaPlayer
{
	internal class Config {
		private static string dataf = "config.json";

		public class ConfigInt {
			public int vibrationBufferDuration { get; set; } = 5000;
			public double vibrationUpdateDiff { get; set; } = 0.2;
			public double vibrationCalcDiff { get; set; } = 0.15;
			public double vibrationMaxSpeed { get; set; } = 2;
			public void save() {
				Trace.WriteLine("Config save!");
				string output = JsonSerializer.Serialize(this); ;
				File.WriteAllText(dataf, output);
			}
		}

		public static ConfigInt load() {
			Trace.WriteLine("Config load!");
			if (!File.Exists(dataf)) {
				ConfigInt c = new ConfigInt();
				string output = JsonSerializer.Serialize<ConfigInt>(c); ;
				File.WriteAllText(dataf, output);
				return c;
			}

			using (var reader = new StreamReader(dataf)) {
				ConfigInt data = JsonSerializer.Deserialize<ConfigInt>(reader.ReadToEnd());
				return data;
			}
		}

		public static ConfigInt cfg = Config.load();
	}
}
