#nullable enable
using System.Timers;
using Godot;
using MegaCrit.Sts2.Core.Nodes;
using Timer = System.Timers.Timer;

namespace ZaQiZaBa;

internal sealed class NarratorSubtitleUI : Control
{
    private static NarratorSubtitleUI? _instance;
    private static CanvasLayer? _canvas;

    private Label _label = null!;

    private Timer? _hideTimer;
    private Tween? _tween;

    private Vector2 _restPosition;
    private bool _isDisposed;

    private const double FLY_DURATION = 0.35;
    private const double FADE_DURATION = 0.25;

    public static NarratorSubtitleUI? Instance => GetOrCreate();

    private static NarratorSubtitleUI? GetOrCreate()
    {
        if (_instance != null && GodotObject.IsInstanceValid(_instance) && !_instance._isDisposed)
        {
            if (_canvas != null && _canvas.GetParent() == null && NGame.Instance != null)
                NGame.Instance.AddChild(_canvas);
            return _instance;
        }

        if (NGame.Instance == null)
            return null;

        _canvas = new CanvasLayer
        {
            Layer = 999,
            Name = "NarratorCanvas",
        };
        _instance = new NarratorSubtitleUI();
        _canvas.AddChild(_instance);
        NGame.Instance.AddChild(_canvas);
        return _instance;
    }

    public NarratorSubtitleUI()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        _label = new Label
        {
            Name = "NarratorLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_label);
    }

    public override void _EnterTree()
    {
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        var vp = GetViewport();
        float w = vp?.GetVisibleRect().Size.X ?? 1920f;
        float h = vp?.GetVisibleRect().Size.Y ?? 1080f;

        int fontSize = (int)Mathf.Round(h * 0.038f);
        float controlH = fontSize * 2f;

        _restPosition = new Vector2(0f, h * 0.10f);
        Position = _restPosition;
        Size = new Vector2(w, controlH);

        _label.Position = Vector2.Zero;
        _label.Size = Size;
        _label.AddThemeFontSizeOverride("font_size", fontSize);
        _label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        _label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.7f));
        _label.AddThemeConstantOverride("outline_size", Mathf.Max(1, fontSize / 8));
        _label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.125f));
        _label.AddThemeConstantOverride("shadow_offset_x", 5);
        _label.AddThemeConstantOverride("shadow_offset_y", 4);

        Modulate = new Color(1f, 1f, 1f, 0f);
    }

    public void ShowSubtitle(string text, double durationSeconds)
    {
        if (!ZaQiZaBaConfig.ShowNarratorSubtitle) return;
        if (_isDisposed) return;
        if (!GodotObject.IsInstanceValid(this)) return;
        if (!IsInsideTree()) return;

        if (Size.X < 100f) ApplyLayout();

        KillHideTimer();
        _tween?.Kill();

        _label.Text = text;
        Position = new Vector2(_restPosition.X, _restPosition.Y + Size.Y);
        Modulate = new Color(1f, 1f, 1f, 0f);

        _tween = CreateTween().SetParallel(true);
        _tween.TweenProperty(this, "position", _restPosition, FLY_DURATION).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f, 1f), FADE_DURATION);

        if (durationSeconds > 0)
        {
            _hideTimer = new Timer(durationSeconds * 1000.0) { AutoReset = false };
            _hideTimer.Elapsed += OnHideDue;
            _hideTimer.Start();
        }
    }

    public void HideImmediate()
    {
        if (_isDisposed || !GodotObject.IsInstanceValid(this)) return;
        KillHideTimer();
        DoFadeOut();
    }

    private void DoFadeOut()
    {
        _tween?.Kill();

        _tween = CreateTween();
        _tween.TweenProperty(this, "modulate:a", 0f, FADE_DURATION);
        _tween.Finished += () => _label.Text = "";
    }

    private void OnHideDue(object? sender, ElapsedEventArgs e)
    {
        CallDeferred(nameof(StartFadeOut));
    }

    private void StartFadeOut()
    {
        if (_isDisposed || !GodotObject.IsInstanceValid(this)) return;
        DoFadeOut();
    }

    private void KillHideTimer()
    {
        if (_hideTimer == null) return;
        try
        {
            _hideTimer.Stop();
            _hideTimer.Elapsed -= OnHideDue;
            _hideTimer.Dispose();
        }
        catch { }
        _hideTimer = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _isDisposed = true;
            KillHideTimer();
            _tween?.Kill();
            if (_instance == this) _instance = null;
        }
        base.Dispose(disposing);
    }
}
