using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    Question[] questions = null;
    public Question[] Questions
    {
        get
        {
            return questions;
        }
    }

    [SerializeField] GameEvents events = null;

    private List<AnswerData> pickedAnsweres = new List<AnswerData>();
    private List<int> finishedQuestions = new List<int>();
    private int currentQuestion = 0;

    private IEnumerator iEWaitTillNextRound = null;

    private bool IsFinished
    {
        get
        {
            return (finishedQuestions.Count < Questions.Length) ? false : true;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadQuestions();

        events.currentFinalScore = 0;

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
    }

    Question GetRandomQuestion()
    {
        var randomIndex = GetRandomQuestionIndex();
        currentQuestion = randomIndex;
        return Questions[currentQuestion];
    }

    int GetRandomQuestionIndex ()
    {
        var random = 0;
        if (finishedQuestions.Count < Questions.Length)
        {
            do
            {
                random = UnityEngine.Random.Range(0, Questions.Length);
            } while (finishedQuestions.Contains(random) || random == currentQuestion);
        }
        return random;
    }

    void LoadQuestions()
    {
        Object[] objects = Resources.LoadAll("Questions", typeof(Question));
        questions = new Question[objects.Length];
        for (int i = 0; i < objects.Length; i++)
        {
            questions[i] = (Question)objects[i];
        }
    }


    public void UpdateAnswers (AnswerData newAnswer)
    {
        if (Questions[currentQuestion].GetAnswerType == Question.AnswerType.Single)
        {
            foreach (var answer in pickedAnsweres)
            {
                if (answer != newAnswer)
                {
                    answer.Reset();
                }
                pickedAnsweres.Clear();
                pickedAnsweres.Add(newAnswer);
            }
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
        bool isCorrect = CheckAnswers();
        finishedQuestions.Add(currentQuestion);

        UpdateScore((isCorrect) ? Questions[currentQuestion].AddScore : -Questions[currentQuestion].AddScore);

        var type = (IsFinished) ? UIManager.ResolutionScreenType.Finish : (isCorrect) ? UIManager.ResolutionScreenType.Correct : UIManager.ResolutionScreenType.Incorrect;

        if (events.DisplayResolutionScreen != null)
        {
            events.DisplayResolutionScreen(type, Questions[currentQuestion].AddScore);
        }

        if (iEWaitTillNextRound != null)
        {
            StopCoroutine(iEWaitTillNextRound);
        }
        iEWaitTillNextRound = WaitTillNextRound();
        StartCoroutine(iEWaitTillNextRound);
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
            List<int> correctAnswers = Questions[currentQuestion].GetCorrectAnswers();
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

        if (events.ScoreUpdated != null)
        {
            events.ScoreUpdated();
        }
    }

    IEnumerator WaitTillNextRound ()
    {
        yield return new WaitForSeconds(GameUtility.ResolutionDelayTime);
        Display();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
