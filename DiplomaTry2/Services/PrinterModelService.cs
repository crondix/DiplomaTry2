using System.ComponentModel.DataAnnotations;
using DiplomaModels;
using DiplomaTry2.Data;
using Microsoft.EntityFrameworkCore;

namespace DiplomaTry2.Services;

public sealed class PrinterModelDraft : IValidatableObject
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Укажите название модели.")]
    public string Name { get; set; } = "";
    [Range(1, short.MaxValue, ErrorMessage = "Количество копий должно быть от 1 до 32767.")]
    public short? MaxCopyPerPrint { get; set; }
    public bool? IsColor { get; set; }
    public bool? IsDuplexing { get; set; }
    [StringLength(80, ErrorMessage = "Тип устройства: не более 80 символов.")] public string? DeviceType { get; set; }
    [StringLength(200, ErrorMessage = "Функции устройства: не более 200 символов.")] public string? DeviceFunctions { get; set; }
    [StringLength(80, ErrorMessage = "Технология печати: не более 80 символов.")] public string? PrintTechnology { get; set; }
    [StringLength(40, ErrorMessage = "Формат бумаги: не более 40 символов.")] public string? MaximumPaperFormat { get; set; }
    [StringLength(100, ErrorMessage = "Разрешение печати: не более 100 символов.")] public string? MonochromeResolution { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Месячный объём должен быть положительным целым числом.")] public int? RecommendedMonthlyVolume { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Месячный объём должен быть положительным целым числом.")] public int? MaximumMonthlyVolume { get; set; }
    [StringLength(200, ErrorMessage = "Интерфейсы: не более 200 символов.")] public string? Interfaces { get; set; }
    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RecommendedMonthlyVolume.HasValue && MaximumMonthlyVolume.HasValue && RecommendedMonthlyVolume > MaximumMonthlyVolume)
            yield return new ValidationResult("Рекомендуемый месячный объём не может превышать максимальный.", [nameof(RecommendedMonthlyVolume)]);
    }
}

public sealed class PrinterModelService(IDbContextFactory<ApplicationDbContext> factory,
    ApplicationSettingsService? settings = null)
{
    public static string NameKey(string name) => name.Trim().ToUpperInvariant().Replace(" ", "");
    public PrinterModelService Capture(IDbContextFactory<ApplicationDbContext>? capturedFactory = null) =>
        new(capturedFactory ?? (factory is RuntimeDatabase runtime ? runtime.Capture() : factory), settings);

    public async Task<List<PrinterModel>> ListAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        // The list does not need potentially large image blobs; load them only for a card.
        return await db.PrinterModels.AsNoTracking().OrderBy(m => m.ModelName).Select(m => new PrinterModel
        {
            Id = m.Id, ModelName = m.ModelName, IsColor = m.IsColor, IsDuplexing = m.IsDuplexing, MaxCopyPerPrint = m.MaxCopyPerPrint,
            DeviceType = m.DeviceType, DeviceFunctions = m.DeviceFunctions, PrintTechnology = m.PrintTechnology,
            MaximumPaperFormat = m.MaximumPaperFormat, MonochromeResolution = m.MonochromeResolution,
            RecommendedMonthlyVolume = m.RecommendedMonthlyVolume, MaximumMonthlyVolume = m.MaximumMonthlyVolume,
            Interfaces = m.Interfaces, ImageContentType = m.ImageContentType
        }).ToListAsync();
    }

    public async Task<PrinterModel> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.PrinterModels.AsNoTracking().Include(m => m.PaperSizes).SingleOrDefaultAsync(m => m.Id == id)
            ?? throw new InvalidOperationException("Модель уже удалена. Обновите список.");
    }

    public async Task SaveAsync(PrinterModelDraft draft)
    {
        if (settings is not null) await settings.RequireAdminAsync();
        Validator.ValidateObject(draft, new ValidationContext(draft), true);
        var imageType = draft.ImageData is null ? null : PrinterModelImage.ContentType(draft.ImageData);
        if (string.IsNullOrWhiteSpace(draft.Name)) throw new ValidationException("Укажите название модели.");
        var key = NameKey(draft.Name);
        await using var db = await factory.CreateDbContextAsync();
        if (await db.PrinterModels.AnyAsync(m => m.NormalizedModelName == key && m.Id != draft.Id))
            throw new ValidationException("Модель с таким названием уже существует.");
        var model = draft.Id == 0 ? new PrinterModel() : await db.PrinterModels.FindAsync(draft.Id)
            ?? throw new InvalidOperationException("Модель уже удалена. Обновите список.");
        model.ModelName = draft.Name.Trim();
        model.IsColor = draft.IsColor;
        model.IsDuplexing = draft.IsDuplexing;
        model.MaxCopyPerPrint = draft.MaxCopyPerPrint;
        model.DeviceType = OptionalText(draft.DeviceType);
        model.DeviceFunctions = OptionalText(draft.DeviceFunctions);
        model.PrintTechnology = OptionalText(draft.PrintTechnology);
        model.MaximumPaperFormat = OptionalText(draft.MaximumPaperFormat);
        model.MonochromeResolution = OptionalText(draft.MonochromeResolution);
        model.RecommendedMonthlyVolume = draft.RecommendedMonthlyVolume;
        model.MaximumMonthlyVolume = draft.MaximumMonthlyVolume;
        model.Interfaces = OptionalText(draft.Interfaces);
        model.ImageData = draft.ImageData;
        model.ImageContentType = imageType;
        if (draft.Id == 0) db.PrinterModels.Add(model);
        await db.SaveChangesAsync();
    }

    public async Task<int> AddSelectedAsync(IEnumerable<string> names)
    {
        if (settings is not null) await settings.RequireAdminAsync();
        var selected = names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).DistinctBy(NameKey).ToArray();
        await using var db = await factory.CreateDbContextAsync();
        var existing = (await db.PrinterModels.Select(m => m.NormalizedModelName).ToListAsync()).ToHashSet(StringComparer.Ordinal);
        var additions = selected.Where(n => !existing.Contains(NameKey(n))).Select(n => new PrinterModel { ModelName = n }).ToArray();
        db.PrinterModels.AddRange(additions);
        await db.SaveChangesAsync();
        return additions.Length;
    }

    public async Task<int> CountPrintersAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.NetworkPrinters.CountAsync(p => p.PrinterModelId == id);
    }

    public async Task DeleteAsync(int id)
    {
        if (settings is not null) await settings.RequireAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var model = await db.PrinterModels.Include(m => m.PaperSizes).SingleOrDefaultAsync(m => m.Id == id)
            ?? throw new InvalidOperationException("Модель уже удалена. Обновите список.");
        await db.NetworkPrinters.Where(p => p.PrinterModelId == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.PrinterModelId, (int?)null));
        // Existing paper sizes are retained; their optional relationship is cleared by EF.
        db.PrinterModels.Remove(model);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task AssignAsync(int printerId, int? modelId)
    {
        if (settings is not null) await settings.RequireAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        if (modelId.HasValue && !await db.PrinterModels.AnyAsync(m => m.Id == modelId))
            throw new InvalidOperationException("Выбранная модель уже удалена. Обновите список.");
        var printer = await db.NetworkPrinters.FindAsync(printerId)
            ?? throw new InvalidOperationException("Принтер уже удалён. Обновите список.");
        printer.PrinterModelId = modelId;
        await db.SaveChangesAsync();
    }
    private static string? OptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
