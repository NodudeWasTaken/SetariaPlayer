using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SetariaPlayer.Func {
	internal class Filler {
		private class FillerData : Data {
			private Func<double> mod;
			public override long Start { 
				get {
					return 0;
				} 
			}
			public override long End {
				get {
					return Config.cfg.fillerDur;
				}
			}
			public override List<(long, int)> Actions { 
				//Hacking your own classes?!
				//Naughty
				get {
					return new List<(long, int)>
						{
							(0,0),
							(Config.cfg.fillerDur/2, (int)Math.Min(Config.cfg.fillerHeight*mod(), 100)),
							(Config.cfg.fillerDur, 0)
						};
				}
			}
			public FillerData(Func<double> mod) : base("InactiveState", "Filler", 0, 0, true, new List<(long, int)>()) { this.mod = mod; }
		}
		private long? last = null;
		private double fillerMod = 1.0;
		private Data filler;
		private ScriptPlayer sp;
		private Task watcher;
		private bool running = false;

		public Filler(ScriptPlayer sp) {
			this.sp = sp;
			this.filler = new FillerData(() => fillerMod);

		}
		public void Start() {
			sp.Play(filler);
			running = true;

			watcher = new Task(() => {
				while (running) {
					//long durfix = Math.Max(850, filler.Duration());
					long durfix = Config.cfg.fillerModTime;
					if (last != null && Utilities.curtime() > last + durfix) {
						sp.setTimeScale(1.0);
						fillerMod = 1.0;
						last = null;
					}
					Thread.Sleep(1);
				}
			});
			watcher.Start();
		}
		public void Stop() {
			if (running) {
				last = null;
				sp.Stop();
				running = false;
				watcher.Wait();
			}
		}
		public bool Running() {
			return this.running;
		}
		public static (long, int) getAction(int height) {
			return (150, height);
		}
		public void Fire() {
			sp.setTimeScale(Config.cfg.fillerModFireSpeed);
			fillerMod = Config.cfg.fillerModFireHeight;
			last = Utilities.curtime();
		}
		public void Lazer() {
			sp.setTimeScale(Config.cfg.fillerModLazerSpeed);
			fillerMod = Config.cfg.fillerModLazerHeight;
			last = Utilities.curtime();
		}
		public void Damage() {
			sp.setTimeScale(Config.cfg.fillerModDamageSpeed);
			fillerMod = Config.cfg.fillerModDamageHeight;
			last = Utilities.curtime();
		}
	}
}
