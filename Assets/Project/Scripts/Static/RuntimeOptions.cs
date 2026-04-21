public static class RuntimeOptions
{
    public static float AudioRuntimeSFX = 0.5f;
    public static float AudioRuntimeMusic = 0.5f;
    public static float AudioRuntimeOverall = 1f;

    public static float AudioRuntimeEffectiveSFX
    {
        get
        {
            return AudioRuntimeSFX * AudioRuntimeOverall;
        }
    }

    public static float AudioRuntimeEffectiveMusic
    {
        get
        {
            return AudioRuntimeMusic * AudioRuntimeOverall;
        }
    }

    public static CharacterType ConfirmedCharacter = CharacterType.CharacterCyan;

    public static CameraViewType ConfirmedCameraView = CameraViewType.ThirdPerson;

    public static bool MultiplayerMode = false;

    public static bool InputBlocked = false;

    public static bool UseGamepad = true;
}