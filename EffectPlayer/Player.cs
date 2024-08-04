using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using static Buttplug.ServerMessage.Types;

namespace SetariaPlayer.EffectPlayer
{
    class ActionMove
    {
        // in MS
        public long dur;
        // in (min 0 max 100)%
        public int height;
        public ActionMove(long dur, int height) {
            this.dur = dur;
            this.height = Math.Max(Math.Min(height, 100), 0);
        }
        public ActionMove(ActionMove old) {
            this.dur = old.dur;
            this.height = old.height;
        }
    }

	interface IEffect {
		ActionMove ApplyEffect(ActionMove action);
	}

	class Interaction
    {
		public List<ActionMove> _actions;
		public bool _loop;
		protected ActionMove? _last;
		protected int index = 0;
        protected long _dur;

        public Interaction(List<ActionMove> actions, bool loop, long dur) {
            _actions = actions;
            _loop = loop;
			_dur = dur;
        }
        public Interaction(Interaction inter) {
            _actions = inter._actions;
            _loop = inter._loop;
        }
        public void SetLoop(bool loop) {
            _loop = loop;
        }
        public void reset() {
            index = 0;
            _last = null;
        }
        public long GetDuration()
        {
            return _dur;
        }

        public virtual ActionMove? last() {
            return _last;
        }
        public virtual ActionMove? next() {
            if (_actions.Count == 0) return null;
            if (index >= _actions.Count)
            {
                if (_loop)
                {
                    index = 0;
                }
                else
                {
                    return null;
                }
            }

            ActionMove a = _actions[index++];
            _last = a;
            return a;
        }

        public static Interaction FromData(Data data) {
            List<ActionMove> moves = new List<ActionMove>();
			(long, int)? last = null;
			foreach (var action in data.Actions) {
                long dur = action.Item1;
                if (last != null) {
                    dur -= last.Value.Item1;
                }
				moves.Add(new ActionMove(dur, action.Item2));
                last = action;
			}
            long lastPointDuration = data.Duration() - data.Actions.Last().Item1;
            if (lastPointDuration > 5) {
                (long, int) a = (moves.Last().dur, moves.Last().height);
                (long, int) b = (moves.First().dur + a.Item1, moves.First().height);
				moves.Add(new ActionMove(lastPointDuration, Utils.InterpolateHeight(a, b, lastPointDuration)));// data.Actions.First().Item2));
            }
            for (int i=1; i<moves.Count;i++) {
                moves[i].height = Utils.LimitSpeed(
                    (moves[i].dur, moves[i].height),
					(0, moves[i-1].height),
                    Config.cfg.strokeMaxAccel
				).Item2;
            }
			return new Interaction(moves, data.Loop, data.Duration());
        }
    }

    class Player
    {
        protected ButtplugInt _client;
        protected Interaction? _interaction;
        protected ActionMove? _action;
        protected long _played = 0L;
        protected long _paused = 0L;
        protected long _drift = 0L;
        protected VibrationConvert _vibrate = new VibrationConvert();
        protected (long, double) oldact = (0,0);

        public Player(ButtplugInt client)
        {
            _client = client;
        }

        public void SetInteraction(Interaction interaction)
        {
            _interaction = interaction;
            _action = null;
            _played = 0;
            _drift = 0;
			Interrupt();
        }

        public void Pause() {
			if (_paused > 0L) {
				return;
			}

			_paused = Utils.UnixTimeMS();
        }
        public void Resume() {
            if (_paused <= 0) {
                return;
            }

			_played += Utils.UnixTimeMS() - _paused;
			_paused = 0L;
		}

        public void Interrupt() {
            if (_interaction == null) {
                return;
            }

            _action = _interaction.last();
        }

        public void Loop(bool command = true)
        {
            var acc = _action;
            if (_interaction == null ||
                _paused != 0L ||
               _played > Utils.UnixTimeMS()
            ) {
                return;
            }

            _action = _interaction.next();
            if (_action == null) {
                return;
            }
            ActionMove action = _action;

			double _posp = action.height / 100.0;
			_vibrate.Update((action.dur, _posp));
            double intensity = _vibrate.Get();
            if (command) {
				double pos = (_posp * (Config.cfg.strokeMax - Config.cfg.strokeMin)) + Config.cfg.strokeMin;
                long actionDuration = action.dur;

				// Ensure that the action duration is not negative
				actionDuration = Math.Max(actionDuration, 0);

                // TODO: Still fix drift

                if (actionDuration > 50) {
					// Infinity fix
					pos = Math.Clamp(pos, 0.0, 1.0);
                    intensity = Math.Clamp(intensity, 0.0, 1.0);

				    // Use the Dispatcher to update the UI on the main thread
				    MainWindow.DumbPointerHack.Dispatcher.BeginInvoke(new Action(() => {
					    MainWindow.DumbPointerHack.UpdateStrokerHeight(pos);
					    MainWindow.DumbPointerHack.UpdateVibratorHeight(intensity);
				    }));

				    _client.client.Devices.AsParallel().ForAll(device => {
					    if (device.AllowedMessages.ContainsKey(MessageAttributeType.LinearCmd)) {
						    device.SendLinearCmd((uint)actionDuration, pos);
					    }
					    if (device.AllowedMessages.ContainsKey(MessageAttributeType.VibrateCmd)) {
						    device.SendVibrateCmd(intensity);
					    }
				    });

                    _played = Utils.UnixTimeMS() + actionDuration;
                }
			}

        }
        public void Stop() {
			// Use the Dispatcher to update the UI on the main thread
			MainWindow.DumbPointerHack.Dispatcher.BeginInvoke(new Action(() =>
			{
				MainWindow.DumbPointerHack.UpdateVibratorHeight(0.0);
			}));

			_client.client.Devices.AsParallel().ForAll(device => {
				if (device.AllowedMessages.ContainsKey(MessageAttributeType.VibrateCmd)) {
                    device.SendStopDeviceCmd();
                }
            });

			_interaction = null;
			_action = null;
			_played = 0;
		}
    }
}
