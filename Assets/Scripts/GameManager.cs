using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Analytics;
using UnityEngine.Advertisements;

public class GameManager : MonoBehaviour {

    public int level = 1;

    public GameType type;
    public Difficulty difficulty = Difficulty.Normal;

    public bool chaosMode;

    public Transform slider, chaosBG;
    public Text objectiveText, maxLevelText;
    public Image timerImage, fader;
    public Text versionText;
    public Color[] choasColorChoice;

    Button[] buttons = new Button[9];
    int[] buttonValue = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9};
    int[] objective;
    int nextIndex = 0;

    int maxNumbersAchieved;

    float timer;
    float maxTimer = 1.5f;

    //analytics ----
    int anaHitPlay;
    int anaPlayed;
    int anaPlayedChaos;

    float adTimer;
    bool ableToAd = false;
    GameType wantedTypeBeforeAd = GameType.Menu;

    void Awake() {
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

        changeBackground(false);

        Load();

        NewNumber(StartType.Restart);
    }
	
    /// <summary>
    /// Loads from Playerprefs
    /// </summary>
    void Load() {
        maxNumbersAchieved = PlayerPrefs.GetInt("HighScore");
        maxLevelText.text = maxNumbersAchieved.ToString();
    }

    /// <summary>
    /// Saves to Playerprefs
    /// </summary>
    void Save() {
        PlayerPrefs.SetInt("HighScore", maxNumbersAchieved);
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
            if(level > maxNumbersAchieved)
                maxNumbersAchieved = level;
            maxLevelText.text = maxNumbersAchieved.ToString();

            if (chaosMode) {
                anaPlayedChaos++;
            } else {
                anaPlayed++;
            }
        } 
        else if (newLevel == StartType.RemoveOne) {
            level = Mathf.Clamp(level - 1, 1, int.MaxValue);
            maxTimer = Mathf.Clamp(maxTimer - DificultyTime, 1.5f, float.MaxValue);

            if (chaosMode) {
                anaPlayedChaos++;
            } else {
                anaPlayed++;
            }
        } 
        else if(newLevel == StartType.Restart) {
            level = 1;
            maxTimer = 1.5f;
        }

        if (chaosMode) {
            changeButtonValue(true);
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

        if (chaosMode) {
            for(int i = 1; i < chaosBG.childCount; i++) {
                chaosBG.GetChild(i).Rotate(Vector3.forward * Time.deltaTime * (45 * ((i + 1) * 0.2f)));
            }
        }

        if (Input.GetKey(KeyCode.Escape)) {
            if (type == GameType.Menu) {
                Save();

                Analytics.CustomEvent("App Closed", new Dictionary<string, object>
                  {
                    { "Hit Play", anaHitPlay },
                    { "Numbers changed", anaPlayed },
                    { "Numbers changed Chaos", anaPlayedChaos },
                    { "Difficulty", (int)difficulty }
                  });

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

        anaHitPlay++;

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

        changeBackground(chaosMode);
    }

    void changeBackground(bool chaos) {
        if (chaos) {
            List<Color> tempList = new List<Color>();
            foreach (Color c in choasColorChoice) {
                tempList.Add(c);
            }
            for (int i = 0; i < chaosBG.childCount; i++) {
                int temp = Random.Range(0, tempList.Count);
                chaosBG.GetChild(i).GetComponent<Image>().color = tempList[temp];
                tempList.RemoveAt(temp);
            }
        }else {
            for (int i = 0; i < chaosBG.childCount; i++) {
                chaosBG.GetChild(i).GetComponent<Image>().color = new Color(0.1294f, 0.1294f, 0.1294f, 1);
            }
        }
    }

    /// <summary>
    /// Set difficulty to dropdown box
    /// </summary>
    /// <param name="drop"></param>
    public void ToggleDifficulty(Dropdown drop) {
        difficulty = (Difficulty)drop.value;
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
    Start, Menu, Game, ToGame, ToMenu, Ad
}

public enum Difficulty {
    Easy, Normal, Hard, ULTRA
}