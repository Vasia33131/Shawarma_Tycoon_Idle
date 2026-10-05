namespace ShaurmaTycoon.Onboarding
{
    /// <summary>Шаги онбординга (GDD §9): тапни → купи уровень → открой вторую станцию → найми менеджера.</summary>
    public enum TutorialStep
    {
        Tap = 0,
        BuyLevel = 1,
        UnlockStation = 2,
        HireManager = 3,
        Done = 4
    }
}
