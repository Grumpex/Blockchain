using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using XSystem.Security.Cryptography;

namespace Blockchain
{
    public partial class Form1 : Form
    {

        TcpClient client = null;
        TcpListener listener = null;

        IPAddress ip = IPAddress.Parse("127.0.0.1");
        int port = 0;
        List<Block> list_blockchain = new List<Block>();


        public int counter { get; set; }




        int difficulty = 4;

        int index = 0;

        int izpis = 100000;

        public List<TcpClient> clients { get; set; }
        NetworkStream dataStream = null;
        public Form1()
        {
            InitializeComponent();
        }

        static string sha256(string nonHash)
        {
            var crypt = new SHA256Managed();
            string hash = String.Empty;
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(nonHash));
            for (int i = 0;i < crypto.Length;i++)
            {
                byte theByte = crypto[i];
                hash += theByte.ToString("x2");
            }
            return hash;
        }

        private void trenutna_tezavnost(Block block) // Izracuna trenutno tezavnost (Razbral iz psevdokode naloge)
        {
            Block previousAdjustmentBlock = list_blockchain[block.index - blockDiff];
            TimeSpan timeExpected = interval * blockDiff;
            TimeSpan timeTaken = block.timestamp - previousAdjustmentBlock.timestamp;

            difficulty = timeTaken >= (timeExpected / 2)
                ? timeTaken > (timeExpected * 2) ? previousAdjustmentBlock.difficulty - 1 : previousAdjustmentBlock.difficulty
                : previousAdjustmentBlock.difficulty + 1;
        }

        private void rudarjenje() // Rudarjenje in algoritem proof of work
        {
            Block block = new Block();

            while (true)
            {
                switch (list_blockchain.Count)
                {
                    case 0:
                    block.index = index;
                    block.data = node_name.Text;
                    block.timestamp = DateTime.Now;
                    block.previousHash = "0";
                    block.difficulty = difficulty;

                    block = proof_of_work(block);
                    if (validacija(block))
                    {
                        list_blockchain.Add(block);

                        index++;
                    }
                    break;
                    default:
                    block.index = index;
                    block.data = node_name.Text;
                    block.timestamp = DateTime.Now;
                    block.previousHash = list_blockchain[index - 1].hash;
                    block.difficulty = difficulty;

                    block = proof_of_work(block);
                    if (validacija(block))
                    {
                        list_blockchain.Add(block);
                        richTextBox1.AppendText("\n" + "index: " + block.index.ToString() + "\n" + "Data: " + block.data + "\n" + "Timestamp: " + block.timestamp.ToString() + "\n" + "Hash: " + block.hash + "\n" + "Previous hash: " + block.previousHash + "\n" + "Diff: " + block.difficulty.ToString() + "\n" + "Nonce: " + block.nonce.ToString() + "\n");
                        index++;
                    }

                    if (block.index % blockDiff == 0)
                    {
                        trenutna_tezavnost(block);
                    }
                    break;
                }




            }
        }

        const int blockDiff = 10;

        private bool validacija(Block block)
        {
            switch (block.index)
            {
                case 0:
                if (block.hash != sha256(block.index.ToString() + block.timestamp.ToString() + block.data + block.previousHash + block.difficulty.ToString() + block.nonce.ToString()))
                {
                    return false;
                }
                else
                {
                    return true;
                }

                default:
                {
                    Block lastBlock = list_blockchain[block.index - 1];

                    if (lastBlock.index + 1 == block.index && lastBlock.hash == block.previousHash && block.hash == sha256(block.index.ToString() + block.timestamp.ToString() + block.data + block.previousHash + block.difficulty.ToString() + block.nonce.ToString()))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        bool flag = false;

        private Block proof_of_work(Block block)
        {
            int nonce = 0;
            string hash = "";
            int i = 0;
            for (;;)
            {
                if (flag != true)
                {
                    hash = sha256(block.index.ToString() + block.timestamp.ToString() + block.data + block.previousHash + block.difficulty.ToString() + nonce.ToString());

                    if (nonce % izpis == 0)
                    {
                        richTextBox2.AppendText(hash + " difficulty: " + block.difficulty.ToString());
                    }


                    if (preveri_tezavnost(block,hash))
                    {
                        block.nonce = nonce;
                        block.hash = hash;
                        return block;
                    }
                    else
                    {
                        nonce++;
                    }
                }
                else
                {
                    flag = false;
                    break;
                }
            }
            return block;
        }

        private bool preveri_tezavnost(Block block,string hash)
        {
            for (int i = 0;i < difficulty;i++)
            {
                switch (hash[i])
                {
                    case '0':
                    break;
                    default:
                    return false;
                }
            }
            return true;
        }

        /* Block Deserialize(byte[] polje)
         {

             string stringData = Encoding.UTF8.GetString(polje);
             Block block = JsonConvert.DeserializeObject<Block>(stringData);
         }
        */


        byte[] getBytes(Block str) // Uporabljen vir https://stackoverflow.com/questions/3278827/how-to-convert-a-structure-to-a-byte-array-in-c
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(str,ptr,true);
                Marshal.Copy(ptr,arr,0,size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }

        Block fromBytes(byte[] arr) // Uporabljen vir https://stackoverflow.com/questions/3278827/how-to-convert-a-structure-to-a-byte-array-in-c
        {
            Block str = new Block();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(arr,0,ptr,size);

                str = (Block)Marshal.PtrToStructure(ptr,str.GetType());
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return str;
        }



        private void ListenForConnections() // Povezava streznika (poslusalec)
        {
            byte[] message;
            listener = new TcpListener(ip,port);

            try
            {
                listener.Start();
                TcpClient client = listener.AcceptTcpClient();
                NetworkStream ns = client.GetStream();

                try
                {

                    for (;;)
                    {
                        List<Block> receivedBlockchain = new List<Block>();
                        for (;;)
                        {
                            message = Preberi(ns);
                            if (Encoding.ASCII.GetString(message) != "A") // Protokol za komunikacijo z odjemalcem
                            {
                                receivedBlockchain.Add(fromBytes(message));
                                Poslji(ns,Encoding.ASCII.GetBytes("B")); // Protokol za komunikacijo z odjemalcem
                            }
                            else
                            {
                                break;
                            }
                        }
                        validacijaCasovnihZnack(receivedBlockchain);

                        for (;;)
                        {
                            Thread.Sleep(2000);
                            foreach (var item in list_blockchain)
                            {
                                message = getBytes(item);
                                Poslji(ns,message);
                                message = Preberi(ns);
                            }
                            message = Encoding.ASCII.GetBytes("A"); // Protokol za komunikacijo z odjemalcem
                            Poslji(ns,message);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void SendPacket()
        {
            byte[] message;
            TcpClient client = new TcpClient();

            try
            {
                client.Connect(ip,port);
                NetworkStream ns = client.GetStream();

                for (;;)
                {
                    List<Block> receivedBlockchain = new List<Block>();
                    for (;;)
                    {
                        Thread.Sleep(2000);
                        foreach (var item in list_blockchain)
                        {
                            message = getBytes(item);
                            Poslji(ns,message);
                            message = Preberi(ns);
                        }
                        message = Encoding.ASCII.GetBytes("A"); // Protokol za komunikacijo z odjemalcem
                        Poslji(ns,message);
                        break;
                    }

                    for (;;)
                    {
                        message = Preberi(ns);
                        if (Encoding.ASCII.GetString(message) == "A") // Protokol za komunikacijo z odjemalcem
                        {
                            break;
                        }
                        receivedBlockchain.Add(fromBytes(message));
                        Poslji(ns,Encoding.ASCII.GetBytes("B")); // Protokol za komunikacijo z odjemalcem
                    }
                    validacijaCasovnihZnack(receivedBlockchain);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void validacijaCasovnihZnack(List<Block> blockchain)
        {
            int shrani_vrednost = 0;
            int Blockchain = 0;

            foreach (var cumulitativna_tezavnost in from index in list_blockchain
                                                    let cumulitativna_tezavnost = Math.Pow(2,index.difficulty) // Validacija casovnih znack
                                                    select cumulitativna_tezavnost)
            {
                shrani_vrednost += (int)cumulitativna_tezavnost;
            }

            foreach (var cumulitativna_tezavnost in from index in blockchain
                                                    let cumulitativna_tezavnost = Math.Pow(2,index.difficulty) // Validacija casovnih znack
                                                    select cumulitativna_tezavnost)
            {
                Blockchain += (int)cumulitativna_tezavnost;
            }

            if (Blockchain > shrani_vrednost)
            {
                list_blockchain = blockchain;
                if (index != 0)
                {
                    for (int i = index;i < list_blockchain[list_blockchain.Count - 1].index + 1;i++)
                    {
                        if (i % blockDiff == 0)
                        {
                            trenutna_tezavnost(list_blockchain[i]);
                        }
                    }
                }

                index = list_blockchain[^1].index + 1; // Dodaja +1 index usak loop
                difficulty = list_blockchain[^1].difficulty; // Izpis difficulty
                flag = true;
                richTextBox1.AppendText("");
                for (int i = 0;i < list_blockchain.Count;i++)
                {
                    Block index = list_blockchain[i];
                    richTextBox1.AppendText("\n" + "index: " + index.index.ToString() + "\n" + "Data: " + index.data + "\n" + "Timestamp: " + index.timestamp.ToString() + "\n" + "Hash: " + index.hash + "\n" + "Previous hash: " + index.previousHash + "\n" + "Diff: " + index.difficulty.ToString() + "\n" + "Nonce: " + index.nonce.ToString() + "\n");
                    /*richTextBox1.AppendText("Data: " + index.data + "\n");
                    richTextBox1.AppendText("Timestamp: " + index.timestamp.ToString() + "\n");
                    richTextBox1.AppendText("Hash: " + index.hash + "\n");
                    richTextBox1.AppendText("Previous hash: " + index.previousHash + "\n");
                    richTextBox1.AppendText("Diff: " + index.difficulty.ToString() + "\n");
                    richTextBox1.AppendText("Nonce: " + index.nonce.ToString() + "\n");*/

                }
            }
        }



        static int FreeTcpPort() // Pomagal sem si z stackoverflow... Dobis free port, ki se ni odprt.
        {
            TcpListener l = new TcpListener(IPAddress.Loopback,0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        Thread thClient = null;
        Thread thListener = null;
        Thread thMiner = null;
        TimeSpan interval = TimeSpan.FromSeconds(10);
        static SemaphoreSlim signal = new SemaphoreSlim(0,9);
        private void connect_Click(object sender,EventArgs e)
        {

            if (!string.IsNullOrEmpty(node_name.Text))
            {
                connect.Enabled = false;

                port = FreeTcpPort();
                thListener = new Thread(new ThreadStart(ListenForConnections));
                thListener.IsBackground = true;
                thListener.Start();
            }
            else
            {
                connect.Enabled = true;
                MessageBox.Show("Vpisi node name!");
            }
            status.Text = "Online PORT:" + " " + port;
        }

        private void port_connect_Click(object sender,EventArgs e)
        {

            port = Convert.ToInt32(textBox1.Text);
            richTextBox2.AppendText("Connected client: " + port);
            // ustvarimo novo povezavo na strežnik v loèeni niti
            // na tak naèin bi lahko izvedli veè simultanih povezav na isti strežnik
            client = new TcpClient();
            thClient = new Thread(new ThreadStart(SendPacket));
            thClient.IsBackground = true;
            thClient.Start();
        }


        private void mine_Click(object sender,EventArgs e) // Gumb za rudarjenje
        {
            mine.Enabled = false;
            thMiner = new Thread(rudarjenje);
            thListener.IsBackground = true;
            thMiner.Start();

        }



        static byte[] Preberi(NetworkStream stream) // Metoda za branje poslanega sporocila
        {
            try
            {
                byte[] beri = new byte[1024];
                MemoryStream data = new MemoryStream();
                int dolzina = stream.Read(beri,0,beri.Length);
                while (dolzina > 0)
                {
                    data.Write(beri,0,dolzina);
                    if (stream.DataAvailable)
                    {
                        dolzina = stream.Read(beri,0,beri.Length);
                    }
                    else break;
                }
                return data.ToArray();
                {

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Napaka!" + ex.Message + " " + ex.StackTrace);
                return null;
            }
        }

        static void Poslji(NetworkStream stream,byte[] mssg) // Metoda za posiljanje sporocila
        {
            try
            {
                byte[] poslji = mssg;
                stream.Write(poslji,0,poslji.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Napaka!" + ex.Message + " " + ex.StackTrace);
            }
        }
    }
}
public struct Block // Struktura Bloka
{
    public int index;
    [MarshalAs(UnmanagedType.ByValTStr,SizeConst = 200)]
    public string data;
    public DateTime timestamp;
    [MarshalAs(UnmanagedType.ByValTStr,SizeConst = 200)]
    public string hash;
    [MarshalAs(UnmanagedType.ByValTStr,SizeConst = 200)]
    public string previousHash;
    public int difficulty;
    public int nonce;
}
