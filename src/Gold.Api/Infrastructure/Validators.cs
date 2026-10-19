using FluentValidation;
using Gold.Application.Auth;
using Gold.Application.Catalog;
using Gold.Application.Orders;
using Gold.Application.Rates;
using Gold.Domain.Pricing;

namespace Gold.Api.Infrastructure;

public sealed class RequestOtpValidator : AbstractValidator<RequestOtpRequest>
{
    public RequestOtpValidator() => RuleFor(x => x.Phone).NotEmpty().MaximumLength(20)
        .Must(p => AuthService.IranMobile().IsMatch(AuthService.NormalizePhone(p))).WithMessage("شماره موبایل معتبر نیست.");
}

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Code).NotEmpty().Matches(@"^\d{5}$").WithMessage("کد ۵ رقمی است.");
    }
}

public sealed class SetRateValidator : AbstractValidator<SetRateRequest>
{
    public SetRateValidator()
    {
        RuleFor(x => x.SellPerGram18Rial).GreaterThan(0);
        RuleFor(x => x.BuyPerGram18Rial).GreaterThan(0).LessThanOrEqualTo(x => x.SellPerGram18Rial)
            .WithMessage("نرخ خرید نباید از نرخ فروش بیشتر باشد.");
    }
}

public sealed class SetPricingValidator : AbstractValidator<SetPricingSettingsRequest>
{
    public SetPricingValidator()
    {
        RuleFor(x => x.ProfitBp).InclusiveBetween(0, 5000);
        RuleFor(x => x.TaxBp).InclusiveBetween(0, 5000);
        RuleFor(x => x.RoundingIncrementRial).InclusiveBetween(1, 10_000_000);
        RuleFor(x => x.RoundingDirection).IsInEnum();
    }
}

public sealed class CategoryValidator : AbstractValidator<UpsertCategoryRequest>
{
    public CategoryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(80).Matches("^[a-z0-9-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}

public sealed class BranchValidator : AbstractValidator<CreateBranchRequest>
{
    public BranchValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
}

public sealed class ProductValidator : AbstractValidator<UpsertProductRequest>
{
    public ProductValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(120).Matches("^[a-z0-9-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Audience).IsInEnum();
        RuleFor(x => x.Color).IsInEnum();
        RuleFor(x => x.WageKind).IsInEnum();
        RuleFor(x => x.WageValue).InclusiveBetween(0, 10_000).When(x => x.WageKind == WageKind.PercentOfRawGold)
            .WithMessage("اجرت درصدی باید بین ۰ تا ۱۰۰٪ (۰ تا ۱۰۰۰۰ واحد پایه) باشد.");
        RuleFor(x => x.WageValue).GreaterThan(0).When(x => x.WageKind == WageKind.FixedPerGram);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}

public sealed class PieceValidator : AbstractValidator<UpsertPieceRequest>
{
    public PieceValidator()
    {
        RuleFor(x => x.Barcode).NotEmpty().MaximumLength(40);
        RuleFor(x => x.GoldGrams).GreaterThan(0).LessThan(10_000).PrecisionScale(8, 3, true)
            .WithMessage("وزن باید مثبت و حداکثر ۳ رقم اعشار باشد.");
        RuleFor(x => x.StoneGrams).GreaterThanOrEqualTo(0).PrecisionScale(8, 3, true);
        RuleFor(x => x.Karat).InclusiveBetween(1, 1000);
        RuleFor(x => x.StoneValueRial).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Size).MaximumLength(20);
    }
}

public sealed class QuoteValidator : AbstractValidator<CreateQuoteRequest>
{
    public QuoteValidator() => RuleFor(x => x.PieceIds).NotEmpty().Must(l => l.Count <= 10).WithMessage("حداکثر ۱۰ قطعه.");
}

public sealed class BuyBackValidator : AbstractValidator<BuyBackRequest>
{
    public BuyBackValidator()
    {
        RuleFor(x => x).Must(x => x.Grams is > 0 || x.Mesghal is > 0).WithMessage("وزن به گرم یا مثقال لازم است.");
        RuleFor(x => x.Karat).InclusiveBetween(1, 1000);
    }
}
