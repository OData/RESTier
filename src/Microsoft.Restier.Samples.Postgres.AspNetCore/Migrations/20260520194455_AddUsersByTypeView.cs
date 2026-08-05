using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microsoft.Restier.Samples.Postgres.AspNetCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersByTypeView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW ""UsersByType"" AS
SELECT ut.""DisplayName"" AS ""TypeName"",
       COUNT(u.""Id"")::int AS ""UserCount""
FROM ""UserTypes"" ut
LEFT JOIN ""Users"" u ON u.""UserTypeId"" = ut.""Id""
GROUP BY ut.""DisplayName"";
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""UsersByType"";");
        }
    }
}
