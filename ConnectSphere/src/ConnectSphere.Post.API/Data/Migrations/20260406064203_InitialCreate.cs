using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ConnectSphere.Post.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "post");

            migrationBuilder.CreateTable(
                name: "Posts",
                schema: "post",
                columns: table => new
                {
                    PostId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    MediaUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    MediaType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Visibility = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Hashtags = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    CommentCount = table.Column<int>(type: "integer", nullable: false),
                    ShareCount = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalPostId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.PostId);
                    table.ForeignKey(
                        name: "FK_Posts_Posts_OriginalPostId",
                        column: x => x.OriginalPostId,
                        principalSchema: "post",
                        principalTable: "Posts",
                        principalColumn: "PostId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_OriginalPostId",
                schema: "post",
                table: "Posts",
                column: "OriginalPostId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId_CreatedAt",
                schema: "post",
                table: "Posts",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Posts",
                schema: "post");
        }
    }
}
