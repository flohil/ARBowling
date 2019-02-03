using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GUIState
{
    MENU, HIGHSCORE, GAME, GAME_NEW, GAME_MENU, GAME_END
}

public enum HighscoreType
{
    LOCAL, ONLINE
}

public static class GUI
{
    private static GUIState _state = GUIState.MENU;

    public static GUIState state
    {
        get
        {
            return _state;
        }
    }

    public static string errorMessage;
    public static string message;

    public static float appliedBonus;
    public static float rollBonus;
    public static float distanceBonus;
    public static float lockedDistanceBonus;

    public static bool remainingRollBonus;

    public static bool endFrame;

    public static int frame;
    public static int roll;
    public static float score;

    public static int framePins;
    public static float frameScore;

    public static int MAX_FRAMES = 10;
    public static int MAX_ROLLS = 2;

    public static Color dimColor = new Color(0, 0, 0, 60f/255f);
    public static Color white = new Color(1f, 1f, 1f, 1f);
    public static Color black = new Color(0f, 0f, 0f, 1f);
    public static Color transparentColor = new Color(0, 0, 0, 0);
    public static Color inactiveColor = new Color(157f / 255f, 157f / 255f, 157f / 255f, 1f);

    public static bool enteredName;
    public static string name;

    public static Dictionary<GUIState, GameObject> states;
    public static Image canvasImage;

    public static bool needLoadHighscores = false;

    public static HighscoreType highscoreType = HighscoreType.LOCAL;

    public static void setState(GUIState state)
    {
        GameObject targetState;
        GameObject currentState;

        if (states.TryGetValue(state, out targetState))
        {
            targetState.SetActive(true);
        }

        if (states.TryGetValue(GUI.state, out currentState))
        {
            currentState.SetActive(false);
        }

        GUI._state = state;

        if (state == GUIState.GAME_MENU || state == GUIState.GAME_END)
        {
            canvasImage.color = white;
        }
        else if (state == GUIState.GAME)
        {
            canvasImage.color = transparentColor;
        }

        if (state == GUIState.HIGHSCORE)
        {
            needLoadHighscores = true;
        }
    }
}

public class guiController : MonoBehaviour {

    GameObject canvas;

    Text errorText;
    Text text;
    Text bonusText;
    Text scoreText;
    Text frameText;
    Text rollText;
    Text endScoreText;
    Text localHighscoresButtonText;
    Text onlineHighscoresButtonText;

    GameObject gameViewPanel;
    GameObject gameViewMainPanel;
    GameObject newGameViewPanel;
    GameObject gameEndViewPanel;
    GameObject gameMenuViewPanel;
    GameObject menuViewPanel;
    GameObject highscoreViewPanel;

    InputField nameInput;
    Button newGameButton;
    Button highscoresButton;
    Button homepageButton;
    Button startGameButton;
    Button gameMenuButton;
    Button gameMenuContinueButton;
    Button backToMainMenuButton;
    Button hsBackToMainMenuButton;
    Button endBackToMainMenuButton;
    Button endNewGameButton;
    Button localHighscoresButton;
    Button onlineHighscoresButton;

    SortedList<float, string> highscores;

    RectTransform highscoreContent;
    public GameObject highscoreEntry;

    // Use this for initialization
    void Start () {
        GUI.errorMessage = null;
        GUI.message = null;
        GUI.score = 0f;
        GUI.frame = 1;
        GUI.roll = 1;

        GUI.frameScore = 0f;
        GUI.remainingRollBonus = false;
        GUI.rollBonus = 1;
        GUI.endFrame = false;
        GUI.lockedDistanceBonus = 1f;

        canvas = GameObject.Find("Canvas");

        GUI.canvasImage = canvas.GetComponent<Image>();

        errorText = GameObject.Find("ErrorText").GetComponent<Text>();
        text = GameObject.Find("MessageText").GetComponent<Text>();
        bonusText = GameObject.Find("BonusText").GetComponent<Text>();
        scoreText = GameObject.Find("ScoreText").GetComponent<Text>();
        frameText = GameObject.Find("FrameText").GetComponent<Text>();
        rollText = GameObject.Find("RollText").GetComponent<Text>();
        endScoreText = GameObject.Find("EndScoreText").GetComponent<Text>();

        gameViewPanel = GameObject.Find("GameView");
        gameViewMainPanel = GameObject.Find("Main");
        newGameViewPanel = GameObject.Find("NewGameView");
        gameEndViewPanel = GameObject.Find("GameEndView");
        gameMenuViewPanel = GameObject.Find("GameMenuView");
        menuViewPanel = GameObject.Find("MenuView");
        highscoreViewPanel = GameObject.Find("HighscoreView");

        highscoreContent = GameObject.Find("HighscoreContent").GetComponent<RectTransform>();

        GUI.states = new Dictionary<GUIState, GameObject>();

        GUI.states.Add(GUIState.MENU, menuViewPanel);
        GUI.states.Add(GUIState.GAME_NEW, newGameViewPanel);
        GUI.states.Add(GUIState.GAME_MENU, gameMenuViewPanel);
        GUI.states.Add(GUIState.GAME_END, gameEndViewPanel);
        GUI.states.Add(GUIState.GAME, gameViewPanel);
        GUI.states.Add(GUIState.HIGHSCORE, highscoreViewPanel);

        startGameButton = GameObject.Find("StartGameButton").GetComponent<Button>();
        nameInput = GameObject.Find("NameInput").GetComponent<InputField>();
        gameMenuButton = GameObject.Find("GameMenuButton").GetComponent<Button>();
        gameMenuContinueButton = GameObject.Find("ContinueGameButton").GetComponent<Button>();
        backToMainMenuButton = GameObject.Find("BackToMainMenuButton").GetComponent<Button>();
        newGameButton = GameObject.Find("NewGameButton").GetComponent<Button>();
        highscoresButton = GameObject.Find("HighscoreButton").GetComponent<Button>();
        homepageButton = GameObject.Find("HomepageButton").GetComponent<Button>();
        hsBackToMainMenuButton = GameObject.Find("HSBackToMenuButton").GetComponent<Button>();
        endBackToMainMenuButton = GameObject.Find("EndBackToMainMenuButton").GetComponent<Button>();
        endNewGameButton = GameObject.Find("EndNewGameButton").GetComponent<Button>();
        localHighscoresButton = GameObject.Find("LocalHighscoresButton").GetComponent<Button>();
        onlineHighscoresButton = GameObject.Find("OnlineHighscoresButton").GetComponent<Button>();

        localHighscoresButtonText = localHighscoresButton.GetComponentInChildren<Text>();
        onlineHighscoresButtonText = onlineHighscoresButton.GetComponentInChildren<Text>();

        errorText.enabled = false;
        text.enabled = false;

        gameViewPanel.SetActive(false);
        newGameViewPanel.SetActive(false);
        gameEndViewPanel.SetActive(false);
        gameMenuViewPanel.SetActive(false);
        highscoreViewPanel.SetActive(false);
        newGameViewPanel.SetActive(false);

        GUI.enteredName = false;
        GUI.name = null;

        startGameButton.interactable = false;

        nameInput.onValueChanged.AddListener(delegate { onNameInputChanged(); });
        startGameButton.onClick.AddListener(delegate { onStartGameButtonClicked(); });
        gameMenuButton.onClick.AddListener(delegate { onGameMenuButtonClicked(); });
        gameMenuContinueButton.onClick.AddListener(delegate { onGameMenuContinueButtonClicked(); });
        backToMainMenuButton.onClick.AddListener(delegate { onBackToMainMenuButtonClick(); });
        newGameButton.onClick.AddListener(delegate { onNewGameButtonClick(); });
        highscoresButton.onClick.AddListener(delegate { onHighscoresButtonClick(); });
        homepageButton.onClick.AddListener(delegate { onHomepageButtonClick(); });
        hsBackToMainMenuButton.onClick.AddListener(delegate { onHsBackToMainMenuButtonClick(); });
        endBackToMainMenuButton.onClick.AddListener(delegate { onEndBackToMainMenuButtonClick(); });
        endNewGameButton.onClick.AddListener(delegate { onEndNewGameButtonClick(); });
        localHighscoresButton.onClick.AddListener(delegate { onLocalHighscoresButtonClick(); });
        onlineHighscoresButton.onClick.AddListener(delegate { onOnlineHighscoresButtonClick(); });

        Highscore.loadLocalHighscores();
        Highscore.loadOnlineHighscores();
    }

    public void onNewGameButtonClick()
    {
        GUI.setState(GUIState.GAME_NEW);
    }

    public void onHighscoresButtonClick()
    {
        GUI.setState(GUIState.HIGHSCORE);
    }

    public void onHomepageButtonClick()
    {
        Application.OpenURL("https://arbowling.klauswagner.com");
    }

    public void onHsBackToMainMenuButtonClick()
    {
        GUI.setState(GUIState.MENU);
    }

    public void onNameInputChanged()
    {
        if (nameInput.text.Length > 0)
        {
            startGameButton.interactable = true;
        }
        else
        {
            startGameButton.interactable = false;
        }
    }

    public void onStartGameButtonClicked()
    {
        GUI.name = nameInput.text;
        newGameViewPanel.SetActive(false);
        gameViewPanel.SetActive(true);

        GUI.frame = 1;
        GUI.roll = 1;
        GUI.score = 0;
        GUI.remainingRollBonus = false;
        GUI.rollBonus = 1;
        GUI.framePins = 0;

        GUI.setState(GUIState.GAME);

        PinController.state = PinControllerState.HARDRESET;
        Ball.state = BallState.RESET;
    }

    public void onGameMenuButtonClicked()
    {
        GUI.setState(GUIState.GAME_MENU);
    }

    public void onGameMenuContinueButtonClicked()
    {
        GUI.setState(GUIState.GAME);
    }

    public void onBackToMainMenuButtonClick()
    {
        GUI.setState(GUIState.MENU);
    }

    public void onEndBackToMainMenuButtonClick()
    {
        GUI.setState(GUIState.MENU);
    }

    public void onEndNewGameButtonClick()
    {
        GUI.setState(GUIState.GAME_NEW);
    }

    public void onLocalHighscoresButtonClick()
    {
        GUI.highscoreType = HighscoreType.LOCAL;
        GUI.needLoadHighscores = true;

    }

    public void onOnlineHighscoresButtonClick()
    {
        GUI.highscoreType = HighscoreType.ONLINE;
        GUI.needLoadHighscores = true;
    }

    // Update is called once per frame
    void Update ()
    {
        if (GUI.state == GUIState.MENU)
        {
            renderMenu();
        }
        else if (GUI.state == GUIState.HIGHSCORE)
        {
            renderHighscores();
        }
        else if (GUI.state == GUIState.GAME)
        {
            renderGame();
        }
        else if (GUI.state == GUIState.GAME_NEW)
        {
            renderGameNew();
        }
        else if (GUI.state == GUIState.GAME_MENU)
        {
            renderGameMenu();
        }
        else if (GUI.state == GUIState.GAME_END)
        {
            renderGameEnd();
        }
    }

    void renderMenu()
    {

    }

    void renderHighscores()
    {
        if (GUI.needLoadHighscores)
        {
            switch (GUI.highscoreType)
            {
                case HighscoreType.LOCAL:
                    highscores = Highscore.getLocalHighscores();

                    localHighscoresButtonText.color = GUI.black;
                    onlineHighscoresButtonText.color = GUI.inactiveColor;

                    break;
                case HighscoreType.ONLINE:
                    highscores = Highscore.getOnlineHighscores();

                    onlineHighscoresButtonText.color = GUI.black;
                    localHighscoresButtonText.color = GUI.inactiveColor;

                    break;
            }

            highscoreContent.DetachChildren();

            foreach (KeyValuePair<float, string> kvp in highscores)
            {
                GameObject _highscoreEntry = (GameObject)Instantiate(highscoreEntry);

                _highscoreEntry.transform.SetParent(highscoreContent);

                Text[] texts = _highscoreEntry.GetComponentsInChildren<Text>();
                texts[0].text = kvp.Value;
                texts[1].text = kvp.Key.ToString("0.00");
            }

            GUI.needLoadHighscores = false;
        }
    }

    void renderGame()
    {
        errorText.enabled = false;
        text.enabled = false;

        if (GUI.errorMessage != null)
        {
            errorText.text = GUI.errorMessage;
            errorText.enabled = true;
            gameViewMainPanel.GetComponent<Image>().color = GUI.dimColor;
        }
        else if (GUI.message != null)
        {
            text.text = GUI.message;
            text.enabled = true;
            gameViewMainPanel.GetComponent<Image>().color = GUI.dimColor;
        }
        else
        {
            gameViewMainPanel.GetComponent<Image>().color = GUI.transparentColor;
        }

        if (GUI.appliedBonus > 0)
        {
            bonusText.text = GUI.appliedBonus.ToString("0.00");
        }
        else if (GUI.distanceBonus > 0)
        {
            float bonus = GUI.distanceBonus * GUI.rollBonus;
            bonusText.text = bonus.ToString("0.00");
        }
        else
        {
            bonusText.text = "-";
        }

        scoreText.text = GUI.score.ToString("0.00");
        frameText.text = GUI.frame.ToString() + "/" + GUI.MAX_FRAMES.ToString();
        rollText.text = GUI.roll.ToString() + "/" + GUI.MAX_ROLLS.ToString();

        if (GUI.frame == GUI.MAX_FRAMES && GUI.roll == (GUI.MAX_ROLLS + 1))
        {
            rollText.text = GUI.roll.ToString() + "/" + (GUI.MAX_ROLLS + 1).ToString();
        }

        GUI.distanceBonus = 0f;
        GUI.errorMessage = null;
    }

    void renderGameNew()
    {
        
    }

    void renderGameMenu()
    {

    }

    void renderGameEnd()
    {
        endScoreText.text = GUI.score.ToString("0.00");
    }
}