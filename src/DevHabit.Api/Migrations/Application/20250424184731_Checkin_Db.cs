using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevHabit.Api.Migrations.Application
{
    /// <inheritdoc />
    public partial class Checkin_Db : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "frequency_timer_per_period",
                schema: "dev_habit",
                table: "habits",
                newName: "frequency_times_per_period");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "frequency_times_per_period",
                schema: "dev_habit",
                table: "habits",
                newName: "frequency_timer_per_period");
        }
    }
}
