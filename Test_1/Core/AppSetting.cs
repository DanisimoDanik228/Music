namespace Test_1.Core
{
    public static class AppSetting
    {
        public static bool DEBUG_USE_LOCAL_DATA { get; set; }   
        public static string DEBUG_PATH_ZIP { get; set; }
        public static string DEBUG_PATH_JSON { get; set; }
        public static string PATH_STORAGE { get; set; }

        public static void Ctor(IConfiguration configuration)
        {
            DEBUG_USE_LOCAL_DATA = "true" == configuration["DEBUG_USE_LOCAL_DATA"];
            DEBUG_PATH_ZIP = configuration["DEBUG_PATH_ZIP"];
            DEBUG_PATH_JSON = configuration["DEBUG_PATH_JSON"];
            PATH_STORAGE = configuration["PATH_STORAGE"];
        }
    }
}
