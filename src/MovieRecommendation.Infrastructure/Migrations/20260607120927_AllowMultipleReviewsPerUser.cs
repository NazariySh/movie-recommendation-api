using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieRecommendation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleReviewsPerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_movie_reviews_user_id_movie_id",
                table: "movie_reviews");

            migrationBuilder.CreateIndex(
                name: "ix_movie_reviews_user_id_movie_id",
                table: "movie_reviews",
                columns: new[] { "user_id", "movie_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_movie_reviews_user_id_movie_id",
                table: "movie_reviews");

            migrationBuilder.CreateIndex(
                name: "ix_movie_reviews_user_id_movie_id",
                table: "movie_reviews",
                columns: new[] { "user_id", "movie_id" },
                unique: true,
                filter: "parent_review_id IS NULL");
        }
    }
}
