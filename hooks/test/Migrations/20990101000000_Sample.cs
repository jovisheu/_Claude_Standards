// Migration 檢查的測試樣本：Up() 的每個操作都應被 check-migration.js 標示，Down() 不檢查（不參與任何建置）
namespace backend.Data.Migrations
{
    public partial class Sample : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bulletin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "流水號（含括號 ) 的描述）")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bulletin", x => x.Id);
                });

            migrationBuilder.AddColumn<bool>(
                name: "IsTop",
                table: "Bulletin",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Bulletin");

            migrationBuilder.Sql("UPDATE Bulletin SET IsTop = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bulletin");
        }
    }
}
