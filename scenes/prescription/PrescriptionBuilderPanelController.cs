using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using Godot;

namespace FreeRehabHub.App.Prescription;

public partial class PrescriptionBuilderPanelController : Control
{
    private const int MinRepetitions = 0;
    private const int MaxRepetitions = 100;
    private const int DefaultRepetitions = 10;
    private const int MinSets = 0;
    private const int MaxSets = 20;
    private const int DefaultSets = 1;
    private const int RowSeparation = 10;
    private const string EnglishLocale = "en";

    [Export] private NodePath _titleLabelPath = null!;
    [Export] private NodePath _errorLabelPath = null!;
    [Export] private NodePath _libraryItemListPath = null!;
    [Export] private NodePath _addButtonPath = null!;
    [Export] private NodePath _selectedItemsContainerPath = null!;
    [Export] private NodePath _notesInputPath = null!;
    [Export] private NodePath _saveButtonPath = null!;
    [Export] private NodePath _cancelButtonPath = null!;

    private Label _titleLabel = null!;
    private Label _errorLabel = null!;
    private ItemList _libraryItemList = null!;
    private Button _addButton = null!;
    private VBoxContainer _selectedItemsContainer = null!;
    private LineEdit _notesInput = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;
    private AppServices _appServices = null!;
    private SessionContext _sessionContext = null!;
    private LocalizationAutoload _localization = null!;

    private IReadOnlyList<ExerciseCard> _libraryCards = Array.Empty<ExerciseCard>();
    private readonly List<SelectedItemRow> _selectedRows = new();

    public override async void _Ready()
    {
        _titleLabel = GetNode<Label>(_titleLabelPath);
        _errorLabel = GetNode<Label>(_errorLabelPath);
        _libraryItemList = GetNode<ItemList>(_libraryItemListPath);
        _addButton = GetNode<Button>(_addButtonPath);
        _selectedItemsContainer = GetNode<VBoxContainer>(_selectedItemsContainerPath);
        _notesInput = GetNode<LineEdit>(_notesInputPath);
        _saveButton = GetNode<Button>(_saveButtonPath);
        _cancelButton = GetNode<Button>(_cancelButtonPath);
        _appServices = GetNode<AppServices>("/root/AppServices");
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");
        _localization = GetNode<LocalizationAutoload>("/root/LocalizationAutoload");

        _errorLabel.Text = string.Empty;
        _addButton.Pressed += OnAddPressed;
        _saveButton.Pressed += OnSavePressed;
        _cancelButton.Pressed += OnCancelPressed;

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var patient = _sessionContext.ActivePatient;
        if (patient is null)
        {
            _errorLabel.Text = "Aktif hasta bulunamadı.";
            _saveButton.Disabled = true;
            return;
        }

        _titleLabel.Text = $"Egzersiz Reçetesi — {patient.FullName}";

        _libraryCards = await _appServices.ExerciseLibraryRepository!.GetAllAsync();
        _libraryItemList.Clear();
        foreach (var card in _libraryCards)
        {
            _libraryItemList.AddItem(Localize(card.DisplayName));
        }

        var therapist = _sessionContext.ActiveTherapist;
        if (therapist is null)
        {
            return;
        }

        var latest = await _appServices.PrescriptionService!.GetLatestByPatientIdAsync(patient.Id, therapist.Id);
        if (latest is null)
        {
            return;
        }

        _notesInput.Text = latest.Notes ?? string.Empty;
        foreach (var item in latest.Items)
        {
            var card = _libraryCards.FirstOrDefault(c => c.Id == item.ExerciseCardId);
            if (card is not null)
            {
                AddSelectedRow(card, item.Repetitions ?? DefaultRepetitions, item.Sets ?? DefaultSets);
            }
        }
    }

    private void OnAddPressed()
    {
        var selectedIndices = _libraryItemList.GetSelectedItems();
        if (selectedIndices.Length == 0)
        {
            return;
        }

        var card = _libraryCards[selectedIndices[0]];
        if (_selectedRows.Any(row => row.ExerciseCardId == card.Id))
        {
            return;
        }

        AddSelectedRow(card, card.SuggestedRepetitions ?? DefaultRepetitions, card.SuggestedSets ?? DefaultSets);
    }

    private void AddSelectedRow(ExerciseCard card, int repetitions, int sets)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", RowSeparation);

        var nameLabel = new Label { Text = Localize(card.DisplayName), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(nameLabel);

        row.AddChild(new Label { Text = "Tekrar" });
        var repsSpinBox = new SpinBox { MinValue = MinRepetitions, MaxValue = MaxRepetitions, Value = repetitions };
        row.AddChild(repsSpinBox);

        row.AddChild(new Label { Text = "Set" });
        var setsSpinBox = new SpinBox { MinValue = MinSets, MaxValue = MaxSets, Value = sets };
        row.AddChild(setsSpinBox);

        var removeButton = new Button { Text = "Kaldır" };
        row.AddChild(removeButton);

        _selectedItemsContainer.AddChild(row);

        var selectedRow = new SelectedItemRow(card.Id, repsSpinBox, setsSpinBox, row);
        _selectedRows.Add(selectedRow);
        removeButton.Pressed += () => RemoveSelectedRow(selectedRow);
    }

    private void RemoveSelectedRow(SelectedItemRow row)
    {
        _selectedRows.Remove(row);
        row.RowControl.QueueFree();
    }

    private async void OnSavePressed()
    {
        var patient = _sessionContext.ActivePatient;
        var therapist = _sessionContext.ActiveTherapist;
        if (patient is null || therapist is null)
        {
            _errorLabel.Text = "Aktif hasta veya terapist bulunamadı.";
            return;
        }

        if (_selectedRows.Count == 0)
        {
            _errorLabel.Text = "En az bir egzersiz eklemelisiniz.";
            return;
        }

        var prescription = new ExercisePrescription
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            CreatedByTherapistId = therapist.Id,
            CreatedAt = DateTime.UtcNow,
            Notes = string.IsNullOrWhiteSpace(_notesInput.Text) ? null : _notesInput.Text,
            Items = _selectedRows.Select(row => new PrescriptionItem
            {
                ExerciseCardId = row.ExerciseCardId,
                Repetitions = (int)row.RepsSpinBox.Value,
                Sets = (int)row.SetsSpinBox.Value
            }).ToList()
        };

        await _appServices.PrescriptionService!.AddAsync(prescription, therapist.Id);

        _sessionContext.SetActivePatient(null);
        GetTree().ChangeSceneToFile("res://scenes/shells/TherapistShell.tscn");
    }

    private void OnCancelPressed()
    {
        _sessionContext.SetActivePatient(null);
        GetTree().ChangeSceneToFile("res://scenes/shells/TherapistShell.tscn");
    }

    private string Localize(LocalizedText text)
    {
        return _localization.CurrentLocale == EnglishLocale ? text.En : text.Tr;
    }

    private sealed record SelectedItemRow(string ExerciseCardId, SpinBox RepsSpinBox, SpinBox SetsSpinBox, Control RowControl);
}
