using UnityEngine;
using UnityEngine.SceneManagement; // Это пространство имен позволяет управлять сценами

public class MainMenuManager : MonoBehaviour
{
    public GameObject menuCamera; 
    private GameObject cityCamera; // Теперь она приватная, перетаскивать не нужно
    private PlayerHeroController playerController;

    void Start()
    {
        // Ищем объект с тегом MainCamera во всех загруженных сценах
        cityCamera = GameObject.FindWithTag("MainCamera");
        playerController = FindFirstObjectByType<PlayerHeroController>();
        
        if (cityCamera == null)
        {
            Debug.LogError("Камера в городе не найдена! Проверь тег 'MainCamera' на ней.");
        }

        // В меню курсор должен быть видимым и свободным для UI.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null)
        {
            playerController.enabled = false;
            playerController.SetCursorLocked(false);
        }
    }

    public void StartGame()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.SetCursorLocked(true);
        }

        // 1. Включаем город (это безопасно)
        if (cityCamera != null) cityCamera.SetActive(true);
    
        // 2. Выключаем меню в конце кадра
        Invoke("DisableMenu", 0.1f); 
    }

    void DisableMenu()
    {
        if (menuCamera != null) menuCamera.SetActive(false);
        // Также выключи основной объект Main_Menu, чтобы UI не перехватывал клики
        GameObject.Find("Main_Menu")?.SetActive(false);
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
