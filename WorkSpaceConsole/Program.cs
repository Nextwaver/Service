using System;
using System.IO;
using System.Net;
using System.Threading;

namespace WorkSpaceConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Timer threadingTimer = new Timer(GetTimestamp, 10, 1, 1000);
 
            Console.ReadLine();
        }

        static void GetTimestamp(Object args)
        {

            //  String Result = CallAPI("https://localhost:44387/WS/V1/CallTest");

            String XXX = "XXXX";

            Console.WriteLine("Time : " + DateTime.Now + " >>>> " + XXX);
 
        }
        private static string CallAPI(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();

            var reader = new StreamReader(dataStream);
            var textFromApi = reader.ReadToEnd();

            reader.Close();
            response.Close();


            return textFromApi;

        }

    }
}
