using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace LeonetAutoAuthentication
{
    static class Program
    {
        public static bool isReset = false;
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            if (args.Length > 0)
            {
                if ("/reset".Equals(args[0]))
                {
                    isReset = true;
                }

                string leonetlink = System.Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\leonet.lnk";

                if ("/regist".Equals(args[0]))
                {
                    CreateShortCut(
                        leonetlink,
                        Application.ExecutablePath,
                        "");
                    MessageBox.Show("スタートアップに登録しました。");
                    Environment.Exit(0);
                }

                if ("/unregist".Equals(args[0]))
                {
                    if (File.Exists(leonetlink))
                    {
                        File.Delete(leonetlink);
                        MessageBox.Show("スタートアップを解除しました。");
                        
                    }
                    Environment.Exit(0);
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        
        /// <summary>
        /// ショートカットの作成
        /// </summary>
        /// <remarks>WSHを使用して、ショートカット(lnkファイル)を作成します。(遅延バインディング)</remarks>
        /// <param name="path">出力先のファイル名(*.lnk)</param>
        /// <param name="targetPath">対象のアセンブリ(*.exe)</param>
        /// <param name="description">説明</param>
        private static void CreateShortCut(String path, String targetPath, String description)
        {
            //using System.Reflection;

            // WSHオブジェクトを作成し、CreateShortcutメソッドを実行する
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            object shortCut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });

            Type shortcutType = shell.GetType();
            // TargetPathプロパティをセットする
            shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortCut, new object[] { targetPath });
            // Descriptionプロパティをセットする
            shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortCut, new object[] { description });
            // Saveメソッドを実行する
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortCut, null);

        }
        
    }
}
