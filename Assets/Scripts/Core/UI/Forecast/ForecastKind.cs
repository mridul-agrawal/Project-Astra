namespace ProjectAstra.Core.UI.Forecast
{
    // Which kind of action the forecast is previewing. Decided by the targeting flow (the
    // player's choice) and passed as data to the calculator, which picks the computation.
    public enum ForecastKind
    {
        Attack,
        StaffHeal,
        StaffOffensive,
    }
}
