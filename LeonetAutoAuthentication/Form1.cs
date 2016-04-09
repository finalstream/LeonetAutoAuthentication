using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace LeonetAutoAuthentication
{
    public partial class Form1 : Form
    {
        private string pw = "FINALSTREAM.NET";
        private int errcnt;

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
            Properties.Settings.Default.UserId = txtUserId.Text;
            Properties.Settings.Default.Password = EncryptString(txtPassword.Text, pw);
            Properties.Settings.Default.Save();

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

        private void button1_Click(object sender, EventArgs e)
        {
            errcnt = 0;
            execAuth();
        }

        private void execAuth()
        {

            
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

            //HttpWebRequestの作成
            System.Net.HttpWebRequest webreq = (System.Net.HttpWebRequest)
                System.Net.WebRequest.Create(Properties.Settings.Default.RequestURL.Replace("#GATEWAY#",defaultGatwayAddress));

            //認証の設定
            webreq.Credentials =
                new System.Net.NetworkCredential(txtUserId.Text, txtPassword.Text);

            // timeout 10sec
            webreq.Timeout = Properties.Settings.Default.WaitMillisecond;

            System.Net.HttpWebResponse webres = null;
            notifyIcon.BalloonTipTitle = "LeonetAutoAuthentication";
            try
            {
                //HttpWebResponseの取得
                webres =
                    (System.Net.HttpWebResponse) webreq.GetResponse();

                //受信して表示
                
                if (webres.StatusCode == HttpStatusCode.OK)
                {
                    notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    notifyIcon.BalloonTipText = "Success";
                    notifyIcon.ShowBalloonTip(Properties.Settings.Default.ViewMillisecond);
                }
                else
                {
                    notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
                    notifyIcon.BalloonTipText = webres.StatusDescription;
                }
            }
            catch(Exception ex)
            {
                System.Threading.Thread.Sleep(3000);
                errcnt++;
                ProcessStartInfo psi =
                        new ProcessStartInfo();

                //ComSpecのパスを取得する
                psi.FileName = System.Environment.GetEnvironmentVariable("ComSpec");

                //出力を読み取れるようにする
                psi.RedirectStandardInput = false;
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                //ウィンドウを表示しないようにする
                psi.CreateNoWindow = true;
                //コマンドラインを指定（"/c"は実行後閉じるために必要）
                psi.Arguments = @"/c ipconfig /renew";
                //起動
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);
                //出力を読み取る
                string results = p.StandardOutput.ReadToEnd();
                //WaitForExitはReadToEndの後である必要がある
                //(親プロセス、子プロセスでブロック防止のため)
                p.WaitForExit();
                
                if (errcnt < 10)
                {
                    execAuth();
                    return;
                }
                notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
                notifyIcon.BalloonTipText =  ex.Message;
                if (ex.InnerException != null)
                {
                    notifyIcon.BalloonTipText += "\n" + ex.InnerException.Message;
                }
                notifyIcon.ShowBalloonTip(Properties.Settings.Default.ViewMillisecond);
            }
            

            if (webres != null && webres.StatusCode == HttpStatusCode.OK)
            {

                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (sw.ElapsedMilliseconds < Properties.Settings.Default.ViewMillisecond)
                {
                    // 何らかの処理
                    System.Threading.Thread.Sleep(10);

                    // メッセージ・キューにあるWindowsメッセージをすべて処理する
                    Application.DoEvents();
                }
                exit();
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

        private void Form1_Load(object sender, EventArgs e)
        {
            txtUserId.Text = Properties.Settings.Default.UserId;
            txtPassword.Text = DecryptString(Properties.Settings.Default.Password, pw);
            
            if (!Program.isReset && !String.IsNullOrEmpty(txtUserId.Text) && !String.IsNullOrEmpty(txtPassword.Text))
            {
                // wait
                //System.Threading.Thread.Sleep(Properties.Settings.Default.WaitMillisecond);

                execAuth();
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
