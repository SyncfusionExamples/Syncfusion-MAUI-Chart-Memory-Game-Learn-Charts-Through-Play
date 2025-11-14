using Plugin.Maui.Audio;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ChartMemoryGame
{
    public partial class GameViewModel : INotifyPropertyChanged
    {
        #region Event Handler

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion

        #region Fields

        private readonly Random rand = new();
        private int round = 1;
        private int segments = 5;
        private const int maxRounds = 3;
        private int score = 0;
        private Func<int, Task>? sequenceHighlighter { get; set; }

        private List<int> sequence = new();
        private List<int> userInput = new();

        private bool isAnimating;
        private bool isAcceptingInput;
        private bool isPaused;
        private CancellationTokenSource? inputCts;
        private const int attemptTimeMs = 15000;
        private int remainingMs = attemptTimeMs;

        Color button = Color.FromArgb("#66FFFFFF");
        private string sequenceText = "Sequence: -";
        private bool winVisible;
        private double timerProgress;
        private bool showSequenceText = true;
        private bool startEnabled = true;
        private Brush startButtonStroke = Colors.Transparent;
        private Brush resumeButtonStroke = Colors.Transparent;
        private string activeAction = "Start";

        // Audio
        private IAudioManager? audioManager;
        private IAudioPlayer? happyPlayer;
        private IAudioPlayer? sadPlayer;
        private MemoryStream? happyStream;
        private MemoryStream? sadStream;

        ObservableCollection<Brush> paletteBrushes = new ObservableCollection<Brush>();
        ObservableCollection<Brush> chartColors;
        LinearGradientBrush chartBarGradient = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Offset = 0, Color = Color.FromArgb("#A3B6E6") },
                new GradientStop { Offset = 0.5f, Color = Color.FromArgb("#7F97DC") },
                new GradientStop { Offset = 1, Color = Color.FromArgb("#577BDC") }
            }
        };

        #endregion

        #region Properties

        public ObservableCollection<DataModel> ChartData { get; } = new();

        public ICommand PauseCommand { get; }

        public ICommand InfoCommand { get; }

        public ICommand StartCommand { get; }

        public ICommand ResetCommand { get; }

        public ObservableCollection<Brush> PaletteBrushes
        {
            get => paletteBrushes;
            private set { paletteBrushes = value; OnPropertyChanged(); }
        }

        public bool IsPaused
        {
            get => isPaused;
            private set { isPaused = value; OnPropertyChanged(); OnPropertyChanged(nameof(PauseButtonText)); }
        }

        public string PauseButtonText => IsPaused ? "Resume" : "Pause";

        public bool IsAnimating
        {
            get => isAnimating;
            private set { isAnimating = value; OnPropertyChanged(); }
        }

        public bool IsAcceptingInput
        {
            get => isAcceptingInput;
            private set { isAcceptingInput = value; OnPropertyChanged(); }
        }

        public int Round
        {
            get => round;
            private set { round = value; OnPropertyChanged(); }
        }

        public int Score
        {
            get => score;
            private set { score = value; OnPropertyChanged(); }
        }

        public string SequenceText
        {
            get => sequenceText;
            private set { sequenceText = value; OnPropertyChanged(); }
        }

        public bool WinVisible
        {
            get => winVisible;
            private set { winVisible = value; OnPropertyChanged(); }
        }

        public double TimerProgress
        {
            get => timerProgress;
            private set { timerProgress = value; OnPropertyChanged(); }
        }

        public bool ShowSequenceText
        {
            get => showSequenceText;
            set { showSequenceText = value; OnPropertyChanged(); }
        }

        public bool StartEnabled
        {
            get => startEnabled;
            private set { startEnabled = value; OnPropertyChanged(); }
        }

        public Brush StartButtonStroke
        {
            get => startButtonStroke;
            set { startButtonStroke = value; OnPropertyChanged(); }
        }

        public Brush ResumeButtonStroke
        {
            get => resumeButtonStroke;
            set { resumeButtonStroke = value; OnPropertyChanged(); }
        }

        public string ActiveAction
        {
            get => activeAction;
            set { activeAction = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsStartActive)); OnPropertyChanged(nameof(IsResumeActive)); }
        }

        public bool IsStartActive => string.Equals(ActiveAction, "Start", StringComparison.Ordinal);

        public bool IsResumeActive => string.Equals(ActiveAction, "Resume", StringComparison.Ordinal);

        #endregion
    }
}