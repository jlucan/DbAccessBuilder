USE [Suites]
GO
/****** Object:  StoredProcedure [dbo].[CreateDbColumnLists]    Script Date: 1/22/2015 2:44:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =========================================================================3===
-- Author:		Jim Lucan
-- Create date: 1-20-2015
-- Description:	Creates table column listings that can be used to create C# code
-- ==============================================================================
ALTER PROCEDURE [dbo].[CreateDbColumnLists]
AS
BEGIN
	SET NOCOUNT ON;

	WITH TableColumns AS (

	SELECT
		TABLE_SCHEMA
		,TABLE_NAME
		,REPLACE(COLUMN_NAME, ' ', '_') AS ColumnName
		,ORDINAL_POSITION
		,IS_NULLABLE
		,DATA_TYPE
		,CHARACTER_MAXIMUM_LENGTH AS MaxLen
		,CASE 
			WHEN DATA_TYPE = 'int' AND IS_NULLABLE = 'NO' THEN 'int'
			WHEN DATA_TYPE = 'int' AND IS_NULLABLE = 'YES' THEN 'int?'
			WHEN DATA_TYPE = 'smallint' AND IS_NULLABLE = 'NO' THEN 'int'
			WHEN DATA_TYPE = 'smallint' AND IS_NULLABLE = 'YES' THEN 'int?'
			WHEN DATA_TYPE = 'bigint' AND IS_NULLABLE = 'NO' THEN 'long'
			WHEN DATA_TYPE = 'bigint' AND IS_NULLABLE = 'YES' THEN 'long?'
			WHEN DATA_TYPE = 'tinyint' AND IS_NULLABLE = 'NO' THEN 'byte'
			WHEN DATA_TYPE = 'tinyint' AND IS_NULLABLE = 'YES' THEN 'byte?'
			WHEN DATA_TYPE = 'varchar' THEN 'string'
			WHEN DATA_TYPE = 'nvarchar' THEN 'string'
			WHEN DATA_TYPE = 'char' THEN 'string'
			WHEN DATA_TYPE = 'nchar' THEN 'string'
			WHEN DATA_TYPE = 'bit' AND IS_NULLABLE = 'NO' THEN 'bool'
			WHEN DATA_TYPE = 'bit' AND IS_NULLABLE = 'YES' THEN 'bool?'
			WHEN DATA_TYPE = 'datetime' AND IS_NULLABLE = 'NO' THEN 'DateTime'
			WHEN DATA_TYPE = 'datetime' AND IS_NULLABLE = 'YES' THEN 'DateTime?'
			WHEN DATA_TYPE = 'datetime2' AND IS_NULLABLE = 'NO' THEN 'DateTime'
			WHEN DATA_TYPE = 'datetime2' AND IS_NULLABLE = 'YES' THEN 'DateTime?'
			WHEN DATA_TYPE = 'smalldatetime' AND IS_NULLABLE = 'NO' THEN 'DateTime'
			WHEN DATA_TYPE = 'smalldatetime' AND IS_NULLABLE = 'YES' THEN 'DateTime?'
			WHEN DATA_TYPE = 'date' AND IS_NULLABLE = 'NO' THEN 'DateTime'
			WHEN DATA_TYPE = 'date' AND IS_NULLABLE = 'YES' THEN 'DateTime?'
			WHEN DATA_TYPE = 'money' AND IS_NULLABLE = 'NO' THEN 'Decimal'
			WHEN DATA_TYPE = 'money' AND IS_NULLABLE = 'YES' THEN 'Decimal?'
			WHEN DATA_TYPE = 'smallmoney' AND IS_NULLABLE = 'NO' THEN 'Decimal'
			WHEN DATA_TYPE = 'smallmoney' AND IS_NULLABLE = 'YES' THEN 'Decimal?'
			WHEN DATA_TYPE = 'decimal' AND IS_NULLABLE = 'NO' THEN 'Decimal'
			WHEN DATA_TYPE = 'decimal' AND IS_NULLABLE = 'YES' THEN 'Decimal?'
			WHEN DATA_TYPE = 'float' AND IS_NULLABLE = 'NO' THEN 'double'
			WHEN DATA_TYPE = 'float' AND IS_NULLABLE = 'YES' THEN 'double?'
			WHEN DATA_TYPE = 'real' AND IS_NULLABLE = 'NO' THEN 'float'
			WHEN DATA_TYPE = 'real' AND IS_NULLABLE = 'YES' THEN 'float?'
			WHEN DATA_TYPE = 'uniqueidentifier' AND IS_NULLABLE = 'NO' THEN 'Guid'
			WHEN DATA_TYPE = 'uniqueidentifier' AND IS_NULLABLE = 'YES' THEN 'Guid?'
			WHEN DATA_TYPE = 'binary' THEN 'byte[]' 
			WHEN DATA_TYPE = 'timestamp' THEN 'byte[]' 
		END AS DotNetDataType

	FROM INFORMATION_SCHEMA.COLUMNS
	)

	SELECT
		TABLE_SCHEMA + '.' + TABLE_NAME
		,'private ' + ISNULL(DotNetDataType, '???') + ' _' + ColumnName + ';'
			+ CASE WHEN MaxLen IS NOT NULL THEN '    // Max length = ' + CONVERT(varchar(10), MaxLen) ELSE '' END
		,'public ' + ISNULL(DotNetDataType, '???') + ' ' + ColumnName + ' { get { return _' + ColumnName + '; } set { if (' + '_' + ColumnName + ' != value) { ' + '_' + ColumnName + ' = value; this.modified = true; }; } }'
		,'this._' + ColumnName + ' = dbResult.' + ColumnName + ';'
	
	FROM TableColumns
	ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION
END
