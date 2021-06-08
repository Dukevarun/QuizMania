using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnswerType
{
    Multi,
    Single
}

[Serializable]
public class Answer
{
    public string info = string.Empty;
    public bool isCorrect = false;

    public Answer() { }
}

[Serializable]
public class Question
{
    public String info = null;
    public Answer[] answers = null;
    public Boolean useTimer = false;
    public AnswerType answerType = AnswerType.Single;
    public Int32 timer = 0;
    public Int32 addScore = 0;

    public Question() { }

    public List<int> GetCorrectAnswers()
    {
        List<int> CorrectAnswers = new List<int>();
        for (int i = 0; i < answers.Length; i++)
        {
            if (answers[i].isCorrect)
            {
                CorrectAnswers.Add(i);
            }
        }
        return CorrectAnswers;
    }
}