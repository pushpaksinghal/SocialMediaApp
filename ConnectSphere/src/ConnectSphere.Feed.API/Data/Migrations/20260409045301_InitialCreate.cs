using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ConnectSphere.Feed.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "feed");

            migrationBuilder.CreateTable(
                name: "FeedItems",
                schema: "feed",
                columns: table => new
                {
                    FeedItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedItems", x => x.FeedItemId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedItems_UserId_CreatedAt",
                schema: "feed",
                table: "FeedItems",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedItems",
                schema: "feed");
        }
    }
}
