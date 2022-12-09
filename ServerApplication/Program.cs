using System.Text;
using System.Net;
using System.Net.Sockets;
using HtmlAgilityPack;
using Microsoft.Data.Sqlite;

namespace Module11_Server {
    public static class StringExtension
    {
        public static string StripPunctuation(this string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (char.IsPunctuation(c)) {
                    // n-dash and m-dash should be delimiter
                    if (c == '-' || c == '—') {
                        sb.Append("|");
                    }
                } else {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }

    class Program {
        private static void initDatabase() {
            if (File.Exists("occur.db")) {
                File.Delete("occur.db");
            }

            using (SqliteConnection conn = new SqliteConnection("Data Source=occur.db")) {
                conn.Open();

                var command = conn.CreateCommand();
                command.CommandText =
                @"
                    CREATE TABLE word (
                        word_text       TEXT NOT NULL PRIMARY KEY,
                        occurrences     INTEGER
                    );
                ";
                command.ExecuteNonQuery();
            }
        }

        private static void addWord(String newWord) {
            using (SqliteConnection conn = new SqliteConnection("Data Source=occur.db")) {
                conn.Open();

                var command = conn.CreateCommand();
                command.CommandText = 
                @"
                    SELECT word_text, occurrences
                    FROM word
                    WHERE word_text = $new_word;
                ";
                command.Parameters.AddWithValue("$new_word", newWord);

                string word = "";
                Int32 occurrences = 0;
                Boolean wordExists = false;

                using (var reader = command.ExecuteReader()) {
                    if (reader.Read()) {
                        word = reader.GetString(0);
                        occurrences = reader.GetInt32(1) + 1;
                        wordExists = true;
                    }
                }

                if (wordExists) {
                    command.CommandText =
                    @"
                        UPDATE word 
                        SET occurrences = $new_occur
                        WHERE word_text = $word
                    ";
                    command.Parameters.AddWithValue("$new_occur", occurrences);
                    command.Parameters.AddWithValue("$word", word);
                    command.ExecuteNonQuery();
                } else {
                    command.CommandText =
                    @"
                        INSERT INTO word (word_text, occurrences)
                        VALUES ($word, 1)
                    ";
                    command.Parameters.AddWithValue("$word", newWord);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static string[] getWords(int limit) {
            string[] topResults = new string[limit];
            int i = 0;

            using (SqliteConnection conn = new SqliteConnection("Data Source=occur.db")) {
                conn.Open();

                var command = conn.CreateCommand();
                command.CommandText = 
                @"
                    SELECT 
                            word_text,
                            occurrences 
                    FROM 
                            word
                    ORDER BY 
                            occurrences DESC
                    LIMIT $result_limit;
                ";
                command.Parameters.AddWithValue("$result_limit", limit);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string word = reader.GetString(0);
                        Int16 occurrences = reader.GetInt16(1);

                        topResults[i] = word + ", " + occurrences + "\n";
                        i += 1;
                    }
                }
            }

            return topResults;
        }

        private static HtmlNode parsePoem(HtmlNode poem) {
            /*
                Manipulate HTML to remove br characters since removing it
                from the list directly is not a straightforward option
            */
            string poemHtml = poem.OuterHtml;

            // Convert <br> and newline character to delimiter
            poemHtml = poemHtml.Replace("<br>", "|").Replace("\n", "|");

            HtmlDocument poemDoc = new HtmlDocument();
            poemDoc.LoadHtml(poemHtml);

            return poemDoc.DocumentNode.SelectSingleNode("//div");
        }

        static void Main(string[] args) {
            Console.WriteLine("Booting up server");

            Console.WriteLine("Initializing Database...");
            initDatabase();

            Console.WriteLine("Parsing document...");
            HtmlDocument doc = new HtmlDocument();
            doc.Load(@"the_file.html");
        
            HtmlNode body = doc.DocumentNode.SelectSingleNode("//body");
            HtmlNode title = body.SelectSingleNode("//h1");
            HtmlNode byLine = body.SelectSingleNode("//h2");
            HtmlNode poem = parsePoem(body.SelectSingleNode("//div[@class='chapter']"));

            string fullPoem = $"{title.InnerText} {byLine.InnerText} {poem.InnerText}";

            // Convert space between words to delimiter
            fullPoem = fullPoem.StripPunctuation().Replace(" ", "|");

            // Split on delimiter and remove any empty values
            string[] words = fullPoem.Split("|").Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Console.WriteLine("Populating word counts...");
            foreach (string word in words) {
                addWord(word.ToLower());
            }

            // Create a localhost server
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            TcpListener server = new TcpListener(ip, 715);

            // Start the server which just listens for a connection
            server.Start();

            Console.WriteLine("Server started at {0}", DateTime.Now.ToString());

            TcpClient client = server.AcceptTcpClient();
            NetworkStream stream = client.GetStream();

            while (true) {
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                Console.WriteLine("Number received for result limit: {0}", dataReceived);

                string[] topResults;

                try {
                    int limit = Int32.Parse(dataReceived);
                    topResults = Program.getWords(limit);
                }
                catch (FormatException e) {
                    Console.WriteLine(e.ToString());
                    topResults = new string[] { dataReceived + " is not a number." };
                }

                string results = "";
                foreach (string result in topResults) {
                    results = results + result;
                }

                byte[] response = new byte[client.ReceiveBufferSize];
                response = Encoding.ASCII.GetBytes(results);
                stream.Write(response, 0, response.Count());
            }

            //client.Close();
            //server.Stop();
        }
    }
}