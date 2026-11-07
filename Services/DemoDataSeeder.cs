using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CloneAmazonBack.Services;

public sealed record DemoSeedOptions(int ProductCount = 60, bool Reset = false);

public sealed record DemoSeedResult(
    int Sellers,
    int Categories,
    int Subcategories,
    int Products,
    int Images,
    int Customers,
    int Orders,
    int Reviews,
    int Questions,
    int PromoCodes,
    int GiftCards,
    DateTime SeededAt);

/// <summary>
/// Creates a realistic demo marketplace: sellers, customers, categories, products,
/// product photos stored in PostgreSQL, reviews, questions, carts, orders, promo codes
/// and gift cards. Safe to run repeatedly: every demo row has a deterministic id or
/// a recognisable demo marker, so a second run only adds what is missing.
/// </summary>
public sealed class DemoDataSeeder
{
    private const string DemoEmailDomain = "@demo.treemiix.local";
    private const string DemoSlugPrefix = "demo-";
    private const string DemoSkuPrefix = "DEMO-";
    private const string DemoPromoPrefix = "DEMO";
    private const string DemoGiftPrefix = "DEMO-GIFT-";
    private const string DemoMediaPrefix = "demo-";
    private const string DemoPassword = "Demo123!";
    private const int MaxProducts = 60;
    private const int MaxImageBytes = 500 * 1024;

    private static readonly SemaphoreSlim SeedGate = new(1, 1);

    private static readonly string[] Colors =
    [
        "Black", "Graphite", "Sand", "White", "Forest Green", "Ocean Blue",
        "Burgundy", "Silver", "Terracotta", "Midnight Blue"
    ];

    private static readonly string[] Materials =
    [
        "Aluminum", "Cotton", "Leather", "Bamboo", "Stainless Steel",
        "Recycled Plastic", "Glass", "Ceramic", "Canvas", "Wood"
    ];

    private static readonly string[] Countries =
    [
        "United States", "Germany", "United Kingdom", "France", "Canada",
        "Denmark", "Italy", "Japan", "Spain", "Netherlands"
    ];

    private static readonly string[] Dimensions =
    [
        "24 x 18 x 12 cm", "32 x 24 x 10 cm", "18 x 18 x 28 cm",
        "40 x 30 x 15 cm", "28 x 20 x 8 cm", "35 x 25 x 12 cm"
    ];

    private static readonly string[] Weights =
    [
        "320 g", "650 g", "1.2 kg", "850 g", "2.4 kg", "480 g", "1.8 kg"
    ];

    private static readonly string[] ReviewTexts =
    [
        "Exactly as described. The quality feels premium and it arrived well packed.",
        "Great value for the price. I have been using it every day for two weeks.",
        "Looks even better in person. The finish and materials are excellent.",
        "Very happy with this purchase. Shipping was fast and the box was intact.",
        "Works perfectly and feels sturdy. Would definitely buy from this seller again.",
        "A solid product overall. The size is right and the design is practical.",
        "Bought this as a gift and it was a big hit. Good quality for the money.",
        "The product matches the photos and the description is accurate.",
        "Comfortable, well made and easy to use. No complaints so far.",
        "Nice attention to detail. It has become one of my favourite purchases.",
        "Does the job well. The instructions could be clearer, but the product is good.",
        "Excellent quality and a very smooth buying experience."
    ];

    private static readonly string[] QuestionTexts =
    [
        "Does this come with a warranty?",
        "Is the colour the same as in the photos?",
        "What are the exact dimensions of this item?",
        "Is this suitable for everyday use?",
        "How long does shipping usually take?",
        "Does the package include everything shown in the picture?",
        "Is there a smaller or larger size available?",
        "Can this be used outdoors?",
        "What material is it made from?",
        "Would you recommend this as a gift?"
    ];

    private static readonly string[] AnswerTexts =
    [
        "Yes, it includes a standard manufacturer warranty. Please keep the order confirmation.",
        "The colour is very close to the listing photos. Slight differences can appear on different screens.",
        "The full dimensions are listed in the product details section.",
        "Yes, it is designed for regular everyday use.",
        "Orders are usually processed within 1-2 business days.",
        "Yes, the package contains everything shown in the main photo.",
        "We currently offer the sizes listed on this page. More options are coming soon.",
        "It can be used outdoors, although we recommend storing it indoors when not in use.",
        "The materials are listed in the product specification table.",
        "It makes a great gift. The packaging is presentable and ready to give."
    ];

    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DemoDataSeeder> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DemoSeedResult> SeedAsync(
        DemoSeedOptions options,
        CancellationToken cancellationToken = default)
    {
        await SeedGate.WaitAsync(cancellationToken);
        try
        {
            var productCount = Math.Clamp(options.ProductCount, 1, MaxProducts);

            if (options.Reset)
            {
                _logger.LogWarning("Resetting demo marketplace data");
                await ResetDemoDataAsync(cancellationToken);
                _context.ChangeTracker.Clear();
            }

            await DbSeeder.SeedRolesAsync(_context, _configuration);

            var now = DateTime.UtcNow;
            var roles = await _context.UserRoles.ToDictionaryAsync(r => r.Name, r => r, cancellationToken);
            var sellerRole = roles[Roles.Seller];
            var userRole = roles[Roles.User];
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

            var categories = await SeedCategoriesAsync(now, cancellationToken);
            var sellers = await SeedSellersAsync(now, sellerRole, passwordHash, cancellationToken);
            var customers = await SeedCustomersAsync(now, userRole, passwordHash, cancellationToken);

            var allProductSeeds = CategoryDefinitions
                .SelectMany(category => category.Products.Select((product, index) =>
                    new ProductSeed(category.Slug, category.Name, index, product)))
                .ToList();
            var productSeeds = SelectProducts(allProductSeeds, productCount);

            var mediaRequests = BuildMediaRequests(categories, sellers, customers, productSeeds);
            var media = await EnsureMediaAsync(mediaRequests, cancellationToken);
            await AssignMediaToSellersAndUsersAsync(sellers, customers, media, cancellationToken);

            var products = await SeedProductsAsync(
                productSeeds, categories, sellers, media, now, cancellationToken);

            var promoCodes = await SeedPromoCodesAsync(products, now, cancellationToken);
            await SeedReviewsAndQuestionsAsync(products, customers, sellers, now, cancellationToken);
            await SeedCartsAsync(customers, products, promoCodes, cancellationToken);
            await SeedOrdersAsync(customers, products, sellers, promoCodes, now, cancellationToken);
            await SeedGiftCardsAsync(customers, now, cancellationToken);

            var result = await BuildResultAsync(cancellationToken);
            _logger.LogInformation(
                "Demo marketplace ready: {Sellers} sellers, {Products} products, {Images} images, {Orders} orders",
                result.Sellers,
                result.Products,
                result.Images,
                result.Orders);

            return result;
        }
        finally
        {
            SeedGate.Release();
        }
    }

    private async Task<Dictionary<string, Category>> SeedCategoriesAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await _context.Categories
            .ToDictionaryAsync(category => category.Slug, category => category, cancellationToken);

        var result = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in CategoryDefinitions)
        {
            if (!existing.TryGetValue(definition.Slug, out var category))
            {
                category = new Category
                {
                    Id = CreateId($"category:{definition.Slug}"),
                    Name = definition.Name,
                    Slug = definition.Slug,
                    Path = definition.Name,
                    SortOrder = definition.SortOrder,
                    IsActive = true
                };
                _context.Categories.Add(category);
                existing[definition.Slug] = category;
            }
            else
            {
                category.SortOrder = definition.SortOrder;
                category.IsActive = true;
            }

            result[definition.Slug] = category;

            var subSortOrder = 1;
            foreach (var subcategoryName in definition.Subcategories)
            {
                var subcategorySlug = $"{DemoSlugPrefix}{definition.Slug}-{Slugify(subcategoryName)}";
                var sortOrder = 1000 + definition.SortOrder * 100 + subSortOrder;
                if (!existing.TryGetValue(subcategorySlug, out var subcategory))
                {
                    subcategory = new Category
                    {
                        Id = CreateId($"category:{subcategorySlug}"),
                        ParentId = category.Id,
                        Name = subcategoryName,
                        Slug = subcategorySlug,
                        Path = $"{category.Name} > {subcategoryName}",
                        SortOrder = sortOrder,
                        IsActive = true
                    };
                    _context.Categories.Add(subcategory);
                    existing[subcategorySlug] = subcategory;
                }
                else
                {
                    subcategory.ParentId = category.Id;
                    subcategory.Name = subcategoryName;
                    subcategory.Path = $"{category.Name} > {subcategoryName}";
                    subcategory.SortOrder = sortOrder;
                    subcategory.IsActive = true;
                }

                subSortOrder++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<List<DemoSeller>> SeedSellersAsync(
        DateTime now,
        UserRole sellerRole,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        var existingUsers = await _context.Users
            .Where(user => user.Email.EndsWith(DemoEmailDomain))
            .ToDictionaryAsync(user => user.Email, user => user, cancellationToken);

        var existingSellers = await _context.Sellers
            .Where(seller => seller.StoreSlug.StartsWith(DemoSlugPrefix))
            .ToDictionaryAsync(seller => seller.StoreSlug, seller => seller, cancellationToken);

        var result = new List<DemoSeller>(SellerDefinitions.Length);

        for (var index = 0; index < SellerDefinitions.Length; index++)
        {
            var definition = SellerDefinitions[index];
            var email = $"seller{index + 1}{DemoEmailDomain}";

            if (!existingUsers.TryGetValue(email, out var user))
            {
                user = new User
                {
                    Id = CreateId($"user:{email}"),
                    Email = email,
                    Password = passwordHash,
                    FirstName = definition.FirstName,
                    LastName = definition.LastName,
                    IsActive = true,
                    EmailConfirmed = true,
                    UserRoleId = sellerRole.Id
                };
                _context.Users.Add(user);
                existingUsers[email] = user;
            }
            else
            {
                user.FirstName = definition.FirstName;
                user.LastName = definition.LastName;
                user.IsActive = true;
                user.EmailConfirmed = true;
                user.UserRoleId = sellerRole.Id;
            }

            var storeSlug = $"{DemoSlugPrefix}{definition.StoreSlug}";
            var isActive = definition.Status == SellerStatus.Active;
            var paidUntil = definition.Status switch
            {
                SellerStatus.Active => now.AddDays(30),
                SellerStatus.Suspended => now.AddDays(-5),
                _ => (DateTime?)null
            };
            var approvedAt = definition.Status is SellerStatus.Active or SellerStatus.Suspended
                ? now.AddDays(-30)
                : (DateTime?)null;
            var rejectedAt = definition.Status == SellerStatus.Rejected
                ? now.AddDays(-10)
                : (DateTime?)null;
            var submittedAt = definition.Status == SellerStatus.PendingVerification
                ? now.AddDays(-1)
                : now.AddDays(-31);

            if (!existingSellers.TryGetValue(storeSlug, out var seller))
            {
                seller = new Seller
                {
                    Id = CreateId($"seller:{storeSlug}"),
                    UserId = user.Id,
                    StoreName = definition.StoreName,
                    StoreSlug = storeSlug,
                    Description = definition.Description,
                    BusinessType = definition.BusinessType,
                    LegalName = definition.LegalName,
                    Country = definition.Country,
                    SupportEmail = $"support{index + 1}{DemoEmailDomain}",
                    SupportPhone = $"+1-555-01{index:00}",
                    TaxNumber = $"DEMO-TAX-{index + 1:000}",
                    LegalAddress = $"{index + 1} Demo Street, Demo City",
                    BankAccount = $"DEMO-BANK-{index + 1:000}",
                    Rating = 4.2m + (index % 8) * 0.1m,
                    Status = definition.Status,
                    CommissionRate = 0.10m,
                    OnboardingStep = 4,
                    SubmittedAt = submittedAt,
                    ApprovedAt = approvedAt,
                    RejectedAt = rejectedAt,
                    RejectionReason = definition.RejectionReason,
                    SubscriptionStatus = definition.SubscriptionStatus,
                    SubscriptionPaidUntil = paidUntil,
                    CreatedAt = now.AddDays(-60 + index)
                };
                _context.Sellers.Add(seller);
                existingSellers[storeSlug] = seller;
            }
            else
            {
                seller.UserId = user.Id;
                seller.StoreName = definition.StoreName;
                seller.Description = definition.Description;
                seller.BusinessType = definition.BusinessType;
                seller.LegalName = definition.LegalName;
                seller.Country = definition.Country;
                seller.SupportEmail = $"support{index + 1}{DemoEmailDomain}";
                seller.SupportPhone = $"+1-555-01{index:00}";
                seller.TaxNumber = $"DEMO-TAX-{index + 1:000}";
                seller.LegalAddress = $"{index + 1} Demo Street, Demo City";
                seller.BankAccount = $"DEMO-BANK-{index + 1:000}";
                seller.Rating = 4.2m + (index % 8) * 0.1m;
                seller.Status = definition.Status;
                seller.CommissionRate = 0.10m;
                seller.OnboardingStep = 4;
                seller.SubmittedAt = submittedAt;
                seller.ApprovedAt = approvedAt;
                seller.RejectedAt = rejectedAt;
                seller.RejectionReason = definition.RejectionReason;
                seller.SubscriptionStatus = definition.SubscriptionStatus;
                seller.SubscriptionPaidUntil = paidUntil;
            }

            await EnsureProfileAndAddressAsync(
                user,
                index,
                "New York",
                definition.Country,
                cancellationToken);

            result.Add(new DemoSeller(seller, user, isActive));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<List<User>> SeedCustomersAsync(
        DateTime now,
        UserRole userRole,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        var existingUsers = await _context.Users
            .Where(user => user.Email.EndsWith(DemoEmailDomain))
            .ToDictionaryAsync(user => user.Email, user => user, cancellationToken);

        var result = new List<User>(CustomerDefinitions.Length);

        for (var index = 0; index < CustomerDefinitions.Length; index++)
        {
            var definition = CustomerDefinitions[index];
            var email = $"customer{index + 1}{DemoEmailDomain}";

            if (!existingUsers.TryGetValue(email, out var user))
            {
                user = new User
                {
                    Id = CreateId($"user:{email}"),
                    Email = email,
                    Password = passwordHash,
                    FirstName = definition.FirstName,
                    LastName = definition.LastName,
                    IsActive = true,
                    EmailConfirmed = true,
                    UserRoleId = userRole.Id
                };
                _context.Users.Add(user);
                existingUsers[email] = user;
            }
            else
            {
                user.FirstName = definition.FirstName;
                user.LastName = definition.LastName;
                user.IsActive = true;
                user.EmailConfirmed = true;
                user.UserRoleId = userRole.Id;
            }

            await EnsureProfileAndAddressAsync(
                user,
                index,
                definition.City,
                definition.Country,
                cancellationToken);

            result.Add(user);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task EnsureProfileAndAddressAsync(
        User user,
        int index,
        string city,
        string country,
        CancellationToken cancellationToken)
    {
        if (!await _context.UserProfiles.AnyAsync(profile => profile.UserId == user.Id, cancellationToken))
        {
            _context.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id,
                DateOfBirth = new DateTime(
                    1980 + index % 20,
                    1 + index % 12,
                    1 + index % 27,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc)
            });
        }

        var addressId = CreateId($"address:{user.Email}:0");
        if (!await _context.UserAddresses.AnyAsync(address => address.Id == addressId, cancellationToken))
        {
            _context.UserAddresses.Add(new UserAddress
            {
                Id = addressId,
                UserId = user.Id,
                Country = country,
                City = city,
                Street = $"{100 + index} Demo Avenue",
                Building = $"{index + 1}",
                Apartment = index % 3 == 0 ? $"{index + 2}A" : null,
                PostalCode = $"100{index:00}",
                IsDefault = true
            });
        }
    }

    private static List<ProductSeed> SelectProducts(List<ProductSeed> all, int count)
    {
        if (count >= all.Count)
            return all;

        var selected = new List<ProductSeed>(count);
        for (var index = 0; index < count; index++)
        {
            var sourceIndex = (int)Math.Floor(index * (double)all.Count / count);
            selected.Add(all[sourceIndex]);
        }

        return selected;
    }

    private static List<MediaRequest> BuildMediaRequests(
        Dictionary<string, Category> categories,
        List<DemoSeller> sellers,
        List<User> customers,
        List<ProductSeed> productSeeds)
    {
        var requests = new List<MediaRequest>();
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in productSeeds)
        {
            for (var offset = 0; offset < 3; offset++)
            {
                var imageIndex = (seed.IndexInCategory + offset) % 5;
                var key = $"product:{seed.CategorySlug}:{imageIndex}";
                if (!keys.Add(key))
                    continue;

                var categoryName = categories.TryGetValue(seed.CategorySlug, out var category)
                    ? category.Name
                    : seed.CategoryName;

                requests.Add(new MediaRequest(
                    key,
                    UnsplashUrl(CategoryDefinitions
                        .First(definition => definition.Slug == seed.CategorySlug)
                        .ImageIds[imageIndex]),
                    $"{categoryName} collection",
                    "Treemiix demo"));
            }
        }

        foreach (var seller in sellers)
        {
            requests.Add(new MediaRequest(
                $"seller-logo:{seller.Seller.StoreSlug}",
                null,
                seller.Seller.StoreName,
                "Verified seller"));
        }

        foreach (var user in sellers.Select(seller => seller.User).Concat(customers))
        {
            requests.Add(new MediaRequest(
                $"avatar:{user.Email}",
                null,
                $"{user.FirstName[..1]}{user.LastName[..1]}".ToUpperInvariant(),
                $"{user.FirstName} {user.LastName}"));
        }

        return requests;
    }

    private async Task<Dictionary<string, MediaAsset>> EnsureMediaAsync(
        List<MediaRequest> requests,
        CancellationToken cancellationToken)
    {
        var existing = await _context.MediaFiles
            .Where(media => media.FileName.StartsWith(DemoMediaPrefix))
            .ToDictionaryAsync(media => media.FileName, media => media, cancellationToken);

        var assets = new ConcurrentDictionary<string, MediaAsset>(StringComparer.OrdinalIgnoreCase);
        var newMedia = new ConcurrentBag<MediaFile>();
        using var gate = new SemaphoreSlim(4);

        var tasks = requests.Select(async request =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var preferredExtension = request.Url is null ? ".svg" : ".jpg";
                var preferredName = MediaFileName(request.Key, preferredExtension);
                if (existing.TryGetValue(preferredName, out var existingMedia))
                {
                    assets[request.Key] = ToAsset(existingMedia);
                    return;
                }

                byte[] bytes;
                string contentType;
                string extension;

                if (request.Url is not null)
                {
                    var downloaded = await TryDownloadAsync(request.Url, cancellationToken);
                    if (downloaded is not null)
                    {
                        bytes = downloaded.Value.Bytes;
                        contentType = downloaded.Value.ContentType;
                        extension = ExtensionForContentType(contentType);
                    }
                    else
                    {
                        bytes = BuildSvg(request.SvgTitle ?? "Treemiix", request.SvgSubtitle ?? "Demo", request.Key);
                        contentType = "image/svg+xml";
                        extension = ".svg";
                    }
                }
                else
                {
                    bytes = BuildSvg(request.SvgTitle ?? "Treemiix", request.SvgSubtitle ?? "Demo", request.Key);
                    contentType = "image/svg+xml";
                    extension = ".svg";
                }

                var fileName = MediaFileName(request.Key, extension);
                if (existing.TryGetValue(fileName, out existingMedia))
                {
                    assets[request.Key] = ToAsset(existingMedia);
                    return;
                }

                var media = new MediaFile
                {
                    Id = CreateId($"media:{fileName}"),
                    FileName = fileName,
                    ContentType = contentType,
                    Size = bytes.LongLength,
                    Bytes = bytes,
                    CreatedAt = DateTime.UtcNow
                };

                newMedia.Add(media);
                assets[request.Key] = ToAsset(media);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);

        if (!newMedia.IsEmpty)
        {
            _context.MediaFiles.AddRange(newMedia);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new Dictionary<string, MediaAsset>(assets, StringComparer.OrdinalIgnoreCase);
    }

    private async Task AssignMediaToSellersAndUsersAsync(
        List<DemoSeller> sellers,
        List<User> customers,
        Dictionary<string, MediaAsset> media,
        CancellationToken cancellationToken)
    {
        foreach (var seller in sellers)
        {
            if (media.TryGetValue($"seller-logo:{seller.Seller.StoreSlug}", out var logo))
                seller.Seller.LogoUrl = logo.Path;
        }

        var allUsers = sellers.Select(seller => seller.User).Concat(customers).ToList();
        var userIds = allUsers.Select(user => user.Id).ToList();
        var profiles = await _context.UserProfiles
            .Where(profile => userIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, profile => profile, cancellationToken);

        foreach (var user in allUsers)
        {
            if (profiles.TryGetValue(user.Id, out var profile) &&
                media.TryGetValue($"avatar:{user.Email}", out var avatar))
            {
                profile.AvatarUrl = avatar.Path;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<Product>> SeedProductsAsync(
        List<ProductSeed> productSeeds,
        Dictionary<string, Category> categories,
        List<DemoSeller> sellers,
        Dictionary<string, MediaAsset> media,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (productSeeds.Count == 0)
            return [];

        var activeSellers = sellers
            .Where(seller => seller.IsActive)
            .Select(seller => seller.Seller)
            .ToList();

        if (activeSellers.Count == 0)
            activeSellers = sellers.Select(seller => seller.Seller).ToList();

        var existingBySku = await _context.Products
            .Where(product => product.Sku != null && product.Sku.StartsWith(DemoSkuPrefix))
            .ToDictionaryAsync(product => product.Sku!, product => product, cancellationToken);

        var result = new List<Product>(productSeeds.Count);
        var globalIndex = 0;

        foreach (var seed in productSeeds)
        {
            var sku = $"{DemoSkuPrefix}{seed.CategorySlug.ToUpperInvariant()}-{seed.IndexInCategory + 1:00}";
            if (existingBySku.TryGetValue(sku, out var existingProduct))
            {
                result.Add(existingProduct);
                globalIndex++;
                continue;
            }

            var category = categories[seed.CategorySlug];
            var seller = activeSellers[globalIndex % activeSellers.Count];
            var product = new Product
            {
                Id = CreateId($"product:{sku}"),
                Name = seed.Definition.Name,
                Slug = Slugify($"{seed.Definition.Name}-{seed.CategorySlug}-{seed.IndexInCategory + 1}"),
                SellerId = seller.Id,
                CategoryId = category.Id,
                Price = seed.Definition.Price,
                OldCost = seed.Definition.OldCost,
                Rating = 0,
                Stock = 10 + globalIndex * 7 % 190,
                Description = BuildDescription(seed),
                IsActive = true,
                CreatedAt = now.AddDays(-(globalIndex % 120 + 1)),
                UpdatedAt = now,
                Status = ProductStatus.Active,
                Sku = sku,
                Asin = $"B0DEMO{globalIndex:0000}",
                ItemModelNumber = $"{seed.Definition.Brand}-{seed.IndexInCategory + 1:000}",
                Manufacturer = seed.Definition.Brand,
                CountryOfOrigin = Countries[globalIndex % Countries.Length],
                ProductDimensions = Dimensions[globalIndex % Dimensions.Length],
                ItemWeight = Weights[globalIndex % Weights.Length],
                WarrantyInfo = "2-year limited warranty",
                Features = JsonSerializer.Serialize(BuildFeatures(seed)),
                Binding = "Standard",
                ReleaseDate = now.AddDays(-(globalIndex % 300 + 30)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };

            _context.Products.Add(product);

            for (var galleryIndex = 0; galleryIndex < 3; galleryIndex++)
            {
                var imageIndex = (seed.IndexInCategory + galleryIndex) % 5;
                if (!media.TryGetValue($"product:{seed.CategorySlug}:{imageIndex}", out var image))
                    continue;

                _context.ProductGalleries.Add(new ProductGallery
                {
                    Id = CreateId($"gallery:{sku}:{galleryIndex}"),
                    ProductId = product.Id,
                    Path = image.Path,
                    SortOrder = galleryIndex,
                    IsMain = galleryIndex == 0
                });
            }

            AddAttribute(product, "Brand", seed.Definition.Brand, sku);
            AddAttribute(product, "Color", Colors[globalIndex % Colors.Length], sku);
            AddAttribute(product, "Material", Materials[globalIndex % Materials.Length], sku);
            AddAttribute(product, "Model", $"{seed.Definition.Brand}-{seed.IndexInCategory + 1:000}", sku);
            AddAttribute(product, "Warranty", "2 years", sku);

            _context.HistoryPriceProducts.Add(new HistoryPriceProduct
            {
                Id = CreateId($"price:{sku}:60"),
                ProductId = product.Id,
                Price = Math.Round(product.Price * 1.15m, 2),
                CreatedAt = now.AddDays(-60)
            });
            _context.HistoryPriceProducts.Add(new HistoryPriceProduct
            {
                Id = CreateId($"price:{sku}:30"),
                ProductId = product.Id,
                Price = Math.Round(product.Price * 1.05m, 2),
                CreatedAt = now.AddDays(-30)
            });
            _context.HistoryPriceProducts.Add(new HistoryPriceProduct
            {
                Id = CreateId($"price:{sku}:0"),
                ProductId = product.Id,
                Price = product.Price,
                CreatedAt = now
            });

            result.Add(product);
            existingBySku[sku] = product;
            globalIndex++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<List<PromoCode>> SeedPromoCodesAsync(
        List<Product> products,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await _context.PromoCodes
            .Where(promo => promo.Code.StartsWith(DemoPromoPrefix))
            .ToDictionaryAsync(promo => promo.Code, promo => promo, cancellationToken);

        var result = new List<PromoCode>(PromoDefinitions.Length);

        foreach (var definition in PromoDefinitions)
        {
            if (!existing.TryGetValue(definition.Code, out var promo))
            {
                promo = new PromoCode
                {
                    Id = CreateId($"promo:{definition.Code}"),
                    Code = definition.Code,
                    DiscountValue = definition.DiscountValue,
                    DiscountType = definition.DiscountType,
                    MinOrderAmount = definition.MinOrderAmount,
                    MaxDiscountAmount = definition.MaxDiscountAmount,
                    MaxActivations = definition.MaxActivations,
                    UsedActivationsCount = 0,
                    LimitPerUser = definition.LimitPerUser,
                    StartsAt = now.AddDays(-30),
                    ExpiresAt = now.AddDays(90),
                    IsActive = true
                };
                _context.PromoCodes.Add(promo);
                existing[definition.Code] = promo;
            }
            else
            {
                promo.DiscountValue = definition.DiscountValue;
                promo.DiscountType = definition.DiscountType;
                promo.MinOrderAmount = definition.MinOrderAmount;
                promo.MaxDiscountAmount = definition.MaxDiscountAmount;
                promo.MaxActivations = definition.MaxActivations;
                promo.LimitPerUser = definition.LimitPerUser;
                promo.StartsAt = now.AddDays(-30);
                promo.ExpiresAt = now.AddDays(90);
                promo.IsActive = true;
            }

            result.Add(promo);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var existingLinks = await _context.PromoCodeProducts
            .Select(link => link.Id)
            .ToHashSetAsync(cancellationToken);

        var promo10 = result.FirstOrDefault(promo => promo.Code == "DEMO10");
        var promoSave15 = result.FirstOrDefault(promo => promo.Code == "DEMOSAVE15");

        if (promo10 is not null)
        {
            foreach (var product in products.Take(10))
                AddPromoProductLink(promo10, product, existingLinks);
        }

        if (promoSave15 is not null)
        {
            foreach (var product in products.Where(product => product.Price >= 75m).Take(12))
                AddPromoProductLink(promoSave15, product, existingLinks);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private void AddPromoProductLink(PromoCode promo, Product product, HashSet<Guid> existingLinks)
    {
        var id = CreateId($"promo-product:{promo.Code}:{product.Id}");
        if (!existingLinks.Add(id))
            return;

        _context.PromoCodeProducts.Add(new PromoCodeProduct
        {
            Id = id,
            PromoCodeId = promo.Id,
            ProductId = product.Id
        });
    }

    private async Task SeedReviewsAndQuestionsAsync(
        List<Product> products,
        List<User> customers,
        List<DemoSeller> sellers,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (products.Count == 0 || customers.Count == 0)
            return;

        var answerUsers = sellers
            .Where(seller => seller.IsActive)
            .Select(seller => seller.User)
            .ToList();

        if (answerUsers.Count == 0)
            answerUsers = customers;

        var productIds = products.Select(product => product.Id).ToList();
        var existingReviewIds = await _context.ProductReviews
            .Where(review => productIds.Contains(review.ProductId))
            .Select(review => review.Id)
            .ToHashSetAsync(cancellationToken);
        var existingQuestionIds = await _context.ProductQuestions
            .Where(question => productIds.Contains(question.ProductId))
            .Select(question => question.Id)
            .ToHashSetAsync(cancellationToken);
        var existingAnswerIds = await _context.ProductAnswers
            .Where(answer => existingQuestionIds.Contains(answer.QuestionId))
            .Select(answer => answer.Id)
            .ToHashSetAsync(cancellationToken);
        var existingVoteIds = await _context.QuestionVotes
            .Where(vote => existingQuestionIds.Contains(vote.QuestionId))
            .Select(vote => vote.Id)
            .ToHashSetAsync(cancellationToken);

        var reviews = new List<ProductReview>();
        var questions = new List<ProductQuestion>();
        var answers = new List<ProductAnswer>();
        var votes = new List<QuestionVote>();

        for (var productIndex = 0; productIndex < products.Count; productIndex++)
        {
            var product = products[productIndex];
            var productKey = product.Sku ?? product.Id.ToString();
            var reviewCount = 3 + productIndex % 4;

            for (var reviewIndex = 0; reviewIndex < reviewCount; reviewIndex++)
            {
                var reviewId = CreateId($"review:{productKey}:{reviewIndex}");
                if (existingReviewIds.Contains(reviewId))
                    continue;

                var reviewer = customers[(productIndex + reviewIndex) % customers.Count];
                reviews.Add(new ProductReview
                {
                    Id = reviewId,
                    UserId = reviewer.Id,
                    ProductId = product.Id,
                    LikesCount = (productIndex * 3 + reviewIndex * 7) % 40,
                    Text = ReviewTexts[(productIndex + reviewIndex) % ReviewTexts.Length],
                    Rating = 3 + (productIndex + reviewIndex) % 3,
                    CreatedAt = now.AddDays(-(reviewIndex * 5 + productIndex % 20 + 1))
                });
            }

            var questionCount = 1 + productIndex % 2;
            for (var questionIndex = 0; questionIndex < questionCount; questionIndex++)
            {
                var questionId = CreateId($"question:{productKey}:{questionIndex}");
                if (existingQuestionIds.Contains(questionId))
                    continue;

                var asker = customers[(productIndex + questionIndex + 2) % customers.Count];
                var question = new ProductQuestion
                {
                    Id = questionId,
                    ProductId = product.Id,
                    UserId = asker.Id,
                    Content = QuestionTexts[(productIndex + questionIndex) % QuestionTexts.Length],
                    VotesCount = 0,
                    CreatedAt = now.AddDays(-(questionIndex * 3 + productIndex % 15 + 2)),
                    UpdatedAt = now.AddDays(-(questionIndex * 3 + productIndex % 15 + 1))
                };
                questions.Add(question);

                var answerCount = 1 + (productIndex + questionIndex) % 2;
                for (var answerIndex = 0; answerIndex < answerCount; answerIndex++)
                {
                    var answerId = CreateId($"answer:{productKey}:{questionIndex}:{answerIndex}");
                    if (existingAnswerIds.Contains(answerId))
                        continue;

                    var isOfficial = answerIndex == 0;
                    var answerUser = isOfficial
                        ? answerUsers[(productIndex + questionIndex) % answerUsers.Count]
                        : customers[(productIndex + questionIndex + answerIndex + 4) % customers.Count];

                    answers.Add(new ProductAnswer
                    {
                        Id = answerId,
                        QuestionId = questionId,
                        UserId = answerUser.Id,
                        Content = AnswerTexts[(productIndex + questionIndex + answerIndex) % AnswerTexts.Length],
                        IsOfficialAnswer = isOfficial,
                        CreatedAt = question.CreatedAt.AddHours(6 + answerIndex),
                        UpdatedAt = question.CreatedAt.AddHours(6 + answerIndex)
                    });
                }

                var voteCount = (productIndex + questionIndex) % 4;
                var voteSum = 0;
                for (var voteIndex = 0; voteIndex < voteCount; voteIndex++)
                {
                    var voteId = CreateId($"vote:{productKey}:{questionIndex}:{voteIndex}");
                    if (existingVoteIds.Contains(voteId))
                        continue;

                    var voter = customers[(productIndex + questionIndex + voteIndex + 6) % customers.Count];
                    var value = (short)(voteIndex % 3 == 0 ? -1 : 1);
                    votes.Add(new QuestionVote
                    {
                        Id = voteId,
                        QuestionId = questionId,
                        UserId = voter.Id,
                        Value = value
                    });
                    voteSum += value;
                }

                question.VotesCount = voteSum;
            }
        }

        _context.ProductReviews.AddRange(reviews);
        _context.ProductQuestions.AddRange(questions);
        _context.ProductAnswers.AddRange(answers);
        _context.QuestionVotes.AddRange(votes);
        await _context.SaveChangesAsync(cancellationToken);

        var averages = await _context.ProductReviews
            .Where(review => productIds.Contains(review.ProductId))
            .GroupBy(review => review.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Average = group.Average(review => review.Rating)
            })
            .ToListAsync(cancellationToken);

        var averageByProduct = averages.ToDictionary(item => item.ProductId, item => item.Average);
        foreach (var product in products)
        {
            if (averageByProduct.TryGetValue(product.Id, out var average))
                product.Rating = (int)Math.Round(average);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCartsAsync(
        List<User> customers,
        List<Product> products,
        List<PromoCode> promoCodes,
        CancellationToken cancellationToken)
    {
        if (customers.Count == 0 || products.Count == 0)
            return;

        var existingCartIds = await _context.Carts
            .Select(cart => cart.Id)
            .ToHashSetAsync(cancellationToken);

        var carts = new List<Cart>();
        var cartItems = new List<CartItem>();

        for (var customerIndex = 0; customerIndex < customers.Count; customerIndex++)
        {
            var customer = customers[customerIndex];
            var cartId = CreateId($"cart:{customer.Email}");
            if (existingCartIds.Contains(cartId))
                continue;

            var cart = new Cart
            {
                Id = cartId,
                UserId = customer.Id,
                PromoCodeId = customerIndex % 3 == 0 && promoCodes.Count > 0
                    ? promoCodes[customerIndex % promoCodes.Count].Id
                    : null
            };
            carts.Add(cart);

            var itemCount = 2 + customerIndex % 3;
            for (var itemIndex = 0; itemIndex < itemCount; itemIndex++)
            {
                var product = products[(customerIndex * 3 + itemIndex) % products.Count];
                cartItems.Add(new CartItem
                {
                    Id = CreateId($"cart-item:{customer.Email}:{itemIndex}"),
                    CartId = cartId,
                    ProductId = product.Id,
                    Quantity = 1 + itemIndex % 2,
                    Price = product.Price
                });
            }
        }

        _context.Carts.AddRange(carts);
        _context.CartItems.AddRange(cartItems);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedOrdersAsync(
        List<User> customers,
        List<Product> products,
        List<DemoSeller> sellers,
        List<PromoCode> promoCodes,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (customers.Count == 0 || products.Count == 0)
            return;

        var activeSellers = sellers
            .Where(seller => seller.IsActive)
            .Select(seller => seller.Seller)
            .ToList();

        if (activeSellers.Count == 0)
            return;

        var productsBySeller = products
            .GroupBy(product => product.SellerId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var productIds = products.Select(product => product.Id).ToList();
        var mainGalleries = await _context.ProductGalleries
            .Where(gallery => gallery.IsMain && productIds.Contains(gallery.ProductId))
            .OrderBy(gallery => gallery.SortOrder)
            .ToListAsync(cancellationToken);

        var mainImageByProduct = new Dictionary<Guid, string>();
        foreach (var gallery in mainGalleries)
            mainImageByProduct.TryAdd(gallery.ProductId, gallery.Path);

        var existingOrderIds = await _context.Orders
            .Select(order => order.Id)
            .ToHashSetAsync(cancellationToken);

        var orders = new List<Order>();
        var orderItems = new List<OrderItem>();
        var statuses = new[]
        {
            OrderStatus.Pending,
            OrderStatus.Processing,
            OrderStatus.Shipped,
            OrderStatus.Delivered,
            OrderStatus.Cancelled,
            OrderStatus.Refunded
        };

        for (var customerIndex = 0; customerIndex < customers.Count; customerIndex++)
        {
            var customer = customers[customerIndex];

            for (var orderIndex = 0; orderIndex < 2; orderIndex++)
            {
                var orderId = CreateId($"order:{customer.Email}:{orderIndex}");
                if (existingOrderIds.Contains(orderId))
                    continue;

                var seller = activeSellers[(customerIndex + orderIndex) % activeSellers.Count];
                if (!productsBySeller.TryGetValue(seller.Id, out var sellerProducts) ||
                    sellerProducts.Count == 0)
                {
                    continue;
                }

                var itemCount = Math.Min(1 + (customerIndex + orderIndex) % 3, sellerProducts.Count);
                var startIndex = (customerIndex + orderIndex) % sellerProducts.Count;
                var selected = Enumerable.Range(0, itemCount)
                    .Select(offset => sellerProducts[(startIndex + offset) % sellerProducts.Count])
                    .DistinctBy(product => product.Id)
                    .ToList();

                if (selected.Count == 0)
                    continue;

                var lines = selected
                    .Select((product, index) => new
                    {
                        Product = product,
                        Quantity = 1 + (customerIndex + orderIndex + index) % 2
                    })
                    .ToList();

                var subtotal = lines.Sum(line => line.Product.Price * line.Quantity);
                var promo = (customerIndex + orderIndex) % 4 == 0 && promoCodes.Count > 0
                    ? promoCodes[(customerIndex + orderIndex) % promoCodes.Count]
                    : null;
                var discount = promo is null ? 0m : CalculateDiscount(promo, subtotal);
                var createdAt = now.AddDays(-(1 + (customerIndex * 3 + orderIndex * 7) % 90));

                var order = new Order
                {
                    Id = orderId,
                    UserId = customer.Id,
                    SellerId = seller.Id,
                    PromoCodeId = discount > 0 ? promo?.Id : null,
                    Status = statuses[(customerIndex + orderIndex) % statuses.Length],
                    TotalAmount = Math.Max(0, subtotal - discount),
                    DiscountAmount = discount,
                    ShippingAddress = $"{100 + customerIndex} Demo Avenue, {customer.FirstName} {customer.LastName}, New York",
                    ReceiverName = $"{customer.FirstName} {customer.LastName}",
                    ReceiverPhone = $"+1-555-01{customerIndex:00}",
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt.AddDays(1)
                };
                orders.Add(order);

                foreach (var line in lines)
                {
                    orderItems.Add(new OrderItem
                    {
                        Id = CreateId($"order-item:{orderId}:{line.Product.Id}"),
                        OrderId = orderId,
                        ProductId = line.Product.Id,
                        ProductName = line.Product.Name,
                        ProductPrice = line.Product.Price,
                        ProductAvatarUrl = mainImageByProduct.TryGetValue(line.Product.Id, out var imagePath)
                            ? imagePath
                            : string.Empty,
                        Quantity = line.Quantity,
                        TotalPrice = line.Product.Price * line.Quantity
                    });
                }
            }
        }

        _context.Orders.AddRange(orders);
        _context.OrderItems.AddRange(orderItems);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var order in orders.Where(order => order.PromoCodeId is not null))
        {
            var promo = promoCodes.FirstOrDefault(candidate => candidate.Id == order.PromoCodeId);
            if (promo is not null)
                promo.UsedActivationsCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedGiftCardsAsync(
        List<User> customers,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (customers.Count == 0)
            return;

        var existingCodes = await _context.GiftCards
            .Where(card => card.Code.StartsWith(DemoGiftPrefix))
            .Select(card => card.Code)
            .ToHashSetAsync(cancellationToken);

        for (var index = 0; index < GiftCardDefinitions.Length; index++)
        {
            var code = $"{DemoGiftPrefix}{index + 1:0000}";
            if (existingCodes.Contains(code))
                continue;

            var definition = GiftCardDefinitions[index];
            var purchasedBy = customers[index % customers.Count];
            var activatedBy = index % 2 == 0
                ? customers[(index + 1) % customers.Count]
                : null;

            _context.GiftCards.Add(new GiftCard
            {
                Id = CreateId($"gift-card:{code}"),
                Code = code,
                InitialBalance = definition.InitialBalance,
                CurrentBalance = definition.CurrentBalance,
                PurchasedByUserId = purchasedBy.Id,
                ActivatedByUserId = activatedBy?.Id,
                ExpiresAt = now.AddDays(365),
                IsActive = true,
                CreatedAt = now.AddDays(-(index * 5 + 10))
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<DemoSeedResult> BuildResultAsync(CancellationToken cancellationToken)
    {
        var demoUserIds = await _context.Users
            .Where(user => user.Email.EndsWith(DemoEmailDomain))
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);
        var demoSellerIds = await _context.Sellers
            .Where(seller => seller.StoreSlug.StartsWith(DemoSlugPrefix) || demoUserIds.Contains(seller.UserId))
            .Select(seller => seller.Id)
            .ToListAsync(cancellationToken);
        var demoProductIds = await _context.Products
            .Where(product =>
                (product.Sku != null && product.Sku.StartsWith(DemoSkuPrefix)) ||
                demoSellerIds.Contains(product.SellerId))
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);

        return new DemoSeedResult(
            Sellers: demoSellerIds.Count,
            Categories: await _context.Categories.CountAsync(cancellationToken),
            Subcategories: await _context.Categories.CountAsync(
                category => category.Slug.StartsWith(DemoSlugPrefix), cancellationToken),
            Products: demoProductIds.Count,
            Images: await _context.MediaFiles.CountAsync(
                media => media.FileName.StartsWith(DemoMediaPrefix), cancellationToken),
            Customers: await _context.Users.CountAsync(
                user => user.Email.EndsWith(DemoEmailDomain) && user.UserRole.Name == Roles.User,
                cancellationToken),
            Orders: await _context.Orders.CountAsync(
                order => demoUserIds.Contains(order.UserId) || demoSellerIds.Contains(order.SellerId),
                cancellationToken),
            Reviews: await _context.ProductReviews.CountAsync(
                review => demoProductIds.Contains(review.ProductId), cancellationToken),
            Questions: await _context.ProductQuestions.CountAsync(
                question => demoProductIds.Contains(question.ProductId), cancellationToken),
            PromoCodes: await _context.PromoCodes.CountAsync(
                promo => promo.Code.StartsWith(DemoPromoPrefix), cancellationToken),
            GiftCards: await _context.GiftCards.CountAsync(
                card => card.Code.StartsWith(DemoGiftPrefix), cancellationToken),
            SeededAt: DateTime.UtcNow);
    }

    private async Task ResetDemoDataAsync(CancellationToken cancellationToken)
    {
        if (!_context.Database.IsRelational())
            throw new InvalidOperationException("Demo reset is supported only on relational databases.");

        IDbContextTransaction? transaction = null;
        if (_context.Database.IsRelational())
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var demoUserIds = await _context.Users
                .Where(user => user.Email.EndsWith(DemoEmailDomain))
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);
            var demoSellerIds = await _context.Sellers
                .Where(seller =>
                    seller.StoreSlug.StartsWith(DemoSlugPrefix) ||
                    demoUserIds.Contains(seller.UserId))
                .Select(seller => seller.Id)
                .ToListAsync(cancellationToken);
            var demoProductIds = await _context.Products
                .Where(product =>
                    (product.Sku != null && product.Sku.StartsWith(DemoSkuPrefix)) ||
                    demoSellerIds.Contains(product.SellerId))
                .Select(product => product.Id)
                .ToListAsync(cancellationToken);
            var demoQuestionIds = await _context.ProductQuestions
                .Where(question =>
                    demoProductIds.Contains(question.ProductId) ||
                    demoUserIds.Contains(question.UserId))
                .Select(question => question.Id)
                .ToListAsync(cancellationToken);
            var demoOrderIds = await _context.Orders
                .Where(order =>
                    demoUserIds.Contains(order.UserId) ||
                    demoSellerIds.Contains(order.SellerId))
                .Select(order => order.Id)
                .ToListAsync(cancellationToken);
            var demoCartIds = await _context.Carts
                .Where(cart => demoUserIds.Contains(cart.UserId))
                .Select(cart => cart.Id)
                .ToListAsync(cancellationToken);
            var demoPromoIds = await _context.PromoCodes
                .Where(promo => promo.Code.StartsWith(DemoPromoPrefix))
                .Select(promo => promo.Id)
                .ToListAsync(cancellationToken);

            await _context.QuestionVotes
                .Where(vote =>
                    demoQuestionIds.Contains(vote.QuestionId) ||
                    demoUserIds.Contains(vote.UserId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.ProductAnswers
                .Where(answer =>
                    demoQuestionIds.Contains(answer.QuestionId) ||
                    demoUserIds.Contains(answer.UserId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.ProductQuestions
                .Where(question => demoQuestionIds.Contains(question.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.ProductReviews
                .Where(review =>
                    demoProductIds.Contains(review.ProductId) ||
                    demoUserIds.Contains(review.UserId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.CartItems
                .Where(item =>
                    demoCartIds.Contains(item.CartId) ||
                    demoProductIds.Contains(item.ProductId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.Carts
                .Where(cart => demoCartIds.Contains(cart.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.OrderItems
                .Where(item =>
                    demoOrderIds.Contains(item.OrderId) ||
                    (item.ProductId != null && demoProductIds.Contains(item.ProductId.Value)))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.Orders
                .Where(order => demoOrderIds.Contains(order.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.PromoCodeProducts
                .Where(link =>
                    demoPromoIds.Contains(link.PromoCodeId) ||
                    demoProductIds.Contains(link.ProductId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.PromoCodes
                .Where(promo => demoPromoIds.Contains(promo.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.GiftCards
                .Where(card =>
                    demoUserIds.Contains(card.PurchasedByUserId) ||
                    (card.ActivatedByUserId != null && demoUserIds.Contains(card.ActivatedByUserId.Value)))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.HistoryPriceProducts
                .Where(history => demoProductIds.Contains(history.ProductId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.ProductGalleries
                .Where(gallery => demoProductIds.Contains(gallery.ProductId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.ProductVideos
                .Where(video => demoProductIds.Contains(video.ProductId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.ProductAttributeValues
                .Where(attribute => demoProductIds.Contains(attribute.ProductId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.Products
                .Where(product => demoProductIds.Contains(product.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.Sellers
                .Where(seller => demoSellerIds.Contains(seller.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.UserAddresses
                .Where(address => demoUserIds.Contains(address.UserId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.UserProfiles
                .Where(profile => demoUserIds.Contains(profile.UserId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.RefreshTokens
                .Where(token => demoUserIds.Contains(token.UserId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.Users
                .Where(user => demoUserIds.Contains(user.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.MediaFiles
                .Where(media => media.FileName.StartsWith(DemoMediaPrefix))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.Categories
                .Where(category => category.Slug.StartsWith(DemoSlugPrefix))
                .ExecuteDeleteAsync(cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private static decimal CalculateDiscount(PromoCode promo, decimal subtotal)
    {
        if (promo.MinOrderAmount is decimal minimum && subtotal < minimum)
            return 0m;

        var discount = promo.DiscountType == DiscountType.Percentage
            ? subtotal * promo.DiscountValue / 100m
            : promo.DiscountValue;

        if (promo.MaxDiscountAmount is decimal maximum && discount > maximum)
            discount = maximum;

        return Math.Round(Math.Max(0m, discount), 2);
    }

    private static string BuildDescription(ProductSeed seed) =>
        $"{seed.Definition.Blurb} The {seed.Definition.Name} by {seed.Definition.Brand} combines " +
        $"thoughtful design, reliable materials and everyday practicality. This is a demo listing " +
        $"in the {seed.CategoryName} category, shipped by a verified Treemiix seller.";

    private static string[] BuildFeatures(ProductSeed seed) =>
    [
        $"Premium quality from {seed.Definition.Brand}",
        "Designed for everyday use",
        $"Category: {seed.CategoryName}",
        $"Model: {seed.Definition.Brand}-{seed.IndexInCategory + 1:000}",
        "2-year limited warranty"
    ];

    private void AddAttribute(Product product, string name, string value, string sku)
    {
        _context.ProductAttributeValues.Add(new ProductAttributeValue
        {
            Id = CreateId($"attribute:{sku}:{name}"),
            ProductId = product.Id,
            NameAttr = name,
            Value = value
        });
    }

    private async Task<(byte[] Bytes, string ContentType)?> TryDownloadAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("demo-seed");
            using var response = await client.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Demo image download failed with {StatusCode}: {Url}",
                    (int)response.StatusCode,
                    url);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            if (contentType is null || !contentType.StartsWith("image/", StringComparison.Ordinal))
            {
                _logger.LogWarning("Demo image URL returned a non-image content type: {Url}", url);
                return null;
            }

            if (response.Content.Headers.ContentLength is long contentLength &&
                contentLength > MaxImageBytes)
            {
                _logger.LogWarning("Demo image is larger than {MaxBytes} bytes: {Url}", MaxImageBytes, url);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            long total = 0;
            int read;

            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                total += read;
                if (total > MaxImageBytes)
                {
                    _logger.LogWarning("Demo image exceeded {MaxBytes} bytes: {Url}", MaxImageBytes, url);
                    return null;
                }

                await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
            }

            return total == 0 ? null : (buffer.ToArray(), contentType);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Demo image download failed: {Url}", url);
            return null;
        }
    }

    private static MediaAsset ToAsset(MediaFile media) =>
        new(media.FileName, $"/media/{media.FileName}", media.ContentType);

    private static string MediaFileName(string key, string extension) =>
        $"{DemoMediaPrefix}{Hash(key)[..24]}{extension}";

    private static string ExtensionForContentType(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => ".jpg"
    };

    private static string UnsplashUrl(string photoId) =>
        $"https://images.unsplash.com/photo-{photoId}?auto=format&fit=crop&w=600&h=600&q=55&fm=jpg";

    private static byte[] BuildSvg(string title, string subtitle, string key)
    {
        var (startColor, endColor) = Palette(key);
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeSubtitle = WebUtility.HtmlEncode(subtitle);

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="600" height="600" viewBox="0 0 600 600">
              <defs>
                <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0" stop-color="{startColor}" />
                  <stop offset="1" stop-color="{endColor}" />
                </linearGradient>
              </defs>
              <rect width="600" height="600" fill="url(#g)" />
              <circle cx="480" cy="120" r="160" fill="#ffffff" fill-opacity="0.12" />
              <circle cx="90" cy="520" r="220" fill="#ffffff" fill-opacity="0.08" />
              <text x="40" y="290" textLength="520" lengthAdjust="spacingAndGlyphs"
                    fill="#ffffff" font-family="Arial, Helvetica, sans-serif"
                    font-size="38" font-weight="700">{safeTitle}</text>
              <text x="42" y="345" textLength="500" lengthAdjust="spacingAndGlyphs"
                    fill="#ffffff" fill-opacity="0.82" font-family="Arial, Helvetica, sans-serif"
                    font-size="22">{safeSubtitle}</text>
              <text x="42" y="540" fill="#ffffff" fill-opacity="0.65"
                    font-family="Arial, Helvetica, sans-serif" font-size="18">TREEMIIX DEMO</text>
            </svg>
            """;

        return Encoding.UTF8.GetBytes(svg);
    }

    private static (string Start, string End) Palette(string key)
    {
        var palettes = new[]
        {
            ("#0f172a", "#2563eb"),
            ("#3b0764", "#db2777"),
            ("#064e3b", "#10b981"),
            ("#7c2d12", "#f59e0b"),
            ("#1e3a8a", "#06b6d4"),
            ("#4a044e", "#a855f7"),
            ("#3f1d0b", "#ef4444"),
            ("#0c4a6e", "#22c55e")
        };

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return palettes[bytes[0] % palettes.Length];
    }

    private static Guid CreateId(string key) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16));

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string Slugify(string value)
    {
        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "item" : slug;
    }

    private sealed record DemoSeller(Seller Seller, User User, bool IsActive);

    private sealed record ProductSeed(
        string CategorySlug,
        string CategoryName,
        int IndexInCategory,
        DemoProduct Definition);

    private sealed record MediaRequest(
        string Key,
        string? Url,
        string? SvgTitle,
        string? SvgSubtitle);

    private sealed record MediaAsset(string FileName, string Path, string ContentType);

    private sealed record DemoProduct(
        string Name,
        string Brand,
        decimal Price,
        decimal? OldCost,
        string Blurb);

    private sealed record DemoCategory(
        string Name,
        string Slug,
        int SortOrder,
        string[] Subcategories,
        DemoProduct[] Products,
        string[] ImageIds);

    private sealed record SellerDefinition(
        string StoreName,
        string StoreSlug,
        string FirstName,
        string LastName,
        SellerBusinessType BusinessType,
        string LegalName,
        string Country,
        string Description,
        SellerStatus Status,
        SellerSubscriptionStatus SubscriptionStatus,
        string? RejectionReason = null);

    private sealed record CustomerDefinition(
        string FirstName,
        string LastName,
        string City,
        string Country);

    private sealed record PromoDefinition(
        string Code,
        decimal DiscountValue,
        DiscountType DiscountType,
        decimal? MinOrderAmount,
        decimal? MaxDiscountAmount,
        int MaxActivations,
        int LimitPerUser);

    private sealed record GiftCardDefinition(
        decimal InitialBalance,
        decimal CurrentBalance);

    private static readonly DemoCategory[] CategoryDefinitions =
    [
        new(
            "Electronics",
            "electronics",
            1,
            ["Audio", "Computers & Accessories", "Smart Home", "Cameras", "Gaming"],
            [
                new("Wireless Noise-Cancelling Headphones", "SoundWave", 129.99m, 179.99m,
                    "Immersive sound with adaptive noise cancellation and a comfortable over-ear fit."),
                new("UltraBook Pro 14 Laptop", "NovaTech", 1099.00m, null,
                    "A slim, powerful laptop for work, study and creative projects."),
                new("Pulse Smartwatch Series 8", "Pulse", 199.99m, 249.99m,
                    "Health tracking, notifications and a bright always-on display."),
                new("Mirrorless Camera Kit", "Lumina", 749.00m, null,
                    "A compact mirrorless camera with a versatile zoom lens and 4K video."),
                new("Portable Bluetooth Speaker", "BoomBox", 59.99m, 79.99m,
                    "Room-filling sound in a splash-resistant, travel-friendly design.")
            ],
            [
                "1505740420928-5e560c06d30e",
                "1496181133206-80ce9b88a853",
                "1523275335684-37898b6baf30",
                "1516035069371-29a1b244cc32",
                "1608043152269-423dbba4e7e1"
            ]),
        new(
            "Home & Kitchen",
            "home-kitchen",
            2,
            ["Kitchen Appliances", "Cookware", "Home Decor", "Furniture", "Bedding"],
            [
                new("Nordic Table Lamp", "Hygge", 49.99m, 69.99m,
                    "A warm, minimalist lamp that fits living rooms, bedrooms and reading corners."),
                new("Ergonomic Lounge Chair", "ComfortLine", 299.00m, null,
                    "A supportive lounge chair with a clean silhouette and durable upholstery."),
                new("Barista Espresso Machine", "BrewMaster", 349.99m, 429.99m,
                    "Cafe-quality espresso at home with a built-in milk frother."),
                new("Chef's Non-Stick Cookware Set", "CopperChef", 89.99m, 119.99m,
                    "A versatile cookware set with even heating and easy-clean surfaces."),
                new("Organic Cotton Bedding Set", "CloudSleep", 79.99m, null,
                    "Soft, breathable bedding made from certified organic cotton.")
            ],
            [
                "1507473885765-e6ed057f782c",
                "1567538096630-e0c55bd6374c",
                "1517668808822-9ebb02f2a0e6",
                "1556911220-bff31c812dba",
                "1522771739844-6a9f6d5f14af"
            ]),
        new(
            "Fashion",
            "fashion",
            3,
            ["Men's Clothing", "Women's Clothing", "Shoes", "Bags & Accessories", "Jewelry"],
            [
                new("Classic Leather Sneakers", "UrbanStep", 89.99m, 119.99m,
                    "Clean everyday sneakers with a cushioned sole and premium leather upper."),
                new("Premium Cotton T-Shirt", "Essential", 24.99m, null,
                    "A soft, heavyweight cotton tee with a relaxed unisex fit."),
                new("Water-Resistant Bomber Jacket", "NorthWay", 149.99m, 199.99m,
                    "A lightweight bomber jacket built for changing weather and city commutes."),
                new("Everyday Leather Tote Bag", "Mira", 129.00m, null,
                    "A roomy tote with a structured shape, inner pockets and a magnetic closure."),
                new("Polarized Aviator Sunglasses", "SunLine", 59.99m, 79.99m,
                    "Classic aviators with polarized lenses and a lightweight metal frame.")
            ],
            [
                "1542291026-7eec264c27ff",
                "1521572163474-6864f9cf17ab",
                "1551028719-00167b16eac5",
                "1584917865442-de89df76afd3",
                "1511499767150-a48a237f0083"
            ]),
        new(
            "Footwear & Sports",
            "footwear-sports",
            4,
            ["Running Shoes", "Fitness Equipment", "Yoga", "Cycling", "Outdoor"],
            [
                new("AirFlex Running Shoes", "Stride", 119.99m, 149.99m,
                    "Responsive running shoes with breathable mesh and a shock-absorbing sole."),
                new("Adjustable Dumbbell Set", "IronCore", 199.00m, null,
                    "A space-saving dumbbell set with quick-change weight plates."),
                new("Eco Yoga Mat", "ZenFlow", 39.99m, 49.99m,
                    "A non-slip yoga mat made from sustainable, cushioning materials."),
                new("City Commuter Bicycle", "VeloCity", 499.00m, 599.00m,
                    "A lightweight commuter bike with a comfortable upright riding position."),
                new("4-Person Camping Tent", "TrailMate", 159.99m, null,
                    "A weather-ready tent with quick setup and room for the whole family.")
            ],
            [
                "1595950653106-6c9ebd614d3a",
                "1517836357463-d25dfeac3438",
                "1601925260368-ae2f83cf8b7f",
                "1485965120184-e220f721d03e",
                "1504280390367-361c6d9f38f4"
            ]),
        new(
            "Beauty & Personal Care",
            "beauty-personal-care",
            5,
            ["Skincare", "Hair Care", "Fragrance", "Makeup", "Oral Care"],
            [
                new("Hydrating Vitamin C Serum", "GlowLab", 29.99m, 39.99m,
                    "A brightening serum with vitamin C and hyaluronic acid for daily hydration."),
                new("Eau de Parfum - Amber Noir", "Maison", 79.99m, null,
                    "A warm, long-lasting fragrance with amber, vanilla and cedar notes."),
                new("Matte Lipstick Collection", "Velvet", 34.99m, 44.99m,
                    "A set of richly pigmented matte lipsticks in wearable everyday shades."),
                new("Argan Oil Hair Mask", "SilkRoots", 24.99m, null,
                    "A deep-conditioning hair mask for softer, smoother and shinier hair."),
                new("Electric Sonic Toothbrush", "DentPro", 49.99m, 69.99m,
                    "A rechargeable sonic toothbrush with multiple cleaning modes and a timer.")
            ],
            [
                "1556228578-8c89e6adf883",
                "1541643600914-78b084683601",
                "1596462502278-27bfdc403348",
                "1522338242992-e1a54906a8da",
                "1559599101-f09722fb4948"
            ]),
        new(
            "Toys & Games",
            "toys-games",
            6,
            ["Board Games", "Building Sets", "Puzzles", "Outdoor Play", "Learning Toys"],
            [
                new("Wooden Building Blocks Set", "TinyArchitect", 39.99m, 49.99m,
                    "A colourful wooden block set that encourages creative, open-ended play."),
                new("Family Strategy Board Game", "GameNight", 44.99m, null,
                    "A clever strategy game for family game nights and small groups."),
                new("1000-Piece Landscape Puzzle", "PuzzleCraft", 19.99m, 24.99m,
                    "A high-quality jigsaw puzzle with a detailed landscape illustration."),
                new("Remote-Control Racing Car", "Speedster", 59.99m, 74.99m,
                    "A fast, rechargeable RC car with responsive steering and durable tyres."),
                new("Plush Teddy Bear", "CuddleCo", 24.99m, null,
                    "A soft, huggable teddy bear made from child-friendly materials.")
            ],
            [
                "1558060370-d644479cb6f7",
                "1585366119957-e9730b6d0f60",
                "1610890716171-6b1bb98ffd09",
                "1606092195730-5d7b9af1efc5",
                "1562040506-a9b32cb51b94"
            ]),
        new(
            "Books",
            "books",
            7,
            ["Fiction", "Non-Fiction", "Children's Books", "Cookbooks", "Business"],
            [
                new("The Silent Orchard - A Novel", "RiverHouse", 18.99m, 24.99m,
                    "A atmospheric family drama about memory, land and the secrets we keep."),
                new("Atomic Habits for Teams", "PeakPress", 22.99m, null,
                    "A practical guide to building better routines and stronger teams."),
                new("The Little Explorer's Atlas", "BrightPages", 16.99m, 21.99m,
                    "An illustrated atlas that introduces children to countries and cultures."),
                new("Everyday Mediterranean Cookbook", "Table & Spoon", 27.99m, null,
                    "Simple, fresh recipes inspired by the Mediterranean kitchen."),
                new("The Founder's Playbook", "LaunchLab", 29.99m, 34.99m,
                    "A hands-on guide to validating ideas, launching products and finding customers.")
            ],
            [
                "1544947950-fa07a98d237f",
                "1512820790803-83ca734da794",
                "1507842217343-583bb7270b66",
                "1553729459-efe14ef6055d",
                "1531346878377-a5be20888e57"
            ]),
        new(
            "Pet Supplies",
            "pet-supplies",
            8,
            ["Dog Supplies", "Cat Supplies", "Pet Food", "Aquatics", "Pet Toys"],
            [
                new("Orthopedic Dog Bed", "PawComfort", 69.99m, 89.99m,
                    "A supportive memory-foam bed that keeps older dogs comfortable."),
                new("Cat Tree with Scratching Post", "WhiskerWood", 89.99m, null,
                    "A multi-level cat tree with cosy perches and a durable scratching post."),
                new("Grain-Free Salmon Dog Food", "HappyBowl", 44.99m, 54.99m,
                    "A balanced grain-free recipe made with salmon and wholesome vegetables."),
                new("Interactive Feather Cat Toy", "PlayPaws", 14.99m, null,
                    "A playful feather toy that encourages healthy exercise and curiosity."),
                new("10-Gallon Aquarium Starter Kit", "AquaNest", 79.99m, 99.99m,
                    "A complete starter aquarium with filter, LED light and quiet pump.")
            ],
            [
                "1583337130417-3346a1be7dee",
                "1514888286974-6c03e2ca1dba",
                "1589924691995-400dc9ecc119",
                "1576201836106-db1758fd1c97",
                "1520302630591-fd1c66edc19d"
            ]),
        new(
            "Garden & Outdoor",
            "garden-outdoor",
            9,
            ["Garden Tools", "Plants", "Outdoor Furniture", "Grills", "Bird Care"],
            [
                new("Indoor Herb Garden Kit", "GreenRoot", 34.99m, 44.99m,
                    "Everything you need to grow fresh herbs on a kitchen windowsill."),
                new("Folding Patio Lounge Set", "Gardenia", 349.00m, null,
                    "A comfortable, space-saving lounge set for balconies and patios."),
                new("Portable Charcoal Grill", "GrillMate", 129.99m, 159.99m,
                    "A compact grill with a large cooking surface for picnics and camping."),
                new("Ceramic Planter Trio", "Terra", 39.99m, null,
                    "Three hand-finished ceramic planters in complementary neutral tones."),
                new("Cedar Bird Feeder", "WingHouse", 24.99m, 29.99m,
                    "A weather-resistant cedar feeder that is easy to fill and clean.")
            ],
            [
                "1485955900006-10f4d324d411",
                "1416879595882-3373a0480b5b",
                "1600210492486-724fe5c67fb0",
                "1555939594-58d7cb561ad1",
                "1444464666168-49d633b86797"
            ]),
        new(
            "Office Products",
            "office-products",
            10,
            ["Office Furniture", "Stationery", "Printers", "Desk Accessories", "Office Electronics"],
            [
                new("Ergonomic Mesh Office Chair", "WorkWell", 229.00m, 279.00m,
                    "A breathable mesh chair with adjustable lumbar support and armrests."),
                new("Premium Hardcover Notebook", "NoteCraft", 19.99m, null,
                    "A lay-flat hardcover notebook with thick, fountain-pen-friendly paper."),
                new("Wireless All-in-One Printer", "PrintPro", 149.99m, 179.99m,
                    "Print, scan and copy wirelessly from your phone, tablet or laptop."),
                new("Bamboo Desk Organizer", "DeskZen", 34.99m, null,
                    "A tidy bamboo organizer with compartments for pens, notes and devices."),
                new("Ultra-Wide LED Monitor", "ViewMax", 399.00m, 449.00m,
                    "A spacious ultra-wide monitor for multitasking and immersive work.")
            ],
            [
                "1518455027359-f3f8164ba6bd",
                "1455390582262-044cdead277a",
                "1612815154858-60aa4c59eaa6",
                "1580480055273-228ff5388ef8",
                "1497366754035-f200968a6e72"
            ]),
        new(
            "Health & Household",
            "health-household",
            11,
            ["Vitamins & Supplements", "Household Supplies", "Medical Supplies", "Personal Care", "Baby Care"],
            [
                new("Daily Multivitamin Gummies", "VitaWell", 19.99m, 24.99m,
                    "A fruity daily multivitamin with essential vitamins and minerals."),
                new("Plant-Based Cleaning Spray", "PureHome", 9.99m, null,
                    "A gentle plant-based spray for everyday kitchen and bathroom cleaning."),
                new("Digital Blood Pressure Monitor", "MediCheck", 49.99m, 64.99m,
                    "An easy-to-use upper-arm monitor with a large backlit display."),
                new("Ultra-Soft Baby Blanket", "LittleCloud", 24.99m, null,
                    "A breathable, ultra-soft blanket for naps, prams and bedtime."),
                new("Reusable Water Bottle 1L", "HydroFlow", 29.99m, 34.99m,
                    "A leak-proof stainless steel bottle that keeps drinks cold for hours.")
            ],
            [
                "1550572017-edd951b55104",
                "1585421514738-01798e348b17",
                "1584982751601-97dcc096659c",
                "1515488042361-ee00e0ddd4e4",
                "1556228720-195a672e8a03"
            ]),
        new(
            "Automotive",
            "automotive",
            12,
            ["Car Accessories", "Car Care", "Tools & Equipment", "Tires & Wheels", "Motorcycle"],
            [
                new("Car Phone Mount with Wireless Charging", "DriveGear", 39.99m, 49.99m,
                    "A secure dashboard mount with fast wireless charging for compatible phones."),
                new("Premium Car Wash Kit", "ShinePro", 34.99m, null,
                    "A complete wash kit with shampoo, microfiber towels and detailing tools."),
                new("Portable Tire Inflator", "RoadFix", 59.99m, 74.99m,
                    "A compact digital inflator that plugs into your car and stops at the set pressure."),
                new("All-Season Floor Mats", "GuardMat", 49.99m, null,
                    "Durable, easy-to-clean floor mats that protect your car interior all year."),
                new("Bluetooth Motorcycle Helmet Headset", "RideTalk", 79.99m, 99.99m,
                    "Hands-free calls, navigation prompts and intercom support for riders.")
            ],
            [
                "1494976388531-d1058494cdd8",
                "1503376780353-7e6692767b70",
                "1530124566582-a618bc2615dc",
                "1553440569-bcc63803a83d",
                "1558981806-ec527fa84c39"
            ])
    ];

    private static readonly SellerDefinition[] SellerDefinitions =
    [
        new("Treemiix Official Store", "treemiix-official", "Maya", "Chen",
            SellerBusinessType.Company, "Treemiix Demo LLC", "US",
            "Curated electronics and everyday essentials from the Treemiix team.",
            SellerStatus.Active, SellerSubscriptionStatus.Active),
        new("SoundWave Audio", "soundwave-audio", "Daniel", "Reed",
            SellerBusinessType.Company, "SoundWave Audio Inc.", "US",
            "Premium headphones, speakers and audio accessories.",
            SellerStatus.Active, SellerSubscriptionStatus.Active),
        new("Hygge Home", "hygge-home", "Freja", "Nielsen",
            SellerBusinessType.Company, "Hygge Home ApS", "DK",
            "Warm Nordic design for kitchens, living rooms and bedrooms.",
            SellerStatus.Active, SellerSubscriptionStatus.Active),
        new("UrbanStep Footwear", "urbanstep-footwear", "Alex", "Morgan",
            SellerBusinessType.SoleProprietor, "UrbanStep Studio", "GB",
            "Comfort-first sneakers and everyday footwear.",
            SellerStatus.Active, SellerSubscriptionStatus.Active),
        new("GlowLab Beauty", "glowlab-beauty", "Camille", "Laurent",
            SellerBusinessType.Company, "GlowLab Cosmetics SAS", "FR",
            "Clean skincare, fragrance and personal care.",
            SellerStatus.Active, SellerSubscriptionStatus.Active),
        new("GreenRoot Garden", "greenroot-garden", "Noah", "Green",
            SellerBusinessType.Individual, "GreenRoot Garden", "CA",
            "Plants, planters and outdoor living essentials.",
            SellerStatus.Active, SellerSubscriptionStatus.Active),
        new("NovaTech Electronics", "novatech-electronics", "Elena", "Petrova",
            SellerBusinessType.Company, "NovaTech Devices GmbH", "DE",
            "Computers, monitors and smart devices.",
            SellerStatus.Active, SellerSubscriptionStatus.Active),
        new("TrailMate Outdoors", "trailmate-outdoors", "Owen", "Brooks",
            SellerBusinessType.Company, "TrailMate Outdoors LLC", "US",
            "Camping, cycling and outdoor adventure gear.",
            SellerStatus.Suspended, SellerSubscriptionStatus.PastDue),
        new("PixelPlay Games", "pixelplay-games", "Riley", "Kim",
            SellerBusinessType.Company, "PixelPlay Games Inc.", "US",
            "Board games, puzzles and family entertainment.",
            SellerStatus.PendingVerification, SellerSubscriptionStatus.None),
        new("ComfortLine Living", "comfortline-living", "Sofia", "Martinez",
            SellerBusinessType.Company, "ComfortLine Living LLC", "US",
            "Furniture and home comfort for modern living.",
            SellerStatus.Rejected, SellerSubscriptionStatus.None,
            "Business documents could not be verified. Please re-apply with a valid tax certificate.")
    ];

    private static readonly CustomerDefinition[] CustomerDefinitions =
    [
        new("Emma", "Johnson", "New York", "US"),
        new("Liam", "Smith", "Los Angeles", "US"),
        new("Olivia", "Brown", "Chicago", "US"),
        new("Noah", "Davis", "Houston", "US"),
        new("Ava", "Wilson", "Phoenix", "US"),
        new("Ethan", "Miller", "Philadelphia", "US"),
        new("Sophia", "Moore", "San Antonio", "US"),
        new("Mason", "Taylor", "San Diego", "US"),
        new("Isabella", "Anderson", "Dallas", "US"),
        new("Lucas", "Thomas", "San Jose", "US"),
        new("Mia", "Jackson", "Austin", "US"),
        new("James", "White", "Seattle", "US")
    ];

    private static readonly PromoDefinition[] PromoDefinitions =
    [
        new("DEMO10", 10m, DiscountType.Percentage, 50m, 20m, 1000, 1),
        new("DEMO20", 20m, DiscountType.Percentage, 100m, 50m, 500, 1),
        new("DEMOSAVE15", 15m, DiscountType.FixedAmount, 75m, null, 300, 2),
        new("DEMOFREESHIP", 5m, DiscountType.FixedAmount, 25m, null, 1000, 1),
        new("DEMOWELCOME5", 5m, DiscountType.Percentage, null, 10m, 5000, 1)
    ];

    private static readonly GiftCardDefinition[] GiftCardDefinitions =
    [
        new(50m, 50m),
        new(100m, 75m),
        new(25m, 25m),
        new(200m, 120m),
        new(75m, 75m)
    ];
}
