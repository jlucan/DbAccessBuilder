USE [XXXX]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Jim Lucan

-- =============================================
CREATE PROCEDURE [dbo].[GetTableDefs]
AS
BEGIN

	SET NOCOUNT ON;


	--SELECT cu.CONSTRAINT_NAME, cu.COLUMN_NAME
	--FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE cu
	--WHERE EXISTS
	--	( SELECT tc.*
	--	  FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
	--	  WHERE tc.CONSTRAINT_CATALOG = @databasename AND tc.TABLE_NAME = @tablename
	--	  AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND tc.CONSTRAINT_NAME = cu.CONSTRAINT_NAME );


	DECLARE @ColumDefs TABLE
	(
		TableSchema nvarchar(128),
		TableName nvarchar(128),
		ColumnName nvarchar(128),
		SqlType nvarchar(128),
		[Len] nvarchar(10),
		Nullable nchar,
		ColAttrib nvarchar(10)
	)

	DECLARE @TableSchema nvarchar(128), @TableName nvarchar(128);
	DECLARE @ColName nvarchar(128), @DataType nvarchar(128),@CharLength int, @Nullable bit;

	DECLARE tableCursor CURSOR FAST_FORWARD FOR 
		SELECT TABLE_SCHEMA, TABLE_NAME
		FROM INFORMATION_SCHEMA.TABLES
		WHERE TABLE_TYPE = 'BASE TABLE'
		ORDER BY TABLE_SCHEMA, TABLE_NAME;
	OPEN tableCursor;
	FETCH NEXT FROM tableCursor INTO @TableSchema, @TableName;
	WHILE @@FETCH_STATUS = 0 BEGIN

		DECLARE colCursor CURSOR FAST_FORWARD FOR 
			SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, CASE IS_NULLABLE WHEN 'YES' THEN 1 ELSE 0 END
			FROM INFORMATION_SCHEMA.COLUMNS
			WHERE TABLE_SCHEMA = @TableSchema AND TABLE_NAME = @TableName
			ORDER BY ORDINAL_POSITION
		OPEN colCursor;
		FETCH NEXT FROM colCursor INTO @ColName, @DataType, @CharLength, @Nullable;
		WHILE @@FETCH_STATUS = 0 BEGIN
			INSERT INTO @ColumDefs
				(TableSchema, TableName, ColumnName, SqlType, [Len], Nullable, ColAttrib)
			VALUES
				(@TableSchema, @TableName, @ColName, @DataType, CASE WHEN @CharLength IS NULL THEN '' ELSE CONVERT (nvarchar(10), @CharLength) END, CASE WHEN @Nullable = 1 THEN 'T' ELSE 'F' END, 'RW' );
			FETCH NEXT FROM colCursor INTO @ColName, @DataType, @CharLength, @Nullable;
		END
		CLOSE colCursor;
		DEALLOCATE colCursor;
		FETCH NEXT FROM tableCursor INTO @TableSchema, @TableName;
	END
	CLOSE tableCursor;
	DEALLOCATE tableCursor;
	SELECT * FROM @ColumDefs FOR XML AUTO;
END

GO


