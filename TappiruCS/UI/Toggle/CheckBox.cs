namespace TappiruCS.UI.Toggle;

public class CheckBox : Toggle
{
    public CheckBox(float x, float y, float scaleX, float scaleY, string texName = "checkbox-default")
        : base(x, y, scaleX, scaleY, texName)
    {
    }

    protected override void OnClick()
    {
        // Обычный чекбокс: переключаем состояние
        SetSelected(!IsSelected, raiseEvent: true);
    }

}