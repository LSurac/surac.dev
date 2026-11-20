using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuracDev.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileWorkingMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkingMode",
                table: "profile_translations",
                type: "text",
                nullable: true);

            // Data step added by hand to the generated migration: the existing profile keeps the text the
            // start page showed from the web bundle (ui-strings.ts) until the section became content.
            migrationBuilder.Sql(
                "UPDATE profile_translations SET \"WorkingMode\" = '"
                + "Ich entwickle mit KI-Agenten als Werkzeug. Jede Aufgabe beginnt mit einem Plan, den ich abnehme, "
                + "und mit festen Regeln im Repository: Architektur, Teststrategie und Code-Stil stehen in AGENTS.md "
                + "und den Dokumenten unter docs/.\n\n"
                + "Agenten suchen im Code, schreiben Tests und Implementierung nach diesen Regeln und führen die "
                + "Prüfungen aus. Architektur, Datenmodell, Sicherheit und die Abnahme jeder Änderung bleiben "
                + "bei mir.\n\n"
                + "Diese Seite ist so entstanden. Ihr Quellcode zeigt, wie das im Alltag aussieht."
                + "' WHERE \"Locale\" = 'de' AND \"WorkingMode\" IS NULL;");
            migrationBuilder.Sql(
                "UPDATE profile_translations SET \"WorkingMode\" = '"
                + "I develop with AI agents as a tool. Every task starts with a plan I approve and with fixed rules "
                + "in the repository: architecture, test strategy and code style are written down in AGENTS.md and "
                + "the documents under docs/.\n\n"
                + "Agents search the code, write tests and implementation by those rules and run the checks. "
                + "Architecture, data model, security and the acceptance of every change stay with me.\n\n"
                + "This site was built that way. Its source code shows what that looks like day to day."
                + "' WHERE \"Locale\" = 'en' AND \"WorkingMode\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkingMode",
                table: "profile_translations");
        }
    }
}
