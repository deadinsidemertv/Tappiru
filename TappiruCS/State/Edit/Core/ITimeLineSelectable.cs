namespace TappiruCS.State.Edit.Core
{
    public interface ITimelineSelectable
    {
        float StartTime { get; set; }
        float EndTime { get; set; }

        string GetDisplayName();
        string GetTypeName();
    }
}