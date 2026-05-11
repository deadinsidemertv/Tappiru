// TimelineDragHandler.cs — инкапсуляция всей drag-логики Timeline
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using TappiruCS.GameLogic;
using TappiruCS.State.Edit.Core;
using TappiruCS.UI;

namespace TappiruCS.State.Edit.TimelineSystem
{
    /// <summary>
    /// Отвечает за обработку всех drag-операций на Timeline:
    ///   • перетаскивание playhead
    ///   • панорамирование (pan)
    ///   • изменение границ фраз (левая / правая ручка)
    ///   • изменение границ слайдеров (левая / правая ручка)
    ///
    /// Timeline передаёт сюда MouseState и контекст (bounds, phrases, visible range),
    /// получая обратно флаг «нужно перерисовать» и новое время для playhead.
    /// </summary>
    internal class TimelineDragHandler
    {
        // ── Состояние ────────────────────────────────────────────────────────────
        private bool _draggingPlayhead;
        private bool _panning;
        private bool _draggingLeftHandle;
        private bool _draggingRightHandle;
        private bool _draggingSliderLeft;
        private bool _draggingSliderRight;

        private Phrase? _draggedPhrase;
        private int _draggedIndex = -1;
        private Phrase? _draggedSliderPhrase;
        private int _draggedSliderIndex = -1;
        private Vector2 _lastMousePos;

        public bool AnyDrag =>
            _draggingPlayhead || _panning ||
            _draggingLeftHandle || _draggingRightHandle ||
            _draggingSliderLeft || _draggingSliderRight;

        // ── Основной метод — вызывается из Timeline.Update ───────────────────────
        /// <param name="mouse">Текущий MouseState</param>
        /// <param name="vx">Виртуальная X мыши (мировые координаты канваса)</param>
        /// <param name="vy">Виртуальная Y мыши</param>
        /// <param name="ctx">Контекст таймлайна (bounds, phrases, visible range)</param>
        /// <param name="timelineClicked">Выбранное время, если пользователь кликнул (иначе null)</param>
        public void Process(
            MouseState mouse,
            float vx, float vy,
            TimelineContext ctx,
            out float? timelineClicked)
        {
            timelineClicked = null;

            if (mouse.IsButtonDown(MouseButton.Left))
            {
                if (!AnyDrag)
                    BeginDrag(vx, vy, ctx);

                if (_draggingPlayhead)
                    timelineClicked = ProcessPlayheadDrag(vx, vy, ctx);

                if (_panning)
                    ProcessPan(vx, ctx);

                if (_draggingLeftHandle || _draggingRightHandle)
                    ProcessPhraseDrag(vx, ctx);

                if (_draggingSliderLeft || _draggingSliderRight)
                    ProcessSliderDrag(vx, ctx);

                _lastMousePos = new Vector2(vx * ctx.CanvasScaleX, _lastMousePos.Y);
            }
            else
            {
                ClearAll();

                // Одиночный клик (без drag)
                if (mouse.IsButtonPressed(MouseButton.Left) && ctx.IsOver(vx, vy))
                {
                    float norm = (vx - ctx.Bounds.designLeft) / ctx.Bounds.effWidth;
                    timelineClicked = ctx.VisibleStart + norm * ctx.VisibleDuration;
                }
            }
        }

        // ── Начало drag-жеста ────────────────────────────────────────────────────
        private void BeginDrag(float vx, float vy, TimelineContext ctx)
        {
            _lastMousePos = new Vector2(vx * ctx.CanvasScaleX, vy);

            if (TryStartSliderDrag(vx, vy, ctx, out bool sliderLeft))
            {
                if (sliderLeft) _draggingSliderLeft = true;
                else _draggingSliderRight = true;
            }
            else if (TryStartPhraseDrag(vx, vy, ctx, out bool phraseLeft))
            {
                if (phraseLeft) _draggingLeftHandle = true;
                else _draggingRightHandle = true;
            }
            else if (ctx.IsNearPlayhead(vx, vy))
            {
                _draggingPlayhead = true;
            }
            else if (ctx.IsOver(vx, vy))
            {
                _panning = true;
            }
        }

        // ── Drag: playhead ───────────────────────────────────────────────────────
        private float? ProcessPlayheadDrag(float vx, float vy, TimelineContext ctx)
        {
            if (!ctx.IsOver(vx, vy)) return null;
            float norm = (vx - ctx.Bounds.designLeft) / ctx.Bounds.effWidth;
            return ctx.VisibleStart + norm * ctx.VisibleDuration;
        }

        // ── Drag: pan ────────────────────────────────────────────────────────────
        private void ProcessPan(float vx, TimelineContext ctx)
        {
            float deltaPixels = vx - _lastMousePos.X / ctx.CanvasScaleX;
            float deltaTime = deltaPixels / ctx.Bounds.effWidth * ctx.VisibleDuration;

            ctx.VisibleStart -= deltaTime;
            ctx.VisibleEnd -= deltaTime;

            // Зажимаем в [0, total]
            if (ctx.VisibleStart < 0) { ctx.VisibleEnd += -ctx.VisibleStart; ctx.VisibleStart = 0; }
            if (ctx.VisibleEnd > ctx.TotalDuration)
            {
                ctx.VisibleStart -= ctx.VisibleEnd - ctx.TotalDuration;
                ctx.VisibleEnd = ctx.TotalDuration;
            }
        }

        // ── Drag: границы фразы ──────────────────────────────────────────────────
        private void ProcessPhraseDrag(float vx, TimelineContext ctx)
        {
            if (_draggedPhrase == null) return;
            float newTime = ctx.TimeAtX(vx);

            if (_draggingLeftHandle)
            {
                float minStart = _draggedIndex > 0
                    ? ctx.Phrases[_draggedIndex - 1].EndTime + 0.05f
                    : 0f;
                _draggedPhrase.StartTime = Math.Clamp(newTime, minStart, _draggedPhrase.EndTime - 0.2f);
            }
            else
            {
                float maxEnd = _draggedIndex < ctx.Phrases.Count - 1
                    ? ctx.Phrases[_draggedIndex + 1].StartTime - 0.05f
                    : ctx.TotalDuration;
                _draggedPhrase.EndTime = Math.Clamp(newTime, _draggedPhrase.StartTime + 0.2f, maxEnd);
            }
        }

        // ── Drag: границы слайдера ───────────────────────────────────────────────
        private void ProcessSliderDrag(float vx, TimelineContext ctx)
        {
            if (_draggedSliderPhrase == null || _draggedSliderIndex < 0) return;
            var sliders = _draggedSliderPhrase.Sliders;
            if (sliders == null || _draggedSliderIndex >= sliders.Count) return;

            var slider = sliders[_draggedSliderIndex];
            float newTime = ctx.TimeAtX(vx);

            if (_draggingSliderLeft)
                slider.startTime = Math.Clamp(newTime, _draggedSliderPhrase.StartTime, slider.endTime - 0.1f);
            else
                slider.endTime = Math.Clamp(newTime, slider.startTime + 0.1f, _draggedSliderPhrase.EndTime);
        }

        // ── Hit-test: начало drag фразы ──────────────────────────────────────────
        private bool TryStartPhraseDrag(float vx, float vy, TimelineContext ctx, out bool isLeft)
        {
            isLeft = false;
            const float hit = 13f;
            float phraseY = ctx.PhraseCenterWorldY;

            for (int i = 0; i < ctx.LeftHandles.Count; i++)
            {
                if (Math.Abs(vx - ctx.LeftHandles[i].WorldPosition.X) < hit &&
                    Math.Abs(vy - phraseY) < 25f)
                {
                    _draggedPhrase = ctx.Phrases[i];
                    _draggedIndex = i;
                    isLeft = true;
                    return true;
                }
            }
            for (int i = 0; i < ctx.RightHandles.Count; i++)
            {
                if (Math.Abs(vx - ctx.RightHandles[i].WorldPosition.X) < hit &&
                    Math.Abs(vy - phraseY) < 25f)
                {
                    _draggedPhrase = ctx.Phrases[i];
                    _draggedIndex = i;
                    return true;
                }
            }
            return false;
        }

        // ── Hit-test: начало drag слайдера ───────────────────────────────────────
        private bool TryStartSliderDrag(float vx, float vy, TimelineContext ctx, out bool isLeft)
        {
            isLeft = false;
            const float hit = 13f;

            for (int i = 0; i < ctx.SliderLeftHandles.Count; i++)
            {
                var h = ctx.SliderLeftHandles[i];
                if (Math.Abs(vx - h.WorldPosition.X) < hit &&
                    Math.Abs(vy - h.WorldPosition.Y) < 25f)
                {
                    (_draggedSliderPhrase, _draggedSliderIndex) = ctx.SliderRefs[i];
                    isLeft = true;
                    return true;
                }
            }
            for (int i = 0; i < ctx.SliderRightHandles.Count; i++)
            {
                var h = ctx.SliderRightHandles[i];
                if (Math.Abs(vx - h.WorldPosition.X) < hit &&
                    Math.Abs(vy - h.WorldPosition.Y) < 25f)
                {
                    (_draggedSliderPhrase, _draggedSliderIndex) = ctx.SliderRefs[i];
                    return true;
                }
            }
            return false;
        }

        private void ClearAll()
        {
            _draggingPlayhead = _panning =
            _draggingLeftHandle = _draggingRightHandle =
            _draggingSliderLeft = _draggingSliderRight = false;

            _draggedPhrase = null;
            _draggedIndex = -1;
            _draggedSliderPhrase = null;
            _draggedSliderIndex = -1;
        }
    }

    // ── Value Object: контекст для TimelineDragHandler ───────────────────────────
    /// <summary>
    /// Передаётся из Timeline в DragHandler — содержит всё необходимое
    /// для hit-testing и вычисления новых значений времени.
    /// Mutable поля VisibleStart/End — drag-обработчик может их изменить.
    /// </summary>
    internal class TimelineContext
    {
        // Ссылки (readonly — DragHandler только читает)
        public required List<Phrase> Phrases { get; init; }
        public required List<SpriteObject> LeftHandles { get; init; }
        public required List<SpriteObject> RightHandles { get; init; }
        public required List<SpriteObject> SliderLeftHandles { get; init; }
        public required List<SpriteObject> SliderRightHandles { get; init; }
        public required List<(Phrase phrase, int sliderIndex)> SliderRefs { get; init; }

        // Геометрия (readonly)
        public required (float designLeft, float designTop, float effWidth, float effHeight) Bounds { get; init; }
        public required float PhraseCenterWorldY { get; init; }
        public required float CanvasScaleX { get; init; }
        public required float TotalDuration { get; init; }

        // Mutable — DragHandler сдвигает видимое окно при pan
        public float VisibleStart { get; set; }
        public float VisibleEnd { get; set; }

        public float VisibleDuration => VisibleEnd - VisibleStart;

        public float TimeAtX(float vx)
        {
            float norm = (vx - Bounds.designLeft) / Bounds.effWidth;
            return VisibleStart + norm * VisibleDuration;
        }

        public bool IsOver(float vx, float vy) =>
            vx >= Bounds.designLeft &&
            vx <= Bounds.designLeft + Bounds.effWidth &&
            vy >= Bounds.designTop &&
            vy <= Bounds.designTop + Bounds.effHeight;

        public bool IsNearPlayhead(float vx, float vy) => false; // Timeline сам проверяет playhead
    }
}