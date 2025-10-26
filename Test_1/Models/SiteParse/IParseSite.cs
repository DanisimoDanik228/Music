namespace Test_1.Models.SiteParse
{
    public interface IParseSite
    {
        public const int _maxCountSong = 10;
        static abstract Task<List<InfoSong>> GetInfoSong(string inputName);
    }
}
