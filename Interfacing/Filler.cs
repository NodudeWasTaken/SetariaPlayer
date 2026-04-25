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
			}, true, 600) {
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

		private long last = 0; // 0 = unset
		public double fillerHeightMod = 1.0;
		public double fillerLengthMod = 1.0;
		private FillerInt filler;
		private Controller sp;
		private Task? watcher;
		private volatile bool running = false;
		private Func<bool> shouldrun;

		public Filler(Controller sp, Func<bool> shouldrun) {
			this.shouldrun = shouldrun;
			this.sp = sp;
			this.filler = new FillerInt(() => fillerHeightMod, () => fillerLengthMod);
		}
		public void Start() {
			if (running) return;
			if (!this.shouldrun()) {
				return;
			}

			running = true;
			sp.Play(filler);

			watcher = new Task(() => {
				while (running) {
					long lastVal = Interlocked.Read(ref last);
					if (lastVal > 0 && Utils.UnixTimeMS() > lastVal) {
						sp.Play(filler);
						Interlocked.Exchange(ref last, 0);
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
				Interlocked.Exchange(ref last, 0);
				running = false;
				watcher?.Wait();
				sp.Stop();
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
		public long Melee() {
			Interlocked.Exchange(ref this.last, Utils.UnixTimeMS() + Config.cfg.fillerAModFireLength);
			return this.sp.Overwrite(getMeleeScript());
        }
		public long Fire() {
			Interlocked.Exchange(ref this.last, Utils.UnixTimeMS() + Config.cfg.fillerAModFireLength);
            return this.sp.Overwrite(getFireScript());
		}
		public long Lazer() {
			Interlocked.Exchange(ref this.last, Utils.UnixTimeMS() + Config.cfg.fillerAModLazerLength);
            return this.sp.Overwrite(getLazerScript());
        }
		public long Damage(double damage_perc = 1.0) {
			Interlocked.Exchange(ref this.last, Utils.UnixTimeMS() + Config.cfg.fillerAModDamageLength);
            return this.sp.Overwrite(getDamageScript(damage_perc));
        }
	}
}
