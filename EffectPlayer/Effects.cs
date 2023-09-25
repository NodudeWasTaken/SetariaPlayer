using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SetariaPlayer.EffectPlayer
{
	class SlowmotionEffect : IEffect {
		public double scale = 1.0;
		public ActionMove ApplyEffect(ActionMove action) {
			if (action == null) {
				return null;
			}

			ActionMove modifiedAction = new ActionMove(action);
			modifiedAction.dur = (long)(action.dur * scale);
			return modifiedAction;
		}
	}
	class AmplifyHeightEffect : IEffect {
		public double scaleFactor = 1.0;
		public ActionMove ApplyEffect(ActionMove action) {
			if (action == null) {
				return null;
			}

			ActionMove modifiedAction = new ActionMove(action);
			// Amplify the height using the scaleFactor
			modifiedAction.height = Math.Min((int)(action.height * scaleFactor), 100);
			return modifiedAction;
		}
	}
}
