using UnityEngine;
using TMPro;

public class KeyBindManager : MonoBehaviour
{
    public GameObject confirmationWindow;
    public TextMeshProUGUI statusText;

    // Ссылки на текстовые поля напротив действий
    public TextMeshProUGUI forwardLabel;
    public TextMeshProUGUI backwardLabel;
    public TextMeshProUGUI leftLabel;
    public TextMeshProUGUI rightLabel;
    public TextMeshProUGUI jumpLabel;
    public TextMeshProUGUI interactLabel;

    private string currentAction;

    private void Start()
    {
        UpdateAllLabels();
    }

    // Этот метод вызывается кнопкой "Змінити"
    public void OpenConfirmation(string actionName)
    {
        currentAction = actionName; // Теперь мы запоминаем, ЧТО именно меняем
        confirmationWindow.SetActive(true);
        statusText.text = "Press key for: " + actionName; // Подсказка пользователю
    }

    void OnGUI()
    {
        if (confirmationWindow.activeSelf)
        {
            Event e = Event.current;
            if (e.isKey && e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            {
                SaveNewKey(currentAction, e.keyCode);
                confirmationWindow.SetActive(false);
            }
        }
    }

    void SaveNewKey(string action, KeyCode key)
    {
        // Теперь в консоли будет полное сообщение
        Debug.Log("Клавіша для " + action + " змінена на " + key);

        // Обновляем визуальное отображение в меню
        UpdateActionLabel(action, key);

        // Сохраняем настройку (Backend часть)
        PlayerPrefs.SetString(action, key.ToString());
        PlayerPrefs.Save();
    }

    public static KeyCode GetBoundKey(string action, KeyCode defaultKey)
    {
        string savedValue = PlayerPrefs.GetString(action, string.Empty);
        if (string.IsNullOrEmpty(savedValue))
        {
            return defaultKey;
        }

        if (System.Enum.TryParse(savedValue, out KeyCode parsedKey))
        {
            return parsedKey;
        }

        return defaultKey;
    }

    private void UpdateAllLabels()
    {
        UpdateActionLabel("Forward", GetBoundKey("Forward", KeyCode.W));
        UpdateActionLabel("Backward", GetBoundKey("Backward", KeyCode.S));
        UpdateActionLabel("Left", GetBoundKey("Left", KeyCode.A));
        UpdateActionLabel("Right", GetBoundKey("Right", KeyCode.D));
        UpdateActionLabel("Jump", GetBoundKey("Jump", KeyCode.Space));
        UpdateActionLabel("Interact", GetBoundKey("Interact", KeyCode.E));
    }

    private void UpdateActionLabel(string action, KeyCode key)
    {
        if (action == "Forward" && forwardLabel != null)
        {
            forwardLabel.text = key.ToString();
            return;
        }

        if (action == "Backward" && backwardLabel != null)
        {
            backwardLabel.text = key.ToString();
            return;
        }

        if (action == "Left" && leftLabel != null)
        {
            leftLabel.text = key.ToString();
            return;
        }

        if (action == "Right" && rightLabel != null)
        {
            rightLabel.text = key.ToString();
            return;
        }

        if (action == "Jump" && jumpLabel != null)
        {
            jumpLabel.text = key.ToString();
            return;
        }

        if (action == "Interact" && interactLabel != null)
        {
            interactLabel.text = key.ToString();
        }
    }
}