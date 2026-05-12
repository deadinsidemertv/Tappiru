namespace TappiruCS.UI.Toggle;

public class RadioButton : Toggle
{
    public RadioButton(float x, float y, float scaleX, float scaleY, string texName = "radiobutton-default")
        : base(x, y, scaleX, scaleY, texName)
    {
    }

    protected override void OnClick()
    {
        // Радио-кнопка не меняет состояние сама, а только запрашивает активацию
        RaiseRequestActivation();
    }
}