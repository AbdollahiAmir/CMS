using System;
using BackEnd.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261112070000_AddScheduledPublishing")]
    public partial class AddScheduledPublishing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "Posts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "ContentPages",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "ContentPages");
        }
    }
}
