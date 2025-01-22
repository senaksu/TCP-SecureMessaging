using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace Server
{
    class Program
    {
        #region Statik Tanımlamalar
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private static readonly List<string> users = new List<string>();

        private const int BUFFER_SIZE = 2048;
        private const int PORT = 5445;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];

        private static SqlConnection baglanti;
        private static SqlCommand komut;
        private static SqlDataReader reader;
        #endregion



        static void Main()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;

            Console.SetWindowSize(70, 35);
            Console.Clear();

            Console.Title = "Mesajlaşma Server";
            SetupServer();

            while (Console.ReadLine() != "cikis") ;

            CloseAllSockets();
        }

        private static void SetupServer()
        {
            Console.WriteLine("Server ayarları yapılıyor.");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);

            baglanti = new SqlConnection();
            baglanti.ConnectionString = "Data Source = (localdb)\\MSSQLLocalDB; Initial Catalog = socketmesaj; Integrated Security = True; ";
            komut = new SqlCommand();
            komut.Connection = baglanti;

            Console.WriteLine("Server hazır.");
        }

        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("İstemci bağlandı. İstek bekleniyor.");
            serverSocket.BeginAccept(AcceptCallback, null);

        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            #region Tanımlamalar
            Socket current = (Socket)AR.AsyncState;
            Console.WriteLine(current.RemoteEndPoint);
            int received;
            #endregion

            #region Try-catch Bloğu

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("İstemci Bağlantısını Kesti.");

                int index = clientSockets.IndexOf(current);
                if (users.Count > index) users.Remove(users[index]);
                current.Close();
                clientSockets.Remove(current);

                return;
            }
            #endregion

            #region İstemciden gelen mesaj

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine("Gelen Mesaj: " + text);

            #endregion

            #region İstemcinin bağlantı koparması

            if (text.ToLower() == "cikis")
            {
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                int index = clientSockets.IndexOf(current);
                users.Remove(users[index]);
                clientSockets.Remove(current);

                Console.WriteLine("İstemci bağlantısı koparıldı");
                return;
            }

            #endregion

               else
               {
                   string[] token = text.Split(';');  // Gelen mesajı parçalama

                   #region kullanıcı servera kullanıcı adını göndermişse
                   if (token[0] == "100")
                   {
                       users.Add(token[2]);
                   }
                   #endregion

                   #region Bireysel sohbet mesajı ise
                   else if (Convert.ToInt32(token[0]) == 1)
                   {
                       string messageHash = CalculateHash(token[3]); // token[3] şifreli mesaj
                       sendMessage(token[1], token[2], token[3], messageHash);

                      // Console.WriteLine("Sohbet Mesajı iletildi");
                   }
                   #endregion

                   #region Grup sohbeti ise
                   else if (Convert.ToInt32(token[0]) > 1)
                   {
                       string kisiler;

                       komut.Connection = baglanti;
                       komut.CommandText = "SELECT * FROM groups WHERE grupAd='" + token[1] + "'";
                       baglanti.Open();
                       reader = komut.ExecuteReader();

                       if (reader.Read())
                       {
                           kisiler = reader[2].ToString();
                           string[] grup = kisiler.Split(',');
                           baglanti.Close();
                           reader.Close();

                           // Mesajın hash'ini hesapla
                           string messageHash = CalculateHash(token[3]);

                           for (int i = 0; i < Convert.ToInt32(token[0]); i++)
                           {
                               if (grup[i] == token[2]) continue;
                               sendMessage(grup[i], token[2], token[3], messageHash);
                              // Console.WriteLine("Sohbet Mesajı iletildi");
                           }
                       }
                   }
                   #endregion

                   #region Herkese (Yayın Mesajı ise)
                   else if (Convert.ToInt32(token[0]) == 0)
                   {
                       List<string> alici = new List<string>();

                       komut.Connection = baglanti;
                       komut.CommandText = "SELECT kadi FROM db_users";
                       baglanti.Open();
                       reader = komut.ExecuteReader();

                       while (reader.Read())
                       {
                           alici.Add(reader[0].ToString());
                       }

                       baglanti.Close();
                       reader.Close();

                       // Mesajın hash'ini hesapla
                       string messageHash = CalculateHash(token[3]);

                       for (int i = 0; i < alici.Count; i++)
                       {
                           if (alici[i] == token[2]) continue;
                           sendMessage(alici[i], token[2], token[3], messageHash);
                           Console.WriteLine("Sohbet Mesajı iletildi");
                       }
                   }
                   #endregion
               }

               // İstek işlendikten sonra yeni istek bekleme
               current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
           }
            
        

            private static void sendMessage(string receive, string sender, string encryptedMessage, string messageHash)
            {
                // messageHash null veya boşsa, geçerli bir değer ata
                if (string.IsNullOrEmpty(messageHash))
                {
                    messageHash = "default_hash_value";
                }

                Socket client = null;

                if (users.IndexOf(receive) != -1)
                {
                    client = clientSockets[users.IndexOf(receive)];
                    byte[] data = Encoding.ASCII.GetBytes(sender + ";" + encryptedMessage);
                    client.Send(data);
                    // Sadece şifreli mesajı kaydet
                    komut.CommandText = "INSERT INTO messages (gonderen, alici, encrypted_message, message_hash, iletim) VALUES (@sender, @receive, @encryptedMessage, @messageHash, '1')";
                }
                else
                {
                    Console.WriteLine("Kullanıcı aktif değil. Online olunca mesaj iletilecektir.");
                    // Sadece şifreli mesajı kaydet
                    komut.CommandText = "INSERT INTO messages (gonderen, alici, encrypted_message, message_hash, iletim) VALUES (@sender, @receive, @encryptedMessage, @messageHash, '0')";
                }

                komut.Parameters.Clear();
                komut.Parameters.AddWithValue("@sender", sender);
                komut.Parameters.AddWithValue("@receive", receive);
                komut.Parameters.AddWithValue("@encryptedMessage", encryptedMessage);
                komut.Parameters.AddWithValue("@messageHash", messageHash);

                baglanti.Open();
                komut.ExecuteNonQuery();
                baglanti.Close();
            }


            // Mesajın hash'ini hesaplamak için metot
            private static string CalculateHash(string encryptedMessage)
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptedMessage));
                    StringBuilder builder = new StringBuilder();
                    foreach (var b in bytes)
                    {
                        builder.Append(b.ToString("x2"));
                    }
                    return builder.ToString();
                }
            }
        }
    }

