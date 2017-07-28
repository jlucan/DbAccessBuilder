# DbAccessBuilder
DbAccessBuilder is a Winform application that generates a database access layer written in C# that interfaces to Microsoft SQL Server.

The code that is produced contains two modules: The generated access layer and a module that contains core functionality.

Key features:
• Configured via editable XML configuration files.
• Does all CRUD operations.
• Uses stored procedures for create, read and delete operations.
• On update, only updates modified fields.
• Generates final result in three phases.
• Phase 1 generates initial editable XML configuration file containing database metadata.
• Phase 2 generates editable XML file containing data on stored procedures that implement create, read and delete operations.
• Phase 3 generates the final database access layer using XMl configuration and metadata from the database.

The database access layer contains 2 classes for each database table accessed: One that represents a row in the table and one that represents a collection of rows. The collection class contains basic access methods and is also declared as a partial class so that more functionality can be easily added.

Jim Lucan
j.lucan@att.net
