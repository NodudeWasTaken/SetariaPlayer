using SetariaPlayer.EffectPlayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SetariaPlayer {
	internal class Filler {
		class FillerInt : Interaction {
			private bool b = true;
			private Func<double> heightmod;
			private Func<double> widthmod;
			public FillerInt(Func<double> heightmod, Func<double> widthmod) : base(new List<ActionMove> {
				new ActionMove(200, 25),
				new ActionMove(400, 0)
			}, true) {
				this.heightmod = heightmod;
				this.widthmod = widthmod;
			}
			public override ActionMove? next() {
				long dur = (long)(Config.cfg.fillerDur/2 * this.widthmod());
				ActionMove move = b ? 
					new ActionMove(dur, (int)(Config.cfg.fillerHeight * this.heightmod())) : 
					new ActionMove(dur*2, 0);
				b = !b;
				_last = move;
				return move;
			}
		}

		private long? last = null;
		public double fillerHeightMod = 1.0;
		public double fillerLengthMod = 1.0;
		private FillerInt filler;
		private Controller sp;
		private Task watcher;
		private bool running = false;
		private Func<bool> shouldrun;

		public Filler(Controller sp, Func<bool> shouldrun) {
			this.shouldrun = shouldrun;
			this.sp = sp;
			this.filler = new FillerInt(() => fillerHeightMod, () => fillerLengthMod);
		}
		public void Start() {
			if (!this.shouldrun()) {
				return;
			}
			// BUG: Cannot disable if enabled live

			running = true;
			sp.Play(filler);

			watcher = new Task(() => {
				while (running) {
					if (last != null && Utils.UnixTimeMS() > last) {
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
		public static Interaction getMeleeScript() {
			return Interaction.FromData(new Data("vibrate", "Melee", 0, Config.cfg.fillerAModMeleeLength, false,
				new List<(long, int)> {
					(Config.cfg.fillerAModMeleeLength/2, Config.cfg.fillerAModMeleeHeight),
					(Config.cfg.fillerAModMeleeLength, 0),
				}));
		}
		public static Interaction getFireScript() {
			return Interaction.FromData(new Data("vibrate", "Fire", 0, Config.cfg.fillerAModFireLength, false,
				new List<(long, int)> {
					(Config.cfg.fillerAModFireLength/2, Config.cfg.fillerAModFireHeight),
					(Config.cfg.fillerAModFireLength, 0),
				}));
		}
		public static Interaction getLazerScript() {
			return Interaction.FromData(new Data("vibrate", "Lazer", 0, Config.cfg.fillerAModLazerLength, false,
				new List<(long, int)> {
					(Config.cfg.fillerAModLazerLength/2, Config.cfg.fillerAModLazerHeight),
					(Config.cfg.fillerAModLazerLength, 0),
				}));
		}
		public static Interaction getDamageScript(double damage_perc = 1.0) {
			return Interaction.FromData(new Data("vibrate", "Damage", 0, Config.cfg.fillerAModDamageLength, false,
				new List<(long, int)> {
					(Config.cfg.fillerAModDamageLength/2, (int)(Config.cfg.fillerAModDamageHeight * damage_perc)),
					(Config.cfg.fillerAModDamageLength, 0),
				}));
		}
		public void Melee() {
			this.last = Utils.UnixTimeMS() + Config.cfg.fillerAModFireLength;
			this.sp.Overwrite(getMeleeScript());
		}
		public void Fire() {
			this.last = Utils.UnixTimeMS() + Config.cfg.fillerAModFireLength;
			this.sp.Overwrite(getFireScript());
		}
		public void Lazer() {
			this.last = Utils.UnixTimeMS() + Config.cfg.fillerAModLazerLength;
			this.sp.Overwrite(getLazerScript());
		}
		public void Damage(double damage_perc = 1.0) {
			this.last = Utils.UnixTimeMS() + Config.cfg.fillerAModDamageLength;
			this.sp.Overwrite(getDamageScript(damage_perc));
		}
	}
}
