using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace LeonetAutoAuthentication
{
    public partial class Form1 : Form
    {
        private string pw = "FINALSTREAM.NET";

        public Form1()
        {
            InitializeComponent();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exit();
        }

        private void exit()
        {
            //Properties.Settings.Default.UserId = txtUserId.Text;
            //Properties.Settings.Default.Password = EncryptString(txtPassword.Text, pw);
            //Properties.Settings.Default.Save();

            var appConfig = Program.AppConfig;

            appConfig.UserId = txtUserId.Text;
            appConfig.Password = EncryptString(txtPassword.Text, pw);
            File.WriteAllText(Program.AppConfigPath, JsonConvert.SerializeObject(appConfig));


            //Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            //CreateShortCut("user.config.lnk",config.FilePath,"");

            Application.Exit();
        }

        

        private void showSetting()
        {
            this.Visible = true;
            //this.Size = new Size(241, 85);
        }

        private void 設定ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showSetting();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await execAuthAsync();
        }

    
        private async System.Threading.Tasks.Task execAuthAsync()
        {
            var appConfig = Program.AppConfig;

            
            string defaultGatwayAddress = "";
            // デフォルトゲートウェイ取得
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in adapters)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                    GatewayIPAddressInformationCollection addresses =
                        adapterProperties.GatewayAddresses;
                    if (addresses.Count > 0)
                    {
                        foreach (GatewayIPAddressInformation address in addresses)
                        {
                            var gatewayAddress = address.Address.ToString();
                            if (!Equals(IPAddress.Any, address.Address)) defaultGatwayAddress = gatewayAddress.ToString();
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(defaultGatwayAddress)) break;
                }
            }

            if (String.IsNullOrEmpty(defaultGatwayAddress))
            {
                MessageBox.Show("デフォルトゲートウェイアドレス取得失敗。LANケーブルが接続されているか確認してください。","NG", MessageBoxButtons.OK, MessageBoxIcon.Error);
                exit();
            }


            notifyIcon.BalloonTipTitle = "LeonetAutoAuthentication";

            var credentials = new NetworkCredential(txtUserId.Text, txtPassword.Text);
            using (var client = new HttpClient(new RetryHandler(new HttpClientHandler() { Credentials = credentials })))
            {
                client.Timeout = TimeSpan.FromMilliseconds(appConfig.ConnectionTimeout);
                var result = await client.GetAsync(appConfig.ConnectUrl.Replace("#GATEWAY#", defaultGatwayAddress));
                if (result.IsSuccessStatusCode)
                {
                    notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    notifyIcon.BalloonTipText = "Success";
                    notifyIcon.ShowBalloonTip(appConfig.ViewMillisecond);
                }
                else
                {
                    notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
                    notifyIcon.BalloonTipText = result.ReasonPhrase;
                }

                if (result.IsSuccessStatusCode)
                {

                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    while (sw.ElapsedMilliseconds < appConfig.ViewMillisecond)
                    {
                        // 何らかの処理
                        await Task.Delay(10);

                        // メッセージ・キューにあるWindowsメッセージをすべて処理する
                        Application.DoEvents();
                    }
                    exit();
                }
            }

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Visible = false;
                e.Cancel = true;
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            var appConfig = Program.AppConfig;

            
            txtUserId.Text = appConfig.UserId;
            txtPassword.Text = DecryptString(appConfig.Password, pw);
            
            if (!Program.isReset && !String.IsNullOrEmpty(txtUserId.Text) && !String.IsNullOrEmpty(txtPassword.Text))
            {
                // wait
                //System.Threading.Thread.Sleep(Properties.Settings.Default.WaitMillisecond);

                await execAuthAsync();
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {

            showSetting();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.Visible = false;
            this.Opacity = 100;


            if (String.IsNullOrEmpty(txtUserId.Text) || String.IsNullOrEmpty(txtPassword.Text))
            {
                showSetting();
            } else if (Program.isReset)
            {
                showSetting();
            }
            
            
        }

        /// <summary>
        /// 文字列を暗号化する
        /// </summary>
        /// <param name="sourceString">暗号化する文字列</param>
        /// <param name="password">暗号化に使用するパスワード</param>
        /// <returns>暗号化された文字列</returns>
        public static string EncryptString(string sourceString, string password)
        {
            //RijndaelManagedオブジェクトを作成
            System.Security.Cryptography.RijndaelManaged rijndael =
                new System.Security.Cryptography.RijndaelManaged();

            //パスワードから共有キーと初期化ベクタを作成
            byte[] key, iv;
            GenerateKeyFromPassword(
                password, rijndael.KeySize, out key, rijndael.BlockSize, out iv);
            rijndael.Key = key;
            rijndael.IV = iv;

            //文字列をバイト型配列に変換する
            byte[] strBytes = System.Text.Encoding.UTF8.GetBytes(sourceString);

            //対称暗号化オブジェクトの作成
            System.Security.Cryptography.ICryptoTransform encryptor =
                rijndael.CreateEncryptor();
            //バイト型配列を暗号化する
            //復号化に失敗すると例外CryptographicExceptionが発生
            byte[] encBytes = encryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
            //閉じる
            encryptor.Dispose();

            //バイト型配列を文字列に変換して返す
            return System.Convert.ToBase64String(encBytes);
        }

        /// <summary>
        /// 暗号化された文字列を復号化する
        /// </summary>
        /// <param name="sourceString">暗号化された文字列</param>
        /// <param name="password">暗号化に使用したパスワード</param>
        /// <returns>復号化された文字列</returns>
        public static string DecryptString(string sourceString, string password)
        {
            if (String.IsNullOrEmpty(sourceString))
            {
                return "";
            }

            //RijndaelManagedオブジェクトを作成
            System.Security.Cryptography.RijndaelManaged rijndael =
                new System.Security.Cryptography.RijndaelManaged();

            //パスワードから共有キーと初期化ベクタを作成
            byte[] key, iv;
            GenerateKeyFromPassword(
                password, rijndael.KeySize, out key, rijndael.BlockSize, out iv);
            rijndael.Key = key;
            rijndael.IV = iv;

            //文字列をバイト型配列に戻す
            byte[] strBytes = System.Convert.FromBase64String(sourceString);

            //対称暗号化オブジェクトの作成
            System.Security.Cryptography.ICryptoTransform decryptor =
                rijndael.CreateDecryptor();
            //バイト型配列を復号化する
            byte[] decBytes = decryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
            //閉じる
            decryptor.Dispose();

            //バイト型配列を文字列に戻して返す
            return System.Text.Encoding.UTF8.GetString(decBytes);
        }

        /// <summary>
        /// パスワードから共有キーと初期化ベクタを生成する
        /// </summary>
        /// <param name="password">基になるパスワード</param>
        /// <param name="keySize">共有キーのサイズ（ビット）</param>
        /// <param name="key">作成された共有キー</param>
        /// <param name="blockSize">初期化ベクタのサイズ（ビット）</param>
        /// <param name="iv">作成された初期化ベクタ</param>
        private static void GenerateKeyFromPassword(string password,
            int keySize, out byte[] key, int blockSize, out byte[] iv)
        {
            //パスワードから共有キーと初期化ベクタを作成する
            //saltを決める
            byte[] salt = System.Text.Encoding.UTF8.GetBytes("saltは必ず8バイト以上");
            //Rfc2898DeriveBytesオブジェクトを作成する
            System.Security.Cryptography.Rfc2898DeriveBytes deriveBytes =
                new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt);
            //.NET Framework 1.1以下の時は、PasswordDeriveBytesを使用する
            //System.Security.Cryptography.PasswordDeriveBytes deriveBytes =
            //    new System.Security.Cryptography.PasswordDeriveBytes(password, salt);
            //反復処理回数を指定する デフォルトで1000回
            deriveBytes.IterationCount = 1000;

            //共有キーと初期化ベクタを生成する
            key = deriveBytes.GetBytes(keySize / 8);
            iv = deriveBytes.GetBytes(blockSize / 8);
        }
    }
}
