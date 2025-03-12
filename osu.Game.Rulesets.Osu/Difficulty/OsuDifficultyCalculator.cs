// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 0.0675;

        public override int Version => 20250306;

        public OsuDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new OsuDifficultyAttributes { Mods = mods };

            var aim = skills.OfType<Aim>().Single();
            double aimRating = Math.Sqrt(aim.DifficultyValue()) * difficulty_multiplier;

            var speed = skills.OfType<Speed>().Single();
            double speedRating = Math.Sqrt(speed.DifficultyValue()) * difficulty_multiplier;

            var flashlight = skills.OfType<Flashlight>().Single();
            double flashlightRating = Math.Sqrt(flashlight.DifficultyValue()) * difficulty_multiplier;

            // var flashlightHidden = skills.OfType<Flashlight>().Single(f => f.HasHiddenMod);
            // double flashlightHiddenRating = Math.Sqrt(flashlightHidden.DifficultyValue()) * difficulty_multiplier;

            if (mods.Any(m => m is OsuModTouchDevice))
            {
                aimRating = Math.Pow(aimRating, 0.8);
                flashlightRating = Math.Pow(flashlightRating, 0.8);
                // flashlightHiddenRating = Math.Pow(flashlightHiddenRating, 0.8);
            }

            if (mods.Any(h => h is OsuModRelax))
            {
                aimRating *= 0.9;
                speedRating = 0.0;
                flashlightRating *= 0.7;
                // flashlightHiddenRating *= 0.7;
            }
            else if (mods.Any(h => h is OsuModAutopilot))
            {
                speedRating *= 0.5;
                aimRating = 0.0;
                flashlightRating *= 0.4;
                // flashlightHiddenRating *= 0.4;
            }

            double baseAimPerformance = OsuStrainSkill.DifficultyToPerformance(aimRating);
            double baseSpeedPerformance = OsuStrainSkill.DifficultyToPerformance(speedRating);
            double baseFlashlightPerformance = Flashlight.DifficultyToPerformance(flashlightRating);
            // double baseFlashlightHiddenPerformance = Flashlight.DifficultyToPerformance(flashlightHiddenRating);

            double basePerformance =
                Math.Pow(
                    Math.Pow(baseAimPerformance, 1.1) +
                    Math.Pow(baseSpeedPerformance, 1.1) +
                    Math.Pow(0, 1.1), 1.0 / 1.1
                );

            double basePerformanceWithFlashlight =
                Math.Pow(
                    Math.Pow(baseAimPerformance, 1.1) +
                    Math.Pow(baseSpeedPerformance, 1.1) +
                    Math.Pow(baseFlashlightPerformance, 1.1), 1.0 / 1.1
                );

            // double basePerformanceWithFlashlightHidden =
            //     Math.Pow(
            //         Math.Pow(baseAimPerformance, 1.1) +
            //         Math.Pow(baseSpeedPerformance, 1.1) +
            //         Math.Pow(baseFlashlightHiddenPerformance, 1.1), 1.0 / 1.1
            //     );

            double starRating = basePerformance > 0.00001
                ? Math.Cbrt(OsuPerformanceCalculator.PERFORMANCE_BASE_MULTIPLIER) * 0.027 * (Math.Cbrt(100000 / Math.Pow(2, 1 / 1.1) * basePerformance) + 4)
                : 0;

            double starRatingWithFlashlight = basePerformanceWithFlashlight > 0.00001
                ? Math.Cbrt(OsuPerformanceCalculator.PERFORMANCE_BASE_MULTIPLIER) * 0.027 * (Math.Cbrt(100000 / Math.Pow(2, 1 / 1.1) * basePerformanceWithFlashlight) + 4)
                : 0;

            // double starRatingWithFlashlightHidden = basePerformanceWithFlashlightHidden > 0.00001
            //     ? Math.Cbrt(OsuPerformanceCalculator.PERFORMANCE_BASE_MULTIPLIER) * 0.027 * (Math.Cbrt(100000 / Math.Pow(2, 1 / 1.1) * basePerformanceWithFlashlightHidden) + 4)
            //     : 0;

            double drainRate = beatmap.Difficulty.DrainRate;

            int hitCirclesCount = beatmap.HitObjects.Count(h => h is HitCircle);
            int sliderCount = beatmap.HitObjects.Count(h => h is Slider);
            int spinnerCount = beatmap.HitObjects.Count(h => h is Spinner);

            OsuDifficultyAttributes attributes = new OsuDifficultyAttributes
            {
                StarRating = starRating,
                StarRatingWithFlashlight = starRatingWithFlashlight,
                // StarRatingWithFlashlightHidden = starRatingWithFlashlightHidden,
                Mods = mods,
                AimDifficulty = aimRating,
                AimDifficultSliderCount = 0,
                SpeedDifficulty = speedRating,
                SpeedNoteCount = 0,
                FlashlightDifficulty = flashlightRating,
                SliderFactor = 0,
                AimDifficultStrainCount = 0,
                SpeedDifficultStrainCount = 0,
                DrainRate = drainRate,
                MaxCombo = beatmap.GetMaxCombo(),
                HitCircleCount = hitCirclesCount,
                SliderCount = sliderCount,
                SpinnerCount = spinnerCount,
            };

            return attributes;
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double clockRate)
        {
            List<DifficultyHitObject> objects = new List<DifficultyHitObject>();

            // The first jump is formed by the first two hitobjects of the map.
            // If the map has less than two OsuHitObjects, the enumerator will not return anything.
            for (int i = 1; i < beatmap.HitObjects.Count; i++)
            {
                var lastLast = i > 1 ? beatmap.HitObjects[i - 2] : null;
                objects.Add(new OsuDifficultyHitObject(beatmap.HitObjects[i], beatmap.HitObjects[i - 1], lastLast, clockRate, objects, objects.Count));
            }

            return objects;
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            // var modsWithHidden = mods.Append(new OsuModHidden()).ToArray();
            var skills = new List<Skill>
            {
                new Aim(mods, true),
                new Speed(mods),
                new Flashlight(mods),
                // new Flashlight(modsWithHidden)
            };

            return skills.ToArray();
        }

        protected override Mod[] DifficultyAdjustmentMods => new Mod[]
        {
            new OsuModTouchDevice(),
            new OsuModDoubleTime(),
            new OsuModHalfTime(),
            new OsuModEasy(),
            new OsuModHardRock(),
            new OsuModFlashlight(),
            new MultiMod(new OsuModFlashlight(), new OsuModHidden())
        };
    }
}
