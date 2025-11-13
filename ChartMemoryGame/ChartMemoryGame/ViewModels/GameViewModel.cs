using Microsoft.Maui.Controls.Shapes;
using Plugin.Maui.Audio;
using Syncfusion.Maui.Charts;
using Syncfusion.Maui.Popup;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ChartMemoryGame
{
    public partial class GameViewModel
    {
        #region Contructor

        public GameViewModel()
        {
            audioManager = AudioManager.Current;
            InitializeRound(true);

            PauseCommand = new Command(ExecutePauseCommand);
            InfoCommand = new Command<object>(ExecuteInfoCommand);
            StartCommand = new Command<object>(ExecuteStartCommand);
            ResetCommand = new Command<object>(ExecuteResetCommand);

            chartColors = new ObservableCollection<Brush>()
            {
                chartBarGradient
            };

            PaletteBrushes = chartColors;
        }

        #endregion

        #region Methods

        #region Public Methods

        public async Task EnsureAudioAsync()
        {
            try
            {
                if (audioManager == null) return;

                if (happyPlayer == null)
                {
                    using var happyPkg = await FileSystem.OpenAppPackageFileAsync("happy.mp3");
                    happyStream = new MemoryStream();
                    await happyPkg.CopyToAsync(happyStream);
                    happyStream.Position = 0;
                    happyPlayer = audioManager.CreatePlayer(happyStream);
                }

                if (sadPlayer == null)
                {
                    using var sadPkg = await FileSystem.OpenAppPackageFileAsync("sad.mp3");
                    sadStream = new MemoryStream();
                    await sadPkg.CopyToAsync(sadStream);
                    sadStream.Position = 0;
                    sadPlayer = audioManager.CreatePlayer(sadStream);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing audio: {ex.Message}");
            }
        }

        public void InitializeRound(bool resetToFirst)
        {
            if (resetToFirst)
            {
                Round = 1;
                Score = 0;
            }

            segments = Round * 5;
            attemptsDoneThisRound = 0;
            userInput.Clear();
            IsAnimating = false;
            IsAcceptingInput = false;
            WinVisible = false;
            ActiveAction = "Resume";

            BuildChartData(segments);

            GenerateSequenceForRound();
            DisplaySequenceInLabel();
            AttemptStatus = $"{attemptsPerRound} more to complete";
            UpdateAttemptsDots();
            StartEnabled = true;
            TimerProgress = 0;
            IsPaused = true;
            ActiveAction = "Resume";
            remainingMs = attemptTimeMs;

            // Reset UI visuals
            ShowSequenceText = true;
            StartButtonStroke = Colors.Transparent;
            ResumeButtonStroke = Colors.Transparent;
        }

        public async Task StartRoundAsync(Func<int, Task> highlight)
        {
            if (IsAnimating) return;

            sequenceHighlighter = highlight;

            userInput.Clear();
            StartEnabled = false;
            IsPaused = false;
            ActiveAction = "Start";

            StartButtonStroke = button;
            ResumeButtonStroke = Colors.Transparent;

            // brief delay
            await Task.Delay(300);

            // Animate full sequence with orange
            IsAnimating = true;
            IsAcceptingInput = false;
            foreach (var n in sequence)
            {
                await (sequenceHighlighter?.Invoke(n - 1) ?? highlight(n - 1));
            }

            // Start input phase with timer
            IsAnimating = false;
            IsAcceptingInput = true;
            remainingMs = attemptTimeMs;
            ShowSequenceText = false;
            _ = RunInputTimerAsync();
        }

        public async Task OnTappedAsync(int tappedIndex, Func<int, Task> flash)
        {
            if (IsAnimating || !IsAcceptingInput || IsPaused) return;

            int tappedNumber = tappedIndex + 1;
            _ = flash(tappedIndex);

            userInput.Add(tappedNumber);
            int pos = userInput.Count - 1;
            if (userInput[pos] != sequence[pos])
            {
                FailAttempt();
                await ShowAlertAsync("Game Over", "You've failed. Please start again from level 1.");
                InitializeRound(true);
                return;
            }

            Score++;

            if (userInput.Count == sequence.Count)
            {
                attemptsDoneThisRound++;
                UpdateAttemptsDots();
                if (attemptsDoneThisRound < attemptsPerRound)
                {
                    int remaining = attemptsPerRound - attemptsDoneThisRound;
                    AttemptStatus = remaining == 1 ? "1 more to complete" : $"{remaining} more to complete";

                    userInput.Clear();
                    IsAcceptingInput = false;
                    await Task.Delay(500);
                    // Replay using the stored sequence highlighter (orange), not the green tap flash
                    if (sequenceHighlighter != null)
                        await StartRoundAsync(sequenceHighlighter);
                    else
                        await StartRoundAsync(flash);
                }
                else
                {
                    IsAcceptingInput = false;
                    AttemptStatus = string.Empty;
                    PlayHappy();
                    await ShowAlertAsync("Round Complete", $"You have successfully completed round {Round}.");

                    if (Round >= maxRounds)
                    {
                        WinVisible = true;
                        StartEnabled = false;
                        SequenceText = "Sequence: -";
                        return;
                    }

                    Round++;
                    InitializeRound(false);
                }
            }
        }

        public async Task HighlightBarAsync(int index, SolidColorBrush highlightColor, int durationMs, int gapMs)
        {
            var brushes = new ObservableCollection<Brush>();
            for (int i = 0; i < ChartData.Count; i++)
            {
                if (i == index)
                {
                    brushes.Add(highlightColor);
                }
                else
                {
                    brushes.Add(chartBarGradient);
                }
            }

            PaletteBrushes = brushes;
            await Task.Delay(durationMs);

            // Revert to default between highlights
            var defaultBrushes = new ObservableCollection<Brush>();
            for (int i = 0; i < ChartData.Count; i++)
            {
                defaultBrushes.Add(chartBarGradient);
            }

            PaletteBrushes = defaultBrushes;
            await Task.Delay(gapMs);
        }

        #endregion

        #region Private Methods

        private void PlayHappy() { try { happyPlayer?.Stop(); happyPlayer?.Play(); } catch { } }
        private void PlaySad() { try { sadPlayer?.Stop(); sadPlayer?.Play(); } catch { } }

        private void BuildChartData(int segments)
        {
            ChartData.Clear();
            for (int i = 1; i <= segments; i++)
            {
                ChartData.Add(new DataModel { Category = i.ToString(), Value = 8.5 });
            }
        }

        private void GenerateSequenceForRound()
        {
            int length = (Round == 1) ? 3 : 5;
            sequence = Enumerable.Range(1, segments)
                                   .OrderBy(_ => rand.Next())
                                   .Take(length)
                                   .ToList();
        }

        private void DisplaySequenceInLabel()
        {
            SequenceText = $"Sequence: {string.Join(", ", sequence)}";
        }

        private async Task RunInputTimerAsync()
        {
            inputCts?.Cancel();
            inputCts = new CancellationTokenSource();
            var token = inputCts.Token;
            remainingMs = attemptTimeMs;
            const int step = 100; // ms

            while (remainingMs > 0 && !token.IsCancellationRequested)
            {
                if (!IsPaused && IsAcceptingInput)
                {
                    try { await Task.Delay(step, token); } catch { }
                    remainingMs -= step;
                    TimerProgress = 1.0 - Math.Max(0, Math.Min(attemptTimeMs, remainingMs)) / (double)attemptTimeMs;
                }
                else
                {
                    try { await Task.Delay(step, token); } catch { }
                }
            }

            if (remainingMs <= 0 && !token.IsCancellationRequested && IsAcceptingInput)
            {
                FailAttempt();
                await ShowAlertAsync("Time's up", "You ran out of time. Please start again from level 1.");
                InitializeRound(true);
            }
        }

        private void FailAttempt()
        {
            IsAcceptingInput = false;
            inputCts?.Cancel();
            PlaySad();
        }

        private void TogglePause()
        {
            if (IsAnimating) return;

            IsPaused = !IsPaused;
            IsAcceptingInput = !IsPaused;
            ActiveAction = IsPaused ? "Resume" : "Pause";
            StartButtonStroke = Colors.Transparent;
            ResumeButtonStroke = button;
        }

        private void UpdateAttemptsDots()
        {
            int remaining = Math.Max(0, attemptsPerRound - attemptsDoneThisRound);
            AttemptsDots = remaining switch
            {
                3 => "●●●",
                2 => "●●○",
                1 => "●○○",
                _ => "○○○"
            };
        }

        private async Task ShowAlertAsync(string title, string message)
        {
            try
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (page != null)
                    await page.DisplayAlert(title, message, "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing alert: {ex.Message}");
            }
        }

        private void ExecutePauseCommand()
        {
            TogglePause();
        }

        private void ExecuteInfoCommand(object parameter)
        {
            if (parameter is not View view) return;

            var popup = new SfPopup
            {
                ShowFooter = false,
                ShowHeader = false,
                WidthRequest = 140,
                HeightRequest = 50,
                AutoCloseDuration = 1300,
                PopupStyle = new PopupStyle
                {
                    CornerRadius = 8,
                    PopupBackground = Colors.Transparent
                },
                ContentTemplate = new DataTemplate(() =>
                {
                    var popupContent = new Label
                    {
                        Text = SequenceText,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        TextColor = Color.FromArgb("#022c1d")
                    };

                    var border = new Border
                    {
                        Stroke = Color.FromArgb("#05965b"),
                        StrokeThickness = 2,
                        BackgroundColor = Color.FromArgb("#6ee7ab"),
                        Padding = 5,
                        StrokeShape = new RoundRectangle { CornerRadius = 8 },
                        Content = popupContent
                    };

                    return border;
                })
            };

            popup.ShowRelativeToView(view, PopupRelativePosition.AlignBottom);
        }

        private async void ExecuteStartCommand(object param)
        {
            ShowSequenceText = false;

            if (param is DataPointSelectionBehavior selectionBehavior)
                selectionBehavior.ClearSelection();

            await StartRoundAsync(i => HighlightBarAsync(i, new SolidColorBrush(Color.FromArgb("#ff9fa7")), 450, 150));
        }

        private void ExecuteResetCommand(object param)
        {
            InitializeRound(true);
            if (param is DataPointSelectionBehavior selectionBehavior)
                selectionBehavior.ClearSelection();

            ShowSequenceText = true;
        }

        #endregion

        #endregion
    }
}