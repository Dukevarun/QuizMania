using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable()]
public struct UIManagerParameters
{
    [Header("Answers Options")]
    [SerializeField] float margins;
    public float Margins
    {
        get
        {
            return margins;
        }
    }

    [Header("Resolution Screen options")]
    [SerializeField] Color correctBGColor;
    public Color CorrectBGColor
    {
        get
        {
            return correctBGColor;
        }
    }

    [SerializeField] Color incorrectBGColor;
    public Color IncorrectBGColor
    {
        get
        {
            return incorrectBGColor;
        }
    }

    [SerializeField] Color finalBGColor;
    public Color FinalBGColor
    {
        get
        {
            return finalBGColor;
        }
    }
}

[Serializable()]
public struct UIElements
{
    [SerializeField] RectTransform answersContentArea;
    public RectTransform AnswersContentArea 
    { 
        get
        {
            return answersContentArea;
        }
    }

    [SerializeField] TextMeshProUGUI questionInfoTextObject;
    public TextMeshProUGUI QuestionInfoTextObject
    {
        get
        {
            return questionInfoTextObject;
        }
    }

    [SerializeField] TextMeshProUGUI scoreText;
    public TextMeshProUGUI ScoreText
    {
        get
        {
            return scoreText;
        }
    }
    [Space]

    [SerializeField] Animator resolutionScreenAnimator;
    public Animator ResolutionScreenAnimator
    {
        get
        {
            return resolutionScreenAnimator;
        }
    }

    [SerializeField] Image resolutionBG;
    public Image ResolutionBG
    {
        get
        {
            return resolutionBG;
        }
    }

    [SerializeField] TextMeshProUGUI resolutionStateInfoText;
    public TextMeshProUGUI ResolutionStateInfoText
    {
        get
        {
            return resolutionStateInfoText;
        }
    }

    [SerializeField] TextMeshProUGUI resolutionScoreText;
    public TextMeshProUGUI ResolutionScoreText
    {
        get
        {
            return resolutionScoreText;
        }
    }

    [Space]
    [SerializeField] TextMeshProUGUI highScoreText;
    public TextMeshProUGUI HighScoreText
    {
        get
        {
            return highScoreText;
        }
    }

    [SerializeField] CanvasGroup mainCanvasGroup;
    public CanvasGroup MainCanvasGroup
    {
        get
        {
            return mainCanvasGroup;
        }
    }

    [SerializeField] RectTransform finishUIElements;
    public RectTransform FinishUIElements
    {
        get
        {
            return finishUIElements;
        }
    }
}

public class UIManager : MonoBehaviour
{
    public enum ResolutionScreenType
    {
        Correct,
        Incorrect,
        Finish
    }

    [Header("References")]
    [SerializeField] GameEvents events;

    [Header("UI Elements (Prefabs)")]
    [SerializeField] AnswerData answerPrefab;

    [SerializeField] UIElements uIElements;

    [Space]
    [SerializeField] UIManagerParameters parameters;

    List<AnswerData> currentAnswer = new List<AnswerData>();
    private int resolutionStateParameterHash = 0;

    private IEnumerator iEDisplayTimedResolution;

    // Start is called before the first frame update
    void Start()
    {
        UpdateScoreUI();
        resolutionStateParameterHash = Animator.StringToHash("ScreenState");
    }

    private void OnEnable()
    {
        events.UpdateQuestionUI += UpdateQuestionUI;
        events.DisplayResolutionScreen += DisplayResolution;
        events.ScoreUpdated += UpdateScoreUI;
    }

    private void OnDisable()
    {
        events.UpdateQuestionUI -= UpdateQuestionUI;
        events.DisplayResolutionScreen += DisplayResolution;
        events.ScoreUpdated -= UpdateScoreUI;
    }

    void UpdateQuestionUI(Question question)
    {
        uIElements.QuestionInfoTextObject.text = question.Info;
        CreateAnswers(question);
    }

    void CreateAnswers(Question question)
    {
        EraseAnswers();

        float offset = 0 - parameters.Margins;
        for (int i = 0; i < question.Answers.Length; i++)
        {
            AnswerData newAnswer = (AnswerData)Instantiate(answerPrefab, uIElements.AnswersContentArea);
            newAnswer.UpdateData(question.Answers[i].Info, i);

            newAnswer.Rect.anchoredPosition = new Vector2(0, offset);

            offset -= (newAnswer.Rect.sizeDelta.y + parameters.Margins);
            uIElements.AnswersContentArea.sizeDelta = new Vector2(uIElements.AnswersContentArea.sizeDelta.x, offset * -1);

            currentAnswer.Add(newAnswer);
        }
    }

    void EraseAnswers()
    {
        foreach (var answer in currentAnswer)
        {
            Destroy(answer.gameObject);
        }
        currentAnswer.Clear();
    }
    
    void DisplayResolution (ResolutionScreenType type, int score)
    {
        UpdateResolutionUI(type, score);
        uIElements.ResolutionScreenAnimator.SetInteger(resolutionStateParameterHash, 2);
        uIElements.MainCanvasGroup.blocksRaycasts = false;

        if (type != ResolutionScreenType.Finish)
        {
            if (iEDisplayTimedResolution != null)
            {
                StopCoroutine(iEDisplayTimedResolution);
            }
            iEDisplayTimedResolution = DisplayTimedResolution();
            StartCoroutine(iEDisplayTimedResolution);
        }
    }

    void UpdateResolutionUI (ResolutionScreenType type, int score)
    {
        var highscore = PlayerPrefs.GetInt(GameUtility.SavePrefKey);

        switch (type)
        {
            case ResolutionScreenType.Correct:
                uIElements.ResolutionBG.color = parameters.CorrectBGColor;
                uIElements.ResolutionStateInfoText.text = "CORRECT!";
                uIElements.ResolutionScoreText.text = "+" + score;
                break;
            case ResolutionScreenType.Incorrect:
                uIElements.ResolutionBG.color = parameters.IncorrectBGColor;
                uIElements.ResolutionStateInfoText.text = "INCORRECT!";
                uIElements.ResolutionScoreText.text = "-" + score;
                break;
            case ResolutionScreenType.Finish:
                uIElements.ResolutionBG.color = parameters.FinalBGColor;
                uIElements.ResolutionStateInfoText.text = "FINAL SCORE";

                StartCoroutine(CalculateScore());
                uIElements.FinishUIElements.gameObject.SetActive(true);
                uIElements.HighScoreText.gameObject.SetActive(true);
                uIElements.HighScoreText.text = ((highscore > events.startupHighScore) ? "<color = FFEB04>new </color>" : string.Empty + "HighScore: " + highscore);
                break;
            default:
                break;
        }
    }

    IEnumerator CalculateScore ()
    {
        var scoreValue = 0;
        while (scoreValue < events.currentFinalScore)
        {
            scoreValue++;
            uIElements.ResolutionScoreText.text = scoreValue.ToString();

            yield return null;
        }
    }

    IEnumerator DisplayTimedResolution ()
    {
        yield return new WaitForSeconds(GameUtility.ResolutionDelayTime);
        uIElements.ResolutionScreenAnimator.SetInteger(resolutionStateParameterHash, 0);
        uIElements.MainCanvasGroup.blocksRaycasts = true;
    }

    void UpdateScoreUI()
    {
        uIElements.ScoreText.text = "Score: " + events.currentFinalScore;
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
