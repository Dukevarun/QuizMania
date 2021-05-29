using System.Collections;
using System.Collections.Generic;
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

    // Start is called before the first frame update
    void Start()
    {
        LoadQuestions();
        foreach (var question in Questions)
        {
            Debug.Log(question.Info);
        }
        //Display();
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
