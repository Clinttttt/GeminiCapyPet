using System.Windows;
using System.Windows.Controls;

namespace GeminiCapyPet;

public partial class PetSettingsWindow : Window
{
    private readonly PetWindow _pet;
    private bool _ready;

    public PetSettingsWindow(PetWindow pet)
    {
        _pet = pet;
        Owner = pet;
        InitializeComponent();
        PetLog.Info("Customization window initialized");
        _ready = false;
        RefreshFromPet();
        _ready = true;
    }

    public void RefreshFromPet()
    {
        _ready = false;
        PoseChoice.SelectedIndex = (int)_pet.SelectedPose;
        SizeSlider.Value = _pet.PetSize;
        SizeLabel.Text = $"{_pet.PetSize:0} px";
        AutoSleepCheck.IsChecked = _pet.AutoSleep;
        AutoSleepCheck.IsEnabled = _pet.SelectedPose == PetPose.Automatic;
        PosePreview.Source = _pet.CurrentPoseFrame;
        _ready = true;
    }

    private void OnPoseChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || PoseChoice.SelectedIndex < 0) return;
        _pet.SetPose((PetPose)PoseChoice.SelectedIndex);
        RefreshFromPet();
    }

    private void OnSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_ready) return;
        _pet.SetSize(SizeSlider.Value);
        SizeLabel.Text = $"{_pet.PetSize:0} px";
    }

    private void OnAutoSleepChanged(object sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        _pet.SetAutoSleep(AutoSleepCheck.IsChecked == true);
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        _pet.SetPose(PetPose.Automatic);
        _pet.SetSize(220);
        _pet.SetAutoSleep(true);
        _pet.ResetPetPosition();
        RefreshFromPet();
    }

    private void OnDone(object sender, RoutedEventArgs e)
    {
        _pet.SaveCurrentPreferences();
        Close();
    }
}
