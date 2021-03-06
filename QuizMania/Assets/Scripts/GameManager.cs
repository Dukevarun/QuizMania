using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private Data data = new Data();
    
    [SerializeField] GameEvents events = null;

    [SerializeField] Animator timerAnimator = null;
    [SerializeField] TextMeshProUGUI timerText = null;
    [SerializeField] Color timerHalfWayOutColor = Color.yellow;
    [SerializeField] Color timerAlmostOutColor = Color.red;

    private List<AnswerData> pickedAnsweres = new List<AnswerData>();
    private List<int> finishedQuestions = new List<int>();
    private int currentQuestion = 0;

    private IEnumerator iEWaitTillNextRound = null;
    private IEnumerator iEStartTimer;
    private Color timerDefaultColor;
    private int timerStateParameterHash = 0;

    private bool IsFinished
    {
        get
        {
            return (finishedQuestions.Count < data.questions.Length) ? false : true;
        }
    }

    private void Awake()
    {
        events.currentFinalScore = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        events.startupHighScore = PlayerPrefs.GetInt(GameUtility.SavePrefKey);
        timerDefaultColor = timerText.color;
        LoadData();

        timerStateParameterHash = Animator.StringToHash("TimerState");

        var seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        UnityEngine.Random.InitState(seed);

        Display();
    }

    private void OnEnable()
    {
        events.UpdateQuestionAnswer += UpdateAnswers;
    }

    private void OnDisable()
    {
        events.UpdateQuestionAnswer -= UpdateAnswers;
    }

    public void EraseAnswers()
    {
        pickedAnsweres = new List<AnswerData>();
    }

    void Display()
    {
        EraseAnswers();
        var question = GetRandomQuestion();

        if (events.UpdateQuestionUI != null)
        {
            events.UpdateQuestionUI(question);
        }
        else
        {
            Debug.LogWarning("OOPS! Something went wrong while trying to display new Question UI Data. GameEvents.UpdateQuestionUI is null. Issue occured in GameManager.Display() method.");
        }

        if (question.useTimer)
        {
            UpdateTimer(question.useTimer);
        }
    }

    Question GetRandomQuestion()
    {
        var randomIndex = GetRandomQuestionIndex();
        currentQuestion = randomIndex;
        return data.questions[currentQuestion];
    }

    int GetRandomQuestionIndex()
    {
        var random = 0;
        if (finishedQuestions.Count < data.questions.Length)
        {
            do
            {
                random = UnityEngine.Random.Range(0, data.questions.Length);
            } while (finishedQuestions.Contains(random) || random == currentQuestion);
        }
        return random;
    }

    void LoadData()
    {
        data = Data.Fetch();
    }

    public void UpdateAnswers(AnswerData newAnswer)
    {
        if (data.questions[currentQuestion].answerType == AnswerType.Single)
        {
            foreach (var answer in pickedAnsweres)
            {
                if (answer != newAnswer)
                {
                    answer.Reset();
                }
            }
            pickedAnsweres.Clear();
            pickedAnsweres.Add(newAnswer);
        }
        else
        {
            bool alreadyPicked = pickedAnsweres.Exists(x => x == newAnswer);
            if (alreadyPicked)
            {
                pickedAnsweres.Remove(newAnswer);
            }
            else
            {
                pickedAnsweres.Add(newAnswer);
            }
        }
    }

    public void Accept()
    {
        UpdateTimer(false);
        bool isCorrect = CheckAnswers();
        finishedQuestions.Add(currentQuestion);

        UpdateScore((isCorrect) ? data.questions[currentQuestion].addScore : -data.questions[currentQuestion].addScore);

        if (IsFinished)
        {
            SetHighScore();
        }

        var type = (IsFinished) ? UIManager.ResolutionScreenType.Finish : (isCorrect) ? UIManager.ResolutionScreenType.Correct : UIManager.ResolutionScreenType.Incorrect;

        if (events.DisplayResolutionScreen != null)
        {
            events.DisplayResolutionScreen(type, data.questions[currentQuestion].addScore);
        }

        AudioManager.instance.PlaySound((isCorrect) ? "CorrectSFX" : "IncorrectSFX");

        if (type != UIManager.ResolutionScreenType.Finish)
        {
            if (iEWaitTillNextRound != null)
            {
                StopCoroutine(iEWaitTillNextRound);
            }
            iEWaitTillNextRound = WaitTillNextRound();
            StartCoroutine(iEWaitTillNextRound);
        }        
    }

    private bool CheckAnswers()
    {
        if (!CompareAnswers())
        {
            return false;
        }
        return true;
    }

    bool CompareAnswers()
    {
        if (pickedAnsweres.Count > 0)
        {
            List<int> correctAnswers = data.questions[currentQuestion].GetCorrectAnswers();
            List<int> pickAnswers = pickedAnsweres.Select(x => x.AnswerIndex).ToList();

            var first = correctAnswers.Except(pickAnswers).ToList();
            var second = pickAnswers.Except(correctAnswers).ToList();

            return (!first.Any() && !second.Any());
        }
        return false;
    }

    private void UpdateScore(int score)
    {
        events.currentFinalScore += score;
        if (events.currentFinalScore < 0)
        {
            events.currentFinalScore = 0;
        }

        if (events.ScoreUpdated != null)
        {
            events.ScoreUpdated();
        }
    }

    IEnumerator WaitTillNextRound()
    {
        yield return new WaitForSeconds(GameUtility.ResolutionDelayTime);
        Display();
    }

    void UpdateTimer(bool state)
    {
        switch (state)
        {
            case true:
                iEStartTimer = StartTimer();
                StartCoroutine(iEStartTimer);
                timerAnimator.SetInteger(timerStateParameterHash, 2);
                break;
            case false:
                if (iEStartTimer != null)
                {
                    StopCoroutine(iEStartTimer);
                }
                timerAnimator.SetInteger(timerStateParameterHash, 1);
                break;
            default:
        }
    }

    IEnumerator StartTimer()
    {
        var totalTime = data.questions[currentQuestion].timer;
        var timeLeft = totalTime;

        timerText.color = timerDefaultColor;

        while (timeLeft > 0)
        {
            timeLeft--;
            AudioManager.instance.PlaySound("CountdownSFX");

            if (timeLeft < totalTime / 2 && timeLeft > totalTime / 4)
            {
                timerText.color = timerHalfWayOutColor;
            }
            if (timeLeft < totalTime / 4)
            {
                timerText.color = timerAlmostOutColor;
            }
            timerText.text = timeLeft.ToString();
            yield return new WaitForSeconds(1.0f);
        }
        Accept();
    }


    private void SetHighScore()
    {
        var highscore = PlayerPrefs.GetInt(GameUtility.SavePrefKey);
        if (highscore < events.currentFinalScore)
        {
            PlayerPrefs.SetInt(GameUtility.SavePrefKey, events.currentFinalScore);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
