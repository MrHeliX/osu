// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuPerformanceCalculator : PerformanceCalculator
    {
        public new OsuDifficultyAttributes Attributes => (OsuDifficultyAttributes)base.Attributes;

        private Mod[] mods;

        private double accuracy;
        private int scoreMaxCombo;
        private int countGreat;
        private int countOk;
        private int countMeh;
        private int countMiss;

        public OsuPerformanceCalculator(Ruleset ruleset, DifficultyAttributes attributes, ScoreInfo score)
            : base(ruleset, attributes, score)
        {
        }

        public override double Calculate(Dictionary<string, double> categoryRatings = null)
        {
            mods = Score.Mods;
            accuracy = Score.Accuracy;
            scoreMaxCombo = Score.MaxCombo;
            countGreat = Score.Statistics.GetValueOrDefault(HitResult.Great);
            countOk = Score.Statistics.GetValueOrDefault(HitResult.Ok);
            countMeh = Score.Statistics.GetValueOrDefault(HitResult.Meh);
            countMiss = Score.Statistics.GetValueOrDefault(HitResult.Miss);

            double multiplier = 1.12; // This is being adjusted to keep the final pp value scaled around what it used to be when changing things.

            if (mods.Any(m => m is OsuModNoFail))
                multiplier *= 0.90f;

            if (mods.Any(m => m is OsuModSpunOut))
                multiplier *= 0.95f;

            double aimValue = computeAimValue();
            double speedValue = computeSpeedValue();
            double accuracyValue = computeAccuracyValue();
            double totalValue =
                Math.Pow(
                    Math.Pow(aimValue, 1.1) +
                    Math.Pow(speedValue, 1.1) +
                    Math.Pow(accuracyValue, 1.1), 1.0 / 1.1
                ) * multiplier;

            if (categoryRatings != null)
            {
                categoryRatings.Add("Aim", aimValue);
                categoryRatings.Add("Speed", speedValue);
                categoryRatings.Add("Accuracy", accuracyValue);
                categoryRatings.Add("OD", Attributes.OverallDifficulty);
                categoryRatings.Add("AR", Attributes.ApproachRate);
                categoryRatings.Add("Max Combo", Attributes.MaxCombo);
            }

            return totalValue;
        }

        private double computeAimValue()
        {
            double aimValue = Math.Pow(5.0f * Math.Max(1.0f, Attributes.AimStrain / 0.0675f) - 4.0f, 3.0f) / 100000.0f;

            // Longer maps are worth more
            double lengthBonus = 0.95f + 0.4f * Math.Min(1.0f, totalHits / 2000.0f) +
                (totalHits > 2000 ? Math.Log10(totalHits / 2000.0f) * 0.5f : 0.0f);

            aimValue *= lengthBonus;

            // Penalize misses exponentially. This mainly fixes tag4 maps and the likes until a per-hitobject solution is available
            aimValue *= Math.Pow(0.97f, countMiss);

            // Combo scaling
            if (Attributes.MaxCombo > 0)
                aimValue *= Math.Min(Math.Pow(scoreMaxCombo, 0.8f) / Math.Pow(Attributes.MaxCombo, 0.8f), 1.0f);

            double approachRateFactor = 1.0f;
            if (Attributes.ApproachRate > 10.33f)
                approachRateFactor += 0.45f * (Attributes.ApproachRate - 10.33f);
            else if (Attributes.ApproachRate < 8.0f)
            {
                // HD is worth more with lower ar!
                if (mods.Any(h => h is OsuModHidden))
                    approachRateFactor += 0.02f * (8.0f - Attributes.ApproachRate);
                else
                    approachRateFactor += 0.01f * (8.0f - Attributes.ApproachRate);
            }

            aimValue *= approachRateFactor;

            // We want to give more reward for lower AR when it comes to aim and HD. This nerfs high AR and buffs lower AR.
            if (mods.Any(m => m is OsuModHidden))
                aimValue *= 1.18f;

            if (mods.Any(h => h is OsuModFlashlight))
                aimValue *= 1.45f;

            // Scale the aim value with accuracy _slightly_
            aimValue *= 0.5f + accuracy / 2.0f;
            // It is important to also consider accuracy difficulty when doing that
            aimValue *= 0.98f + Math.Pow(Attributes.OverallDifficulty, 2) / 1500;

            return aimValue;
        }

        private double computeSpeedValue()
        {
            double speedValue = Math.Pow(5.0f * Math.Max(1.0f, Attributes.SpeedStrain / 0.0675f) - 4.0f, 3.0f) / 100000.0f;

            // Longer maps are worth more
            speedValue *= 0.95f + 0.4f * Math.Min(1.0f, totalHits / 2000.0f) +
                (totalHits > 2000 ? Math.Log10(totalHits / 2000.0f) * 0.5f : 0.0f);

            // Penalize misses exponentially. This mainly fixes tag4 maps and the likes until a per-hitobject solution is available
            speedValue *= Math.Pow(0.97f, countMiss);

            // Combo scaling
            if (Attributes.MaxCombo > 0)
                speedValue *= Math.Min(Math.Pow(scoreMaxCombo, 0.8f) / Math.Pow(Attributes.MaxCombo, 0.8f), 1.0f);

            // Scale the speed value with accuracy _slightly_
            speedValue *= 0.5f + accuracy / 2.0f;
            // It is important to also consider accuracy difficulty when doing that
            speedValue *= 0.98f + Math.Pow(Attributes.OverallDifficulty, 2) / 2500;

            return speedValue;
        }

        private double computeAccuracyValue()
        {
            if (mods.Any(h => h is OsuModRelax))
                return 0.0;

            // This percentage only considers HitCircles of any value - in this part of the calculation we focus on hitting the timing hit window.
            double betterAccuracyPercentage;
            int amountHitObjectsWithAccuracy = Attributes.HitCircleCount;

            if (amountHitObjectsWithAccuracy > 0)
                betterAccuracyPercentage = ((countGreat - (totalHits - amountHitObjectsWithAccuracy)) * 6 + countOk * 2 + countMeh) / (double)(amountHitObjectsWithAccuracy * 6);
            else
                betterAccuracyPercentage = 0;

            // It is possible to reach a negative accuracy with this formula. Cap it at zero - zero points.
            if (betterAccuracyPercentage < 0)
                betterAccuracyPercentage = 0;

            // Lots of arbitrary values from testing.
            // Considering to use derivation from perfect accuracy in a probabilistic manner - assume normal distribution.
            double accuracyValue = Math.Pow(1.52163, Attributes.OverallDifficulty) * Math.Pow(betterAccuracyPercentage, 24) * 2.83;

            // Bonus for many hitcircles - it's harder to keep good accuracy up for longer.
            accuracyValue *= Math.Min(1.15, Math.Pow(amountHitObjectsWithAccuracy / 1000.0, 0.3));

            if (mods.Any(m => m is OsuModHidden))
                accuracyValue *= 1.02f;
            if (mods.Any(m => m is OsuModFlashlight))
                accuracyValue *= 1.02f;

            return accuracyValue;
        }

        private int totalHits => countGreat + countOk + countMeh + countMiss;
        private int totalSuccessfulHits => countGreat + countOk + countMeh;
    }
}
