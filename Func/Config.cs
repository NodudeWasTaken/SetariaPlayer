﻿using System;
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
			public string intifaceUrl { get; set; } = "ws://localhost:12345";
			public string scriptPath { get; set; } = "rec.funscript";
			public int vibrationBufferDuration { get; set; } = 5000;
			public double vibrationUpdateDiff { get; set; } = 0.2;
			public double vibrationCalcDiff { get; set; } = 0.15;
			public double vibrationMaxSpeed { get; set; } = 2;
			public double strokeMax { get; set; } = 1.0;
			public double strokeMin { get; set; } = 0;
			public double strokeAccelMax { get; set; } = 20; //TODO: Implement
			public bool filler { get; set; } = false;
			public int fillerDur { get; set; } = 300;
			public int fillerHeight { get; set; } = 15;
			public int fillerModTime { get; set; } = 650;
			public int fillerAModFireLength { get; set; } = 140;
			public int fillerAModFireHeight { get; set; } = 10;
			public int fillerAModLazerLength { get; set; } = 120;
			public int fillerAModLazerHeight { get; set; } = 10;
			public int fillerAModDamageLength { get; set; } = 100;
			public int fillerAModDamageHeight { get; set; } = 10;
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
