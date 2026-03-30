using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace sqlite_cli
{
    class Program
    {
        static string connStr = "Data Source=Database.sqlite;Version=3;";

        static string defaultTable = "";
   
        const char useTable = '1',
            createTable = '2',
            readFromTable = '3',
            readFromTableWhere = '4',
            readWholeTable = '5',
            insertIntoTable = '6',
            insertDuplicate = '7',
            deleteFromTable = '8',
            update = '9',
            updateWholeTable = 'u',
            dropTable = 'd',
            truncateTable = 't',
            checkTableForDuplicates = 'c',
            viewColumns = 'n',
            viewPKColumn = 'p',
            clrScreen = 's',
            executeNonQuery = 'e';

        static char[] options = new char[] {
            useTable,
            createTable,
            readFromTable,
            readFromTableWhere,
            readWholeTable,
            insertIntoTable,
            insertDuplicate,
            deleteFromTable,
            update,
            updateWholeTable,
            dropTable,
            truncateTable,
            checkTableForDuplicates,
            viewColumns,
            viewPKColumn,
            clrScreen,
            executeNonQuery
        };

        static string [] optionsDescriptions = new string[] {
            "Use existing table",
            "Create table",
            "Read from table",
            "Read from table with WHERE conditions",
            "Read entire table",
            "Insert into table",
            "Insert same row multiple times",
            "Delete from table",
            "UPDATE",
            "UPDATE whole table",
            "DROP TABLE",
            "TRUNCATE TABLE",
            "Check table for duplicate rows",
            "View columns of table",
            "View Primary Key column of table",
            "Clear console screen",
            "Execute non-query command"
        };

        static void DisplayRegularMessage(string message)
        {
            Console.WriteLine("\n" + message);
        }

        static void DisplaySuccessMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            DisplayRegularMessage(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void DisplayWarningMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            DisplayRegularMessage(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void DisplayErrorMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            DisplayRegularMessage(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        static bool ynHelper(string message, bool warning)
        {
            if (warning) DisplayWarningMessage(message + " <y/n>");
            else DisplayRegularMessage(message + " <y/n>");

            ConsoleKey ck;
            try
            {
                do
                {
                    ck = ReadKeyWithEscape().Key;
                }
                while (ck != ConsoleKey.Y && ck != ConsoleKey.N);

                if (ck == ConsoleKey.N) return false;
                else if (ck == ConsoleKey.Y) return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            return false;
        }

        //--

        static void CreateTableIfNotExists(SQLiteConnection conn, string TableName, List<string> Columns)
        {
            try
            {
                string command = string.Format("CREATE TABLE IF NOT EXISTS {0} ({1}) STRICT;", TableName, string.Join(", ", Columns));
                new SQLiteCommand(command, conn).ExecuteNonQuery();
                DisplaySuccessMessage("Command executed successfully");
            }
            catch
            {
                throw;
            }
        }

        static string ReadFromTable(SQLiteConnection conn, bool Write, string TableName, List<string> Columns)
        {
            string returnString = "";

            string command = string.Format("SELECT {0} FROM {1};", string.Join(", ", Columns), TableName);

            try
            {
                using (SQLiteDataReader reader = new SQLiteCommand(command, conn).ExecuteReader())
                {
                    if (Write) Console.WriteLine("\n");

                    while (reader.Read())
                    {
                        for (int i = 0; i < Columns.Count; i++)
                        {
                            string value = "";

                            if (reader.GetFieldType(i) == typeof(string))
                                value = "'" + reader[i].ToString() + "'";
                            else
                                value = reader[i].ToString();

                            string line = Columns[i] + ": " + value + "\n";

                            returnString += line;

                            if (Write)
                            {
                                Console.Write(line);

                                if ((i+1)%Columns.Count == 0)
                                {
                                    Console.WriteLine();
                                }
                            }
                        }
                    }
                }
            }

            catch
            {
                throw;
            }

            if (Write)
                DisplaySuccessMessage("Command executed successfully");

            return returnString;
        }

        static string ReadFromTableWhere(SQLiteConnection conn, bool Write, string TableName, List<string> ColumnsToRead, List<string> ColumnsForWHERE, List<string> ValuesForWHERE)
        {
            string returnString = "";

            List<string> equals = new List<string>(ColumnsForWHERE.Count);

            for (int i = 0; i < ColumnsForWHERE.Count; i++)
            {
                equals.Add(string.Format("{0}={1}", ColumnsForWHERE[i], ValuesForWHERE[i]));
            }

            string command = string.Format("SELECT {0} FROM {1} WHERE {2};", string.Join(", ", ColumnsToRead), TableName, string.Join(" AND ", equals));

            try
            {
                using (SQLiteDataReader reader = new SQLiteCommand(command, conn).ExecuteReader())
                {
                    if (Write) Console.WriteLine("\n");

                    while (reader.Read())
                    {
                        for (int i = 0; i < ColumnsToRead.Count; i++)
                        {
                            string value = "";

                            if (reader.GetFieldType(i) == typeof(string))
                                value = "'" + reader[i].ToString() + "'";
                            else
                                value = reader[i].ToString();

                            string line = ColumnsToRead[i] + ": " + value + "\n";

                            returnString += line;

                            if (Write)
                            {
                                Console.Write(line);

                                if ((i+1)%ColumnsToRead.Count == 0)
                                {
                                    Console.WriteLine();
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                throw;
            }

            if (Write)
                DisplaySuccessMessage("Command executed successfully");

            return returnString;
        }

        static void InsertIntoTable(SQLiteConnection conn, string TableName, List<string> Columns, List<string> Values, bool Write)
        {
            try
            {
                string command = string.Format("INSERT INTO {0} ({1}) VALUES ({2});", TableName, string.Join(", ", Columns), string.Join(", ", Values));

                new SQLiteCommand(command, conn).ExecuteNonQuery();
                if (Write)
                    DisplaySuccessMessage("Successfully inserted row.");
            }
            catch
            {
                throw;
            }
        }

        static void DeleteRowFromTable(SQLiteConnection conn, string TableName, List<string> Columns, List<string> Values, bool Write)
        {
            List<string> equals = new List<string>(Columns.Count);

            for (int i = 0; i < Columns.Count; i++)
            {
                equals.Add(string.Format("{0}={1}", Columns[i], Values[i]));
            }

            string command = string.Format("DELETE FROM {0} WHERE {1};", TableName, string.Join(" AND ", equals));

            try
            {
                new SQLiteCommand(command, conn).ExecuteNonQuery();

                if (Write)
                    DisplaySuccessMessage("Successfully deleted row");
            }
            catch
            {
                throw;
            }
        }

        static void DropTable(SQLiteConnection conn, string TableName)
        {
            try
            {
                if (!ynHelper("Are you sure you want to DROP TABLE " + TableName + "?", true)) { throw new OperationCanceledException(); }
 
                new SQLiteCommand("DROP TABLE " + TableName + ";", conn).ExecuteNonQuery();
                DisplaySuccessMessage("successfully dropped table");
            }
            catch
            {
                throw;
            }
        }

        static void TruncateTable(SQLiteConnection conn, string TableName)
        {
            try
            {
                if (!ynHelper("Are you sure you want to TRUNCATE TABLE " + TableName + "?", true)) { throw new OperationCanceledException(); }

                new SQLiteCommand("DELETE FROM " + TableName + ";", conn).ExecuteNonQuery();
                DisplaySuccessMessage("successfully truncated table.");
            }
            catch
            {
                throw;
            }
        }

        static List<string> ListColumnsOfTable(SQLiteConnection conn, string TableName, bool Write, bool returnOnlyName, bool checkOnlyPK)
        {
            List<string> columns = new List<string>();

            try
            {
                using (SQLiteDataReader datareader = new SQLiteCommand("PRAGMA table_info(" + TableName + ");", conn).ExecuteReader())
                {
                    while (datareader.Read())
                    {
                        string column = "";

                        if (checkOnlyPK)
                        {
                            if (Convert.ToInt32(datareader["pk"]) == 1)
                            {
                                column += datareader["name"].ToString() + " ";

                                if (!returnOnlyName)
                                {
                                    column += datareader["type"].ToString() + " ";
                                    column += "PRIMARY KEY";
                                }

                                if (Write) Console.WriteLine(column);

                                return new List<string> { column };
                            }
                        }

                        else
                        {
                            column += datareader["name"].ToString() + " ";

                            if (!returnOnlyName)
                            {
                                column += datareader["type"].ToString() + " ";

                                if (Convert.ToInt32(datareader["pk"]) == 1)
                                    column += "PRIMARY KEY";
                                else if (Convert.ToInt32(datareader["notnull"]) == 1)
                                    column += "NOT NULL";
                            }

                            columns.Add(column);

                            if (Write) Console.WriteLine(column);
                        }
                    }
                }
            }
            catch
            {
                throw;
            }

            if (Write)
                DisplaySuccessMessage("Command executed successfully");

            return columns;
        }

        static void CheckTableForDuplicateRows(SQLiteConnection conn, string TableToCheck)
        {

            //Find all columns of table, remove the primary key column
            List<string> ColumnsToCheck = new List<string>(ListColumnsOfTable(conn, TableToCheck, false, true, false));

            string PKColumn = ListColumnsOfTable(conn, TableToCheck, false, true, true)[0];

            if (isInputEmpty(PKColumn))
            {
                DisplayWarningMessage("Table doesn't have a Primary Key column. Aborting...");
                throw new OperationCanceledException();
            }

            ColumnsToCheck.Remove(PKColumn);
            //

            //Read the table, organize read lines into "rows"
            string read = ReadFromTable(conn, false, TableToCheck, ColumnsToCheck);
            
            string[] lines = read.Split(new string[] {"\n"}, StringSplitOptions.None);

            int numTotalRows = lines.Length/ColumnsToCheck.Count;

            List<string> rows = new List<string>(numTotalRows);

            for (int i = 0; i < numTotalRows; i++)
            {
                for (int j = 0; j < ColumnsToCheck.Count; j++)
                {
                    rows.Add(string.Join("\n", lines[(i * ColumnsToCheck.Count) + j]));
                }
            }
            //

            //Check each row against all other rows in the table
            int deletedRows = 0;
            for (int i = 0; i < rows.Count - 1; i++)
            {
                for (int j = i + 1; j < rows.Count; j++)
                {
                    if (rows[i] == rows[j])
                    {
                        //Find the values of each column in each row
                        List<string> values = new List<string>(ColumnsToCheck);
                        string[] rowLines = rows[j].Split(new string [] {"\n"}, StringSplitOptions.None);

                        for (int k = 0; k < values.Count; k++)
                        {
                            values[k] = rowLines[k].Split(new string[] { ": " }, StringSplitOptions.None)[1];
                        }
                        //

                        //Find the Primary Key value of duplicate rows so we can delete the second one from the table
                        string[] PKLines = ReadFromTableWhere(conn, false, TableToCheck, new List<string> {PKColumn}, ColumnsToCheck, values).Split(new string[] {"\n"}, StringSplitOptions.None);
                        string PKLineSecondRow = PKLines[1];

                        string PKValueSecondRow = PKLineSecondRow.Split(new string[] {": "}, StringSplitOptions.None)[1];
                        //

                        //Delete the duplicate row and adjust the rows array so that we the for loops dont break
                        try
                        {
                            DeleteRowFromTable(conn, TableToCheck, new List<string> { PKColumn }, new List<string> { PKValueSecondRow }, false);

                            rows.RemoveAt(j);
                            j--;

                            deletedRows++;
                        }
                        catch
                        {
                            throw;
                        }
                        //
                    }
                }
            }

            DisplaySuccessMessage("Successfully deleted " + deletedRows + " rows.");
        }

        static string ReadWholeTable(SQLiteConnection conn, string TableName, bool Write)
        {
            try
            {
                List<string> Columns = ListColumnsOfTable(conn, TableName, false, true, false);

                return ReadFromTable(conn, Write, TableName, Columns);
            }
            catch
            {
                throw;
            }
        }

        static void Update(SQLiteConnection conn, string TableName, List<string> ColumnsToChange, List<string> ValuesNew, List<string> ColumnsForWHERE, List<string> ValuesForWHERE)
        {
            List<string> equalsToChange = new List<string>(ColumnsToChange.Count);

            for (int i = 0; i < ColumnsToChange.Count; i++)
            {
                equalsToChange.Add(string.Format("{0}={1}", ColumnsToChange[i], ValuesNew[i]));
            }

            List<string> equalsForWHERE = new List<string>(ColumnsForWHERE.Count);

            for (int i = 0; i < ColumnsForWHERE.Count; i++)
            {
                equalsForWHERE.Add(string.Format("{0}={1}", ColumnsForWHERE[i], ValuesForWHERE[i]));
            }

            string command = string.Format("UPDATE {0} SET {1} WHERE {2}", TableName, string.Join(", ", equalsToChange), string.Join(" AND ", equalsForWHERE));

            try
            {
                new SQLiteCommand(command, conn).ExecuteNonQuery();
                DisplaySuccessMessage("Command executed successfully");
            }
            catch
            {
                throw;
            }
        }

        static void UpdateWholeTable(SQLiteConnection conn, string TableName, List<string> Columns, List<string> Values)
        {
            List<string> equals = new List<string>(Columns.Count);

            for (int i = 0; i < Columns.Count; i++)
            {
                equals.Add(string.Format("{0}={1}", Columns[i], Values[i]));
            }

            string command = string.Format("UPDATE {0} SET {1}", TableName, string.Join(", ", equals));

            try
            {
                new SQLiteCommand(command, conn).ExecuteNonQuery();
                DisplaySuccessMessage("Command executed successfully");
            }
            catch
            {
                throw;
            }
        } 

        //-

        static bool isInputEmpty(string input)
        {
            return (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input));
        }

        static bool isInputNumber(string input)
        {
            if (isInputEmpty(input))
            {
                DisplayWarningMessage("Please enter a number or press ESC to cancel");
                return false;
            }

            for (int i = 0; i < input.Length; i++)
            {
                if (!char.IsDigit(input[i]))
                {
                    DisplayWarningMessage("Please enter a number or press ESC to cancel");
                    return false;
                }
            }

            return true;
        }

        static int AskUserForNumberOfItems(bool col)
        {
            string input;

            do 
            {
                DisplayRegularMessage("Number of " + ((col) ? "columns" : "rows") + ": ");
                input = ReadLineWithEscape();
            }
            while (!isInputNumber(input));

            return int.Parse(input);
        }

        static List<string> AskUserForStrArray(bool askForColumnName, List<string> columnNames, bool AskUserForNumItems, int? numItems)
        {
            if (AskUserForNumItems) numItems = AskUserForNumberOfItems(askForColumnName);

            List<string> strArray = new List<string>();

            try
            {
                for (int i = 0; i < numItems; i++)
                {
                    string str;

                    if (askForColumnName)
                        if (numItems > 1)
                            str = (i + 1) + ". column:";
                        else
                            str = "column:";
                    else
                        str = columnNames[i] + "=";

                ask:
                    DisplayRegularMessage(str);

                    string input = ReadLineWithEscape();

                    if (isInputEmpty(input))
                    {
                        DisplayWarningMessage("Please enter a value or press ESC to cancel.");
                        goto ask;
                    }

                    strArray.Add(input);
                }
            }
            catch
            {
                throw;
            }

            return strArray;
        }

        //-

        static void SetCurrentTable(SQLiteConnection conn, bool user)
        {
            if (!user && !isInputEmpty(defaultTable)) return;

            List<string> tables = new List<string>();

            try
            {
                using (SQLiteDataReader reader = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table';", conn).ExecuteReader())
                {

                    int i = 1;
                    while (reader.Read())
                    {
                        tables.Add(reader[0].ToString());
                        Console.WriteLine("[{0}] {1}", i, reader[0]);
                        i++;
                    }
                }

                if (tables.Count == 0)
                {
                    DisplayWarningMessage("No tables found");

                    DisplayRegularMessage("Create the table to be used during this session. (You can change it any time)");

                    DisplayWarningMessage("CREATING TABLE");
                asktablename:
                    DisplayRegularMessage("Enter table name to create: ");
                    defaultTable = ReadLineWithEscape();

                    if (isInputEmpty(defaultTable))
                    {
                        DisplayWarningMessage("Please enter a table name");
                        goto asktablename;
                    }

                    DisplayWarningMessage("Declare columns");
                    List<string> columns = AskUserForStrArray(true, null, true, null);

                    CreateTableIfNotExists(conn, defaultTable, columns);

                    return;
                }

                DisplayRegularMessage("The table you choose will be marked as default for this session\nyou can change it anytime by the menu option");
                asktablenumber:
                DisplayRegularMessage("Enter the number of the table you want to choose: ");

                string input = ReadLineWithEscape();

                if (!isInputNumber(input)) goto asktablenumber;

                int selected_table = int.Parse(input) - 1;

                if (selected_table < 0 || selected_table >= tables.Count)
                {
                    DisplayWarningMessage("invalid selection.");
                    goto asktablenumber;
                }

                defaultTable = tables[selected_table];
            }
            catch
            {
                throw;
            }
        }
        
        //-

        static string ReadLineWithEscape()
        {
            StringBuilder input = new StringBuilder();
            int cursorPos = 0;

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    throw new OperationCanceledException();
                }

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return input.ToString();
                }

                if (key.Key == ConsoleKey.LeftArrow)
                {
                    if (cursorPos > 0)
                    {
                        cursorPos--;
                        Console.CursorLeft--;
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.RightArrow)
                {
                    if (cursorPos < input.Length)
                    {
                        cursorPos++;
                        Console.CursorLeft++;
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (cursorPos > 0)
                    {
                        input.Remove(cursorPos - 1, 1);
                        cursorPos--;

                        RedrawLine(input, cursorPos);
                    }
                    continue;
                }

                // Ignore non-character keys
                if (char.IsControl(key.KeyChar))
                    continue;

                // Insert at cursor position
                input.Insert(cursorPos, key.KeyChar);
                cursorPos++;

                RedrawLine(input, cursorPos);
            }
        }

        static void RedrawLine(StringBuilder input, int cursorPos)
        {
            int currentLineCursor = Console.CursorLeft;

            // Move to start of input
            Console.CursorLeft = 0;

            // Rewrite whole line
            Console.Write(input.ToString() + " ");

            // Restore cursor position
            Console.CursorLeft = cursorPos;
        }

        static ConsoleKeyInfo ReadKeyWithEscape(bool intercept = true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept);

            if (key.Key == ConsoleKey.Escape)
            {
                throw new OperationCanceledException();
            }

            return key;
        }

        //-

        static void Main()
        {
            Console.Title = "SQLite CLI";
            Console.ForegroundColor = ConsoleColor.White;

            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                conn.Open();

                start:
                try
                {
                    Console.WriteLine();

                    for (int i = 0; i < options.Length; i++)
                    {
                        Console.WriteLine("[{0}] {1}", options[i], optionsDescriptions[i]);
                    }
                    Console.WriteLine("[ESC] Cancel out of an operation\n");
                    
                    char ck = ReadKeyWithEscape().KeyChar;
                    ck = char.ToLower(ck);

                    bool validSelection = false;

                    foreach (char option in options)
                    {
                        if (ck == option)
                        {
                            validSelection = true;
                            break;
                        }
                    }

                    if (!validSelection)
                    {
                        DisplayErrorMessage("Invalid selection");
                        goto start;
                    }

                    if (ck == useTable) SetCurrentTable(conn, true);
                    else if (ck != clrScreen && ck != createTable && ck != executeNonQuery) SetCurrentTable(conn, false);

                    switch (ck)
                    {
                        case createTable:

                            DisplayWarningMessage("CREATING TABLE");

                            asktablename:
                            DisplayRegularMessage("Enter table name to create: ");
                            string table_name = ReadLineWithEscape();

                            if (isInputEmpty(table_name))
                            {
                                DisplayWarningMessage("Please enter a name or press ESC to cancel");
                                goto asktablename;
                            }

                            DisplayWarningMessage("Declare columns");
                            List<string> columns = AskUserForStrArray(true, null, true, null);

                            CreateTableIfNotExists(conn, table_name, columns);

                            break;

                        case readFromTable:

                            DisplayWarningMessage("READING FROM TABLE " + defaultTable);
                            DisplayRegularMessage("Specify the columns you want to read");

                            columns = AskUserForStrArray(true, null, true, null);

                            ReadFromTable(conn, true, defaultTable, columns);

                            break;

                        case readFromTableWhere:

                            DisplayWarningMessage("READING FROM TABLE " + defaultTable);

                            DisplayRegularMessage("Specify the columns you want to read");
                            List<string> colsToRead = AskUserForStrArray(true, null, true, null);

                            DisplayRegularMessage("Specify the columns to be included in the search query");
                            List<string> colsConditions = AskUserForStrArray(true, null, true, null);

                            List<string> values = new List<string>();
                            
                            DisplayRegularMessage("Specify the values to search for");
                            
                            for (int i = 0; i < colsConditions.Count; i++)
                            {
                                DisplayRegularMessage(colsConditions[i] + "=");
                                values.Add(ReadLineWithEscape());
                            }
                            
                            ReadFromTableWhere(conn, true, defaultTable, colsToRead, colsConditions, values);
                            
                            break;

                        case readWholeTable:

                            DisplayWarningMessage("READING ALL ROWS FROM TABLE " + defaultTable);
                            ReadWholeTable(conn, defaultTable, true);
                            
                            break;

                        case insertIntoTable:

                            DisplayWarningMessage("INSERTING INTO TABLE " + defaultTable);

                            DisplayRegularMessage("Specify how many rows to insert");
                            int numRows = AskUserForNumberOfItems(false);

                            DisplayRegularMessage("Specify columns to insert");
                            columns = AskUserForStrArray(true, null, true, null);

                            for (int i = 0; i < numRows; i++)
                            {
                                DisplayRegularMessage("Specify values to insert");
                                values = AskUserForStrArray(false, columns, false, columns.Count);

                                InsertIntoTable(conn, defaultTable, columns, values, true);
                            }

                            break;

                        case insertDuplicate:
                            DisplayWarningMessage("INSERTING DUPLICATE ROWS INTO TABLE " + defaultTable);

                            DisplayRegularMessage("Specify how many times (rows) to insert"); 
                            numRows = AskUserForNumberOfItems(false);

                            DisplayRegularMessage("Specify columns to insert");
                            columns = AskUserForStrArray(true, null, true, null);

                            DisplayRegularMessage("Specify values to insert");
                            values = AskUserForStrArray(false, columns, false, columns.Count);

                            for (int i = 0; i < numRows; i++)
                            {
                                InsertIntoTable(conn, defaultTable, columns, values, false);
                            }

                            DisplaySuccessMessage("Successfully inserted " + numRows + " rows");

                            break;
                            
                        case deleteFromTable:

                            DisplayWarningMessage("DELETING FROM TABLE " + defaultTable);

                            DisplayRegularMessage("Specify which columns will be in the WHERE statement");
                            columns = AskUserForStrArray(true, null, true, null);
                            
                            DeleteRowFromTable(conn, defaultTable, columns, AskUserForStrArray(false, columns, false, columns.Count), true);
                            
                            break;

                        case update:

                            DisplayWarningMessage("UPDATING TABLE " + defaultTable);

                            DisplayRegularMessage("Specify which columns to update:");
                            List<string> ColumnsToChange = AskUserForStrArray(true, null, true, null);

                            DisplayRegularMessage("Specify new values for the columns:");
                            List<string> ValuesNew = AskUserForStrArray(false, ColumnsToChange, false, ColumnsToChange.Count);

                            DisplayRegularMessage("Specify which columns will be in the WHERE statement:");
                            List<string> ColumnsForWHERE = AskUserForStrArray(true, null, true, null);

                            DisplayRegularMessage("Specify which values will be checked:");
                            List<string> ValuesForWHERE = AskUserForStrArray(false, ColumnsForWHERE, false, ColumnsForWHERE.Count);

                            Update(conn, defaultTable, ColumnsToChange, ValuesNew, ColumnsForWHERE, ValuesForWHERE);

                            break;

                        case updateWholeTable:

                            if (!ynHelper("This action will update all rows within the table. Proceed?", true)) { throw new OperationCanceledException(); }

                            DisplayWarningMessage("UPDATING TABLE " + defaultTable);

                            DisplayRegularMessage("Specify which columns to update:");
                            columns = AskUserForStrArray(true, null, true, null);

                            DisplayRegularMessage("Specify new values for the columns:");
                            List<string> Values = AskUserForStrArray(false, columns, false, columns.Count);

                            UpdateWholeTable(conn, defaultTable, columns, Values);

                            break;

                        case dropTable:

                            DisplayErrorMessage("DROPPING TABLE " + defaultTable);

                            DropTable(conn, defaultTable);
                            defaultTable = null;

                            break;

                        case truncateTable:

                            DisplayErrorMessage("TRUNCATING TABLE " + defaultTable);

                            TruncateTable(conn, defaultTable);

                            break;

                        case checkTableForDuplicates:

                            DisplayWarningMessage("CHECKING TABLE " + defaultTable + " FOR DUPLICATES");

                            if (!ynHelper("This operation will check the table " + defaultTable + " for rows that have the same values except for their Primary Key field and delete them. Proceed?", true))
                                break;

                            CheckTableForDuplicateRows(conn, defaultTable);

                            break;

                        case viewColumns:

                            DisplayWarningMessage("VIEWING COLUMNS OF TABLE " + defaultTable);

                            ListColumnsOfTable(conn, defaultTable, true, false, false);
                            
                            break;

                        case viewPKColumn:

                            DisplayWarningMessage("VIEWING PRIMARY KEY COLUMN OF TABLE " + defaultTable);
                            
                            ListColumnsOfTable(conn, defaultTable, true, false, true);
                            
                            break;

                        case clrScreen:
                            Console.Clear();
                            break;

                        case executeNonQuery:

                            DisplayWarningMessage("EXECUTING NON-QUERY COMMAND");

                            string command = "";
                            DisplayRegularMessage("Command: ");
                            command = ReadLineWithEscape();

                            new SQLiteCommand(command, conn).ExecuteNonQuery();

                            DisplaySuccessMessage("Command Executed Successfully.");

                            break;
                            
                        default:
                            break;
                    }

                    goto start;
                }
                catch (Exception e)
                {
                    DisplayErrorMessage(e.Message);
                    goto start;
                }
            }
        }
    }
}
