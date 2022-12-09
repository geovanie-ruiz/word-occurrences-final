using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Module11_Server {
    class Program {
        public static bool IsPrime(int number) {
            if (number <= 1) return false;
            if (number % 2 == 0) return false;
            if (number == 2) return true;

            int limit = (int)(Math.Floor(Math.Sqrt(number)));

            for (int i = 3; i <= limit; i += 2) {
                if (number % i == 0) {
                    return false;
                }
            }

            return true;
        }

        static void Main(string[] args) {

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

                Console.WriteLine("Number received from client to check prime number is: {0}", dataReceived);

                byte[] response = new byte[64];

                try {
                    int numberToCheck = Int32.Parse(dataReceived);

                    if (Program.IsPrime(numberToCheck)) {
                        response = Encoding.ASCII.GetBytes(dataReceived + " is prime.");
                    } else {
                        response = Encoding.ASCII.GetBytes(dataReceived + " is not prime.");
                    }
                }
                catch (FormatException e) {
                    Console.WriteLine(e.ToString());
                    response = Encoding.ASCII.GetBytes(dataReceived + " is not a number.");
                }

                stream.Write(response, 0, response.Count());
            }

            //client.Close();
            //server.Stop();
        }
    }
}