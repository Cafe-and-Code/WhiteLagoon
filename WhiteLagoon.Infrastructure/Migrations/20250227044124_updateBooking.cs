using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhiteLagoon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa cột cũ (datetime2)
            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "Bookings");

            // Tạo lại cột với kiểu float
            migrationBuilder.AddColumn<double>(
                name: "TotalCost",
                table: "Bookings",
                type: "float",
                nullable: false,
                defaultValue: 0.0); // Gán giá trị mặc định để tránh lỗi dữ liệu null
        }



        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa cột float
            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "Bookings");

            // Tạo lại cột với kiểu datetime2
            migrationBuilder.AddColumn<DateTime>(
                name: "TotalCost",
                table: "Bookings",
                type: "datetime2",
                nullable: false);
        }

    }
}
