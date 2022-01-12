using System.Collections.Generic;

public static class DownloadHelper
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static string GetMainURL=>"https://row-match.s3.amazonaws.com/levels/";

    public static List<string> GetLevelNames(int levelCount)
    {
        //Assuming each letter will have 15 level
        //Otherwise could create list for letter level counts
        //If level names or the main url could change completely, would write them manually or read them from a text file
        var levelNames = new List<string>();
        for (int i = 0; i < levelCount; i++)
        {
            var n = "RM_" + Alphabet[i / 15] + (i % 15 + 1);
            levelNames.Add(n);
        }

        return levelNames;
    }
}