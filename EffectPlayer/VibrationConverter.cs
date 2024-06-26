﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetariaPlayer.EffectPlayer
{
    internal class VibrationConvert
    {
        private List<(long, double)> pos = new List<(long, double)>();
        private double dist
        {
            get
            {
                return Config.cfg.vibrationBufferDuration;
            }
        }
        private double def = 0.10;
        public VibrationConvert() { }

        // Filter by distance
        private void FilterDist((long, double) timestep)
        {
            pos = pos.Where((pos) => pos.Item1 + dist >= timestep.Item1).ToList();
        }
        private void FilterRange((long, double) timestep)
        {
            if (pos.Count > 5)
            {
                pos = pos.GetRange(pos.Count - 5, 5);
            }
        }
        private void FilterDiff((long, double) timestep)
        {
            for (int i = pos.Count; i > 0; i--)
            {
                if (Math.Abs(timestep.Item2 / pos[i].Item2) > dist)
                {
                    pos = pos.GetRange(i, pos.Count - i);
                    break;
                }
            }
        }

        public void Update((long, double) timestep)
        {
            //TODO: Dont need to do this every time
            //Even if it should be relatively inexpensive
            if (pos.Any()) {
                timestep.Item1 += pos.Last().Item1;
            }
            FilterDist(timestep);
            pos.Add(timestep);
        }
        public static double ActionSpeed((long, double) o, (long, double) c)
        {
            //Distance over 15cm
            /*double d1 = 0.15 * o.Item2;
            double d2 = 0.15 * c.Item2;*/
            double d1 = o.Item2;
            double d2 = c.Item2;
            //Speed in m pr second
            double m = Math.Abs(d1 - d2);
            double s = Math.Abs(o.Item1 - c.Item1) / 1000.0;
            //TODO: Fix divide by zero
            double ms = m / s;
            return ms;
        }

        public static List<double> FindPeaks(List<double> data)
        {
            return data
                .Where((value, index) => index + 1 <= data.Count || data[index - 1] < value && value >= data[index + 1])
                .ToList();
        }

        public double Get()
        {
            if (pos.Count < 2)
            {
                return def;
            }

            List<double> result = new List<double>();

            //Convert to speed
            //Enables us to tell the intensity
            for (int i = 1; i < pos.Count; i++)
            {
                var o = pos[i - 1];
                var c = pos[i];
                result.Add(ActionSpeed(o, c));
            }

            result = FindPeaks(result);

            //Filter if too different
            //Enables us to react to local changes
            double lastElem = result.Last();
            for (int i = result.Count - 1; i > 0; i--)
            {
                if (Utils.diff(lastElem, result[i]) > Config.cfg.vibrationCalcDiff)
                {
                    result = result.GetRange(i, result.Count - i);
                    break;
                }
            }

            //Gets the local average (local intensity)
            //Assume max 2m/s
            double speed = result.Average() / Config.cfg.vibrationMaxSpeed;
            //Limit the max
            speed = Math.Min(speed, 1.0);

            // Onlydown
            if (Config.cfg.vibrationOnlyDown)
            {
                if (pos[pos.Count - 2].Item2 < pos[pos.Count - 1].Item2)
                {
                    speed = 0f;
                }
                else
                {
                    speed = result.Last();
                }
            }

            //TODO: Fix divide by zero
            if (double.IsNaN(speed))
            {
                return def;
            }
            return speed;
        }
        public void Clear()
        {
            pos.Clear();
        }
    }

}
