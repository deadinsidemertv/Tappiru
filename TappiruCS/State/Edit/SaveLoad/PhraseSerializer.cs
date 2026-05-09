using TappiruCS.GameLogic;
using TappiruCS.State.Edit.Core;

internal static class PhraseSerializer
{
    public static List<TimingEvent> ToEvents(IEnumerable<Phrase> phrases) =>
        phrases.Select(p => new TimingEvent
        {
            startTime = p.StartTime,
            endTime = p.EndTime,
            text = p.Text,
            transription = p.Transcription ?? "",

            sliders = p.Sliders?
                .Select(s => new TappiruCS.GameLogic.SliderTiming
                {
                    charIndex = s.charIndex,
                    startTime = s.startTime,
                    endTime = s.endTime
                }).ToList() ?? new(),

            // ← НОВОЕ
            mapping = p.Mapping?.ToList() ?? new List<int>()

        }).ToList();

    public static List<Phrase> FromEvents(IEnumerable<TimingEvent>? events)
    {
        if (events == null) return new List<Phrase>();

        return events.Select(e =>
        {
            float start = (float)e.startTime;
            float end = (float)(e.endTime > 0 ? e.endTime : e.startTime + 4.0);

            var phrase = new Phrase(start, end, e.text ?? "", e.transription ?? "");

            if (e.sliders != null)
            {
                phrase.Sliders = e.sliders.Select(s => new TappiruCS.State.Edit.Core.SliderTiming
                {
                    charIndex = s.charIndex,
                    startTime = s.startTime,
                    endTime = s.endTime
                }).ToList();
            }

            // ← НОВОЕ
            if (e.mapping != null)
                phrase.Mapping = new List<int>(e.mapping);
            else
                phrase.Mapping = new List<int>(new int[phrase.Text.Length]);

            return phrase;
        }).ToList();
    }
}