using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Books.Migrations
{
    /// <inheritdoc />
    public partial class InitialBooksSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PublicationYear = table.Column<int>(type: "int", nullable: false),
                    Isbn = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Publisher = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CountPages = table.Column<int>(type: "int", nullable: false),
                    ContentsXml = table.Column<string>(type: "xml", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "sysutcdatetime()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblBooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblGenres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblGenres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblPublishers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblPublishers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblBookGenres",
                columns: table => new
                {
                    BookId = table.Column<int>(type: "int", nullable: false),
                    GenreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblBookGenres", x => new { x.BookId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_TblBookGenres_TblGenres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "TblGenres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TblBookGenres_tblBooks_BookId",
                        column: x => x.BookId,
                        principalTable: "tblBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TblBookTypes",
                columns: table => new
                {
                    BookId = table.Column<int>(type: "int", nullable: false),
                    TypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblBookTypes", x => new { x.BookId, x.TypeId });
                    table.ForeignKey(
                        name: "FK_TblBookTypes_TblTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "TblTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TblBookTypes_tblBooks_BookId",
                        column: x => x.BookId,
                        principalTable: "tblBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TblBookGenres_GenreId",
                table: "TblBookGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title_Author",
                table: "tblBooks",
                columns: new[] { "Title", "Author" });

            migrationBuilder.CreateIndex(
                name: "UX_TblBooks_Isbn",
                table: "tblBooks",
                column: "Isbn",
                unique: true,
                filter: "[Isbn] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TblBookTypes_TypeId",
                table: "TblBookTypes",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "UX_TblGenres_Name",
                table: "TblGenres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TblPublishers_Name",
                table: "TblPublishers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TblTypes_Name",
                table: "TblTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TblBookGenres");

            migrationBuilder.DropTable(
                name: "TblBookTypes");

            migrationBuilder.DropTable(
                name: "TblPublishers");

            migrationBuilder.DropTable(
                name: "TblGenres");

            migrationBuilder.DropTable(
                name: "TblTypes");

            migrationBuilder.DropTable(
                name: "tblBooks");
        }
    }
}
