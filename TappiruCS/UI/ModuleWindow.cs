using TappiruCS.Core.GameObject;

public abstract class ModuleWindow
{
    public List<GameObject> obj;
    public Scene _scene;
    public ModuleWindow(Scene scene)
    {


        _scene = scene;
        obj = new List<GameObject>();
        
        Show();
    }

    public virtual void Show()
    {
        foreach (GameObject o in obj)
            _scene.Add(o);
    }

    public virtual void Close()
    {
        foreach (GameObject o in obj)
            _scene?.Remove(o);
    }

}