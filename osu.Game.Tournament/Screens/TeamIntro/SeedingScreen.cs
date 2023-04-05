// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using NuGet.Versioning;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Ladder.Components;
using osuTK;

namespace osu.Game.Tournament.Screens.TeamIntro
{
    public partial class SeedingScreen : TournamentMatchScreen
    {
        private Container mainContainer;

        private readonly Bindable<TournamentTeam> currentTeam = new Bindable<TournamentTeam>();
        private readonly Bindable<TournamentTeam> otherTeam = new Bindable<TournamentTeam>();

        private TourneyButton showFirstTeamButton;
        private TourneyButton showSecondTeamButton;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                new TourneyVideo("seeding")
                {
                    RelativeSizeAxes = Axes.Both,
                    Loop = true,
                },
                mainContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                },
                new ControlPanel
                {
                    Children = new Drawable[]
                    {
                        showFirstTeamButton = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Show first team",
                            Action = () => currentTeam.Value = CurrentMatch.Value.Team1.Value,
                        },
                        showSecondTeamButton = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Show second team",
                            Action = () => currentTeam.Value = CurrentMatch.Value.Team2.Value,
                        },
                        new SettingsTeamDropdown(LadderInfo.Teams)
                        {
                            LabelText = "Show specific team",
                            Current = currentTeam,
                        }
                    }
                }
            };

            currentTeam.BindValueChanged(teamChanged, true);
        }

        private void teamChanged(ValueChangedEvent<TournamentTeam> team) => updateTeamDisplay();

        public override void Show()
        {
            base.Show();

            // Changes could have been made on editor screen.
            // Rather than trying to track all the possibilities (teams / players / scores) just force a full refresh.
            updateTeamDisplay();
        }

        protected override void CurrentMatchChanged(ValueChangedEvent<TournamentMatch> match)
        {
            base.CurrentMatchChanged(match);

            if (match.NewValue == null)
            {
                showFirstTeamButton.Enabled.Value = false;
                showSecondTeamButton.Enabled.Value = false;
                return;
            }

            showFirstTeamButton.Enabled.Value = true;
            showSecondTeamButton.Enabled.Value = true;

            currentTeam.Value = match.NewValue.Team1.Value;
        }

        private void updateTeamDisplay() => Scheduler.AddOnce(() =>
        {
            if (currentTeam.Value == null)
            {
                mainContainer.Clear();
                return;
            }

            int otherIndex = 65 - int.Parse(currentTeam.Value.Seed.Value) - 1;
            if (otherIndex >= 0)
                otherTeam.Value = LadderInfo.Teams[otherIndex];
            else
                otherTeam.Value = null;

            mainContainer.Children = new Drawable[]
            {
                new LeftInfo(currentTeam.Value) { Position = new Vector2(55, 150), },
                new MiddleInfo(currentTeam.Value, otherTeam.Value) { Position = new Vector2(500, 150), },
                new RightInfo(otherTeam.Value) { Position = new Vector2(200, 150)  },
            };
        });

        private partial class RightInfo : CompositeDrawable
        {
            public RightInfo(TournamentTeam team)
            {
                Width = 200;
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopRight;

                if (team == null) return;

                InternalChildren = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Anchor = Anchor.TopRight,
                        Children = new Drawable[]
                        {
                            new TeamDisplay(team) { Margin = new MarginPadding { Bottom = 30 } },
                            new RowDisplay("Rank", $"#{team.GlobalRank:#,0}"),
                            new RowDisplay("NL Rank", $"#{team.CountryRank:#,0}"),
                            new RowDisplay("NBB 2022", team.LastYearPlacing.Value > 0 ? $"#{team.LastYearPlacing:#,0}" : "-"),

                            new Container { Margin = new MarginPadding { Bottom = 30 } },

                            new RowDisplay("Punten", team.TotalPoints.Value),
                            new RowDisplay("Seed", "#" + team.Seed.Value)
                        }
                    },
                };
            }

            internal partial class RowDisplay : CompositeDrawable
            {
                public RowDisplay(string left, string right)
                {
                    AutoSizeAxes = Axes.Y;
                    RelativeSizeAxes = Axes.X;
                    Anchor = Anchor.TopRight;

                    InternalChildren = new Drawable[]
                    {
                        new TournamentSpriteText
                        {
                            Text = right,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Font = OsuFont.Torus.With(size: 22, weight: FontWeight.Regular),
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopRight
                        },
                        new TournamentSpriteText
                        {
                            Text = left,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Font = OsuFont.Torus.With(size: 22, weight: FontWeight.SemiBold),
                        },
                    };
                }
            }

            private partial class TeamDisplay : DrawableTournamentTeam
            {
                public TeamDisplay(TournamentTeam team)
                    : base(team)
                {
                    AutoSizeAxes = Axes.Both;
                    Anchor = Anchor.TopRight;

                    Flag.RelativeSizeAxes = Axes.None;
                    Flag.Scale = new Vector2(1.2f);
                    Flag.Anchor = Anchor.TopRight;
                    Flag.Origin = Anchor.TopRight;

                    InternalChild = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 5),
                        Margin = new MarginPadding { Left = 50 },
                        Children = new Drawable[]
                        {
                            Flag,
                            new OsuSpriteText
                            {
                                Text = team?.FullName.Value ?? "???",
                                Font = OsuFont.Torus.With(size: 32, weight: FontWeight.SemiBold),
                                Colour = TournamentGame.TEXT_COLOUR,
                            },
                        }
                    };
                }
            }
        }

        private partial class MiddleInfo : CompositeDrawable
        {
            public MiddleInfo(TournamentTeam team, TournamentTeam otherTeam)
            {
                FillFlowContainer fill;

                Width = 200;

                InternalChildren = new Drawable[]
                {
                    fill = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Anchor = Anchor.TopCentre
                    },
                };

                int seedIndex = 0;
                foreach (var seeding in team.SeedingResults)
                {
                    var otherSeeding = otherTeam?.SeedingResults[seedIndex];
                    fill.Add(new ModRow(seeding.Mod.Value, seeding.Seed.Value, otherSeeding?.Seed.Value ?? 0));
                    int beatmapIndex = 0;
                    foreach (var beatmap in seeding.Beatmaps)
                    {
                        if (beatmap.Beatmap == null)
                            continue;

                        var otherBeatmap = otherSeeding?.Beatmaps[beatmapIndex];

                        fill.Add(new BeatmapScoreRow(beatmap, otherBeatmap));
                        beatmapIndex++;
                    }
                    seedIndex++;
                }
            }

            internal partial class RowDisplay : CompositeDrawable
            {
                public RowDisplay(string left, string middle, string right)
                {
                    AutoSizeAxes = Axes.Y;
                    RelativeSizeAxes = Axes.X;
                    Anchor = Anchor.TopRight;

                    InternalChildren = new Drawable[]
                    {
                        new TournamentSpriteText
                        {
                            Text = right,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 20),
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopRight,
                            Margin = new MarginPadding { Right = 60 }
                        },
                        new TournamentSpriteText
                        {
                            Text = middle,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 20),
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopRight
                        },
                        new TournamentSpriteText
                        {
                            Text = left,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Font = OsuFont.Torus.With(size: 20),
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                        },
                    };
                }
            }

            private partial class BeatmapScoreRow : CompositeDrawable
            {
                public BeatmapScoreRow(SeedingBeatmap beatmap, SeedingBeatmap otherBeatmap)
                {
                    Debug.Assert(beatmap.Beatmap != null);

                    RelativeSizeAxes = Axes.X;
                    AutoSizeAxes = Axes.Y;
                    Anchor = Anchor.TopCentre;

                    InternalChildren = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(40),
                            Margin = new MarginPadding { Right = 490 },
                            Children = new Drawable[]
                            {
                                new RowDisplay(beatmap.Score.ToString("#,0"), beatmap.Points.Value.ToString(), "#" + beatmap.Seed.Value.ToString("#,0")),
                            }
                        },

                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(5),
                            Margin = new MarginPadding { Right = 97 },
                            Children = new Drawable[]
                            {
                                new TournamentSpriteText { Text = beatmap.Acronym.ToString(), Colour = TournamentGame.TEXT_COLOUR, Font = OsuFont.Torus.With(size: 20) },
                            }
                        },

                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(40),
                            Margin = new MarginPadding { Left = 20 },
                            Children = new Drawable[]
                            {
                                new TournamentSpriteText { Text = otherBeatmap?.Score.ToString("#,0") ?? "", Colour = TournamentGame.TEXT_COLOUR, Width = 80, Font = OsuFont.Torus.With(size: 20) },
                                new TournamentSpriteText { Text = otherBeatmap?.Points.Value.ToString() ?? "", Colour = TournamentGame.TEXT_COLOUR, Font = OsuFont.Torus.With(size: 20) },
                                new TournamentSpriteText
                                    { Text = otherBeatmap != null ? "#" + otherBeatmap.Seed.Value.ToString("#,0") : "", Colour = TournamentGame.TEXT_COLOUR, Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 20) },
                            }
                        },
                    };
                }
            }

            private partial class ModRow : CompositeDrawable
            {
                private readonly string mods;
                private readonly int seeding;
                private readonly int otherSeeding;

                public ModRow(string mods, int seeding, int otherSeeding)
                {
                    this.mods = mods;
                    this.seeding = seeding;
                    this.otherSeeding = otherSeeding;

                    Padding = new MarginPadding { Vertical = 10 };

                    AutoSizeAxes = Axes.Y;
                }

                [BackgroundDependencyLoader]
                private void load(TextureStore textures)
                {
                    FillFlowContainer row;

                    InternalChildren = new Drawable[]
                    {
                        row = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(5),
                        },
                    };

                    row.Add(new Container
                    {
                        Size = new Vector2(50, 16),
                        CornerRadius = 10,
                        Masking = true,
                        Children = new Drawable[]
    {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = TournamentGame.ELEMENT_BACKGROUND_COLOUR,
                            },
                            new TournamentSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "#" + seeding.ToString("#,0"),
                                Colour = TournamentGame.ELEMENT_FOREGROUND_COLOUR
                            },
    }
                    });

                    if (!string.IsNullOrEmpty(mods))
                    {
                        row.Add(new TournamentSpriteText
                        {
                            Text = mods,
                            Colour = TournamentGame.TEXT_COLOUR
                        });
                    }

                    if (otherSeeding > 0)
                    {
                        row.Add(new Container
                        {
                            Size = new Vector2(50, 16),
                            CornerRadius = 10,
                            Masking = true,
                            Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = TournamentGame.ELEMENT_BACKGROUND_COLOUR,
                            },
                            new TournamentSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "#" + otherSeeding.ToString("#,0"),
                                Colour = TournamentGame.ELEMENT_FOREGROUND_COLOUR
                            },
                        }
                        });
                    }
                }
            }
        }

        private partial class LeftInfo : CompositeDrawable
        {
            public LeftInfo(TournamentTeam team)
            {
                Width = 200;

                if (team == null) return;

                InternalChildren = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new TeamDisplay(team) { Margin = new MarginPadding { Bottom = 30 } },
                            new RowDisplay("Rank", $"#{team.GlobalRank:#,0}"),
                            new RowDisplay(team.Country + " Rank", $"#{team.CountryRank:#,0}"),
                            new RowDisplay("NBB 2022", team.LastYearPlacing.Value > 0 ? $"#{team.LastYearPlacing:#,0}" : "-"),

                            new Container { Margin = new MarginPadding { Bottom = 30 } },

                            new RowDisplay("Punten", team.TotalPoints.Value),
                            new RowDisplay("Seed", "#" + team.Seed.Value)
                        }
                    },
                };
            }

            internal partial class RowDisplay : CompositeDrawable
            {
                public RowDisplay(string left, string right)
                {
                    AutoSizeAxes = Axes.Y;
                    RelativeSizeAxes = Axes.X;

                    InternalChildren = new Drawable[]
                    {
                        new TournamentSpriteText
                        {
                            Text = left,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Font = OsuFont.Torus.With(size: 22, weight: FontWeight.SemiBold),
                        },
                        new TournamentSpriteText
                        {
                            Text = right,
                            Colour = TournamentGame.TEXT_COLOUR,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopLeft,
                            Font = OsuFont.Torus.With(size: 22, weight: FontWeight.Regular),
                        },
                    };
                }
            }

            private partial class TeamDisplay : DrawableTournamentTeam
            {
                public TeamDisplay(TournamentTeam team)
                    : base(team)
                {
                    AutoSizeAxes = Axes.Both;

                    Flag.RelativeSizeAxes = Axes.None;
                    Flag.Scale = new Vector2(1.2f);

                    InternalChild = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 5),
                        Children = new Drawable[]
                        {
                            Flag,
                            new OsuSpriteText
                            {
                                Text = team?.FullName.Value ?? "???",
                                Font = OsuFont.Torus.With(size: 32, weight: FontWeight.SemiBold),
                                Colour = TournamentGame.TEXT_COLOUR,
                            },
                        }
                    };
                }
            }
        }
    }
}
