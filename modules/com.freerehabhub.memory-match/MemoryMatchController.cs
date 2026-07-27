using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeRehabHub.Core;
using FreeRehabHub.Modules.Contracts;
using FreeRehabHub.Modules.MemoryMatch.Scoring;
using Godot;

namespace FreeRehabHub.Modules.MemoryMatch;

public partial class MemoryMatchController : Node, IExerciseModule
{
    private const int TotalPairs = 6;
    private const float MismatchFlipBackSeconds = 1.0f;

    private static readonly Color[] Palette =
    {
        Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow, Colors.Purple, Colors.Orange
    };

    [Export] private NodePath _statusLabelPath = null!;
    [Export] private NodePath _cardsContainerPath = null!;

    private readonly MemoryMatchScorer _scorer = new();
    private readonly RandomNumberGenerator _random = new();

    private Label _statusLabel = null!;
    private GridContainer _cardsContainer = null!;
    private Godot.Timer _mismatchTimer = null!;
    private Button[] _cardButtons = Array.Empty<Button>();
    private Action[] _cardPressedHandlers = Array.Empty<Action>();
    private Color[] _cardColors = Array.Empty<Color>();
    private bool[] _matched = Array.Empty<bool>();

    private ModuleContext _context = null!;
    private int? _firstFlippedIndex;
    private int _pendingMismatchFirstIndex;
    private int _pendingMismatchSecondIndex;
    private int _matchedPairs;
    private int _totalAttempts;
    private bool _isRunning;
    private bool _completed;

    public string ModuleId => Manifest.Id;

    public ModuleManifest Manifest { get; } = new()
    {
        Id = "com.freerehabhub.memory-match",
        Version = "1.0.0",
        Kind = ModuleKind.Exercise,
        DisplayName = new LocalizedText { Tr = "Hafıza Kartları", En = "Memory Match" },
        Description = new LocalizedText
        {
            Tr = "Ters çevrilmiş kart çiftlerini bulup eşleştiren, görsel-mekansal kısa süreli " +
                 "belleği çalıştıran, kamera gerektirmeyen bir bellek oyunu.",
            En = "A camera-free memory game that trains short-term visuospatial memory by " +
                 "finding and matching pairs of face-down cards."
        },
        Disciplines = new List<Discipline>
        {
            Discipline.OccupationalTherapy, Discipline.SpeechTherapy, Discipline.Psychology
        },
        DifficultyRange = new DifficultyRange { Min = 1, Max = 1 },
        RequiredCapabilities = new List<string>(),
        MinAppVersion = "0.1.0",
        EntryPointType = "FreeRehabHub.Modules.MemoryMatch.MemoryMatchController",
        ScenePath = "res://modules/com.freerehabhub.memory-match/MemoryMatch.tscn",
        MetricLabels = new Dictionary<string, LocalizedText>
        {
            ["totalPairs"] = new LocalizedText { Tr = "Toplam Çift", En = "Total Pairs" },
            ["matchedPairs"] = new LocalizedText { Tr = "Bulunan Çift", En = "Matched Pairs" },
            ["totalAttempts"] = new LocalizedText { Tr = "Deneme Sayısı", En = "Attempt Count" }
        }
    };

    public event EventHandler<ModuleResult>? Completed;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>(_statusLabelPath);
        _cardsContainer = GetNode<GridContainer>(_cardsContainerPath);
        _cardButtons = _cardsContainer.GetChildren().Cast<Button>().ToArray();
        _matched = new bool[_cardButtons.Length];

        _mismatchTimer = new Godot.Timer { OneShot = true, WaitTime = MismatchFlipBackSeconds };
        _mismatchTimer.Timeout += OnMismatchTimerTimeout;
        AddChild(_mismatchTimer);

        _cardPressedHandlers = new Action[_cardButtons.Length];
        for (var i = 0; i < _cardButtons.Length; i++)
        {
            var cardIndex = i;
            _cardPressedHandlers[i] = () => OnCardPressed(cardIndex);
            _cardButtons[i].Pressed += _cardPressedHandlers[i];
        }
    }

    public Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        _matchedPairs = 0;
        _totalAttempts = 0;
        _firstFlippedIndex = null;
        _completed = false;
        _statusLabel.Text = string.Empty;

        _cardColors = BuildShuffledDeck();
        for (var i = 0; i < _matched.Length; i++)
        {
            _matched[i] = false;
            _cardButtons[i].Modulate = Colors.White;
        }

        UpdateStatusLabel();
        return Task.CompletedTask;
    }

    public void OnActivated() => _isRunning = true;

    public void OnPaused()
    {
        _isRunning = false;
        _mismatchTimer.Paused = true;
    }

    public void OnResumed()
    {
        _isRunning = true;
        _mismatchTimer.Paused = false;
    }

    public void OnDeactivated()
    {
        // LSP: Completed tam olarak bir kez tetiklenmeli (bkz. godot-csharp-standards § 5) — kullanıcı
        // tüm çiftleri bulmadan modülden çıkarsa bile ModuleHost'un beklediği garanti burada sağlanıyor.
        _isRunning = false;
        RaiseCompletedIfNeeded();
    }

    private void OnCardPressed(int index)
    {
        if (!_isRunning || _completed || _matched[index] || _mismatchTimer.TimeLeft > 0)
        {
            return;
        }

        if (_firstFlippedIndex is null)
        {
            _firstFlippedIndex = index;
            RevealCard(index);
            return;
        }

        if (_firstFlippedIndex == index)
        {
            return;
        }

        var firstIndex = _firstFlippedIndex.Value;
        RevealCard(index);
        _totalAttempts++;

        if (_cardColors[firstIndex] == _cardColors[index])
        {
            _matched[firstIndex] = true;
            _matched[index] = true;
            _firstFlippedIndex = null;
            _matchedPairs++;
            UpdateStatusLabel();

            if (_matchedPairs >= TotalPairs)
            {
                RaiseCompletedIfNeeded();
            }
        }
        else
        {
            _pendingMismatchFirstIndex = firstIndex;
            _pendingMismatchSecondIndex = index;
            _firstFlippedIndex = null;
            UpdateStatusLabel();
            _mismatchTimer.Start();
        }
    }

    private void OnMismatchTimerTimeout()
    {
        HideCard(_pendingMismatchFirstIndex);
        HideCard(_pendingMismatchSecondIndex);
    }

    private void RevealCard(int index)
    {
        _cardButtons[index].Modulate = _cardColors[index];
    }

    private void HideCard(int index)
    {
        if (!_matched[index])
        {
            _cardButtons[index].Modulate = Colors.White;
        }
    }

    private Color[] BuildShuffledDeck()
    {
        var deck = new List<Color>();
        for (var i = 0; i < TotalPairs; i++)
        {
            deck.Add(Palette[i]);
            deck.Add(Palette[i]);
        }

        for (var i = deck.Count - 1; i > 0; i--)
        {
            var j = _random.RandiRange(0, i);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        return deck.ToArray();
    }

    private void UpdateStatusLabel()
    {
        _statusLabel.Text = $"Eşleşen: {_matchedPairs} / {TotalPairs}  (Deneme: {_totalAttempts})";
    }

    private void RaiseCompletedIfNeeded()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        var result = _scorer.Score(ModuleId, TotalPairs, _matchedPairs, _totalAttempts, _context);
        Completed?.Invoke(this, result);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            for (var i = 0; i < _cardButtons.Length; i++)
            {
                if (_cardButtons[i] is not null)
                {
                    _cardButtons[i].Pressed -= _cardPressedHandlers[i];
                }
            }

            if (_mismatchTimer is not null)
            {
                _mismatchTimer.Timeout -= OnMismatchTimerTimeout;
            }
        }

        base.Dispose(disposing);
    }
}
