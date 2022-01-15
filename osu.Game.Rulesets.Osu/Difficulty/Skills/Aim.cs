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
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : StrainSkill
    {
        public Aim(Mod[] mods)
            : base(mods)
        {
        }

        private double skillMultiplier = 26.25;
        private double strainDecayBase = 0.15;

        private double currentStrain = 1;

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain *= Math.Pow(strainDecayBase, current.DeltaTime / 1000);
            currentStrain += strainValueOf(current) * skillMultiplier;

            return currentStrain;
        }

        protected override double CalculateInitialStrain(double time) => currentStrain * Math.Pow(strainDecayBase, (time - Previous[0].StartTime) / 1000);

        private double strainValueOf(DifficultyHitObject current) => (Math.Pow(((OsuDifficultyHitObject)current).JumpDistance, 0.99) + Math.Pow(((OsuDifficultyHitObject)current).TravelDistance, 0.99)) / Math.Max(50, ((OsuDifficultyHitObject)current).DeltaTime);
    }
}
