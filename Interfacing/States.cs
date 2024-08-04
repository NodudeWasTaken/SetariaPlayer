using SetariaPlayer.EffectPlayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Buttplug.DeviceMessage.Types;
using static Buttplug.ServerMessage.Types;

namespace SetariaPlayer
{
	enum State
	{
		Inactive,
		ScenePlaying,
	};
	class StateBase
	{
		public string name;
		public string urlPrefix;
		protected ButtplugInt b;
		protected Controller sp;
		protected ScriptParser sr;
		public StateBase(ButtplugInt b, Controller sp, ScriptParser sr) {
			this.b = b;
			this.sp = sp;
			this.sr = sr;
		}
		public virtual void Enter(HttpListenerRequest r, ref State s) { }
		public virtual bool ShouldEnter(HttpListenerRequest r)
		{
			return r.Url.AbsolutePath.Equals(this.urlPrefix);
		}
		public virtual bool ShouldExit(HttpListenerRequest r) {
			return !ShouldEnter(r);
		}
		public virtual void Update(HttpListenerRequest r, ref State s) { }
		public virtual void Update() { }
		public virtual void Pause() { sp.Pause(); }
		public virtual void Resume() { sp.Resume(); }
		public virtual void Exit(HttpListenerRequest r, ref State s) { }
	}
	class InactiveState : StateBase
	{
		public float hp;
		public float maxhp;
		public float mp;
		public Filler fi;
		private long lastcall;
		private int MIN_TIME = 80;

		public InactiveState(ButtplugInt b, Controller sp, ScriptParser sr) : base(b, sp, sr) {
			this.name = "Inactive";
			this.urlPrefix = "/game/player_damage";
			fi = new Filler(sp, () => Config.cfg.filler);
		}
		public override void Enter(HttpListenerRequest r, ref State s) {
			base.Enter(r, ref s);
			if (Config.cfg.filler)
				fi.Start();
		}
		public override void Update() {
			base.Update();

			if (!Config.cfg.filler) {
				if (fi.Running())
					fi.Stop();
			} else {
				if (!fi.Running())
					fi.Start();
			}
			//TODO: oDeathRoom and oCyclops
		}
		public override void Update(HttpListenerRequest r, ref State s) {
			base.Update(r, ref s);
			this.Update();

			float? newhp = null;
			float? newmaxhp = null;
			float? newmp = null;
			float? damage = null;
			if (r.QueryString.HasKeys()) {
				foreach (string qs in r.QueryString.AllKeys) {
					string value = r.QueryString[qs];
					if (qs == "hp")
						newhp = float.Parse(value, CultureInfo.InvariantCulture);
					if (qs == "maxhp")
						newmaxhp = float.Parse(value, CultureInfo.InvariantCulture);
					if (qs == "mp")
						newmp = float.Parse(value, CultureInfo.InvariantCulture);
				}

				if (newhp.HasValue && newmaxhp.HasValue && newhp.Value < hp) {
					double val = Utils.NormalizedApproach(newhp.Value, maxhp);
					//fi.fillerLengthMod = 1.0 + (val * Config.cfg.fillerModHPImpact);
					fi.fillerHeightMod = 1.0 + (val * Config.cfg.fillerModHPImpact);
					damage = Math.Abs(newhp.Value - hp) / maxhp;
					damage = (float)Utils.Limit(damage.Value, 0, 1);
				}
			}

			if (r.Url.AbsolutePath.Equals("/game/melee") && lastcall < Utils.UnixTimeMS()) {
                lastcall = Utils.UnixTimeMS() + fi.Melee(); 
			}
			if (r.Url.AbsolutePath.Equals("/game/fire") && lastcall < Utils.UnixTimeMS()) {
                lastcall = Utils.UnixTimeMS() + fi.Fire();
			}
			if (r.Url.AbsolutePath.Equals("/game/fire_lazer") && lastcall < Utils.UnixTimeMS()) {
                lastcall = Utils.UnixTimeMS() + fi.Lazer();
			}
			if (r.Url.AbsolutePath.Equals("/game/fire_shotgun") && lastcall < Utils.UnixTimeMS()) {
                lastcall = Utils.UnixTimeMS() + fi.Lazer();
			}
			if (r.Url.AbsolutePath.Equals("/game/player_damage") && lastcall < Utils.UnixTimeMS()) {
                            double damageProd = 1;
				if (damage.HasValue) {
					// TODO: Config
					damageProd += Config.cfg.damageImpact * damage.Value;
				}
                lastcall = Utils.UnixTimeMS() + fi.Damage(damageProd);
			}
			if (r.Url.AbsolutePath.Equals("/game/custom_interval") && lastcall < Utils.UnixTimeMS()) {
                float dist = 500;
				int length = 2000;
				int min = 0;
				int max = 25;

				foreach (string qs in r.QueryString.AllKeys) {
					string value = r.QueryString[qs];
					if (qs == "dist")
						dist = int.Parse(value, CultureInfo.InvariantCulture);
					if (qs == "length")
						length = int.Parse(value, CultureInfo.InvariantCulture);
					if (qs == "min")
						min = int.Parse(value, CultureInfo.InvariantCulture);
					if (qs == "max")
						max = int.Parse(value, CultureInfo.InvariantCulture);
				}

				if (r.QueryString.AllKeys.Contains("speed")) {
					//abs(min-max) / (dist/1000)=speed
					float speed = int.Parse(r.QueryString["speed"], CultureInfo.InvariantCulture);
					dist = 1000.0f * (Math.Abs(min - max) / speed);
				}

				if (dist > 0) {
					var actions = new List<ActionMove>();
					bool b = true;
					for (float i = 0; i < length; i += dist) {
						actions.Add(new ActionMove((int)dist, b ? max : min));
						b = !b;
					}

                    lastcall = Utils.UnixTimeMS() + sp.Overwrite(new Interaction(actions, false, length));
				} else {
					Trace.WriteLine($"InactiveState.Error: dist less than zero!");
				}
			}

			hp = newhp.HasValue ? newhp.Value : hp;
			maxhp = newmaxhp.HasValue ? newmaxhp.Value : maxhp;
			mp = newmp.HasValue ? newmp.Value : mp;
		}
		public override void Pause() {
			base.Pause();
			fi.Stop();
		}
		public override void Resume() {
			base.Resume();
			if (Config.cfg.filler)
				fi.Start();
		}
		public override void Exit(HttpListenerRequest r, ref State s) {
			base.Exit(r, ref s);
			fi.Stop();
			sp.Stop();
		}
	}
	class BossState : InactiveState {
		public BossState(ButtplugInt b, Controller sp, ScriptParser sr) : base(b, sp, sr) {
			this.name = "Boss mode";
			this.urlPrefix = "/game/custom_bossmode";
		}
		public override void Update() {
			base.Update();

			if (!fi.Running())
				fi.Start();
		}
		public override void Update(HttpListenerRequest r, ref State s) {
			base.Update(r, ref s);
			if (r.Url.AbsolutePath.Equals("/game/custom_bossmode_stop")) {
				this.Exit(r, ref s);
				return;
			}

			// TODO MAYBE Ideas:
			// Treat boss attacks as fire events
			// A "versus" where you are punished the more % the boss has of health compared to you, or reverse
			// Special attacks, randomize or insanity mode (extreme speed for short periods).
			//
			base.fi.fillerHeightMod = Math.Max(fi.fillerHeightMod, 1.45);
			base.fi.fillerLengthMod = Math.Min(fi.fillerLengthMod, 0.9);
			if (r.Url.AbsolutePath.Equals("/game/fire_shotgun"))
				fi.Lazer();
		}
	}
	// Unused
	class DeathRoomState : StateBase {
		private long time = 0;
		public DeathRoomState(ButtplugInt b, Controller sp, ScriptParser sr) : base(b, sp, sr) {
			this.name = "DeathRoom";
			this.urlPrefix = "/game/custom_DeathRoomGirl";
		}
		public override void Enter(HttpListenerRequest r, ref State s) {
			base.Enter(r, ref s);
			this.Update(r, ref s);
			sp.Play(Interaction.FromData(sr.get("sHFairy1", "start")), false);
		}
		public override void Update(HttpListenerRequest r, ref State s) {
			base.Update(r, ref s);
			if (r.Url.AbsolutePath.Equals("/game/custom_DeathRoomGirl_stop") || r.Url.AbsolutePath.Equals("/game/gallery_stop")) {
				this.Exit(r, ref s);
				return;
			}

			if (r.Url.AbsolutePath.Equals("/game/custom_DeathRoomGirl_action")) {
				float animflag = float.Parse(r.QueryString["anim_flag"], CultureInfo.InvariantCulture);
				if (animflag == 7) {
					//Max update time is 1/10 a second
					if ((time + Config.cfg.fillerAModDamageLength) < Utils.UnixTimeMS()) {
						this.sp.Play(Filler.getDamageScript());
						time = Utils.UnixTimeMS();
					}
				}
			}
		}
	}
	class Fairy1State : StateBase {
		private float max = -569;
		private float min = -469;
		private long time = 0;
		public Fairy1State(ButtplugInt b, Controller sp, ScriptParser sr) : base(b, sp, sr) {
			this.name = "Fairy1 player";
			this.urlPrefix = "/game/custom_HFairy1";
		}
		public override void Enter(HttpListenerRequest r, ref State s) {
			base.Enter(r, ref s);
			this.Update(r, ref s);
			sp.Play(Interaction.FromData(sr.get("sHFairy1", "start")), false);
		}
		public override void Update(HttpListenerRequest r, ref State s) {
			base.Update(r, ref s);
			if (r.Url.AbsolutePath.Equals("/game/custom_HFairy1_stop") || r.Url.AbsolutePath.Equals("/game/gallery_stop")) {
				this.Exit(r, ref s);
				return;
			}

			if (r.Url.AbsolutePath.Equals("/game/custom_HFairy1_finish")) {
				sp.Play(Interaction.FromData(sr.get("sHFairy1", "finish")), false);
				return;
			}

			/*if (!r.QueryString.HasKeys()) {
				Trace.WriteLine("Missing query string!");
				return;
			}*/

			if (r.Url.AbsolutePath.Equals("/game/custom_HFairy1_action")) {
				float bonepos = float.Parse(r.QueryString["bone_pos_y"], CultureInfo.InvariantCulture);
				max = Math.Min(max, bonepos);
				min = Math.Max(min, bonepos);

				float p = 1-Math.Abs((bonepos - min) / (max - min));

				//Max update time is 1/10 a second
				if (time + 100 < Utils.UnixTimeMS()) {
					this.b.client.Devices.AsParallel().ForAll(device => {
						if (device.AllowedMessages.ContainsKey(MessageAttributeType.LinearCmd)) {
							Trace.WriteLine($"HFairy1 action {p}");
							//TODO: We can probably calculate accceleration
							device.SendLinearCmd(150, p);
						} else if (device.AllowedMessages.ContainsKey(MessageAttributeType.VibrateCmd)) {
							device.SendVibrateCmd(p);
						}
					});
					time = Utils.UnixTimeMS();
				}
			}
		}
		public override void Exit(HttpListenerRequest r, ref State s) {
			base.Exit(r, ref s);
			sp.Stop();
		}
	}
	class ChairRoomState : ScenePlayerState {
		// For Lutellaria Area3State animation
		public ChairRoomState(ButtplugInt b, Controller sp, ScriptParser sr): base(b,sp,sr) {
			this.name = "Chairroom";
			this.urlPrefix = "/game/area3_gallery";
		}
		public override bool ShouldExit(HttpListenerRequest r) {
			return r.Url.AbsolutePath.Equals("/game/gallery_stop");
		}
	}
	class ScenePlayerState : StateBase {
		private string lastmob = "";
		private Data? curscript = null;
		public ScenePlayerState(ButtplugInt b, Controller sp, ScriptParser sr) : base(b,sp,sr) {
			this.name = "Scene player";
			this.urlPrefix = "/game/gallery";
		}
		public override void Enter(HttpListenerRequest r, ref State s) {
			base.Enter(r, ref s);
			s = State.ScenePlaying;
			lastmob = "";
			this.Update(r,ref s);
		}
		public override bool ShouldExit(HttpListenerRequest r) {
			return r.Url.AbsolutePath.Equals("/game/gallery_stop") || (!r.Url.AbsolutePath.StartsWith(this.urlPrefix) && !r.Url.AbsolutePath.StartsWith("/game/player_damage2"));
		}
		public override void Update(HttpListenerRequest r, ref State s) {
			base.Update(r, ref s);
			if (this.ShouldExit(r)) {
				//player_damage2 is end animation damage
				this.Exit(r, ref s);
				return;
			}
			if (!r.QueryString.HasKeys()) {
				Trace.WriteLine("Missing query string!");
				return;
			}
			if (!r.QueryString.AllKeys.Contains("anim_name") ||
				!r.QueryString.AllKeys.Contains("anim_scene") ||
				!r.QueryString.AllKeys.Contains("anim_speed") ||
				!r.QueryString.AllKeys.Contains("enemy_skin")) {
				Trace.WriteLine("Wrong query string!");
				return;
			}

			string mob = r.QueryString["anim_name"];
			string animation_scene = r.QueryString["anim_scene"];
			float transition_time = 0;
			if (r.QueryString.AllKeys.Contains("anim_dur"))
				transition_time = float.Parse(r.QueryString["anim_dur"], CultureInfo.InvariantCulture);
			float animation_speed = float.Parse(r.QueryString["anim_speed"], CultureInfo.InvariantCulture);
			/*
	Var enemy_id  =302
	Var enemy_name=GHMojya
	Var enemy_skin=default
	Var anim_id   =184
	Var anim_name =sHMojya
	Var anim_speed=1
	Var anim_scene=finish
	Var anim_dur  =0.50
	Var hp        =3
	Var mp        =1
	Var armor_state=3
	Var anim_flag =7
			*/

			/*
			 * Sandwitch trap hotfix
			 * It slows down the animation speed instead of having a finish scene
			 * So we here detect this and overwrite it.
			 */
			if (mob == "sHSandWitchTrap" && r.QueryString["enemy_skin"] == "finish") {
				mob = "sHSandWitchTrap;enemy_skin=finish";
				animation_scene = "scene1";
				animation_speed = 1f;
			}
			/*if (mob == "sArea3Animation" && animation_speed == 0) {
				animation_speed = 1f;
			}*/

			var script = sr.get(mob, animation_scene);
			if (script == null) {
				Trace.WriteLine($"Missing script for {mob} {animation_scene}!");
				sp.Stop();
				curscript = null;
				return;
			}

			if (curscript != null && curscript.GetId() == script.GetId()) {
				Trace.WriteLine($"Already playing {mob} {animation_scene}!");
				sp.SetTimeScale(animation_speed);
				return;
			}

			//If this wasn't a scene-switch, dont delay
			if (mob != lastmob) {
				transition_time = 0;
			}
			if (lastmob != "") {
				sp.Stop();
			}
			lastmob = mob;

			foreach (var d in b.client.Devices) {
				Trace.WriteLine($"Device name: {d.Name}");
			}

			int shift = (int)(transition_time * 1000.0);
			//TODO: Fix
			//shift = 0;
			this.Resume();
			sp.Play(Interaction.FromData(script));
			sp.SetTimeScale(animation_speed);
			curscript = script;
		}
		public override void Exit(HttpListenerRequest r, ref State s) {
			base.Exit(r, ref s);
			sp.Stop();
			s = State.Inactive;
			lastmob = "";
			curscript = null;
		}
	}
}
