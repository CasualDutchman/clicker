using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Analytics;
using UnityEngine.Advertisements;

public class GameManager : MonoBehaviour {

    public int level = 1;

    public GameType type = GameType.AgeGender;
    public Difficulty difficulty = Difficulty.Normal;

    public bool chaosMode, colorMode;

    public Transform slider;
    public Text objectiveText, maxLevelText;
    public Image timerImage, fader;
    public Text versionText;
    public Color[] chaosColorChoice;

    AnalyticsManager analyticsManager;

    Button[] buttons = new Button[9];
    int[] buttonValue = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9};
    int[] objective;
    int nextIndex = 0;

    int[] highScores;

    float timer;
    float maxTimer = 1.5f;

    float adTimer;
    bool ableToAd = false;
    GameType wantedTypeBeforeAd = GameType.Menu;

    bool hasAgeGender;

    void Awake() {
        fader.enabled = true;

        if (!GetComponent<AnalyticsManager>()) {
            analyticsManager = gameObject.AddComponent<AnalyticsManager>();
        } else {
            analyticsManager = GetComponent<AnalyticsManager>();
        }

        if (PlayerPrefs.HasKey("UserBirth")) {
            Destroy(slider.Find("OnlyFirst").gameObject);
            hasAgeGender = true;
        } else {
            slider.localPosition = new Vector3(400, 0, 0);
        }
    }

	void Start () {
        Transform holdingButtons = GameObject.FindObjectOfType<GridLayoutGroup>().transform;
        for (int i = 0; i < 9; i++) {
            buttons[i] = holdingButtons.GetChild(i).GetComponent<Button>();
            int index = i;
            buttons[i].onClick.AddListener(() => HitButton(index));
        }
        versionText.text = "v" + Application.version;

        changeButtonColors(false);

        highScores = new int[4];

        Load();

        NewNumber(StartType.Restart);
    }

    int GetHighScore {
        get {
            return (int)difficulty;
        }
    }
	
    /// <summary>
    /// Loads from Playerprefs
    /// </summary>
    void Load() {
        for (int i = 0; i < highScores.Length; i++) {
            highScores[i] = PlayerPrefs.GetInt("HighScore" + i);
        }
        maxLevelText.text = highScores[GetHighScore].ToString();

        analyticsManager.Load();
    }

    /// <summary>
    /// Saves to Playerprefs
    /// </summary>
    void Save() {
        for (int i = 0; i < highScores.Length; i++) {
            PlayerPrefs.SetInt("HighScore" + i, highScores[i]);
        }

        analyticsManager.Save();
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Rewrites the game's level indicators
    /// </summary>
    /// <param name="newLevel"></param>
    void NewNumber(StartType newLevel) {
        nextIndex = 0;
        timer = 0;
        if (newLevel == StartType.AddOne) {
            level += 1;
            maxTimer += DificultyTime;
            if(level > highScores[GetHighScore])
                highScores[GetHighScore] = level;
            maxLevelText.text = highScores[GetHighScore].ToString();

            analyticsManager.AddClick(chaosMode);
        } 
        else if (newLevel == StartType.RemoveOne) {
            level = Mathf.Clamp(level - 1, 1, int.MaxValue);
            maxTimer = Mathf.Clamp(maxTimer - DificultyTime, 1.5f, float.MaxValue);

            analyticsManager.AddClick(chaosMode);
        } 
        else if(newLevel == StartType.Restart) {
            level = 1;
            maxTimer = 1.5f;
        }

        if (chaosMode) {
            changeButtonValue(true);
        }

        if (colorMode) {
            changeButtonColors(true);
        }

        objectiveText.text = GetRandomNumber();
    }

    float DificultyTime {
        get {
            return difficulty == Difficulty.Easy ? 0.7f : (difficulty == Difficulty.Normal ? 0.5f : (difficulty == Difficulty.Hard ? 0.4f : 0.3f));
        }
    }

    /// <summary>
    /// Sets the objective array
    /// </summary>
    /// <returns></returns>
    string GetRandomNumber() {
        string number = "";
        objective = new int[level];
        for (int i = 0; i < objective.Length; i++) {
            objective[i] = Random.Range(1, 9);
            number += objective[i];
        }
        return number;
    }

    /// <summary>
    /// change the button values for chaosmode
    /// </summary>
    /// <param name="change">true for chaos, false to reset</param>
    void changeButtonValue(bool change) {
        for (int i = 0; i < 9; i++) {
            int temp = 0;
            if (change) {
                int swapper = Random.Range(0, 9);
                temp = buttonValue[swapper];
                buttonValue[swapper] = buttonValue[i];
            }
            buttonValue[i] = change ? temp : i + 1;
        }
        for (int i = 0; i < 9; i++) {
        Text text = buttons[i].transform.GetChild(0).GetComponent<Text>();
        text.text = buttonValue[i].ToString();
        }
    }

    /// <summary>
    /// When the player hits a button
    /// </summary>
    /// <param name="i"></param>
    void HitButton(int i) {
        if (objective[nextIndex] == buttonValue[i]) {
            nextIndex++;

            string temp = "<color=#00ff00ff>";
            for (int j = 0; j < nextIndex; j++) {
                temp += objective[j];
            }
            temp += "</color>";
            for (int k = nextIndex; k < objective.Length; k++) {
                temp += objective[k];
            }
            objectiveText.text = temp;
            
            if (nextIndex >= level) {
                NewNumber(StartType.AddOne);
                return;
            }
        } else {
            NewNumber(StartType.RemoveOne);
        }
    }

	void Update () {
        adTimer += Time.deltaTime;
        if (adTimer >= 60 * 5) {
            ableToAd = true;
        }

        if (Input.GetKey(KeyCode.Escape)) {
            if (type == GameType.Menu) {
                Save();

                analyticsManager.SendAnalytics((int)difficulty);

                Application.Quit();
            }
            else if (type == GameType.Game) {
                type = GameType.ToMenu;
            }
        }

        if (type == GameType.Start) {
            timer += Time.deltaTime * 0.7f;
            fader.color = new Color(33 / 255f, 33 / 255f, 33 / 255f, 1 - timer);
            if (1 - timer <= 0f) {
                timer = 0;
                type = hasAgeGender ? GameType.Menu : GameType.AgeGender;
                Destroy(fader.gameObject);                    
            }
        }
        else if (type == GameType.Game) {
            timer += Time.deltaTime;

            timerImage.fillAmount = 1 - (timer / maxTimer);

            if (timer >= maxTimer) {
                NewNumber(StartType.RemoveOne);
            }
        }
        else if (type == GameType.AgeToMenu) {
            timer += Time.deltaTime * 500;
            slider.localPosition = Vector3.Lerp(new Vector3(400, 0, 0), Vector3.zero, timer / 400);
            if (slider.localPosition.x <= 0) {
                slider.localPosition = Vector3.zero;
                timer = 0;
                type = GameType.Menu;
                Destroy(slider.Find("OnlyFirst").gameObject);
            }
        }
        else if (type == GameType.ToGame) {
            timer += Time.deltaTime * 500;
            slider.localPosition = Vector3.Lerp(Vector3.zero, new Vector3(-400, 0, 0), timer / 400);
            if (slider.localPosition.x <= -400) {
                slider.localPosition = new Vector3(-400, 0, 0);
                timer = 0;
                type = GameType.Game;
            }
        } else if (type == GameType.ToMenu) {
            timer += Time.deltaTime * 500;
            slider.localPosition = Vector3.Lerp(new Vector3(-400, 0, 0), Vector3.zero, timer / 400);
            if (slider.localPosition.x >= 0) {
                slider.localPosition = Vector3.zero;
                timer = 0;
                type = GameType.Menu;
            }
        }
    }

    /// <summary>
    /// When the player hits start
    /// </summary>
    public void HitStart() {
        type = GameType.ToGame;
        NewNumber(StartType.Restart);
        timerImage.fillAmount = 1;
        if (!chaosMode)
            changeButtonValue(false);

        if (!colorMode)
            changeButtonColors(false);

        ShowAd(GameType.ToGame);
    }

    /// <summary>
    /// When the player hits home
    /// </summary>
    public void HitHome() {
        type = GameType.ToMenu;
        ShowAd(GameType.ToMenu);
    }

    /// <summary>
    /// Toggle chaosmode on/off
    /// </summary>
    public void toggleChoasMode() {
        chaosMode = !chaosMode;
    }

    /// <summary>
    /// Toggle colorMode on/off
    /// </summary>
    public void toggleColorMode() {
        colorMode = !colorMode;
    }

    /// <summary>
    /// Change the color of all buttons randomly
    /// </summary>
    /// <param name="color"></param>
    void changeButtonColors(bool color) {
        foreach (Button b in buttons) {
            Image im = b.GetComponent<Image>();
            im.color = color ? chaosColorChoice[Random.Range(0, chaosColorChoice.Length)] : Color.white;

            Text te = b.transform.GetChild(0).GetComponent<Text>();
            te.color = color ? Color.white - im.color + Color.black : new Color(50f / 255f, 50f / 255f, 50f / 255f, 1);
        }
    }

    /// <summary>
    /// Set difficulty to dropdown box
    /// </summary>
    /// <param name="drop"></param>
    public void ToggleDifficulty(Dropdown drop) {
        difficulty = (Difficulty)drop.value;

        maxLevelText.text = highScores[GetHighScore].ToString();
    }

    public void ClearPlayerprefs() {
        PlayerPrefs.DeleteAll();
    }

    public void GoToMenu() {
        type = GameType.AgeToMenu;
        hasAgeGender = true;
    }

    /// <summary>
    /// Show an Ad if able
    /// </summary>
    void ShowAd(GameType gtype) {
        if (ableToAd) {
            wantedTypeBeforeAd = gtype;
            ShowOptions options = new ShowOptions { resultCallback = AdDone };
            Advertisement.Show(options);
            type = GameType.Ad;
        }
    }

    /// <summary>
    /// When an ad is done
    /// </summary>
    /// <param name="result"></param>
    void AdDone(ShowResult result) {
        type = GameType.Menu;
        ableToAd = false;
        adTimer = 0;
        type = wantedTypeBeforeAd;
    }
}

public enum StartType {
    Restart, AddOne, RemoveOne
}

public enum GameType {
    Start, Menu, Game, ToGame, ToMenu, Ad, AgeGender, AgeToMenu
}

public enum Difficulty {
    Easy, Normal, Hard, ULTRA
}