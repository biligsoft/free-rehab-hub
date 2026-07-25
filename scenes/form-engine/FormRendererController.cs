using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Modules.Contracts;
using Godot;

namespace FreeRehabHub.App.FormEngine;

public partial class FormRendererController : Control
{
    private const double DefaultNumberFieldMin = -1_000_000.0;
    private const double DefaultNumberFieldMax = 1_000_000.0;
    private const double DefaultScaleFieldMin = 0.0;
    private const double DefaultScaleFieldMax = 10.0;
    private const double ScaleStep = 1.0;
    private const int FieldRowSeparation = 6;
    private const string MultiChoiceValueSeparator = ",";
    private const string BooleanTrueValue = "true";
    private const string BooleanFalseValue = "false";
    private const string EnglishLocale = "en";

    [Export] private NodePath _titleLabelPath = null!;
    [Export] private NodePath _fieldsContainerPath = null!;
    [Export] private NodePath _submitButtonPath = null!;
    [Export] private NodePath _errorLabelPath = null!;

    private Label _titleLabel = null!;
    private VBoxContainer _fieldsContainer = null!;
    private Button _submitButton = null!;
    private Label _errorLabel = null!;
    private LocalizationAutoload _localization = null!;

    private FormSchema? _schema;
    private readonly List<(FormField Field, Control ValueControl)> _fieldControls = new();

    public event EventHandler<FormSubmission>? Submitted;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>(_titleLabelPath);
        _fieldsContainer = GetNode<VBoxContainer>(_fieldsContainerPath);
        _submitButton = GetNode<Button>(_submitButtonPath);
        _errorLabel = GetNode<Label>(_errorLabelPath);
        _localization = GetNode<LocalizationAutoload>("/root/LocalizationAutoload");

        _errorLabel.Text = string.Empty;
        _submitButton.Pressed += OnSubmitPressed;
    }

    public void LoadSchema(FormSchema schema)
    {
        _schema = schema;
        _errorLabel.Text = string.Empty;
        _titleLabel.Text = Localize(schema.Title);

        foreach (var child in _fieldsContainer.GetChildren())
        {
            child.QueueFree();
        }

        _fieldControls.Clear();

        foreach (var field in schema.Fields)
        {
            BuildFieldRow(field);
        }
    }

    private void BuildFieldRow(FormField field)
    {
        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", FieldRowSeparation);

        var label = new Label { Text = Localize(field.Label) };
        row.AddChild(label);

        var valueControl = CreateValueControl(field);
        row.AddChild(valueControl);

        _fieldsContainer.AddChild(row);
        _fieldControls.Add((field, valueControl));
    }

    private Control CreateValueControl(FormField field)
    {
        return field.Type switch
        {
            FormFieldType.Text => new LineEdit(),
            FormFieldType.Number => CreateNumberControl(field),
            FormFieldType.Scale => CreateScaleControl(field),
            FormFieldType.SingleChoice => CreateSingleChoiceControl(field),
            FormFieldType.MultiChoice => CreateMultiChoiceControl(field),
            FormFieldType.Boolean => new CheckBox(),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field.Type, "Bilinmeyen form alanı tipi.")
        };
    }

    private static SpinBox CreateNumberControl(FormField field)
    {
        return new SpinBox
        {
            MinValue = field.MinValue ?? DefaultNumberFieldMin,
            MaxValue = field.MaxValue ?? DefaultNumberFieldMax
        };
    }

    private static HSlider CreateScaleControl(FormField field)
    {
        return new HSlider
        {
            MinValue = field.MinValue ?? DefaultScaleFieldMin,
            MaxValue = field.MaxValue ?? DefaultScaleFieldMax,
            Step = ScaleStep
        };
    }

    private OptionButton CreateSingleChoiceControl(FormField field)
    {
        var optionButton = new OptionButton();
        foreach (var option in field.Options)
        {
            optionButton.AddItem(Localize(option.Label));
        }

        return optionButton;
    }

    private VBoxContainer CreateMultiChoiceControl(FormField field)
    {
        var container = new VBoxContainer();
        foreach (var option in field.Options)
        {
            container.AddChild(new CheckBox { Text = Localize(option.Label), Name = option.Value });
        }

        return container;
    }

    private string Localize(LocalizedText text)
    {
        return _localization.CurrentLocale == EnglishLocale ? text.En : text.Tr;
    }

    private void OnSubmitPressed()
    {
        if (_schema is null)
        {
            return;
        }

        var submission = new FormSubmission();
        foreach (var (field, control) in _fieldControls)
        {
            var value = ReadValue(field, control);
            if (field.Required && string.IsNullOrWhiteSpace(value))
            {
                _errorLabel.Text = $"\"{Localize(field.Label)}\" alanı zorunlu.";
                return;
            }

            submission.FieldValues[field.Id] = value;
        }

        _errorLabel.Text = string.Empty;
        Submitted?.Invoke(this, submission);
    }

    private static string ReadValue(FormField field, Control control)
    {
        return field.Type switch
        {
            FormFieldType.Text => ((LineEdit)control).Text,
            FormFieldType.Number => ((SpinBox)control).Value.ToString(CultureInfo.InvariantCulture),
            FormFieldType.Scale => ((HSlider)control).Value.ToString(CultureInfo.InvariantCulture),
            FormFieldType.SingleChoice => ReadSingleChoiceValue(field, (OptionButton)control),
            FormFieldType.MultiChoice => ReadMultiChoiceValue((VBoxContainer)control),
            FormFieldType.Boolean => ((CheckBox)control).ButtonPressed ? BooleanTrueValue : BooleanFalseValue,
            _ => throw new ArgumentOutOfRangeException(nameof(field), field.Type, "Bilinmeyen form alanı tipi.")
        };
    }

    private static string ReadSingleChoiceValue(FormField field, OptionButton optionButton)
    {
        var selectedIndex = optionButton.Selected;
        if (selectedIndex < 0 || selectedIndex >= field.Options.Count)
        {
            return string.Empty;
        }

        return field.Options[selectedIndex].Value;
    }

    private static string ReadMultiChoiceValue(VBoxContainer container)
    {
        var selectedValues = container.GetChildren()
            .OfType<CheckBox>()
            .Where(checkBox => checkBox.ButtonPressed)
            .Select(checkBox => checkBox.Name.ToString());

        return string.Join(MultiChoiceValueSeparator, selectedValues);
    }
}
