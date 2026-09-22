using UnityEngine;
using UnityEngine.SceneManagement;

public enum Scenes
{
    MainMenu,
    GameObjects,
    Dots
}

public class UIManager : Singleton<UIManager>
{
    public void LoadMainMenuScene()
    {
        SceneManager.LoadScene((int)Scenes.MainMenu);
    }
    
    public void LoadGameObjectScene()
    {
        SceneManager.LoadScene((int)Scenes.GameObjects);
    }
    
    public void LoadDotsScene()
    {
        SceneManager.LoadScene((int)Scenes.Dots);
    }
    
    public void Quit()
    {
        Application.Quit();
    }
}
