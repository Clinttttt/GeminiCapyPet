using System.Windows;
using Image = System.Windows.Controls.Image;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GeminiCapyPet;

internal enum PetPose { Automatic, Standing, Sitting, LyingDown, Sleeping }

/// <summary>25-frame (5x5) sprite sheet with selectable poses and automatic sleep/wake.</summary>
internal sealed class SpriteAnimator : IDisposable
{
    private readonly Image _image;
    private readonly BitmapSource[] _frames = new BitmapSource[25];
    private readonly DispatcherTimer _timer;
    private readonly ScaleTransform _breathing = new(1, 1);
    private MotionState _state = MotionState.Awake;
    private int _frame;

    private enum MotionState { Awake, FallingAsleep, Asleep, WakingUp }

    public PetPose Pose { get; private set; } = PetPose.Automatic;
    public bool IsAsleep => _state is MotionState.Asleep or MotionState.FallingAsleep;
    public BitmapSource CurrentFrame => _frames[_frame];

    public SpriteAnimator(Image image)
    {
        _image = image;
        var sheet = new BitmapImage(new Uri("pack://application:,,,/Assets/Capies-sleep.png"));
        if (sheet.PixelWidth != 1280 || sheet.PixelHeight != 1280)
            throw new InvalidOperationException("Expected 1280x1280 Capies sprite sheet.");

        for (var i = 0; i < _frames.Length; i++)
        {
            var crop = new CroppedBitmap(sheet,
                new Int32Rect(i % 5 * 256, i / 5 * 256, 256, 256));
            crop.Freeze();
            _frames[i] = crop;
        }

        _image.Source = _frames[0];
        _image.RenderTransform = _breathing;
        _breathing.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation
        {
            From = 1,
            To = 1.018,
            Duration = TimeSpan.FromMilliseconds(850),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        });
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(95) };
        _timer.Tick += OnFrame;
    }

    public void SetPose(PetPose pose)
    {
        _timer.Stop();
        Pose = pose;
        _state = pose == PetPose.Sleeping ? MotionState.Asleep : MotionState.Awake;
        _frame = pose switch
        {
            PetPose.Standing => 0,
            PetPose.Sitting => 7,
            PetPose.LyingDown => 10,
            PetPose.Sleeping => 24,
            _ => 0
        };
        _image.Source = _frames[_frame];
    }

    public void Sleep()
    {
        if (Pose != PetPose.Automatic || _state != MotionState.Awake) return;
        _state = MotionState.FallingAsleep;
        _frame = 0;
        _timer.Start();
    }

    public void Wake()
    {
        if (Pose != PetPose.Automatic || _state is MotionState.Awake or MotionState.WakingUp) return;
        _state = MotionState.WakingUp;
        _frame = Math.Clamp(_frame, 0, 24);
        _timer.Start();
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        if (_state == MotionState.FallingAsleep)
        {
            _frame = Math.Min(24, _frame + 1);
            if (_frame == 24)
            {
                _state = MotionState.Asleep;
                _timer.Stop();
            }
        }
        else if (_state == MotionState.WakingUp)
        {
            _frame = Math.Max(0, _frame - 1);
            if (_frame == 0)
            {
                _state = MotionState.Awake;
                _timer.Stop();
            }
        }
        _image.Source = _frames[_frame];
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnFrame;
        _breathing.BeginAnimation(ScaleTransform.ScaleYProperty, null);
    }
}
