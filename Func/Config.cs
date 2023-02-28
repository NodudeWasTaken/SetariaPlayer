using System;
using System.Collections.Generic;
using System.ComponentModel;
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
			public string intifaceUrl { get; set; } = "";
			public string scriptPath { get; set; } = "rec.funscript";
			public int vibrationBufferDuration { get; set; } = 5000;
			public double vibrationUpdateDiff { get; set; } = 0.2;
			public double vibrationCalcDiff { get; set; } = 0.15;
			public double vibrationMaxSpeed { get; set; } = 2;
			public bool filler { get; set; } = false;
			public int fillerDur { get; set; } = 300;
			public int fillerHeight { get; set; } = 15;
			public int fillerModTime { get; set; } = 650;
			public double fillerModFireSpeed { get; set; } = 1.25;
			public double fillerModFireHeight { get; set; } = 1.25;
			public double fillerModLazerSpeed { get; set; } = 1.5;
			public double fillerModLazerHeight { get; set; } = 1.30;
			public double fillerModDamageSpeed { get; set; } = 1.5;
			public double fillerModDamageHeight { get; set; } = 1.45;
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

		public static ConfigInt cfg;
	}
}
