// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osu.Framework.Utils;
using osu.Game.Rulesets.Difficulty.Skills;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to press keys with regards to keeping up with the speed at which objects need to be hit.
    /// </summary>
    public class Speed : StrainSkill
    {
        public Speed(Mod[] mods)
            : base(mods)
        {
        }

        private const double single_spacing_threshold = 125;
        private const double stream_spacing_threshold = 110;
        private const double almost_diameter = 90;

        private double skillMultiplier = 1400;
        private double strainDecayBase = 0.3;

        private double currentStrain = 1;

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain *= Math.Pow(strainDecayBase, current.DeltaTime / 1000);
            currentStrain += strainValueOf(current) * skillMultiplier;

            return currentStrain;
        }

        private double strainValueOf(DifficultyHitObject current)
        {
            double distance = ((OsuDifficultyHitObject)current).JumpDistance + ((OsuDifficultyHitObject)current).TravelDistance;

            double speedValue;
            if (distance > single_spacing_threshold)
                speedValue = 2.5;
            else if (distance > stream_spacing_threshold)
                speedValue = 1.6 + 0.9 * (distance - stream_spacing_threshold) / (single_spacing_threshold - stream_spacing_threshold);
            else if (distance > almost_diameter)
                speedValue = 1.2 + 0.4 * (distance - almost_diameter) / (stream_spacing_threshold - almost_diameter);
            else if (distance > almost_diameter / 2)
                speedValue = 0.95 + 0.25 * (distance - almost_diameter / 2) / (almost_diameter / 2);
            else
                speedValue = 0.95;

            return speedValue / Math.Max(50, current.DeltaTime);
        }

        protected override double CalculateInitialStrain(double time) => currentStrain * Math.Pow(strainDecayBase, (time - Previous[0].StartTime) / 1000);
    }
}
