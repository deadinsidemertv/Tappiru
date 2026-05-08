// Timeline.cs — ИСПРАВЛЕННАЯ ВЕРСИЯ (добавлены AddChild)
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using TappiruCS.Render.Text;
using TappiruCS.Render.Audio;
using TappiruCS.State.Edit.Core;

namespace TappiruCS.State.Edit.TimelineSystem
{
    public class Timeline : GameObject
    {
        public event Action<float> OnTimeClicked;
        public event Action<ITimelineSelectable?>? OnObjectSelected;
        public SpriteObject Background { get; private set; }
        public SpriteObject Playhead { get; private set; }
        private SpriteObject _playheadLine;

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

        public List<Phrase> _phrases = new();
        public float TotalDuration { get; private set; } = 300f;

        private float _visibleStart = 0f;
        private float _visibleEnd = 300f;
        private float _lastScrollValue = 0f;

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

        public ITimelineSelectable? SelectedObject { get; set; }

        public Timeline(float x, float y, float width, float height)
        {
            LocalPosition = new Vector2(x, y);
            Layer = 5;

            Background = new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, width, height)
            {
                Color = new Color4(0.13f, 0.13f, 0.19f, 0.5f),
                Layer = 6,
            };
            AddChild(Background);   // ← добавил

            Playhead = new SpriteObject(TextureManager.GetTexture("marker"), 0, -35f, 28, 28)
            {
                Color = new Color4(1f, 0.35f, 0.35f, 1f),
                Pivot = new Vector2(0.5f, 1f),
                Layer = 7,
            };
            AddChild(Playhead);     // ← добавил

            _playheadLine = new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 4, height)
            {
                Color = new Color4(1f, 0.35f, 0.35f, 0.9f),
                Pivot = new Vector2(0.5f, 0.5f),
                Layer = 7,
            };
            AddChild(_playheadLine); // ← добавил
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
            float norm = Math.Clamp((time - _visibleStart) / (_visibleEnd - _visibleStart), 0f, 1f);
            var bounds = Background.GetDesignBounds(); // (designLeft, designTop, effWidth, effHeight)
            float worldX = bounds.designLeft + norm * bounds.effWidth;
            float localX = worldX - WorldPosition.X;
            Playhead.LocalPosition = new Vector2(localX, Playhead.LocalPosition.Y);
            _playheadLine.LocalPosition = new Vector2(localX, _playheadLine.LocalPosition.Y);
        }
        #endregion

        #region Update & Input
        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);
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
            float normMouse = Math.Clamp((virtualMouseX - bounds.designLeft) / bounds.effWidth, 0f, 1f);
            float timeUnderCursor = _visibleStart + normMouse * (_visibleEnd - _visibleStart);

            float zoomFactor = scrollDelta > 0 ? 0.75f : 1.35f;
            float newDur = (_visibleEnd - _visibleStart) * zoomFactor;
            newDur = Math.Clamp(newDur, 0.5f, TotalDuration);

            float newStart = timeUnderCursor - normMouse * newDur;
            float newEnd = newStart + newDur;

            if (newStart < 0) { newStart = 0; newEnd = newDur; }
            if (newEnd > TotalDuration) { newEnd = TotalDuration; newStart = TotalDuration - newDur; }

            _visibleStart = newStart;
            _visibleEnd = newEnd;
            RefreshAllVisuals();
        }

        private void HandleMouseInteraction(MouseState mouse)
        {
            float virtualX = mouse.X / CanvasScale.X;
            float virtualY = mouse.Y / CanvasScale.Y;

            // === НОВОЕ: Обработка одиночного клика для выбора объекта ===
            if (mouse.IsButtonPressed(MouseButton.Left))
            {
                // Сначала пытаемся выбрать слайдер (они находятся выше фраз)
                if (TrySelectSlider(virtualX, virtualY, out SliderTiming? selectedSlider))
                {
                    OnObjectSelected?.Invoke(selectedSlider);
                    return; // не запускаем pan или playhead
                }

                // Затем пытаемся выбрать фразу
                if (TrySelectPhrase(virtualX, virtualY, out Phrase? selectedPhrase))
                {
                    OnObjectSelected?.Invoke(selectedPhrase);
                    return; // не запускаем pan или playhead
                }
            }

            // === Старый код drag-логики (остаётся почти без изменений) ===
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
                    else if (Math.Abs(virtualX - (WorldPosition.X + Playhead.LocalPosition.X)) < 30f &&
                             Math.Abs(virtualY - (WorldPosition.Y + Playhead.LocalPosition.Y)) < 45f)
                    {
                        _draggingPlayhead = true;
                    }
                    else if (IsOverTimeline(virtualX, virtualY))
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

            // Обычный клик по пустому месту таймлайна (playhead)
            if (mouse.IsButtonPressed(MouseButton.Left) && IsOverTimeline(virtualX, virtualY) &&
                !_draggingPlayhead && !_panning && !_draggingLeftHandle && !_draggingRightHandle &&
                !_draggingSliderLeftHandle && !_draggingSliderRightHandle)
            {
                var bounds = Background.GetDesignBounds();
                float norm = (virtualX - bounds.designLeft) / bounds.effWidth;
                OnTimeClicked?.Invoke(_visibleStart + norm * (_visibleEnd - _visibleStart));
            }
        }

        private bool TryStartHandleDrag(float virtualX, float virtualY, out bool isLeft)
        {
            isLeft = false;

            // Можно начинать drag только если фраза выбрана
            if (SelectedObject is not Phrase selectedPhrase)
                return false;

            const float hit = 13f;
            float phraseY = Background.WorldPosition.Y - Background.Scale.Y * 0.31f;

            for (int i = 0; i < _leftHandles.Count; i++)
            {
                if (_phrases[i] != selectedPhrase) continue; // ← только выбранная фраза

                float hx = _leftHandles[i].WorldPosition.X;
                if (Math.Abs(virtualX - hx) < hit && Math.Abs(virtualY - phraseY) < 25f)
                {
                    _draggedPhrase = _phrases[i];
                    _draggedIndex = i;
                    isLeft = true;
                    return true;
                }
            }

            for (int i = 0; i < _rightHandles.Count; i++)
            {
                if (_phrases[i] != selectedPhrase) continue; // ← только выбранная фраза

                float hx = _rightHandles[i].WorldPosition.X;
                if (Math.Abs(virtualX - hx) < hit && Math.Abs(virtualY - phraseY) < 25f)
                {
                    _draggedPhrase = _phrases[i];
                    _draggedIndex = i;
                    isLeft = false;
                    return true;
                }
            }
            return false;
        }

        private void ProcessHandleDrag(float virtualX, (float designLeft, float designTop, float effWidth, float effHeight) bounds)
        {
            if ((!_draggingLeftHandle && !_draggingRightHandle) || _draggedPhrase == null) return;
            float norm = (virtualX - bounds.designLeft) / bounds.effWidth;
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

        private bool TryStartSliderHandleDrag(float virtualX, float virtualY, out bool isLeft)
        {
            isLeft = false;

            // Можно начинать drag только если выбран именно этот слайдер
            if (SelectedObject is not SliderTiming selectedSlider)
                return false;

            const float hit = 13f;

            for (int i = 0; i < _sliderLeftHandles.Count; i++)
            {
                if (i >= _sliderReferences.Count) break;

                var (phrase, index) = _sliderReferences[i];
                if (phrase.Sliders?[index] != selectedSlider) continue; // ← только выбранный слайдер

                var h = _sliderLeftHandles[i];
                if (Math.Abs(virtualX - h.WorldPosition.X) < hit &&
                    Math.Abs(virtualY - h.WorldPosition.Y) < 25f)
                {
                    _draggedSliderPhrase = phrase;
                    _draggedSliderIndex = index;
                    isLeft = true;
                    return true;
                }
            }

            for (int i = 0; i < _sliderRightHandles.Count; i++)
            {
                if (i >= _sliderReferences.Count) break;

                var (phrase, index) = _sliderReferences[i];
                if (phrase.Sliders?[index] != selectedSlider) continue;

                var h = _sliderRightHandles[i];
                if (Math.Abs(virtualX - h.WorldPosition.X) < hit &&
                    Math.Abs(virtualY - h.WorldPosition.Y) < 25f)
                {
                    _draggedSliderPhrase = phrase;
                    _draggedSliderIndex = index;
                    isLeft = false;
                    return true;
                }
            }
            return false;
        }

        private void ProcessSliderHandleDrag(float virtualX, float virtualY)
        {
            if ((!_draggingSliderLeftHandle && !_draggingSliderRightHandle) || _draggedSliderPhrase == null || _draggedSliderIndex < 0)
                return;
            if (!IsOverTimeline(virtualX, virtualY)) return;

            var bounds = Background.GetDesignBounds();
            float norm = (virtualX - bounds.designLeft) / bounds.effWidth;
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
            float norm = (virtualX - bounds.designLeft) / bounds.effWidth;
            float newTime = _visibleStart + norm * (_visibleEnd - _visibleStart);
            OnTimeClicked?.Invoke(newTime);

            float worldX = bounds.designLeft + norm * bounds.effWidth;
            float localX = worldX - WorldPosition.X;
            Playhead.LocalPosition = new Vector2(localX, Playhead.LocalPosition.Y);
            _playheadLine.LocalPosition = new Vector2(localX, _playheadLine.LocalPosition.Y);
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
            return virtualX >= bounds.designLeft && virtualX <= bounds.designLeft + bounds.effWidth &&
                   virtualY >= bounds.designTop && virtualY <= bounds.designTop + bounds.effHeight;
        }
        #endregion

        #region Visual Updates (local coordinates)
        public void RefreshAllVisuals()
        {
            UpdateWaveformVisuals();
            UpdatePhrasesVisuals();
            RefreshVisualTicks();
        }

        private void UpdateWaveformVisuals()
        {
            var bounds = Background.GetDesignBounds();
            float leftWorld = bounds.designLeft;
            float widthWorld = bounds.effWidth;
            float centerYWorld = Background.WorldPosition.Y;
            float maxH = Background.Scale.Y * 0.55f;

            int target = (int)Math.Clamp(widthWorld / 2.8f, 64, 1200);
            EnsurePoolSize(_waveformBars, target, () => new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 1, 1)
            {
                Color = new Color4(0.25f, 0.65f, 1.0f, 0.92f),
                Pivot = new Vector2(0.5f, 0.5f),
            });
            // Добавляем в иерархию через EnsurePoolSize (там есть AddChild)

            float[] preview = AudioManager.Instance?.WaveformPreview ?? Array.Empty<float>();
            float visDur = _visibleEnd - _visibleStart;
            float timePerBar = visDur / target;

            for (int i = 0; i < target; i++)
            {
                float t = _visibleStart + (i + 0.5f) * timePerBar;
                int bin = preview.Length > 0 ? Math.Clamp((int)(t / TotalDuration * preview.Length), 0, preview.Length - 1) : 0;
                float amp = preview.Length > 0 ? preview[bin] : 0.3f;

                float barWorldX = leftWorld + i * (widthWorld / target) + (widthWorld / target) * 0.5f;
                float barLocalX = barWorldX - WorldPosition.X;
                _waveformBars[i].LocalPosition = new Vector2(barLocalX, centerYWorld - WorldPosition.Y);
                _waveformBars[i].Scale = new Vector2(widthWorld / target * 0.85f, amp * maxH);
            }
        }

        private void UpdatePhrasesVisuals()
        {
            int phraseCount = _phrases.Count;
            EnsurePoolSize(_phraseBars, phraseCount, () => new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 1, 1)
            {
                Color = new Color4(0.55f, 0.25f, 0.85f, 0.5f),
                Opacity = 0.5f,
                Pivot = new Vector2(0.5f, 0.5f),
                Layer = 6,
            });
            EnsurePoolSize(_leftHandles, phraseCount, () => new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 15, 15)
            {
                Color = new Color4(1f, 1f, 1f, 0.95f),
                Pivot = new Vector2(0.5f, 0.5f),
            });
            EnsurePoolSize(_rightHandles, phraseCount, () => new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 15, 15)
            {
                Color = new Color4(1f, 1f, 1f, 0.95f),
                Pivot = new Vector2(0.5f, 0.5f),
            });

            float phraseHeight = Background.Scale.Y * 0.28f;
            float phraseCenterWorldY = Background.WorldPosition.Y - Background.Scale.Y * 0.31f;
            float phraseCenterLocalY = phraseCenterWorldY - WorldPosition.Y;

            for (int i = 0; i < phraseCount; i++)
            {
                var phrase = _phrases[i];
                var (centerXWorld, barWidthWorld) = GetTimeBarBounds(phrase.StartTime, phrase.EndTime);

                if (barWidthWorld < 12f)
                {
                    _phraseBars[i].Scale = Vector2.Zero;
                    _leftHandles[i].Scale = Vector2.Zero;
                    _rightHandles[i].Scale = Vector2.Zero;
                    continue;
                }

                // Подсветка выбранной фразы
                bool isSelected = SelectedObject == phrase;

                _phraseBars[i].Color = isSelected
                    ? new Color4(0.95f, 0.75f, 0.25f, 0.9f)
                    : new Color4(0.55f, 0.25f, 0.85f, 0.5f);

                float localX = centerXWorld - WorldPosition.X;
                _phraseBars[i].LocalPosition = new Vector2(localX, phraseCenterLocalY);
                _phraseBars[i].Scale = new Vector2(barWidthWorld, phraseHeight);

                _leftHandles[i].LocalPosition = new Vector2(localX - barWidthWorld * 0.5f, phraseCenterLocalY);
                _leftHandles[i].Scale = new Vector2(8, phraseHeight * 1.3f);

                _rightHandles[i].LocalPosition = new Vector2(localX + barWidthWorld * 0.5f, phraseCenterLocalY);
                _rightHandles[i].Scale = new Vector2(8, phraseHeight * 1.3f);
            }

            // Слайдеры
            int totalSliders = _phrases.Sum(p => p.Sliders?.Count ?? 0);
            EnsurePoolSize(_sliderBars, totalSliders, () => new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 1, 1)
            {
                Color = new Color4(0.95f, 0.65f, 0.25f, 0.95f),
                Pivot = new Vector2(0.5f, 0.5f),
            });
            EnsurePoolSize(_sliderLeftHandles, totalSliders, () => new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 8, 1)
            {
                Color = new Color4(1f, 1f, 1f, 0.95f),
                Pivot = new Vector2(0.5f, 0.5f),
            });
            EnsurePoolSize(_sliderRightHandles, totalSliders, () => new SpriteObject(TextureManager.GetTexture("slider_line"), 0, 0, 8, 1)
            {
                Color = new Color4(1f, 1f, 1f, 0.95f),
                Pivot = new Vector2(0.5f, 0.5f),
            });

            _sliderReferences.Clear();
            float sliderYWorld = phraseCenterWorldY + phraseHeight * 0.75f;
            float sliderYLocal = sliderYWorld - WorldPosition.Y;
            int sliderIdx = 0;

            foreach (var phrase in _phrases)
            {
                if (phrase.Sliders == null) continue;
                int localSliderIdx = 0;
                foreach (var slider in phrase.Sliders)
                {
                    var (centerXWorld, widthWorld) = GetTimeBarBounds(slider.startTime, slider.endTime);
                    float visualWidth = Math.Max(widthWorld, 6f);
                    float localX = centerXWorld - WorldPosition.X;

                    // ==================== ПОДСВЕТКА СЛАЙДЕРА ====================
                    bool isSliderSelected = SelectedObject == slider;

                    _sliderBars[sliderIdx].Color = isSliderSelected
                        ? new Color4(1f, 0.9f, 0.35f, 1f)
                        : new Color4(0.95f, 0.65f, 0.25f, 0.95f);

                    _sliderBars[sliderIdx].LocalPosition = new Vector2(localX, sliderYLocal);
                    _sliderBars[sliderIdx].Scale = new Vector2(visualWidth, phraseHeight * 0.62f);

                    _sliderLeftHandles[sliderIdx].LocalPosition = new Vector2(localX - visualWidth * 0.5f, sliderYLocal);
                    _sliderLeftHandles[sliderIdx].Scale = new Vector2(8, phraseHeight * 1.1f);

                    _sliderRightHandles[sliderIdx].LocalPosition = new Vector2(localX + visualWidth * 0.5f, sliderYLocal);
                    _sliderRightHandles[sliderIdx].Scale = new Vector2(8, phraseHeight * 1.1f);

                    _sliderReferences.Add((phrase, localSliderIdx));
                    sliderIdx++;
                    localSliderIdx++;
                }
            }

            for (int i = totalSliders; i < _sliderBars.Count; i++)
            {
                _sliderBars[i].Scale = Vector2.Zero;
                _sliderLeftHandles[i].Scale = Vector2.Zero;
                _sliderRightHandles[i].Scale = Vector2.Zero;
            }
        }

        private (float centerXWorld, float widthWorld) GetTimeBarBounds(float start, float end)
        {
            float visDur = _visibleEnd - _visibleStart;
            if (visDur <= 0) return (0, 0);
            float nStart = Math.Max(0f, (start - _visibleStart) / visDur);
            float nEnd = Math.Min(1f, (end - _visibleStart) / visDur);
            if (nEnd - nStart < 0.001f) return (0, 0);
            var bounds = Background.GetDesignBounds();
            float leftWorld = bounds.designLeft;
            float widthWorld = bounds.effWidth;
            float barLeftWorld = leftWorld + nStart * widthWorld;
            float w = (nEnd - nStart) * widthWorld;
            return (barLeftWorld + w * 0.5f, w);
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
            float leftWorld = bounds.designLeft;
            float widthWorld = bounds.effWidth;
            float bottomYWorld = Background.WorldPosition.Y ;
            float baseLocalY = bottomYWorld - WorldPosition.Y;

            for (float t = (float)Math.Ceiling(_visibleStart / step) * step; t <= _visibleEnd + 0.01f; t += step)
            {
                float norm = (t - _visibleStart) / dur;
                if (norm < 0 || norm > 1) continue;
                float worldX = leftWorld + norm * widthWorld;
                float localX = worldX - WorldPosition.X;
                bool major = Math.Abs(t % 5) < 0.01f || Math.Abs(t % 10) < 0.01f;

                var tick = new SpriteObject(TextureManager.GetTexture("slider_line"), localX, baseLocalY, 2, major ? 42 : 24)
                {
                    Color = major ? new Color4(0.8f, 0.8f, 0.9f, 1f) : new Color4(0.5f, 0.5f, 0.55f, 1f),
                    Pivot = new Vector2(0.5f, 0f),
                };
                AddChild(tick);   // ← добавил
                _tickLines.Add(tick);

                if (major)
                {
                    string labelText = TimeSpan.FromSeconds(t).ToString(t >= 60 ? @"m\:ss" : @"s\.ff");
                    var label = new TextObject(labelText, localX, baseLocalY + 18f, 72f)
                    {
                        Color = Color4.White,
                        Align = TextAlign.Center,
                        ScaleMultiply = 0.33f,
                    };
                    AddChild(label);  // ← добавил
                    _timeLabels.Add(label);
                }
            }
        }

        private void EnsurePoolSize<T>(List<T> pool, int targetSize, Func<T> factory) where T : GameObject
        {
            while (pool.Count < targetSize)
            {
                var item = factory();
                AddChild(item);
                pool.Add(item);
            }
            for (int i = 0; i < pool.Count; i++)
                pool[i].Active = i < targetSize;
        }


        private bool TrySelectPhrase(float virtualX, float virtualY, out Phrase? phrase)
        {
            phrase = null;

            float phraseCenterY = Background.WorldPosition.Y - Background.Scale.Y * 0.31f;
            float phraseHalfHeight = Background.Scale.Y * 0.22f;

            for (int i = 0; i < _phrases.Count; i++)
            {
                var ph = _phrases[i];
                var (centerX, width) = GetTimeBarBounds(ph.StartTime, ph.EndTime);

                if (width < 20f) continue; // слишком узкая — не выбираем

                float halfWidth = width * 0.5f - 12f; // ← уменьшаем область выбора, чтобы не цепляло ручки

                if (Math.Abs(virtualX - centerX) < halfWidth &&
                    Math.Abs(virtualY - phraseCenterY) < phraseHalfHeight)
                {
                    phrase = ph;
                    return true;
                }
            }
            return false;
        }

        private bool TrySelectSlider(float virtualX, float virtualY, out SliderTiming? slider)
        {
            slider = null;
            int activeCount = _sliderReferences.Count;

            for (int i = 0; i < activeCount && i < _sliderBars.Count; i++)
            {
                var bar = _sliderBars[i];
                if (bar.Scale.X < 8f) continue;

                float centerX = bar.WorldPosition.X;
                // Уменьшаем область выбора, чтобы не цепляло ручки слайдера
                float halfWidth = bar.Scale.X * 0.5f - 10f;

                if (Math.Abs(virtualX - centerX) < halfWidth &&
                    Math.Abs(virtualY - bar.WorldPosition.Y) < 26f)
                {
                    var (phrase, index) = _sliderReferences[i];
                    if (phrase.Sliders != null && index >= 0 && index < phrase.Sliders.Count)
                    {
                        slider = phrase.Sliders[index];
                        return true;
                    }
                }
            }
            return false;
        }


        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection);
        }
        #endregion
    }
}