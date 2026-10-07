using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetCompany.FleetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResourceCoordination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE TABLE fleet_availability_revision (id integer PRIMARY KEY, version bigint NOT NULL); INSERT INTO fleet_availability_revision VALUES (1, 0);");
            migrationBuilder.CreateIndex(
                name: "ux_active_mission_driver",
                schema: "operations",
                table: "missions",
                column: "AssignedDriverId",
                unique: true,
                filter: "\"Status\" IN (3,4)");

            migrationBuilder.CreateIndex(
                name: "ux_active_mission_vehicle",
                schema: "operations",
                table: "missions",
                column: "AssignedVehicleId",
                unique: true,
                filter: "\"Status\" IN (3,4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE fleet_availability_revision;");
            migrationBuilder.DropIndex(
                name: "ux_active_mission_driver",
                schema: "operations",
                table: "missions");

            migrationBuilder.DropIndex(
                name: "ux_active_mission_vehicle",
                schema: "operations",
                table: "missions");
        }
    }
}
