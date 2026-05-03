using Microsoft.AspNetCore.Http;

namespace Web.Localization
{
    public sealed class AppLocalizer : IAppLocalizer
    {
        public const string CookieName = "omniport.lang";

        private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Resources =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["uk"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Language"] = "Мова",
                    ["Ukrainian"] = "Українська",
                    ["English"] = "English",
                    ["Logout"] = "Вийти",
                    ["NotAuthorized"] = "У вас немає доступу до цієї сторінки.",
                    ["OmniPortTitle"] = "OmniPort: усе в одному",
                    ["BasicTemplates"] = "Базові шаблони",
                    ["TransformingTemplates"] = "Шаблони трансформації",
                    ["SignIn"] = "Увійти",
                    ["Email"] = "Email",
                    ["Password"] = "Пароль",
                    ["InvalidCredentials"] = "Неправильний email або пароль.",
                    ["Name"] = "Назва",
                    ["Fields"] = "Поля",
                    ["SourceType"] = "Тип джерела",
                    ["Actions"] = "Дії",
                    ["Edit"] = "Редагувати",
                    ["Delete"] = "Видалити",
                    ["NoTemplatesFound"] = "Шаблонів не знайдено.",
                    ["CreateTemplate"] = "Створити шаблон",
                    ["EditTemplate"] = "Редагувати шаблон",
                    ["TemplateName"] = "Назва шаблону",
                    ["AddField"] = "+ Додати поле",
                    ["NoFieldsYet"] = "Поки немає полів.",
                    ["Cancel"] = "Скасувати",
                    ["SaveTemplate"] = "Зберегти шаблон",
                    ["FieldName"] = "Назва поля",
                    ["ItemType"] = "-- тип елемента --",
                    ["Child"] = "+ Дочірнє",
                    ["ItemField"] = "+ Поле елемента",
                    ["Remove"] = "Прибрати",
                    ["LoadingTemplates"] = "Завантаження шаблонів...",
                    ["SourceTemplate"] = "Шаблон джерела",
                    ["SelectTemplate"] = "Оберіть шаблон",
                    ["FieldsFromOriginalData"] = "Поля з початкових даних",
                    ["NoFields"] = "Полів немає.",
                    ["InternalCrmSchema"] = "Внутрішня CRM-схема",
                    ["SelectSchema"] = "Оберіть схему",
                    ["MatchSourceFields"] = "Зіставте поля джерела з цільовою структурою (повна ієрархія)",
                    ["NotMapped"] = "-- Не зіставлено --",
                    ["Inside"] = "Всередині",
                    ["MapWhole"] = "Зіставити весь",
                    ["SelectTargetTemplateToMap"] = "Оберіть цільовий шаблон для зіставлення.",
                    ["SaveMapping"] = "Зберегти мапінг",
                    ["SavedJoinTemplates"] = "Збережені шаблони об'єднання",
                    ["TargetTemplate"] = "Цільовий шаблон",
                    ["NoSavedJoinTemplates"] = "Збережених шаблонів об'єднання не знайдено.",
                    ["Template"] = "Шаблон",
                    ["ImportMode"] = "Режим імпорту:",
                    ["Upload"] = "Завантаження",
                    ["Url"] = "URL",
                    ["UploadFile"] = "Завантажити файл",
                    ["Selected"] = "Обрано:",
                    ["FileUrl"] = "URL файлу",
                    ["GenerateTemplateFromFile"] = "Згенерувати поля з файлу або URL",
                    ["Generate"] = "Згенерувати",
                    ["GeneratingFields"] = "Генеруємо поля...",
                    ["NoFieldsDetected"] = "Не вдалося знайти поля у файлі.",
                    ["SchemaInferenceFailed"] = "Не вдалося згенерувати поля. Перевірте тип джерела і файл.",
                    ["CheckIntervalMin"] = "Інтервал перевірки (хв)",
                    ["RunTransformation"] = "Запустити трансформацію",
                    ["AddToWatchlist"] = "Додати до списку спостереження",
                    ["WatchedUrls"] = "URL під спостереженням",
                    ["IntervalMin"] = "Інтервал (хв)",
                    ["StableLink"] = "Постійне посилання",
                    ["LatestResult"] = "Останній результат",
                    ["UrlConversionHistory"] = "Історія URL-конверсій",
                    ["InputUrl"] = "Вхідний URL",
                    ["ConvertedAt"] = "Конвертовано",
                    ["Output"] = "Результат",
                    ["View"] = "Переглянути",
                    ["FileConversionHistory"] = "Історія файлових конверсій",
                    ["FileName"] = "Назва файлу",
                    ["Error"] = "Помилка",
                    ["RequestId"] = "ID запиту",
                    ["DevelopmentMode"] = "Режим розробки",
                    ["UnexpectedError"] = "Під час обробки запиту сталася помилка.",
                    ["DevelopmentModeDetails"] = "Перемикання на середовище Development покаже більше деталей про помилку.",
                    ["DevelopmentModeWarning"] = "Середовище Development не слід вмикати для розгорнутих застосунків.",
                    ["Enum.FieldDataType.String"] = "Рядок",
                    ["Enum.FieldDataType.Integer"] = "Ціле число",
                    ["Enum.FieldDataType.Decimal"] = "Десяткове число",
                    ["Enum.FieldDataType.Boolean"] = "Логічний",
                    ["Enum.FieldDataType.DateTime"] = "Дата/час",
                    ["Enum.FieldDataType.Object"] = "Об'єкт",
                    ["Enum.FieldDataType.Array"] = "Масив",
                    ["Enum.SourceType.CSV"] = "CSV",
                    ["Enum.SourceType.JSON"] = "JSON",
                    ["Enum.SourceType.XML"] = "XML",
                    ["Enum.SourceType.Excel"] = "Excel"
                },
                ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Language"] = "Language",
                    ["Ukrainian"] = "Українська",
                    ["English"] = "English",
                    ["Logout"] = "Log out",
                    ["NotAuthorized"] = "You are not authorized to view this page.",
                    ["OmniPortTitle"] = "OmniPort: All-in-one",
                    ["BasicTemplates"] = "Basic Templates",
                    ["TransformingTemplates"] = "Transforming Templates",
                    ["SignIn"] = "Sign in",
                    ["Email"] = "Email",
                    ["Password"] = "Password",
                    ["InvalidCredentials"] = "Invalid credentials.",
                    ["Name"] = "Name",
                    ["Fields"] = "Fields",
                    ["SourceType"] = "Source Type",
                    ["Actions"] = "Actions",
                    ["Edit"] = "Edit",
                    ["Delete"] = "Delete",
                    ["NoTemplatesFound"] = "No templates found.",
                    ["CreateTemplate"] = "Create Template",
                    ["EditTemplate"] = "Edit Template",
                    ["TemplateName"] = "Template Name",
                    ["AddField"] = "+ Add Field",
                    ["NoFieldsYet"] = "No fields yet.",
                    ["Cancel"] = "Cancel",
                    ["SaveTemplate"] = "Save Template",
                    ["FieldName"] = "Field name",
                    ["ItemType"] = "-- item type --",
                    ["Child"] = "+ Child",
                    ["ItemField"] = "+ Item field",
                    ["Remove"] = "Remove",
                    ["LoadingTemplates"] = "Loading templates...",
                    ["SourceTemplate"] = "Source Template",
                    ["SelectTemplate"] = "Select Template",
                    ["FieldsFromOriginalData"] = "Fields from original data",
                    ["NoFields"] = "No fields.",
                    ["InternalCrmSchema"] = "Internal CRM Schema",
                    ["SelectSchema"] = "Select Schema",
                    ["MatchSourceFields"] = "Match source fields to target structure (full hierarchy)",
                    ["NotMapped"] = "-- Not Mapped --",
                    ["Inside"] = "Inside",
                    ["MapWhole"] = "Map to the whole",
                    ["SelectTargetTemplateToMap"] = "Select target template to map.",
                    ["SaveMapping"] = "Save Mapping",
                    ["SavedJoinTemplates"] = "Saved Join Templates",
                    ["TargetTemplate"] = "Target Template",
                    ["NoSavedJoinTemplates"] = "No saved join templates found.",
                    ["Template"] = "Template",
                    ["ImportMode"] = "Import mode:",
                    ["Upload"] = "Upload",
                    ["Url"] = "URL",
                    ["UploadFile"] = "Upload File",
                    ["Selected"] = "Selected:",
                    ["FileUrl"] = "File URL",
                    ["GenerateTemplateFromFile"] = "Generate fields from file or URL",
                    ["Generate"] = "Generate",
                    ["GeneratingFields"] = "Generating fields...",
                    ["NoFieldsDetected"] = "No fields were detected in the file.",
                    ["SchemaInferenceFailed"] = "Could not generate fields. Check the source type and file.",
                    ["CheckIntervalMin"] = "Check Interval (min)",
                    ["RunTransformation"] = "Run Transformation",
                    ["AddToWatchlist"] = "Add to Watchlist",
                    ["WatchedUrls"] = "Watched URLs",
                    ["IntervalMin"] = "Interval (min)",
                    ["StableLink"] = "Stable link",
                    ["LatestResult"] = "Latest result",
                    ["UrlConversionHistory"] = "URL Conversion History",
                    ["InputUrl"] = "Input URL",
                    ["ConvertedAt"] = "Converted At",
                    ["Output"] = "Output",
                    ["View"] = "View",
                    ["FileConversionHistory"] = "File Conversion History",
                    ["FileName"] = "File Name",
                    ["Error"] = "Error",
                    ["RequestId"] = "Request ID",
                    ["DevelopmentMode"] = "Development Mode",
                    ["UnexpectedError"] = "An error occurred while processing your request.",
                    ["DevelopmentModeDetails"] = "Swapping to Development environment will display more detailed information about the error that occurred.",
                    ["DevelopmentModeWarning"] = "The Development environment shouldn't be enabled for deployed applications.",
                    ["Enum.FieldDataType.String"] = "String",
                    ["Enum.FieldDataType.Integer"] = "Integer",
                    ["Enum.FieldDataType.Decimal"] = "Decimal",
                    ["Enum.FieldDataType.Boolean"] = "Boolean",
                    ["Enum.FieldDataType.DateTime"] = "Date/time",
                    ["Enum.FieldDataType.Object"] = "Object",
                    ["Enum.FieldDataType.Array"] = "Array",
                    ["Enum.SourceType.CSV"] = "CSV",
                    ["Enum.SourceType.JSON"] = "JSON",
                    ["Enum.SourceType.XML"] = "XML",
                    ["Enum.SourceType.Excel"] = "Excel"
                }
            };

        public AppLocalizer(IHttpContextAccessor httpContextAccessor)
        {
            var requestedCulture = httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
            Culture = NormalizeCulture(requestedCulture);
        }

        public event Action? CultureChanged;

        public string Culture { get; private set; }

        public string this[string key]
        {
            get
            {
                var cultureResources = Resources[Culture];

                if (cultureResources.TryGetValue(key, out var value))
                {
                    return value;
                }

                return Resources["uk"].TryGetValue(key, out var fallback) ? fallback : key;
            }
        }

        public string Enum<TEnum>(TEnum value)
            where TEnum : struct, Enum
        {
            return this[$"Enum.{typeof(TEnum).Name}.{value}"];
        }

        public void SetCulture(string culture)
        {
            var normalizedCulture = NormalizeCulture(culture);

            if (string.Equals(Culture, normalizedCulture, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Culture = normalizedCulture;
            CultureChanged?.Invoke();
        }

        public static string NormalizeCulture(string? culture)
        {
            return string.Equals(culture, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "uk";
        }
    }
}
