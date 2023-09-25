using System;
using System.Collections;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using System.Xml.Linq;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.Diagnostics;

namespace SetariaPlayer
{
	public class Data
	{
		public virtual string Name { get; set; }
		public virtual string Scene { get; set; }
		public virtual long Start { get; set; }
		public virtual long End { get; set; }
		public virtual bool Loop { get; set; }
		public virtual List<(long, int)> Actions { get; set; }
		public Data(
			string Name, 
			string Scene, 
			long Start, 
			long End, 
			bool Loop, 
			List<(long, int)> Actions
		) {
			this.Name = Name;
			this.Scene = Scene;
			this.Start = Start;
			this.End = End;
			this.Loop = Loop;
			this.Actions = Actions;
		}
		public string GetId() {
			return $"{Name}_{Scene}";
		}
		public long Duration() {
			return this.End - this.Start;
		}
	}
	public class Funscript
	{
		public List<Dictionary<string, long>> actions { get; set; }
		public bool inverted { get; set; }
		public Dictionary<string,object> metadata { get; set; }
		public int range { get; set; }
		public string version { get; set; }
	}
	class ScriptParser
	{
		private List<Data> records = new List<Data>();
		public ScriptParser() {
			string recordpath = Config.cfg.scriptPath;
			this.Load(recordpath);
		}

		public void Load(string name) {
			string recordpath = name;
			string datapath = Path.ChangeExtension(recordpath, ".csv"); ;

			records.Clear();

			if (!File.Exists(recordpath)) {
				Trace.WriteLine("Funscript missing!");
				return;
			}
			if (!File.Exists(datapath)) {
				Trace.WriteLine("Funscript records missing!");
				return;
			}

			using (var reader = new StreamReader(datapath))
			using (TextFieldParser parser = new TextFieldParser(reader)) {
				parser.TextFieldType = FieldType.Delimited;
				parser.SetDelimiters(",");
				while (!parser.EndOfData) {
					//Process row
					string[] fields = parser.ReadFields();
					if (fields[0] == "Name")
						continue;
					//TODO: Find a better builtin method
					records.Add(new Data(
						fields[0],
						fields[1],
						long.Parse(fields[2]),
						long.Parse(fields[3]),
						bool.Parse(fields[4]),
						null
					));
				}
			}

			using (var reader = new StreamReader(recordpath)) {
				Funscript data = JsonSerializer.Deserialize<Funscript>(reader.ReadToEnd());
				foreach (var rec in this.records) {
					var actions = data.actions
						.FindAll((d) => rec.Start <= d["at"] && d["at"] <= rec.End)
						.Select((d) => ((long)d["at"], (int)d["pos"]))
						.ToList();

					long offset = rec.Start;
					for (var i = 0; i < actions.Count; i++) {
						actions[i] = (actions[i].Item1 - offset, actions[i].Item2);
					}

					rec.Actions = actions;
				}
			}
		}
		public Data get(string Name, string Scene) {
			return this.records.Find((r) => r.Name == Name && r.Scene == Scene);
		}
	}
}
