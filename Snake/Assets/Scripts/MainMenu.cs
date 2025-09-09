using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    [SerializeField] UIDocument mainMenuDocument;

    private Button settingsButton;
    private Button exitButton;
    private Button playButton;
    private void Awake()
    {
        VisualElement root = mainMenuDocument.rootVisualElement;

        playButton = root.Q<Button>("PlayButton");
        settingsButton = root.Q<Button>("OptionsButton");
        exitButton = root.Q<Button>("ExitButton");

        playButton.clickable.clicked += PlayGame;
        settingsButton.clickable.clicked += ShowSettingsMenu;
        exitButton.clickable.clicked += ExitGame;
    }
    private void ShowSettingsMenu()
    {
        Debug.Log("Ustawienia"); //Do implementacji
    }
    private void PlayGame()
    {
        SceneManager.LoadScene("SampleScene");
    }
    public void ExitGame()
    {
        Application.Quit();
    }
}
