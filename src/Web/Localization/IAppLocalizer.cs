namespace Web.Localization
{
    public interface IAppLocalizer
    {
        event Action? CultureChanged;

        string Culture { get; }

        string this[string key] { get; }

        void SetCulture(string culture);

        string Enum<TEnum>(TEnum value)
            where TEnum : struct, Enum;
    }
}
