using UnityEngine;
using UnityEngine.UI;
using GooglePlayGames;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {

    public int level = 1;

    public GameType type;
    public Difficulty difficulty = Difficulty.Normal;

    public bool chaosMode, colorMode;

    public Transform slider;
    public Text objectiveText, maxLevelText;
    public Image timerImage, fader;
    public Text versionText;
    public Color[] chaosColorChoice;

    Button[] buttons = new Button[9];
    int[] buttonValue = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9};
    int[] objective;
    int nextIndex = 0;

    int[] highScores;

    float timer;
    float maxTimer = 1.5f;

    void Awake() {

        PlayGamesPlatform.Activate();
        Social.localUser.Authenticate((bool success) => {
            if (success) {
                type = GameType.Start;
            }
        });

        fader.enabled = true;
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
    }

    /// <summary>
    /// Saves to Playerprefs
    /// </summary>
    void Save() {
        for (int i = 0; i < highScores.Length; i++) {
            PlayerPrefs.SetInt("HighScore" + i, highScores[i]);
        }
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
            if (level > highScores[GetHighScore]) {
                highScores[GetHighScore] = level;

                if (highScores[3] >= 15 && colorMode && chaosMode) {
                    UnlockAchievement(GPGSIds.achievement_ultra_color_chaos);
                }

                if (highScores[0] >= 20 && highScores[1] >= 20 && highScores[2] >= 20 && highScores[3] >= 20) {
                    UnlockAchievement(GPGSIds.achievement_four_of_a_kind);
                }

                for (int i = 0; i < 4; i++) {
                    if (highScores[i] >= 50) {
                        UnlockAchievement(GPGSIds.achievement_5050);
                    }
                }
            }
            maxLevelText.text = highScores[GetHighScore].ToString();
        } 
        else if (newLevel == StartType.RemoveOne) {
            level = Mathf.Clamp(level - 1, 1, int.MaxValue);
            maxTimer = Mathf.Clamp(maxTimer - DificultyTime, 1.5f, float.MaxValue);
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
        if (Input.GetKey(KeyCode.Escape)) {
            if (type == GameType.Menu) {
                Save();
                Application.Quit();
            }
            else if (type == GameType.Game) {
                HitHome();
            }
        }

        if (type == GameType.Start) {
            timer += Time.deltaTime * 0.7f;
            fader.color = new Color(33 / 255f, 33 / 255f, 33 / 255f, 1 - timer);
            if (1 - timer <= 0f) {
                timer = 0;
                type = GameType.Menu;
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
        } else if (type == GameType.ToServices) {
            timer += Time.deltaTime * 500;
            slider.localPosition = Vector3.Lerp(Vector3.zero, new Vector3(0, 600, 0), timer / 400);
            if (slider.localPosition.y >= 600) {
                slider.localPosition = new Vector3(0, 600, 0);
                timer = 0;
                type = GameType.Services;
            }
        } else if (type == GameType.SetToMenu) {
            timer += Time.deltaTime * 500;
            slider.localPosition = Vector3.Lerp(new Vector3(0, 600, 0), Vector3.zero, timer / 400);
            if (slider.localPosition.y <= 0) {
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
    }

    /// <summary>
    /// When the player hits home
    /// </summary>
    public void HitHome() {
        type = GameType.ToMenu;

        ReportScoreToLeaderboard(difficulty);
    }

    public void HitBack() {
        type = GameType.SetToMenu;
    }

    public void HitServices() {
        type = GameType.ToServices;
    }

    void ReportScoreToLeaderboard(Difficulty dif) {
        string lbname = GetLeaderboardID(dif);

        if (lbname.Length > 0) {
            Social.ReportScore(highScores[(int)dif], lbname, (bool success) => { });
            UnlockAchievement(GPGSIds.achievement_hit_the_leaderboards);
        }
    }

    string GetLeaderboardID(Difficulty dif) {
        switch (dif) {
            case Difficulty.Hard: return GPGSIds.leaderboard_hard_score;
            case Difficulty.ULTRA: return GPGSIds.leaderboard_ultra_score;
            default: break;
        }
        return "";
    }

    void UnlockAchievement(string id) {
        Social.ReportProgress(id, 100.0f, (bool success) => { });
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

    public void ShowAchievements() {
        PlayGamesPlatform.Instance.ShowAchievementsUI();
    }

    public void ShowLeaderboard() {
        PlayGamesPlatform.Instance.ShowLeaderboardUI();
    }

    public void ClearPlayerprefs() {
        PlayerPrefs.DeleteAll();
    }
}

public enum StartType {
    Restart, AddOne, RemoveOne
}

public enum GameType {
    Start, Menu, Game, ToGame, ToMenu, None, Services, ToServices, SetToMenu
}

public enum Difficulty {
    Easy, Normal, Hard, ULTRA
}