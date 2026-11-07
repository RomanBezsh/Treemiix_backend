using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneAmazonBack.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:discount_type", "percentage,fixed_amount")
                .Annotation("Npgsql:Enum:order_status", "pending,processing,shipped,delivered,cancelled,refunded")
                .Annotation("Npgsql:Enum:product_status", "active,inactive,archived,deleted")
                .Annotation("Npgsql:Enum:seller_status", "active,inactive,suspended,pending_verification,draft,rejected")
                .OldAnnotation("Npgsql:Enum:discount_type", "percentage,fixed_amount")
                .OldAnnotation("Npgsql:Enum:order_status", "pending,processing,shipped,delivered,cancelled,refunded")
                .OldAnnotation("Npgsql:Enum:product_status", "active,inactive,archived,deleted")
                .OldAnnotation("Npgsql:Enum:seller_status", "active,inactive,suspended,pending_verification");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Sellers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessType",
                table: "Sellers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Sellers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Sellers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OnboardingStep",
                table: "Sellers",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Sellers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Sellers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Sellers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionPaidUntil",
                table: "Sellers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionStatus",
                table: "Sellers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SupportEmail",
                table: "Sellers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportPhone",
                table: "Sellers",
                type: "text",
                nullable: true);

            // Existing sellers already passed onboarding. Active ones keep selling:
            // MVP billing considers them paid (renewals are marked by an admin).
            migrationBuilder.Sql("""
                UPDATE "Sellers"
                SET "OnboardingStep" = 4,
                    "ApprovedAt" = COALESCE("ApprovedAt", "CreatedAt"),
                    "SubscriptionStatus" = 1
                WHERE "Status" = 0;
                """);

            migrationBuilder.Sql("""
                UPDATE "Sellers"
                SET "OnboardingStep" = 4
                WHERE "OnboardingStep" < 4;
                """);

            // Older databases may contain sellers whose users still have the User role.
            migrationBuilder.Sql("""
                UPDATE "Users" u
                SET "UserRoleId" = r."Id"
                FROM "UserRoles" r
                WHERE r."Name" = 'Seller'
                  AND u."Id" IN (SELECT "UserId" FROM "Sellers")
                  AND u."UserRoleId" <> r."Id"
                  AND u."UserRoleId" NOT IN (SELECT "Id" FROM "UserRoles" WHERE "Name" = 'Admin');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "BusinessType",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "OnboardingStep",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "SubscriptionPaidUntil",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "SupportEmail",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "SupportPhone",
                table: "Sellers");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:discount_type", "percentage,fixed_amount")
                .Annotation("Npgsql:Enum:order_status", "pending,processing,shipped,delivered,cancelled,refunded")
                .Annotation("Npgsql:Enum:product_status", "active,inactive,archived,deleted")
                .Annotation("Npgsql:Enum:seller_status", "active,inactive,suspended,pending_verification")
                .OldAnnotation("Npgsql:Enum:discount_type", "percentage,fixed_amount")
                .OldAnnotation("Npgsql:Enum:order_status", "pending,processing,shipped,delivered,cancelled,refunded")
                .OldAnnotation("Npgsql:Enum:product_status", "active,inactive,archived,deleted")
                .OldAnnotation("Npgsql:Enum:seller_status", "active,inactive,suspended,pending_verification,draft,rejected");
        }
    }
}
