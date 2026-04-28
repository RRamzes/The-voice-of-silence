using UnityEngine;
using UnityEngine.SceneManagement; // Это пространство имен позволяет управлять сценами

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        // Загружаем сцену под индексом 1 (наш Город)
        SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        // Закрывает игру (работает только в собранной версии .exe)
        Application.Quit();
        Debug.Log("Игра закрыта");
    }
    
    // Ссылка на объект с текстом (перетащим в инспекторе)
    public GameObject authorsPanel;

    // Метод для открытия окна авторов
    public void OpenAuthors()
    {
        authorsPanel.SetActive(true);
    }

    // Метод для закрытия (чтобы вернуться в меню)
    public void CloseAuthors()
    {
        authorsPanel.SetActive(false);
    }
}
