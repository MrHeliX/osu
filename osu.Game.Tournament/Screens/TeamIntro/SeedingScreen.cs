// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
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
        private Container mainContainer = null!;

        private readonly Bindable<TournamentTeam?> currentTeam = new Bindable<TournamentTeam?>();

        private TourneyButton showFirstTeamButton = null!;
        private TourneyButton showSecondTeamButton = null!;

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
                            Action = () => currentTeam.Value = CurrentMatch.Value?.Team1.Value,
                        },
                        showSecondTeamButton = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Show second team",
                            Action = () => currentTeam.Value = CurrentMatch.Value?.Team2.Value,
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

        private void teamChanged(ValueChangedEvent<TournamentTeam?> team) => updateTeamDisplay();

        public override void Show()
        {
            base.Show();

            // Changes could have been made on editor screen.
            // Rather than trying to track all the possibilities (teams / players / scores) just force a full refresh.
            updateTeamDisplay();
        }

        protected override void CurrentMatchChanged(ValueChangedEvent<TournamentMatch?> match)
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

            mainContainer.Children = new Drawable[]
            {
                new LeftInfo(currentTeam.Value) { Position = new Vector2(55, 150), },
                new RightInfo(currentTeam.Value) { Position = new Vector2(500, 150), },
            };
        });

        private partial class RightInfo : CompositeDrawable
        {
            public RightInfo(TournamentTeam team)
            {
                FillFlowContainer fill;

                Width = 600;

                InternalChildren = new Drawable[]
                {
                    fill = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                    },
                };

                foreach (var seeding in team.SeedingResults)
                {
                    fill.Add(new ModRow(seeding.Mod.Value, seeding.Seed.Value));

                    foreach (var beatmap in seeding.Beatmaps)
                    {
                        if (beatmap.Beatmap == null)
                            continue;

                        fill.Add(new BeatmapScoreRow(beatmap));
                    }
                }
            }

            private partial class BeatmapScoreRow : CompositeDrawable
            {
                public BeatmapScoreRow(SeedingBeatmap beatmap)
                {
                    Debug.Assert(beatmap.Beatmap != null);

                    RelativeSizeAxes = Axes.X;
                    AutoSizeAxes = Axes.Y;

                    InternalChildren = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(5),
                            Children = new Drawable[]
                            {
                                new TournamentSpriteText { Text = beatmap.Beatmap.Metadata.Title, Colour = TournamentGame.TEXT_COLOUR, Font = OsuFont.Torus.With(size: 18) },
                                new TournamentSpriteText { Text = "by", Colour = TournamentGame.TEXT_COLOUR, Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 18) },
                                new TournamentSpriteText { Text = beatmap.Beatmap.Metadata.Artist, Colour = TournamentGame.TEXT_COLOUR, Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 18) },
                            }
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(40),
                            Children = new Drawable[]
                            {
                                new TournamentSpriteText { Text = beatmap.Score.ToString("#,0"), Colour = TournamentGame.TEXT_COLOUR, Width = 80, Font = OsuFont.Torus.With(size: 18) },
                                new TournamentSpriteText
                                    { Text = beatmap.Points.Value + "p | #" + beatmap.Seed.Value.ToString("#,0"), Colour = TournamentGame.TEXT_COLOUR, Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 18) },
                            }
                        },
                    };
                }
            }

            private partial class ModRow : CompositeDrawable
            {
                private readonly string mods;
                private readonly int seeding;

                public ModRow(string mods, int seeding)
                {
                    this.mods = mods;
                    this.seeding = seeding;

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

                    if (!string.IsNullOrEmpty(mods))
                    {
                        row.Add(new Sprite
                        {
                            Texture = textures.Get($"Mods/{mods.ToLowerInvariant()}"),
                            Scale = new Vector2(0.5f)
                        });
                    }

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
                                Text = $"#{seeding.ToString("#,0")}",
                                Colour = TournamentGame.ELEMENT_FOREGROUND_COLOUR
                            },
                        }
                    });
                }
            }
        }

        private partial class LeftInfo : CompositeDrawable
        {
            public LeftInfo(TournamentTeam? team)
            {
                FillFlowContainer fill;

                Width = 200;

                if (team == null) return;

                InternalChildren = new Drawable[]
                {
                    fill = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new TeamDisplay(team) { Margin = new MarginPadding { Bottom = 30 } },
                            new RowDisplay("PNK 2022:", team.LastYearPlacing.Value == 999 ? "NQ" : (team.LastYearPlacing.Value > 0 ? $"#{team.LastYearPlacing:#,0}" : "-")),
                            new RowDisplay("Best result:", team.BestPlacing.Value == 999 ? $"NQ ({team.BestPlacingYear})" : team.BestPlacing.Value > 0 ? $"#{team.BestPlacing:#,0} ({team.BestPlacingYear})" : "-"),
                            new Container { Margin = new MarginPadding { Bottom = 30 } },

                            new TournamentSpriteText
                            {
                                Text = team.TotalPoints.Value + " points",
                                Font = OsuFont.Torus.With(size: 48)
                            },
                            new TournamentSpriteText
                            {
                                Text = "Seed #" + team.Seed.Value,
                                Font = OsuFont.Torus.With(size: 26)
                            },
                            new TournamentSpriteText
                            {
                                Text = "Opponent: " + team.Opponent.Value,
                                Font = OsuFont.Torus.With(size: 26)
                            },
                            new Container { Margin = new MarginPadding { Bottom = 15 } },
                            new TournamentSpriteText
                            {
                                Text = "Average score: " + team.QualifiersAverageScore.Value.ToString("#,0"),
                                Font = OsuFont.Torus.With(size: 20)
                            },
                            new TournamentSpriteText
                            {
                                Text = "Carry factor: " + team.QualifiersCarryFactor.Value.ToString("0.##"),
                                Font = OsuFont.Torus.With(size: 20)
                            },

                            new Container { Margin = new MarginPadding { Bottom = 20 } }
                        }
                    },
                };

                if (team.Players.Count > 0)
                {
                    fill.Add(new TournamentSpriteText
                    {
                        Text = "Players",
                        Font = OsuFont.Torus.With(size: 26)
                    });

                    foreach (var p in team.Players)
                        fill.Add(new RowDisplay(p.Username, p.QualifiersPerformance.ToString("0.###")));
                }
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
                public TeamDisplay(TournamentTeam? team)
                    : base(team)
                {
                    AutoSizeAxes = Axes.Both;

                    Flag.RelativeSizeAxes = Axes.None;
                    Flag.Scale = new Vector2(1.5f);

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
