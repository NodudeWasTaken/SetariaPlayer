using Buttplug.Client;
using Buttplug.Core.Messages;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

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
            _dur = inter._dur;
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
            if (data.Actions.Count == 0) {
                return new Interaction(moves, data.Loop, data.Duration());
            }
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
        // Minimum gap between consecutive LinearCmd sends. Newer Handy firmware
        // queues LinearCmd aggressively; sending faster than ~6-7Hz causes queue
        // stalls where the device appears frozen and then jumps. Vibrate commands
        // are not throttled here since they stream continuously by design.
        private const long MIN_LINEAR_INTERVAL_MS = 150;

        protected ButtplugInt _client;
        protected Interaction? _interaction;
        protected ActionMove? _action;
        protected long _played = 0L;
        protected long _paused = 0L;
        // Timestamp at which the device is expected to finish the previously issued
        // LinearCmd. New sends are skipped until now >= this (but the MIN_INTERVAL
        // floor still applies so we don't flood on very short actions).
        protected long _nextLinearSendAllowedMs = 0L;
        protected double _lastSentPos = -1.0;
        protected VibrationConvert _vibrate = new VibrationConvert();

        public Player(ButtplugInt client)
        {
            _client = client;
        }

        public void SetInteraction(Interaction interaction)
        {
            _interaction = interaction;
            _action = null;
            _played = 0;
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
                long actionDuration = Math.Max(action.dur, 0);

                if (actionDuration > 50) {
					// Infinity fix
					pos = Math.Clamp(pos, 0.0, 1.0);
                    intensity = Math.Clamp(intensity, 0.0, 1.0);

				    // Use the Dispatcher to update the UI on the main thread
				    MainWindow.DumbPointerHack.Dispatcher.BeginInvoke(new Action(() => {
					    MainWindow.DumbPointerHack.UpdateStrokerHeight(pos);
					    MainWindow.DumbPointerHack.UpdateVibratorHeight(intensity);
				    }));

				    long now = Utils.UnixTimeMS();
				    // Three conditions must all be met to send a LinearCmd:
				    //  (a) device is expected to have finished the prior command (queue isn't full)
				    //  (b) the position actually changed meaningfully (don't waste slots on no-ops)
				    // The first send (_nextLinearSendAllowedMs == 0) bypasses (a).
				    bool timeOk = _nextLinearSendAllowedMs == 0 || now >= _nextLinearSendAllowedMs;
				    bool posChanged = _lastSentPos < 0 || Math.Abs(pos - _lastSentPos) >= 0.01;
				    bool canSendLinear = timeOk && posChanged;

				    foreach (var device in _client.client.Devices) {
					    if (canSendLinear && device.HasOutput(OutputType.HwPositionWithDuration)) {
						    _ = device.RunOutputAsync(DeviceOutput.PositionWithDuration.Percent(pos, (uint)actionDuration));
					    }
					    if (device.HasOutput(OutputType.Vibrate)) {
						    _ = device.RunOutputAsync(DeviceOutput.Vibrate.Percent(intensity));
					    }
				    }

				    if (canSendLinear) {
					    // Block further LinearCmds until at least the action's own duration passes,
					    // or MIN_LINEAR_INTERVAL_MS — whichever is larger.
					    _nextLinearSendAllowedMs = now + Math.Max(actionDuration, MIN_LINEAR_INTERVAL_MS);
					    _lastSentPos = pos;
				    }

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

			// Avoid device.StopAsync()/client.StopAllDevicesAsync(): Buttplug C# v5.0.0 still
			foreach (var device in _client.client.Devices) {
				if (device.HasOutput(OutputType.Vibrate)) {
					_ = device.RunOutputAsync(DeviceOutput.Vibrate.Percent(0));
				}
				if (device.HasOutput(OutputType.HwPositionWithDuration)) {
					_ = device.RunOutputAsync(DeviceOutput.PositionWithDuration.Percent(0, 200));
				}
			}

			_interaction = null;
			_action = null;
			_played = 0;
		}
    }
}
