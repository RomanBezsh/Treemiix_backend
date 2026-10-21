using System.Security.Cryptography;
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
            .SumAsync(v => (int?)v.Value) ?? 0;

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
        var buffer = new char[16];

        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];

        return new string(buffer);
    }

    public static async Task<OrderDiscountResult?> ApplyPromoCodeAsync(
        this AppDbContext context,
        Guid? promoCodeId,
        decimal totalAmount,
        Guid? userId = null)
    {
        if (!promoCodeId.HasValue)
            return null;

        var promo = await context.PromoCodes.FindAsync(promoCodeId.Value);
        var now = DateTime.UtcNow;

        if (promo == null || !promo.IsActive)
            return null;

        if (now < promo.StartsAt || now > promo.ExpiresAt)
            return null;

        if (promo.MaxActivations > 0 && promo.UsedActivationsCount >= promo.MaxActivations)
            return null;

        if (promo.MinOrderAmount.HasValue && totalAmount < promo.MinOrderAmount.Value)
            return null;

        if (userId.HasValue && promo.LimitPerUser > 0)
        {
            var userUsages = await context.Orders
                .CountAsync(o => o.PromoCodeId == promo.Id && o.UserId == userId.Value);

            if (userUsages >= promo.LimitPerUser)
                return null;
        }

        var discountAmount = promo.DiscountType == DiscountType.Percentage
            ? totalAmount * promo.DiscountValue / 100
            : promo.DiscountValue;

        if (promo.MaxDiscountAmount.HasValue)
            discountAmount = Math.Min(discountAmount, promo.MaxDiscountAmount.Value);

        // The discount can never make the order free beyond its total.
        discountAmount = Math.Clamp(discountAmount, 0m, totalAmount);

        promo.UsedActivationsCount++;

        return new OrderDiscountResult(discountAmount);
    }
}

public record OrderDiscountResult(decimal DiscountAmount);
