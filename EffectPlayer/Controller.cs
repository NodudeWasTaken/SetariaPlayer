using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SetariaPlayer.EffectPlayer
{
    class EnhancedInteraction : Interaction {
        public Interaction? interaction = null;
        private List<IEffect> effects = new List<IEffect>();

		public EnhancedInteraction(Interaction inter) : base(inter) { interaction = inter; }

		public void AddEffect(IEffect effect) {
			effects.Add(effect);
		}

		public override ActionMove? next() {
            var _interaction = interaction;
            if (_interaction == null) { return null; }
			ActionMove? action = _interaction.next();
            if (action == null) { return null; }

			foreach (var effect in effects) {
				action = effect.ApplyEffect(action);
			}

			return action;
		}

		public void SetInteraction(Interaction newInteraction) {
			newInteraction.reset();
			this._actions = newInteraction._actions;
            this._loop = newInteraction._loop;
			interaction = newInteraction;
		}
	}
	class OverwriteInteraction : EnhancedInteraction {
		protected System.Action? _callback;

		public OverwriteInteraction(Interaction interaction, System.Action callback) : base(interaction) {
			_callback = callback;
		}

		public override ActionMove? next() {
			ActionMove? action = base.next();
			if (action == null && _callback != null) {
				_callback();
				_callback = null;
			}
			return action;
		}
	}

	class Controller
    {
        protected Player _player;
        protected EnhancedInteraction _main = new EnhancedInteraction(new Interaction(new List<ActionMove>(), false, 0));
        protected Player _owplayer;
        protected EnhancedInteraction _overwrite = new EnhancedInteraction(new Interaction(new List<ActionMove>(), false, 0));
		protected SlowmotionEffect slowmotion = new SlowmotionEffect();
        protected Task runner;

		public Controller(ButtplugInt client)
        {
            _player = new Player(client);
            _owplayer = new Player(client);
            _main.AddEffect(slowmotion);
            runner = new Task(() => {
                try
                {
                    while (true)
                    {
                        this.Loop();
                        Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Critical exception:");
					Trace.WriteLine(ex);
                }
            });
            runner.Start();
		}

        public void Play(Interaction interaction)
        {
            _main.SetInteraction(interaction);
            _player.SetInteraction(_main);
            _overwrite.interaction = null;
		}

        // Legacy
        public void Play(Interaction interaction, bool? loopOverride = null) {
            Interaction act = new Interaction(interaction);
            if (loopOverride != null) {
				act.SetLoop((bool)loopOverride);
			}
            this.Play(act);
        }

        public void SetTimeScale(double scale) {
            this.slowmotion.scale = scale;
        }

		public long Overwrite(Interaction interaction)
        {
            var inter = new OverwriteInteraction(interaction, () =>
            {
                if (_main.interaction != null)
                {
                    _player.SetInteraction(_main);
                }
                _owplayer.Stop();
                _overwrite.interaction = null;
            });
            _overwrite.SetInteraction(inter);
            _owplayer.SetInteraction(_overwrite);

            return inter.GetDuration();
        }

        public void Loop()
        {
            _player.Loop(_overwrite.interaction == null);
            _owplayer.Loop(_overwrite.interaction != null);
        }

        public void Pause() {
            _player.Pause();
            _owplayer.Pause();
        }

        public void Resume() {
            _player.Resume();
            _owplayer.Resume();
        }

        public void Stop() { 
            _player.Stop();
			_owplayer.Stop();
            _main.interaction = null;
			_overwrite.interaction = null;
		}
    }
}
