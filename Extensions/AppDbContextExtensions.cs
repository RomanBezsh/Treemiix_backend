using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Extensions;

public static class AppDbContextExtensions
{
    public static async Task RecalculateVotesCountAsync(this AppDbContext context, Guid questionId)
    {
        var totalVotes = await context.QuestionVotes
            .Where(v => v.QuestionId == questionId)
            .SumAsync(v => (int)v.Value);

        var question = await context.ProductQuestions.FindAsync(questionId);
        if (question != null)
            question.VotesCount = totalVotes;
    }

    public static async Task ResetOtherMainImagesAsync(this AppDbContext context, Guid productId, Guid currentId)
    {
        var others = await context.ProductGalleries
            .Where(g => g.ProductId == productId && g.Id != currentId && g.IsMain)
            .ToListAsync();

        foreach (var other in others)
            other.IsMain = false;
    }

    public static async Task<string> BuildCategoryPathAsync(this AppDbContext context, Guid? parentId, string slug)
    {
        if (!parentId.HasValue)
            return slug;

        var parent = await context.Categories.FindAsync(parentId.Value);
        return parent != null ? $"{parent.Path}/{slug}" : slug;
    }

    public static string GenerateGiftCardCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return string.Create(16, chars, (buffer, alphabet) =>
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = alphabet[Random.Shared.Next(alphabet.Length)];
        });
    }

    public static async Task<OrderDiscountResult?> ApplyPromoCodeAsync(this AppDbContext context, Guid? promoCodeId, decimal totalAmount)
    {
        if (!promoCodeId.HasValue)
            return null;

        var promo = await context.PromoCodes.FindAsync(promoCodeId.Value);
        if (promo == null || !promo.IsActive)
            return null;

        var discountAmount = promo.DiscountType == DiscountType.Percentage
            ? totalAmount * promo.DiscountValue / 100
            : promo.DiscountValue;

        promo.UsedActivationsCount++;

        return new OrderDiscountResult(discountAmount);
    }
}

public record OrderDiscountResult(decimal DiscountAmount);
