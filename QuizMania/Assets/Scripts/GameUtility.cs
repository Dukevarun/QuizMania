using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class GameUtility
{
    public const float ResolutionDelayTime = 1;
    public const string SavePrefKey = "Game_Highscore_Value";

    public const string xmlFileName = "QuestionsData.xml";
    public static string XMLFilePath
    {
        get
        {
            return Application.dataPath + "/" + xmlFileName;
        }
    }
}

[System.Serializable()]
public class Data
{
    public Question[] questions = new Question[0];

    public Data() { }

    public static void Write(Data data)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(Data));
        using (Stream stream = new FileStream(GameUtility.XMLFilePath, FileMode.Create))
        {
            serializer.Serialize(stream, data);
        }
    }

    public static Data Fetch()
    {
        return Fetch(out bool result);
    }

    public static Data Fetch(out bool result)
    {
        if (!File.Exists(GameUtility.XMLFilePath))
        {
            result = false;
            return new Data();
        }
        XmlSerializer deserializer = new XmlSerializer(typeof(Data));
        using (Stream stream = new FileStream(GameUtility.XMLFilePath, FileMode.Open))
        {
            var data = (Data)deserializer.Deserialize(stream);
            result = true;
            return data;
        }
    }
}