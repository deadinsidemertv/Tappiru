using TappiruCS.Core.GameObject;

public abstract class ModuleWindow
{
    public List<GameObject> obj;
    public Scene _scene;

    public event Action OnClosed;
    public ModuleWindow(Scene scene)
    {


        _scene = scene;
        obj = new List<GameObject>();
       
    }

    public virtual void Show()
    {
        foreach (GameObject o in obj)
            _scene.Add(o);
    }

    public virtual void Close()
    {
        OnClosed?.Invoke();

        foreach (GameObject o in obj)
            _scene?.Remove(o);

    }

}