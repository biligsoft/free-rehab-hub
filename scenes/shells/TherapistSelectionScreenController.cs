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
    [Export] private ItemList _therapistList = null!;
    [Export] private Button _selectButton = null!;
    [Export] private LineEdit _newTherapistNameInput = null!;
    [Export] private OptionButton _newTherapistDisciplineOption = null!;
    [Export] private Button _createButton = null!;
    [Export] private Label _errorLabel = null!;

    private AppServices _appServices = null!;
    private IReadOnlyList<Therapist> _therapists = Array.Empty<Therapist>();

    public override async void _Ready()
    {
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
