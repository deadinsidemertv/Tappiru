// PhraseSerializer.cs — конвертация между Phrase (runtime) и TimingEvent (JSON)
using System.Collections.Generic;
using System.Linq;
using TappiruCS.GameLogic;

namespace TappiruCS.State.Edit
{
    /// <summary>
    /// Преобразует список Phrase в список TimingEvent для сохранения в JsonMap и обратно.
    /// Вынесено из EditState, чтобы не смешивать I/O-логику с UI-логикой.
    /// </summary>
    internal static class PhraseSerializer
    {
            public static List<TimingEvent> ToEvents(IEnumerable<Phrase> phrases) =>
                phrases.Select(p => new TimingEvent
                {
                    startTime = p.StartTime,
                    endTime = p.EndTime,
                    text = p.Text,
                    transription = p.Transcription ?? "",                    // ← добавлено
                    sliders = p.Sliders?
                        .Select(s => new SliderTiming
                        {
                            charIndex = s.charIndex,
                            startTime = s.startTime,
                            endTime = s.endTime
                        }).ToList() ?? new List<SliderTiming>()
                }).ToList();

            public static List<Phrase> FromEvents(IEnumerable<TimingEvent>? events)
            {
                if (events == null) return new List<Phrase>();

                return events.Select(e =>
                {
                    float start = (float)e.startTime;
                    float end = (float)(e.endTime > 0 ? e.endTime : e.startTime + 4.0);

                    var phrase = new Phrase(
                        start,
                        end,
                        e.text ?? "",
                        e.transription ?? ""   // ← добавлено
                    );

                    if (e.sliders != null)
                    {
                        phrase.Sliders = e.sliders.Select(s => new SliderTiming
                        {
                            charIndex = s.charIndex,
                            startTime = s.startTime,
                            endTime = s.endTime
                        }).ToList();
                    }

                    return phrase;
                }).ToList();
            }
        }
    }
