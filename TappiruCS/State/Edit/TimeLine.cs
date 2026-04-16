// Timeline.cs — ПОЛНАЯ ЧИСТАЯ ВЕРСИЯ С ФРАЗАМИ + СЛАЙДЕРАМИ (с ручками)
using Gtk;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.State.Edit;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Edit
{
    /// <summary>
    /// Основной таймлайн редактора. Поддерживает waveform, фразы с draggable ручками,
    /// слайдеры с отдельными ручками, pan, zoom и playhead.
    /// </summary>
    public class Timeline : GameObject
    {
        public event Action<float> OnTimeClicked;

        // Основные элементы
        public SpriteObject Background { get; private set; }
        public SpriteObject Playhead { get; private set; }
        private SpriteObject _playheadLine = null!;

        // Пулы визуальных объектов
        private readonly List<SpriteObject> _waveformBars = new();
        private readonly List<SpriteObject> _phraseBars = new();
        private readonly List<SpriteObject> _leftHandles = new();
        private readonly List<SpriteObject> _rightHandles = new();
        private readonly List<SpriteObject> _sliderBars = new();
        private readonly List<SpriteObject> _sliderLeftHandles = new();
        private readonly List<SpriteObject> _sliderRightHandles = new();
        private readonly List<(Phrase phrase, int sliderIndex)> _sliderReferences = new();

        private readonly List<SpriteObject> _tickLines = new();
        private readonly List<TextObject> _timeLabels = new();

        public float TotalDuration { get; private set; } = 300f;

        private float _visibleStart = 0f;
        private float _visibleEnd = 300f;
        private float _lastScrollValue = 0f;

        // Состояния взаимодействия
        private bool _draggingPlayhead = false;
        private bool _panning = false;
        private bool _draggingLeftHandle = false;
        private bool _draggingRightHandle = false;
        private bool _draggingSliderLeftHandle = false;
        private bool _draggingSliderRightHandle = false;

        private Phrase? _draggedPhrase = null;
        private int _draggedIndex = -1;
        private Phrase? _draggedSliderPhrase = null;
        private int _draggedSliderIndex = -1;

        private Vector2 _lastMousePos;

        private readonly List<Phrase> _phrases = new();

        public Timeline(float x, float y, float width, float height)
        {

            Background = new SpriteObject(TextureManager.GetTexture("slider_line"), x, y, width, height)
            {
                Color = new Color4(0.13f, 0.13f, 0.19f, 1f)
            };

            Playhead = new SpriteObject( TextureManager.GetTexture("marker"), x, y - 35f, 28, 28)
            {
                Color = new Color4(1f, 0.35f, 0.35f, 1f),
                Pivot = new Vector2(0.5f, 1f)
            };

            _playheadLine = new SpriteObject(TextureManager.GetTexture("slider_line"), x, y, 4, height)
            {
                Color = new Color4(1f, 0.35f, 0.35f, 0.9f),
                Pivot = new Vector2(0.5f, 0.5f)
            };

            AddChild(Background);
            AddChild(Playhead);
            AddChild(_playheadLine);
        }

        #region Public API
        public void SetPhrases(List<Phrase> phrases)
        {
            _phrases.Clear();
            if (phrases != null) _phrases.AddRange(phrases);
            RefreshAllVisuals();
        }

        public void SetDuration(float duration)
        {
            TotalDuration = Math.Max(duration, 1f);
            _visibleStart = 0f;
            _visibleEnd = TotalDuration;
            RefreshAllVisuals();
        }

        public void SetCurrentTime(float time)
        {
            if (_visibleEnd <= _visibleStart) return;
            float normalized = Math.Clamp((time - _visibleStart) / (_visibleEnd - _visibleStart), 0f, 1f);
            var bounds = Background.GetDesignBounds();
            float newX = bounds.Item1 + normalized * bounds.Item3;

            Playhead.Position = new Vector2(newX, Background.Position.Y - 35f);
            _playheadLine.Position = new Vector2(newX, Background.Position.Y);
        }
        #endregion

        #region Update & Input
        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime);
            HandleSimpleZoom(mouse);
            HandleMouseInteraction(mouse);
        }

        private void HandleSimpleZoom(MouseState mouse)
        {
            float scrollDelta = mouse.Scroll.Y - _lastScrollValue;
            _lastScrollValue = mouse.Scroll.Y;
            if (Math.Abs(scrollDelta) < 0.1f) return;

            var bounds = Background.GetDesignBounds();
            float virtualMouseX = mouse.X / Scene.CanvasScale.X;
            float normalizedMouse = Math.Clamp((virtualMouseX - bounds.Item1) / bounds.Item3, 0f, 1f);

            float timeUnderCursor = _visibleStart + normalizedMouse * (_visibleEnd - _visibleStart);

            float zoomFactor = scrollDelta > 0 ? 0.75f : 1.35f;
            float newDuration = (_visibleEnd - _visibleStart) * zoomFactor;
            newDuration = Math.Clamp(newDuration, 0.5f, TotalDuration);

            float newStart = timeUnderCursor - normalizedMouse * newDuration;
            float newEnd = newStart + newDuration;

            if (newStart < 0) { newStart = 0; newEnd = newDuration; }
            if (newEnd > TotalDuration) { newEnd = TotalDuration; newStart = TotalDuration - newDuration; }

            _visibleStart = newStart;
            _visibleEnd = newEnd;
            RefreshAllVisuals();
        }

        private void HandleMouseInteraction(MouseState mouse)
        {
            float virtualX = mouse.X / CanvasScale.X;
            float virtualY = mouse.Y / CanvasScale.Y;

            if (mouse.IsButtonDown(MouseButton.Left))
            {
                if (!_draggingPlayhead && !_panning && !_draggingLeftHandle && !_draggingRightHandle &&
                    !_draggingSliderLeftHandle && !_draggingSliderRightHandle)
                {
                    if (TryStartSliderHandleDrag(virtualX, virtualY, out bool isLeftSlider))
                    {
                        if (isLeftSlider) _draggingSliderLeftHandle = true;
                        else _draggingSliderRightHandle = true;
                    }
                    else if (TryStartHandleDrag(virtualX, virtualY, out bool isLeft))
                    {
                        if (isLeft) _draggingLeftHandle = true;
                        else _draggingRightHandle = true;
                    }
                    else if (Math.Abs(virtualX - Playhead.Position.X) < 30f &&
         Math.Abs(virtualY - Playhead.Position.Y) < 45f)
                    {
                        _draggingPlayhead = true;
                    }
                    else if (IsOverTimeline(virtualX, virtualY))   // ← теперь с Y
                    {
                        _panning = true;
                    }
                    _lastMousePos = new Vector2(mouse.X, mouse.Y);
                }

                ProcessPlayheadDrag(virtualX, virtualY);
                ProcessPan(virtualX, virtualY);
                ProcessHandleDrag(virtualX, Background.GetDesignBounds());
                ProcessSliderHandleDrag(virtualX, virtualY);
            }
            else
            {
                _draggingPlayhead = _panning = _draggingLeftHandle = _draggingRightHandle =
                _draggingSliderLeftHandle = _draggingSliderRightHandle = false;

                _draggedPhrase = null;
                _draggedIndex = -1;
                _draggedSliderPhrase = null;
                _draggedSliderIndex = -1;
            }

            if (mouse.IsButtonPressed(MouseButton.Left) && IsOverTimeline(virtualX, virtualY) &&
                !_draggingPlayhead && !_panning && !_draggingLeftHandle && !_draggingRightHandle &&
                !_draggingSliderLeftHandle && !_draggingSliderRightHandle)
            {
                var bounds = Background.GetDesignBounds();
                float norm = (virtualX - bounds.designLeft) / bounds.effWidth;
                OnTimeClicked?.Invoke(_visibleStart + norm * (_visibleEnd - _visibleStart));
            }
        }

        // ==================== РУЧКИ ФРАЗ ====================
        private bool TryStartHandleDrag(float virtualX, float virtualY, out bool isLeft)
        {
            isLeft = false;
            const float hitWidthX = 13f;
            const float hitHeightY = 25f;

            float phraseY = Background.Position.Y - Background.Scale.Y * 0.31f;

            for (int i = 0; i < _leftHandles.Count; i++)
            {
                float hx = _leftHandles[i].Position.X;
                if (Math.Abs(virtualX - hx) < hitWidthX && Math.Abs(virtualY - phraseY) < hitHeightY)
                {
                    _draggedPhrase = _phrases[i];
                    _draggedIndex = i;
                    isLeft = true;
                    return true;
                }
            }

            for (int i = 0; i < _rightHandles.Count; i++)
            {
                float hx = _rightHandles[i].Position.X;
                if (Math.Abs(virtualX - hx) < hitWidthX && Math.Abs(virtualY - phraseY) < hitHeightY)
                {
                    _draggedPhrase = _phrases[i];
                    _draggedIndex = i;
                    isLeft = false;
                    return true;
                }
            }

            return false;

        }

        private void ProcessHandleDrag(float virtualX, (float x, float y, float w, float h) bounds)
        {
            if ((!_draggingLeftHandle && !_draggingRightHandle) || _draggedPhrase == null) return;

            float norm = (virtualX - bounds.x) / bounds.w;
            float newTime = _visibleStart + norm * (_visibleEnd - _visibleStart);

            if (_draggingLeftHandle)
            {
                float minStart = _draggedIndex > 0 ? _phrases[_draggedIndex - 1].EndTime + 0.05f : 0f;
                _draggedPhrase.StartTime = Math.Clamp(newTime, minStart, _draggedPhrase.EndTime - 0.2f);
                RefreshAllVisuals();
            }
            else
            {
                float maxEnd = _draggedIndex < _phrases.Count - 1 ? _phrases[_draggedIndex + 1].StartTime - 0.05f : TotalDuration;
                _draggedPhrase.EndTime = Math.Clamp(newTime, _draggedPhrase.StartTime + 0.2f, maxEnd);
                RefreshAllVisuals();
            }

        }

        // ==================== РУЧКИ СЛАЙДЕРОВ ====================
        private bool TryStartSliderHandleDrag(float virtualX, float virtualY, out bool isLeft)
        {
            isLeft = false;
            const float hitWidthX = 13f;
            const float hitHeightY = 25f;

            for (int i = 0; i < _sliderLeftHandles.Count; i++)
            {
                var h = _sliderLeftHandles[i];
                if (Math.Abs(virtualX - h.Position.X) < hitWidthX && Math.Abs(virtualY - h.Position.Y) < hitHeightY)
                {
                    _draggedSliderPhrase = _sliderReferences[i].phrase;
                    _draggedSliderIndex = _sliderReferences[i].sliderIndex;
                    isLeft = true;

                    return true;
                }
            }
            for (int i = 0; i < _sliderRightHandles.Count; i++)
            {
                var h = _sliderRightHandles[i];
                if (Math.Abs(virtualX - h.Position.X) < hitWidthX && Math.Abs(virtualY - h.Position.Y) < hitHeightY)
                {
                    _draggedSliderPhrase = _sliderReferences[i].phrase;
                    _draggedSliderIndex = _sliderReferences[i].sliderIndex;
                    isLeft = false;
                    
                    return true;
                }
            }
            return false;
        }

        private void ProcessSliderHandleDrag(float virtualX, float virtualY)
        {
            if ((!_draggingSliderLeftHandle && !_draggingSliderRightHandle) ||
                _draggedSliderPhrase == null || _draggedSliderIndex < 0)
                return;

            if (!IsOverTimeline(virtualX, virtualY)) return;

            var bounds = Background.GetDesignBounds();
            float norm = (virtualX - bounds.Item1) / bounds.Item3;
            float newTime = _visibleStart + norm * (_visibleEnd - _visibleStart);

            var sliders = _draggedSliderPhrase.Sliders;
            if (sliders == null || _draggedSliderIndex >= sliders.Count) return;

            var slider = sliders[_draggedSliderIndex];
            float phraseStart = _draggedSliderPhrase.StartTime;
            float phraseEnd = _draggedSliderPhrase.EndTime;

            if (_draggingSliderLeftHandle)
                slider.startTime = Math.Clamp(newTime, phraseStart, slider.endTime - 0.1f);
            else
                slider.endTime = Math.Clamp(newTime, slider.startTime + 0.1f, phraseEnd);

            RefreshAllVisuals();
        }

        private void ProcessPlayheadDrag(float virtualX, float virtualY)
        {
            if (!_draggingPlayhead || !IsOverTimeline(virtualX, virtualY)) return;

            var bounds = Background.GetDesignBounds();
            float norm = (virtualX - bounds.Item1) / bounds.Item3;
            float newTime = _visibleStart + norm * (_visibleEnd - _visibleStart);
            OnTimeClicked?.Invoke(newTime);

            float newX = bounds.Item1 + norm * bounds.Item3;
            Playhead.Position = new Vector2(newX, Background.Position.Y - 35f);
            _playheadLine.Position = new Vector2(newX, Background.Position.Y);
        }

        private void ProcessPan(float virtualX, float virtualY)
        {
            if (!_panning || !IsOverTimeline(virtualX, virtualY)) return;

            var bounds = Background.GetDesignBounds();
            float deltaPixels = virtualX - _lastMousePos.X / CanvasScale.X;
            float deltaTime = deltaPixels / bounds.effWidth * (_visibleEnd - _visibleStart);

            _visibleStart -= deltaTime;
            _visibleEnd -= deltaTime;

            if (_visibleStart < 0) { _visibleEnd += -_visibleStart; _visibleStart = 0; }
            if (_visibleEnd > TotalDuration) { _visibleStart -= _visibleEnd - TotalDuration; _visibleEnd = TotalDuration; }

            RefreshAllVisuals();
            _lastMousePos = new Vector2(virtualX * CanvasScale.X, _lastMousePos.Y);
        }

        private bool IsOverTimeline(float virtualX, float virtualY)
        {
            var bounds = Background.GetDesignBounds();
            return virtualX >= bounds.designLeft &&
                   virtualX <= bounds.designLeft + bounds.effWidth &&
                   virtualY >= bounds.designTop &&
                   virtualY <= bounds.designTop + bounds.effHeight;
        }
        #endregion

        #region Visual Updates
        private void RefreshAllVisuals()
        {
            UpdateWaveformVisuals();
            UpdatePhrasesVisuals();        // ← теперь содержит оба пула
            RefreshVisualTicks();

            RemoveChild(Playhead);
            RemoveChild(_playheadLine);
            AddChild(Playhead);
            AddChild(_playheadLine);
        }

        private void UpdateWaveformVisuals()
        {
            var bounds = Background.GetDesignBounds();
            float left = bounds.Item1;
            float width = bounds.Item3;
            float centerY = Background.Position.Y;
            float maxH = Background.Scale.Y * 0.55f;

            int target = (int)Math.Clamp(width / 2.8f, 64, 1200);

            while (_waveformBars.Count < target)
            {
                var bar = new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 1, 1)
                {
                    Color = new Color4(0.25f, 0.65f, 1.0f, 0.92f),
                    Pivot = new Vector2(0.5f, 0.5f)
                };
                AddChild(bar);
                _waveformBars.Add(bar);
            }
            for (int i = target; i < _waveformBars.Count; i++)
                _waveformBars[i].Scale = Vector2.Zero;

            float[] preview = AudioManager.Instance?.WaveformPreview ?? Array.Empty<float>();
            float visDur = _visibleEnd - _visibleStart;
            float timePerBar = visDur / target;

            for (int i = 0; i < target; i++)
            {
                float t = _visibleStart + (i + 0.5f) * timePerBar;
                int bin = preview.Length > 0 ? Math.Clamp((int)(t / TotalDuration * preview.Length), 0, preview.Length - 1) : 0;
                float amp = preview.Length > 0 ? preview[bin] : 0.3f;

                float barX = left + i * (width / target) + (width / target) * 0.5f;
                _waveformBars[i].Position = new Vector2(barX, centerY);
                _waveformBars[i].Scale = new Vector2(width / target * 0.85f, amp * maxH);
            }
        }

        private void UpdatePhrasesVisuals()
        {
            int phraseCount = _phrases.Count;

            // ====================== ФИОЛЕТОВЫЕ ФРАЗЫ (пул) ======================
            while (_phraseBars.Count < phraseCount)
            {
                var bar = new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 1, 1)
                {
                    Color = new Color4(0.55f, 0.25f, 0.85f, 0.5f),
                    Opacity = 0.5f,
                    Pivot = new Vector2(0.5f, 0.5f)
                };
                AddChild(bar);
                _phraseBars.Add(bar);

                var lh = new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 8, 1)
                {
                    Color = new Color4(1f, 1f, 1f, 0.95f),
                    Pivot = new Vector2(0.5f, 0.5f)
                };
                AddChild(lh);
                _leftHandles.Add(lh);

                var rh = new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 8, 1)
                {
                    Color = new Color4(1f, 1f, 1f, 0.95f),
                    Pivot = new Vector2(0.5f, 0.5f)
                };
                AddChild(rh);
                _rightHandles.Add(rh);
            }

            // Скрываем лишние фразы
            for (int i = phraseCount; i < _phraseBars.Count; i++)
            {
                _phraseBars[i].Scale = Vector2.Zero;
                _leftHandles[i].Scale = Vector2.Zero;
                _rightHandles[i].Scale = Vector2.Zero;
            }

            float phraseHeight = Background.Scale.Y * 0.28f;
            float phraseY = Background.Position.Y - Background.Scale.Y * 0.31f;

            for (int i = 0; i < phraseCount; i++)
            {
                var phrase = _phrases[i];
                var (centerX, barWidth) = GetTimeBarBounds(phrase.StartTime, phrase.EndTime);

                if (barWidth < 12f)
                {
                    _phraseBars[i].Scale = Vector2.Zero;
                    _leftHandles[i].Scale = Vector2.Zero;
                    _rightHandles[i].Scale = Vector2.Zero;
                    continue;
                }

                _phraseBars[i].Position = new Vector2(centerX, phraseY);
                _phraseBars[i].Scale = new Vector2(barWidth, phraseHeight);

                _leftHandles[i].Position = new Vector2(centerX - barWidth * 0.5f, phraseY);
                _leftHandles[i].Scale = new Vector2(8, phraseHeight * 1.3f);

                _rightHandles[i].Position = new Vector2(centerX + barWidth * 0.5f, phraseY);
                _rightHandles[i].Scale = new Vector2(8, phraseHeight * 1.3f);
            }

            // ====================== ЖЁЛТЫЕ СЛАЙДЕРЫ (ТЕПЕРЬ ТОЖЕ ПУЛ) ======================
            int totalSliders = _phrases.Sum(p => p.Sliders?.Count ?? 0);

            while (_sliderBars.Count < totalSliders)
            {
                // Основная полоса слайдера
                var sBar = new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 1, 1)
                {
                    Color = new Color4(0.95f, 0.65f, 0.25f, 0.95f),
                    Pivot = new Vector2(0.5f, 0.5f)
                };
                AddChild(sBar);
                _sliderBars.Add(sBar);

                // Левая ручка
                var lh = new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 8, 1)
                {
                    Color = new Color4(1f, 1f, 1f, 0.95f),
                    Pivot = new Vector2(0.5f, 0.5f)
                };
                AddChild(lh);
                _sliderLeftHandles.Add(lh);

                // Правая ручка
                var rh = new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 8, 1)
                {
                    Color = new Color4(1f, 1f, 1f, 0.95f),
                    Pivot = new Vector2(0.5f, 0.5f)
                };
                AddChild(rh);
                _sliderRightHandles.Add(rh);
            }

            // Скрываем лишние слайдеры
            for (int i = totalSliders; i < _sliderBars.Count; i++)
            {
                _sliderBars[i].Scale = Vector2.Zero;
                _sliderLeftHandles[i].Scale = Vector2.Zero;
                _sliderRightHandles[i].Scale = Vector2.Zero;
            }

            _sliderReferences.Clear();

            float sliderY = phraseY + phraseHeight * 0.75f;
            float minVisualWidth = 6f;
            int sliderIdx = 0;

            foreach (var phrase in _phrases)
            {
                if (phrase.Sliders == null || phrase.Sliders.Count == 0) continue;

                for (int i = 0; i < phrase.Sliders.Count; i++)
                {
                    var slider = phrase.Sliders[i];
                    var (sx, sw) = GetTimeBarBounds(slider.startTime, slider.endTime);
                    float visualWidth = Math.Max(sw, minVisualWidth);

                    _sliderBars[sliderIdx].Position = new Vector2(sx, sliderY);
                    _sliderBars[sliderIdx].Scale = new Vector2(visualWidth, phraseHeight * 0.62f);

                    _sliderLeftHandles[sliderIdx].Position = new Vector2(sx - visualWidth * 0.5f, sliderY);
                    _sliderLeftHandles[sliderIdx].Scale = new Vector2(8, phraseHeight * 1.1f);

                    _sliderRightHandles[sliderIdx].Position = new Vector2(sx + visualWidth * 0.5f, sliderY);
                    _sliderRightHandles[sliderIdx].Scale = new Vector2(8, phraseHeight * 1.1f);

                    _sliderReferences.Add((phrase, i));
                    sliderIdx++;
                }
            }
        }

        private (float centerX, float width) GetTimeBarBounds(float start, float end)
        {
            float visDur = _visibleEnd - _visibleStart;
            if (visDur <= 0) return (0, 0);

            float nStart = Math.Max(0f, (start - _visibleStart) / visDur);
            float nEnd = Math.Min(1f, (end - _visibleStart) / visDur);
            if (nEnd - nStart < 0.001f) return (0, 0);

            var b = Background.GetDesignBounds();
            float barLeft = b.Item1 + nStart * b.Item3;
            float w = (nEnd - nStart) * b.Item3;

            return (barLeft + w * 0.5f, w);
        }

        private void RefreshVisualTicks()
        {
            // Удаляем старые
            foreach (var t in _tickLines) RemoveChild(t);
            foreach (var l in _timeLabels) RemoveChild(l);
            _tickLines.Clear();
            _timeLabels.Clear();

            float dur = _visibleEnd - _visibleStart;
            if (dur <= 0) return;

            float step = dur switch
            {
                < 5 => 0.2f,
                < 15 => 0.5f,
                < 60 => 1f,
                < 180 => 5f,
                _ => 10f
            };

            var bounds = Background.GetDesignBounds();
            float left = bounds.Item1;
            float w = bounds.Item3;
            float bottomY = Background.Position.Y + 38f;

            for (float t = (float)Math.Ceiling(_visibleStart / step) * step; t <= _visibleEnd + 0.01f; t += step)
            {
                float norm = (t - _visibleStart) / dur;
                if (norm < 0 || norm > 1) continue;

                float x = left + norm * w;

                bool major = Math.Abs(t % 5) < 0.01f || Math.Abs(t % 10) < 0.01f;

                var tick = new SpriteObject(TextureManager.GetTexture("slider_line"),
                    x, Background.Position.Y, 2, major ? 42 : 24)
                {
                    Color = major ? new Color4(0.8f, 0.8f, 0.9f, 1f) : new Color4(0.5f, 0.5f, 0.55f, 1f),
                    Pivot = new Vector2(0.5f, 0f)
                };
                AddChild(tick);
                _tickLines.Add(tick);

                if (major)
                {
                    string labelText = TimeSpan.FromSeconds(t).ToString(t >= 60 ? @"m\:ss" : @"s\.ff");
                    var label = new TextObject(labelText, x, bottomY, 0.9f)
                    {
                        Color = Color4.White,
                        Align = TextRender.TextAlign.Center,
                        ScaleMultiply = 0.33f
                    };
                    AddChild(label);
                    _timeLabels.Add(label);
                }
            }
        }
        #endregion
    }
}