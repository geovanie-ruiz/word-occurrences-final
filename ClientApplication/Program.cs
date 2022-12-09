using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Module11_Client {
    class Program {
        public static void GetTopWords(Socket client, int number) {
            try {
                byte[] sendmsg = Encoding.ASCII.GetBytes(number.ToString());

                int n = client.Send(sendmsg);

                byte[] data = new byte[client.ReceiveBufferSize];

                int m = client.Receive(data);

                Console.WriteLine(Encoding.ASCII.GetString(data));
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        static void Main(string[] args) {
            IPHostEntry iphostInfo = Dns.GetHostEntry(IPAddress.Parse("127.0.0.1"));
            IPAddress ipAdress = iphostInfo.AddressList[0];
            IPEndPoint ipEndpoint = new IPEndPoint(ipAdress, 715);

            Socket client = new Socket(ipAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(ipEndpoint);

            while (true) {
                Console.WriteLine("Get the top X occurrences wher X =");
                var input = Console.ReadLine();
                int limit = 0;

                if (Int32.TryParse(input, out limit) && limit > 0) {
                    Program.GetTopWords(client, limit);
                } else if (limit <= 0) {
                    Console.WriteLine("Input must be greater than zero.\n");
                } else {
                    Console.WriteLine("Input is not a number. Try again.\n");
                }
            }

            //client.Shutdown(SocketShutdown.Both);
            //client.Close();
        }
    }
}