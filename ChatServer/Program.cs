using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener( IPAddress.Any,8888);
            server.Start();
            server.BeginAcceptTcpClient(new AsyncCallback(ReturnAccept), server);

            Console.WriteLine("Enter any char exist..");
            Console.Read();
        }

        static void ReturnAccept(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("Star AcceptCallBack");

                TcpListener Server = (TcpListener)ar.AsyncState;
                TcpClient client = Server.EndAcceptTcpClient(ar);

                var usr = new User(client);
                usr.Welcome();


                Server.BeginAcceptTcpClient(new AsyncCallback(ReturnAccept), Server);
            }catch( Exception  ex){
                Console.WriteLine("accept async exception:{0} ", ex.Message);
            }
        }
    }
}
