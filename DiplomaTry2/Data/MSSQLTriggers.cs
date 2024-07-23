using System;
using System.Data.SqlClient;

using DevExpress.Xpo;

namespace DiplomaTry2.Data
{
    public class MSSQLTriggers
    {
        private readonly string _connectionString;

        
        SqlCommand ChangeLogTrigger(string tbName)
        {
           
            
                SqlCommand command = new SqlCommand();
                command.CommandText = $@"
            CREATE TRIGGER trgUpdateLastModified_{tbName}
            ON EventsSuccessfulPrinting
            AFTER INSERT, UPDATE, DELETE
            AS
            BEGIN
                SET NOCOUNT ON;
                
                IF EXISTS (SELECT * FROM ChangeLog WHERE TableName = '{tbName}')
                BEGIN
                    UPDATE ChangeLog
                    SET LastModified = GETDATE()
                    WHERE TableName = '{tbName}';
                END
                ELSE
                BEGIN
                    INSERT INTO ChangeLog (TableName, LastModified)
                    VALUES ('{tbName}', GETDATE());
                END
            END;
                     ";
          
            return command;
        }
        //async Task AddTriggers(string connectionString)
        //{
        //    using (SqlConnection connection = new SqlConnection(connectionString))
        //    {

        //        command=ChangeLogTrigger("");
        //    }
        //    }
    }

}