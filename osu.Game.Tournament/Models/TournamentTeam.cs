// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;
using osu.Framework.Bindables;

namespace osu.Game.Tournament.Models
{
    /// <summary>
    /// A team representation. For official tournaments this is generally a country.
    /// </summary>
    [Serializable]
    public class TournamentTeam
    {
        /// <summary>
        /// The name of this team.
        /// </summary>
        public Bindable<string> FullName = new Bindable<string>(string.Empty);

        /// <summary>
        /// Name of the file containing the flag.
        /// </summary>
        public Bindable<string> FlagName = new Bindable<string>(string.Empty);

        /// <summary>
        /// Short acronym which appears in the group boxes post-selection.
        /// </summary>
        public Bindable<string> Acronym = new Bindable<string>(string.Empty);

        public BindableList<SeedingResult> SeedingResults = new BindableList<SeedingResult>();

        public Bindable<string> Seed = new Bindable<string>(string.Empty);

        public Bindable<string> TotalPoints = new Bindable<string>(string.Empty);

        public Bindable<string> Opponent = new Bindable<string>(string.Empty);

        public Bindable<int> LastYearPlacing = new BindableInt
        {
            MinValue = 0,
            MaxValue = 256
        };

        public Bindable<int> BestPlacing = new BindableInt
        {
            MinValue = 0,
            MaxValue = 256
        };

        public Bindable<string> BestPlacingYear = new Bindable<string>(string.Empty);

        public Bindable<int> QualifiersAverageScore = new BindableInt(0);

        public Bindable<double> QualifiersCarryFactor = new Bindable<double>(0);

        [JsonProperty]
        public BindableList<TournamentUser> Players { get; } = new BindableList<TournamentUser>();

        public TournamentTeam()
        {
            Acronym.ValueChanged += val =>
            {
                // use a sane default flag name based on acronym.
                if (val.OldValue.StartsWith(FlagName.Value, StringComparison.InvariantCultureIgnoreCase))
                    FlagName.Value = val.NewValue?.Length >= 2 ? val.NewValue.Substring(0, 2).ToUpperInvariant() : string.Empty;
            };

            FullName.ValueChanged += val =>
            {
                // use a sane acronym based on full name.
                if (val.OldValue.StartsWith(Acronym.Value, StringComparison.InvariantCultureIgnoreCase))
                    Acronym.Value = val.NewValue?.Length >= 3 ? val.NewValue.Substring(0, 3).ToUpperInvariant() : string.Empty;
            };
        }

        public override string ToString() => FullName.Value ?? Acronym.Value;
    }
}
