namespace WordBattleGame.Utils
{
    public static class WordUtils
    {
        public static string ShuffleWord(string word)
        {
            var chars = word.ToCharArray();
            var rng = new Random();
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }
    }
}
