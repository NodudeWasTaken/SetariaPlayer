using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SetariaPlayer.Func {
	internal class Filler {
		private class FillerData : Data {
			private Func<double> height_mod;
			private Func<double> length_mod;
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
				//TODO: Move to script
				get {
					long fillerD = (long)(Config.cfg.fillerDur*length_mod());
					return new List<(long, int)>
						{
							(0,0),
							(fillerD/2, (int)Math.Min(Config.cfg.fillerHeight*height_mod(), 100)),
							(fillerD, 0)
						};
				}
			}
			public FillerData(Func<double> height_mod, Func<double> length_mod) : base("InactiveState", "Filler", 0, 0, true, new List<(long, int)>()) { this.height_mod = height_mod; this.length_mod = length_mod; }
		}
		private long? last = null;
		public double fillerHeightMod = 1.0;
		public double fillerLengthMod = 1.0;
		private Data filler;
		private ScriptPlayer sp;
		private Task watcher;
		private bool running = false;
		private Func<bool> shouldrun;

		public Filler(ScriptPlayer sp, Func<bool> shouldrun) {
			this.shouldrun = shouldrun;
			this.sp = sp;
			this.filler = new FillerData(() => fillerHeightMod, () => fillerLengthMod);

		}
		public void Start() {
			if (!this.shouldrun()) {
				return;
			}

			running = true;
			sp.Play(filler);

			watcher = new Task(() => {
				while (running) {
					if (last != null && Utilities.curtime() > last) {
						sp.SetTimeScale(1.0);
						sp.Play(filler);
						last = null;
					} else {
						fillerHeightMod = 1.0;
						fillerLengthMod = 1.0;
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
		/*private long ModTime() {
			//long durfix = Math.Max(850, filler.Duration());
			return Config.cfg.fillerModTime;
		}*/
		public static Data getMeleeScript() {
			return new Data("vibrate", "Melee", 0, Config.cfg.fillerAModMeleeLength, false,
				new List<(long, int)> {
					(Config.cfg.fillerAModMeleeLength/2, Config.cfg.fillerAModMeleeHeight),
					(Config.cfg.fillerAModMeleeLength, 0),
				});
		}
		public static Data getFireScript() {
			return new Data("vibrate", "Fire", 0, Config.cfg.fillerAModFireLength, false,
				new List<(long, int)> {
					(Config.cfg.fillerAModFireLength/2, Config.cfg.fillerAModFireHeight),
					(Config.cfg.fillerAModFireLength, 0),
				});
		}
		public static Data getLazerScript() {
			return new Data("vibrate", "Lazer", 0, Config.cfg.fillerAModLazerLength, false,
				new List<(long, int)> {
					(Config.cfg.fillerAModLazerLength/2, Config.cfg.fillerAModLazerHeight),
					(Config.cfg.fillerAModLazerLength, 0),
				});
		}
		public static Data getDamageScript(double damage_perc=1.0) {
			return new Data("vibrate", "Damage", 0, Config.cfg.fillerAModDamageLength, false,
				new List<(long, int)> {
					(Config.cfg.fillerAModDamageLength/2, (int)(Config.cfg.fillerAModDamageHeight * damage_perc)),
					(Config.cfg.fillerAModDamageLength, 0),
				});
		}
		public void Melee() {
			this.last = Utilities.curtime() + Config.cfg.fillerAModFireLength;
			this.sp.Play(getMeleeScript());
		}
		public void Fire() {
			this.last = Utilities.curtime() + Config.cfg.fillerAModFireLength;
			this.sp.Play(getFireScript());
		}
		public void Lazer() {
			this.last = Utilities.curtime() + Config.cfg.fillerAModLazerLength;
			this.sp.Play(getLazerScript());
		}
		public void Damage(double damage_perc = 1.0) {
			this.last = Utilities.curtime() + Config.cfg.fillerAModDamageLength;
			this.sp.Play(getDamageScript(damage_perc));
		}
	}
}
