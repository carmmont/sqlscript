/*

SELECT 
    CASE [type]
        WHEN 'P' THEN 'StoredProcedure'
        WHEN 'U' THEN 'Table'
        WHEN 'IF' THEN 'UserDefinedFunction'
        WHEN 'FN' THEN 'UserDefinedFunction'
        ELSE [type_desc]
    END As [TYPE]
     ,    

     '[' + S.Name + '].[' + O.Name + ']'
         AS Name,
create_date, modify_date
, DATEDIFF(n, O.modify_date, GETDATE()) As Min
, type, type_desc

FROM sys.objects O INNER JOIN sys.schemas S on S.schema_id = O.schema_id
WHERE type NOT IN('IT', 'S', 'D', 'PK', 'SQ')
--AND DATEDIFF(n, O.modify_date, GETDATE()) <= 1440
ORDER BY O.modify_date desc


 */