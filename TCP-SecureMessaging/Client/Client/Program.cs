using System;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Client
{


    class Program
    {

        private static readonly Socket ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static Thread messageThread = null;
        private static volatile bool isRunning = true;

        private static int pieceUser;
        private static string RecUser, SendUser;
        private static SqlConnection connection;
        private static SqlCommand command;
        private static SqlDataReader reader;

        // Helper metod buraya eklenecek
        private static void WriteAt(string text, int left, int top)
        {
            Console.SetCursorPosition(left, top);
            Console.Write(text);
        }

        static void Main()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.SetWindowSize(70, 35);
            Console.Clear();
            Console.Title = "Mesajlaşma İstemcisi";

            try
            {
                ConnectToServer("10.74.113.58");
                SetupDatabaseConnection();
                UserLogin();
                ShowMenu();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Veritabanı bağlantısı sırasında bir hata oluştu: " + ex.Message);
                Console.ReadKey();
            }
;


        }

        private static void SetupDatabaseConnection()
        {
            connection = new SqlConnection("Data Source = (localdb)\\MSSQLLocalDB; Initial Catalog = socketmesaj; Integrated Security = True;");
            command = new SqlCommand { Connection = connection };
        }

        private static void ConnectToServer(string serverIp)
        {
            int attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Tekrar Bağlanıyor ({0})", attempts);
                    ClientSocket.Connect(IPAddress.Parse(serverIp), 5445);
                    Console.WriteLine("çalışıyor mu deneme");

                }
                catch (SocketException)
                {
                    Console.Clear();
                }
            }

            Console.Clear();
            Console.WriteLine("Sunucuya Bağlandı.");
        }

        private static void UserLogin()
        {
            string username, password;

            Console.Write("Kullanıcı Adınız : ");
            username = Console.ReadLine();
            Console.Write("Şifreniz : ");
            password = Console.ReadLine();

            command.Parameters.Clear();
            command.CommandText = "SELECT * FROM db_users WHERE kadi=@username AND sifre=@password";
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", password);

            try
            {
                connection.Open();
                reader = command.ExecuteReader();

                if (reader.Read())
                {
                    Console.WriteLine("Giriş Başarılı\nDevam etmek için bir tuşa basınız..");
                    Console.ReadKey();
                    SendUser = username;
                    SendUserName(SendUser);
                    connection.Close();
                    ShowOfflineMessages();
                }
                else
                {
                    Console.WriteLine("Kullanıcı Adı veya Şifre yanlış!!!");
                    connection.Close();
                    UserLogin();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Veritabanı bağlantısı sırasında bir hata oluştu: " + ex.Message);
                connection.Close();
            }


        }

        private static void SendUserName(string username)
        {
            string message = "100;" + " " + ";" + username + "; ";
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private static void ShowOfflineMessages()
        {
            SqlCommand updateCommand = new SqlCommand { Connection = connection };

            command.Parameters.Clear();
            command.CommandText = "SELECT gonderen, encrypted_message FROM messages WHERE alici=@username AND iletim=0";
            command.Parameters.AddWithValue("@username", SendUser);

            try
            {
                connection.Open();
                reader = command.ExecuteReader();

                Console.Clear();
                Console.WriteLine("Okunmamış mesajlar \n");

                while (reader.Read())
                {
                    try
                    {
                        string encryptedMessage = reader["encrypted_message"].ToString();
                        string sender = reader["gonderen"].ToString();

                        // Şifre çözme işlemi
                        string decryptedMessage = SecurityHelper.DecryptMessage(encryptedMessage);

                        // Başarılı sonucu göster
                        Console.WriteLine($"{sender} : {decryptedMessage}");
                        Console.WriteLine("----------------------------------------");
                    }
                    catch
                    {
                        // Mesaj işleme hatalarında devam et
                        continue;
                    }
                }
                reader.Close();

                // Mesajları okundu olarak işaretle
                updateCommand.CommandText = "UPDATE messages SET iletim=1 WHERE alici=@username";
                updateCommand.Parameters.Clear();
                updateCommand.Parameters.AddWithValue("@username", SendUser);
                updateCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Veritabanı işlemi hatası: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();

                Console.WriteLine("\nDevam etmek için bir tuşa basın...");
                Console.ReadKey();
            }
        }




        private static void ShowMenu()
        {
            Console.WriteLine("MESAJ-");
            Console.WriteLine("1-) Bireysel Mesajlaşma");
            Console.WriteLine("2-) Grupla Mesajlaşma");
            Console.WriteLine("3-) Herkese Mesaj At");
            Console.Write("\nSeçiminiz: ");

            string userInput = Console.ReadLine();

            // Çıkış ve Menü kontrolü
            if (userInput.ToLower() == "cikis")
            {
                Exit();
                return;
            }
            else if (userInput.ToLower() == "-menü-")
            {
                Console.Clear();
                ShowMenu();
                return;
            }

            // Sayısal giriş kontrolü
            if (!int.TryParse(userInput, out int selection))
            {
                Console.WriteLine("Hatalı giriş! Lütfen 1, 2 veya 3 arasında bir seçim yapınız.");
                ShowMenu();
                return;
            }

            switch (selection)
            {
                case 1:
                    StartSingleMessage();
                    break;
                case 2:
                    StartGroupMessage();
                    break;
                case 3:
                    SendMessageToEveryone();
                    break;
                default:
                    Console.WriteLine("Hatalı Seçim!!");
                    ShowMenu();
                    break;
            }
        }





        private static void StartSingleMessage()
        {
            while (true)
            {
                while (true)
                {
                    Console.WriteLine("Mevcut Kullanıcılar:");

                    command.Parameters.Clear();
                    command.CommandText = "SELECT kadi FROM db_users WHERE kadi != @currentUser";
                    command.Parameters.AddWithValue("@currentUser", SendUser);

                    try
                    {
                        if (connection.State == ConnectionState.Open)
                            connection.Close();

                        connection.Open();
                        reader = command.ExecuteReader();

                        // Kullanıcıları yan yana yazdırma
                        int counter = 0;
                        while (reader.Read())
                        {
                            Console.Write($"[{reader["kadi"]}] ");
                            counter++;
                            if (counter % 5 == 0) // Her 5 kullanıcıda bir alt satıra geç
                                Console.WriteLine();
                        }
                        Console.WriteLine("\n"); // Ekstra boş satır

                        reader.Close();
                        Console.WriteLine("Alıcı adını giriniz:");
                        string recipient = Console.ReadLine();

                        command.Parameters.Clear();
                        command.CommandText = "SELECT * FROM db_users WHERE kadi=@recipient";
                        command.Parameters.AddWithValue("@recipient", recipient);

                        reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            Console.WriteLine("Bağlantı kuruldu..");
                            Console.ReadKey();
                            Console.Clear();

                            RecUser = recipient;
                            Console.Title = $"Bireysel Sohbet - {SendUser} >> {RecUser}";
                            pieceUser = 1;

                            reader.Close();
                            connection.Close();

                            StartRequestLoop();
                            Exit();
                            return;
                        }
                        else
                        {
                            Console.WriteLine($"'{recipient}' isimli kullanıcı bulunamadı.\nLütfen kullanıcı adını tekrar giriniz..");
                            Console.ReadKey();
                            Console.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Veritabanı hatası: {ex.Message}");
                    }
                    finally
                    {
                        if (reader != null && !reader.IsClosed)
                            reader.Close();
                        if (connection.State == ConnectionState.Open)
                            connection.Close();
                    }
                }
            }
        }

        private static void StartGroupMessage()
        {

            int selection;

            Console.WriteLine("Grup Sohbeti\n1-) Yeni Grup Oluştur\n2-) Var olan gruba yaz.");
            Console.Write("Seçiminiz : ");
            selection = Convert.ToInt32(Console.ReadLine());

            if (selection == 1)
            {
                Console.Write("Grup adı: ");
                string groupName = Console.ReadLine();

                // Mevcut kullanıcıları göster
                Console.WriteLine("\nMevcut Kullanıcılar:");
                command.Parameters.Clear();
                command.CommandText = "SELECT kadi FROM db_users WHERE kadi != @currentUser";
                command.Parameters.AddWithValue("@currentUser", SendUser);

                connection.Open();
                reader = command.ExecuteReader();

                // Kullanıcıları yan yana yazdırma
                int counter = 0;
                while (reader.Read())
                {
                    Console.Write($"[{reader["kadi"]}] ");
                    counter++;
                    if (counter % 5 == 0) // Her 5 kullanıcıda bir alt satıra geç
                        Console.WriteLine();
                }
                Console.WriteLine("\n");
                reader.Close();
                connection.Close();

                Console.WriteLine("Gruba eklemek istediğiniz kişileri virgülle ayırarak yazın:");
                string members = Console.ReadLine();
                members += "," + SendUser;

                command.Parameters.Clear();
                command.CommandText = "INSERT INTO groups (grupAd, kisiler) VALUES (@groupName, @members)";
                command.Parameters.AddWithValue("@groupName", groupName);
                command.Parameters.AddWithValue("@members", members);

                connection.Open();
                int result = command.ExecuteNonQuery();
                connection.Close();

                if (result > 0)
                {
                    Console.WriteLine("Grup Başarıyla oluşturuldu");

                    // Mesajlaşma ekranına geçiş
                    Console.Clear();
                    Console.Title = $"Grup Sohbeti - {SendUser} >> {groupName}";
                    RecUser = groupName; // Aktif grup adını RecUser'a atıyoruz
                    StartRequestLoop();
                    Exit();
                }
                else
                {
                    Console.WriteLine("Grup oluşturma başarısız");
                }

                Console.ReadKey();

            }
            else if (selection == 2)
            {
                Console.WriteLine("\nMevcut Gruplar:");
                command.Parameters.Clear();
                command.CommandText = "SELECT grupAd FROM groups WHERE kisiler LIKE @userPattern";
                command.Parameters.AddWithValue("@userPattern", $"%{SendUser}%");

                connection.Open();
                reader = command.ExecuteReader();

                // Grupları yan yana yazdırma
                int counter = 0;
                while (reader.Read())
                {
                    Console.Write($"[{reader["grupAd"]}] ");
                    counter++;
                    if (counter % 3 == 0) // Her 3 grupta bir alt satıra geç
                        Console.WriteLine();
                }
                Console.WriteLine("\n");
                reader.Close();
                connection.Close();

                bool groupFound = false;
                string groupName = "";
                string members = "";

                while (!groupFound)
                {
                    Console.Write("Grup adı: ");
                    groupName = Console.ReadLine();

                    command.Parameters.Clear();
                    command.CommandText = "SELECT * FROM groups WHERE grupAd=@groupName";
                    command.Parameters.AddWithValue("@groupName", groupName);
                    connection.Open();
                    reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        members = reader[2].ToString();
                        groupFound = true;
                        connection.Close();
                    }
                    else
                    {
                        Console.WriteLine("Grup Bulunamadı!!");
                        reader.Close();
                        connection.Close();
                    }
                }

                string[] membersArray = members.Split(',');

                Console.Title = $"Grup Sohbeti - {SendUser} >> {groupName}";
                pieceUser = membersArray.Length;
                RecUser = groupName;
                StartRequestLoop();
                Exit();
            }
            else
            {

                Console.WriteLine("Hatalı Seçim!!");
                StartGroupMessage();
                return;
            }
        }




        private static void SendMessageToEveryone()
        {
            StopMessageThread();
            pieceUser = 0;
            RecUser = "Herkes";
            Console.Title = $"Toplu Mesaj - {SendUser} >> Herkese";
            StartRequestLoop();
            Exit();
            ShowMenu();
        }


   

        private static void StartMessageThread()
        {
            isRunning = true;
            if (messageThread == null || !messageThread.IsAlive)
            {
                messageThread = new Thread(ReceiveMessages);
                messageThread.Start();
            }
        }

        private static void StopMessageThread()
        {
            if (messageThread != null && messageThread.IsAlive)
            {
                isRunning = false;
                try
                {
                    messageThread.Join(1000);
                }
                catch { }
            }
        }

        private static void StartRequestLoop()
        {
            Console.WriteLine(@"<Uygulamayı sonlandırmak için ""cikis"" yazınız.>");
            Console.WriteLine(@"<Menüye dönmek için ""-menü-"" yazınız.>");
            Console.WriteLine(GetLocalIPAddress());

            StartMessageThread();

            while (true)
            {
                string request = Console.ReadLine();

                if (request.ToLower() == "-menü-")
                {
                    StopMessageThread();
                    Console.Clear();
                    ShowMenu();
                    return;
                }

                if (request.ToLower() == "cikis")
                {
                    Exit();
                    return;
                }

                SendRequest(request);
            }
        }

        private static void SendRequest(string request)
        {
            SendString(request);
        }

        private static void SendString(string text)
        {
            if (text != "cikis")
                text = $"{pieceUser};{RecUser};{SendUser};{text}";

            byte[] buffer = Encoding.ASCII.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private static void ReceiveMessages()
        {
            while (isRunning)
            {
                try
                {
                    byte[] buffer = new byte[2048];
                    if (ClientSocket.Connected)
                    {
                        int received = ClientSocket.Receive(buffer, SocketFlags.None);

                        if (received == 0) continue;

                        ReceiveResponse(buffer, received);
                        Thread.Sleep(500);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (SocketException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private static void ReceiveResponse(byte[] buffer, int received)
        {
            byte[] data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            string[] token = text.Split(';');

            Console.WriteLine("\n{0} :{1}", token[0], token[1]);
        }

        private static void Exit()
        {
            StopMessageThread();
            SendString("cikis");

            try
            {
                if (ClientSocket.Connected)
                {
                    ClientSocket.Shutdown(SocketShutdown.Both);
                    ClientSocket.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Bağlantı kapatma hatası: {ex.Message}");
            }

            Environment.Exit(0);
        }

        static string GetLocalIPAddress()
        {
            string ip = null;
            foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                ip = address.ToString();
            }
            return ip;
        }
    }

}

