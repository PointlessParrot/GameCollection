using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static GameCollection.Extras.UtilFunctions;

namespace GameCollection.Extras
{
    static class UtilFunctions
    {
        public static double clamp(double min, double max,  double value) => Math.Min(Math.Max(value, min), max);
        public static bool inRange(double min, double max, double value) => value > min && value < max;
        public static bool inRadius(double mid, double rad, double value) => abs(mid - value) < rad;
        public static int max(int a, int b) => a  > b ? a : b;
        public static int min(int a, int b) => a < b ? a : b;
        public static int abs(int a) => a * sign(a);
        public static double abs(double a) => a * sign(a);
        public static int sign(double a) => a == 0 ? 0 : a > 0 ? 1 : -1;
        public static int positiveMod(int a, int b) => (a % b + b) % b;
    }
    
    struct Coord
    {
        public double x;
        public double y;
            
        public Coord(double xIn, double yIn) => (x, y) = (xIn, yIn);
            
        public static Coord operator +(Coord a, Coord b) => new Coord(a.x + b.x, a.y + b.y);
        public static Coord operator -(Coord a, Coord b) => new Coord(a.x - b.x, a.y - b.y);
        public static Coord operator *(Coord a, double b) => new Coord(a.x * b, a.y * b);
        public static Coord operator *(double a, Coord b) => new Coord(b.x * a, b.y * a);
        public static Coord operator /(Coord a, double b) => new Coord(a.x / b, a.y / b);
            
        public static Coord mul(Coord a, Coord b) => new Coord(a.x * b.x, a.y * b.y);
        public static Coord div(Coord a, Coord b) => new Coord(a.x / b.x, a.y / b.y);
        public static double abs(Coord a) => Math.Sqrt(a.x * a.x + a.y * a.y);

        public Coord rotate(double angle) => new Coord(x * Math.Cos(angle) - y * Math.Sin(angle), y * Math.Cos(angle) + x * Math.Sin(angle));
        
        public static implicit operator Coord((double x, double y) a) => new Coord(a.x, a.y);
        public static implicit operator (double x, double y)(Coord a) => (a.x, a.y);

        public override string ToString() => $"({x}, {y})";
    }
    
    struct IntegerCoord : IEquatable<IntegerCoord>
    {
        public int x;
        public int y;
            
        public IntegerCoord(int xIn, int yIn) => (x, y) = (xIn, yIn);

        public static IntegerCoord operator -(IntegerCoord a) => -1 * a;
        
        public static IntegerCoord operator +(IntegerCoord a, IntegerCoord b) => new IntegerCoord(a.x + b.x, a.y + b.y);
        public static IntegerCoord operator -(IntegerCoord a, IntegerCoord b) => new IntegerCoord(a.x - b.x, a.y - b.y);
        public static IntegerCoord operator *(IntegerCoord a, int b) => new IntegerCoord(a.x * b, a.y * b);
        public static IntegerCoord operator *(int a, IntegerCoord b) => new IntegerCoord(b.x * a, b.y * a);
        public static IntegerCoord operator /(IntegerCoord a, int b) => new IntegerCoord(a.x / b, a.y / b);
            
        public static IntegerCoord mul(IntegerCoord a, IntegerCoord b) => new IntegerCoord(a.x * b.x, a.y * b.y);
        public static IntegerCoord div(IntegerCoord a, IntegerCoord b) => new IntegerCoord(a.x / b.x, a.y / b.y);
            
        public static implicit operator IntegerCoord((int x, int y) a) => new IntegerCoord(a.x, a.y);
        public static implicit operator (int x, int y)(IntegerCoord a) => (a.x, a.y);

        public static implicit operator Coord(IntegerCoord a) => new Coord(a.x, a.y);
        public static explicit operator IntegerCoord(Coord a) => new IntegerCoord((int)a.x, (int)a.y);

        public static IntegerCoord north => ( 0, +1);
        public static IntegerCoord east  => (+1,  0);
        public static IntegerCoord south => ( 0, -1);
        public static IntegerCoord west  => (-1,  0);

        public static IntegerCoord consoleNorth => -north;
        public static IntegerCoord consoleEast  =>   east;
        public static IntegerCoord consoleSouth => -south;
        public static IntegerCoord consoleWest  =>   west;


        public IntegerCoord rotateCW() => new IntegerCoord(y, -x);
        public IntegerCoord rotateACW() => new IntegerCoord(-y, x);
        public IntegerCoord rotate(int quarters) => rotateInternal(positiveMod(quarters, 4));
        IntegerCoord rotateInternal(int quarters) =>
            quarters < 1 ? this : quarters < 2 ? rotateACW() : quarters < 3 ? -this : rotateCW();  
        
        public override string ToString() => $"({x}, {y})"; 
        public void Deconstruct(out int x, out int y)
        {
            x = this.x;
            y = this.y;
        }

        public static bool operator ==(IntegerCoord a, IntegerCoord b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(IntegerCoord a, IntegerCoord b) => a.x != b.x || a.y != b.y;

        public bool Equals(IntegerCoord other)
        {
            return x == other.x && y == other.y;
        }
        public override bool Equals(object obj)
        {
            return obj is IntegerCoord other && Equals(other);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }
    }
    
    static class GameScreen
    {
        static Queue<IntegerCoord> clearQueue = new Queue<IntegerCoord>();
        static char[][] output = Array.Empty<char[]>();
        static char get(IntegerCoord pos) => output[pos.y][pos.x];
        static void set(IntegerCoord pos, char value) => output[pos.y][pos.x] = value;
        
        static void clearOutput()
        {
            output = Enumerable.Repeat(viewSize.x, viewSize.y).Select(x => Enumerable.Repeat(' ', x).ToArray()).ToArray();
        }
        public static void changeViewSize(IntegerCoord size)
        {
            viewSize =  size;
            clearOutput();
        }
        static IntegerCoord viewSize;
        static int windowWidth => Console.WindowWidth;
        static int windowHeight => Console.WindowHeight;
        static IntegerCoord offset => (windowSize - viewSize) / 2; 
        
        public static async Task writeAsync() => await Task.Run(write);
        public static void clearAll() 
        {
            clearOutput();
            Console.Clear();
        }
        public static void clear()
        {
            while (clearQueue.Count > 0)
                set(clearQueue.Dequeue(), ' ');
        }
        public static void write()
        {
            // <TEMP>
            Console.CursorTop = 1;
            writeText($" ");
            // </TEMP>

            string padding = new string(' ', offset.x);
            Console.CursorTop = offset.y;
            Console.Out.WriteAsync(string.Join(Environment.NewLine, output.Select(chars => padding + string.Concat(chars))));
            
            clear();
        }

        public static bool cursorVisible
        {
            get => Console.CursorVisible; 
            set => Console.CursorVisible = value;
        }
        public static IntegerCoord windowSize
        {
            get => (windowWidth, windowHeight); 
            set => Console.SetWindowSize(value.x, value.y);
        }

        public static void queueAndKeep(IntegerCoord pos, char c) => set(pos, c);
        public static void queue(IntegerCoord pos, char c)
        {
            set(pos, c);
            clearQueue.Enqueue(pos);
        }

        public static void setHight(int y) => Console.CursorTop = y + offset.y;
        public static void newLine() => Console.WriteLine();
        public static void newLine(int count) => Console.Write(string.Concat(Enumerable.Repeat(Environment.NewLine, count)));
        public static void writeText(string line, bool centred = true, bool newLine = true)
        {
            Console.CursorLeft = centred ? (windowWidth - line.Length) / 2 : 0;
            Console.Write(line);
            GameScreen.newLine(newLine ? 1 : 0);
        }
        public static void writeText(IEnumerable<string> lines, bool centred = true)
        {
            foreach (string line in lines)
                writeText(line, centred);
        }
        public static string readLine() => Console.ReadLine();
        public static string textInput()
        {
            cursorVisible = true;
            string text = readLine();
            cursorVisible = false;
            return text;
        }
    }

    static class GameLog
    {
        static string fileName = "logFile.txt";
        static StreamWriter writer; 

        public static void createLog()
        {
            File.Delete(fileName);
            writer = new StreamWriter(File.Open(fileName, FileMode.Create));
            string topText = $"Creation Date: {DateTime.Now.ToShortDateString()}" + Environment.NewLine;
            writer.Write(topText);
        }

        public static async Task log(string message)
        {
            await writer.WriteAsync($"{DateTime.Now.ToShortTimeString()}: {message}" + Environment.NewLine);
            await writer.FlushAsync();
        }
    }
    
    static class SqlGameDatabase
    {
        const string dbName = "GameData.db";
        static SQLiteConnection conn;

        public static void setupConnection()
        {
            SQLiteConnection.CreateFile(dbName);
            conn = new SQLiteConnection($"Data Source={dbName};Version=3;New=True;Compress=True");
            conn.Open();
        }

        static bool tableExists(string tableName) => 
            new SQLiteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'", conn)
                .ExecuteScalar() != null;
        static void tryCreateTable(string tableName, List<(string name, string type)> attributes) =>
            new SQLiteCommand($"CREATE TABLE IF NOT EXISTS {tableName} ({attributes.Select(x => $"{x.name} {x.type},")})", conn)
                .ExecuteNonQuery();
        static bool tryExecuteReader(SQLiteCommand command, out SQLiteDataReader result)
        {
            try
            {
                command.Connection = conn;
                result = command.ExecuteReader();
                return true;
            }
            catch (SQLiteException e)
            {
                result = null;
                return false;
            }
        }
        static bool tryExecuteNonQuery(SQLiteCommand command, out int affectedRows)
        {
            try
            {
                command.Connection = conn;
                affectedRows = command.ExecuteNonQuery();
                return true;
            }
            catch (SQLiteException e)
            {
                affectedRows = -1;
                return false;
            }
        }
        
        public static bool trySetupLeaderboard(string tableName)
        {
            if (tableExists("PongLeaderboard"))
                return false;

            List<(string, string)> attributes = new List<(string, string)>
            {
                ("score", "INTEGER"),
                ("name", "TEXT"),
                ("date", "DATE"),
            };
            tryCreateTable("PongLeaderboard", attributes);
            return true;
        }
        public static bool tryGetHighscores(string tableName, int count, out List<(int score, string name, DateTime date)> results)
        {
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM @tableName ORDER BY score DESC LIMIT @count");
            command.Parameters.AddWithValue("@tableName", tableName);
            command.Parameters.AddWithValue("@count", count);
            
            results = null;
            if (tryExecuteReader(command, out SQLiteDataReader result) == false)
                return false;
            
            results = new List<(int score, string name, DateTime date)>();
            while (result.Read())
                results.Add((result.GetInt32(0), result.GetString(1), result.GetDateTime(2)));
            
            return true;
        }
        public static bool tryAddScore(string tableName, int score, string name)
        {
            SQLiteCommand command = new SQLiteCommand("INSERT INTO @tableName VALUES (@score, @name, current_date)");
            command.Parameters.AddWithValue("@tableName", tableName);
            command.Parameters.AddWithValue("@score", score);
            command.Parameters.AddWithValue("@name", name);
            return tryExecuteNonQuery(command, out _);
        }
    }

    static class NonSqlGameDatabase
    {
        static bool locked = false;
        
        class Table
        {
            public struct Record
            {
                public int score;
                public string name;
                public DateTime date;

                public Record(int score, string name, DateTime date) =>
                    (this.score, this.name, this.date) = (score, name, date);

                public Record(int score, string name) =>
                    (this.score, this.name, this.date) = (score, name, DateTime.Today);

                public (int score, string name, DateTime date) toTuple() =>
                    (score, name, date);
            }

            List<Record> data = new List<Record>();

            public void addScore(int score, string name) => data.Add(new Record(score, name));
            public void addScore(int score, string name, DateTime date) => data.Add(new Record(score, name, date));

            public void getHighscores(int count, out List<Record> results) =>
                results = data.OrderByDescending(x => x.score).TakeWhile((_, i) => i < count).ToList();
        }

        const string folderName = "GameData";
        
        public static void setupConnection()
        {
            Directory.CreateDirectory(folderName);
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), folderName));
        }

        static Table loadTable(string tableName)
        {
            Table table = new Table();
            
            while (locked)
                Thread.Sleep(10);
            
            locked = true;
            using (StreamReader reader = new StreamReader(File.Open($"{tableName}.txt", FileMode.Open)))
            {
                try
                {
                    while (true)
                    {
                        if (reader.ReadLine() != "RECORD") break;
                        int score = int.Parse(reader.ReadLine());
                        string name = reader.ReadLine();
                        DateTime date = DateTime.Parse(reader.ReadLine());
                        table.addScore(score, name, date);
                    }
                }
                catch (Exception e)
                {
                    GameLog.log(e.Message);
                    
                    GameScreen.newLine(5);
                    GameScreen.writeText("An error occured while loading leaderboard data");
                    GameScreen.writeText("Please edit or delete the leaderboard file");
                    GameScreen.newLine(5);
                    
                    Thread.Sleep(2000);
                }
            }
            locked = false;
            return table;
        }
        static bool tableExist(string tableName) => File.Exists($"{tableName}.txt");
        static void saveRecord(string tableName, Table.Record record)
        {
            while (locked)
                Thread.Sleep(10);
            
            locked = true;
            using (StreamWriter writer = new StreamWriter(File.Open($"{tableName}.txt", FileMode.Append)))
            {
                writer.WriteLine("RECORD");
                writer.WriteLine(record.score.ToString());
                writer.WriteLine(record.name);
                writer.WriteLine(record.date.ToLongDateString());
            }
            locked = false;
        }
        
        public static bool trySetupLeaderboard(string tableName)
        {
            if (File.Exists($"{tableName}.txt"))
                return false;
            
            File.Create($"{tableName}.txt");
            return true;
        }
        public static bool tryGetHighscores(string tableName, int count, out List<(int score, string name, DateTime date)> results)
        {
            results = new List<(int score, string name, DateTime date)>();
            if (!tableExist(tableName))
                return false;

            Table table = loadTable(tableName);
            table.getHighscores(count, out var resultsTemp);
            results = resultsTemp.Select(x => x.toTuple()).ToList();
            return true;
        }
        public static bool tryAddScore(string tableName, int score, string name)
        {
            if (!tableExist(tableName))
                return false;
            
            saveRecord(tableName, new Table.Record(score, name));
            return true;
        }
    }
}