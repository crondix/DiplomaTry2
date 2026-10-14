using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.AspNetCore.Components.Forms;

namespace DiplomaTry2.Services;

public static class PrinterModelImage
{
    public const int MaxBytes = 2 * 1024 * 1024;
    public static string ContentType(byte[] data)
    {
        if (data.Length > MaxBytes) throw new ValidationException("Размер изображения не должен превышать 2 МБ.");
        if (data.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (data.AsSpan().StartsWith(new byte[] { 255, 216, 255 })) return "image/jpeg";
        if (data.Length >= 12 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) && data.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return "image/webp";
        throw new ValidationException("Выберите изображение PNG, JPEG или WebP.");
    }
    public static async Task<byte[]> ReadAsync(IBrowserFile file)
    {
        if (file.Size > MaxBytes) throw new ValidationException("Размер изображения не должен превышать 2 МБ.");
        await using var input = file.OpenReadStream(MaxBytes);
        using var output = new MemoryStream();
        await input.CopyToAsync(output);
        var data = output.ToArray();
        ContentType(data);
        return data;
    }
    public static string Url(byte[]? data, string? contentType) => data is { Length: > 0 } && contentType is "image/png" or "image/jpeg" or "image/webp"
        ? $"data:{contentType};base64,{Convert.ToBase64String(data)}" : "images/menu/printer_models.png";
}
