using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using Godot;

namespace FreeRehabHub.App.Shells;

public partial class TherapistSelectionScreenController : Control
{
    [Export] private NodePath _therapistListPath = null!;
    [Export] private NodePath _selectButtonPath = null!;
    [Export] private NodePath _newTherapistNameInputPath = null!;
    [Export] private NodePath _newTherapistDisciplineOptionPath = null!;
    [Export] private NodePath _createButtonPath = null!;
    [Export] private NodePath _errorLabelPath = null!;

    private ItemList _therapistList = null!;
    private Button _selectButton = null!;
    private LineEdit _newTherapistNameInput = null!;
    private OptionButton _newTherapistDisciplineOption = null!;
    private Button _createButton = null!;
    private Label _errorLabel = null!;
    private AppServices _appServices = null!;
    private IReadOnlyList<Therapist> _therapists = Array.Empty<Therapist>();

    public override async void _Ready()
    {
        _therapistList = GetNode<ItemList>(_therapistListPath);
        _selectButton = GetNode<Button>(_selectButtonPath);
        _newTherapistNameInput = GetNode<LineEdit>(_newTherapistNameInputPath);
        _newTherapistDisciplineOption = GetNode<OptionButton>(_newTherapistDisciplineOptionPath);
        _createButton = GetNode<Button>(_createButtonPath);
        _errorLabel = GetNode<Label>(_errorLabelPath);
        _appServices = GetNode<AppServices>("/root/AppServices");
        _errorLabel.Text = string.Empty;
        _selectButton.Disabled = true;

        foreach (var discipline in Enum.GetValues<Discipline>())
        {
            _newTherapistDisciplineOption.AddItem(DisciplineLabel(discipline));
        }

        _therapistList.ItemSelected += _ => _selectButton.Disabled = false;
        _selectButton.Pressed += OnSelectPressed;
        _createButton.Pressed += OnCreatePressed;

        await ReloadTherapistsAsync();
    }

    private async Task ReloadTherapistsAsync()
    {
        _therapists = await _appServices.TherapistService!.GetAllAsync();

        _therapistList.Clear();
        foreach (var therapist in _therapists)
        {
            _therapistList.AddItem($"{therapist.FullName} ({DisciplineLabel(therapist.Discipline)})");
        }
    }

    private void OnSelectPressed()
    {
        var selectedIndices = _therapistList.GetSelectedItems();
        if (selectedIndices.Length == 0)
        {
            return;
        }

        EnterAs(_therapists[selectedIndices[0]]);
    }

    private async void OnCreatePressed()
    {
        var fullName = _newTherapistNameInput.Text;
        if (string.IsNullOrWhiteSpace(fullName))
        {
            _errorLabel.Text = "Ad boş olamaz.";
            return;
        }

        var therapist = new Therapist
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Discipline = (Discipline)_newTherapistDisciplineOption.Selected,
            CreatedAt = DateTime.UtcNow
        };

        await _appServices.TherapistService!.AddAsync(therapist, therapist.Id);
        EnterAs(therapist);
    }

    private void EnterAs(Therapist therapist)
    {
        GetNode<SessionContext>("/root/SessionContext").SetActiveTherapist(therapist);
        GetTree().ChangeSceneToFile("res://scenes/shells/TherapistShell.tscn");
    }

    private static string DisciplineLabel(Discipline discipline) => discipline switch
    {
        Discipline.Physiotherapy => "Fizyoterapi",
        Discipline.OccupationalTherapy => "Ergoterapi",
        Discipline.SpeechTherapy => "Konuşma Terapisi",
        Discipline.Psychology => "Psikoloji",
        Discipline.SpecialEducation => "Özel Eğitim",
        _ => discipline.ToString()
    };
}
