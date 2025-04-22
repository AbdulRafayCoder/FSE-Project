using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEBACKEND.Migrations
{
    /// <inheritdoc />
    public partial class updateChallanModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChallanFile",
                table: "Challans");

            migrationBuilder.RenameColumn(
                name: "RequestId",
                table: "Challans",
                newName: "IssuedDate");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Challans",
                newName: "Amount");

            migrationBuilder.AddColumn<int>(
                name: "LendId",
                table: "Challans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Challans_LendId",
                table: "Challans",
                column: "LendId");

            migrationBuilder.AddForeignKey(
                name: "FK_Challans_Lends_LendId",
                table: "Challans",
                column: "LendId",
                principalTable: "Lends",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challans_Lends_LendId",
                table: "Challans");

            migrationBuilder.DropIndex(
                name: "IX_Challans_LendId",
                table: "Challans");

            migrationBuilder.DropColumn(
                name: "LendId",
                table: "Challans");

            migrationBuilder.RenameColumn(
                name: "IssuedDate",
                table: "Challans",
                newName: "RequestId");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Challans",
                newName: "Email");

            migrationBuilder.AddColumn<string>(
                name: "ChallanFile",
                table: "Challans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
