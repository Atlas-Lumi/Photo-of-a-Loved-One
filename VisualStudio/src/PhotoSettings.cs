namespace PhotoMod
{
    internal class PhotoModSettings : JsonModSettings
    {

        [Name("Button to look at your picture")]
        [Description("")]
        public KeyCode usePictureButton = KeyCode.T;

    }

    internal static class Settings
    {
        public static PhotoModSettings options;

        public static void OnLoad()
        {
            options = new PhotoModSettings();
            options.AddToModSettings("Photo of a Loved One");
        }
    }
}