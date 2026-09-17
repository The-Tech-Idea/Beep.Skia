using System.Collections.Generic;
using System.Linq;
using Beep.Skia.ERD;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for schema comparison and forward/rollback migration script generation.
    /// </summary>
    public class SchemaComparisonTests
    {
        private static DDLImporter.TableInfo Table(string name, params DDLImporter.ColumnInfo[] columns)
        {
            return new DDLImporter.TableInfo
            {
                TableName = name,
                Columns = columns.ToList()
            };
        }

        private static DDLImporter.ColumnInfo Column(string name, string type, bool nullable = true, bool pk = false, string defaultValue = null, int? maxLength = null)
        {
            return new DDLImporter.ColumnInfo
            {
                Name = name,
                DataType = type,
                IsNullable = nullable,
                IsPrimaryKey = pk,
                DefaultValue = defaultValue,
                MaxLength = maxLength
            };
        }

        [Fact]
        public void IdenticalSchemas_ReportNoChanges()
        {
            var schema = new List<DDLImporter.TableInfo> { Table("users", Column("id", "INT", nullable: false, pk: true)) };
            var result = new SchemaComparer().Compare(schema, schema);

            Assert.False(result.HasChanges);
            Assert.Equal("Schemas are identical.", result.Summary());
        }

        [Fact]
        public void AddedTable_GeneratesCreateAndRollbackDrop()
        {
            var source = new List<DDLImporter.TableInfo>();
            var target = new List<DDLImporter.TableInfo>
            {
                Table("orders",
                    Column("id", "INT", nullable: false, pk: true),
                    Column("total", "DECIMAL", nullable: true))
            };

            var diff = new SchemaComparer().Compare(source, target);
            Assert.Single(diff.OfKind(SchemaChangeKind.AddedTable));

            var generator = new MigrationScriptGenerator();
            var forward = generator.GenerateForward(diff);
            var rollback = generator.GenerateRollback(diff);

            Assert.Contains("CREATE TABLE \"orders\"", forward);
            Assert.Contains("\"id\" INT NOT NULL PRIMARY KEY", forward);
            Assert.Contains("DROP TABLE \"orders\";", rollback);
        }

        [Fact]
        public void AddedColumn_GeneratesAddAndRollbackDrop()
        {
            var source = new List<DDLImporter.TableInfo> { Table("orders", Column("id", "INT", nullable: false, pk: true)) };
            var target = new List<DDLImporter.TableInfo>
            {
                Table("orders",
                    Column("id", "INT", nullable: false, pk: true),
                    Column("discount", "DECIMAL", nullable: true, maxLength: null))
            };

            var diff = new SchemaComparer().Compare(source, target);
            Assert.Single(diff.OfKind(SchemaChangeKind.AddedColumn));

            var generator = new MigrationScriptGenerator();
            Assert.Contains("ADD COLUMN \"discount\" DECIMAL NULL", generator.GenerateForward(diff));
            Assert.Contains("DROP COLUMN \"discount\";", generator.GenerateRollback(diff));
        }

        [Fact]
        public void RemovedColumn_GeneratesDropAndRollbackAdd()
        {
            var source = new List<DDLImporter.TableInfo>
            {
                Table("orders",
                    Column("id", "INT", nullable: false, pk: true),
                    Column("legacy", "TEXT"))
            };
            var target = new List<DDLImporter.TableInfo> { Table("orders", Column("id", "INT", nullable: false, pk: true)) };

            var diff = new SchemaComparer().Compare(source, target);
            Assert.Single(diff.OfKind(SchemaChangeKind.RemovedColumn));

            var generator = new MigrationScriptGenerator();
            Assert.Contains("DROP COLUMN \"legacy\";", generator.GenerateForward(diff));
            Assert.Contains("ADD COLUMN \"legacy\" TEXT NULL", generator.GenerateRollback(diff));
        }

        [Fact]
        public void AlteredColumn_GeneratesAlterAndRollback()
        {
            var source = new List<DDLImporter.TableInfo>
            {
                Table("users", Column("name", "VARCHAR", nullable: true, maxLength: 50))
            };
            var target = new List<DDLImporter.TableInfo>
            {
                Table("users", Column("name", "VARCHAR", nullable: false, maxLength: 100))
            };

            var diff = new SchemaComparer().Compare(source, target);
            Assert.Single(diff.OfKind(SchemaChangeKind.AlteredColumn));

            var generator = new MigrationScriptGenerator();
            var forward = generator.GenerateForward(diff);
            var rollback = generator.GenerateRollback(diff);

            Assert.Contains("VARCHAR(100)", forward);
            Assert.Contains("VARCHAR(50)", rollback);
        }

        [Fact]
        public void ForeignKeys_DetectAddAndRemove()
        {
            var sourceTable = Table("orders", Column("id", "INT", nullable: false, pk: true), Column("user_id", "INT"));
            sourceTable.Constraints.Add(new DDLImporter.ConstraintInfo
            {
                Name = "fk_orders_users",
                Type = "FOREIGN KEY",
                Columns = "user_id",
                ReferencedTable = "users",
                ReferencedColumns = "id"
            });

            var targetTable = Table("orders", Column("id", "INT", nullable: false, pk: true), Column("user_id", "INT"));
            targetTable.Constraints.Add(new DDLImporter.ConstraintInfo
            {
                Name = "fk_orders_users",
                Type = "FOREIGN KEY",
                Columns = "user_id",
                ReferencedTable = "users",
                ReferencedColumns = "id"
            });
            targetTable.Constraints.Add(new DDLImporter.ConstraintInfo
            {
                Name = "fk_orders_regions",
                Type = "FOREIGN KEY",
                Columns = "region_id",
                ReferencedTable = "regions",
                ReferencedColumns = "id"
            });

            var diff = new SchemaComparer().Compare(new[] { sourceTable }, new[] { targetTable });
            Assert.Single(diff.OfKind(SchemaChangeKind.AddedForeignKey));

            var generator = new MigrationScriptGenerator();
            Assert.Contains("ADD CONSTRAINT", generator.GenerateForward(diff));
            Assert.Contains("DROP CONSTRAINT", generator.GenerateRollback(diff));
        }

        [Fact]
        public void SqlServerDialect_UsesBracketsAndAddWithoutColumnKeyword()
        {
            var source = new List<DDLImporter.TableInfo> { Table("orders", Column("id", "INT", nullable: false, pk: true)) };
            var target = new List<DDLImporter.TableInfo>
            {
                Table("orders", Column("id", "INT", nullable: false, pk: true), Column("discount", "DECIMAL"))
            };

            var diff = new SchemaComparer().Compare(source, target);
            var forward = new MigrationScriptGenerator().GenerateForward(diff, DDLImporter.SQLDialect.SQLServer);

            Assert.Contains("ALTER TABLE [orders] ADD [discount] DECIMAL NULL;", forward);
        }

        [Fact]
        public void MySqlDialect_UsesBackticks()
        {
            var source = new List<DDLImporter.TableInfo> { Table("orders", Column("id", "INT", nullable: false, pk: true)) };
            var target = new List<DDLImporter.TableInfo>
            {
                Table("orders", Column("id", "INT", nullable: false, pk: true), Column("discount", "DECIMAL"))
            };

            var diff = new SchemaComparer().Compare(source, target);
            var forward = new MigrationScriptGenerator().GenerateForward(diff, DDLImporter.SQLDialect.MySQL);

            Assert.Contains("ALTER TABLE `orders` ADD COLUMN `discount` DECIMAL NULL;", forward);
        }

        [Fact]
        public void EndToEnd_ParsesDdlAndGeneratesMigration()
        {
            var importer = new DDLImporter();
            var source = importer.Parse("CREATE TABLE users (id INT NOT NULL PRIMARY KEY, name VARCHAR(50) NULL);");
            var target = importer.Parse("CREATE TABLE users (id INT NOT NULL PRIMARY KEY, name VARCHAR(100) NOT NULL, email VARCHAR(200) NULL);");

            var diff = new SchemaComparer().Compare(source, target);

            Assert.Single(diff.OfKind(SchemaChangeKind.AddedColumn));
            Assert.Single(diff.OfKind(SchemaChangeKind.AlteredColumn));

            var forward = new MigrationScriptGenerator().GenerateForward(diff);
            Assert.Contains("email", forward);
            Assert.Contains("VARCHAR(100)", forward);
        }

        [Fact]
        public void UniqueConstraint_DetectedAndScripted()
        {
            var source = new List<DDLImporter.TableInfo>
            {
                Table("users", Column("id", "INT", nullable: false, pk: true), Column("email", "VARCHAR"))
            };
            var targetTable = Table("users", Column("id", "INT", nullable: false, pk: true), Column("email", "VARCHAR"));
            targetTable.Constraints.Add(new DDLImporter.ConstraintInfo
            {
                Name = "uq_users_email",
                Type = "UNIQUE",
                Columns = "email"
            });

            var diff = new SchemaComparer().Compare(source, new[] { targetTable });
            Assert.Single(diff.OfKind(SchemaChangeKind.AddedConstraint));

            var generator = new MigrationScriptGenerator();
            var forward = generator.GenerateForward(diff);
            var rollback = generator.GenerateRollback(diff);

            Assert.Contains("ADD CONSTRAINT \"uq_users_email\" UNIQUE (\"email\")", forward);
            Assert.Contains("DROP CONSTRAINT \"uq_users_email\";", rollback);
        }

        [Fact]
        public void CheckConstraint_DetectedAndScripted()
        {
            var source = new List<DDLImporter.TableInfo>
            {
                Table("users", Column("id", "INT", nullable: false, pk: true), Column("age", "INT"))
            };
            var targetTable = Table("users", Column("id", "INT", nullable: false, pk: true), Column("age", "INT"));
            targetTable.Constraints.Add(new DDLImporter.ConstraintInfo
            {
                Name = "ck_users_age",
                Type = "CHECK",
                Columns = "age >= 0"
            });

            var diff = new SchemaComparer().Compare(source, new[] { targetTable });
            Assert.Single(diff.OfKind(SchemaChangeKind.AddedConstraint));

            var forward = new MigrationScriptGenerator().GenerateForward(diff);
            Assert.Contains("CHECK (age >= 0)", forward);
        }

        [Fact]
        public void MySql_RemovedUniqueConstraint_DropsIndex()
        {
            var sourceTable = Table("users", Column("id", "INT", nullable: false, pk: true), Column("email", "VARCHAR"));
            sourceTable.Constraints.Add(new DDLImporter.ConstraintInfo
            {
                Name = "uq_users_email",
                Type = "UNIQUE",
                Columns = "email"
            });
            var target = new List<DDLImporter.TableInfo>
            {
                Table("users", Column("id", "INT", nullable: false, pk: true), Column("email", "VARCHAR"))
            };

            var diff = new SchemaComparer().Compare(new[] { sourceTable }, target);
            Assert.Single(diff.OfKind(SchemaChangeKind.RemovedConstraint));

            var forward = new MigrationScriptGenerator().GenerateForward(diff, DDLImporter.SQLDialect.MySQL);
            Assert.Contains("DROP INDEX `uq_users_email`", forward);
        }

        [Fact]
        public void CreateTable_IncludesUniqueAndCheckConstraints()
        {
            var targetTable = Table("accounts", Column("id", "INT", nullable: false, pk: true), Column("email", "VARCHAR"), Column("age", "INT"));
            targetTable.Constraints.Add(new DDLImporter.ConstraintInfo
            {
                Name = "uq_accounts_email",
                Type = "UNIQUE",
                Columns = "email"
            });
            targetTable.Constraints.Add(new DDLImporter.ConstraintInfo
            {
                Name = "ck_accounts_age",
                Type = "CHECK",
                Columns = "age >= 0"
            });

            var diff = new SchemaComparer().Compare(new List<DDLImporter.TableInfo>(), new[] { targetTable });
            var forward = new MigrationScriptGenerator().GenerateForward(diff);

            Assert.Contains("UNIQUE (\"email\")", forward);
            Assert.Contains("CHECK (age >= 0)", forward);
        }
    }
}
