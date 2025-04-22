using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEBACKEND.Migrations
{
    /// <inheritdoc />
    public partial class AddIsReturnedToLend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReturned",
                table: "Lends",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReturned",
                table: "Lends");
        }
    }
}
